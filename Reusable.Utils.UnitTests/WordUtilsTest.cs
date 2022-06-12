using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Xunit;

namespace Reusable.Utils.UnitTests
{
    public class WordUtilsTest
    {
        [Fact]
        public void FindEndOfWord_WhenWithinWord_ThenFindEnd()
        {
            const string text = "Eine kurze Geschichte: Ende";
            Assert.Equal(10, WordUtils.FindEndOfWord(text, 7));
            Assert.Equal(21, WordUtils.FindEndOfWord(text, 15));
            Assert.Equal(text.Length, WordUtils.FindEndOfWord(text, 24));
        }

        [Fact]
        public void FindEndOfWord_WhenOutOfWord_ThenReturnSameIndex()
        {
            const string text = "Eine kurze Geschichte: Ende";
            Assert.Equal(4, WordUtils.FindEndOfWord(text, 4));
            Assert.Equal(21, WordUtils.FindEndOfWord(text, 21));
            Assert.Equal(22, WordUtils.FindEndOfWord(text, 22));
        }

        [Fact]
        public void FindStartOfWord_WhenWithinWord_ThenFindStart()
        {
            const string text = "Arbeiter&Bourgeoisie kämpfen.";
            Assert.Equal(0, WordUtils.FindStartOfWord(text, 3));
            Assert.Equal(9, WordUtils.FindStartOfWord(text, 12));
            Assert.Equal(21, WordUtils.FindStartOfWord(text, 24));
        }

        [Fact]
        public void FindStartOfWord_WhenOutOfWord_ThenReturnSameIndex()
        {
            const string text = "Hallo! Tchüss.";
            Assert.Equal(5, WordUtils.FindStartOfWord(text, 5));
            Assert.Equal(6, WordUtils.FindStartOfWord(text, 6));
            Assert.Equal(13, WordUtils.FindStartOfWord(text, 13));
        }

        [Fact]
        public void FindClosestStartOrEndOfWord_WhenWithinWord_ThenFindStartOrEnd()
        {
            const string text = "abcd, abcd";
            Assert.Equal(0, WordUtils.FindClosestStartOrEndOfWord(text, 1));
            Assert.Equal(4, WordUtils.FindClosestStartOrEndOfWord(text, 2));
            Assert.Equal(6, WordUtils.FindClosestStartOrEndOfWord(text, 7));
            Assert.Equal(10, WordUtils.FindClosestStartOrEndOfWord(text, 8));
        }

        [Fact]
        public void FindClosestStartOrEndOfWord_WhenOutOfWord_ThenReturnSameIndex()
        {
            const string text = "abcd, abcd";
            Assert.Equal(4, WordUtils.FindClosestStartOrEndOfWord(text, 4));
            Assert.Equal(5, WordUtils.FindClosestStartOrEndOfWord(text, 5));
        }

        [Fact]
        public void SplitIntoWords_WhenRange_ThenGiveWordsInRange()
        {
            const string text = "Noch mal: nichts Neues";
            Assert.Equal(new[] { "mal", "nichts", "Neues" }, WordUtils.SplitIntoWords(text, 4, text.Length));
            Assert.Equal(new[] { "Noch", "mal", "nichts" }, WordUtils.SplitIntoWords(text, 0, 16));
            Assert.Equal(new[] { "mal", "nichts" }, WordUtils.SplitIntoWords(text, 4, 16));
        }

        [Fact]
        public void SplitIntoWordsAsRefs_WhenRange_ThenGiveWordsInRange()
        {
            const string text = "Noch mal: nichts Neues";

            SubstringRef[] refs = new[] {
                new SubstringRef(text, 0, 4),
                new SubstringRef(text, 5, 3),
                new SubstringRef(text, 10, 6),
                new SubstringRef(text, 17, 5),
            };
            Assert.Equal(refs.Skip(1).Take(3), WordUtils.SplitIntoWordsAsRefs(text, 4, text.Length));
            Assert.Equal(refs.Take(3), WordUtils.SplitIntoWordsAsRefs(text, 0, 16));
            Assert.Equal(refs.Skip(1).Take(2), WordUtils.SplitIntoWordsAsRefs(text, 4, 16));
        }
    }
}
