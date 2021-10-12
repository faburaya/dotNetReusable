using System;
using System.Collections.Generic;
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

        /// <summary>
        /// Erstellt eine neue Instanz.
        /// </summary>
        /// <param name="hypertextFetcher">Injizierte Implementierung für die Abrufung von Hypertext.</param>
        public DatenSauger(IHypertextFetcher hypertextFetcher)
        {
            _hypertextFetcher = hypertextFetcher;
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
                try
                {
                    collectedData.AddRange(contentParser.ParseContent(hypertext));
                }
                catch (Exception parserException)
                {
                    throw new Exception($"Zergliederung des Inhalts aus {url} ist gescheitert!", parserException);
                }
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
                    IEnumerable<DataType> parsedObjects;

                    try
                    {
                        parsedObjects = contentParser.ParseContent(hypertext);
                    }
                    catch (Exception parserException)
                    {
                        throw new Exception($"Zergliederung des Inhalts aus {url} ist gescheitert!", parserException);
                    }

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
            IEnumerable<Uri> hyperlinks;

            try
            {
                hyperlinks = linksParser.ParseHyperlinks(hypertext, keywords);
            }
            catch (Exception parserException)
            {
                throw new Exception($"Zergliederung der Hyperlinks im {url} ist gescheitert!", parserException);
            }

            var nextHops = hops.Skip(1);
            var recursiveCalls = new Task[hyperlinks.Count()];

            int idx = 0;
            foreach (Uri linkAddress in hyperlinks)
            {
                recursiveCalls[idx++] = CollectContentRecursivelyAsync(linkAddress, nextHops, contents);
            }

            Task.WaitAll(recursiveCalls);
        }

    }// end of class DatenSauger

}// end of namespace Reusable.WebAccess
