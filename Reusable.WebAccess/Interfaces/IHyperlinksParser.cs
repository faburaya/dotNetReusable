using System;
using System.Collections.Generic;

namespace Reusable.WebAccess
{
    /// <summary>
    /// Schnittstelle für die Zergliederung von Links in Hypertext.
    /// </summary>
    public interface IHyperlinksParser
    {
        /// <summary>
        /// Zergliedert bestimmte Links von Hypertext.
        /// </summary>
        /// <param name="hypertext">Der gegebene Hypertext.</param>
        /// <param name="shouldParse">Diese Rückrufaktion wird nur einmal aufgerufen mit dem gesamten Inhalt des Hypertexts und entscheidet, ob der Hypertext überhaupt zergliedert wird.</param>
        /// <param name="linkNameFilter">Ein Erbegnisse beschränkender Filter, der für jedes Hyperlinks aufgerufen wird und dessen Namen erhält.</param>
        /// <exception cref="ParserException">Dieser Typ darf speziell behandelt werden.</exception>
        public IEnumerable<Uri> ParseHyperlinks(string hypertext,
                                                Func<string, bool> shouldParse,
                                                Func<string, bool> linkNameFilter);
    }
}
