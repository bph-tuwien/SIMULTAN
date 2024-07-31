using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data.SimMath;
using SIMULTAN.Data.ValueMappings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace SIMULTAN.Tests.ValueMappings
{
    [TestClass]
    public class SimLinearGradientColorMapTests
    {
        [TestMethod]
        public void Ctor()
        {
            var map = new SimLinearGradientColorMap();

            Assert.AreEqual(0, map.ColorMarkers.Count);
        }

        [TestMethod]
        public void CtorWithMarker()
        {
            Assert.ThrowsException<ArgumentNullException>(() => { new SimLinearGradientColorMap(null); });

            var map = new SimLinearGradientColorMap(new SimColorMarker[]
            {
                new SimColorMarker(1.0, SimColors.Red),
                new SimColorMarker(5.0, SimColors.Blue)
            });

            Assert.AreEqual(2, map.ColorMarkers.Count);

            Assert.AreEqual(map.ColorMarkers, map.ColorMarkers[0].Owner);
            Assert.AreEqual(map.ColorMarkers, map.ColorMarkers[1].Owner);
        }

        [TestMethod]
        public void AddMarker()
        {
            var map = new SimLinearGradientColorMap();

            map.ColorMarkers.Add(new SimColorMarker(1.0, SimColors.Red));
            map.ColorMarkers.Add(new SimColorMarker(5.0, SimColors.Blue));

            Assert.AreEqual(2, map.ColorMarkers.Count);

            Assert.AreEqual(map.ColorMarkers, map.ColorMarkers[0].Owner);
            Assert.AreEqual(map.ColorMarkers, map.ColorMarkers[1].Owner);
        }

        [TestMethod]
        public void RemoveMarker()
        {
            var map = new SimLinearGradientColorMap(new SimColorMarker[]
            {
                new SimColorMarker(1.0, SimColors.Red),
                new SimColorMarker(3.0, SimColors.Green),
                new SimColorMarker(5.0, SimColors.Blue)
            });

            var m1 = map.ColorMarkers[1];

            map.ColorMarkers.RemoveAt(1);

            Assert.AreEqual(2, map.ColorMarkers.Count);
            Assert.AreEqual(null, m1.Owner);
        }

        [TestMethod]
        public void ClearMarker()
        {
            var map = new SimLinearGradientColorMap(new SimColorMarker[]
            {
                new SimColorMarker(1.0, SimColors.Red),
                new SimColorMarker(3.0, SimColors.Green),
                new SimColorMarker(5.0, SimColors.Blue)
            });

            var m1 = map.ColorMarkers[1];

            map.ColorMarkers.Clear();

            Assert.AreEqual(0, map.ColorMarkers.Count);
            Assert.AreEqual(null, m1.Owner);
        }

        [TestMethod]
        public void Map()
        {
            var map = new SimLinearGradientColorMap(new SimColorMarker[]
            {
                new SimColorMarker(1.0, SimColors.Red),
                new SimColorMarker(5.0, SimColors.Blue)
            });

            Assert.AreEqual(SimColor.FromArgb(255, 127, 0, 127), map.Map(3.0));
            Assert.AreEqual(DefaultColorMappingColors.OutOfRangeColor, map.Map(0.0));
            Assert.AreEqual(DefaultColorMappingColors.OutOfRangeColor, map.Map(6.0));
        }
    }
}
