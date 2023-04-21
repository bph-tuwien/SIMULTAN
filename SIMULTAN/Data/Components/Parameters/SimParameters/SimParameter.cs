//using SIMULTAN.Data.Geometry;
//using SIMULTAN.Data.Taxonomy;
//using SIMULTAN.Data.Users;
//using SIMULTAN.Excel;
//using System;
//using System.Collections.Generic;
//using System.Linq;

//namespace SIMULTAN.Data.Components
//{
//    //Uses SimObjectNew<ISimManagedCollection> because it can either be part of a ComponentFactory or ParameterFactory

//    /// <summary>
//    /// Stores a numeric and a textual value inside a component.
//    /// Also provides logic to reference other parameters or ValueFields
//    /// 
//    /// Parameters store a numeric value and a textual value at the same time. Depending on how the parameter is used,
//    /// either <see cref="ValueCurrent"/> (the numerical value) or <see cref="TextValue"/> (the textual value) is used.
//    /// 
//    /// Parameters can be configured to take the value from a ValueField. When the <see cref="ValueSource"/> property is set to Null,
//    /// the user is assumed to set the value directly. When <see cref="ValueSource"/> is not Null, the value is taken from the MultiValue.
//    /// Changes to attached MultiValues are automatically propagated to the <see cref="ValueCurrent"/> property.
//    /// 
//    /// A parameter can be locked for certain operations. See <see cref="AllowedOperations"/> for details.
//    /// 
//    /// Each parameter has a override value stored in each <see cref="SimComponentInstance"/>. Depending on the <see cref="InstancePropagationMode"/>,
//    /// the <see cref="ValueCurrent"/> is propagated to the instances.
//    /// </summary>
//    public class SimParameter : SimObjectNew<ISimManagedCollection>
//    {
//        //~SimParameter() { Console.WriteLine("~SimParameter");}
//        #region Properties

//        /// <summary>
//        /// The <see cref="NameTaxonomyEntry"/> of the parameter
//        /// </summary>
//        [ExcelMappingProperty("SIM_OBJECT_NAME", PropertyType = typeof(String))]
//        public SimTaxonomyEntryOrString NameTaxonomyEntry
//        {
//            get
//            {
//                return taxonomyEntry;
//            }
//            set
//            {
//                NotifyWriteAccess();

//                if (taxonomyEntry.HasTaxonomyEntryReference())
//                {
//                    NameTaxonomyEntry.TaxonomyEntryReference.RemoveDeleteAction();
//                }

//                taxonomyEntry = value;

//                if (taxonomyEntry.HasTaxonomyEntry())
//                {
//                    taxonomyEntry.TaxonomyEntryReference.SetDeleteAction(TaxonomyEntryDeleted);
//                }

//                UpdateState();
//                NotifyPropertyChanged(nameof(NameTaxonomyEntry));
//                NotifyChanged();
//            }
//        }
//        private SimTaxonomyEntryOrString taxonomyEntry;

//        /// <summary>
//        /// The unit of the parameter
//        /// </summary>
//		[ExcelMappingProperty("SIM_PARAMETER_UNIT")]
//        public string Unit
//        {
//            get { return this.unit; }
//            set
//            {
//                if (this.unit != value)
//                {
//                    NotifyWriteAccess();

//                    this.unit = value;
//                    this.NotifyPropertyChanged(nameof(Unit));

//                    this.NotifyChanged();
//                }
//            }
//        }
//        private string unit;

//        /// <summary>
//        /// The category of the parameter
//        /// </summary>
//		[ExcelMappingProperty("SIM_PARAMETER_CATEGORY")]
//        public SimCategory Category
//        {
//            get { return this.category; }
//            set
//            {
//                if (this.category != value)
//                {
//                    this.NotifyWriteAccess();

//                    this.category = value;

//                    this.NotifyPropertyChanged(nameof(Category));
//                    this.NotifyChanged();
//                }
//            }
//        }
//        private SimCategory category;

//        /// <summary>
//        /// Defines how the parameter may be used. See InfoFlow for more details
//        /// </summary>
//		[ExcelMappingProperty("SIM_PARAMETER_PROPAGATION")]
//        public SimInfoFlow Propagation
//        {
//            get { return this.propagation; }
//            set
//            {
//                if (this.propagation != value)
//                {
//                    this.NotifyWriteAccess();

