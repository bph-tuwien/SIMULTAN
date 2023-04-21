using SIMULTAN.Data.Geometry;

namespace SIMULTAN.Exchange.SimNetworkConnectors
{
    /// <summary>
    /// Abstract class for the geometry connectors of a SimNetwork
    /// </summary>
    public abstract class BaseSimnetworkGeometryConnector
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
