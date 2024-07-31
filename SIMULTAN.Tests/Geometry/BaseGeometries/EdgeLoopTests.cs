using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data.Geometry;
using SIMULTAN.Data.SimMath;
using SIMULTAN.Tests.Geometry.EventData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



namespace SIMULTAN.Tests.Geometry.BaseGeometries
{
    [TestClass]
    public class EdgeLoopTests
    {
        private (Vertex[] v, Edge[] e) TestData(Layer layer)
        {
            Vertex[] v = new Vertex[]
            {
                new Vertex(layer, "", new SimPoint3D(1, 2, 3)),
                new Vertex(layer, "", new SimPoint3D(2, 4, 6)),
                new Vertex(layer, "", new SimPoint3D(3, 6, 9)),
                new Vertex(layer, "", new SimPoint3D(3, 6, 9))
            };

            Edge[] e = new Edge[]
            {
                new Edge(layer, "", new Vertex[] { v[0], v[1] }),
                new Edge(layer, "", new Vertex[] { v[1], v[2] }),
                new Edge(layer, "", new Vertex[] { v[0], v[2] }),
                new Edge(layer, "", new Vertex[] { v[1], v[3] }),
                new Edge(layer, "", new Vertex[] { v[3], v[0] }),
            };

            return (v, e);
        }

        private (EdgeLoop loop, BaseGeometryEventData eventData) LoopWithEvents(EdgeLoop loop)
        {
            return (loop, new BaseGeometryEventData(loop));
        }

        [TestMethod]
        public void Ctor()
        {
            //Prepare data
            var data = GeometryModelHelper.EmptyModel();
            (var v, var e) = TestData(data.layer);

            Assert.ThrowsException<ArgumentNullException>(() => { EdgeLoop l0 = new EdgeLoop(null, "", new Edge[] { e[0], e[1], e[2] }); }); //layer null
            Assert.ThrowsException<ArgumentNullException>(() => { EdgeLoop l0 = new EdgeLoop(data.layer, null, new Edge[] { e[0], e[1], e[2] }); }); //edges null
            Assert.ThrowsException<ArgumentNullException>(() => { EdgeLoop l0 = new EdgeLoop(data.layer, "", null); }); //edges null
            Assert.ThrowsException<ArgumentException>(() => { EdgeLoop l0 = new EdgeLoop(data.layer, "", new Edge[] { e[0], e[1] }); }); //<3 edges
            Assert.ThrowsException<ArgumentException>(() => { EdgeLoop l0 = new EdgeLoop(data.layer, "", new Edge[] { e[0], e[1], e[3] }); }); //no loop
            Assert.ThrowsException<ArgumentException>(() => { EdgeLoop l0 = new EdgeLoop(ulong.MaxValue, data.layer, "", new Edge[] { e[0], e[1], e[2] }); }); //id wrong

            EdgeLoop l1 = new EdgeLoop(data.layer, "", new Edge[] { e[0], e[1], e[2] });
            {
                Assert.AreEqual(1, data.model.Geometry.EdgeLoops.Count);
                Assert.IsTrue(data.model.Geometry.EdgeLoops.Contains(l1));

                Assert.AreEqual(data.layer, l1.Layer);
                Assert.AreEqual(3, l1.Edges.Count);

                for (int i = 0; i < 3; i++)
                {
                    Assert.AreEqual(1, e[i].PEdges.Count);
                    Assert.IsTrue(e[i].PEdges[0].Edge == e[i]);
                    Assert.IsTrue(e[i].PEdges[0].Parent == l1);
                    Assert.IsTrue(l1.Edges.Contains(e[i].PEdges[0]));
                }
            }

            EdgeLoop l2 = new EdgeLoop(99, data.layer, "", new Edge[] { e[0], e[1], e[2] });
            {
                Assert.AreEqual((ulong)99, l2.Id);
                Assert.AreEqual(2, data.model.Geometry.EdgeLoops.Count);
                Assert.IsTrue(data.model.Geometry.EdgeLoops.Contains(l2));

                Assert.AreEqual(data.layer, l2.Layer);
                Assert.AreEqual(3, l2.Edges.Count);

                for (int i = 0; i < 3; i++)
                {
                    Assert.AreEqual(2, e[i].PEdges.Count);
                    var pedge = e[i].PEdges.FirstOrDefault(x => x.Edge == e[i] && x.Parent == l2);
                    Assert.IsNotNull(pedge);
                    Assert.IsTrue(l2.Edges.Contains(pedge));
                }
            }
        }

