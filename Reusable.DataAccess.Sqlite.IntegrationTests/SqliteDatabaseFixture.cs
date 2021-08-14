using System;
using System.Data;

using Dapper;
using Xunit;

namespace Reusable.DataAccess.Sqlite.IntegrationTests
{
    public class SqliteDatabaseFixture : IDisposable
    {
        public string DatabaseFilePath => "sqlite-db.dat";

        public string TableName => "MeineTabelle";

        public IDbConnection Connection { get; }

        public SqliteDatabaseFixture()
        {
            Connection = SqliteDatabaseCreationHelper.OpenOrCreateDatabase(DatabaseFilePath);
            Assert.NotNull(Connection);
            Connection.Open();
        }

        private bool _disposed = false;

        private void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing && Connection != null)
            {
                Connection.Execute($"drop table if exists {TableName};");
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

    }// end of class SqliteDatabaseFixture

}// end of namespace Reusable.DataAccess.Sqlite.IntegrationTests
