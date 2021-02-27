using System.Collections.Generic;

namespace Reusable.DataModels
{
    /// <summary>
    /// Implementiert den Vergleich des Inhalts zwei Objekte,
    /// die in Cosmos Datenbank gespeichert wurden.
    /// </summary>
    /// <remarks>
    /// Unterschiede in Daten der Objekte, die nicht in der Datenbank serialisiert werden,
    /// werden von dieser Implementierung nicht berücksichtigt.
    /// </remarks>
    /// <typeparam name="ItemType">Der zu vergleichende Type.</typeparam>
    public class CosmosStoredItemComparer<ItemType>
        : IEqualityComparer<ItemType>
        where ItemType : CosmosDbItem
    {
        public bool Equals(ItemType a, ItemType b)
        {
            return a.IsEquivalentInStorageTo(b);
        }

        public int GetHashCode(ItemType item)
        {
            return CosmosDbPartitionedItem<ItemType>.CalculateHashOfJsonFor(item);
        }
    }
}
