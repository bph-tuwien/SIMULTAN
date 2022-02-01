using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data.Geometry;
using SIMULTAN.Tests.Geometry.EventData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace SIMULTAN.Tests.Geometry.BaseGeometries
{
    [TestClass]
    public class PEdgeTests
    {
        private (Edge edge, BaseGeometryEventData eventData) EdgeWithEvents(Edge edge)
        {
            return (edge, new BaseGeometryEventData(edge));
        }

        [TestMethod]
        public void StartVertex()
        {
            var data = GeometryModelHelper.EmptyModel();

            var v0 = new Vertex(data.layer, "", new Point3D(1, 2, 3));
            var v1 = new Vertex(data.layer, "", new Point3D(2, 4, 6));

            var e0 = new Edge(data.layer, "", new Vertex[] { v0, v1 });

            var l0 = new Polyline(data.layer, "", new Edge[] { e0 });

            Assert.AreEqual(GeometricOrientation.Forward, l0.Edges[0].Orientation);
            Assert.AreEqual(v0, l0.Edges[0].StartVertex);

            l0.Edges[0].Orientation = GeometricOrientation.Backward;

            Assert.AreEqual(GeometricOrientation.Backward, l0.Edges[0].Orientation);
            Assert.AreEqual(v1, l0.Edges[0].StartVertex);
        }
    }
}
