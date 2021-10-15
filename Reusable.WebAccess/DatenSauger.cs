using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Reusable.WebAccess
{
    /// <summary>
    /// Ein Webcrawler für die Datensammlung von Webseiten durch die Durchsuchung von Hyperlinks.
    /// </summary>
    /// <remarks>
    /// Implementiert die Datensammlung, sodass sie in einer bestimmten Webseite aufbricht.
    /// Jede Webseite enthält Hyperlinks, die mithilfe von Schlüsselwörtern gefasst werden müssen,
    /// und die dann zu einer anderen Webseite führen, was hiermit als "Hop" gennant wird.
    /// </remarks>
    /// <typeparam name="DataType">Der Typ für die Daten, die am Ende gesammelt werden.</typeparam>
    public class DatenSauger<DataType>
    {
        private readonly IHypertextFetcher _hypertextFetcher;

        private readonly Action<ParserException> _exceptionHandler;

        /// <summary>
        /// Erstellt eine neue Instanz.
        /// </summary>
        /// <param name="hypertextFetcher">Injizierte Implementierung für die Abrufung von Hypertext.</param>
        /// <param name="exceptionHandler">Handler für aufgetretene Ausnahmen von Typ <see cref="ParserException"/>.</param>
        public DatenSauger(IHypertextFetcher hypertextFetcher,
                           Action<ParserException> exceptionHandler = null)
        {
            _hypertextFetcher = hypertextFetcher;
            _exceptionHandler = exceptionHandler;
        }

        /// <summary>
        /// Startet die Datensammlung asynchron.
        /// </summary>
        /// <param name="firstUrl">Das URL der ersten Webseite.</param>
        /// <param name="hops"> Eine Liste von erwarteten Hops. Jedes Hop besteht in einem injizierten Zerglieder und seinen benötigten Schlüsselwörtern.</param>
        /// <param name="contentParser">Injizierter Zerglieder für den Inhalt der am Ende der Hops erreichten Webseite.</param>
        /// <returns>Eine asynchrone Aufgabe, die die gesamten gesammelten Daten verspricht.</returns>
        public async Task<IEnumerable<DataType>> CollectDataAsync(
            Uri firstUrl,
            IEnumerable<(IHyperlinksParser, IEnumerable<string>)> hops,
            IHypertextContentParser<DataType> contentParser)
        {
            var contents = new List<(Uri, string)>(capacity: hops.Count());
            await CollectContentRecursivelyAsync(firstUrl, hops, contents);

            var collectedData = new List<DataType>();
            foreach ((Uri url, string hypertext) in contents)
            {
                collectedData.AddRange(
                    WrapCall(() => contentParser.ParseContent(hypertext), url)
                );
            }

            return collectedData;
        }

        /// <summary>
        /// Führt die Datensammlung durch.
        /// </summary>
        /// <param name="firstUrl">Das URL der ersten Webseite.</param>
        /// <param name="hops"> Eine Liste von erwarteten Hops. Jedes Hop besteht in einem injizierten Zerglieder und seinen benötigten Schlüsselwörtern.</param>
        /// <param name="contentParser">Injizierter Zerglieder für den Inhalt der am Ende der Hops erreichten Webseite.</param>
        /// <remarks>Denn das Herunterladen von Webseiten zeitintensiv sein kann, läuft es asynchron im Hintergrund, während die Ergebnisse schrittweise freigegeben werden.</remarks>
        /// <returns>Eine Liste mit den gesammelten Daten.</returns>
        public IEnumerable<DataType> CollectData(
            Uri firstUrl,
            IEnumerable<(IHyperlinksParser, IEnumerable<string>)> hops,
            IHypertextContentParser<DataType> contentParser)
        {
            var contents = new List<(Uri, string)>(capacity: hops.Count());
            Task contentDownloadTask = CollectContentRecursivelyAsync(firstUrl, hops, contents);

            IEnumerable<(Uri, string)> availableContent;
            int taken = 0;
            bool downloading = true;
            while (downloading)
            {
                lock (contents)
                {
                    downloading = !contentDownloadTask.IsCompleted;
                    availableContent = contents.Skip(taken).Take(contents.Count - taken);
                }

                foreach ((Uri url, string hypertext) in availableContent)
                {
                    IEnumerable<DataType> parsedObjects =
                        WrapCall(() => contentParser.ParseContent(hypertext), url);

                    foreach (DataType obj in parsedObjects)
                    {
                        yield return obj;
                    }

                    ++taken;
                } 
            }
        }

        private async Task CollectContentRecursivelyAsync(
            Uri url,
            IEnumerable<(IHyperlinksParser, IEnumerable<string>)> hops,
            List<(Uri, string)> contents)
        {
            string hypertext = await _hypertextFetcher.DownloadFrom(url);

            if (hops.Count() == 0)
            {
                lock (contents)
                    contents.Add((url, hypertext));
                return;
            }

            (IHyperlinksParser linksParser, IEnumerable<string> keywords) = hops.First();
            IEnumerable<Uri> hyperlinks =
                WrapCall(() => linksParser.ParseHyperlinks(hypertext, keywords), url);

            var nextHops = hops.Skip(1);
            var recursiveCalls = new Task[hyperlinks.Count()];

            int idx = 0;
            foreach (Uri linkAddress in hyperlinks)
            {
                recursiveCalls[idx++] = CollectContentRecursivelyAsync(linkAddress, nextHops, contents);
            }

            Task.WaitAll(recursiveCalls);
        }

        /// <summary>
        /// Wickelt den Ruf einer Rückrufaktion ein, sodass eine eventuelle Ausnahme angemessen behandelt wird, ohne dass der Anrufer gestört wird.
        /// </summary>
        /// <typeparam name="ReturnType">Der Datentyp der zurückzugebenden Liste.</typeparam>
        /// <param name="callback">Die einzuwickelnde Rückrufaktion.</param>
        /// <param name="url">Gibt den kontext an, falls eine Ausnahme auftritt.</param>
        /// <returns>Die Rückgabe der Rückrufaktion.</returns>
        private IEnumerable<ReturnType> WrapCall<ReturnType>(Func<IEnumerable<ReturnType>> callback, Uri url)
        {
            try
            {
                return callback();
            }
            catch (ParserException exception)
            {
                var wrappedException =
                    new ParserException($"Zergliederung ist gescheitert! URL = {url}", exception);

                if (_exceptionHandler != null)
                {
                    _exceptionHandler(wrappedException);
                }
                else
                {
                    Trace.WriteLine(wrappedException);
                }

                return new ReturnType[0];
            }
        }

    }// end of class DatenSauger

}// end of namespace Reusable.WebAccess
