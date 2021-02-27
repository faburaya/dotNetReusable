using Newtonsoft.Json;

namespace Reusable.DataModels
{
    /// <summary>
    /// Minimal Definition für ein Objekt, das in Azure Cosmos Datenbank gespeichert werden soll.
    /// Jede solches Objekt muss sich davon ableiten.
    /// </summary>
    public abstract class CosmosDbItem
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        public abstract string PartitionKeyValue { get; }

        public virtual bool IsEquivalentInStorageTo<Type>(Type other) where Type : CosmosDbItem
        {
            return CosmosDbPartitionedItem<Type>.CalculateHashOfJsonFor((Type)this)
                == CosmosDbPartitionedItem<Type>.CalculateHashOfJsonFor(other);
        }

        public override string ToString()
        {
            return JsonConvert.SerializeObject(this);
        }

        public virtual ToType ShallowCopy<ToType>()
        {
            return (ToType)this.MemberwiseClone();
        }
    }
}
