using System;
using System.Text.Json;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Xunit;

namespace Reusable.DataModels.UnitTests
{
    public class CosmosDbItemTest
    {
        [Fact]
        public void GetHashCode_WhenPropertiesChange_ThenHashAlsoChanges()
        {
            var item = new MyCosmosItem { Id = "BABEFEED", Key = "Schlüssel", Value = 42 };
            int hashCodeBefore = item.GetHashCode();
            item.Value = 696;
            int hashCodeAfter = item.GetHashCode();
            Assert.NotEqual(hashCodeBefore, hashCodeAfter);
        }

        [Fact]
        public void ShallowCopy_IfCopy_ThenPropertiesAreSame()
        {
            var item = new MyCosmosItem { Id = "BABEFEED", Key = "Schlüssel", Value = 42 };
            MyCosmosItem copy = item.ShallowCopy();
            
            Assert.Equal(item.Id, copy.Id);
            Assert.Equal(item.Key, copy.Key);
            Assert.Equal(item.Value, copy.Value);
        }

        [Fact]
        public void PartitionKeyValue_WhenSet_ThenMatchCorrespondingProperty()
        {
            const string partitionKey = "Schlüssel";
            var item = new MyCosmosItem { Id = "BABEFEED", Key = partitionKey, Value = 42 };
            Assert.Equal(partitionKey, item.PartitionKeyValue);
        }

        [Fact]
        public void ToString_WhenSerialized_ThenJsonIsCorrect()
        {
            var item = new MyCosmosItem { Id = "BABEFEED", Key = "Schlüssel", Value = 42 };
            string jsonText = item.ToString();
            JObject jsonObject = JObject.Parse(jsonText);
            Assert.Equal(item.Id, (string)jsonObject["id"]);
            Assert.Equal(item.Key, (string)jsonObject["key"]);
            Assert.Equal(item.Value, (int)jsonObject["value"]);
        }

    }// end of class CosmosDbItemTest

    /// <summary>
    /// Dieser Typ sollte als Element der Datenbank gut funktionieren.
    /// </summary>
    [CosmosContainer(Name = "box")]
    internal class MyCosmosItem : CosmosDbItem<MyCosmosItem>, IEquatable<MyCosmosItem>
    {
        [CosmosPartitionKey]
        [JsonProperty("key")]
        public string Key { get; set; }

        [JsonProperty("value")]
        public int Value { get; set; }

        public bool Equals(MyCosmosItem other)
        {
            return this.Id == other.Id
                && this.Key == other.Key 
                && this.Value == other.Value;
        }
    }

}// end of namespace Reusable.DataModels.UnitTests