        [TestMethod]
        public void Add()
        {
            var data = GeometryModelHelper.EmptyModelWithEvents();
            (var v, var e) = TestData(data.layer);
            var l0edges = new Edge[] { e[0], e[1], e[2] };
            data.eventData.Reset();

            int layerCount = data.layer.Elements.Count;

            var l0 = LoopWithEvents(new EdgeLoop(data.layer, "", l0edges));

            //Pedges
            Assert.AreEqual(1, e[0].PEdges.Count);
            Assert.AreEqual(1, e[1].PEdges.Count);
            Assert.AreEqual(1, e[2].PEdges.Count);
            Assert.AreEqual(0, e[3].PEdges.Count);
            Assert.AreEqual(0, e[4].PEdges.Count);

            Assert.AreEqual(3, l0.loop.Edges.Count);

            for (int i = 0; i < 3; ++i)
            {
                Assert.IsTrue(l0edges[i].PEdges.Any(x => x.Edge == l0edges[i] && x.Parent == l0.loop));
                Assert.AreEqual(l0edges[i].PEdges.First(x => x.Parent == l0.loop), l0.loop.Edges.First(x => x.Edge == l0edges[i]));
            }

            //Next/Prev ptr
            var l0startEdge = l0.loop.Edges.First(x => x.Edge == e[0]);

            var l0next = l0startEdge;

            for (int i = 0; i < 3; ++i)
            {
                Assert.AreEqual(l0next, l0next.Next.Prev);
                Assert.AreEqual(l0edges[i], l0next.Edge);
                l0next = l0next.Next;
            }

            Assert.AreEqual(l0startEdge, l0next);

            Assert.AreEqual(0, data.eventData.BatchOperationFinishedCount);
            Assert.AreEqual(1, data.eventData.TopologyChangedEventData.Count);
            Assert.AreEqual(1, data.eventData.AddEventData.Count);
            Assert.AreEqual(1, data.eventData.AddEventData[0].Count());
            Assert.AreEqual(l0.loop, data.eventData.AddEventData[0].First());
            Assert.AreEqual(0, data.eventData.RemoveEventData.Count);
            Assert.AreEqual(0, data.eventData.GeometryChangedEventData.Count);
            Assert.AreEqual(0, l0.eventData.GeometryChangedCount);
            Assert.AreEqual(0, l0.eventData.PropertyChangedData.Count);
            Assert.AreEqual(layerCount + 1, data.layer.Elements.Count);
            Assert.IsTrue(data.layer.Elements.Contains(l0.loop));
        }

        [TestMethod]
        public void Remove()
        {
            var data = GeometryModelHelper.EmptyModelWithEvents();
            (var v, var e) = TestData(data.layer);

            var loop = LoopWithEvents(new EdgeLoop(data.layer, "", new Edge[] { e[0], e[1], e[2] }));
            loop.eventData.Reset();
            data.eventData.Reset();

            var isDeleted = loop.loop.RemoveFromModel();

            //General
            Assert.AreEqual(true, isDeleted);
            Assert.AreEqual(0, data.layer.Elements.Count(x => x is EdgeLoop));
            Assert.AreEqual(0, data.model.Geometry.EdgeLoops.Count);

            //Events
            Assert.AreEqual(0, data.eventData.BatchOperationFinishedCount);
            Assert.AreEqual(1, data.eventData.TopologyChangedEventData.Count);
            Assert.AreEqual(0, data.eventData.AddEventData.Count);
            Assert.AreEqual(1, data.eventData.RemoveEventData.Count);
            Assert.AreEqual(1, data.eventData.RemoveEventData[0].Count());
            Assert.AreEqual(loop.loop, data.eventData.RemoveEventData[0].First());
            Assert.AreEqual(0, data.eventData.GeometryChangedEventData.Count);
            Assert.AreEqual(0, loop.eventData.GeometryChangedCount);
            Assert.AreEqual(0, loop.eventData.PropertyChangedData.Count);

            //PEdges
            for (int i = 0; i < 3; ++i)
                Assert.AreEqual(0, e[i].PEdges.Count);


            //Double deletion: no effect (hopefully :)
            isDeleted = loop.loop.RemoveFromModel();

            Assert.AreEqual(false, isDeleted);

            //Events
            Assert.AreEqual(0, data.eventData.BatchOperationFinishedCount);
            Assert.AreEqual(1, data.eventData.TopologyChangedEventData.Count);
            Assert.AreEqual(0, data.eventData.AddEventData.Count);
            Assert.AreEqual(1, data.eventData.RemoveEventData.Count);
            Assert.AreEqual(1, data.eventData.RemoveEventData[0].Count());
            Assert.AreEqual(loop.loop, data.eventData.RemoveEventData[0].First());
            Assert.AreEqual(0, data.eventData.GeometryChangedEventData.Count);
            Assert.AreEqual(0, loop.eventData.GeometryChangedCount);
            Assert.AreEqual(0, loop.eventData.PropertyChangedData.Count);
        }

