using SIMULTAN.Data.Geometry;
using SIMULTAN.Data.Taxonomy;
using SIMULTAN.Data.Users;
using System;
using System.Linq;

namespace SIMULTAN.Data.Components
{
    /// <summary>
    /// Abstract class of SimParameters which handles type parameter T
    /// <see cref="SimDoubleParameter"/>
    /// <see cref="SimIntegerParameter"/>
    /// <see cref="SimStringParameter"/>
    /// <see cref="SimBoolParameter"/>
    /// <see cref="SimEnumParameter"/>
    /// </summary>
    /// <typeparam name="T">The type of the stored value</typeparam>
    public abstract class SimBaseParameter<T> : SimBaseParameter
    {
        #region ValueType

        /// <summary>
        /// The value of the parameter. Type depends on the implementation. 
        /// </summary>
        public virtual new T Value
        {
            get { return value; }
            set
            {
                if (!IsSameValue(this.value, value))
                {
                    this.NotifyWriteAccess();

                    this.value = value;
                    this.NotifyPropertyChanged(nameof(Value));
                    UpdateState();
                    this.NotifyChanged();

                    //Notify geometry exchange
                    if (this.Component != null && this.Component.Factory != null)
                        this.Component.Factory.ProjectData.ComponentGeometryExchange.OnParameterValueChanged(this);
                }
            }
        }
        /// <summary>
        /// The value of the parameter
        /// </summary>
        protected T value;

        /// <inheritdoc/>
        protected override object GetValue()
        {
            return this.Value;
        }

        /// <summary>
        /// Tests if two values are equal
        /// </summary>
        /// <param name="value1">The first value to test against</param>
        /// <param name="value2">The second value to test against</param>
        /// <returns>True when either the values are equal</returns>
        internal abstract bool IsSameValue(T value1, T value2);

