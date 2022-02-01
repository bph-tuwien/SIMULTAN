using SIMULTAN.Data.Components;
using SIMULTAN.Data.SitePlanner;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace SIMULTAN.Exchange.Connectors
{
    /// <summary>
    /// Manages the connection from one component to one building. The information flow is only from the component
    /// to the building.
    /// </summary>
    internal class ConnectorParameterizingToBuilding : Connector
    {
        #region PROPERTIES: Connection

        /// <summary>
        /// The component source of the connection. It holds the data for updating the target geometries.
        /// </summary>
        public SimComponent ParameterizingSource { get; protected set; }

        int? paramToGeometryValue = null;

        #endregion

        #region FIELDS

        /// <summary>
        /// The instance holding and managing all connectors
        /// </summary>
        private ComponentSitePlannerExchange bcomm_manager;

        private SitePlannerBuilding sitePlannerBuilding;

        #endregion

        #region .CTOR

        /// <summary>
        /// Initializes a connector btw a component and a building.
        /// </summary>
        /// <param name="_bcomm_manager">the connector to the building pool</param>
        /// <param name="_source_comp">the component that acts as a source information</param>
        /// <param name="_index_of_building_model">the index of the model holding the buildings</param>
        /// <param name="_target_building">the actual building</param>
        public ConnectorParameterizingToBuilding(ComponentSitePlannerExchange _bcomm_manager,
                                        SimComponent _source_comp, int _index_of_building_model, SitePlannerBuilding _target_building)
            : base(_source_comp.Parent, _index_of_building_model, (_target_building == null) ? ulong.MaxValue : _target_building.ID)
        {
            if (_source_comp.InstanceType != SimInstanceType.BuiltStructure)
                throw new ArgumentException("The component is of the wrong type!");

            this.bcomm_manager = _bcomm_manager;

            // establish the connector
            this.ParameterizingSource = _source_comp;
            this.ParameterizingSource.Parameters.ParameterPropertyChanged += ContainedParameters_ParameterPropertyChanged;
            this.ParameterizingSource.IsBeingDeleted += Source_IsBeingDeleted;

            // create the instance
            this.SynchronizeSourceWTarget(_target_building);
        }

        #endregion

        #region METHODS

        /// <summary>
        /// Synchronizes the state of the component with the target.
        /// </summary>
        /// <param name="_target"></param>
        public void SynchronizeSourceWTarget(SitePlannerBuilding _target)
        {
            if (this.ParameterizingSource == null) return;

            if (_target != null)
            {
                using (AccessCheckingDisabler.Disable(this.ParameterizingSource.Factory))
                {
                    // perform parameter synchronization
                    CreateParameterIfNotExists(this.ParameterizingSource, ReservedParameters.RP_PARAM_TO_GEOMETRY, 1.0,
                        SimParameterInstancePropagation.PropagateIfInstance, out _);

                    // update the corresponding (single) instance
                    var instance = this.ParameterizingSource.Instances[0]; //.SetToRealized(this.TargetId, this.TargetModelIndex, null);
                    if (instance.Placements.Count == 0)
                    {
                        instance.Placements.Add(new SimInstancePlacementGeometry(this.TargetModelIndex, this.TargetId, null));
                    }
                    else if (instance.Placements[0] is SimInstancePlacementGeometry gp)
                    {
                        gp.FileId = this.TargetModelIndex;
                        gp.GeometryId = this.TargetId;
                        gp.RelatedIds = null;
                        gp.State = SimInstancePlacementState.Valid;
                    }

                    instance.State = new SimInstanceState(true, SimInstanceConnectionState.Ok);
                }
                this.SyncState = SynchronizationState.SYNCHRONIZED;
            }
            else
            {
                // leave the parameters as they were
                // reset the corresponding (single) instance
                this.ParameterizingSource.Instances[0].State = new SimInstanceState(false, SimInstanceConnectionState.GeometryNotFound);
                var pl = this.ParameterizingSource.Instances[0].Placements.FirstOrDefault(x => x is SimInstancePlacementGeometry pg &&
                    pg.FileId == TargetModelIndex && pg.GeometryId == TargetId);
                if (pl != null)
                    pl.State = SimInstancePlacementState.InstanceTargetMissing;
                this.SyncState = SynchronizationState.SOURCE_COMPONENT_NOT_UP_TO_DATE;
            }
            this.sitePlannerBuilding = _target;
        }

        /// <summary>
        /// Checks if the parameters in the source component have changed since the last time this check was performed.
        /// </summary>
        /// <returns>true, if the parameters in the source component changed since the last check; false otherwise</returns>
        protected bool ParameterizingParameterUpdateDetected()
        {
            var parameter = this.ParameterizingSource.Parameters.FirstOrDefault(x => x.Name == ReservedParameters.RP_PARAM_TO_GEOMETRY);

            int? newValue = null;
            if (parameter != null)
                newValue = (int)parameter.ValueCurrent;

            bool hasChanged = newValue != this.paramToGeometryValue;
            this.paramToGeometryValue = newValue;

            return hasChanged;
        }

        /// <summary>
        /// Clean-up before deletion of the parameterizing component.
        /// </summary>
        internal override void BeforeDeletion()
        {
            if (this.ParameterizingSource != null)
            {
                this.ParameterizingSource.Parameters.ParameterPropertyChanged -= ContainedParameters_ParameterPropertyChanged;
                this.ParameterizingSource.IsBeingDeleted -= Source_IsBeingDeleted;
                this.ParameterizingSource = null;
            }
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            string representation = "ParamConnector [" + ((this.ParameterizingSource == null) ? "NULL -> " : this.ParameterizingSource.ToInfoString()) + "]";
            return representation;
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
            this.BeforeDeletion();

            if (this.ParameterizingSource != null)
                bcomm_manager.DisAssociate(this.ParameterizingSource, this.sitePlannerBuilding);
        }

        private void ContainedParameters_ParameterPropertyChanged(object sender, SimComponent.SimParameterCollection.ParameterPropertyChangedEventArgs e)
        {
            if (e.ModifiedParameters.Any(x => x.parameter.Name == ReservedParameters.RP_PARAM_TO_GEOMETRY))
            {
                if (this.ParameterizingParameterUpdateDetected())
                {
                    bcomm_manager.AssociatedComponentParameterChanged(sitePlannerBuilding);
                }
            }
        }

        #endregion
    }
}
