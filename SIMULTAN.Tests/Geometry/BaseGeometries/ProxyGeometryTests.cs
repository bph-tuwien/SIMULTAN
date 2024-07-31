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
    public class ProxyGeometryTests
    {
        private (ProxyGeometry p, BaseGeometryEventData eventData) ProxyWithEvents(ProxyGeometry p)
        {
            return (p, new BaseGeometryEventData(p));
        }

        [TestMethod]
        public void Ctor()
        {
            var data = GeometryModelHelper.EmptyModel();
            var v0 = new Vertex(data.layer, "", new SimPoint3D(1, 2, 3));

            Assert.ThrowsException<ArgumentNullException>(() => { var p0 = new ProxyGeometry(null, "", v0); });
            Assert.ThrowsException<ArgumentNullException>(() => { var p0 = new ProxyGeometry(data.layer, null, v0); });
            Assert.ThrowsException<ArgumentNullException>(() => { var p0 = new ProxyGeometry(data.layer, "", null); });
            Assert.ThrowsException<ArgumentException>(() => { var p0 = new ProxyGeometry(ulong.MaxValue, data.layer, "", v0); });

            var p1 = new ProxyGeometry(data.layer, "", v0);
            Assert.AreEqual(p1, v0.ProxyGeometries[0]);
            Assert.AreEqual(v0, p1.Vertex);
            Assert.AreEqual(data.layer, p1.Layer);
            Assert.IsNull(p1.Positions);
            Assert.IsNull(p1.Normals);
            Assert.IsNull(p1.Indices);

            var p2 = new ProxyGeometry(999, data.layer, "", v0);
            Assert.AreEqual((ulong)999, p2.Id);
        }

        [TestMethod]
        public void Add()
        {
            var data = GeometryModelHelper.EmptyModelWithEvents();
            var v0 = new Vertex(data.layer, "", new SimPoint3D(1, 2, 3));
            data.eventData.Reset();

            var p0 = ProxyWithEvents(new ProxyGeometry(data.layer, "", v0));

            Assert.AreEqual(1, data.model.Geometry.ProxyGeometries.Count);
            Assert.AreEqual(p0.p, data.model.Geometry.ProxyGeometries[0]);

            Assert.AreEqual(0, data.eventData.BatchOperationFinishedCount);
            Assert.AreEqual(1, data.eventData.TopologyChangedEventData.Count);
            Assert.AreEqual(1, data.eventData.AddEventData.Count);
            Assert.AreEqual(1, data.eventData.AddEventData[0].Count());
            Assert.AreEqual(p0.p, data.eventData.AddEventData[0].First());
            Assert.AreEqual(0, data.eventData.RemoveEventData.Count);
            Assert.AreEqual(0, data.eventData.GeometryChangedEventData.Count);

            Assert.AreEqual(0, p0.eventData.GeometryChangedCount);
            Assert.AreEqual(0, p0.eventData.TopologyChangedCount);

            Assert.AreEqual(1, data.layer.Elements.Count(x => x is ProxyGeometry));
            Assert.IsTrue(data.layer.Elements.Contains(p0.p));
        }

        [TestMethod]
        public void Remove()
        {
            var data = GeometryModelHelper.EmptyModelWithEvents();
            var v0 = new Vertex(data.layer, "", new SimPoint3D(1, 2, 3));
            var p0 = ProxyWithEvents(new ProxyGeometry(data.layer, "", v0));
            data.eventData.Reset();

            var isDeleted = p0.p.RemoveFromModel();

            Assert.IsTrue(isDeleted);
            Assert.AreEqual(0, data.model.Geometry.ProxyGeometries.Count);
            Assert.AreEqual(0, data.layer.Elements.Count(x => x is ProxyGeometry));
            Assert.AreEqual(0, v0.ProxyGeometries.Count);

            Assert.AreEqual(0, data.eventData.BatchOperationFinishedCount);
            Assert.AreEqual(1, data.eventData.TopologyChangedEventData.Count);
            Assert.AreEqual(0, data.eventData.AddEventData.Count);
            Assert.AreEqual(1, data.eventData.RemoveEventData.Count);
            Assert.AreEqual(1, data.eventData.RemoveEventData[0].Count());
            Assert.AreEqual(p0.p, data.eventData.RemoveEventData[0].First());
            Assert.AreEqual(0, data.eventData.GeometryChangedEventData.Count);

            Assert.AreEqual(0, p0.eventData.GeometryChangedCount);
            Assert.AreEqual(0, p0.eventData.TopologyChangedCount);

            //Second delete should do nothing
            isDeleted = p0.p.RemoveFromModel();
            Assert.IsFalse(isDeleted);
            Assert.AreEqual(0, data.model.Geometry.ProxyGeometries.Count);
            Assert.AreEqual(0, data.layer.Elements.Count(x => x is ProxyGeometry));

            Assert.AreEqual(0, data.eventData.BatchOperationFinishedCount);
            Assert.AreEqual(1, data.eventData.TopologyChangedEventData.Count);
            Assert.AreEqual(0, data.eventData.AddEventData.Count);
            Assert.AreEqual(1, data.eventData.RemoveEventData.Count);
            Assert.AreEqual(1, data.eventData.RemoveEventData[0].Count());
            Assert.AreEqual(p0.p, data.eventData.RemoveEventData[0].First());
            Assert.AreEqual(0, data.eventData.GeometryChangedEventData.Count);

            Assert.AreEqual(0, p0.eventData.GeometryChangedCount);
            Assert.AreEqual(0, p0.eventData.TopologyChangedCount);
        }

        [TestMethod]
        public void Readd()
        {
            var data = GeometryModelHelper.EmptyModelWithEvents();
            var v0 = new Vertex(data.layer, "", new SimPoint3D(1, 2, 3));
            var p0 = ProxyWithEvents(new ProxyGeometry(data.layer, "", v0));
            p0.p.RemoveFromModel();
            data.eventData.Reset();

            p0.p.AddToModel();

            Assert.AreEqual(1, data.model.Geometry.ProxyGeometries.Count);
            Assert.AreEqual(1, data.layer.Elements.Count(x => x is ProxyGeometry));
            Assert.AreEqual(p0.p, v0.ProxyGeometries[0]);

            Assert.AreEqual(0, data.eventData.BatchOperationFinishedCount);
            Assert.AreEqual(1, data.eventData.TopologyChangedEventData.Count);
            Assert.AreEqual(1, data.eventData.AddEventData.Count);
            Assert.AreEqual(1, data.eventData.AddEventData[0].Count());
            Assert.AreEqual(p0.p, data.eventData.AddEventData[0].First());
            Assert.AreEqual(0, data.eventData.RemoveEventData.Count);
            Assert.AreEqual(0, data.eventData.GeometryChangedEventData.Count);

            Assert.AreEqual(0, p0.eventData.GeometryChangedCount);
            Assert.AreEqual(0, p0.eventData.TopologyChangedCount);

            //Second add crashes
            Assert.ThrowsException<Exception>(() => p0.p.AddToModel());
        }

        [TestMethod]
        public void BatchAdd()
        {
            var data = GeometryModelHelper.EmptyModelWithEvents();
            var v0 = new Vertex(data.layer, "", new SimPoint3D(1, 2, 3));
            var v1 = new Vertex(data.layer, "", new SimPoint3D(2, 4, 6));
            data.eventData.Reset();

            data.model.Geometry.StartBatchOperation();

            var p0 = ProxyWithEvents(new ProxyGeometry(data.layer, "", v0));
            var p1 = ProxyWithEvents(new ProxyGeometry(data.layer, "", v0));

            Assert.AreEqual(0, data.eventData.TopologyChangedEventData.Count);
            Assert.AreEqual(0, data.eventData.AddEventData.Count);
            Assert.AreEqual(0, data.eventData.RemoveEventData.Count);
            Assert.AreEqual(0, data.eventData.GeometryChangedEventData.Count);
            Assert.AreEqual(0, data.eventData.BatchOperationFinishedCount);
            Assert.AreEqual(0, p0.eventData.GeometryChangedCount);
            Assert.AreEqual(0, p0.eventData.TopologyChangedCount);
            Assert.AreEqual(0, p1.eventData.GeometryChangedCount);
            Assert.AreEqual(0, p1.eventData.TopologyChangedCount);

            data.model.Geometry.EndBatchOperation();

            Assert.AreEqual(2, data.model.Geometry.ProxyGeometries.Count);
            Assert.AreEqual(2, v0.ProxyGeometries.Count);
            Assert.AreEqual(2, data.layer.Elements.Count(x => x is ProxyGeometry));

            Assert.AreEqual(1, data.eventData.TopologyChangedEventData.Count);
            Assert.AreEqual(1, data.eventData.AddEventData.Count);
            Assert.AreEqual(2, data.eventData.AddEventData[0].Count());
            Assert.IsTrue(data.eventData.AddEventData[0].Contains(p0.p));
            Assert.IsTrue(data.eventData.AddEventData[0].Contains(p1.p));
            Assert.AreEqual(0, data.eventData.RemoveEventData.Count);
            Assert.AreEqual(0, data.eventData.GeometryChangedEventData.Count);
            Assert.AreEqual(1, data.eventData.BatchOperationFinishedCount);
            Assert.AreEqual(0, p0.eventData.GeometryChangedCount);
            Assert.AreEqual(0, p0.eventData.TopologyChangedCount);
            Assert.AreEqual(0, p1.eventData.GeometryChangedCount);
            Assert.AreEqual(0, p1.eventData.TopologyChangedCount);
        }

        [TestMethod]
        public void BatchRemove()
        {
            var data = GeometryModelHelper.EmptyModelWithEvents();
            var v0 = new Vertex(data.layer, "", new SimPoint3D(1, 2, 3));
            var v1 = new Vertex(data.layer, "", new SimPoint3D(2, 4, 6));
            var p0 = ProxyWithEvents(new ProxyGeometry(data.layer, "", v0));
            var p1 = ProxyWithEvents(new ProxyGeometry(data.layer, "", v0));
            data.eventData.Reset();

            data.model.Geometry.StartBatchOperation();

            p0.p.RemoveFromModel();
            p1.p.RemoveFromModel();

            Assert.AreEqual(0, data.eventData.TopologyChangedEventData.Count);
            Assert.AreEqual(0, data.eventData.AddEventData.Count);
            Assert.AreEqual(0, data.eventData.RemoveEventData.Count);
            Assert.AreEqual(0, data.eventData.GeometryChangedEventData.Count);
            Assert.AreEqual(0, data.eventData.BatchOperationFinishedCount);
            Assert.AreEqual(0, p0.eventData.GeometryChangedCount);
            Assert.AreEqual(0, p0.eventData.TopologyChangedCount);
            Assert.AreEqual(0, p1.eventData.GeometryChangedCount);
            Assert.AreEqual(0, p1.eventData.TopologyChangedCount);


            data.model.Geometry.EndBatchOperation();

            Assert.AreEqual(0, data.model.Geometry.ProxyGeometries.Count);
            Assert.AreEqual(0, data.layer.Elements.Count(x => x is ProxyGeometry));
            Assert.AreEqual(0, v0.ProxyGeometries.Count);

            Assert.AreEqual(1, data.eventData.TopologyChangedEventData.Count);
            Assert.AreEqual(0, data.eventData.AddEventData.Count);
            Assert.AreEqual(1, data.eventData.RemoveEventData.Count);
            Assert.AreEqual(2, data.eventData.RemoveEventData[0].Count());
            Assert.IsTrue(data.eventData.RemoveEventData[0].Contains(p0.p));
            Assert.IsTrue(data.eventData.RemoveEventData[0].Contains(p1.p));
            Assert.AreEqual(0, data.eventData.GeometryChangedEventData.Count);
            Assert.AreEqual(1, data.eventData.BatchOperationFinishedCount);
            Assert.AreEqual(0, p0.eventData.GeometryChangedCount);
            Assert.AreEqual(0, p0.eventData.TopologyChangedCount);
            Assert.AreEqual(0, p1.eventData.GeometryChangedCount);
            Assert.AreEqual(0, p1.eventData.TopologyChangedCount);
        }

        [TestMethod]
        public void PositionSetTest()
        {
            var data = GeometryModelHelper.EmptyModelWithEvents();
            var v0 = new Vertex(data.layer, "", new SimPoint3D(1, 2, 3));
            var p0 = ProxyWithEvents(new ProxyGeometry(data.layer, "", v0));
            data.eventData.Reset();

            var posList = new List<SimPoint3D> { new SimPoint3D(0, 0, 0), new SimPoint3D(1, 1, 1), new SimPoint3D(2, 2, 2) };
            p0.p.Positions = posList;

            Assert.AreEqual(posList, p0.p.Positions);
            Assert.AreEqual(0, data.eventData.TopologyChangedEventData.Count);
            Assert.AreEqual(0, data.eventData.AddEventData.Count);
            Assert.AreEqual(0, data.eventData.RemoveEventData.Count);
            Assert.AreEqual(0, data.eventData.GeometryChangedEventData.Count);
            //Assert.AreEqual(1, data.eventData.GeometryChangedEventData[0].Count());
            //Assert.AreEqual(p0.p, data.eventData.GeometryChangedEventData[0].First());
            Assert.AreEqual(0, data.eventData.BatchOperationFinishedCount);
            Assert.AreEqual(0, p0.eventData.GeometryChangedCount);
            Assert.AreEqual(0, p0.eventData.TopologyChangedCount);
        }

        [TestMethod]
        public void NormalSetTest()
        {
            var data = GeometryModelHelper.EmptyModelWithEvents();
            var v0 = new Vertex(data.layer, "", new SimPoint3D(1, 2, 3));
            var p0 = ProxyWithEvents(new ProxyGeometry(data.layer, "", v0));
            data.eventData.Reset();

            var posList = new List<SimVector3D> { new SimVector3D(0, 0, 0), new SimVector3D(1, 1, 1), new SimVector3D(2, 2, 2) };
            p0.p.Normals = posList;

            Assert.AreEqual(posList, p0.p.Normals);
            Assert.AreEqual(0, data.eventData.TopologyChangedEventData.Count);
            Assert.AreEqual(0, data.eventData.AddEventData.Count);
            Assert.AreEqual(0, data.eventData.RemoveEventData.Count);
            Assert.AreEqual(0, data.eventData.GeometryChangedEventData.Count);
            //Assert.AreEqual(1, data.eventData.GeometryChangedEventData[0].Count());
            //Assert.AreEqual(p0.p, data.eventData.GeometryChangedEventData[0].First());
            Assert.AreEqual(0, data.eventData.BatchOperationFinishedCount);
            Assert.AreEqual(0, p0.eventData.GeometryChangedCount);
            Assert.AreEqual(0, p0.eventData.TopologyChangedCount);
        }

        [TestMethod]
        public void IndexSetTest()
        {
            var data = GeometryModelHelper.EmptyModelWithEvents();
            var v0 = new Vertex(data.layer, "", new SimPoint3D(1, 2, 3));
            var p0 = ProxyWithEvents(new ProxyGeometry(data.layer, "", v0));
            data.eventData.Reset();

            var posList = new List<int> { 0, 1, 2 };
            p0.p.Indices = posList;

            Assert.AreEqual(posList, p0.p.Indices);
            Assert.AreEqual(0, data.eventData.TopologyChangedEventData.Count);
            Assert.AreEqual(0, data.eventData.AddEventData.Count);
            Assert.AreEqual(0, data.eventData.RemoveEventData.Count);
            Assert.AreEqual(0, data.eventData.GeometryChangedEventData.Count);
            //Assert.AreEqual(1, data.eventData.GeometryChangedEventData[0].Count());
            //Assert.AreEqual(p0.p, data.eventData.GeometryChangedEventData[0].First());
            Assert.AreEqual(0, data.eventData.BatchOperationFinishedCount);
            Assert.AreEqual(0, p0.eventData.GeometryChangedCount);
            Assert.AreEqual(0, p0.eventData.TopologyChangedCount);
        }

        [TestMethod]
        public void Transformations()
        {
            var data = GeometryModelHelper.EmptyModelWithEvents();
            var v0 = new Vertex(data.layer, "", new SimPoint3D(1, 2, 3));
            var p0 = ProxyWithEvents(new ProxyGeometry(data.layer, "", v0));
            data.eventData.Reset();

            Assert.AreEqual(new SimVector3D(1, 1, 1), p0.p.Size);

            SimMatrix3D mat = new SimMatrix3D(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, 1, 2, 3, 1);
            Assert.AreEqual(mat, p0.p.Transformation);

            p0.p.Size = new SimVector3D(6, 7, 8);
            Assert.AreEqual(new SimVector3D(6, 7, 8), p0.p.Size);
            mat = new SimMatrix3D(6, 0, 0, 0, 0, 7, 0, 0, 0, 0, 8, 0, 1, 2, 3, 1);
            Assert.AreEqual(mat, p0.p.Transformation);
        }

        [TestMethod]
        public void GeometryChanged()
        {
            var data = GeometryModelHelper.EmptyModelWithEvents();
            var v0 = new Vertex(data.layer, "", new SimPoint3D(1, 2, 3));
            var p0 = ProxyWithEvents(new ProxyGeometry(data.layer, "", v0));
            data.eventData.Reset();

            v0.Position = new SimPoint3D(-1, -2, -3);

            Assert.AreEqual(new SimMatrix3D(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, -1, -2, -3, 1), p0.p.Transformation);
            Assert.AreEqual(0, data.eventData.TopologyChangedEventData.Count);
            Assert.AreEqual(0, data.eventData.AddEventData.Count);
            Assert.AreEqual(0, data.eventData.RemoveEventData.Count);
            Assert.AreEqual(2, data.eventData.GeometryChangedEventData.Count);
            Assert.AreEqual(p0.p, data.eventData.GeometryChangedEventData[1].First());
            Assert.AreEqual(0, data.eventData.BatchOperationFinishedCount);
            Assert.AreEqual(1, p0.eventData.GeometryChangedCount);
            Assert.AreEqual(0, p0.eventData.TopologyChangedCount);
        }

        [TestMethod]
        public void BatchGeometryChanged()
        {
            var data = GeometryModelHelper.EmptyModelWithEvents();
            var v0 = new Vertex(data.layer, "", new SimPoint3D(1, 2, 3));
            var p0 = ProxyWithEvents(new ProxyGeometry(data.layer, "", v0));
            data.eventData.Reset();

            data.model.Geometry.StartBatchOperation();

            v0.Position = new SimPoint3D(-1, -2, -3);
            Assert.AreEqual(0, data.eventData.TopologyChangedEventData.Count);
            Assert.AreEqual(0, data.eventData.AddEventData.Count);
            Assert.AreEqual(0, data.eventData.RemoveEventData.Count);
            Assert.AreEqual(0, data.eventData.GeometryChangedEventData.Count);
            Assert.AreEqual(0, data.eventData.BatchOperationFinishedCount);
            Assert.AreEqual(0, p0.eventData.GeometryChangedCount);
            Assert.AreEqual(0, p0.eventData.TopologyChangedCount);

            data.model.Geometry.EndBatchOperation();

            Assert.AreEqual(new SimMatrix3D(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, -1, -2, -3, 1), p0.p.Transformation);
            Assert.AreEqual(0, data.eventData.TopologyChangedEventData.Count);
            Assert.AreEqual(0, data.eventData.AddEventData.Count);
            Assert.AreEqual(0, data.eventData.RemoveEventData.Count);
            Assert.AreEqual(1, data.eventData.GeometryChangedEventData.Count);
            Assert.IsTrue(data.eventData.GeometryChangedEventData[0].Contains(p0.p));
            Assert.AreEqual(1, data.eventData.BatchOperationFinishedCount);
            Assert.AreEqual(1, p0.eventData.GeometryChangedCount);
            Assert.AreEqual(0, p0.eventData.TopologyChangedCount);
        }

        [TestMethod]
        public void MoveToLayer()
        {
            var data = GeometryModelHelper.EmptyModelWithEvents();
            Layer targetLayer = new Layer(data.model.Geometry, "TargetLayer") { Color = new DerivedColor(SimColors.Pink) };

            var v0 = new Vertex(data.layer, "", new SimPoint3D(1, 2, 3));
            var p0 = ProxyWithEvents(new ProxyGeometry(data.layer, "", v0));
            data.eventData.Reset();

            Assert.AreEqual(data.layer, p0.p.Layer);
            Assert.AreEqual(1, data.layer.Elements.Count(x => x is ProxyGeometry));
            Assert.AreEqual(SimColors.Red, p0.p.Color.Color);

            p0.p.Layer = targetLayer;
            Assert.AreEqual(targetLayer, p0.p.Layer);
            Assert.AreEqual(0, data.layer.Elements.Count(x => x is ProxyGeometry));
            Assert.AreEqual(1, targetLayer.Elements.Count(x => x is ProxyGeometry));
            Assert.AreEqual(v0.Color.Color, p0.p.Color.Color); //Proxy takes vertex color!

            Assert.AreEqual(0, data.eventData.GeometryChangedEventData.Count);
            Assert.AreEqual(0, data.eventData.AddEventData.Count);
            Assert.AreEqual(0, data.eventData.RemoveEventData.Count);
            Assert.AreEqual(0, data.eventData.BatchOperationFinishedCount);
            Assert.AreEqual(0, data.eventData.TopologyChangedEventData.Count);
            Assert.AreEqual(0, p0.eventData.GeometryChangedCount);
            Assert.AreEqual(2, p0.eventData.PropertyChangedData.Count);
            Assert.IsTrue(p0.eventData.PropertyChangedData.Contains("Layer"));
            Assert.IsTrue(p0.eventData.PropertyChangedData.Contains("IsActuallyVisible"));
        }
    }
}
