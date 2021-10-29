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
        /// <exception cref="ParserException">Dieser Typ darf speziell behandelt werden.</exception>
        public IEnumerable<Uri> ParseHyperlinks(string hypertext);
    }
}
