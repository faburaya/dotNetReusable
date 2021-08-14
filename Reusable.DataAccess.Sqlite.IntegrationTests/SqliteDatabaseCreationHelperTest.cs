using System;
using System.Linq;
using System.Data;

using Dapper;
using Xunit;

using Reusable.DataModels;

namespace Reusable.DataAccess.Sqlite.IntegrationTests
{
    [Collection("IntegrationTests")]
    public class SqliteDatabaseCreationHelperTest
    {
        private SqliteDatabaseFixture Fixture { get; }

        public SqliteDatabaseCreationHelperTest(SqliteDatabaseFixture testFixture)
        {
            Fixture = testFixture;
        }

        [Fact]
        public void CreateTableIfNotExistent_WhenSimplestType()
        {
            var helper = new SqliteDatabaseCreationHelper();
            helper.CreateTableIfNotExistentAsync<MySimpleClass>(Fixture.TableName, Fixture.Connection).Wait();

            SqliteSchema actualTable =
                ReadActualTableSchemaFromDatabase(Fixture.TableName, Fixture.Connection);

            Assert.NotNull(actualTable);

            CheckTableCreationStatement(Fixture.TableName, actualTable.sql, new[] { "Id integer" });
        }

        [Fact]
        public void CreateTableIfNotExistent_WhenManyTypes_ThenTableHasCorrespondingSqlTypes()
        {
            var helper = new SqliteDatabaseCreationHelper();
            helper.CreateTableIfNotExistentAsync<Person>(Fixture.TableName, Fixture.Connection).Wait();

            SqliteSchema actualTable =
                ReadActualTableSchemaFromDatabase(Fixture.TableName, Fixture.Connection);

            Assert.NotNull(actualTable);

            CheckTableCreationStatement(Fixture.TableName, actualTable.sql, new[] {
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
            helper.CreateTableIfNotExistentAsync<MyClassWithPrimaryKey>(Fixture.TableName, Fixture.Connection).Wait();

            SqliteSchema actualTable =
                ReadActualTableSchemaFromDatabase(Fixture.TableName, Fixture.Connection);

            Assert.NotNull(actualTable);

            CheckTableCreationStatement(Fixture.TableName,
                                        actualTable.sql,
                                        new[] { "Id integer not null primary key" });
        }

        [Fact]
        public void CreateTableIfNotExistent_WhenTypeHasIndex()
        {
            var helper = new SqliteDatabaseCreationHelper();
            helper.CreateTableIfNotExistentAsync<MyClassWithIndex>(Fixture.TableName, Fixture.Connection).Wait();

            SqliteSchema indexSchema =
                ReadIndexSchemaFromDatabase(Fixture.TableName, "Number", Fixture.Connection);

            Assert.NotNull(indexSchema);
        }

        [Fact]
        public void CreateTableIfNotExistent_WhenTypeHasPrimaryKeyAndIndex()
        {
            var helper = new SqliteDatabaseCreationHelper();
            helper.CreateTableIfNotExistentAsync<MyClassWithPrimaryKeyAndIndex>(Fixture.TableName, Fixture.Connection).Wait();

            SqliteSchema indexSchema =
                ReadIndexSchemaFromDatabase(Fixture.TableName, "Number", Fixture.Connection);

            Assert.NotNull(indexSchema);

            SqliteSchema actualTable =
                ReadActualTableSchemaFromDatabase(Fixture.TableName, Fixture.Connection);

            Assert.NotNull(actualTable);

            CheckTableCreationStatement(Fixture.TableName,
                                        actualTable.sql,
                                        new[] { "Id integer not null primary key" });
        }

        [Fact]
        public void CreateTableIfNotExistent_WhenManyPrimaryKeys_ThenThrow()
        {
            var helper = new SqliteDatabaseCreationHelper();

            Assert.ThrowsAsync<ApplicationException>(() =>
                helper.CreateTableIfNotExistentAsync<MyClassWithTwoPrimaryKeys>(
                    Fixture.TableName, Fixture.Connection)
            );
        }

        [Fact]
        public void CreateTableIfNotExistent_WhenPrimaryKeyAlsoIndex_ThenThrow()
        {
            var helper = new SqliteDatabaseCreationHelper();

            Assert.ThrowsAsync<ApplicationException>(() =>
                helper.CreateTableIfNotExistentAsync<MyClassWithConflictingAttributes>(
                    Fixture.TableName, Fixture.Connection)
            );
        }

        private static SqliteSchema ReadActualTableSchemaFromDatabase(string tableName,
                                                                      IDbConnection connection)
        {
            var databaseObjects =
                connection.Query<SqliteSchema>("select type, name, tbl_name, sql from sqlite_schema;");

            Assert.NotEmpty(databaseObjects);

            SqliteSchema actualTable = (
                from x in databaseObjects
                where x.type.ToLower() == "table"
                    && x.name.ToLower() == tableName.ToLower()
                select x
            ).FirstOrDefault();

            return actualTable;
        }

        private static SqliteSchema ReadIndexSchemaFromDatabase(string tableName,
                                                                string columnName,
                                                                IDbConnection connection)
        {
            var databaseObjects =
                connection.Query<SqliteSchema>("select type, name, tbl_name, sql from sqlite_schema;");

            Assert.NotEmpty(databaseObjects);

            SqliteSchema actualTable = (
                from x in databaseObjects
                where x.type.ToLower() == "index"
                    && x.tbl_name.ToLower() == tableName.ToLower()
                    && x.sql.ToLower().Contains(columnName.ToLower())
                select x
            ).FirstOrDefault();

            return actualTable;
        }

        private void CheckTableCreationStatement(string tableName,
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
