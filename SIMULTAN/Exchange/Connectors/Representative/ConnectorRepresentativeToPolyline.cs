using SIMULTAN.Data.Components;
using SIMULTAN.Data.FlowNetworks;
using SIMULTAN.Data.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace SIMULTAN.Exchange.Connectors
{
    /// <summary>
    /// Manages the connection of a network EDGE container to its geometric representation.
    /// </summary>
    internal class ConnectorRepresentativeToPolyline : ConnectorRepresentativeToBase
    {
        #region .CTOR

        /// <summary>
        /// Initializes an instance of the class ConnectorRepresentativeToPolyline.
        /// </summary>
        /// <param name="_comm_manager">the manager, initializing this instance</param>
        /// <param name="_network_connector">the connector of the network containing the represented container</param>
        /// <param name="_source_edge">the network edge whose content is being represented</param>
        /// <param name="_index_of_geometry_model">the index of the <see cref="GeometryModelData"/> where the polyline resides</param>
        /// <param name="_target_polyline">the target polyline that represents the source</param>
        public ConnectorRepresentativeToPolyline(ComponentGeometryExchange _comm_manager,
                                             ConnectorToGeometryModel _network_connector, SimFlowNetworkEdge _source_edge,
                                             int _index_of_geometry_model, Polyline _target_polyline)
            : base(_comm_manager, _network_connector, _source_edge, _index_of_geometry_model, _target_polyline)
        {
            this.SynchronizeSourceWTarget(_target_polyline);
        }

        #endregion

        #region METHOD OVERRIDES

        /// <inheritdoc/>
        protected override bool SynchTargetIsAdmissible(BaseGeometry _target)
        {
            return (_target is Polyline && this.TargetId == _target.Id);
        }

        /// <inheritdoc/>
        protected override void PassRepresentationInfoToInstance(BaseGeometry _representation)
        {
            Polyline pl_rep = _representation as Polyline;
            if (pl_rep == null) return;

            base.PassRepresentationInfoToInstance(_representation);

            List<Vertex> vertices = NetworkConverter.GetOrderedVerticesOfPolyline(pl_rep);
            List<Point3D> path = vertices.Select(x => x.Position).ToList();

            if (this.ChildConnector != null)
                this.ChildConnector.AdoptPath(path);
            else if (this.instance != null)
            {
                using (AccessCheckingDisabler.Disable(this.instance.Component.Factory))
                {
                    this.instance.InstancePath = path;
                }
            }
        }

        #endregion
    }
}
