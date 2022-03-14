using System;

namespace Reusable.DataModels
{
    /// <summary>
    /// Dieses Attribut beschreibt das Feld, mit dem ein Property in Verbindung steht.
    /// Dieses Feld darf zu einem Formular, zu einer Tabelle einer Datenbank oder Ähnliches gehören.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class FieldAttribute : Attribute
    {
        /// <summary>
        /// Der Name des Felds.
        /// </summary>
        public string Name { get; set; }
    }
}
