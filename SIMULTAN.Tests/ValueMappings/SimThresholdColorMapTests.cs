using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data.ValueMappings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace SIMULTAN.Tests.ValueMappings
{
    [TestClass]
    public class SimThresholdColorMapTests
    {
        [TestMethod]
        public void Ctor()
        {
            var map = new SimThresholdColorMap();

            Assert.AreEqual(0, map.ColorMarkers.Count);
        }

        [TestMethod]
        public void CtorWithMarker()
        {
            Assert.ThrowsException<ArgumentNullException>(() => { new SimThresholdColorMap(null); });

            var map = new SimThresholdColorMap(new SimColorMarker[]
            {
                new SimColorMarker(1.0, Colors.Red),
                new SimColorMarker(5.0, Colors.Blue)
            });

            Assert.AreEqual(2, map.ColorMarkers.Count);

            Assert.AreEqual(map.ColorMarkers, map.ColorMarkers[0].Owner);
            Assert.AreEqual(map.ColorMarkers, map.ColorMarkers[1].Owner);
        }

        [TestMethod]
        public void AddMarker()
        {
            var map = new SimThresholdColorMap();

            map.ColorMarkers.Add(new SimColorMarker(1.0, Colors.Red));
            map.ColorMarkers.Add(new SimColorMarker(5.0, Colors.Blue));

            Assert.AreEqual(2, map.ColorMarkers.Count);

            Assert.AreEqual(map.ColorMarkers, map.ColorMarkers[0].Owner);
            Assert.AreEqual(map.ColorMarkers, map.ColorMarkers[1].Owner);
        }

        [TestMethod]
        public void RemoveMarker()
        {
            var map = new SimThresholdColorMap(new SimColorMarker[]
            {
                new SimColorMarker(1.0, Colors.Red),
                new SimColorMarker(3.0, Colors.Green),
                new SimColorMarker(5.0, Colors.Blue)
            });

            var m1 = map.ColorMarkers[1];

            map.ColorMarkers.RemoveAt(1);

            Assert.AreEqual(2, map.ColorMarkers.Count);
            Assert.AreEqual(null, m1.Owner);
        }

        [TestMethod]
        public void ClearMarker()
        {
            var map = new SimThresholdColorMap(new SimColorMarker[]
            {
                new SimColorMarker(1.0, Colors.Red),
                new SimColorMarker(3.0, Colors.Green),
                new SimColorMarker(5.0, Colors.Blue)
            });

            var m1 = map.ColorMarkers[1];

            map.ColorMarkers.Clear();

            Assert.AreEqual(0, map.ColorMarkers.Count);
            Assert.AreEqual(null, m1.Owner);
        }

        [TestMethod]
        public void Map()
        {
            var map = new SimThresholdColorMap(new SimColorMarker[]
            {
                new SimColorMarker(1.0, Colors.Red),
                new SimColorMarker(5.0, Colors.Blue)
            });

            Assert.AreEqual(Color.FromArgb(255, 255, 0, 0), map.Map(3.0));
            Assert.AreEqual(Color.FromArgb(255, 0, 0, 255), map.Map(5.0));
            Assert.AreEqual(DefaultColorMappingColors.OutOfRangeColor, map.Map(0.0));
            Assert.AreEqual(DefaultColorMappingColors.OutOfRangeColor, map.Map(6.0));
        }
    }
}
