using SIMULTAN.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace SIMULTAN.Data.Geometry
{
    /// <summary>
    /// Methods for working with/converting offset models
    /// </summary>
    public static class OffsetSurfaceAlgorithms
    {
        /// <summary>
        /// Converts a set of offset faces into a new geometry model. Holes are ignored.
        /// Vertices and Edges are merged if they are at the same location after transformation.
        /// Intersections are not handled.
        /// </summary>
        /// <param name="faces">The offset faces that should be converted</param>
        /// <param name="transformation">An additional transformation which is applied to all vertices</param>
        /// <param name="tolerance">Tolerance for merging vertices</param>
        /// <returns>A geometry model containing all the faces</returns>
        public static GeometryModelData ConvertToModel(IEnumerable<OffsetFace> faces, Matrix3D transformation, double tolerance = 0.01)
        {
            var t2 = tolerance * tolerance;

            GeometryModelData data = new GeometryModelData(null);
            Layer layer = new Layer(data, "OffsetModel");

            //Calculation maximum and minimum along each axis
            var minmax = VertexAlgorithms.BoundingBox(faces.SelectMany(f => f.Boundary).Select(v => transformation.Transform(v)));

            AABBGrid vertexGrid = new AABBGrid(minmax.min, minmax.max, new Vector3D(5, 5, 5));

            List<Vertex> vertices = new List<Vertex>();
            List<Edge> edges = new List<Edge>();

            data.StartBatchOperation();

            foreach (var f in faces)
            {
                vertices.Clear();
                edges.Clear();

                // Vertices
                for (int i = 0; i < f.Boundary.Count; ++i)
                {
                    var v = f.Boundary[i];
                    var vpos = transformation.Transform(v);

                    Vertex vertex = null;

                    vertexGrid.ForCell(vpos, c =>
                    {
                        var cell = vertexGrid[c];
                        if (cell != null && cell.Any())
                        {
                            var minVertex = cell.ArgMin(aabb => (vpos - ((Vertex)aabb.Content).Position).LengthSquared);
                            if (minVertex.key <= t2)
                                vertex = (Vertex)minVertex.value.Content;
                        }
                    });

                    if (vertex == null) //No existing vertex
                    {
                        vertex = new Vertex(layer, string.Format("{0} ({1})", f.Face.Name, i), vpos);
                        vertexGrid.Add(new AABB(vertex));
                    }

                    vertices.Add(vertex);
                }

                //Edges
                for (int i = 0; i < vertices.Count; ++i)
                {
                    var v0 = vertices[i];
                    var v1 = vertices[(i + 1) % vertices.Count];

                    var edge = v0.Edges.FirstOrDefault(x => x.Vertices.Contains(v1));

                    if (edge == null) //No edge found
                    {
                        edge = new Edge(layer, string.Format("Edge {0} - {1}", v0.Name, v1.Name), new Vertex[] { v0, v1 });
                    }

                    edges.Add(edge);
                }

                //Loop
                var loop = new EdgeLoop(layer, f.Face.Name,  edges);

                //Face
                var face = new Face(layer, f.Face.Name, loop);
            }

            data.EndBatchOperation();

            return data;
        }
    }
}
