using Deft;
using DeftUnitTests.ProjectClasses;
using DeftUnitTests.Utils;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SimpleInjector;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DeftUnitTests
{
    [TestClass]
    public class AE_DependencyInjectionTests
    {
        private class Router : DeftRouter
        {
            private ITestService testService;

            public Router(ITestService testService)
            {
                this.testService = testService;

                Add<TestArgs, TestResponse>("/async", Async, ThreadOptions.ExecuteAsync);
            }

            private DeftResponse<TestResponse> Async(DeftConnectionOwner from, DeftRequest<TestArgs> req)
            {
                int n = int.Parse(req.Headers["n"]);
                for (int i = 0; i < n; i++)
                {
                    this.testService.IncrementCounter();
                    Thread.Sleep(100);
                }

                return DeftResponse<TestResponse>
                    .From(ResponseStatusCode.OK)
                    .WithHeader("counter", this.testService.GetCounter().ToString());
            }
        }

        static string MainURL = "DependencyInjectionTests/";

        static AE_DependencyInjectionTests()
        {
            Deft.Deft.InitDependencyInjection(new InjectionContainer(), builder => builder.Register<ITestService, TestService>());

            DeftMethods.AddRouter<Router>("DependencyInjectionTests");
        }

        [TestMethod]
        public async Task WhenRouteHandlersAreAsync_ShouldInjectDifferentInstances()
        {
            var port = 7000;
            var clientListener = new ClientListener(port);

            var server = await DeftConnector.ConnectAsync<Server>("localhost", port, "Server");

            DeftResponse<TestResponse> response1 = null;
            DeftResponse<TestResponse> response2 = null;

            server.SendMethod<TestArgs, TestResponse>(MainURL + "async", null, new Dictionary<string, string>()
            {
                { "n", "8" }
            }, r => response1 = r);
            server.SendMethod<TestArgs, TestResponse>(MainURL + "async", null, new Dictionary<string, string>()
            {
                { "n", "11" }
            }, r => response2 = r);

            await TaskUtils.WaitFor(() => response1 != null);
            await TaskUtils.WaitFor(() => response2 != null);

            response1.Ok.Should().BeTrue();
            response2.Ok.Should().BeTrue();

            response1.Headers.Should().Contain(new KeyValuePair<string, string>("counter", "8"));
            response2.Headers.Should().Contain(new KeyValuePair<string, string>("counter", "11"));
        }
    }

}
