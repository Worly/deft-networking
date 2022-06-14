using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Deft
{
    internal static class HealthManager
    {
        private static Dictionary<DeftConnection, HealthStatus> statuses = new Dictionary<DeftConnection, HealthStatus>();
        private static bool isStarted = false;

        internal static void Register(DeftConnection connection)
        {
            lock (statuses)
            {
                statuses.Add(connection, new HealthStatus()
                {
                    Connection = connection
                });
            }

            Start();
        }

        internal static void UnRegister(DeftConnection connection)
        {
            lock (statuses)
            {
                statuses.Remove(connection);
            }
        }

        internal static HealthStatus GetStatus(DeftConnection connection)
        {
            return statuses[connection];
        }

        private static void Start()
        {
            if (isStarted)
                return;

            Update();

            isStarted = true;
        }

        private static void Update()
        {
            CheckHealth();

            // call CheckHealth every 500ms until the CancellationToken is triggered
            Task.Delay(500, Deft.CancellationTokenSource.Token).ContinueWith(t =>
            {
                DeftThread.ExecuteOnDeftThread(Update);
            }, TaskContinuationOptions.NotOnCanceled);
        }

        private static void CheckHealth()
        {
            Dictionary<DeftConnection, HealthStatus> statusesCopy;

            lock (statuses)
            {
                statusesCopy = new Dictionary<DeftConnection, HealthStatus>(statuses);
            }

            foreach (var status in statusesCopy.Values)
            {
                // check quiet time
                if (DateTime.UtcNow - status.Connection.LastPacketReceivedTime > DeftConfig.Health.MaxQuietTime
                    && status.HealthCheckSentTime == null)
                {
                    SendHealthCheck(status.Connection);
                }

                // check timeout time
                if (status.HealthCheckSentTime != null
                    && DateTime.UtcNow - status.HealthCheckSentTime > TimeSpan.FromMilliseconds(DeftConfig.Health.HealthCheckTimeoutMs))
                {
                    Logger.LogInfo($"Health check failed, disconnecting {status.Connection}!");
                    status.Connection.CloseConnection();
                }
            }
        }

        internal static void SendHealthCheck(DeftConnection connection)
        {
            if (statuses.TryGetValue(connection, out HealthStatus status))
            {
                if (status.HealthCheckSentTime != null)
                    return;

                status.HealthCheckSentTime = DateTime.UtcNow;
                status.NumberOfHealthChecksSent++;
                PacketBuilder.SendHealthCheck(connection);
            }
        }

        internal static void RecieveHealthCheckResponse(DeftConnection connection)
        {
            if (statuses.TryGetValue(connection, out HealthStatus status))
            {
                status.HealthCheckSentTime = null;
            }
        }

    }

    public class HealthStatus
    {
        public DeftConnection Connection { get; internal set; }
        public DateTime? HealthCheckSentTime { get; internal set; } = null;
        public int NumberOfHealthChecksSent { get; internal set; } = 0;
    }
}
