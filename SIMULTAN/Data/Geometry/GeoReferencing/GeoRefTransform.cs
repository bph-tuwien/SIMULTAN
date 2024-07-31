using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SIMULTAN.Data.SimMath;

namespace SIMULTAN.Data.Geometry
{
    /// <summary>
    /// Holds all parameters necessary to interpolate the WGS84 coordinates for an unreferenced point in object space
    /// </summary>
    public class GeoRefTransform
    {
        /// <summary>
        /// Origin of coordinate system used to interpolate and transform
        /// </summary>
        public GeoRefPoint RefOrigin { get; private set; }

        /// <summary>
        /// First point (in tangent direction) of coordinate system used to interpolate and transform
        /// </summary>
        public GeoRefPoint RefP1 { get; private set; }

        /// <summary>
        /// Second point (in bitangent direction) of coordinate system used to interpolate and transform
        /// </summary>
        public GeoRefPoint RefP2 { get; private set; }

        /// <summary>
        /// Azimuth of direction between origin and P1
        /// </summary>
        public double Azimuth1 { get; private set; }

        /// <summary>
        /// Azimuth of direction between origin and P2
        /// </summary>
        public double Azimuth2 { get; private set; }

        /// <summary>
        /// Initializes a new instance of class GeoRefTransform and computes intrinsic parameters
        /// </summary>
        /// <param name="origin">Origin of the coordinate system</param>
        /// <param name="P1">First point (along tangent) of the coordinate system</param>
        /// <param name="P2">Second point (along bitangent) of the coordinate system</param>
        public GeoRefTransform(GeoRefPoint origin, GeoRefPoint P1, GeoRefPoint P2)
        {
            this.RefOrigin = origin;
            this.RefP1 = P1;
            this.RefP2 = P2;

            // compute 2 directions in WGS space with a shared origin and their azimuth (angle to projected north), use average height for origin
            SimPoint3D originWGS = new SimPoint3D(RefOrigin.WGS.X, RefOrigin.WGS.Y, RefOrigin.WGS.Z);
            originWGS.Z = (1.0 / 3.0) * (RefOrigin.WGS.Z + RefP1.WGS.Z + RefP2.WGS.Z);
            double d1, d2;
            (d1, Azimuth1) = GeoReferenceAlgorithms.VincentyIndirect(originWGS, RefP1.WGS);
            (d2, Azimuth2) = GeoReferenceAlgorithms.VincentyIndirect(originWGS, RefP2.WGS);
        }
    }
}
