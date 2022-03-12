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
        /// <summary>
        /// Die Indentifikationsnummer des Elements.
        /// </summary>
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        /// <summary>
        /// Der Partitionsschlüssel.
        /// </summary>
        public string PartitionKeyValue
            => CosmosDbPartitionedItem<ItemType>.GetPartitionKeyValue(this);

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return CosmosDbPartitionedItem<ItemType>.CalculateHashOfJsonFor(this);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }

        /// <summary>
        /// Führt eine oberflächliche Kopie des Objekts.
        /// </summary>
        /// <returns>Eine Kopie des Objekts.</returns>
        public ItemType ShallowCopy()
        {
            return (ItemType)this.MemberwiseClone();
        }
    }
}