//                    this.propagation = value;
//                    this.NotifyPropertyChanged(nameof(Propagation));

//                    UpdateState();
//                    this.NotifyChanged();
//                }
//            }
//        }
//        private SimInfoFlow propagation;

//        /// <summary>
//        /// Stores the component to which the parameter is attached
//        /// </summary>
//        public SimComponent Component
//        {
//            get { return component; }
//            internal set
//            {
//                component = value;
//                UpdateState();

//                this.ValueSource?.OnParameterComponentChanged(this.Component);
//                this.NotifyPropertyChanged(nameof(Component));
//            }
//        }
//        private SimComponent component;

//        /// <summary>
//        /// Stores which operations are allowed for this parameter. Used to mark readonly or ValuePointer parameters
//        /// </summary>
//        public SimParameterOperations AllowedOperations
//        {
//            get { return allowedOperations; }
//            set
//            {
//                this.NotifyWriteAccess();

//                allowedOperations = value;

//                NotifyPropertyChanged(nameof(AllowedOperations));
//                this.NotifyChanged();
//            }
//        }
//        private SimParameterOperations allowedOperations;

//        /// <summary>
//        /// Specifies when a parameter value should be propagated to instances
//        /// </summary>
//        public SimParameterInstancePropagation InstancePropagationMode
//        {
//            get { return instancePropagationMode; }
//            set
//            {
//                this.NotifyWriteAccess();

//                if (instancePropagationMode != value)
//                {
//                    instancePropagationMode = value;

//                    NotifyPropertyChanged(nameof(InstancePropagationMode));
//                    this.NotifyChanged();
//                }
//            }
//        }
//        private SimParameterInstancePropagation instancePropagationMode = SimParameterInstancePropagation.PropagateIfInstance;

//        /// <summary>
//        /// The current state of the paramter
//        /// </summary>
//        public SimParameterState State
//        {
//            get { return state; }
//            private set
//            {
//                if (state != value)
//                {
//                    state = value;
//                    NotifyPropertyChanged(nameof(State));
//                }
//            }
//        }
//        private SimParameterState state;

//        /// <summary>
//        /// Returns True when the parameter has been generated automatically. Used to supress certain state warnings
//        /// </summary>
//        public bool IsAutomaticallyGenerated
//        {
//            get { return isAutomaticallyGenerated; }
//            set
//            {
//                if (isAutomaticallyGenerated != value)
//                {
//                    this.NotifyWriteAccess();

//                    isAutomaticallyGenerated = value;

//                    NotifyPropertyChanged(nameof(IsAutomaticallyGenerated));

//                    UpdateState();
//                    this.NotifyChanged();
//                }
//            }
//        }
//        private bool isAutomaticallyGenerated = false;

//        /// <summary>
//        /// The textual value of the parameter
//        /// </summary>
//        [ExcelMappingProperty("SIM_PARAMETER_TEXTVALUE")]
//        public string TextValue
//        {
//            get { return this.textValue; }
//            set
//            {
//                if (this.textValue != value)
//                {
//                    this.NotifyWriteAccess();

//                    this.textValue = value;

//                    this.NotifyPropertyChanged(nameof(TextValue));
//                    this.NotifyChanged();
//                }
//            }
//        }
//        private string textValue;

//        /// <summary>
//        /// The numerical value of the parameter (default: 0.0)
//        /// </summary>
//        [ExcelMappingProperty("SIM_PARAMETER_VALUECURRENT")]
//        public double ValueCurrent
//        {
//            get { return this.value; }
//            set
//            {
//                if (!HasSameCurrentValue(value))
//                {
//                    this.NotifyWriteAccess();

//                    this.value = value;
//                    this.NotifyPropertyChanged(nameof(ValueCurrent));
//                    UpdateState();
//                    this.NotifyChanged();

//                    //Notify geometry exchange
//                    // if (this.Component != null && this.Component.Factory != null)
//                    //    this.Component.Factory.ProjectData.ComponentGeometryExchange.OnParameterValueChanged(this);
//                }
//            }
//        }
//        private double value;

