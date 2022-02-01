using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.Geometry
{
    /// <summary>
    /// Class that stores a single coordinate in UTM format
    /// </summary>
    public struct UTMCoord
    {
        /// <summary>
        /// Zone, must be in [1, 60]
        /// </summary>
        public int Zone;
        /// <summary>
        /// Flag whether point is on northern hemisphere
        /// </summary>
        public bool NorthernHemisphere;
        /// <summary>
        /// Easting
        /// </summary>
        public double Easting;
        /// <summary>
        ///  Northing
        /// </summary>
        public double Northing;
        /// <summary>
        /// Height in meter
        /// </summary>
        public double Height;
    }
}
