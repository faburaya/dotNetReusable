using System;

namespace Reusable.Utils
{
    /// <summary>
    /// Grundlegende Implementierung für die Erstellung von einzigartigen Identificakationen.
    /// </summary>
    public class UidGenerator<DataType>
    {
        /// <summary>
        /// Erstellt eine einzigartige Identifikation für ein gegebenes Objekt.
        /// </summary>
        /// <param name="obj">Das gegebene Objekt.</param>
        /// <returns>Eine Identifikation.</returns>
        /// <remarks>Die Identifikation ist nie dieselbe, auch wenn das Objekt dasselbe ist.</remarks>
        public virtual string GenerateIdFor(DataType obj)
        {
            return Guid.NewGuid().ToString();
        }
    }
}
