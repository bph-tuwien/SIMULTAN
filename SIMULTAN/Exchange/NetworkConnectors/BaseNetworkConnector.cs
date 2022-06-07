using SIMULTAN.Data.Components;
using SIMULTAN.Data.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Exchange.NetworkConnectors
{
    /// <summary>
    /// Base class for all network element connectors
    /// </summary>
    abstract internal class BaseNetworkConnector
    {
        /// <summary>
        /// The geometry associated with the network element
        /// </summary>
        internal abstract BaseGeometry Geometry { get; }

        /// <summary>
        /// Called when a <see cref="GeometryModelData.GeometryChanged"/> event has been called with <see cref="Geometry"/>
        /// in the argument list
        /// </summary>
        internal abstract void OnGeometryChanged();
        /// <summary>
        /// Called when a <see cref="GeometryModelData.TopologyChanged"/> event has been called with <see cref="Geometry"/>
        /// in the argument list
        /// </summary>
        internal abstract void OnTopologyChanged();

        /// <summary>
        /// Called when the geometry has to be changed. Can, e.g., be caused by replacing the geometrymodel data
        /// </summary>
        /// <param name="geometry"></param>
        internal abstract void ChangeBaseGeometry(BaseGeometry geometry);

        /// <summary>
        /// Called when the connector should be deleted. Has to free all ressources and disconnect all events
        /// </summary>
        public virtual void Dispose() { }
    }
}