//        /// <summary>
//        /// The maximum value of the parameter. Does not prevent setting a higher ValueCurrent, but sets the State to OutOfRange
//        /// </summary>
//        [ExcelMappingProperty("SIM_PARAMETER_VALUEMAX")]
//        public double ValueMax
//        {
//            get { return this.valueMax; }
//            set
//            {
//                if (this.valueMax != value)
//                {
//                    this.NotifyWriteAccess();

//                    this.valueMax = value;
//                    this.NotifyPropertyChanged(nameof(ValueMax));

//                    UpdateState();
//                    this.NotifyChanged();
//                }
//            }
//        }
//        private double valueMax;

//        /// <summary>
//        /// The minimum value of the parameter. Does not prevent setting a lower ValueCurrent, but sets the State to OutOfRange
//        /// </summary>
//		[ExcelMappingProperty("SIM_PARAMETER_VALUEMIN")]
//        public double ValueMin
//        {
//            get { return this.valueMin; }
//            set
//            {
//                if (this.valueMin != value)
//                {
//                    this.NotifyWriteAccess();

//                    this.valueMin = value;
//                    this.NotifyPropertyChanged(nameof(ValueMin));

//                    UpdateState();
//                    this.NotifyChanged();
//                }
//            }
//        }
//        private double valueMin;

//        /// <summary>
//        /// Stores the ValuePointer for this parameter. When null, the parameter is not bound to a ValueField
//        /// </summary>
//        //public SimParameterValueSource ValueSource
//        //{
//        //    get
//        //    {
//        //        return multiValuePointer;
//        //    }
//        //    set
//        //    {
//        //        this.NotifyWriteAccess();

//        //        if (this.multiValuePointer != null)
//        //        {
//        //            if (this.multiValuePointer is SimGeometryParameterSource gps)
//        //                this.Factory?.ProjectData.ComponentGeometryExchange.OnParameterSourceRemoved(gps);
//        //            this.multiValuePointer.TargetParameter = null;
//        //        }

//        //        this.multiValuePointer = value;
//        //        NotifyPropertyChanged(nameof(ValueSource));
//        //        this.NotifyChanged();

//        //        if (this.multiValuePointer != null)
//        //        {
//        //            // this.multiValuePointer.TargetParameter = this;
//        //            if (this.multiValuePointer is SimGeometryParameterSource gps)
//        //                this.Factory?.ProjectData.ComponentGeometryExchange.OnParameterSourceAdded(gps);
//        //        }
//        //    }
//        //}
//        //private SimParameterValueSource multiValuePointer;

//        /// <summary>
//        /// Stores all calculations referencing this parameter either as an input or as an output
//        /// </summary>
//        public IReadOnlyList<SimCalculation> ReferencingCalculations { get { return this.referencingCalculations; } }
//        /// <summary>
//        /// Stores all calculations referencing this parameter either as an input or as an output.
//        /// Same as <see cref="ReferencingCalculations"/>, but allows for writing access.
//        /// </summary>
//        internal List<SimCalculation> ReferencingCalculations_Internal { get { return this.referencingCalculations; } }
//        private List<SimCalculation> referencingCalculations = new List<SimCalculation>();

//        #endregion

//        #region EVENTS

//        /// <summary>
//        /// Handler for the IsBeingDelted event.
//        /// </summary>
//        /// <param name="sender"></param>
//        public delegate void IsBeingDeletedEventHandler(object sender);
//        /// <summary>
//        /// Emitted just before the parameter is being deleted.
//        /// </summary>
//        public event IsBeingDeletedEventHandler IsBeingDeleted;
//        /// <summary>
//        /// Emits the IsBeingDelted event.
//        /// </summary>
//        public void OnIsBeingDeleted()
//        {
//            this.IsBeingDeleted?.Invoke(this);
//        }

//        /// <inheritdoc />
//        protected override void NotifyPropertyChanged(string property)
//        {
//            base.NotifyPropertyChanged(property);

//            if (this.Component != null)
//            {
//                //  this.Component.Factory?.NotifyParameterPropertyChanged(this, property);
//                this.Component.Parameters.OnParameterPropertyChanged(this, property);
//            }
//        }

