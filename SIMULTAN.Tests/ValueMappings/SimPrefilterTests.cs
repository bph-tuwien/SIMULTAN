using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data.ValueMappings;
using SIMULTAN.Tests.TestUtils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Tests.ValueMappings
{
    [TestClass]
    public class SimPrefilterTests
    {
        [TestMethod]
        public void PrefilterDefault()
        {
            var prefilter = new SimDefaultPrefilter();
            double[] values = new double[] { 1.0, 2.5, 5.0 };

            Assert.ThrowsException<ArgumentNullException>(() => { prefilter.Filter(null, 0).ToList(); });

            AssertUtil.ContainEqualValues(new double[] { 2.5 }, prefilter.Filter(values, 1));
            //Outside
            AssertUtil.ContainEqualValues(new double[] { 0.0 }, prefilter.Filter(values, 99));
            //Negative
            Assert.ThrowsException<ArgumentOutOfRangeException>(() => { prefilter.Filter(values, -1).ToList(); });
        }

        [TestMethod]
        public void PrefilterMinimum()
        {
            var prefilter = new SimMinimumPrefilter();
            double[] values = new double[] { 1.0, 2.5, 5.0 };

            Assert.ThrowsException<ArgumentNullException>(() => { prefilter.Filter(null, 0).ToList(); });

            AssertUtil.ContainEqualValues(new double[] { 1.0 }, prefilter.Filter(values, 1));
            //Outside (doesn't matter)
            AssertUtil.ContainEqualValues(new double[] { 1.0 }, prefilter.Filter(values, 99));
        }

        [TestMethod]
        public void PrefilterMaximum()
        {
            var prefilter = new SimMaximumPrefilter();
            double[] values = new double[] { 1.0, 2.5, 5.0 };

            Assert.ThrowsException<ArgumentNullException>(() => { prefilter.Filter(null, 0).ToList(); });

            AssertUtil.ContainEqualValues(new double[] { 5.0 }, prefilter.Filter(values, 1));
            //Outside (doesn't matter)
            AssertUtil.ContainEqualValues(new double[] { 5.0 }, prefilter.Filter(values, 99));
        }

        [TestMethod]
        public void PrefilterAverage()
        {
            var prefilter = new SimAveragePrefilter();
            double[] values = new double[] { 1.0, 2.5, 5.5 };

            Assert.ThrowsException<ArgumentNullException>(() => { prefilter.Filter(null, 0).ToList(); });

            AssertUtil.ContainEqualValues(new double[] { 3.0 }, prefilter.Filter(values, 1));
            //Outside (doesn't matter)
            AssertUtil.ContainEqualValues(new double[] { 3.0 }, prefilter.Filter(values, 99));
        }
    }
}
