using System;
using System.Net.Http;
using System.Threading.Tasks;

using Reusable.Utils;

namespace Reusable.WebAccess
{
    /// <summary>
    /// Gewährt die Fähigkeit zum Abrufen des Hypertext aus einer Webseite.
    /// </summary>
    public class HypertextFetcher : IHypertextFetcher
    {
        private static readonly HttpClient httpClient;

        private static readonly TimeSpan timeSlotForRetry;

        static HypertextFetcher()
        {
            httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add(
                "user-agent", "Mozilla/5.0 (compatible; MSIE 10.0; Windows NT 6.2; WOW64; Trident/6.0)");

            timeSlotForRetry = new TimeSpan((long)(100 /*ms*/ * 1e4 /*ticks per ms*/));
        }

        private readonly ushort _maxRetries;

        /// <summary>
        /// Ertellt eine neue Instanz von <see cref="HypertextFetcher"/>.
        /// </summary>
        /// <param name="maxRetries">Legt fest, wievielmal das Herunterladen zu versuchen ist.</param>
        public HypertextFetcher(ushort maxRetries = 5)
        {
            _maxRetries = maxRetries;
        }

        /// <inheritdoc/>
        public async Task<string> DownloadFrom(Uri url)
        {
            uint attempt = 1;
            while (true)
            {
                try
                {
                    var request = httpClient.GetStringAsync(url);
                    return await request;
                }
                catch (HttpRequestException ex) when (ex.Message.Contains(/*HTTP Fehler*/" 403 "))
                {
                    if (attempt <= _maxRetries)
                    {
                        System.Threading.Thread.Sleep(
                            RetryStrategy.CalculateExponentialBackoff(timeSlotForRetry, attempt)
                        );
                        ++attempt;
                        continue;
                    }

                    throw new Exception($"Das Abrufen von Hypertext aus der URL {url} is gescheitert!", ex);
                }
            }
        }

    }// end of class HypertextFetcher

}// end of namespace Reusable.WebAccess
