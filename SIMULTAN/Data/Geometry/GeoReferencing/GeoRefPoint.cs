using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SIMULTAN.Data.SimMath;

namespace SIMULTAN.Data.Geometry
{
    /// <summary>
    /// Class for representing a georeferenced point in 3D
    /// </summary>
    public struct GeoRefPoint
    {
        /// <summary>
        /// Point in object space
        /// </summary>
        public SimPoint3D OS { get; private set; }

        /// <summary>
        /// Point in WGS84 space (long/lat/height)
        /// </summary>
        public SimPoint3D WGS { get; private set; }

        /// <summary>
        /// Initializes a new instance of the class GeoRefPoint
        /// </summary>
        /// <param name="OS">Position in object space</param>
        /// <param name="WGS">Position in WGS84 space (long/lat/height)</param>
        public GeoRefPoint(SimPoint3D OS, SimPoint3D WGS)
        {
            this.OS = OS;
            this.WGS = WGS;
        }
    }
}
