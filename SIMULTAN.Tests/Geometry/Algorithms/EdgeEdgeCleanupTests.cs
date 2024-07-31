using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data.Geometry;
using SIMULTAN.Data.SimMath;
using SIMULTAN.Tests.TestUtils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Tests.Geometry.Algorithms
{
    [TestClass]
    public class EdgeEdgeCleanupTests : BaseProjectTest
    {
        private static readonly FileInfo testProject = new FileInfo(@"./CleanupTests.simultan");

        [TestMethod]
        public void EdgeEdgeSinglePoint()
        {
            LoadProject(testProject);

            (var gm, var resource) = ProjectUtils.LoadGeometry("EdgeEdgeSplit1.simgeo", projectData, sp);

            var originalVertices = gm.Geometry.Vertices.ToList();
            var originalEdges = gm.Geometry.Edges.ToList();

            AABBGrid edgeGrid= null;
            ModelCleanupAlgorithms.SplitEdgeEdgeIntersections(gm.Geometry, 0.01, ref edgeGrid);

            Assert.AreEqual(5, gm.Geometry.Vertices.Count);
            Assert.AreEqual(4, gm.Geometry.Edges.Count);

            //Vertex positions
            var newVertex = gm.Geometry.Vertices.First(x => !originalVertices.Contains(x));
            Assert.AreEqual(new SimPoint3D(5, 0, 0), newVertex.Position);

            //Edges
            Assert.IsTrue(gm.Geometry.Edges.All(x => !originalEdges.Contains(x)));
            foreach (var edge in gm.Geometry.Edges)
            {
                Assert.IsTrue(edge.Vertices.Any(x => originalVertices.Contains(x)));
                Assert.IsTrue(edge.Vertices.Any(x => x == newVertex));
            }
        }
    }
}