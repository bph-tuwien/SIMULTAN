using SIMULTAN.Data.Components;
using SIMULTAN.Data.SimNetworks;
using System;

namespace SIMULTAN.Serializer.JSON
{

    /// <summary>
    /// Serializable class for the <see cref="SimInstancePlacementSimNetwork"/>
    /// </summary>
    public class SimInstancePlacementSimNetworkSerializable : SimInstancePlacementSerializable
    {
        /// <summary>
        /// Id of the network itself
        /// </summary>
        public long NetworkId { get; set; }
        /// <summary>
        /// ID of the network
        /// </summary>
        public long NetworkElementId { get; set; }


        /// <summary>
        /// Creates a new instance of the SimInstancePlacementSimNetworkSerializable
        /// </summary>
        /// <param name="placement">The placement</param>
        public SimInstancePlacementSimNetworkSerializable(SimInstancePlacementSimNetwork placement)
        {
            this.InstanceType = placement.InstanceType.ToString();

            if (placement.NetworkElement is SimNetworkPort port)
            {
                var rootNW = JSONExportHelpers.GetRootNetwork(port.ParentNetworkElement);
                if (rootNW != null)
                {
                    this.NetworkId = rootNW.LocalID;
                }

            }
            if (placement.NetworkElement is SimNetworkBlock block)
            {
                var rootNW = JSONExportHelpers.GetRootNetwork(block);
                if (rootNW != null)
                {
                    this.NetworkId = rootNW.LocalID;
                }
            }
            this.NetworkElementId = placement.NetworkElement.Id.LocalId;
        }

        //DO NOT USE. Only required for the XMLSerializer class to operate on this type
        private SimInstancePlacementSimNetworkSerializable() { throw new NotImplementedException(); }
    }
}
