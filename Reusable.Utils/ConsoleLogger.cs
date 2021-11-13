using System;
using System.Threading;

namespace Reusable.Utils
{
    /// <summary>
    /// Bietet Protokollierung, die auf die Konsole schreibt.
    /// </summary>
    public class ConsoleLogger : ILogger
    {
        private static void PrintEvent(string levelLabel, string message)
        {
            Console.WriteLine(
                $"{DateTime.Now} (thread {Thread.CurrentThread.ManagedThreadId}) [{levelLabel}] {message}");
        }

        /// <inheritdoc/>
        public void Critical(string message) => PrintEvent("CRITICAL", message);

        /// <inheritdoc/>
        public void Error(string message) => PrintEvent("ERROR", message);

        /// <inheritdoc/>
        public void Warning(string message) => PrintEvent("WARNING", message);

        /// <inheritdoc/>
        public void Info(string message) => PrintEvent("INFO", message);

        /// <inheritdoc/>
        public void Debug(string message) => PrintEvent("DEBUG", message);

        /// <inheritdoc/>
        public void Trace(string message) => PrintEvent("TRACE", message);
    }
}
