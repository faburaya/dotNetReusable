using System;

namespace Reusable.DataAccess.Common
{
    /// <summary>
    /// Implementiert eine Ausnahme für ORM Fehler.
    /// </summary>
    public class OrmException : Exception
    {
        public OrmException(string message, Exception innerEx = null)
            : base(message, innerEx) { }
    }
}
