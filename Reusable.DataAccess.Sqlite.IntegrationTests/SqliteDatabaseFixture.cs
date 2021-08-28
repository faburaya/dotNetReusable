using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

using Dapper;
using Xunit;

namespace Reusable.DataAccess.Sqlite.IntegrationTests
{
    internal class SqliteDatabaseFixture : IDisposable
    {
        public IDbConnection Connection { get; }

        public SqliteDatabaseFixture()
        {
            Connection = new SqliteDatabaseCreationHelper().OpenOrCreateDatabase("sqlite-db.dat");
            Assert.NotNull(Connection);
            Connection.Open();
            DropDatabase(Connection);
        }

        private static IEnumerable<SqliteSchema> QueryDatabaseObjects(IDbConnection connection)
        {
            return connection.Query<SqliteSchema>("select type as Type, name as EntityName, tbl_name as TableName, sql as CreateStatement from sqlite_schema;");
        }

        public SqliteSchema ReadActualTableSchemaFromDatabase(string tableName)
        {
            var databaseObjects = QueryDatabaseObjects(Connection);
            Assert.NotEmpty(databaseObjects);

            SqliteSchema actualTable = (
                from x in databaseObjects
                where x.Type.ToLower() == "table"
                    && x.EntityName.ToLower() == tableName.ToLower()
                select x
            ).FirstOrDefault();

            return actualTable;
        }

        public SqliteSchema ReadIndexSchemaFromDatabase(string tableName, string columnName)
        {
            var databaseObjects = QueryDatabaseObjects(Connection);
            Assert.NotEmpty(databaseObjects);

            SqliteSchema actualTable = (
                from x in databaseObjects
                where x.Type.ToLower() == "index"
                    && x.TableName.ToLower() == tableName.ToLower()
                    // die Reihen mit "sqlite_autoindex_*" weisen keine Anweisung auf
                    && (x.CreateStatement ?? "").ToLower().Contains(columnName.ToLower())
                select x
            ).FirstOrDefault();

            return actualTable;
        }

        private static void DropDatabase(IDbConnection connection)
        {
            using IDbTransaction transaction = connection.BeginTransaction(IsolationLevel.Serializable);

            var databaseObjects = QueryDatabaseObjects(connection);
            var tables = (from x in databaseObjects where x.Type.ToLower() == "table" select x.EntityName);
            foreach (string table in tables)
            {
                connection.Execute($"drop table {table};");
            }

            transaction.Commit();
        }

        #region Entsorgung

        private bool _disposed = false;

        private void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing && Connection != null)
            {
                Connection.Close();
                Connection.Dispose();
            }

            _disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~SqliteDatabaseFixture() => Dispose(false);

        #endregion

    }// end of class SqliteDatabaseFixture

}// end of namespace Reusable.DataAccess.Sqlite.IntegrationTests
