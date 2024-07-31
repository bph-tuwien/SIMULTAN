using SIMULTAN.Data.SimMath;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Serializer.JSON
{
    /// <summary>
    /// Serializable representation of a <see cref="SimQuaternion"/>
    /// </summary>
    public class SimQuaternionSerializable
    {
        /// <summary>
        /// X Coordinate of the Quaternion
        /// </summary>
        public double X { get; set; }
        /// <summary>
        /// Y Coordinate of the Quaternion
        /// </summary>
        public double Y { get; set; }
        /// <summary>
        /// Z Coordinate of the Quaternion
        /// </summary>
        public double Z { get; set; }
        /// <summary>
        /// W Coordinate of the Quaternion
        /// </summary>
        public double W { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SimQuaternionSerializable"/> class
        /// </summary>
        /// <param name="vector">The initial quaternion</param>
        public SimQuaternionSerializable(SimQuaternion vector)
        {
            X = vector.X;
            Y = vector.Y;
            Z = vector.Z;
            W = vector.W;
        }

        //DO NOT USE. Only required for the XMLSerializer class to operate on this type
        private SimQuaternionSerializable() { throw new NotImplementedException(); }
    }
}
