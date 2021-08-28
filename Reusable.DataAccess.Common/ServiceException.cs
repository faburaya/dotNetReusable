using System;

namespace Reusable.DataAccess.Common
{
    /// <summary>
    /// Implementiert eine Ausnahme für gescheiterte Vorgänge in einem Dienst.
    /// </summary>
    public class ServiceException : Exception
    {
        /// <summary>
        /// Erstellt eine neue Instanz der Klasse <see cref="ServiceException"/>.
        /// </summary>
        /// <param name="message">Die Nachricht.</param>
        /// <param name="innerEx">Die eingebettete Ausnahme.</param>
        public ServiceException(string message, Exception innerEx = null)
            : base(message, innerEx) { }
    }
}
