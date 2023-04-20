using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data;
using SIMULTAN.Data.MultiValues;
using SIMULTAN.Data.ValueMappings;
using SIMULTAN.Tests.TestUtils;
using System;
using System.Collections.Generic;
using System.Windows.Media;

namespace SIMULTAN.Tests.ValueMappings
{
    [TestClass]
    public class SimValueMappingTests
    {
        internal static SimValueMapping CreateMapping()
        {
            var prefilter = new SimDefaultPrefilter();
            var colorMap = new SimLinearGradientColorMap(new List<SimColorMarker> {
                new SimColorMarker(0.0, Colors.Red),
                new SimColorMarker(5.0, Colors.Blue)
            });
            var table = new SimMultiValueBigTable("", "", "",
                new List<SimMultiValueBigTableHeader> { new SimMultiValueBigTableHeader("", "") },
                new List<SimMultiValueBigTableHeader> { new SimMultiValueBigTableHeader("", "") },
                new double[,] { { 1.0 } });

            return new SimValueMapping("mapping1", table, prefilter, colorMap);
        }




        [TestMethod]
        public void CtorExceptions()
        {
            var prefilter = new SimDefaultPrefilter();
            var colorMap = new SimLinearGradientColorMap();
            var table = new SimMultiValueBigTable("", "", "",
                new List<SimMultiValueBigTableHeader> { new SimMultiValueBigTableHeader("", "") },
                new List<SimMultiValueBigTableHeader> { new SimMultiValueBigTableHeader("", "") },
                new double[,] { { 1.0 } });

            Assert.ThrowsException<ArgumentNullException>(() => { new SimValueMapping("name", null, prefilter, colorMap); });
            Assert.ThrowsException<ArgumentNullException>(() => { new SimValueMapping("name", table, null, colorMap); });
            Assert.ThrowsException<ArgumentNullException>(() => { new SimValueMapping("name", table, prefilter, null); });
        }

        [TestMethod]
        public void ApplyMapping()
        {
            var prefilter = new SimMinimumPrefilter();
            var colorMap = new SimLinearGradientColorMap(new List<SimColorMarker>
            {
                new SimColorMarker(0.0, Colors.Red),
                new SimColorMarker(4.0, Colors.Blue),
            });
            var table = new SimMultiValueBigTable("", "", "",
                new List<SimMultiValueBigTableHeader> { new SimMultiValueBigTableHeader("", ""), new SimMultiValueBigTableHeader("", "") },
                new List<SimMultiValueBigTableHeader> { new SimMultiValueBigTableHeader("", ""), new SimMultiValueBigTableHeader("", "") },
                new double[,] { { 1.0, 2.0 }, { 3.0, 4.0 } });

            var mapping = new SimValueMapping("mapping1", table, prefilter, colorMap)
            {
                ComponentIndexUsage = SimComponentIndexUsage.Column
            };

            Assert.ThrowsException<IndexOutOfRangeException>(() => { mapping.ApplyMapping(99, 0); });

            Assert.AreEqual(Color.FromArgb(255, 63, 0, 191), mapping.ApplyMapping(1, 0));
        }

        #region Properties

        [TestMethod]
        public void PropertyColorMap()
        {
            var map = new SimThresholdColorMap();

            SimValueMapping mapping = CreateMapping();
            PropertyTestUtils.CheckProperty(mapping, nameof(SimValueMapping.ColorMap), map);
        }

        [TestMethod]
        public void PropertyComponentIndexUsage()
        {
            SimValueMapping mapping = CreateMapping();
            PropertyTestUtils.CheckProperty(mapping, nameof(SimValueMapping.ComponentIndexUsage), SimComponentIndexUsage.Column);
        }

        [TestMethod]
        public void PropertyDescription()
        {
            SimValueMapping mapping = CreateMapping();
            PropertyTestUtils.CheckProperty(mapping, nameof(SimValueMapping.Description), "otherdescription");
        }

