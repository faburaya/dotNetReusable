using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Xunit;

namespace Reusable.DataAccess.IntegrationTests
{
    public class CosmosDbServiceTest : IClassFixture<CosmosDatabaseFixture>
    {
        private CosmosDatabaseFixture Fixture { get; }

        public CosmosDbServiceTest(CosmosDatabaseFixture fixture)
        {
            this.Fixture = fixture;
        }

        [Fact]
        public void AddItem_WhenSingle()
        {
            var expectedItem = new TestItem { Name = "Werner", Family = "Heisenberg" };
            Fixture.Service.AddItemAsync(expectedItem).Wait();

            // Überprüfung:
            using var cosmosDataAccess = Fixture.GetAccessToCosmosContainerData();
            var results = cosmosDataAccess.CollectResultsFromQuery(source => source.Select(item => item));

            Assert.Single(results);
            TestItem actualItem = results.First();
            Assert.NotNull(actualItem);
            Assert.False(string.IsNullOrEmpty(actualItem.Id));
            Assert.True(actualItem.Name == expectedItem.Name);
            Assert.True(actualItem.Family == expectedItem.Family);
        }

        [Fact]
        public void AddItem_WhenDistinct()
        {
            var expectedItems = new List<TestItem> {
                new TestItem { Name = "Werner", Family = "Heisenberg" },
                new TestItem { Name = "Katze", Family = "Schrödinger" },
            };
            TestAddMultipleItems(expectedItems);
        }

        [Fact]
        public void AddItem_WhenEquivalent()
        {
            var item = new TestItem { Name = "Andressa", Family = "Rabah" };
            var expectedItems = new List<TestItem> { item, item };
            TestAddMultipleItems(expectedItems);
        }

        private void TestAddMultipleItems(IList<TestItem> expectedItems)
        {
            var tasks = (from item in expectedItems select Fixture.Service.AddItemAsync(item)).ToArray();
            Task.WaitAll(tasks);

            // Überprüfung:
            using var cosmosDataAccess = Fixture.GetAccessToCosmosContainerData();
            var storedItems = cosmosDataAccess.CollectResultsFromQuery(
                source => source.Select(item => item).OrderBy(item => item.Id));
            Assert.Equal(expectedItems.Count, storedItems.Count());
            Assert.All(storedItems, item => Assert.False(string.IsNullOrEmpty(item.Id)));

            foreach (TestItem expectedItem in expectedItems)
            {
                Assert.Contains(storedItems,
                    actualItem => {
                        return actualItem.Name == expectedItem.Name
                            && actualItem.Family == expectedItem.Family;
                    });
            }

            var returnedItems = (from task in tasks select task.Result).OrderBy(item => item.Id);
            Assert.Equal(
                returnedItems,
                storedItems,
                new DataModels.CosmosStoredItemComparer<TestItem>());
        }

        private IEnumerable<TestItem> AddAndRetrieveItems(IList<TestItem> items,
                                                          ContainerDataAutoReset cosmosDataAccess)
        {
            var itemsAddedToContainer = cosmosDataAccess.AddToContainer(items);

            if (itemsAddedToContainer.Count() != items.Count)
            {
                throw new Exception($"Es ist der Vorbereitung des Testszenarios nicht gelungen, dem Container einige Elemente hinzuzufügen! (Nur {itemsAddedToContainer.Count()} Elemente statt {items.Count} sind dort gespeichert.)");
            }

            return itemsAddedToContainer;
        }

        [Fact]
        public void UpsertItem_WhenNotPresent_ThenAddIt()
        {
            using var cosmosDataAccess = Fixture.GetAccessToCosmosContainerData();

            var availableItems = AddAndRetrieveItems(new List<TestItem> {
                new TestItem { Name = "Paloma", Family = "Farah" },
                new TestItem { Name = "Andressa", Family = "Rabah" },
            }, cosmosDataAccess);

            var newItem = new TestItem { Id = "CAFEBABE", Name = "Liane", Family = "Oliveira" };
            Fixture.Service.UpsertItemAsync(newItem.PartitionKeyValue, newItem).Wait();

            // Überprüft, dass das Element tatsächliche so gespeichert wurde:
            var results = cosmosDataAccess.CollectResultsFromQuery(source => source.Select(item => item));
            Assert.Equal(availableItems.Count() + 1, results.Count);
            Assert.Contains(results, storedItem => storedItem.IsEquivalentInStorageTo(newItem));
            foreach (TestItem item in availableItems)
            {
                Assert.Contains(results, storedItem => storedItem.IsEquivalentInStorageTo(item));
            }
        }

