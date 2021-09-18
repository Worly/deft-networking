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
    public class AC_MiddlewareTests
    {
        private class Router : DeftRouter
        {
            public Router()
            {
                UseMiddleware(MiddlewareOne);
                UseMiddleware(MiddlewareTwo);
                Add<TestArgs, TestResponse>("/", Index);
                Add<ExceptionRouter>("exceptionRouter");
            }

            private async Task<DeftResponse> MiddlewareOne(DeftRequest request, Func<Task<DeftResponse>> next)
            {
                if (!request.Headers.ContainsKey("callStack"))
                    request.Headers["callStack"] = "";

                if (request.Headers.ContainsKey("callStack"))
                    request.Headers["callStack"] += " - MiddlewareOne(entry)";

                var result = await next();

                if (result.Headers.ContainsKey("callStack"))
                    result.Headers["callStack"] += " - MiddlewareOne(exit)";

                return result;
            }

            private async Task<DeftResponse> MiddlewareTwo(DeftRequest request, Func<Task<DeftResponse>> next)
            {
                if (!request.Headers.ContainsKey("callStack"))
                    request.Headers["callStack"] = "";

                if (request.Headers.ContainsKey("callStack"))
                    request.Headers["callStack"] += " - MiddlewareTwo(entry)";

                var result = await next();

                if (result.Headers.ContainsKey("callStack"))
                    result.Headers["callStack"] += " - MiddlewareTwo(exit)";

                return result;
            }

            private DeftResponse<TestResponse> Index(DeftConnectionOwner from, DeftRequest<TestArgs> req)
            {
                return DeftResponse<TestResponse>
                    .From(ResponseStatusCode.OK)
                    .WithHeader("callStack", req.Headers["callStack"] + " - Index");
            }
        }

        private class ExceptionRouter : DeftRouter
        {
            public ExceptionRouter()
            {
                UseMiddleware(HandleExceptionMiddleware);
                UseMiddleware(ExceptionMiddleware);
            }

            private async Task<DeftResponse> HandleExceptionMiddleware(DeftRequest request, Func<Task<DeftResponse>> next)
            {
                try
                {
                    return await next();
                }
                catch (Exception e)
                {
                    if (request.Headers.ContainsKey("handle-exception"))
                    {
                        return new DeftResponse(new Dictionary<string, string>()
                        {
                            { "exception-handled-message", e.Message }
                        });
                    }
                    else
                        throw;
                }
            }

            private async Task<DeftResponse> ExceptionMiddleware(DeftRequest request, Func<Task<DeftResponse>> next)
            {
                if (request.Headers.ContainsKey("throw-exception"))
                    throw new Exception(request.Headers["throw-exception"]);
                return await next();
            }
        }

        static string MainURL = "MiddlewareTests/";

        static AC_MiddlewareTests()
        {
            DeftMethods.DefaultRouter.Add<Router>(MainURL);
        }

        [TestMethod]
        public async Task WhenMultipleMiddlewareAreUsed_ShouldFollowOrder()
        {
            var port = 5000;
            var callStack = " - MiddlewareOne(entry) - MiddlewareTwo(entry) - Index - MiddlewareTwo(exit) - MiddlewareOne(exit)";

            var clientListener = new ClientListener(port);

            var server = await DeftConnector.ConnectAsync<Server>("localhost", port, "Server");

            var response = await server.SendMethodAsync<TestArgs, TestResponse>(MainURL + "/", null);

            response.Ok.Should().BeTrue();
            response.Headers.Should().Contain(new KeyValuePair<string, string>("callStack", callStack));
        }

        [TestMethod]
        public async Task WhenExceptionIsThrownInMiddleware_ShouldResultInInternalServerError()
        {
            var port = 5001;
            var exceptionMessage = "saadsfadsfsadf";
            var clientListener = new ClientListener(port);

            var server = await DeftConnector.ConnectAsync<Server>("localhost", port, "Server");

            var response = await server.SendMethodAsync<TestArgs, TestResponse>(MainURL + "exceptionRouter", null, new Dictionary<string, string>()
            {
                { "throw-exception", exceptionMessage }
            });

            response.Ok.Should().BeFalse();
            response.StatusCode.Should().Be(ResponseStatusCode.InternalServerError);
            response.Headers.Should().Contain(new KeyValuePair<string, string>("exception-type", "Exception"));
            response.Headers.Should().Contain(new KeyValuePair<string, string>("exception-message", exceptionMessage));
        }

        [TestMethod]
        public async Task WhenExceptionIsHandled_ShouldWorkCorrectly()
        {
            var port = 5002;
            var exceptionMessage = "saadsfadsfsadf";
            var clientListener = new ClientListener(port);

            var server = await DeftConnector.ConnectAsync<Server>("localhost", port, "Server");

            var response = await server.SendMethodAsync<TestArgs, TestResponse>(MainURL + "exceptionRouter", null, new Dictionary<string, string>()
            {
                { "throw-exception", exceptionMessage },
                { "handle-exception", "true" }
            });

            response.Ok.Should().BeTrue();
            response.StatusCode.Should().Be(ResponseStatusCode.OK);
            response.Headers.Should().Contain(new KeyValuePair<string, string>("exception-handled-message", exceptionMessage));
        }
    }
}
