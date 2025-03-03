using SIMULTAN.Data.FlowNetworks;
using SIMULTAN.Data.SimMath;
using SIMULTAN.Data.SimNetworks;
using SIMULTAN.Data.Taxonomy;
using SIMULTAN.Data.Users;
using SIMULTAN.Exceptions;
using SIMULTAN.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

namespace SIMULTAN.Data.Components
{
    /// <summary>
    /// Stores the data of an instance of a component
    /// 
    /// <para>Size &amp; Orientation</para>
    /// <para>
    /// Each instance has a size, which can either be set by the user directly (<see cref="InstanceSize"/>) or which can be derived from parameter
    /// values or from the path (<see cref="SizeTransfer"/>).
    /// Each instance also provides an orientation in space.
    /// </para>
    /// 
    /// <para>Parameters</para>
    /// <para>
    /// An instance stores it's own values for each parameter in the component, except for CALC_IN parameters.
    /// Two sets of parameters values exist per instance: temporary values (<see cref="InstanceParameterValuesTemporary" />) 
    /// and persistent values (<see cref="InstanceParameterValuesPersistent"/>). Persistent values are meant to be set by the user
    /// or the application and are stored in the component file. Temporary values are not stored and may be used to store
    /// intermediate results, for example, during a network calculation.
    /// 
    /// Both collections are automatically populated according to changes to parameters in the component. The user can
    /// control whether parameter value changes are also propagated by setting the ReservedParameters.RP_INST_PROPAGATE parameter.
    /// A value of 0 means that value changes are not propagated, in all other cases, changes to the parameters ValueCurrent are duplicated
    /// into the persistent parameter collection. Temporary parameter values are never updated.
    /// </para>
    /// 
    /// <para>Placements</para>
    /// <para>
    /// An instance can be place/used in several different locations. For example, the same instance my be placed in a network node and may
    /// at the same time be associated with geometry that describes the position of the network node in the building.
    /// Placements (= the association of an instance with a target), are stored in <see cref="Placements"/>. 
    /// The specializations of <see cref="SimInstancePlacement"/> describes which exact placement is used.
    /// </para>
    /// 
    /// </summary>
    public partial class SimComponentInstance : SimNamedObject<SimComponentCollection>
    {
        #region STATIC

        private static double GetPathLength(List<SimPoint3D> _path)
        {
            if (_path == null) return 0.0;

            double length = 0.0;
            for (int i = 0; i < _path.Count - 1; i++)
            {
                length += (_path[i] - _path[i + 1]).Length;
            }

            return length;
        }

        #endregion

        #region Properties for Loading

        /// <summary>
        /// Stores the network element id until all networks have been loaded. Afterwards: Always SimObjectId.Empty
        /// </summary>
        [Obsolete]
        internal SimObjectId LoadingNetworkElementId { get; private set; }

        /// <summary>
        /// Stores the simnetwork element id until all networks have been loaded. Afterwards: Always SimObjectId.Empty
        /// </summary>
        [Obsolete]
        public SimId LoadingSimNetworkElmentId { get; private set; }

        //parameterName is only used when the id is not set (used for loading legacy projects)
        internal List<(SimId id, string parameterName, object value)> LoadingParameterValuesPersistent { get; private set; }

        #endregion


        #region Placement

        /// <summary>
        /// Stores the placements of this instance. See specializations of the
        /// <see cref="SimInstancePlacement"/> class for potential content.
        /// </summary>
        public SimInstancePlacementCollection Placements { get; }

        #endregion

        #region PROPERTIES: Size & Orientation

        /// <summary>
        /// Stores the orientation of the instance in the geometry.
        /// This is used to orient proxy geometry attached to this component
        /// </summary>
		public SimQuaternion InstanceRotation
        {
            get { return this.instanceRotation; }
            set
            {
                if (this.instanceRotation != value)
                {
                    this.NotifyWriteAccess();

                    this.instanceRotation = value;
                    this.NotifyPropertyChanged(nameof(this.InstanceRotation));
                    this.NotifyChanged();
                }
            }
        }
        private SimQuaternion instanceRotation;

        /// <summary>
        /// Stores the size of the instance.
        /// When a proxy geometry is attached, the size is treated as a scaling factor.
        /// 
        /// The size can either be stored directly in the InstanceSize, or it can be derived
        /// from parameter values or the instance path. In this case InstanceSize is overwritten based
        /// on the information stored in <see cref="SizeTransfer"/>
        /// 
        /// </summary>
        /// <remarks>
        /// For performance reasons, use the <see cref="SetSize"/> method when setting <see cref="InstanceSize"/> and <see cref="SizeTransfer"/> together.
        /// </remarks>
        public SimInstanceSize InstanceSize
        {
            get { return this.instanceSize; }
            set
            {
                if (this.instanceSize != value)
                {
                    this.NotifyWriteAccess();

                    this.instanceSize = value;

                    //Update cumulative sizes
                    UpdateAutoParameters(this.Component);
                    this.NotifyPropertyChanged(nameof(this.InstanceSize));
                    this.NotifyChanged();
                }
            }
        }
        private SimInstanceSize instanceSize;

