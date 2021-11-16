using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Xunit;

namespace Reusable.Utils.UnitTests
{
    public class SimpleFileLoggerTest
    {
        [Fact]
        public void InstantiateAndDispose_FileDoesNotExist()
        {
            const string filePath = "log.txt";
            var log = new SimpleFileLogger(filePath);
            log.Dispose();
            File.Delete(filePath);
        }

        [Fact]
        public void InstantiateAndDispose_FileExists()
        {
            const string filePath = "log.txt";
            File.WriteAllText(filePath, "Etwas.");
            var log = new SimpleFileLogger(filePath);
            log.Dispose();
            File.Delete(filePath);
        }

        [Fact]
        public void LogOneMessage_SingleThread()
        {
            const string message = "Nachricht";
            const string filePath = "log.txt";
            using (var log = new SimpleFileLogger(filePath))
            {
                log.Trace(message);
            }

            string actualContent = File.ReadAllText(filePath);
            Assert.Contains(message, actualContent);
            File.Delete(filePath);
        }

        private delegate bool FindSubstring(string substring, string text);

        private static void AssertThatEverySubstringIsContainedInDistinctLine(
            IEnumerable<string> substrings, IList<string> lines, FindSubstring predicate)
        {
            foreach (string substring in substrings)
            {
                int idx;
                for (idx = 0; idx < lines.Count; ++idx)
                {
                    if (lines[idx] == null || !predicate(substring, lines[idx]))
                    {
                        continue;
                    }

                    lines[idx] = null;
                    break;
                }

                Assert.True(idx != lines.Count, $"'{substring}' kann nicht gefunden werden!");
            }
        }

        [Fact]
        public void LogManyMessages_SingleThread()
        {
            string[] messages = With<string>.CreateArrayOf(100, (int index) => $"Nachricht #{index}");

            const string filePath = "log.txt";
            using (var log = new SimpleFileLogger(filePath))
            {
                foreach (string message in messages)
                {
                    log.Trace(message);
                }
            }

            string[] actualLines = File.ReadAllLines(filePath);
            Assert.Equal(messages.Count(), actualLines.Count());
            AssertThatEverySubstringIsContainedInDistinctLine(messages, actualLines,
                (message, line) => line.EndsWith(message));

            File.Delete(filePath);
        }

        [Fact]
        public void LogMessagesFromAllLevels_SingleThread()
        {
            const string filePath = "log.txt";
            using (var log = new SimpleFileLogger(filePath))
            {
                string message = "Nachricht";
                log.Trace(message);
                log.Debug(message);
                log.Info(message);
                log.Warning(message);
                log.Error(message);
                log.Critical(message);
            }

            string[] actualLines =
                (from line in File.ReadAllLines(filePath) select line.ToLower()).ToArray();

            var expectedLabels = new[] { "trace", "debug", "info", "warning", "error", "critical" };

            Assert.Equal(expectedLabels.Count(), actualLines.Count());
            AssertThatEverySubstringIsContainedInDistinctLine(expectedLabels, actualLines,
                (message, line) => line.Contains(message));

            File.Delete(filePath);
        }

        [Fact]
        public void LogManyMessages_Concurrently()
        {
            const int listSize = 1000;
            string[][] messageLists = new[] {
                With<string>.CreateArrayOf(listSize, (int index) => $"Nachricht #{index}"),
                With<string>.CreateArrayOf(listSize, (int index) => $"Nachricht #{index + listSize}"),
                With<string>.CreateArrayOf(listSize, (int index) => $"Nachricht #{index + 2*listSize}"),
            };

            static void LogAllMessages(ILogger log, string[] messages)
            {
                foreach (string message in messages)
                {
                    log.Trace(message);
                }
            }

            const string filePath = "log.txt";
            using (var log = new SimpleFileLogger(filePath))
            {
                Task.WaitAll((
                    from list in messageLists
                    select Task.Run(() => LogAllMessages(log, list))
                ).ToArray());
            }

            string[] actualLines = File.ReadAllLines(filePath);
            Assert.Equal(messageLists.Count() * listSize, actualLines.Count());

            AssertThatEverySubstringIsContainedInDistinctLine(
                With<string>.ConcatenateViewOf(messageLists),
                actualLines,
                (message, line) => line.EndsWith(message));

            File.Delete(filePath);
        }
    }
}