        [Fact]
        public void UpsertItem_WhenPresent_IfOtherPropertyChanges_ThenUpdateIt()
        {
            using var cosmosDataAccess = Fixture.GetAccessToCosmosContainerData();

            var availableItems = AddAndRetrieveItems(new List<TestItem> {
                new TestItem { Name = "Paloma", Family = "Farah" },
                new TestItem { Name = "Andressa", Family = "Rabah" },
            }, cosmosDataAccess);

            TestUpdateItem(cosmosDataAccess,
                           availableItems,
                           availableItems.First(),
                           item => item.Name = "Liane");
        }

        [Fact]
        public void UpsertItem_WhenPresent_IfPartitionKeyChanges_ThenUpdateIt()
        {
            using var cosmosDataAccess = Fixture.GetAccessToCosmosContainerData();

            var availableItems = AddAndRetrieveItems(new List<TestItem> {
                new TestItem { Name = "Paloma", Family = "Farah" },
                new TestItem { Name = "Andressa", Family = "Rabah" },
            }, cosmosDataAccess);

            TestUpdateItem(cosmosDataAccess,
                           availableItems,
                           availableItems.Last(),
                           item => item.Family = "Oliveira");
        }

        void TestUpdateItem(ContainerDataAutoReset cosmosDataAccess,
                            IEnumerable<TestItem> availableItems,
                            TestItem itemToUpdate,
                            Action<TestItem> mutate)
        {
            string originalPartitionKey = itemToUpdate.PartitionKeyValue;
            mutate(itemToUpdate);
            Fixture.Service.UpsertItemAsync(originalPartitionKey, itemToUpdate).Wait();

            // Überprüft, dass das Element tatsächliche so gespeichert wurde:
            var results = cosmosDataAccess.CollectResultsFromQuery(source => source.Select(item => item));
            foreach (TestItem item in availableItems)
            {
                Assert.Contains(results, storedItem => storedItem.IsEquivalentInStorageTo(item));
            }
            Assert.Equal(availableItems.Count(), results.Count());
        }

        [Fact]
        public void DeleteItem_WhenDistinct_IfAllDeleted_ThenNothingRemains()
        {
            using var cosmosDataAccess = Fixture.GetAccessToCosmosContainerData();

            var itemsBeforeDeletion = AddAndRetrieveItems(new List<TestItem> {
                new TestItem { Name = "Paloma", Family = "Farah" },
                new TestItem { Name = "Andressa", Family = "Rabah" },
            }, cosmosDataAccess);

            Task.WaitAll((from item in itemsBeforeDeletion
                          select Fixture.Service.DeleteItemAsync(item.PartitionKeyValue, item.Id))
                          .ToArray());

            var itemsAfterDeletion =
                cosmosDataAccess.CollectResultsFromQuery(source => source.Select(item => item));

            foreach (TestItem item in itemsBeforeDeletion)
            {
                Assert.DoesNotContain(itemsAfterDeletion,
                    remainingItem => item.IsEquivalentInStorageTo(remainingItem));
            }
            Assert.Empty(itemsAfterDeletion);
        }

        [Fact]
        public void DeleteItem_WhenSimilar_IfOneDeleted_ThenAnotherRemains()
        {
            using var cosmosDataAccess = Fixture.GetAccessToCosmosContainerData();

            var itemsBeforeDeletion = AddAndRetrieveItems(new List<TestItem> {
                new TestItem { Name = "Paloma", Family = "Farah" },
                new TestItem { Name = "Andressa", Family = "Rabah" },
            }, cosmosDataAccess);

            // Löscht das erste Element:
            TestItem item1 = itemsBeforeDeletion.First();
            Fixture.Service.DeleteItemAsync(item1.PartitionKeyValue, item1.Id).Wait();

            var itemsAfterDeletion =
                cosmosDataAccess.CollectResultsFromQuery(source => source.Select(item => item));

            Assert.DoesNotContain(itemsAfterDeletion,
                remainingItem => item1.IsEquivalentInStorageTo(remainingItem));

            Assert.Equal(itemsBeforeDeletion.Count() - 1, itemsAfterDeletion.Count());

            // Löscht das zweite Element:
            TestItem item2 = itemsBeforeDeletion.Last();
            Fixture.Service.DeleteItemAsync(item2.PartitionKeyValue, item2.Id).Wait();

            itemsAfterDeletion =
                cosmosDataAccess.CollectResultsFromQuery(source => source.Select(item => item));

            Assert.DoesNotContain(itemsAfterDeletion,
                remainingItem => item2.IsEquivalentInStorageTo(remainingItem));

            Assert.Equal(itemsBeforeDeletion.Count() - 2, itemsAfterDeletion.Count());
        }