        /// <summary>
        /// Stores a calculation definition for each axis of the <see cref="InstanceSize"/>.
        /// Sizes can be based either on user input, path length or on <see cref="InstanceParameterValuesPersistent"/>
        /// </summary>
        /// <remarks>
        /// For performance reasons, use the <see cref="SetSize"/> method when setting <see cref="InstanceSize"/> and <see cref="SizeTransfer"/> together.
        /// </remarks>
        public ISimInstanceSizeTransferDefinition SizeTransfer
        {
            get { return this.sizeTransfer; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                if (this.sizeTransfer != value)
                {
                    this.NotifyWriteAccess();

                    this.sizeTransfer = value.Clone();
                    this.InstanceSize = this.ApplySizeTransferSettings(this.instanceSize);
                    this.NotifyPropertyChanged(nameof(this.SizeTransfer));
                    this.NotifyChanged();
                }
            }
        }
        private ISimInstanceSizeTransferDefinition sizeTransfer;

        #endregion

        #region PROPERTIES: State, Component

        /// <summary>
        /// Stores the state in which this instance is. This information is used to update <see cref="SimComponent.InstanceState"/>.
        /// This information has to be set manually. IT IS NOT DERIVED FROM THE PLACEMENT STATES
        /// </summary>
        public SimInstanceState State
        {
            get { return this.state; }
            internal set
            {
                if (this.state != value)
                {
                    this.NotifyWriteAccess();

                    this.state = value;

                    this.Component?.OnInstanceStateChanged();
                    this.NotifyPropertyChanged(nameof(this.State));
                    this.NotifyChanged();
                }
            }
        }
        private SimInstanceState state;

        /// <summary>
        /// Stores the component to which this instance belongs.
        /// Automatically set when the instance is added to <see cref="SimComponent.Instances"/>
        /// </summary>
        public SimComponent Component
        {
            get { return this.component; }
            internal set
            {
                if (this.component != value)
                {
                    if (this.component != null)
                        this.Placements.ForEach(x => x.RemoveFromTarget());

                    this.component = value;
                    this.NotifyPropertyChanged(nameof(this.Component));

                    if (this.component != null)
                    {

                        this.Placements.ForEach(x => x.AddToTarget());
                        this.UpdateInstanceParameters(this.InstanceParameterValuesPersistent);
                        this.UpdateInstanceParameters(this.InstanceParameterValuesTemporary);
                    }
                }
            }

        }
        private SimComponent component;

        #endregion

        #region PROPERTIES: Instance Parameters

        /// <summary>
        /// Specifies whether parameter values may be propagated to this instance.
        /// 
        /// This setting is used in combination with <see cref="SimBaseParameter.InstancePropagationMode"/> to identify 
        /// if a parameter value change should be propagated. When this property changes to True, a reevaluation of
        /// all parameters is performed.
        /// </summary>
        public bool PropagateParameterChanges
        {
            get => this.propagateParameterChanges;
            set
            {
                if (this.propagateParameterChanges != value)
                {
                    this.propagateParameterChanges = value;

                    // if propagate, update parameters and notify
                    if (this.propagateParameterChanges && this.Component != null)
                    {
                        foreach (var updateParam in this.Component.Parameters)
                        {
                            if ((
                                 updateParam.InstancePropagationMode == SimParameterInstancePropagation.PropagateAlways ||
                                 updateParam.InstancePropagationMode == SimParameterInstancePropagation.PropagateIfInstance
                                ) &&
                                this.InstanceParameterValuesPersistent.Contains(updateParam))
                            {

                                if (updateParam is SimEnumParameter enumParam)
                                {
                                    var newTaxonomyEntryRef = new SimTaxonomyEntryReference(enumParam.Value.Target);
                                    this.InstanceParameterValuesPersistent[updateParam] = newTaxonomyEntryRef;
                                }
                                else
                                {
                                    this.InstanceParameterValuesPersistent[updateParam] = updateParam.Value;
                                }

                            }
                        }
                        //if (this.Component.Factory != null)
                        //{
                        //    this.Component.Factory.ProjectData.ComponentGeometryExchange.OnParameterValueChanged(
                        //        this.Component.Parameters.Where(x =>
                        //            x.InstancePropagationMode == SimParameterInstancePropagation.PropagateAlways ||
                        //            x.InstancePropagationMode == SimParameterInstancePropagation.PropagateIfInstance),
                        //        this
                        //        );
                        //}
                    }

                    this.NotifyPropertyChanged(nameof(this.PropagateParameterChanges));
                }
            }
        }
        private bool propagateParameterChanges = true;

        /// <summary>
        /// Stores the persistent parameter values of each parameter. The user may change those values by using the [Parameter] operator.
        /// Entries for all parameters of the component are automatically created.
        /// 
        /// In contrast to <see cref="InstanceParameterValuesTemporary"/>, this values are persisted when the project is saved.
        /// </summary>
        public SimInstanceParameterCollection InstanceParameterValuesPersistent { get; }

