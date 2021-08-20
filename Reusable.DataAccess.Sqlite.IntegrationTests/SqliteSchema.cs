
namespace Reusable.DataAccess.Sqlite.IntegrationTests
{
    /// <summary>
    /// Entspricht dem Schema aus https://sqlite.org/faq.html#q7.
    /// </summary>
    internal class SqliteSchema
    {
        public string Type { get; set; }

        public string EntityName { get; set; }

        public string TableName { get; set; }

        public string CreateStatement { get; set; }
    }
}
