using System;
using System.Collections.Generic;
using System.Linq;

using Xunit;

namespace Reusable.DataAccess.IntegrationTests
{
    [Collection("IntegrationTests")]
    public class CosmosDbItemAccessTest
    {
        private CosmosDatabaseFixture Fixture { get; }

        private CosmosDbItemAccess<TestItem> ItemAccess { get; }

        public CosmosDbItemAccessTest(CosmosDatabaseFixture fixture)
        {
            this.Fixture = fixture;
            this.ItemAccess = new CosmosDbItemAccess<TestItem>(fixture.Service, 50);
        }

        private IEnumerable<TestItem> AddAndRetrieveItems(IList<TestItem> items,
                                                          ContainerDataAutoReset cosmosDirectAccess)
        {
            var itemsAddedToContainer = cosmosDirectAccess.AddToContainer(items);

            if (itemsAddedToContainer.Count() != items.Count)
            {
                throw new Exception($"Es ist der Vorbereitung des Testszenarios nicht gelungen, dem Container einige Elemente hinzuzufügen! (Nur {itemsAddedToContainer.Count()} Elemente statt {items.Count} sind dort gespeichert.)");
            }

            return itemsAddedToContainer;
        }

        [Fact]
        public void IsEmpty_WhenEmpty_ThenReturnTrue()
        {
            Assert.True(ItemAccess.IsEmpty());
        }

        [Fact]
        public void IsEmpty_WhenNotEmpty_ThenReturnFalse()
        {
            using var cosmosDirectAccess = Fixture.GetDirectAccessToCosmosContainer();

            var availableItems = AddAndRetrieveItems(new List<TestItem> {
                new TestItem { Name = "Paloma", Family = "Farah" },
                new TestItem { Name = "Andressa", Family = "Rabah" },
            }, cosmosDirectAccess);

            Assert.False(ItemAccess.IsEmpty());
        }

        [Fact]
        public void InsertAndCommit_WhenNothingToInsert_ThenDoNothing()
        {
            ItemAccess.Commit();
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(100)]
        public void InsertAndCommit_WhenHasItemsToInsert_ThenAddThem(int numItems)
        {
            using var cosmosDirectAccess = Fixture.GetDirectAccessToCosmosContainer();

            var itemsToAdd = new List<TestItem>(capacity: numItems);
            for (int count = 0; count < numItems; ++count)
            {
                itemsToAdd.Add(new TestItem {
                    Name = $"Name{count}",
                    Family = $"Familie{count % 5}"
                });
            }

            foreach (TestItem item in itemsToAdd)
            {
                ItemAccess.Insert(item);
            }
            ItemAccess.Commit();

            var results = cosmosDirectAccess.CollectResultsFromQuery(source => source.Select(item => item));
            foreach (TestItem item in itemsToAdd)
            {
                Assert.Contains(results, storedItem =>
                    storedItem.Name == item.Name && storedItem.Family == item.Family);
            }
            Assert.Equal(itemsToAdd.Count, results.Count());
        }

    }// end of class CosmosDbItemAccessTest*

}// end of namespace Reusable.DataAccess.IntegrationTests