using Xunit;

namespace Reusable.DataAccess.Cosmos.IntegrationTests
{
    [CollectionDefinition("IntegrationTests")]
    public class MyFixturesCollection
        : ICollectionFixture<CosmosDatabaseFixture>
    {
    }
}
