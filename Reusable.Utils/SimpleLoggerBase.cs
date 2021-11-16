using System;
using System.Threading;

namespace Reusable.Utils
{
    /// <summary>
    /// Eine sehr einfache Implementierung für Protokollierung.
    /// Sie kann für Tests nützlich sein.
    /// </summary>
    public abstract class SimpleLoggerBase : ILogger
    {
        private static string CreateEventMessage(string levelLabel, string message)
        {
            return $"{DateTime.Now} (thread {Thread.CurrentThread.ManagedThreadId}) [{levelLabel}] {message}";
        }

        /// <summary>
        /// Die Überschreibung dieser Methode bestimmt, wie das zu protokollierende Ereignis geschrieben wird.
        /// </summary>
        /// <param name="text">Der zu schreibende Text.</param>
        protected abstract void WriteLine(string text);

        /// <inheritdoc/>
        public void Critical(string message) => WriteLine(CreateEventMessage("CRITICAL", message));

        /// <inheritdoc/>
        public void Error(string message) => WriteLine(CreateEventMessage("ERROR", message));

        /// <inheritdoc/>
        public void Warning(string message) => WriteLine(CreateEventMessage("WARNING", message));

        /// <inheritdoc/>
        public void Info(string message) => WriteLine(CreateEventMessage("INFO", message));

        /// <inheritdoc/>
        public void Debug(string message) => WriteLine(CreateEventMessage("DEBUG", message));

        /// <inheritdoc/>
        public void Trace(string message) => WriteLine(CreateEventMessage("TRACE", message));
    }
}
