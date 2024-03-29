using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Moq;
using Xunit;
using Reusable.Utils;
using Reusable.DataAccess.Common;

namespace Reusable.WebAccess.UnitTests
{
    public class DatenSaugerTest : IDisposable
    {
        private const string _logFilePath = "DatenSaugerTest.log.txt";

        private readonly SimpleFileLogger _log = new(_logFilePath);

        private bool _disposed = false;

        public void Dispose()
        {
            if (_disposed)
                return;

            _log.Dispose();
            File.Delete(_logFilePath);

            _disposed = true;
            GC.SuppressFinalize(this);
        }

        [Fact]
        public void CollectData_WhenNoHops_ThenJustParseContent()
        {
            var website = new FakeFinalWebsite(url: new Uri("http://daten-quelle.de"),
                                               content: "Hier steht HTML mit echtem Inhalt",
                                               parsedObjects: new int[] { 1, 2, 3 });

            // Richtet das Holen von Hypertext aus den Webseiten ein:
            Mock<IHypertextFetcher> hypertextFetcherMock =
                CreateMockToFetchHypertext(new[] { (website.url, website.content) });

            // Richtet die Zergliederung von dem Inhalt in der endgültigen Webseite ein:
            Mock<IHypertextContentParser<int>> contentParserMock =
                CreateMockToParseContent(new[] { website }, out List<int> expectedCollectedData);

            // Startet den DatenSauger ohne Hops:
            var crawler = new DatenSauger<int>(hypertextFetcherMock.Object, _log);
            IEnumerable<int> actualCollectedData =
                crawler.CollectDataAsync(
                    website.url,
                    new IHyperlinksParser[0],
                    contentParserMock.Object
                ).Result;

            Assert.Equal(actualCollectedData, expectedCollectedData);

            contentParserMock.VerifyAll();
            hypertextFetcherMock.VerifyAll();
        }

        [Fact]
        public void CollectData_WhenOneHop_ThenHopBeforeParsingContent()
        {
            var finalWebsite = new FakeFinalWebsite(url: new Uri("http://daten-quelle.de"),
                                                    content: "Hier steht HTML mit echtem Inhalt",
                                                    parsedObjects: new int[] { 1, 2, 3 });

            var firstWebsite = new FakeHopWebsite(url: new Uri("http://erste-webseite.de"),
                                                  content: "Das ist nur ein Hop vor der letzten Webseite",
                                                  hyperlinks: new[] { finalWebsite.url });

            // Richtet das Holen von Hypertext aus den Webseiten ein:
            Mock<IHypertextFetcher> hypertextFetcherMock = CreateMockToFetchHypertext(new[] {
                (firstWebsite.url, firstWebsite.content),
                (finalWebsite.url, finalWebsite.content),
            });

            // Richtet die Zergliederung von dem Inhalt in der endgültigen Webseite ein:
            Mock<IHypertextContentParser<int>> contentParserMock =
                CreateMockToParseContent(new[] { finalWebsite }, out List<int> expectedCollectedData);

            // Startet den DatenSauger mit einem einzigen Hop:
            var crawler = new DatenSauger<int>(hypertextFetcherMock.Object, _log);
            Mock<IHyperlinksParser> hyperlinksParserMock = CreateMockForHop(firstWebsite);
            IEnumerable<int> actualCollectedData =
                crawler.CollectDataAsync(
                    firstWebsite.url,
                    new[] { hyperlinksParserMock.Object },
                    contentParserMock.Object
                ).Result;

            Assert.Equal(actualCollectedData, expectedCollectedData);

            contentParserMock.VerifyAll();
            hyperlinksParserMock.VerifyAll();
            hypertextFetcherMock.VerifyAll();
        }

        [Fact]
        public void CollectData_WhenTwoHops_ThenHopTwiceBeforeParsingContent()
        {
            var finalWebsite = new FakeFinalWebsite(url: new Uri("http://daten-quelle.de"),
                                                    content: "Hier steht HTML mit echtem Inhalt",
                                                    parsedObjects: new int[] { 1, 2, 3 });

            var secondWebsite = new FakeHopWebsite(url: new Uri("http://zweite-webseite.de"),
                                                   content: "Das ist noch eines Hop",
                                                   hyperlinks: new[] { finalWebsite.url });

            var firstWebsite = new FakeHopWebsite(url: new Uri("http://erste-webseite.de"),
                                                  content: "Das ist nur ein Hop",
                                                  hyperlinks: new[] { secondWebsite.url });

            // Richtet das Holen von Hypertext aus den Webseiten ein:
            Mock<IHypertextFetcher> hypertextFetcherMock = CreateMockToFetchHypertext(new[] {
                (firstWebsite.url, firstWebsite.content),
                (secondWebsite.url, secondWebsite.content),
                (finalWebsite.url, finalWebsite.content)
            });

            // Richtet die Zergliederung von dem Inhalt in der endgültigen Webseite ein:
            Mock<IHypertextContentParser<int>> contentParserMock =
                CreateMockToParseContent(new[] { finalWebsite }, out List<int> expectedCollectedData);

            // Startet den DatenSauger mit zwei Hops:
            var crawler = new DatenSauger<int>(hypertextFetcherMock.Object, _log);
            Mock<IHyperlinksParser>[] hyperlinkParsersMocks = new[] {
                CreateMockForHop(firstWebsite),
                CreateMockForHop(secondWebsite)
            };

            IEnumerable<int> actualCollectedData =
                crawler.CollectDataAsync(
                    firstWebsite.url,
                    from mock in hyperlinkParsersMocks select mock.Object,
                    contentParserMock.Object
                ).Result;

            Assert.Equal(actualCollectedData, expectedCollectedData);

            foreach (var mock in hyperlinkParsersMocks)
            {
                mock.VerifyAll();
            }
            contentParserMock.VerifyAll();
            hypertextFetcherMock.VerifyAll();
        }

