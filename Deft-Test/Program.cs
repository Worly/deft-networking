using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Deft;

namespace Deft_Test
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Deft.Logger.LogLevel = Deft.Logger.Level.WARNING;

            Console.WriteLine("CurrentThread: " + Thread.CurrentThread.ManagedThreadId);

            if (args.Length > 0 && args[0] == "--server")
            //if (true)
            {
                var clientListener = new ClientListener();

                DeftMethods.DefaultRouter
                    .Add<TestArgs, TestResponse>("hello", (from, req) =>
                    {
                        Console.WriteLine("IN ROUTE HANDLER CurrentThread: " + Thread.CurrentThread.ManagedThreadId);
                        return new TestResponse()
                        {
                            Message = "Hello " + req.Body.Name
                        };
                    })
                    .Add<TestArgs, TestResponse>("slow", (from, req) =>
                    {
                        Console.WriteLine("IN ROUTE HANDLER SLOW CurrentThread: " + Thread.CurrentThread.ManagedThreadId);
                        Thread.Sleep(1000);
                        return new TestResponse()
                        {
                            Message = "Hello " + req.Body.Name
                        };
                    }, ThreadOptions.ExecuteAsync);

                new Thread(() =>
                {
                    while (true)
                        Thread.Sleep(1000);
                }).Start();
            }
            else
            {
                var s = await DeftConnector.ConnectAsync<Server>("127.0.0.1", 3000, "Server");

                Console.WriteLine("SUCESS");

                s.SendMethod<TestArgs, TestResponse>("hello", new TestArgs() { Name = "Tino" }, null, r =>
                {
                    Console.WriteLine("IN RESPONSE1 CurrentThread: " + Thread.CurrentThread.ManagedThreadId);

                    s.SendMethod<TestArgs, TestResponse>("hello", new TestArgs() { Name = "Tino" }, null, r =>
                    {
                        Console.WriteLine("IN RESPONSE2 CurrentThread: " + Thread.CurrentThread.ManagedThreadId);
                    });
                });

                s.SendMethod<TestArgs, TestResponse>("hello", new TestArgs() { Name = "Tino" }, null, r =>
                {
                    Console.WriteLine("IN RESPONSE3 CurrentThread: " + Thread.CurrentThread.ManagedThreadId);
                });

                var r = await s.SendMethodAsync<TestArgs, TestResponse>("hello", new TestArgs() { Name = "Tino" }, null);

                Console.WriteLine("AFTER RESPONSE4 CurrentThread: " + Thread.CurrentThread.ManagedThreadId);

                s.SendMethod<TestArgs, TestResponse>("slow", new TestArgs() { Name = "Tino" }, null, r =>
                {
                    Console.WriteLine("IN RESPONSE CurrentThread: " + Thread.CurrentThread.ManagedThreadId + " with status code " + r.StatusCode);
                });
                s.SendMethod<TestArgs, TestResponse>("slow", new TestArgs() { Name = "Tino" }, null, r =>
                {
                    Console.WriteLine("IN RESPONSE CurrentThread: " + Thread.CurrentThread.ManagedThreadId + " with status code " + r.StatusCode);
                });
                s.SendMethod<TestArgs, TestResponse>("slow", new TestArgs() { Name = "Tino" }, null, r =>
                {
                    Console.WriteLine("IN RESPONSE CurrentThread: " + Thread.CurrentThread.ManagedThreadId + " with status code " + r.StatusCode);
                });
                s.SendMethod<TestArgs, TestResponse>("slow", new TestArgs() { Name = "Tino" }, null, r =>
                {
                    Console.WriteLine("IN RESPONSE CurrentThread: " + Thread.CurrentThread.ManagedThreadId + " with status code " + r.StatusCode);
                }, ThreadOptions.ExecuteAsync);


                //s.Disconnect();


                new Thread(() =>
                {
                    while (true)
                        Thread.Sleep(1000);
                }).Start();
            }

        }
    }
}
