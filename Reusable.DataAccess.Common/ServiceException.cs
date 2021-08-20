using System;

namespace Reusable.DataAccess.Common
{
    /// <summary>
    /// Implementiert eine Ausnahme für gescheiterte Vorgänge in einem Dienst.
    /// </summary>
    public class ServiceException : Exception
    {
        public ServiceException(string message, Exception innerEx = null)
            : base(message, innerEx) { }
    }
}