//        #endregion


//        #region .CTOR

//        /// <summary>
//        /// Initializes a new instance of the SimParameter class
//        /// </summary>
//        /// <param name="name">The name of the parameter</param>
//        /// <param name="unit">Unit of the parameter</param>
//        /// <param name="value">The current value of the parameter</param>
//        /// <param name="allowedOperations">The operations the user is expected to perform on this parameter</param>
//        public SimParameter(string name, string unit, double value, SimParameterOperations allowedOperations = SimParameterOperations.All)
//            : this(name, unit, value, double.NegativeInfinity, double.PositiveInfinity, allowedOperations)
//        { }

//        /// <summary>
//        /// Initializes a new instance of the SimParameter class
//        /// </summary>
//        /// <param name="nameTaxonomyEntry">The name taxonomy entry of the parameter</param>
//        /// <param name="unit">Unit of the parameter</param>
//        /// <param name="value">The current value of the parameter</param>
//        /// <param name="allowedOperations">The operations the user is expected to perform on this parameter</param>
//        public SimParameter(SimTaxonomyEntry nameTaxonomyEntry, string unit, double value, SimParameterOperations allowedOperations = SimParameterOperations.All)
//            : this(nameTaxonomyEntry, unit, value, double.NegativeInfinity, double.PositiveInfinity, allowedOperations)
//        { }

//        /// <summary>
//        /// Initializes a new instance of the SimParameter class
//        /// </summary>
//        /// <param name="name">The name of the parameter</param>
//        /// <param name="unit">Unit of the parameter</param>
//        /// <param name="value">The current value of the parameter</param>
//        /// <param name="allowedOperations">The operations the user is expected to perform on this parameter</param>
//        public SimParameter(string name, string unit, string value, SimParameterOperations allowedOperations = SimParameterOperations.All)
//        {
//            NameTaxonomyEntry = new SimTaxonomyEntryOrString(name);
//            this.Unit = unit;
//            this.Category = SimCategory.None;
//            this.Propagation = SimInfoFlow.Mixed;

//            this.ValueMin = double.NegativeInfinity;
//            this.ValueMax = double.PositiveInfinity;
//            this.ValueCurrent = 0.0;
//            this.TextValue = value;

//            this.AllowedOperations = allowedOperations;

//            this.ValueSource = null;

//            UpdateState();
//        }

//        /// <summary>
//        /// Initializes a new instance of the SimParameter class
//        /// </summary>
//        /// <param name="nameTaxonomyEntry">The name taxonomy entry of the parameter</param>
//        /// <param name="unit">Unit of the parameter</param>
//        /// <param name="value">The current value of the parameter</param>
//        /// <param name="allowedOperations">The operations the user is expected to perform on this parameter</param>
//        public SimParameter(SimTaxonomyEntry nameTaxonomyEntry, string unit, string value, SimParameterOperations allowedOperations = SimParameterOperations.All)
//        {
//            NameTaxonomyEntry = new SimTaxonomyEntryOrString(nameTaxonomyEntry);
//            this.Unit = unit;
//            this.Category = SimCategory.None;
//            this.Propagation = SimInfoFlow.Mixed;

//            this.ValueMin = double.NegativeInfinity;
//            this.ValueMax = double.PositiveInfinity;
//            this.ValueCurrent = 0.0;
//            this.TextValue = value;

//            this.AllowedOperations = allowedOperations;

//            this.ValueSource = null;

//            UpdateState();
//        }

//        /// <summary>
//        /// Initializes a new instance of the SimParameter class
//        /// </summary>
//        /// <param name="name">The name of the parameter</param>
//        /// <param name="unit">Unit of the parameter</param>
//        /// <param name="value">The current value of the parameter</param>
//        /// <param name="minValue">The minimum valid value</param>
//        /// <param name="maxValue">The maximum valid value</param>
//        /// <param name="allowedOperations">The operations the user is expected to perform on this parameter</param>
//        public SimParameter(string name, string unit, double value, double minValue, double maxValue,
//            SimParameterOperations allowedOperations = SimParameterOperations.All)
//        {
//            NameTaxonomyEntry = new SimTaxonomyEntryOrString(name);
//            this.Unit = unit;
//            this.Category = SimCategory.None;
//            this.Propagation = SimInfoFlow.Mixed;

