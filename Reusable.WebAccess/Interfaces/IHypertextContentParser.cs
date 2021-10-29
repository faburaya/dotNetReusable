using System;
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
        /// <param name="shouldParse">Diese Rückrufaktion wird nur einmal aufgerufen mit dem gesamten Inhalt des Hypertexts und entscheidet, ob der Hypertext überhaupt zergliedert wird.</param>
        /// <returns>Die aus den erfassten Daten erstellten Objekte.</returns>
        /// <exception cref="ParserException">Dieser Typ darf speziell behandelt werden.</exception>
        public IEnumerable<DataType> ParseContent(string hypertext, Func<string, bool> shouldParse);
    }
}
