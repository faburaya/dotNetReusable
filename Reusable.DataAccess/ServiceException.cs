using System;

namespace Reusable.DataAccess
{
    /// <summary>
    /// Implementiert eine Ausnahme für gescheiterte Vorgänge in einem Dienst.
    /// </summary>
    public class ServiceException : ApplicationException
    {
        public ServiceException(string message, Exception innerEx = null)
            : base(message, innerEx) { }
    }
}
