using System;

namespace Deft
{
    public static class Logger
    {
        public static Level LogLevel { get; set; } = Level.INFO;

        public static Action<string, Level> LogFunction = (text, level) => Console.WriteLine(text);

        internal static void Log(string text, Level level)
        {
            if (level < LogLevel)
                return;

            var log = string.Format("[{0}] {1}", level.ToString(), text);
            if (LogFunction != null)
                LogFunction(log, level);
        }

        internal static void LogError(string text)
        {
            Log(text, Level.ERROR);
        }

        internal static void LogDebug(string text)
        {
            Log(text, Level.DEBUG);
        }

        internal static void LogInfo(string text)
        {
            Log(text, Level.INFO);
        }

        internal static void LogWarning(string text)
        {
            Log(text, Level.WARNING);
        }

        public enum Level
        {
            DEBUG,
            INFO,
            WARNING,
            ERROR
        }
    }
}