        [TestMethod]
        public void Readd()
        {
            var data = GeometryModelHelper.EmptyModelWithEvents();
            (var v, var e) = TestData(data.layer);

            var loop = LoopWithEvents(new EdgeLoop(data.layer, "", new Edge[] { e[0], e[1], e[2] }));
            loop.loop.RemoveFromModel();
            loop.eventData.Reset();
            data.eventData.Reset();

            loop.loop.AddToModel();

            //General
            Assert.AreEqual(1, data.layer.Elements.Count(x => x is EdgeLoop));
            Assert.AreEqual(1, data.model.Geometry.EdgeLoops.Count);

            //Events
            Assert.AreEqual(0, data.eventData.BatchOperationFinishedCount);
            Assert.AreEqual(1, data.eventData.TopologyChangedEventData.Count);
            Assert.AreEqual(1, data.eventData.AddEventData.Count);
            Assert.AreEqual(1, data.eventData.AddEventData[0].Count());
            Assert.AreEqual(loop.loop, data.eventData.AddEventData[0].First());
            Assert.AreEqual(0, data.eventData.RemoveEventData.Count);
            Assert.AreEqual(0, data.eventData.GeometryChangedEventData.Count);
            Assert.AreEqual(0, loop.eventData.GeometryChangedCount);
            Assert.AreEqual(0, loop.eventData.PropertyChangedData.Count);

            Assert.ThrowsException<Exception>(() => loop.loop.AddToModel());
        }

        [TestMethod]
        public void BatchAdd()
        {
            var data = GeometryModelHelper.EmptyModelWithEvents();
            (var v, var e) = TestData(data.layer);
            var l0edges = new Edge[] { e[0], e[1], e[2] };
            var l1edges = new Edge[] { e[0], e[3], e[4] };

            data.eventData.Reset();

            data.model.Geometry.StartBatchOperation();

            var l0 = LoopWithEvents(new EdgeLoop(data.layer, "", l0edges));
            var l1 = LoopWithEvents(new EdgeLoop(data.layer, "", l1edges));

            Assert.AreEqual(0, data.eventData.TopologyChangedEventData.Count);
            Assert.AreEqual(0, data.eventData.AddEventData.Count);
            Assert.AreEqual(0, data.eventData.RemoveEventData.Count);
            Assert.AreEqual(0, data.eventData.GeometryChangedEventData.Count);
            Assert.AreEqual(0, data.eventData.BatchOperationFinishedCount);

            data.model.Geometry.EndBatchOperation();

            //Geometry Model checks
            Assert.AreEqual(2, data.model.Geometry.EdgeLoops.Count);
            Assert.IsTrue(data.model.Geometry.EdgeLoops.Contains(l0.loop));
            Assert.IsTrue(data.model.Geometry.EdgeLoops.Contains(l1.loop));

            //Pedges
            Assert.AreEqual(2, e[0].PEdges.Count);
            Assert.AreEqual(1, e[1].PEdges.Count);
            Assert.AreEqual(1, e[2].PEdges.Count);
            Assert.AreEqual(1, e[3].PEdges.Count);
            Assert.AreEqual(1, e[4].PEdges.Count);

            Assert.AreEqual(3, l0.loop.Edges.Count);
            Assert.AreEqual(3, l1.loop.Edges.Count);

            for (int i = 0; i < 3; ++i)
            {
                Assert.IsTrue(l0edges[i].PEdges.Any(x => x.Edge == l0edges[i] && x.Parent == l0.loop));
                Assert.AreEqual(l0edges[i].PEdges.First(x => x.Parent == l0.loop), l0.loop.Edges.First(x => x.Edge == l0edges[i]));

                Assert.IsTrue(l1edges[i].PEdges.Any(x => x.Edge == l1edges[i] && x.Parent == l1.loop));
                Assert.AreEqual(l1edges[i].PEdges.First(x => x.Parent == l1.loop), l1.loop.Edges.First(x => x.Edge == l1edges[i]));
            }

            //Next/Prev ptr
            var l0startEdge = l0.loop.Edges.First(x => x.Edge == e[0]);
            var l1startEdge = l1.loop.Edges.First(x => x.Edge == e[0]);

            var l0next = l0startEdge;
            var l1next = l1startEdge;

            for (int i = 0; i < 3; ++i)
            {
                Assert.AreEqual(l0next, l0next.Next.Prev);
                Assert.AreEqual(l0edges[i], l0next.Edge);
                l0next = l0next.Next;

                Assert.AreEqual(l1next, l1next.Next.Prev);
                Assert.AreEqual(l1edges[i], l1next.Edge);
                l1next = l1next.Next;
            }

            Assert.AreEqual(l0startEdge, l0next);
            Assert.AreEqual(l1startEdge, l1next);

            Assert.AreEqual(1, data.eventData.BatchOperationFinishedCount);
            Assert.AreEqual(1, data.eventData.TopologyChangedEventData.Count);
            Assert.AreEqual(1, data.eventData.AddEventData.Count);
            Assert.AreEqual(2, data.eventData.AddEventData[0].Count());
            Assert.IsTrue(data.eventData.AddEventData[0].Contains(l0.loop));
            Assert.IsTrue(data.eventData.AddEventData[0].Contains(l1.loop));
            Assert.AreEqual(0, data.eventData.RemoveEventData.Count);
            Assert.AreEqual(0, data.eventData.GeometryChangedEventData.Count);
            Assert.AreEqual(0, l0.eventData.GeometryChangedCount);
            Assert.AreEqual(0, l1.eventData.GeometryChangedCount);
            Assert.AreEqual(2, data.layer.Elements.Count(x => x is EdgeLoop));
        }

