using System;
using System.Collections.Generic;

namespace Reusable.WebAccess
{
    /// <summary>
    /// Packt den Zerglieder zusammen mit seinen Filtern ein.
    /// </summary>
    public struct HyperlinksParserPackage
    {
        private readonly IHyperlinksParser _parser;
        private readonly Func<string, bool> _shouldParse;
        private readonly Func<string, bool> _linkNameFilter;

        /// <summary>
        /// Erstellt ein neues Objekt des Typs <see cref="HyperlinksParserPackage"/>.
        /// </summary>
        /// <param name="parser">Zerglieder Hyperlinks aus einem Hypertext.</param>
        /// <param name="shouldParse">Diese Rückrufaktion wird dem Aufruf des Zerglieders weitergegeben, und etscheidet, wann der Hypertext zergliedert wird.</param>
        /// <param name="linkNameFilter">Diese Rückrufaktion wird dem Auruf des Zerglieder weitergegeben, und etscheidet, welche zergliederte Hyperlinks zurückgegeben werden.</param>
        public HyperlinksParserPackage(IHyperlinksParser parser,
                                       Func<string, bool> shouldParse,
                                       Func<string, bool> linkNameFilter)
        {
            _parser = parser;
            _shouldParse = shouldParse;
            _linkNameFilter = linkNameFilter;
        }

        /// <summary>
        /// Ruf den Zerglieder auf.
        /// </summary>
        /// <param name="hypertext">Der zu zergliedernde Hypertext.</param>
        /// <returns>Die zergliederten Hyperlinks.</returns>
        public IEnumerable<Uri> Parse(string hypertext)
        {
            return _parser.ParseHyperlinks(hypertext, _shouldParse, _linkNameFilter);
        }
    }

}
