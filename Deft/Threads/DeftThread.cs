using System;
using System.Threading;

namespace Deft
{
    public static class DeftThread
    {
        public static TaskQueue TaskQueue { get; private set; } = new TaskQueue();

        internal static void ExecuteOnSelectedTaskQueue(Action action, ITaskQueue taskQueue)
        {
            if (action == null)
                throw new ArgumentNullException("action");
            if (taskQueue == null)
                taskQueue = TaskQueue;

            if (taskQueue == TaskQueue)
                ExecuteOnDeftThread(action);
            else
                taskQueue.EnqueueTask(action);
        }

        internal static void ExecuteOnDeftThread(Action action)
        {
            if (action == null)
                throw new ArgumentNullException("action");

            if (Thread.CurrentThread == TaskQueue.Thread)
            {
                action();
                return;
            }

            TaskQueue.EnqueueTask(action);
        }

        public static void Join()
        {
            if (Thread.CurrentThread == TaskQueue.Thread)
                Logger.LogWarning("Do not Join() from DeftThread because it will lock DeftThread and break everything");
            TaskQueue.Thread.Join();
        }
    }

}
