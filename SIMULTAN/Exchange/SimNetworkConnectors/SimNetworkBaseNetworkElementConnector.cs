using SIMULTAN.Data.Geometry;
using SIMULTAN.Data.SimNetworks;
using System.Collections.ObjectModel;

namespace SIMULTAN.Exchange.SimNetworkConnectors
{
    /// <summary>
    /// Base class for the connector of a SimNetworkBlock and a SimNetwork <see cref="SimNetworkBlock"/> <see cref="SimNetwork"/>
    /// </summary>
    internal abstract class SimNetworkBaseNetworkElementConnector : BaseSimnetworkGeometryConnector
    {

        /// <summary>
        /// The vertex
        /// </summary>
        internal Vertex Vertex { get; set; }
        internal override BaseGeometry Geometry => Vertex;

        /// <summary>
        /// The network element represented by this connector (it is either a SimNetworkBlock or a SimNetwork)
        /// </summary>
        internal abstract BaseSimNetworkElement NetworkElement { get; }

        /// <summary>
        /// Port connectors
        /// </summary>
        internal ObservableCollection<SimNetworkPortConnector> PortConnectors = new ObservableCollection<SimNetworkPortConnector>();
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
