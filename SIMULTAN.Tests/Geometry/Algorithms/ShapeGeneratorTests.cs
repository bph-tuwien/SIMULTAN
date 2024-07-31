using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data.Geometry;
using SIMULTAN.Data.SimMath;
using SIMULTAN.Tests.Geometry.EventData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SIMULTAN.Tests.Geometry.Algorithms
{
    [TestClass]
    public class ShapeGeneratorTests
    {
        [TestMethod]
        public void GenerateFaceWithHoleTopology()
        {
            var empty = GeometryModelHelper.EmptyModel();

            var face = ShapeGenerator.GenerateFaceWithHole(empty.layer, new SimSize(4, 3), SimMatrix3D.Identity);

            Assert.AreEqual(8, empty.model.Geometry.Vertices.Count);
            Assert.AreEqual(8, empty.model.Geometry.Edges.Count);
            Assert.AreEqual(2, empty.model.Geometry.EdgeLoops.Count);
            Assert.AreEqual(1, empty.model.Geometry.Faces.Count);
            Assert.AreEqual(1, empty.model.Geometry.Faces[0].Holes.Count);
            Assert.AreEqual(0, empty.model.Geometry.Volumes.Count);
        }

        [TestMethod]
        public void GenerateFaceWithHoleOrientation()
        {
            var empty = GeometryModelHelper.EmptyModel();

            var face = ShapeGenerator.GenerateFaceWithHole(empty.layer, new SimSize(4, 3), Math.PI / 2.0, 0.0);

            List<SimPoint3D> expectedVertices = new List<SimPoint3D>()
            {
                new SimPoint3D(-2, 0, 0),
                new SimPoint3D(2, 0, 0),
                new SimPoint3D(2, 3, 0),
                new SimPoint3D(-2, 3, 0),
            };

            foreach (var p in expectedVertices)
            {
                Assert.IsTrue(empty.model.Geometry.Vertices.Any(x => (x.Position - p).Length < 0.001));
            }
        }

        [TestMethod]
        public void GenerateFaceWithHoleIncline()
        {
            var empty = GeometryModelHelper.EmptyModel();

            var face = ShapeGenerator.GenerateFaceWithHole(empty.layer, new SimSize(4, 3), 0.0, Math.PI / 4.0);

            List<SimPoint3D> expectedVertices = new List<SimPoint3D>()
            {
                new SimPoint3D(0, 0, -2),
                new SimPoint3D(0, 0, 2),
                new SimPoint3D(-2.1213, 2.1213, 2),
                new SimPoint3D(-2.1213, 2.1213, -2),
            };

            foreach (var p in expectedVertices)
            {
                Assert.IsTrue(empty.model.Geometry.Vertices.Any(x => (x.Position - p).Length < 0.001));
            }
        }

        [TestMethod]
        public void GenerateFaceWithHoleMixed()
        {
            var empty = GeometryModelHelper.EmptyModel();

            var face = ShapeGenerator.GenerateFaceWithHole(empty.layer, new SimSize(4, 3), Math.PI / 4.0, Math.PI / 4.0);

            List<SimPoint3D> expectedVertices = new List<SimPoint3D>()
            {
                new SimPoint3D(-1.4142, 0, -1.4142),
                new SimPoint3D(1.4142, 0, 1.4142),
                new SimPoint3D(-0.0858, 2.1213, 2.9142),
                new SimPoint3D(-2.9142, 2.1213, 0.0858),
            };

            foreach (var p in expectedVertices)
            {
                Assert.IsTrue(empty.model.Geometry.Vertices.Any(x => (x.Position - p).Length < 0.001));
            }
        }
    }
}
