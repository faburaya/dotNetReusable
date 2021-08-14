
namespace Reusable.DataAccess.Sqlite.IntegrationTests
{
    /// <summary>
    /// Entspricht dem Schema aus https://sqlite.org/faq.html#q7.
    /// </summary>
    internal class SqliteSchema
    {
        public string type { get; set; }

        public string name { get; set; }

        public string tbl_name { get; set; }

        public string sql { get; set; }
    }
}
