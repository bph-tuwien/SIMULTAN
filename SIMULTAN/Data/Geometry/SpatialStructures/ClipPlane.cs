using SIMULTAN.Data.SimMath;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        public SimVector3D Normal { get; set; }

        /// <summary>
        /// Distance of the plane (d) from the origin (along the plane normal)
        /// </summary>
        public double Distance { get; set; }

        /// <summary>
        /// Initializes a new instance of the ClipPlane class from a given normal and distance
        /// </summary>
        /// <param name="normal">Normal of the plane</param>
        /// <param name="distance">Distance from the origin along the normal</param>
        public ClipPlane(SimVector3D normal, double distance)
        {
            this.Normal = normal;
            this.Distance = distance;
        }

        /// <summary>
        /// Initializes a new instance of the ClipPlane class from a given normal and point on the plane 
        /// </summary>
        /// <param name="pointOnPlane">Point on the plane</param>
        /// <param name="normal">Normal of the plane</param>
        public ClipPlane(SimPoint3D pointOnPlane, SimVector3D normal)
        {
            normal.Normalize();
            this.Normal = normal;
            this.Distance = -SimVector3D.DotProduct(normal, (SimVector3D)pointOnPlane);
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
        public bool IsInside(SimPoint3D p)
        {
            return SimVector3D.DotProduct(Normal, (SimVector3D)p) <= -Distance;
        }

        /// <summary>
        /// Intersects the line given as (p1- p0) with this clip plane
        /// </summary>
        /// <param name="p0">Start point of the line</param>
        /// <param name="p1">End point of the line</param>
        /// <param name="epsilon">Epsilon for comparison if the edge lies in the plane</param>
        /// <param name="insideEpsilon">Epsilon to add or subtract to ensure that t is inside in edge cases</param>
        /// <returns>
        /// The intersection point and the distance t between start point and intersection point
        /// If t is negative, the intersection point was not found
        /// </returns>
        public (SimPoint3D intersectionPoint, double t) IntersectLine(SimPoint3D p0, SimPoint3D p1, double epsilon = 1e-9, double insideEpsilon = 1e-6)
        {
            SimPoint3D intersection;
            double t = 0.0;

            var lineDir = p1 - p0;

            // line parallel to plane -> no intersection
            if (Math.Abs(SimVector3D.DotProduct(Normal, lineDir / lineDir.Length)) < epsilon)
                return (new SimPoint3D(), -1.0);

            var nom = (SimVector3D.DotProduct((SimVector3D)p0, Normal) + Distance);
            var denom = (SimVector3D.DotProduct((SimVector3D)p1, Normal) + Distance);
            t = nom / (nom - denom);

            if (IsInside(p0))
                t -= insideEpsilon;
            else
                t += insideEpsilon;

            intersection = p0 + t * lineDir;

            return (intersection, t);
        }
    }
}
