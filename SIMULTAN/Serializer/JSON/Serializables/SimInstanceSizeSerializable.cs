using SIMULTAN.Data.Components;
using SIMULTAN.Data.SimMath;
using System;
using System.Text.Json.Serialization;

namespace SIMULTAN.Serializer.JSON
{
    /// <summary>
    /// Serializable class for the <see cref="SimInstanceSize"/>
    /// </summary>
    public class SimInstanceSizeSerializable
    {
        /// <summary>
        /// Minimum
        /// </summary>
        public SimVector3DSerializable Min { get; set; }
        /// <summary>
        /// Maximum
        /// </summary>
        public SimVector3DSerializable Max { get; set; }

        /// <summary>
        /// Creates a new instance of SimInstanceSizeSerializable
        /// </summary>
        /// <param name="instanceSize"></param>
        public SimInstanceSizeSerializable(SimInstanceSize instanceSize)
        {
            this.Min = new SimVector3DSerializable(instanceSize.Min);
            this.Max = new SimVector3DSerializable(instanceSize.Max);
        }

        //DO NOT USE. Only required for the XMLSerializer class to operate on this type
        private SimInstanceSizeSerializable() { throw new NotImplementedException(); }
    }
}
