using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data.MultiValues;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SIMULTAN.Tests.Values
{
    [TestClass]
    public class SimMultiValueBigTableHeaderTests
    {
        [TestMethod]
        public void Ctor()
        {
            Assert.ThrowsException<ArgumentNullException>(() => { new SimMultiValueBigTableHeader(null, "unit"); });
            Assert.ThrowsException<ArgumentNullException>(() => { new SimMultiValueBigTableHeader("name", null); });

            var header = new SimMultiValueBigTableHeader("name", "unit");
            Assert.AreEqual("name", header.Name);
            Assert.AreEqual("unit", header.Unit);
            Assert.AreEqual(-1, header.Index);
            Assert.AreEqual(null, header.Table);
            Assert.AreEqual(SimMultiValueBigTableHeader.AxisEnum.Undefined, header.Axis);
        }

        [TestMethod]
        public void PropertyChanged()
        {
            var header = new SimMultiValueBigTableHeader("name", "unit");
            List<string> propertyChanged = new List<string>();
            header.PropertyChanged += (s, o) => propertyChanged.Add(o.PropertyName);

            header.Name = "newname";
            Assert.AreEqual("newname", header.Name);
            Assert.AreEqual(1, propertyChanged.Count);
            Assert.AreEqual(nameof(SimMultiValueBigTableHeader.Name), propertyChanged.Last());

            header.Unit = "newunit";
            Assert.AreEqual("newunit", header.Unit);
            Assert.AreEqual(2, propertyChanged.Count);
            Assert.AreEqual(nameof(SimMultiValueBigTableHeader.Unit), propertyChanged.Last());

            header.Index = 99;
            Assert.AreEqual(99, header.Index);
            Assert.AreEqual(2, propertyChanged.Count);

            header.Axis = SimMultiValueBigTableHeader.AxisEnum.Rows;
            Assert.AreEqual(SimMultiValueBigTableHeader.AxisEnum.Rows, header.Axis);
            Assert.AreEqual(2, propertyChanged.Count);

            var dTable = SimMultiValueBigTableTests.DoubleTestDataTable(3, 4);
            var iTable = SimMultiValueBigTableTests.IntTestDataTable(3, 4);
            var sTable = SimMultiValueBigTableTests.StringTestDataTable(3, 4);
            var boolTable = SimMultiValueBigTableTests.BoolTestDataTable(3, 4);



            header.Table = dTable.table;
            Assert.AreEqual(dTable.table, header.Table);

            header.Table = iTable.table;
            Assert.AreEqual(iTable.table, header.Table);

            header.Table = sTable.table;
            Assert.AreEqual(sTable.table, header.Table);

            header.Table = boolTable.table;
            Assert.AreEqual(boolTable.table, header.Table);

            Assert.AreEqual(2, propertyChanged.Count);
        }

        [TestMethod]
        public void Clone()
        {
            var data = SimMultiValueBigTableTests.DoubleTestDataTable(3, 4);
            var header = new SimMultiValueBigTableHeader("name", "unit")
            {
                Index = 99,
                Axis = SimMultiValueBigTableHeader.AxisEnum.Columns,
                Table = data.table
            };

            var clone = header.Clone();
            Assert.AreEqual("name", clone.Name);
            Assert.AreEqual("unit", clone.Unit);
            Assert.AreEqual(-1, clone.Index);
            Assert.AreEqual(null, clone.Table);
            Assert.AreEqual(SimMultiValueBigTableHeader.AxisEnum.Undefined, clone.Axis);
        }
    }
}
