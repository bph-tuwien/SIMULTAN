using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data.Geometry;
using SIMULTAN.Projects;
using SIMULTAN.Tests.Geometry.EventData;
using SIMULTAN.Utils;
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

    }
}
