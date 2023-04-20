using SIMULTAN.Data.Taxonomy;
using System;

namespace SIMULTAN.Data.Components
{
    /// <summary>
    /// SimParameter with Value <see cref="double"/>
    /// </summary>
    public class SimDoubleParameter : SimBaseNumericParameter<double>
    {
        /// <inheritdoc/>
        internal override bool IsSameValue(double value1, double value2)
        {
            return (double.IsNaN(value1) && double.IsNaN(value2)) || Math.Abs(value1 - value2) < 0.00000001;
        }

        #region .CTOR

        /// <summary>
        /// Initializes a new instance of the SimParameter class
        /// </summary>
        /// <param name="name">The name of the parameter</param>
        /// <param name="unit">Unit of the parameter</param>
        /// <param name="value">The current value of the parameter</param>
        /// <param name="allowedOperations">The operations the user is expected to perform on this parameter</param>
        public SimDoubleParameter(string name, string unit, double value, SimParameterOperations allowedOperations = SimParameterOperations.All)
            : this(name, unit, value, double.NegativeInfinity, double.PositiveInfinity, allowedOperations)
        { }

        /// <summary>
        /// Initializes a new instance of the SimParameter class
        /// </summary>
        /// <param name="nameTaxonomyEntry">The name taxonomy entry of the parameter</param>
        /// <param name="unit">Unit of the parameter</param>
        /// <param name="value">The current value of the parameter</param>
        /// <param name="allowedOperations">The operations the user is expected to perform on this parameter</param>
        public SimDoubleParameter(SimTaxonomyEntry nameTaxonomyEntry, string unit, double value, SimParameterOperations allowedOperations = SimParameterOperations.All)
            : this(nameTaxonomyEntry, unit, value, double.NegativeInfinity, double.PositiveInfinity, allowedOperations)
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
        public SimDoubleParameter(string name, string unit, double value, double minValue, double maxValue,
            SimParameterOperations allowedOperations = SimParameterOperations.All)
            : base(name, unit, value, Math.Min(minValue, maxValue), Math.Max(minValue, maxValue), allowedOperations)
        {
            UpdateState();
        }

        /// <summary>
        /// Initializes a new instance of the SimParameter class
        /// </summary>
        /// <param name="nameTaxonomyEntry">The name taxonomy entry of the parameter</param>
        /// <param name="unit">Unit of the parameter</param>
        /// <param name="value">The current value of the parameter</param>
        /// <param name="minValue">The minimum valid value</param>
        /// <param name="maxValue">The maximum valid value</param>
        /// <param name="allowedOperations">The operations the user is expected to perform on this parameter</param>
        public SimDoubleParameter(SimTaxonomyEntry nameTaxonomyEntry, string unit, double value, double minValue, double maxValue,
            SimParameterOperations allowedOperations = SimParameterOperations.All)
            : base(nameTaxonomyEntry, unit, value, Math.Min(minValue, maxValue), Math.Max(minValue, maxValue), allowedOperations)
        {
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
        /// <param name="description">The textual value of the parameter</param>
        /// <param name="valueFieldPointer">A pointer to a valid field. When set, the value is ignored. Pass null when no pointer exists</param>
        /// <param name="allowedOperations">The operations the user is expected to perform on this parameter</param>
        /// <param name="instancePropagationMode">The instance value propagation mode for this parameter</param>
        /// <param name="isAutomaticallyGenerated">When set to True, the parameter is marked as being automatically generated</param>
        internal SimDoubleParameter(long localId, string name, string unit, SimCategory category, SimInfoFlow propagation,
                           double value, double minValue, double maxValue,
                           string description, SimParameterValueSource valueFieldPointer,
                           SimParameterOperations allowedOperations = SimParameterOperations.All,
                           SimParameterInstancePropagation instancePropagationMode = SimParameterInstancePropagation.PropagateIfInstance,
                           bool isAutomaticallyGenerated = false)
            : base(localId, name, unit, category, propagation, value, minValue, maxValue, description, valueFieldPointer,
                  allowedOperations, instancePropagationMode, isAutomaticallyGenerated)
        {
            UpdateState();
        }

        /// <summary>
        /// Initializes a new instance of the Parameter class by copying all settings from another paramter
        /// </summary>
        /// <param name="original">The parameter to copy from</param>
        protected SimDoubleParameter(SimDoubleParameter original) : base(original)
        {
            UpdateState();
        }


        #endregion

        /// <summary>
        /// Updates the state property
        /// </summary>
        protected override SimParameterState GetState()
        {
            SimParameterState newState = base.GetState();

            if (double.IsNaN(Value))
            {
                if (this.AllowedOperations.HasFlag(SimParameterOperations.EditValue))
                    newState |= SimParameterState.ValueNaN;
            }
            else if (SanitizedDouble(Value) < SanitizedDouble(ValueMin) || SanitizedDouble(Value) > SanitizedDouble(ValueMax))
            {
                if (this.AllowedOperations.HasFlag(SimParameterOperations.EditValue))
                    newState |= SimParameterState.ValueOutOfRange;
            }

            return newState;
        }

        /// <summary>
        /// Creates a copy of the current parameter
        /// </summary>
        /// <returns>A copy of the parameter</returns>
        public override SimBaseParameter Clone()
        {
            return new SimDoubleParameter(this);
        }


        private double SanitizedDouble(double value)
        {
            if (value == double.MaxValue)
                return double.PositiveInfinity;
            if (value == double.MinValue)
                return double.NegativeInfinity;
            return value;
        }

        /// <inheritdoc />
        public override void ConvertValueFrom(object value)
        {
            this.Value = ConvertFromValue(value);
        }
        /// <summary>
        /// Converts the data value of any other parameters to the value of a <see cref="SimDoubleParameter"/>
        /// </summary>
        /// <param name="value">The value of the other parameter</param>
        /// <returns>The converted double. <see cref="double.NaN"/> for everything that can't be converted</returns>
        public static double ConvertFromValue(object value)
        {
            switch (value)
            {
                case double d:
                    return d;
                case int i:
                    return i;
                case bool b:
                    return b ? 1 : 0;
                case string s:
                    if (double.TryParse(s, out var dv))
                    {
                        return dv;
                    }
                    else
                    {
                        return double.NaN;
                    }
                default:
                    return double.NaN;
            }
        }

        /// <inheritdoc />
        public override void SetToNeutral()
        {
            this.Value = double.NaN;
        }
    }
}
