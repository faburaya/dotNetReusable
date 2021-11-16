using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Reusable.Utils
{
    /// <summary>
    /// Bietet einfache Protokollierung, die auf eine Datei geschrieben wird.
    /// </summary>
    public class SimpleFileLogger : SimpleLoggerBase, IDisposable
    {
        private readonly FileStream _fileStream;

        private readonly StreamWriter _streamWriter;

        private readonly ConcurrentQueue<string> _eventsQueue;

        private readonly Task _recorderTask;

        /// <summary>
        /// Erstellt eine neue Instanz von <see cref="SimpleFileLogger"/>.
        /// </summary>
        /// <param name="logFilePath">Das Pfad der Datei, auf die protokolliert wird.</param>
        public SimpleFileLogger(string logFilePath)
        {
            _disposed = false;
            _fileStream = new FileStream(
                logFilePath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read);
            _streamWriter = new StreamWriter(_fileStream);
            _eventsQueue = new ConcurrentQueue<string>();
            _recorderTask = Task.Run(RecordEventsAsynchronously);
        }

        /// <inheritdoc/>
        protected override void WriteLine(string text)
        {
            _eventsQueue.Enqueue(text);
        }

        private void RecordEventsAsynchronously()
        {
            while (true)
            {
                while (_eventsQueue.TryDequeue(out string text))
                {
                    if (text == null)
                        return; // muss das Thread beenden

                    _streamWriter.WriteLine(text);
                }

                Thread.Sleep(50);
            }
        }

        private bool _disposed;

        /// <inheritdoc/>
        public void Dispose()
        {
            if (_disposed)
                return;

            // fordert das Ende des Threads und wartet darauf
            _eventsQueue.Enqueue(null);
            _recorderTask.Wait();
            _recorderTask.Dispose();

            _streamWriter.Dispose();
            _fileStream.Dispose();

            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }
}
