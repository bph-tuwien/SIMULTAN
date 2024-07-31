using SIMULTAN.Data.SimNetworks;
using SIMULTAN.Serializer.DXF;
using System.Collections.Generic;
using System.Windows;
using SIMULTAN.Data.SimMath;
using System;

namespace SIMULTAN.Serializer.JSON
{

    /// <summary>
    /// Serializable class for the <see cref="SimNetworkBlock"/>
    /// </summary>
    public class SimNetworkBlockSerializable
    {
        /// <summary>
        /// ID of the SimNetworkBlock
        /// </summary>
        public long Id { get; set; }
        /// <summary>
        /// Name of the SimNetworkBlock
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Color of the SimNetworkBlock
        /// </summary>
        public string Color { get; set; }
        /// <summary>
        /// 2D Position of the block in the network
        /// </summary>
        public SimPointSerializable Position { get; set; }
        /// <summary>
        /// Ports of the SimNetworkBlock
        /// </summary>
        public List<SimNetworkPortSerializable> Ports { get; set; } = new List<SimNetworkPortSerializable>();
        
        /// <summary>
        /// Creates a new instance of SimNetworkBlockSerializable
        /// </summary>
        /// <param name="block">The created instance</param>
        public SimNetworkBlockSerializable(SimNetworkBlock block)
        {
            this.Id = block.LocalID;
            this.Name = DXFDataConverter<string>.P.ToDXFString(block.Name);
            this.Color = DXFDataConverter<SimColor>.P.ToDXFString(block.Color);
            this.Position = new SimPointSerializable(block.Position);

            for (int i = 0; i < block.Ports.Count; i++)
            {
                this.Ports.Add(new SimNetworkPortSerializable(block.Ports[i]));
            }
        }


        //DO NOT USE. Only required for the XMLSerializer class to operate on this type
        private SimNetworkBlockSerializable() { throw new NotImplementedException(); }
    }
}
