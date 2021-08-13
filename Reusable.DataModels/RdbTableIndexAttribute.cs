using System;

namespace Reusable.DataModels
{
    /// <summary>
    /// Legt fest, dass die Werte der hingewiesenen Eigenschaft der Klasse für die
    /// Erzeugung eines Indexes verwendet werden, wenn die Instanzen dieser Klasse
    /// in einer Tabelle einer relationalen Datenbank gespeichert werden.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class RdbTableIndexAttribute : Attribute
    {
        public ValueSortingOrder SortingOrder { get; }

        /// <summary>
        /// Zusätzliche SQL-Klausen, die für die Erzeugung des Indexes
        /// durch "CREATE INDEX" verwendet werden sollen.
        /// </summary>
        public string AdditionalSqlClauses { get; }
    }
}
