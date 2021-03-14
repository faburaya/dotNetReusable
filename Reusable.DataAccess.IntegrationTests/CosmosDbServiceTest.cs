using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Xunit;

namespace Reusable.DataAccess.IntegrationTests
{
    [Collection("IntegrationTests")]
    public class CosmosDbServiceTest
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
            using var cosmosDirectAccess = Fixture.GetDirectAccessToCosmosContainer();
            var results = cosmosDirectAccess.CollectResultsFromQuery(source => source.Select(item => item));

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
            using var cosmosDirectAccess = Fixture.GetDirectAccessToCosmosContainer();
            var storedItems = cosmosDirectAccess.CollectResultsFromQuery(
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
            Assert.Equal(returnedItems, storedItems);
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

        [Theory]
        [InlineData(null, "Oliveira")]
        [InlineData("CAFEBABE", null)]
        public void UpsertItem_WhenIdOrKeyNull_ThenThrow(string id, string partitionKey)
        {
            using var cosmosDirectAccess = Fixture.GetDirectAccessToCosmosContainer();

            var newItem = new TestItem { Id = id, Name = "Liane", Family = partitionKey };
            var exception = Assert.Throws<AggregateException>(
                () => Fixture.Service.UpsertItemAsync(newItem.PartitionKeyValue, newItem).Wait());
            
            Assert.IsType<ArgumentException>(exception.InnerException);

            // Überprüft, dass das Element tatsächliche so gespeichert wurde:
            var results = cosmosDirectAccess.CollectResultsFromQuery(source => source.Select(item => item));
            Assert.Empty(results);
        }

        [Fact]
        public void UpsertItem_WhenNotPresent_ThenAddIt()
        {
            using var cosmosDirectAccess = Fixture.GetDirectAccessToCosmosContainer();

            var availableItems = AddAndRetrieveItems(new List<TestItem> {
                new TestItem { Name = "Paloma", Family = "Farah" },
                new TestItem { Name = "Andressa", Family = "Rabah" },
            }, cosmosDirectAccess);

            var newItem = new TestItem { Id = "CAFEBABE", Name = "Liane", Family = "Oliveira" };
            Fixture.Service.UpsertItemAsync(newItem.PartitionKeyValue, newItem).Wait();

            // Überprüft, dass das Element tatsächliche so gespeichert wurde:
            var results = cosmosDirectAccess.CollectResultsFromQuery(source => source.Select(item => item));
            Assert.Equal(availableItems.Count() + 1, results.Count);
            Assert.Contains(results, storedItem => storedItem.Equals(newItem));
            foreach (TestItem item in availableItems)
            {
                Assert.Contains(results, storedItem => storedItem.Equals(item));
            }
        }

        [Fact]
        public void UpsertItem_WhenPresent_IfOtherPropertyChanges_ThenUpdateIt()
        {
            using var cosmosDirectAccess = Fixture.GetDirectAccessToCosmosContainer();

            var availableItems = AddAndRetrieveItems(new List<TestItem> {
                new TestItem { Name = "Paloma", Family = "Farah" },
                new TestItem { Name = "Andressa", Family = "Rabah" },
            }, cosmosDirectAccess);

            TestUpdateItem(cosmosDirectAccess,
                           availableItems,
                           availableItems.First(),
                           item => item.Name = "Liane");
        }

        [Fact]
        public void UpsertItem_WhenPresent_IfPartitionKeyChanges_ThenUpdateIt()
        {
            using var cosmosDirectAccess = Fixture.GetDirectAccessToCosmosContainer();

            var availableItems = AddAndRetrieveItems(new List<TestItem> {
                new TestItem { Name = "Paloma", Family = "Farah" },
                new TestItem { Name = "Andressa", Family = "Rabah" },
            }, cosmosDirectAccess);

            TestUpdateItem(cosmosDirectAccess,
                           availableItems,
                           availableItems.Last(),
                           item => item.Family = "Oliveira");
        }

        void TestUpdateItem(ContainerDataAutoReset cosmosDirectAccess,
                            IEnumerable<TestItem> availableItems,
                            TestItem itemToUpdate,
                            Action<TestItem> mutate)
        {
            string originalPartitionKey = itemToUpdate.PartitionKeyValue;
            mutate(itemToUpdate);
            Fixture.Service.UpsertItemAsync(originalPartitionKey, itemToUpdate).Wait();

            // Überprüft, dass das Element tatsächliche so gespeichert wurde:
            var results = cosmosDirectAccess.CollectResultsFromQuery(
                source => source.Select(item => item).OrderBy(item => item.Id));

            Assert.Equal(
                (from item in availableItems orderby item.Id select item),
                results);
        }

        [Fact]
        public void UpsertBatch_WhenNotInTheSamePartition_ThenThrow()
        {
            using var cosmosDirectAccess = Fixture.GetDirectAccessToCosmosContainer();

            var exception = Fixture.Service.UpsertBatchAsync(new List<TestItem> {
                new TestItem { Name = "Paloma", Family = "Farah" },
                new TestItem { Name = "Andressa", Family = "Rabah" },
            }).Exception?.InnerException;

            Assert.NotNull(exception);
            Assert.IsType<ArgumentException>(exception);
        }

        [Fact]
        public void UpsertBatch_WhenPresent_ThenUpdateIt()
        {
            using var cosmosDirectAccess = Fixture.GetDirectAccessToCosmosContainer();

            const string family = "Vieira Aburaya";
            var itemsBeforeUpdate = AddAndRetrieveItems(new List<TestItem> {
                new TestItem { Name = "Felipe", Family = family },
                new TestItem { Name = "Renata", Family = family },
                new TestItem { Name = "Caroline", Family = family },
            }, cosmosDirectAccess);

            var itemsToUpdate = (
                from item in itemsBeforeUpdate
                select new TestItem { Id = item.Id, Name = item.Name + '*', Family = item.Family }
            ).ToList();

            Fixture.Service.UpsertBatchAsync(itemsToUpdate).Wait();

            VerifyUpsertedBatch(cosmosDirectAccess, itemsToUpdate);
        }

        [Fact]
        public void UpsertBatch_WhenNotPresent_ThenAddIt()
        {
            using var cosmosDirectAccess = Fixture.GetDirectAccessToCosmosContainer();

            const string family = "Vieira Aburaya";
            var itemsBeforeUpdate = AddAndRetrieveItems(new List<TestItem> {
                new TestItem { Name = "Felipe", Family = family },
                new TestItem { Name = "Renata", Family = family },
            }, cosmosDirectAccess);

            var itemsToUpdate = (
                from item in itemsBeforeUpdate
                select new TestItem { Id = item.Id, Name = item.Name + '*', Family = item.Family }
            ).ToList();

            itemsToUpdate.Add(
                new TestItem { Id = Guid.NewGuid().ToString(), Name = "Caroline", Family = family });

            Fixture.Service.UpsertBatchAsync(itemsToUpdate).Wait();

            VerifyUpsertedBatch(cosmosDirectAccess, itemsToUpdate);
        }

        [Fact]
        public void UpsertBatch_WhenTooMany_ThenExecuteInMultipleBatches()
        {
            using var cosmosDirectAccess = Fixture.GetDirectAccessToCosmosContainer();

            const string family = "Familie";
            const int countItems = 250;
            var itemsToAdd = new List<TestItem>(countItems);
            for (int idx = 0; idx < countItems; ++idx)
            {
                itemsToAdd.Add(
                    new TestItem { Id = idx.ToString("X4"), Name = $"Name{idx}", Family = family });
            }

            Fixture.Service.UpsertBatchAsync(itemsToAdd).Wait();

            VerifyUpsertedBatch(cosmosDirectAccess, itemsToAdd);
        }

        private void VerifyUpsertedBatch(ContainerDataAutoReset cosmosDirectAccess,
                                         IList<TestItem> itemsToUpsert)
        {
            // Überprüft, dass die Elemente tatsächliche so gespeichert wurden:
            var results = cosmosDirectAccess.CollectResultsFromQuery(
                source => source.Select(item => item).OrderBy(item => item.Id));

            Assert.Equal(
                (from item in itemsToUpsert orderby item.Id select item),
                results);
        }

        [Fact]
        public void DeleteItem_WhenDistinct_IfAllDeleted_ThenNothingRemains()
        {
            using var cosmosDirectAccess = Fixture.GetDirectAccessToCosmosContainer();

            var itemsBeforeDeletion = AddAndRetrieveItems(new List<TestItem> {
                new TestItem { Name = "Paloma", Family = "Farah" },
                new TestItem { Name = "Andressa", Family = "Rabah" },
            }, cosmosDirectAccess);

            Task.WaitAll((from item in itemsBeforeDeletion
                          select Fixture.Service.DeleteItemAsync(item.PartitionKeyValue, item.Id))
                          .ToArray());

            var itemsAfterDeletion =
                cosmosDirectAccess.CollectResultsFromQuery(source => source.Select(item => item));

            foreach (TestItem item in itemsBeforeDeletion)
            {
                Assert.DoesNotContain(itemsAfterDeletion,
                    remainingItem => item.Equals(remainingItem));
            }
            Assert.Empty(itemsAfterDeletion);
        }

        [Fact]
        public void DeleteItem_WhenSimilar_IfOneDeleted_ThenAnotherRemains()
        {
            using var cosmosDirectAccess = Fixture.GetDirectAccessToCosmosContainer();

            var itemsBeforeDeletion = AddAndRetrieveItems(new List<TestItem> {
                new TestItem { Name = "Paloma", Family = "Farah" },
                new TestItem { Name = "Andressa", Family = "Rabah" },
            }, cosmosDirectAccess);

            // Löscht das erste Element:
            TestItem item1 = itemsBeforeDeletion.First();
            Fixture.Service.DeleteItemAsync(item1.PartitionKeyValue, item1.Id).Wait();

            var itemsAfterDeletion =
                cosmosDirectAccess.CollectResultsFromQuery(source => source.Select(item => item));

            Assert.DoesNotContain(itemsAfterDeletion,
                remainingItem => item1.Equals(remainingItem));

            Assert.Equal(itemsBeforeDeletion.Count() - 1, itemsAfterDeletion.Count());

            // Löscht das zweite Element:
            TestItem item2 = itemsBeforeDeletion.Last();
            Fixture.Service.DeleteItemAsync(item2.PartitionKeyValue, item2.Id).Wait();

            itemsAfterDeletion =
                cosmosDirectAccess.CollectResultsFromQuery(source => source.Select(item => item));

            Assert.DoesNotContain(itemsAfterDeletion,
                remainingItem => item2.Equals(remainingItem));

            Assert.Equal(itemsBeforeDeletion.Count() - 2, itemsAfterDeletion.Count());
        }

        [Fact]
        public void DeleteBatch_WhenAllDeleted_ThenNothingRemains()
        {
            using var cosmosDirectAccess = Fixture.GetDirectAccessToCosmosContainer();

            const string family = "Vieira Aburaya";
            var itemsBeforeDeletion = AddAndRetrieveItems(new List<TestItem> {
                new TestItem { Name = "Felipe", Family = family },
                new TestItem { Name = "Renata", Family = family },
            }, cosmosDirectAccess);

            Fixture.Service.DeleteBatchAsync(family,
                (from item in itemsBeforeDeletion select item.Id).ToList()).Wait();

            var itemsAfterDeletion =
                cosmosDirectAccess.CollectResultsFromQuery(source => source.Select(item => item));

            foreach (TestItem item in itemsBeforeDeletion)
            {
                Assert.DoesNotContain(itemsAfterDeletion,
                    remainingItem => item.Equals(remainingItem));
            }
            Assert.Empty(itemsAfterDeletion);
        }

        [Fact]
        public void DeleteBatch_WhenSomeDeleted_ThenOthersRemain()
        {
            using var cosmosDirectAccess = Fixture.GetDirectAccessToCosmosContainer();

            const string family = "Vieira Aburaya";
            var itemsBeforeDeletion = AddAndRetrieveItems(new List<TestItem> {
                new TestItem { Name = "Felipe", Family = family },
                new TestItem { Name = "Renata", Family = family },
                new TestItem { Name = "Caroline", Family = family },
            }, cosmosDirectAccess);

            var itemsToDelete = itemsBeforeDeletion.Take(2);

            Fixture.Service.DeleteBatchAsync(family,
                (from item in itemsToDelete select item.Id).ToList()).Wait();

            var itemsAfterDeletion =
                cosmosDirectAccess.CollectResultsFromQuery(source => source.Select(item => item));

            foreach (TestItem item in itemsToDelete)
            {
                Assert.DoesNotContain(itemsAfterDeletion,
                    remainingItem => item.Equals(remainingItem));
            }
            Assert.Equal(itemsBeforeDeletion.Count() - itemsToDelete.Count(), itemsAfterDeletion.Count);
        }

        [Fact]
        public void DeleteBatch_WhenTooMany_ThenExecuteInMultipleBatches()
        {
            using var cosmosDirectAccess = Fixture.GetDirectAccessToCosmosContainer();

            const string family = "Familie";
            const int countItems = 250;
            var itemsToAdd = new List<TestItem>(countItems);
            for (int idx = 0; idx < countItems; ++idx)
            {
                itemsToAdd.Add(new TestItem { Name = $"Name{idx}", Family = family });
            }

            var itemsBeforeDeletion = AddAndRetrieveItems(itemsToAdd, cosmosDirectAccess);

            Fixture.Service.DeleteBatchAsync(family,
                (from item in itemsBeforeDeletion select item.Id).ToList()).Wait();

            var itemsAfterDeletion =
                cosmosDirectAccess.CollectResultsFromQuery(source => source.Select(item => item));

            foreach (TestItem item in itemsBeforeDeletion)
            {
                Assert.DoesNotContain(itemsAfterDeletion,
                    remainingItem => item.Equals(remainingItem));
            }
            Assert.Empty(itemsAfterDeletion);
        }

        [Fact]
        public void GetItemCount_BeforeAndAfterAddingNew()
        {
            using var cosmosDirectAccess = Fixture.GetDirectAccessToCosmosContainer();

            Assert.Equal(0, Fixture.Service.GetItemCountAsync().Result);

            var addedItems = AddAndRetrieveItems(new List<TestItem> {
                new TestItem { Name = "Paloma", Family = "Farah" },
                new TestItem { Name = "Andressa", Family = "Rabah" },
            }, cosmosDirectAccess);

            Assert.Equal(addedItems.Count(), Fixture.Service.GetItemCountAsync().Result);
        }

        [Fact]
        public void GetItem_WhenNotPresent_ReturnNull()
        {
            using var cosmosDirectAccess = Fixture.GetDirectAccessToCosmosContainer();

            Assert.Null(Fixture.Service.GetItemAsync("nicht", "vorhanden").Result);

            var availableItems = AddAndRetrieveItems(new List<TestItem> {
                new TestItem { Name = "Paloma", Family = "Farah" },
                new TestItem { Name = "Andressa", Family = "Rabah" },
            }, cosmosDirectAccess);

            Assert.Null(Fixture.Service.GetItemAsync("nicht", "vorhanden").Result);
        }

        [Fact]
        public void GetItem_WhenPresent_ReturnIt()
        {
            using var cosmosDirectAccess = Fixture.GetDirectAccessToCosmosContainer();

            var addedItems = AddAndRetrieveItems(new List<TestItem> {
                new TestItem { Name = "Paloma", Family = "Farah" },
                new TestItem { Name = "Andressa", Family = "Rabah" },
            }, cosmosDirectAccess);

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
                x => Assert.True(x.actualItem.Equals(x.expectedItem))
            );
        }

