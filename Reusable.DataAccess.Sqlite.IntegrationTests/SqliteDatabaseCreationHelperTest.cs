using System;

using Dapper;
using Xunit;

using Reusable.DataModels;

namespace Reusable.DataAccess.Sqlite.IntegrationTests
{
    public class SqliteDatabaseCreationHelperTest : IDisposable
    {
        private string TableName => "MeineTabelle";

        private SqliteDatabaseFixture Fixture { get; }

        #region Einrichtung und Entsorgung des Fixtures

        public SqliteDatabaseCreationHelperTest()
        {
            Fixture = new SqliteDatabaseFixture();
        }

        private bool _disposed = false;

        private void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
                Fixture.Dispose();

            _disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        ~SqliteDatabaseCreationHelperTest() => Dispose(false);

        #endregion

        [Fact]
        public void CreateTableIfNotExistent_WhenSimplestType()
        {
            var helper = new SqliteDatabaseCreationHelper();
            helper.CreateTableIfNotExistentAsync<MySimpleClass>(TableName, Fixture.Connection).Wait();

            SqliteSchema actualTable = Fixture.ReadActualTableSchemaFromDatabase(TableName);
            Assert.NotNull(actualTable);

            CheckTableCreationStatement(TableName, actualTable.CreateStatement, new[] { "Id integer" });
        }

        [Fact]
        public void CreateTableIfNotExistent_WhenTableAlreadyExists()
        {
            Fixture.Connection.Execute($"create table {TableName} (dummy_column integer)");
            var helper = new SqliteDatabaseCreationHelper();
            helper.CreateTableIfNotExistentAsync<MySimpleClass>(TableName, Fixture.Connection).Wait();
            Assert.NotNull(Fixture.ReadActualTableSchemaFromDatabase(TableName));
        }

        [Fact]
        public void CreateTableIfNotExistent_WhenManyTypes_ThenTableHasCorrespondingSqlTypes()
        {
            var helper = new SqliteDatabaseCreationHelper();
            helper.CreateTableIfNotExistentAsync<Person>(TableName, Fixture.Connection).Wait();

            SqliteSchema actualTable = Fixture.ReadActualTableSchemaFromDatabase(TableName);
            Assert.NotNull(actualTable);

            CheckTableCreationStatement(TableName, actualTable.CreateStatement, new[] {
                "Id integer",
                "Name text",
                "Gender text",
                "BirthDate text",
                "IsAlive integer",
                "Height real",
                "UniversalId text",
                "Picture blob"
            });
        }

        [Fact]
        public void CreateTableIfNotExistent_WhenTypeHasPrimaryKey()
        {
            var helper = new SqliteDatabaseCreationHelper();
            helper.CreateTableIfNotExistentAsync<MyClassWithPrimaryKey>(TableName, Fixture.Connection).Wait();

            SqliteSchema actualTable = Fixture.ReadActualTableSchemaFromDatabase(TableName);
            Assert.NotNull(actualTable);

            CheckTableCreationStatement(TableName,
                                        actualTable.CreateStatement,
                                        new[] { "Id integer not null primary key" });
        }

        [Fact]
        public void CreateTableIfNotExistent_WhenTypeHasIndex()
        {
            var helper = new SqliteDatabaseCreationHelper();
            helper.CreateTableIfNotExistentAsync<MyClassWithIndex>(TableName, Fixture.Connection).Wait();
            Assert.NotNull(Fixture.ReadIndexSchemaFromDatabase(TableName, "Id"));
        }

        [Fact]
        public void CreateTableIfNotExistent_WhenTypeHasPrimaryKeyAndIndex()
        {
            var helper = new SqliteDatabaseCreationHelper();
            helper.CreateTableIfNotExistentAsync<MyClassWithPrimaryKeyAndIndex>(TableName, Fixture.Connection).Wait();

            Assert.NotNull(Fixture.ReadIndexSchemaFromDatabase(TableName, "Number"));

            SqliteSchema actualTable = Fixture.ReadActualTableSchemaFromDatabase(TableName);
            Assert.NotNull(actualTable);

            CheckTableCreationStatement(TableName,
                                        actualTable.CreateStatement,
                                        new[] { "Id integer not null primary key" });
        }

        [Fact]
        public void CreateTableIfNotExistent_WhenManyPrimaryKeys_ThenThrow()
        {
            var helper = new SqliteDatabaseCreationHelper();

            Assert.ThrowsAsync<Common.OrmException>(() =>
                helper.CreateTableIfNotExistentAsync<MyClassWithTwoPrimaryKeys>(
                    TableName, Fixture.Connection)
            );
        }

        [Fact]
        public void CreateTableIfNotExistent_WhenPrimaryKeyAlsoIndex_ThenThrow()
        {
            var helper = new SqliteDatabaseCreationHelper();

            Assert.ThrowsAsync<Common.OrmException>(() =>
                helper.CreateTableIfNotExistentAsync<MyClassWithConflictingAttributes>(
                    TableName, Fixture.Connection)
            );
        }

        private static void CheckTableCreationStatement(string tableName,
                                                        string statement,
                                                        string[] columnDefinitions)
        {
            statement = statement.ToLower();

            Assert.False(string.IsNullOrWhiteSpace(statement));
            Assert.StartsWith("create table", statement);
            Assert.Contains(tableName.ToLower(), statement);

            foreach (string column in columnDefinitions)
            {
                Assert.Contains(column.ToLower(), statement);
            }
        }

    }// end of class SqliteDatabaseCreationHelperTest

    #region invalid types

    internal class MyClassWithTwoPrimaryKeys
    {
        [RdbTablePrimaryKey]
        public int Id { get; }

        [RdbTablePrimaryKey]
        public string Name { get; }
    }

    internal class MyClassWithConflictingAttributes
    {
        [RdbTablePrimaryKey]
        [RdbTableIndex]
        public int Id { get; }
    }

    #endregion

    #region valid types

    internal class MySimpleClass
    {
        public int Id { get; set; }
    }

    internal class MyClassWithPrimaryKey
    {
        [RdbTablePrimaryKey]
        public int Id { get; set; }
    }

    internal class MyClassWithIndex
    {
        [RdbTableIndex]
        public int Id { get; set; }
    }

    internal class MyClassWithPrimaryKeyAndIndex
    {
        [RdbTablePrimaryKey]
        public int Id { get; set; }

        [RdbTableIndex]
        public int Number { get; set; }
    }

    internal class Person
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public char Gender { get; set; }

        public DateTime BirthDate { get; set; }

        public bool IsAlive { get; set; }

        public float Height { get; set; }

        public Guid UniversalId { get; set; }

        public byte[] Picture { get; set; }
    }

    #endregion

}// end of namespace Reusable.DataAccess.Sqlite.UnitTests
