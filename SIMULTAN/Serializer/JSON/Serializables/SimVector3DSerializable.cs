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
    public class SimVector3DSerializable
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
        /// Z Coordinate of the Vector
        /// </summary>
        public double Z { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SimVector3DSerializable"/> class
        /// </summary>
        /// <param name="vector">The initial vector</param>
        public SimVector3DSerializable(SimVector3D vector)
        {
            X = vector.X;
            Y = vector.Y;
            Z = vector.Z;
        }

        //DO NOT USE. Only required for the XMLSerializer class to operate on this type
        private SimVector3DSerializable() { throw new NotImplementedException(); }
    }
}
