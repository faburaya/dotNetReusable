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
        /// <summary>
        /// Wie die Werte des Index zu sortieren sind.
        /// </summary>
        public ValueSortingOrder SortingOrder { get; set; }

        /// <summary>
        /// Zusätzliche SQL-Klausen, die für die Erzeugung des Indexes
        /// durch "CREATE INDEX" verwendet werden sollen.
        /// </summary>
        public string AdditionalSqlClauses { get; set; }

        /// <summary>
        /// Legt die vorgegebenen Werte fest.
        /// </summary>
        public RdbTableIndexAttribute()
        {
            SortingOrder = ValueSortingOrder.Ascending;
            AdditionalSqlClauses = "";
        }
    }
}
