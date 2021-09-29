using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Deft
{
    public static class DeftConnector
    {
        /// <summary>
        /// Connects to Deft's ClientListener on server application
        /// </summary>
        /// <param name="ip">IP address of server running Deft with initialized ClientListener</param>
        /// <param name="connectionIdentifier">Connection identifier (ex. 'MasterServer', 'LoggingServer'...)</param>
        /// <param name="connectionSettings">Connection settings used for this connection</param>
        /// <returns>Returns connected Server ready for communication</returns>
        public static async Task<T> ConnectAsync<T>(IPEndPoint ip, string connectionIdentifier, int connectionTimeoutMilliseconds = 3000) where T : Server, new()
        {
            if (ip == null)
                throw new ArgumentNullException("ip");
            if (connectionIdentifier == null)
                throw new ArgumentNullException("connectionIdentifier");

            Logger.LogDebug($"Trying to connect to {ip}");

            try
            {
                var tcpClient = new TcpClient();

                var delayTask = Task.Delay(connectionTimeoutMilliseconds, Deft.CancellationTokenSource.Token);

                var connectTask = tcpClient.ConnectAsync(ip.Address, ip.Port);

                if (await Task.WhenAny(delayTask, connectTask) == delayTask)
                {
                    Deft.CancellationTokenSource.Token.ThrowIfCancellationRequested();

                    throw new FailedToConnectException("Could not connect, TCP timeout", FailedToConnectException.FailReason.TCP_TIMEOUT, null);
                }

                if (!tcpClient.Connected)
                {
                    Deft.CancellationTokenSource.Token.ThrowIfCancellationRequested();

                    throw new FailedToConnectException("Could not connect, TCP timeout", FailedToConnectException.FailReason.TCP_TIMEOUT, null);
                }

                var connection = new DeftConnection(connectionIdentifier);
                connection.Connect(tcpClient);

                var handshakeCompletedSource = new TaskCompletionSource<T>();
                var delayTask2 = Task.Delay(connectionTimeoutMilliseconds, Deft.CancellationTokenSource.Token);

                T server = null;
                connection.OnClientIdentified(myClientId =>
                {
                    if (Deft.CancellationTokenSource.IsCancellationRequested)
                        return;

                    server = new T();
                    server.Bind(connection, myClientId);
                    handshakeCompletedSource.SetResult(server);
                });


                if (await Task.WhenAny(delayTask2, handshakeCompletedSource.Task) == delayTask2)
                {
                    connection.CloseConnection();

                    Deft.CancellationTokenSource.Token.ThrowIfCancellationRequested();

                    throw new FailedToConnectException("Could not connect, handshake timeout", FailedToConnectException.FailReason.HANDSHAKE_TIMEOUT, null);
                }

                return server;
            }
            catch (Exception e)
            {
                if (e is FailedToConnectException)
                {
                    Logger.LogError(e.ToString());
                    throw;
                }
                else if (e is OperationCanceledException)
                    throw;
                else
                {
                    Logger.LogError($"Could not connect, see exception: " + e.ToString());
                    throw new FailedToConnectException(null, FailedToConnectException.FailReason.OTHER_EXCEPTION, e);
                }
            }
        }

        /// <summary>
        /// Connects to Deft's ClientListener on server application
        /// </summary>
        /// <param name="ipOrHostName">IP address or HostName of server running Deft with initialized ClientListener</param>
        /// <param name="connectionIdentifier">Connection identifier (ex. 'MasterServer', 'LoggingServer'...)</param>
        /// <param name="connectionSettings">Connection settings used for this connection</param>
        /// <returns>Returns connected Server ready for communication</returns>
        public static async Task<T> ConnectAsync<T>(string ipOrHostName, int port, string connectionIdentifier, int connectionTimeoutMilliseconds = 3000) where T : Server, new()
        {
            IPEndPoint ipEndPoint;
            if (IPAddress.TryParse(ipOrHostName, out IPAddress ipAddress))
            {
                ipEndPoint = new IPEndPoint(ipAddress, port);
            }
            else
            {
                Logger.LogInfo($"Resolving IP address of {ipOrHostName}");

                try
                {
                    var addresses = Dns.GetHostAddresses(ipOrHostName).Where(o => o.AddressFamily == AddressFamily.InterNetwork).ToArray();
                    if (addresses.Length == 0)
                        throw new FailedToConnectException("Could not resolve hostname, unkown hostname", FailedToConnectException.FailReason.UNKNOWN_HOSTNAME, null);
                    else
                    {
                        if (addresses.Length > 1)
                            Logger.LogInfo($"Resloved multiple IP addresses ({addresses.Select(o => o.ToString()).Aggregate((f, s) => f + ", " + s)}), taking the first one");
                        Logger.LogInfo($"Resloved ip: {addresses[0]}");
                        ipEndPoint = new IPEndPoint(addresses[0], port);
                    }
                }
                catch (Exception e)
                {
                    var socketException = e as SocketException;

                    if (e is FailedToConnectException)
                    {
                        Logger.LogError(e.ToString());
                        throw;
                    }
                    if (e is SocketException && (socketException.SocketErrorCode == SocketError.HostNotFound || socketException.SocketErrorCode == SocketError.TryAgain))
                    {
                        Logger.LogError($"Could not reslove hostname, see exception: " + e.ToString());
                        throw new FailedToConnectException(null, FailedToConnectException.FailReason.UNKNOWN_HOSTNAME, e);
                    }
                    else
                    {
                        Logger.LogError($"Could not reslove hostname, see exception: " + e.ToString());
                        throw new FailedToConnectException(null, FailedToConnectException.FailReason.OTHER_EXCEPTION, e);
                    }
                }
            }

            return await ConnectAsync<T>(ipEndPoint, connectionIdentifier, connectionTimeoutMilliseconds);
        }

        /// <summary>
        /// Connects to Deft's ClientListener on server application
        /// </summary>
        /// <param name="ipOrHostName">IP address or HostName of server running Deft with initialized ClientListener</param>
        /// <param name="connectionIdentifier">Connection identifier (ex. 'MasterServer', 'LoggingServer'...)</param>
        /// <param name="connectionSettings">Connection settings used for this connection</param>
        /// <param name="onConnected">Callback called when connection was successfully made</param>
        /// <param name="onFailed">Callback called when connection failed</param>
        /// <returns>Returns connected Server ready for communication</returns>
        public static void Connect<T>(string ipOrHostName, int port, string connectionIdentifier, Action<T> onConnected, Action<Exception> onFailed, int connectionTimeoutMilliseconds = 3000) where T : Server, new()
        {
            var connectionTask = ConnectAsync<T>(ipOrHostName, port, connectionIdentifier, connectionTimeoutMilliseconds);

            connectionTask.ContinueWith(t => onConnected(t.Result), TaskContinuationOptions.OnlyOnRanToCompletion);
            connectionTask.ContinueWith(t => onFailed(t.Exception), TaskContinuationOptions.OnlyOnFaulted);
        }

        /// <summary>
        /// Connects to Deft's ClientListener on server application
        /// </summary>
        /// <param name="ip">IP address of server running Deft with initialized ClientListener</param>
        /// <param name="connectionIdentifier">Connection identifier (ex. 'MasterServer', 'LoggingServer'...)</param>
        /// <param name="connectionSettings">Connection settings used for this connection</param>
        /// <param name="onConnected">Callback called when connection was successfully made</param>
        /// <param name="onFailed">Callback called when connection failed</param>
        /// <returns>Returns connected Server ready for communication</returns>
        public static void Connect<T>(IPEndPoint ip, string connectionIdentifier, Action<T> onConnected, Action<Exception> onFailed, int connectionTimeoutMilliseconds = 3000) where T : Server, new()
        {
            var connectionTask = ConnectAsync<T>(ip, connectionIdentifier, connectionTimeoutMilliseconds);

            connectionTask.ContinueWith(t => onConnected(t.Result), TaskContinuationOptions.OnlyOnRanToCompletion);
            connectionTask.ContinueWith(t => onFailed(t.Exception), TaskContinuationOptions.OnlyOnFaulted);
        }
    }

    public class FailedToConnectException : Exception
    {
        public FailReason Reason { get; private set; }

        public FailedToConnectException(string message, FailReason reason, Exception inner)
            : base(message, inner)
        {
            this.Reason = reason;
        }

        public enum FailReason
        {
            TCP_TIMEOUT,
            HANDSHAKE_TIMEOUT,
            UNKNOWN_HOSTNAME,
            OTHER_EXCEPTION
        }
    }
}
