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
        public void ZZWhenStopped_ShouldCancelConnection()
        {
            var port = 9999;

            Logger.LogLevel = Logger.Level.DEBUG;

            var task = DeftConnector.ConnectAsync<Server>("localhost", port, "Server_WhenStopped_ShouldCancelConnection");

            Deft.Deft.Stop();

            task.IsCompleted.Should().BeTrue();
        }
    }
}