        [TestMethod]
        public void BatchRemove()
        {
            var data = GeometryModelHelper.EmptyModelWithEvents();
            (var v, var e) = TestData(data.layer);
            var l0edges = new Edge[] { e[0], e[1], e[2] };
            var l1edges = new Edge[] { e[0], e[3], e[4] };
            var l0 = LoopWithEvents(new EdgeLoop(data.layer, "", l0edges));
            var l1 = LoopWithEvents(new EdgeLoop(data.layer, "", l1edges));

            data.eventData.Reset();
            l0.eventData.Reset();
            l1.eventData.Reset();

            data.model.Geometry.StartBatchOperation();

            l0.loop.RemoveFromModel();
            l1.loop.RemoveFromModel();

            //No events should be issued during batch operation
            Assert.AreEqual(0, data.eventData.BatchOperationFinishedCount);
            Assert.AreEqual(0, data.eventData.TopologyChangedEventData.Count);
            Assert.AreEqual(0, data.eventData.AddEventData.Count);
            Assert.AreEqual(0, data.eventData.RemoveEventData.Count);
            Assert.AreEqual(0, data.eventData.GeometryChangedEventData.Count);

            data.model.Geometry.EndBatchOperation();


            Assert.AreEqual(0, data.model.Geometry.EdgeLoops.Count);
            //Check events afterwards
            for (int i = 0; i < 5; ++i)
                Assert.AreEqual(0, e[i].PEdges.Count);
            Assert.AreEqual(0, data.layer.Elements.Count(x => x is EdgeLoop));

            Assert.AreEqual(1, data.eventData.BatchOperationFinishedCount);
            Assert.AreEqual(1, data.eventData.TopologyChangedEventData.Count);
            Assert.AreEqual(0, data.eventData.AddEventData.Count);
            Assert.AreEqual(1, data.eventData.RemoveEventData.Count);
            Assert.AreEqual(2, data.eventData.RemoveEventData[0].Count());
            Assert.IsTrue(data.eventData.RemoveEventData[0].Contains(l0.loop));
            Assert.IsTrue(data.eventData.RemoveEventData[0].Contains(l1.loop));
            Assert.AreEqual(0, data.eventData.GeometryChangedEventData.Count);
            Assert.AreEqual(0, l0.eventData.GeometryChangedCount);
            Assert.AreEqual(0, l1.eventData.GeometryChangedCount);
        }

