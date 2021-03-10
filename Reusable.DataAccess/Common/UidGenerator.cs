using System;

namespace Reusable.DataAccess.Common
{
    /// <summary>
    /// Grundlegende Implementierung für die Erstellung von einzigartigen Identificakationsnummern.
    /// </summary>
    public class UidGenerator<DataType>
    {
        public virtual string GenerateIdFor(DataType obj)
        {
            return Guid.NewGuid().ToString();
        }
    }
}
