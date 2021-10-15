using System.Collections.Generic;

namespace Reusable.WebAccess
{
    /// <summary>
    /// Schnittstelle für die Zergliderung von Inhalt in Hypertext.
    /// </summary>
    /// <typeparam name="DataType">Der Typ, in dem die zergliederte Daten gespeichert werden sollen.</typeparam>
    public interface IHypertextContentParser<out DataType>
    {
        /// <summary>
        /// Zergliedert den gegebenen Hypertext.
        /// </summary>
        /// <param name="hypertext">Der zu zergliedernde Hypertext.</param>
        /// <returns>Die aus den erfassten Daten erstellten Objekte.</returns>
        /// <exception cref="ParserException">Dieser Typ darf speziell behandelt werden.</exception>
        public IEnumerable<DataType> ParseContent(string hypertext);
    }
}
