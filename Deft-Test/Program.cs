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
            Deft.Logger.LogLevel = Deft.Logger.Level.INFO;


            if (args.Length > 0 && args[0] == "--server")
            //if (true)
            {
                var clientListener = new ClientListener();

                DeftMethods.DefaultRouter
                    .Add<TestArgs, TestResponse>("hello", (from, req) =>
                    {
                        return new TestResponse()
                        {
                            Message = "Hello " + req.Body.Name
                        };
                    })
                    .Add<TestArgs, TestResponse>("slow", (from, req) =>
                    {
                        //Thread.Sleep(1000);
                        return new TestResponse()
                        {
                            Message = "Hello " + req.Body.Name
                        };
                    });

                DeftThread.Join();
            }
            else
            {
                var s = await DeftConnector.ConnectAsync<Server>("127.0.0.1", 3000, "Server");

                Console.WriteLine("SUCESS");

                //s.SendMethod<TestArgs, TestResponse>("hello", new TestArgs() { Name = "Tino" }, null, r =>
                //{

                //    s.SendMethod<TestArgs, TestResponse>("hello", new TestArgs() { Name = "Tino" }, null, r =>
                //    {
                //    });
                //});

                //s.SendMethod<TestArgs, TestResponse>("hello", new TestArgs() { Name = "Tino" }, null, r =>
                //{
                //});

                var r = await s.SendMethodAsync<TestArgs, TestResponse>("hello", new TestArgs() { Name = "Tino" }, null);

                s.SendMethod<TestArgs, TestResponse>("slow", new TestArgs() { Name = "Tino" }, null, r =>
                {
                    
                });



                //s.Disconnect();

                DeftThread.Join();
            }

        }
    }
}
