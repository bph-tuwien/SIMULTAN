using SIMULTAN.Data.Components;
using SIMULTAN.Data.FlowNetworks;
using SIMULTAN.Data.Geometry;
using SIMULTAN.Exchange.ConnectorInteraction;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace SIMULTAN.Exchange.Connectors
{
    /// <summary>
    /// Manages the connection of a <see cref="SimComponentInstance"/> to the <see cref="BaseGeometry"/> it is 
    /// attached to (e.g., a fan coil in a space represented by a volume.
    /// </summary>
    internal abstract class ConnectorAttachedToBase : ConnectorBase
    {
        #region PROPERTIES: connection

        /// <summary>
        /// The source component instance of the connection. Holds position or path data
        /// extracted from the geometry.
        /// </summary>
        public SimComponentInstance AttachedSource { get; }

        /// <summary>
        /// The connector that holds the container of the <see cref="AttachedSource"/> within a network (see <see cref="SimFlowNetwork"/>).
        /// </summary>
        public ConnectorRepresentativeToBase ContainingConnector { get; }

        #endregion


        #region .CTOR

        /// <summary>
        /// Initializes an instance of the class ConnectorAttachedToBase.
        /// </summary>
        /// <param name="_comm_manager">the manager, initializing this instance</param>
        /// <param name="_source_parent_comp">the parent component of the source instance</param>
        /// <param name="_containing_connector">the connector that holds the container of the source</param>
        /// <param name="_source">the component instance that is being attached</param>
        /// <param name="_index_of_geometry_model">the index of the <see cref="GeometryModelData"/> where the geometry resides</param>
        /// <param name="_target_geometry">the geometry to which the source is attached</param>
        public ConnectorAttachedToBase(ComponentGeometryExchange _comm_manager, SimComponent _source_parent_comp,
                                        ConnectorRepresentativeToBase _containing_connector, SimComponentInstance _source,
                                        int _index_of_geometry_model, BaseGeometry _target_geometry)
            : base(_comm_manager, _source_parent_comp, _index_of_geometry_model, _target_geometry)
        {
            this.AttachedSource = _source;

            //Check if geometry placement exists (there may only be one)
            if (this.AttachedSource.Placements.Count == 0)
            {
                this.AttachedSource.Placements.Add(
                    new SimInstancePlacementGeometry(_index_of_geometry_model, _target_geometry.Id, Connector.GetRelatedGeometryIds(_target_geometry)));
            }
            else if (this.AttachedSource.Placements[0] is SimInstancePlacementGeometry gp)
            {
                gp.FileId = _index_of_geometry_model;
                gp.GeometryId = _target_geometry.Id;
                gp.RelatedIds = Connector.GetRelatedGeometryIds(_target_geometry);
            }

            this.AttachedSource.State = new SimInstanceState(true, SimInstanceConnectionState.Ok);
            foreach (var pl in this.AttachedSource.Placements.OfType<SimInstancePlacementGeometry>())
            {
                if (pl.FileId == _index_of_geometry_model && pl.GeometryId == _target_geometry.Id)
                    pl.State = SimInstancePlacementState.Valid;
            }

            this.SyncState = SynchronizationState.SYNCHRONIZED;
            this.ContainingConnector = _containing_connector;
        }

        #endregion

        #region METHODS

        internal void AdoptPosition(Point3D _position)
        {
            if (this.ContainingConnector != null &&
                this.AttachedSource.State.IsRealized &&
                this.AttachedSource.State.ConnectionState == SimInstanceConnectionState.Ok)
            {
                using (AccessCheckingDisabler.Disable(this.AttachedSource.Component.Factory))
                {
                    ConnectorAlgorithms.TransferPositionToInstance(_position, this.AttachedSource);
                }
            }
        }

        internal void AdoptPath(IEnumerable<Point3D> _path)
        {
            if (this.ContainingConnector != null &&
                this.AttachedSource.State.IsRealized &&
                this.AttachedSource.State.ConnectionState == SimInstanceConnectionState.Ok)
            {
                using (AccessCheckingDisabler.Disable(this.AttachedSource.Component.Factory))
                {
                    this.AttachedSource.InstancePath = _path.ToList();
                }
            }
        }

        #endregion

        #region EVENT HANDLERS

        #endregion
    }
}
