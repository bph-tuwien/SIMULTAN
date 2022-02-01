using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data.Geometry;
using SIMULTAN.Tests.Geometry.EventData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Tests.Geometry.Algorithms
{
    [TestClass]
    public class BaseGeometryAlgorithmsTests
    {
        [TestMethod]
        public void GetVertexVertices()
        {
            var gm = GeometryModelHelper.CubeModel();

            var v = BaseGeometryAlgorithms.GetVertices(gm.Vertices[1]);

            Assert.AreEqual(1, v.Count);
            Assert.AreEqual(gm.Vertices[1], v[0]);
        }

        [TestMethod]
        public void GetEdgeVertices()
        {
            var gm = GeometryModelHelper.CubeModel();

            var v = BaseGeometryAlgorithms.GetVertices(gm.Edges[1]);

            Assert.AreEqual(2, v.Count);
            Assert.IsTrue(v.Contains(gm.Edges[1].Vertices[0]));
            Assert.IsTrue(v.Contains(gm.Edges[1].Vertices[1]));
        }

        [TestMethod]
        public void GetFaceVertices()
        {
            var gm = GeometryModelHelper.CubeModel();

            var v = BaseGeometryAlgorithms.GetVertices(gm.Faces[0]);

            Assert.AreEqual(4, v.Count);
            for (int i = 0; i < 4; ++i)
                Assert.IsTrue(v.Contains(gm.Vertices[i]));
        }

        [TestMethod]
        public void GetVolumeVertices()
        {
            var gm = GeometryModelHelper.CubeModel();

            var v = BaseGeometryAlgorithms.GetVertices(gm.Volumes[0]);

            Assert.AreEqual(8, v.Count);
            for (int i = 0; i < 8; ++i)
                Assert.IsTrue(v.Contains(gm.Vertices[i]));
        }

        [TestMethod]
        public void GetUnsupportedVertices()
        {
            var gm = GeometryModelHelper.CubeModel();

            var v = BaseGeometryAlgorithms.GetVertices(gm.EdgeLoops[0]);

            Assert.AreEqual(0, v.Count);
        }
    }
}