        [Fact]
        public void GetItemCount_BeforeAndAfterAddingNew()
        {
            using var cosmosDataAccess = Fixture.GetAccessToCosmosContainerData();

            Assert.Equal(0, Fixture.Service.GetItemCountAsync().Result);

            var addedItems = AddAndRetrieveItems(new List<TestItem> {
                new TestItem { Name = "Paloma", Family = "Farah" },
                new TestItem { Name = "Andressa", Family = "Rabah" },
            }, cosmosDataAccess);

            Assert.Equal(addedItems.Count(), Fixture.Service.GetItemCountAsync().Result);
        }

        [Fact]
        public void GetItem_WhenNotPresent_ReturnNull()
        {
            using var cosmosDataAccess = Fixture.GetAccessToCosmosContainerData();

            Assert.Null(Fixture.Service.GetItemAsync("nicht", "vorhanden").Result);

            var availableItems = AddAndRetrieveItems(new List<TestItem> {
                new TestItem { Name = "Paloma", Family = "Farah" },
                new TestItem { Name = "Andressa", Family = "Rabah" },
            }, cosmosDataAccess);

            Assert.Null(Fixture.Service.GetItemAsync("nicht", "vorhanden").Result);
        }

        [Fact]
        public void GetItem_WhenPresent_ReturnIt()
        {
            using var cosmosDataAccess = Fixture.GetAccessToCosmosContainerData();

            var addedItems = AddAndRetrieveItems(new List<TestItem> {
                new TestItem { Name = "Paloma", Family = "Farah" },
                new TestItem { Name = "Andressa", Family = "Rabah" },
            }, cosmosDataAccess);

            var getItemRequests = (from item in addedItems
                                   select new {
                                       expectedItem = item,
                                       promise = Fixture.Service.GetItemAsync(item.PartitionKeyValue, item.Id)
                                   }).ToArray();

            Assert.All(from request in getItemRequests
                       select new {
                           expectedItem = request.expectedItem,
                           actualItem = request.promise.Result
                       },
                x => Assert.True(x.actualItem.IsEquivalentInStorageTo(x.expectedItem))
            );
        }

        [Fact]
        public void Query_WhenNotPresent_ReturnNothing()
        {
            using var cosmosDataAccess = Fixture.GetAccessToCosmosContainerData();

            var addedItems = AddAndRetrieveItems(new List<TestItem> {
                new TestItem { Name = "Paloma", Family = "Farah" },
                new TestItem { Name = "Andressa", Family = "Rabah" },
            }, cosmosDataAccess);

            var results = Fixture.Service.QueryAsync(source =>
                source.Where(item => item.Name == "Liane")
                      .Select(item => item)).Result;

            Assert.Empty(results);
        }

        [Fact]
        public void Query_WhenPresent_IfOne_ReturnIt()
        {
            using var cosmosDataAccess = Fixture.GetAccessToCosmosContainerData();

            var addedItems = AddAndRetrieveItems(new List<TestItem> {
                new TestItem { Name = "Paloma", Family = "Farah" },
                new TestItem { Name = "Andressa", Family = "Rabah" },
            }, cosmosDataAccess);

            var itemToQuery = addedItems.First();

            var task = Fixture.Service.QueryAsync(source =>
                source.Where(item => item.Name == itemToQuery.Name && item.Family == itemToQuery.Family)
                      .Select(item => item));

            var retrievedItem = task.Result.FirstOrDefault();
            Assert.NotNull(retrievedItem);
            Assert.True(itemToQuery.IsEquivalentInStorageTo(retrievedItem));
        }

        [Fact]
        public void Query_WhenPresent_IfMany_ReturnThem()
        {
            using var cosmosDataAccess = Fixture.GetAccessToCosmosContainerData();

            var addedItems = AddAndRetrieveItems(new List<TestItem> {
                new TestItem { Name = "Paloma", Family = "Farah" },
                new TestItem { Name = "Andressa", Family = "Rabah" },
            }, cosmosDataAccess);

            var retrievedItems = Fixture.Service.QueryAsync(
                source => source.Select(item => item).OrderBy(item => item.Family)).Result;

            Assert.NotEmpty(retrievedItems);
            Assert.Equal(addedItems.OrderBy(item => item.Family),
                         retrievedItems,
                         new DataModels.CosmosStoredItemComparer<TestItem>());
        }

    }// end of class CosmosDbServiceTest

}// end of namespace Reusable.DataAccess.IntegrationTests
