using Deft;
using DeftUnitTests.ProjectClasses;
using DeftUnitTests.Utils;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SimpleInjector;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DeftUnitTests
{
    [TestClass]
    public class AF_HealthCheckTests
    {
        class Empty
        {

        }

        [TestMethod]
        public async Task WhenQuiet_ShouldAutoSendHealthCheck()
        {
            var port = 8000;
            var connectionIdentifier = "Server";

            var remoteEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), port);

            var clientListener = new ClientListener(port);

            var server = await DeftConnector.ConnectAsync<Server>(remoteEndPoint, connectionIdentifier);

            DeftConfig.Health.MaxQuietTime = TimeSpan.FromSeconds(1);
            await Task.Delay((int)DeftConfig.Health.MaxQuietTime.TotalMilliseconds + 510);

            server.Connection.GetHealthStatus().NumberOfHealthChecksSent.Should().Be(1);
        }

        [TestMethod]
        public async Task WhenForce_ShouldSendHealthCheck()
        {
            var port = 8001;
            var connectionIdentifier = "Server";

            var remoteEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), port);

            var clientListener = new ClientListener(port);

            var server = await DeftConnector.ConnectAsync<Server>(remoteEndPoint, connectionIdentifier);

            server.Connection.CheckHealthStatus();

            server.Connection.GetHealthStatus().NumberOfHealthChecksSent.Should().Be(1);
        }

        [TestMethod]
        public async Task WhenHealthCheckTimeout_ShouldCloseConnection()
        {
            var port = 8002;
            var connectionIdentifier = "Server";

            var remoteEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), port);

            var clientListener = new ClientListener(port);

            var server = await DeftConnector.ConnectAsync<Server>(remoteEndPoint, connectionIdentifier);

            server.Connection._Test_DontReplyToHealthCheck = true;

            DeftConfig.Health.MaxQuietTime = TimeSpan.FromSeconds(1);
            await Task.Delay((int)DeftConfig.Health.MaxQuietTime.TotalMilliseconds + 510 + DeftConfig.Health.HealthCheckTimeoutMs + 510);

            server.IsConnected.Should().BeFalse();
        }

        [TestMethod]
        public async Task WhenNotQuiet_ShouldNotSendHealthChecks()
        {
            var port = 8003;
            var connectionIdentifier = "Server";

            var remoteEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), port);

            var clientListener = new ClientListener(port);

            var server = await DeftConnector.ConnectAsync<Server>(remoteEndPoint, connectionIdentifier);

            DeftConfig.Health.MaxQuietTime = TimeSpan.FromSeconds(1);

            server.SendMethod<Empty, Empty>("DontCare", new Empty());
            await Task.Delay(500);

            server.SendMethod<Empty, Empty>("DontCare", new Empty());
            await Task.Delay(500);

            server.SendMethod<Empty, Empty>("DontCare", new Empty());
            await Task.Delay(500);

            server.SendMethod<Empty, Empty>("DontCare", new Empty());
            await Task.Delay(500);

            server.IsConnected.Should().BeTrue();
            server.Connection.GetHealthStatus().NumberOfHealthChecksSent.Should().Be(0);
        }
    }

}
