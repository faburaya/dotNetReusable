using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

using Dapper;

using Microsoft.Data.Sqlite;

namespace Reusable.DataAccess.Sqlite;

/// <inheritdoc cref="Common.IDatabaseCreationHelper{DataType}"/>
public class SqliteDatabaseCreationHelper<DataType> : Common.IDatabaseCreationHelper<DataType>
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

    private static IReadOnlyCollection<string> _schemaCreationStatements = null;

    /// <inheritdoc/>
    public async Task<bool> CreateTableIfNotExistentAsync(string tableName, IDbConnection connection)
    {
        int prevTableCount = (
            await connection.QueryAsync<int>(
                $"select count(1) from sqlite_master WHERE type='table' AND name='{tableName}'")
        ).First();
        Debug.Assert(prevTableCount <= 1);

        _schemaCreationStatements ??=
            SqliteStatementGenerator.GenerateStatementsToCreateSchema(tableName, typeof(DataType));

        using IDbTransaction exclusiveTransaction =
            connection.BeginTransaction(IsolationLevel.Serializable);

        foreach (string statement in _schemaCreationStatements)
        {
            await connection.ExecuteAsync(statement, transaction: exclusiveTransaction);
        }

        exclusiveTransaction.Commit();
        return prevTableCount == 0;
    }

    private static string _insertStatement = null;

    /// <inheritdoc/>
    public async Task<int> InsertAsync(string tableName,
                                       IDbConnection connection,
                                       IEnumerable<DataType> objects)
    {
        _insertStatement ??= SqliteStatementGenerator.GenerateInsertStatement(tableName, typeof(DataType));
        IDbTransaction transaction = connection.BeginTransaction(IsolationLevel.ReadUncommitted);
        int rowCount = await connection.ExecuteAsync(_insertStatement, objects, transaction);
        transaction.Commit();
        return rowCount;
    }

    /// <inheritdoc/>
    public async Task<int> InsertAsync(string tableName,
                                       IDbConnection connection,
                                       IDbTransaction transaction,
                                       IEnumerable<DataType> objects)
    {
        _insertStatement ??= SqliteStatementGenerator.GenerateInsertStatement(tableName, typeof(DataType));
        int rowCount = await connection.ExecuteAsync(_insertStatement, objects, transaction);
        return rowCount;
    }

}// end of class SqliteDatabaseCreationHelper

