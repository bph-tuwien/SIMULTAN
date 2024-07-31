using SIMULTAN.Data.SimNetworks;
using SIMULTAN.Serializer.DXF;
using SIMULTAN.Data.SimMath;
using System;

namespace SIMULTAN.Serializer.JSON
{
    /// <summary>
    /// Serializable class for the <see cref="SimNetworkPort"/>
    /// </summary>
    public class SimNetworkPortSerializable
    {
        /// <summary>
        /// ID of the SimNetworkPort
        /// </summary>
        public long Id { get; set; }
        /// <summary>
        /// Name of the SimNetworkPort
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Color of the SimNetworkPort
        /// </summary>
        public string Color { get; set; }
        /// <summary>
        /// Type of the SimNetworkPort <see cref="SIMULTAN.Data.SimNetworks.PortType"/>
        /// </summary>
        public string PortType { get; set; }

        /// <summary>
        /// Creates a new instance of SimNetworkPortSerializable
        /// </summary>
        /// <param name="port">The created instance</param>
        public SimNetworkPortSerializable(SimNetworkPort port)
        {
            this.Id = port.Id.LocalId;
            this.Name = DXFDataConverter<string>.P.ToDXFString(port.Name);
            this.Color = DXFDataConverter<SimColor>.P.ToDXFString(port.Color);
            this.PortType = port.PortType.ToString();
        }

        //DO NOT USE. Only required for the XMLSerializer class to operate on this type
        private SimNetworkPortSerializable() { throw new NotImplementedException(); }
    }
}

