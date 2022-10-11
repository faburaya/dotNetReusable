using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Dapper;
using Xunit;

using Reusable.DataModels;
using System.Transactions;
using System.Data;

namespace Reusable.DataAccess.Sqlite.IntegrationTests
{
    public class SqliteDatabaseCreationHelperTest : IDisposable
    {
        private static string TableName => "MeineTabelle";

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
            SqliteDatabaseCreationHelper<MySimpleClass> helper = new();
            Assert.True(helper.CreateTableIfNotExistentAsync(TableName, Fixture.Connection).Result);
            CheckTableCreationStatement<MySimpleClass>(new[] { "Id integer" });
        }

        [Fact]
        public void CreateTableIfNotExistent_WhenTableAlreadyExists()
        {
            Fixture.Connection.Execute($"create table {TableName} (dummy_column integer)");
            SqliteDatabaseCreationHelper<MySimpleClass> helper = new();
            Assert.False(helper.CreateTableIfNotExistentAsync(TableName, Fixture.Connection).Result);
            Assert.NotNull(Fixture.ReadActualTableSchemaFromDatabase(TableName));
        }

        [Fact]
        public void CreateTableIfNotExistent_WhenManyTypes_ThenTableHasCorrespondingSqlTypes()
        {
            SqliteDatabaseCreationHelper<Person> helper = new();
            Assert.True(helper.CreateTableIfNotExistentAsync(TableName, Fixture.Connection).Result);

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
            SqliteDatabaseCreationHelper<MyClassWithPrimaryKey> helper = new();
            Assert.True(helper.CreateTableIfNotExistentAsync(TableName, Fixture.Connection).Result);
            CheckTableCreationStatement<MyClassWithPrimaryKey>(
                new[] { "Id integer not null primary key asc" });
        }

        [Fact]
        public void CreateTableIfNotExistent_WhenTypeHasIndexOrderAsc()
        {
            SqliteDatabaseCreationHelper<MyClassWithIndexAsc> helper = new();
            Assert.True(helper.CreateTableIfNotExistentAsync(TableName, Fixture.Connection).Result);
            CheckIndexCreationStatement<MyClassWithIndexAsc>("Id");
        }

        [Fact]
        public void CreateTableIfNotExistent_WhenTypeHasIndexOrderDesc()
        {
            SqliteDatabaseCreationHelper<MyClassWithIndexDesc> helper = new();
            Assert.True(helper.CreateTableIfNotExistentAsync(TableName, Fixture.Connection).Result);
            CheckIndexCreationStatement<MyClassWithIndexDesc>("Id");
        }

        [Fact]
        public void CreateTableIfNotExistent_WhenTypeHasIndexWithContraint()
        {
            SqliteDatabaseCreationHelper<MyClassWithContrainedIndex> helper = new();
            Assert.True(helper.CreateTableIfNotExistentAsync(TableName, Fixture.Connection).Result);
            CheckIndexCreationStatement<MyClassWithContrainedIndex>("Id");
        }

        [Fact]
        public void CreateTableIfNotExistent_WhenTypeHasPrimaryKeyAndIndex()
        {
            SqliteDatabaseCreationHelper<MyClassWithPrimaryKeyAndIndex> helper = new();
            Assert.True(helper.CreateTableIfNotExistentAsync(TableName, Fixture.Connection).Result);
            CheckTableCreationStatement<MyClassWithPrimaryKeyAndIndex>(
                new[] { "Id integer not null primary key desc" });
            CheckIndexCreationStatement<MyClassWithPrimaryKeyAndIndex>("Number");
        }

        [Fact]
        public void CreateTableIfNotExistent_WhenManyPrimaryKeys_ThenThrow()
        {
            SqliteDatabaseCreationHelper<MyClassWithTwoPrimaryKeys> helper = new();
            Assert.ThrowsAsync<Common.OrmException>(() =>
                helper.CreateTableIfNotExistentAsync(TableName, Fixture.Connection)
            );
        }

        [Fact]
        public void CreateTableIfNotExistent_WhenPrimaryKeyAlsoIndex_ThenThrow()
        {
            SqliteDatabaseCreationHelper<MyClassWithConflictingAttributes> helper = new();
            Assert.ThrowsAsync<Common.OrmException>(() =>
                helper.CreateTableIfNotExistentAsync(TableName, Fixture.Connection)
            );
        }

        [Fact]
        public void Insert_WhenEmptyList_ThenDoNothing()
        {
            SqliteDatabaseCreationHelper<Person> helper = new();
            Assert.True(helper.CreateTableIfNotExistentAsync(TableName, Fixture.Connection).Result);
            int rowCount = helper.InsertAsync(TableName, Fixture.Connection, Array.Empty<Person>()).Result;
            Assert.Equal(0, rowCount);
            int actualRowCount = Fixture.Connection.Query<int>($"select count(1) from {TableName}").First();
            Assert.Equal(0, actualRowCount);
        }

        private static readonly Person[] _expectedPeople = new[] {
            new Person(1, "Paloma Farah", 'F', DateTime.Now, true, 1.65f, Guid.NewGuid(), null),
            new Person(1, "Andressa Rabah", 'F', DateTime.Now, true, 1.62f, Guid.NewGuid(), null),
            new Person(1, "Larissa Barbosa", 'F', DateTime.Now, true, 1.60f, Guid.NewGuid(), null),
        };

        [Fact]
        public void Insert_WhenObjectsAvailable_ThenInsertThem()
        {
            SqliteDatabaseCreationHelper<Person> helper = new();
            Assert.True(helper.CreateTableIfNotExistentAsync(TableName, Fixture.Connection).Result);
            int rowCount = helper.InsertAsync(TableName, Fixture.Connection, _expectedPeople).Result;
            Person[] actualPeople = Fixture.Connection.Query<Person>($"select * from {TableName}").ToArray();
            Array.Sort(_expectedPeople, (a, b) => a.Name.CompareTo(b.Name));
            Array.Sort(actualPeople, (a, b) => a.Name.CompareTo(b.Name));
            Assert.Equal(actualPeople, _expectedPeople);
            Assert.Equal(actualPeople.Length, rowCount);
        }

        [Fact]
        public void Insert_WhenTransactionProvided_ThenInsertWithinTransaction()
        {
            SqliteDatabaseCreationHelper<Person> helper = new();
            Assert.True(helper.CreateTableIfNotExistentAsync(TableName, Fixture.Connection).Result);

            using IDbTransaction transaction = Fixture.Connection.BeginTransaction();
            int rowCount = 0;
            Person[] somePeople = _expectedPeople.Take(_expectedPeople.Length / 2).ToArray();
            rowCount += helper.InsertAsync(TableName, Fixture.Connection, transaction, somePeople).Result;
            Person[] morePeople = _expectedPeople.Skip(somePeople.Length).ToArray();
            rowCount += helper.InsertAsync(TableName, Fixture.Connection, transaction, morePeople).Result;
            transaction.Commit();

            Person[] actualPeople = Fixture.Connection.Query<Person>($"select * from {TableName}").ToArray();
            Array.Sort(_expectedPeople, (a, b) => a.Name.CompareTo(b.Name));
            Array.Sort(actualPeople, (a, b) => a.Name.CompareTo(b.Name));
            Assert.Equal(actualPeople, _expectedPeople);
            Assert.Equal(actualPeople.Length, rowCount);
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

    record struct Person(
        int Id,
        string Name,
        char Gender,
        DateTime BirthDate,
        bool IsAlive,
        float Height,
        Guid UniversalId,
        byte[] Picture);

    #endregion

}// end of namespace Reusable.DataAccess.Sqlite.UnitTests
