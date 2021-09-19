using Deft;
using DeftUnitTests.ProjectClasses;
using DeftUnitTests.Utils;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DeftUnitTests
{
    [TestClass]
    public class AB_MethodTests
    {
        private class FirstRouter : DeftRouter
        {
            public FirstRouter()
            {
                Add<TestArgs, TestResponse>("/", Index);
                Add<SecondTestArgs, SecondTestResponse>("secondTestArgs", SecondTest);
                Add<TestArgs, TestResponse>("headersTest", HeadersTest);
                Add<TestArgs, TestResponse>("testNullHeaders", NullHeadersTest);
                Add<TestArgs, TestResponse>("testEmptyHeaders", EmptyHeadersTest, ThreadOptions.ExecuteAsync);
                Add<TestArgs, TestResponse>("testNullBody", NullBodyTest);
                Add<TestArgs, TestResponse>("testException", ExceptionTest);
                Add<TestArgs, TestResponse>("nested/main", NestedTest);
                Add<NestedRouter>("nested");
                Add<TestArgs, TestResponse>("timeoutTest", TimeoutTest);
                AddFor<SecondClient, TestArgs, TestResponse>("clientTypeTest", ClientTypeTest);
            }

            private DeftResponse<TestResponse> Index(DeftConnectionOwner from, TestArgs req)
            {
                return DeftResponse<TestResponse>.From(new TestResponse()
                {
                    DatePlusOneDay = req.Date.AddDays(1),
                    Message = "Hello " + req.Message + " from /",
                    NumberTimesTwo = req.Number * 2,
                    SortedNumberList = req.NumberList
                }).WithHeader("route", "/");
            }

            private SecondTestResponse SecondTest(DeftConnectionOwner from, DeftRequest<SecondTestArgs> req)
            {
                return new SecondTestResponse()
                {
                    Message = req.Body.Message + " hello",
                    FloatNumberTimesTwo = req.Body.FloatNumber * 2
                };
            }

            private DeftResponse<TestResponse> HeadersTest(DeftConnectionOwner from, DeftRequest<TestArgs> req)
            {
                return DeftResponse<TestResponse>
                    .From(new TestResponse() { })
                    .WithHeader("header1", "header1")
                    .WithHeader("header2", "header2")
                    .WithHeader("sentHeader", req.Headers["sentHeader"] + " hello")
                    .WithStatusCode(ResponseStatusCode.Unauthorized);
            }

            private DeftResponse<TestResponse> NullHeadersTest(DeftConnectionOwner from, DeftRequest<TestArgs> req)
            {
                return DeftResponse<TestResponse>
                    .From(new TestResponse())
                    .WithStatusCode(req.Headers.Count == 0 ? ResponseStatusCode.OK : ResponseStatusCode.BadRequest);
            }

            private DeftResponse<TestResponse> EmptyHeadersTest(DeftConnectionOwner from, DeftRequest<TestArgs> req)
            {
                return DeftResponse<TestResponse>
                    .From(new TestResponse(), new Dictionary<string, string>())
                    .WithStatusCode(req.Headers.Keys.Count() == 0 ? ResponseStatusCode.OK : ResponseStatusCode.BadRequest);
            }

            private DeftResponse<TestResponse> NullBodyTest(DeftConnectionOwner from, DeftRequest<TestArgs> req)
            {
                return DeftResponse<TestResponse>
                    .From(null)
                    .WithStatusCode(req.Body == null ? ResponseStatusCode.OK : ResponseStatusCode.BadRequest);
            }

            private DeftResponse<TestResponse> ExceptionTest(DeftConnectionOwner from, DeftRequest<TestArgs> req)
            {
                throw new NotImplementedException("This method was not implemented on purpose for testing");
            }

            private DeftResponse<TestResponse> NestedTest(DeftConnectionOwner from, DeftRequest<TestArgs> req)
            {
                return DeftResponse<TestResponse>.From(new TestResponse())
                .WithHeader("route", "/nested/main");
            }

            private DeftResponse<TestResponse> TimeoutTest(DeftConnectionOwner from, DeftRequest<TestArgs> req)
            {
                Thread.Sleep(DeftConfig.MethodTimeoutMs + 500);
                return new TestResponse() { };
            }

            private TestResponse ClientTypeTest(SecondClient from, TestArgs req)
            {
                return new TestResponse() { };
            }
        }

        private class NestedRouter : DeftRouter
        {
            public NestedRouter()
            {
                Add<TestArgs, TestResponse>("route", RouteTest);
                Add<TestArgs, TestResponse>("/", Index);
            }

            private DeftResponse<TestResponse> RouteTest(DeftConnectionOwner from, DeftRequest<TestArgs> req)
            {
                return DeftResponse<TestResponse>.From(new TestResponse())
                    .WithHeader("route", "/nested/route");
            }

            private DeftResponse<TestResponse> Index(DeftConnectionOwner from, DeftRequest<TestArgs> req)
            {
                return DeftResponse<TestResponse>.From(new TestResponse())
                    .WithHeader("route", "/nested");
            }
        }

        static AB_MethodTests()
        {
            DeftMethods.DefaultRouter.Add<FirstRouter>("MethodTests");
        }

        static string MainURL = "MethodTests/";

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

            server.SendMethod<TestArgs, TestResponse>(MainURL + "/", args, null, r =>
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

            server.SendMethod<TestArgs, TestResponse>(MainURL + "thisRouteDoesNotExist", args, null, r => response = r);

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

            server.SendMethod<TestArgs, TestResponse>(MainURL + "headersTest", new TestArgs(), new Dictionary<string, string>() {
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
        public async Task WhenNullHeadersAreSent_ShouldReceiveEmptyHeaders()
        {
            var port = 4002;
            var clientListener = new ClientListener(port);

            var server = await DeftConnector.ConnectAsync<Server>("localhost", port, "Server");

            DeftResponse<TestResponse> response = null;

            server.SendMethod<TestArgs, TestResponse>(MainURL + "testNullHeaders", new TestArgs(), null, r =>
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

            server.SendMethod<TestArgs, TestResponse>(MainURL + "testEmptyHeaders", new TestArgs(), new Dictionary<string, string>(), r =>
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

            server.SendMethod<TestArgs, TestResponse>(MainURL + "testNullBody", null, null, r =>
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

            server.SendMethod<TestArgs, TestResponse>(MainURL + "testException", null, null, r =>
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

            server.SendMethod<TestArgs, TestResponse>(MainURL + "/nested/route", null, null, r =>
            {
                response = r;
            });

            await TaskUtils.WaitFor(() => response != null);

            response.Ok.Should().BeTrue();
            response.Headers.Should().Contain(new KeyValuePair<string, string>("route", "/nested/route"));


            response = null;
            server.SendMethod<TestArgs, TestResponse>(MainURL + "/nested", null, null, r =>
            {
                response = r;
            });

            await TaskUtils.WaitFor(() => response != null);

            response.Ok.Should().BeTrue();
            response.Headers.Should().Contain(new KeyValuePair<string, string>("route", "/nested"));


            response = null;
            server.SendMethod<TestArgs, TestResponse>(MainURL + "/nested/main", null, null, r =>
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

            server.SendMethod<TestArgs, TestResponse>(MainURL + "/", new TestArgs(), null, r => response = r);

            await TaskUtils.WaitFor(() => response != null);

            response.Ok.Should().BeFalse();
            response.StatusCode.Should().Be(ResponseStatusCode.NotReachable);
        }

        [TestMethod]
        public async Task WhenResponseNotComing_ShouldRespondWithTimeout()
        {
            var normalTimeoutMs = DeftConfig.MethodTimeoutMs;
            DeftConfig.MethodTimeoutMs = 200;

            var port = 4008;
            var clientListener = new ClientListener(port);

            var server = await DeftConnector.ConnectAsync<Server>("localhost", port, "Server");

            DeftResponse<TestResponse> response = null;

            server.SendMethod<TestArgs, TestResponse>(MainURL + "/timeoutTest", new TestArgs(), null, r => response = r);

            await TaskUtils.WaitFor(() => response != null);

            DeftConfig.MethodTimeoutMs = normalTimeoutMs;

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

            serverAsSecond.SendMethod<TestArgs, TestResponse>(MainURL + "/clientTypeTest", new TestArgs(), null, r => response = r);

            await TaskUtils.WaitFor(() => response != null);

            response.Ok.Should().BeTrue();
            response.StatusCode.Should().Be(ResponseStatusCode.OK);


            response = null;

            serverAsFirst.SendMethod<TestArgs, TestResponse>(MainURL + "/clientTypeTest", new TestArgs(), null, r => response = r);

            await TaskUtils.WaitFor(() => response != null);

            response.Ok.Should().BeFalse();
            response.StatusCode.Should().Be(ResponseStatusCode.Unauthorized);
        }

        [TestMethod]
        public async Task WhenSendingAlot_ShouldWorkCorrectly()
        {
            var port = 4011;
            var clientListener = new ClientListener(port);

            var server = await DeftConnector.ConnectAsync<Server>("localhost", port, "Server");

            uint count = 1000;
            uint okCount = 0;
            uint notOkCount = 0;

            for (int i = 0; i < count; i++)
            {
                server.SendMethod<SecondTestArgs, SecondTestResponse>(MainURL + "/SecondTestArgs", new SecondTestArgs() { FloatNumber = 10, Message = "aa" }, null, r =>
                {
                    if (r.Ok)
                        okCount++;
                    else
                        notOkCount++;
                });
            }

            await TaskUtils.WaitFor(() => okCount + notOkCount >= count);

            notOkCount.Should().Be(0);
            okCount.Should().Be(count);
        }

        [TestMethod]
        public async Task WhenSendingLongMessage_ShouldWorkCorrectly()
        {
            var port = 4012;
            var clientListener = new ClientListener(port);

            var messageBuilder = new StringBuilder();
            for (int i = 0; i < 10000; i++)
                messageBuilder.Append("-message text-");

            var args = new SecondTestArgs()
            {
                FloatNumber = 1,
                Message = messageBuilder.ToString()
            };

            var server = await DeftConnector.ConnectAsync<Server>("localhost", port, "Server");

            DeftResponse<SecondTestResponse> response = null;

            server.SendMethod<SecondTestArgs, SecondTestResponse>(MainURL + "/SecondTestArgs", args, null, r => response = r);

            await TaskUtils.WaitFor(() => response != null);

            response.Ok.Should().BeTrue();
            response.Body.Message.Should().Be(args.Message + " hello");
        }
    }
}
