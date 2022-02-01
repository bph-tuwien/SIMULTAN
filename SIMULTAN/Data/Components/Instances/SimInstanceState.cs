using SIMULTAN.Serializer.DXF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.Components
{
    /// <summary>
    /// Defines the state an instance is in
    /// </summary>
    [DXFSerializerTypeNameAttribute("ParameterStructure.Instances.InstanceState")]
    public struct SimInstanceState
    {
        /// <summary>
        /// When set to True, the instance is realized.
        /// </summary>
        public bool IsRealized { get; }
        /// <summary>
        /// The connection state of the instance
        /// </summary>
        public SimInstanceConnectionState ConnectionState { get; }

        /// <summary>
        /// Initializes a new instance of the SimInstanceState class.
        /// The <see cref="ConnectionState"/> is set to <see cref="SimInstanceConnectionState.Ok"/>
        /// </summary>
        /// <param name="isRealized">When set to True, the instance is set to realized</param>
        public SimInstanceState(bool isRealized)
        {
            this.IsRealized = isRealized;
            this.ConnectionState = SimInstanceConnectionState.Ok;
        }
        /// <summary>
        /// Initializes a new instance of the SimInstanceState class.
        /// </summary>
        /// <param name="isRealized">When set to True, the instance is set to realized</param>
        /// <param name="connectionState">The connection state</param>
        public SimInstanceState(bool isRealized, SimInstanceConnectionState connectionState)
        {
            this.IsRealized = isRealized;
            this.ConnectionState = connectionState;
        }


        /// <inheritdoc />
        public override string ToString()
        {
            string realized = this.IsRealized ? "realisiert" : "leer";
            return realized;
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return this.IsRealized.GetHashCode() ^ this.ConnectionState.GetHashCode();
        }
        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return (obj is SimInstanceState) && (this == (SimInstanceState)obj);
        }
        /// <inheritdoc />
        public static bool operator ==(SimInstanceState lhs, SimInstanceState rhs)
        {
            return lhs.IsRealized == rhs.IsRealized && lhs.ConnectionState == rhs.ConnectionState;
        }
        /// <inheritdoc />
        public static bool operator !=(SimInstanceState lhs, SimInstanceState rhs)
        {
            return !(lhs == rhs);
        }
    }
}
