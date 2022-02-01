using SIMULTAN.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.Geometry
{
    /// <summary>
    /// Provides algorithms for working with polylines
    /// </summary>
    public class PolylineAlgorithms
    {
        /// <summary>
        /// Tries to return an ordered list of edges
        /// </summary>
        /// <param name="edges">The input (unsorted) edges</param>
        /// <returns>A bool indicating whether the edge list forms a polyline and a list of edges ordered along the polyline</returns>
        public static (bool isConnected, List<PEdge> polyline) Order(IEnumerable<PEdge> edges)
        {
            //Find vertices that are only used in one edge
            var endVertices = edges.SelectMany(x => x.Edge.Vertices).GroupBy(x => x).Where(x => x.Count() == 1).Select(x => x.Key).ToList();

            if (endVertices.Count != 2)
                return (false, null);

            //Walk through line from one startvertex
            var startVertex = endVertices.ArgMin(x => x.Id).value;
            List<PEdge> unusedEdges = new List<PEdge>(edges);
            List<PEdge> result = new List<PEdge>();

            while (unusedEdges.Count > 0)
            {
                var nextEdge = unusedEdges.FirstOrDefault(x => x.Edge.Vertices.Contains(startVertex));

                if (nextEdge == null) //No polyline (or cycle)
                    return (false, null);

                result.Add(nextEdge);
                unusedEdges.Remove(nextEdge);
                startVertex = nextEdge.Edge.Vertices.First(x => x != startVertex);
            }

            return (true, result);
        }

        /// <summary>
        /// Returns the start and the end of a polyline described by a set of edges. Sets isConnected to False when the edges do not form polyline
        /// or describe a closed loop
        /// </summary>
        /// <param name="edges">The edges</param>
        /// <returns>
        ///		isConnected == True when the edges form a single polyline, otherwise False
        ///		startVertex, endVertex the start and end of the polyline. Only valid when isConnected equals True
        /// </returns>
        public static (bool isConnected, Vertex startVertex, Vertex endVertex) GetStartEnd(IEnumerable<Edge> edges)
        {
            //Find vertices that are only used in one edge
            var endVertices = edges.SelectMany(x => x.Vertices).GroupBy(x => x).Where(x => x.Count() == 1).Select(x => x.Key).ToList();

            if (endVertices.Count != 2)
                return (false, null, null);

            //Walk through line from one startvertex
            var startVertex = endVertices.ArgMin(x => x.Id).value;
            List<Edge> unusedEdges = new List<Edge>(edges);
            List<Edge> result = new List<Edge>();

            while (unusedEdges.Count > 0)
            {
                var nextEdge = unusedEdges.FirstOrDefault(x => x.Vertices.Contains(startVertex));

                if (nextEdge == null) //No polyline (or cycle)
                    return (false, null, null);

                result.Add(nextEdge);
                unusedEdges.Remove(nextEdge);
                startVertex = nextEdge.Vertices.First(x => x != startVertex);
            }

            return (true, endVertices[0], endVertices[1]);
        }
    }
}
