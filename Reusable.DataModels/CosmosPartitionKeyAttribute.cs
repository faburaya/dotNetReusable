using System;

namespace Reusable.DataModels
{
    /// <summary>
    /// Attribute für ein Property, das als Partitionsschlüssel in der Datenbank dient.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class CosmosPartitionKeyAttribute : Attribute
    {
        /// <summary>
        /// Weist auf, dass das Property den Partitionsschlüsselwert gibt.
        /// </summary>
        public bool IsDatabasePartitionKey => true;

    }
}