        [Theory]
        [InlineData(2, false)]
        [InlineData(2, true)]
        [InlineData(300, false)]
        public void CollectData_WhenMultipleLinks_ThenParseContentFromAll(int websiteCount, bool withFailures)
        {
            List<FakeFinalWebsite> finalWebsites = CreateManyFinalWebsites(websiteCount);
            
            if (withFailures)
            {
                finalWebsites.Add(
                    new FakeFinalWebsite(new Uri("http://beschädigte-webseite.de"),
                                         "Hier steht fehlerhaftes HTML.",
                                         new ParserException("Ruhig: vorgetäuschter Fehler :-)"))
                );
            }

            var firstWebsite = new FakeHopWebsite(new Uri("http://erste-webseite.de"),
                                                  "Das ist nur ein Hop vor der endgültigen Webseiten",
                                                  from website in finalWebsites select website.url);

            // Richtet das Holen von Hypertext aus den Webseiten ein:
            var allWebsites = new List<(Uri, string)> { (firstWebsite.url, firstWebsite.content) };
            allWebsites.AddRange(from website in finalWebsites select (website.url, website.content));
            Mock<IHypertextFetcher> hypertextFetcherMock = CreateMockToFetchHypertext(allWebsites);

            // Richtet die Zergliederung von dem Inhalt in der endgültigen Webseite ein:
            Mock<IHypertextContentParser<int>> contentParserMock =
                CreateMockToParseContent(finalWebsites, out List<int> expectedCollectedData);

            // Startet den DatenSauger mit einem einzigen Hop:
            Mock<IHyperlinksParser> hyperlinksParserMock = CreateMockForHop(firstWebsite);
            var crawler = new DatenSauger<int>(hypertextFetcherMock.Object, _log);
            IEnumerable<int> actualCollectedData =
                crawler.CollectDataAsync(
                    firstWebsite.url,
                    new[] { hyperlinksParserMock.Object },
                    contentParserMock.Object
                ).Result;

            Assert.Equal(actualCollectedData, expectedCollectedData);

            contentParserMock.VerifyAll();
            hyperlinksParserMock.VerifyAll();
            hypertextFetcherMock.VerifyAll();
        }

        public enum SimulationOptions { WithFailures, NoFailures }

        public enum SpeedOptions { WithParallelWebRequests, SlowlyNoParallelWebRequests }

        [Theory]
        [InlineData(SimulationOptions.NoFailures, SpeedOptions.WithParallelWebRequests)]
        [InlineData(SimulationOptions.NoFailures, SpeedOptions.SlowlyNoParallelWebRequests)]
        [InlineData(SimulationOptions.WithFailures, SpeedOptions.WithParallelWebRequests)]
        [InlineData(SimulationOptions.WithFailures, SpeedOptions.SlowlyNoParallelWebRequests)]
        public void CollectData_WhenMultipleLinks_IfSynchronous_ThenEnsureThatSameDataIsCollected(SimulationOptions simulation, SpeedOptions speed)
        {
            List<FakeFinalWebsite> finalWebsites = CreateManyFinalWebsites(100);

            if (simulation == SimulationOptions.WithFailures)
            {
                finalWebsites.Add(
                    new FakeFinalWebsite(new Uri("http://beschädigte-webseite.de"),
                                         "Hier steht fehlerhaftes HTML.",
                                         new ParserException("Ruhig: vorgetäuschter Fehler :-)"))
                );
            }

            var firstWebsite = new FakeHopWebsite(new Uri("http://erste-webseite.de"),
                                                  "Das ist nur ein Hop vor der endgültigen Webseiten",
                                                  from website in finalWebsites select website.url);

            // Richtet das Holen von Hypertext aus den Webseiten ein:
            var allWebsites = new List<(Uri, string)> { (firstWebsite.url, firstWebsite.content) };
            allWebsites.AddRange(from website in finalWebsites select (website.url, website.content));
            Mock<IHypertextFetcher> hypertextFetcherMock = CreateMockToFetchHypertext(allWebsites);

            // Richtet die Zergliederung von dem Inhalt in der endgültigen Webseite ein:
            Mock<IHypertextContentParser<int>> contentParserMock =
                CreateMockToParseContent(finalWebsites, out List<int> expectedCollectedData);

            // Startet den DatenSauger mit einem einzigen Hop:
            var crawler = new DatenSauger<int>(hypertextFetcherMock.Object, _log);
            Mock<IHyperlinksParser> hyperlinksParserMock = CreateMockForHop(firstWebsite);
            Task<IEnumerable<int>> asynchronousCollection =
                crawler.CollectDataAsync(
                    firstWebsite.url,
                    new[] { hyperlinksParserMock.Object },
                    contentParserMock.Object
                );

            IEnumerable<int> synchronousCollection = speed switch
            {
                SpeedOptions.WithParallelWebRequests =>
                    crawler.CollectData(firstWebsite.url,
                                        new[] { hyperlinksParserMock.Object },
                                        contentParserMock.Object),

                SpeedOptions.SlowlyNoParallelWebRequests =>
                    crawler.CollectDataSlowly(firstWebsite.url,
                                              new[] { hyperlinksParserMock.Object },
                                              contentParserMock.Object),

                _ => throw new NotSupportedException(),
            };

            var synchronouslyCollectedData = new List<int>();
            foreach (int parsedObject in synchronousCollection)
            {
                synchronouslyCollectedData.Add(parsedObject);
            }

            Assert.Equal(synchronouslyCollectedData, asynchronousCollection.Result);

            contentParserMock.VerifyAll();
            hyperlinksParserMock.VerifyAll();
            hypertextFetcherMock.VerifyAll();
        }

