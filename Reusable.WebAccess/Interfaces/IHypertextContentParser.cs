using System.Collections.Generic;

namespace Reusable.WebAccess
{
    /// <summary>
    /// Schnittstelle für die Zergliderung von Inhalt in Hypertext.
    /// </summary>
    /// <typeparam name="DataType">Der Typ, in dem die zergliederte Daten gespeichert werden sollen.</typeparam>
    public interface IHypertextContentParser<DataType>
    {
        public IEnumerable<DataType> ParseContent(string hypertext);
    }
}
