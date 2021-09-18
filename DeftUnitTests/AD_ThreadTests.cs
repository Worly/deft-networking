using Deft;
using DeftUnitTests.ProjectClasses;
using DeftUnitTests.Utils;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DeftUnitTests
{
    [TestClass]
    public class AD_ThreadTests
    {
        private class Router : DeftRouter
        {
            public Router()
            {
                Add<ThreadArgs, ThreadResponse>("/default", Default);
                Add<ThreadArgs, ThreadResponse>("/async", Async, ThreadOptions.ExecuteAsync);
                Add<ThreadArgs, ThreadResponse>("/timeout", Timeout, ThreadOptions.ExecuteAsync);
            }

            private DeftResponse<ThreadResponse> Default(DeftConnectionOwner owner, DeftRequest<ThreadArgs> req)
            {
                return new ThreadResponse()
                {
                    IsDeftThread = DeftThread.TaskQueue.Thread == Thread.CurrentThread,
                    IsPoolThread = Thread.CurrentThread.IsThreadPoolThread,
                    IsResponseThread = Thread.CurrentThread == (DeftConfig.DefaultMethodResponseTaskQueue as TaskQueue).Thread,
                    IsRequestThread = Thread.CurrentThread == (DeftConfig.DefaultRouteHandlerTaskQueue as TaskQueue).Thread
                };
            }

            private DeftResponse<ThreadResponse> Async(DeftConnectionOwner owner, DeftRequest<ThreadArgs> req)
            {
                return new ThreadResponse()
                {
                    IsDeftThread = DeftThread.TaskQueue.Thread == Thread.CurrentThread,
                    IsPoolThread = Thread.CurrentThread.IsThreadPoolThread,
                    IsResponseThread = Thread.CurrentThread == (DeftConfig.DefaultMethodResponseTaskQueue as TaskQueue).Thread,
                    IsRequestThread = Thread.CurrentThread == (DeftConfig.DefaultRouteHandlerTaskQueue as TaskQueue).Thread
                };
            }

            private DeftResponse<ThreadResponse> Timeout(DeftConnectionOwner owner, DeftRequest<ThreadArgs> req)
            {
                Thread.Sleep(DeftConfig.MethodTimeoutMs + 100);
                return new ThreadResponse();
            }
        }

        static object _lock = new object();
        static TaskQueue responseTaskQueue = new TaskQueue();
        static TaskQueue requestTaskQueue = new TaskQueue();
        static string MainURL = "ThreadTests/";

        static AD_ThreadTests()
        {
            DeftMethods.DefaultRouter.Add<Router>(MainURL);
        }

        [TestMethod]
        public async Task WhenDefaultOptions_ShouldExecuteOnSelectedThread()
        {
            var port = 6000;
            var clientListener = new ClientListener(port);

            var server = await DeftConnector.ConnectAsync<Server>("localhost", port, "Server");

            lock (_lock)
            {
                DeftConfig.DefaultMethodResponseTaskQueue = DeftThread.TaskQueue;
                DeftConfig.DefaultRouteHandlerTaskQueue = DeftThread.TaskQueue;

                DeftResponse<ThreadResponse> response = null;
                Thread responseThread = null;

                server.SendMethod<ThreadArgs, ThreadResponse>(MainURL + "default", new ThreadArgs(), null, r =>
                {
                    responseThread = Thread.CurrentThread;
                    response = r;
                });

                TaskUtils.WaitFor(() => response != null).Wait();

                response.Ok.Should().BeTrue();
                response.Body.IsDeftThread.Should().BeTrue();
                response.Body.IsPoolThread.Should().BeFalse();
                response.Body.IsResponseThread.Should().BeTrue(); //because both are DeftThread.TaskQueue
                response.Body.IsRequestThread.Should().BeTrue();
                responseThread.Should().Be(DeftThread.TaskQueue.Thread);
                responseThread.Should().Be((DeftConfig.DefaultMethodResponseTaskQueue as TaskQueue).Thread);


                DeftConfig.DefaultMethodResponseTaskQueue = responseTaskQueue;
                DeftConfig.DefaultRouteHandlerTaskQueue = requestTaskQueue;

                response = null;
                responseThread = null;

                server.SendMethod<ThreadArgs, ThreadResponse>(MainURL + "default", new ThreadArgs(), null, r =>
                {
                    responseThread = Thread.CurrentThread;
                    response = r;
                });

                TaskUtils.WaitFor(() => response != null).Wait();

                response.Ok.Should().BeTrue();
                response.Body.IsDeftThread.Should().BeFalse();
                response.Body.IsPoolThread.Should().BeFalse();
                response.Body.IsResponseThread.Should().BeFalse();
                response.Body.IsRequestThread.Should().BeTrue();
                responseThread.Should().NotBe(DeftThread.TaskQueue.Thread);
                responseThread.Should().Be((DeftConfig.DefaultMethodResponseTaskQueue as TaskQueue).Thread);
            }
        }

        [TestMethod]
        public async Task WhenAsyncOptions_ShouldExecuteOnNewThread()
        {
            var port = 6001;
            var clientListener = new ClientListener(port);

            var server = await DeftConnector.ConnectAsync<Server>("localhost", port, "Server");

            DeftResponse<ThreadResponse> response = null;
            Thread responseThread = null;

            server.SendMethod<ThreadArgs, ThreadResponse>(MainURL + "async", new ThreadArgs(), null, r =>
            {
                responseThread = Thread.CurrentThread;
                response = r;
            }, ThreadOptions.ExecuteAsync);

            await TaskUtils.WaitFor(() => response != null);

            response.Ok.Should().BeTrue();
            response.Body.IsDeftThread.Should().BeFalse();
            response.Body.IsPoolThread.Should().BeTrue();
            response.Body.IsResponseThread.Should().BeFalse();
            response.Body.IsRequestThread.Should().BeFalse();
            responseThread.Should().NotBe(DeftThread.TaskQueue.Thread);
            responseThread.Should().NotBe((DeftConfig.DefaultMethodResponseTaskQueue as TaskQueue).Thread);
            responseThread.IsThreadPoolThread.Should().BeTrue();
        }

        [TestMethod]
        public async Task WhenTimeout_ShouldExecuteOnCorrectThread()
        {
            var port = 6002;
            var clientListener = new ClientListener(port);

            var server = await DeftConnector.ConnectAsync<Server>("localhost", port, "Server");

            lock (_lock)
            {
                DeftConfig.DefaultMethodResponseTaskQueue = responseTaskQueue;
                DeftConfig.DefaultRouteHandlerTaskQueue = requestTaskQueue;

                DeftResponse<ThreadResponse> response = null;
                Thread responseThread = null;

                server.SendMethod<ThreadArgs, ThreadResponse>(MainURL + "timeout", new ThreadArgs(), null, r =>
                {
                    responseThread = Thread.CurrentThread;
                    response = r;
                });

                TaskUtils.WaitFor(() => response != null).Wait();

                response.Ok.Should().BeFalse();
                response.StatusCode.Should().Be(ResponseStatusCode.Timeout);
                responseThread.Should().NotBe(DeftThread.TaskQueue.Thread);
                responseThread.Should().Be((DeftConfig.DefaultMethodResponseTaskQueue as TaskQueue).Thread);
            }
        }

    }
}
