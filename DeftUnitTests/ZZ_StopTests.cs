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

            Logger.LogLevel = Logger.Level.DEBUG;

            Server server = null;
            Exception exception = null;

            DeftConnector.Connect<Server>("localhost", port, "Server_WhenStopped_ShouldCancelConnection", s => server = s, e => exception = e);

            Deft.Deft.Stop();

            await Task.Delay(1000);

            server.Should().BeNull();
            exception.Should().BeNull();
        }
    }
}
