using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using Dapper;
using Microsoft.Data.Sqlite;
using Reusable.DataModels;

namespace Reusable.DataAccess.Sqlite
{
    /// <summary>
    /// Hilft bei der Herstellung einer Verbindung mit einer Datenbank,
    /// die vielleicht noch nicht existiert oder leer ist.
    /// </summary>
    public class SqliteDatabaseCreationHelper : Common.IDatabaseCreationHelper
    {
        private static readonly SortedList<string, string> clrToSqliteType;

        static SqliteDatabaseCreationHelper()
        {
            // Aus https://docs.microsoft.com/de-de/dotnet/standard/data/sqlite/types
            clrToSqliteType = new SortedList<string, string> {
                { typeof(bool).Name, "integer" },
                { typeof(byte).Name, "integer" },
                { typeof(byte[]).Name, "blob" },
                { typeof(char).Name, "text" },
                { typeof(DateTime).Name, "text" },
                { typeof(DateTimeOffset).Name, "text" },
                { typeof(decimal).Name, "text" },
                { typeof(double).Name, "real" },
                { typeof(float).Name, "real" },
                { typeof(Guid).Name, "text" },
                { typeof(Int16).Name, "integer" },
                { typeof(Int32).Name, "integer" },
                { typeof(Int64).Name, "integer" },
                { typeof(sbyte).Name, "integer" },
                { typeof(string).Name, "text" },
                { typeof(TimeSpan).Name, "text" },
                { typeof(UInt16).Name, "integer" },
                { typeof(UInt32).Name, "integer" },
                { typeof(UInt64).Name, "integer" }
            };
        }

        /// <summary>
        /// Stellt eine neue Verbindung mit der Datenbank her.
        /// Wenn die Datenbank nicht existiert, wird er erstellt.
        /// </summary>
        /// <param name="databaseFilePath">Das Pfad der Datei, welche die SQLite Datenbank enthält.</param>
        /// <returns>Eine Verbindung (ADO.NET) mit der Datenbank.</returns>
        public static IDbConnection OpenOrCreateDatabase(string databaseFilePath)
        {
            string connectionString = new SqliteConnectionStringBuilder()
            {
                Mode = SqliteOpenMode.ReadWriteCreate,
                Cache = SqliteCacheMode.Shared,
                DataSource = databaseFilePath
            }.ToString();

            return new SqliteConnection(connectionString);
        }

        /// <summary>
        /// Erstellt eine Tabelle in der Datenbank zur Speicherung des angegeben Datentyp,
        /// falls sie nicht vorhanden ist.
        /// </summary>
        /// <typeparam name="DataType">Der zu speichernde Datentyp.</typeparam>
        /// <param name="tableName">Der Name der Tabelle.</param>
        /// <param name="connection">Die Verbindung mit der Datenbank.</param>
        public async Task CreateTableIfNotExistentAsync<DataType>(string tableName, IDbConnection connection)
        {
            GenerateStatementsToCreateSchema(tableName, typeof(DataType), out IList<string> statements);

            using IDbTransaction exclusiveTransaction =
                connection.BeginTransaction(IsolationLevel.Serializable);

            foreach (string statement in statements)
            {
                await connection.ExecuteAsync(statement, transaction: exclusiveTransaction);
            }

            exclusiveTransaction.Commit();
        }

        /// <summary>
        /// Erzeugt SQL-Anweisungen zum Erstellen der Tabelle und ihre Indices.
        /// </summary>
        /// <param name="tableName">Der vorgegebene Name der Tabelle.</param>
        /// <param name="objectType">Der Datentyp, dessen Objekte in der Tabelle gespeichert werden.</param>
        /// <param name="statements">Die erzeugten SQL-Anweisungen.</param>
        private static void GenerateStatementsToCreateSchema(string tableName,
                                                             Type objectType,
                                                             out IList<string> statements)
        {
            statements = new List<string> { null };

            bool hasAlreadyPrimaryKey = false;
            var columnDefinitions = new StringBuilder();
            PropertyInfo[] allPublicProperties = objectType.GetProperties();
            for (int idx = 0; idx < allPublicProperties.Length; ++idx)
            {
                var propertyInfo = allPublicProperties[idx];
                columnDefinitions.AppendFormat("{0} {1}",
                                             propertyInfo.Name,
                                             clrToSqliteType[propertyInfo.PropertyType.Name]);

                var primaryKeyAttribute = propertyInfo.GetCustomAttribute<RdbTablePrimaryKeyAttribute>();
                if (primaryKeyAttribute != null)
                {
                    if (hasAlreadyPrimaryKey)
                    {
                        throw new Common.OrmException($"Eine Tabelle für den Typ {objectType.Name} darf nicht in Datenbank erstellt werden: die Klasse verfügt über mehrere Primärschlüsseln!");
                    }
                    hasAlreadyPrimaryKey = true;

                    columnDefinitions.AppendFormat(" not null primary key {0}",
                        primaryKeyAttribute.SortingOrder == ValueSortingOrder.Ascending ? "asc" : "desc");
                }

                if (idx + 1 < allPublicProperties.Length)
                {
                    columnDefinitions.Append(", ");
                }

                var tableIndexAttribute = propertyInfo.GetCustomAttribute<RdbTableIndexAttribute>();
                if (tableIndexAttribute != null)
                {
                    if (primaryKeyAttribute != null)
                    {
                        throw new Common.OrmException($"Eine Tabelle für den Typ {objectType.Name} darf nicht in Datenbank erstellt werden: die Klasse verfügt über eine Eigenschaft, die gleichzeitig als Primärschlüssel und Index gilt!");
                    }

                    statements.Add($"create index if not exists {tableName}_{propertyInfo.Name} on {tableName} ({propertyInfo.Name} {(tableIndexAttribute.SortingOrder == ValueSortingOrder.Ascending ? "asc" : "desc")}) {tableIndexAttribute.AdditionalSqlClauses};");
                }
            }

            statements[0] = $"create table if not exists {tableName} ({columnDefinitions});";
        }

    }// end of class SqliteDatabaseCreationHelper

}// end of namespace Reusable.DataAccess.Sqlite
