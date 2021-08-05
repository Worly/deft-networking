using Deft;
using DeftUnitTests.ProjectClasses;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DeftUnitTests
{
    [TestClass]
    public class AB_ExceptionTests
    {
        static AB_ExceptionTests()
        {
            var router = new Router();

            router.Add<TestArgs, TestResponse>("throwExceptionBase", (from, req) =>
            {
                throw new ExceptionBase();
            });

            router.Add<TestArgs, TestResponse>("throwExceptionDerived", (from, req) =>
            {
                throw new ExceptionDerived();
            });

            router.Add<TestArgs, TestResponse>("throwExceptionInvalid", (from, req) =>
            {
                throw new InvalidOperationException();
            });

            router.Add<TestArgs, TestResponse>("throwExceptionArgument", (from, req) =>
            {
                throw new ArgumentException();
            });

            router.AddExceptionHandler<ExceptionBase, TestResponse>((from, e) =>
            {
                return DeftResponse<TestResponse>.From(new TestResponse()
                {
                    Message = "woo"
                })
                .WithHeader("handledBy", "exceptionBase");
            });

            router.AddExceptionHandler<ExceptionDerived, TestResponse>((from, e) =>
            {
                return DeftResponse<TestResponse>.From(new TestResponse()
                {
                    Message = "woo"
                })
                .WithHeader("handledBy", "exceptionDerived");
            });

            router.AddExceptionHandler<ArgumentException, TestResponse>((from, e) =>
            {
                throw new ExceptionDerived();
            });

            router.AddExceptionHandler<Exception, TestResponse>((from, e) =>
            {
                return DeftResponse<TestResponse>.From(new TestResponse()
                {
                    Message = "woo"
                })
                .WithHeader("handledBy", "exception");
            });

            var innerRouter = new Router();

            innerRouter.Add<TestArgs, TestResponse>("throwException", (from, req) =>
            {
                throw new ArgumentException();
            });

            innerRouter.Add<TestArgs, TestResponse>("throwDerivedException", (from, req) =>
            {
                throw new ExceptionDerived();
            });

            innerRouter.AddExceptionHandler<ArgumentException, TestResponse>((from, e) =>
            {
                throw new InvalidOperationException();
            });

            router.Add("inner", innerRouter);

            DeftMethods.DefaultRouter.Add("exceptionTests", router);
        }

        [TestMethod]
        public async Task WhenExceptionIsThrown_ShouldHandle()
        {
            var port = 6000;
            var clientListener = new ClientListener(port);

            var server = await DeftConnector.ConnectAsync<Server>("localhost", port, "Server");

            var response = await server.SendMethodAsync<TestArgs, TestResponse>("exceptionTests/throwExceptionBase", null);

            response.Ok.Should().BeTrue();
            response.Body.Message.Should().Be("woo");
            response.Headers.Should().Contain(new KeyValuePair<string, string>("handledBy", "exceptionBase"));
        }

        [TestMethod]
        public async Task WhenDerivedExceptionIsThrown_ShouldHandleCorrectHandler()
        {
            var port = 6001;
            var clientListener = new ClientListener(port);

            var server = await DeftConnector.ConnectAsync<Server>("localhost", port, "Server");

            var response = await server.SendMethodAsync<TestArgs, TestResponse>("exceptionTests/throwExceptionDerived", null);

            response.Ok.Should().BeTrue();
            response.Body.Message.Should().Be("woo");
            response.Headers.Should().Contain(new KeyValuePair<string, string>("handledBy", "exceptionBase"));

            response = await server.SendMethodAsync<TestArgs, TestResponse>("exceptionTests/throwExceptionInvalid", null);

            response.Ok.Should().BeTrue();
            response.Body.Message.Should().Be("woo");
            response.Headers.Should().Contain(new KeyValuePair<string, string>("handledBy", "exception"));
        }

        [TestMethod]
        public async Task WhenExceptionHandlerThrowsException_ShouldRehandle()
        {
            var port = 6002;
            var clientListener = new ClientListener(port);

            var server = await DeftConnector.ConnectAsync<Server>("localhost", port, "Server");

            var response = await server.SendMethodAsync<TestArgs, TestResponse>("exceptionTests/throwExceptionArgument", null);

            response.Ok.Should().BeTrue();
            response.Body.Message.Should().Be("woo");
            response.Headers.Should().Contain(new KeyValuePair<string, string>("handledBy", "exceptionBase"));
        }

        [TestMethod]
        public async Task WhenExceptionIsThrowInNestedRouter_ShouldOverflowToParentIfNotHandledInNested()
        {
            var port = 6003;
            var clientListener = new ClientListener(port);

            var server = await DeftConnector.ConnectAsync<Server>("localhost", port, "Server");

            var response = await server.SendMethodAsync<TestArgs, TestResponse>("exceptionTests/inner/throwException", null);

            response.Ok.Should().BeTrue();
            response.Body.Message.Should().Be("woo");
            response.Headers.Should().Contain(new KeyValuePair<string, string>("handledBy", "exception"));

            response = await server.SendMethodAsync<TestArgs, TestResponse>("exceptionTests/inner/throwDerivedException", null);

            response.Ok.Should().BeTrue();
            response.Body.Message.Should().Be("woo");
            response.Headers.Should().Contain(new KeyValuePair<string, string>("handledBy", "exceptionBase"));
        }
    }
}
