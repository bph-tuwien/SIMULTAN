using SIMULTAN.Data.FlowNetworks;
using SIMULTAN.Data.Users;
using SIMULTAN.Excel;
using SIMULTAN.Serializer.DXF;
using SIMULTAN.Serializer.DXF.DXFEntities;
using SIMULTAN.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Media3D;

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
    /// <para>Path</para>
    /// <para>
    /// The <see cref="InstancePath"/> contains geometric informations about the placement of an instance, depending on the current placements.
    /// For placements in a network node, the path contains the Position of the <see cref="SimFlowNetworkNode"/>.
    /// For placements in a network edge, the path contains the Position the start and end of the <see cref="SimFlowNetworkEdge"/>
    /// When the network placement also has a geometric description, the positions from the geometry are used instead.
    /// 
    /// For descriptive face or edge loops, the path contains the Boundary Vertices.
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
    public partial class SimComponentInstance : SimObjectNew<SimComponentCollection>
    {
        #region STATIC

        private static double GetPathLength(List<Point3D> _path)
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
        internal SimObjectId LoadingNetworkElementId { get; private set; }

        //parameterName is only used when the id is not set (used for loading legacy projects)
        internal List<(SimId id, string parameterName, double value)> LoadingParameterValuesPersistent { get; private set; }

        #endregion


        #region Placement

        /// <summary>
        /// Collection for managing placements of the instance. 
        /// Automatically set/unsets properties of the placement to ensure a valid two-way connection
        /// </summary>
        public class PlacementCollection : ObservableCollection<SimInstancePlacement>
        {
            private SimComponentInstance owner;

            /// <summary>
            /// Initializes a new instance of the PlacementCollection class
            /// </summary>
            /// <param name="owner">The instance to which this collection belongs</param>
            public PlacementCollection(SimComponentInstance owner)
            {
                this.owner = owner;
            }

            #region Collection Implementation

            /// <inheritdoc />
            protected override void InsertItem(int index, SimInstancePlacement item)
            {
                if (item == null)
                    throw new ArgumentNullException(nameof(item));

                owner.NotifyWriteAccess();

                base.InsertItem(index, item);
                SetValue(item);

                owner.OnInstanceStateChanged();
                owner.NotifyChanged();

                if (owner.Factory != null && item is SimInstancePlacementGeometry gp)
                    owner.Factory.ProjectData.ComponentGeometryExchange.OnPlacementAdded(gp);
            }
            /// <inheritdoc />
            protected override void RemoveItem(int index)
            {
                owner.NotifyWriteAccess();

                var oldItem = this[index];

                if (owner.Factory != null && oldItem is SimInstancePlacementGeometry gp)
                    owner.Factory.ProjectData.ComponentGeometryExchange.OnPlacementRemoved(gp);

                UnsetValue(this[index]);
                base.RemoveItem(index);
                owner.OnInstanceStateChanged();
                owner.NotifyChanged();
            }
            /// <inheritdoc />
            protected override void ClearItems()
            {
                owner.NotifyWriteAccess();

                foreach (var pl in this)
                {
                    if (owner.Factory != null && pl is SimInstancePlacementGeometry gp)
                        owner.Factory.ProjectData.ComponentGeometryExchange.OnPlacementRemoved(gp);
                    UnsetValue(pl);
                }

                base.ClearItems();
                owner.OnInstanceStateChanged();
                owner.NotifyChanged();
            }
            /// <inheritdoc />
            protected override void SetItem(int index, SimInstancePlacement item)
            {
                if (item == null)
                    throw new ArgumentNullException(nameof(item));

                owner.NotifyWriteAccess();

                if (owner.Factory != null && this[index] is SimInstancePlacementGeometry gp)
                    owner.Factory.ProjectData.ComponentGeometryExchange.OnPlacementRemoved(gp);

                UnsetValue(this[index]);
                base.SetItem(index, item);
                SetValue(item);

                owner.OnInstanceStateChanged();
                owner.NotifyChanged();

                if (owner.Factory != null && item is SimInstancePlacementGeometry gpNew)
                    owner.Factory.ProjectData.ComponentGeometryExchange.OnPlacementAdded(gpNew);
            }

            #endregion

            private void SetValue(SimInstancePlacement placement)
            {
                placement.Instance = this.owner;
            }
            private void UnsetValue(SimInstancePlacement placement)
            {
                placement.Instance = null;
            }
        }

        /// <summary>
        /// Stores the placements of this instance. See specializations of the
        /// <see cref="SimInstancePlacement"/> class for potential content.
        /// </summary>
        public PlacementCollection Placements { get; }

        #endregion

        #region PROPERTIES: Size & Orientation

        /// <summary>
        /// Stores the orientation of the instance in the geometry.
        /// This is used to orient proxy geometry attached to this component
        /// </summary>
		public Quaternion InstanceRotation
        {
            get { return this.instanceRotation; }
            set
            {
                if (this.instanceRotation != value)
                {
                    NotifyWriteAccess();

                    this.instanceRotation = value;
                    this.NotifyPropertyChanged(nameof(InstanceRotation));
                    this.NotifyChanged();
                }
            }
        }
        private Quaternion instanceRotation;

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
                if (instanceSize != value)
                {
                    this.NotifyWriteAccess();

                    this.instanceSize = value;

                    //Update cumulative sizes
                    UpdateAutoParameters(this.Component);
                    this.NotifyPropertyChanged(nameof(InstanceSize));
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
            get { return sizeTransfer; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                if (sizeTransfer != value)
                {
                    this.NotifyWriteAccess();

                    sizeTransfer = value.Clone();
                    this.InstanceSize = this.ApplySizeTransferSettings(this.instanceSize);
                    this.NotifyPropertyChanged(nameof(SizeTransfer));
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
                    this.NotifyPropertyChanged(nameof(State));
                    this.NotifyChanged();
                }
            }
        }
        private SimInstanceState state;

        /// <summary>
        /// Stores the instance type of this instance. The type has to match the <see cref="SimComponent.InstanceType"/>.
        /// </summary>
        public SimInstanceType InstanceType { get; }

        /// <summary>
        /// Stores the component to which this instance belongs.
        /// Automatically set when the instance is added to <see cref="SimComponent.Instances"/>
        /// </summary>
        public SimComponent Component
        {
            get { return component; }
            internal set
            {
                if (this.component != value)
                {
                    if (this.component != null)
                        this.Placements.ForEach(x => x.RemoveFromTarget());

                    this.component = value;
                    NotifyPropertyChanged(nameof(Component));

                    if (this.component != null)
                    {
                        this.Placements.ForEach(x => x.AddToTarget());
                        UpdateInstanceParameters(this.InstanceParameterValuesPersistent);
                        UpdateInstanceParameters(this.InstanceParameterValuesTemporary);
                    }
                }
            }

        }
        private SimComponent component;

        #endregion

        #region PROPERTIES: Path

        /// <summary>
        /// Stores geometric positions which describe this instance.
        /// Contains different values based on the InstanceType. See the class description for details.
        /// </summary>
        public List<Point3D> InstancePath
        {
            get { return this.instancePath; }
            set
            {
                if (instancePath != value)
                {
                    NotifyWriteAccess();

                    this.instancePath = value; // in the GeometryViewer
                    this.NotifyPropertyChanged(nameof(InstancePath));
                    this.NotifyChanged();

                    this.InstancePathLength = GetPathLength(this.instancePath);

                    if (SizeTransfer != null && SizeTransfer.Any(x => x.Source == SimInstanceSizeTransferSource.Path))
                        this.InstanceSize = this.ApplySizeTransferSettings(this.instanceSize);
                }
            }
        }
        private List<Point3D> instancePath;

        /// <summary>
        /// Length of the <see cref="InstancePath"/>. Only available when the Path contains at least two elements.
        /// </summary>
        public double InstancePathLength
        {
            get { return this.instancePathLength; }
            private set
            {
                this.instancePathLength = value;
                this.NotifyPropertyChanged(nameof(InstancePathLength));
                this.NotifyChanged();

                if (SizeTransfer != null && SizeTransfer.Any(x => x.Source == SimInstanceSizeTransferSource.Path))
                    this.InstanceSize = ApplySizeTransferSettings(this.instanceSize);
            }
        }
        private double instancePathLength;

        #endregion

        #region PROPERTIES: Instance Parameters

        /// <summary>
        /// Specifies whether parameter values may be propagated to this instance.
        /// 
        /// This setting is used in combination with <see cref="SimParameter.InstancePropagationMode"/> to identify 
        /// if a parameter value change should be propagated. When this property changes to True, a reevaluation of
        /// all parameters is performed.
        /// </summary>
        public bool PropagateParameterChanges 
        {
            get => propagateParameterChanges;
            set
            {
                if (propagateParameterChanges != value)
                {
                    this.propagateParameterChanges = value;

                    // if propagate, update parameters and notify
                    if (propagateParameterChanges && Component != null)
                    {
                        foreach (var updateParam in Component.Parameters)
                        {
                            if ((
                                 updateParam.InstancePropagationMode == SimParameterInstancePropagation.PropagateAlways ||
                                 updateParam.InstancePropagationMode == SimParameterInstancePropagation.PropagateIfInstance
                                ) &&
                                this.InstanceParameterValuesPersistent.Contains(updateParam))
                            {
                                this.InstanceParameterValuesPersistent.SetWithoutNotify(updateParam, updateParam.ValueCurrent);
                            }
                        }
                        if (Component.Factory != null)
                        {
                            Component.Factory.ProjectData.ComponentGeometryExchange.OnParameterValueChanged(
                                Component.Parameters.Where(x =>
                                    x.InstancePropagationMode == SimParameterInstancePropagation.PropagateAlways ||
                                    x.InstancePropagationMode == SimParameterInstancePropagation.PropagateIfInstance),
                                this
                                );
                        }
                    }

                    NotifyPropertyChanged(nameof(PropagateParameterChanges));
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
        [ExcelMappingProperty("SIM_INSTANCE_PARAMVALUESPERSISTENT", IsFilterable = false)]
        public SimInstanceParameterCollection InstanceParameterValuesPersistent { get; }

        /// <summary>
        /// Stores temporary values for all parameters in the component.
        /// Parameters are automatically added/removed.
        /// <para>To be used only for calculations within a flow network for saving intermediate values for each of the parent component's parameters.</para>
        /// <para>NOTE: Do not update every time a parameter in the parent component changes, updates have to be performed manually by 
        /// calling <see cref="Reset"/>.</para>
        /// </summary>
        [ExcelMappingProperty("SIM_INSTANCE_PARAMVALUES", IsFilterable = false)]
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
        /// Initializes a new instance of the ComponentInstance class
        /// </summary>
        private SimComponentInstance()
        {
            this.Placements = new PlacementCollection(this);
            this.InstanceParameterValuesTemporary = new SimInstanceParameterCollectionTemporary(this);
            this.InstanceParameterValuesPersistent = new SimInstanceParameterCollectionPersistent(this);

            this.InstanceType = SimInstanceType.None;
            this.InstanceSize = SimInstanceSize.Default;
            this.SizeTransfer = new SimInstanceSizeTransferDefinition();

            this.InstancePath = new List<Point3D>();
        }
        /// <summary>
        /// Initializes a new instance of the ComponentInstance class
        /// </summary>
        /// <param name="type">The instance type</param>
        public SimComponentInstance(SimInstanceType type) : this()
        {
            this.InstanceType = type;
        }

        /// <summary>
        /// Initializes a new instance of the ComponentInstance class.
        /// 
        /// Adds a <see cref="SimInstancePlacementNetwork"/> to the <see cref="Placements"/>.
        /// The instance type is determined from the networkElements type:
        /// For nodes, <see cref="SimInstanceType.NetworkNode"/> is used, 
        /// For edges, <see cref="SimInstanceType.NetworkEdge"/> is used.
        /// </summary>
        /// <param name="networkElement">The network element to which a connection should be established</param>
        /// <param name="pathOffset">Initial offset for the path. Used by subnetworks to determine the origin.</param>
        public SimComponentInstance(SimFlowNetworkElement networkElement, Point pathOffset)
            : this()
        {
            if (networkElement == null)
                throw new ArgumentNullException(nameof(networkElement));

            this.Name = string.Format("Network Placement {0}", networkElement.Name);
            this.InstanceType = (networkElement is SimFlowNetworkEdge) ? SimInstanceType.NetworkEdge : SimInstanceType.NetworkNode;
            var placement = new SimInstancePlacementNetwork(networkElement);
            this.Placements.Add(placement);
        }
        /// <summary>
        /// Initializes a new instance of the ComponentInstance class.
        /// 
        /// Adds a <see cref="SimInstancePlacementGeometry"/> to the <see cref="Placements"/>.
        /// </summary>
        /// <param name="type">The instance type</param>
        /// <param name="fileId">Id (key) of the geometry file</param>
        /// <param name="geometryId">The id of the geometry itself</param>
        /// <param name="relatedIds">A list of related ids</param>
        public SimComponentInstance(SimInstanceType type, int fileId, ulong geometryId, IEnumerable<ulong> relatedIds) : this()
        {
            if (fileId < 0)
                throw new ArgumentException(string.Format("{0} must be 0 or a positive integer", nameof(fileId)));
            if (geometryId < 0)
                throw new ArgumentException(string.Format("{0} must be 0 or a positive integer", nameof(geometryId)));

            this.Name = string.Format("Geometry Placement {0}:{1}", fileId, geometryId);
            this.InstanceType = type;
            var placement = new SimInstancePlacementGeometry(fileId, geometryId, relatedIds);
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
        /// <param name="instanceType">The instance type</param>
        /// <param name="state">The current state of the instance</param>
        /// <param name="geometryRef">Information about a geometry placement. When set to null, no <see cref="SimInstancePlacementGeometry"/> is created</param>
        /// <param name="instanceRotation">Rotation of the instance</param>
        /// <param name="instanceSize">Instance size</param>
        /// <param name="sizeTransfer">Instance size transfer settings</param>
        /// <param name="networkElementId">
        /// Id of a network element. 
        /// When set to <see cref="SimObjectId.Empty"/>, no <see cref="SimInstancePlacementNetwork"/> is created
        /// </param>
        /// <param name="propagateParamterChanges">If the parameters changes should be propagated</param>
        /// <param name="_i_path">The path (geometric information) of the instance</param>
        /// <param name="parameterValuesPersistent">A list of all persistent parameter values present in this instance</param>
        internal SimComponentInstance(long localId, string name, SimInstanceType instanceType, SimInstanceState state,
                                       (int fileId, ulong geometryId, List<ulong> relatedIds)? geometryRef,
                                       Quaternion instanceRotation,
                                       SimInstanceSize instanceSize, SimInstanceSizeTransferDefinition sizeTransfer, SimObjectId networkElementId,
                                       List<Point3D> _i_path, List<(SimId id, string parameterName, double value)> parameterValuesPersistent, bool propagateParamterChanges)
            : base(new SimId(localId))
        {
            this.Placements = new PlacementCollection(this);

            this.InstanceParameterValuesTemporary = new SimInstanceParameterCollectionTemporary(this);
            this.InstanceParameterValuesPersistent = new SimInstanceParameterCollectionPersistent(this);

            this.LoadingParameterValuesPersistent = parameterValuesPersistent;

            this.Name = name;
            this.State = state;

            if (geometryRef.HasValue)
            {
                var geometryPlacement = new SimInstancePlacementGeometry(geometryRef.Value.fileId, geometryRef.Value.geometryId, geometryRef.Value.relatedIds);
                this.Placements.Add(geometryPlacement);
            }

            this.InstanceType = instanceType;
            this.InstanceRotation = instanceRotation;

            this.InstanceSize = instanceSize.Clone();
            this.LoadingNetworkElementId = networkElementId;
            this.InstancePath = new List<Point3D>(_i_path);

            this.SizeTransfer = sizeTransfer;

            this.PropagateParameterChanges = propagateParamterChanges;
        }


        #endregion


        #region ToString

        /// <inheritdoc />
        [Obsolete]
        public override string ToString()
        {
            throw new NotImplementedException();
            // VERSION 2.
            //string output = "{" + this.Id.LocalId + ": { ";
            //output += NumberUtils.ToDisplayString(InstanceSize.Min.X, "F2") + " ";
            //output += NumberUtils.ToDisplayString(InstanceSize.Min.Y, "F2") + " ";
            //output += NumberUtils.ToDisplayString(InstanceSize.Min.Z, "F2") + " ";
            //output += NumberUtils.ToDisplayString(InstanceSize.Max.X, "F2") + " ";
            //output += NumberUtils.ToDisplayString(InstanceSize.Max.Y, "F2") + " ";
            //output += NumberUtils.ToDisplayString(InstanceSize.Max.Z, "F2") + " ";
            //output += "}, { ";

            //foreach (var entry in this.InstanceParameterValuesTemporary)
            //{
            //    output += "\"" + entry.Key + "\": " + NumberUtils.ToDisplayString(entry.Value, "F2") + " ";
            //}

            //output += "} }";

            //return output;
        }

        public virtual void AddToExport(ref StringBuilder _sb)
        {
            if (_sb == null) return;

            _sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString()); // 0
            _sb.AppendLine(ParamStructTypes.GEOM_RELATION);                           // GEOM_RELATIONSHIP

            _sb.AppendLine(((int)ParamStructCommonSaveCode.CLASS_NAME).ToString());
            _sb.AppendLine(this.GetType().ToString());

            // id, name, state
            _sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_ID).ToString());
            _sb.AppendLine(this.Id.LocalId.ToString());

            _sb.AppendLine(((int)ComponentInstanceSaveCode.NAME).ToString());
            _sb.AppendLine(this.Name);

            _sb.AppendLine(((int)ComponentInstanceSaveCode.STATE_TYPE).ToString());
            _sb.AppendLine(DXFComponentInstance.InstanceTypeToString(this.InstanceType));

            _sb.AppendLine(((int)ComponentInstanceSaveCode.STATE_ISREALIZED).ToString());
            string tmp = (this.State.IsRealized) ? "1" : "0";
            _sb.AppendLine(tmp);

            _sb.AppendLine(((int)ComponentInstanceSaveCode.STATE_CONNECTION_STATE).ToString());
            _sb.AppendLine(this.State.ConnectionState.ToString());

            // referenced geometry
            var firstGeometry = (SimInstancePlacementGeometry)this.Placements.FirstOrDefault(x => x is SimInstancePlacementGeometry);
            if (firstGeometry != null)
            {
                _sb.AppendLine(((int)ComponentInstanceSaveCode.GEOM_REF_FILE).ToString());
                _sb.AppendLine(firstGeometry.FileId.ToString());

                _sb.AppendLine(((int)ComponentInstanceSaveCode.GEOM_REF_ID).ToString());
                _sb.AppendLine(firstGeometry.GeometryId.ToString());
            }

            // ---------------------- INSTANCE INFORMATION ----------------------------- //
            // rotation
            _sb.AppendLine(((int)ComponentInstanceSaveCode.INST_ROTATION).ToString());
            _sb.AppendLine(instanceRotation.ToString(CultureInfo.InvariantCulture));

            //  size
            _sb.AppendLine(((int)ComponentInstanceSaveCode.INST_SIZE).ToString());
            _sb.AppendLine("6");

            _sb.AppendLine(((int)ParamStructCommonSaveCode.X_VALUE).ToString());
            _sb.AppendLine(DXFDecoder.DoubleToString(instanceSize.Min.X, "F8"));
            _sb.AppendLine(((int)ParamStructCommonSaveCode.X_VALUE).ToString());
            _sb.AppendLine(DXFDecoder.DoubleToString(instanceSize.Min.Y, "F8"));
            _sb.AppendLine(((int)ParamStructCommonSaveCode.X_VALUE).ToString());
            _sb.AppendLine(DXFDecoder.DoubleToString(instanceSize.Min.Z, "F8"));
            _sb.AppendLine(((int)ParamStructCommonSaveCode.X_VALUE).ToString());
            _sb.AppendLine(DXFDecoder.DoubleToString(instanceSize.Max.X, "F8"));
            _sb.AppendLine(((int)ParamStructCommonSaveCode.X_VALUE).ToString());
            _sb.AppendLine(DXFDecoder.DoubleToString(instanceSize.Max.Y, "F8"));
            _sb.AppendLine(((int)ParamStructCommonSaveCode.X_VALUE).ToString());
            _sb.AppendLine(DXFDecoder.DoubleToString(instanceSize.Max.Z, "F8"));

            // size transfer settings
            _sb.AppendLine(((int)ComponentInstanceSaveCode.INST_SIZE_TRANSSETTINGS).ToString());
            int count = 6;
            _sb.AppendLine(count.ToString());

            for (int i = 0; i < 6; ++i)
            {
                var size_tr = this.SizeTransfer[(SimInstanceSizeIndex)i];

                _sb.AppendLine(((int)ComponentInstanceSaveCode.INST_SIZE_TS_SOURCE).ToString());
                _sb.AppendLine(SimInstanceSizeTransferDefinition.SourceToString(size_tr.Source));

                if (size_tr.Parameter != null)
                {
                    _sb.AppendLine(((int)ComponentInstanceSaveCode.INST_SIZE_TS_PARAMETER).ToString());
                    _sb.AppendLine(size_tr.Parameter.LocalID.ToString());
                }

                _sb.AppendLine(((int)ComponentInstanceSaveCode.INST_SIZE_TS_CORRECT).ToString());
                _sb.AppendLine(DXFDecoder.DoubleToString(size_tr.Addend, "F8"));
            }

            var networkPlacement = (SimInstancePlacementNetwork)this.Placements.FirstOrDefault(x => x is SimInstancePlacementNetwork);
            if (networkPlacement != null)
            {
                // network element info
                _sb.AppendLine(((int)ComponentInstanceSaveCode.INST_NWE_ID).ToString());
                _sb.AppendLine(networkPlacement.NetworkElement.ID.LocalId.ToString());

                _sb.AppendLine(((int)ComponentInstanceSaveCode.INST_NWE_LOCATION).ToString());
                _sb.AppendLine(networkPlacement.NetworkElement.ID.GlobalId.ToString());
            }

            // path
            _sb.AppendLine(((int)ComponentInstanceSaveCode.INST_PATH).ToString());
            _sb.AppendLine(this.InstancePath.Count.ToString());

            foreach (Point3D vertex in this.InstancePath)
            {
                _sb.AppendLine(((int)ParamStructCommonSaveCode.X_VALUE).ToString());
                _sb.AppendLine(DXFDecoder.DoubleToString(vertex.X, "F8"));

                _sb.AppendLine(((int)ParamStructCommonSaveCode.Y_VALUE).ToString());
                _sb.AppendLine(DXFDecoder.DoubleToString(vertex.Y, "F8"));

                _sb.AppendLine(((int)ParamStructCommonSaveCode.Z_VALUE).ToString());
                _sb.AppendLine(DXFDecoder.DoubleToString(vertex.Z, "F8"));
            }

            // added 23.07.2018: instance parameters
            _sb.AppendLine(((int)ComponentInstanceSaveCode.INST_PARAMS).ToString());
            _sb.AppendLine(this.InstanceParameterValuesPersistent.Count.ToString());

            foreach (var entry in this.InstanceParameterValuesPersistent)
            {
                _sb.AppendLine(((int)ComponentInstanceSaveCode.INST_PARAM_KEY).ToString());
                _sb.AppendLine(entry.Key.Id.LocalId.ToString());

                _sb.AppendLine(((int)ComponentInstanceSaveCode.INST_PARAM_VAL).ToString());
                _sb.AppendLine(DXFDecoder.DoubleToString(entry.Value, "F8"));
            }
        }


        #endregion

        #region METHODS: Instance Definition / Update

        internal void ChangeParameterValue(SimParameter parameter)
        {
            if (parameter == null)
                throw new ArgumentNullException(nameof(parameter));

            // ToDo: Do you have to do somethign special if the PropagateParamterChanges property changes?
            /*
            if (parameter.Name == ReservedParameters.RP_INST_PROPAGATE)
            {
                this.PropagateParameterChanges = parameter.ValueCurrent != 0;

                if (this.PropagateParameterChanges) //Make sure that all parameters are updated
                {
                    foreach (var updateParam in Component.Parameters)
                    {
                        if ((
                             updateParam.InstancePropagationMode == SimParameterInstancePropagation.PropagateAlways ||
                             updateParam.InstancePropagationMode == SimParameterInstancePropagation.PropagateIfInstance
                            ) &&
                            this.InstanceParameterValuesPersistent.Contains(updateParam))
                        {
                            this.InstanceParameterValuesPersistent.SetWithoutNotify(updateParam, updateParam.ValueCurrent);
                        }
                    }

                    if (Component.Factory != null)
                    {
                        Component.Factory.ProjectData.ComponentGeometryExchange.OnParameterValueChanged(
                            Component.Parameters.Where(x =>
                                x.InstancePropagationMode == SimParameterInstancePropagation.PropagateAlways ||
                                x.InstancePropagationMode == SimParameterInstancePropagation.PropagateIfInstance),
                            this
                            );
                    }
                }
            }
            */

            if (parameter.InstancePropagationMode == SimParameterInstancePropagation.PropagateAlways ||
                (parameter.InstancePropagationMode == SimParameterInstancePropagation.PropagateIfInstance && PropagateParameterChanges))
            {
                // update value
                if (this.InstanceParameterValuesPersistent.Contains(parameter))
                    this.InstanceParameterValuesPersistent.SetWithoutNotify(parameter, parameter.ValueCurrent);
            }
        }

        internal void AddParameter(SimParameter parameter)
        {
            if (parameter == null)
                throw new ArgumentNullException(nameof(parameter));

            if (IsInstanceableParameter(parameter))
            {
                // add or update
                this.InstanceParameterValuesPersistent.Add(parameter, parameter.ValueCurrent);
                this.InstanceParameterValuesTemporary.Add(parameter, parameter.ValueCurrent);
            }
        }

        internal void RemoveParameter(SimParameter parameter)
        {
            if (parameter == null)
                throw new ArgumentNullException(nameof(parameter));

            // remove
            this.InstanceParameterValuesPersistent.Remove(parameter);
            this.InstanceParameterValuesTemporary.Remove(parameter);
        }

        /// <summary>
        /// Excludes special parameters like triangle (instance propagation)
        /// </summary>
        /// <returns>True when the parameter should be placed in the InstanceParameter* lists, otherwise False</returns>
        private static bool IsInstanceableParameter(SimParameter parameter)
        {
            return parameter.Name != ReservedParameters.RP_INST_PROPAGATE;
        }

        private void UpdateInstanceParameters(SimInstanceParameterCollection instanceValues)
        {
            HashSet<SimParameter> removeKeys = instanceValues.Keys.ToHashSet();

            //Make sure all component parameters are in instance
            foreach (var param in Component.Parameters)
            {
                if (SimComponentInstance.IsInstanceableParameter(param))
                {
                    if (!instanceValues.Contains(param))
                        instanceValues.Add(param, param.ValueCurrent);

                    removeKeys.Remove(param);
                }
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
                sizes[i] = EvaluateSizeTransferItem(SizeTransfer[(SimInstanceSizeIndex)i], sizes[i]);

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
                        return item.Parameter.ValueCurrent + item.Addend;
                    else
                        return item.Addend;
                case SimInstanceSizeTransferSource.Path:
                    return this.InstancePathLength + item.Addend;
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
            if (sizeTransfer == null)
                throw new ArgumentNullException(nameof(sizeTransfer));

            var instSize = size;

            if (sizeTransfer != sizeTransfers)
            {
                this.NotifyWriteAccess();

                sizeTransfer = sizeTransfers.Clone();
                instSize = this.ApplySizeTransferSettings(size);

                this.NotifyPropertyChanged(nameof(SizeTransfer));
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
            if (this.LoadingNetworkElementId != SimObjectId.Empty)
            {
                if (networkElements.TryGetValue(this.LoadingNetworkElementId, out var nwe))
                {
                    this.Placements.Add(new SimInstancePlacementNetwork(nwe));
                    this.LoadingNetworkElementId = SimObjectId.Empty;
                }
                else
                    Console.WriteLine("Unable to locate network element for reference");
            }
            if (this.LoadingParameterValuesPersistent != null)
            {
                foreach (var loadingData in LoadingParameterValuesPersistent)
                {
                    SimParameter parameter = null;

                    //For newer files: Search for parameter by id
                    if (loadingData.id != SimId.Empty)
                    {
                        parameter = this.Factory.ProjectData.IdGenerator.GetById<SimParameter>(loadingData.id);
                    }
                    else //For old files: Search for parameter by name.
                    {
                        parameter = this.Component.Parameters.FirstOrDefault(x => x.Name == loadingData.parameterName);
                    }

                    if (parameter != null && parameter.Component == this.Component && IsInstanceableParameter(parameter))
                        this.InstanceParameterValuesPersistent[parameter] = loadingData.value;
                }
            }

            this.SizeTransfer.RestoreReferences(this);
        }

        /// <summary>
        /// Has to be called whenever the state of the instance has changed. 
        /// </summary>
        internal void OnInstanceStateChanged()
        {
            using (AccessCheckingDisabler.Disable(Factory))
            {
                SimInstanceConnectionState state = SimInstanceConnectionState.Ok;
                if (Placements.Any(x => x.State != SimInstancePlacementState.Valid))
                {
                    state = SimInstanceConnectionState.GeometryNotFound;
                }

                bool isRealized = false;

                if (InstanceType == SimInstanceType.NetworkEdge)
                {
                    isRealized = Placements.Any(x =>
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
                    isRealized = Placements.Any(x => x is SimInstancePlacementGeometry);
                }

                if (State.IsRealized != isRealized || State.ConnectionState != state)
                {
                    State = new SimInstanceState(isRealized, state);
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
            if (component.InstanceType == SimInstanceType.NetworkEdge || component.InstanceType == SimInstanceType.NetworkNode)
                AddCumulativSubcomponent(component);
        }

        /// <summary>
        /// Has to be called when a property has changed that affects the cumulative subcomponent
        /// </summary>
        /// <param name="component">The component to update</param>
        internal static void UpdateAutoParameters(SimComponent component)
        {
            if (component != null && (component.InstanceType == SimInstanceType.NetworkNode || component.InstanceType == SimInstanceType.NetworkEdge))
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
                SetParameterIfExists(cumulativeComponent, ReservedParameters.RP_LENGTH_MIN_TOTAL, p_L_min_total_value);
                SetParameterIfExists(cumulativeComponent, ReservedParameters.RP_LENGTH_MAX_TOTAL, p_L_max_total_value);

                SetParameterIfExists(cumulativeComponent, ReservedParameters.RP_AREA_MIN_TOTAL, p_A_min_total_value);
                SetParameterIfExists(cumulativeComponent, ReservedParameters.RP_AREA_MAX_TOTAL, p_A_max_total_value);

                SetParameterIfExists(cumulativeComponent, ReservedParameters.RP_VOLUME_MIN_TOTAL, p_V_min_total_value);
                SetParameterIfExists(cumulativeComponent, ReservedParameters.RP_VOLUME_MAX_TOTAL, p_V_max_total_value);

                SetParameterIfExists(cumulativeComponent, ReservedParameters.RP_COUNT, component.Instances.Count);
            }
        }


        private static void SetParameterIfExists(SimComponent component, string parameterName, double value)
        {
            var parameter = component.Parameters.FirstOrDefault(x => x.Name == parameterName);
            if (parameter != null)
                parameter.ValueCurrent = value;
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
            AddCumulativeParametersForInstancing(subComponent);

            if (needsComponentCreate)
            {
                // add to the parent
                var slot = component.Components.FindAvailableSlot(ComponentUtils.InstanceTypeToSlotBase(SimInstanceType.NetworkNode), "AG{0}");
                subComponent.CurrentSlot = slot.SlotBase;

                using (AccessCheckingDisabler.Disable(component.Factory))
                {
                    component.Components.Add(new SimChildComponentEntry(slot, subComponent));
                }
            }
        }

        private static void AddCumulativeParametersForInstancing(SimComponent component)
        {
            if (!HasParameter(component, ReservedParameters.RP_LENGTH_MIN_TOTAL, "m", SimInfoFlow.Automatic))
            {
                // cumulative values over all instances
                SimParameter p11 = new SimParameter(ReservedParameters.RP_LENGTH_MIN_TOTAL, "m", 0.0, 0.0, double.MaxValue);
                p11.IsAutomaticallyGenerated = true;
                p11.TextValue = "Net total length";
                p11.Category |= SimCategory.Geometry | SimCategory.Communication;
                p11.Propagation = SimInfoFlow.Automatic;
                p11.AllowedOperations = SimParameterOperations.None;
                component.Parameters.Add(p11);
            }

            if (!HasParameter(component, ReservedParameters.RP_AREA_MIN_TOTAL, "m²", SimInfoFlow.Automatic))
            {
                SimParameter p12 = new SimParameter(ReservedParameters.RP_AREA_MIN_TOTAL, "m²", 0.0, 0.0, double.MaxValue);
                p12.IsAutomaticallyGenerated = true;
                p12.TextValue = "Net total area";
                p12.Category |= SimCategory.Geometry | SimCategory.Communication;
                p12.Propagation = SimInfoFlow.Automatic;
                p12.AllowedOperations = SimParameterOperations.None;
                component.Parameters.Add(p12);
            }

            if (!HasParameter(component, ReservedParameters.RP_VOLUME_MIN_TOTAL, "m³", SimInfoFlow.Automatic))
            {
                SimParameter p13 = new SimParameter(ReservedParameters.RP_VOLUME_MIN_TOTAL, "m³", 0.0, 0.0, double.MaxValue);
                p13.IsAutomaticallyGenerated = true;
                p13.TextValue = "Net total volume";
                p13.Category |= SimCategory.Geometry | SimCategory.Communication;
                p13.Propagation = SimInfoFlow.Automatic;
                p13.AllowedOperations = SimParameterOperations.None;
                component.Parameters.Add(p13);
            }

            if (!HasParameter(component, ReservedParameters.RP_LENGTH_MAX_TOTAL, "m", SimInfoFlow.Automatic))
            {
                SimParameter p14 = new SimParameter(ReservedParameters.RP_LENGTH_MAX_TOTAL, "m", 0.0, 0.0, double.MaxValue);
                p14.IsAutomaticallyGenerated = true;
                p14.TextValue = "Gross total length";
                p14.Category |= SimCategory.Geometry | SimCategory.Communication;
                p14.Propagation = SimInfoFlow.Automatic;
                p14.AllowedOperations = SimParameterOperations.None;
                component.Parameters.Add(p14);
            }

            if (!HasParameter(component, ReservedParameters.RP_AREA_MAX_TOTAL, "m²", SimInfoFlow.Automatic))
            {
                SimParameter p15 = new SimParameter(ReservedParameters.RP_AREA_MAX_TOTAL, "m²", 0.0, 0.0, double.MaxValue);
                p15.IsAutomaticallyGenerated = true;
                p15.TextValue = "Gross total area";
                p15.Category |= SimCategory.Geometry | SimCategory.Communication;
                p15.Propagation = SimInfoFlow.Automatic;
                p15.AllowedOperations = SimParameterOperations.None;
                component.Parameters.Add(p15);
            }

            if (!HasParameter(component, ReservedParameters.RP_VOLUME_MAX_TOTAL, "m³", SimInfoFlow.Automatic))
            {
                SimParameter p16 = new SimParameter(ReservedParameters.RP_VOLUME_MAX_TOTAL, "m³", 0.0, 0.0, double.MaxValue);
                p16.IsAutomaticallyGenerated = true;
                p16.TextValue = "Gross total volume";
                p16.Category |= SimCategory.Geometry | SimCategory.Communication;
                p16.Propagation = SimInfoFlow.Automatic;
                p16.AllowedOperations = SimParameterOperations.None;
                component.Parameters.Add(p16);
            }

            if (!HasParameter(component, ReservedParameters.RP_COUNT, "-", SimInfoFlow.Automatic))
            {
                SimParameter p17 = new SimParameter(ReservedParameters.RP_COUNT, "-", 0.0, 0.0, 10000);
                p17.IsAutomaticallyGenerated = true;
                p17.TextValue = "Total number";
                p17.Category |= SimCategory.Geometry | SimCategory.Communication;
                p17.Propagation = SimInfoFlow.Automatic;
                p17.AllowedOperations = SimParameterOperations.None;
                component.Parameters.Add(p17);
            }
        }
        private static bool HasParameter(SimComponent component, string name, string unit, SimInfoFlow propagation)
        {
            return component.Parameters.Any(x => x.Name == name && x.Unit == unit && x.Propagation == propagation);
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
