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
    public class PolylineTests
    {
        private (Vertex[] v, Edge[] e) TestData(Layer layer)
        {
            Vertex[] v = new Vertex[]
            {
                new Vertex(layer, "", new SimPoint3D(1, 2, 3)),
                new Vertex(layer, "", new SimPoint3D(2, 4, 6)),
                new Vertex(layer, "", new SimPoint3D(3, 6, 9)),
                new Vertex(layer, "", new SimPoint3D(-1, -2, -3)),
                new Vertex(layer, "", new SimPoint3D(-2, -4, -6)),
            };

            Edge[] e = new Edge[]
            {
                new Edge(layer, "", new Vertex[] { v[0], v[1] }),
                new Edge(layer, "", new Vertex[] { v[1], v[2] }),
                new Edge(layer, "", new Vertex[] { v[2], v[3] }),
                new Edge(layer, "", new Vertex[] { v[1], v[3] }),
                new Edge(layer, "", new Vertex[] { v[3], v[4] }),
            };

            return (v, e);
        }

        private (Polyline line, BaseGeometryEventData eventData) LineWithEvents(Polyline line)
        {
            return (line, new BaseGeometryEventData(line));
        }

        [TestMethod]
        public void Ctor()
        {
            //Prepare data
            var data = GeometryModelHelper.EmptyModel();
            (var v, var e) = TestData(data.layer);

            Assert.ThrowsException<ArgumentNullException>(() => { Polyline l0 = new Polyline(null, "", new Edge[] { e[0], e[1], e[2] }); }); //layer null
            Assert.ThrowsException<ArgumentNullException>(() => { Polyline l0 = new Polyline(data.layer, null, new Edge[] { e[0], e[1], e[2] }); }); //layer null
            Assert.ThrowsException<ArgumentNullException>(() => { Polyline l0 = new Polyline(data.layer, "", null); }); //edges null
            Assert.ThrowsException<ArgumentException>(() => { Polyline l0 = new Polyline(data.layer, "", new Edge[] { }); }); //empty edges
            Assert.ThrowsException<ArgumentException>(() => { Polyline l0 = new Polyline(ulong.MaxValue, data.layer, "", new Edge[] { e[0], e[1], e[2] }); }); //id wrong

            Polyline l1 = new Polyline(data.layer, "", new Edge[] { e[0], e[1], e[2] });
            {
                Assert.AreEqual(1, data.model.Geometry.Polylines.Count);
                Assert.IsTrue(data.model.Geometry.Polylines.Contains(l1));

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

            Polyline l2 = new Polyline(99, data.layer, "line", new Edge[] { e[0], e[1], e[2] });
            {
                Assert.AreEqual("line", l2.Name);

                Assert.AreEqual((ulong)99, l2.Id);
                Assert.AreEqual(2, data.model.Geometry.Polylines
.Count);
                Assert.IsTrue(data.model.Geometry.Polylines.Contains(l2));

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

            var l0 = LineWithEvents(new Polyline(data.layer, "", l0edges));

            //Pedges
            Assert.AreEqual(1, e[0].PEdges.Count);
            Assert.AreEqual(1, e[1].PEdges.Count);
            Assert.AreEqual(1, e[2].PEdges.Count);
            Assert.AreEqual(0, e[3].PEdges.Count);
            Assert.AreEqual(0, e[4].PEdges.Count);

            Assert.AreEqual(3, l0.line.Edges.Count);

            for (int i = 0; i < 3; ++i)
            {
                Assert.IsTrue(l0edges[i].PEdges.Any(x => x.Edge == l0edges[i] && x.Parent == l0.line));
                Assert.AreEqual(l0edges[i].PEdges.First(x => x.Parent == l0.line), l0.line.Edges.First(x => x.Edge == l0edges[i]));
            }

            //Next/Prev ptr
            var l0startEdge = l0.line.Edges[0];
            Assert.IsNull(l0startEdge.Prev);

            var l0next = l0startEdge;

            for (int i = 0; i < 2; ++i)
            {
                Assert.AreEqual(l0next, l0next.Next.Prev);
                Assert.AreEqual(l0edges[i], l0next.Edge);
                l0next = l0next.Next;
            }

            Assert.AreEqual(l0edges[2], l0next.Edge);
            Assert.IsNull(l0next.Next);

            Assert.AreEqual(0, data.eventData.BatchOperationFinishedCount);
            Assert.AreEqual(1, data.eventData.TopologyChangedEventData.Count);
            Assert.AreEqual(1, data.eventData.AddEventData.Count);
            Assert.AreEqual(1, data.eventData.AddEventData[0].Count());
            Assert.AreEqual(l0.line, data.eventData.AddEventData[0].First());
            Assert.AreEqual(0, data.eventData.RemoveEventData.Count);
            Assert.AreEqual(0, data.eventData.GeometryChangedEventData.Count);
            Assert.AreEqual(0, l0.eventData.GeometryChangedCount);
            Assert.AreEqual(0, l0.eventData.PropertyChangedData.Count);
            Assert.AreEqual(layerCount + 1, data.layer.Elements.Count);
            Assert.IsTrue(data.layer.Elements.Contains(l0.line));
        }

        [TestMethod]
        public void Remove()
        {
            var data = GeometryModelHelper.EmptyModelWithEvents();
            (var v, var e) = TestData(data.layer);

            var loop = LineWithEvents(new Polyline(data.layer, "", new Edge[] { e[0], e[1], e[2] }));
            loop.eventData.Reset();
            data.eventData.Reset();

            var isDeleted = loop.line.RemoveFromModel();

            //General
            Assert.AreEqual(true, isDeleted);
            Assert.AreEqual(0, data.layer.Elements.Count(x => x is EdgeLoop));
            Assert.AreEqual(0, data.model.Geometry.Polylines.Count);

            //Events
            Assert.AreEqual(0, data.eventData.BatchOperationFinishedCount);
            Assert.AreEqual(1, data.eventData.TopologyChangedEventData.Count);
            Assert.AreEqual(0, data.eventData.AddEventData.Count);
            Assert.AreEqual(1, data.eventData.RemoveEventData.Count);
            Assert.AreEqual(1, data.eventData.RemoveEventData[0].Count());
            Assert.AreEqual(loop.line, data.eventData.RemoveEventData[0].First());
            Assert.AreEqual(0, data.eventData.GeometryChangedEventData.Count);
            Assert.AreEqual(0, loop.eventData.GeometryChangedCount);
            Assert.AreEqual(0, loop.eventData.PropertyChangedData.Count);

            //PEdges
            for (int i = 0; i < 3; ++i)
                Assert.AreEqual(0, e[i].PEdges.Count);


            //Double deletion: no effect (hopefully :)
            isDeleted = loop.line.RemoveFromModel();

            Assert.AreEqual(false, isDeleted);

            //Events
            Assert.AreEqual(0, data.eventData.BatchOperationFinishedCount);
            Assert.AreEqual(1, data.eventData.TopologyChangedEventData.Count);
            Assert.AreEqual(0, data.eventData.AddEventData.Count);
            Assert.AreEqual(1, data.eventData.RemoveEventData.Count);
            Assert.AreEqual(1, data.eventData.RemoveEventData[0].Count());
            Assert.AreEqual(loop.line, data.eventData.RemoveEventData[0].First());
            Assert.AreEqual(0, data.eventData.GeometryChangedEventData.Count);
            Assert.AreEqual(0, loop.eventData.GeometryChangedCount);
            Assert.AreEqual(0, loop.eventData.PropertyChangedData.Count);
        }

        [TestMethod]
        public void Readd()
        {
            var data = GeometryModelHelper.EmptyModelWithEvents();
            (var v, var e) = TestData(data.layer);

            var loop = LineWithEvents(new Polyline(data.layer, "", new Edge[] { e[0], e[1], e[2] }));
            loop.line.RemoveFromModel();
            loop.eventData.Reset();
            data.eventData.Reset();

            loop.line.AddToModel();

            //General
            Assert.AreEqual(1, data.layer.Elements.Count(x => x is Polyline));
            Assert.AreEqual(1, data.model.Geometry.Polylines.Count);

            //Events
            Assert.AreEqual(0, data.eventData.BatchOperationFinishedCount);
            Assert.AreEqual(1, data.eventData.TopologyChangedEventData.Count);
            Assert.AreEqual(1, data.eventData.AddEventData.Count);
            Assert.AreEqual(1, data.eventData.AddEventData[0].Count());
            Assert.AreEqual(loop.line, data.eventData.AddEventData[0].First());
            Assert.AreEqual(0, data.eventData.RemoveEventData.Count);
            Assert.AreEqual(0, data.eventData.GeometryChangedEventData.Count);
            Assert.AreEqual(0, loop.eventData.GeometryChangedCount);
            Assert.AreEqual(0, loop.eventData.PropertyChangedData.Count);

            Assert.ThrowsException<Exception>(() => loop.line.AddToModel());
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

            var l0 = LineWithEvents(new Polyline(data.layer, "", l0edges));
            var l1 = LineWithEvents(new Polyline(data.layer, "", l1edges));

            Assert.AreEqual(0, data.eventData.TopologyChangedEventData.Count);
            Assert.AreEqual(0, data.eventData.AddEventData.Count);
            Assert.AreEqual(0, data.eventData.RemoveEventData.Count);
            Assert.AreEqual(0, data.eventData.GeometryChangedEventData.Count);
            Assert.AreEqual(0, data.eventData.BatchOperationFinishedCount);

            data.model.Geometry.EndBatchOperation();

            //Geometry Model checks
            Assert.AreEqual(2, data.model.Geometry.Polylines.Count);
            Assert.IsTrue(data.model.Geometry.Polylines.Contains(l0.line));
            Assert.IsTrue(data.model.Geometry.Polylines.Contains(l1.line));

            //Pedges
            Assert.AreEqual(2, e[0].PEdges.Count);
            Assert.AreEqual(1, e[1].PEdges.Count);
            Assert.AreEqual(1, e[2].PEdges.Count);
            Assert.AreEqual(1, e[3].PEdges.Count);
            Assert.AreEqual(1, e[4].PEdges.Count);

            Assert.AreEqual(3, l0.line.Edges.Count);
            Assert.AreEqual(3, l1.line.Edges.Count);

            for (int i = 0; i < 3; ++i)
            {
                Assert.IsTrue(l0edges[i].PEdges.Any(x => x.Edge == l0edges[i] && x.Parent == l0.line));
                Assert.AreEqual(l0edges[i].PEdges.First(x => x.Parent == l0.line), l0.line.Edges.First(x => x.Edge == l0edges[i]));

                Assert.IsTrue(l1edges[i].PEdges.Any(x => x.Edge == l1edges[i] && x.Parent == l1.line));
                Assert.AreEqual(l1edges[i].PEdges.First(x => x.Parent == l1.line), l1.line.Edges.First(x => x.Edge == l1edges[i]));
            }

            //Next/Prev ptr
            var l0startEdge = l0.line.Edges.First(x => x.Edge == e[0]);
            var l1startEdge = l1.line.Edges.First(x => x.Edge == e[0]);

            Assert.IsNull(l0startEdge.Prev);
            Assert.IsNull(l1startEdge.Prev);

            var l0next = l0startEdge;
            var l1next = l1startEdge;

            for (int i = 0; i < 2; ++i)
            {
                Assert.AreEqual(l0next, l0next.Next.Prev);
                Assert.AreEqual(l0edges[i], l0next.Edge);
                l0next = l0next.Next;

                Assert.AreEqual(l1next, l1next.Next.Prev);
                Assert.AreEqual(l1edges[i], l1next.Edge);
                l1next = l1next.Next;
            }

            Assert.AreEqual(l0edges[2], l0next.Edge);
            Assert.AreEqual(l1edges[2], l1next.Edge);
            Assert.IsNull(l0next.Next);
            Assert.IsNull(l1next.Next);

            Assert.AreEqual(1, data.eventData.BatchOperationFinishedCount);
            Assert.AreEqual(1, data.eventData.TopologyChangedEventData.Count);
            Assert.AreEqual(1, data.eventData.AddEventData.Count);
            Assert.AreEqual(2, data.eventData.AddEventData[0].Count());
            Assert.IsTrue(data.eventData.AddEventData[0].Contains(l0.line));
            Assert.IsTrue(data.eventData.AddEventData[0].Contains(l1.line));
            Assert.AreEqual(0, data.eventData.RemoveEventData.Count);
            Assert.AreEqual(0, data.eventData.GeometryChangedEventData.Count);
            Assert.AreEqual(0, l0.eventData.GeometryChangedCount);
            Assert.AreEqual(0, l1.eventData.GeometryChangedCount);
            Assert.AreEqual(2, data.layer.Elements.Count(x => x is Polyline));
        }

        [TestMethod]
        public void BatchRemove()
        {
            var data = GeometryModelHelper.EmptyModelWithEvents();
            (var v, var e) = TestData(data.layer);
            var l0edges = new Edge[] { e[0], e[1], e[2] };
            var l1edges = new Edge[] { e[0], e[3], e[4] };
            var l0 = LineWithEvents(new Polyline(data.layer, "", l0edges));
            var l1 = LineWithEvents(new Polyline(data.layer, "", l1edges));

            data.eventData.Reset();
            l0.eventData.Reset();
            l1.eventData.Reset();

            data.model.Geometry.StartBatchOperation();

            l0.line.RemoveFromModel();
            l1.line.RemoveFromModel();

            //No events should be issued during batch operation
            Assert.AreEqual(0, data.eventData.BatchOperationFinishedCount);
            Assert.AreEqual(0, data.eventData.TopologyChangedEventData.Count);
            Assert.AreEqual(0, data.eventData.AddEventData.Count);
            Assert.AreEqual(0, data.eventData.RemoveEventData.Count);
            Assert.AreEqual(0, data.eventData.GeometryChangedEventData.Count);

            data.model.Geometry.EndBatchOperation();

            Assert.AreEqual(0, data.model.Geometry.Polylines.Count);

            //Check events afterwards
            for (int i = 0; i < 5; ++i)
                Assert.AreEqual(0, e[i].PEdges.Count);
            Assert.AreEqual(0, data.layer.Elements.Count(x => x is Polyline));

            Assert.AreEqual(1, data.eventData.BatchOperationFinishedCount);
            Assert.AreEqual(1, data.eventData.TopologyChangedEventData.Count);
            Assert.AreEqual(0, data.eventData.AddEventData.Count);
            Assert.AreEqual(1, data.eventData.RemoveEventData.Count);
            Assert.AreEqual(2, data.eventData.RemoveEventData[0].Count());
            Assert.IsTrue(data.eventData.RemoveEventData[0].Contains(l0.line));
            Assert.IsTrue(data.eventData.RemoveEventData[0].Contains(l1.line));
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

            var l0 = LineWithEvents(new Polyline(data.layer, "", l0edges));
            var l1 = LineWithEvents(new Polyline(data.layer, "", l1edges));
            l0.eventData.Reset();
            l1.eventData.Reset();
            data.eventData.Reset();

            v[2].Position = new SimPoint3D(-1, -2, -3);
            Assert.AreEqual(5, data.eventData.GeometryChangedEventData.Count());
            for (int i = 0; i < data.eventData.GeometryChangedEventData.Count(); ++i)
                Assert.AreEqual(1, data.eventData.GeometryChangedEventData[i].Count());
            Assert.AreEqual(l0.line, data.eventData.GeometryChangedEventData[2].First());
            Assert.AreEqual(l0.line, data.eventData.GeometryChangedEventData[4].First());
            Assert.AreEqual(0, data.eventData.AddEventData.Count());
            Assert.AreEqual(0, data.eventData.RemoveEventData.Count());
            Assert.AreEqual(0, data.eventData.BatchOperationFinishedCount);
            Assert.AreEqual(0, data.eventData.TopologyChangedEventData.Count);
            Assert.AreEqual(2, l0.eventData.GeometryChangedCount);
            Assert.AreEqual(0, l1.eventData.GeometryChangedCount);

            v[0].Position = new SimPoint3D(-2, -4, -6);
            Assert.AreEqual(5 + 4, data.eventData.GeometryChangedEventData.Count());
            for (int i = 0; i < data.eventData.GeometryChangedEventData.Count(); ++i)
                Assert.AreEqual(1, data.eventData.GeometryChangedEventData[i].Count());
            Assert.AreEqual(l0.line, data.eventData.GeometryChangedEventData[7].First());
            Assert.AreEqual(l1.line, data.eventData.GeometryChangedEventData[8].First());

            Assert.AreEqual(0, data.eventData.AddEventData.Count());
            Assert.AreEqual(0, data.eventData.RemoveEventData.Count());
            Assert.AreEqual(0, data.eventData.BatchOperationFinishedCount);
            Assert.AreEqual(0, data.eventData.TopologyChangedEventData.Count);
            Assert.AreEqual(3, l0.eventData.GeometryChangedCount);
            Assert.AreEqual(1, l1.eventData.GeometryChangedCount);
        }

        [TestMethod]
        public void BatchGeomeryChanged()
        {
            var data = GeometryModelHelper.EmptyModelWithEvents();
            (var v, var e) = TestData(data.layer);
            var l0edges = new Edge[] { e[0], e[1], e[2] };
            var l1edges = new Edge[] { e[0], e[3], e[4] };

            var l0 = LineWithEvents(new Polyline(data.layer, "", l0edges));
            var l1 = LineWithEvents(new Polyline(data.layer, "", l1edges));
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
            Assert.AreEqual(7, data.eventData.GeometryChangedEventData[0].Count());
            Assert.AreEqual(1, data.eventData.GeometryChangedEventData[0].Count(x => x == l0.line));
            Assert.AreEqual(1, data.eventData.GeometryChangedEventData[0].Count(x => x == l1.line));
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
            var l0 = LineWithEvents(new Polyline(data.layer, "", l0edges));

            var replaceEdge = new Edge(data.layer, "", e[2].Vertices);
            l0edges[2] = replaceEdge;

            data.eventData.Reset();
            l0.eventData.Reset();

            l0.line.Edges[2] = new PEdge(replaceEdge, GeometricOrientation.Undefined, l0.line);

            Assert.AreEqual(1, e[0].PEdges.Count);
            Assert.AreEqual(1, e[1].PEdges.Count);
            Assert.AreEqual(0, e[2].PEdges.Count);
            Assert.AreEqual(0, e[3].PEdges.Count);
            Assert.AreEqual(0, e[4].PEdges.Count);
            Assert.AreEqual(1, replaceEdge.PEdges.Count);

            //Next/Prev ptr
            var l0startEdge = l0.line.Edges.First(x => x.Edge == e[0]);

            var l0next = l0startEdge;
            Assert.AreEqual(null, l0startEdge.Prev);

            for (int i = 0; i < 2; ++i)
            {
                Assert.AreEqual(l0next, l0next.Next.Prev);
                Assert.AreEqual(l0edges[i], l0next.Edge);
                l0next = l0next.Next;
            }

            Assert.AreEqual(l0edges[2], l0next.Edge);
            Assert.AreEqual(null, l0next.Next);

            //Events
            Assert.AreEqual(0, data.eventData.GeometryChangedEventData.Count());
            Assert.AreEqual(0, data.eventData.AddEventData.Count());
            Assert.AreEqual(0, data.eventData.RemoveEventData.Count());
            Assert.AreEqual(0, data.eventData.BatchOperationFinishedCount);
            Assert.AreEqual(1, data.eventData.TopologyChangedEventData.Count);
            Assert.AreEqual(0, l0.eventData.GeometryChangedCount);
            Assert.AreEqual(1, l0.eventData.TopologyChangedCount);

            Assert.ThrowsException<Exception>(() => l0.line.Edges[2] = new PEdge(e[4], GeometricOrientation.Undefined, l0.line));
        }

        [TestMethod]
        public void BatchEdgeExchange()
        {
            var data = GeometryModelHelper.EmptyModelWithEvents();
            (var v, var e) = TestData(data.layer);
            var l0edges = new Edge[] { e[0], e[1], e[2] };
            var l0 = LineWithEvents(new Polyline(data.layer, "", l0edges));

            data.eventData.Reset();
            l0.eventData.Reset();

            data.model.Geometry.StartBatchOperation();
            l0.line.Edges.RemoveAt(1);
            l0.line.Edges.RemoveAt(1);

            l0.line.Edges.Add(new PEdge(e[3], GeometricOrientation.Undefined, l0.line));
            l0.line.Edges.Add(new PEdge(e[4], GeometricOrientation.Undefined, l0.line));

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
            Assert.AreEqual(1, l0.eventData.TopologyChangedCount);
        }

        [TestMethod]
        public void VertexExchange()
        {
            var data = GeometryModelHelper.EmptyModelWithEvents();
            (var v, var e) = TestData(data.layer);
            var l0edges = new Edge[] { e[0], e[1], e[2] };
            var l0 = LineWithEvents(new Polyline(data.layer, "", l0edges));

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
            Assert.AreEqual(3, data.eventData.TopologyChangedEventData[0].Count());
            Assert.IsTrue(data.eventData.TopologyChangedEventData[0].Contains(l0.line));
            Assert.IsTrue(data.eventData.TopologyChangedEventData[0].Contains(e[1]));
            Assert.IsTrue(data.eventData.TopologyChangedEventData[0].Contains(e[2]));
            Assert.AreEqual(0, l0.eventData.GeometryChangedCount);
        }

        [TestMethod]
        public void VertexExchangeNotClosed()
        {
            var data = GeometryModelHelper.EmptyModelWithEvents();
            (var v, var e) = TestData(data.layer);
            var l0edges = new Edge[] { e[0], e[1], e[2] };
            var l0 = LineWithEvents(new Polyline(data.layer, "", l0edges));

            data.eventData.Reset();
            l0.eventData.Reset();

            Assert.ThrowsException<Exception>(() => e[1].Vertices[1] = v[4]); //No valid loop
        }

        [TestMethod]
        public void Visibility()
        {
            //Data
            var data = GeometryModelHelper.EmptyModelWithEvents();
            (var v, var e) = TestData(data.layer);
            var l0edges = new Edge[] { e[0], e[1], e[2] };
            var l0 = LineWithEvents(new Polyline(data.layer, "", l0edges));
            data.eventData.Reset();

            Assert.AreEqual(true, data.layer.IsVisible);
            Assert.AreEqual(true, l0.line.IsVisible);
            Assert.AreEqual(true, l0.line.IsActuallyVisible);
            Assert.AreEqual(0, l0.eventData.PropertyChangedData.Count);

            l0.line.IsVisible = false;
            Assert.AreEqual(false, l0.line.IsVisible);
            Assert.AreEqual(false, l0.line.IsActuallyVisible);
            Assert.AreEqual(2, l0.eventData.PropertyChangedData.Count);
            Assert.AreEqual(nameof(Vertex.IsVisible), l0.eventData.PropertyChangedData[0]);
            Assert.AreEqual(nameof(Vertex.IsActuallyVisible), l0.eventData.PropertyChangedData[1]);

            l0.line.IsVisible = true;
            Assert.AreEqual(true, l0.line.IsVisible);
            Assert.AreEqual(true, l0.line.IsActuallyVisible);
            Assert.AreEqual(4, l0.eventData.PropertyChangedData.Count);
            Assert.AreEqual(nameof(Vertex.IsVisible), l0.eventData.PropertyChangedData[2]);
            Assert.AreEqual(nameof(Vertex.IsActuallyVisible), l0.eventData.PropertyChangedData[3]);

            data.layer.IsVisible = false;
            Assert.AreEqual(true, l0.line.IsVisible);
            Assert.AreEqual(false, l0.line.IsActuallyVisible);
            Assert.AreEqual(5, l0.eventData.PropertyChangedData.Count);
            Assert.AreEqual(nameof(Vertex.IsActuallyVisible), l0.eventData.PropertyChangedData[4]);

            data.layer.IsVisible = true;
            Assert.AreEqual(true, l0.line.IsVisible);
            Assert.AreEqual(true, l0.line.IsActuallyVisible);
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
            var l0 = LineWithEvents(new Polyline(data.layer, "", l0edges));
            data.eventData.Reset();

            Assert.AreEqual(0, l0.eventData.PropertyChangedData.Count);

            l0.line.Name = "Renamed";
            Assert.AreEqual("Renamed", l0.line.Name);
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
            var l0 = LineWithEvents(new Polyline(data.layer, "", l0edges));
            data.eventData.Reset();

            Assert.AreEqual(data.layer, l0.line.Layer);
            Assert.AreEqual(1, data.layer.Elements.Count(x => x is Polyline));
            Assert.AreEqual(SimColors.Red, l0.line.Color.Color);

            l0.line.Layer = targetLayer;
            Assert.AreEqual(targetLayer, l0.line.Layer);
            Assert.AreEqual(0, data.layer.Elements.Count(x => x is Polyline));
            Assert.AreEqual(1, targetLayer.Elements.Count(x => x is Polyline));
            Assert.AreEqual(SimColors.Pink, l0.line.Color.Color);

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
    }
}
