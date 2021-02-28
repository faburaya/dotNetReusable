using System.ComponentModel.DataAnnotations;

using Newtonsoft.Json;

namespace Reusable.DataAccess.IntegrationTests
{
    /// <summary>
    /// Dieser Type sollte als Element der Datenbank gut funktionieren.
    /// </summary>
    [DataModels.CosmosContainer(Name = "CosmosDbServiceTest.TestItem")]
    public class TestItem : DataModels.CosmosDbItem
    {
        [Required]
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        public override string PartitionKeyValue
            => DataModels.CosmosDbPartitionedItem<TestItem>.GetPartitionKeyValue(this);

        [Required]
        [DataModels.CosmosPartitionKey]
        [JsonProperty(PropertyName = "family")]
        public string Family { get; set; }
    }
}