        [TestMethod]
        public void PropertyId()
        {
            SimValueMapping mapping = CreateMapping();
            PropertyTestUtils.CheckProperty(mapping, nameof(SimValueMapping.Id), new SimId(99));
        }

        [TestMethod]
        public void PropertyName()
        {
            SimValueMapping mapping = CreateMapping();
            PropertyTestUtils.CheckProperty(mapping, nameof(SimValueMapping.Name), "othername");
        }

        [TestMethod]
        public void PropertyPrefilter()
        {
            var prefilter = new SimMinimumPrefilter();
            SimValueMapping mapping = CreateMapping();
            PropertyTestUtils.CheckProperty(mapping, nameof(SimValueMapping.Prefilter), prefilter);
        }

        [TestMethod]
        public void PropertyTable()
        {
            var otherTable = new SimMultiValueBigTable("", "", "",
                new List<SimMultiValueBigTableHeader> { new SimMultiValueBigTableHeader("", "") },
                new List<SimMultiValueBigTableHeader> { new SimMultiValueBigTableHeader("", "") },
                new double[,] { { 2.0 } });

            SimValueMapping mapping = CreateMapping();
            PropertyTestUtils.CheckProperty(mapping, nameof(SimValueMapping.Table), otherTable);
        }

        #endregion

        #region ValueMappingChanged

        [TestMethod]
        public void ValueMappingChangedPrefilterChanged()
        {
            var mapping = CreateMapping();

            int count = 0;
            mapping.ValueMappingChanged += (s) => count++;

            mapping.Prefilter = new SimAveragePrefilter();

            Assert.AreEqual(1, count);
        }

        [TestMethod]
        public void ValueMappingChangedIndexUsageChanged()
        {
            var mapping = CreateMapping();

            int count = 0;
            mapping.ValueMappingChanged += (s) => count++;

            mapping.ComponentIndexUsage = SimComponentIndexUsage.Column;

            Assert.AreEqual(1, count);
        }

        [TestMethod]
        public void ValueMappingChangedMapChanged()
        {
            var mapping = CreateMapping();

            int count = 0;
            mapping.ValueMappingChanged += (s) => count++;

            mapping.ColorMap = new SimThresholdColorMap();

            Assert.AreEqual(1, count);
        }

        [TestMethod]
        public void ValueMappingChangedMapMarkerAdded()
        {
            var mapping = CreateMapping();

            int count = 0;
            mapping.ValueMappingChanged += (s) => count++;

            ((SimLinearGradientColorMap)mapping.ColorMap).ColorMarkers.Add(new SimColorMarker(100.0, Colors.Pink));

            Assert.AreEqual(1, count);
        }

        [TestMethod]
        public void ValueMappingChangedMapMarkerRemoved()
        {
            var mapping = CreateMapping();

            int count = 0;
            mapping.ValueMappingChanged += (s) => count++;

            ((SimLinearGradientColorMap)mapping.ColorMap).ColorMarkers.RemoveAt(0);

            Assert.AreEqual(1, count);
        }


        [TestMethod]
        public void ValueMappingChangedMapMarkerValue()
        {
            var mapping = CreateMapping();

            int count = 0;
            mapping.ValueMappingChanged += (s) => count++;

            ((SimLinearGradientColorMap)mapping.ColorMap).ColorMarkers[0].Value = -5.0;

            Assert.AreEqual(1, count);
        }

        [TestMethod]
        public void ValueMappingChangedMapMarkerColorChanged()
        {
            var mapping = CreateMapping();

            int count = 0;
            mapping.ValueMappingChanged += (s) => count++;

            ((SimLinearGradientColorMap)mapping.ColorMap).ColorMarkers[0].Color = Colors.Pink;

            Assert.AreEqual(1, count);
        }



        [TestMethod]
        public void CheckDifferentParamTypeMapping()
        {
            var mapping = CreateMapping();

            int count = 0;
            mapping.ValueMappingChanged += (s) => count++;

            ((SimLinearGradientColorMap)mapping.ColorMap).ColorMarkers.RemoveAt(0);

            Assert.AreEqual(1, count);
        }

        #endregion
    }
}
