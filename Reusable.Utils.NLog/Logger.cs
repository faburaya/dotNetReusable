namespace Reusable.Utils.NLog
{
    /// <summary>
    /// Bietet von NLog unterstützte Protokollierung.
    /// </summary>
    public class Logger : ILogger
    {
        private readonly global::NLog.Logger _log;

        /// <summary>
        /// Erstellt eine Instanz von <see cref="Logger"/>.
        /// </summary>
        /// <param name="log">Das Objekt, das den Zugang auf die von NLog unterstützte Protokollierung gewährt.</param>
        public Logger(global::NLog.Logger log)
        {
            _log = log;
        }

        /// <inheritdoc/>
        public void Critical(string message) => _log.Fatal(message);

        /// <inheritdoc/>
        public void Error(string message) => _log.Error(message);

        /// <inheritdoc/>
        public void Warning(string message) => _log.Warn(message);

        /// <inheritdoc/>
        public void Info(string message) => _log.Info(message);

        /// <inheritdoc/>
        public void Debug(string message) => _log.Debug(message);

        /// <inheritdoc/>
        public void Trace(string message) => _log.Trace(message);
    }
}