        /// <summary>
        /// Stores temporary values for all parameters in the component.
        /// Parameters are automatically added/removed.
        /// <para>To be used only for calculations within a flow network for saving intermediate values for each of the parent component's parameters.</para>
        /// <para>NOTE: Do not update every time a parameter in the parent component changes, updates have to be performed manually by 
        /// calling <see cref="Reset"/>.</para>
        /// </summary>
        public SimInstanceParameterCollection InstanceParameterValuesTemporary { get; }

        #endregion


        #region EVENTS

        /// <summary>
        /// Handler for the <see cref="IsBeingDeleted"/> event.
        /// </summary>
        /// <param name="sender">The object which invoked the command</param>
        public delegate void IsBeingDeletedEventHandler(object sender);
        /// <summary>
        /// Emitted just before the instance is being deleted.
        /// </summary>
        public event IsBeingDeletedEventHandler IsBeingDeleted;
        /// <summary>
        /// Invokes the <see cref="IsBeingDeleted"/> event.
        /// </summary>
        public void OnIsBeingDeleted()
        {
            this.IsBeingDeleted?.Invoke(this);
        }

        #endregion


        #region .CTOR

        /// <summary>
        /// Initializes a new instance of the <see cref="SimComponentInstance"/> class
        /// </summary>
        public SimComponentInstance()
        {
            this.Placements = new SimInstancePlacementCollection(this);
            this.InstanceParameterValuesTemporary = new SimInstanceParameterCollectionTemporary(this);
            this.InstanceParameterValuesPersistent = new SimInstanceParameterCollectionPersistent(this);

            this.InstanceSize = SimInstanceSize.Default;
            this.SizeTransfer = new SimInstanceSizeTransferDefinition();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SimComponentInstance"/> class.
        /// 
        /// Adds a <see cref="SimInstancePlacementNetwork"/> to the <see cref="Placements"/>.
        /// The instance type is determined from the networkElements type:
        /// For nodes, <see cref="SimInstanceType.NetworkNode"/> is used, 
        /// For edges, <see cref="SimInstanceType.NetworkEdge"/> is used.
        /// </summary>
        /// <param name="networkElement">The network element to which a connection should be established</param>
        public SimComponentInstance(SimFlowNetworkElement networkElement)
            : this()
        {
            if (networkElement == null)
                throw new ArgumentNullException(nameof(networkElement));

            this.Name = string.Format("Network Placement {0}", networkElement.Name);
            var placementType = (networkElement is SimFlowNetworkEdge) ? SimInstanceType.NetworkEdge : SimInstanceType.NetworkNode;
            var placement = new SimInstancePlacementNetwork(networkElement, placementType);
            this.Placements.Add(placement);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SimComponentInstance"/> class. 
        /// Adds a <see cref="SimInstancePlacementSimNetwork"/> to the <see cref="Placements"/>.
        /// </summary>
        /// <param name="simNetworkBlock">The network block this instance is bound to</param>
        public SimComponentInstance(SimNetworkBlock simNetworkBlock) : this()
        {
            if (simNetworkBlock == null)
                throw new ArgumentNullException(nameof(simNetworkBlock));

            this.Name = string.Format("SimNetwork Placement {0}", simNetworkBlock.Name);
            var placement = new SimInstancePlacementSimNetwork(simNetworkBlock, SimInstanceType.SimNetworkBlock);
            this.Placements.Add(placement);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SimComponentInstance"/> class. 
        /// Adds a <see cref="SimInstancePlacementSimNetwork"/> to the <see cref="Placements"/>.
        /// </summary>
        /// <param name="port">The network port this instance is bound to</param>
        public SimComponentInstance(SimNetworkPort port) : this()
        {
            if (port == null)
                throw new ArgumentNullException(nameof(port));
            this.Name = string.Format("SimNetwork Placement {0}", port.Name);

            SimInstanceType placementType = port.PortType == PortType.Input ? SimInstanceType.InPort : SimInstanceType.OutPort;
            var placement = new SimInstancePlacementSimNetwork(port, placementType);
            this.Placements.Add(placement);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SimComponentInstance"/> class.
        /// 
        /// Adds a <see cref="SimInstancePlacementGeometry"/> to the <see cref="Placements"/>.
        /// </summary>
        /// <param name="placementType">The geometry type of the placement created</param>
        /// <param name="fileId">Id (key) of the geometry file</param>
        /// <param name="geometryId">The id of the geometry itself</param>
        public SimComponentInstance(SimInstanceType placementType, int fileId, ulong geometryId) : this()
        {
            if (fileId < 0)
                throw new ArgumentException(string.Format("{0} must be 0 or a positive integer", nameof(fileId)));
            if (geometryId < 0)
                throw new ArgumentException(string.Format("{0} must be 0 or a positive integer", nameof(geometryId)));

            this.Name = string.Format("Geometry Placement {0}:{1}", fileId, geometryId);
            var placement = new SimInstancePlacementGeometry(fileId, geometryId, placementType, SimInstancePlacementState.Valid);
            this.Placements.Add(placement);
        }

        #endregion

        #region .CTOR PARSING

        /// <summary>
        /// Initializes a new instance of the ComponentInstance class. May only be used during DXF file loading.
        /// 
        /// networkElementId and parameterValuesPersistent are only restored after calling <see cref="RestoreReferences(Dictionary{SimObjectId, SimFlowNetworkElement})"/>.
        /// </summary>
        /// <param name="localId">The local id of the instance</param>
        /// <param name="name">The name of the instance</param>
        /// <param name="state">The current state of the instance</param>
        /// <param name="placements">A list of placements in this instance</param>
        /// <param name="instanceRotation">Rotation of the instance</param>
        /// <param name="instanceSize">Instance size</param>
        /// <param name="sizeTransfer">Instance size transfer settings</param>
        /// <param name="propagateParamterChanges">If the parameters changes should be propagated</param>
        /// <param name="parameterValuesPersistent">A list of all persistent parameter values present in this instance</param>
        internal SimComponentInstance(long localId, string name, SimInstanceState state,
                                      IEnumerable<SimInstancePlacement> placements,
                                       SimQuaternion instanceRotation,
                                       SimInstanceSize instanceSize, SimInstanceSizeTransferDefinition sizeTransfer,
                                       List<(SimId id, string parameterName, object value)> parameterValuesPersistent, bool propagateParamterChanges)
            : base(new SimId(localId))
        {
            this.Placements = new SimInstancePlacementCollection(this);

            this.InstanceParameterValuesTemporary = new SimInstanceParameterCollectionTemporary(this);
            this.InstanceParameterValuesPersistent = new SimInstanceParameterCollectionPersistent(this);

            this.LoadingParameterValuesPersistent = parameterValuesPersistent;

            this.Name = name;
            this.State = state;

            this.Placements = new SimInstancePlacementCollection(this);
            foreach (var pl in placements)
                Placements.Add(pl);

            this.InstanceRotation = instanceRotation;

            this.InstanceSize = instanceSize.Clone();

            this.SizeTransfer = sizeTransfer;

            this.PropagateParameterChanges = propagateParamterChanges;
        }

        #endregion

        #region METHODS: Instance Definition / Update

        internal void ChangeParameterValue(SimBaseParameter parameter)
        {
            if (parameter == null)
                throw new ArgumentNullException(nameof(parameter));

            if (parameter.InstancePropagationMode == SimParameterInstancePropagation.PropagateAlways ||
                (parameter.InstancePropagationMode == SimParameterInstancePropagation.PropagateIfInstance && this.PropagateParameterChanges))
            {
                // update value
                if (this.InstanceParameterValuesPersistent.Contains(parameter))
                {
                    if (parameter is SimEnumParameter enumParam)
                    {
                        SimTaxonomyEntryReference newValue = null;
                        if (enumParam.Value != null)
                        {
                            newValue = new SimTaxonomyEntryReference(enumParam.Value.Target);
                        }
                        this.InstanceParameterValuesPersistent[enumParam] = newValue;
                    }
                    else
                    {
                        //Set with special function to prevent updating the geometry twice
                        this.InstanceParameterValuesPersistent.SetWithoutNotify(parameter, parameter.Value);
                    }
                }
            }
        }

        internal void AddParameter(SimBaseParameter parameter)
        {
            if (parameter == null)
                throw new ArgumentNullException(nameof(parameter));

            // add or update
            this.InstanceParameterValuesPersistent.Add(parameter, parameter.Value);
            this.InstanceParameterValuesTemporary.Add(parameter, parameter.Value);
        }

        internal void RemoveParameter(SimBaseParameter parameter)
        {
            if (parameter == null)
                throw new ArgumentNullException(nameof(parameter));

            // remove
            this.InstanceParameterValuesPersistent.Remove(parameter);
            this.InstanceParameterValuesTemporary.Remove(parameter);
        }

        private void UpdateInstanceParameters(SimInstanceParameterCollection instanceValues)
        {
            HashSet<SimBaseParameter> removeKeys = instanceValues.Keys.ToHashSet();

            //Make sure all component parameters are in instance
            foreach (var param in this.Component.Parameters)
            {
                if (!instanceValues.Contains(param))
                {
                    instanceValues.Add(param, param.Value);
                }
                removeKeys.Remove(param);
            }

            //Check if additional parameters are there which should be removed
            foreach (var rem in removeKeys)
                instanceValues.Remove(rem);
        }

        #endregion

        #region METHODS: SIZE TRANSFER

        internal SimInstanceSize ApplySizeTransferSettings(SimInstanceSize size)
        {
            List<double> sizes = size.ToList();

            for (int i = 0; i < 6; ++i)
                sizes[i] = this.EvaluateSizeTransferItem(this.SizeTransfer[(SimInstanceSizeIndex)i], sizes[i]);

            return SimInstanceSize.FromList(sizes);
        }
        /// <summary>
        /// Returns the evaluated value for a <see cref="SimInstanceSizeTransferDefinitionItem"/> (as stored in <see cref="SizeTransfer"/>).
        /// </summary>
        /// <param name="item">The definition item</param>
        /// <param name="currentSize">The current at the axis described by item</param>
        /// <returns>The evaluated value of the item</returns>
        public double EvaluateSizeTransferItem(SimInstanceSizeTransferDefinitionItem item, double currentSize)
        {
            switch (item.Source)
            {
                case SimInstanceSizeTransferSource.User:
                    return currentSize;
                case SimInstanceSizeTransferSource.Parameter:
                    if (item.Parameter != null)
                    {
                        if (item.Parameter is SimDoubleParameter doubleParam)
                        {
                            return doubleParam.Value + item.Addend;
                        }
                        else
                        {
                            throw new NotImplementedException(item.Parameter.GetType().ToString());
                        }
                    }
                    else
                    {
                        return item.Addend;
                    }

                case SimInstanceSizeTransferSource.Path:
                    return 0.0;
                default:
                    //not possible
                    throw new NotSupportedException("Unknown enum value");
            }
        }

        /// <summary>
        /// Faster method to set both, <see cref="InstanceSize"/> and <see cref="SizeTransfer"/>.
        /// Events are only issued once in this case.
        /// </summary>
        /// <param name="size">The new size</param>
        /// <param name="sizeTransfers">The new size transfer definition</param>
        public void SetSize(SimInstanceSize size, ISimInstanceSizeTransferDefinition sizeTransfers)
        {
            if (this.sizeTransfer == null)
                throw new ArgumentNullException(nameof(this.sizeTransfer));

            var instSize = size;

            if (this.sizeTransfer != sizeTransfers)
            {
                this.NotifyWriteAccess();

                this.sizeTransfer = sizeTransfers.Clone();
                instSize = this.ApplySizeTransferSettings(size);

                this.NotifyPropertyChanged(nameof(this.SizeTransfer));
                this.NotifyChanged();
            }

            this.InstanceSize = instSize;
        }

        #endregion


        /// <summary>
        /// Restores references after loading. This method restores network element ids and initializes the persistent parameters.
        /// Has to be called on instances created by the parsing ctor.
        /// </summary>
        /// <param name="networkElements">A dictionary containing all network elements in the project</param>
        public void RestoreReferences(Dictionary<SimObjectId, SimFlowNetworkElement> networkElements)
        {
            for (int i = 0; i < Placements.Count; ++i)
            {
                var valid = Placements[i].RestoreReferences(networkElements);
                if (!valid)
                {
                    Placements.RemoveAt(i);
                    i--;
                }
            }
            if (this.LoadingParameterValuesPersistent != null)
            {
                foreach (var loadingData in this.LoadingParameterValuesPersistent)
                {
                    SimBaseParameter parameter = null;

                    //For newer files: Search for parameter by id
                    if (loadingData.id != SimId.Empty)
                    {
                        parameter = this.Factory.ProjectData.IdGenerator.GetById<SimBaseParameter>(loadingData.id);
                    }
                    else //For old files: Search for parameter by name.
                    {
                        // lookup with taxonomy entry or text if it is a reserved one
                        if (ReservedParameterKeys.NameToKeyLookup.TryGetValue(loadingData.parameterName, out var key))
                        {
                            var taxentry = this.Factory.ProjectData.Taxonomies.GetReservedParameter(key);
                            // if the default tax entries were not restored yet, also check text
                            parameter = this.Component.Parameters.FirstOrDefault(x =>
                                (x.NameTaxonomyEntry.HasTaxonomyEntry && x.NameTaxonomyEntry.TaxonomyEntryReference.Target == taxentry) ||
                                (!x.NameTaxonomyEntry.HasTaxonomyEntry && x.NameTaxonomyEntry.Text == loadingData.parameterName));
                        }
                        else
                        {
                            parameter = this.Component.Parameters.FirstOrDefault(x => !x.NameTaxonomyEntry.HasTaxonomyEntry &&
                                x.NameTaxonomyEntry.Text == loadingData.parameterName);
                        }
                    }

                    if (parameter != null && parameter.Component == this.Component)
                    {
                        if (parameter is SimEnumParameter enumParam)
                        {
                            if ((long)loadingData.value != long.MinValue)
                            {
                                var referece = new SimTaxonomyEntryReference(enumParam.Items.FirstOrDefault(t => t.LocalID == (long)loadingData.value));
                                this.InstanceParameterValuesPersistent[parameter] = referece;
                            }
                        }
                        else
                        {
                            this.InstanceParameterValuesPersistent[parameter] = loadingData.value;
                        }
                    }
                }
            }

            this.SizeTransfer.RestoreReferences(this);
        }

        /// <summary>
        /// Has to be called whenever the state of the instance has changed. 
        /// </summary>
        internal void OnInstanceStateChanged()
        {
            using (AccessCheckingDisabler.Disable(this.Factory))
            {
                SimInstanceConnectionState state = SimInstanceConnectionState.Ok;
                if (this.Placements.Any(x => x.State != SimInstancePlacementState.Valid))
                {
                    state = SimInstanceConnectionState.GeometryNotFound;
                }

                bool isRealized = false;

                if (this.Component != null && this.Component.InstanceType.HasFlag(SimInstanceType.NetworkEdge)) //Only relevant for old networks
                {
                    isRealized = this.Placements.Any(x =>
                    {
                        if (x is SimInstancePlacementNetwork pln)
                        {
                            if (pln.NetworkElement is SimFlowNetworkEdge edge)
                            {
                                if (edge.Start.Content != null && edge.End.Content != null)
                                {
                                    return edge.Start.Content.State.IsRealized && edge.End.Content.State.IsRealized;
                                }
                            }
                        }
                        return false;
                    });
                }
                else
                {
                    isRealized = this.Placements.Any(x => x is SimInstancePlacementGeometry);
                }

                if (this.State.IsRealized != isRealized || this.State.ConnectionState != state)
                {
                    this.State = new SimInstanceState(isRealized, state);
                }

                if (this.Component != null)
                    this.Component.OnInstanceStateChanged();
            }
        }

        /// <summary>
        /// Resets the <see cref="InstanceParameterValuesTemporary"/> to the values stored in <see cref="InstanceParameterValuesPersistent"/>
        /// </summary>
        public void Reset()
        {
            foreach (var entry in this.InstanceParameterValuesPersistent)
                this.InstanceParameterValuesTemporary[entry.Key] = entry.Value;
        }

        #region Generated Subcomponents

        /// <summary>
        /// Adds generated parameters based on the instance type to the component
        /// </summary>
        /// <param name="component">The component to which the parameters should be added</param>
        internal static void AddAutoParameters(SimComponent component)
        {
            if (component.InstanceType.HasFlag(SimInstanceType.NetworkEdge) || component.InstanceType.HasFlag(SimInstanceType.NetworkNode))
                AddCumulativSubcomponent(component);
        }

        /// <summary>
        /// Has to be called when a property has changed that affects the cumulative subcomponent
        /// </summary>
        /// <param name="component">The component to update</param>
        internal static void UpdateAutoParameters(SimComponent component)
        {
            if (component != null && (component.InstanceType.HasFlag(SimInstanceType.NetworkNode) || component.InstanceType.HasFlag(SimInstanceType.NetworkEdge)))
            {
                if (component.Factory != null)
                {
                    //Update cumulative parameters
                    using (AccessCheckingDisabler.Disable(component.Factory))
                    {
                        CalculateAndUpdateAutoParameters(component);
                    }
                }
                else
                    CalculateAndUpdateAutoParameters(component);
            }
        }

        private static void CalculateAndUpdateAutoParameters(SimComponent component)
        {
            var cumulativeComponent = component.Components.FirstOrDefault(x => x.Component != null && x.Component.Name == "Cumulative")?.Component;
            if (cumulativeComponent != null)
            {
                //Calculate sizes
                double p_L_min_total_value = 0;
                double p_L_max_total_value = 0;
                double p_A_min_total_value = 0;
                double p_A_max_total_value = 0;
                double p_V_min_total_value = 0;
                double p_V_max_total_value = 0;

                foreach (SimComponentInstance gr in component.Instances)
                {
                    p_L_min_total_value += gr.InstanceSize.Min.Z;
                    p_L_max_total_value += gr.InstanceSize.Max.Z;
                    p_A_min_total_value += gr.InstanceSize.Min.X * gr.InstanceSize.Min.Y;
                    p_A_max_total_value += gr.InstanceSize.Max.X * gr.InstanceSize.Max.Y;
                    p_V_min_total_value += gr.InstanceSize.Min.X * gr.InstanceSize.Min.Y * gr.InstanceSize.Min.Z;
                    p_V_max_total_value += gr.InstanceSize.Max.X * gr.InstanceSize.Max.Y * gr.InstanceSize.Max.Z;
                }

                //Set sizes
                SetParameterIfExists(cumulativeComponent, ReservedParameterKeys.RP_LENGTH_MIN_TOTAL, p_L_min_total_value);
                SetParameterIfExists(cumulativeComponent, ReservedParameterKeys.RP_LENGTH_MAX_TOTAL, p_L_max_total_value);

                SetParameterIfExists(cumulativeComponent, ReservedParameterKeys.RP_AREA_MIN_TOTAL, p_A_min_total_value);
                SetParameterIfExists(cumulativeComponent, ReservedParameterKeys.RP_AREA_MAX_TOTAL, p_A_max_total_value);

                SetParameterIfExists(cumulativeComponent, ReservedParameterKeys.RP_VOLUME_MIN_TOTAL, p_V_min_total_value);
                SetParameterIfExists(cumulativeComponent, ReservedParameterKeys.RP_VOLUME_MAX_TOTAL, p_V_max_total_value);

                SetParameterIfExists(cumulativeComponent, ReservedParameterKeys.RP_COUNT, component.Instances.Count);
            }
        }


        private static void SetParameterIfExists(SimComponent component, string parameterKey, object value)
        {
            var parameter = component.Parameters.FirstOrDefault(x => x.HasReservedTaxonomyEntry(parameterKey));
            if (parameter != null)
            {
                if (parameter is SimDoubleParameter doubleParam && (value is double || value is int))
                {
                    doubleParam.Value = Convert.ToDouble(value);
                }
                else if (parameter is SimStringParameter stringParam && value is string)
                {
                    stringParam.Value = (string)value;
                }
                else if (parameter is SimIntegerParameter integerParam && value is int)
                {
                    integerParam.Value = (int)value;
                }
                else if (parameter is SimBoolParameter boolParam && value is bool)
                {
                    boolParam.Value = (bool)value;
                }
                else if (parameter is SimEnumParameter simEnumParameter && value is SimTaxonomyEntryReference refVal)
                {
                    simEnumParameter.Value = new SimTaxonomyEntryReference(refVal.Target);
                }
                else
                {
                    throw new InvalidCastException("Type of the parameter should be the proposed value´s");
                }
            }
        }

        private static void AddCumulativSubcomponent(SimComponent component)
        {
            var subComponent = component.Components.FirstOrDefault(x => x.Component != null && x.Component.Name == "Cumulative")?.Component;
            bool needsComponentCreate = subComponent == null;

            if (needsComponentCreate)
            {
                subComponent = new SimComponent();
                subComponent.Name = "Cumulative";
                subComponent.Description = "DO NOT CHANGE";
            }

            var usersWithWriteAccess = component.AccessLocal.Where(x => x.Access.HasFlag(SimComponentAccessPrivilege.Write));
            SimUserRole user = SimUserRole.ADMINISTRATOR;
            if (usersWithWriteAccess.Count() > 1)
                user = usersWithWriteAccess.First(x => x.Role != SimUserRole.ADMINISTRATOR).Role;

            subComponent.AccessLocal.ForEach(x => x.Access |= SimComponentAccessPrivilege.Read);
            subComponent.AccessLocal[user].Access |= SimComponentAccessPrivilege.Write | SimComponentAccessPrivilege.Supervize;
            subComponent.AccessLocal[SimUserRole.BUILDING_DEVELOPER].Access |= SimComponentAccessPrivilege.Release;

            // get the correct parameter set
            AddCumulativeParametersForInstancing(subComponent, component.Factory);

            if (needsComponentCreate)
            {
                // add to the parent
                var positionTax = component.Factory.ProjectData.Taxonomies.GetDefaultSlot(SimDefaultSlotKeys.GeometricReference);
                var slot = component.Components.FindAvailableSlot(positionTax, "AG{0}");
                subComponent.Slots.Add(new SimTaxonomyEntryReference(slot.SlotBase));

                using (AccessCheckingDisabler.Disable(component.Factory))
                {
                    component.Components.Add(new SimChildComponentEntry(slot, subComponent));
                }
            }
        }

        private static void AddCumulativeParametersForInstancing(SimComponent component, SimComponentCollection factory)
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            if (!HasDoubleParam(component, ReservedParameters.RP_LENGTH_MIN_TOTAL, ReservedParameterKeys.RP_LENGTH_MIN_TOTAL, "m", SimInfoFlow.Automatic))
            {
                // cumulative values over all instances
                SimDoubleParameter p11 = new SimDoubleParameter(ReservedParameterKeys.RP_LENGTH_MIN_TOTAL, "m", 0.0, 0.0, double.MaxValue);
                var taxEntry = factory.ProjectData.Taxonomies.GetReservedParameter(ReservedParameterKeys.RP_LENGTH_MIN_TOTAL);
                p11.NameTaxonomyEntry = new SimTaxonomyEntryOrString(new SimTaxonomyEntryReference(taxEntry));
                p11.IsAutomaticallyGenerated = true;
                p11.Description = "Net total length";
                p11.Category |= SimCategory.Geometry | SimCategory.Communication;
                p11.Propagation = SimInfoFlow.Automatic;
                p11.AllowedOperations = SimParameterOperations.None;
                component.Parameters.Add(p11);
            }

            if (!HasDoubleParam(component, ReservedParameters.RP_AREA_MIN_TOTAL, ReservedParameterKeys.RP_AREA_MIN_TOTAL, "m²", SimInfoFlow.Automatic))
            {
                SimDoubleParameter p12 = new SimDoubleParameter(ReservedParameterKeys.RP_AREA_MIN_TOTAL, "m²", 0.0, 0.0, double.MaxValue);
                var taxEntry = factory.ProjectData.Taxonomies.GetReservedParameter(ReservedParameterKeys.RP_AREA_MIN_TOTAL);
                p12.NameTaxonomyEntry = new SimTaxonomyEntryOrString(new SimTaxonomyEntryReference(taxEntry));
                p12.IsAutomaticallyGenerated = true;
                p12.Description = "Net total area";
                p12.Category |= SimCategory.Geometry | SimCategory.Communication;
                p12.Propagation = SimInfoFlow.Automatic;
                p12.AllowedOperations = SimParameterOperations.None;
                component.Parameters.Add(p12);
            }

            if (!HasDoubleParam(component, ReservedParameters.RP_VOLUME_MIN_TOTAL, ReservedParameterKeys.RP_VOLUME_MIN_TOTAL, "m³", SimInfoFlow.Automatic))
            {
                SimDoubleParameter p13 = new SimDoubleParameter(ReservedParameterKeys.RP_VOLUME_MIN_TOTAL, "m³", 0.0, 0.0, double.MaxValue);
                var taxEntry = factory.ProjectData.Taxonomies.GetReservedParameter(ReservedParameterKeys.RP_VOLUME_MIN_TOTAL);
                p13.NameTaxonomyEntry = new SimTaxonomyEntryOrString(new SimTaxonomyEntryReference(taxEntry));
                p13.IsAutomaticallyGenerated = true;
                p13.Description = "Net total volume";
                p13.Category |= SimCategory.Geometry | SimCategory.Communication;
                p13.Propagation = SimInfoFlow.Automatic;
                p13.AllowedOperations = SimParameterOperations.None;
                component.Parameters.Add(p13);
            }

            if (!HasDoubleParam(component, ReservedParameters.RP_LENGTH_MAX_TOTAL, ReservedParameterKeys.RP_LENGTH_MAX_TOTAL, "m", SimInfoFlow.Automatic))
            {
                SimDoubleParameter p14 = new SimDoubleParameter(ReservedParameterKeys.RP_LENGTH_MAX_TOTAL, "m", 0.0, 0.0, double.MaxValue);
                var taxEntry = factory.ProjectData.Taxonomies.GetReservedParameter(ReservedParameterKeys.RP_LENGTH_MAX_TOTAL);
                p14.NameTaxonomyEntry = new SimTaxonomyEntryOrString(new SimTaxonomyEntryReference(taxEntry));
                p14.IsAutomaticallyGenerated = true;
                p14.Description = "Gross total length";
                p14.Category |= SimCategory.Geometry | SimCategory.Communication;
                p14.Propagation = SimInfoFlow.Automatic;
                p14.AllowedOperations = SimParameterOperations.None;
                component.Parameters.Add(p14);
            }

            if (!HasDoubleParam(component, ReservedParameters.RP_AREA_MAX_TOTAL, ReservedParameterKeys.RP_AREA_MAX_TOTAL, "m²", SimInfoFlow.Automatic))
            {
                SimDoubleParameter p15 = new SimDoubleParameter(ReservedParameterKeys.RP_AREA_MAX_TOTAL, "m²", 0.0, 0.0, double.MaxValue);
                var taxEntry = factory.ProjectData.Taxonomies.GetReservedParameter(ReservedParameterKeys.RP_AREA_MAX_TOTAL);
                p15.NameTaxonomyEntry = new SimTaxonomyEntryOrString(new SimTaxonomyEntryReference(taxEntry));
                p15.IsAutomaticallyGenerated = true;
                p15.Description = "Gross total area";
                p15.Category |= SimCategory.Geometry | SimCategory.Communication;
                p15.Propagation = SimInfoFlow.Automatic;
                p15.AllowedOperations = SimParameterOperations.None;
                component.Parameters.Add(p15);
            }

            if (!HasDoubleParam(component, ReservedParameters.RP_VOLUME_MAX_TOTAL, ReservedParameterKeys.RP_VOLUME_MAX_TOTAL, "m³", SimInfoFlow.Automatic))
            {
                SimDoubleParameter p16 = new SimDoubleParameter(ReservedParameterKeys.RP_VOLUME_MAX_TOTAL, "m³", 0.0, 0.0, double.MaxValue);
                var taxEntry = factory.ProjectData.Taxonomies.GetReservedParameter(ReservedParameterKeys.RP_VOLUME_MAX_TOTAL);
                p16.NameTaxonomyEntry = new SimTaxonomyEntryOrString(new SimTaxonomyEntryReference(taxEntry));
                p16.IsAutomaticallyGenerated = true;
                p16.Description = "Gross total volume";
                p16.Category |= SimCategory.Geometry | SimCategory.Communication;
                p16.Propagation = SimInfoFlow.Automatic;
                p16.AllowedOperations = SimParameterOperations.None;
                component.Parameters.Add(p16);
            }

            if (!HasDoubleParam(component, ReservedParameters.RP_COUNT, ReservedParameterKeys.RP_COUNT, "-", SimInfoFlow.Automatic))
            {
                SimDoubleParameter p17 = new SimDoubleParameter(ReservedParameterKeys.RP_COUNT, "-", 0.0, 0.0, 10000);
                var taxEntry = factory.ProjectData.Taxonomies.GetReservedParameter(ReservedParameterKeys.RP_COUNT);
                p17.NameTaxonomyEntry = new SimTaxonomyEntryOrString(new SimTaxonomyEntryReference(taxEntry));
                p17.IsAutomaticallyGenerated = true;
                p17.Description = "Total number";
                p17.Category |= SimCategory.Geometry | SimCategory.Communication;
                p17.Propagation = SimInfoFlow.Automatic;
                p17.AllowedOperations = SimParameterOperations.None;
                component.Parameters.Add(p17);
            }
        }

        private static bool HasDoubleParam(SimComponent component, string name, string key, string unit, SimInfoFlow propagation)
        {
            return component.Parameters.OfType<SimDoubleParameter>().Any(doubleParam => (doubleParam.NameTaxonomyEntry.Text == name || doubleParam.HasReservedTaxonomyEntry(key)) && doubleParam.Unit == unit && doubleParam.Propagation == propagation);

        }

        #endregion

        /// <inheritdoc />
        protected override void NotifyWriteAccess()
        {
            if (this.Component != null)
                this.Component.RecordWriteAccess();

            base.NotifyWriteAccess();
        }
    }
}
