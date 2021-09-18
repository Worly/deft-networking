using System;
using System.Collections.Generic;
using System.Threading;

namespace Deft
{
    public class TaskQueue : IDisposable, ITaskQueue
    {
        readonly object locker = new object();
        public Thread Thread { get; private set; }

        readonly Queue<Action> taskQ = new Queue<Action>();

        private SynchronizationContext synchronizationContext;

        public TaskQueue(SynchronizationContext synchronizationContext = null)
        {
            this.synchronizationContext = synchronizationContext;

            Deft.StopEvent += Deft_Stop;

            Thread = new Thread(Consume);
            Thread.Start();
        }

        private void Deft_Stop(object sender, EventArgs args)
        {
            Dispose();
        }

        public void Dispose()
        {
            Deft.StopEvent -= Deft_Stop;
            EnqueueTask(null);
        }

        public void EnqueueTask(Action task)
        {
            lock (locker)
            {
                taskQ.Enqueue(task);
                Monitor.PulseAll(locker);
            }
        }

        void Consume()
        {
            if (this.synchronizationContext != null)
                SynchronizationContext.SetSynchronizationContext(this.synchronizationContext);

            while (true)
            {
                Action task;
                lock (locker)
                {
                    while (taskQ.Count == 0)
                        Monitor.Wait(locker);
                    task = taskQ.Dequeue();
                }
                if (task == null) // This signals our exit
                {
                    Logger.LogDebug("TaskQueue found null task, stopping consumer thread");
                    return;         
                }

                task.Invoke();
            }
        }
    }
}