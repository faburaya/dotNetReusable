using System.Data;
using System.Threading.Tasks;

namespace Reusable.DataAccess.Common
{
    /// <summary>
    /// Hilft bei der Herstellung einer Verbindung mit einer Datenbank,
    /// die vielleicht noch nicht existiert oder leer ist.
    /// </summary>
    public interface IDatabaseCreationHelper
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
        /// <typeparam name="DataType">Der zu speichernde Datentyp.</typeparam>
        /// <param name="tableName">Der Name der Tabelle.</param>
        /// <param name="connection">Die Verbindung mit der Datenbank.</param>
        /// <returns>Ob die Tabelle erstellt wurde.</returns>
        Task<bool> CreateTableIfNotExistentAsync<DataType>(string tableName, IDbConnection connection);
    }
}
