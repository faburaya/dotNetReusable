using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Reusable.DataAccess.Cosmos
{
    /// <summary>
    /// Gewährt Zugang auf den Elementtyp <typeparamref name="DataType"/> in Azure Cosmos Datenbank.
    /// </summary>
    public class CosmosDbItemAccess<DataType> : Common.ITableAccess<DataType>
        where DataType : DataModels.CosmosDbItem<DataType>, IEquatable<DataType>
    {
        private readonly int _maxAsyncTasks;

        private ICosmosDbService<DataType> DatabaseService { get; }

        private List<Task> Insertions { get; set; }

        /// <summary>
        /// Erstellt eine neue Instanz dieser Klasse.
        /// </summary>
        /// <param name="dbService">Der Datenbankdienst.</param>
        /// <param name="maxConcurrentOperations">Anzahl von Vorgängen, die parallel durchgeführt werden dürfen.</param>
        public CosmosDbItemAccess(ICosmosDbService<DataType> dbService, int maxConcurrentOperations = 20)
        {
            this.DatabaseService = dbService;
            this.Insertions = new List<Task>(capacity: maxConcurrentOperations);
            _maxAsyncTasks = maxConcurrentOperations;
        }

        /// <inheritdoc/>
        public bool IsEmpty()
        {
            return (DatabaseService.GetItemCountAsync().Result == 0);
        }

        /// <inheritdoc/>
        public void Insert(DataType obj)
        {
            Insertions.Add(DatabaseService.AddItemAsync(obj));

            if (Insertions.Count >= _maxAsyncTasks)
            {
                var pendingRequests = Insertions.SkipWhile(task => task.IsCompleted);
                pendingRequests.FirstOrDefault()?.Wait();
                Insertions = pendingRequests.ToList();
            }
        }

        /// <inheritdoc/>
        public void Commit()
        {
            Task.WaitAll(Insertions.ToArray());
            Insertions.Clear();
        }

    }// end of class CosmosDbItemAccess

}// using namespace Reusable.DataAccess.Cosmos
