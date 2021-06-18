﻿using System;
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
