using System;
using System.Threading;

namespace Deft
{
    public static class DeftThread
    {
        public static TaskQueue TaskQueue { get; private set; } = new TaskQueue(new DeftSynchronizationContext());

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

            if (IsOnDeftThread())
            {
                action();
                return;
            }

            TaskQueue.EnqueueTask(action);
        }

        public static bool IsOnDeftThread()
        {
            return Thread.CurrentThread == TaskQueue.Thread;
        }

        public static void Join()
        {
            if (Thread.CurrentThread == TaskQueue.Thread)
                Logger.LogWarning("Do not Join() from DeftThread because it will lock DeftThread and break everything");
            TaskQueue.Thread.Join();
        }
    }


    public enum ThreadOptions
    {
        Default = 0,
        ExecuteAsync = 1,
    }
}