//            this.ValueMin = Math.Min(minValue, maxValue);
//            this.ValueMax = Math.Max(minValue, maxValue);
//            this.ValueCurrent = value;
//            this.TextValue = string.Empty;

//            this.AllowedOperations = allowedOperations;

//            this.ValueSource = null;

//            UpdateState();
//        }

//        /// <summary>
//        /// Initializes a new instance of the SimParameter class
//        /// </summary>
//        /// <param name="nameTaxonomyEntry">The name taxonomy entry of the parameter</param>
//        /// <param name="unit">Unit of the parameter</param>
//        /// <param name="value">The current value of the parameter</param>
//        /// <param name="minValue">The minimum valid value</param>
//        /// <param name="maxValue">The maximum valid value</param>
//        /// <param name="allowedOperations">The operations the user is expected to perform on this parameter</param>
//        public SimParameter(SimTaxonomyEntry nameTaxonomyEntry, string unit, double value, double minValue, double maxValue,
//            SimParameterOperations allowedOperations = SimParameterOperations.All)
//        {
//            NameTaxonomyEntry = new SimTaxonomyEntryOrString(nameTaxonomyEntry);
//            this.Unit = unit;
//            this.Category = SimCategory.None;
//            this.Propagation = SimInfoFlow.Mixed;

//            this.ValueMin = Math.Min(minValue, maxValue);
//            this.ValueMax = Math.Max(minValue, maxValue);
//            this.ValueCurrent = value;
//            this.TextValue = string.Empty;

//            this.AllowedOperations = allowedOperations;

//            this.ValueSource = null;

//            UpdateState();
//        }

//        /// <summary>
//        /// Initializes a new instance of the SimParameter class
//        /// </summary>
//        /// <param name="localId">The local Id (may only be different from 0 during loading)</param>
//        /// <param name="name">The name of the parameter</param>
//        /// <param name="unit">Unit of the parameter</param>
//        /// <param name="category">The category of the parameter</param>
//        /// <param name="propagation">The way in which the parameter may be accessed</param>
//        /// <param name="value">The current value of the parameter</param>
//        /// <param name="minValue">The minimum valid value</param>
//        /// <param name="maxValue">The maximum valid value</param>
//        /// <param name="textValue">The textual value of the parameter</param>
//        /// <param name="valueFieldPointer">A pointer to a valid field. When set, the value is ignored. Pass null when no pointer exists</param>
//        /// <param name="allowedOperations">The operations the user is expected to perform on this parameter</param>
//        /// <param name="instancePropagationMode">The instance value propagation mode for this parameter</param>
//        /// <param name="isAutomaticallyGenerated">When set to True, the parameter is marked as being automatically generated</param>
//        internal SimParameter(long localId, string name, string unit, SimCategory category, SimInfoFlow propagation,
//                           double value, double minValue, double maxValue,
//                           string textValue, SimParameterValueSource valueFieldPointer,
//                           SimParameterOperations allowedOperations = SimParameterOperations.All,
//                           SimParameterInstancePropagation instancePropagationMode = SimParameterInstancePropagation.PropagateIfInstance,
//                           bool isAutomaticallyGenerated = false)
//            : base(new SimId(localId))
//        {

//            NameTaxonomyEntry = new SimTaxonomyEntryOrString(name);
//            this.Unit = unit;
//            this.Category = category;
//            this.Propagation = propagation;
//            this.InstancePropagationMode = instancePropagationMode;

//            this.ValueMin = minValue;
//            this.ValueMax = maxValue;
//            this.ValueCurrent = value;

//            this.ValueSource = valueFieldPointer;
//            the value field will be set later(DO NOT FORGET TO OVERRIDE THE VALUE FIELD POINTER!!!)

//            this.TextValue = textValue;

//            this.AllowedOperations = allowedOperations;
//            this.IsAutomaticallyGenerated = isAutomaticallyGenerated;

//            UpdateState();
//        }

