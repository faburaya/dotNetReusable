using Xunit;

namespace Reusable.DataAccess.Sqlite.IntegrationTests
{
    [CollectionDefinition("IntegrationTests")]
    public class MyFixturesCollection
        : ICollectionFixture<SqliteDatabaseFixture>
    {
    }
}
