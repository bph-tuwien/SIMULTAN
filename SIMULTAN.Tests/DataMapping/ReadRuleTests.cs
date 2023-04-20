using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data.Components;
using SIMULTAN.DataMapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Tests.DataMapping
{
    [TestClass]
    public class ReadRuleTests
    {
        [TestMethod]
        public void CloneTest()
        {
            var param = new SimDoubleParameter("asdf", "c/d²", 1.4);

            var rule = new SimDataMappingReadRule()
            {
                Parameter = param,
                Range = new Utils.RowColumnRange(1, 2, 3, 4),
                SheetName = "Sheet B"
            };

            var clonedRule = rule.Clone();
            Assert.AreEqual(param, clonedRule.Parameter);
            Assert.AreEqual(1, clonedRule.Range.RowStart);
            Assert.AreEqual(2, clonedRule.Range.ColumnStart);
            Assert.AreEqual(3, clonedRule.Range.RowCount);
            Assert.AreEqual(4, clonedRule.Range.ColumnCount);
            Assert.AreEqual("Sheet B", clonedRule.SheetName);
        }
    }
}
