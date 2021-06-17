using System;
using System.Collections.Generic;
using System.Threading;

namespace Deft
{
    public class TaskQueue : IDisposable, ITaskQueue
    {
        object locker = new object();
        public Thread Thread { get; private set; }
        Queue<Action> taskQ = new Queue<Action>();

        public TaskQueue()
        {
            Thread = new Thread(Consume);
            Thread.Start();
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
                    while (taskQ.Count == 0) Monitor.Wait(locker);
                    task = taskQ.Dequeue();
                }
                if (task == null) return;         // This signals our exit

                task.Invoke();
            }
        }
    }
}