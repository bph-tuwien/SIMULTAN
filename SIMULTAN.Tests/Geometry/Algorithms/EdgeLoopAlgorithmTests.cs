using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data.Geometry;
using SIMULTAN.Data.SimMath;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Tests.Geometry.Algorithms
{
    [TestClass]
    public class EdgeLoopAlgorithmTests
    {
        [TestMethod]
        public void TestContains2D()
        {
            // Not numerically stable, so may give incorrect results at boudnary
            /*
            var tolerance = 0.1;
            var zTolerance = double.MaxValue;

            var polygon = new List<SimPoint3D>
            {
                new SimPoint3D(0,0,0),
                new SimPoint3D(10,0,0),
                new SimPoint3D(10,-10,0),
                new SimPoint3D(0,-10,0),
            };

            // points on the edges of the outline
            var center = new SimPoint3D(5, -5, 0);
            var p1 = new SimPoint3D(5, 0, 0);
            var p2 = new SimPoint3D(10, -5, 0);
            var p3 = new SimPoint3D(5, -10, 0);
            var p4 = new SimPoint3D(0, -5, 0);

            var ic = EdgeLoopAlgorithms.Contains2D(polygon, center, tolerance, zTolerance);
            var i1 = EdgeLoopAlgorithms.Contains2D(polygon, p1, tolerance, zTolerance);
            var i2 = EdgeLoopAlgorithms.Contains2D(polygon, p2, tolerance, zTolerance);
            var i3 = EdgeLoopAlgorithms.Contains2D(polygon, p3, tolerance, zTolerance);
            var i4 = EdgeLoopAlgorithms.Contains2D(polygon, p4, tolerance, zTolerance);

            Assert.IsTrue(ic);
            Assert.IsTrue(i1);
            Assert.IsTrue(i2);
            Assert.IsTrue(i3);
            Assert.IsTrue(i4);
            */
        }
    }
}
