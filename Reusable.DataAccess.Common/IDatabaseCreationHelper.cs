using System.Data;
using System.Threading.Tasks;

namespace Reusable.DataAccess.Common
{
    /// <summary>
    /// Hilft beim der Einrichung einer neuen Datenbank.
    /// </summary>
    public interface IDatabaseCreationHelper
    {
        /// <summary>
        /// Wenn nicht vorhanden, erstellt eine Tabelle zur Speicherung eines gegebenen Datentyps.
        /// </summary>
        /// <typeparam name="DataType">Der zu speichernde Datentyp.</typeparam>
        /// <param name="tableName">Der Name der Tabelle.</param>
        /// <param name="connection">Die Verbindung mit der Datenbank.</param>
        Task CreateTableIfNotExistentAsync<DataType>(string tableName, IDbConnection connection);
    }
}
