using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;

using Reusable.DataAccess.Common;

namespace Reusable.WebAccess
{
    /// <summary>
    /// Ein Webcrawler für die Datensammlung von Webseiten durch die Durchsuchung von Hyperlinks.
    /// </summary>
    /// <remarks>
    /// Implementiert die Datensammlung, sodass sie in einer bestimmten Webseite aufbricht.
    /// Jede Webseite enthält zu erfassende Hyperlinks, die dann zu einer anderen Webseite führen,
    /// was hiermit als "Hop" gennant wird.
    /// </remarks>
    /// <typeparam name="DataType">Der Typ für die Daten, die am Ende gesammelt werden.</typeparam>
    public class DatenSauger<DataType>
    {
        private readonly IHypertextFetcher _hypertextFetcher;

        private readonly Utils.ILogger _log;

        /// <summary>
        /// Erstellt eine neue Instanz.
        /// </summary>
        /// <param name="hypertextFetcher">Injizierte Implementierung für die Abrufung von Hypertext.</param>
        /// <param name="log">Injizierter Dienst für Protokollierung.</param>
        public DatenSauger(IHypertextFetcher hypertextFetcher, Utils.ILogger log = null)
        {
            _hypertextFetcher = hypertextFetcher;
            _log = log;
        }

        /// <summary>
        /// Startet die Datensammlung asynchron.
        /// </summary>
        /// <param name="firstUrl">Das URL der ersten Webseite.</param>
        /// <param name="hops"> Eine Liste von Zergliedern für jedes erwartetes Hop.</param>
        /// <param name="contentParser">Injizierter Zerglieder für den Inhalt der am Ende der Hops erreichten Webseite.</param>
        /// <returns>Eine asynchrone Aufgabe, die die gesamten gesammelten Daten verspricht.</returns>
        /// <exception cref="ParserException" />
        public async Task<IEnumerable<DataType>> CollectDataAsync(
            Uri firstUrl,
            IEnumerable<IHyperlinksParser> hops,
            IHypertextContentParser<DataType> contentParser)
        {
            var contents = new ConcurrentQueue<(Uri, string)>();
            await CollectContentRecursivelyAsync(firstUrl, hops, contents);

            var collectedData = new List<DataType>();
            foreach ((Uri url, string hypertext) in contents)
            {
                IEnumerable<DataType> parsedObjects =
                    WrapCall(() => contentParser.ParseContent(hypertext), url);

                collectedData.AddRange(parsedObjects);

                _log?.Info($"Die Zergliederung von {url} hat {parsedObjects.Count()} ergeben.");
            }

            return collectedData;
        }

        /// <summary>
        /// Führt die Datensammlung durch.
        /// </summary>
        /// <param name="firstUrl">Das URL der ersten Webseite.</param>
        /// <param name="hops"> Eine Liste von Zergliedern für jedes erwartetes Hop.</param>
        /// <param name="contentParser">Injizierter Zerglieder für den Inhalt der am Ende der Hops erreichten Webseite.</param>
        /// <remarks>Denn das Herunterladen von Webseiten zeitintensiv sein kann, läuft es asynchron im Hintergrund, während die Ergebnisse schrittweise freigegeben werden.</remarks>
        /// <returns>Eine Liste mit den gesammelten Daten.</returns>
        /// <exception cref="ParserException" />
        public IEnumerable<DataType> CollectData(
            Uri firstUrl,
            IEnumerable<IHyperlinksParser> hops,
            IHypertextContentParser<DataType> contentParser)
        {
            var contents = new ConcurrentQueue<(Uri, string)>();
            Task contentDownloadTask = CollectContentRecursivelyAsync(firstUrl, hops, contents);

            bool downloading = true;
            while (downloading)
            {
                downloading = !contentDownloadTask.IsCompleted;

                while (contents.TryDequeue(out (Uri url, string hypertext) content))
                {
                    IEnumerable<DataType> parsedObjects =
                        WrapCall(() => contentParser.ParseContent(content.hypertext), content.url);

                    foreach (DataType obj in parsedObjects)
                    {
                        yield return obj;
                    }

                    _log?.Info($"Die Zergliederung von {content.url} hat {parsedObjects.Count()} ergeben.");
                } 
            }
        }

