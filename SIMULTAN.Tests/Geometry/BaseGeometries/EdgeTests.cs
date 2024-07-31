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
    public class EdgeTests
    {
        private (Edge edge, BaseGeometryEventData eventData) EdgeWithEvents(Edge edge)
        {
            return (edge, new BaseGeometryEventData(edge));
        }

        [TestMethod]
        public void Ctor()
        {
            var data = GeometryModelHelper.EmptyModel();

            var v0 = new Vertex(data.layer, "", new SimPoint3D(1, 2, 3));
            var v1 = new Vertex(data.layer, "", new SimPoint3D(2, 4, 6));
            var v2 = new Vertex(data.layer, "", new SimPoint3D(3, 6, 9));

            Assert.ThrowsException<ArgumentNullException>(() => { Edge e0 = new Edge(null, "", new Vertex[] { v0, v1 }); });
            Assert.ThrowsException<ArgumentNullException>(() => { Edge e0 = new Edge(data.layer, null, new Vertex[] { v0, v1 }); });
            Assert.ThrowsException<ArgumentNullException>(() => { Edge e0 = new Edge(data.layer, "", null); });
            Assert.ThrowsException<ArgumentException>(() => { Edge e0 = new Edge(data.layer, "", new Vertex[] { v0 }); });
            Assert.ThrowsException<ArgumentException>(() => { Edge e0 = new Edge(data.layer, "", new Vertex[] { v0, v1, v2 }); });
            Assert.ThrowsException<ArgumentException>(() => { Edge e0 = new Edge(ulong.MaxValue, data.layer, "", new Vertex[] { v0, v1 }); });

            Edge e1 = new Edge(data.layer, "vertex X", new Vertex[] { v0, v1 });
            {
                Assert.AreEqual("vertex X", e1.Name);

                Assert.AreEqual(2, e1.Vertices.Count, "Vertex Count");
                Assert.IsTrue(e1.Vertices.Contains(v0), "Contains Vertex 0");
                Assert.IsTrue(e1.Vertices.Contains(v1), "Contains Vertex 1");
                Assert.AreEqual(1, v0.Edges.Count, "Edge Count");
                Assert.IsTrue(v0.Edges.Contains(e1), "Contains Edge");
                Assert.AreEqual(1, v1.Edges.Count, "Edge Count");
                Assert.IsTrue(v1.Edges.Contains(e1), "Contains Edge");
                Assert.AreEqual(data.layer, e1.Layer, "Edge Layer");

                Assert.IsTrue(data.model.Geometry.Edges.Count == 1);
                Assert.IsTrue(data.model.Geometry.Edges.Contains(e1));
            }

            Edge e2 = new Edge(99, data.layer, "vertex Y", new Vertex[] { v1, v2 });
            {
                Assert.AreEqual("vertex Y", e2.Name);

                Assert.AreEqual(2, e2.Vertices.Count, "Vertex Count");
                Assert.IsTrue(e2.Vertices.Contains(v1), "Contains Vertex 1");
                Assert.IsTrue(e2.Vertices.Contains(v2), "Contains Vertex 2");

                Assert.AreEqual(1, v0.Edges.Count, "Edge Count");
                Assert.AreEqual(2, v1.Edges.Count, "Edge Count");
                Assert.AreEqual(1, v2.Edges.Count, "Edge Count");

                Assert.IsTrue(v1.Edges.Contains(e2), "Contains Edge");
                Assert.IsTrue(v2.Edges.Contains(e2), "Contains Edge");
                Assert.AreEqual(data.layer, e2.Layer, "Edge Layer");

                Assert.AreEqual((ulong)99, e2.Id);
                Assert.AreEqual(e2, data.model.Geometry.GeometryFromId(99));
            }
        }

        [TestMethod]
        public void Add()
        {
            var data = GeometryModelHelper.EmptyModelWithEvents();

            var v0 = new Vertex(data.layer, "", new SimPoint3D(1, 2, 3));
            var v1 = new Vertex(data.layer, "", new SimPoint3D(2, 4, 6));

            //Add an edge
            Edge e0 = new Edge(data.layer, "", new Vertex[] { v0, v1 });

            Assert.AreEqual(0, data.eventData.BatchOperationFinishedCount);

            Assert.AreEqual(3, data.eventData.TopologyChangedEventData.Count);
            Assert.AreEqual(3, data.eventData.AddEventData.Count);
            for (int i = 0; i < 3; i++)
                Assert.AreEqual(1, data.eventData.AddEventData[i].Count());

            Assert.AreEqual(v0, data.eventData.AddEventData[0].First());
            Assert.AreEqual(v1, data.eventData.AddEventData[1].First());
            Assert.AreEqual(e0, data.eventData.AddEventData[2].First());

            Assert.AreEqual(0, data.eventData.RemoveEventData.Count);
            Assert.AreEqual(0, data.eventData.GeometryChangedEventData.Count);

            Assert.AreEqual(3, data.layer.Elements.Count);
            Assert.IsTrue(data.layer.Elements.Contains(e0));
        }

        [TestMethod]
        public void Remove()
        {
            var data = GeometryModelHelper.EmptyModelWithEvents();

            //Testdata
            var v0 = new Vertex(data.layer, "", new SimPoint3D(1, 2, 3));
            var v1 = new Vertex(data.layer, "", new SimPoint3D(2, 4, 6));
            var e0 = EdgeWithEvents(new Edge(data.layer, "", new Vertex[] { v0, v1 }));
            data.eventData.Reset();

            //Remove from model
            var isRemoved = e0.edge.RemoveFromModel();

            Assert.AreEqual(true, isRemoved);
            Assert.AreEqual(0, data.model.Geometry.Edges.Count);
            Assert.AreEqual(2, data.model.Geometry.Vertices.Count);
            Assert.AreEqual(0, data.eventData.BatchOperationFinishedCount);
            Assert.AreEqual(1, data.eventData.TopologyChangedEventData.Count);
            Assert.AreEqual(0, data.eventData.AddEventData.Count);
            Assert.AreEqual(1, data.eventData.RemoveEventData.Count);
            Assert.AreEqual(1, data.eventData.RemoveEventData[0].Count());
            Assert.AreEqual(e0.edge, data.eventData.RemoveEventData[0].First());
            Assert.AreEqual(0, data.eventData.GeometryChangedEventData.Count);
            Assert.AreEqual(0, e0.eventData.GeometryChangedCount);
            Assert.AreEqual(0, e0.eventData.TopologyChangedCount);
            Assert.AreEqual(0, v0.Edges.Count);
            Assert.AreEqual(0, v1.Edges.Count);
            Assert.AreEqual(0, data.layer.Elements.Count(x => x is Edge));

            //Second remove does nothing
            data.eventData.Reset();
            isRemoved = e0.edge.RemoveFromModel();

            Assert.AreEqual(false, isRemoved);
            Assert.AreEqual(2, data.model.Geometry.Vertices.Count);
            Assert.AreEqual(0, data.eventData.BatchOperationFinishedCount);
            Assert.AreEqual(0, data.eventData.TopologyChangedEventData.Count);
            Assert.AreEqual(0, data.eventData.AddEventData.Count);
            Assert.AreEqual(0, data.eventData.RemoveEventData.Count);
            Assert.AreEqual(0, data.eventData.GeometryChangedEventData.Count);
            Assert.AreEqual(0, e0.eventData.GeometryChangedCount);
            Assert.AreEqual(0, e0.eventData.TopologyChangedCount);
            Assert.AreEqual(0, data.layer.Elements.Count(x => x is Edge));
        }

        [TestMethod]
        public void ReAdd()
        {
            var data = GeometryModelHelper.EmptyModelWithEvents();

            //Testdata
            var v0 = new Vertex(data.layer, "", new SimPoint3D(1, 2, 3));
            var v1 = new Vertex(data.layer, "", new SimPoint3D(2, 4, 6));
            var e0 = EdgeWithEvents(new Edge(data.layer, "", new Vertex[] { v0, v1 }));
            e0.edge.RemoveFromModel();
            data.eventData.Reset();

            //Add event
            e0.edge.AddToModel();

            Assert.AreEqual(1, data.model.Geometry.Edges.Count);
            Assert.AreEqual(0, data.eventData.BatchOperationFinishedCount);
            Assert.AreEqual(1, data.eventData.TopologyChangedEventData.Count);
            Assert.AreEqual(1, data.eventData.AddEventData.Count);
            Assert.AreEqual(1, data.eventData.AddEventData[0].Count());
            Assert.AreEqual(e0.edge, data.eventData.AddEventData[0].First());
            Assert.AreEqual(0, data.eventData.RemoveEventData.Count);
            Assert.AreEqual(0, data.eventData.GeometryChangedEventData.Count);
            Assert.AreEqual(0, e0.eventData.GeometryChangedCount);
            Assert.AreEqual(0, e0.eventData.TopologyChangedCount);
            Assert.AreEqual(1, v0.Edges.Count);
            Assert.AreEqual(1, v1.Edges.Count);
            Assert.AreEqual(e0.edge, v0.Edges[0]);
            Assert.AreEqual(e0.edge, v1.Edges[0]);
            Assert.AreEqual(1, data.layer.Elements.Count(x => x is Edge));


            //Second add throws exception
            Assert.ThrowsException<Exception>(() => e0.edge.AddToModel());
        }

        [TestMethod]
        public void BatchAdd()
        {
            var data = GeometryModelHelper.EmptyModelWithEvents();

            var v0 = new Vertex(data.layer, "", new SimPoint3D(1, 2, 3));
            var v1 = new Vertex(data.layer, "", new SimPoint3D(2, 4, 6));
            var v2 = new Vertex(data.layer, "", new SimPoint3D(3, 6, 9));
            data.eventData.Reset();

            //Prepare data
            data.model.Geometry.StartBatchOperation();

            var e0 = EdgeWithEvents(new Edge(data.layer, "", new Vertex[] { v0, v1 }));
            var e1 = EdgeWithEvents(new Edge(data.layer, "", new Vertex[] { v1, v2 }));

            Assert.AreEqual(0, data.eventData.TopologyChangedEventData.Count);
            Assert.AreEqual(0, data.eventData.AddEventData.Count);
            Assert.AreEqual(0, data.eventData.RemoveEventData.Count);
            Assert.AreEqual(0, data.eventData.GeometryChangedEventData.Count);
            Assert.AreEqual(0, data.eventData.BatchOperationFinishedCount);

            data.model.Geometry.EndBatchOperation();

            //Check events after batch operation
            Assert.AreEqual(1, v0.Edges.Count);
            Assert.AreEqual(2, v1.Edges.Count);
            Assert.AreEqual(1, v2.Edges.Count);
            Assert.AreEqual(1, data.eventData.BatchOperationFinishedCount);
            Assert.AreEqual(1, data.eventData.TopologyChangedEventData.Count);
            Assert.AreEqual(1, data.eventData.AddEventData.Count);
            Assert.AreEqual(2, data.eventData.AddEventData[0].Count());
            Assert.IsTrue(data.eventData.AddEventData[0].Contains(e0.edge));
            Assert.IsTrue(data.eventData.AddEventData[0].Contains(e1.edge));
            Assert.AreEqual(0, data.eventData.RemoveEventData.Count);
            Assert.AreEqual(0, data.eventData.GeometryChangedEventData.Count);
            Assert.AreEqual(0, e0.eventData.GeometryChangedCount);
            Assert.AreEqual(0, e0.eventData.TopologyChangedCount);
            Assert.AreEqual(0, e1.eventData.GeometryChangedCount);
            Assert.AreEqual(0, e1.eventData.TopologyChangedCount);
            Assert.AreEqual(5, data.layer.Elements.Count);
            Assert.AreEqual(2, data.layer.Elements.Count(x => x is Edge));
        }

        [TestMethod]
        public void BatchRemove()
        {
            var data = GeometryModelHelper.EmptyModelWithEvents();

            //Prepare data
            data.model.Geometry.StartBatchOperation();

            var v0 = new Vertex(data.layer, "", new SimPoint3D(1, 2, 3));
            var v1 = new Vertex(data.layer, "", new SimPoint3D(2, 4, 6));
            var v2 = new Vertex(data.layer, "", new SimPoint3D(3, 6, 9));

            var e0 = EdgeWithEvents(new Edge(data.layer, "", new Vertex[] { v0, v1 }));
            var e1 = EdgeWithEvents(new Edge(data.layer, "", new Vertex[] { v1, v2 }));

            data.model.Geometry.EndBatchOperation();
            data.eventData.Reset();

            //Test
            data.model.Geometry.StartBatchOperation();
            e0.edge.RemoveFromModel();
            e1.edge.RemoveFromModel();


            Assert.AreEqual(0, data.eventData.BatchOperationFinishedCount);
            Assert.AreEqual(0, data.eventData.TopologyChangedEventData.Count);
            Assert.AreEqual(0, data.eventData.AddEventData.Count);
            Assert.AreEqual(0, data.eventData.RemoveEventData.Count);
            Assert.AreEqual(0, data.eventData.GeometryChangedEventData.Count);
            data.model.Geometry.EndBatchOperation();

            //Check events after batch operation
            Assert.AreEqual(0, v0.Edges.Count);
            Assert.AreEqual(0, v1.Edges.Count);
            Assert.AreEqual(0, v2.Edges.Count);
            Assert.AreEqual(0, data.layer.Elements.Count(x => x is Edge));

            Assert.AreEqual(1, data.eventData.BatchOperationFinishedCount);
            Assert.AreEqual(1, data.eventData.TopologyChangedEventData.Count);
            Assert.AreEqual(0, data.eventData.AddEventData.Count);
            Assert.AreEqual(1, data.eventData.RemoveEventData.Count);
            Assert.AreEqual(2, data.eventData.RemoveEventData[0].Count());
            Assert.IsTrue(data.eventData.RemoveEventData[0].Contains(e0.edge));
            Assert.IsTrue(data.eventData.RemoveEventData[0].Contains(e1.edge));
            Assert.AreEqual(0, data.eventData.GeometryChangedEventData.Count);
            Assert.AreEqual(0, e0.eventData.GeometryChangedCount);
            Assert.AreEqual(0, e0.eventData.TopologyChangedCount);
            Assert.AreEqual(0, e1.eventData.GeometryChangedCount);
            Assert.AreEqual(0, e1.eventData.TopologyChangedCount);
        }

        [TestMethod]
        public void GeometryChanged()
        {
            var data = GeometryModelHelper.EmptyModelWithEvents();

            //Prepare data
            data.model.Geometry.StartBatchOperation();

            var v0 = new Vertex(data.layer, "", new SimPoint3D(1, 2, 3));
            var v1 = new Vertex(data.layer, "", new SimPoint3D(2, 4, 6));
            var v2 = new Vertex(data.layer, "", new SimPoint3D(3, 6, 9));

            var e0 = EdgeWithEvents(new Edge(data.layer, "", new Vertex[] { v0, v1 }));
            var e1 = EdgeWithEvents(new Edge(data.layer, "", new Vertex[] { v1, v2 }));

            data.model.Geometry.EndBatchOperation();
            data.eventData.Reset();

            v0.Position = new SimPoint3D(1, 2, 3);
            Assert.AreEqual(2, data.eventData.GeometryChangedEventData.Count(), "ChangedEvent");
            Assert.AreEqual(1, data.eventData.GeometryChangedEventData[0].Count(), "ChangedEvent");
            Assert.IsTrue(data.eventData.GeometryChangedEventData[0].Contains(v0), "ChangedEvent.Contains");
            Assert.AreEqual(1, data.eventData.GeometryChangedEventData[1].Count(), "ChangedEvent");
            Assert.IsTrue(data.eventData.GeometryChangedEventData[1].Contains(e0.edge), "ChangedEvent.Contains");
            Assert.AreEqual(0, data.eventData.AddEventData.Count(), "AddEvent");
            Assert.AreEqual(0, data.eventData.RemoveEventData.Count(), "RemoveEvent");
            Assert.AreEqual(0, data.eventData.BatchOperationFinishedCount, "BatchOperationFinishedCount");
            Assert.AreEqual(0, data.eventData.TopologyChangedEventData.Count, "TopologyChangedCount");
            Assert.AreEqual(1, e0.eventData.GeometryChangedCount);
            Assert.AreEqual(0, e0.eventData.TopologyChangedCount);
            Assert.AreEqual(0, e1.eventData.GeometryChangedCount);
            Assert.AreEqual(0, e1.eventData.TopologyChangedCount);

            v1.Position = new SimPoint3D(-1, -2, -3);
            Assert.AreEqual(5, data.eventData.GeometryChangedEventData.Count(), "ChangedEvent");
            Assert.AreEqual(1, data.eventData.GeometryChangedEventData[2].Count(), "ChangedEvent");
            Assert.IsTrue(data.eventData.GeometryChangedEventData[2].Contains(v1), "ChangedEvent.Contains");
            Assert.AreEqual(1, data.eventData.GeometryChangedEventData[3].Count(), "ChangedEvent");
            Assert.IsTrue(data.eventData.GeometryChangedEventData[3].Contains(e0.edge), "ChangedEvent.Contains");
            Assert.AreEqual(1, data.eventData.GeometryChangedEventData[4].Count(), "ChangedEvent");
            Assert.IsTrue(data.eventData.GeometryChangedEventData[4].Contains(e1.edge), "ChangedEvent.Contains");
            Assert.AreEqual(0, data.eventData.AddEventData.Count(), "AddEvent");
            Assert.AreEqual(0, data.eventData.RemoveEventData.Count(), "RemoveEvent");
            Assert.AreEqual(0, data.eventData.BatchOperationFinishedCount, "BatchOperationFinishedCount");
            Assert.AreEqual(0, data.eventData.TopologyChangedEventData.Count, "TopologyChangedCount");
            Assert.AreEqual(2, e0.eventData.GeometryChangedCount);
            Assert.AreEqual(0, e0.eventData.TopologyChangedCount);
            Assert.AreEqual(1, e1.eventData.GeometryChangedCount);
            Assert.AreEqual(0, e1.eventData.TopologyChangedCount);
        }

        [TestMethod]
        public void BatchGeometryChanged()
        {
            var data = GeometryModelHelper.EmptyModelWithEvents();

            //Prepare data
            data.model.Geometry.StartBatchOperation();

            var v0 = new Vertex(data.layer, "", new SimPoint3D(1, 2, 3));
            var v1 = new Vertex(data.layer, "", new SimPoint3D(2, 4, 6));
            var v2 = new Vertex(data.layer, "", new SimPoint3D(3, 6, 9));

            var e0 = EdgeWithEvents(new Edge(data.layer, "", new Vertex[] { v0, v1 }));
            var e1 = EdgeWithEvents(new Edge(data.layer, "", new Vertex[] { v1, v2 }));

            data.model.Geometry.EndBatchOperation();
            data.eventData.Reset();

            //Test
            data.model.Geometry.StartBatchOperation();

            v0.Position = new SimPoint3D(1, 2, 3);
            Assert.AreEqual(new SimPoint3D(1, 2, 3), v0.Position, "Position 2");
            Assert.AreEqual(0, data.eventData.GeometryChangedEventData.Count(), "ChangedEvent");
            Assert.AreEqual(0, data.eventData.AddEventData.Count(), "AddEvent");
            Assert.AreEqual(0, data.eventData.RemoveEventData.Count(), "RemoveEvent");
            Assert.AreEqual(0, data.eventData.BatchOperationFinishedCount, "BatchOperationFinishedCount");
            Assert.AreEqual(0, data.eventData.TopologyChangedEventData.Count, "TopologyChangedCount");
            Assert.AreEqual(0, e0.eventData.GeometryChangedCount);
            Assert.AreEqual(0, e0.eventData.TopologyChangedCount);
            Assert.AreEqual(0, e1.eventData.GeometryChangedCount);
            Assert.AreEqual(0, e1.eventData.TopologyChangedCount);

            v1.Position = new SimPoint3D(-1, -2, -3);
            Assert.AreEqual(new SimPoint3D(-1, -2, -3), v1.Position, "Position 2");
            Assert.AreEqual(0, data.eventData.GeometryChangedEventData.Count(), "ChangedEvent");
            Assert.AreEqual(0, data.eventData.AddEventData.Count(), "AddEvent");
            Assert.AreEqual(0, data.eventData.RemoveEventData.Count(), "RemoveEvent");
            Assert.AreEqual(0, data.eventData.BatchOperationFinishedCount, "BatchOperationFinishedCount");
            Assert.AreEqual(0, data.eventData.TopologyChangedEventData.Count, "TopologyChangedCount");
            Assert.AreEqual(0, e0.eventData.GeometryChangedCount);
            Assert.AreEqual(0, e0.eventData.TopologyChangedCount);
            Assert.AreEqual(0, e1.eventData.GeometryChangedCount);
            Assert.AreEqual(0, e1.eventData.TopologyChangedCount);

            data.model.Geometry.EndBatchOperation();

            Assert.AreEqual(1, data.eventData.GeometryChangedEventData.Count(), "ChangedEvent");
            Assert.AreEqual(4, data.eventData.GeometryChangedEventData[0].Count(), "ChangedEvent");
            Assert.IsTrue(data.eventData.GeometryChangedEventData[0].Contains(v0), "ChangedEvent.Contains");
            Assert.IsTrue(data.eventData.GeometryChangedEventData[0].Contains(v1), "ChangedEvent.Contains");
            Assert.IsTrue(data.eventData.GeometryChangedEventData[0].Contains(e0.edge), "ChangedEvent.Contains");
            Assert.IsTrue(data.eventData.GeometryChangedEventData[0].Contains(e1.edge), "ChangedEvent.Contains");
            Assert.AreEqual(0, data.eventData.AddEventData.Count(), "AddEvent");
            Assert.AreEqual(0, data.eventData.RemoveEventData.Count(), "RemoveEvent");
            Assert.AreEqual(1, data.eventData.BatchOperationFinishedCount, "BatchOperationFinishedCount");
            Assert.AreEqual(0, data.eventData.TopologyChangedEventData.Count, "TopologyChangedCount");
            Assert.AreEqual(1, e0.eventData.GeometryChangedCount);
            Assert.AreEqual(0, e0.eventData.TopologyChangedCount);
            Assert.AreEqual(1, e1.eventData.GeometryChangedCount);
            Assert.AreEqual(0, e1.eventData.TopologyChangedCount);
        }

        [TestMethod]
        public void ExchangeVertex()
        {
            var data = GeometryModelHelper.EmptyModelWithEvents();

            var v0 = new Vertex(data.layer, "", new SimPoint3D(1, 2, 3));
            var v1 = new Vertex(data.layer, "", new SimPoint3D(2, 4, 6));
            var v2 = new Vertex(data.layer, "", new SimPoint3D(3, 6, 9));

            var e0 = EdgeWithEvents(new Edge(data.layer, "", new Vertex[] { v0, v1 }));

            data.eventData.Reset();

            //Change vertex
            e0.edge.Vertices[0] = v2;

            Assert.AreEqual(2, e0.edge.Vertices.Count);
            Assert.AreEqual(v2, e0.edge.Vertices[0]);
            Assert.AreEqual(v1, e0.edge.Vertices[1]);

            Assert.AreEqual(0, v0.Edges.Count);
            Assert.AreEqual(1, v1.Edges.Count);
            Assert.AreEqual(1, v2.Edges.Count);

            Assert.AreEqual(e0.edge, v1.Edges[0]);
            Assert.AreEqual(e0.edge, v2.Edges[0]);

            Assert.AreEqual(1, data.eventData.TopologyChangedEventData.Count, "TopologyChangedCount");
            Assert.AreEqual(0, data.eventData.GeometryChangedEventData.Count(), "ChangedEvent");
            Assert.AreEqual(0, data.eventData.AddEventData.Count(), "AddEvent");
            Assert.AreEqual(0, data.eventData.RemoveEventData.Count(), "RemoveEvent");
            Assert.AreEqual(0, data.eventData.BatchOperationFinishedCount, "BatchOperationFinishedCount");
            Assert.AreEqual(0, e0.eventData.GeometryChangedCount);
            Assert.AreEqual(1, e0.eventData.TopologyChangedCount);
        }

        [TestMethod]
        public void BatchExchangeVertex()
        {
            var data = GeometryModelHelper.EmptyModelWithEvents();

            var v0 = new Vertex(data.layer, "", new SimPoint3D(1, 2, 3));
            var v1 = new Vertex(data.layer, "", new SimPoint3D(2, 4, 6));
            var v2 = new Vertex(data.layer, "", new SimPoint3D(3, 6, 9));
            var v3 = new Vertex(data.layer, "", new SimPoint3D(4, 8, 12));

            var e0 = EdgeWithEvents(new Edge(data.layer, "", new Vertex[] { v0, v1 }));

            data.eventData.Reset();

            data.model.Geometry.StartBatchOperation();

            //Change vertex1
            e0.edge.Vertices[0] = v2;
            Assert.AreEqual(0, data.eventData.TopologyChangedEventData.Count);
            Assert.AreEqual(0, data.eventData.GeometryChangedEventData.Count());
            Assert.AreEqual(0, data.eventData.AddEventData.Count());
            Assert.AreEqual(0, data.eventData.RemoveEventData.Count());
            Assert.AreEqual(0, data.eventData.BatchOperationFinishedCount);
            Assert.AreEqual(0, e0.eventData.GeometryChangedCount);
            Assert.AreEqual(0, e0.eventData.TopologyChangedCount);

            //Change vertex 2
            e0.edge.Vertices[1] = v3;
            Assert.AreEqual(0, data.eventData.TopologyChangedEventData.Count);
            Assert.AreEqual(0, data.eventData.GeometryChangedEventData.Count());
            Assert.AreEqual(0, data.eventData.AddEventData.Count());
            Assert.AreEqual(0, data.eventData.RemoveEventData.Count());
            Assert.AreEqual(0, data.eventData.BatchOperationFinishedCount);
            Assert.AreEqual(0, e0.eventData.GeometryChangedCount);
            Assert.AreEqual(0, e0.eventData.TopologyChangedCount);

            data.model.Geometry.EndBatchOperation();

            //Check after batch
            Assert.AreEqual(2, e0.edge.Vertices.Count);
            Assert.AreEqual(v2, e0.edge.Vertices[0]);
            Assert.AreEqual(v3, e0.edge.Vertices[1]);

            Assert.AreEqual(0, v0.Edges.Count);
            Assert.AreEqual(0, v1.Edges.Count);
            Assert.AreEqual(1, v2.Edges.Count);
            Assert.AreEqual(1, v3.Edges.Count);

            Assert.AreEqual(e0.edge, v2.Edges[0]);
            Assert.AreEqual(e0.edge, v3.Edges[0]);

            Assert.AreEqual(1, data.eventData.TopologyChangedEventData.Count);
            Assert.AreEqual(0, data.eventData.GeometryChangedEventData.Count());
            Assert.AreEqual(0, data.eventData.AddEventData.Count());
            Assert.AreEqual(0, data.eventData.RemoveEventData.Count());
            Assert.AreEqual(1, data.eventData.BatchOperationFinishedCount);
            Assert.AreEqual(0, e0.eventData.GeometryChangedCount);
            Assert.AreEqual(1, e0.eventData.TopologyChangedCount);
        }

        [TestMethod]
        public void Visibility()
        {
            var data = GeometryModelHelper.EmptyModelWithEvents();

            //Add an edge
            var v0 = new Vertex(data.layer, "", new SimPoint3D(1, 2, 3));
            var v1 = new Vertex(data.layer, "", new SimPoint3D(2, 4, 6));
            var e0 = EdgeWithEvents(new Edge(data.layer, "", new Vertex[] { v0, v1 }));
            data.eventData.Reset();

            Assert.AreEqual(true, data.layer.IsVisible);
            Assert.AreEqual(true, e0.edge.IsVisible);
            Assert.AreEqual(true, e0.edge.IsActuallyVisible);
            Assert.AreEqual(0, e0.eventData.PropertyChangedData.Count);

            e0.edge.IsVisible = false;
            Assert.AreEqual(false, e0.edge.IsVisible);
            Assert.AreEqual(false, e0.edge.IsActuallyVisible);
            Assert.AreEqual(2, e0.eventData.PropertyChangedData.Count);
            Assert.AreEqual(nameof(Vertex.IsVisible), e0.eventData.PropertyChangedData[0]);
            Assert.AreEqual(nameof(Vertex.IsActuallyVisible), e0.eventData.PropertyChangedData[1]);

            e0.edge.IsVisible = true;
            Assert.AreEqual(true, e0.edge.IsVisible);
            Assert.AreEqual(true, e0.edge.IsActuallyVisible);
            Assert.AreEqual(4, e0.eventData.PropertyChangedData.Count);
            Assert.AreEqual(nameof(Vertex.IsVisible), e0.eventData.PropertyChangedData[2]);
            Assert.AreEqual(nameof(Vertex.IsActuallyVisible), e0.eventData.PropertyChangedData[3]);

            data.layer.IsVisible = false;
            Assert.AreEqual(true, e0.edge.IsVisible);
            Assert.AreEqual(false, e0.edge.IsActuallyVisible);
            Assert.AreEqual(5, e0.eventData.PropertyChangedData.Count);
            Assert.AreEqual(nameof(Vertex.IsActuallyVisible), e0.eventData.PropertyChangedData[4]);

            data.layer.IsVisible = true;
            Assert.AreEqual(true, e0.edge.IsVisible);
            Assert.AreEqual(true, e0.edge.IsActuallyVisible);
            Assert.AreEqual(6, e0.eventData.PropertyChangedData.Count);
            Assert.AreEqual(nameof(Vertex.IsActuallyVisible), e0.eventData.PropertyChangedData[5]);

            Assert.AreEqual(0, e0.eventData.GeometryChangedCount);
            Assert.AreEqual(0, e0.eventData.TopologyChangedCount);
        }

        [TestMethod]
        public void Name()
        {
            var data = GeometryModelHelper.EmptyModelWithEvents();

            //Add a vertex
            var v0 = new Vertex(data.layer, "", new SimPoint3D(1, 2, 3));
            var v1 = new Vertex(data.layer, "", new SimPoint3D(2, 4, 6));
            var e0 = EdgeWithEvents(new Edge(data.layer, "", new Vertex[] { v0, v1 }));
            data.eventData.Reset();
            Assert.AreEqual(0, e0.eventData.PropertyChangedData.Count);

            e0.edge.Name = "RenamedVertex";
            Assert.AreEqual("RenamedVertex", e0.edge.Name);
            Assert.AreEqual(1, e0.eventData.PropertyChangedData.Count);
            Assert.AreEqual(nameof(Vertex.Name), e0.eventData.PropertyChangedData[0]);

            Assert.AreEqual(0, e0.eventData.GeometryChangedCount);
            Assert.AreEqual(0, e0.eventData.TopologyChangedCount);
        }

        [TestMethod]
        public void MoveToLayer()
        {
            var data = GeometryModelHelper.EmptyModelWithEvents();
            Layer targetLayer = new Layer(data.model.Geometry, "TargetLayer") { Color = new DerivedColor(SimColors.Pink) };

            //Add an edge
            var v0 = new Vertex(data.layer, "", new SimPoint3D(1, 2, 3));
            var v1 = new Vertex(data.layer, "", new SimPoint3D(2, 4, 6));
            var e0 = EdgeWithEvents(new Edge(data.layer, "", new Vertex[] { v0, v1 }));
            data.eventData.Reset();

            Assert.AreEqual(data.layer, e0.edge.Layer);
            Assert.AreEqual(1, data.layer.Elements.Count(x => x is Edge));
            Assert.AreEqual(SimColors.Red, e0.edge.Color.Color);

            e0.edge.Layer = targetLayer;
            Assert.AreEqual(targetLayer, e0.edge.Layer);
            Assert.AreEqual(0, data.layer.Elements.Count(x => x is Edge));
            Assert.AreEqual(1, targetLayer.Elements.Count(x => x is Edge));
            Assert.AreEqual(SimColors.Pink, e0.edge.Color.Color);

            Assert.AreEqual(0, data.eventData.GeometryChangedEventData.Count);
            Assert.AreEqual(0, data.eventData.AddEventData.Count);
            Assert.AreEqual(0, data.eventData.RemoveEventData.Count);
            Assert.AreEqual(0, data.eventData.BatchOperationFinishedCount);
            Assert.AreEqual(0, data.eventData.TopologyChangedEventData.Count);
            Assert.AreEqual(0, e0.eventData.GeometryChangedCount);
            Assert.AreEqual(3, e0.eventData.PropertyChangedData.Count);
            Assert.IsTrue(e0.eventData.PropertyChangedData.Contains("Layer"));
            Assert.IsTrue(e0.eventData.PropertyChangedData.Contains("Color"));
            Assert.IsTrue(e0.eventData.PropertyChangedData.Contains("IsActuallyVisible"));
        }
    }
}
