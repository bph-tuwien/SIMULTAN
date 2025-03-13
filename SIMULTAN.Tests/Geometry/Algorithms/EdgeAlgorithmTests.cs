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


        [TestMethod]
        public void OrderLoopExceptions()
        {
            var data = OrderLoopTestData();

            Assert.ThrowsException<ArgumentNullException>(() => { EdgeAlgorithms.OrderLoop((List<Edge>)null); });
            Assert.ThrowsException<ArgumentException>(() => { EdgeAlgorithms.OrderLoop(new List<Edge>()); });

            Assert.ThrowsException<ArgumentNullException>(() => { EdgeAlgorithms.OrderLoop((List<PEdge>)null); });
            Assert.ThrowsException<ArgumentException>(() => { EdgeAlgorithms.OrderLoop(new List<PEdge>()); });

            Assert.ThrowsException<ArgumentNullException>(() => { EdgeAlgorithms.OrderLoop(null, data.model.Geometry.Edges[0], 
                GeometricOrientation.Forward); });
            Assert.ThrowsException<ArgumentNullException>(() => { EdgeAlgorithms.OrderLoop(data.model.Geometry.EdgeLoops[0].Edges,
                null, GeometricOrientation.Forward); });
            Assert.ThrowsException<ArgumentException>(() => {
                EdgeAlgorithms.OrderLoop(data.model.Geometry.EdgeLoops[0].Edges,
                    data.model.Geometry.Edges[0], GeometricOrientation.Undefined);
            });
        }

        [TestMethod]
        public void OrderLoopEdges()
        {
            var data = OrderLoopTestData();
            var gd = data.model.Geometry;

            var or = EdgeAlgorithms.OrderLoop(new Edge[] {
                gd.Edges[0], gd.Edges[2], gd.Edges[1], gd.Edges[3], gd.Edges[4]
            });

            Assert.IsTrue(or.isLoop);
            AssertUtil.ContainEqualValues(
                new Edge[] {
                    gd.Edges[0], gd.Edges[1], gd.Edges[2], gd.Edges[3], gd.Edges[4]
                },
                or.loop
                );
        }

        [TestMethod]
        public void OrderLoopEdgesNoLoop()
        {
            var data = OrderLoopTestData();
            var gd = data.model.Geometry;

            var or = EdgeAlgorithms.OrderLoop(new Edge[] {
                gd.Edges[0], gd.Edges[1], gd.Edges[3], gd.Edges[4]
            });

            Assert.IsFalse(or.isLoop);
            Assert.IsNull(or.loop);
        }

        [TestMethod]
        public void OrderLoopPEdges()
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
            });

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
        }

        [TestMethod]
        public void OrderLoopPEdgesNoLoop()
        {
            var data = OrderLoopTestData();
            var gd = data.model.Geometry;

            var or = EdgeAlgorithms.OrderLoop(new PEdge[]
            {
                gd.EdgeLoops[0].Edges[2],
                gd.EdgeLoops[0].Edges[1],
                gd.EdgeLoops[0].Edges[0],
                gd.EdgeLoops[0].Edges[4],
            });

            Assert.IsFalse(or.isLoop);
            Assert.IsNull(or.loop);
        }

        [TestMethod]
        public void OrderLoopPEdgesInverted()
        {
            var data = OrderLoopTestData();
            var gd = data.model.Geometry;

            var or = EdgeAlgorithms.OrderLoop(new PEdge[]
            {
                gd.EdgeLoops[0].Edges[1],
                gd.EdgeLoops[0].Edges[0],
                gd.EdgeLoops[0].Edges[2],
                gd.EdgeLoops[0].Edges[4],
                gd.EdgeLoops[0].Edges[3],
            });

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
