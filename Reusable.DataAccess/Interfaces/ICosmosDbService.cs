using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Reusable.DataAccess
{
    /// <summary>
    /// Generische Schnittstelle, die Zugang auf die Azure Cosmos Datenbank gewährt.
    /// </summary>
    /// <typeparam name="ItemType">Der Typ in der Datenbank, mit dem man umgehen will.</typeparam>
    public interface ICosmosDbService<ItemType>
        where ItemType : DataModels.CosmosDbItem<ItemType>, IEquatable<ItemType>
    {
        /// <summary>
        /// Fragt die Datenbank ab.
        /// </summary>
        /// <param name="query">Die LINQ-Abfrage.</param>
        /// <returns>Die von der Abfrage zurückgegebenen Elemente.</returns>
        Task<IEnumerable<ItemType>> QueryAsync(Func<IOrderedQueryable<ItemType>, IQueryable<ItemType>> query);

        /// <summary>
        /// Holt ein Element in der Datenbank.
        /// </summary>
        /// <param name="partitionKey">Der Partitionsschlüssel.</param>
        /// <param name="id">Die Identifikation des Elements.</param>
        Task<ItemType> GetItemAsync(string partitionKey, string id);

        /// <summary>
        /// Stellt das Anzahl von bisherig gespeicherten Elementen.
        /// </summary>
        /// <returns>Wie viele Element des Typs in der Datenbank dastehen.</returns>
        Task<int> GetItemCountAsync();

        /// <summary>
        /// Fügt ein neues Element in der Datenbank hinzu.
        /// </summary>
        /// <param name="item">Das zu speichernde Element.</param>
        /// <remarks>Ein neues ID wird geschaffen für das neue Element.</remarks>
        Task<ItemType> AddItemAsync(ItemType item);

        /// <summary>
        /// Löscht ein Element in der Datenbank.
        /// </summary>
        /// <param name="partitionKey">Der Partitionsschlüssel.</param>
        /// <param name="id">Die Identifikation des Elements.</param>
        Task DeleteItemAsync(string partitionKey, string id);

        /// <summary>
        /// Löscht viele Elemente in der Datenbank durch transaktionale Batchvorgänge.
        /// </summary>
        /// <remarks>
        /// Azure Cosmos Datenbank gewährt transaktionale Batchvorgänge,
        /// insofern alle Elemente zu der gleichen Partition gehören.
        /// </remarks>
        /// <param name="partitionKey">Der Partitionsschlüssel aller Elemente.</param>
        /// <param name="ids">Die Identifikatiosnummern der zu löschenden Elemente.</param>
        Task DeleteBatchAsync(string partitionKey, IList<string> ids);

        /// <summary>
        /// Ändert ein vorherig bestehendes Element, oder
        /// wenn es nicht vorhanden ist, wird es hinzugefügt. 
        /// </summary>
        /// <param name="partitionKey">Der urprüngliche Partitionsschlüssel (vor der Änderung) des Elements.</param>
        /// <param name="item">Das zu speichernde geänderte Element.</param>
        /// <remarks>
        /// Achtung!
        /// (1) Anders als das Hinzufügen, erwartungsmäßig wird kein neues ID für das zu ändernde
        /// Element geschaffen. Das im Element bestehende ID muss passend sein.
        /// (2) Wenn der gegebene ursprüngliche Partitionsschlüssel nicht mit dem Wert im gegebenen
        /// zu ändernden Element übereinstimmt, heißt es, dass solches Element auf eine andere Partition
        /// verschoben wird. Bevor es in der neuen Partition hinzugefügt wird, muss es zuerst in der
        /// bisherigen gelöscht werden. Wenn der ursprüngliche Wert nicht stimmt, dann wird es nicht
        /// gefunden und die ganze Operation scheitert.
        /// </remarks>
        Task UpsertItemAsync(string partitionKey, ItemType item);

        /// <summary>
        /// Führt ein transaktionales Batch von Upsert-Vorgänge.
        /// </summary>
        /// <param name="items">
        /// Eine Liste der zu ändernden Elemente.
        /// Alle Elemente müssen zu der gleichen Partition gehören.
        /// </param>
        Task UpsertBatchAsync(IList<ItemType> items);
    }

}// namespace Reusable.DataAccess