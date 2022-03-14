using System.Collections.Generic;

using Xunit;

namespace Reusable.Utils.UnitTests
{
    public class CollectionExtensionTest
    {
        [Fact]
        public void Dictionary_ChangeValue_WhenValueType_IfPresent_ThenChangeIt()
        {
            Dictionary<int, int> dictionary = new();
            int key = 1;
            int previousValue = 32;
            dictionary.Add(key, previousValue);
            Assert.True(
                dictionary.ChangeValue(key, value => {
                    Assert.Equal(previousValue, value);
                    return ++value;
                })
            );
            Assert.Equal(previousValue + 1, dictionary[key]);
        }

        [Fact]
        public void Dictionary_ChangeValue_WhenValueType_IfNotPresent_ThenAddIt()
        {
            Dictionary<int, int> dictionary = new();
            int key = 1;
            Assert.False(
                dictionary.ChangeValue(key, value => {
                    Assert.Equal(default, value);
                    return ++value;
                })
            );
            Assert.Equal(1, dictionary[key]);
        }

        [Fact]
        public void Dictionary_ChangeValue_WhenRefType_IfPresent_ThenChangeIt()
        {
            Dictionary<int, List<int>> dictionary = new();
            int key = 1;
            List<int> list = new();
            dictionary.Add(key, list);
            Assert.True(
                dictionary.ChangeValue(key, list => {
                    Assert.NotNull(list);
                    list.Add(1);
                    return list;
                })
            );
            Assert.Equal(new int[] { 1 }, dictionary[key]);
        }

        [Fact]
        public void Dictionary_ChangeValue_WhenRefType_IfNotPresent_ThenAddIt()
        {
            Dictionary<int, List<int>> dictionary = new();
            int key = 1;
            Assert.False(
                dictionary.ChangeValue(key, list => {
                    Assert.Null(list);
                    list = new List<int> { 1 };
                    return list;
                })
            );
            Assert.Equal(new int[] { 1 }, dictionary[key]);
        }
    }
}