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
        /// <param name="keywords">Die zu suchenden Schlüsselwörter, welche die Ergebnisse beschränken. Gibt nur die Marken zurück, die mit einem Schlüsselwort übereinstimmen. Der Vergleich unterscheidet nicht zwischen Klein- und Großschreibung. Wenn kein Schlüsselwort gegeben wird, hat dieser Parameter keine Wirkung.</param>
        /// <exception cref="ParserException">Dieser Typ darf speziell behandelt werden.</exception>
        public IEnumerable<Uri> ParseHyperlinks(string hypertext,
                                                IEnumerable<string> keywords);
    }
}
