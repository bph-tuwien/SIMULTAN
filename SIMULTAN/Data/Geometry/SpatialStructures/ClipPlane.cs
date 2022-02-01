using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace SIMULTAN.Data.Geometry
{
    /// <summary>
    /// Describes a plane in Hessian normal form (ax + by + cz + d = 0) to clip against
    /// </summary>
    public class ClipPlane
    {
        /// <summary>
        /// Normal of the plane (a, b, c)
        /// </summary>
        public Vector3D Normal { get; set; }

        /// <summary>
        /// Distance of the plane (d) from the origin (along the plane normal)
        /// </summary>
        public double Distance { get; set; }

        /// <summary>
        /// Initializes a new instance of the ClipPlane class from a given normal and distance
        /// </summary>
        /// <param name="normal">Normal of the plane</param>
        /// <param name="distance">Distance from the origin along the normal</param>
        public ClipPlane(Vector3D normal, double distance)
        {
            this.Normal = normal;
            this.Distance = distance;
        }

        /// <summary>
        /// Initializes a new instance of the ClipPlane class from a given normal and point on the plane 
        /// </summary>
        /// <param name="pointOnPlane">Point on the plane</param>
        /// <param name="normal">Normal of the plane</param>
        public ClipPlane(Point3D pointOnPlane, Vector3D normal)
        {
            normal.Normalize();
            this.Normal = normal;
            this.Distance = -Vector3D.DotProduct(normal, (Vector3D)pointOnPlane);
        }

        /// <summary>
        /// Copies the parameters of another clip plane
        /// </summary>
        /// <param name="plane">ClipPlane to copy</param>
        public ClipPlane(ClipPlane plane)
        {
            this.Normal = plane.Normal;
            this.Distance = plane.Distance;
        }

        /// <summary>
        /// Checks whether the given point lies inside (i.e. behind the plane according to its normal)
        /// Point on the plane are considered as inside
        /// </summary>
        /// <param name="p">Point to test</param>
        /// <returns>Returns true, if the point lies inside (behind the plane)</returns>
        public bool IsInside(Point3D p)
        {
            return Vector3D.DotProduct(Normal, (Vector3D)p) <= -Distance;
        }

        /// <summary>
        /// Intersects the line given as (p1- p0) with this clip plane
        /// </summary>
        /// <param name="p0">Start point of the line</param>
        /// <param name="p1">End point of the line</param>
        /// <returns>
        /// The intersection point and the distance t between start point and intersection point
        /// If t is negative, the intersection point was not found
        /// </returns>
        public (Point3D intersectionPoint, double t) IntersectLine(Point3D p0, Point3D p1)
        {
            Point3D intersection;
            double t = 0.0;

            var lineDir = p1 - p0;

            // line parallel to plane -> no intersection
            if (Math.Abs(Vector3D.DotProduct(Normal, lineDir / lineDir.Length)) < 1e-9)
                return (new Point3D(), -1.0);

            var nom = (Vector3D.DotProduct((Vector3D)p0, Normal) + Distance);
            var denom = (Vector3D.DotProduct((Vector3D)p1, Normal) + Distance);
            t = nom / (nom - denom);

            if (IsInside(p0))
                t -= 1e-6;
            else
                t += 1e-6;

            intersection = p0 + t * lineDir;

            return (intersection, t);
        }
    }
}
