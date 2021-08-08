using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Moq;
using Xunit;

using Reusable.Utils;

namespace Reusable.WebAccess.UnitTests
{
    public class DatenSaugerTest
    {
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
            var crawler = new DatenSauger<int>(hypertextFetcherMock.Object);
            IEnumerable<int> actualCollectedData =
                crawler.CollectDataAsync(
                    website.url,
                    new (IHyperlinksParser, IEnumerable<string>)[0],
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
            var crawler = new DatenSauger<int>(hypertextFetcherMock.Object);
            var hyperlinksParserMock = new Mock<IHyperlinksParser>(MockBehavior.Strict);
            IEnumerable<int> actualCollectedData =
                crawler.CollectDataAsync(
                    firstWebsite.url,
                    new[] {
                        CreateHopWith(hyperlinksParserMock, firstWebsite)
                    },
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
            var crawler = new DatenSauger<int>(hypertextFetcherMock.Object);
            var hyperlinkParsersMocks = With<Mock<IHyperlinksParser>>
                .CreateArrayOf(2, () => new Mock<IHyperlinksParser>(MockBehavior.Strict));

            IEnumerable<int> actualCollectedData =
                crawler.CollectDataAsync(
                    firstWebsite.url,
                    new[] {
                        CreateHopWith(hyperlinkParsersMocks[0], firstWebsite),
                        CreateHopWith(hyperlinkParsersMocks[1], secondWebsite)
                    },
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
        [InlineData(2)]
        [InlineData(300)]
        public void CollectData_WhenMultipleLinks_ThenParseContentFromAll(int websiteCount)
        {
            var finalWebsites = CreateManyFinalWebsites(websiteCount);
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
            var crawler = new DatenSauger<int>(hypertextFetcherMock.Object);
            var hyperlinksParserMock = new Mock<IHyperlinksParser>(MockBehavior.Strict);
            IEnumerable<int> actualCollectedData =
                crawler.CollectDataAsync(
                    firstWebsite.url,
                    new[] { CreateHopWith(hyperlinksParserMock, firstWebsite) },
                    contentParserMock.Object
                ).Result;

            Assert.Equal(actualCollectedData, expectedCollectedData);

            contentParserMock.VerifyAll();
            hyperlinksParserMock.VerifyAll();
            hypertextFetcherMock.VerifyAll();
        }

        [Fact]
        public void CollectData_WhenMultipleLinks_IfSynchronous_ThenEnsureThatSameDataIsCollected()
        {
            var finalWebsites = CreateManyFinalWebsites(100);
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
            var crawler = new DatenSauger<int>(hypertextFetcherMock.Object);
            var hyperlinksParserMock = new Mock<IHyperlinksParser>(MockBehavior.Strict);

            Task<IEnumerable<int>> asynchronousCollection =
                crawler.CollectDataAsync(
                    firstWebsite.url,
                    new[] { CreateHopWith(hyperlinksParserMock, firstWebsite) },
                    contentParserMock.Object
                );

            IEnumerable<int> synchronousCollection =
                crawler.CollectData(
                    firstWebsite.url,
                    new[] { CreateHopWith(hyperlinksParserMock, firstWebsite) },
                    contentParserMock.Object
                );

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

        private Mock<IHypertextFetcher> CreateMockToFetchHypertext(
            IEnumerable<(Uri, string)> urlsWithContent)
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
            foreach (var website in websites)
            {
                mock.Setup(obj => obj.ParseContent(website.content)).Returns(website.parsedObjects);
                expectedCollectedData.AddRange(website.parsedObjects);
            }
            return mock;
        }

        private (IHyperlinksParser, IEnumerable<string>) CreateHopWith(
            Mock<IHyperlinksParser> mock,
            FakeHopWebsite website)
        {
            IEnumerable<string> keywordsForHyperlinks = new string[] { "Schlüssel", "Wörter" };

            mock.Setup(
                obj => obj.ParseHyperlinks(
                    website.content,
                    It.Is<IEnumerable<string>>(keywords =>
                        keywords.SequenceEqual(keywordsForHyperlinks))
                )
            ).Returns(website.hyperlinks);

            return (mock.Object, keywordsForHyperlinks);
        }

        private IList<FakeFinalWebsite> CreateManyFinalWebsites(int count)
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

    struct FakeFinalWebsite
    {
        public Uri url;
        public string content;
        public int[] parsedObjects;

        public FakeFinalWebsite(Uri url, string content, int[] parsedObjects)
        {
            this.url = url;
            this.content = content;
            this.parsedObjects = parsedObjects;
        }
    }

    struct FakeHopWebsite
    {
        public Uri url;
        public string content;
        public IEnumerable<Uri> hyperlinks;

        public FakeHopWebsite(Uri url, string content, IEnumerable<Uri> hyperlinks)
        {
            this.url = url;
            this.content = content;
            this.hyperlinks = hyperlinks;
        }
    }

}// end of namespace Reusable.WebAccess.UnitTests