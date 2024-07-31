using SIMULTAN.Data.SimNetworks;
using SIMULTAN.Serializer.DXF;
using SIMULTAN.Data.SimMath;
using System;

namespace SIMULTAN.Serializer.JSON
{
    /// <summary>
    /// Serializable class for the <see cref="SimNetworkConnector"/>
    /// </summary>
    public class SimNetworkConnectorSerializable
    {
        /// <summary>
        /// The ID of the Source SimNetworkPort
        /// </summary>
        public long Source { get; set; }
        /// <summary>
        /// The ID of the Target SimNetworkPort
        /// </summary>
        public long Target { get; set; }
        /// <summary>
        /// Color of the SimNetworkConnector
        /// </summary>
        public string Color { get; set; }
        /// <summary>
        /// Creates a new instance of the SimNetworkConnectorSerializable
        /// </summary>
        /// <param name="connector">The connector in the network which is serialized</param>
        public SimNetworkConnectorSerializable(SimNetworkConnector connector)
        {
            this.Source = connector.Source.LocalID;
            this.Target = connector.Target.LocalID;
            this.Color = DXFDataConverter<SimColor>.P.ToDXFString(connector.Color);
        }

        //DO NOT USE. Only required for the XMLSerializer class to operate on this type
        private SimNetworkConnectorSerializable() { throw new NotImplementedException(); }
    }
}
