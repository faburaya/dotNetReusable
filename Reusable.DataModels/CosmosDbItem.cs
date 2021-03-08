using System;

using Newtonsoft.Json;

namespace Reusable.DataModels
{
    /// <summary>
    /// Minimal Definition für ein Objekt, das in Azure Cosmos Datenbank gespeichert werden soll.
    /// Jede solches Objekt muss sich davon ableiten.
    /// </summary>
    public abstract class CosmosDbItem<ItemType> where ItemType : class, IEquatable<ItemType>
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        public string PartitionKeyValue
            => CosmosDbPartitionedItem<ItemType>.GetPartitionKeyValue(this);

        public override int GetHashCode()
        {
            return CosmosDbPartitionedItem<ItemType>.CalculateHashOfJsonFor(this);
        }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }

        public ItemType ShallowCopy()
        {
            return (ItemType)this.MemberwiseClone();
        }
    }
}
