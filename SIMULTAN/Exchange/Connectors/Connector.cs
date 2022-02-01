using SIMULTAN.Data.Components;
using SIMULTAN.Data.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SIMULTAN.Exchange.Connectors
{
    /// <summary>
    /// Abstract superclass for both connectors to geometry and to buildings.
    /// It defines the events SourceIsBeingDeleted and TargetIsBeingDeleted.
    /// </summary>
    internal abstract class Connector
    {
        #region PROPERTIES: Connection

        /// <summary>
        /// The parent of the connected component
        /// </summary>
        public SimComponent SourceParent { get; protected set; }

        /// <summary>
        /// If true, the component has an asset corresponding to the target geometry.
        /// If false, the component asset may need to be replaced. 
        /// </summary>
        protected bool asset_adjusted;
        /// <summary>
        /// the internal representation of the Property TargetId before the current one
        /// </summary>
        protected ulong prev_target_id;
        /// <summary>
        /// internal representation of the Property TargetId
        /// </summary>
        protected ulong target_id;
        /// <summary>
        /// The id of the connected geometry
        /// </summary>
        public ulong TargetId
        {
            get { return this.target_id; }
            private set
            {
                this.prev_target_id = this.target_id;
                this.target_id = value;
                this.asset_adjusted = false;
            }
        }

        /// <summary>
        /// the internal representation of the Property TargetModelIndex before the current one
        /// </summary>
        protected int prev_target_model_index;
        /// <summary>
        /// internal representation of the Property TargetModelIndex
        /// </summary>
        protected int target_model_index;
        /// <summary>
        /// The index of the model where the connected geometry resides.
        /// </summary>
        public int TargetModelIndex
        {
            get { return this.target_model_index; }
            private set
            {
                this.prev_target_model_index = this.target_model_index;
                this.target_model_index = value;
                this.asset_adjusted = false;
            }
        }

        /// <summary>
        /// the internal representation of Property 'ConnState'
        /// </summary>
        protected ConnectorConnectionState conn_state;
        /// <summary>
        /// The current state of the connection. 
        /// The setter in the inheriting classes handles changes in the target geometry (deletion, un-deletion).
        /// </summary>
        public virtual ConnectorConnectionState ConnState
        {
            get { return this.conn_state; }
            internal set
            {
                this.conn_state = value;
            }
        }
        /// <summary>
        /// The current state of information exchange (or synchronization) between the 
        /// source component and the target geometry.
        /// </summary>
        public SynchronizationState SyncState { get; protected set; }

        #endregion

        #region EVENTS

        /// <summary>
        /// Handler for the SourceIsBeingDeleted event.
        /// </summary>
        /// <param name="sender">Object which emitted the event</param>
        public delegate void SourceIsBeingDeletedEventHandler(object sender);

        /// <summary>
        /// Emitted when the source component is about to be deleted
        /// </summary>
        public event SourceIsBeingDeletedEventHandler SourceIsBeingDeleted;

        /// <summary>
        /// Emits the SourceIsBeingDeleted event
        /// </summary>
        public void OnSourceIsBeingDeleted()
        {
            this.SourceIsBeingDeleted?.Invoke(this);
        }

        /// <summary>
        /// Handler for the TargetIsBeingDeleted event.
        /// </summary>
        /// <param name="sender">Object which emitted the event</param>
        /// <param name="target">the geometry being deleted</param>
        public delegate void TargetIsBeingDeletedEventHandler(object sender, BaseGeometry target);

        /// <summary>
        /// Emitted when the target geometry is about to be deleted
        /// </summary>
        public event TargetIsBeingDeletedEventHandler TargetIsBeingDeleted;

        /// <summary>
        /// Emits the TargetIsBeingDeleted event
        /// </summary>
        internal void OnTargetIsBeingDeleted(BaseGeometry target)
        {
            this.TargetIsBeingDeleted?.Invoke(this, target);
        }

        #endregion

        #region .CTOR

        /// <summary>
        /// Initializes an instance of the class Connector
        /// </summary>
        /// <param name="_source_parent_comp">the parent component of the source(component or instance)</param>
        /// <param name="_index_of_geometry_model">the index of the file where the target resides</param>
        /// <param name="_target_geometry_id">the id of target geometry or building</param>
        protected Connector(SimComponent _source_parent_comp, int _index_of_geometry_model, ulong _target_geometry_id)
        {
            this.SourceParent = _source_parent_comp;
            this.TargetId = _target_geometry_id;
            this.TargetModelIndex = _index_of_geometry_model;
            this.ConnState = ConnectorConnectionState.OK;
            this.SyncState = SynchronizationState.UNKNOWN;
        }

        #endregion

        #region METHODS

        /// <summary>
        /// Deletes all automatically created sub-components in the source component. 
        /// Resets all automatically created parameters of the source component to their default values.
        /// Removes all references to itself from other components.
        /// </summary>
        internal virtual void BeforeDeletion()
        { }

        #endregion

        #region UTILS

        /// <summary>
        /// Queries the geometry ids related to the given geometry.
        /// </summary>
        /// <param name="bg">the geometry</param>
        /// <returns>a list od ids</returns>
        protected static List<ulong> GetRelatedGeometryIds(BaseGeometry bg)
        {
            List<ulong> related = new List<ulong>();
            if (bg == null)
                return related;

            if (bg is Face f)
            {
                if (f.Boundary != null)
                    related.Add(f.Boundary.Id);
            }
            else if (bg is EdgeLoop e)
            {
                if (e.Faces != null)
                    related.AddRange(e.Faces.Select(x => x.Id));
            }
            return related;
        }

        internal static SimParameter SetOrCreateParameter(SimComponent component, string parameterName, double value)
        {
            if (component == null)
                throw new ArgumentNullException(nameof(component));

            //Try to find existing parameter
            var isCreated = CreateParameterIfNotExists(component, parameterName, value, SimParameterInstancePropagation.PropagateIfInstance, out var param);

            if (!isCreated)
            {
                param.ValueCurrent = value;
                param.IsAutomaticallyGenerated = true;
            }

            return param;
        }

        /// <summary>
        /// Create a parameter if it is not already present. Returns True when a new parameter has been created
        /// </summary>
        internal static bool CreateParameterIfNotExists(SimComponent component, string parameterName, double value,
            SimParameterInstancePropagation instPropagation, out SimParameter parameter)
        {
            if (component == null)
                throw new ArgumentNullException(nameof(component));

            //Try to find existing parameter
            parameter = component.Parameters.FirstOrDefault(x => x.Name == parameterName);

            if (parameter == null)
            {
                parameter = new SimParameter(parameterName, GetReservedUnits(parameterName), value);
                parameter.IsAutomaticallyGenerated = true;
                parameter.InstancePropagationMode = instPropagation;
                parameter.ValueMin = double.MinValue;
                parameter.ValueMax = double.MaxValue;
                parameter.TextValue = "generated";
                if (parameterName == ReservedParameters.RP_MATERIAL_COMPOSITE_D_OUT || parameterName == ReservedParameters.RP_MATERIAL_COMPOSITE_D_IN)
                    parameter.Propagation = SimInfoFlow.Mixed;
                else
                    parameter.Propagation = SimInfoFlow.Input;

                parameter.AllowedOperations = GetAllowedOperationsForGeometry(parameterName);
                parameter.Category |= SimCategory.Geometry;

                component.Parameters.Add(parameter);

                return true;
            }
            else
            {
                parameter.InstancePropagationMode = instPropagation;
            }

            return false;
        }

        /// <summary>
        /// Hard-coded operation permissions for interacting with geometry.
        /// </summary>
        /// <param name="_name">the parameter name</param>
        /// <returns>the permissions</returns>
        private static SimParameterOperations GetAllowedOperationsForGeometry(string _name)
        {
            switch (_name)
            {
                case ReservedParameters.RP_COUNT:
                case ReservedParameters.RP_LENGTH:
                case ReservedParameters.RP_LENGTH_MIN:
                case ReservedParameters.RP_LENGTH_MAX:

                case ReservedParameters.RP_AREA:
                case ReservedParameters.RP_AREA_MIN:
                case ReservedParameters.RP_AREA_MAX:
                case ReservedParameters.RP_WIDTH:
                case ReservedParameters.RP_WIDTH_MIN:
                case ReservedParameters.RP_WIDTH_MAX:
                case ReservedParameters.RP_HEIGHT:
                case ReservedParameters.RP_HEIGHT_MIN:
                case ReservedParameters.RP_HEIGHT_MAX:

                case ReservedParameters.RP_K_FOK:
                case ReservedParameters.RP_K_FOK_ROH:
                case ReservedParameters.RP_K_F_AXES:
                case ReservedParameters.RP_K_DUK:
                case ReservedParameters.RP_K_DUK_ROH:
                case ReservedParameters.RP_K_D_AXES:
                case ReservedParameters.RP_H_NET:
                case ReservedParameters.RP_H_GROSS:
                case ReservedParameters.RP_H_AXES:
                case ReservedParameters.RP_L_PERIMETER:
                case ReservedParameters.RP_AREA_BGF:
                case ReservedParameters.RP_AREA_NGF:
                case ReservedParameters.RP_AREA_NF:
                case ReservedParameters.RP_AREA_AXES:
                case ReservedParameters.RP_VOLUME_BRI:
                case ReservedParameters.RP_VOLUME_NRI:
                case ReservedParameters.RP_VOLUME_NRI_NF:
                case ReservedParameters.RP_VOLUME_AXES:
                    return SimParameterOperations.None;
                case ReservedParameters.RP_MATERIAL_COMPOSITE_D_IN:
                case ReservedParameters.RP_MATERIAL_COMPOSITE_D_OUT:
                case ReservedParameters.RP_PARAM_TO_GEOMETRY:
                    return SimParameterOperations.EditValue;
                default:
                    return SimParameterOperations.All;
            }
        }

        private static string GetReservedUnits(string _reserved_name)
        {
            switch (_reserved_name)
            {
                case ReservedParameters.RP_COST_POSITION:
                case ReservedParameters.RP_COST_NET:
                case ReservedParameters.RP_COST_TOTAL:
                    return "€";
                case ReservedParameters.RP_WIDTH:
                case ReservedParameters.RP_HEIGHT:
                case ReservedParameters.RP_LENGTH:
                case ReservedParameters.RP_K_FOK:
                case ReservedParameters.RP_K_FOK_ROH:
                case ReservedParameters.RP_K_F_AXES:
                case ReservedParameters.RP_K_DUK:
                case ReservedParameters.RP_K_DUK_ROH:
                case ReservedParameters.RP_K_D_AXES:
                case ReservedParameters.RP_H_NET:
                case ReservedParameters.RP_H_GROSS:
                case ReservedParameters.RP_H_AXES:
                case ReservedParameters.RP_L_PERIMETER:
                    return "m";
                case ReservedParameters.RP_DIAMETER:
                    return "mm";
                case ReservedParameters.RP_AREA:
                case ReservedParameters.RP_AREA_BGF:
                case ReservedParameters.RP_AREA_NGF:
                case ReservedParameters.RP_AREA_NF:
                case ReservedParameters.RP_AREA_AXES:
                    return "m²";
                case ReservedParameters.RP_VOLUME_BRI:
                case ReservedParameters.RP_VOLUME_NRI:
                case ReservedParameters.RP_VOLUME_NRI_NF:
                case ReservedParameters.RP_VOLUME_AXES:
                    return "m³";
                default:
                    return "-";
            }
        }

        #endregion
    }
}
