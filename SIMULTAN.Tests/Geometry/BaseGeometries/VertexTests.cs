using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data.Geometry;
using SIMULTAN.Tests.Geometry.EventData;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace SIMULTAN.Tests.Geometry.BaseGeometries
{
    [TestClass]
    public class VertexTests
    {
        private (Vertex v, BaseGeometryEventData eventData) VertexWithEvents(Vertex v)
        {
            return (v, new BaseGeometryEventData(v));
        }

        [TestMethod]
        public void Ctor()
        {
            var empty = GeometryModelHelper.EmptyModel();

            Assert.ThrowsException<ArgumentNullException>(() => { Vertex v0 = new Vertex(null, "", new Point3D(0, 0, 0)); });
            Assert.ThrowsException<ArgumentNullException>(() => { Vertex v0 = new Vertex(empty.layer, null, new Point3D(0, 0, 0)); });
            Assert.ThrowsException<ArgumentException>(() => { Vertex v0 = new Vertex(ulong.MaxValue, empty.layer, "", new Point3D(0, 0, 0)); });

            Vertex v = new Vertex(empty.layer, "", new Point3D(1, 2, 3));
            Assert.AreEqual(1, empty.model.Geometry.Vertices.Count);
            Assert.IsTrue(empty.model.Geometry.Vertices.Contains(v));
            Assert.AreEqual(empty.layer, v.Layer);
            Assert.AreEqual(new Point3D(1, 2, 3), v.Position);
            Assert.AreEqual(true, v.IsVisible);
            Assert.AreEqual(true, v.IsActuallyVisible);

            Vertex v2 = new Vertex(99, empty.layer, "", new Point3D(3, 4, 5));
            Assert.AreEqual(2, empty.model.Geometry.Vertices.Count);
            Assert.IsTrue(empty.model.Geometry.Vertices.Contains(v2));
            Assert.AreEqual(empty.layer, v2.Layer);
            Assert.AreEqual(new Point3D(3, 4, 5), v2.Position);
            Assert.AreEqual((ulong)99, v2.Id);

            Assert.AreEqual(v2, empty.model.Geometry.GeometryFromId(99));
        }

        [TestMethod]
        public void Add()
        {
            var data = GeometryModelHelper.EmptyModelWithEvents();

            //Add a vertex
            var v0 = VertexWithEvents(new Vertex(data.layer, "", new Point3D(0, 0, 0)));

            Assert.AreEqual(0, data.eventData.BatchOperationFinishedCount);
            Assert.AreEqual(1, data.eventData.TopologyChangedEventData.Count);
            Assert.AreEqual(1, data.eventData.AddEventData.Count);
            Assert.AreEqual(1, data.eventData.AddEventData[0].Count());
            Assert.AreEqual(v0.v, data.eventData.AddEventData[0].First());
            Assert.AreEqual(0, data.eventData.RemoveEventData.Count);
            Assert.AreEqual(0, data.eventData.GeometryChangedEventData.Count);
            Assert.AreEqual(0, v0.eventData.GeometryChangedCount);
            Assert.AreEqual(0, v0.eventData.PropertyChangedData.Count);
            Assert.AreEqual(1, data.layer.Elements.Count);
        }

        [TestMethod]
        public void Remove()
        {
            var data = GeometryModelHelper.EmptyModelWithEvents();

            //Add a vertex
            var v0 = VertexWithEvents(new Vertex(data.layer, "", new Point3D(0, 0, 0)));
            data.eventData.Reset();

            //Remove vertex
            var isDeleted = v0.v.RemoveFromModel();

            Assert.AreEqual(true, isDeleted);
            Assert.AreEqual(0, data.model.Geometry.Vertices.Count);
            Assert.AreEqual(0, data.eventData.BatchOperationFinishedCount);
            Assert.AreEqual(1, data.eventData.TopologyChangedEventData.Count);
            Assert.AreEqual(0, data.eventData.AddEventData.Count);
            Assert.AreEqual(1, data.eventData.RemoveEventData.Count);
            Assert.AreEqual(1, data.eventData.RemoveEventData[0].Count());
            Assert.AreEqual(v0.v, data.eventData.RemoveEventData[0].First());
            Assert.AreEqual(0, data.eventData.GeometryChangedEventData.Count);
            Assert.AreEqual(0, v0.eventData.GeometryChangedCount);
            Assert.AreEqual(0, v0.eventData.PropertyChangedData.Count);
            Assert.AreEqual(0, data.layer.Elements.Count);

            //Double delete should be possible, but do nothing.
            data.eventData.Reset();
            isDeleted = v0.v.RemoveFromModel();

            Assert.AreEqual(false, isDeleted);
            Assert.AreEqual(0, data.model.Geometry.Vertices.Count);
            Assert.AreEqual(0, data.eventData.BatchOperationFinishedCount);
            Assert.AreEqual(0, data.eventData.TopologyChangedEventData.Count);
            Assert.AreEqual(0, data.eventData.AddEventData.Count);
            Assert.AreEqual(0, data.eventData.RemoveEventData.Count);
            Assert.AreEqual(0, data.eventData.GeometryChangedEventData.Count);
            Assert.AreEqual(0, v0.eventData.GeometryChangedCount);
            Assert.AreEqual(0, v0.eventData.PropertyChangedData.Count);
            Assert.AreEqual(0, data.layer.Elements.Count);
        }

        [TestMethod]
        public void ReAdd()
        {
            var data = GeometryModelHelper.EmptyModelWithEvents();

            //Add a vertex
            var v0 = VertexWithEvents(new Vertex(data.layer, "", new Point3D(0, 0, 0)));

            //Remove vertex
            v0.v.RemoveFromModel();
            data.eventData.Reset();

            //Add event
            v0.v.AddToModel();

            Assert.AreEqual(0, data.eventData.BatchOperationFinishedCount);
            Assert.AreEqual(1, data.eventData.TopologyChangedEventData.Count);
            Assert.AreEqual(1, data.eventData.AddEventData.Count);
            Assert.AreEqual(1, data.eventData.AddEventData[0].Count());
            Assert.AreEqual(v0.v, data.eventData.AddEventData[0].First());
            Assert.AreEqual(0, data.eventData.RemoveEventData.Count);
            Assert.AreEqual(0, data.eventData.GeometryChangedEventData.Count);
            Assert.AreEqual(0, v0.eventData.GeometryChangedCount);
            Assert.AreEqual(0, v0.eventData.PropertyChangedData.Count);
            Assert.AreEqual(1, data.layer.Elements.Count);

            //Second add should fail with exception
            Assert.ThrowsException<Exception>(() => v0.v.AddToModel());
        }

        [TestMethod]
        public void BatchAdd()
        {
            var data = GeometryModelHelper.EmptyModelWithEvents();

            //Prepare data
            data.model.Geometry.StartBatchOperation();

            var v0 = VertexWithEvents(new Vertex(data.layer, "", new Point3D(0, 0, 0)));
            var v1 = VertexWithEvents(new Vertex(data.layer, "", new Point3D(0, 0, 0)));
            var v2 = VertexWithEvents(new Vertex(data.layer, "", new Point3D(0, 0, 0)));

            Assert.AreEqual(0, data.eventData.TopologyChangedEventData.Count);
            Assert.AreEqual(0, data.eventData.AddEventData.Count);
            Assert.AreEqual(0, data.eventData.RemoveEventData.Count);
            Assert.AreEqual(0, data.eventData.GeometryChangedEventData.Count);

            data.model.Geometry.EndBatchOperation();

            //Check events after batch operation
            Assert.AreEqual(1, data.eventData.BatchOperationFinishedCount);
            Assert.AreEqual(1, data.eventData.TopologyChangedEventData.Count);
            Assert.AreEqual(1, data.eventData.AddEventData.Count);
            Assert.AreEqual(3, data.eventData.AddEventData[0].Count());
            Assert.IsTrue(data.eventData.AddEventData[0].Contains(v0.v));
            Assert.IsTrue(data.eventData.AddEventData[0].Contains(v1.v));
            Assert.IsTrue(data.eventData.AddEventData[0].Contains(v2.v));
            Assert.AreEqual(0, data.eventData.RemoveEventData.Count);
            Assert.AreEqual(0, data.eventData.GeometryChangedEventData.Count);
            Assert.AreEqual(0, v0.eventData.GeometryChangedCount);
            Assert.AreEqual(0, v0.eventData.PropertyChangedData.Count);
            Assert.AreEqual(0, v1.eventData.GeometryChangedCount);
            Assert.AreEqual(0, v1.eventData.PropertyChangedData.Count);
            Assert.AreEqual(0, v2.eventData.GeometryChangedCount);
            Assert.AreEqual(0, v2.eventData.PropertyChangedData.Count);
        }

        [TestMethod]
        public void BatchRemove()
        {
            var data = GeometryModelHelper.EmptyModelWithEvents();

            //Prepare data
            data.model.Geometry.StartBatchOperation();

            var v0 = VertexWithEvents(new Vertex(data.layer, "", new Point3D(0, 0, 0)));
            var v1 = VertexWithEvents(new Vertex(data.layer, "", new Point3D(0, 0, 0)));
            var v2 = VertexWithEvents(new Vertex(data.layer, "", new Point3D(0, 0, 0)));

            data.model.Geometry.EndBatchOperation();

            data.eventData.Reset();

            data.model.Geometry.StartBatchOperation();
            v0.v.RemoveFromModel();
            v2.v.RemoveFromModel();
            v1.v.RemoveFromModel();

            Assert.AreEqual(0, data.eventData.TopologyChangedEventData.Count);
            Assert.AreEqual(0, data.eventData.AddEventData.Count);
            Assert.AreEqual(0, data.eventData.RemoveEventData.Count);
            Assert.AreEqual(0, data.eventData.GeometryChangedEventData.Count);
            data.model.Geometry.EndBatchOperation();

            //Check events after batch operation
            Assert.AreEqual(1, data.eventData.BatchOperationFinishedCount);
            Assert.AreEqual(1, data.eventData.TopologyChangedEventData.Count);
            Assert.AreEqual(0, data.eventData.AddEventData.Count);
            Assert.AreEqual(1, data.eventData.RemoveEventData.Count);
            Assert.AreEqual(3, data.eventData.RemoveEventData[0].Count());
            Assert.IsTrue(data.eventData.RemoveEventData[0].Contains(v0.v));
            Assert.IsTrue(data.eventData.RemoveEventData[0].Contains(v1.v));
            Assert.IsTrue(data.eventData.RemoveEventData[0].Contains(v2.v));
            Assert.AreEqual(0, data.eventData.GeometryChangedEventData.Count);
            Assert.AreEqual(0, v0.eventData.GeometryChangedCount);
            Assert.AreEqual(0, v0.eventData.PropertyChangedData.Count);
            Assert.AreEqual(0, v1.eventData.GeometryChangedCount);
            Assert.AreEqual(0, v1.eventData.PropertyChangedData.Count);
            Assert.AreEqual(0, v2.eventData.GeometryChangedCount);
            Assert.AreEqual(0, v2.eventData.PropertyChangedData.Count);
        }

        [TestMethod]
        public void Clone()
        {
            var data = GeometryModelHelper.EmptyModelWithEvents();

            Vertex v0 = new Vertex(data.layer, "", new Point3D(0, 0, 0));
            v0.IsVisible = false;

            //Clone on same layer
            var v1 = v0.Clone();
            Assert.AreEqual(v0.Name, v1.Name, "Name");
            Assert.AreNotEqual(v0.Id, v1.Id, "ID");
            Assert.AreEqual(v0.Layer, v1.Layer, "Layer");
            Assert.AreEqual(v0.Position, v1.Position, "Position");
            Assert.AreEqual(v0.Color.LocalColor, v1.Color.LocalColor, "Color.LocalColor");
            Assert.AreEqual(v0.Color.IsFromParent, v1.Color.IsFromParent, "Color.IsFromParent");
            Assert.AreEqual(v0.IsVisible, v1.IsVisible, "IsVisible");

            var l2 = new Layer(data.model.Geometry, "Layer2");
            data.layer.Layers.Add(l2);

            //Clone to other layer
            var v2 = v0.Clone(l2);
            Assert.AreEqual(v0.Name, v2.Name, "Name");
            Assert.AreNotEqual(v0.Id, v2.Id, "ID");
            Assert.AreEqual(l2, v2.Layer, "Layer");
            Assert.AreEqual(v0.Position, v2.Position, "Position");
            Assert.AreEqual(v0.Color.LocalColor, v2.Color.LocalColor, "Color.LocalColor");
            Assert.AreEqual(v0.Color.IsFromParent, v2.Color.IsFromParent, "Color.IsFromParent");
            Assert.AreEqual(v0.IsVisible, v2.IsVisible, "IsVisible");
        }

        [TestMethod]
        public void GeometryChanged()
        {
            var data = GeometryModelHelper.EmptyModelWithEvents();

            var v0 = VertexWithEvents(new Vertex(data.layer, "", new Point3D(0, 0, 0)));
            var v1 = VertexWithEvents(new Vertex(data.layer, "", new Point3D(2, 4, 6)));

            Assert.AreEqual(new Point3D(0, 0, 0), v0.v.Position, "Position 1");
            Assert.AreEqual(new Point3D(2, 4, 6), v1.v.Position, "Position 1");
            data.eventData.Reset();

            v0.v.Position = new Point3D(1, 2, 3);
            Assert.AreEqual(new Point3D(1, 2, 3), v0.v.Position, "Position 2");
            Assert.AreEqual(1, data.eventData.GeometryChangedEventData.Count(), "ChangedEvent");
            Assert.AreEqual(1, data.eventData.GeometryChangedEventData[0].Count(), "ChangedEvent[0]");
            Assert.IsTrue(data.eventData.GeometryChangedEventData[0].Contains(v0.v), "ChangedEvent[0].Contains");
            Assert.AreEqual(0, data.eventData.AddEventData.Count(), "AddEvent");
            Assert.AreEqual(0, data.eventData.RemoveEventData.Count(), "RemoveEvent");
            Assert.AreEqual(0, data.eventData.BatchOperationFinishedCount, "BatchOperationFinishedCount");
            Assert.AreEqual(0, data.eventData.TopologyChangedEventData.Count, "TopologyChangedCount");
            Assert.AreEqual(1, v0.eventData.GeometryChangedCount);
            Assert.AreEqual(1, v0.eventData.PropertyChangedData.Count);
            Assert.AreEqual(nameof(Vertex.Position), v0.eventData.PropertyChangedData[0]);
            Assert.AreEqual(0, v1.eventData.GeometryChangedCount);
            Assert.AreEqual(0, v1.eventData.PropertyChangedData.Count);

            v1.v.Position = new Point3D(-1, -2, -3);
            Assert.AreEqual(new Point3D(-1, -2, -3), v1.v.Position, "Position 2");
            Assert.AreEqual(2, data.eventData.GeometryChangedEventData.Count(), "ChangedEvent");
            Assert.AreEqual(1, data.eventData.GeometryChangedEventData[1].Count(), "ChangedEvent[0]");
            Assert.IsTrue(data.eventData.GeometryChangedEventData[1].Contains(v1.v), "ChangedEvent[0].Contains");
            Assert.AreEqual(0, data.eventData.AddEventData.Count(), "AddEvent");
            Assert.AreEqual(0, data.eventData.RemoveEventData.Count(), "RemoveEvent");
            Assert.AreEqual(0, data.eventData.BatchOperationFinishedCount, "BatchOperationFinishedCount");
            Assert.AreEqual(0, data.eventData.TopologyChangedEventData.Count, "TopologyChangedCount");
            Assert.AreEqual(1, v0.eventData.GeometryChangedCount);
            Assert.AreEqual(1, v0.eventData.PropertyChangedData.Count);
            Assert.AreEqual(1, v1.eventData.GeometryChangedCount);
            Assert.AreEqual(1, v1.eventData.PropertyChangedData.Count);
            Assert.AreEqual(nameof(Vertex.Position), v1.eventData.PropertyChangedData[0]);
        }

        [TestMethod]
        public void BatchGeometryChanged()
        {
            var data = GeometryModelHelper.EmptyModelWithEvents();

            var v0 = VertexWithEvents(new Vertex(data.layer, "", new Point3D(0, 0, 0)));
            var v1 = VertexWithEvents(new Vertex(data.layer, "", new Point3D(2, 4, 6)));

            Assert.AreEqual(new Point3D(0, 0, 0), v0.v.Position, "Position 1");
            Assert.AreEqual(new Point3D(2, 4, 6), v1.v.Position, "Position 1");
            data.eventData.Reset();

            data.model.Geometry.StartBatchOperation();

            v0.v.Position = new Point3D(1, 2, 3);
            Assert.AreEqual(new Point3D(1, 2, 3), v0.v.Position, "Position 2");
            Assert.AreEqual(0, data.eventData.GeometryChangedEventData.Count(), "ChangedEvent");
            Assert.AreEqual(0, data.eventData.AddEventData.Count(), "AddEvent");
            Assert.AreEqual(0, data.eventData.RemoveEventData.Count(), "RemoveEvent");
            Assert.AreEqual(0, data.eventData.BatchOperationFinishedCount, "BatchOperationFinishedCount");
            Assert.AreEqual(0, data.eventData.TopologyChangedEventData.Count, "TopologyChangedCount");
            Assert.AreEqual(0, v0.eventData.GeometryChangedCount);
            Assert.AreEqual(1, v0.eventData.PropertyChangedData.Count);
            Assert.AreEqual(nameof(Vertex.Position), v0.eventData.PropertyChangedData[0]);
            Assert.AreEqual(0, v1.eventData.GeometryChangedCount);
            Assert.AreEqual(0, v1.eventData.PropertyChangedData.Count);

            v1.v.Position = new Point3D(-1, -2, -3);
            Assert.AreEqual(new Point3D(-1, -2, -3), v1.v.Position, "Position 2");
            Assert.AreEqual(0, data.eventData.GeometryChangedEventData.Count(), "ChangedEvent");
            Assert.AreEqual(0, data.eventData.AddEventData.Count(), "AddEvent");
            Assert.AreEqual(0, data.eventData.RemoveEventData.Count(), "RemoveEvent");
            Assert.AreEqual(0, data.eventData.BatchOperationFinishedCount, "BatchOperationFinishedCount");
            Assert.AreEqual(0, data.eventData.TopologyChangedEventData.Count, "TopologyChangedCount");
            Assert.AreEqual(0, v0.eventData.GeometryChangedCount);
            Assert.AreEqual(1, v0.eventData.PropertyChangedData.Count);
            Assert.AreEqual(1, v1.eventData.PropertyChangedData.Count);
            Assert.AreEqual(nameof(Vertex.Position), v1.eventData.PropertyChangedData[0]);

            data.model.Geometry.EndBatchOperation();

            Assert.AreEqual(1, data.eventData.GeometryChangedEventData.Count(), "ChangedEvent");
            Assert.AreEqual(2, data.eventData.GeometryChangedEventData[0].Count(), "ChangedEvent[0]");
            Assert.IsTrue(data.eventData.GeometryChangedEventData[0].Contains(v0.v), "ChangedEvent[0].Contains");
            Assert.IsTrue(data.eventData.GeometryChangedEventData[0].Contains(v1.v), "ChangedEvent[0].Contains");
            Assert.AreEqual(0, data.eventData.AddEventData.Count(), "AddEvent");
            Assert.AreEqual(0, data.eventData.RemoveEventData.Count(), "RemoveEvent");
            Assert.AreEqual(1, data.eventData.BatchOperationFinishedCount, "BatchOperationFinishedCount");
            Assert.AreEqual(0, data.eventData.TopologyChangedEventData.Count, "TopologyChangedCount");
            Assert.AreEqual(1, v0.eventData.GeometryChangedCount);
            Assert.AreEqual(1, v0.eventData.PropertyChangedData.Count);
            Assert.AreEqual(1, v1.eventData.GeometryChangedCount);
            Assert.AreEqual(1, v1.eventData.PropertyChangedData.Count);
        }

        [TestMethod]
        public void Visibility()
        {
            var data = GeometryModelHelper.EmptyModelWithEvents();

            //Add a vertex
            var v0 = VertexWithEvents(new Vertex(data.layer, "", new Point3D(0, 0, 0)));
            data.eventData.Reset();

            Assert.AreEqual(true, data.layer.IsVisible);
            Assert.AreEqual(true, v0.v.IsVisible);
            Assert.AreEqual(true, v0.v.IsActuallyVisible);
            Assert.AreEqual(0, v0.eventData.PropertyChangedData.Count);

            v0.v.IsVisible = false;
            Assert.AreEqual(false, v0.v.IsVisible);
            Assert.AreEqual(false, v0.v.IsActuallyVisible);
            Assert.AreEqual(2, v0.eventData.PropertyChangedData.Count);
            Assert.AreEqual(nameof(Vertex.IsVisible), v0.eventData.PropertyChangedData[0]);
            Assert.AreEqual(nameof(Vertex.IsActuallyVisible), v0.eventData.PropertyChangedData[1]);

            v0.v.IsVisible = true;
            Assert.AreEqual(true, v0.v.IsVisible);
            Assert.AreEqual(true, v0.v.IsActuallyVisible);
            Assert.AreEqual(4, v0.eventData.PropertyChangedData.Count);
            Assert.AreEqual(nameof(Vertex.IsVisible), v0.eventData.PropertyChangedData[2]);
            Assert.AreEqual(nameof(Vertex.IsActuallyVisible), v0.eventData.PropertyChangedData[3]);

            data.layer.IsVisible = false;
            Assert.AreEqual(true, v0.v.IsVisible);
            Assert.AreEqual(false, v0.v.IsActuallyVisible);
            Assert.AreEqual(5, v0.eventData.PropertyChangedData.Count);
            Assert.AreEqual(nameof(Vertex.IsActuallyVisible), v0.eventData.PropertyChangedData[4]);

            data.layer.IsVisible = true;
            Assert.AreEqual(true, v0.v.IsVisible);
            Assert.AreEqual(true, v0.v.IsActuallyVisible);
            Assert.AreEqual(6, v0.eventData.PropertyChangedData.Count);
            Assert.AreEqual(nameof(Vertex.IsActuallyVisible), v0.eventData.PropertyChangedData[5]);

            Assert.AreEqual(0, v0.eventData.GeometryChangedCount);
        }

        [TestMethod]
        public void Name()
        {
            var data = GeometryModelHelper.EmptyModelWithEvents();

            //Add a vertex
            var v0 = VertexWithEvents(new Vertex(data.layer, "", new Point3D(0, 0, 0)));
            data.eventData.Reset();
            Assert.AreEqual(0, v0.eventData.PropertyChangedData.Count);

            v0.v.Name = "RenamedVertex";
            Assert.AreEqual("RenamedVertex", v0.v.Name);
            Assert.AreEqual(1, v0.eventData.PropertyChangedData.Count);
            Assert.AreEqual(nameof(Vertex.Name), v0.eventData.PropertyChangedData[0]);
        }

        [TestMethod]
        public void MoveToLayer()
        {
            var data = GeometryModelHelper.EmptyModelWithEvents();
            Layer targetLayer = new Layer(data.model.Geometry, "TargetLayer") { Color = new DerivedColor(Colors.Pink) };

            //Add a vertex
            var v0 = VertexWithEvents(new Vertex(data.layer, "", new Point3D(0, 0, 0)));
            data.eventData.Reset();

            Assert.AreEqual(data.layer, v0.v.Layer);
            Assert.AreEqual(1, data.layer.Elements.Count);
            Assert.AreEqual(Colors.Red, v0.v.Color.Color);

            v0.v.Layer = targetLayer;
            Assert.AreEqual(targetLayer, v0.v.Layer);
            Assert.AreEqual(0, data.layer.Elements.Count);
            Assert.AreEqual(1, targetLayer.Elements.Count);
            Assert.AreEqual(Colors.Pink, v0.v.Color.Color);

            Assert.AreEqual(0, data.eventData.GeometryChangedEventData.Count);
            Assert.AreEqual(0, data.eventData.AddEventData.Count);
            Assert.AreEqual(0, data.eventData.RemoveEventData.Count);
            Assert.AreEqual(0, data.eventData.BatchOperationFinishedCount);
            Assert.AreEqual(0, data.eventData.TopologyChangedEventData.Count);
            Assert.AreEqual(0, v0.eventData.GeometryChangedCount);
            Assert.AreEqual(3, v0.eventData.PropertyChangedData.Count);
            Assert.IsTrue(v0.eventData.PropertyChangedData.Contains("Layer"));
            Assert.IsTrue(v0.eventData.PropertyChangedData.Contains("Color"));
            Assert.IsTrue(v0.eventData.PropertyChangedData.Contains("IsActuallyVisible"));
        }
    }
}
