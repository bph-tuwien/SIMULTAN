using SIMULTAN.Data.MultiValues;
using SIMULTAN.Data.Users;
using SIMULTAN.Excel;
using SIMULTAN.Serializer.DXF;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace SIMULTAN.Data.Components
{
    //Uses SimObjectNew<ISimManagedCollection> because it can either be part of a ComponentFactory or ParameterFactory

    /// <summary>
    /// Stores a numeric and a textual value inside a component.
    /// Also provides logic to reference other parameters or ValueFields
    /// 
    /// Parameters store a numeric value and a textual value at the same time. Depending on how the parameter is used,
    /// either <see cref="ValueCurrent"/> (the numerical value) or <see cref="TextValue"/> (the textual value) is used.
    /// 
    /// Parameters can be configured to take the value from a ValueField. When the <see cref="MultiValuePointer"/> property is set to Null,
    /// the user is assumed to set the value directly. When <see cref="MultiValuePointer"/> is not Null, the value is taken from the MultiValue.
    /// Changes to attached MultiValues are automatically propagated to the <see cref="ValueCurrent"/> property.
    /// 
    /// A parameter can be locked for certain operations. See <see cref="AllowedOperations"/> for details.
    /// 
    /// Each parameter has a override value stored in each <see cref="SimComponentInstance"/>. Depending on the <see cref="instancePropagationMode"/>,
    /// the <see cref="ValueCurrent"/> is propagated to the instances.
    /// </summary>
    public class SimParameter : SimObjectNew<ISimManagedCollection>
    {
        #region Properties

        /// <summary>
        /// The unit of the parameter
        /// </summary>
		[ExcelMappingProperty("SIM_PARAMETER_UNIT")]
        public string Unit
        {
            get { return this.unit; }
            set
            {
                if (this.unit != value)
                {
                    NotifyWriteAccess();

                    this.unit = value;
                    this.NotifyPropertyChanged(nameof(Unit));

                    this.NotifyChanged();
                }
            }
        }
        private string unit;

        /// <summary>
        /// The category of the parameter
        /// </summary>
		[ExcelMappingProperty("SIM_PARAMETER_CATEGORY")]
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
        private SimCategory category;

        /// <summary>
        /// Defines how the parameter may be used. See InfoFlow for more details
        /// </summary>
		[ExcelMappingProperty("SIM_PARAMETER_PROPAGATION")]
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
        private SimInfoFlow propagation;

        /// <summary>
        /// Stores the component to which the parameter is attached
        /// </summary>
        public SimComponent Component
        {
            get { return component; }
            internal set
            {
                component = value;
                UpdateState();

                if (component == null)
                    DetachPointerEvents();
                else
                    AttachPointerEvents();

                this.NotifyPropertyChanged(nameof(Component));
            }
        }
        private SimComponent component;

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
        /// The textual value of the parameter
        /// </summary>
        [ExcelMappingProperty("SIM_PARAMETER_TEXTVALUE")]
        public string TextValue
        {
            get { return this.textValue; }
            set
            {
                if (this.textValue != value)
                {
                    this.NotifyWriteAccess();

                    this.textValue = value;

                    this.NotifyPropertyChanged(nameof(TextValue));
                    this.NotifyChanged();
                }
            }
        }
        private string textValue;

        /// <summary>
        /// The numerical value of the parameter (default: 0.0)
        /// </summary>
        [ExcelMappingProperty("SIM_PARAMETER_VALUECURRENT")]
        public double ValueCurrent
        {
            get { return this.value; }
            set
            {
                if (!HasSameCurrentValue(value))
                {
                    this.NotifyWriteAccess();

                    this.value = value;
                    this.NotifyPropertyChanged(nameof(ValueCurrent));
                    UpdateState();
                    this.NotifyChanged();
                }
            }
        }
        private double value;

        /// <summary>
        /// The maximum value of the parameter. Does not prevent setting a higher ValueCurrent, but sets the State to OutOfRange
        /// </summary>
        [ExcelMappingProperty("SIM_PARAMETER_VALUEMAX")]
        public double ValueMax
        {
            get { return this.valueMax; }
            set
            {
                if (this.valueMax != value)
                {
                    this.NotifyWriteAccess();

                    this.valueMax = value;
                    this.NotifyPropertyChanged(nameof(ValueMax));

                    UpdateState();
                    this.NotifyChanged();
                }
            }
        }
        private double valueMax;

        /// <summary>
        /// The minimum value of the parameter. Does not prevent setting a lower ValueCurrent, but sets the State to OutOfRange
        /// </summary>
		[ExcelMappingProperty("SIM_PARAMETER_VALUEMIN")]
        public double ValueMin
        {
            get { return this.valueMin; }
            set
            {
                if (this.valueMin != value)
                {
                    this.NotifyWriteAccess();

                    this.valueMin = value;
                    this.NotifyPropertyChanged(nameof(ValueMin));

                    UpdateState();
                    this.NotifyChanged();
                }
            }
        }
        private double valueMin;

        /// <summary>
        /// Stores the ValuePointer for this parameter. When null, the parameter is not bound to a ValueField
        /// </summary>
        public SimMultiValuePointer MultiValuePointer
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
                    this.multiValuePointer.TargetParameter = null;
                    DetachPointerEvents();
                }

                this.multiValuePointer = value;
                NotifyPropertyChanged(nameof(MultiValuePointer));
                this.NotifyChanged();

                if (this.multiValuePointer != null)
                {
                    this.multiValuePointer.TargetParameter = this;
                    AttachPointerEvents();
                }
            }
        }
        private SimMultiValuePointer multiValuePointer;

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

        #endregion


        #region .CTOR

        /// <summary>
        /// Initializes a new instance of the SimParameter class
        /// </summary>
        /// <param name="name">The name of the parameter</param>
        /// <param name="unit">Unit of the parameter</param>
        /// <param name="value">The current value of the parameter</param>
        /// <param name="allowedOperations">The operations the user is expected to perform on this parameter</param>
        public SimParameter(string name, string unit, double value, SimParameterOperations allowedOperations = SimParameterOperations.All)
            : this(name, unit, value, double.NegativeInfinity, double.PositiveInfinity, allowedOperations)
        { }

        /// <summary>
        /// Initializes a new instance of the SimParameter class
        /// </summary>
        /// <param name="name">The name of the parameter</param>
        /// <param name="unit">Unit of the parameter</param>
        /// <param name="value">The current value of the parameter</param>
        /// <param name="minValue">The minimum valid value</param>
        /// <param name="maxValue">The maximum valid value</param>
        /// <param name="allowedOperations">The operations the user is expected to perform on this parameter</param>
        public SimParameter(string name, string unit, double value, double minValue, double maxValue,
            SimParameterOperations allowedOperations = SimParameterOperations.All)
        {
            this.Name = name;
            this.Unit = unit;
            this.Category = SimCategory.None;
            this.Propagation = SimInfoFlow.Mixed;

            this.ValueMin = Math.Min(minValue, maxValue);
            this.ValueMax = Math.Max(minValue, maxValue);
            this.ValueCurrent = value;
            this.TextValue = string.Empty;

            this.AllowedOperations = allowedOperations;

            this.MultiValuePointer = null;

            UpdateState();
        }

        /// <summary>
        /// Initializes a new instance of the SimParameter class
        /// </summary>
        /// <param name="localId">The local Id (may only be different from 0 during loading)</param>
        /// <param name="name">The name of the parameter</param>
        /// <param name="unit">Unit of the parameter</param>
        /// <param name="category">The category of the parameter</param>
        /// <param name="propagation">The way in which the parameter may be accessed</param>
        /// <param name="value">The current value of the parameter</param>
        /// <param name="minValue">The minimum valid value</param>
        /// <param name="maxValue">The maximum valid value</param>
        /// <param name="textValue">The textual value of the parameter</param>
        /// <param name="valueFieldPointer">A pointer to a valid field. When set, the value is ignored. Pass null when no pointer exists</param>
        /// <param name="allowedOperations">The operations the user is expected to perform on this parameter</param>
        /// <param name="instancePropagationMode">The instance value propagation mode for this parameter</param>
        /// <param name="isAutomaticallyGenerated">When set to True, the parameter is marked as being automatically generated</param>
        internal SimParameter(long localId, string name, string unit, SimCategory category, SimInfoFlow propagation,
                           double value, double minValue, double maxValue,
                           string textValue, SimMultiValuePointer valueFieldPointer,
                           SimParameterOperations allowedOperations = SimParameterOperations.All,
                           SimParameterInstancePropagation instancePropagationMode = SimParameterInstancePropagation.PropagateIfInstance,
                           bool isAutomaticallyGenerated = false)
            : base(new SimId(localId))
        {

            this.Name = name;
            this.Unit = unit;
            this.Category = category;
            this.Propagation = propagation;
            this.InstancePropagationMode = instancePropagationMode;

            this.ValueMin = minValue;
            this.ValueMax = maxValue;
            this.ValueCurrent = value;

            this.MultiValuePointer = valueFieldPointer;
            // the value field will be set later (DO NOT FORGET TO OVERRIDE THE VALUE FIELD POINTER !!!)

            this.TextValue = textValue;

            this.AllowedOperations = allowedOperations;
            this.IsAutomaticallyGenerated = isAutomaticallyGenerated;

            UpdateState();
        }

        /// <summary>
        /// Initializes a new instance of the Parameter class by copying all settings from another paramter
        /// </summary>
        /// <param name="original">The parameter to copy from</param>
        protected SimParameter(SimParameter original)
        {
            this.Name = original.Name;
            this.Unit = original.Unit;
            this.Category = original.Category;
            this.Propagation = original.Propagation;

            this.ValueMin = original.ValueMin;
            this.ValueMax = original.ValueMax;
            this.ValueCurrent = original.ValueCurrent;
            this.TextValue = original.TextValue;

            this.MultiValuePointer = original.MultiValuePointer?.Clone();

            this.allowedOperations = original.allowedOperations;
            UpdateState();
        }

        /// <summary>
        /// Creates a copy of the current parameter
        /// </summary>
        /// <returns>A copy of the parameter</returns>
        public SimParameter Clone()
        {
            return new SimParameter(this);
        }

        #endregion


        #region METHODS : To and From String

        /// <inheritdoc />
        [Obsolete]
        public override string ToString()
        {
            throw new NotImplementedException();
            //string output = this.Id.LocalId + ": " + this.Name + " [" + this.Unit + "]: " + NumberUtils.ToDisplayString(this.ValueCurrent, "F2");

            //output += " in [" + NumberUtils.ToDisplayString(this.ValueMin, "F2") + ", " +
            //                       NumberUtils.ToDisplayString(this.ValueMax, "F2") + "] ";
            //output += "[" + ComponentUtils.InfoFlowToString(this.Propagation) + "]\n";

            //return output;
        }

        public virtual void AddToExport(ref StringBuilder _sb)
        {
            if (_sb == null) return;

            _sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString()); // 0
            _sb.AppendLine(ParamStructTypes.PARAMETER);                               // PARAMETER

            _sb.AppendLine(((int)ParamStructCommonSaveCode.CLASS_NAME).ToString());
            _sb.AppendLine(this.GetType().ToString());

            // general
            _sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_ID).ToString());
            _sb.AppendLine(this.Id.LocalId.ToString());

            _sb.AppendLine(((int)ParameterSaveCode.NAME).ToString());
            _sb.AppendLine(this.Name);

            _sb.AppendLine(((int)ParameterSaveCode.UNIT).ToString());
            _sb.AppendLine(this.Unit);

            _sb.AppendLine(((int)ParameterSaveCode.CATEGORY).ToString());
            _sb.AppendLine(ComponentUtils.CategoryToString(this.Category));

            _sb.AppendLine(((int)ParameterSaveCode.PROPAGATION).ToString());
            _sb.AppendLine(ComponentUtils.InfoFlowToString(this.Propagation));

            _sb.AppendLine(((int)ParameterSaveCode.INSTANCE_PROPAGATION).ToString());
            _sb.AppendLine(((int)this.InstancePropagationMode).ToString());

            // value management (changed 26.10.2016)
            _sb.AppendLine(((int)ParameterSaveCode.VALUE_MIN).ToString());
            _sb.AppendLine(DXFDecoder.DoubleToString(this.ValueMin, "F8"));

            _sb.AppendLine(((int)ParameterSaveCode.VALUE_MAX).ToString());
            _sb.AppendLine(DXFDecoder.DoubleToString(this.ValueMax, "F8"));

            _sb.AppendLine(((int)ParameterSaveCode.VALUE_CURRENT).ToString());
            _sb.AppendLine(DXFDecoder.DoubleToString(this.ValueCurrent, "F8"));

            _sb.AppendLine(((int)ParameterSaveCode.ALLOWED_OPERATIONS).ToString());
            _sb.AppendLine(this.AllowedOperations.ToString());

            // text value
            _sb.AppendLine(((int)ParameterSaveCode.VALUE_TEXT).ToString());
            _sb.AppendLine(this.TextValue);

            _sb.AppendLine(((int)ParameterSaveCode.IS_AUTOGENERATED).ToString());
            _sb.AppendLine(this.IsAutomaticallyGenerated ? "1" : "0");

            if (this.MultiValuePointer != null)
                this.MultiValuePointer.AddToExport(ref _sb);
        }

        #endregion

        #region Property management

        /// <summary>
        /// Tests if the value of the parameter is the same as the supplied value.
        /// Handles NaN values correctly
        /// </summary>
        /// <param name="value">The value to test against</param>
        /// <returns>True when either the value of both is equal, or when both values are double.Nan</returns>
        internal bool HasSameCurrentValue(double value)
        {
            return (double.IsNaN(this.ValueCurrent) && double.IsNaN(value)) || Math.Abs(this.ValueCurrent - value) < 0.00000001;
        }

        /// <inheritdoc />
		protected override void OnNameChanged()
        {
            UpdateState();
            base.OnNameChanged();
        }

        /// <summary>
        /// Updates the state property
        /// </summary>
		public void UpdateState()
        {
            SimParameterState newState = SimParameterState.Valid;

            if (double.IsNaN(ValueCurrent))
            {
                if (this.AllowedOperations.HasFlag(SimParameterOperations.EditValue))
                    newState |= SimParameterState.ValueNaN;
            }
            else if (ValueCurrent < ValueMin || ValueCurrent > ValueMax)
            {
                if (this.AllowedOperations.HasFlag(SimParameterOperations.EditValue))
                    newState |= SimParameterState.ValueOutOfRange;
            }

            if (this.Component != null)
            {
                if (this.Propagation != SimInfoFlow.FromReference && this.Propagation != SimInfoFlow.Automatic &&
                    !this.IsAutomaticallyGenerated)
                {
                    foreach (var referenced in this.Component.ReferencedComponents.Where(x => x.Target != null))
                    {
                        if (referenced.Target.Parameters.Any(x => x.Name == this.Name))
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

            State = newState;
        }

        #endregion

        #region INFO

        /// <summary>
        /// Returns a list of all calculations that use this parameter
        /// </summary>
        /// <returns>A list of all calculations that use this parameter</returns>
        public List<SimCalculation> GetReferencingCalculations()
        {
            if (this.Component == null || this.Component.Factory == null)
                throw new InvalidOperationException("This operation is only possible when the containing component is part of a factory");

            List<SimCalculation> result = new List<SimCalculation>();

            foreach (var comp in this.Component.Factory)
                GetReferencingCalculations(comp, result);

            return result;
        }
        private void GetReferencingCalculations(SimComponent component, List<SimCalculation> results)
        {
            if (component == null)
            {
                return;
            }
            foreach (var calc in component.Calculations)
                if (calc.InputParams.ContainsValue(this) || calc.ReturnParams.ContainsValue(this))
                    results.Add(calc);

            foreach (var child in component.Components.Where(x => x.Component != null))
                GetReferencingCalculations(child.Component, results);
        }

        /// <summary>
        /// Returns the referenced parameter in case Propagation is set to REF_IN.
        /// </summary>
        /// <returns>The referenced parameter, or NULL when no such parameter exists. Returns itself for other propagations than REF_IN</returns>
        public SimParameter GetReferencedParameter()
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
                        var matchingParam = refComp.Target.Parameters.FirstOrDefault(x => x.Name == this.Name);
                        if (matchingParam != null)
                            return matchingParam;
                    }
                }

                component = (SimComponent)component.Parent;
            }

            return null;
        }

        #endregion

        #region ValueField management

        private void AttachPointerEvents()
        {
            if (this.multiValuePointer != null)
            {
                if (this.Component != null)
                {
                    this.multiValuePointer.AttachEvents();
                    this.multiValuePointer.ValueChanged += MultiValuePointer_ValueChanged;
                }

                this.ValueCurrent = multiValuePointer.GetValue();
            }
        }
        private void DetachPointerEvents()
        {
            if (this.multiValuePointer != null)
            {
                this.multiValuePointer.DetachEvents();
                this.multiValuePointer.ValueChanged -= MultiValuePointer_ValueChanged;
            }
        }

        private void MultiValuePointer_ValueChanged(object sender, EventArgs e)
        {
            this.ValueCurrent = multiValuePointer.GetValue();
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
    }
}