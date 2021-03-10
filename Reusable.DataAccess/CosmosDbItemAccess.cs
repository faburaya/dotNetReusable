using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Reusable.DataAccess
{
    /// <summary>
    /// Gewährt Zugang auf ein Element in Azure Cosmos Datenbank.
    /// </summary>
    public class CosmosDbItemAccess<DataType> : ITableAccess<DataType>
        where DataType : DataModels.CosmosDbItem<DataType>, IEquatable<DataType>
    {
        private static readonly int maxAsyncTasks = 20;

        private ICosmosDbService<DataType> DatabaseService { get; }

        private List<Task> Insertions { get; }

        public CosmosDbItemAccess(ICosmosDbService<DataType> dbService)
        {
            this.DatabaseService = dbService;
            this.Insertions = new List<Task>();
        }

        public bool IsEmpty()
        {
            return (DatabaseService.GetItemCountAsync().Result == 0);
        }

        public void Insert(DataType obj)
        {
            var asyncAdd = DatabaseService.AddItemAsync(obj);

            if (Insertions.Count == maxAsyncTasks)
            {
                Commit();
            }

            Insertions.Add(asyncAdd);
        }

        public void Commit()
        {
            Task.WaitAll(Insertions.ToArray());
            Insertions.Clear();
        }
    }
}