        [TestMethod]
        public void GeomeryChanged()
        {
            var data = GeometryModelHelper.EmptyModelWithEvents();
            (var v, var e) = TestData(data.layer);
            var l0edges = new Edge[] { e[0], e[1], e[2] };
            var l1edges = new Edge[] { e[0], e[3], e[4] };

            var l0 = LoopWithEvents(new EdgeLoop(data.layer, "", l0edges));
            var l1 = LoopWithEvents(new EdgeLoop(data.layer, "", l1edges));
            l0.eventData.Reset();
            l1.eventData.Reset();
            data.eventData.Reset();

            v[2].Position = new SimPoint3D(-1, -2, -3);
            Assert.AreEqual(5, data.eventData.GeometryChangedEventData.Count());
            for (int i = 0; i < data.eventData.GeometryChangedEventData.Count(); ++i)
                Assert.AreEqual(1, data.eventData.GeometryChangedEventData[i].Count());
            Assert.AreEqual(l0.loop, data.eventData.GeometryChangedEventData[2].First());
            Assert.AreEqual(l0.loop, data.eventData.GeometryChangedEventData[4].First());
            Assert.AreEqual(0, data.eventData.AddEventData.Count());
            Assert.AreEqual(0, data.eventData.RemoveEventData.Count());
            Assert.AreEqual(0, data.eventData.BatchOperationFinishedCount);
            Assert.AreEqual(0, data.eventData.TopologyChangedEventData.Count);
            Assert.AreEqual(2, l0.eventData.GeometryChangedCount);
            Assert.AreEqual(0, l1.eventData.GeometryChangedCount);

            v[0].Position = new SimPoint3D(-2, -4, -6);
            Assert.AreEqual(5 + 8, data.eventData.GeometryChangedEventData.Count());
            for (int i = 0; i < data.eventData.GeometryChangedEventData.Count(); ++i)
                Assert.AreEqual(1, data.eventData.GeometryChangedEventData[i].Count());
            Assert.AreEqual(l0.loop, data.eventData.GeometryChangedEventData[7].First());
            Assert.AreEqual(l1.loop, data.eventData.GeometryChangedEventData[8].First());
            Assert.AreEqual(l0.loop, data.eventData.GeometryChangedEventData[10].First());
            Assert.AreEqual(l1.loop, data.eventData.GeometryChangedEventData[12].First());

            Assert.AreEqual(0, data.eventData.AddEventData.Count());
            Assert.AreEqual(0, data.eventData.RemoveEventData.Count());
            Assert.AreEqual(0, data.eventData.BatchOperationFinishedCount);
            Assert.AreEqual(0, data.eventData.TopologyChangedEventData.Count);
            Assert.AreEqual(4, l0.eventData.GeometryChangedCount);
            Assert.AreEqual(2, l1.eventData.GeometryChangedCount);
        }

        [TestMethod]
        public void BatchGeomeryChanged()
        {
            var data = GeometryModelHelper.EmptyModelWithEvents();
            (var v, var e) = TestData(data.layer);
            var l0edges = new Edge[] { e[0], e[1], e[2] };
            var l1edges = new Edge[] { e[0], e[3], e[4] };

            var l0 = LoopWithEvents(new EdgeLoop(data.layer, "", l0edges));
            var l1 = LoopWithEvents(new EdgeLoop(data.layer, "", l1edges));
            l0.eventData.Reset();
            l1.eventData.Reset();
            data.eventData.Reset();

            data.model.Geometry.StartBatchOperation();

            v[2].Position = new SimPoint3D(-1, -2, -3);
            v[0].Position = new SimPoint3D(-2, -4, -6);

            Assert.AreEqual(0, data.eventData.GeometryChangedEventData.Count());
            Assert.AreEqual(0, data.eventData.AddEventData.Count());
            Assert.AreEqual(0, data.eventData.RemoveEventData.Count());
            Assert.AreEqual(0, data.eventData.BatchOperationFinishedCount);
            Assert.AreEqual(0, data.eventData.TopologyChangedEventData.Count);
            Assert.AreEqual(0, l0.eventData.GeometryChangedCount);
            Assert.AreEqual(0, l1.eventData.GeometryChangedCount);

            data.model.Geometry.EndBatchOperation();

            Assert.AreEqual(1, data.eventData.GeometryChangedEventData.Count());
            Assert.AreEqual(8, data.eventData.GeometryChangedEventData[0].Count());
            Assert.AreEqual(1, data.eventData.GeometryChangedEventData[0].Count(x => x == l0.loop));
            Assert.AreEqual(1, data.eventData.GeometryChangedEventData[0].Count(x => x == l1.loop));
            Assert.AreEqual(0, data.eventData.AddEventData.Count());
            Assert.AreEqual(0, data.eventData.RemoveEventData.Count());
            Assert.AreEqual(1, data.eventData.BatchOperationFinishedCount);
            Assert.AreEqual(0, data.eventData.TopologyChangedEventData.Count);
            Assert.AreEqual(1, l0.eventData.GeometryChangedCount);
            Assert.AreEqual(1, l1.eventData.GeometryChangedCount);
        }