        /// <summary>
        /// Führt die Datensammlung langsam durch,
        /// damit eine Webseite nicht überfordert wird.
        /// </summary>
        /// <param name="firstUrl">Das URL der ersten Webseite.</param>
        /// <param name="hops"> Eine Liste von Zergliedern für jedes erwartetes Hop.</param>
        /// <param name="contentParser">Injizierter Zerglieder für den Inhalt der am Ende der Hops erreichten Webseite.</param>
        /// <remarks>Denn das Herunterladen von Webseiten zeitintensiv sein kann, läuft es asynchron im Hintergrund, während die Ergebnisse schrittweise freigegeben werden.</remarks>
        /// <returns>Eine Liste mit den gesammelten Daten.</returns>
        /// <exception cref="ParserException" />
        public IEnumerable<DataType> CollectDataSlowly(
            Uri firstUrl,
            IEnumerable<IHyperlinksParser> hops,
            IHypertextContentParser<DataType> contentParser)
        {
            var contents = new ConcurrentQueue<(Uri, string)>();
            Task contentDownloadTask = Task.Run(
                () => CollectContentRecursivelySlowly(firstUrl, hops, contents));

            bool downloading = true;
            while (downloading)
            {
                downloading = !contentDownloadTask.IsCompleted;

                while (contents.TryDequeue(out (Uri url, string hypertext) content))
                {
                    IEnumerable<DataType> parsedObjects =
                        WrapCall(() => contentParser.ParseContent(content.hypertext), content.url);

                    foreach (DataType obj in parsedObjects)
                    {
                        yield return obj;
                    }

                    _log?.Info($"Die Zergliederung von {content.url} hat {parsedObjects.Count()} ergeben.");
                }
            }
        }

        private void CollectContentRecursivelySlowly(Uri url,
                                                     IEnumerable<IHyperlinksParser> hops,
                                                     ConcurrentQueue<(Uri, string)> contents)
        {
            _log?.Debug($"Ladet {url} herunter...");

            string hypertext = _hypertextFetcher.DownloadFrom(url).Result;

            if (hops.Count() == 0)
            {
                contents.Enqueue((url, hypertext));
                _log?.Debug($"Letztes Hop erreicht: {url} ist heruntergeladen.");
                return;
            }

            IHyperlinksParser hyperlinksParser = hops.First();
            IEnumerable<Uri> hyperlinks =
                WrapCall(() => hyperlinksParser.ParseHyperlinks(hypertext), url);

            _log?.Info($"{hyperlinks.Count()} Hyperlinks wurden durch {url} ergeben.");

            var nextHops = hops.Skip(1);

            foreach (Uri linkAddress in hyperlinks)
            {
                CollectContentRecursivelySlowly(linkAddress, nextHops, contents);
            }
        }

        private async Task CollectContentRecursivelyAsync(Uri url,
                                                          IEnumerable<IHyperlinksParser> hops,
                                                          ConcurrentQueue<(Uri, string)> contents)
        {
            _log?.Debug($"Ladet {url} herunter...");

            string hypertext = await _hypertextFetcher.DownloadFrom(url);

            if (hops.Count() == 0)
            {
                contents.Enqueue((url, hypertext));
                _log?.Debug($"Letztes Hop erreicht: {url} ist heruntergeladen.");
                return;
            }

            IHyperlinksParser hyperlinksParser = hops.First();
            IEnumerable<Uri> hyperlinks =
                WrapCall(() => hyperlinksParser.ParseHyperlinks(hypertext), url);

            _log?.Info($"{hyperlinks.Count()} Hyperlinks wurden durch {url} ergeben.");

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
            void HandleException(Exception exception)
            {
                var wrappedException = new ParserException(
                    $"Die Zergliederung einer Internetseite ist nicht gelungen. ({url})", exception);

                _log?.Error($"Fehler ist aufgetreten: {wrappedException}");
            }

            try
            {
                _log?.Debug($"Startet die Zergliederung von {url}");
                return callback();
            }
            catch (ParserException exception)
            {
                HandleException(exception);
            }
            catch (AggregateException exception)
                when (exception.InnerExceptions.All(ex => ex is ParserException))
            {
                HandleException(exception);
            }

            return new ReturnType[0];
        }

    }// end of class DatenSauger

}// end of namespace Reusable.WebAccess
