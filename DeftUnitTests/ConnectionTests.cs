using Deft;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using FluentAssertions;

namespace DeftUnitTests
{
    [TestClass]
    public class ConnectionTests
    {
        [TestMethod]
        public async Task WhenUsingIPEndPoint_ShouldConnect()
        {
            var port = 3000;
            var connectionIdentifier = "Server";

            var remoteEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), port);

            var clientListener = new ClientListener(port);

            var server = await DeftConnector.ConnectAsync<Server>(remoteEndPoint, connectionIdentifier);

            server.IsConnected.Should().BeTrue();
            server.Connection.Should().NotBeNull();
            server.Connection.ConnectionIdentifier.Should().Be(connectionIdentifier);
            server.Connection.RemoteEndPoint.Address.MapToIPv6().Should().Be(remoteEndPoint.Address.MapToIPv6());
            clientListener.ConnectedClients.Should().HaveCount(1);
        }

        [TestMethod]
        public async Task WhenUsingHostName_ShouldResolveHostNameAndConnect()
        {
            int port = 3001;

            var clientListener = new ClientListener(port);

            var server = await DeftConnector.ConnectAsync<Server>("localhost", port, "Server");

            server.IsConnected.Should().BeTrue();
        }

        [TestMethod]
        public async Task WhenUsingIPString_ShouldCorrectlyParseAndConnect()
        {
            int port = 3002;

            var clientListener = new ClientListener(port);

            var server = await DeftConnector.ConnectAsync<Server>("127.0.0.1", port, "Server");

            server.IsConnected.Should().BeTrue();
        }

        [TestMethod]
        public void WhenPortIsWrong_ShouldThrowTCPTimeoutException()
        {
            int port = 3003;
            int wrongPort = 3100;

            var clientListener = new ClientListener(port);

            Func<Task> action = () => DeftConnector.ConnectAsync<Server>("localhost", wrongPort, "Server", new ConnectionSettings()
            {
                ConnectionTimeoutMilliseconds = 100
            });

            action.Should()
                .Throw<FailedToConnectException>()
                .WithMessage("*TCP timeout*")
                .And.Reason.Should().Be(FailedToConnectException.FailReason.TCP_TIMEOUT);
        }

        [TestMethod]
        public void WhenHostNameIsWrong_ShouldThrowSocketException()
        {
            int port = 3004;

            var clientListener = new ClientListener(port);

            Func<Task> action = () => DeftConnector.ConnectAsync<Server>("thisHostDoesNotExist", port, "Server", new ConnectionSettings()
            {
                ConnectionTimeoutMilliseconds = 100
            });

            action.Should()
                .Throw<FailedToConnectException>()
                .Where(o => o.Reason == FailedToConnectException.FailReason.UNKNOWN_HOSTNAME)
                .WithInnerException<SocketException>()
                .And.SocketErrorCode.Should().Be(SocketError.HostNotFound);
        }

        [TestMethod]
        public void WhenConnectionIdentifierIsNull_ShouldThrowArgumentNullException()
        {
            Func<Task> action = () => DeftConnector.ConnectAsync<Server>(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 3000), null);

            action.Should()
                .Throw<ArgumentNullException>()
                .And.ParamName.Should().Be("connectionIdentifier");
        }

        [TestMethod]
        public void WhenIPIsNull_ShouldThrowArgumentNullException()
        {
            Func<Task> action = () => DeftConnector.ConnectAsync<Server>(null, "Server");

            action.Should()
                .Throw<ArgumentNullException>()
                .And.ParamName.Should().Be("ip");
        }

        [TestMethod]
        public void WhenPortIsTaken_ShouldThrowSocketException()
        {
            var port = 3005;

            var clientListener = new ClientListener(port);

            Action action = () => new ClientListener(port);

            action.Should()
                .Throw<SocketException>()
                .And.SocketErrorCode.Should().Be(SocketError.AddressAlreadyInUse);
        }

        [TestMethod]
        public async Task WhenMultipleClientsConnect_ShouldAssignClientIdCorrectly()
        {
            var port = 3006;

            var clientListener = new ClientListener(port);

            var server1 = await DeftConnector.ConnectAsync<Server>("localhost", port, "Server1");
            var server2 = await DeftConnector.ConnectAsync<Server>("localhost", port, "Server2");
            var server3 = await DeftConnector.ConnectAsync<Server>("localhost", port, "Server3");

            var ids = new int[] { server1.MyClientId, server2.MyClientId, server3.MyClientId };

            server1.IsConnected.Should().BeTrue();
            server2.IsConnected.Should().BeTrue();
            server3.IsConnected.Should().BeTrue();
            ids.Should().BeInAscendingOrder().And.OnlyHaveUniqueItems();
            clientListener.ConnectedClients.Should().HaveCount(3);
            clientListener.ConnectedClients.Select(o => o.ClientId).Should().BeEquivalentTo(ids);
        }

        [TestMethod]
        public async Task WhenDisconnectOnClient_ShouldDisconnectOnServer()
        {
            var port = 3007;

            var clientListener = new ClientListener(port);

            var server = await DeftConnector.ConnectAsync<Server>("localhost", port, "Server");

            server.IsConnected.Should().BeTrue();
            clientListener.ConnectedClients.ElementAt(0).IsConnected.Should().BeTrue();

            server.Disconnect();

            await Task.Delay(100);

            server.IsConnected.Should().BeFalse();
            clientListener.ConnectedClients.Should().BeEmpty();
        }

        [TestMethod]
        public async Task WhenDisconnectOnServer_ShouldDisconnectOnClient()
        {
            var port = 3008;

            var clientListener = new ClientListener(port);

            var server = await DeftConnector.ConnectAsync<Server>("localhost", port, "Server");

            server.IsConnected.Should().BeTrue();
            clientListener.ConnectedClients.ElementAt(0).IsConnected.Should().BeTrue();

            clientListener.ConnectedClients.ElementAt(0).Disconnect();

            await Task.Delay(100);

            server.IsConnected.Should().BeFalse();
            clientListener.ConnectedClients.Should().BeEmpty();
        }

        [TestMethod]
        public async Task WhenReconnect_ShouldKeepClientId()
        {
            var port = 3009;

            var clientListener = new ClientListener(port);

            var server = await DeftConnector.ConnectAsync<Server>("localhost", port, "Server_WhenReconnect_ShouldKeepClientId");

            server.IsConnected.Should().BeTrue();
            clientListener.ConnectedClients.Should().HaveCount(1);
            clientListener.ConnectedClients.ElementAt(0).IsConnected.Should().BeTrue();
            clientListener.ConnectedClients.ElementAt(0).ClientId.Should().Be(server.MyClientId);

            var clientId = server.MyClientId;
            var client = clientListener.ConnectedClients.ElementAt(0);

            server.Disconnect();
            
            var otherServer = await DeftConnector.ConnectAsync<Server>("localhost", port, "Server");
            otherServer.Disconnect();

            server = await DeftConnector.ConnectAsync<Server>("localhost", port, "Server_WhenReconnect_ShouldKeepClientId");

            server.IsConnected.Should().BeTrue();
            server.MyClientId.Should().Be(clientId);
            clientListener.ConnectedClients.Should().HaveCount(1);
            clientListener.ConnectedClients.ElementAt(0).IsConnected.Should().BeTrue();
            clientListener.ConnectedClients.ElementAt(0).ClientId.Should().Be(server.MyClientId);
            clientListener.ConnectedClients.ElementAt(0).Should().Be(client);
        }
    }
}
