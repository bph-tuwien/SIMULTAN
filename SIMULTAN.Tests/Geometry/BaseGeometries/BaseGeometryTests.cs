using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data.Geometry;
using SIMULTAN.Projects;
using SIMULTAN.Tests.Geometry.EventData;
using System.Windows.Media.Media3D;

namespace SIMULTAN.Tests.Geometry.BaseGeometries
{
    [TestClass]
    public class BaseGeometryTests
    {
        private (Vertex v, BaseGeometryEventData eventData) VertexWithEvents(Vertex v)
        {
            return (v, new BaseGeometryEventData(v));
        }

        private (Edge edge, BaseGeometryEventData eventData) EdgeWithEvents(Edge edge)
        {
            return (edge, new BaseGeometryEventData(edge));
        }


        [TestMethod]
        public void Parent()
        {
            ServicesProvider serviceProvider = new ServicesProvider();
            var projectData = new ExtendedProjectData();

            var data = GeometryModelHelper.EmptyModelWithEvents();
            projectData.GeometryModels.AddGeometryModel(data.model);
            var v0 = VertexWithEvents(new Vertex(data.layer, Lang.GEO_VERTEX_DEFAULTNAME, new Point3D(0, 0, 0)));
            var v1 = new Vertex(data.layer, Lang.GEO_VERTEX_DEFAULTNAME, new Point3D(0, 2, 0));
            var v2 = new Vertex(data.layer, Lang.GEO_VERTEX_DEFAULTNAME, new Point3D(0, 2, 2));

            var e0 = EdgeWithEvents(new Edge(data.layer, Lang.GEO_EDGE_DEFAULTNAME, new Vertex[] { v1, v2 }));

            v0.v.Parent = new GeometryReference(e0.edge, projectData.GeometryModels);

            Assert.AreEqual(e0.edge, v0.v.Parent.Target);
            Assert.AreEqual(nameof(BaseGeometry.Parent), v0.eventData.PropertyChangedData[0]);
            Assert.AreEqual(0, v0.eventData.GeometryChangedCount);
            Assert.AreEqual(0, v0.eventData.TopologyChangedCount);

            Assert.AreEqual(0, e0.eventData.PropertyChangedData.Count);
            Assert.AreEqual(0, e0.eventData.GeometryChangedCount);
            Assert.AreEqual(0, e0.eventData.TopologyChangedCount);
        }
    }
}
