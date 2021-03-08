using System;

using Newtonsoft.Json;
using Xunit;

namespace Reusable.DataModels.UnitTests
{
    public class CosmosDbPartitionedItemTest
    {
        private void LoadItemType_Throws<ItemType>() where ItemType : class
        {
            Exception thrownException = Assert.ThrowsAny<Exception>(
                () => CosmosDbPartitionedItem<ItemType>.PartitionKeyPath);

            while (thrownException.InnerException != null)
                thrownException = thrownException.InnerException;

            Assert.Equal(typeof(NotSupportedException), thrownException.GetType());
        }

        [Fact]
        public void LoadItemType_WhenNoKey_ThenThrow()
        {
            LoadItemType_Throws<TestClassItemNoKey>();
        }

        [Fact]
        public void LoadItemType_WhenKeyLacksJson_ThenThrow()
        {
            LoadItemType_Throws<TestClassItemKeyLacksJson>();
        }

        [Fact]
        public void LoadItemType_WhenTwoKeys_ThenThrow()
        {
            LoadItemType_Throws<TestClassItemWithTwoKeys>();
        }

        [Fact]
        public void LoadItemType_WhenNoContainer_ThenThrow()
        {
            LoadItemType_Throws<TestClassItemNoContainer>();
        }

        [Fact]
        public void GetPartitionKeyPath()
        {
            Assert.Equal("/key", CosmosDbPartitionedItem<TestClassItem>.PartitionKeyPath);
        }

        [Fact]
        public void GetPartitionKeyValue()
        {
            var obj = new TestClassItem { Key = "Schlüssel", Value = 32 };;
            Assert.Equal(obj.Key, CosmosDbPartitionedItem<TestClassItem>.GetPartitionKeyValue(obj));
            obj.Key = "Etwas Anderes";
            Assert.Equal(obj.Key, CosmosDbPartitionedItem<TestClassItem>.GetPartitionKeyValue(obj));
        }

        [Fact]
        public void CalculateHashOfJsonFor_WhenItemsEqual_ThenSameHash()
        {
            var obj1 = new TestClassItem { Key = "Schlüssel", Value = 32 };
            var obj2 = new TestClassItem { Key = "Schlüssel", Value = 32 };

            Assert.Equal(
                CosmosDbPartitionedItem<TestClassItem>.CalculateHashOfJsonFor(obj1),
                CosmosDbPartitionedItem<TestClassItem>.CalculateHashOfJsonFor(obj1)
            );

            Assert.Equal(
                CosmosDbPartitionedItem<TestClassItem>.CalculateHashOfJsonFor(obj2),
                CosmosDbPartitionedItem<TestClassItem>.CalculateHashOfJsonFor(obj2)
            );

            Assert.Equal(
                CosmosDbPartitionedItem<TestClassItem>.CalculateHashOfJsonFor(obj1),
                CosmosDbPartitionedItem<TestClassItem>.CalculateHashOfJsonFor(obj2)
            );
        }

        [Fact]
        public void CalculateHashOfJsonFor_WhenItemsDifferent_ThenDifferentHash()
        {
            var obj1 = new TestClassItem { Key = "Schlüssel", Value = 32 };
            var obj2 = new TestClassItem { Key = "Schlüssel", Value = 1 };
            var obj3 = new TestClassItem { Key = "Tür", Value = 32 };

            Assert.NotEqual(
                CosmosDbPartitionedItem<TestClassItem>.CalculateHashOfJsonFor(obj1),
                CosmosDbPartitionedItem<TestClassItem>.CalculateHashOfJsonFor(obj2)
            );

            Assert.NotEqual(
                CosmosDbPartitionedItem<TestClassItem>.CalculateHashOfJsonFor(obj1),
                CosmosDbPartitionedItem<TestClassItem>.CalculateHashOfJsonFor(obj3)
            );

            Assert.NotEqual(
                CosmosDbPartitionedItem<TestClassItem>.CalculateHashOfJsonFor(obj2),
                CosmosDbPartitionedItem<TestClassItem>.CalculateHashOfJsonFor(obj3)
            );
        }

        [Fact]
        public void CalculateHashOfJsonFor_WhenDifferenceIrrelevant_ThenSameHash()
        {
            var obj1 = new TestClassItem { Key = "Schlüssel", Value = 32, IrrelevantForHashCalculation = 1 };
            var obj2 = new TestClassItem { Key = "Schlüssel", Value = 32, IrrelevantForHashCalculation = 100 };

            Assert.Equal(
                CosmosDbPartitionedItem<TestClassItem>.CalculateHashOfJsonFor(obj1),
                CosmosDbPartitionedItem<TestClassItem>.CalculateHashOfJsonFor(obj2)
            );
        }
    }

    /// <summary>
    /// Dieser Typ sollte als Element der Datenbank gut funktionieren.
    /// </summary>
    [CosmosContainer(Name = "box")]
    internal class TestClassItem
    {
        [CosmosPartitionKey]
        [JsonProperty("key")]
        public string Key { get; set; }

        [JsonProperty("value")]
        public int Value { get; set; }

        public int IrrelevantForHashCalculation { get; set; }
    }

    /// <summary>
    /// Dieser Typ funktioniert nicht als Element:
    /// ihm felht der Name des Containers.
    /// </summary>
    internal class TestClassItemNoContainer
    {
        [CosmosPartitionKey]
        [JsonProperty("key")]
        public string Key { get; set; }

        public string Value { get; set; }
    }

    /// <summary>
    /// Dieser Typ funktioniert nicht als Element:
    /// Partitionsschlüssel nicht vorhanden.
    /// </summary>
    [CosmosContainer(Name = "box")]
    internal class TestClassItemNoKey
    {
        [JsonProperty("key")]
        public string Key { get; set; }

        [JsonProperty("value")]
        public string Value { get; set; }
    }

    /// <summary>
    /// Dieser Typ funktioniert nicht als Element:
    /// Es fehlt dem Partitionsschlüssel ein Name aus JSON-Serialisierung.
    /// </summary>
    [CosmosContainer(Name = "box")]
    internal class TestClassItemKeyLacksJson
    {
        [CosmosPartitionKey]
        public string Key { get; set; }

        [JsonProperty("value")]
        public string Value { get; set; }
    }

    /// <summary>
    /// Dieser Typ funktioniert nicht als Element:
    /// Nur ein einzelner Partitionsschlüssel ist erlaubt.
    /// </summary>
    [CosmosContainer(Name = "box")]
    internal class TestClassItemWithTwoKeys
    {
        [CosmosPartitionKey]
        [JsonProperty("key")]
        public string Key { get; set; }

        [CosmosPartitionKey]
        public string Value { get; set; }
    }

}// end of namespace Reusable.DataAccess.UnitTests
