using SIMULTAN.Data.Geometry;
using SIMULTAN.Data.SimNetworks;
using System.Collections.ObjectModel;

namespace SIMULTAN.Exchange.SimNetworkConnectors
{
    /// <summary>
    /// Base class for the connector of a <see cref="SimNetworkBlock"/> and a <see cref="SimNetwork"/>
    /// </summary>
    internal abstract class SimNetworkBaseNetworkElementConnector : BaseSimNetworkGeometryConnector
    {
        /// <summary>
        /// The vertex
        /// </summary>
        internal Vertex Vertex { get; set; }
        internal override BaseGeometry Geometry => Vertex;

        /// <summary>
        /// The network element represented by this connector (it is either a <see cref="SimNetworkBlock"/> and a <see cref="SimNetwork"/>)
        /// </summary>
        internal abstract BaseSimNetworkElement NetworkElement { get; }

        /// <inheritdoc />

        internal override void ChangeBaseGeometry(BaseGeometry geometry)
        {
        }

        internal override void OnGeometryChanged()
        {
        }

        internal override void OnTopologyChanged()
        {
        }
    }
}
