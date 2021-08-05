using Deft.Utils.Settings;
using System;

namespace Deft
{
    public static class DeftConfig
    {
        public static int HandshakeTimeoutMs { get; set; } = 3000;
        public static int MethodTimeoutMs { get; set; } = 3000;

        /// <summary>
        /// Set this to true if you want to receive exception stacktrace of exceptions thrown in RouteHandlers, <br/>
        /// Stacktrace will be sent in headers under 'exception-stacktrace', <br/>
        /// default value is false
        /// </summary>
        public static bool RespondWithExceptionStackTrace { get; set; } = false;

        /// <summary>
        /// Default <see cref="ITaskQueue"/> for executing RouteHandler methods for incoming requests, <br/>
        /// can be overriden when attaching a route, <br/>
        /// default value is DeftThread.TaskQueue
        /// </summary>
        public static ITaskQueue DefaultRouteHandlerTaskQueue { get; set; } = DeftThread.TaskQueue;

        /// <summary>
        /// Default <see cref="ITaskQueue"/> for executing response callback methods for returned responses, <br/>
        /// can be overriden when calling SendMethod, <br/>
        /// default value is DeftThread.TaskQueue
        /// </summary>
        public static ITaskQueue DefaultMethodResponseTaskQueue { get; set; } = DeftThread.TaskQueue;

        public static string ApplicationName { get; set; } = null;

        public static ISettings Settings { get; set; } = new DefaultFolderSettings();
    }
}
