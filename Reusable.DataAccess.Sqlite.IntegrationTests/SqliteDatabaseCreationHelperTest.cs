using System;
using System.Linq;
using System.Reflection;

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
            CheckTableCreationStatement<MySimpleClass>(new[] { "Id integer" });
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

            CheckTableCreationStatement<Person>(new[] {
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
            CheckTableCreationStatement<MyClassWithPrimaryKey>(new[] { "Id integer not null primary key asc" });
        }

        [Fact]
        public void CreateTableIfNotExistent_WhenTypeHasIndexOrderAsc()
        {
            var helper = new SqliteDatabaseCreationHelper();
            helper.CreateTableIfNotExistentAsync<MyClassWithIndexAsc>(TableName, Fixture.Connection).Wait();
            CheckIndexCreationStatement<MyClassWithIndexAsc>("Id");
        }

        [Fact]
        public void CreateTableIfNotExistent_WhenTypeHasIndexOrderDesc()
        {
            var helper = new SqliteDatabaseCreationHelper();
            helper.CreateTableIfNotExistentAsync<MyClassWithIndexDesc>(TableName, Fixture.Connection).Wait();
            CheckIndexCreationStatement<MyClassWithIndexDesc>("Id");
        }

        [Fact]
        public void CreateTableIfNotExistent_WhenTypeHasIndexWithContraint()
        {
            var helper = new SqliteDatabaseCreationHelper();
            helper.CreateTableIfNotExistentAsync<MyClassWithContrainedIndex>(TableName, Fixture.Connection).Wait();
            CheckIndexCreationStatement<MyClassWithContrainedIndex>("Id");
        }

        [Fact]
        public void CreateTableIfNotExistent_WhenTypeHasPrimaryKeyAndIndex()
        {
            var helper = new SqliteDatabaseCreationHelper();
            helper.CreateTableIfNotExistentAsync<MyClassWithPrimaryKeyAndIndex>(TableName, Fixture.Connection).Wait();
            CheckTableCreationStatement<MyClassWithPrimaryKeyAndIndex>(new[] { "Id integer not null primary key desc" });
            CheckIndexCreationStatement<MyClassWithPrimaryKeyAndIndex>("Number");
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

        private void CheckTableCreationStatement<DataType>(string[] columnDefinitions)
        {
            SqliteSchema actualTable = Fixture.ReadActualTableSchemaFromDatabase(TableName);
            Assert.NotNull(actualTable);

            string statement = actualTable.CreateStatement.ToLower();
            Assert.False(string.IsNullOrWhiteSpace(statement));

            foreach (string column in columnDefinitions)
            {
                Assert.Contains(column.ToLower(), statement);
            }
        }

        private void CheckIndexCreationStatement<DataType>(string columnName)
        {
            SqliteSchema actualIndex = Fixture.ReadIndexSchemaFromDatabase(TableName, columnName);
            Assert.NotNull(actualIndex);

            string statement = actualIndex.CreateStatement.ToLower();
            Assert.False(string.IsNullOrWhiteSpace(statement));

            var attribute = GetAttributeFromProperty<RdbTableIndexAttribute>(columnName, typeof(DataType));
            Assert.NotNull(attribute);
            Assert.Contains(attribute.AdditionalSqlClauses, statement);

            switch (attribute.SortingOrder)
            {
                case ValueSortingOrder.Ascending:
                    Assert.Contains("asc", statement);
                    break;
                case ValueSortingOrder.Descending:
                    Assert.Contains("desc", statement);
                    break;
            }
        }

        private static AttributeType GetAttributeFromProperty<AttributeType>(
            string propertyName, Type dataType) where AttributeType : Attribute
        {
            PropertyInfo propertyInfo = (from property in dataType.GetProperties()
                                         where property.Name == propertyName
                                         select property).FirstOrDefault();
            Assert.NotNull(propertyInfo);
            return propertyInfo.GetCustomAttribute<AttributeType>();
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

    class MySimpleClass
    {
        public int Id { get; set; }
    }

    class MyClassWithPrimaryKey
    {
        [RdbTablePrimaryKey]
        public int Id { get; set; }
    }

    class MyClassWithIndexAsc
    {
        [RdbTableIndex(SortingOrder = ValueSortingOrder.Ascending)]
        public int Id { get; set; }
    }

    class MyClassWithIndexDesc
    {
        [RdbTableIndex(SortingOrder = ValueSortingOrder.Descending)]
        public int Id { get; set; }
    }

    class MyClassWithContrainedIndex
    {
        [RdbTableIndex(AdditionalSqlClauses = "where id < 100")]
        public int Id { get; set; }
    }

    class MyClassWithPrimaryKeyAndIndex
    {
        [RdbTablePrimaryKey(SortingOrder = ValueSortingOrder.Descending)]
        public int Id { get; set; }

        [RdbTableIndex]
        public int Number { get; set; }
    }

    class Person
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
