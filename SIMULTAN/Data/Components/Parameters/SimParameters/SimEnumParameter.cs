using SIMULTAN.Data.Taxonomy;
using System;


namespace SIMULTAN.Data.Components
{
    /// <summary>
    /// Parameter representing an enumeration. 
    /// The enumeration value is coming from the DefiningTaxonomy <see cref="SimTaxonomy"/>
    /// </summary>
    public class SimEnumParameter : SimBaseParameter<SimTaxonomyEntryReference>
    {
        /// <summary>
        /// Value pf the parameter
        /// </summary>
        public override SimTaxonomyEntryReference Value
        {
            get
            {
                return base.Value;
            }
            set
            {
                if (value != null && value.Target.Parent == null)
                {
                    throw new ArgumentNullException(nameof(value.Target.Parent));
                }
                if (value != null && value.Target.Parent != this.ParentTaxonomyEntryRef.Target)
                {
                    throw new InvalidOperationException("Can only have the ParentTaxonomyEntry´s children as values");
                }

                if (Value != null)
                {
                    this.Value.RemoveDeleteAction();
                }
                base.Value = value;
                if (base.Value != null)
                {
                    this.Value.SetDeleteAction(ValueTaxonomyEntryDeleted);
                }
            }
        }


        private void ValueTaxonomyEntryDeleted(SimTaxonomyEntry caller)
        {
            if (this.Factory != null)
            {
                this.Value.RemoveDeleteAction();
                this.Value = null;
                foreach (var instance in this.Component.Instances)
                {
                    instance.InstanceParameterValuesPersistent.SetWithoutNotify(this, null);
                    instance.InstanceParameterValuesTemporary.SetWithoutNotify(this, null);
                }
            }
        }



        /// <summary>
        /// Possible values for the parameter
        /// </summary>
        public SimChildTaxonomyEntryCollection Items
        {
            get
            {
                return this.ParentTaxonomyEntryRef.Target.Children;
            }
        }

        /// <summary>
        /// The taxonomy which's taxonomy entries can be the values of the enumeration
        /// </summary>
        public SimTaxonomyEntryReference ParentTaxonomyEntryRef
        {
            get
            {
                return parentTaxonomyEntryRef;
            }
            set
            {

                if (parentTaxonomyEntryRef != null)
                {
                    parentTaxonomyEntryRef.RemoveDeleteAction();
                }
                this.parentTaxonomyEntryRef = value;
                parentTaxonomyEntryRef.SetDeleteAction(ParentTaxonomyEntryRefTaxonomyEntryDeleted);


                NotifyPropertyChanged(nameof(ParentTaxonomyEntryRef));
                NotifyPropertyChanged(nameof(Items));
            }
        }
        private SimTaxonomyEntryReference parentTaxonomyEntryRef;
        private void ParentTaxonomyEntryRefTaxonomyEntryDeleted(SimTaxonomyEntry caller)
        {
            if (this.Factory != null)
            {
                this.ParentTaxonomyEntryRef.RemoveDeleteAction();
                var defaultParent = this.Factory.ProjectData.Taxonomies.GetReservedParameter(ReservedParameterKeys.SIMENUMPARAM_DEFAULT);
                this.ParentTaxonomyEntryRef = new SimTaxonomyEntryReference(defaultParent);
                this.Value = null;
                foreach (var instance in this.Component.Instances)
                {
                    instance.InstanceParameterValuesPersistent.SetWithoutNotify(this, null);
                    instance.InstanceParameterValuesTemporary.SetWithoutNotify(this, null);
                }
            }
        }



        #region .CTOR
        /// <summary>
        /// Initializes a new instance of the SimEnumParameter class
        /// </summary>
        /// <param name="nameTaxonomyEntry">The name taxonomy entry of the parameter</param>
        /// <param name="parentTaxonomyEntry">The <see cref="SimTaxonomyEntry" /> which´s entries are the enum´s possible values /></param>
        /// <param name="value">The current value of the parameter</param>
        /// <param name="allowedOperations">The operations the user is expected to perform on this parameter</param>
        public SimEnumParameter(SimTaxonomyEntry nameTaxonomyEntry, SimTaxonomyEntry parentTaxonomyEntry,
            SimTaxonomyEntry value = null,
            SimParameterOperations allowedOperations = SimParameterOperations.All)
            : base(nameTaxonomyEntry, null, allowedOperations)
        {
            if (parentTaxonomyEntry == null)
                throw new ArgumentNullException(nameof(parentTaxonomyEntry));
            if (value != null && value.Parent != parentTaxonomyEntry)
                throw new ArgumentException("Value is not a sub entry of parentTaxonomyEntry");

            this.ParentTaxonomyEntryRef = new SimTaxonomyEntryReference(parentTaxonomyEntry);
            this.Value = value != null ? new SimTaxonomyEntryReference(value) : null;
            UpdateState();
        }



