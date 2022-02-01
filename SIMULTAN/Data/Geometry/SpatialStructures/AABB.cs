using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace SIMULTAN.Data.Geometry
{
    /// <summary>
    /// Stores an axis-aligned bounding box
    /// </summary>
	public class AABB
    {
        /// <summary>
        /// The minimum along each axis
        /// </summary>
		public Point3D Min { get; set; }
        /// <summary>
        /// The maximum along each axis
        /// </summary>
		public Point3D Max { get; set; }

        /// <summary>
        /// The BaseGeometry this box belongs to
        /// </summary>
		public BaseGeometry Content { get; }

        /// <summary>
        /// Initializes a new instance of the AABB class
        /// </summary>
        /// <param name="min">The minimum position</param>
        /// <param name="max">The maximum position</param>
        /// <param name="content">The geometry this aabb contains</param>
		public AABB(Point3D min, Point3D max, BaseGeometry content)
        {
            if (min.X > max.X || min.Y > max.Y || min.Z > max.Z)
                throw new ArgumentException("Minimum may not be larger than maximum");
            this.Min = min;
            this.Max = max;
            this.Content = content;
        }
        /// <summary>
        /// Initializes a new instance of the AABB class
        /// </summary>
        /// <param name="face">The face for which the AABB should be calculated</param>
		public AABB(Face face)
        {
            Min = new Point3D(double.PositiveInfinity, double.PositiveInfinity, double.PositiveInfinity);
            Max = new Point3D(double.NegativeInfinity, double.NegativeInfinity, double.NegativeInfinity);

            foreach (var v in face.Boundary.Edges.Select(x => x.StartVertex))
            {
                var p = v.Position;
                Min = new Point3D(Math.Min(Min.X, p.X), Math.Min(Min.Y, p.Y), Math.Min(Min.Z, p.Z));
                Max = new Point3D(Math.Max(Max.X, p.X), Math.Max(Max.Y, p.Y), Math.Max(Max.Z, p.Z));
            }

            this.Content = face;
        }
        /// <summary>
        /// Initializes a new instance of the AABB class
        /// </summary>
        /// <param name="vertex">The vertex for which the AABB should be calculated</param>
		public AABB(Vertex vertex)
        {
            Min = vertex.Position;
            Max = vertex.Position;
            Content = vertex;
        }
        /// <summary>
        /// Initializes a new instance of the AABB class
        /// </summary>
        /// <param name="edge">The edge for which the AABB should be calculated</param>
		public AABB(Edge edge)
        {
            Min = new Point3D(
                Math.Min(edge.Vertices[0].Position.X, edge.Vertices[1].Position.X),
                Math.Min(edge.Vertices[0].Position.Y, edge.Vertices[1].Position.Y),
                Math.Min(edge.Vertices[0].Position.Z, edge.Vertices[1].Position.Z)
                );
            Max = new Point3D(
                Math.Max(edge.Vertices[0].Position.X, edge.Vertices[1].Position.X),
                Math.Max(edge.Vertices[0].Position.Y, edge.Vertices[1].Position.Y),
                Math.Max(edge.Vertices[0].Position.Z, edge.Vertices[1].Position.Z)
                );
            Content = edge;
        }
        /// <summary>
        /// Initializes a new instance of the AABB class
        /// </summary>
        /// <param name="volume">The volume for which the AABB should be calculated</param>
		public AABB(Volume volume)
        {
            Min = new Point3D(double.PositiveInfinity, double.PositiveInfinity, double.PositiveInfinity);
            Max = new Point3D(double.NegativeInfinity, double.NegativeInfinity, double.NegativeInfinity);

            foreach (var face in volume.Faces)
            {
                foreach (var v in face.Face.Boundary.Edges.Select(x => x.StartVertex))
                {
                    var p = v.Position;
                    Min = new Point3D(Math.Min(Min.X, p.X), Math.Min(Min.Y, p.Y), Math.Min(Min.Z, p.Z));
                    Max = new Point3D(Math.Max(Max.X, p.X), Math.Max(Max.Y, p.Y), Math.Max(Max.Z, p.Z));
                }
            }

            this.Content = volume;
        }

        /// <summary>
        /// Calculates the aabb which contains all item AABBs
        /// </summary>
        /// <param name="items">A list of AABBs which should be contained in the result</param>
        /// <returns>The minimum and maximum of all boxes</returns>
		public static (Point3D min, Point3D max) Merge(IEnumerable<AABB> items)
        {
            Point3D min = new Point3D(double.PositiveInfinity, double.PositiveInfinity, double.PositiveInfinity);
            Point3D max = new Point3D(double.NegativeInfinity, double.NegativeInfinity, double.NegativeInfinity);

            foreach (var item in items)
            {
                min = new Point3D(Math.Min(min.X, item.Min.X), Math.Min(min.Y, item.Min.Y), Math.Min(min.Z, item.Min.Z));
                max = new Point3D(Math.Max(max.X, item.Max.X), Math.Max(max.Y, item.Max.Y), Math.Max(max.Z, item.Max.Z));
            }

            return (min, max);
        }
    }
}