        [TestMethod]
        public void EdgeExchange()
        {
            var data = GeometryModelHelper.EmptyModelWithEvents();
            (var v, var e) = TestData(data.layer);
            var l0edges = new Edge[] { e[0], e[1], e[2] };
            var l0 = LoopWithEvents(new EdgeLoop(data.layer, "", l0edges));

            var replaceEdge = new Edge(data.layer, "", e[2].Vertices);
            l0edges[2] = replaceEdge;

            data.eventData.Reset();
            l0.eventData.Reset();

            l0.loop.Edges[2] = new PEdge(replaceEdge, GeometricOrientation.Undefined, l0.loop);

            Assert.AreEqual(1, e[0].PEdges.Count);
            Assert.AreEqual(1, e[1].PEdges.Count);
            Assert.AreEqual(0, e[2].PEdges.Count);
            Assert.AreEqual(0, e[3].PEdges.Count);
            Assert.AreEqual(0, e[4].PEdges.Count);
            Assert.AreEqual(1, replaceEdge.PEdges.Count);

            //Next/Prev ptr
            var l0startEdge = l0.loop.Edges.First(x => x.Edge == e[0]);

            var l0next = l0startEdge;

            for (int i = 0; i < 3; ++i)
            {
                Assert.AreEqual(l0next, l0next.Next.Prev);
                Assert.AreEqual(l0edges[i], l0next.Edge);
                l0next = l0next.Next;
            }

            Assert.AreEqual(l0startEdge, l0next);

            //Events
            Assert.AreEqual(0, data.eventData.GeometryChangedEventData.Count());
            Assert.AreEqual(0, data.eventData.AddEventData.Count());
            Assert.AreEqual(0, data.eventData.RemoveEventData.Count());
            Assert.AreEqual(0, data.eventData.BatchOperationFinishedCount);
            Assert.AreEqual(1, data.eventData.TopologyChangedEventData.Count);
            Assert.AreEqual(0, l0.eventData.GeometryChangedCount);
            Assert.AreEqual(1, l0.eventData.TopologyChangedCount);

            Assert.ThrowsException<Exception>(() => l0.loop.Edges[2] = new PEdge(e[4], GeometricOrientation.Undefined, l0.loop));
        }

        [TestMethod]
        public void BatchEdgeExchange()
        {
            var data = GeometryModelHelper.EmptyModelWithEvents();
            (var v, var e) = TestData(data.layer);
            var l0edges = new Edge[] { e[0], e[1], e[2] };
            var l0 = LoopWithEvents(new EdgeLoop(data.layer, "", l0edges));

            data.eventData.Reset();
            l0.eventData.Reset();

            data.model.Geometry.StartBatchOperation();
            l0.loop.Edges.RemoveAt(1);
            l0.loop.Edges.RemoveAt(1);

            l0.loop.Edges.Add(new PEdge(e[3], GeometricOrientation.Undefined, l0.loop));
            l0.loop.Edges.Add(new PEdge(e[4], GeometricOrientation.Undefined, l0.loop));

            Assert.AreEqual(0, data.eventData.GeometryChangedEventData.Count());
            Assert.AreEqual(0, data.eventData.AddEventData.Count());
            Assert.AreEqual(0, data.eventData.RemoveEventData.Count());
            Assert.AreEqual(0, data.eventData.BatchOperationFinishedCount);
            Assert.AreEqual(0, data.eventData.TopologyChangedEventData.Count);
            Assert.AreEqual(0, l0.eventData.GeometryChangedCount);

            data.model.Geometry.EndBatchOperation();

            Assert.AreEqual(1, e[0].PEdges.Count);
            Assert.AreEqual(0, e[1].PEdges.Count);
            Assert.AreEqual(0, e[2].PEdges.Count);
            Assert.AreEqual(1, e[3].PEdges.Count);
            Assert.AreEqual(1, e[4].PEdges.Count);

            Assert.AreEqual(0, data.eventData.GeometryChangedEventData.Count());
            Assert.AreEqual(0, data.eventData.AddEventData.Count());
            Assert.AreEqual(0, data.eventData.RemoveEventData.Count());
            Assert.AreEqual(1, data.eventData.BatchOperationFinishedCount);
            Assert.AreEqual(1, data.eventData.TopologyChangedEventData.Count);
            Assert.AreEqual(0, l0.eventData.GeometryChangedCount);
        }

