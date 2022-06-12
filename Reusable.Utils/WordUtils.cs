using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Reusable.Utils
{
    /// <summary>
    /// Sammlung von Algorithmen, die bei der Arbeit mit Wörtern nützlich sind.
    /// </summary>
    public static class WordUtils
    {
        private static readonly Regex _notWordForwardRegex = new(@"([^\w]+)", RegexOptions.Compiled);

        /// <summary>
        /// Findet das Ende eines Wortes.
        /// </summary>
        /// <param name="text">Der Text.</param>
        /// <param name="start">Ab dieser Indexposition nach rechts startet die Suche.</param>
        /// <returns>Falls <paramref name="start"/> mitten in einem Wort liegt, dann gibt die Indexposition nach seinem letzten Charakter zurück, sonst <paramref name="start"/>.</returns>
        public static int FindEndOfWord(string text, int start)
        {
            Match nextMatch = _notWordForwardRegex.Match(text, start);
            return (nextMatch.Success ? nextMatch.Index : text.Length);
        }

        private static readonly Regex _notWordBackwardRegex =
            new(@"([^\w]+)", RegexOptions.Compiled | RegexOptions.RightToLeft);

        /// <summary>
        /// Findet den Anfang eines Wortes.
        /// </summary>
        /// <param name="text">Der Text.</param>
        /// <param name="last">Ab dieser Indexposition nach links startet die Suche.</param>
        /// <returns>Falls <paramref name="last"/> mitten in einem Wort liegt, dann gibt die Indexposition seines ersten Charakters zurück, sonst <paramref name="last"/>.</returns>
        public static int FindStartOfWord(string text, int last)
        {
            Match previousMatch = _notWordBackwardRegex.Match(text, last + 1);
            return Math.Min(last, previousMatch.Success ? previousMatch.Index + previousMatch.Length : 0);
        }

        /// <summary>
        /// Findet den Anfang oder das Ende eines Wortes, je nachdem, welche näher ist.
        /// </summary>
        /// <param name="text">Der Text.</param>
        /// <param name="pos">Es wird um diese Indexposition rückwärts nach dem Anfang des Wortes und vorwärts nach dem Ende des Wortes gesucht.</param>
        /// <returns>Falls <paramref name="pos"/> innerhalb eines Wort liegt, dann gibt die Indexposition des Anfangs oder des Endes des Wortes zurück, je nachdem, welche näher ist. Sonst, gibt <paramref name="pos"/> zurück.</returns>
        public static int FindClosestStartOrEndOfWord(string text, int pos)
        {
            int previous = FindStartOfWord(text, pos);
            int next = FindEndOfWord(text, pos);

            if (next - pos <= pos - previous)
            {
                return next;
            }
            else
            {
                return previous;
            }
        }

        private static readonly Regex _wordRegex = new(@"([\w]+)");

        private static List<OutType> SplitIntoWordsImpl<OutType>(
            string text, int begin, int end, Func<Match, OutType> callback)
        {
            List<OutType> words = new();
            foreach (Match match in _wordRegex.Matches(text, begin))
            {
                if (match.Index + match.Length <= end)
                {
                    words.Add(callback(match));
                }
                else
                    break;
            }
            return words;
        }

        /// <summary>
        /// Spaltet einen Bereich im Text in Wörter auf.
        /// </summary>
        /// <param name="text">Der Text.</param>
        /// <param name="begin">Legt den Bereich fest: die Indexposition des ersten Charakter.</param>
        /// <param name="end">Legt den Bereich fest: die Indexposition nach dem letzten Charakter.</param>
        /// <returns>Eine Liste mit Instanzen von <see cref="SubstringRef"/>, die auf die Wörter verweisen.</returns>
        public static List<SubstringRef> SplitIntoWordsAsRefs(string text, int begin, int end)
        {
            return SplitIntoWordsImpl(text, begin, end,
                (Match match) => new SubstringRef(match, text));
        }

        /// <summary>
        /// Spaltet einen Bereich im Text in Wörter auf.
        /// </summary>
        /// <param name="text">Der Text.</param>
        /// <param name="begin">Legt den Bereich fest: die Indexposition des ersten Charakter.</param>
        /// <param name="end">Legt den Bereich fest: die Indexposition nach dem letzten Charakter.</param>
        /// <returns>Eine Liste mit Kopien der aufgespalteten Wörter.</returns>
        public static List<string> SplitIntoWords(string text, int begin, int end)
        {
            return SplitIntoWordsImpl(text, begin, end,
                (Match match) => match.Value);
        }
    }
}
