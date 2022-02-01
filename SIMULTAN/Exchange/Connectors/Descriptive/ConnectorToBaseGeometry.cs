using SIMULTAN.Data.Components;
using SIMULTAN.Data.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Exchange.Connectors
{
    /// <summary>
    /// The base class for all descriptive connectors, where the <see cref="SimComponent"/> is updated by the geometry.
    /// It defines a source component and a list of dependent instance connectors(not used yet).
    /// Emits the <see cref="Connector.SourceIsBeingDeleted"/> event on deletion of the source component(not used yet).
    /// </summary>
    internal abstract class ConnectorToBaseGeometry : ConnectorBase
    {
        #region PROPERTIES: Connection

        /// <summary>
        /// The component source of the connection (holds the data extracted from the geometry)
        /// </summary>
        public SimComponent DescriptiveSource { get; protected set; }
        /// <summary>
        /// Connectors that associate a sub-component of a <see cref="DescriptiveSource"/> with a geometric child of its target.
        /// </summary>
        public List<ConnectorToBaseGeometry> GeometryBasedChildren { get; }

        /// <inheritdoc/>
        public override ConnectorConnectionState ConnState
        {
            get { return this.conn_state; }
            internal set
            {
                this.conn_state = value;
                // adapt the component's state
                if (this.DescriptiveSource != null)
                {
                    foreach (var instance in this.DescriptiveSource.Instances)
                    {
                        var pl = instance.Placements.FirstOrDefault(x => x is SimInstancePlacementGeometry pg &&
                            pg.FileId == this.TargetModelIndex && pg.GeometryId == this.TargetId);

                        if (pl != null)
                        {
                            if (this.conn_state == ConnectorConnectionState.TARGET_GEOMETRY_NULL)
                            {
                                instance.State = new SimInstanceState(instance.State.IsRealized, SimInstanceConnectionState.GeometryDeleted);
                                pl.State = SimInstancePlacementState.InstanceTargetMissing;
                            }
                            else
                            {
                                instance.State = new SimInstanceState(instance.State.IsRealized, SimInstanceConnectionState.Ok);
                                pl.State = SimInstancePlacementState.Valid;
                            }
                        }
                    }
                }
            }
        }

        #endregion


        #region .CTOR

        /// <summary>
        /// Initializes an object of type ConnectorToBaseGeometry
        /// </summary>
        /// <param name="_comm_manager">the manager, initializing this instance</param>
        /// <param name="_source_parent_comp">the parent component of the source</param>
        /// <param name="_source_comp">the source component</param>
        /// <param name="_index_of_geometry_model">the index of the <see cref="GeometryModelData"/> where the geometry resides</param>
        /// <param name="_target_geometry">the target geometry</param>
        protected ConnectorToBaseGeometry(ComponentGeometryExchange _comm_manager,
                                        SimComponent _source_parent_comp, SimComponent _source_comp, int _index_of_geometry_model,
                                        BaseGeometry _target_geometry)
            : base(_comm_manager, _source_parent_comp, _index_of_geometry_model, _target_geometry)
        {
            this.DescriptiveSource = _source_comp;
            this.DescriptiveSource.IsBeingDeleted += Source_IsBeingDeleted;
            this.GeometryBasedChildren = new List<ConnectorToBaseGeometry>();
        }



        #endregion

        #region METHOD OVERRIDES

        /// <inheritdoc/>
        public override void SynchronizeSourceWTarget(BaseGeometry _target)
        {
            bool isStructureValid = SynchronizeStructure(_target);

            if (isStructureValid)
            {
                // perform parameter synchronization
                this.UpdateSourceParameters(_target);
            }
        }

        public bool SynchronizeStructure(BaseGeometry _target)
        {
            if (this.DescriptiveSource == null) return false;

            if (this.SynchTargetIsAdmissible(_target))
            {
                // update the corresponding (single) instance
                var instance = this.DescriptiveSource.Instances.FirstOrDefault();
                if (instance == null)
                {
                    instance = new SimComponentInstance(this.DescriptiveSource.InstanceType,
                        this.TargetModelIndex, this.TargetId, Connector.GetRelatedGeometryIds(_target));
                    this.DescriptiveSource.Instances.Add(instance);
                }

                instance.State = new SimInstanceState(true, SimInstanceConnectionState.Ok);
                var pl = instance.Placements.First(x => x is SimInstancePlacementGeometry pg &&
                    pg.FileId == this.TargetModelIndex && pg.GeometryId == _target.Id);
                if (pl != null)
                    pl.State = SimInstancePlacementState.Valid;

                // add an asset to the parent component
                if (!this.asset_adjusted)
                {
                    this.comm_manager.RemoveAssetFromComponent(this, this.prev_target_model_index, this.prev_target_id);
                    this.comm_manager.AddAssetToComponent(this, _target);
                    this.asset_adjusted = true;
                }

                this.SyncState = SynchronizationState.SYNCHRONIZED;
                return true;
            }
            else
            {
                // leave the parameters as they were
                // reset the corresponding (single) instance
                var instance = this.DescriptiveSource.Instances[0];
                instance.State = new SimInstanceState(false, SimInstanceConnectionState.GeometryNotFound);

                this.SyncState = SynchronizationState.SOURCE_COMPONENT_NOT_UP_TO_DATE;
                return false;
            }
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            string representation = "Connector [";
            representation += (this.DescriptiveSource == null) ? "NULL -> " : this.DescriptiveSource.ToInfoString() + " -> ";
            representation += this.TargetId.ToString() + "]";
            return representation;
        }

        /// <inheritdoc/>
        internal override void BeforeDeletion()
        {
            // remove the asset from the component
            this.comm_manager.RemoveAssetFromComponent(this, this.TargetModelIndex, this.TargetId);

            if (this.DescriptiveSource != null)
            {
                this.DescriptiveSource.IsBeingDeleted -= Source_IsBeingDeleted;
            }
        }

        #endregion

        #region EVENT HANDLERS

        /// <summary>
        /// Emits the event SourceIsBeingDeleted so that the manager (of type ComponentGeometryExchange)
        /// can do some house-keeping.
        /// </summary>
        /// <param name="sender">the source of the connector</param>
        protected void Source_IsBeingDeleted(object sender)
        {
            this.OnSourceIsBeingDeleted();
        }

        #endregion
    }
}
