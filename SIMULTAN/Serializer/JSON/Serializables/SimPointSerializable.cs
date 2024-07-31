using SIMULTAN.Data.SimMath;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Serializer.JSON
{
    /// <summary>
    /// Serializable representation of a <see cref="SimVector3D"/>
    /// </summary>
    public class SimPointSerializable
    {
        /// <summary>
        /// X Coordinate of the Vector
        /// </summary>
        public double X { get; set; }
        /// <summary>
        /// Y Coordinate of the Vector
        /// </summary>
        public double Y { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SimVector3DSerializable"/> class
        /// </summary>
        /// <param name="point">The initial vector</param>
        public SimPointSerializable(SimPoint point)
        {
            X = point.X;
            Y = point.Y;
        }

        //DO NOT USE. Only required for the XMLSerializer class to operate on this type
        private SimPointSerializable() { throw new NotImplementedException(); }
    }
}
