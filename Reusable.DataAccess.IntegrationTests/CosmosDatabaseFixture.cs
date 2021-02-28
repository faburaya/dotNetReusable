using System;

using Microsoft.Azure.Cosmos;

using Xunit;

namespace Reusable.DataAccess.IntegrationTests
{
    public class CosmosDatabaseFixture : IDisposable
    {
        private string ConnectionString => "AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";

        private string DatabaseName => "CurrencyMonitor.DataAccess.IntegrationTests";

        private CosmosClient Client { get; }

        public CosmosDbService<TestItem> Service { get; }

        /// <summary>
        /// Gewährt Zugang auf die Daten im Cosmos Container.
        /// </summary>
        /// <remarks>
        /// Es geht davon aus, dass das Container und die Datenbank vorhanden sind.
        /// </remarks>
        public ContainerDataAutoReset GetAccessToCosmosContainerData()
        {
            Container container = Client.GetContainer(DatabaseName,
                DataModels.CosmosDbPartitionedItem<TestItem>.ContainerName);
            Assert.NotNull(container);
            return new ContainerDataAutoReset(container);
        }

        public CosmosDatabaseFixture()
        {
            this.Client = new CosmosClient(ConnectionString);

            CosmosDbService<TestItem> cosmosDbService =
                CosmosDbService<TestItem>.InitializeCosmosClientInstanceAsync(DatabaseName, ConnectionString)
                    .GetAwaiter()
                    .GetResult();

            Assert.NotNull(cosmosDbService);
            this.Service = cosmosDbService;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~CosmosDatabaseFixture() => Dispose(false);

        private bool _disposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            // Macht alle Änderungen in Cosmos Datenbank rückgängig:
            Database database = Client.GetDatabase(DatabaseName);
            database?.DeleteAsync().Wait();

            if (disposing)
            {
                Client.Dispose();
            }

            _disposed = true;
        }

    }// end of class CosmosDatabaseFixture

}// end of namespace CurrencyMonitor.DataAccess.IntegrationTests
