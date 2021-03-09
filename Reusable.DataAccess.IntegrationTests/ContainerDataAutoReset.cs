using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;

namespace Reusable.DataAccess.IntegrationTests
{
    /// <summary>
    /// Gewährt Zugang auf die Daten im Cosmos Container
    /// und löscht sie jedes Mal wenn es verworfen wird.
    /// </summary>
    public class ContainerDataAutoReset : IDisposable
    {
        private Container Container { get; }

        public ContainerDataAutoReset(Container cosmosContainer)
        {
            this.Container = cosmosContainer;
        }

        public delegate IQueryable<TestItem> QueryCosmos(IOrderedQueryable<TestItem> cosmosQueryableSource);

        /// <summary>
        /// Fragt die Cosmos Datenbank ab, wartet auf Ergebnisse und gibt sie zurück.
        /// </summary>
        /// <param name="query">Die zu laufende LINQ-Abfrage.</param>
        /// <returns>Die Ergebnisse der Abfrage.</returns>
        public IList<TestItem> CollectResultsFromQuery(QueryCosmos query)
        {
            using FeedIterator<TestItem> iterator =
                query(Container.GetItemLinqQueryable<TestItem>())
                .ToFeedIterator();

            var results = new List<TestItem>();
            while (iterator.HasMoreResults)
            {
                FeedResponse<TestItem> response = iterator.ReadNextAsync().Result;
                results.AddRange(response);
            }

            return results;
        }

        private static readonly int maxNumTasks = 20;

        /// <summary>
        /// Fügt dem Container neue Elemente hinzu.
        /// </summary>
        /// <param name="items">Die hinzuzufügenden Elemente.</param>
        public IEnumerable<TestItem> AddToContainer(IList<TestItem> items)
        {
            var tasks = new List<Task<ItemResponse<TestItem>>>(capacity: maxNumTasks);
            var addedItems = new List<TestItem>(capacity: items.Count);

            var randomizer = new Random();
            for (int idx = 0; idx < items.Count; ++idx)
            {
                TestItem item = items[idx];
                item.Id = randomizer.Next().ToString("X8");
                var asyncCreateTask = Container.CreateItemAsync(item, new PartitionKey(item.PartitionKeyValue));

                tasks.Add(asyncCreateTask);
                if (tasks.Count == maxNumTasks || idx + 1 == items.Count)
                {
                    Task.WaitAll(tasks.ToArray());
                    addedItems.AddRange(from task in tasks select task.Result.Resource);
                    tasks.Clear();
                }
            }

            return addedItems;
        }

        private void EraseAllItemsInContainer()
        {
            var allItems = CollectResultsFromQuery(source => source.Select(item => item));

            var tasks = new List<Task>(capacity: maxNumTasks);
            foreach (var item in allItems)
            {
                Task deleteAsyncTask =
                    Container.DeleteItemAsync<TestItem>(
                        item.Id, new PartitionKey(item.PartitionKeyValue));

                tasks.Add(deleteAsyncTask);

                if (tasks.Count == maxNumTasks)
                {
                    Task.WaitAll(tasks.ToArray());
                    tasks.Clear();
                }
            }

            Task.WaitAll(tasks.ToArray());
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~ContainerDataAutoReset() => Dispose(false);

        private bool _disposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                // verwirf hier die verwalteten Ressourcen
            }

            EraseAllItemsInContainer();

            _disposed = true;
        }

    }// end of class ContainerDataAutoReset

}// using namespace Reusable.DataAccess.IntegrationTests
