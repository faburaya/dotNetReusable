using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace Reusable.DataAccess.Common
{
    /// <summary>
    /// Hilft bei der Herstellung einer Verbindung mit einer Datenbank,
    /// die vielleicht noch nicht existiert oder leer ist.
    /// </summary>
    /// <typeparam name="DataType">Der zu speichernde Datentyp.</typeparam>
    public interface IDatabaseCreationHelper<in DataType>
    {
        /// <summary>
        /// Stellt eine neue Verbindung mit der Datenbank her.
        /// Wenn die Datenbank nicht existiert, wird er erstellt.
        /// </summary>
        /// <param name="dataSource">Das DataSource Teil der Verbindungzeichenkette.</param>
        /// <returns>Eine Verbindung (ADO.NET) mit der Datenbank.</returns>
        IDbConnection OpenOrCreateDatabase(string dataSource);

        /// <summary>
        /// Wenn nicht vorhanden, erstellt eine Tabelle zur Speicherung eines gegebenen Datentyps.
        /// </summary>
        /// <param name="tableName">Der Name der Tabelle.</param>
        /// <param name="connection">Die Verbindung mit der Datenbank.</param>
        /// <returns>Ob die Tabelle erstellt wurde.</returns>
        Task<bool> CreateTableIfNotExistentAsync(string tableName, IDbConnection connection);

        /// <summary>
        /// Fügt einer Tabelle neue Reihe hinzu.
        /// </summary>
        /// <param name="tableName">Der vorgegebene Name der Tabelle.</param>
        /// <param name="connection">Die Verbindung mit der Datenbank.</param>
        /// <param name="objects">Die in die Tabelle hinzufügende Objekte.</param>
        /// <returns>Wie viele Objekte der angegebene Tabelle hinzugefügt worden sind.</returns>
        /// <remarks>Am Ende des Aufrufs sind die Objekte gespeichert.</remarks>
        Task<int> InsertAsync(string tableName,
                              IDbConnection connection,
                              IEnumerable<DataType> objects);

        /// <summary>
        /// Fügt einer Tabelle neue Reihe hinzu.
        /// </summary>
        /// <param name="tableName">Der vorgegebene Name der Tabelle.</param>
        /// <param name="connection">Die Verbindung mit der Datenbank.</param>
        /// <param name="transaction">Die zu verwendende Transaktion.</param>
        /// <param name="objects">Die in die Tabelle hinzufügende Objekten.</param>
        /// <returns>Wie viele Objekte der angegebene Tabelle hinzugefügt worden sind.</returns>
        /// <remarks>Am Ende des Aufrufs ist die Transaktion immer noch offen, deswegen muss sie geschlossen werden, bevor die zu speichernde Objekte sicherlich zur Verfügung stehen.</remarks>
        Task<int> InsertAsync(string tableName,
                              IDbConnection connection,
                              IDbTransaction transaction,
                              IEnumerable<DataType> objects);
    }
}
