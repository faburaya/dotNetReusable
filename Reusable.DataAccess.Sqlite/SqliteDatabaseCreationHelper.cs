using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using Dapper;
using Microsoft.Data.Sqlite;
using Reusable.DataModels;

namespace Reusable.DataAccess.Sqlite;

#region Type handlers for Dapper

abstract class SqliteTypeHandler<T> : SqlMapper.TypeHandler<T>
{
    // Parameters are converted by Microsoft.Data.Sqlite
    public override void SetValue(IDbDataParameter parameter, T value)
        => parameter.Value = value;
}

class DateTimeOffsetHandler : SqliteTypeHandler<DateTimeOffset>
{
    public override DateTimeOffset Parse(object value)
        => DateTimeOffset.Parse((string)value);
}

class GuidHandler : SqliteTypeHandler<Guid>
{
    public override Guid Parse(object value)
        => Guid.Parse((string)value);
}

class TimeSpanHandler : SqliteTypeHandler<TimeSpan>
{
    public override TimeSpan Parse(object value)
        => TimeSpan.Parse((string)value);
}

#endregion

/// <inheritdoc cref="Common.IDatabaseCreationHelper"/>
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

        SqlMapper.AddTypeHandler(new DateTimeOffsetHandler());
        SqlMapper.AddTypeHandler(new GuidHandler());
        SqlMapper.AddTypeHandler(new TimeSpanHandler());
    }

    /// <inheritdoc/>
    /// <param name="databaseFilePath">Das Pfad der Datei, welche die Datenbank enthält.</param>
    public IDbConnection OpenOrCreateDatabase(string databaseFilePath)
    {
        string connectionString = new SqliteConnectionStringBuilder()
        {
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared,
            DataSource = databaseFilePath
        }.ToString();

        return new SqliteConnection(connectionString);
    }

    /// <inheritdoc/>
    public async Task<bool> CreateTableIfNotExistentAsync<DataType>(string tableName,
                                                                    IDbConnection connection)
    {
        int prevTableCount = (
            await connection.QueryAsync<int>(
                $"select count(1) from sqlite_master WHERE type='table' AND name='{tableName}'")
        ).First();
        Debug.Assert(prevTableCount <= 1);

        GenerateStatementsToCreateSchema(tableName, typeof(DataType), out IList<string> statements);

        using IDbTransaction exclusiveTransaction =
            connection.BeginTransaction(IsolationLevel.Serializable);

        foreach (string statement in statements)
        {
            await connection.ExecuteAsync(statement, transaction: exclusiveTransaction);
        }

        exclusiveTransaction.Commit();
        return prevTableCount == 0;
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

    /// <summary>
    /// Erzeugt eine SQL-Anweisung zum Ausfüllen einer Tabelle.
    /// </summary>
    /// <param name="tableName">Der vorgegebene Name der Tabelle.</param>
    /// <param name="objectType">Der Datentyp, dessen Objekte in der Tabelle gespeichert werden.</param>
    /// <returns>Eine INSERT-Anweisungen, deren Spalten den Properties des Datentyp <paramref name="objectType"/> entsprechen.</returns>
    private static string GenerateInsertStatement(string tableName, Type objectType)
    {
        PropertyInfo[] allPublicProperties = objectType.GetProperties();
        string[] columnsNames =
            (from propertyInfo in allPublicProperties select propertyInfo.Name).ToArray();

        StringBuilder buffer = new($"insert into {tableName} (");
        for (int idx = 0; idx < columnsNames.Length; ++idx)
        {
            buffer.Append(columnsNames[idx]);
            buffer.Append((idx < columnsNames.Length - 1) ? ',' : ')');
        }

        buffer.Append(" values (");
        for (int idx = 0; idx < columnsNames.Length; ++idx)
        {
            buffer.Append('@');
            buffer.Append(columnsNames[idx]);
            buffer.Append((idx < columnsNames.Length - 1) ? ',' : ')');
        }

        return buffer.ToString();
    }

    /// <summary>
    /// Fügt einer Tabelle neue Reihe hinzu.
    /// </summary>
    /// <typeparam name="DataType">Der zu speichernde Datentyp.</typeparam>
    /// <param name="tableName">Der vorgegebene Name der Tabelle.</param>
    /// <param name="connection">Die Verbindung mit der Datenbank.</param>
    /// <param name="objects">Die in die Tabelle hinzufügende Objekte.</param>
    /// <remarks>Am Ende des Aufrufs sind die Objekte gespeichert.</remarks>
    public async Task InsertAsync<DataType>(string tableName,
                                            IDbConnection connection,
                                            IEnumerable<DataType> objects)
    {
        string statement = GenerateInsertStatement(tableName, typeof(DataType));
        IDbTransaction transaction = connection.BeginTransaction(IsolationLevel.ReadUncommitted);
        await connection.ExecuteAsync(statement, objects, transaction);
        transaction.Commit();
    }

    /// <summary>
    /// Fügt einer Tabelle neue Reihe hinzu.
    /// </summary>
    /// <typeparam name="DataType">Der zu speichernde Datentyp.</typeparam>
    /// <param name="tableName">Der vorgegebene Name der Tabelle.</param>
    /// <param name="connection">Die Verbindung mit der Datenbank.</param>
    /// <param name="transaction">Die zu verwendende Transaktion.</param>
    /// <param name="objects">Die in die Tabelle hinzufügende Objekten.</param>
    /// <remarks>Am Ende des Aufrufs ist die Transaktion immer noch offen, deswegen muss sie geschlossen werden, bevor die zu speichernde Objekte sicherlich zur Verfügung stehen.</remarks>
    public async Task InsertAsync<DataType>(string tableName,
                                            IDbConnection connection,
                                            IDbTransaction transaction,
                                            IEnumerable<DataType> objects)
    {
        string statement = GenerateInsertStatement(tableName, typeof(DataType));
        await connection.ExecuteAsync(statement, objects, transaction);
    }

}// end of class SqliteDatabaseCreationHelper

