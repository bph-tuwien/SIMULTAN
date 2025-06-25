using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data.Geometry;
using SIMULTAN.Data.SimMath;
using SIMULTAN.Tests.Geometry.EventData;
using SIMULTAN.Tests.TestUtils;
using SIMULTAN.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Tests.Geometry.Algorithms
{
    [TestClass]
    public class EdgeAlgorithmTests
    {
        private static (GeometryModel model, Layer layer) OrderLoopTestData()
        {
            var gm = GeometryModelHelper.EmptyModel();
            var g = gm.model.Geometry;

            new Vertex(gm.layer, "", new SimPoint3D(0, 0, 0));
            new Vertex(gm.layer, "", new SimPoint3D(5, 0, 0));
            new Vertex(gm.layer, "", new SimPoint3D(5, 5, 0));
            new Vertex(gm.layer, "", new SimPoint3D(2.5, 5, 0));
            new Vertex(gm.layer, "", new SimPoint3D(0, 5, 0));

            new Edge(gm.layer, "", new Vertex[] { g.Vertices[0], g.Vertices[1] });
            new Edge(gm.layer, "", new Vertex[] { g.Vertices[2], g.Vertices[1] });
            new Edge(gm.layer, "", new Vertex[] { g.Vertices[2], g.Vertices[3] });
            new Edge(gm.layer, "", new Vertex[] { g.Vertices[3], g.Vertices[4] });
            new Edge(gm.layer, "", new Vertex[] { g.Vertices[4], g.Vertices[0] });

            g.EdgeLoops.Add(new EdgeLoop(gm.layer, "", g.Edges));

            return gm;
        }
        private static (GeometryModel model, Layer layer) OrderLoopIntersectTestData()
        {
            var gm = GeometryModelHelper.EmptyModel();
            var g = gm.model.Geometry;

            new Vertex(gm.layer, "", new SimPoint3D(0, 0, 0));
            new Vertex(gm.layer, "", new SimPoint3D(5, 5, 0));
            new Vertex(gm.layer, "", new SimPoint3D(5, -5, 0));
            new Vertex(gm.layer, "", new SimPoint3D(-5, -5, 0));
            new Vertex(gm.layer, "", new SimPoint3D(-5, 5, 0));

            new Edge(gm.layer, "", new Vertex[] { g.Vertices[0], g.Vertices[1] });
            new Edge(gm.layer, "", new Vertex[] { g.Vertices[1], g.Vertices[2] });
            new Edge(gm.layer, "", new Vertex[] { g.Vertices[2], g.Vertices[0] });
            new Edge(gm.layer, "", new Vertex[] { g.Vertices[0], g.Vertices[3] });
            new Edge(gm.layer, "", new Vertex[] { g.Vertices[3], g.Vertices[4] });
            new Edge(gm.layer, "", new Vertex[] { g.Vertices[4], g.Vertices[0] });

            return gm;
        }


        [TestMethod]
        public void OrderLoopExceptions()
        {
            var data = OrderLoopTestData();

            Assert.ThrowsException<ArgumentNullException>(() => { EdgeAlgorithms.OrderLoop((List<Edge>)null); });
            Assert.ThrowsException<ArgumentException>(() => { EdgeAlgorithms.OrderLoop(new List<Edge>()); });

            Assert.ThrowsException<ArgumentNullException>(() => { EdgeAlgorithms.OrderLoop((List<PEdge>)null); });
            Assert.ThrowsException<ArgumentException>(() => { EdgeAlgorithms.OrderLoop(new List<PEdge>()); });

            Assert.ThrowsException<ArgumentNullException>(() =>
            {
                EdgeAlgorithms.OrderLoop((IEnumerable<Edge>)null, data.model.Geometry.Edges[0],
                GeometricOrientation.Forward);
            });
            Assert.ThrowsException<ArgumentNullException>(() =>
            {
                EdgeAlgorithms.OrderLoop(data.model.Geometry.EdgeLoops[0].Edges,
                null, GeometricOrientation.Forward);
            });
            Assert.ThrowsException<ArgumentException>(() =>
            {
                EdgeAlgorithms.OrderLoop(data.model.Geometry.EdgeLoops[0].Edges,
                    data.model.Geometry.Edges[0], GeometricOrientation.Undefined);
            });
        }

        [TestMethod]
        public void OrderLoopEdges()
        {
            var data = OrderLoopTestData();
            var gd = data.model.Geometry;

            var loop = new Edge[] {
                gd.Edges[0], gd.Edges[2], gd.Edges[1], gd.Edges[3], gd.Edges[4]
            };
            var ordered = new Edge[] {
                gd.Edges[0], gd.Edges[1], gd.Edges[2], gd.Edges[3], gd.Edges[4]
            };

            var or = EdgeAlgorithms.OrderLoop(loop);

            Assert.IsTrue(or.isLoop);
            AssertUtil.ContainEqualValues(ordered, or.loop);

            for (int i = 0; i < loop.Length; ++i)
            {
                or = EdgeAlgorithms.OrderLoop(loop, loop[i], GeometricOrientation.Forward);
                Assert.IsTrue(or.isLoop);
                or = EdgeAlgorithms.OrderLoop(loop, loop[i], GeometricOrientation.Backward);
                Assert.IsTrue(or.isLoop);
                or = EdgeAlgorithms.OrderLoop(loop, loop[i], GeometricOrientation.Undefined);
                Assert.IsTrue(or.isLoop);
            }

            loop = loop.Reverse().ToArray();
            for (int i = 0; i < loop.Length; ++i)
            {
                or = EdgeAlgorithms.OrderLoop(loop, loop[i], GeometricOrientation.Forward);
                Assert.IsTrue(or.isLoop);
                or = EdgeAlgorithms.OrderLoop(loop, loop[i], GeometricOrientation.Backward);
                Assert.IsTrue(or.isLoop);
                or = EdgeAlgorithms.OrderLoop(loop, loop[i], GeometricOrientation.Undefined);
                Assert.IsTrue(or.isLoop);
            }
        }

        [TestMethod]
        public void OderLoopIntersect()
        {
            var data = OrderLoopIntersectTestData();
            var gd = data.model.Geometry;

            var loop = gd.Edges.ToList();
            var or = EdgeAlgorithms.OrderLoop(loop);
            Assert.IsFalse(or.isLoop);
            Assert.IsNull(or.loop);

            for (int i = 0; i < loop.Count; ++i)
            {
                or = EdgeAlgorithms.OrderLoop(loop, gd.Edges[i], GeometricOrientation.Forward);
                Assert.IsFalse(or.isLoop);
                or = EdgeAlgorithms.OrderLoop(loop, gd.Edges[i], GeometricOrientation.Backward);
                Assert.IsFalse(or.isLoop);
                or = EdgeAlgorithms.OrderLoop(loop, gd.Edges[i], GeometricOrientation.Undefined);
                Assert.IsFalse(or.isLoop);
            }
            loop.Reverse();
            for (int i = 0; i < loop.Count; ++i)
            {
                or = EdgeAlgorithms.OrderLoop(loop, gd.Edges[i], GeometricOrientation.Forward);
                Assert.IsFalse(or.isLoop);
                or = EdgeAlgorithms.OrderLoop(loop, gd.Edges[i], GeometricOrientation.Backward);
                Assert.IsFalse(or.isLoop);
                or = EdgeAlgorithms.OrderLoop(loop, gd.Edges[i], GeometricOrientation.Undefined);
                Assert.IsFalse(or.isLoop);
            }
        }
        [TestMethod]
        public void OderLoopIntersectPEdges()
        {
            var data = OrderLoopIntersectTestData();
            var gd = data.model.Geometry;

            gd.StartBatchOperation();

            var el = new EdgeLoop(gd.Layers[0], "LOOP", gd.Edges.Take(3));
            var el2 = new EdgeLoop(gd.Layers[0], "LOOP2", gd.Edges.Skip(3));
            var loop = el.Edges.Concat(el2.Edges).ToList();
            var or = EdgeAlgorithms.OrderLoop(loop);
            Assert.IsFalse(or.isLoop);
            Assert.IsNull(or.loop);

            for (int i = 0; i < loop.Count; ++i)
            {
                or = EdgeAlgorithms.OrderLoop(loop, gd.Edges[i], GeometricOrientation.Forward);
                Assert.IsFalse(or.isLoop);
                or = EdgeAlgorithms.OrderLoop(loop, gd.Edges[i], GeometricOrientation.Backward);
                Assert.IsFalse(or.isLoop);
            }
            loop.Reverse();
            for (int i = 0; i < loop.Count; ++i)
            {
                or = EdgeAlgorithms.OrderLoop(loop, loop[i].Edge, GeometricOrientation.Forward);
                Assert.IsFalse(or.isLoop);
                or = EdgeAlgorithms.OrderLoop(loop, loop[i].Edge, GeometricOrientation.Backward);
                Assert.IsFalse(or.isLoop);
            }

            gd.EndBatchOperation();
        }


        [TestMethod]
        public void OrderLoopEdgesNoLoop()
        {
            var data = OrderLoopTestData();
            var gd = data.model.Geometry;

            var loop = new Edge[] {
                gd.Edges[0], gd.Edges[1], gd.Edges[3], gd.Edges[4]
            };
            var or = EdgeAlgorithms.OrderLoop(loop);

            Assert.IsFalse(or.isLoop);
            Assert.IsNull(or.loop);

            for (int i = 0; i < loop.Length; ++i)
            {
                or = EdgeAlgorithms.OrderLoop(loop, loop[i], GeometricOrientation.Forward);
                Assert.IsFalse(or.isLoop);
                or = EdgeAlgorithms.OrderLoop(loop, loop[i], GeometricOrientation.Backward);
                Assert.IsFalse(or.isLoop);
                or = EdgeAlgorithms.OrderLoop(loop, loop[i], GeometricOrientation.Undefined);
                Assert.IsFalse(or.isLoop);
            }

            loop = loop.Reverse().ToArray();
            for (int i = 0; i < loop.Length; ++i)
            {
                or = EdgeAlgorithms.OrderLoop(loop, loop[i], GeometricOrientation.Forward);
                Assert.IsFalse(or.isLoop);
                or = EdgeAlgorithms.OrderLoop(loop, loop[i], GeometricOrientation.Backward);
                Assert.IsFalse(or.isLoop);
                or = EdgeAlgorithms.OrderLoop(loop, loop[i], GeometricOrientation.Undefined);
                Assert.IsFalse(or.isLoop);
            }
        }

        [TestMethod]
        public void OrderLoopPEdges()
        {
            var data = OrderLoopTestData();
            var gd = data.model.Geometry;

            var loop = new PEdge[]
            {
                gd.EdgeLoops[0].Edges[2],
                gd.EdgeLoops[0].Edges[1],
                gd.EdgeLoops[0].Edges[0],
                gd.EdgeLoops[0].Edges[4],
                gd.EdgeLoops[0].Edges[3],
            };
            var or = EdgeAlgorithms.OrderLoop(loop);

            Assert.IsTrue(or.isLoop);
            AssertUtil.ContainEqualValues(
                new PEdge[]
                {
                    gd.EdgeLoops[0].Edges[2],
                    gd.EdgeLoops[0].Edges[3],
                    gd.EdgeLoops[0].Edges[4],
                    gd.EdgeLoops[0].Edges[0],
                    gd.EdgeLoops[0].Edges[1],
                },
                or.loop
                );

            for (int i = 0; i < loop.Length; ++i)
            {
                or = EdgeAlgorithms.OrderLoop(loop, loop[i].Edge, GeometricOrientation.Forward);
                Assert.IsTrue(or.isLoop);
                or = EdgeAlgorithms.OrderLoop(loop, loop[i].Edge, GeometricOrientation.Backward);
                Assert.IsTrue(or.isLoop);
            }

            loop = loop.Reverse().ToArray();
            for (int i = 0; i < loop.Length; ++i)
            {
                or = EdgeAlgorithms.OrderLoop(loop, loop[i].Edge, GeometricOrientation.Forward);
                Assert.IsTrue(or.isLoop);
                or = EdgeAlgorithms.OrderLoop(loop, loop[i].Edge, GeometricOrientation.Backward);
                Assert.IsTrue(or.isLoop);
            }
        }

        [TestMethod]
        public void OrderLoopPEdgesNoLoop()
        {
            var data = OrderLoopTestData();
            var gd = data.model.Geometry;

            var loop = new PEdge[]
            {
                gd.EdgeLoops[0].Edges[2],
                gd.EdgeLoops[0].Edges[1],
                gd.EdgeLoops[0].Edges[0],
                gd.EdgeLoops[0].Edges[4],
            };
            var or = EdgeAlgorithms.OrderLoop(loop);

            Assert.IsFalse(or.isLoop);
            Assert.IsNull(or.loop);

            for (int i = 0; i < loop.Length; ++i)
            {
                or = EdgeAlgorithms.OrderLoop(loop, loop[i].Edge, GeometricOrientation.Forward);
                Assert.IsFalse(or.isLoop);
                or = EdgeAlgorithms.OrderLoop(loop, loop[i].Edge, GeometricOrientation.Backward);
                Assert.IsFalse(or.isLoop);
            }

            loop = loop.Reverse().ToArray();
            for (int i = 0; i < loop.Length; ++i)
            {
                or = EdgeAlgorithms.OrderLoop(loop, loop[i].Edge, GeometricOrientation.Forward);
                Assert.IsFalse(or.isLoop);
                or = EdgeAlgorithms.OrderLoop(loop, loop[i].Edge, GeometricOrientation.Backward);
                Assert.IsFalse(or.isLoop);
            }
        }

        [TestMethod]
        public void OrderLoopPEdgesInverted()
        {
            var data = OrderLoopTestData();
            var gd = data.model.Geometry;

            var loop = new PEdge[]
            {
                gd.EdgeLoops[0].Edges[1],
                gd.EdgeLoops[0].Edges[0],
                gd.EdgeLoops[0].Edges[2],
                gd.EdgeLoops[0].Edges[4],
                gd.EdgeLoops[0].Edges[3],
            };
            var or = EdgeAlgorithms.OrderLoop(loop);

            Assert.IsTrue(or.isLoop);
            AssertUtil.ContainEqualValues(
                new PEdge[]
                {
                    gd.EdgeLoops[0].Edges[1],
                    gd.EdgeLoops[0].Edges[0],
                    gd.EdgeLoops[0].Edges[4],
                    gd.EdgeLoops[0].Edges[3],
                    gd.EdgeLoops[0].Edges[2],
                },
                or.loop
                );

            for (int i = 0; i < loop.Length; ++i)
            {
                or = EdgeAlgorithms.OrderLoop(loop, loop[i].Edge, GeometricOrientation.Forward);
                Assert.IsTrue(or.isLoop);
                or = EdgeAlgorithms.OrderLoop(loop, loop[i].Edge, GeometricOrientation.Backward);
                Assert.IsTrue(or.isLoop);
            }

            loop = loop.Reverse().ToArray();
            for (int i = 0; i < loop.Length; ++i)
            {
                or = EdgeAlgorithms.OrderLoop(loop, loop[i].Edge, GeometricOrientation.Forward);
                Assert.IsTrue(or.isLoop);
                or = EdgeAlgorithms.OrderLoop(loop, loop[i].Edge, GeometricOrientation.Backward);
                Assert.IsTrue(or.isLoop);
            }
        }

        [TestMethod]
        public void OrderLoopPEdgesBaseEdge()
        {
            var data = OrderLoopTestData();
            var gd = data.model.Geometry;

            var or = EdgeAlgorithms.OrderLoop(new PEdge[]
            {
                gd.EdgeLoops[0].Edges[2],
                gd.EdgeLoops[0].Edges[1],
                gd.EdgeLoops[0].Edges[0],
                gd.EdgeLoops[0].Edges[4],
                gd.EdgeLoops[0].Edges[3],
            }, gd.EdgeLoops[0].Edges[0].Edge, GeometricOrientation.Forward);

            Assert.IsTrue(or.isLoop);
            AssertUtil.ContainEqualValues(
                new PEdge[]
                {
                    gd.EdgeLoops[0].Edges[0],
                    gd.EdgeLoops[0].Edges[1],
                    gd.EdgeLoops[0].Edges[2],
                    gd.EdgeLoops[0].Edges[3],
                    gd.EdgeLoops[0].Edges[4],
                },
                or.loop
                );
        }

        [TestMethod]
        public void OrderLoopPEdgesBaseEdgeInverted()
        {
            var data = OrderLoopTestData();
            var gd = data.model.Geometry;

            var or = EdgeAlgorithms.OrderLoop(new PEdge[]
            {
                gd.EdgeLoops[0].Edges[2],
                gd.EdgeLoops[0].Edges[1],
                gd.EdgeLoops[0].Edges[0],
                gd.EdgeLoops[0].Edges[4],
                gd.EdgeLoops[0].Edges[3],
            }, gd.EdgeLoops[0].Edges[0].Edge, GeometricOrientation.Backward);

            Assert.IsTrue(or.isLoop);
            AssertUtil.ContainEqualValues(
                new PEdge[]
                {
                    gd.EdgeLoops[0].Edges[0],
                    gd.EdgeLoops[0].Edges[4],
                    gd.EdgeLoops[0].Edges[3],
                    gd.EdgeLoops[0].Edges[2],
                    gd.EdgeLoops[0].Edges[1],
                },
                or.loop
                );
        }
    }
}