        /// <summary>
        /// Initializes a new instance of the SimParameter class
        /// </summary>
        /// <param name="name">The name of the parameter</param>
        /// <param name="parentTaxonomyEntry">The <see cref="SimTaxonomyEntry" /> which´s entries are the enum´s possible values /></param>
        /// <param name="value">The value of the parameter</param>
        /// <param name="allowedOperations">The operations the user is expected to perform on this parameter</param>
        public SimEnumParameter(string name, SimTaxonomyEntry parentTaxonomyEntry, SimTaxonomyEntry value = null,
            SimParameterOperations allowedOperations = SimParameterOperations.All)
            : base(name, null, allowedOperations)
        {
            if (parentTaxonomyEntry == null)
                throw new ArgumentNullException(nameof(parentTaxonomyEntry));
            if (value != null && value.Parent != parentTaxonomyEntry)
                throw new ArgumentException("Value is not a sub entry of parentTaxonomyEntry");

            this.ParentTaxonomyEntryRef = new SimTaxonomyEntryReference(parentTaxonomyEntry);
            this.Value = value != null ? new SimTaxonomyEntryReference(value) : null;
            UpdateState();
        }



        /// <summary>
        /// Initializes a new instance of the SimParameter class
        /// </summary>
        /// <param name="localId">The local Id (may only be different from 0 during loading)</param>
        /// <param name="name">The name of the parameter</param>
        /// <param name="category">The category of the parameter</param>
        /// <param name="propagation">The way in which the parameter may be accessed</param>
        /// <param name="parentTaxonomyEntry">TaxoomyEntry defining the possible values of the Enum</param>
        /// <param name="value">The current value of the parameter</param>
        /// <param name="description">The textual value of the parameter</param>
        /// <param name="valueFieldPointer">A pointer to a valid field. When set, the value is ignored. Pass null when no pointer exists</param>
        /// <param name="allowedOperations">The operations the user is expected to perform on this parameter</param>
        /// <param name="instancePropagationMode">The instance value propagation mode for this parameter</param>
        /// <param name="isAutomaticallyGenerated">When set to True, the parameter is marked as being automatically generated</param>
        internal SimEnumParameter(long localId, string name, SimCategory category, SimInfoFlow propagation, SimTaxonomyEntry parentTaxonomyEntry,
                           SimTaxonomyEntry value,
                           string description, SimParameterValueSource valueFieldPointer,
                           SimParameterOperations allowedOperations = SimParameterOperations.All,
                           SimParameterInstancePropagation instancePropagationMode = SimParameterInstancePropagation.PropagateIfInstance,
                           bool isAutomaticallyGenerated = false)
            : base(localId, name, category, propagation, null,
                  description, valueFieldPointer, allowedOperations, instancePropagationMode, isAutomaticallyGenerated)

        {
            if (parentTaxonomyEntry == null)
                throw new ArgumentNullException(nameof(parentTaxonomyEntry));
            if (value != null && value.Parent != parentTaxonomyEntry)
                throw new ArgumentException("Value is not a sub entry of parentTaxonomyEntry");

            this.ParentTaxonomyEntryRef = new SimTaxonomyEntryReference(parentTaxonomyEntry);
            this.Value = value != null ? new SimTaxonomyEntryReference(value) : null;

            UpdateState();
        }

        /// <summary>
        /// Initializes a new instance of the Parameter class by copying all settings from another parameter
        /// </summary>
        /// <param name="original">The parameter to copy from</param>
        protected SimEnumParameter(SimEnumParameter original) : base(original, false)
        {
            this.ParentTaxonomyEntryRef = new SimTaxonomyEntryReference(original.ParentTaxonomyEntryRef);
            this.Value = original.Value != null ? new SimTaxonomyEntryReference(original.Value) : null;
            UpdateState();
        }


        #endregion


        /// <summary>
        /// Creates a copy of the current parameter
        /// </summary>
        /// <returns>A copy of the parameter</returns>
        public override SimBaseParameter Clone()
        {
            return new SimEnumParameter(this);
        }

        /// <inheritdoc />
        public override void ConvertValueFrom(object value) { }

        /// <inheritdoc />
        internal override bool IsSameValue(SimTaxonomyEntryReference value1, SimTaxonomyEntryReference value2)
        {
            if (value1 != null && value2 != null)
                return value1.Target == value2.Target;
            else if (value1 == null && value2 == null)
                return true;
            return false;
        }

        /// <inheritdoc />
        public override void SetToNeutral()
        {
            this.Value = null;
        }
    }
}