        [TestMethod]
        public void VertexExchange()
        {
            var data = GeometryModelHelper.EmptyModelWithEvents();
            (var v, var e) = TestData(data.layer);
            var l0edges = new Edge[] { e[0], e[1], e[2] };
            var l0 = LoopWithEvents(new EdgeLoop(data.layer, "", l0edges));

            data.eventData.Reset();
            l0.eventData.Reset();

            data.model.Geometry.StartBatchOperation();

            e[1].Vertices[1] = v[3];
            e[2].Vertices[1] = v[3];

            Assert.AreEqual(0, data.eventData.GeometryChangedEventData.Count());
            Assert.AreEqual(0, data.eventData.AddEventData.Count());
            Assert.AreEqual(0, data.eventData.RemoveEventData.Count());
            Assert.AreEqual(0, data.eventData.BatchOperationFinishedCount);
            Assert.AreEqual(0, data.eventData.TopologyChangedEventData.Count);
            Assert.AreEqual(0, l0.eventData.GeometryChangedCount);

            data.model.Geometry.EndBatchOperation();

            Assert.AreEqual(0, data.eventData.GeometryChangedEventData.Count());
            Assert.AreEqual(0, data.eventData.AddEventData.Count());
            Assert.AreEqual(0, data.eventData.RemoveEventData.Count());
            Assert.AreEqual(1, data.eventData.BatchOperationFinishedCount);
            Assert.AreEqual(1, data.eventData.TopologyChangedEventData.Count);
            Assert.AreEqual(0, l0.eventData.GeometryChangedCount);
        }

        [TestMethod]
        public void VertexExchangeNoLoop()
        {
            var data = GeometryModelHelper.EmptyModelWithEvents();
            (var v, var e) = TestData(data.layer);
            var l0edges = new Edge[] { e[0], e[1], e[2] };
            var l0 = LoopWithEvents(new EdgeLoop(data.layer, "", l0edges));

            data.eventData.Reset();
            l0.eventData.Reset();

            Assert.ThrowsException<Exception>(() => e[1].Vertices[1] = v[3]); //No valid loop
        }

        [TestMethod]
        public void BatchVertexExchangeNoLoop()
        {
            var data = GeometryModelHelper.EmptyModelWithEvents();
            (var v, var e) = TestData(data.layer);
            var l0edges = new Edge[] { e[0], e[1], e[2] };
            var l0 = LoopWithEvents(new EdgeLoop(data.layer, "", l0edges));

            data.eventData.Reset();
            l0.eventData.Reset();

            data.model.Geometry.StartBatchOperation();

            e[1].Vertices[1] = v[3];

            Assert.ThrowsException<Exception>(() => data.model.Geometry.EndBatchOperation()); //No valid loop
        }


        [TestMethod]
        public void Visibility()
        {
            //Data
            var data = GeometryModelHelper.EmptyModelWithEvents();
            (var v, var e) = TestData(data.layer);
            var l0edges = new Edge[] { e[0], e[1], e[2] };
            var l0 = LoopWithEvents(new EdgeLoop(data.layer, "", l0edges));
            data.eventData.Reset();

            Assert.AreEqual(true, data.layer.IsVisible);
            Assert.AreEqual(true, l0.loop.IsVisible);
            Assert.AreEqual(true, l0.loop.IsActuallyVisible);
            Assert.AreEqual(0, l0.eventData.PropertyChangedData.Count);

            l0.loop.IsVisible = false;
            Assert.AreEqual(false, l0.loop.IsVisible);
            Assert.AreEqual(false, l0.loop.IsActuallyVisible);
            Assert.AreEqual(2, l0.eventData.PropertyChangedData.Count);
            Assert.AreEqual(nameof(Vertex.IsVisible), l0.eventData.PropertyChangedData[0]);
            Assert.AreEqual(nameof(Vertex.IsActuallyVisible), l0.eventData.PropertyChangedData[1]);

            l0.loop.IsVisible = true;
            Assert.AreEqual(true, l0.loop.IsVisible);
            Assert.AreEqual(true, l0.loop.IsActuallyVisible);
            Assert.AreEqual(4, l0.eventData.PropertyChangedData.Count);
            Assert.AreEqual(nameof(Vertex.IsVisible), l0.eventData.PropertyChangedData[2]);
            Assert.AreEqual(nameof(Vertex.IsActuallyVisible), l0.eventData.PropertyChangedData[3]);

            data.layer.IsVisible = false;
            Assert.AreEqual(true, l0.loop.IsVisible);
            Assert.AreEqual(false, l0.loop.IsActuallyVisible);
            Assert.AreEqual(5, l0.eventData.PropertyChangedData.Count);
            Assert.AreEqual(nameof(Vertex.IsActuallyVisible), l0.eventData.PropertyChangedData[4]);

            data.layer.IsVisible = true;
            Assert.AreEqual(true, l0.loop.IsVisible);
            Assert.AreEqual(true, l0.loop.IsActuallyVisible);
            Assert.AreEqual(6, l0.eventData.PropertyChangedData.Count);
            Assert.AreEqual(nameof(Vertex.IsActuallyVisible), l0.eventData.PropertyChangedData[5]);

            Assert.AreEqual(0, l0.eventData.GeometryChangedCount);
        }