//        /// <summary>
//        /// Initializes a new instance of the Parameter class by copying all settings from another paramter
//        /// </summary>
//        /// <param name="original">The parameter to copy from</param>
//        protected SimParameter(SimParameter original)
//        {
//            this.NameTaxonomyEntry = new SimTaxonomyEntryOrString(original.NameTaxonomyEntry);
//            this.Unit = original.Unit;
//            this.Category = original.Category;
//            this.Propagation = original.Propagation;

//            this.ValueMin = original.ValueMin;
//            this.ValueMax = original.ValueMax;
//            this.ValueCurrent = original.ValueCurrent;
//            this.TextValue = original.TextValue;

//            this.ValueSource = original.ValueSource?.Clone();

//            this.allowedOperations = original.allowedOperations;
//            UpdateState();
//        }

//        /// <summary>
//        /// Creates a copy of the current parameter
//        /// </summary>
//        /// <returns>A copy of the parameter</returns>
//        public SimParameter Clone()
//        {
//            return new SimParameter(this);
//        }

//        #endregion


//        #region Property management

//        /// <summary>
//        /// Called when the referenced TaxonomyEntry got deleted.
//        /// Handed to the TaxonomyEntryReference to be used as a callback.
//        /// </summary>
//        private void TaxonomyEntryDeleted(SimTaxonomyEntry caller)
//        {
//            this.NameTaxonomyEntry = new SimTaxonomyEntryOrString(this.NameTaxonomyEntry.Name);
//        }

//        /// <summary>
//        /// Tests if the value of the parameter is the same as the supplied value.
//        /// Handles NaN values correctly
//        /// </summary>
//        /// <param name="value">The value to test against</param>
//        /// <returns>True when either the value of both is equal, or when both values are double.Nan</returns>
//        internal bool HasSameCurrentValue(double value)
//        {
//            return (double.IsNaN(this.ValueCurrent) && double.IsNaN(value)) || Math.Abs(this.ValueCurrent - value) < 0.00000001;
//        }

//        /// <summary>
//        /// Updates the state property
//        /// </summary>
//		public void UpdateState()
//        {
//            SimParameterState newState = SimParameterState.Valid;

//            if (double.IsNaN(ValueCurrent))
//            {
//                if (this.AllowedOperations.HasFlag(SimParameterOperations.EditValue))
//                    newState |= SimParameterState.ValueNaN;
//            }
//            else if (SanitizedDouble(ValueCurrent) < SanitizedDouble(ValueMin) || SanitizedDouble(ValueCurrent) > SanitizedDouble(ValueMax))
//            {
//                if (this.AllowedOperations.HasFlag(SimParameterOperations.EditValue))
//                    newState |= SimParameterState.ValueOutOfRange;
//            }

//            if (this.Component != null)
//            {
//                if (this.Propagation != SimInfoFlow.FromReference && this.Propagation != SimInfoFlow.Automatic &&
//                    !this.IsAutomaticallyGenerated)
//                {
//                    foreach (var referenced in this.Component.ReferencedComponents.Where(x => x.Target != null))
//                    {
//                        if (referenced.Target.Parameters.Any(x => x.NameTaxonomyEntry.Equals(NameTaxonomyEntry)))
//                        {
//                            newState |= SimParameterState.HidesReference;
//                            break;
//                        }
//                    }
//                }
//                else if (this.Propagation == SimInfoFlow.FromReference)
//                {
//                    var refTarget = GetReferencedParameter();
//                    if (refTarget == null)
//                    {
//                        newState |= SimParameterState.ReferenceNotFound;
//                    }
//                }
//            }

//            State = newState;
//        }

//        private double SanitizedDouble(double value)
//        {
//            if (value == double.MaxValue)
//                return double.PositiveInfinity;
//            if (value == double.MinValue)
//                return double.NegativeInfinity;
//            return value;
//        }

//        /// <summary>
//        /// Restores references to other connected parts, f.e. TaxonomyEntries
//        /// </summary>
//        /// <param name="idGenerator">The idGenerator used to look up the references</param>
//        public void RestoreReferences(SimIdGenerator idGenerator)
//        {
//            if (NameTaxonomyEntry.HasTaxonomyEntryReference())
//            {
//                var entry = idGenerator.GetById<SimTaxonomyEntry>(NameTaxonomyEntry.TaxonomyEntryReference.TaxonomyEntryId);
//                NameTaxonomyEntry = new SimTaxonomyEntryOrString(new SimTaxonomyEntryReference(entry));
//            }
//        }

