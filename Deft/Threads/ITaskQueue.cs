using System;

namespace Deft
{
    /// <summary>
    /// Thread safe queue for invoking given tasks on another thread, <br/>
    /// Default implementation is <see cref="TaskQueue"/> which starts new thread on which actions are executed
    /// </summary>
    public interface ITaskQueue
    {
        public void EnqueueTask(Action task);
    }
}
