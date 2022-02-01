using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data.Geometry;
using SIMULTAN.Tests.Geometry.EventData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace SIMULTAN.Tests.Geometry.BaseGeometries
{
    [TestClass]
    public class FaceTests
    {
        private (Face f, BaseGeometryEventData eventData) FaceWithEvents(Face f)
        {
            return (f, new BaseGeometryEventData(f));
        }

        private (Vertex[] v, Edge[] e, EdgeLoop[] l) TestData(Layer layer)
        {
            Vertex[] v = new Vertex[]
            {
                new Vertex(layer, "", new Point3D(0, 0, 0)),
                new Vertex(layer, "", new Point3D(0, 0, 2)),
                new Vertex(layer, "", new Point3D(2, 0, 2)),
                new Vertex(layer, "", new Point3D(2, 0, 0)),
                new Vertex(layer, "", new Point3D(2, 2, 2)),
                new Vertex(layer, "", new Point3D(2, 2, 0)),

				//Hole in 0-1-2-3
				new Vertex(layer, "", new Point3D(0.5, 0, 1)),
                new Vertex(layer, "", new Point3D(1, 0, 1.5)),
                new Vertex(layer, "", new Point3D(1.5, 0, 1)),
                new Vertex(layer, "", new Point3D(1, 0, 0.5)),

                new Vertex(layer, "", new Point3D(0.5, 0, 0.5)),
                new Vertex(layer, "", new Point3D(1.5, 0, 0.5)),
                new Vertex(layer, "", new Point3D(1.5, 0, 1.5)),
                new Vertex(layer, "", new Point3D(0.5, 0, 1.5)),
            };

            Edge[] e = new Edge[]
            {
                new Edge(layer, "", new Vertex[] { v[0], v[1] }),
                new Edge(layer, "", new Vertex[] { v[1], v[2] }),
                new Edge(layer, "", new Vertex[] { v[2], v[3] }),
                new Edge(layer, "", new Vertex[] { v[3], v[0] }),
                new Edge(layer, "", new Vertex[] { v[3], v[4] }),
                new Edge(layer, "", new Vertex[] { v[4], v[5] }),
                new Edge(layer, "", new Vertex[] { v[5], v[2] }),

                new Edge(layer, "", new Vertex[] {v[6], v[7] }),
                new Edge(layer, "", new Vertex[] {v[7], v[8] }),
                new Edge(layer, "", new Vertex[] {v[8], v[9] }),
                new Edge(layer, "", new Vertex[] {v[9], v[6] }),

                new Edge(layer, "", new Vertex[] {v[10], v[11] }),
                new Edge(layer, "", new Vertex[] {v[11], v[12] }),
                new Edge(layer, "", new Vertex[] {v[12], v[13] }),
                new Edge(layer, "", new Vertex[] {v[13], v[10] }),
            };

            EdgeLoop[] l = new EdgeLoop[]
            {
                new EdgeLoop(layer, "", new Edge[]{e[0], e[1], e[2], e[3]}),
                new EdgeLoop(layer, "", new Edge[]{e[2], e[4], e[5], e[6]}),
                new EdgeLoop(layer, "", new Edge[]{e[7], e[8], e[9], e[10] }),
                new EdgeLoop(layer, "", new Edge[]{e[11], e[12], e[13], e[14]})
            };

            return (v, e, l);
        }

        [TestMethod]
        public void Ctor()
        {
            var data = GeometryModelHelper.EmptyModel();
            (var v, var e, var l) = TestData(data.layer);
            Assert.ThrowsException<ArgumentNullException>(() => { Face f0 = new Face(null, "", l[0], GeometricOrientation.Forward, null); }); //layer null
            Assert.ThrowsException<ArgumentNullException>(() => { Face f0 = new Face(data.layer, "", null, GeometricOrientation.Forward, null); }); //boundary null
            Assert.ThrowsException<ArgumentException>(() => { Face f0 = new Face(ulong.MaxValue, data.layer, "", l[0]); });  //id wrong

            Face f1 = new Face(data.layer, "", l[0]);
            Assert.AreEqual(1, data.model.Geometry.Faces.Count);
            Assert.AreEqual(f1, data.model.Geometry.Faces[0]);
            Assert.AreEqual(data.layer, f1.Layer);
            Assert.IsTrue(f1.Layer.Elements.Contains(f1));
            Assert.AreEqual(l[0], f1.Boundary);
            Assert.AreEqual(GeometricOrientation.Forward, f1.Orientation);
            Assert.AreEqual(true, f1.IsVisible);
            Assert.AreEqual(true, f1.IsActuallyVisible);
            Assert.AreEqual(0, f1.Holes.Count);

            Face f2 = new Face(data.layer, "", l[1], GeometricOrientation.Backward);
            Assert.AreEqual(2, data.model.Geometry.Faces.Count);
            Assert.AreEqual(GeometricOrientation.Backward, f2.Orientation);
            Assert.AreEqual(l[1], f2.Boundary);
            Assert.AreEqual(0, f2.Holes.Count);

            Face f3 = new Face(data.layer, "", l[0], GeometricOrientation.Forward, new EdgeLoop[] { l[2] });
            Assert.AreEqual(3, data.model.Geometry.Faces.Count);
            Assert.AreEqual(1, f3.Holes.Count);
            Assert.AreEqual(l[2], f3.Holes[0]);

            Face f4 = new Face(999, data.layer, "", l[0]);
            Assert.AreEqual((ulong)999, f4.Id);

            Assert.AreEqual(3, l[0].Faces.Count);
            Assert.IsTrue(l[0].Faces.Contains(f1));
            Assert.IsTrue(l[2].Faces.Contains(f3));
            Assert.AreEqual(1, l[1].Faces.Count);
            Assert.IsTrue(l[1].Faces.Contains(f2));
            Assert.AreEqual(1, l[2].Faces.Count);
            Assert.IsTrue(l[2].Faces.Contains(f3));
        }

        [TestMethod]
        public void Add()
        {
            var data = GeometryModelHelper.EmptyModelWithEvents();
            (var v, var e, var l) = TestData(data.layer);
            data.eventData.Reset();

            //Add face
            var f0 = FaceWithEvents(new Face(data.layer, "", l[0], GeometricOrientation.Forward, new EdgeLoop[] { l[2] }));

            Assert.AreEqual(0, data.eventData.BatchOperationFinishedCount);
            Assert.AreEqual(1, data.eventData.TopologyChangedEventData.Count);
            Assert.AreEqual(1, data.eventData.AddEventData.Count);
            Assert.AreEqual(1, data.eventData.AddEventData[0].Count());
            Assert.AreEqual(f0.f, data.eventData.AddEventData[0].First());
            Assert.AreEqual(0, data.eventData.RemoveEventData.Count);
            Assert.AreEqual(0, data.eventData.GeometryChangedEventData.Count);

            Assert.AreEqual(1, data.layer.Elements.Count(x => x is Face));
            Assert.IsTrue(data.layer.Elements.Contains(f0.f));

            //Boundary
            Assert.AreEqual(1, l[0].Faces.Count);
            Assert.AreEqual(f0.f, l[0].Faces[0]);
            Assert.AreEqual(l[0], f0.f.Boundary);

            //Hole
            Assert.AreEqual(1, l[2].Faces.Count);
            Assert.AreEqual(f0.f, l[2].Faces[0]);
            Assert.AreEqual(1, f0.f.Holes.Count);
            Assert.AreEqual(l[2], f0.f.Holes[0]);
        }

        [TestMethod]
        public void Remove()
        {
            var data = GeometryModelHelper.EmptyModelWithEvents();
            (var v, var e, var l) = TestData(data.layer);

            //Add face
            var f0 = FaceWithEvents(new Face(data.layer, "", l[0], GeometricOrientation.Forward, new EdgeLoop[] { l[2] }));
            data.eventData.Reset();

            var isRemoved = f0.f.RemoveFromModel();

            Assert.AreEqual(true, isRemoved);
            Assert.AreEqual(0, data.model.Geometry.Faces.Count);
            Assert.AreEqual(0, data.eventData.BatchOperationFinishedCount);
            Assert.AreEqual(1, data.eventData.TopologyChangedEventData.Count);
            Assert.AreEqual(0, data.eventData.AddEventData.Count);
            Assert.AreEqual(1, data.eventData.RemoveEventData.Count);
            Assert.AreEqual(1, data.eventData.RemoveEventData[0].Count());
            Assert.AreEqual(f0.f, data.eventData.RemoveEventData[0].First());
            Assert.AreEqual(0, data.eventData.GeometryChangedEventData.Count);
            Assert.AreEqual(0, f0.eventData.GeometryChangedCount);
            Assert.AreEqual(0, data.layer.Elements.Count(x => x is Face));
            Assert.AreEqual(0, l[0].Faces.Count);
            Assert.AreEqual(0, l[2].Faces.Count);

            //Second remove does nothing
            isRemoved = f0.f.RemoveFromModel();

            Assert.AreEqual(false, isRemoved);
            Assert.AreEqual(0, data.model.Geometry.Faces.Count);
            Assert.AreEqual(0, data.eventData.BatchOperationFinishedCount);
            Assert.AreEqual(1, data.eventData.TopologyChangedEventData.Count);
            Assert.AreEqual(0, data.eventData.AddEventData.Count);
            Assert.AreEqual(1, data.eventData.RemoveEventData.Count);
            Assert.AreEqual(1, data.eventData.RemoveEventData[0].Count());
            Assert.AreEqual(f0.f, data.eventData.RemoveEventData[0].First());
            Assert.AreEqual(0, data.eventData.GeometryChangedEventData.Count);
            Assert.AreEqual(0, f0.eventData.GeometryChangedCount);
            Assert.AreEqual(0, data.layer.Elements.Count(x => x is Face));
            Assert.AreEqual(0, l[0].Faces.Count);
            Assert.AreEqual(0, l[2].Faces.Count);
        }

        [TestMethod]
        public void Readd()
        {
            var data = GeometryModelHelper.EmptyModelWithEvents();
            (var v, var e, var l) = TestData(data.layer);

            //Add face
            var f0 = FaceWithEvents(new Face(data.layer, "", l[0], GeometricOrientation.Forward, new EdgeLoop[] { l[2] }));
            f0.f.RemoveFromModel();
            data.eventData.Reset();

            f0.f.AddToModel();

            Assert.AreEqual(1, data.model.Geometry.Faces.Count);
            Assert.AreEqual(0, data.eventData.BatchOperationFinishedCount);
            Assert.AreEqual(1, data.eventData.TopologyChangedEventData.Count);
            Assert.AreEqual(1, data.eventData.AddEventData.Count);
            Assert.AreEqual(1, data.eventData.AddEventData[0].Count());
            Assert.AreEqual(f0.f, data.eventData.AddEventData[0].First());
            Assert.AreEqual(0, data.eventData.RemoveEventData.Count);
            Assert.AreEqual(0, data.eventData.GeometryChangedEventData.Count);
            Assert.AreEqual(0, f0.eventData.GeometryChangedCount);
            Assert.AreEqual(1, data.layer.Elements.Count(x => x is Face));
            Assert.AreEqual(1, l[0].Faces.Count);
            Assert.AreEqual(1, l[2].Faces.Count);

            Assert.ThrowsException<Exception>(() => f0.f.AddToModel());
        }

        [TestMethod]
        public void BatchAdd()
        {
            var data = GeometryModelHelper.EmptyModelWithEvents();
            (var v, var e, var l) = TestData(data.layer);
            data.eventData.Reset();

            data.model.Geometry.StartBatchOperation();

            //Add face
            var f0 = FaceWithEvents(new Face(data.layer, "", l[0], GeometricOrientation.Forward, new EdgeLoop[] { l[2] }));
            var f1 = FaceWithEvents(new Face(data.layer, "", l[2], GeometricOrientation.Forward, null));

            Assert.AreEqual(0, data.eventData.TopologyChangedEventData.Count);
            Assert.AreEqual(0, data.eventData.AddEventData.Count);
            Assert.AreEqual(0, data.eventData.RemoveEventData.Count);
            Assert.AreEqual(0, data.eventData.GeometryChangedEventData.Count);
            Assert.AreEqual(0, data.eventData.BatchOperationFinishedCount);

            data.model.Geometry.EndBatchOperation();

            //Geometry Model checks
            Assert.AreEqual(2, data.model.Geometry.Faces.Count);
            Assert.IsTrue(data.model.Geometry.Faces.Contains(f0.f));
            Assert.IsTrue(data.model.Geometry.Faces.Contains(f1.f));

            Assert.AreEqual(1, l[0].Faces.Count);
            Assert.AreEqual(2, l[2].Faces.Count);

            Assert.AreEqual(1, data.eventData.BatchOperationFinishedCount);
            Assert.AreEqual(1, data.eventData.TopologyChangedEventData.Count);
            Assert.AreEqual(1, data.eventData.AddEventData.Count);
            Assert.AreEqual(2, data.eventData.AddEventData[0].Count());
            Assert.IsTrue(data.eventData.AddEventData[0].Contains(f0.f));
            Assert.IsTrue(data.eventData.AddEventData[0].Contains(f1.f));
            Assert.AreEqual(0, data.eventData.RemoveEventData.Count);
            Assert.AreEqual(0, data.eventData.GeometryChangedEventData.Count);
            Assert.AreEqual(0, f0.eventData.GeometryChangedCount);
            Assert.AreEqual(0, f1.eventData.GeometryChangedCount);
            Assert.AreEqual(2, data.layer.Elements.Count(x => x is Face));
        }

        [TestMethod]
        public void BatchRemove()
        {
            var data = GeometryModelHelper.EmptyModelWithEvents();
            (var v, var e, var l) = TestData(data.layer);
            var l0edges = new Edge[] { e[0], e[1], e[2] };
            var l1edges = new Edge[] { e[0], e[3], e[4] };
            var f0 = FaceWithEvents(new Face(data.layer, "", l[0], GeometricOrientation.Forward, new EdgeLoop[] { l[2] }));
            var f1 = FaceWithEvents(new Face(data.layer, "", l[2], GeometricOrientation.Forward, null));

            data.eventData.Reset();
            f0.eventData.Reset();
            f1.eventData.Reset();

            data.model.Geometry.StartBatchOperation();

            f0.f.RemoveFromModel();
            f1.f.RemoveFromModel();

            //No events should be issued during batch operation
            Assert.AreEqual(0, data.eventData.BatchOperationFinishedCount);
            Assert.AreEqual(0, data.eventData.TopologyChangedEventData.Count);
            Assert.AreEqual(0, data.eventData.AddEventData.Count);
            Assert.AreEqual(0, data.eventData.RemoveEventData.Count);
            Assert.AreEqual(0, data.eventData.GeometryChangedEventData.Count);

            data.model.Geometry.EndBatchOperation();

            Assert.AreEqual(0, data.model.Geometry.Faces.Count);
            Assert.AreEqual(0, data.layer.Elements.Count(x => x is Face));
            Assert.AreEqual(0, l[0].Faces.Count);
            Assert.AreEqual(0, l[2].Faces.Count);

            Assert.AreEqual(1, data.eventData.BatchOperationFinishedCount);
            Assert.AreEqual(1, data.eventData.TopologyChangedEventData.Count);
            Assert.AreEqual(0, data.eventData.AddEventData.Count);
            Assert.AreEqual(1, data.eventData.RemoveEventData.Count);
            Assert.AreEqual(2, data.eventData.RemoveEventData[0].Count());
            Assert.IsTrue(data.eventData.RemoveEventData[0].Contains(f0.f));
            Assert.IsTrue(data.eventData.RemoveEventData[0].Contains(f1.f));
            Assert.AreEqual(0, data.eventData.GeometryChangedEventData.Count);
            Assert.AreEqual(0, f0.eventData.GeometryChangedCount);
            Assert.AreEqual(0, f1.eventData.GeometryChangedCount);
        }

        [TestMethod]
        public void GeomeryChanged()
        {
            var data = GeometryModelHelper.EmptyModelWithEvents();
            (var v, var e, var l) = TestData(data.layer);
            var f0 = FaceWithEvents(new Face(data.layer, "", l[0], GeometricOrientation.Forward, new EdgeLoop[] { l[2] }));
            var f1 = FaceWithEvents(new Face(data.layer, "", l[1]));
            f0.eventData.Reset();
            f1.eventData.Reset();
            data.eventData.Reset();

            v[0].Position = new Point3D(-2, -4, -6);
            Assert.AreEqual(7, data.eventData.GeometryChangedEventData.Count());
            for (int i = 0; i < data.eventData.GeometryChangedEventData.Count(); ++i)
                Assert.AreEqual(1, data.eventData.GeometryChangedEventData[i].Count());
            Assert.AreEqual(f0.f, data.eventData.GeometryChangedEventData[3].First());
            Assert.AreEqual(f0.f, data.eventData.GeometryChangedEventData[6].First());
            Assert.AreEqual(0, data.eventData.AddEventData.Count());
            Assert.AreEqual(0, data.eventData.RemoveEventData.Count());
            Assert.AreEqual(0, data.eventData.BatchOperationFinishedCount);
            Assert.AreEqual(0, data.eventData.TopologyChangedEventData.Count);
            Assert.AreEqual(2, f0.eventData.GeometryChangedCount);
            Assert.AreEqual(0, f1.eventData.GeometryChangedCount);

            data.eventData.Reset();
            f0.eventData.Reset();
            f1.eventData.Reset();

            v[2].Position = new Point3D(-1, -2, -3);
            Assert.AreEqual(12, data.eventData.GeometryChangedEventData.Count());
            for (int i = 0; i < data.eventData.GeometryChangedEventData.Count(); ++i)
                Assert.AreEqual(1, data.eventData.GeometryChangedEventData[i].Count());
            Assert.AreEqual(f0.f, data.eventData.GeometryChangedEventData[3].First());
            Assert.AreEqual(f0.f, data.eventData.GeometryChangedEventData[6].First());
            Assert.AreEqual(f1.f, data.eventData.GeometryChangedEventData[8].First());
            Assert.AreEqual(f1.f, data.eventData.GeometryChangedEventData[11].First());

            Assert.AreEqual(0, data.eventData.AddEventData.Count());
            Assert.AreEqual(0, data.eventData.RemoveEventData.Count());
            Assert.AreEqual(0, data.eventData.BatchOperationFinishedCount);
            Assert.AreEqual(0, data.eventData.TopologyChangedEventData.Count);
            Assert.AreEqual(2, f0.eventData.GeometryChangedCount);
            Assert.AreEqual(2, f1.eventData.GeometryChangedCount);
        }

        [TestMethod]
        public void GeomeryChangedHole()
        {
            var data = GeometryModelHelper.EmptyModelWithEvents();
            (var v, var e, var l) = TestData(data.layer);
            var f0 = FaceWithEvents(new Face(data.layer, "", l[0], GeometricOrientation.Forward, new EdgeLoop[] { l[2] }));
            f0.eventData.Reset();
            data.eventData.Reset();

            v[6].Position = new Point3D(-2, -4, -6);
            Assert.AreEqual(7, data.eventData.GeometryChangedEventData.Count());
            for (int i = 0; i < data.eventData.GeometryChangedEventData.Count(); ++i)
                Assert.AreEqual(1, data.eventData.GeometryChangedEventData[i].Count());
            Assert.AreEqual(f0.f, data.eventData.GeometryChangedEventData[3].First());
            Assert.AreEqual(f0.f, data.eventData.GeometryChangedEventData[6].First());
            Assert.AreEqual(0, data.eventData.AddEventData.Count());
            Assert.AreEqual(0, data.eventData.RemoveEventData.Count());
            Assert.AreEqual(0, data.eventData.BatchOperationFinishedCount);
            Assert.AreEqual(0, data.eventData.TopologyChangedEventData.Count);
            Assert.AreEqual(2, f0.eventData.GeometryChangedCount);
            Assert.AreEqual(0, f0.eventData.TopologyChangedCount);
        }

        [TestMethod]
        public void BatchGeomeryChanged()
        {
            var data = GeometryModelHelper.EmptyModelWithEvents();
            (var v, var e, var l) = TestData(data.layer);
            var f0 = FaceWithEvents(new Face(data.layer, "", l[0], GeometricOrientation.Forward, new EdgeLoop[] { l[2] }));
            var f1 = FaceWithEvents(new Face(data.layer, "", l[1]));
            f0.eventData.Reset();
            f1.eventData.Reset();
            data.eventData.Reset();

            data.model.Geometry.StartBatchOperation();

            v[0].Position = new Point3D(-2, -4, -6);
            v[2].Position = new Point3D(-1, -2, -3);

            Assert.AreEqual(0, data.eventData.GeometryChangedEventData.Count());
            Assert.AreEqual(0, data.eventData.AddEventData.Count());
            Assert.AreEqual(0, data.eventData.RemoveEventData.Count());
            Assert.AreEqual(0, data.eventData.BatchOperationFinishedCount);
            Assert.AreEqual(0, data.eventData.TopologyChangedEventData.Count);
            Assert.AreEqual(0, f0.eventData.GeometryChangedCount);
            Assert.AreEqual(0, f1.eventData.GeometryChangedCount);

            data.model.Geometry.EndBatchOperation();

            Assert.AreEqual(1, data.eventData.GeometryChangedEventData.Count());
            Assert.AreEqual(11, data.eventData.GeometryChangedEventData[0].Count());
            Assert.IsTrue(data.eventData.GeometryChangedEventData[0].Contains(f0.f));
            Assert.IsTrue(data.eventData.GeometryChangedEventData[0].Contains(f1.f));

            Assert.AreEqual(0, data.eventData.AddEventData.Count());
            Assert.AreEqual(0, data.eventData.RemoveEventData.Count());
            Assert.AreEqual(1, data.eventData.BatchOperationFinishedCount);
            Assert.AreEqual(0, data.eventData.TopologyChangedEventData.Count);
            Assert.AreEqual(1, f0.eventData.GeometryChangedCount);
            Assert.AreEqual(1, f1.eventData.GeometryChangedCount);
        }

        [TestMethod]
        public void BatchGeomeryChangedHole()
        {
            var data = GeometryModelHelper.EmptyModelWithEvents();
            (var v, var e, var l) = TestData(data.layer);
            var f0 = FaceWithEvents(new Face(data.layer, "", l[0], GeometricOrientation.Forward, new EdgeLoop[] { l[2] }));
            f0.eventData.Reset();
            data.eventData.Reset();

            data.model.Geometry.StartBatchOperation();

            v[6].Position = new Point3D(-2, -4, -6);
            v[8].Position = new Point3D(-1, -2, -3);

            Assert.AreEqual(0, data.eventData.GeometryChangedEventData.Count());
            Assert.AreEqual(0, data.eventData.AddEventData.Count());
            Assert.AreEqual(0, data.eventData.RemoveEventData.Count());
            Assert.AreEqual(0, data.eventData.BatchOperationFinishedCount);
            Assert.AreEqual(0, data.eventData.TopologyChangedEventData.Count);
            Assert.AreEqual(0, f0.eventData.GeometryChangedCount);
            Assert.AreEqual(0, f0.eventData.TopologyChangedCount);

            data.model.Geometry.EndBatchOperation();

            Assert.AreEqual(1, data.eventData.GeometryChangedEventData.Count());
            Assert.AreEqual(8, data.eventData.GeometryChangedEventData[0].Count());
            Assert.IsTrue(data.eventData.GeometryChangedEventData[0].Contains(f0.f));

            Assert.AreEqual(0, data.eventData.AddEventData.Count());
            Assert.AreEqual(0, data.eventData.RemoveEventData.Count());
            Assert.AreEqual(1, data.eventData.BatchOperationFinishedCount);
            Assert.AreEqual(0, data.eventData.TopologyChangedEventData.Count);
            Assert.AreEqual(1, f0.eventData.GeometryChangedCount);
            Assert.AreEqual(0, f0.eventData.TopologyChangedCount);
        }

        [TestMethod]
        public void ExchangeVertex()
        {
            var data = GeometryModelHelper.EmptyModelWithEvents();
            (var v, var e, var l) = TestData(data.layer);
            var f0 = FaceWithEvents(new Face(data.layer, "", l[0]));

            Vertex replaceVertex = new Vertex(data.layer, "", v[1].Position);

            data.eventData.Reset();
            f0.eventData.Reset();

            data.model.Geometry.StartBatchOperation();

            e[0].Vertices[1] = replaceVertex;
            e[1].Vertices[0] = replaceVertex;

            Assert.AreEqual(0, data.eventData.GeometryChangedEventData.Count());
            Assert.AreEqual(0, data.eventData.AddEventData.Count());
            Assert.AreEqual(0, data.eventData.RemoveEventData.Count());
            Assert.AreEqual(0, data.eventData.BatchOperationFinishedCount);
            Assert.AreEqual(0, data.eventData.TopologyChangedEventData.Count);
            Assert.AreEqual(0, f0.eventData.GeometryChangedCount);
            Assert.AreEqual(0, f0.eventData.TopologyChangedCount);

            data.model.Geometry.EndBatchOperation();

            Assert.AreEqual(0, data.eventData.GeometryChangedEventData.Count());
            Assert.AreEqual(0, data.eventData.AddEventData.Count());
            Assert.AreEqual(0, data.eventData.RemoveEventData.Count());
            Assert.AreEqual(1, data.eventData.BatchOperationFinishedCount);
            Assert.AreEqual(1, data.eventData.TopologyChangedEventData.Count);
            Assert.AreEqual(0, f0.eventData.GeometryChangedCount);
            Assert.AreEqual(1, f0.eventData.TopologyChangedCount);
        }

        [TestMethod]
        public void EdgeExchange()
        {
            var data = GeometryModelHelper.EmptyModelWithEvents();
            (var v, var e, var l) = TestData(data.layer);
            var f0 = FaceWithEvents(new Face(data.layer, "", l[0]));

            var replaceEdge = new Edge(data.layer, "", e[2].Vertices);

            data.eventData.Reset();
            f0.eventData.Reset();

            f0.f.Boundary.Edges[2] = new PEdge(replaceEdge, GeometricOrientation.Forward, f0.f.Boundary);

            //Events
            Assert.AreEqual(1, data.eventData.GeometryChangedEventData.Count());
            Assert.AreEqual(0, data.eventData.AddEventData.Count());
            Assert.AreEqual(0, data.eventData.RemoveEventData.Count());
            Assert.AreEqual(0, data.eventData.BatchOperationFinishedCount);
            Assert.AreEqual(2, data.eventData.TopologyChangedEventData.Count);
            Assert.AreEqual(1, data.eventData.TopologyChangedEventData[0].Count());
            Assert.IsTrue(data.eventData.TopologyChangedEventData[0].Contains(f0.f.Boundary));
            Assert.AreEqual(1, data.eventData.TopologyChangedEventData[1].Count());
            Assert.IsTrue(data.eventData.TopologyChangedEventData[1].Contains(f0.f));
            Assert.AreEqual(1, f0.eventData.GeometryChangedCount);
            Assert.AreEqual(1, f0.eventData.TopologyChangedCount);
        }

        [TestMethod]
        public void EdgeExchangeHole()
        {
            var data = GeometryModelHelper.EmptyModelWithEvents();
            (var v, var e, var l) = TestData(data.layer);
            var f0 = FaceWithEvents(new Face(data.layer, "", l[0], GeometricOrientation.Forward, new EdgeLoop[] { l[2] }));

            var replaceEdge = new Edge(data.layer, "", l[2].Edges[2].Edge.Vertices);

            data.eventData.Reset();
            f0.eventData.Reset();

            l[2].Edges[2] = new PEdge(replaceEdge, GeometricOrientation.Undefined, l[2]);

            //Events
            Assert.AreEqual(1, data.eventData.GeometryChangedEventData.Count());
            Assert.AreEqual(0, data.eventData.AddEventData.Count());
            Assert.AreEqual(0, data.eventData.RemoveEventData.Count());
            Assert.AreEqual(0, data.eventData.BatchOperationFinishedCount);
            Assert.AreEqual(2, data.eventData.TopologyChangedEventData.Count);
            Assert.AreEqual(1, data.eventData.TopologyChangedEventData[0].Count());
            Assert.IsTrue(data.eventData.TopologyChangedEventData[0].Contains(l[2]));
            Assert.AreEqual(1, data.eventData.TopologyChangedEventData[1].Count());
            Assert.IsTrue(data.eventData.TopologyChangedEventData[1].Contains(f0.f));
            Assert.AreEqual(1, f0.eventData.GeometryChangedCount);
            Assert.AreEqual(1, f0.eventData.TopologyChangedCount);
        }

        [TestMethod]
        public void BatchEdgeExchange()
        {
            var data = GeometryModelHelper.EmptyModelWithEvents();
            (var v, var e, var l) = TestData(data.layer);
            var f0 = FaceWithEvents(new Face(data.layer, "", l[0]));

            var replaceEdge = new Edge(data.layer, "", e[2].Vertices);
            var replaceEdge2 = new Edge(data.layer, "", e[3].Vertices);

            data.eventData.Reset();
            f0.eventData.Reset();

            data.model.Geometry.StartBatchOperation();

            f0.f.Boundary.Edges[2] = new PEdge(replaceEdge, GeometricOrientation.Forward, f0.f.Boundary);
            f0.f.Boundary.Edges[3] = new PEdge(replaceEdge2, GeometricOrientation.Forward, f0.f.Boundary);

            Assert.AreEqual(0, data.eventData.GeometryChangedEventData.Count());
            Assert.AreEqual(0, data.eventData.AddEventData.Count());
            Assert.AreEqual(0, data.eventData.RemoveEventData.Count());
            Assert.AreEqual(0, data.eventData.BatchOperationFinishedCount);
            Assert.AreEqual(0, data.eventData.TopologyChangedEventData.Count);
            Assert.AreEqual(0, f0.eventData.GeometryChangedCount);
            Assert.AreEqual(0, f0.eventData.TopologyChangedCount);

            data.model.Geometry.EndBatchOperation();

            //Events
            Assert.AreEqual(0, data.eventData.GeometryChangedEventData.Count());
            Assert.AreEqual(0, data.eventData.AddEventData.Count());
            Assert.AreEqual(0, data.eventData.RemoveEventData.Count());
            Assert.AreEqual(1, data.eventData.BatchOperationFinishedCount);
            Assert.AreEqual(1, data.eventData.TopologyChangedEventData.Count);
            Assert.AreEqual(0, f0.eventData.GeometryChangedCount);
            Assert.AreEqual(1, f0.eventData.TopologyChangedCount);
        }

        [TestMethod]
        public void BatchEdgeExchangeHole()
        {
            var data = GeometryModelHelper.EmptyModelWithEvents();
            (var v, var e, var l) = TestData(data.layer);
            var f0 = FaceWithEvents(new Face(data.layer, "", l[0], GeometricOrientation.Forward, new EdgeLoop[] { l[2] }));

            var replaceEdge = new Edge(data.layer, "", l[2].Edges[2].Edge.Vertices);
            var replaceEdge2 = new Edge(data.layer, "", l[2].Edges[3].Edge.Vertices);

            data.eventData.Reset();
            f0.eventData.Reset();

            data.model.Geometry.StartBatchOperation();

            f0.f.Holes[0].Edges[2] = new PEdge(replaceEdge, GeometricOrientation.Forward, f0.f.Holes[0]);
            f0.f.Holes[0].Edges[3] = new PEdge(replaceEdge2, GeometricOrientation.Forward, f0.f.Holes[0]);

            Assert.AreEqual(0, data.eventData.GeometryChangedEventData.Count());
            Assert.AreEqual(0, data.eventData.AddEventData.Count());
            Assert.AreEqual(0, data.eventData.RemoveEventData.Count());
            Assert.AreEqual(0, data.eventData.BatchOperationFinishedCount);
            Assert.AreEqual(0, data.eventData.TopologyChangedEventData.Count);
            Assert.AreEqual(0, f0.eventData.GeometryChangedCount);
            Assert.AreEqual(0, f0.eventData.TopologyChangedCount);

            data.model.Geometry.EndBatchOperation();

            //Events
            Assert.AreEqual(0, data.eventData.GeometryChangedEventData.Count());
            Assert.AreEqual(0, data.eventData.AddEventData.Count());
            Assert.AreEqual(0, data.eventData.RemoveEventData.Count());
            Assert.AreEqual(1, data.eventData.BatchOperationFinishedCount);
            Assert.AreEqual(1, data.eventData.TopologyChangedEventData.Count);
            Assert.AreEqual(0, f0.eventData.GeometryChangedCount);
            Assert.AreEqual(1, f0.eventData.TopologyChangedCount);
        }

        [TestMethod]
        public void HoleExchange()
        {
            var data = GeometryModelHelper.EmptyModelWithEvents();
            (var v, var e, var l) = TestData(data.layer);
            var f0 = FaceWithEvents(new Face(data.layer, "", l[0], GeometricOrientation.Forward, new EdgeLoop[] { l[2] }));
            data.eventData.Reset();

            f0.f.Holes[0] = l[3];
            Assert.AreEqual(l[3], f0.f.Holes[0]);
            Assert.AreEqual(0, data.eventData.GeometryChangedEventData.Count());
            Assert.AreEqual(0, data.eventData.AddEventData.Count());
            Assert.AreEqual(0, data.eventData.RemoveEventData.Count());
            Assert.AreEqual(0, data.eventData.BatchOperationFinishedCount);
            Assert.AreEqual(1, data.eventData.TopologyChangedEventData.Count);
            Assert.AreEqual(0, f0.eventData.GeometryChangedCount);
            Assert.AreEqual(1, f0.eventData.TopologyChangedCount);

            v[12].Position = new Point3D(-1, -1, -1);
            Assert.AreEqual(7, data.eventData.GeometryChangedEventData.Count());
            Assert.IsTrue(data.eventData.GeometryChangedEventData[3].Contains(f0.f));
            Assert.IsTrue(data.eventData.GeometryChangedEventData[6].Contains(f0.f));
            Assert.AreEqual(0, data.eventData.AddEventData.Count());
            Assert.AreEqual(0, data.eventData.RemoveEventData.Count());
            Assert.AreEqual(0, data.eventData.BatchOperationFinishedCount);
            Assert.AreEqual(1, data.eventData.TopologyChangedEventData.Count);
            Assert.AreEqual(2, f0.eventData.GeometryChangedCount);
            Assert.AreEqual(1, f0.eventData.TopologyChangedCount);
        }

        [TestMethod]
        public void BatchHoleExchange()

        {
            var data = GeometryModelHelper.EmptyModelWithEvents();
            (var v, var e, var l) = TestData(data.layer);
            var f0 = FaceWithEvents(new Face(data.layer, "", l[0], GeometricOrientation.Forward, new EdgeLoop[] { l[2] }));
            data.eventData.Reset();

            data.model.Geometry.StartBatchOperation();

            f0.f.Holes[0] = l[3];

            data.model.Geometry.EndBatchOperation();

            Assert.AreEqual(l[3], f0.f.Holes[0]);
            Assert.AreEqual(0, data.eventData.GeometryChangedEventData.Count());
            Assert.AreEqual(0, data.eventData.AddEventData.Count());
            Assert.AreEqual(0, data.eventData.RemoveEventData.Count());
            Assert.AreEqual(1, data.eventData.BatchOperationFinishedCount);
            Assert.AreEqual(1, data.eventData.TopologyChangedEventData.Count);
            Assert.AreEqual(0, f0.eventData.GeometryChangedCount);
            Assert.AreEqual(1, f0.eventData.TopologyChangedCount);

            v[12].Position = new Point3D(-1, -1, -1);
            Assert.AreEqual(7, data.eventData.GeometryChangedEventData.Count());
            Assert.IsTrue(data.eventData.GeometryChangedEventData[3].Contains(f0.f));
            Assert.IsTrue(data.eventData.GeometryChangedEventData[6].Contains(f0.f));
            Assert.AreEqual(0, data.eventData.AddEventData.Count());
            Assert.AreEqual(0, data.eventData.RemoveEventData.Count());
            Assert.AreEqual(1, data.eventData.BatchOperationFinishedCount);
            Assert.AreEqual(1, data.eventData.TopologyChangedEventData.Count);
            Assert.AreEqual(2, f0.eventData.GeometryChangedCount);
            Assert.AreEqual(1, f0.eventData.TopologyChangedCount);
        }

        [TestMethod]
        public void HoleAdd()
        {
            var data = GeometryModelHelper.EmptyModelWithEvents();
            (var v, var e, var l) = TestData(data.layer);
            var f0 = FaceWithEvents(new Face(data.layer, "", l[0], GeometricOrientation.Forward, new EdgeLoop[] { l[2] }));
            data.eventData.Reset();

            f0.f.Holes.Add(l[3]);

            Assert.AreEqual(2, f0.f.Holes.Count);
            Assert.AreEqual(l[3], f0.f.Holes[1]);
            Assert.AreEqual(0, data.eventData.GeometryChangedEventData.Count());
            Assert.AreEqual(0, data.eventData.AddEventData.Count());
            Assert.AreEqual(0, data.eventData.RemoveEventData.Count());
            Assert.AreEqual(0, data.eventData.BatchOperationFinishedCount);
            Assert.AreEqual(1, data.eventData.TopologyChangedEventData.Count);
            Assert.AreEqual(0, f0.eventData.GeometryChangedCount);
            Assert.AreEqual(1, f0.eventData.TopologyChangedCount);

            v[12].Position = new Point3D(-1, -1, -1);
            Assert.AreEqual(7, data.eventData.GeometryChangedEventData.Count());
            Assert.IsTrue(data.eventData.GeometryChangedEventData[3].Contains(f0.f));
            Assert.IsTrue(data.eventData.GeometryChangedEventData[6].Contains(f0.f));
            Assert.AreEqual(0, data.eventData.AddEventData.Count());
            Assert.AreEqual(0, data.eventData.RemoveEventData.Count());
            Assert.AreEqual(0, data.eventData.BatchOperationFinishedCount);
            Assert.AreEqual(1, data.eventData.TopologyChangedEventData.Count);
            Assert.AreEqual(2, f0.eventData.GeometryChangedCount);
            Assert.AreEqual(1, f0.eventData.TopologyChangedCount);
        }

        [TestMethod]
        public void BatchHoleAdd()
        {
            var data = GeometryModelHelper.EmptyModelWithEvents();
            (var v, var e, var l) = TestData(data.layer);
            var f0 = FaceWithEvents(new Face(data.layer, "", l[0], GeometricOrientation.Forward, new EdgeLoop[] { l[2] }));
            data.eventData.Reset();

            data.model.Geometry.StartBatchOperation();

            f0.f.Holes.Add(l[3]);

            data.model.Geometry.EndBatchOperation();

            Assert.AreEqual(2, f0.f.Holes.Count);
            Assert.AreEqual(l[3], f0.f.Holes[1]);
            Assert.AreEqual(0, data.eventData.GeometryChangedEventData.Count());
            Assert.AreEqual(0, data.eventData.AddEventData.Count());
            Assert.AreEqual(0, data.eventData.RemoveEventData.Count());
            Assert.AreEqual(1, data.eventData.BatchOperationFinishedCount);
            Assert.AreEqual(1, data.eventData.TopologyChangedEventData.Count);
            Assert.AreEqual(0, f0.eventData.GeometryChangedCount);
            Assert.AreEqual(1, f0.eventData.TopologyChangedCount);

            v[12].Position = new Point3D(-1, -1, -1);
            Assert.AreEqual(7, data.eventData.GeometryChangedEventData.Count());
            Assert.IsTrue(data.eventData.GeometryChangedEventData[3].Contains(f0.f));
            Assert.IsTrue(data.eventData.GeometryChangedEventData[6].Contains(f0.f));
            Assert.AreEqual(0, data.eventData.AddEventData.Count());
            Assert.AreEqual(0, data.eventData.RemoveEventData.Count());
            Assert.AreEqual(1, data.eventData.BatchOperationFinishedCount);
            Assert.AreEqual(1, data.eventData.TopologyChangedEventData.Count);
            Assert.AreEqual(2, f0.eventData.GeometryChangedCount);
            Assert.AreEqual(1, f0.eventData.TopologyChangedCount);
        }

        [TestMethod]
        public void HoleRemove()
        {
            var data = GeometryModelHelper.EmptyModelWithEvents();
            (var v, var e, var l) = TestData(data.layer);
            var f0 = FaceWithEvents(new Face(data.layer, "", l[0], GeometricOrientation.Forward, new EdgeLoop[] { l[2] }));
            data.eventData.Reset();

            f0.f.Holes.RemoveAt(0);

            Assert.AreEqual(0, f0.f.Holes.Count);
            Assert.AreEqual(0, data.eventData.GeometryChangedEventData.Count());
            Assert.AreEqual(0, data.eventData.AddEventData.Count());
            Assert.AreEqual(0, data.eventData.RemoveEventData.Count());
            Assert.AreEqual(0, data.eventData.BatchOperationFinishedCount);
            Assert.AreEqual(1, data.eventData.TopologyChangedEventData.Count);
            Assert.AreEqual(0, f0.eventData.GeometryChangedCount);
            Assert.AreEqual(1, f0.eventData.TopologyChangedCount);

            v[12].Position = new Point3D(-1, -1, -1);
            Assert.AreEqual(5, data.eventData.GeometryChangedEventData.Count());
            Assert.IsTrue(data.eventData.GeometryChangedEventData.All(x => x != f0.f));
            Assert.AreEqual(0, data.eventData.AddEventData.Count());
            Assert.AreEqual(0, data.eventData.RemoveEventData.Count());
            Assert.AreEqual(0, data.eventData.BatchOperationFinishedCount);
            Assert.AreEqual(1, data.eventData.TopologyChangedEventData.Count);
            Assert.AreEqual(0, f0.eventData.GeometryChangedCount);
            Assert.AreEqual(1, f0.eventData.TopologyChangedCount);
        }

        [TestMethod]
        public void BatchHoleRemove()
        {
            var data = GeometryModelHelper.EmptyModelWithEvents();
            (var v, var e, var l) = TestData(data.layer);
            var f0 = FaceWithEvents(new Face(data.layer, "", l[0], GeometricOrientation.Forward, new EdgeLoop[] { l[2] }));
            data.eventData.Reset();

            data.model.Geometry.StartBatchOperation();

            f0.f.Holes.RemoveAt(0);

            data.model.Geometry.EndBatchOperation();

            Assert.AreEqual(0, f0.f.Holes.Count);
            Assert.AreEqual(0, data.eventData.GeometryChangedEventData.Count());
            Assert.AreEqual(0, data.eventData.AddEventData.Count());
            Assert.AreEqual(0, data.eventData.RemoveEventData.Count());
            Assert.AreEqual(1, data.eventData.BatchOperationFinishedCount);
            Assert.AreEqual(1, data.eventData.TopologyChangedEventData.Count);
            Assert.AreEqual(0, f0.eventData.GeometryChangedCount);
            Assert.AreEqual(1, f0.eventData.TopologyChangedCount);

            v[12].Position = new Point3D(-1, -1, -1);
            Assert.AreEqual(5, data.eventData.GeometryChangedEventData.Count());
            Assert.IsTrue(data.eventData.GeometryChangedEventData.All(x => x != f0.f));
            Assert.AreEqual(0, data.eventData.AddEventData.Count());
            Assert.AreEqual(0, data.eventData.RemoveEventData.Count());
            Assert.AreEqual(1, data.eventData.BatchOperationFinishedCount);
            Assert.AreEqual(1, data.eventData.TopologyChangedEventData.Count);
            Assert.AreEqual(0, f0.eventData.GeometryChangedCount);
            Assert.AreEqual(1, f0.eventData.TopologyChangedCount);
        }

        [TestMethod]
        public void Visibility()
        {
            //Data
            var data = GeometryModelHelper.EmptyModelWithEvents();
            (var v, var e, var l) = TestData(data.layer);
            var f0 = FaceWithEvents(new Face(data.layer, "", l[0], GeometricOrientation.Forward, new EdgeLoop[] { l[2] }));
            data.eventData.Reset();

            Assert.AreEqual(true, data.layer.IsVisible);
            Assert.AreEqual(true, f0.f.IsVisible);
            Assert.AreEqual(true, f0.f.IsActuallyVisible);
            Assert.AreEqual(0, f0.eventData.PropertyChangedData.Count);

            f0.f.IsVisible = false;
            Assert.AreEqual(false, f0.f.IsVisible);
            Assert.AreEqual(false, f0.f.IsActuallyVisible);
            Assert.AreEqual(2, f0.eventData.PropertyChangedData.Count);
            Assert.AreEqual(nameof(Vertex.IsVisible), f0.eventData.PropertyChangedData[0]);
            Assert.AreEqual(nameof(Vertex.IsActuallyVisible), f0.eventData.PropertyChangedData[1]);

            f0.f.IsVisible = true;
            Assert.AreEqual(true, f0.f.IsVisible);
            Assert.AreEqual(true, f0.f.IsActuallyVisible);
            Assert.AreEqual(4, f0.eventData.PropertyChangedData.Count);
            Assert.AreEqual(nameof(Vertex.IsVisible), f0.eventData.PropertyChangedData[2]);
            Assert.AreEqual(nameof(Vertex.IsActuallyVisible), f0.eventData.PropertyChangedData[3]);

            data.layer.IsVisible = false;
            Assert.AreEqual(true, f0.f.IsVisible);
            Assert.AreEqual(false, f0.f.IsActuallyVisible);
            Assert.AreEqual(5, f0.eventData.PropertyChangedData.Count);
            Assert.AreEqual(nameof(Vertex.IsActuallyVisible), f0.eventData.PropertyChangedData[4]);

            data.layer.IsVisible = true;
            Assert.AreEqual(true, f0.f.IsVisible);
            Assert.AreEqual(true, f0.f.IsActuallyVisible);
            Assert.AreEqual(6, f0.eventData.PropertyChangedData.Count);
            Assert.AreEqual(nameof(Vertex.IsActuallyVisible), f0.eventData.PropertyChangedData[5]);

            Assert.AreEqual(0, f0.eventData.GeometryChangedCount);
        }

        [TestMethod]
        public void Name()
        {
            //Data
            var data = GeometryModelHelper.EmptyModelWithEvents();
            (var v, var e, var l) = TestData(data.layer);
            var f0 = FaceWithEvents(new Face(data.layer, "", l[0], GeometricOrientation.Forward, new EdgeLoop[] { l[2] }));
            data.eventData.Reset();

            Assert.AreEqual(0, f0.eventData.PropertyChangedData.Count);

            f0.f.Name = "Renamed";
            Assert.AreEqual("Renamed", f0.f.Name);
            Assert.AreEqual(1, f0.eventData.PropertyChangedData.Count);
            Assert.AreEqual(nameof(Vertex.Name), f0.eventData.PropertyChangedData[0]);
        }

        [TestMethod]
        public void NonExistingHoleTest()
        {
            //Data
            var data = GeometryModelHelper.EmptyModelWithEvents();
            (var v, var e, var l) = TestData(data.layer);
            var f0 = FaceWithEvents(new Face(data.layer, "", l[0], GeometricOrientation.Forward, new EdgeLoop[] { l[2] }));
            data.eventData.Reset();

            data.model.Geometry.StartBatchOperation();
            l[2].RemoveFromModel();

            Assert.ThrowsException<Exception>(() => data.model.Geometry.EndBatchOperation());
        }

        [TestMethod]
        public void MoveToLayer()
        {
            var data = GeometryModelHelper.EmptyModelWithEvents();
            Layer targetLayer = new Layer(data.model.Geometry, "TargetLayer") { Color = new DerivedColor(Colors.Pink) };

            (var v, var e, var l) = TestData(data.layer);
            var f0 = FaceWithEvents(new Face(data.layer, "", l[0], GeometricOrientation.Forward, new EdgeLoop[] { l[2] }));
            data.eventData.Reset();

            Assert.AreEqual(data.layer, f0.f.Layer);
            Assert.AreEqual(1, data.layer.Elements.Count(x => x is Face));
            Assert.AreEqual(Colors.Red, f0.f.Color.Color);

            f0.f.Layer = targetLayer;
            Assert.AreEqual(targetLayer, f0.f.Layer);
            Assert.AreEqual(0, data.layer.Elements.Count(x => x is Face));
            Assert.AreEqual(1, targetLayer.Elements.Count(x => x is Face));
            Assert.AreEqual(Colors.Pink, f0.f.Color.Color);

            Assert.AreEqual(0, data.eventData.GeometryChangedEventData.Count);
            Assert.AreEqual(0, data.eventData.AddEventData.Count);
            Assert.AreEqual(0, data.eventData.RemoveEventData.Count);
            Assert.AreEqual(0, data.eventData.BatchOperationFinishedCount);
            Assert.AreEqual(0, data.eventData.TopologyChangedEventData.Count);
            Assert.AreEqual(0, f0.eventData.GeometryChangedCount);
            Assert.AreEqual(3, f0.eventData.PropertyChangedData.Count);
            Assert.IsTrue(f0.eventData.PropertyChangedData.Contains("Layer"));
            Assert.IsTrue(f0.eventData.PropertyChangedData.Contains("Color"));
            Assert.IsTrue(f0.eventData.PropertyChangedData.Contains("IsActuallyVisible"));
        }
    }
}
