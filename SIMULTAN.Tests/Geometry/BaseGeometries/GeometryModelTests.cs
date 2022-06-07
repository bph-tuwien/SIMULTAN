using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data.Assets;
using SIMULTAN.Data.Geometry;
using SIMULTAN.Tests.Geometry.EventData;
using SIMULTAN.Tests.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace SIMULTAN.Tests.Geometry.BaseGeometries
{
    [TestClass]
    public class GeometryModelTests
    {
        [TestMethod]
        public void Ctor()
        {
            var file = new FileInfo("./unittest.geosim");

            GeometryModelData model1 = new GeometryModelData();
            Assert.IsNotNull(model1.Vertices);
            Assert.IsNotNull(model1.EdgeLoops);
            Assert.IsNotNull(model1.Edges);
            Assert.IsNotNull(model1.Polylines);
            Assert.IsNotNull(model1.Faces);
            Assert.IsNotNull(model1.Volumes);
            Assert.IsNotNull(model1.ProxyGeometries);
            Assert.IsTrue(model1.HandleConsistency);
        }

        [TestMethod]
        public void Clone()
        {
            var data = GeometryModelHelper.EmptyModel();

            Layer l2 = new Layer(data.model.Geometry, "Layer2");
            data.layer.Layers.Add(l2);
            Layer l3 = new Layer(data.model.Geometry, "Layer3")
            {
                IsVisible = false
            };
            data.layer.Layers.Add(l3);

            ShapeGenerator.GenerateCube(data.layer, new Point3D(0, 0, 0), new Point3D(1, 1, 1));
            ProxyShapeGenerator.GenerateCube(l2, "cube", data.model.Geometry.Vertices[0], new Point3D(0.2, 0.2, 0.2));
            ProxyShapeGenerator.GenerateCube(l3, "cube", data.model.Geometry.Vertices[1], new Point3D(0.2, 0.2, 0.2));

            Polyline pl = new Polyline(l2, "", new Edge[] { data.model.Geometry.Edges[0], data.model.Geometry.Edges[1] });

            data.model.Geometry.Vertices[0].IsVisible = false;
            data.model.Geometry.EdgeLoops[0].IsVisible = false;
            data.model.Geometry.Edges[0].IsVisible = false;
            data.model.Geometry.Faces[0].IsVisible = false;
            data.model.Geometry.Volumes[0].IsVisible = false;
            data.model.Geometry.Polylines[0].IsVisible = false;
            data.model.Geometry.ProxyGeometries[0].IsVisible = false;

            var cloned = data.model.Geometry.Clone();

            GeometryModelHelper.Compare(data.model.Geometry, cloned);
        }

        [TestMethod]
        public void ContainsGeometry()
        {
            var data = GeometryModelHelper.EmptyModel();
            ShapeGenerator.GenerateCube(data.layer, new Point3D(0, 0, 0), new Point3D(1, 1, 1));
            ProxyShapeGenerator.GenerateCube(data.layer, "cube", data.model.Geometry.Vertices[0], new Point3D(0.2, 0.2, 0.2));
            ProxyShapeGenerator.GenerateCube(data.layer, "cube", data.model.Geometry.Vertices[1], new Point3D(0.2, 0.2, 0.2));
            Polyline pl = new Polyline(data.layer, "", new Edge[] { data.model.Geometry.Edges[0], data.model.Geometry.Edges[1] });

            foreach (var g in data.model.Geometry.Vertices)
                Assert.AreEqual(true, data.model.Geometry.ContainsGeometry(g));
            foreach (var g in data.model.Geometry.Edges)
                Assert.AreEqual(true, data.model.Geometry.ContainsGeometry(g));
            foreach (var g in data.model.Geometry.EdgeLoops)
                Assert.AreEqual(true, data.model.Geometry.ContainsGeometry(g));
            foreach (var g in data.model.Geometry.Polylines)
                Assert.AreEqual(true, data.model.Geometry.ContainsGeometry(g));
            foreach (var g in data.model.Geometry.Faces)
                Assert.AreEqual(true, data.model.Geometry.ContainsGeometry(g));
            foreach (var g in data.model.Geometry.Volumes)
                Assert.AreEqual(true, data.model.Geometry.ContainsGeometry(g));
            foreach (var g in data.model.Geometry.ProxyGeometries)
                Assert.AreEqual(true, data.model.Geometry.ContainsGeometry(g));

            Assert.AreEqual(false, data.model.Geometry.ContainsGeometry(null));

            BaseGeometry g2 = data.model.Geometry.Vertices[0];
            g2.RemoveFromModel();
            Assert.AreEqual(false, data.model.Geometry.ContainsGeometry(g2));
            g2 = data.model.Geometry.Edges[0];
            g2.RemoveFromModel();
            Assert.AreEqual(false, data.model.Geometry.ContainsGeometry(g2));
            g2 = data.model.Geometry.EdgeLoops[0];
            g2.RemoveFromModel();
            Assert.AreEqual(false, data.model.Geometry.ContainsGeometry(g2));
            g2 = data.model.Geometry.Polylines[0];
            g2.RemoveFromModel();
            Assert.AreEqual(false, data.model.Geometry.ContainsGeometry(g2));
            g2 = data.model.Geometry.Faces[0];
            g2.RemoveFromModel();
            Assert.AreEqual(false, data.model.Geometry.ContainsGeometry(g2));
            g2 = data.model.Geometry.Volumes[0];
            g2.RemoveFromModel();
            Assert.AreEqual(false, data.model.Geometry.ContainsGeometry(g2));
            g2 = data.model.Geometry.ProxyGeometries[0];
            g2.RemoveFromModel();
            Assert.AreEqual(false, data.model.Geometry.ContainsGeometry(g2));
        }

        [TestMethod]
        public void GeometryFromId()
        {
            var data = GeometryModelHelper.EmptyModel();
            ShapeGenerator.GenerateCube(data.layer, new Point3D(0, 0, 0), new Point3D(1, 1, 1));
            ProxyShapeGenerator.GenerateCube(data.layer, "cube", data.model.Geometry.Vertices[0], new Point3D(0.2, 0.2, 0.2));
            ProxyShapeGenerator.GenerateCube(data.layer, "cube", data.model.Geometry.Vertices[1], new Point3D(0.2, 0.2, 0.2));
            Polyline pl = new Polyline(data.layer, "", new Edge[] { data.model.Geometry.Edges[0], data.model.Geometry.Edges[1] });

            foreach (var g in data.model.Geometry.Vertices)
                Assert.AreEqual(g, data.model.Geometry.GeometryFromId(g.Id));
            foreach (var g in data.model.Geometry.Edges)
                Assert.AreEqual(g, data.model.Geometry.GeometryFromId(g.Id));
            foreach (var g in data.model.Geometry.EdgeLoops)
                Assert.AreEqual(g, data.model.Geometry.GeometryFromId(g.Id));
            foreach (var g in data.model.Geometry.Polylines)
                Assert.AreEqual(g, data.model.Geometry.GeometryFromId(g.Id));
            foreach (var g in data.model.Geometry.Faces)
                Assert.AreEqual(g, data.model.Geometry.GeometryFromId(g.Id));
            foreach (var g in data.model.Geometry.Volumes)
                Assert.AreEqual(g, data.model.Geometry.GeometryFromId(g.Id));
            foreach (var g in data.model.Geometry.ProxyGeometries)
                Assert.AreEqual(g, data.model.Geometry.GeometryFromId(g.Id));

            BaseGeometry g2 = data.model.Geometry.ProxyGeometries[0];
            g2.RemoveFromModel();
            Assert.AreEqual(null, data.model.Geometry.GeometryFromId(g2.Id));
            g2 = data.model.Geometry.Polylines[0];
            g2.RemoveFromModel();
            Assert.AreEqual(null, data.model.Geometry.GeometryFromId(g2.Id));
            g2 = data.model.Geometry.Volumes[0];
            g2.RemoveFromModel();
            Assert.AreEqual(null, data.model.Geometry.GeometryFromId(g2.Id));
            g2 = data.model.Geometry.Faces[0];
            g2.RemoveFromModel();
            Assert.AreEqual(null, data.model.Geometry.GeometryFromId(g2.Id));
            g2 = data.model.Geometry.EdgeLoops[0];
            g2.RemoveFromModel();
            Assert.AreEqual(null, data.model.Geometry.GeometryFromId(g2.Id));
            g2 = data.model.Geometry.Edges[0];
            g2.RemoveFromModel();
            Assert.AreEqual(null, data.model.Geometry.GeometryFromId(g2.Id));
            g2 = data.model.Geometry.Vertices[0];
            g2.RemoveFromModel();
            Assert.AreEqual(null, data.model.Geometry.GeometryFromId(g2.Id));






        }

        [TestMethod]
        public void LayerFromId()
        {
            var data = GeometryModelHelper.EmptyModel();

            Assert.AreEqual(data.layer, data.model.Geometry.LayerFromId(data.layer.Id));
            Assert.AreEqual(null, data.model.Geometry.LayerFromId(9999));
        }

        [TestMethod]
        public void IsVisible()
        {
            var data = GeometryModelHelper.EmptyModelWithEvents();

            data.model.Geometry.IsVisible = false;
            Assert.AreEqual(1, data.eventData.PropertyChangedData.Count);
            Assert.AreEqual(nameof(GeometryModelData.IsVisible), data.eventData.PropertyChangedData[0]);

            data.model.Geometry.IsVisible = false;
            Assert.AreEqual(1, data.eventData.PropertyChangedData.Count);

            data.model.Geometry.IsVisible = true;
            Assert.AreEqual(2, data.eventData.PropertyChangedData.Count);
            Assert.AreEqual(nameof(GeometryModelData.IsVisible), data.eventData.PropertyChangedData[1]);
        }

        [TestMethod]
        public void LinkedModel()
        {
            var data = GeometryModelHelper.EmptyModel();

            var gm2 = new GeometryModelData();

            data.model.LinkedModels.Add(new GeometryModel(Guid.NewGuid(), "Model2", new DummyResourceFileEntry("dummy2.geosim", 2), OperationPermission.None, gm2));
            Assert.AreEqual(1, data.model.LinkedModels.Count);
            Assert.AreEqual(gm2, data.model.LinkedModels[0].Geometry);

            data.model.LinkedModels.RemoveAt(0);
            Assert.AreEqual(0, data.model.LinkedModels.Count);
        }

        [TestMethod]
        public void GetFreeId()
        {
            var gm = new GeometryModelData();

            var freeId = gm.GetFreeId();
            Assert.AreEqual((ulong)0, freeId);
            freeId = gm.GetFreeId();
            Assert.AreEqual((ulong)1, freeId);

            var layer = new Layer(99, gm, "Layer99");
            gm.Layers.Add(layer);
            freeId = gm.GetFreeId();
            Assert.AreEqual((ulong)100, freeId);
        }

        [TestMethod]
        public void OperationFinished()
        {
            var data = GeometryModelHelper.EmptyModelWithEvents();

            var v = new Vertex(data.layer, "", new Point3D(0, 0, 0));
            data.model.Geometry.OnOperationFinished(new BaseGeometry[] { v });

            Assert.AreEqual(1, data.eventData.OperationFinished.Count);
            Assert.AreEqual(1, data.eventData.OperationFinished[0].Count());
            Assert.AreEqual(v, data.eventData.OperationFinished[0].First());

            data.eventData.Reset();

            data.model.Geometry.OnOperationFinished(new BaseGeometry[] { });
            Assert.AreEqual(0, data.eventData.OperationFinished.Count);
            data.model.Geometry.OnOperationFinished(null);
            Assert.AreEqual(0, data.eventData.OperationFinished.Count);
        }
    }
}
