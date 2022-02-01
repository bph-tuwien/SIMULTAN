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
    /// Manages a connection from a <see cref="SimComponent"/> to multiple <see cref="Face"/> instances, 
    /// where the geometry is updated by the component.
    /// Defines a component as a prescriptive source and a list of dependent instance connectors.
    /// Maintains parameters for inner and outer offset, as well as cumulative parameters as info about the dependent geometry.
    /// Creates and manages its own dependent instance connectors.
    /// Triggers the GeometryInvalidated event in the managing <see cref="ComponentGeometryExchange"/> instance.
    /// </summary>
    internal class ConnectorPrescriptiveToFace : ConnectorBase
    {
        #region PROPERTIES: Connection

        /// <summary>
        /// The component source of the connection. It holds the data for updating the target geometries.
        /// </summary>
        public SimComponent PrescriptiveSource { get; protected set; }

        private double din = 0.0, dout = 0.0;

        /// <summary>
        /// List of connections of the individual instances of the source component with Faces.
        /// </summary>
        protected List<InstanceConnectorToFace> dependent_instance_connectors;

        #endregion

        #region .CTOR

        /// <summary>
        /// Initializes an instance of the class ConnectorPrescriptiveToFace.
        /// </summary>
        /// <param name="_comm_manager">the manager, initializing and managing this instance</param>
        /// <param name="_source_parent_comp">the parent component of the source</param>
        /// <param name="_source_comp">the source component acting as a prescriptor</param>
        /// <param name="_model_file_index">the index of the model in which the target geometry resides</param>
        public ConnectorPrescriptiveToFace(ComponentGeometryExchange _comm_manager, SimComponent _source_parent_comp, SimComponent _source_comp, int _model_file_index)
            : base(_comm_manager, _source_parent_comp, _model_file_index, null)
        {
            // establish the connector
            this.PrescriptiveSource = _source_comp;
            this.PrescriptiveSource.Parameters.ParameterPropertyChanged += PrescriptiveSource_ParameterPropertyChanged;
            this.dependent_instance_connectors = new List<InstanceConnectorToFace>();
        }

        #endregion

        #region METHOD OVERRIDES

        /// <summary>
        /// Alerts all dependent <see cref="Face"/> instances to any change in the offsets by emitting events.
        /// </summary>
        /// <param name="_target"></param>
        public override void SynchronizeSourceWTarget(BaseGeometry _target)
        {
            if (this.PrescriptiveSource == null) return;

            if (this.SynchTargetIsAdmissible(_target))
            {
                // perform parameter synchronization
                this.UpdateCumulativeParameters();
            }
        }

        /// <summary>
        /// Part of the synchronization routine. Checks if the given geometry is a <see cref="Face"/> instances.
        /// </summary>
        /// <param name="_target">geometry to synchronize with, has to be a Face</param>
        /// <returns>true, if the geometry is of the correct type</returns>
        protected override bool SynchTargetIsAdmissible(BaseGeometry _target)
        {
            return (_target is Face);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            string representation = "PConnector [";
            representation += (this.PrescriptiveSource == null) ? "NULL -> " : this.PrescriptiveSource.ToInfoString() + " -> ";
            representation += this.dependent_instance_connectors.Count + " Faces]";
            return representation;
        }

        /// <inheritdoc/>
        internal override void BeforeDeletion()
        {
            if (this.PrescriptiveSource != null)
                this.PrescriptiveSource.Parameters.ParameterPropertyChanged -= PrescriptiveSource_ParameterPropertyChanged;
        }

        #endregion

        #region METHODS: Instance creation, deletion and management

        /// <summary>
        /// Gathers the total number of dependent faces, sums their area and updates the 
        /// parameters of the <see cref="ConnectorPrescriptiveToFace.PrescriptiveSource"/>.
        /// </summary>
        internal void UpdateCumulativeParameters(SimComponentInstance callingInstance = null)
        {
            if (this.PrescriptiveSource != null && this.PrescriptiveSource.Factory != null)
            {
                using (AccessCheckingDisabler.Disable(this.PrescriptiveSource.Factory))
                {
                    CreateParameterIfNotExists(this.PrescriptiveSource, ReservedParameters.RP_AREA, 0.0,
                        SimParameterInstancePropagation.PropagateNever, out var areaParam);
                    CreateParameterIfNotExists(this.PrescriptiveSource, ReservedParameters.RP_COUNT, 0.0,
                        SimParameterInstancePropagation.PropagateNever, out var countParam);

                    double area = 0.0;
                    int count = 0;

                    if (areaParam != null && countParam != null)
                    {
                        foreach (var inst in this.PrescriptiveSource.Instances)
                        {
                            var firstGeom = (SimInstancePlacementGeometry)inst.Placements.FirstOrDefault(x => x is SimInstancePlacementGeometry);
                            if (inst.State.ConnectionState == SimInstanceConnectionState.Ok && firstGeom != null)
                            {
                                var face = this.comm_manager.GetGeometryFromId(firstGeom.FileId, firstGeom.GeometryId) as Face;

                                if ((callingInstance == null || inst == callingInstance) && face != null)
                                {
                                    //Instance needs update
                                    var instArea = FaceAlgorithms.Area(face);
                                    inst.InstanceParameterValuesPersistent[areaParam] = instArea;
                                    inst.InstanceParameterValuesPersistent[countParam] = 1.0;
                                    area += instArea;
                                }
                                else
                                {
                                    //Instance not open -> use existing value
                                    area += inst.InstanceParameterValuesPersistent[areaParam];
                                }

                                count++;
                            }
                            else
                            {
                                //Invalid geometry
                                inst.InstanceParameterValuesPersistent[areaParam] = 0.0;
                                inst.InstanceParameterValuesPersistent[countParam] = 0.0;
                            }
                        }

                        areaParam.ValueCurrent = area;
                        countParam.ValueCurrent = count;
                    }
                }
            }
        }

        /// <summary>
        /// Creates an instance (Geometric Relationship) as interface with the given Face. If a
        /// connection to the face already exists, nothing changes.
        /// </summary>
        /// <param name="_file_id">the id of the geometry file containing the face</param>
        /// <param name="_f">the target face</param>
		/// <param name="_update">if true, update cumulative parameters</param>
        /// <returns>the instance connector to the given face and a boolean to signify if the connector was just created</returns>
        public (InstanceConnectorToFace instance_connector, bool newly_created) ConnectToFace(int _file_id, Face _f, bool _update = true)
        {
            if (_f == null) return (null, false);

            this.SynchronizeSourceWTarget();

            SimComponentInstance duplicate_gr = null;

            // 1. check, if a connection to that face is already established
            if (this.PrescriptiveSource.InstanceType == SimInstanceType.Attributes2D)
            {
                duplicate_gr = this.PrescriptiveSource.Instances.FirstOrDefault(x => x.Placements.Any(
                    p => p is SimInstancePlacementGeometry pg && pg.FileId == _file_id && pg.GeometryId == _f.Id));
            }

            if (duplicate_gr != null)
            {
                SimInstanceState old_state = duplicate_gr.State;
                InstanceConnectorToFace duplicate_con = this.dependent_instance_connectors.FirstOrDefault(x => x.Source == duplicate_gr && x.TargetId == _f.Id);
                if (duplicate_con != null)
                {
                    if (_update)
                        this.UpdateCumulativeParameters();
                    return (duplicate_con, false);
                }
                else
                {
                    // if only the connector is missing, add it
                    InstanceConnectorToFace con = new InstanceConnectorToFace(this.comm_manager, this, duplicate_gr, this.comm_manager.GetModelIndex(_f), _f);
                    duplicate_gr.State = new SimInstanceState(old_state.IsRealized, SimInstanceConnectionState.Ok);
                    var pl = duplicate_gr.Placements.FirstOrDefault(x => x is SimInstancePlacementGeometry pg &&
                    pg.FileId == _file_id && pg.GeometryId == _f.Id);
                    if (pl != null)
                        pl.State = SimInstancePlacementState.Valid;

                    con.SourceIsBeingDeleted += dependent_instance_SourceIsBeingDeleted;
                    this.dependent_instance_connectors.Add(con);
                    if (_update)
                        this.UpdateCumulativeParameters();
                    return (con, true);
                }
            }

            // 2. if there is no connection at all, establish it
            var f_gr = new SimComponentInstance(SimInstanceType.Attributes2D, _file_id, _f.Id, Connector.GetRelatedGeometryIds(_f));
            f_gr.State = new SimInstanceState(true);
            this.PrescriptiveSource.Instances.Add(f_gr);

            InstanceConnectorToFace f_con = new InstanceConnectorToFace(this.comm_manager, this, f_gr, this.comm_manager.GetModelIndex(_f), _f);
            f_con.SourceIsBeingDeleted += dependent_instance_SourceIsBeingDeleted;
            this.dependent_instance_connectors.Add(f_con);

            // 3. update the cumulative parameters of the component
            if (_update)
                this.UpdateCumulativeParameters();

            // done
            return (f_con, true);
        }

        private void dependent_instance_SourceIsBeingDeleted(object sender)
        {
            InstanceConnectorToFace f_con = sender as InstanceConnectorToFace;
            //f_con.OnSourceIsBeingDeleted();
            if (f_con != null && this.dependent_instance_connectors.Contains(f_con))
                this.dependent_instance_connectors.Remove(f_con);
            this.UpdateCumulativeParameters();
        }

        /// <summary>
        /// Removes the instance (Geometric Relationship) as interface with the given Face.
        /// If no connection to that face exists, nothing changes.
        /// </summary>
        /// <param name="_file_id">the id of the geometry file containing the given face</param>
        /// <param name="_f">the target face</param>
        /// <returns>true, if there are no faces left associated with the source component; false otherwise</returns>
        public bool DisconnectFromFace(int _file_id, Face _f)
        {
            if (_f == null) return false;

            // 1. look for the connection to the face
            if (this.PrescriptiveSource.InstanceType == SimInstanceType.Attributes2D)
            {
                SimComponentInstance corresponding_gr = this.PrescriptiveSource.Instances.FirstOrDefault(
                    x => x.Placements.Any(p => p is SimInstancePlacementGeometry pg && pg.FileId == _file_id && pg.GeometryId == _f.Id));
                if (corresponding_gr != null)
                {
                    InstanceConnectorToFace corresponding_con = this.dependent_instance_connectors.FirstOrDefault(x => x.Source == corresponding_gr && x.TargetId == _f.Id);
                    if (corresponding_con != null)
                    {
                        // remove the connector first
                        corresponding_con.Reset();
                        corresponding_con.SourceIsBeingDeleted -= dependent_instance_SourceIsBeingDeleted;
                        this.dependent_instance_connectors.Remove(corresponding_con);
                        this.comm_manager.ConnectorManager.RemoveConnector(corresponding_con, _f);
                    }

                    // remove the semantic instance
                    using (AccessCheckingDisabler.Disable(this.PrescriptiveSource.Factory))
                    {
                        this.PrescriptiveSource.Instances.Remove(corresponding_gr);
                    }
                }
            }

            // 2. update the cumulative parameters of the component
            this.UpdateCumulativeParameters();

            // 3. check if there are any faces associated with this component left
            return (this.dependent_instance_connectors.Count == 0);
        }

        #endregion

        #region METHODS: Parameter checks, synchronization

        /// <summary>
        /// Checks if the offset parameters in the source component have changed since
        /// the last time this check was performed.
        /// </summary>
        /// <returns>true, if the offset parameters in the source component changed since the last check; false otherwise</returns>
        protected bool OffsetParameterUpdateDetected()
        {
            //Get din and dout
            var dinParam = this.PrescriptiveSource.Parameters.FirstOrDefault(x => x.Name == ReservedParameters.RP_MATERIAL_COMPOSITE_D_IN);
            var doutParam = this.PrescriptiveSource.Parameters.FirstOrDefault(x => x.Name == ReservedParameters.RP_MATERIAL_COMPOSITE_D_OUT);

            var newDin = dinParam != null ? dinParam.ValueCurrent : double.NaN;
            var newDout = doutParam != null ? doutParam.ValueCurrent : double.NaN;

            bool isDifferent = newDin != this.din || newDout != this.dout;

            this.din = newDin;
            this.dout = newDout;

            return isDifferent;
        }

        public void SynchronizeSourceWTarget()
        {
            this.Dispatcher.Invoke(() =>
            {
                using (AccessCheckingDisabler.Disable(this.PrescriptiveSource.Factory))
                {
                    CreateParameterIfNotExists(this.PrescriptiveSource, ReservedParameters.RP_AREA, 0.0,
                        SimParameterInstancePropagation.PropagateNever, out _);
                    CreateParameterIfNotExists(this.PrescriptiveSource, ReservedParameters.RP_COUNT, 0.0,
                        SimParameterInstancePropagation.PropagateNever, out _);
                    CreateParameterIfNotExists(this.PrescriptiveSource, ReservedParameters.RP_MATERIAL_COMPOSITE_D_OUT, 0.0,
                        SimParameterInstancePropagation.PropagateIfInstance, out _);
                    CreateParameterIfNotExists(this.PrescriptiveSource, ReservedParameters.RP_MATERIAL_COMPOSITE_D_IN, 0.0,
                        SimParameterInstancePropagation.PropagateIfInstance, out _);
                }
            });
        }

        #endregion

        #region EVENT HANDLERS

        private void PrescriptiveSource_ParameterPropertyChanged(object sender, SimComponent.SimParameterCollection.ParameterPropertyChangedEventArgs e)
        {
            if (this.comm_manager != null &&
                e.ModifiedParameters.Any(x =>
                    (x.parameter.Name == ReservedParameters.RP_MATERIAL_COMPOSITE_D_IN ||
                    x.parameter.Name == ReservedParameters.RP_MATERIAL_COMPOSITE_D_OUT) &&
                    x.property == nameof(SimParameter.ValueCurrent))
                )
            {
                if (this.OffsetParameterUpdateDetected())
                {
                    var face_ids = this.dependent_instance_connectors.Select(x => new KeyValuePair<int, ulong>(x.TargetModelIndex, x.TargetId));
                    var faces = face_ids.Select(x => this.comm_manager.GeometryManager.GetGeometryFromId(x.Key, x.Value));
                    this.comm_manager.OnGeometryInvalidated(new List<BaseGeometry>(faces));
                }
            }
        }

        #endregion
    }
}
