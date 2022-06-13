using System;

using Xunit;

namespace Reusable.Utils.UnitTests
{
    public class BinarySearchExtensionTest
    {
        [Fact]
        public void SearchLowerBoundIndex_WhenListEmpty_ThenZero()
        {
            var emptyList = new int[0];
            Assert.Equal(0, emptyList.SearchLowerBoundIndex(key: 1, x => x));
        }

        [Fact]
        public void SearchLowerBoundIndex_WhenKeyTooLow_ThenGiveZero()
        {
            var list = new int[] { 1, 2, 3, 4, 5 };
            Assert.Equal(0, list.SearchLowerBoundIndex(key: 0, x => x));
        }

        [Fact]
        public void SearchLowerBoundIndex_WhenKeyTooHigh_ThenGiveCount()
        {
            var list = new int[] { 1, 2, 3, 4, 5 };
            Assert.Equal(list.Length, list.SearchLowerBoundIndex(key: 9, x => x));
        }

        [Fact]
        public void SearchLowerBoundIndex_WhenKeyPresent_ThenGiveItsIndex()
        {
            var list = new int[] { 1, 2, 3, 4, 5 };
            Assert.Equal(0, list.SearchLowerBoundIndex(key: 1, x => x));
            Assert.Equal(2, list.SearchLowerBoundIndex(key: 3, x => x));
            Assert.Equal(4, list.SearchLowerBoundIndex(key: 5, x => x));
        }

        [Fact]
        public void SearchLowerBoundIndex_WhenKeyRepeats_ThenGiveIndexFirstOccurrence()
        {
            var list = new int[] { 1, 2, 3, 3, 3, 4, 5 };
            Assert.Equal(2, list.SearchLowerBoundIndex(key: 3, x => x));
        }

        [Fact]
        public void SearchLowerBoundIndex_WhenDescendingOrder_ThenLowerBoundStillHasLowerIndex()
        {
            static int compare(int a, int b) => -1 * a.CompareTo(b);
            var list = new int[] { 5, 4, 3, 3, 3, 2, 1 };
            Assert.Equal(2, list.SearchLowerBoundIndex(key: 3, x => x, compare));
            Assert.Equal(5, list.SearchLowerBoundIndex(key: 2, x => x, compare));
        }

        [Fact]
        public void SearchUpperBoundIndex_WhenListEmpty_ThenZero()
        {
            var emptyList = new int[0];
            Assert.Equal(0, emptyList.SearchUpperBoundIndex(key: 1, x => x));
        }

        [Fact]
        public void SearchUpperBoundIndex_WhenKeyTooLow_ThenGiveZero()
        {
            var list = new int[] { 1, 2, 3, 4, 5 };
            Assert.Equal(0, list.SearchUpperBoundIndex(key: 0, x => x));
        }

        [Fact]
        public void SearchUpperBoundIndex_WhenKeyTooHigh_ThenGiveCount()
        {
            var list = new int[] { 1, 2, 3, 4, 5 };
            Assert.Equal(list.Length, list.SearchUpperBoundIndex(key: 9, x => x));
        }

        [Fact]
        public void SearchUpperBoundIndex_WhenKeyPresent_ThenGiveNextIndex()
        {
            var list = new int[] { 1, 2, 3, 4, 5 };
            Assert.Equal(1, list.SearchUpperBoundIndex(key: 1, x => x));
            Assert.Equal(3, list.SearchUpperBoundIndex(key: 3, x => x));
            Assert.Equal(5, list.SearchUpperBoundIndex(key: 5, x => x));
        }

        [Fact]
        public void SearchUpperBoundIndex_WhenKeyRepeats_ThenGiveIndexPastLastOccurrence()
        {
            var list = new int[] { 1, 2, 3, 3, 3, 3, 4, 5 };
            Assert.Equal(6, list.SearchUpperBoundIndex(key: 3, x => x));
        }

        [Fact]
        public void SearchUpperBoundIndex_WhenDescendingOrder_ThenUpperBoundStillHasHigherIndex()
        {
            static int compare(int a, int b) => -1 * a.CompareTo(b);
            var list = new int[] { 5, 4, 3, 3, 3, 3, 2, 1 };
            Assert.Equal(6, list.SearchUpperBoundIndex(key: 3, x => x, compare));
            Assert.Equal(7, list.SearchUpperBoundIndex(key: 2, x => x, compare));
        }
    }
}