        [Fact]
        public void Query_WhenNotPresent_ReturnNothing()
        {
            using var cosmosDirectAccess = Fixture.GetDirectAccessToCosmosContainer();

            var addedItems = AddAndRetrieveItems(new List<TestItem> {
                new TestItem { Name = "Paloma", Family = "Farah" },
                new TestItem { Name = "Andressa", Family = "Rabah" },
            }, cosmosDirectAccess);

            var results = Fixture.Service.QueryAsync(source =>
                source.Where(item => item.Name == "Liane")
                      .Select(item => item)).Result;

            Assert.Empty(results);
        }

        [Fact]
        public void Query_WhenPresent_IfOne_ReturnIt()
        {
            using var cosmosDirectAccess = Fixture.GetDirectAccessToCosmosContainer();

            var addedItems = AddAndRetrieveItems(new List<TestItem> {
                new TestItem { Name = "Paloma", Family = "Farah" },
                new TestItem { Name = "Andressa", Family = "Rabah" },
            }, cosmosDirectAccess);

            var itemToQuery = addedItems.First();

            var task = Fixture.Service.QueryAsync(source =>
                source.Where(item => item.Name == itemToQuery.Name && item.Family == itemToQuery.Family)
                      .Select(item => item));

            var retrievedItem = task.Result.FirstOrDefault();
            Assert.NotNull(retrievedItem);
            Assert.True(itemToQuery.Equals(retrievedItem));
        }

        [Fact]
        public void Query_WhenPresent_IfMany_ReturnThem()
        {
            using var cosmosDirectAccess = Fixture.GetDirectAccessToCosmosContainer();

            var addedItems = AddAndRetrieveItems(new List<TestItem> {
                new TestItem { Name = "Paloma", Family = "Farah" },
                new TestItem { Name = "Andressa", Family = "Rabah" },
            }, cosmosDirectAccess);

            var retrievedItems = Fixture.Service.QueryAsync(
                source => source.Select(item => item).OrderBy(item => item.Family)).Result;

            Assert.NotEmpty(retrievedItems);
            Assert.Equal(addedItems.OrderBy(item => item.Family), retrievedItems);
        }

    }// end of class CosmosDbServiceTest

}// end of namespace Reusable.DataAccess.IntegrationTests
