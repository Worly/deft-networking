using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Deft
{
    public static class Deft
    {
        private static bool stopped = false;

        internal static EventHandler StopEvent;
        internal static CancellationTokenSource CancellationTokenSource = new CancellationTokenSource();

        internal static IInjectionContainer Container { get; private set; } = new SimpleInjector();

        static Deft()
        {
            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
        }

        public static void InitDependencyInjection<T>(T container, Action<ContainerBuilder<T>> builder) where T : IInjectionContainer
        {
            Container = container;

            builder.Invoke(new ContainerBuilder<T>(container));
        }

        private static void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            Stop();
        }

        public static void Stop()
        {
            if (stopped)
            {
                Logger.LogWarning("Trying to stop Deft twice, no need to do that");
                return;
            }

            Logger.LogInfo("Called Deft.Stop(), stopping all tasks!");

            stopped = true;

            CancellationTokenSource.Cancel();

            if (StopEvent != null)
                StopEvent.Invoke(null, null);
        }
    }
}
