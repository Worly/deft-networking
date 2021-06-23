using Deft;
using DeftUnitTests.ProjectClasses;
using DeftUnitTests.Utils;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DeftUnitTests
{
    [TestClass]
    public class MethodTests
    {
        static MethodTests()
        {
            DeftMethods.DefaultRouter.Add<TestArgs, TestResponse>("/", (from, req) =>
            {
                req.Body.NumberList.Sort();

                return DeftResponse<TestResponse>.From(new TestResponse()
                {
                    DatePlusOneDay = req.Body.Date.AddDays(1),
                    Message = "Hello " + req.Body.Message + " from /",
                    NumberTimesTwo = req.Body.Number * 2,
                    SortedNumberList = req.Body.NumberList
                }).WithHeader("route", "/");
            });

            DeftMethods.DefaultRouter.Add<SecondTestArgs, SecondTestResponse>("/secondTestArgs", (from, req) =>
            {
                return new SecondTestResponse()
                {
                    Message = req.Body.Message + " hello",
                    FloatNumberTimesTwo = req.Body.FloatNumber * 2
                };
            });

            DeftMethods.DefaultRouter.Add<TestArgs, TestResponse>("headersTest", (from, req) =>
            {
                return DeftResponse<TestResponse>
                    .From(new TestResponse() { })
                    .WithHeader("header1", "header1")
                    .WithHeader("header2", "header2")
                    .WithHeader("sentHeader", req.Headers["sentHeader"] + " hello")
                    .WithStatusCode(ResponseStatusCode.Unauthorized);
            });

            DeftMethods.DefaultRouter.Add<TestArgs, TestResponse>("testNullHeaders", (from, req) =>
            {
                return DeftResponse<TestResponse>
                    .From(new TestResponse())
                    .WithStatusCode(req.Headers == null ? ResponseStatusCode.OK : ResponseStatusCode.BadRequest);
            });

            DeftMethods.DefaultRouter.Add<TestArgs, TestResponse>("testEmptyHeaders", (from, req) =>
            {
                return DeftResponse<TestResponse>
                    .From(new TestResponse(), new Dictionary<string, string>())
                    .WithStatusCode(req.Headers.Keys.Count() == 0 ? ResponseStatusCode.OK : ResponseStatusCode.BadRequest);
            });

            DeftMethods.DefaultRouter.Add<TestArgs, TestResponse>("testNullBody", (from, req) =>
            {
                return DeftResponse<TestResponse>
                    .From(null)
                    .WithStatusCode(req.Body == null ? ResponseStatusCode.OK : ResponseStatusCode.BadRequest);
            });

            DeftMethods.DefaultRouter.Add<TestArgs, TestResponse>("testException", (from, req) =>
            {
                throw new NotImplementedException("This method was not implemented on purpose for testing");
            });

            var nestedRouter = new Router();
            nestedRouter.Add<TestArgs, TestResponse>("route", (from, req) =>
            {
                _ = req.Body;
                return DeftResponse<TestResponse>.From(new TestResponse())
                .WithHeader("route", "/nested/route");
            });

            DeftMethods.DefaultRouter.Add("nested", nestedRouter);

            DeftMethods.DefaultRouter.Add<TestArgs, TestResponse>("nested/main", (from, req) =>
            {
                _ = req.Body;
                return DeftResponse<TestResponse>.From(new TestResponse())
                .WithHeader("route", "/nested/main");
            });

            nestedRouter.Add<TestArgs, TestResponse>("/", (from, req) =>
            {
                _ = req.Body;
                return DeftResponse<TestResponse>.From(new TestResponse())
                .WithHeader("route", "/nested");
            });

            DeftMethods.DefaultRouter.Add<TestArgs, TestResponse>("timeoutTest", (from, req) =>
            {
                Thread.Sleep(DeftConfig.MethodTimeoutMs + 500);
                return new TestResponse() { };
            });

            DeftMethods.DefaultRouter.Add<SecondClient, TestArgs, TestResponse>("clientTypeTest", (from, req) =>
            {
                return new TestResponse() { };
            });
        }

        [TestMethod]
        public async Task WhenMissingRouteIsTargeted_ShouldReturnNotFound()
        {
            var port = 4000;
            var clientListener = new ClientListener(port);

            var server = await DeftConnector.ConnectAsync<Server>("localhost", port, "Server");

            var args = new TestArgs()
            {
                Date = DateTime.UtcNow,
                Message = "Message1",
                Number = 35,
                NumberList = new List<int>() { 5, 3, 6, 2, 3 }
            };

            DeftResponse<TestResponse> response = null;

            server.SendMethod<TestArgs, TestResponse>("/", args, null, r =>
            {
                response = r;
            });

            await TaskUtils.WaitFor(() => response != null);

            response.Ok.Should().BeTrue();
            response.Body.DatePlusOneDay.Should().Be(args.Date.AddDays(1));
            response.Body.Message.Should().Be("Hello " + args.Message + " from /");
            response.Body.NumberTimesTwo.Should().Be(args.Number * 2);
            response.Body.SortedNumberList.Should().BeEquivalentTo(args.NumberList.OrderBy(o => o));


            response = null;

            server.SendMethod<TestArgs, TestResponse>("thisRouteDoesNotExist", args, null, r => response = r);

            await TaskUtils.WaitFor(() => response != null);

            response.Ok.Should().BeFalse();
            response.Body.Should().BeNull();
            response.StatusCode.Should().Be(ResponseStatusCode.NotFound);
        }

        [TestMethod]
        public async Task WhenHeadersAreSent_ShouldReceiveItCorrectly()
        {
            var port = 4001;
            var clientListener = new ClientListener(port);

            var server = await DeftConnector.ConnectAsync<Server>("localhost", port, "Server");

            DeftResponse<TestResponse> response = null;

            server.SendMethod<TestArgs, TestResponse>("headersTest", new TestArgs(), new Dictionary<string, string>() {
                { "sentHeader", "sentValue" }
            }, r =>
            {
                response = r;
            });

            await TaskUtils.WaitFor(() => response != null);

            response.StatusCode.Should().Be(ResponseStatusCode.Unauthorized);
            response.Headers.Should().Contain(new KeyValuePair<string, string>("header1", "header1"));
            response.Headers.Should().Contain(new KeyValuePair<string, string>("header2", "header2"));
            response.Headers.Should().Contain(new KeyValuePair<string, string>("sentHeader", "sentValue hello"));
        }

        [TestMethod]
        public async Task WhenNullHeadersAreSent_ShouldReceiveNullHeaders()
        {
            var port = 4002;
            var clientListener = new ClientListener(port);

            var server = await DeftConnector.ConnectAsync<Server>("localhost", port, "Server");

            DeftResponse<TestResponse> response = null;

            server.SendMethod<TestArgs, TestResponse>("testNullHeaders", new TestArgs(), null, r =>
            {
                response = r;
            });

            await TaskUtils.WaitFor(() => response != null);

            response.Ok.Should().BeTrue();
            response.Headers.Should().BeNull();
        }

        [TestMethod]
        public async Task WhenEmpyHeadersAreSent_ShouldReceiveEmptyHeaders()
        {
            var port = 4003;
            var clientListener = new ClientListener(port);

            var server = await DeftConnector.ConnectAsync<Server>("localhost", port, "Server");

            DeftResponse<TestResponse> response = null;

            server.SendMethod<TestArgs, TestResponse>("testEmptyHeaders", new TestArgs(), new Dictionary<string, string>(), r =>
            {
                response = r;
            });

            await TaskUtils.WaitFor(() => response != null);

            response.Ok.Should().BeTrue();
            response.Headers.Should().BeEmpty();
        }

        [TestMethod]
        public async Task WhenNullBodyIsSent_ShouldReceiveNullBody()
        {
            var port = 4004;
            var clientListener = new ClientListener(port);

            var server = await DeftConnector.ConnectAsync<Server>("localhost", port, "Server");

            DeftResponse<TestResponse> response = null;

            server.SendMethod<TestArgs, TestResponse>("testNullBody", null, null, r =>
            {
                response = r;
            });

            await TaskUtils.WaitFor(() => response != null);

            response.Ok.Should().BeTrue();
            response.Body.Should().BeNull();
        }

        [TestMethod]
        public async Task WhenExceptionIsThrown_ShouldReturnException()
        {
            var port = 4005;
            var clientListener = new ClientListener(port);

            var server = await DeftConnector.ConnectAsync<Server>("localhost", port, "Server");

            DeftResponse<TestResponse> response = null;

            server.SendMethod<TestArgs, TestResponse>("testException", null, null, r =>
            {
                response = r;
            });

            await TaskUtils.WaitFor(() => response != null);

            response.Ok.Should().BeFalse();
            response.StatusCode.Should().Be(ResponseStatusCode.InternalServerError);
            response.Body.Should().BeNull();
            response.Headers.Should().Contain(new KeyValuePair<string, string>("exception-type", "NotImplementedException"));
            response.Headers.Should().Contain(new KeyValuePair<string, string>("exception-message", "This method was not implemented on purpose for testing"));
        }

        [TestMethod]
        public async Task WhenTargetingNestedRoutes_ShouldWorkCorrectly()
        {
            var port = 4006;
            var clientListener = new ClientListener(port);

            var server = await DeftConnector.ConnectAsync<Server>("localhost", port, "Server");

            DeftResponse<TestResponse> response = null;

            server.SendMethod<TestArgs, TestResponse>("/nested/route", null, null, r =>
            {
                response = r;
            });

            await TaskUtils.WaitFor(() => response != null);

            response.Ok.Should().BeTrue();
            response.Headers.Should().Contain(new KeyValuePair<string, string>("route", "/nested/route"));


            response = null;
            server.SendMethod<TestArgs, TestResponse>("/nested", null, null, r =>
            {
                response = r;
            });

            await TaskUtils.WaitFor(() => response != null);

            response.Ok.Should().BeTrue();
            response.Headers.Should().Contain(new KeyValuePair<string, string>("route", "/nested"));


            response = null;
            server.SendMethod<TestArgs, TestResponse>("/nested/main", null, null, r =>
            {
                response = r;
            });

            await TaskUtils.WaitFor(() => response != null);

            response.Ok.Should().BeTrue();
            response.Headers.Should().Contain(new KeyValuePair<string, string>("route", "/nested/main"));
        }

        [TestMethod]
        public async Task WhenSentToNotConnectedClient_ShouldRespondWithTimeout()
        {
            var port = 4007;
            var clientListener = new ClientListener(port);

            var server = await DeftConnector.ConnectAsync<Server>("localhost", port, "Server");

            clientListener.ConnectedClients.ElementAt(0).Disconnect();

            await TaskUtils.WaitFor(() => !server.IsConnected);

            DeftResponse<TestResponse> response = null;

            server.SendMethod<TestArgs, TestResponse>("/", new TestArgs(), null, r => response = r);

            await TaskUtils.WaitFor(() => response != null);

            response.Ok.Should().BeFalse();
            response.StatusCode.Should().Be(ResponseStatusCode.NotReachable);
        }

        [TestMethod]
        public async Task WhenResponseNotComing_ShouldRespondWithTimeout()
        {
            DeftConfig.MethodTimeoutMs = 200;

            var port = 4008;
            var clientListener = new ClientListener(port);

            var server = await DeftConnector.ConnectAsync<Server>("localhost", port, "Server");

            DeftResponse<TestResponse> response = null;

            server.SendMethod<TestArgs, TestResponse>("/timeoutTest", new TestArgs(), null, r => response = r);

            await TaskUtils.WaitFor(() => response != null);

            response.Ok.Should().BeFalse();
            response.StatusCode.Should().Be(ResponseStatusCode.Timeout);
        }

        [TestMethod]
        public async Task WhenSendingAsAWrongClientType_ShouldRepondWithUnauthorized()
        {
            var port = 4009;
            var clientListener = new AnyListener<Client>(port);

            var secondPort = 4010;
            var secondClientListener = new AnyListener<SecondClient>(secondPort);

            var serverAsFirst = await DeftConnector.ConnectAsync<Server>("localhost", port, "ServerFirst");
            var serverAsSecond = await DeftConnector.ConnectAsync<Server>("localhost", secondPort, "ServerSecond");

            DeftResponse<TestResponse> response = null;

            serverAsSecond.SendMethod<TestArgs, TestResponse>("/clientTypeTest", new TestArgs(), null, r => response = r);

            await TaskUtils.WaitFor(() => response != null);

            response.Ok.Should().BeTrue();
            response.StatusCode.Should().Be(ResponseStatusCode.OK);


            response = null;

            serverAsFirst.SendMethod<TestArgs, TestResponse>("/clientTypeTest", new TestArgs(), null, r => response = r);

            await TaskUtils.WaitFor(() => response != null);

            response.Ok.Should().BeFalse();
            response.StatusCode.Should().Be(ResponseStatusCode.Unauthorized);
        }
    }
}
