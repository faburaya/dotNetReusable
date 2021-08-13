using System;

namespace Reusable.DataModels
{
    /// <summary>
    /// Dieses Attribut beschreibt das Container in der Azure Cosmos Datenbank,
    /// in dem eine Klasse gespeichert werden soll.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public class CosmosContainerAttribute : Attribute
    {
        public string Name { get; set; }
    }
}
