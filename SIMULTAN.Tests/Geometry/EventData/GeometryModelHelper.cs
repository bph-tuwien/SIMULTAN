using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data.Geometry;
using SIMULTAN.Tests.TestUtils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace SIMULTAN.Tests.Geometry.EventData
{
    public static class GeometryModelHelper
    {
        public static (GeometryModel model, Layer layer) EmptyModel()
        {
            (var gm, var layer) = EmptyModelData();

            GeometryModel model = new GeometryModel("TestModel", new DummyResourceFileEntry(@".\testing_dummy.geosim", 1), OperationPermission.All, gm);

            return (model, layer);
        }

        public static (GeometryModelData modelData, Layer layer) EmptyModelData()
        {
            GeometryModelData gm = new GeometryModelData();
            Layer layer = new Layer(gm, "Layer0") { Color = new DerivedColor(Colors.Red) };
            gm.Layers.Add(layer);

            return (gm, layer);
        }

        public static (GeometryModel model, Layer layer, GeometryModelEventData eventData) EmptyModelWithEvents()
        {
            var empty = EmptyModel();
            return (empty.model, empty.layer, new GeometryModelEventData(empty.model.Geometry));
        }

        public static GeometryModelData CubeModel()
        {
            GeometryModelData gm = new GeometryModelData();
            Layer layer = new Layer(gm, "Layer0") { Color = new DerivedColor(Colors.Red) };
            gm.Layers.Add(layer);

            ShapeGenerator.GenerateCube(layer, new Point3D(0, 0, 0), new Point3D(2, 2, 2));

            return gm;
        }

        public static void Compare(GeometryModelData expected, GeometryModelData actual)
        {
            Assert.AreEqual(expected.IsVisible, actual.IsVisible);

            for (int i = 0; i < expected.Layers.Count; ++i)
                Compare(expected.Layers[i], actual.Layers[i]);


            for (int i = 0; i < expected.Vertices.Count; ++i)
                Compare(expected.Vertices[i], actual.Vertices[i]);

            for (int i = 0; i < expected.Edges.Count; ++i)
                Compare(expected.Edges[i], actual.Edges[i]);

            for (int i = 0; i < expected.EdgeLoops.Count; ++i)
                Compare(expected.EdgeLoops[i], actual.EdgeLoops[i]);

            for (int i = 0; i < expected.Polylines.Count; ++i)
                Compare(expected.Polylines[i], actual.Polylines[i]);

            for (int i = 0; i < expected.Faces.Count; ++i)
                Compare(expected.Faces[i], actual.Faces[i]);

            for (int i = 0; i < expected.Volumes.Count; ++i)
                Compare(expected.Volumes[i], actual.Volumes[i]);

            for (int i = 0; i < expected.ProxyGeometries.Count; ++i)
                Compare(expected.ProxyGeometries[i], actual.ProxyGeometries[i]);
        }

        public static void Compare(Vertex expected, Vertex actual)
        {
            CompareBaseGeometry(expected, actual);
            Assert.AreEqual(expected.Position, actual.Position);
            Assert.AreEqual(expected.Edges.Count, actual.Edges.Count);

            for (int i = 0; i < expected.Edges.Count; ++i)
            {
                Assert.AreEqual(expected.Edges[i].Id, actual.Edges[i].Id);
            }
        }

        public static void Compare(Edge expected, Edge actual)
        {
            CompareBaseGeometry(expected, actual);
            for (int i = 0; i < expected.Vertices.Count; ++i)
                Assert.AreEqual(expected.Vertices[i].Id, actual.Vertices[i].Id);

            for (int i = 0; i < expected.PEdges.Count; ++i)
                Compare(expected.PEdges[i], actual.PEdges[i]);
        }

        public static void Compare(PEdge expected, PEdge actual)
        {
            Assert.AreEqual(expected.Edge.Id, actual.Edge.Id);
            Assert.AreEqual(expected.Orientation, actual.Orientation);
            Assert.AreEqual(expected.Parent.Id, actual.Parent.Id);
        }

        public static void Compare(EdgeLoop expected, EdgeLoop actual)
        {
            CompareBaseGeometry(expected, actual);

            for (int i = 0; i < expected.Edges.Count; ++i)
                Compare(expected.Edges[i], actual.Edges[i]);

            for (int i = 0; i < expected.Faces.Count; ++i)
                Assert.AreEqual(expected.Faces[i].Id, actual.Faces[i].Id);
        }

        public static void Compare(Polyline expected, Polyline actual)
        {
            CompareBaseGeometry(expected, actual);

            for (int i = 0; i < expected.Edges.Count; ++i)
                Compare(expected.Edges[i], actual.Edges[i]);
        }

        public static void Compare(Face expected, Face actual)
        {
            CompareBaseGeometry(expected, actual);

            Assert.AreEqual(expected.Boundary.Id, actual.Boundary.Id);
            for (int i = 0; i < expected.Holes.Count; ++i)
                Assert.AreEqual(expected.Holes[i].Id, actual.Holes[i].Id);

            Assert.AreEqual(expected.Orientation, actual.Orientation);
            Assert.AreEqual(expected.Normal, actual.Normal);

            for (int i = 0; i < expected.PFaces.Count; ++i)
                Compare(expected.PFaces[i], actual.PFaces[i]);
        }

        public static void Compare(PFace expected, PFace actual)
        {
            Assert.AreEqual(expected.Face.Id, actual.Face.Id);
            Assert.AreEqual(expected.Volume.Id, actual.Volume.Id);
            Assert.AreEqual(expected.Orientation, actual.Orientation);
        }

        public static void Compare(Volume expected, Volume actual)
        {
            CompareBaseGeometry(expected, actual);

            for (int i = 0; i < expected.Faces.Count; ++i)
                Compare(expected.Faces[i], actual.Faces[i]);

            Assert.AreEqual(expected.IsConsistentOriented, actual.IsConsistentOriented);
        }

        public static void Compare(ProxyGeometry expected, ProxyGeometry actual)
        {
            CompareBaseGeometry(expected, actual);

            Assert.AreEqual(expected.Vertex.Id, actual.Vertex.Id);

            for (int i = 0; i < expected.Positions.Count; ++i)
                Assert.AreEqual(expected.Positions[i], actual.Positions[i]);
            for (int i = 0; i < expected.Normals.Count; ++i)
                Assert.AreEqual(expected.Normals[i], actual.Normals[i]);
            for (int i = 0; i < expected.Indices.Count; ++i)
                Assert.AreEqual(expected.Indices[i], actual.Indices[i]);

            Assert.AreEqual(expected.Size, actual.Size);
        }

        public static void Compare(Layer expected, Layer actual)
        {
            Assert.AreEqual(expected.Id, actual.Id);
            Assert.AreEqual(expected.IsVisible, actual.IsVisible);
            Assert.AreEqual(expected.Name, actual.Name);

            CompareColor(expected.Color, actual.Color);

            if (expected.Parent == null)
                Assert.IsNull(actual.Parent);
            else
                Assert.AreEqual(expected.Id, actual.Id);

            //Layers are the only place where order might change during clone
            Assert.AreEqual(expected.Elements.Count, actual.Elements.Count);
            for (int i = 0; i < expected.Elements.Count; ++i)
                Assert.AreEqual(1, actual.Elements.Count(x => x.Id == expected.Elements[i].Id));

            for (int i = 0; i < expected.Layers.Count; ++i)
                Compare(expected.Layers[i], actual.Layers[i]);
        }

        private static void CompareBaseGeometry(BaseGeometry expected, BaseGeometry actual)
        {
            Assert.AreEqual(expected.Name, actual.Name);
            Assert.AreEqual(expected.IsVisible, actual.IsVisible);
            Assert.AreEqual(expected.Id, actual.Id);
            Assert.AreEqual(expected.Layer.Id, actual.Layer.Id);

            CompareColor(expected.Color, actual.Color);
        }

        public static void CompareColor(DerivedColor expected, DerivedColor actual)
        {
            Assert.AreEqual(expected.LocalColor, actual.LocalColor);
            Assert.AreEqual(expected.Color, actual.Color);
            Assert.AreEqual(expected.ParentColor, actual.ParentColor);
            Assert.AreEqual(expected.IsFromParent, actual.IsFromParent);
        }
    }
}
