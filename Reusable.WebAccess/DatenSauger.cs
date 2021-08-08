using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Reusable.WebAccess
{
    /// <summary>
    /// Ein Webcrawler für die Datensammlung von Webseiten durch die Durchsuchung von Hyperlinks.
    /// </summary>
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
        /// Startet die Datensammlung, die in einer bestimmten Webseite aufbricht.
        /// Jede Webseite enthält Hyperlinks, die mithilfe von Schlüsselwörtern gefasst werden müssen,
        /// und die dann zu einer anderen Webseite führen, was hiermit als "Hop" gennant wird.
        /// </summary>
        /// <param name="firstUrl">Das URL der ersten Webseite.</param>
        /// <param name="hops"> Eine Liste von erwarteten Hops. Jedes Hop besteht in einem injizierten Zerglieder und seinen benötigten Schlüsselwörtern.
        /// </param>
        /// <param name="contentParser">Injizierter Zerglieder für den Inhalt der am Ende der Hops erreichten Webseite.</param>
        /// <remarks>Denn das Verfahren darf wegen des Herunterladens von Webseiten zeitintensiv sein, läuft es asynchron.
        /// </remarks>
        /// <returns></returns>
        public async Task<IEnumerable<DataType>> CollectDataAsync(
            Uri firstUrl,
            IEnumerable<(IHyperlinksParser, IEnumerable<string>)> hops,
            IHypertextContentParser<DataType> contentParser)
        {
            var contents = new List<string>(hops.Count());
            await CollectContentRecursivelyAsync(firstUrl, hops, contents);

            var collectedData = new List<DataType>(contents.Count);
            foreach (string hypertext in contents)
            {
                collectedData.AddRange(contentParser.ParseContent(hypertext));
            }

            return collectedData;
        }

        private async Task CollectContentRecursivelyAsync(
            Uri url,
            IEnumerable<(IHyperlinksParser, IEnumerable<string>)> hops,
            List<string> contents)
        {
            string hypertext = await _hypertextFetcher.DownloadFrom(url);

            if (hops.Count() == 0)
            {
                lock (contents)
                    contents.Add(hypertext);
                return;
            }

            IHyperlinksParser linksParser = hops.First().Item1;
            IEnumerable<string> keywords = hops.First().Item2;
            var nextHops = hops.Skip(1);
            var hyperlinks = linksParser.ParseHyperlinks(hypertext, keywords);
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
