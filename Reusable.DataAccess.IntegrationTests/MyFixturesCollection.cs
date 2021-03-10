using Xunit;

namespace Reusable.DataAccess.IntegrationTests.Fixtures
{
    [CollectionDefinition("IntegrationTests")]
    public class MyFixturesCollection
        : ICollectionFixture<CosmosDatabaseFixture>
    {
    }
}
