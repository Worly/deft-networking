# Deft Networking
C# library for two way "HTTP like" communication over TCP with session persistence and thread saftey.

# Features

<h3>1. Robust TCP Connection</h3>

 * Initialize ClientListener on server application using `new ClientListener<TClient>(int port)`
 * Connect to server using `var server = DeftConnector.Connect<TServer>(ip, port, ...)`
 
<h3>2. Session persistence</h3>
 
 * Single session represends instance of `TClient`
 * `TClient` can implement `OnDisconnected()` and `OnConnected()` events
 
<h3>3. Two way HTTP-like communication</h3>

 * Register route handlers using `DeftMethods.DefaultRouter.Add<TArgs, TResponse>(string route, Func<...> handler)`, supports router nesting
 * Send request method to any connected Client/Server using `client.SendMethod<TArgs, TResponse>(route, args, headers, onResponseCallback)`
 * Every request body and headers, and every response has body, headers and status code
 
<h3>4. Thread safety</h3>

 * Choose on which thread request handler are executed using `ThreadOptions`, available options:
   - `ExecuteAsync` - every new request is executed on new worker thread
   - 'Default' - you can set your own thread using 'DeftConfig.DefaultRouteHandlerTaskQueue' or use default 'DeftThread'
 * You can choose thread for response callback the same way
 
# Simple example

```C#
using System;
using System.Linq;
using System.Threading.Tasks;
using Deft;

namespace Deft_Test
{
    class Client : Deft.Client { } // extended Client class, here you can override OnConnected or OnDisconnected
    class Server : Deft.Server { } // same for Server class

    // data transfer class for sending arguments to method over network
    class Args
    {
        public string Name { get; set; }
    }

    // data transfer class fro sending method response over network
    class Response
    {
        public string Message { get; set; }
    }

    class Program
    {
        static readonly int PORT = 3000;

        static async Task Main(string[] args)
        {
            // start server application if arguments contain --server
            if (args.Contains("--server"))
                Server();
            else
                await Client();
        }

        static void Server()
        {
            // register route handler for route /greeting
            DeftMethods.DefaultRouter.Add<Args, Response>("/greeting", (from, req) =>
            {
                // we just return response with Hello message
                return new Response()
                {
                    Message = "Hello " + req.Body.Name
                };
            });

            // start ClientListener on PORT
            var clientListener = new ClientListener<Client>(PORT);
        }

        static async Task Client()
        {
            // connect to server on localhost:PORT
            var server = await DeftConnector.ConnectAsync<Server>("localhost", PORT, "Server");


            var args = new Args()
            {
                Name = "Worly"
            };
            // send method on /greetings route with given arguments, and print it to console when response is received
            server.SendMethod<Args, Response>("/greeting", args, null, r =>
            {
                Console.WriteLine("Received: " + r.Body.Message);
            });
        }
    }
}

```
 
  