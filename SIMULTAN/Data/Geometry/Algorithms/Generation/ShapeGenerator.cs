using SIMULTAN.Utils;
using System.Windows;
using System.Windows.Media.Media3D;

namespace SIMULTAN.Data.Geometry
{
    /// <summary>
    /// Provides methods for generating shape geometries
    /// </summary>
    public static class ShapeGenerator
    {
        /// <summary>
        /// Generates a cube volume
        /// </summary>
        /// <param name="layer">The layer in which the cube should be placed</param>
        /// <param name="position">Position of the center of the cube</param>
        /// <param name="size">Length of the sides of the cube</param>
        /// <returns>A volume containing edges and vertices that for a cube</returns>
        public static Volume GenerateCube(Layer layer, Point3D position, Point3D size)
        {
            Point3D halfSize = size.Multiply(0.5);

            Vertex[] verts = new Vertex[]
            {
                new Vertex(layer, string.Empty, position - new Vector3D(-halfSize.X, -halfSize.Y, -halfSize.Z)),
                new Vertex(layer, string.Empty,  position - new Vector3D(-halfSize.X, -halfSize.Y, halfSize.Z)),
                new Vertex(layer, string.Empty, position - new Vector3D(halfSize.X, -halfSize.Y, halfSize.Z)),
                new Vertex(layer, string.Empty, position - new Vector3D(halfSize.X, -halfSize.Y, -halfSize.Z)),

                new Vertex(layer, string.Empty, position - new Vector3D(-halfSize.X, halfSize.Y, -halfSize.Z)),
                new Vertex(layer, string.Empty, position - new Vector3D(-halfSize.X, halfSize.Y, halfSize.Z)),
                new Vertex(layer, string.Empty, position - new Vector3D(halfSize.X, halfSize.Y, halfSize.Z)),
                new Vertex(layer, string.Empty, position - new Vector3D(halfSize.X, halfSize.Y, -halfSize.Z)),
            };

            Edge[] edges = new Edge[]
            {
                new Edge(layer, string.Empty, new Vertex[]{ verts[0], verts[1] }),
                new Edge(layer, string.Empty, new Vertex[]{ verts[1], verts[2] }),
                new Edge(layer, string.Empty, new Vertex[]{ verts[2], verts[3] }),
                new Edge(layer, string.Empty, new Vertex[]{ verts[3], verts[0] }),

                new Edge(layer, string.Empty, new Vertex[]{ verts[4], verts[5] }),
                new Edge(layer, string.Empty, new Vertex[]{ verts[5], verts[6] }),
                new Edge(layer, string.Empty, new Vertex[]{ verts[6], verts[7] }),
                new Edge(layer, string.Empty, new Vertex[]{ verts[7], verts[4] }),

                new Edge(layer, string.Empty, new Vertex[]{ verts[0], verts[4] }),
                new Edge(layer, string.Empty, new Vertex[]{ verts[1], verts[5] }),
                new Edge(layer, string.Empty, new Vertex[]{ verts[2], verts[6] }),
                new Edge(layer, string.Empty, new Vertex[]{ verts[3], verts[7] }),
            };

            EdgeLoop[] loops = new EdgeLoop[]
            {
                new EdgeLoop(layer, string.Empty, new Edge[] { edges[0], edges[1], edges[2], edges[3] }),
                new EdgeLoop(layer, string.Empty, new Edge[] { edges[4], edges[5], edges[6], edges[7] }),

                new EdgeLoop(layer, string.Empty, new Edge[] { edges[0], edges[8], edges[4], edges[9] }),
                new EdgeLoop(layer, string.Empty, new Edge[] { edges[1], edges[9], edges[5], edges[10] }),
                new EdgeLoop(layer, string.Empty, new Edge[] { edges[2], edges[10], edges[6], edges[11] }),
                new EdgeLoop(layer, string.Empty, new Edge[] { edges[3], edges[11], edges[7], edges[8] }),
            };

            Face[] faces = new Face[]
            {
                new Face(layer, "Ceiling", loops[0], GeometricOrientation.Backward),
                new Face(layer, "Floor", loops[1]),
                new Face(layer, "WallE", loops[2]),
                new Face(layer, "WallS", loops[3]),
                new Face(layer, "WallW", loops[4]),
                new Face(layer, "WallN", loops[5]),
            };

            Volume vol = new Volume(layer, "Cube", faces);
            return vol;
        }

        /// <summary>
        /// Generates a new rectangle in the XZ-plane
        /// </summary>
        /// <param name="layer">Layer on which the object should be placed</param>
        /// <param name="position">Position of one corner</param>
        /// <param name="size">Size of the rectangle (width along x-axis, height along z-axis)</param>
        /// <returns></returns>
        public static Face GenerateXZRectangle(Layer layer, Point3D position, Size size)
        {
            Vertex[] verts = new Vertex[]
            {
                new Vertex(layer, string.Empty, position),
                new Vertex(layer, string.Empty, position + new Vector3D(size.Width, 0, 0)),
                new Vertex(layer, string.Empty, position + new Vector3D(size.Width, 0, size.Height)),
                new Vertex(layer, string.Empty, position + new Vector3D(0, 0, size.Height)),
            };

            Edge[] edges = new Edge[]
            {
                new Edge(layer, string.Empty, new Vertex[]{ verts[0], verts[1] }),
                new Edge(layer, string.Empty, new Vertex[]{ verts[1], verts[2] }),
                new Edge(layer, string.Empty, new Vertex[]{ verts[2], verts[3] }),
                new Edge(layer, string.Empty, new Vertex[]{ verts[3], verts[0] })
            };

            EdgeLoop loop = new EdgeLoop(layer, string.Empty, new Edge[] { edges[0], edges[1], edges[2], edges[3] });

            return new Face(layer, "XYRectangle", loop);
        }
    }
}
