using SIMULTAN.Data.Components;
using SIMULTAN.Data.SimNetworks;
using SIMULTAN.Serializer.DXF;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Windows;
using SIMULTAN.Data.SimMath;
using System;

namespace SIMULTAN.Serializer.JSON
{
    /// <summary>
    /// Serializable class for the <see cref="SimNetwork"/>
    /// </summary>
    public class SimNetworkSerializable
    {
        /// <summary>
        /// The ID of SimNetwork
        /// </summary>
        public long Id { get; set; }
        /// <summary>
        /// The name of the SimNetwork
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Color of the SimNetwork
        /// </summary>
        public string Color { get; set; }
        /// <summary>
        /// 2D position of the SimNetwork
        /// </summary>
        public SimPointSerializable Position { get; set; }
        /// <summary>
        /// Contained subnetworks
        /// </summary>
        public List<SimNetworkSerializable> Subnetworks { get; set; } = new List<SimNetworkSerializable>();
        /// <summary>
        /// Contained SimNetworkBlocks/nodes
        /// </summary>
        public List<SimNetworkBlockSerializable> Blocks { get; set; } = new List<SimNetworkBlockSerializable>();
        /// <summary>
        /// Contained Connectors
        /// </summary>
        public List<SimNetworkConnectorSerializable> Connectors { get; set; } = new List<SimNetworkConnectorSerializable>();
        /// <summary>
        /// Contained Ports
        /// </summary>
        public List<SimNetworkPortSerializable> Ports { get; set; } = new List<SimNetworkPortSerializable>();

        /// <summary>
        /// JSON serializable of SimNetwork and returns the SimComponents which are assigned to any elements in the network
        /// </summary>
        /// <param name="network">The network to serialize</param>
        public SimNetworkSerializable(SimNetwork network)
        {
            this.Id = network.LocalID;
            this.Name = network.Name;
            this.Color = DXFDataConverter<SimColor>.P.ToDXFString(network.Color);
            this.Position = new SimPointSerializable(network.Position);

            for (int i = 0; i < network.Ports.Count; i++)
            {
                this.Ports.Add(new SimNetworkPortSerializable(network.Ports[i]));
            }
            for (int i = 0; i < network.ContainedElements.Count; i++)
            {
                if (network.ContainedElements[i] is SimNetwork nw)
                {
                    this.Subnetworks.Add(new SimNetworkSerializable(nw));
                }
                else if (network.ContainedElements[i] is SimNetworkBlock block)
                {
                    this.Blocks.Add(new SimNetworkBlockSerializable(block));
                }
            }
            for (int i = 0; i < network.ContainedConnectors.Count; i++)
            {
                this.Connectors.Add(new SimNetworkConnectorSerializable(network.ContainedConnectors[i]));
            }
        }

        /// <summary>
        /// Returns a list of all components associated with a network
        /// </summary>
        /// <param name="network">The network for which the components should be returned</param>
        /// <returns>A list of all components associated with the network</returns>
        public static List<SimComponent> GetComponentInstances(SimNetwork network)
        {
            var nwInstances = new List<SimComponent>();

            for (int i = 0; i < network.Ports.Count; i++)
            {
                if (network.Ports[i].ComponentInstance != null)
                {
                    nwInstances.Add(network.Ports[i].ComponentInstance.Component);
                }
            }
            for (int i = 0; i < network.ContainedElements.Count; i++)
            {
                if (network.ContainedElements[i] is SimNetwork nw)
                {
                    nwInstances.AddRange(GetComponentInstances(nw));
                }
                else if (network.ContainedElements[i] is SimNetworkBlock block)
                {
                    if (block.ComponentInstance != null)
                    {
                        nwInstances.Add(block.ComponentInstance.Component);
                    }
                }
            }
            return nwInstances;
        }

        //DO NOT USE. Only required for the XMLSerializer class to operate on this type
        private SimNetworkSerializable() { throw new NotImplementedException(); }
    }
}
