using Deft;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;

namespace DeftUnitTests
{
    [TestClass]
    public class ZZ_StopTests
    {
        [TestMethod]
        public async Task ZZWhenStopped_ShouldCancelConnection()
        {
            var port = 9999;

            var connectionTimeoutMs = 4000;

            Logger.LogLevel = Logger.Level.DEBUG;

            Server server = null;
            Exception exception = null;

            DeftConnector.Connect<Server>("localhost", port, "Server_WhenStopped_ShouldCancelConnection", s => server = s, e => exception = e, connectionTimeoutMs);

            await Task.Delay(100);

            Deft.Deft.Stop();

            await Task.Delay(connectionTimeoutMs + 200);

            server.Should().BeNull();
            exception.Should().BeNull();
        }
    }
}
