using System;

using System.ComponentModel.DataAnnotations;

using Newtonsoft.Json;

namespace Reusable.DataAccess.Cosmos.IntegrationTests
{
    /// <summary>
    /// Dieser Type sollte als Element der Datenbank gut funktionieren.
    /// </summary>
    [DataModels.CosmosContainer(Name = "CosmosDbServiceTest.TestItem")]
    public class TestItem : DataModels.CosmosDbItem<TestItem>, IEquatable<TestItem>
    {
        [Required]
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [Required]
        [DataModels.CosmosPartitionKey]
        [JsonProperty(PropertyName = "family")]
        public string Family { get; set; }

        public bool Equals(TestItem other)
        {
            return this.Id == other.Id
                && this.Name == other.Name
                && this.Family == other.Family;
        }
    }
}
