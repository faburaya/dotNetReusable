using System;

namespace Reusable.DataAccess.Common
{
    /// <summary>
    /// Stellt eine Ausnahme dar, die beim Zergliedern auftritt.
    /// </summary>
    public class ParserException : Exception
    {
        /// <summary>
        /// Erstellt eine neue Instanz von <see cref="ParserException"/>.
        /// </summary>
        /// <param name="message">Eine Nachricht.</param>
        /// <param name="innerException">Die einzuwickelnde Ausnahme.</param>
        public ParserException(string message, Exception innerException = null)
            : base(message, innerException)
        {
        }
    }
}