        [TestMethod]
        public void Name()
        {
            //Data
            var data = GeometryModelHelper.EmptyModelWithEvents();
            (var v, var e) = TestData(data.layer);
            var l0edges = new Edge[] { e[0], e[1], e[2] };
            var l0 = LoopWithEvents(new EdgeLoop(data.layer, "", l0edges));
            data.eventData.Reset();

            Assert.AreEqual(0, l0.eventData.PropertyChangedData.Count);

            l0.loop.Name = "Renamed";
            Assert.AreEqual("Renamed", l0.loop.Name);
            Assert.AreEqual(1, l0.eventData.PropertyChangedData.Count);
            Assert.AreEqual(nameof(Vertex.Name), l0.eventData.PropertyChangedData[0]);
        }

        [TestMethod]
        public void MoveToLayer()
        {
            var data = GeometryModelHelper.EmptyModelWithEvents();
            Layer targetLayer = new Layer(data.model.Geometry, "TargetLayer") { Color = new DerivedColor(SimColors.Pink) };

            (var v, var e) = TestData(data.layer);
            var l0edges = new Edge[] { e[0], e[1], e[2] };
            var l0 = LoopWithEvents(new EdgeLoop(data.layer, "", l0edges));
            data.eventData.Reset();

            Assert.AreEqual(data.layer, l0.loop.Layer);
            Assert.AreEqual(1, data.layer.Elements.Count(x => x is EdgeLoop));
            Assert.AreEqual(SimColors.Red, l0.loop.Color.Color);

            l0.loop.Layer = targetLayer;
            Assert.AreEqual(targetLayer, l0.loop.Layer);
            Assert.AreEqual(0, data.layer.Elements.Count(x => x is EdgeLoop));
            Assert.AreEqual(1, targetLayer.Elements.Count(x => x is EdgeLoop));
            Assert.AreEqual(SimColors.Pink, l0.loop.Color.Color);

            Assert.AreEqual(0, data.eventData.GeometryChangedEventData.Count);
            Assert.AreEqual(0, data.eventData.AddEventData.Count);
            Assert.AreEqual(0, data.eventData.RemoveEventData.Count);
            Assert.AreEqual(0, data.eventData.BatchOperationFinishedCount);
            Assert.AreEqual(0, data.eventData.TopologyChangedEventData.Count);
            Assert.AreEqual(0, l0.eventData.GeometryChangedCount);
            Assert.AreEqual(3, l0.eventData.PropertyChangedData.Count);
            Assert.IsTrue(l0.eventData.PropertyChangedData.Contains("Layer"));
            Assert.IsTrue(l0.eventData.PropertyChangedData.Contains("Color"));
            Assert.IsTrue(l0.eventData.PropertyChangedData.Contains("IsActuallyVisible"));
        }

        [TestMethod]
        public void BatchEdgeTopologyChanged()
        {
            //There is no non-badged version possible since exchanging one vertex only will result in an unclosed loop

            var data = GeometryModelHelper.EmptyModelWithEvents();
            (var v, var e) = TestData(data.layer);
            var l0edges = new Edge[] { e[0], e[1], e[2] };
            var l0 = LoopWithEvents(new EdgeLoop(data.layer, "", l0edges));

            data.eventData.Reset();
            l0.eventData.Reset();

            data.model.Geometry.StartBatchOperation();

            e[1].Vertices[1] = v[3];
            e[2].Vertices[1] = v[3];

            Assert.AreEqual(0, data.eventData.TopologyChangedEventData.Count);
            Assert.AreEqual(0, l0.eventData.TopologyChangedCount);

            data.model.Geometry.EndBatchOperation();

            Assert.AreEqual(1, data.eventData.TopologyChangedEventData.Count);
            Assert.AreEqual(3, data.eventData.TopologyChangedEventData[0].Count());
            Assert.IsTrue(data.eventData.TopologyChangedEventData[0].Contains(e[1]));
            Assert.IsTrue(data.eventData.TopologyChangedEventData[0].Contains(e[2]));
            Assert.IsTrue(data.eventData.TopologyChangedEventData[0].Contains(l0.loop));
            Assert.AreEqual(1, l0.eventData.TopologyChangedCount);
        }
    }
}
