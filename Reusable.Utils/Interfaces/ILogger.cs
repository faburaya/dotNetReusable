namespace Reusable.Utils
{
    /// <summary>
    /// Emöglicht die Injezierung von unterschiedlichen Protokollanbietern, etwa
    /// NLog oder Serilog, die durch eine gemeinsame Schnittstelle benutzt werden können.
    /// </summary>
    public interface ILogger
    {
        /// <summary>
        /// Protokolliert einen kritischen Fehler.
        /// </summary>
        /// <param name="message">Die Nachricht.</param>
        public void Critical(string message);

        /// <summary>
        /// Protokolliert einen Fehler.
        /// </summary>
        /// <param name="message">Die Nachricht.</param>
        public void Error(string message);

        /// <summary>
        /// Protokolliert eine Warnung.
        /// </summary>
        /// <param name="message">Die Nachricht.</param>
        public void Warning(string message);

        /// <summary>
        /// Protokolliert zusätzliche Information.
        /// </summary>
        /// <param name="message">Die Nachricht.</param>
        public void Info(string message);

        /// <summary>
        /// Protokolliert eine Nachricht zum Debuggen.
        /// </summary>
        /// <param name="message">Die Nachricht.</param>
        public void Debug(string message);

        /// <summary>
        /// Protokolliert eine Nachricht, um den Lauf des Programms zu verfolgen.
        /// </summary>
        /// <param name="message">Die Nachricht.</param>
        public void Trace(string message);
    }
}
