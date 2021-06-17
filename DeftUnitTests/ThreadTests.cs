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
    public class ThreadTests
    {
        static object _lock = new object();
        static TaskQueue responseTaskQueue = new TaskQueue();
        static TaskQueue requestTaskQueue = new TaskQueue();

        static ThreadTests()
        {
            var router = new Router();
            router.Add<ThreadArgs, ThreadResponse>("/default", (from, req) =>
            {
                return new ThreadResponse()
                {
                    IsDeftThread = DeftThread.TaskQueue.Thread == Thread.CurrentThread,
                    IsPoolThread = Thread.CurrentThread.IsThreadPoolThread,
                    IsResponseThread = Thread.CurrentThread == (DeftConfig.DefaultMethodResponseTaskQueue as TaskQueue).Thread,
                    IsRequestThread = Thread.CurrentThread == (DeftConfig.DefaultRouteHandlerTaskQueue as TaskQueue).Thread
                };
            });

            router.Add<ThreadArgs, ThreadResponse>("/async", (from, req) =>
            {
                return DeftResponse<ThreadResponse>.From(new ThreadResponse()
                {
                    IsDeftThread = DeftThread.TaskQueue.Thread == Thread.CurrentThread,
                    IsPoolThread = Thread.CurrentThread.IsThreadPoolThread,
                    IsResponseThread = Thread.CurrentThread == (DeftConfig.DefaultMethodResponseTaskQueue as TaskQueue).Thread,
                    IsRequestThread = Thread.CurrentThread == (DeftConfig.DefaultRouteHandlerTaskQueue as TaskQueue).Thread
                });
            }, ThreadOptions.ExecuteAsync);

            router.Add<ThreadArgs, ThreadResponse>("/timeout", (from, req) =>
            {
                Thread.Sleep(DeftConfig.MethodTimeoutMs + 100);
                return new ThreadResponse();
            }, ThreadOptions.ExecuteAsync);

            DeftMethods.DefaultRouter.Add("thread-tests", router);
        }

        [TestMethod]
        public async Task WhenDefaultOptions_ShouldExecuteOnSelectedThread()
        {
            var port = 5000;
            var clientListener = new ClientListener(port);

            var server = await DeftConnector.ConnectAsync<Server>("localhost", port, "Server");

            lock (_lock)
            {
                DeftConfig.DefaultMethodResponseTaskQueue = DeftThread.TaskQueue;
                DeftConfig.DefaultRouteHandlerTaskQueue = DeftThread.TaskQueue;

                DeftResponse<ThreadResponse> response = null;
                Thread responseThread = null;

                server.SendMethod<ThreadArgs, ThreadResponse>("/thread-tests/default", new ThreadArgs(), null, r =>
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

                server.SendMethod<ThreadArgs, ThreadResponse>("/thread-tests/default", new ThreadArgs(), null, r =>
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
            var port = 5001;
            var clientListener = new ClientListener(port);

            var server = await DeftConnector.ConnectAsync<Server>("localhost", port, "Server");

            DeftResponse<ThreadResponse> response = null;
            Thread responseThread = null;

            server.SendMethod<ThreadArgs, ThreadResponse>("/thread-tests/async", new ThreadArgs(), null, r =>
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
            var port = 5000;
            var clientListener = new ClientListener(port);

            var server = await DeftConnector.ConnectAsync<Server>("localhost", port, "Server");

            lock (_lock)
            {
                DeftConfig.DefaultMethodResponseTaskQueue = responseTaskQueue;
                DeftConfig.DefaultRouteHandlerTaskQueue = requestTaskQueue;

                DeftResponse<ThreadResponse> response = null;
                Thread responseThread = null;

                server.SendMethod<ThreadArgs, ThreadResponse>("/thread-tests/timeout", new ThreadArgs(), null, r =>
                {
                    responseThread = Thread.CurrentThread;
                    response = r;
                });

                TaskUtils.WaitFor(() => response != null).Wait();

                response.Ok.Should().BeFalse();
                responseThread.Should().NotBe(DeftThread.TaskQueue.Thread);
                responseThread.Should().Be((DeftConfig.DefaultMethodResponseTaskQueue as TaskQueue).Thread);
            }
        }

    }
}
