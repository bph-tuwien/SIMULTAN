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
    public class VolumeTests
    {
        private (Volume v, BaseGeometryEventData eventData) VolumeWithEvents(Volume v)
        {
            return (v, new BaseGeometryEventData(v));
        }

        private (Vertex[] v, Edge[] e, EdgeLoop[] l, Face[] f) TestData(Layer layer)
        {
            Vertex[] v = new Vertex[]
            {
                new Vertex(layer, "", new Point3D(0, 0, 0)),
                new Vertex(layer, "", new Point3D(0, 0, 3)),
                new Vertex(layer, "", new Point3D(3, 0, 3)),
                new Vertex(layer, "", new Point3D(3, 0, 0)),

                new Vertex(layer, "", new Point3D(0, 3, 0)),
                new Vertex(layer, "", new Point3D(0, 3, 3)),
                new Vertex(layer, "", new Point3D(3, 3, 3)),
                new Vertex(layer, "", new Point3D(3, 3, 0)),

				//Hole in 0-1-2-3
				new Vertex(layer, "", new Point3D(1, 0, 1)),
                new Vertex(layer, "", new Point3D(1, 0, 2)),
                new Vertex(layer, "", new Point3D(2, 0, 2)),
                new Vertex(layer, "", new Point3D(2, 0, 1)),
            };

            Edge[] e = new Edge[]
            {
				//Bottom
				new Edge(layer, "", new Vertex[] { v[0], v[1] }),
                new Edge(layer, "", new Vertex[] { v[1], v[2] }),
                new Edge(layer, "", new Vertex[] { v[2], v[3] }),
                new Edge(layer, "", new Vertex[] { v[3], v[0] }),

				//Top
				new Edge(layer, "", new Vertex[] { v[4], v[5] }),
                new Edge(layer, "", new Vertex[] { v[5], v[6] }),
                new Edge(layer, "", new Vertex[] { v[6], v[7] }),
                new Edge(layer, "", new Vertex[] { v[7], v[4] }),

                new Edge(layer, "", new Vertex[] { v[0], v[4] }),
                new Edge(layer, "", new Vertex[] { v[1], v[5] }),
                new Edge(layer, "", new Vertex[] { v[2], v[6] }),
                new Edge(layer, "", new Vertex[] { v[3], v[7] }),

				//Hole in 0-1-2-3
				new Edge(layer, "", new Vertex[] { v[8], v[9] }),
                new Edge(layer, "", new Vertex[] { v[9], v[10] }),
                new Edge(layer, "", new Vertex[] { v[10], v[11] }),
                new Edge(layer, "", new Vertex[] { v[11], v[8] }),
            };

            EdgeLoop[] loops = new EdgeLoop[]
            {
                new EdgeLoop(layer, "", new Edge[] { e[0], e[1], e[2], e[3] }),
                new EdgeLoop(layer, "", new Edge[] { e[4], e[5], e[6], e[7] }),

                new EdgeLoop(layer, "", new Edge[] { e[0], e[8], e[4], e[9] }),
                new EdgeLoop(layer, "", new Edge[] { e[1], e[9], e[5], e[10] }),
                new EdgeLoop(layer, "", new Edge[] { e[2], e[10], e[6], e[11] }),
                new EdgeLoop(layer, "", new Edge[] { e[3], e[11], e[7], e[8] }),

                new EdgeLoop(layer, "", new Edge[]{e[12], e[13], e[14], e[15]})
            };

            Face[] faces = new Face[]
            {
                new Face(layer, "", loops[0], GeometricOrientation.Forward, new EdgeLoop[]{ loops[6] }),
                new Face(layer, "", loops[1]),
                new Face(layer, "", loops[2]),
                new Face(layer, "", loops[3]),
                new Face(layer, "", loops[4]),
                new Face(layer, "", loops[5]),

                new Face(layer, "", loops[6])
            };

            return (v, e, loops, faces);
        }

        [TestMethod]
        public void Ctor()
        {
            var data = GeometryModelHelper.EmptyModel();
            (var v, var e, var l, var f) = TestData(data.layer);

            Assert.ThrowsException<ArgumentNullException>(() => { var v0 = new Volume(null, "", new Face[] { }); });
            Assert.ThrowsException<ArgumentNullException>(() => { var v0 = new Volume(data.layer, null, new Face[] { }); });
            Assert.ThrowsException<ArgumentNullException>(() => { var v0 = new Volume(data.layer, "", null); });
            Assert.ThrowsException<ArgumentException>(() => { var v0 = new Volume(ulong.MaxValue, data.layer, "", new Face[] { f[0], f[1], f[2], f[3], f[4], f[5] }); });

            var v1 = new Volume(data.layer, "", new Face[] { f[0], f[1], f[2], f[3], f[4], f[5] });
            Assert.AreEqual(data.layer, v1.Layer);
            Assert.AreEqual(6, v1.Faces.Count);
            for (int i = 0; i < 6; ++i)
            {
                Assert.AreEqual(f[i], v1.Faces[i].Face);
                Assert.AreEqual(v1, v1.Faces[i].Volume);
            }
            Assert.IsTrue(v1.IsConsistentOriented);

            var v2 = new Volume(999, data.layer, "", new Face[] { f[0], f[1], f[2], f[3], f[4], f[5], f[6] });
            Assert.AreEqual((ulong)999, v2.Id);
            Assert.IsTrue(v2.IsConsistentOriented);
        }

        [TestMethod]
        public void Add()
        {
            var data = GeometryModelHelper.EmptyModelWithEvents();
            (var v, var e, var l, var f) = TestData(data.layer);
            data.eventData.Reset();

            var v0 = VolumeWithEvents(new Volume(data.layer, "", new Face[] { f[0], f[1], f[2], f[3], f[4], f[5] }));

            Assert.AreEqual(0, data.eventData.BatchOperationFinishedCount);
            Assert.AreEqual(1, data.eventData.TopologyChangedEventData.Count);
            Assert.AreEqual(1, data.eventData.AddEventData.Count);
            Assert.AreEqual(1, data.eventData.AddEventData[0].Count());
            Assert.AreEqual(v0.v, data.eventData.AddEventData[0].First());
            Assert.AreEqual(0, data.eventData.RemoveEventData.Count);
            Assert.AreEqual(0, data.eventData.GeometryChangedEventData.Count);

            Assert.AreEqual(0, v0.eventData.GeometryChangedCount);
            Assert.AreEqual(0, v0.eventData.TopologyChangedCount);

            Assert.AreEqual(1, data.layer.Elements.Count(x => x is Volume));
            Assert.IsTrue(data.layer.Elements.Contains(v0.v));

            //Pfaces
            for (int i = 0; i < 6; ++i)
            {
                var pfi = v0.v.Faces.FirstOrDefault(x => x.Volume == v0.v && x.Face == f[i]);
                Assert.IsNotNull(pfi);
                Assert.AreEqual(1, f[i].PFaces.Count);
                Assert.IsTrue(f[i].PFaces.Contains(pfi));
            }

            Assert.IsTrue(v0.v.IsConsistentOriented);
        }

        [TestMethod]
        public void Remove()
        {
            var data = GeometryModelHelper.EmptyModelWithEvents();
            (var v, var e, var l, var f) = TestData(data.layer);
            var v0 = VolumeWithEvents(new Volume(data.layer, "", new Face[] { f[0], f[1], f[2], f[3], f[4], f[5] }));
            data.eventData.Reset();

            var isDeleted = v0.v.RemoveFromModel();

            Assert.AreEqual(true, isDeleted);
            Assert.AreEqual(0, data.eventData.BatchOperationFinishedCount);
            Assert.AreEqual(1, data.eventData.TopologyChangedEventData.Count);
            Assert.AreEqual(0, data.eventData.AddEventData.Count);
            Assert.AreEqual(1, data.eventData.RemoveEventData.Count);
            Assert.AreEqual(1, data.eventData.RemoveEventData[0].Count());
            Assert.AreEqual(v0.v, data.eventData.RemoveEventData[0].First());
            Assert.AreEqual(0, data.eventData.GeometryChangedEventData.Count);

            Assert.AreEqual(0, v0.eventData.GeometryChangedCount);
            Assert.AreEqual(0, v0.eventData.TopologyChangedCount);

            Assert.AreEqual(0, data.layer.Elements.Count(x => x is Volume));

            //Pfaces
            for (int i = 0; i < 6; ++i)
            {
                var pfi = v0.v.Faces.FirstOrDefault(x => x.Volume == v0.v && x.Face == f[i]);
                Assert.IsNotNull(pfi);
                Assert.AreEqual(0, f[i].PFaces.Count);
            }

            data.eventData.Reset();

            //Second remove does nothing
            isDeleted = v0.v.RemoveFromModel();
            Assert.AreEqual(false, isDeleted);
            Assert.AreEqual(0, data.eventData.BatchOperationFinishedCount);
            Assert.AreEqual(0, data.eventData.TopologyChangedEventData.Count);
            Assert.AreEqual(0, data.eventData.AddEventData.Count);
            Assert.AreEqual(0, data.eventData.RemoveEventData.Count);
            Assert.AreEqual(0, data.eventData.GeometryChangedEventData.Count);

            Assert.AreEqual(0, v0.eventData.GeometryChangedCount);
            Assert.AreEqual(0, v0.eventData.TopologyChangedCount);
        }

        [TestMethod]
        public void Readd()
        {
            var data = GeometryModelHelper.EmptyModelWithEvents();
            (var v, var e, var l, var f) = TestData(data.layer);
            var v0 = VolumeWithEvents(new Volume(data.layer, "", new Face[] { f[0], f[1], f[2], f[3], f[4], f[5] }));
            v0.v.RemoveFromModel();
            data.eventData.Reset();

            v0.v.AddToModel();

            Assert.AreEqual(0, data.eventData.BatchOperationFinishedCount);
            Assert.AreEqual(1, data.eventData.TopologyChangedEventData.Count);
            Assert.AreEqual(1, data.eventData.AddEventData.Count);
            Assert.AreEqual(1, data.eventData.AddEventData[0].Count());
            Assert.AreEqual(v0.v, data.eventData.AddEventData[0].First());
            Assert.AreEqual(0, data.eventData.RemoveEventData.Count);
            Assert.AreEqual(0, data.eventData.GeometryChangedEventData.Count);

            Assert.AreEqual(0, v0.eventData.GeometryChangedCount);
            Assert.AreEqual(0, v0.eventData.TopologyChangedCount);

            Assert.AreEqual(1, data.layer.Elements.Count(x => x is Volume));
            Assert.IsTrue(data.layer.Elements.Contains(v0.v));

            //Pfaces
            for (int i = 0; i < 6; ++i)
            {
                var pfi = v0.v.Faces.FirstOrDefault(x => x.Volume == v0.v && x.Face == f[i]);
                Assert.IsNotNull(pfi);
                Assert.AreEqual(1, f[i].PFaces.Count);
                Assert.IsTrue(f[i].PFaces.Contains(pfi));
            }

            //Second add not possible
            Assert.ThrowsException<Exception>(() => v0.v.AddToModel());
        }

        [TestMethod]
        public void BatchAdd()
        {
            var data = GeometryModelHelper.EmptyModelWithEvents();
            (var v, var e, var l, var f) = TestData(data.layer);
            data.eventData.Reset();

            data.model.Geometry.StartBatchOperation();

            var v0 = VolumeWithEvents(new Volume(data.layer, "", new Face[] { f[0], f[1], f[2], f[3], f[4], f[5] }));
            var v1 = VolumeWithEvents(new Volume(data.layer, "", new Face[] { f[0], f[1], f[2], f[3], f[4], f[5], f[6] }));

            Assert.AreEqual(0, data.eventData.TopologyChangedEventData.Count);
            Assert.AreEqual(0, data.eventData.AddEventData.Count);
            Assert.AreEqual(0, data.eventData.RemoveEventData.Count);
            Assert.AreEqual(0, data.eventData.GeometryChangedEventData.Count);
            Assert.AreEqual(0, data.eventData.BatchOperationFinishedCount);

            data.model.Geometry.EndBatchOperation();


            Assert.AreEqual(2, data.model.Geometry.Volumes.Count);
            Assert.IsTrue(data.model.Geometry.Volumes.Contains(v0.v));
            Assert.IsTrue(data.model.Geometry.Volumes.Contains(v1.v));

            for (int i = 0; i < 6; ++i)
                Assert.AreEqual(2, f[i].PFaces.Count);
            //Make sure that hole is not included in volume
            Assert.AreEqual(0, f[6].PFaces.Count);

            Assert.AreEqual(1, data.eventData.BatchOperationFinishedCount);
            Assert.AreEqual(1, data.eventData.TopologyChangedEventData.Count);
            Assert.AreEqual(1, data.eventData.AddEventData.Count);
            Assert.AreEqual(2, data.eventData.AddEventData[0].Count());
            Assert.IsTrue(data.eventData.AddEventData[0].Contains(v0.v));
            Assert.IsTrue(data.eventData.AddEventData[0].Contains(v1.v));
            Assert.AreEqual(0, data.eventData.RemoveEventData.Count);
            Assert.AreEqual(0, data.eventData.GeometryChangedEventData.Count);
            Assert.AreEqual(0, v0.eventData.GeometryChangedCount);
            Assert.AreEqual(0, v1.eventData.GeometryChangedCount);
            Assert.AreEqual(2, data.layer.Elements.Count(x => x is Volume));
        }

        [TestMethod]
        public void BatchRemove()
        {
            var data = GeometryModelHelper.EmptyModelWithEvents();
            (var v, var e, var l, var f) = TestData(data.layer);

            var v0 = VolumeWithEvents(new Volume(data.layer, "", new Face[] { f[0], f[1], f[2], f[3], f[4], f[5] }));
            var v1 = VolumeWithEvents(new Volume(data.layer, "", new Face[] { f[0], f[1], f[2], f[3], f[4], f[5], f[6] }));

            data.eventData.Reset();
            v0.eventData.Reset();
            v1.eventData.Reset();

            data.model.Geometry.StartBatchOperation();

            v0.v.RemoveFromModel();
            v1.v.RemoveFromModel();

            //No events should be issued during batch operation
            Assert.AreEqual(0, data.eventData.BatchOperationFinishedCount);
            Assert.AreEqual(0, data.eventData.TopologyChangedEventData.Count);
            Assert.AreEqual(0, data.eventData.AddEventData.Count);
            Assert.AreEqual(0, data.eventData.RemoveEventData.Count);
            Assert.AreEqual(0, data.eventData.GeometryChangedEventData.Count);

            data.model.Geometry.EndBatchOperation();


            Assert.AreEqual(0, data.model.Geometry.Volumes.Count);
            Assert.AreEqual(0, data.layer.Elements.Count(x => x is Volume));

            for (int i = 0; i < 7; ++i)
                Assert.AreEqual(0, f[i].PFaces.Count);

            Assert.AreEqual(1, data.eventData.BatchOperationFinishedCount);
            Assert.AreEqual(1, data.eventData.TopologyChangedEventData.Count);
            Assert.AreEqual(0, data.eventData.AddEventData.Count);
            Assert.AreEqual(1, data.eventData.RemoveEventData.Count);
            Assert.AreEqual(2, data.eventData.RemoveEventData[0].Count());
            Assert.IsTrue(data.eventData.RemoveEventData[0].Contains(v0.v));
            Assert.IsTrue(data.eventData.RemoveEventData[0].Contains(v1.v));
            Assert.AreEqual(0, data.eventData.GeometryChangedEventData.Count);
            Assert.AreEqual(0, v0.eventData.GeometryChangedCount);
            Assert.AreEqual(0, v0.eventData.TopologyChangedCount);
            Assert.AreEqual(0, v1.eventData.GeometryChangedCount);
            Assert.AreEqual(0, v1.eventData.TopologyChangedCount);
        }

        [TestMethod]
        public void GeometryChanged()
        {
            var data = GeometryModelHelper.EmptyModelWithEvents();
            (var v, var e, var l, var f) = TestData(data.layer);

            var v0 = VolumeWithEvents(new Volume(data.layer, "", new Face[] { f[0], f[1], f[2], f[3], f[4], f[5] }));
            var v1 = VolumeWithEvents(new Volume(data.layer, "", new Face[] { f[0], f[1], f[2], f[3], f[4], f[5], f[6] }));

            data.eventData.Reset();
            v0.eventData.Reset();
            v1.eventData.Reset();


            v[0].Position = new Point3D(-2, -4, -6);
            Assert.AreEqual(28, data.eventData.GeometryChangedEventData.Count());
            for (int i = 0; i < data.eventData.GeometryChangedEventData.Count(); ++i)
                Assert.AreEqual(1, data.eventData.GeometryChangedEventData[i].Count());
            Assert.AreEqual(0, data.eventData.AddEventData.Count());
            Assert.AreEqual(0, data.eventData.RemoveEventData.Count());
            Assert.AreEqual(0, data.eventData.BatchOperationFinishedCount);
            Assert.AreEqual(0, data.eventData.TopologyChangedEventData.Count);
            Assert.AreEqual(6, v0.eventData.GeometryChangedCount);
            Assert.AreEqual(6, v1.eventData.GeometryChangedCount);


        }

        [TestMethod]
        public void GeometryChangedHole()
        {
            var data = GeometryModelHelper.EmptyModelWithEvents();
            (var v, var e, var l, var f) = TestData(data.layer);

            var v0 = VolumeWithEvents(new Volume(data.layer, "", new Face[] { f[0], f[1], f[2], f[3], f[4], f[5] }));
            var v1 = VolumeWithEvents(new Volume(data.layer, "", new Face[] { f[0], f[1], f[2], f[3], f[4], f[5], f[6] }));

            data.eventData.Reset();
            v0.eventData.Reset();
            v1.eventData.Reset();


            v[8].Position = new Point3D(-2, -4, -6);

            Assert.AreEqual(13, data.eventData.GeometryChangedEventData.Count());
            for (int i = 0; i < data.eventData.GeometryChangedEventData.Count(); ++i)
                Assert.AreEqual(1, data.eventData.GeometryChangedEventData[i].Count());
            Assert.AreEqual(0, data.eventData.AddEventData.Count());
            Assert.AreEqual(0, data.eventData.RemoveEventData.Count());
            Assert.AreEqual(0, data.eventData.BatchOperationFinishedCount);
            Assert.AreEqual(0, data.eventData.TopologyChangedEventData.Count);
            Assert.AreEqual(2, v0.eventData.GeometryChangedCount);
            Assert.AreEqual(2, v1.eventData.GeometryChangedCount);
        }

        [TestMethod]
        public void BatchGeomeryChanged()
        {
            var data = GeometryModelHelper.EmptyModelWithEvents();
            (var v, var e, var l, var f) = TestData(data.layer);

            var v0 = VolumeWithEvents(new Volume(data.layer, "", new Face[] { f[0], f[1], f[2], f[3], f[4], f[5] }));
            var v1 = VolumeWithEvents(new Volume(data.layer, "", new Face[] { f[0], f[1], f[2], f[3], f[4], f[5], f[6] }));

            data.eventData.Reset();
            v0.eventData.Reset();
            v1.eventData.Reset();

            data.model.Geometry.StartBatchOperation();

            v[0].Position = new Point3D(-2, -4, -6);
            v[1].Position = new Point3D(-3, -6, -9);

            Assert.AreEqual(0, data.eventData.GeometryChangedEventData.Count());
            Assert.AreEqual(0, data.eventData.AddEventData.Count());
            Assert.AreEqual(0, data.eventData.RemoveEventData.Count());
            Assert.AreEqual(0, data.eventData.BatchOperationFinishedCount);
            Assert.AreEqual(0, data.eventData.TopologyChangedEventData.Count);
            Assert.AreEqual(0, v0.eventData.GeometryChangedCount);
            Assert.AreEqual(0, v1.eventData.GeometryChangedCount);

            data.model.Geometry.EndBatchOperation();

            Assert.AreEqual(1, data.eventData.GeometryChangedEventData.Count());
            Assert.AreEqual(17, data.eventData.GeometryChangedEventData[0].Count());
            Assert.IsTrue(data.eventData.GeometryChangedEventData[0].Contains(v0.v));
            Assert.IsTrue(data.eventData.GeometryChangedEventData[0].Contains(v1.v));
            Assert.AreEqual(0, data.eventData.AddEventData.Count());
            Assert.AreEqual(0, data.eventData.RemoveEventData.Count());
            Assert.AreEqual(1, data.eventData.BatchOperationFinishedCount);
            Assert.AreEqual(0, data.eventData.TopologyChangedEventData.Count);
            Assert.AreEqual(1, v0.eventData.GeometryChangedCount);
            Assert.AreEqual(1, v1.eventData.GeometryChangedCount);
        }

        [TestMethod]
        public void BatchGeomeryChangedHole()
        {
            var data = GeometryModelHelper.EmptyModelWithEvents();
            (var v, var e, var l, var f) = TestData(data.layer);

            var v0 = VolumeWithEvents(new Volume(data.layer, "", new Face[] { f[0], f[1], f[2], f[3], f[4], f[5] }));
            var v1 = VolumeWithEvents(new Volume(data.layer, "", new Face[] { f[0], f[1], f[2], f[3], f[4], f[5], f[6] }));

            data.eventData.Reset();
            v0.eventData.Reset();
            v1.eventData.Reset();

            data.model.Geometry.StartBatchOperation();

            v[8].Position = new Point3D(-2, -4, -6);

            data.model.Geometry.EndBatchOperation();

            Assert.AreEqual(1, data.eventData.GeometryChangedEventData.Count());
            Assert.AreEqual(8, data.eventData.GeometryChangedEventData[0].Count());
            Assert.IsTrue(data.eventData.GeometryChangedEventData[0].Contains(v0.v));
            Assert.IsTrue(data.eventData.GeometryChangedEventData[0].Contains(v1.v));
            Assert.AreEqual(0, data.eventData.AddEventData.Count());
            Assert.AreEqual(0, data.eventData.RemoveEventData.Count());
            Assert.AreEqual(1, data.eventData.BatchOperationFinishedCount);
            Assert.AreEqual(0, data.eventData.TopologyChangedEventData.Count);
            Assert.AreEqual(1, v0.eventData.GeometryChangedCount);
            Assert.AreEqual(1, v1.eventData.GeometryChangedCount);
        }

        [TestMethod]
        public void ExchangeVertex()
        {
            var data = GeometryModelHelper.EmptyModelWithEvents();
            (var v, var e, var l, var f) = TestData(data.layer);

            var v0 = VolumeWithEvents(new Volume(data.layer, "", new Face[] { f[0], f[1], f[2], f[3], f[4], f[5] }));

            var replaceVertex = new Vertex(data.layer, "", v[0].Position);

            data.eventData.Reset();
            v0.eventData.Reset();

            data.model.Geometry.StartBatchOperation();

            e[0].Vertices[0] = replaceVertex;
            e[3].Vertices[1] = replaceVertex;
            e[8].Vertices[0] = replaceVertex;

            Assert.AreEqual(0, data.eventData.GeometryChangedEventData.Count());
            Assert.AreEqual(0, data.eventData.AddEventData.Count());
            Assert.AreEqual(0, data.eventData.RemoveEventData.Count());
            Assert.AreEqual(0, data.eventData.BatchOperationFinishedCount);
            Assert.AreEqual(0, data.eventData.TopologyChangedEventData.Count);
            Assert.AreEqual(0, v0.eventData.GeometryChangedCount);
            Assert.AreEqual(0, v0.eventData.TopologyChangedCount);

            data.model.Geometry.EndBatchOperation();

            Assert.AreEqual(0, data.eventData.GeometryChangedEventData.Count());
            Assert.AreEqual(0, data.eventData.AddEventData.Count());
            Assert.AreEqual(0, data.eventData.RemoveEventData.Count());
            Assert.AreEqual(1, data.eventData.BatchOperationFinishedCount);
            Assert.AreEqual(1, data.eventData.TopologyChangedEventData.Count);
            Assert.AreEqual(0, v0.eventData.GeometryChangedCount);
            Assert.AreEqual(1, v0.eventData.TopologyChangedCount);

            data.eventData.Reset();
            v0.eventData.Reset();

            v[0].Position = new Point3D(-3, -6, -9);
            Assert.AreEqual(0, v0.eventData.GeometryChangedCount);
        }

        [TestMethod]
        public void EdgeExchange()
        {
            var data = GeometryModelHelper.EmptyModelWithEvents();
            (var v, var e, var l, var f) = TestData(data.layer);

            var v0 = VolumeWithEvents(new Volume(data.layer, "", new Face[] { f[0], f[1], f[2], f[3], f[4], f[5] }));
            var replaceEdge = new Edge(data.layer, "", e[0].Vertices);

            data.eventData.Reset();
            v0.eventData.Reset();

            data.model.Geometry.StartBatchOperation();

            l[0].Edges[0] = new PEdge(replaceEdge, GeometricOrientation.Undefined, l[0]);
            l[2].Edges[0] = new PEdge(replaceEdge, GeometricOrientation.Undefined, l[2]);

            Assert.AreEqual(0, data.eventData.GeometryChangedEventData.Count());
            Assert.AreEqual(0, data.eventData.AddEventData.Count());
            Assert.AreEqual(0, data.eventData.RemoveEventData.Count());
            Assert.AreEqual(0, data.eventData.BatchOperationFinishedCount);
            Assert.AreEqual(0, data.eventData.TopologyChangedEventData.Count);
            Assert.AreEqual(0, v0.eventData.GeometryChangedCount);
            Assert.AreEqual(0, v0.eventData.TopologyChangedCount);

            data.model.Geometry.EndBatchOperation();

            Assert.AreEqual(0, data.eventData.GeometryChangedEventData.Count());
            Assert.AreEqual(0, data.eventData.AddEventData.Count());
            Assert.AreEqual(0, data.eventData.RemoveEventData.Count());
            Assert.AreEqual(1, data.eventData.BatchOperationFinishedCount);
            Assert.AreEqual(1, data.eventData.TopologyChangedEventData.Count);
            Assert.AreEqual(0, v0.eventData.GeometryChangedCount);
            Assert.AreEqual(1, v0.eventData.TopologyChangedCount);
        }

        [TestMethod]
        public void FaceExchange()
        {
            var data = GeometryModelHelper.EmptyModelWithEvents();
            (var v, var e, var l, var f) = TestData(data.layer);

            var v0 = VolumeWithEvents(new Volume(data.layer, "", new Face[] { f[0], f[1], f[2], f[3], f[4], f[5] }));
            var replaceFace = new Face(data.layer, "", f[0].Boundary);

            data.eventData.Reset();
            v0.eventData.Reset();

            v0.v.Faces[0] = new PFace(replaceFace, v0.v, GeometricOrientation.Undefined);

            Assert.AreEqual(0, f[0].PFaces.Count);
            Assert.AreEqual(1, replaceFace.PFaces.Count);

            Assert.AreEqual(0, data.eventData.GeometryChangedEventData.Count());
            Assert.AreEqual(0, data.eventData.AddEventData.Count());
            Assert.AreEqual(0, data.eventData.RemoveEventData.Count());
            Assert.AreEqual(0, data.eventData.BatchOperationFinishedCount);
            Assert.AreEqual(1, data.eventData.TopologyChangedEventData.Count);
            Assert.AreEqual(0, v0.eventData.GeometryChangedCount);
            Assert.AreEqual(1, v0.eventData.TopologyChangedCount);
        }

        [TestMethod]
        public void BatchFaceExchange()
        {
            var data = GeometryModelHelper.EmptyModelWithEvents();
            (var v, var e, var l, var f) = TestData(data.layer);

            var v0 = VolumeWithEvents(new Volume(data.layer, "", new Face[] { f[0], f[1], f[2], f[3], f[4], f[5] }));
            var replaceFace = new Face(data.layer, "", f[0].Boundary);

            data.eventData.Reset();
            v0.eventData.Reset();

            data.model.Geometry.StartBatchOperation();

            v0.v.Faces[0] = new PFace(replaceFace, v0.v, GeometricOrientation.Undefined);

            Assert.AreEqual(0, data.eventData.GeometryChangedEventData.Count());
            Assert.AreEqual(0, data.eventData.AddEventData.Count());
            Assert.AreEqual(0, data.eventData.RemoveEventData.Count());
            Assert.AreEqual(0, data.eventData.BatchOperationFinishedCount);
            Assert.AreEqual(0, data.eventData.TopologyChangedEventData.Count);
            Assert.AreEqual(0, v0.eventData.GeometryChangedCount);
            Assert.AreEqual(0, v0.eventData.TopologyChangedCount);

            data.model.Geometry.EndBatchOperation();

            Assert.AreEqual(0, f[0].PFaces.Count);
            Assert.AreEqual(1, replaceFace.PFaces.Count);

            Assert.AreEqual(0, data.eventData.GeometryChangedEventData.Count());
            Assert.AreEqual(0, data.eventData.AddEventData.Count());
            Assert.AreEqual(0, data.eventData.RemoveEventData.Count());
            Assert.AreEqual(1, data.eventData.BatchOperationFinishedCount);
            Assert.AreEqual(1, data.eventData.TopologyChangedEventData.Count);
            Assert.AreEqual(0, v0.eventData.GeometryChangedCount);
            Assert.AreEqual(1, v0.eventData.TopologyChangedCount);
        }

        [TestMethod]
        public void AddFace()

        {
            var data = GeometryModelHelper.EmptyModelWithEvents();
            (var v, var e, var l, var f) = TestData(data.layer);
            var v0 = VolumeWithEvents(new Volume(data.layer, "", new Face[] { f[0], f[1], f[2], f[3] }));
            Assert.IsFalse(v0.v.IsConsistentOriented);

            foreach (var pface in v0.v.Faces)
            {
                Assert.AreEqual(GeometricOrientation.Undefined, pface.Orientation);
            }

            data.eventData.Reset();
            v0.eventData.Reset();

            v0.v.AddFace(f[4]);
            v0.v.AddFace(f[5]);

            Assert.IsTrue(v0.v.IsConsistentOriented);

            foreach (var pface in v0.v.Faces)
            {
                Assert.AreNotEqual(GeometricOrientation.Undefined, pface.Orientation);
            }

            for (int i = 4; i <= 5; ++i)
            {
                Assert.AreEqual(1, f[i].PFaces.Count);
                Assert.AreEqual(v0.v, f[i].PFaces[0].Volume);
                Assert.AreEqual(f[i], f[i].PFaces[0].Face);
            }

            Assert.AreEqual(0, data.eventData.GeometryChangedEventData.Count());
            Assert.AreEqual(0, data.eventData.AddEventData.Count());
            Assert.AreEqual(0, data.eventData.RemoveEventData.Count());
            Assert.AreEqual(0, data.eventData.BatchOperationFinishedCount);
            Assert.AreEqual(2, data.eventData.TopologyChangedEventData.Count);

            Assert.AreEqual(0, v0.eventData.GeometryChangedCount);
            Assert.AreEqual(2, v0.eventData.TopologyChangedCount);

            //Adding a face for the second time should do nothing
            v0.v.AddFace(f[4]);
            Assert.AreEqual(6, v0.v.Faces.Count);
            Assert.AreEqual(0, data.eventData.GeometryChangedEventData.Count());
            Assert.AreEqual(0, data.eventData.AddEventData.Count());
            Assert.AreEqual(0, data.eventData.RemoveEventData.Count());
            Assert.AreEqual(0, data.eventData.BatchOperationFinishedCount);
            Assert.AreEqual(2, data.eventData.TopologyChangedEventData.Count);
            Assert.AreEqual(0, v0.eventData.GeometryChangedCount);
            Assert.AreEqual(2, v0.eventData.TopologyChangedCount);
        }

        [TestMethod]
        public void BatchAddFace()
        {
            var data = GeometryModelHelper.EmptyModelWithEvents();
            (var v, var e, var l, var f) = TestData(data.layer);

            var v0 = VolumeWithEvents(new Volume(data.layer, "", new Face[] { f[0], f[1], f[2], f[3] }));
            Assert.IsFalse(v0.v.IsConsistentOriented);

            foreach (var pface in v0.v.Faces)
            {
                Assert.AreEqual(GeometricOrientation.Undefined, pface.Orientation);
            }

            data.eventData.Reset();
            v0.eventData.Reset();

            data.model.Geometry.StartBatchOperation();

            v0.v.AddFace(f[4]);
            v0.v.AddFace(f[5]);

            Assert.AreEqual(0, data.eventData.GeometryChangedEventData.Count());
            Assert.AreEqual(0, data.eventData.AddEventData.Count());
            Assert.AreEqual(0, data.eventData.RemoveEventData.Count());
            Assert.AreEqual(0, data.eventData.BatchOperationFinishedCount);
            Assert.AreEqual(0, data.eventData.TopologyChangedEventData.Count);
            Assert.AreEqual(0, v0.eventData.GeometryChangedCount);
            Assert.AreEqual(0, v0.eventData.TopologyChangedCount);

            data.model.Geometry.EndBatchOperation();

            Assert.IsTrue(v0.v.IsConsistentOriented);

            foreach (var pface in v0.v.Faces)
            {
                Assert.AreNotEqual(GeometricOrientation.Undefined, pface.Orientation);
            }

            for (int i = 4; i <= 5; ++i)
            {
                Assert.AreEqual(1, f[i].PFaces.Count);
                Assert.AreEqual(v0.v, f[i].PFaces[0].Volume);
                Assert.AreEqual(f[i], f[i].PFaces[0].Face);
            }

            Assert.AreEqual(0, data.eventData.GeometryChangedEventData.Count());
            Assert.AreEqual(0, data.eventData.AddEventData.Count());
            Assert.AreEqual(0, data.eventData.RemoveEventData.Count());
            Assert.AreEqual(1, data.eventData.BatchOperationFinishedCount);
            Assert.AreEqual(1, data.eventData.TopologyChangedEventData.Count);
            Assert.AreEqual(0, v0.eventData.GeometryChangedCount);
            Assert.AreEqual(1, v0.eventData.TopologyChangedCount);
        }

        [TestMethod]
        public void AddFaceCollection()
        {
            var data = GeometryModelHelper.EmptyModelWithEvents();
            (var v, var e, var l, var f) = TestData(data.layer);

            var v0 = VolumeWithEvents(new Volume(data.layer, "", new Face[] { f[0], f[1], f[2], f[3] }));
            Assert.IsFalse(v0.v.IsConsistentOriented);

            data.eventData.Reset();
            v0.eventData.Reset();

            v0.v.Faces.Add(new PFace(f[4], v0.v, GeometricOrientation.Undefined));
            v0.v.Faces.Add(new PFace(f[5], v0.v, GeometricOrientation.Undefined));

            Assert.IsTrue(v0.v.IsConsistentOriented);
            for (int i = 4; i <= 5; ++i)
            {
                Assert.AreEqual(1, f[i].PFaces.Count);
                Assert.AreEqual(v0.v, f[i].PFaces[0].Volume);
                Assert.AreEqual(f[i], f[i].PFaces[0].Face);
            }

            Assert.AreEqual(0, data.eventData.GeometryChangedEventData.Count());
            Assert.AreEqual(0, data.eventData.AddEventData.Count());
            Assert.AreEqual(0, data.eventData.RemoveEventData.Count());
            Assert.AreEqual(0, data.eventData.BatchOperationFinishedCount);
            Assert.AreEqual(2, data.eventData.TopologyChangedEventData.Count);
            Assert.AreEqual(0, v0.eventData.GeometryChangedCount);
            Assert.AreEqual(2, v0.eventData.TopologyChangedCount);
        }

        [TestMethod]
        public void BatchAddFaceCollection()
        {
            var data = GeometryModelHelper.EmptyModelWithEvents();
            (var v, var e, var l, var f) = TestData(data.layer);

            var v0 = VolumeWithEvents(new Volume(data.layer, "", new Face[] { f[0], f[1], f[2], f[3] }));
            Assert.IsFalse(v0.v.IsConsistentOriented);

            data.eventData.Reset();
            v0.eventData.Reset();

            data.model.Geometry.StartBatchOperation();

            v0.v.Faces.Add(new PFace(f[4], v0.v, GeometricOrientation.Undefined));
            v0.v.Faces.Add(new PFace(f[5], v0.v, GeometricOrientation.Undefined));

            Assert.AreEqual(0, data.eventData.GeometryChangedEventData.Count());
            Assert.AreEqual(0, data.eventData.AddEventData.Count());
            Assert.AreEqual(0, data.eventData.RemoveEventData.Count());
            Assert.AreEqual(0, data.eventData.BatchOperationFinishedCount);
            Assert.AreEqual(0, data.eventData.TopologyChangedEventData.Count);
            Assert.AreEqual(0, v0.eventData.GeometryChangedCount);
            Assert.AreEqual(0, v0.eventData.TopologyChangedCount);

            data.model.Geometry.EndBatchOperation();

            Assert.IsTrue(v0.v.IsConsistentOriented);
            for (int i = 4; i <= 5; ++i)
            {
                Assert.AreEqual(1, f[i].PFaces.Count);
                Assert.AreEqual(v0.v, f[i].PFaces[0].Volume);
                Assert.AreEqual(f[i], f[i].PFaces[0].Face);
            }

            Assert.AreEqual(0, data.eventData.GeometryChangedEventData.Count());
            Assert.AreEqual(0, data.eventData.AddEventData.Count());
            Assert.AreEqual(0, data.eventData.RemoveEventData.Count());
            Assert.AreEqual(1, data.eventData.BatchOperationFinishedCount);
            Assert.AreEqual(1, data.eventData.TopologyChangedEventData.Count);
            Assert.AreEqual(0, v0.eventData.GeometryChangedCount);
            Assert.AreEqual(1, v0.eventData.TopologyChangedCount);
        }

        [TestMethod]
        public void RemoveFace()
        {
            var data = GeometryModelHelper.EmptyModelWithEvents();
            (var v, var e, var l, var f) = TestData(data.layer);

            var v0 = VolumeWithEvents(new Volume(data.layer, "", new Face[] { f[0], f[1], f[2], f[3], f[4], f[5] }));
            Assert.IsTrue(v0.v.IsConsistentOriented);

            data.eventData.Reset();
            v0.eventData.Reset();

            v0.v.Faces.RemoveAt(5);
            v0.v.Faces.RemoveAt(4);

            Assert.IsFalse(v0.v.IsConsistentOriented);
            Assert.AreEqual(0, f[4].PFaces.Count);
            Assert.AreEqual(0, f[5].PFaces.Count);

            Assert.AreEqual(0, data.eventData.GeometryChangedEventData.Count());
            Assert.AreEqual(0, data.eventData.AddEventData.Count());
            Assert.AreEqual(0, data.eventData.RemoveEventData.Count());
            Assert.AreEqual(0, data.eventData.BatchOperationFinishedCount);
            Assert.AreEqual(2, data.eventData.TopologyChangedEventData.Count);
            Assert.AreEqual(0, v0.eventData.GeometryChangedCount);
            Assert.AreEqual(2, v0.eventData.TopologyChangedCount);
        }

        [TestMethod]
        public void BatchRemoveFace()
        {
            var data = GeometryModelHelper.EmptyModelWithEvents();
            (var v, var e, var l, var f) = TestData(data.layer);

            var v0 = VolumeWithEvents(new Volume(data.layer, "", new Face[] { f[0], f[1], f[2], f[3], f[4], f[5] }));
            Assert.IsTrue(v0.v.IsConsistentOriented);

            data.eventData.Reset();
            v0.eventData.Reset();

            data.model.Geometry.StartBatchOperation();

            v0.v.Faces.RemoveAt(5);
            v0.v.Faces.RemoveAt(4);

            Assert.AreEqual(0, data.eventData.GeometryChangedEventData.Count());
            Assert.AreEqual(0, data.eventData.AddEventData.Count());
            Assert.AreEqual(0, data.eventData.RemoveEventData.Count());
            Assert.AreEqual(0, data.eventData.BatchOperationFinishedCount);
            Assert.AreEqual(0, data.eventData.TopologyChangedEventData.Count);
            Assert.AreEqual(0, v0.eventData.GeometryChangedCount);
            Assert.AreEqual(0, v0.eventData.TopologyChangedCount);

            data.model.Geometry.EndBatchOperation();

            Assert.IsFalse(v0.v.IsConsistentOriented);
            Assert.AreEqual(0, f[4].PFaces.Count);
            Assert.AreEqual(0, f[5].PFaces.Count);

            Assert.AreEqual(0, data.eventData.GeometryChangedEventData.Count());
            Assert.AreEqual(0, data.eventData.AddEventData.Count());
            Assert.AreEqual(0, data.eventData.RemoveEventData.Count());
            Assert.AreEqual(1, data.eventData.BatchOperationFinishedCount);
            Assert.AreEqual(1, data.eventData.TopologyChangedEventData.Count);
            Assert.AreEqual(0, v0.eventData.GeometryChangedCount);
            Assert.AreEqual(1, v0.eventData.TopologyChangedCount);
        }

        [TestMethod]
        public void Visibility()
        {
            //Data
            var data = GeometryModelHelper.EmptyModelWithEvents();
            (var v, var e, var l, var f) = TestData(data.layer);

            var v0 = VolumeWithEvents(new Volume(data.layer, "", new Face[] { f[0], f[1], f[2], f[3], f[4], f[5] }));
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
            //Data
            var data = GeometryModelHelper.EmptyModelWithEvents();
            (var v, var e, var l, var f) = TestData(data.layer);

            var v0 = VolumeWithEvents(new Volume(data.layer, "", new Face[] { f[0], f[1], f[2], f[3], f[4], f[5] }));
            data.eventData.Reset();

            Assert.AreEqual(0, v0.eventData.PropertyChangedData.Count);

            v0.v.Name = "Renamed";
            Assert.AreEqual("Renamed", v0.v.Name);
            Assert.AreEqual(1, v0.eventData.PropertyChangedData.Count);
            Assert.AreEqual(nameof(Vertex.Name), v0.eventData.PropertyChangedData[0]);
        }

        [TestMethod]
        public void MoveToLayer()
        {
            var data = GeometryModelHelper.EmptyModelWithEvents();
            Layer targetLayer = new Layer(data.model.Geometry, "TargetLayer") { Color = new DerivedColor(Colors.Pink) };

            (var v, var e, var l, var f) = TestData(data.layer);
            var v0 = VolumeWithEvents(new Volume(data.layer, "", new Face[] { f[0], f[1], f[2], f[3], f[4], f[5] }));
            data.eventData.Reset();

            Assert.AreEqual(data.layer, v0.v.Layer);
            Assert.AreEqual(1, data.layer.Elements.Count(x => x is Volume));
            Assert.AreEqual(Colors.Red, v0.v.Color.Color);

            v0.v.Layer = targetLayer;
            Assert.AreEqual(targetLayer, v0.v.Layer);
            Assert.AreEqual(0, data.layer.Elements.Count(x => x is Volume));
            Assert.AreEqual(1, targetLayer.Elements.Count(x => x is Volume));
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