//        /// <summary>
//        /// Looks up taxonomy entries for reserved parameters by their name.
//        /// Do this if the default taxonomies changed, could mean that the project is migrated.
//        /// </summary>
//        /// <exception cref="Exception">If the default taxonomy entry could not be found.</exception>
//        public void RestoreDefaultTaxonomyReferences()
//        {
//            if (Component.InstanceType != SimInstanceType.None || Component.Name == "Cumulative")
//            {
//                if (ReservedParameterKeys.NameToKeyLookup.TryGetValue(NameTaxonomyEntry.Name, out var key))
//                {
//                    var taxonomy = Factory.ProjectData.Taxonomies.GetTaxonomyByKeyOrName(ReservedParameterKeys.RP_TAXONOMY_KEY);
//                    var entry = taxonomy.GetTaxonomyEntryByKey(key);
//                    if (entry != null)
//                    {
//                        NameTaxonomyEntry = new SimTaxonomyEntryOrString(new SimTaxonomyEntryReference(entry));
//                    }
//                    else
//                    {
//                        throw new Exception("Could not find reserved taxonomy entry for parameter " + NameTaxonomyEntry.Name);
//                    }
//                }
//            }

//        }

//        /// <summary>
//        /// Returns if the parameter has a default reserved taxonomy entry of the provided key from <see cref="ReservedParameterKeys"/>.
//        /// </summary>
//        /// <param name="entryKey">The key of the taxonomy entry to check. Should be from <see cref="ReservedParameterKeys"/>.</param>
//        /// <returns>if the parameter has a default reserved taxonomy entry of the provided key.</returns>
//        public bool HasReservedTaxonomyEntry(String entryKey)
//        {
//            return taxonomyEntry.HasTaxonomyEntry() &&
//                taxonomyEntry.TaxonomyEntryReference.Target.Taxonomy.Key == ReservedParameterKeys.RP_TAXONOMY_KEY &&
//                taxonomyEntry.TaxonomyEntryReference.Target.Key == entryKey;
//        }

//        #endregion

//        #region INFO

//        /// <summary>
//        /// Returns the referenced parameter in case Propagation is set to REF_IN.
//        /// </summary>
//        /// <returns>The referenced parameter, or NULL when no such parameter exists. Returns itself for other propagations than REF_IN</returns>
//        public SimParameter GetReferencedParameter()
//        {
//            if (this.Component == null)
//                throw new InvalidOperationException("Operation may only be called when the parameter is part of a component");

//            if (this.Propagation != SimInfoFlow.FromReference)
//                return this;

//            //Search up the tree for references. Return the first parameter which matches the name
//            var component = this.Component;
//            while (component != null)
//            {
//                foreach (var refComp in component.ReferencedComponents)
//                {
//                    if (refComp.Target != null)
//                    {
//                        var matchingParam = refComp.Target.Parameters.FirstOrDefault(x => x.NameTaxonomyEntry.Equals(NameTaxonomyEntry));
//                        if (matchingParam != null)
//                            return this;// matchingParam;
//                    }
//                }

//                component = component.Parent;
//            }

//            return null;
//        }

//        #endregion

//        #region Access Management

//        /// <inheritdoc />
//        protected override void NotifyWriteAccess()
//        {
//            if (this.Component != null)
//                this.Component.RecordWriteAccess();
//            base.NotifyWriteAccess();
//        }

//        /// <summary>
//		/// Checks if the user has permission to access the parameter
//		/// </summary>
//		/// <param name="user">The user</param>
//		/// <param name="permission">The permission</param>
//		/// <returns>True when either the parameter is not attached to a component or when the user has access to that component</returns>
//		public bool HasAccess(SimUser user, SimComponentAccessPrivilege permission)
//        {
//            if (this.Component == null)
//                return true;
//            return this.Component.HasAccess(user, permission);
//        }

//        #endregion
//    }
//}