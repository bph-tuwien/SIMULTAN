
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data.Geometry;
using SIMULTAN.Data.SimMath;
using SIMULTAN.Tests.Geometry.EventData;
using SIMULTAN.Tests.TestUtils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Tests.Geometry.Algorithms
{
    [TestClass]
    public class FaceAlgorithmTests
    {
        [TestMethod]
        public void OrientationInclineNormal()
        {
            var io = FaceAlgorithms.OrientationIncline(new SimVector3D(0, 1, 0));
            AssertUtil.AssertDoubleEqual(Math.PI / 2.0, io.incline);
            AssertUtil.AssertDoubleEqual(0.0, io.orientation);

            io = FaceAlgorithms.OrientationIncline(new SimVector3D(0, -1, 0));
            AssertUtil.AssertDoubleEqual(-Math.PI / 2.0, io.incline);
            AssertUtil.AssertDoubleEqual(0.0, io.orientation);

            io = FaceAlgorithms.OrientationIncline(new SimVector3D(0, 0, 1));
            AssertUtil.AssertDoubleEqual(0.0, io.incline);
            AssertUtil.AssertDoubleEqual(0.0, io.orientation);

            io = FaceAlgorithms.OrientationIncline(new SimVector3D(0, 0, -1));
            AssertUtil.AssertDoubleEqual(0.0, io.incline);
            AssertUtil.AssertDoubleEqual(Math.PI, io.orientation);

            io = FaceAlgorithms.OrientationIncline(new SimVector3D(1, 0, 0));
            AssertUtil.AssertDoubleEqual(0.0, io.incline);
            AssertUtil.AssertDoubleEqual(Math.PI * 0.5, io.orientation);

            io = FaceAlgorithms.OrientationIncline(new SimVector3D(-1, 0, 0));
            AssertUtil.AssertDoubleEqual(0.0, io.incline);
            AssertUtil.AssertDoubleEqual(Math.PI * 1.5, io.orientation);

            var n = new SimVector3D(0, 1, 1);
            n.Normalize();
            io = FaceAlgorithms.OrientationIncline(n);
            AssertUtil.AssertDoubleEqual(Math.PI / 4.0, io.incline);
            AssertUtil.AssertDoubleEqual(0.0, io.orientation);

            n = new SimVector3D(1, 0, 1);
            n.Normalize();
            io = FaceAlgorithms.OrientationIncline(n);
            AssertUtil.AssertDoubleEqual(0.0, io.incline);
            AssertUtil.AssertDoubleEqual(Math.PI / 4.0, io.orientation);
        }

        [TestMethod]
        public void OrientationInclinePFace()
        {
            var model = GeometryModelHelper.CubeModel();

            Dictionary<string, double> expectedIncline = new Dictionary<string, double>
            {
                { "Floor", -Math.PI / 2.0 },
                { "Ceiling", Math.PI / 2.0 },
                { "WallN", 0.0 },
                { "WallE", 0.0 },
                { "WallS", 0.0 },
                { "WallW", 0.0 },
            };

            Dictionary<string, double> expectedOrientation = new Dictionary<string, double>
            {
                { "Floor", 0.0 },
                { "Ceiling", 0.0 },
                { "WallN", 0.0 },
                { "WallE", Math.PI * 0.5 },
                { "WallS", Math.PI * 1.0 },
                { "WallW", Math.PI * 1.5 },
            };

            foreach (var pface in model.Volumes.First().Faces)
            {
                var io = FaceAlgorithms.OrientationIncline(pface);
                Assert.AreEqual(expectedIncline[pface.Face.Name], io.incline);
                Assert.AreEqual(expectedOrientation[pface.Face.Name], io.orientation);
            }
        }

        #region edge rotation tests for triangulation
        [TestMethod]
        public void TestFindEdgeRotationDirectionCCW()
        {
            var model = GeometryModelHelper.EmptyModel();
            var layer = model.layer;
            var vertices = new[]
            {
                new Vertex(layer, "vertex {0}", new SimPoint3D(-1, -1, 0)),
                new Vertex(layer, "vertex {0}", new SimPoint3D(1, -1, 0)),
                new Vertex(layer, "vertex {0}", new SimPoint3D(1, 1, 0)),
                new Vertex(layer, "vertex {0}", new SimPoint3D(-1, 1, 0)),
            };
            var vertexPosLookup = vertices.ToDictionary(x => x, x => x.Position);

            var edges = new[] {
                new Edge(layer, "edge {0}", new []{ vertices[0], vertices[1] }),
                new Edge(layer, "edge {0}", new []{ vertices[1], vertices[2] }),
                new Edge(layer, "edge {0}", new []{ vertices[2], vertices[3] }),
                new Edge(layer, "edge {0}", new []{ vertices[3], vertices[0] }),
            };

            foreach (var e in edges)
            {
                var dir = FaceAlgorithms.FindEdgeInsideDirection2D(e, edges, vertexPosLookup);
                Assert.AreEqual(-1, dir);
            }
        }

        [TestMethod]
        public void TestFindEdgeRotationDirectionCW()
        {
            var model = GeometryModelHelper.EmptyModel();
            var layer = model.layer;
            var vertices = new[]
            {
                new Vertex(layer, "vertex {0}", new SimPoint3D(-1, -1, 0)),
                new Vertex(layer, "vertex {0}", new SimPoint3D(1, -1, 0)),
                new Vertex(layer, "vertex {0}", new SimPoint3D(1, 1, 0)),
                new Vertex(layer, "vertex {0}", new SimPoint3D(-1, 1, 0)),
            };
            var vertexPosLookup = vertices.ToDictionary(x => x, x => x.Position);

            var edges = new[] {
                new Edge(layer, "edge {0}", new []{ vertices[1], vertices[0] }),
                new Edge(layer, "edge {0}", new []{ vertices[2], vertices[1] }),
                new Edge(layer, "edge {0}", new []{ vertices[3], vertices[2] }),
                new Edge(layer, "edge {0}", new []{ vertices[0], vertices[3] }),
            };

            foreach (var e in edges)
            {
                var dir = FaceAlgorithms.FindEdgeInsideDirection2D(e, edges, vertexPosLookup);
                Assert.AreEqual(1, dir);
            }
        }

        [TestMethod]
        public void TestFindEdgeRotationDirectionOffsetCCW()
        {
            var model = GeometryModelHelper.EmptyModel();
            var layer = model.layer;
            var vertices = new[]
            {
                new Vertex(layer, "vertex {0}", new SimPoint3D(5, 5, 0)),
                new Vertex(layer, "vertex {0}", new SimPoint3D(6, 5, 0)),
                new Vertex(layer, "vertex {0}", new SimPoint3D(6, 6, 0)),
                new Vertex(layer, "vertex {0}", new SimPoint3D(5, 6, 0)),
            };
            var vertexPosLookup = vertices.ToDictionary(x => x, x => x.Position);

            var edges = new[] {
                new Edge(layer, "edge {0}", new []{ vertices[0], vertices[1] }),
                new Edge(layer, "edge {0}", new []{ vertices[1], vertices[2] }),
                new Edge(layer, "edge {0}", new []{ vertices[2], vertices[3] }),
                new Edge(layer, "edge {0}", new []{ vertices[3], vertices[0] }),
            };

            foreach (var e in edges)
            {
                var dir = FaceAlgorithms.FindEdgeInsideDirection2D(e, edges, vertexPosLookup);
                Assert.AreEqual(-1, dir);
            }
        }

        [TestMethod]
        public void TestFindEdgeRotationDirectionDiagonalCCW()
        {
            var model = GeometryModelHelper.EmptyModel();
            var layer = model.layer;
            var vertices = new[]
            {
                new Vertex(layer, "vertex {0}", new SimPoint3D(-1, 0, 0)),
                new Vertex(layer, "vertex {0}", new SimPoint3D(0, -1, 0)),
                new Vertex(layer, "vertex {0}", new SimPoint3D(1, 0, 0)),
                new Vertex(layer, "vertex {0}", new SimPoint3D(0, 1, 0)),
            };
            var vertexPosLookup = vertices.ToDictionary(x => x, x => x.Position);

            var edges = new[] {
                new Edge(layer, "edge {0}", new []{ vertices[0], vertices[1] }),
                new Edge(layer, "edge {0}", new []{ vertices[1], vertices[2] }),
                new Edge(layer, "edge {0}", new []{ vertices[2], vertices[3] }),
                new Edge(layer, "edge {0}", new []{ vertices[3], vertices[0] }),
            };

            foreach (var e in edges)
            {
                var dir = FaceAlgorithms.FindEdgeInsideDirection2D(e, edges, vertexPosLookup);
                Assert.AreEqual(-1, dir);
            }
        }

        [TestMethod]
        public void TestFindEdgeRotationDirectionDiagonalCW()
        {
            var model = GeometryModelHelper.EmptyModel();
            var layer = model.layer;
            var vertices = new[]
            {
                new Vertex(layer, "vertex {0}", new SimPoint3D(-1, 0, 0)),
                new Vertex(layer, "vertex {0}", new SimPoint3D(0, -1, 0)),
                new Vertex(layer, "vertex {0}", new SimPoint3D(1, 0, 0)),
                new Vertex(layer, "vertex {0}", new SimPoint3D(0, 1, 0)),
            };
            var vertexPosLookup = vertices.ToDictionary(x => x, x => x.Position);

            var edges = new[] {
                new Edge(layer, "edge {0}", new []{ vertices[1], vertices[0] }),
                new Edge(layer, "edge {0}", new []{ vertices[2], vertices[1] }),
                new Edge(layer, "edge {0}", new []{ vertices[3], vertices[2] }),
                new Edge(layer, "edge {0}", new []{ vertices[0], vertices[3] }),
            };

            foreach (var e in edges)
            {
                var dir = FaceAlgorithms.FindEdgeInsideDirection2D(e, edges, vertexPosLookup);
                Assert.AreEqual(1, dir);
            }
        }

        [TestMethod]
        public void TestFindEdgeRotationDirectionStarCCW()
        {
            var model = GeometryModelHelper.EmptyModel();
            var layer = model.layer;
            var vertices = new[]
            {
                new Vertex(layer, "vertex {0}", new SimPoint3D(-1.5, 0, 0)),
                new Vertex(layer, "vertex {0}", new SimPoint3D(-1, -1, 0)),
                new Vertex(layer, "vertex {0}", new SimPoint3D(0, -1.5, 0)),
                new Vertex(layer, "vertex {0}", new SimPoint3D(1, -1, 0)),
                new Vertex(layer, "vertex {0}", new SimPoint3D(1.5, 0, 0)),
                new Vertex(layer, "vertex {0}", new SimPoint3D(1, 1, 0)),
                new Vertex(layer, "vertex {0}", new SimPoint3D(0, 1.5, 0)),
                new Vertex(layer, "vertex {0}", new SimPoint3D(-1, 1, 0)),
            };
            var vertexPosLookup = vertices.ToDictionary(x => x, x => x.Position);

            var edges = new[] {
                // outside boundary
                new Edge(layer, "edge {0}", new []{ vertices[0], vertices[1] }),
                new Edge(layer, "edge {0}", new []{ vertices[1], vertices[2] }),
                new Edge(layer, "edge {0}", new []{ vertices[2], vertices[3] }),
                new Edge(layer, "edge {0}", new []{ vertices[3], vertices[4] }),
                new Edge(layer, "edge {0}", new []{ vertices[4], vertices[5] }),
                new Edge(layer, "edge {0}", new []{ vertices[5], vertices[6] }),
                new Edge(layer, "edge {0}", new []{ vertices[6], vertices[7] }),
                new Edge(layer, "edge {0}", new []{ vertices[7], vertices[0] }),
                // inside square
                new Edge(layer, "square edge {0}", new []{ vertices[3], vertices[1] }),
                new Edge(layer, "square edge {0}", new []{ vertices[5], vertices[3] }),
                new Edge(layer, "square edge {0}", new []{ vertices[7], vertices[5] }),
                new Edge(layer, "square edge {0}", new []{ vertices[1], vertices[7] }),
            };

            foreach (var e in edges)
            {
                var dir = FaceAlgorithms.FindEdgeInsideDirection2D(e, edges, vertexPosLookup);
                if (e.Name.StartsWith("edge"))
                {
                    Assert.AreEqual(-1, dir);
                }
                else
                {
                    Assert.AreEqual(0, dir);
                }
            }
        }

        [TestMethod]
        public void TestFindEdgeRotationDirectionStarCW()
        {
            var model = GeometryModelHelper.EmptyModel();
            var layer = model.layer;
            var vertices = new[]
            {
                new Vertex(layer, "vertex {0}", new SimPoint3D(-1.5, 0, 0)),
                new Vertex(layer, "vertex {0}", new SimPoint3D(-1, -1, 0)),
                new Vertex(layer, "vertex {0}", new SimPoint3D(0, -1.5, 0)),
                new Vertex(layer, "vertex {0}", new SimPoint3D(1, -1, 0)),
                new Vertex(layer, "vertex {0}", new SimPoint3D(1.5, 0, 0)),
                new Vertex(layer, "vertex {0}", new SimPoint3D(1, 1, 0)),
                new Vertex(layer, "vertex {0}", new SimPoint3D(0, 1.5, 0)),
                new Vertex(layer, "vertex {0}", new SimPoint3D(-1, 1, 0)),
            };
            var vertexPosLookup = vertices.ToDictionary(x => x, x => x.Position);

            var edges = new[] {
                // outside boundary
                new Edge(layer, "edge {0}", new []{ vertices[1], vertices[0] }),
                new Edge(layer, "edge {0}", new []{ vertices[2], vertices[1] }),
                new Edge(layer, "edge {0}", new []{ vertices[3], vertices[2] }),
                new Edge(layer, "edge {0}", new []{ vertices[4], vertices[3] }),
                new Edge(layer, "edge {0}", new []{ vertices[5], vertices[4] }),
                new Edge(layer, "edge {0}", new []{ vertices[6], vertices[5] }),
                new Edge(layer, "edge {0}", new []{ vertices[7], vertices[6] }),
                new Edge(layer, "edge {0}", new []{ vertices[0], vertices[7] }),
                // inside square
                new Edge(layer, "square edge {0}", new []{ vertices[1], vertices[3] }),
                new Edge(layer, "square edge {0}", new []{ vertices[3], vertices[5] }),
                new Edge(layer, "square edge {0}", new []{ vertices[5], vertices[7] }),
                new Edge(layer, "square edge {0}", new []{ vertices[7], vertices[1] }),
            };

            foreach (var e in edges)
            {
                var dir = FaceAlgorithms.FindEdgeInsideDirection2D(e, edges, vertexPosLookup);
                if (e.Name.StartsWith("edge"))
                {
                    Assert.AreEqual(1, dir);
                }
                else
                {
                    Assert.AreEqual(0, dir);
                }
            }
        }

        [TestMethod]
        public void TestFindEdgeRotationDirectionThroughVertexCCW()
        {
            var model = GeometryModelHelper.EmptyModel();
            var layer = model.layer;
            var vertices = new[]
            {
                new Vertex(layer, "vertex {0}", new SimPoint3D(-1, -1, 0)),
                new Vertex(layer, "vertex {0}", new SimPoint3D(1, -1, 0)),
                new Vertex(layer, "vertex {0}", new SimPoint3D(2, 0, 0)),
                new Vertex(layer, "vertex {0}", new SimPoint3D(1, 1, 0)),
                new Vertex(layer, "vertex {0}", new SimPoint3D(-1, 1, 0)),
            };
            var vertexPosLookup = vertices.ToDictionary(x => x, x => x.Position);

            var edges = new[] {
                new Edge(layer, "edge {0}", new []{ vertices[0], vertices[1] }),
                new Edge(layer, "edge {0}", new []{ vertices[1], vertices[2] }),
                new Edge(layer, "edge {0}", new []{ vertices[2], vertices[3] }),
                new Edge(layer, "edge {0}", new []{ vertices[3], vertices[4] }),
                new Edge(layer, "edge {0}", new []{ vertices[4], vertices[0] }),
            };

            foreach (var e in edges)
            {
                var dir = FaceAlgorithms.FindEdgeInsideDirection2D(e, edges, vertexPosLookup);
                Assert.AreEqual(-1, dir);
            }
        }

        [TestMethod]
        public void TestFindEdgeRotationDirectionThroughVertexCW()
        {
            var model = GeometryModelHelper.EmptyModel();
            var layer = model.layer;
            var vertices = new[]
            {
                new Vertex(layer, "vertex {0}", new SimPoint3D(-1, -1, 0)),
                new Vertex(layer, "vertex {0}", new SimPoint3D(1, -1, 0)),
                new Vertex(layer, "vertex {0}", new SimPoint3D(2, 0, 0)),
                new Vertex(layer, "vertex {0}", new SimPoint3D(1, 1, 0)),
                new Vertex(layer, "vertex {0}", new SimPoint3D(-1, 1, 0)),
            };
            var vertexPosLookup = vertices.ToDictionary(x => x, x => x.Position);

            var edges = new[] {
                new Edge(layer, "edge {0}", new []{ vertices[1], vertices[0] }),
                new Edge(layer, "edge {0}", new []{ vertices[2], vertices[1] }),
                new Edge(layer, "edge {0}", new []{ vertices[3], vertices[2] }),
                new Edge(layer, "edge {0}", new []{ vertices[4], vertices[3] }),
                new Edge(layer, "edge {0}", new []{ vertices[0], vertices[4] }),
            };

            foreach (var e in edges)
            {
                var dir = FaceAlgorithms.FindEdgeInsideDirection2D(e, edges, vertexPosLookup);
                Assert.AreEqual(1, dir);
            }
        }
        #endregion

        #region Outside boundary tests

        [TestMethod]
        public void TestOutsideBoundaryStarCCW()
        {
            var model = GeometryModelHelper.EmptyModel();
            var layer = model.layer;
            var vertices = new[]
            {
                new Vertex(layer, "vertex {0}", new SimPoint3D(-1.5, 0, 0)),
                new Vertex(layer, "vertex {0}", new SimPoint3D(-1, -1, 0)),
                new Vertex(layer, "vertex {0}", new SimPoint3D(0, -1.5, 0)),
                new Vertex(layer, "vertex {0}", new SimPoint3D(1, -1, 0)),
                new Vertex(layer, "vertex {0}", new SimPoint3D(1.5, 0, 0)),
                new Vertex(layer, "vertex {0}", new SimPoint3D(1, 1, 0)),
                new Vertex(layer, "vertex {0}", new SimPoint3D(0, 1.5, 0)),
                new Vertex(layer, "vertex {0}", new SimPoint3D(-1, 1, 0)),
            };
            var vertexPosLookup = vertices.ToDictionary(x => x, x => x.Position);

            var edges = new[] {
                // outside boundary
                new Edge(layer, "boundary", new []{ vertices[0], vertices[1] }),
                new Edge(layer, "boundary", new []{ vertices[1], vertices[2] }),
                new Edge(layer, "boundary", new []{ vertices[2], vertices[3] }),
                new Edge(layer, "boundary", new []{ vertices[3], vertices[4] }),
                new Edge(layer, "boundary", new []{ vertices[4], vertices[5] }),
                new Edge(layer, "boundary", new []{ vertices[5], vertices[6] }),
                new Edge(layer, "boundary", new []{ vertices[6], vertices[7] }),
                new Edge(layer, "boundary", new []{ vertices[7], vertices[0] }),
                // inside square
                new Edge(layer, "square edge {0}", new []{ vertices[3], vertices[1] }),
                new Edge(layer, "square edge {0}", new []{ vertices[5], vertices[3] }),
                new Edge(layer, "square edge {0}", new []{ vertices[7], vertices[5] }),
                new Edge(layer, "square edge {0}", new []{ vertices[1], vertices[7] }),
                new Edge(layer, "square edge {0}", new []{ vertices[5], vertices[1] }),
            };

            var boundary = FaceAlgorithms.FindOutsideBoundary2D(new SimMatrix3D(), edges.ToList());

            Assert.AreEqual(8, boundary.Count);
            foreach (var edge in boundary)
            {
                Assert.AreEqual("boundary", edge.Name);
            }
        }

        [TestMethod]
        public void TestFindOutsideBoundaryStarCW()
        {
            var model = GeometryModelHelper.EmptyModel();
            var layer = model.layer;
            var vertices = new[]
            {
                new Vertex(layer, "vertex {0}", new SimPoint3D(-1.5, 0, 0)),
                new Vertex(layer, "vertex {0}", new SimPoint3D(-1, -1, 0)),
                new Vertex(layer, "vertex {0}", new SimPoint3D(0, -1.5, 0)),
                new Vertex(layer, "vertex {0}", new SimPoint3D(1, -1, 0)),
                new Vertex(layer, "vertex {0}", new SimPoint3D(1.5, 0, 0)),
                new Vertex(layer, "vertex {0}", new SimPoint3D(1, 1, 0)),
                new Vertex(layer, "vertex {0}", new SimPoint3D(0, 1.5, 0)),
                new Vertex(layer, "vertex {0}", new SimPoint3D(-1, 1, 0)),
            };
            var vertexPosLookup = vertices.ToDictionary(x => x, x => x.Position);

            var edges = new[] {
                // outside boundary
                new Edge(layer, "boundary", new []{ vertices[1], vertices[0] }),
                new Edge(layer, "boundary", new []{ vertices[2], vertices[1] }),
                new Edge(layer, "boundary", new []{ vertices[3], vertices[2] }),
                new Edge(layer, "boundary", new []{ vertices[4], vertices[3] }),
                new Edge(layer, "boundary", new []{ vertices[5], vertices[4] }),
                new Edge(layer, "boundary", new []{ vertices[6], vertices[5] }),
                new Edge(layer, "boundary", new []{ vertices[7], vertices[6] }),
                new Edge(layer, "boundary", new []{ vertices[0], vertices[7] }),
                // inside square
                new Edge(layer, "square edge {0}", new []{ vertices[1], vertices[3] }),
                new Edge(layer, "square edge {0}", new []{ vertices[3], vertices[5] }),
                new Edge(layer, "square edge {0}", new []{ vertices[5], vertices[7] }),
                new Edge(layer, "square edge {0}", new []{ vertices[7], vertices[1] }),
                new Edge(layer, "square edge {0}", new []{ vertices[1], vertices[5] }),
            };

            var boundary = FaceAlgorithms.FindOutsideBoundary2D(new SimMatrix3D(), edges.ToList());

            Assert.AreEqual(8, boundary.Count);
            foreach (var edge in boundary)
            {
                Assert.AreEqual("boundary", edge.Name);
            }
        }

        [TestMethod]
        public void TestOutsideBoundaryStarRandomDirections()
        {
            var model = GeometryModelHelper.EmptyModel();
            var layer = model.layer;
            var vertices = new[]
            {
                new Vertex(layer, "vertex {0}", new SimPoint3D(-1.5, 0, 0)),
                new Vertex(layer, "vertex {0}", new SimPoint3D(-1, -1, 0)),
                new Vertex(layer, "vertex {0}", new SimPoint3D(0, -1.5, 0)),
                new Vertex(layer, "vertex {0}", new SimPoint3D(1, -1, 0)),
                new Vertex(layer, "vertex {0}", new SimPoint3D(1.5, 0, 0)),
                new Vertex(layer, "vertex {0}", new SimPoint3D(1, 1, 0)),
                new Vertex(layer, "vertex {0}", new SimPoint3D(0, 1.5, 0)),
                new Vertex(layer, "vertex {0}", new SimPoint3D(-1, 1, 0)),
            };
            var vertexPosLookup = vertices.ToDictionary(x => x, x => x.Position);

            var edges = new[] {
                // outside boundary
                new Edge(layer, "boundary", new []{ vertices[0], vertices[1] }),
                new Edge(layer, "boundary", new []{ vertices[2], vertices[1] }),
                new Edge(layer, "boundary", new []{ vertices[2], vertices[3] }),
                new Edge(layer, "boundary", new []{ vertices[4], vertices[3] }),
                new Edge(layer, "boundary", new []{ vertices[4], vertices[5] }),
                new Edge(layer, "boundary", new []{ vertices[6], vertices[5] }),
                new Edge(layer, "boundary", new []{ vertices[6], vertices[7] }),
                new Edge(layer, "boundary", new []{ vertices[0], vertices[7] }),
                // inside square
                new Edge(layer, "square edge {0}", new []{ vertices[3], vertices[1] }),
                new Edge(layer, "square edge {0}", new []{ vertices[3], vertices[5] }),
                new Edge(layer, "square edge {0}", new []{ vertices[7], vertices[5] }),
                new Edge(layer, "square edge {0}", new []{ vertices[7], vertices[1] }),
                new Edge(layer, "square edge {0}", new []{ vertices[5], vertices[1] }),
            };

            var boundary = FaceAlgorithms.FindOutsideBoundary2D(new SimMatrix3D(), edges.ToList());

            Assert.AreEqual(8, boundary.Count);
            foreach (var edge in boundary)
            {
                Assert.AreEqual("boundary", edge.Name);
            }
        }

        [TestMethod]
        public void TestFindOutsideBoundaryNoLoop()
        {
            var model = GeometryModelHelper.EmptyModel();
            var layer = model.layer;
            var vertices = new[]
            {
                new Vertex(layer, "vertex {0}", new SimPoint3D(-1, -1, 0)),
                new Vertex(layer, "vertex {0}", new SimPoint3D(1, -1, 0)),
                new Vertex(layer, "vertex {0}", new SimPoint3D(1, 1, 0)),
                new Vertex(layer, "vertex {0}", new SimPoint3D(-1, 1, 0)),
            };
            var vertexPosLookup = vertices.ToDictionary(x => x, x => x.Position);

            var edges = new[] {
                new Edge(layer, "edge {0}", new []{ vertices[0], vertices[1] }),
                new Edge(layer, "edge {0}", new []{ vertices[1], vertices[2] }),
                new Edge(layer, "edge {0}", new []{ vertices[2], vertices[3] }),
            };

            var boundary = FaceAlgorithms.FindOutsideBoundary2D(new SimMatrix3D(), edges.ToList());
            Assert.IsNull(boundary);

        }
        #endregion
    }
}
