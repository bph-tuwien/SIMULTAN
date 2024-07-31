using SIMULTAN.Data.Components;
using System;

namespace SIMULTAN.Serializer.JSON
{
    /// <summary>
    /// Serializable class for the <see cref="SimInstancePlacementNetwork"/>
    /// </summary>
    public class SimInstancePlacementNetworkSerializable : SimInstancePlacementSerializable
    {
        /// <summary>
        /// Id of the network element
        /// </summary>
        public long NetworkElementId { get; set; }
        /// <summary>
        /// Id of the network itself
        /// </summary>
        public long NetworkId { get; set; }

        /// <summary>
        /// Creates a new instance of SimInstancePlacementNetworkSerializable
        /// </summary>
        /// <param name="placement">The created instance</param>
        public SimInstancePlacementNetworkSerializable(SimInstancePlacementNetwork placement)
        {
            this.InstanceType = placement.InstanceType.ToString();
            this.NetworkId = placement.NetworkElement.Network.ID.LocalId;
            this.NetworkElementId = placement.NetworkElement.ID.LocalId;
        }

        //DO NOT USE. Only required for the XMLSerializer class to operate on this type
        private SimInstancePlacementNetworkSerializable() { throw new NotImplementedException(); }
    }
}
