using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;

namespace Reusable.DataAccess
{
    /// <summary>
    /// Implementierung für einen Diest, der Zugang auf Azure Cosmos Datenbank gewährt.
    /// </summary>
    public class CosmosDbService<ItemType> : ICosmosDbService<ItemType>
        where ItemType : DataModels.CosmosDbItem<ItemType>, IEquatable<ItemType>
    {
        private static readonly int maxItemsPerBatch = 100;

        private Common.UidGenerator<ItemType> _idGenerator;

        private readonly Container _container;

        /// <summary>
        /// Gewährleistet, dass die Azure Cosmos Datenbank und Container vorhanden sind,
        /// dann initialisiert ein Client und gibt es der Erstellung einer Instanz von
        /// <see cref="CosmosDbService{ItemType}"/> ab.
        /// </summary>
        /// <param name="databaseName">Der Name der Datenbank.</param>
        /// <param name="connectionString">The Verbindungszeichenfolge für die Datenbank.</param>
        /// <param name="idGenerator">Implementierung für die Beschafung einer einzigartigen ID.</param>
        /// <returns>Die erstellte Instanz von <see cref="CosmosDbService{ItemType}"/></returns>
        public static async Task<CosmosDbService<ItemType>> InitializeCosmosClientInstanceAsync(
            string databaseName,
            string connectionString,
            Common.UidGenerator<ItemType> idGenerator)
        {
            var client = new CosmosClient(connectionString);
            var service = new CosmosDbService<ItemType>(client, databaseName, idGenerator);
            DatabaseResponse response = await client.CreateDatabaseIfNotExistsAsync(databaseName);
            await response.Database.CreateContainerIfNotExistsAsync(
                DataModels.CosmosDbPartitionedItem<ItemType>.ContainerName,
                DataModels.CosmosDbPartitionedItem<ItemType>.PartitionKeyPath);

            return service;
        }

        public static async Task<CosmosDbService<ItemType>> InitializeCosmosClientInstanceAsync(
            string databaseName,
            string connectionString)
        {
            return await InitializeCosmosClientInstanceAsync(databaseName,
                                                             connectionString,
                                                             new Common.UidGenerator<ItemType>());
        }

        private CosmosDbService(CosmosClient dbClient,
                                string databaseName,
                                Common.UidGenerator<ItemType> idGenerator)
        {
            _idGenerator = idGenerator;
            _container = dbClient.GetContainer(databaseName, DataModels.CosmosDbPartitionedItem<ItemType>.ContainerName);
        }

        public async Task<IEnumerable<ItemType>> QueryAsync(
            Func<IOrderedQueryable<ItemType>, IQueryable<ItemType>> query)
        {
            var results = new List<ItemType>();

            using FeedIterator<ItemType> iterator =
                query(_container.GetItemLinqQueryable<ItemType>()).ToFeedIterator();

            while (iterator.HasMoreResults)
            {
                FeedResponse<ItemType> response = await iterator.ReadNextAsync();
                results.AddRange(response);
            }

            return results;
        }

        public async Task<ItemType> GetItemAsync(string partitionKey, string id)
        {
            try
            {
                ItemResponse<ItemType> response =
                    await _container.ReadItemAsync<ItemType>(id, new PartitionKey(partitionKey));
                return response.Resource;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return default;
            }
        }

        public async Task<int> GetItemCountAsync()
        {
            using FeedIterator<int> query = _container.GetItemQueryIterator<int>(
                "select value count(1) from c",
                requestOptions: new QueryRequestOptions { MaxItemCount = -1 }
            );

            if (!query.HasMoreResults)
            {
                throw new ServiceException("Datenbankabfrage für Anzahl von Elementen hat ein leeres Ergebnis zurückgegeben!");
            }

            var response = await query.ReadNextAsync();
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                throw new ServiceException($"Datenbankabfrage für Anzahl von Elementen ist mit HTTP {response.StatusCode} gescheitert!");
            }

            return response.First();
        }

        public async Task<ItemType> AddItemAsync(ItemType item)
        {
            item = item.ShallowCopy();
            item.Id = _idGenerator.GenerateIdFor(item);
            ItemResponse<ItemType> response = await _container.CreateItemAsync(item);
            return response.Resource;
        }

        public async Task DeleteItemAsync(string partitionKey, string id)
        {
            await _container.DeleteItemAsync<ItemType>(id, new PartitionKey(partitionKey));
        }

        public async Task DeleteBatchAsync(string partitionKey, IList<string> ids)
        {
            for (int itemIdx = 0; itemIdx < ids.Count; itemIdx += maxItemsPerBatch)
            {
                IEnumerable<string> slice = ids.Skip(itemIdx).Take(maxItemsPerBatch);
                TransactionalBatch batch = _container.CreateTransactionalBatch(new PartitionKey(partitionKey));
                foreach (string id in slice)
                {
                    batch = batch.DeleteItem(id);
                }

                using TransactionalBatchResponse transaction = await batch.ExecuteAsync();
                if (!transaction.IsSuccessStatusCode)
                {
                    throw new ServiceException($"Batch (ab Element #{itemIdx} aus {ids.Count}) ist mit HTTP {transaction.StatusCode} gescheitert: {transaction.ErrorMessage}");
                }
            }
        }

        public async Task UpsertItemAsync(string partitionKey, ItemType item)
        {
            if (string.IsNullOrWhiteSpace(item.Id))
            {
                throw new ArgumentException("Das ID des zu ändernden Elements darf nicht leer sein!");
            }

            if (string.IsNullOrWhiteSpace(partitionKey))
            {
                throw new ArgumentException(
                    "Der gegebene ursprüngliche Partitionsschlüssel des Elements darf nicht leer sein!");
            }

            // Verändert sich der Partitionsschlüssel?
            if (item.PartitionKeyValue != partitionKey)
            {
                // zuerst löscht das ursprüngliche Element:
                await _container.DeleteItemAsync<ItemType>(item.Id, new PartitionKey(partitionKey));
            }

            await _container.UpsertItemAsync(item);
        }

        public async Task UpsertBatchAsync(IList<ItemType> items)
        {
            if (items.Count == 0)
            {
                return;
            }

            string partitionKey = items.First().PartitionKeyValue;
            ItemType anItemOutOfPartition = (from item in items
                                             where item.PartitionKeyValue != partitionKey
                                             select item).FirstOrDefault();

            if (anItemOutOfPartition != null)
            {
                throw new ArgumentException($"Element.PartitionKeyValue = '{anItemOutOfPartition.PartitionKeyValue}' gehört nicht zur gleichen Partition der anderen Elemente (= '{partitionKey}')");
            }

            for (int idx = 0; idx < items.Count; idx += maxItemsPerBatch)
            {
                TransactionalBatch batch = _container.CreateTransactionalBatch(new PartitionKey(partitionKey));

                var slice = items.Skip(idx).Take(maxItemsPerBatch);
                foreach (ItemType item in slice)
                {
                    batch = batch.UpsertItem(item);
                }

                // wartet auf Transaktion und entsorgt sie
                using var transaction = await batch.ExecuteAsync();
            }
        }

    }// end of class CosmosDbService

}// end of namespace Reusable.DataAccess
