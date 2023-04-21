using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Tests.Util
{
    [TestClass]
    public class RowColumnRangeTests
    {
        [TestMethod]
        public void Ctor()
        {
            RowColumnRange range = new RowColumnRange();
            Assert.AreEqual(0, range.RowStart);
            Assert.AreEqual(0, range.ColumnStart);
            Assert.AreEqual(0, range.RowCount);
            Assert.AreEqual(0, range.ColumnCount);

            range = new RowColumnRange(1, 2, 3, 4);
            Assert.AreEqual(1, range.RowStart);
            Assert.AreEqual(2, range.ColumnStart);
            Assert.AreEqual(3, range.RowCount);
            Assert.AreEqual(4, range.ColumnCount);
        }

        [TestMethod]
        public void Equals()
        {
            RowColumnRange range1 = new RowColumnRange(1, 2, 3, 4);

            RowColumnRange range2 = new RowColumnRange(1, 2, 3, 4);
            Assert.IsTrue(range1 == range2);
            Assert.IsTrue(range1.Equals(range2));
            Assert.IsFalse(range1 != range2);

            range2 = new RowColumnRange(-1, 2, 3, 4);
            Assert.IsFalse(range1 == range2);
            Assert.IsFalse(range1.Equals(range2));
            Assert.IsTrue(range1 != range2);

            range2 = new RowColumnRange(1, -2, 3, 4);
            Assert.IsFalse(range1 == range2);
            Assert.IsFalse(range1.Equals(range2));
            Assert.IsTrue(range1 != range2);

            range2 = new RowColumnRange(1, 2, -3, 4);
            Assert.IsFalse(range1 == range2);
            Assert.IsFalse(range1.Equals(range2));
            Assert.IsTrue(range1 != range2);

            range2 = new RowColumnRange(1, 2, 3, -4);
            Assert.IsFalse(range1 == range2);
            Assert.IsFalse(range1.Equals(range2));
            Assert.IsTrue(range1 != range2);
        }

        [TestMethod]
        public void MergeRange()
        {
            RowColumnRange range1 = new RowColumnRange(-1, -2, 10, 4);
            RowColumnRange range2 = new RowColumnRange(0, 4, 3, 5);

            var merged = RowColumnRange.Merge(range1, range2);
            Assert.AreEqual(-1, merged.RowStart);
            Assert.AreEqual(-2, merged.ColumnStart);
            Assert.AreEqual(10, merged.RowCount);
            Assert.AreEqual(11, merged.ColumnCount);
        }

        [TestMethod]
        public void MergeIndex()
        {
            RowColumnRange range1 = new RowColumnRange(-1, -2, 10, 4);
            IntIndex2D index = new IntIndex2D(7, 4);

            var merged = RowColumnRange.Merge(range1, index);
            Assert.AreEqual(-1, merged.RowStart);
            Assert.AreEqual(-2, merged.ColumnStart);
            Assert.AreEqual(10, merged.RowCount);
            Assert.AreEqual(10, merged.ColumnCount);
        }
    }
}
