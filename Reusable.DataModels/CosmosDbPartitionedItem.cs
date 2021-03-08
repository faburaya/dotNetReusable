using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Newtonsoft.Json;

namespace Reusable.DataModels
{
    /// <summary>
    /// Einrichtung um einen Typ herum, der als Element der Datenbank dient.
    /// </summary>
    public static class CosmosDbPartitionedItem<ItemType> where ItemType : class
    {
        private static readonly PropertyInfo[] serializableProperties;

        /// <summary>
        /// Gibt ein Hash-Code zurück, das berechnet wird, sodass nur die
        /// JSON-serialisierbaren Properties berücksichtigt werden.
        /// </summary>
        public static int CalculateHashOfJsonFor(object obj)
        {
            var item = (ItemType)obj;
            int hashCode = 7;
            foreach (PropertyInfo property in serializableProperties)
            {
                object boxedValue = property.GetValue(item);
                hashCode = 31 * hashCode + (boxedValue != null ? boxedValue.GetHashCode() : 0);
            }
            return hashCode;
        }

        private static readonly PropertyInfo partitionKeyProperty;

        public static string GetPartitionKeyValue(object obj)
        {
            var item = (ItemType)obj;
            return (string)partitionKeyProperty.GetValue(item);
        }

        public static string PartitionKeyPath { get; }

        public static string ContainerName { get; }

        /// <summary>
        /// Initialisiert die Einrichtung.
        /// </summary>
        /// <summary>
        /// Findet den Pfad des Partitionsschlüssels, der von einem Attribut markiert ist,
        /// und dessen Namen den Namen in der JSON-Serialisierung gleicht.
        /// </summary>
        static CosmosDbPartitionedItem()
        {
            var containerAttribute = typeof(ItemType).GetCustomAttribute<CosmosContainerAttribute>();
            if (containerAttribute == null)
            {
                throw new NotSupportedException($"Der Name des Containers für den Datenbank: an dem Typ {typeof(ItemType).Name} fehlt das Attribut [CosmosContainer(Name)]!");
            }

            ContainerName = containerAttribute.Name;

            serializableProperties = (from property in typeof(ItemType).GetProperties()
                                      where property.GetCustomAttribute<JsonPropertyAttribute>() != null
                                      select property).ToArray();

            IEnumerable<PropertyInfo> properties =
                from property in typeof(ItemType).GetProperties()
                where property.GetCustomAttribute<CosmosPartitionKeyAttribute>() != null
                select property;

            if (!properties.Any())
            {
                throw new NotSupportedException($"Der Pfad für den Partitionsschlüssel der Datenbank kann nicht festgestellt werden: an dem Typ {typeof(ItemType).Name} fehlt ein Property mit dem Attributen [CosmosPartitionKey] und [JsonPropertyName]!");
            }

            if (properties.Count() > 1)
            {
                throw new NotSupportedException($"Der Pfad für den Partitionsschlüssel der Datenbank kann nicht festgestellt werden: der Typ {typeof(ItemType).Name} hat mehr als ein Property mit dem Attribut [CosmosPartitionKey]!");
            }

            partitionKeyProperty = properties.First();

            var jsonSerialization = partitionKeyProperty.GetCustomAttribute<JsonPropertyAttribute>();
            if (jsonSerialization != null)
            {
                PartitionKeyPath = $"/{jsonSerialization.PropertyName}";
                return;
            }

            throw new NotSupportedException($"Der Pfad für den Partitionsschlüssel der Datenbank kann nicht festgestellt werden: der Partitionsschlüssel {partitionKeyProperty.Name} fehlt das Attribut [JsonPropertyName].");
        }

    }// end of class CosmosDbItem

}// end of namespace Reusable.DataAccess
