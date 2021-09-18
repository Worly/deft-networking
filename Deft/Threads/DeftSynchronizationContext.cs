using System;
using System.Threading;
using System.Threading.Tasks;

namespace Deft
{
    internal class DeftSynchronizationContext : SynchronizationContext
    {
        public override void Post(SendOrPostCallback d, object state)
        {
            DeftThread.ExecuteOnDeftThread(() => d(state));
        }

        public override void Send(SendOrPostCallback d, object state)
        {
            var taskCompletionSource = new TaskCompletionSource<bool>();

            DeftThread.ExecuteOnDeftThread(() =>
            {
                try
                {
                    d(state);
                    taskCompletionSource.SetResult(true);
                }
                catch (Exception e)
                {
                    taskCompletionSource.SetException(e);
                }
            });

            taskCompletionSource.Task.Wait();
            if (taskCompletionSource.Task.IsFaulted)
                throw taskCompletionSource.Task.Exception;
        }
    }
}
