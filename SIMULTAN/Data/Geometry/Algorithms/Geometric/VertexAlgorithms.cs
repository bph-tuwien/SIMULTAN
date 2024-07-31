using SIMULTAN.Data.SimMath;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.Geometry
{
    /// <summary>
    /// Provides functions which operate on single points or vertices
    /// </summary>
    public static class VertexAlgorithms
    {
        /// <summary>
        /// Converts a point from a mathematically correct coordinate system (z = up) to a coordinate system used in computer graphics (y = up).
        /// i.e. it performs a 90° rotation around the x-axis
        /// </summary>
        /// <param name="x">x coordinate</param>
        /// <param name="y">y coordinate</param>
        /// <param name="z">z coordinate</param>
        /// <returns>Point in computer graphics coordinate system (y = up)</returns>
        public static SimPoint3D FromMathematicalCoordinateSystem(double x, double y, double z)
        {
            return new SimPoint3D(x, z, -y);
        }

        /// <summary>
        /// Converts a point from a mathematically correct coordinate system (z = up) to a coordinate system used in computer graphics (y = up).
        /// i.e. it performs a 90° rotation around the
        /// </summary>
        /// <param name="p">Point in mathematical coordinate system (z = up)</param>
        /// <returns>Point in computer graphics coordinate system (y = up)</returns>
        public static SimPoint3D FromMathematicalCoordinateSystem(SimPoint3D p)
        {
            return FromMathematicalCoordinateSystem(p.X, p.Y, p.Z);
        }

        /// <summary>
        /// Converts a point from a mathematically correct coordinate system (z = up) to a coordinate system used in computer graphics (y = up).
        /// i.e. it performs a 90° rotation around the
        /// </summary>
        /// <param name="v">Vertex in mathematical coordinate system (z = up)</param>
        /// <returns>Point in computer graphics coordinate system (y = up)</returns>
        public static SimPoint3D FromMathematicalCoordinateSystem(Vertex v)
        {
            return FromMathematicalCoordinateSystem(v.Position);
        }

        /// <summary>
        /// Converts a point from rendering coordinate system (y = up) to a mathematically correct coordinate system (z = up).
        /// </summary>
        /// <param name="p">Vertex in rendering system (y = up)</param>
        /// <returns>Point in mathematically correct coordinate system (z = up)</returns>
        public static SimPoint3D ToMathematicalCoordinateSystem(SimPoint3D p)
        {
            return new SimPoint3D(p.X, -p.Z, p.Y);
        }

        /// <summary>
        /// Checks if a given set of points is collinear i.e. lies on the same line.
        /// </summary>
        /// <param name="positions">Set of positions</param>
        /// <param name="threshold">Threshold to detect collinearity (maximum distance of a point from the shared line, max 1), default 1e-10</param>
        /// <returns>true, if points lie on the same line</returns>
        public static bool IsCollinear(List<SimPoint3D> positions, double threshold = 1e-10)
        {
            if (positions.Count < 3) return true;

            SimVector3D dir = positions[1] - positions[0];
            for (int i = 2; i < positions.Count; i++)
            {
                SimVector3D d = positions[i] - positions[0];
                if (SimVector3D.CrossProduct(d, dir).Length / dir.Length > threshold)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Checks if a given set of points is collinear i.e. lies on the same line.
        /// </summary>
        /// <param name="vertices">Set of vertices</param>
        /// <param name="threshold">Threshold to detect collinearity, default 1e-10</param>
        /// <returns>true, if points lie on the same line</returns>
        public static bool IsCollinear(List<Vertex> vertices, double threshold = 1e-10)
        {
            return IsCollinear(vertices.Select(x => x.Position).ToList(), threshold);
        }

        /// <summary>
        /// Checks if all coordinates in 2 given points are equal within a small threshold.
        /// </summary>
        /// <param name="p1">First point</param>
        /// <param name="p2">Second point</param>
        /// <param name="threshold">Maximum allowed difference of coordinates</param>
        /// <returns>true, if points are equal</returns>
        public static bool IsEqual(SimPoint3D p1, SimPoint3D p2, double threshold = 1e-8)
        {
            var d = p1 - p2;
            return Math.Abs(d.X) <= threshold && Math.Abs(d.Y) <= threshold && Math.Abs(d.Z) <= threshold;
        }

        /// <summary>
        /// Calculates the minimum and maximum (axis aligned bounding box) of a set of points along each axis
        /// </summary>
        /// <param name="points">The list of points</param>
        /// <returns>The minimum and maximum along each axis</returns>
        public static (SimPoint3D min, SimPoint3D max) BoundingBox(IEnumerable<SimPoint3D> points)
        {
            SimPoint3D min = new SimPoint3D(double.PositiveInfinity, double.PositiveInfinity, double.PositiveInfinity);
            SimPoint3D max = new SimPoint3D(double.NegativeInfinity, double.NegativeInfinity, double.NegativeInfinity);

            foreach (var p in points)
            {
                min = new SimPoint3D(Math.Min(min.X, p.X), Math.Min(min.Y, p.Y), Math.Min(min.Z, p.Z));
                max = new SimPoint3D(Math.Max(max.X, p.X), Math.Max(max.Y, p.Y), Math.Max(max.Z, p.Z));
            }

            return (min, max);
        }

        /// <summary>
        /// Gets all adjacent faces of the vertex and adds them to the faces list.
        /// </summary>
        /// <param name="v">The vertex</param>
        /// <param name="faces">The faces list</param>
        public static void AdjacentFaces(this Vertex v, ref HashSet<Face> faces)
        {
            foreach (var edge in v.Edges)
            {
                edge.AdjacentFaces(ref faces);
            }
        }
    }
}
