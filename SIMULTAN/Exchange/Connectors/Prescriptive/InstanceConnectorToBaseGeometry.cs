using SIMULTAN.Data.Components;
using SIMULTAN.Data.Geometry;
using SIMULTAN.Exchange.ConnectorInteraction;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Exchange.Connectors
{
    /// <summary>
    /// The base class for all instance connectors – i.e. those connecting a <see cref="SimComponentInstance"/> to geometry.
    /// Instances of this class' subclasses can be used to represent multiple placements of objects in a building (e.g. lamps).
    /// Defines a source <see cref="SimComponentInstance"/> and a parent <see cref="ConnectorBase"/>.
    /// Emits the <see cref="SimComponentInstance"/> event on deletion of the source (not used yet).
    /// Triggers synchronization in the parent connector instead of performing it itself.
    /// </summary>
    internal abstract class InstanceConnectorToBaseGeometry : ConnectorBase
    {
        #region PROPERTIES: Connection

        /// <summary>
        /// The source component instance of the connection (holds the data extracted from the geometry)
        /// </summary>
        public SimComponentInstance Source { get; protected set; }

        /// <summary>
        /// The connector that holds this one as a child. Its source is the component to which the 
        /// source Geometric Relationship belongs.
        /// </summary>
        public ConnectorBase ParentConnector { get; protected set; }

        #endregion

        #region PROPERTIES: Overrides

        /// <inheritdoc/>
        public override ConnectorConnectionState ConnState
        {
            get { return this.conn_state; }
            internal set
            {
                this.conn_state = value;
                // adapt the component's state
                if (this.Source != null)
                {
                    var pl = this.Source.Placements.FirstOrDefault(x => x is SimInstancePlacementGeometry pg &&
                        pg.FileId == this.TargetModelIndex && pg.GeometryId == this.TargetId);

                    SimInstanceState old = this.Source.State;
                    switch (this.conn_state)
                    {
                        case ConnectorConnectionState.TARGET_GEOMETRY_NULL:
                            this.Source.State = new SimInstanceState(old.IsRealized, SimInstanceConnectionState.GeometryDeleted);
                            if (pl != null)
                                pl.State = SimInstancePlacementState.InstanceTargetMissing;
                            break;
                        case ConnectorConnectionState.OK:
                            if (old.ConnectionState == SimInstanceConnectionState.GeometryDeleted)
                            {
                                this.Source.State = new SimInstanceState(old.IsRealized, SimInstanceConnectionState.Ok);
                                if (pl != null)
                                    pl.State = SimInstancePlacementState.InstanceTargetMissing;
                            }
                            else
                            {
                                this.Source.State = new SimInstanceState(old.IsRealized, SimInstanceConnectionState.Ok);
                                if (pl != null)
                                    pl.State = SimInstancePlacementState.Valid;
                            }
                            break;
                    }
                }
            }
        }

        #endregion

        #region .CTOR
        /// <summary>
        /// Initializes an object of type InstanceConnectorToBaseGeometry
        /// </summary>
        /// <param name="_comm_manager">the manager, initializing this instance</param>
        /// <param name="_parent_connector">the parent connector</param>
        /// <param name="_source_gr">the source component instance</param>
        /// <param name="_index_of_geometry_model">the index of the <see cref="GeometryModelData"/> where the geometry resides</param>
        /// <param name="_target_geometry">the target geometry</param>
        protected InstanceConnectorToBaseGeometry(ComponentGeometryExchange _comm_manager,
                                        ConnectorBase _parent_connector, SimComponentInstance _source_gr, int _index_of_geometry_model, BaseGeometry _target_geometry)
            : base(_comm_manager, ConnectorAlgorithms.GetConnectionSource(_parent_connector), _index_of_geometry_model, _target_geometry)
        {
            // TO ADAPT
            this.Source = _source_gr;
            this.Source.IsBeingDeleted += Source_IsBeingDeleted;
            this.ParentConnector = _parent_connector;
        }

        /// <summary>
        /// Emits the SourceIsBeingDeleted event.
        /// </summary>
        /// <param name="sender">the source Geometric Relationship</param>
        protected void Source_IsBeingDeleted(object sender)
        {
            this.OnSourceIsBeingDeleted();
        }

        internal void Reset()
        {
            this.Source.IsBeingDeleted -= Source_IsBeingDeleted;
            this.ParentConnector = null;
        }

        #endregion

        /// <inheritdoc/>
        public override string ToString()
        {
            string representation = "Instance Connector [";
            representation += (this.Source == null) ? "NULL -> " : this.Source.Name + " -> ";
            representation += this.TargetId.ToString() + "]";
            return representation;
        }

        /// <inheritdoc/>
        public override void SynchronizeSourceWTarget(BaseGeometry _target)
        {
            if (this.ParentConnector != null)
                this.ParentConnector.SynchronizeSourceWTarget(_target);
        }
    }

    /// <summary>
    /// Class for connecting a <see cref="SimComponentInstance"/> to a <see cref="Volume"/>.
    /// </summary>
    internal class InstanceConnectorToVolume : InstanceConnectorToBaseGeometry
    {
        internal InstanceConnectorToVolume(ComponentGeometryExchange _comm_manager,
                                        ConnectorBase _parent_connector, SimComponentInstance _source_gr, int _index_of_geometry_model, Volume _target_volume)
            : base(_comm_manager, _parent_connector, _source_gr, _index_of_geometry_model, _target_volume)
        {

        }

    }

    /// <summary>
    /// Class for connecting a <see cref="SimComponentInstance"/> to a <see cref="Face"/>.
    /// </summary>
    internal class InstanceConnectorToFace : InstanceConnectorToBaseGeometry
    {
        internal InstanceConnectorToFace(ComponentGeometryExchange _comm_manager,
                                        ConnectorBase _parent_connector, SimComponentInstance _source_gr, int _index_of_geometry_model, Face _target_face)
            : base(_comm_manager, _parent_connector, _source_gr, _index_of_geometry_model, _target_face)
        {

        }
    }

    /// <summary>
    /// Class for connecting a <see cref="SimComponentInstance"/> to an <see cref="Edge"/>.
    /// </summary>
    internal class InstanceConnectorToEdge : InstanceConnectorToBaseGeometry
    {
        internal InstanceConnectorToEdge(ComponentGeometryExchange _comm_manager,
                                        ConnectorBase _parent_connector, SimComponentInstance _source_gr, int _index_of_geometry_model, Edge _target_edge)
            : base(_comm_manager, _parent_connector, _source_gr, _index_of_geometry_model, _target_edge)
        {

        }
    }

    /// <summary>
    /// Class for connecting a <see cref="SimComponentInstance"/> to a <see cref="Vertex"/>.
    /// </summary>
    internal class InstanceConnectorToVertex : InstanceConnectorToBaseGeometry
    {
        internal InstanceConnectorToVertex(ComponentGeometryExchange _comm_manager,
                                        ConnectorBase _parent_connector, SimComponentInstance _source_gr, int _index_of_geometry_model, Vertex _target_vertex)
            : base(_comm_manager, _parent_connector, _source_gr, _index_of_geometry_model, _target_vertex)
        {

        }
    }

}
