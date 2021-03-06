﻿using System;

namespace Reusable.DataModels
{
    /// <summary>
    /// Attribute für ein Property, das als Partitions Schlüssel in der Datenbank dient.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class CosmosPartitionKeyAttribute : Attribute
    {
        public bool IsDatabasePartitionKey => true;

    }
}
