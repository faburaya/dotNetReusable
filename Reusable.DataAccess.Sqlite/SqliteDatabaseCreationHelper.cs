using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

using Dapper;

using Microsoft.Data.Sqlite;

namespace Reusable.DataAccess.Sqlite;

/// <inheritdoc cref="Common.IDatabaseCreationHelper"/>
public class SqliteDatabaseCreationHelper : Common.IDatabaseCreationHelper
{
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

        var statements =
            SqliteStatementGenerator.GenerateStatementsToCreateSchema(tableName, typeof(DataType));

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
        string statement = SqliteStatementGenerator.GenerateInsertStatement(tableName, typeof(DataType));
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
        string statement = SqliteStatementGenerator.GenerateInsertStatement(tableName, typeof(DataType));
        await connection.ExecuteAsync(statement, objects, transaction);
    }

}// end of class SqliteDatabaseCreationHelper

