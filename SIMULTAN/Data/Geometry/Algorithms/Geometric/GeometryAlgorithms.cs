using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SIMULTAN.Data.SimMath;

namespace SIMULTAN.Data.Geometry
{
    /// <summary>
    /// Provides general algorithms for geometries (intersections, closest points, ...)
    /// </summary>
    public static class GeometryAlgorithms
    {
        /// <summary>
        /// Calculates the closes point on a line for a given input point
        /// </summary>
        /// <param name="p">The input point for which the closest point on the line should be found</param>
        /// <param name="lineOrigin">A point on the line</param>
        /// <param name="lineDirection">The direction vector of the line</param>
        /// <returns>
        /// pclosest: The position of the closest point on the line
        /// t: The line parameter of the closest point. pclosest = lineOrigin + t * lineDirection
        /// </returns>
        public static (SimPoint3D pclosest, double t) ClosestPointOnLine(SimPoint3D p, SimPoint3D lineOrigin, SimVector3D lineDirection)
        {
            var t = SimVector3D.DotProduct(lineDirection, (p - lineOrigin)) / SimVector3D.DotProduct(lineDirection, lineDirection);
            return (lineOrigin + t * lineDirection, t);
        }

        /// <summary>
        /// Calculates the shortest distance between two lines (rays)
        /// </summary>
        /// <param name="l0Origin">Point on the first line</param>
        /// <param name="l0Direction">Direction of the first line</param>
        /// <param name="l1Origin">Point on the second line</param>
        /// <param name="l1Direction">Direction of the second line</param>
        /// <returns>The shortest distance between the two lines. </returns>
        public static double LineLineDistance(SimPoint3D l0Origin, SimVector3D l0Direction, SimPoint3D l1Origin, SimVector3D l1Direction)
        {
            var sctc = LineLineSCTC(l0Origin, l0Direction, l1Origin, l1Direction);

            SimVector3D dP = sctc.w + (sctc.sc * sctc.u) - (sctc.tc * sctc.v);
            return dP.Length;

        }

        /// <summary>
        /// Calculates the shortest distance between a line and a line segment
        /// </summary>
        /// <param name="segmentP0">Start point of the segment</param>
        /// <param name="segmentP1">End point of the segment</param>
        /// <param name="rayOrigin">Point on the line</param>
        /// <param name="rayDirection">Direction of the line</param>
        /// <returns>The shortest distance between a line and a segment</returns>
        public static double LineSegmentDistance(SimPoint3D segmentP0, SimPoint3D segmentP1, SimPoint3D rayOrigin, SimVector3D rayDirection)
        {
            //Test infinte lines
            var segDir = segmentP1 - segmentP0;
            var sctc = LineLineSCTC(segmentP0, segDir, rayOrigin, rayDirection);

            sctc.sc = Math.Min(Math.Max(sctc.sc, 0.0), 1.0);

            SimVector3D dP = sctc.w + (sctc.sc * sctc.u) - (sctc.tc * sctc.v);
            return dP.Length;
        }

        /// <summary>
        /// Calculates the points with the minimum distance between a line and a segment
        /// </summary>
        /// <param name="segmentP0">Start point of the segment</param>
        /// <param name="segmentP1">End point of the segment</param>
        /// <param name="rayOrigin">Point on the line</param>
        /// <param name="rayDirection">Direction of the line</param>
        /// <returns>segmentPoint: Point on the segment, rayPoint: Point on the line</returns>
        public static (SimPoint3D segmentPoint, SimPoint3D rayPoint) LineSegmentClosesPoint(SimPoint3D segmentP0, SimPoint3D segmentP1, SimPoint3D rayOrigin, SimVector3D rayDirection)
        {
            //Test infinte lines
            var segDir = segmentP1 - segmentP0;
            var sctc = LineLineSCTC(segmentP0, segDir, rayOrigin, rayDirection);

            sctc.sc = Math.Min(Math.Max(sctc.sc, 0.0), 1.0);

            return (segmentP0 + sctc.sc * segDir, rayOrigin + sctc.tc * rayDirection);
        }

        private static (double sc, double tc, SimVector3D u, SimVector3D v, SimVector3D w)
            LineLineSCTC(SimPoint3D e0Origin, SimVector3D e0Direction, SimPoint3D e1Origin, SimVector3D e1Direction)
        {
            SimVector3D u = e0Direction;
            SimVector3D v = e1Direction;
            SimVector3D w = e0Origin - e1Origin;

            double a = SimVector3D.DotProduct(u, u);
            double b = SimVector3D.DotProduct(u, v);
            double c = SimVector3D.DotProduct(v, v);
            double d = SimVector3D.DotProduct(u, w);
            double e = SimVector3D.DotProduct(v, w);

            double D = a * c - b * b;

            double sc = 0;
            double tc = 0;

            if (D < 0.001) //Parallel
            {
                sc = 0.0;
                tc = (b > c ? d / b : e / c);
            }
            else
            {
                sc = (b * e - c * d) / D;
                tc = (a * e - b * d) / D;
            }

            return (sc, tc, u, v, w);
        }

        /// <summary>
        /// Calculates the points with the minimum distance between two lines
        /// </summary>
        /// <param name="l0Origin">Point on the first line</param>
        /// <param name="l0Direction">Direction of the first line</param>
        /// <param name="l1Origin">Point on the second line</param>
        /// <param name="l1Direction">Direction of the second line</param>
        /// <returns>l0Point: Point on the first line, l0Param: line parameter of l1Point, l1Param: Same for second line</returns>
        public static (SimPoint3D l0Point, double l0Param, SimPoint3D l1Point, double l1Param)
            LineLineClosestPoint(SimPoint3D l0Origin, SimVector3D l0Direction, SimPoint3D l1Origin, SimVector3D l1Direction)
        {
            var sctc = LineLineSCTC(l0Origin, l0Direction, l1Origin, l1Direction);

            return (l0Origin + sctc.sc * l0Direction, sctc.sc, l1Origin + sctc.tc * l1Direction, sctc.tc);
        }

        /*public static (SimPoint3D point, SimVector3D direction) PlanePlaneIntersection((SimPoint3D pos, SimVector3D direction) plane1, (SimPoint3D pos, SimVector3D direction) plane2, double tolerance)
		{
			//Not tested

			var hessian1 = HessianNormalForm(plane1.pos, plane1.direction);
			var hessian2 = HessianNormalForm(plane2.pos, plane2.direction);

			var dir = SimVector3D.CrossProduct(hessian1.n, hessian2.n);

			if (dir.LengthSquared < tolerance * tolerance)
				return (new SimPoint3D(0, 0, 0), new SimVector3D(0, 0, 0));

			dir.Normalize();
			var p0 = SimVector3D.CrossProduct(hessian2.p * hessian1.n - hessian1.p * hessian2.n, dir);
			return ((SimPoint3D)p0, dir);
		}*/
    }
}