        private Mock<IHypertextFetcher> CreateMockToFetchHypertext(IEnumerable<(Uri, string)> urlsWithContent)
        {
            var mock = new Mock<IHypertextFetcher>(MockBehavior.Strict);

            foreach (var pair in urlsWithContent)
            {
                Uri url = pair.Item1;
                string content = pair.Item2;
                mock.Setup(obj => obj.DownloadFrom(url))
                    .ReturnsAsync(() => {
                        Thread.Sleep(TimeSpan.FromMilliseconds(url.GetHashCode() % 2 == 1 ? 1 : 5));
                        return content;
                    });
            }

            return mock;
        }

        private Mock<IHypertextContentParser<int>> CreateMockToParseContent(
            IEnumerable<FakeFinalWebsite> websites,
            out List<int> expectedCollectedData)
        {
            expectedCollectedData = new List<int>();
            var mock = new Mock<IHypertextContentParser<int>>(MockBehavior.Strict);

            foreach (FakeFinalWebsite website in websites)
            {
                if (website.error == null)
                {
                    mock.Setup(obj =>
                        obj.ParseContent(website.content)
                    ).Returns(website.parsedObjects);

                    expectedCollectedData.AddRange(website.parsedObjects);
                }
                else
                {
                    mock.Setup(obj =>
                        obj.ParseContent(website.content)
                    ).Throws(website.error);
                }
            }

            return mock;
        }

        private Mock<IHyperlinksParser> CreateMockForHop(FakeHopWebsite website)
        {
            var mock = new Mock<IHyperlinksParser>(MockBehavior.Strict);

            if (website.error == null)
            {
                mock.Setup(obj =>
                    obj.ParseHyperlinks(website.content)
                ).Returns(website.hyperlinks);
            }
            else
            {
                mock.Setup(obj =>
                    obj.ParseHyperlinks(website.content)
                ).Throws(website.error);
            }

            return mock;
        }

        private List<FakeFinalWebsite> CreateManyFinalWebsites(int count)
        {
            var random = new Random();
            var websites = new List<FakeFinalWebsite>(capacity: count);

            for (int idx = 0; idx < count; ++idx)
            {
                websites.Add(new FakeFinalWebsite(url: new Uri($"http://daten-quelle-{idx}.de"),
                                                  content: $"Hier steht HTML mit echtem Inhalt #{idx}",
                                                  parsedObjects: new int[] { random.Next(), random.Next() }));
            }

            return websites;
        }

    }// end of class DatenSaugerTest

    #region Typen für die Unterstüzung der Tests

    struct FakeFinalWebsite
    {
        public Uri url;
        public string content;
        public int[] parsedObjects;
        public ParserException error;

        public FakeFinalWebsite(Uri url, string content, int[] parsedObjects)
        {
            this.url = url;
            this.content = content;
            this.parsedObjects = parsedObjects;
            this.error = null;
        }

        public FakeFinalWebsite(Uri url, string content, ParserException error)
        {
            this.url = url;
            this.content = content;
            this.parsedObjects = null;
            this.error = error;
        }
    }

    struct FakeHopWebsite
    {
        public Uri url;
        public string content;
        public IEnumerable<Uri> hyperlinks;
        public ParserException error;

        public FakeHopWebsite(Uri url, string content, IEnumerable<Uri> hyperlinks)
        {
            this.url = url;
            this.content = content;
            this.hyperlinks = hyperlinks;
            this.error = null;
        }

        public FakeHopWebsite(Uri url, string content, ParserException error)
        {
            this.url = url;
            this.content = content;
            this.hyperlinks = null;
            this.error = error;
        }
    }

    #endregion

}// end of namespace Reusable.WebAccess.UnitTests