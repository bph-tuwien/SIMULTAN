using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data.MultiValues;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

            var data = SimMultiValueBigTableTests.TestDataTable(3, 4);
            header.Table = data.table;
            Assert.AreEqual(data.table, header.Table);
            Assert.AreEqual(2, propertyChanged.Count);
        }

        [TestMethod]
        public void Clone()
        {
            var data = SimMultiValueBigTableTests.TestDataTable(3, 4);
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
