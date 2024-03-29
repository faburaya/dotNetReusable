﻿using System;

namespace Reusable.DataModels
{
    /// <summary>
    /// Legt fest, dass die hingewiesene Eigenschaft der Klasse als Primärschlüssel gilt,
    /// wenn die Instanzen der Klasse in einer Tabelle einer relationalen Datenbank gespeichert werden.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class RdbTablePrimaryKeyAttribute : Attribute
    {
        /// <summary>
        /// Wie die Werte zu sortieren sind.
        /// </summary>
        public ValueSortingOrder SortingOrder { get; set; }

        /// <summary>
        ///  Legt die vorgegebenen Werten fest.
        /// </summary>
        public RdbTablePrimaryKeyAttribute()
        {
            SortingOrder = ValueSortingOrder.Ascending;
        }
    }
}
