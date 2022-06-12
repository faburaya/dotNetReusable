using System;
using System.Text.RegularExpressions;

using Xunit;

namespace Reusable.Utils.UnitTests
{
    public class SubstringRefTest
    {
        [Fact]
        public void Construction_WhenParamRegexCapture_ThenReferToMatch()
        {
            string sub = "Wort";
            string str = $" {sub} ";
            Match match = Regex.Match(str, @"(\w+)");
            Assert.True(match.Success);
            SubstringRef obj = new(match, str);
            Assert.Equal(1, obj.start);
            Assert.Equal(sub.Length, obj.length);
        }

        [Fact]
        public void AsSpan_ThenReferToSubstring()
        {
            string sub = "Wort";
            string str = $" {sub} ";
            SubstringRef obj = new(str, 1, sub.Length);
            ReadOnlySpan<char> span = obj.AsSpan();
            Assert.Equal(sub, span.ToString());
        }

        [Fact]
        public void ToString_ThenReferToSubstring()
        {
            string sub = "Wort";
            string str = $" {sub} ";
            SubstringRef obj = new(str, 1, sub.Length);
            Assert.Equal(sub, obj.ToString());
        }

        [Fact]
        public void CompareTo_WhenSameSource()
        {
            string[] subs = new[]{ "Wort1", "Wort2" };
            string str = $"-{subs[0]}-{subs[1]}-{subs[1]}-";

            SubstringRef obj1 = new(str, 1, subs[0].Length);
            SubstringRef obj2 = new(str, 2 + subs[0].Length, subs[1].Length);
            SubstringRef obj3 = new(str, 3 + subs[0].Length + subs[1].Length, subs[1].Length);

            Assert.Equal(0, obj1.CompareTo(obj1));
            Assert.Equal(0, obj2.CompareTo(obj2));
            Assert.Equal(0, obj2.CompareTo(obj3));

            Assert.True(obj1.CompareTo(obj2) < 0);
            Assert.True(obj2.CompareTo(obj1) > 0);
        }

        [Fact]
        public void CompareTo_WhenSameSource_IfComparisonNotOrdinal()
        {
            string[] subs = new[] { "w1", "W1", "W2" };
            string str = $"-{subs[0]}-{subs[1]}-{subs[2]}-";

            SubstringRef obj1 = new(str, 1, subs[0].Length);
            SubstringRef obj2 = new(str, 2 + subs[0].Length, subs[1].Length);
            SubstringRef obj3 = new(str, 3 + subs[0].Length + subs[1].Length, subs[2].Length);

            const StringComparison c = StringComparison.InvariantCultureIgnoreCase;
            Assert.Equal(0, obj1.CompareTo(obj1, c));
            Assert.Equal(0, obj2.CompareTo(obj2, c));
            Assert.Equal(0, obj3.CompareTo(obj3, c));
            Assert.Equal(0, obj1.CompareTo(obj2, c));
            Assert.Equal(0, obj2.CompareTo(obj1, c));

            Assert.True(obj1.CompareTo(obj3, c) < 0);
            Assert.True(obj3.CompareTo(obj1, c) > 0);
            Assert.True(obj2.CompareTo(obj3, c) < 0);
            Assert.True(obj3.CompareTo(obj2, c) > 0);
        }

        [Fact]
        public void CompareTo_WhenOtherSource()
        {
            string[] subs = new[] { "Wort1", "Wort2" };
            string[] strs = new[] { $"-{subs[0]}-{subs[1]}-", $"-{subs[0]}-{subs[1]}-" };

            SubstringRef obj1 = new(strs[0], 1, subs[0].Length);
            SubstringRef obj2 = new(strs[0], 2 + subs[0].Length, subs[1].Length);
            SubstringRef obj3 = new(strs[1], 1, subs[0].Length);
            SubstringRef obj4 = new(strs[1], 2 + subs[0].Length, subs[1].Length);

            Assert.Equal(0, obj1.CompareTo(obj3));
            Assert.Equal(0, obj2.CompareTo(obj4));

            Assert.True(obj1.CompareTo(obj4) < 0);
            Assert.True(obj4.CompareTo(obj1) > 0);
            Assert.True(obj2.CompareTo(obj3) > 0);
            Assert.True(obj3.CompareTo(obj2) < 0);
        }

        [Fact]
        public void CompareTo_WhenOtherSource_IfComparisonNotOrdinal()
        {
            string[] subs = new[] { "w1", "W1", "W2" };
            string[] strs = new[] { $"-{subs[0]}-{subs[1]}-{subs[2]}-", $"-{subs[0]}-{subs[1]}-{subs[2]}-" };

            SubstringRef obj1 = new(strs[0], 1, subs[0].Length);
            SubstringRef obj2 = new(strs[0], 2 + subs[0].Length, subs[1].Length);
            SubstringRef obj3 = new(strs[0], 3 + subs[0].Length + subs[1].Length, subs[2].Length);
            SubstringRef obj4 = new(strs[1], 1, subs[0].Length);
            SubstringRef obj5 = new(strs[1], 2 + subs[0].Length, subs[1].Length);
            SubstringRef obj6 = new(strs[1], 3 + subs[0].Length + subs[1].Length, subs[2].Length);

            const StringComparison c = StringComparison.InvariantCultureIgnoreCase;
            Assert.Equal(0, obj1.CompareTo(obj4, c));
            Assert.Equal(0, obj4.CompareTo(obj1, c));
            Assert.Equal(0, obj1.CompareTo(obj5, c));
            Assert.Equal(0, obj5.CompareTo(obj1, c));

            Assert.True(obj1.CompareTo(obj6, c) < 0);
            Assert.True(obj6.CompareTo(obj1, c) > 0);
            Assert.True(obj2.CompareTo(obj6, c) < 0);
            Assert.True(obj6.CompareTo(obj2, c) > 0);
            Assert.True(obj3.CompareTo(obj4, c) > 0);
            Assert.True(obj4.CompareTo(obj3, c) < 0);
        }
    }
}
