using System;

namespace Reusable.DataAccess.Common
{
    /// <summary>
    /// Implementiert eine Ausnahme für ORM Fehler.
    /// </summary>
    public class OrmException : Exception
    {
        /// <summary>
        /// Erstellt eine neue Instanz der Klasse <see cref="OrmException"/>.
        /// </summary>
        /// <param name="message">Die Nachricht.</param>
        /// <param name="innerEx">Die eingebettete Ausnahme.</param>
        public OrmException(string message, Exception innerEx = null)
            : base(message, innerEx) { }
    }
}
