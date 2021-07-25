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

        public TaskQueue()
        {
            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;

            Thread = new Thread(Consume);
            Thread.Start();
        }

        private void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            AppDomain.CurrentDomain.ProcessExit -= CurrentDomain_ProcessExit;
            Dispose();
        }

        public void Dispose()
        {
            EnqueueTask(null);
            Thread.Join();
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