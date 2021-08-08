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
        /// <param name="keywords">Die zu suchenden Schlüsselwörter.</param>
        /// <returns>Eine Liste mit Linkadressen.</returns>
        public IEnumerable<Uri> ParseHyperlinks(string hypertext,
                                                IEnumerable<string> keywords);
    }
}