        /// <summary>
        /// Stores the component to which the parameter is attached
        /// </summary>
        public override SimComponent Component
        {
            get { return component; }
            internal set
            {
                component = value;
                UpdateState();

                this.ValueSource?.OnParameterComponentChanged(this.Component);
                this.NotifyPropertyChanged(nameof(Component));
            }
        }
        private SimComponent component;

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="SimBaseParameter{T}"/> class
        /// </summary>
        /// <param name="nameTaxonomyEntry">The name taxonomy entry of the parameter</param>
        /// <param name="value">The current value of the parameter</param>
        /// <param name="allowedOperations">The operations the user is expected to perform on this parameter</param>
        public SimBaseParameter(SimTaxonomyEntry nameTaxonomyEntry, T value, SimParameterOperations allowedOperations = SimParameterOperations.All)
            : base(nameTaxonomyEntry, allowedOperations)
        {
            this.Value = value;
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="SimBaseParameter{T}"/> class
        /// </summary>
        /// <param name="name">The name of the parameter</param>
        /// <param name="value">The current value of the parameter</param>
        /// <param name="allowedOperations">The operations the user is expected to perform on this parameter</param>
        public SimBaseParameter(string name, T value, SimParameterOperations allowedOperations = SimParameterOperations.All)
            : base(name, allowedOperations)
        {
            this.Value = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SimBaseParameter{T}"/> class by copying all settings from another parameter
        /// </summary>
        /// <param name="original">The parameter to copy from</param>
        /// <param name="copyValue">When set to true, the value is copied, otherwise the value stays at the default value</param>
        protected SimBaseParameter(SimBaseParameter<T> original, bool copyValue = true) : base(original)
        {
            if (copyValue)
                this.Value = original.Value;
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="SimBaseParameter{T}"/> class
        /// </summary>
        /// <param name="localId">The local Id (may only be different from 0 during loading)</param>
        /// <param name="name">The name of the parameter</param>
        /// <param name="category">The category of the parameter</param>
        /// <param name="propagation">The way in which the parameter may be accessed</param>
        /// <param name="value">The current value of the parameter</param>
        /// <param name="description">The textual value of the parameter</param>
        /// <param name="valueFieldPointer">A pointer to a valid field. When set, the value is ignored. Pass null when no pointer exists</param>
        /// <param name="allowedOperations">The operations the user is expected to perform on this parameter</param>
        /// <param name="instancePropagationMode">The instance value propagation mode for this parameter</param>
        /// <param name="isAutomaticallyGenerated">When set to True, the parameter is marked as being automatically generated</param>
        protected SimBaseParameter(long localId, string name, SimCategory category, SimInfoFlow propagation,
            T value, string description, SimParameterValueSource valueFieldPointer,
            SimParameterOperations allowedOperations = SimParameterOperations.All,
            SimParameterInstancePropagation instancePropagationMode = SimParameterInstancePropagation.PropagateIfInstance,
            bool isAutomaticallyGenerated = false)
            : base(localId, name, category, propagation, description, allowedOperations, instancePropagationMode, isAutomaticallyGenerated)
        {
            this.Value = value;
            this.ValueSource = valueFieldPointer; //ValueSource have to be set after the Value
        }
    }


    /// <summary>
    /// Base class for SimParameters
    /// </summary>
    public abstract class SimBaseParameter : SimObjectNew<ISimManagedCollection>
    {
        #region Abstract

        /// <summary>
        /// Stores the component to which the parameter is attached
        /// </summary>
        public abstract SimComponent Component { get; internal set; }

        /// <summary>
        /// Creates a copy of the current parameter
        /// </summary>
        /// <returns>A copy of the parameter</returns>
        public abstract SimBaseParameter Clone();

        /// <summary>
        /// Returns the value of the parameter. Needs to be implemented by deriving classes
        /// </summary>
        /// <returns>The value of the parameter</returns>
        protected abstract object GetValue();

        /// <summary>
        /// Sets the value of the parameter to neutral. Value depends on the implementation. 
        /// </summary>
        public abstract void SetToNeutral();

        #endregion

        #region Properties


        /// <summary>
        /// The value of the parameter. Type depends on the implementation. 
        /// </summary>
        public object Value { get { return GetValue(); } }

        /// <summary>
        /// Stores the ValuePointer for this parameter. When null, the parameter is not bound to a ValueField
        /// </summary>
        public SimParameterValueSource ValueSource
        {
            get
            {
                return multiValuePointer;
            }
            set
            {
                this.NotifyWriteAccess();

                if (this.multiValuePointer != null)
                {
                    if (this.multiValuePointer is SimGeometryParameterSource gps)
                        this.Factory?.ProjectData.ComponentGeometryExchange.OnParameterSourceRemoved(gps);
                    this.multiValuePointer.TargetParameter = null;
                }

                this.multiValuePointer = value;
                NotifyPropertyChanged(nameof(ValueSource));
                this.NotifyChanged();

                if (this.multiValuePointer != null)
                {
                    this.multiValuePointer.TargetParameter = this;
                    if (this.multiValuePointer is SimGeometryParameterSource gps)
                        this.Factory?.ProjectData.ComponentGeometryExchange.OnParameterSourceAdded(gps);
                }
            }
        }
        private SimParameterValueSource multiValuePointer = null;

        /// <summary>
        /// Defines how the parameter may be used. See InfoFlow for more details
        /// </summary>
        public SimInfoFlow Propagation
        {
            get { return this.propagation; }
            set
            {
                if (this.propagation != value)
                {
                    this.NotifyWriteAccess();

                    this.propagation = value;
                    this.NotifyPropertyChanged(nameof(Propagation));

                    UpdateState();
                    this.NotifyChanged();
                }
            }
        }
        private SimInfoFlow propagation = SimInfoFlow.Mixed;

        /// <summary>
        /// Stores which operations are allowed for this parameter. Used to mark readonly or ValuePointer parameters
        /// </summary>
        public SimParameterOperations AllowedOperations
        {
            get { return allowedOperations; }
            set
            {
                this.NotifyWriteAccess();

                allowedOperations = value;

                NotifyPropertyChanged(nameof(AllowedOperations));
                this.NotifyChanged();
            }
        }
        private SimParameterOperations allowedOperations;

        /// <summary>
        /// Specifies when a parameter value should be propagated to instances
        /// </summary>
        public SimParameterInstancePropagation InstancePropagationMode
        {
            get { return instancePropagationMode; }
            set
            {
                this.NotifyWriteAccess();

                if (instancePropagationMode != value)
                {
                    instancePropagationMode = value;

                    NotifyPropertyChanged(nameof(InstancePropagationMode));

                    this.NotifyChanged();
                }
            }
        }
        private SimParameterInstancePropagation instancePropagationMode = SimParameterInstancePropagation.PropagateIfInstance;

        /// <summary>
        /// The current state of the paramter
        /// </summary>
        public SimParameterState State
        {
            get { return state; }
            private set
            {
                if (state != value)
                {
                    state = value;
                    NotifyPropertyChanged(nameof(State));
                }
            }
        }
        private SimParameterState state;

        /// <summary>
        /// Returns True when the parameter has been generated automatically. Used to supress certain state warnings
        /// </summary>
        public bool IsAutomaticallyGenerated
        {
            get { return isAutomaticallyGenerated; }
            set
            {
                if (isAutomaticallyGenerated != value)
                {
                    this.NotifyWriteAccess();

                    isAutomaticallyGenerated = value;

                    NotifyPropertyChanged(nameof(IsAutomaticallyGenerated));

                    UpdateState();
                    this.NotifyChanged();
                }
            }
        }
        private bool isAutomaticallyGenerated = false;

        /// <summary>
        /// The <see cref="NameTaxonomyEntry"/> of the parameter
        /// </summary>
        public SimTaxonomyEntryOrString NameTaxonomyEntry
        {
            get
            {
                return taxonomyEntry;
            }
            set
            {
                NotifyWriteAccess();

                if (taxonomyEntry.HasTaxonomyEntryReference())
                {
                    NameTaxonomyEntry.TaxonomyEntryReference.RemoveDeleteAction();
                }

                taxonomyEntry = value;

                if (taxonomyEntry.HasTaxonomyEntry())
                {
                    taxonomyEntry.TaxonomyEntryReference.SetDeleteAction(TaxonomyEntryDeleted);
                }
                UpdateState();
                NotifyPropertyChanged(nameof(NameTaxonomyEntry));
                NotifyChanged();
            }
        }
        private SimTaxonomyEntryOrString taxonomyEntry;

        /// <summary>
        /// The category of the parameter
        /// </summary>
        public SimCategory Category
        {
            get { return this.category; }
            set
            {
                if (this.category != value)
                {
                    this.NotifyWriteAccess();

                    this.category = value;

                    this.NotifyPropertyChanged(nameof(Category));
                    this.NotifyChanged();
                }
            }
        }
        private SimCategory category = SimCategory.None;

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="SimBaseParameter"/> class
        /// </summary>
        /// <param name="nameTaxonomyEntry">The name taxonomy entry of the parameter</param>
        /// <param name="allowedOperations">The operations the user is expected to perform on this parameter</param>
        public SimBaseParameter(SimTaxonomyEntry nameTaxonomyEntry, SimParameterOperations allowedOperations = SimParameterOperations.All)
        {
            NameTaxonomyEntry = new SimTaxonomyEntryOrString(nameTaxonomyEntry);
            this.AllowedOperations = allowedOperations;
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="SimBaseParameter"/> class
        /// </summary>
        /// <param name="name">The name of the parameter</param>
        /// <param name="allowedOperations">The operations the user is expected to perform on this parameter</param>
        public SimBaseParameter(string name, SimParameterOperations allowedOperations = SimParameterOperations.All)
        {
            NameTaxonomyEntry = new SimTaxonomyEntryOrString(name);
            this.AllowedOperations = allowedOperations;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SimBaseParameter"/> class by copying all settings from another parameter
        /// </summary>
        /// <param name="original">The parameter to copy from</param>
        protected SimBaseParameter(SimBaseParameter original)
        {
            this.NameTaxonomyEntry = new SimTaxonomyEntryOrString(original.NameTaxonomyEntry);
            this.Category = original.Category;
            this.Propagation = original.Propagation;
            this.InstancePropagationMode = original.InstancePropagationMode;

            this.Description = original.Description;

            this.ValueSource = original.ValueSource?.Clone();

            this.AllowedOperations = original.AllowedOperations;
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="SimBaseParameter"/> class
        /// </summary>
        /// <param name="localId">The local Id (may only be different from 0 during loading)</param>
        /// <param name="name">The name of the parameter</param>
        /// <param name="category">The category of the parameter</param>
        /// <param name="propagation">The way in which the parameter may be accessed</param>
        /// <param name="description">The textual value of the parameter</param>
        /// <param name="allowedOperations">The operations the user is expected to perform on this parameter</param>
        /// <param name="instancePropagationMode">The instance value propagation mode for this parameter</param>
        /// <param name="isAutomaticallyGenerated">When set to True, the parameter is marked as being automatically generated</param>
        protected SimBaseParameter(long localId, string name, SimCategory category, SimInfoFlow propagation,
            string description,
            SimParameterOperations allowedOperations = SimParameterOperations.All,
            SimParameterInstancePropagation instancePropagationMode = SimParameterInstancePropagation.PropagateIfInstance,
            bool isAutomaticallyGenerated = false)
            : base(new SimId(localId))
        {
            NameTaxonomyEntry = new SimTaxonomyEntryOrString(name);
            this.Category = category;
            this.Propagation = propagation;

            this.InstancePropagationMode = instancePropagationMode;

            this.Description = description;

            this.AllowedOperations = allowedOperations;
            this.IsAutomaticallyGenerated = isAutomaticallyGenerated;
        }


        #region Property management
        /// <summary>
        /// Called when the referenced TaxonomyEntry got deleted.
        /// Handed to the TaxonomyEntryReference to be used as a callback.
        /// </summary>
        private void TaxonomyEntryDeleted(SimTaxonomyEntry caller)
        {
            this.NameTaxonomyEntry = new SimTaxonomyEntryOrString(this.NameTaxonomyEntry.Name);
        }

        /// <summary>
        /// Restores references to other connected parts, f.e. TaxonomyEntries
        /// </summary>
        /// <param name="idGenerator">The idGenerator used to look up the references</param>
        public void RestoreReferences(SimIdGenerator idGenerator)
        {
            if (NameTaxonomyEntry.HasTaxonomyEntryReference())
            {
                var entry = idGenerator.GetById<SimTaxonomyEntry>(NameTaxonomyEntry.TaxonomyEntryReference.TaxonomyEntryId);
                NameTaxonomyEntry = new SimTaxonomyEntryOrString(new SimTaxonomyEntryReference(entry));
            }
        }


        /// <summary>
        /// Looks up taxonomy entries for reserved parameters by their name.
        /// Do this if the default taxonomies changed, could mean that the project is migrated.
        /// </summary>
        /// <param name="taxonomyFileVersion">The file version of the loaded managed taxonomy file</param>
        /// <exception cref="Exception">If the default taxonomy entry could not be found.</exception>
        public void RestoreDefaultTaxonomyReferences(ulong taxonomyFileVersion)
        {
            if (!NameTaxonomyEntry.HasTaxonomyEntryReference() && taxonomyFileVersion <= 17)
            {
                if (Component.InstanceType != SimInstanceType.None || Component.Name == "Cumulative")
                {
                    if (ReservedParameterKeys.NameToKeyLookup.TryGetValue(NameTaxonomyEntry.Name, out var key))
                    {

                        var taxonomy = Factory.ProjectData.Taxonomies.GetTaxonomyByKeyOrName(ReservedParameterKeys.RP_TAXONOMY_KEY);
                        var entry = taxonomy.GetTaxonomyEntryByKey(key);
                        if (entry != null)
                        {
                            NameTaxonomyEntry = new SimTaxonomyEntryOrString(new SimTaxonomyEntryReference(entry));
                        }
                        else
                        {
                            throw new Exception("Could not find reserved taxonomy entry for parameter " + NameTaxonomyEntry.Name);
                        }
                    }
                }
            }
        }


        /// <summary>
        /// Returns if the parameter has a default reserved taxonomy entry of the provided key from <see cref="ReservedParameterKeys"/>.
        /// </summary>
        /// <param name="entryKey">The key of the taxonomy entry to check. Should be from <see cref="ReservedParameterKeys"/>.</param>
        /// <returns>if the parameter has a default reserved taxonomy entry of the provided key.</returns>
        public bool HasReservedTaxonomyEntry(String entryKey)
        {
            return taxonomyEntry.HasTaxonomyEntry() &&
                taxonomyEntry.TaxonomyEntryReference.Target.Taxonomy.Key == ReservedParameterKeys.RP_TAXONOMY_KEY &&
                taxonomyEntry.TaxonomyEntryReference.Target.Key == entryKey;
        }

        #endregion

        #region Access Management

        /// <inheritdoc />
        protected override void NotifyWriteAccess()
        {
            if (this.Component != null)
                this.Component.RecordWriteAccess();
            base.NotifyWriteAccess();
        }

        /// <summary>
        /// Checks if the user has permission to access the parameter
        /// </summary>
        /// <param name="user">The user</param>
        /// <param name="permission">The permission</param>
        /// <returns>True when either the parameter is not attached to a component or when the user has access to that component</returns>
        public bool HasAccess(SimUser user, SimComponentAccessPrivilege permission)
        {
            if (this.Component == null)
                return true;
            return this.Component.HasAccess(user, permission);
        }
        #endregion

        #region EVENTS
        /// <summary>
        /// Handler for the IsBeingDelted event.
        /// </summary>
        /// <param name="sender"></param>
        public delegate void IsBeingDeletedEventHandler(object sender);
        /// <summary>
        /// Emitted just before the parameter is being deleted.
        /// </summary>
        public event IsBeingDeletedEventHandler IsBeingDeleted;
        /// <summary>
        /// Emits the IsBeingDelted event.
        /// </summary>
        public void OnIsBeingDeleted()
        {
            this.IsBeingDeleted?.Invoke(this);
        }

        /// <inheritdoc />
        protected override void NotifyPropertyChanged(string property)
        {
            base.NotifyPropertyChanged(property);

            if (this.Component != null)
            {
                this.Component.Factory?.NotifyParameterPropertyChanged(this, property);
                this.Component.Parameters.OnParameterPropertyChanged(this, property);
            }
        }
        #endregion

        /// <summary>
        /// Updates the state property
        /// </summary>
        internal void UpdateState()
        {
            State = GetState();
        }
        /// <summary>
        /// Calculates the new state for this parameter. Used by the <see cref="UpdateState"/> method
        /// </summary>
        protected virtual SimParameterState GetState()
        {
            SimParameterState newState = SimParameterState.Valid;

            if (this.Component != null)
            {
                if (this.Propagation != SimInfoFlow.FromReference && this.Propagation != SimInfoFlow.Automatic &&
                    !this.IsAutomaticallyGenerated)
                {
                    foreach (var referenced in this.Component.ReferencedComponents.Where(x => x.Target != null))
                    {
                        if (referenced.Target.Parameters.Any(x => x.NameTaxonomyEntry.Equals(NameTaxonomyEntry)))
                        {
                            newState |= SimParameterState.HidesReference;
                            break;
                        }
                    }
                }
                else if (this.Propagation == SimInfoFlow.FromReference)
                {
                    var refTarget = GetReferencedParameter();
                    if (refTarget == null)
                    {
                        newState |= SimParameterState.ReferenceNotFound;
                    }
                }
            }

            return newState;
        }

        /// <summary>
        /// Tries to set the value by converting the new value to the desired type.
        /// Sets the parameter value to a neutral value when conversion fails.
        /// </summary>
        /// <param name="value">The new value</param>
        /// <returns>True when the Value can be converted, otherwise False.</returns>
        public abstract void ConvertValueFrom(object value);

        /// <summary>
        /// Returns the referenced parameter in case Propagation is set to REF_IN.
        /// </summary>
        /// <returns>The referenced parameter, or NULL when no such parameter exists. Returns itself for other propagations than REF_IN</returns>
        public SimBaseParameter GetReferencedParameter()
        {
            if (this.Component == null)
                throw new InvalidOperationException("Operation may only be called when the parameter is part of a component");

            if (this.Propagation != SimInfoFlow.FromReference)
                return this;

            //Search up the tree for references. Return the first parameter which matches the name
            var component = this.Component;
            while (component != null)
            {
                foreach (var refComp in component.ReferencedComponents)
                {
                    if (refComp.Target != null)
                    {
                        var matchingParam = refComp.Target.Parameters
                            .FirstOrDefault(x => x.NameTaxonomyEntry.Equals(NameTaxonomyEntry) && x.GetType() == this.GetType()); //Searches for same name && type
                        if (matchingParam != null)
                            return matchingParam;
                    }
                }

                component = component.Parent;
            }

            return null;
        }
    }
}
