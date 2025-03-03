using SIMULTAN;
using SIMULTAN.Data.SimMath;
using SIMULTAN.Exceptions;
using SIMULTAN.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace SIMULTAN.Data.Geometry
{
    /// <summary>
    /// Provides algorithms for working with GeoReferences
    /// </summary>
    public static class GeoReferenceAlgorithms
    {
        // coefficients of reference ellipsoid according to WGS84
        private static double WGS_a = 6378137.0;
        private static double WGS_b = 6356752.314245;
        private static double WGS_f = 1 / 298.257223563;
        private static double WGS_e2 = WGS_f * (2.0 - WGS_f); // e * e

        private static double degToRad = Math.PI / 180.0;
        private static double radToDeg = 180.0 / Math.PI;


        /// <summary>
        /// This searches for the best georeferences to form a 2D coordinate system (3 points) used for interpolation.
        /// Criteria are: axis are close to orthogonal, points span a wide distance, area covered by triangle is maximal
        /// All the points have distinct positions at least on XY and XZ plane
        /// </summary>
        /// <param name="geoReferences">List of georeferences to search</param>
        /// <param name="pOS">Point in object space to find transformation for</param>
        /// <returns>Transformation used for interpolation of WGS84 coordinates</returns>
        private static GeoRefTransform FindBestGeoReferences(List<GeoRefPoint> geoReferences, SimPoint3D pOS)
        {
            // find nearest georef -> first point
            (GeoRefPoint nearestGeoRef, _, _) = geoReferences.Where(t =>
                     (t.OS.X != pOS.X || t.OS.Y != pOS.Y || t.OS.Z != pOS.Z)).ArgMin(x => (x.OS - pOS).LengthSquared);


            var distinctFarthersPoints = geoReferences.Where(t =>
                     (t.OS.X != nearestGeoRef.OS.X || t.OS.Z != nearestGeoRef.OS.Z));
            if (distinctFarthersPoints.Count() < 1)
            {
                //The geo references must have at least three plane-wise-distinct points
                throw new InvalidGeoReferencingException();
            }
            // find farthest point from first point -> second point
            (GeoRefPoint farthestPoint, _, _) = distinctFarthersPoints.ArgMax(x => (x.OS - nearestGeoRef.OS).LengthSquared);


            var distinctPoints = geoReferences.Where(t =>
                      (t.OS.X != farthestPoint.OS.X || t.OS.Z != farthestPoint.OS.Z) &&
                      (t.OS.X != nearestGeoRef.OS.X || t.OS.Z != nearestGeoRef.OS.Z));
            if (distinctPoints.Count() < 1)
            {
                //The geo references must have at least three plane-wise-distinct points
                throw new InvalidGeoReferencingException();
            }
            // find third point which forms a triangle with maximum area
            (GeoRefPoint anglePoint, _, _) = distinctPoints.ArgMax(x => SimVector3D.CrossProduct(nearestGeoRef.OS - x.OS, nearestGeoRef.OS - farthestPoint.OS).LengthSquared);


            return new GeoRefTransform(nearestGeoRef, farthestPoint, anglePoint);
        }

        /// <summary>
        /// Validates list of georeferenced points.
        /// Criteria are: at least 3, not all points collinear in object and WGS space 
        /// (also implicitly considers distance between points => should not be too close together)
        /// </summary>
        /// <param name="geoReferences">List of georeferenced points</param>
        /// <returns>true, if given georeferences are suitable for interpolation of WGS84 coordinates, i.e. building can be placed in world space</returns>
        public static bool ValidateGeoReferences(List<GeoRefPoint> geoReferences)
        {
            if (geoReferences.Count < 3)
                return false;

            // avoid collinearity in object and WGS space
            if (VertexAlgorithms.IsCollinear(geoReferences.Select(x => x.OS).ToList(), 1e-3)
               || VertexAlgorithms.IsCollinear(geoReferences.Select(x => x.WGS).ToList(), 1e-6))
                return false;

            return true;
        }

        /// <summary>
        /// Transforms given list of points into the WGS84 reference system based on a set of valid georeferenced points (use ValidateGeoReferences first).
        /// WGS84 coordinates for non-georeferenced points are interpolated/extrapolated non-linearly - see EstimateWGSNonLinear.
        /// Throws an InvalidGeoReferencingException if geoReferences are invalid
        /// </summary>
        /// <param name="positions">List of points to geo-reference.</param>
        /// <param name="geoReferences">List of known georeferences</param>
        /// <returns>Transformed list of points in Cartesian WGS84 space. Transformed list of points in WGS84 space (longitude/latitude/height).</returns>
        public static (List<SimPoint3D> positionsWS, List<SimPoint3D> positionsWGS) GeoReferenceMesh(List<SimPoint3D> positions, List<GeoRefPoint> geoReferences)
        {
            if (!ValidateGeoReferences(geoReferences))
                throw new InvalidGeoReferencingException();

            // transform from obj space to WGS space, extrapolate non-linearly
            List<SimPoint3D> posWGS = EstimateWGSNonLinear(positions, geoReferences);

            // transform from ellipsoidal coordinates to cartesian space
            List<SimPoint3D> posWS = posWGS.Select(x => VertexAlgorithms.FromMathematicalCoordinateSystem(WGS84ToCart(x))).ToList();

            return (posWS, posWGS);
        }

        /// <summary>
        /// Given a set of georeferences and a set of points, each point gets assigned a WGS84 coordinate.
        /// Points which are already referenced keep their respective georeference. 
        /// Points directly above or below other points are garantueed to have the same long/lat coords.
        /// Other points are interpolated/extrapolated in a non-linear fashion given as:
        /// A 2D coordinate system (3 points) meeting certain criteria (see FindBestGeoReferences) is computed for each non-georeferenced point.
        /// The 2D coordinate system is used to calculate angles between its axes and the current point relative to the 
        /// referenced origin in object space. This angle is used to interpolate linearly between the azimuth of the 2 
        /// chosen directions. Eventually, the interpolated azimuth in combination with the distance in object space (in meter) is used 
        /// to obtain the new WGS84 coordinates using Vincenty's direct formulae.
        /// </summary>
        /// <param name="positions">List of positions to assign WGS84 coordinates to</param>
        /// <param name="geoReferences">List of known georeferenced points</param>
        /// <returns>Georeferenced points (WGS84) in same order as in given point list. </returns>
        public static List<SimPoint3D> EstimateWGSNonLinear(List<SimPoint3D> positions, List<GeoRefPoint> geoReferences)
        {
            SimPoint3D[] tfPoints = new SimPoint3D[positions.Count];

            // compute average height of georeferences
            var avgGeoRefHeight = geoReferences.Average(x => x.OS.Y);

            // group positions such that every group makes up a column, i.e. has the same x and z coordinates
            List<List<(SimPoint3D p, int idx)>> groupedPositions = new List<List<(SimPoint3D p, int idx)>>();
            List<(SimPoint3D grp, int grpIdx)> groups = new List<(SimPoint3D grp, int grpIdx)>();

            for (int i = 0; i < positions.Count; i++)
            {
                int grpIdx = -1;
                // check if group already exists
                for (int j = 0; j < groups.Count; j++)
                {
                    if (VertexAlgorithms.IsEqual(new SimPoint3D(positions[i].X, 0.0, positions[i].Z), groups[j].grp))
                    {
                        grpIdx = groups[j].grpIdx;
                        break;
                    }
                }

                if (grpIdx == -1) // add new group
                {
                    groups.Add((new SimPoint3D(positions[i].X, 0.0, positions[i].Z), groupedPositions.Count));
                    groupedPositions.Add(new List<(SimPoint3D p, int idx)>() { (positions[i], i) });
                }
                else // add to group
                {
                    groupedPositions[grpIdx].Add((positions[i], i));
                }
            }

            // foreach group 
            foreach (var group in groupedPositions)
            {
                // check for existing georefs
                int referenceFound = -1;
                SimPoint3D wgs = new SimPoint3D();
                for (int i = 0; i < group.Count; i++)
                {
                    bool found = false;
                    (found, wgs) = CheckForExistingGeoReference(group[i].p, geoReferences);
                    if (found)
                    {
                        referenceFound = i;
                        break;
                    }
                }

                if (referenceFound != -1) // if group has a known georeference
                {
                    for (int i = 0; i < group.Count; i++)
                    {
                        SimPoint3D pWGS = wgs;
                        if (i != referenceFound)
                        {
                            pWGS += new SimVector3D(0.0, 0.0, group[i].p.Y - group[referenceFound].p.Y);
                        }
                        tfPoints[group[i].idx] = pWGS;
                    }
                }
                else
                {
                    // if only 1 vertex -> interpolate
                    if (group.Count == 1)
                    {
                        var pOS = group[0].p;
                        var transform = FindBestGeoReferences(geoReferences, pOS);
                        wgs = InterpolateWGSCoords(transform, pOS);
                        tfPoints[group[0].idx] = wgs;
                    }
                    else // else interpolate vertex with minimal height distance to average height of georefs and use same long/lat for other points in group
                    {
                        (var avgPOS, _, var minIdx) = group.ArgMin(x => Math.Abs(x.p.Z - avgGeoRefHeight));
                        var transform = FindBestGeoReferences(geoReferences, avgPOS.p);
                        wgs = InterpolateWGSCoords(transform, avgPOS.p);

                        for (int i = 0; i < group.Count; i++)
                        {
                            SimPoint3D pWGS = wgs;
                            if (i != minIdx)
                            {
                                pWGS += new SimVector3D(0.0, 0.0, group[i].p.Y - avgPOS.p.Y);
                            }
                            tfPoints[group[i].idx] = pWGS;
                        }
                    }
                }
            }

            return tfPoints.ToList();
        }

        private static (bool success, SimPoint3D wgs) CheckForExistingGeoReference(SimPoint3D position, List<GeoRefPoint> geoRefs, double epsilon = 1e-8)
        {
            for (int j = 0; j < geoRefs.Count; j++)
            {
                if (Math.Abs(position.X - geoRefs[j].OS.X) <= epsilon &&
                    Math.Abs(position.Z - geoRefs[j].OS.Z) <= epsilon)
                {
                    return (true, geoRefs[j].WGS + new SimVector3D(0.0, 0.0, position.Y - geoRefs[j].OS.Y));
                }
            }

            return (false, new SimPoint3D());
        }

        private static SimPoint3D InterpolateWGSCoords(GeoRefTransform transform, SimPoint3D pOS)
        {
            SimPoint3D oObj = transform.RefOrigin.OS;
            SimPoint3D p1Obj = transform.RefP1.OS;
            SimPoint3D p2Obj = transform.RefP2.OS;

            // directions in object space of georeferenced points
            SimVector dirObj1 = new SimVector(p1Obj.X - oObj.X, p1Obj.Z - oObj.Z);
            SimVector dirObj2 = new SimVector(p2Obj.X - oObj.X, p2Obj.Z - oObj.Z);

            dirObj1.Normalize();
            dirObj2.Normalize();
            SimVector dir = new SimVector(pOS.X - oObj.X, pOS.Z - oObj.Z);
            double l = dir.Length;

            dir.Normalize();
            // interpolate between azimuths according to object space angles
            double alpha = DetectionAlgorithms.SignedAngle(dirObj1, dirObj2);
            double beta = DetectionAlgorithms.SignedAngle(dir, dirObj2);
            var azimuthSpan = DetectionAlgorithms.ShortAngleDist(transform.Azimuth2, transform.Azimuth1);
            double azimuth = transform.Azimuth2 + beta * (azimuthSpan / alpha);
            if (azimuth < 0.0) azimuth += 360.0;
            if (azimuth > 360.0) azimuth -= 360.0;

            // use interpolated azimuth and object space distance to move (vincenty)
            (var pWGS, _) = VincentyDirect(transform.RefOrigin.WGS, azimuth, l);
            return pWGS + new SimVector3D(0.0, 0.0, pOS.Y - oObj.Y);
        }

        /// <summary>
        /// Transforms a point with WGS coordinates into the Cartesian coordinate system of the WGS reference ellipsoid.
        /// https://gssc.esa.int/navipedia/index.php/Ellipsoidal_and_Cartesian_Coordinates_Conversion
        /// https://en.wikipedia.org/wiki/World_Geodetic_System
        /// </summary>
        /// <param name="p">A point consisting of longitude (x), latitude (y) and height relative to surface (z)</param>
        /// <returns>The point in the Cartesian coordinate system of the WGS reference ellipsoid.</returns>
        public static SimPoint3D WGS84ToCart(SimPoint3D p)
        {
            double a = WGS_a;
            double f = WGS_f;
            double e2 = WGS_e2;
            double phi = p.Y * degToRad;
            double lambda = p.X * degToRad;
            double h = p.Z;
            double N = a / Math.Sqrt(1.0 - e2 * Math.Sin(phi) * Math.Sin(phi));

            double X = (N + h) * Math.Cos(phi) * Math.Cos(lambda);
            double Y = (N + h) * Math.Cos(phi) * Math.Sin(lambda);
            double Z = ((1.0 - e2) * N + h) * Math.Sin(phi);

            return new SimPoint3D(X, Y, Z);
        }

        /// <summary>
        /// Transforms a point with cartesian coordinates into the WGS coordinate system.
        /// https://gssc.esa.int/navipedia/index.php/Ellipsoidal_and_Cartesian_Coordinates_Conversion
        /// https://en.wikipedia.org/wiki/World_Geodetic_System
        /// </summary>
        /// <param name="point">A point in the catesian space with [0,0,0] being the earths center.</param>
        /// <param name="precision">The precision of the calculation.</param>
        /// <param name="maxIterations">Maximum number of iterations to prevent infinite loops</param>
        /// <returns>The point in the WGS coordinate system.</returns>
        public static SimPoint3D CartToWGS84(SimPoint3D point, double precision = 0.0000001, int maxIterations = 1000000)
        {
            double lng = Math.Atan2(point.Y, point.X);
            double p = Math.Sqrt(point.X * point.X + point.Y * point.Y);
            double e2 = WGS_e2;
            double phi0 = Math.Atan(point.Z / ((1.0 - e2) * p));
            double phiOld = 0;
            double a = WGS_a;
            double phiNew = 0;
            double h = 0;

            int count = 0;
            do
            {
                double s = Math.Sin(phi0);
                double n = a / Math.Sqrt(1.0 - e2 * s * s);
                h = (p / Math.Cos(phi0)) - n;
                phiNew = Math.Atan(point.Z / ((1.0 - e2 * (n / (n + h))) * p));
                phiOld = phi0;
                phi0 = phiNew;
                count++;
            } while (Math.Abs(phiOld - phiNew) > precision && count < maxIterations);

            return new SimPoint3D(lng * radToDeg, phiNew * radToDeg, h);
        }

        /// <summary>
        /// Computes the tangent frame on the WGS84 ellipsoid in Cartesian coordinates for a given positions in WGS coordinates
        /// </summary>
        /// <param name="p">WGS coordinates of point</param>
        /// <returns>Tangent frame (tangent, bitangent, normal) for given point</returns>
        public static (SimVector3D T, SimVector3D B, SimVector3D N) TangentFrame(SimPoint3D p)
        {
            // maybe replace with analytic? but works fine so far
            (var pT, _) = VincentyDirect(p, 90.0, 1.0);
            (var pB, _) = VincentyDirect(p, 0.0, 1.0);

            var p_WS = VertexAlgorithms.FromMathematicalCoordinateSystem(WGS84ToCart(p));
            var pT_WS = VertexAlgorithms.FromMathematicalCoordinateSystem(WGS84ToCart(pT));
            var pB_WS = VertexAlgorithms.FromMathematicalCoordinateSystem(WGS84ToCart(pB));

            SimVector3D T = pT_WS - p_WS;
            T.Normalize();
            SimVector3D B = -(pB_WS - p_WS);
            B.Normalize();
            SimVector3D N = -SimVector3D.CrossProduct(T, B);

            return (T, B, N);
        }

        /// <summary>
        /// Vincenty's direct formula to walk along a given amount in a given direction to obtain a new point lying on the WGS reference ellipsoid.
        /// https://en.wikipedia.org/wiki/Vincenty%27s_formulae
        /// </summary>
        /// <param name="p">Start point with longitude (x), latitude (y) and height in m (z)</param>
        /// <param name="azimuth">Direction to walk into given as an angle in degrees relative to the north pole [0,360)</param>
        /// <param name="distance">Distance in meters to walk</param>
        /// <param name="tolerance">Tolerance for the iterative algorithm. Default is 1e-12.</param>
        /// <param name="maxIterations">Max iterations used as fallback if tolerance cannot be reached to prevent infinite loops.</param>
        /// <returns>A point (longitude/latitude) which has a geodesic distance to the start point equal to the given input distance and it's final bearing (azimuth). 
        /// Note, that this point has the same height as the input point.</returns>
        public static (SimPoint3D dest, double bearing) VincentyDirect(SimPoint3D p, double azimuth, double distance, double tolerance = 1e-17 /* 1e-18 caused infinite loops in some cases */
            , int maxIterations = 1000000)
        {
            double h = p.Z;
            double a = WGS_a + h;
            double b = WGS_b + h;
            double f = WGS_f;

            double lat1 = p.Y * degToRad;
            double lon1 = p.X * degToRad;
            double brg = azimuth * degToRad;
            double s = distance;

            double sb = Math.Sin(brg);
            double cb = Math.Cos(brg);
            double tu1 = (1 - f) * Math.Tan(lat1);
            double cu1 = 1 / Math.Sqrt((1 + tu1 * tu1));
            double su1 = tu1 * cu1;
            double s2 = Math.Atan2(tu1, cb);
            double sa = cu1 * sb;
            double csa = 1 - sa * sa;
            double us = csa * (a * a - b * b) / (b * b);
            double A = 1 + us / 16384 * (4096 + us * (-768 + us * (320 - 175 * us)));
            double B = us / 1024 * (256 + us * (-128 + us * (74 - 47 * us)));
            double s1 = s / (b * A);
            double s1p = 2 * Math.PI;

            double ss1 = 0.0;
            double cs1 = 0.0;
            double cs1m = 0.0;

            int count = 0;
            while (Math.Abs(s1 - s1p) > tolerance && count < maxIterations)
            {
                cs1m = Math.Cos(2 * s2 + s1);
                ss1 = Math.Sin(s1);
                cs1 = Math.Cos(s1);
                double ds1 = B * ss1 * (cs1m + B / 4 * (cs1 * (-1 + 2 * cs1m * cs1m) - B / 6 * cs1m * (-3 + 4 * ss1 * ss1) * (-3 + 4 * cs1m * cs1m)));
                s1p = s1;
                s1 = s / (b * A) + ds1;
                count++;
            }

            double t = su1 * ss1 - cu1 * cs1 * cb;
            double lat2 = Math.Atan2(su1 * cs1 + cu1 * ss1 * cb, (1 - f) * Math.Sqrt(sa * sa + t * t));
            double l2 = Math.Atan2(ss1 * sb, cu1 * cs1 - su1 * ss1 * cb);
            double c = f / 16 * csa * (4 + f * (4 - 3 * csa));
            double l = l2 - (1 - c) * f * sa * (s1 + c * ss1 * (cs1m + c * cs1 * (-1 + 2 * cs1m * cs1m)));
            double d = Math.Atan2(sa, -t);
            double finalBrg = d + 2 * Math.PI;
            double backBrg = d + Math.PI;
            double lon2 = lon1 + l;

            lat2 = lat2 * radToDeg;
            lon2 = lon2 * radToDeg;
            finalBrg = finalBrg * radToDeg;
            backBrg = backBrg * radToDeg;

            if (lon2 < -180)
            {
                lon2 = lon2 + 360;
            }
            if (lon2 > 180)
            {
                lon2 = lon2 - 360;
            }

            if (finalBrg < 0)
            {
                finalBrg = finalBrg + 360;
            }
            if (finalBrg > 360)
            {
                finalBrg = finalBrg - 360;
            }

            return (new SimPoint3D(lon2, lat2, p.Z), finalBrg);
        }

        /// <summary>
        /// Vincenty's indirect formula to compute the geodesic distance and azimuth between two given WGS84 coordinate pairs.
        /// https://en.wikipedia.org/wiki/Vincenty%27s_formulae
        /// </summary>
        /// <param name="source">Start point with longitude (x), latitude (y) and height in m (z)</param>
        /// <param name="dest">End point with with longitude (x), latitude (y) and height in m (z)</param>
        /// <param name="tolerance">Tolerance for the iterative algorithm. Default is 1e-12.</param>
        /// <returns>Geodesic distance between the two given points as well as it's final bearing (azimuth in [0, 360)).</returns>
        public static (double distance, double bearing) VincentyIndirect(SimPoint3D source, SimPoint3D dest, double tolerance = 1e-18)
        {
            // averaging the height on which we measure since it is impossible to measure geodesic distance across 2 different ellipsoids
            double h = (source.Z + dest.Z) * 0.5;
            double a = WGS_a + h;
            double b = WGS_b + h;
            double f = WGS_f;

            var phi1 = source.Y * degToRad;
            var lambda1 = source.X * degToRad;
            var phi2 = dest.Y * degToRad;
            var lambda2 = dest.X * degToRad;

            var L = lambda2 - lambda1; // L = difference in longitude, U = reduced latitude, defined by tan U = (1-f)·tan phi.
            var tanU1 = (1.0 - f) * Math.Tan(phi1);
            var cosU1 = 1.0 / Math.Sqrt((1.0 + tanU1 * tanU1));
            var sinU1 = tanU1 * cosU1;
            var tanU2 = (1.0 - f) * Math.Tan(phi2);
            var cosU2 = 1.0 / Math.Sqrt((1.0 + tanU2 * tanU2));
            var sinU2 = tanU2 * cosU2;
            var antipodal = Math.Abs(L) > Math.PI * 0.5 || Math.Abs(phi2 - phi1) > Math.PI * 0.5;

            var lambda = L;
            var sinLambda = 0.0;
            var cosLambda = 0.0; // lambda = difference in longitude on an auxiliary sphere
            var sigma = antipodal ? Math.PI : 0.0;
            var sinSigma = 0.0;
            var cosSigma = antipodal ? -1.0 : 1.0;
            var sinSqSigma = 0.0; // sigma = angular distance P₁ P₂ on the sphere
            var cos2SigmaM = 1.0; // sigmaM = angular distance on the sphere from the equator to the midpoint of the line
            var sinAlpha = 0.0;
            var cosSqAlpha = 1.0; // alpha = azimuth of the geodesic at the equator
            var C = 0.0;

            var lambdaP = 0.0;
            var iterations = 0;
            var eps = tolerance;
            do
            {
                sinLambda = Math.Sin(lambda);
                cosLambda = Math.Cos(lambda);
                sinSqSigma = (cosU2 * sinLambda) * (cosU2 * sinLambda) + (cosU1 * sinU2 - sinU1 * cosU2 * cosLambda) * (cosU1 * sinU2 - sinU1 * cosU2 * cosLambda);
                if (Math.Abs(sinSqSigma) < eps)
                    break;  // co-incident/antipodal points (falls back on lambda/sigma = L)
                sinSigma = Math.Sqrt(sinSqSigma);
                cosSigma = sinU1 * sinU2 + cosU1 * cosU2 * cosLambda;
                sigma = Math.Atan2(sinSigma, cosSigma);
                sinAlpha = cosU1 * cosU2 * sinLambda / sinSigma;
                cosSqAlpha = 1 - sinAlpha * sinAlpha;
                cos2SigmaM = (cosSqAlpha != 0.0) ? (cosSigma - 2.0 * sinU1 * sinU2 / cosSqAlpha) : 0.0; // on equatorial line cos^2 alpha = 0 (§6)
                C = f / 16.0 * cosSqAlpha * (4.0 + f * (4.0 - 3.0 * cosSqAlpha));
                lambdaP = lambda;
                lambda = L + (1 - C) * f * sinAlpha * (sigma + C * sinSigma * (cos2SigmaM + C * cosSigma * (-1 + 2 * cos2SigmaM * cos2SigmaM)));
                var iterationCheck = antipodal ? Math.Abs(lambda) - Math.PI : Math.Abs(lambda);
                if (iterationCheck > Math.PI)
                    return (0.0, 0.0);

            } while (Math.Abs(lambda - lambdaP) > eps && ++iterations < 1000);

            if (iterations >= 1000)
                return (0.0, 0.0);

            var uSq = cosSqAlpha * (a * a - b * b) / (b * b);
            var A = 1.0 + uSq / 16384.0 * (4096 + uSq * (-768.0 + uSq * (320.0 - 175.0 * uSq)));
            var B = uSq / 1024.0 * (256.0 + uSq * (-128.0 + uSq * (74.0 - 47.0 * uSq)));
            var deltaSigma = B * sinSigma * (cos2SigmaM + B / 4.0 * (cosSigma * (-1.0 + 2.0 * cos2SigmaM * cos2SigmaM) -
                B / 6.0 * cos2SigmaM * (-3.0 + 4.0 * sinSigma * sinSigma) * (-3.0 + 4.0 * cos2SigmaM * cos2SigmaM)));

            var s = b * A * (sigma - deltaSigma); // s = length of the geodesic

            // note special handling of exactly antipodal points where sin^2 sigma = 0 
            // (due to discontinuity atan2(0, 0) = 0 but atan2(eps, 0) = pi/2 / 90°) 
            //- in which case bearing is always meridional,
            // due north (or due south!)
            // alpha = azimuths of the geodesic; alpha2 the direction P1 P2 produced
            var alpha1 = Math.Abs(sinSqSigma) < eps ? 0 : Math.Atan2(cosU2 * sinLambda, cosU1 * sinU2 - sinU1 * cosU2 * cosLambda);
            var alpha2 = Math.Abs(sinSqSigma) < eps ? Math.PI : Math.Atan2(cosU1 * sinLambda, -sinU1 * cosU2 + cosU1 * sinU2 * cosLambda);

            if (alpha2 < 0.0) alpha2 += 2.0 * Math.PI;
            return (s, Math.Abs(s) < eps ? 0.0 : alpha2 * radToDeg);
        }

        /// <summary>
        /// Converts UTM coordinates to WGS84 coordinates. Height is simply copied from UTM to WGS84.
        /// https://www.movable-type.co.uk/scripts/latlong-utm-mgrs.html
        /// Transverse Mercator with an accuracy of a few nanometers, Karney 2011
        /// </summary>
        /// <param name="utm">UTM coordinates</param>
        /// <returns>WGS84 coordinates</returns>
        public static SimPoint3D ConvertUTMToWGS84(UTMCoord utm)
        {
            double falseEasting = 500e3;
            double falseNorthing = 10000e3;
            double k0 = 0.9996; // UTM scale on central meridian
            double x = utm.Easting - falseEasting;
            double y = utm.NorthernHemisphere ? utm.Northing : utm.Northing - falseNorthing;

            // ---- from Karney 2011 Eq 15-22, 36:

            double e = Math.Sqrt(WGS_e2); // eccentricity
            double n = WGS_f / (2.0 - WGS_f); // 3rd flattening
            double n2 = n * n, n3 = n * n2, n4 = n * n3, n5 = n * n4, n6 = n * n5;

            double A = WGS_a / (1.0 + n) * (1.0 + (1.0 / 4.0) * n2 + (1.0 / 64.0) * n4 + (1.0 / 256.0) * n6); // 2*pi*A is the circumference of a meridian

            double eta = x / (k0 * A);
            double xi = y / (k0 * A);

            double[] beta = new double[] {0.0, // note beta is one-based array (6th order Krüger expressions)
										(1.0 / 2.0) * n - (2.0 / 3.0) * n2 + (37.0 / 96.0) * n3 - (1.0 / 360.0) * n4 - (81.0 / 512.0) * n5 + (96199.0 / 604800.0) * n6,
                                                (1.0 / 48.0) * n2 + (1.0 / 15.0) * n3 - (437.0 / 1440.0) * n4 + (46.0 / 105.0) * n5 - (1118711.0 / 3870720.0) * n6,
                                                        (17.0 / 480.0) * n3 - (37.0 / 840.0) * n4 - (209.0 / 4480.0) * n5 + (5569.0 / 90720.0) * n6,
                                                                    (4397.0 / 161280.0) * n4 - (11.0 / 504.0) * n5 - (830251.0 / 7257600.0) * n6,
                                                                                (4583 / 161280) * n5 - (108847.0 / 3991680.0) * n6,
                                                                                                (20648693.0 / 638668800.0) * n6 };

            double xiPrime = xi;
            for (int j = 1; j <= 6; j++)
                xiPrime -= beta[j] * Math.Sin(2.0 * j * xi) * Math.Cosh(2.0 * j * eta);

            double etaPrime = eta;
            for (int j = 1; j <= 6; j++)
                etaPrime -= beta[j] * Math.Cos(2.0 * j * xi) * Math.Sinh(2.0 * j * eta);

            double sinhEtaPrime = Math.Sinh(etaPrime);
            double sinXiPrime = Math.Sin(xiPrime), cosXiPrime = Math.Cos(xiPrime);

            double tauPrime = sinXiPrime / Math.Sqrt(sinhEtaPrime * sinhEtaPrime + cosXiPrime * cosXiPrime);

            double deltaTauI = 0.0;
            double tauI = tauPrime;

            do
            {
                double sigmaI = Math.Sinh(e * Atanh(e * tauI / Math.Sqrt(1.0 + tauI * tauI)));
                double tauIPrime = tauI * Math.Sqrt(1 + sigmaI * sigmaI) - sigmaI * Math.Sqrt(1.0 + tauI * tauI);
                deltaTauI = (tauPrime - tauIPrime) / Math.Sqrt(1.0 + tauIPrime * tauIPrime)
                    * (1.0 + (1.0 - WGS_e2) * tauI * tauI) / ((1 - WGS_e2) * Math.Sqrt(1.0 + tauI * tauI));
                tauI += deltaTauI;
            } while (Math.Abs(deltaTauI) > 1e-12); // using IEEE 754 deltaTauI -> 0 after 2-3 iterations
                                                   // note relatively large convergence test as deltaTauI toggles on ±1.12e-16 for eg 31 N 400000 5000000
            double tau = tauI;
            double phi = Math.Atan(tau);
            double lambda = Math.Atan2(sinhEtaPrime, cosXiPrime);

            // ---- convergence: Karney 2011 Eq 26, 27

            double p = 1.0;
            for (int j = 1; j <= 6; j++)
                p -= 2.0 * j * beta[j] * Math.Cos(2.0 * j * xi) * Math.Cosh(2.0 * j * eta);

            double q = 0.0;
            for (int j = 1; j <= 6; j++)
                q += 2.0 * j * beta[j] * Math.Sin(2.0 * j * xi) * Math.Sinh(2.0 * j * eta);

            double gammaPrime = Math.Atan(Math.Tan(xiPrime) * Math.Tanh(etaPrime));
            double gammaPrimePrime = Math.Atan2(q, p);

            double gamma = gammaPrime + gammaPrimePrime;

            // ---- scale: Karney 2011 Eq 28

            double sinPhi = Math.Sin(phi);
            double kappaPrime = Math.Sqrt(1.0 - e * e * sinPhi * sinPhi) * Math.Sqrt(1.0 + tau * tau) * Math.Sqrt(sinhEtaPrime * sinhEtaPrime + cosXiPrime * cosXiPrime);
            double kappaPrimePrime = (A / WGS_a) * (1.0 / Math.Sqrt(p * p + q * q));

            double kappa = k0 * kappaPrime * kappaPrimePrime;

            // ------------

            double lambda0 = ((utm.Zone - 1.0) * 6.0 - 180.0 + 3.0) * degToRad; // longitude of central meridian
            lambda += lambda0; // move lambda from zonal to global coordinates

            // round to reasonable precision
            double lat = phi * radToDeg; // nm precision (1nm = 10^-11°)
            double lon = lambda * radToDeg; // (strictly lat rounding should be phi⋅cosPhi!)
            double convergence = gamma * radToDeg;
            double scale = kappa * radToDeg;

            return new SimPoint3D(lon, lat, utm.Height);
        }

        /// <summary>
        /// Creates a grid that is algined along the longitude(x)/latitude(y) axes.
        /// The grid is centered around midPoint and spans the area [widthInM, heightInM] with grid lines inserted every spacingX/spacingY meters.
        /// If spacing does not fit an integer times into any dimension, the resulting grid is cropped to floor(dim/spacing) in the respective dimension.
        /// </summary>
        /// <param name="midPoint">Center point in WGS coordinates</param>
        /// <param name="widthInM">Total width of the grid in meters</param>
        /// <param name="heightInM">Total height </param>
        /// <param name="spacingX"></param>
        /// <param name="spacingY"></param>
        /// <param name="toCartesian">if true, WGS coordinates are converted into Cartesian coordinates</param>
        /// <returns></returns>
        public static SimPoint3D[,] CreateAlignedWGSGrid(SimPoint3D midPoint, double widthInM, double heightInM, double spacingX, double spacingY, bool toCartesian = false)
        {
            const int MIN_VERTICES = 3;
            const int MAX_VERTICES = 1000;

            int numVerticesX = Math.Min(MAX_VERTICES, Math.Max(MIN_VERTICES, (int)Math.Floor(widthInM / spacingX)));
            int numVerticesY = Math.Min(MAX_VERTICES, Math.Max(MIN_VERTICES, (int)Math.Floor(heightInM / spacingY)));

            SimPoint3D[,] vertices = new SimPoint3D[numVerticesX, numVerticesY];

            for (int i = 0; i < numVerticesX; i++)
            {
                for (int j = 0; j < numVerticesY; j++)
                {
                    int x = i - numVerticesX / 2;
                    int y = j - numVerticesY / 2;

                    if (x == 0 && y == 0)
                    {
                        vertices[i, j] = toCartesian ? VertexAlgorithms.FromMathematicalCoordinateSystem(WGS84ToCart(midPoint)) : midPoint;
                    }
                    else
                    {
                        double azimuthX = x < 0 ? 270.0 : 90.0;
                        double azimuthY = y < 0 ? 0.0 : 180.0;

                        (SimPoint3D v_x, _) = GeoReferenceAlgorithms.VincentyDirect(midPoint, azimuthX, Math.Abs(spacingX * x));
                        (SimPoint3D v, _) = GeoReferenceAlgorithms.VincentyDirect(v_x, azimuthY, Math.Abs(spacingY * y));

                        vertices[i, j] = toCartesian ? VertexAlgorithms.FromMathematicalCoordinateSystem(WGS84ToCart(v)) : v;
                    }
                }
            }

            return vertices;
        }

        /// <summary>
        /// Computes the arctangent hyperbolicus using its logarithm form.
        /// </summary>
        /// <param name="X">Parameter</param>
        /// <returns>atanh(x)</returns>
        private static double Atanh(double X)
        {
            return Math.Log((1.0 + X) / (1.0 - X)) / 2.0;
        }

        /// <summary>
        /// Converts building geometry with GeoReferences to a "flat" cartesian frame centered around wgsOrigin with 
        ///  X containing the East direction, 
        ///  Y containing the height
        ///  Z containing the North direction
        ///  
        /// The method assumes that Longitude and Latitude form a cartesian frame which should hold as long as the affected area is small and the area
        /// is not too close to a pole.
        /// This algorithm may not be seen as a proof that earth is actually flat!
        /// </summary>
        /// <param name="wgsCoordinates">
        /// The coordinates in WGS coordinates as returned by <see cref="EstimateWGSNonLinear(List{SimPoint3D}, List{GeoRefPoint})"/>
        /// (X: Longitude, Y: Latitude, Z: height)
        /// </param>
        /// <param name="wgsOrigin">The origin of the reference system, given in WGS coordinates (X: Longitude, Y: Latitude, Z: height)</param>
        /// <returns>The wgsCoordinates convert to a cartesian frame centered around wgsOrigin</returns>
        public static IEnumerable<SimPoint3D> ConvertToFlatEarth(IEnumerable<SimPoint3D> wgsCoordinates, SimPoint3D wgsOrigin)
        {
            foreach (var point in wgsCoordinates)
            {
                var pointOnEllipsoide = new SimPoint3D(point.X, point.Y, wgsOrigin.Z); //Project point onto the Ellipsoid height defined by wgsOrigin
                var wgsDir = GeoReferenceAlgorithms.VincentyIndirect(wgsOrigin, pointOnEllipsoide);
                var flatCoordinate = new SimPoint3D(Math.Sin(wgsDir.bearing * degToRad) * wgsDir.distance,
                    point.Z - wgsOrigin.Z,
                    Math.Cos(wgsDir.bearing * degToRad) * wgsDir.distance);
                yield return flatCoordinate;
            }
        }

        /// <summary>
        /// Computes the signed angle (in counter clockwise direction) between two vectors in 3D.
        /// </summary>
        /// <param name="v1">vector 1</param>
        /// <param name="v2">vector 2</param>
        /// <param name="n">The normal vector around which the rotation happens.</param>
        /// <param name="epsilon">Tolerance for determining if the two vectors point in the same direction (in radians)</param>
        /// <returns>Signed angle between two 3D vectors in the range [-pi, pi].</returns>
        public static double SignedAngle(SimVector3D v1, SimVector3D v2, SimVector3D n, double epsilon = 0.001)
        {
            v1.Normalize();
            v2.Normalize();

            var dot = SimVector3D.DotProduct(v1, v2);
            if (dot > 1 - epsilon) // point in the same direction
                return 0;
            if (dot < -1 + epsilon) // point in opposite direction
                return Math.PI;
            var cross = SimVector3D.CrossProduct(v1, v2);
            cross.Normalize();
            var dotAxis = SimVector3D.DotProduct(n, cross);

            var angle = Math.Acos(dot) * dotAxis;

            return angle;
        }

        /// <summary>
        /// Wraps around WGS so its in [-180,180];[-90,90] again
        /// </summary>
        /// <param name="point">The WGS point</param>
        /// <returns>The normalized point</returns>
        public static SimPoint3D NormalizeWGSCoordinate(SimPoint3D point)
        {
            var lng = point.X;
            var lat = point.Y;
            var ele = point.Z;

            if (lng < -180.0)
                lng += 360.0;
            else if (lng > 180.0)
                lng -= 360.0;
            if (lat < -90.0)
                lat += 180;
            else if (lat > 90)
                lat -= 180;
            return new SimPoint3D(lng, lat, ele);
        }
    }
}
