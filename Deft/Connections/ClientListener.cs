using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Deft
{
    public abstract class ClientListener
    {
        internal abstract void ReceivedIdToken(DeftConnection connection, string idToken);
    }

    public class ClientListener<T> : ClientListener where T : Client, new()
    {
        private List<T> clients = new List<T>();
        private ConcurrentQueue<TcpClient> clientQueue = new ConcurrentQueue<TcpClient>();

        public IEnumerable<T> ConnectedClients { get => clients.Where(o => o.IsConnected); }

        protected TcpListener socket; // Listener for tcp clients

        protected bool isOnline = false;

        public int Port { get; private set; }

        public ClientListener(int port)
        {
            this.Port = port;
            NetworkInitialize();
        }

        private void NetworkInitialize()
        {
            try
            {
                socket = new TcpListener(IPAddress.Any, Port);

                isOnline = true;

                socket.Start();

                Thread t = new Thread(ClientLoop);
                t.Start();

                BeginAccept();

                Logger.LogInfo("Initialized ClientListener on port " + Port);
                OnNetworkInitialized();
            }
            catch (Exception e)
            {
                isOnline = false;
                Logger.LogError("Could not start ClientListener, see exception: " + e.ToString());
                throw;
            }
        }

        private void BeginAccept()
        {
            try
            {
                socket.BeginAcceptTcpClient(AcceptTcpClient, null); // Start Recursion
            }
            catch (Exception e)
            {
                isOnline = false;
                Logger.LogError("Could not begin accepting TCP client, see exception: " + e.ToString());
            }
        }

        private void AcceptTcpClient(IAsyncResult result)
        {
            try
            {
                if (!isOnline)
                    return;
                TcpClient client = socket.EndAcceptTcpClient(result); // accept TCP client
                clientQueue.Enqueue(client);

                BeginAccept();
            }
            catch (Exception e)
            {
                isOnline = false;
                Logger.LogError("Could not accept TCP client, see exception: " + e.ToString());
            }
        }

        // Accepting clients must be very quick, thats why it is immidiatly added to concurrent queue that is thread safe
        // Then client loop extracts new players and adds them to array
        void ClientLoop()
        {
            while (isOnline)
            {
                if (clientQueue.Count > 0)
                {
                    TcpClient client;
                    if (clientQueue.TryDequeue(out client))
                        AddClient(client);
                }
                Thread.Sleep(1);
            }
        }

        void AddClient(TcpClient client)
        {
            var newConnection = new DeftConnection(this);
            newConnection.Connect(client);

            newConnection.StartHandshake();
        }

        internal override void ReceivedIdToken(DeftConnection connection, string idToken)
        {
            var client = clients.FirstOrDefault(o => o.idToken == idToken);
            if (client == null)
            {
                client = new T();
                clients.Add(client);
            }

            PacketBuilder.SendClientIdentified(connection, client.ClientId, client.idToken);

            connection.HandshakeSuccessful = true;
            client.Bind(connection);
        }

        public virtual void OnNetworkInitialized()
        {

        }

    }
}
