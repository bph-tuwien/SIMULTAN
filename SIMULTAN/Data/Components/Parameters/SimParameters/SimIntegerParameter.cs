using SIMULTAN.Data.Taxonomy;
using System;

namespace SIMULTAN.Data.Components
{

    /// <summary>
    /// SimParameter with Value <see cref="int"/>
    /// </summary>
    public class SimIntegerParameter : SimBaseNumericParameter<int>
    {
        #region .CTOR

        /// <summary>
        /// Initializes a new instance of the SimIntegerParameter class
        /// </summary>
        /// <param name="name">The name of the parameter</param>
        /// <param name="unit">Unit of the parameter</param>
        /// <param name="value">The current value of the parameter</param>
        /// <param name="allowedOperations">The operations the user is expected to perform on this parameter</param>
        public SimIntegerParameter(string name, string unit, int value, SimParameterOperations allowedOperations = SimParameterOperations.All)
            : this(name, unit, value, int.MinValue, int.MaxValue, allowedOperations)
        { }

        /// <summary>
        /// Initializes a new instance of the SimIntegerParameter class
        /// </summary>
        /// <param name="nameTaxonomyEntry">The name taxonomy entry of the parameter</param>
        /// <param name="unit">Unit of the parameter</param>
        /// <param name="value">The current value of the parameter</param>
        /// <param name="allowedOperations">The operations the user is expected to perform on this parameter</param>
        public SimIntegerParameter(SimTaxonomyEntry nameTaxonomyEntry, string unit, int value, SimParameterOperations allowedOperations = SimParameterOperations.All)
            : this(nameTaxonomyEntry, unit, value, int.MinValue, int.MaxValue, allowedOperations)
        { }

        /// <summary>
        /// Initializes a new instance of the SimIntegerParameter class
        /// </summary>
        /// <param name="name">The name of the parameter</param>
        /// <param name="unit">Unit of the parameter</param>
        /// <param name="value">The current value of the parameter</param>
        /// <param name="minValue">The minimum valid value</param>
        /// <param name="maxValue">The maximum valid value</param>
        /// <param name="allowedOperations">The operations the user is expected to perform on this parameter</param>
        public SimIntegerParameter(string name, string unit, int value, int minValue, int maxValue,
            SimParameterOperations allowedOperations = SimParameterOperations.All)
            : base(name, unit, value, Math.Min(minValue, maxValue), Math.Max(minValue, maxValue), allowedOperations)
        {
            UpdateState();
        }

        /// <summary>
        /// Initializes a new instance of the SimIntegerParameter class
        /// </summary>
        /// <param name="nameTaxonomyEntry">The name taxonomy entry of the parameter</param>
        /// <param name="unit">Unit of the parameter</param>
        /// <param name="value">The current value of the parameter</param>
        /// <param name="minValue">The minimum valid value</param>
        /// <param name="maxValue">The maximum valid value</param>
        /// <param name="allowedOperations">The operations the user is expected to perform on this parameter</param>
        public SimIntegerParameter(SimTaxonomyEntry nameTaxonomyEntry, string unit, int value, int minValue, int maxValue,
            SimParameterOperations allowedOperations = SimParameterOperations.All)
            : base(nameTaxonomyEntry, unit, value, Math.Min(minValue, maxValue), Math.Max(minValue, maxValue), allowedOperations)
        {
            UpdateState();
        }

        /// <summary>
        /// Initializes a new instance of the SimIntegerParameter class
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
        internal SimIntegerParameter(long localId, string name, string unit, SimCategory category, SimInfoFlow propagation,
                           int value, int minValue, int maxValue,
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
        protected SimIntegerParameter(SimIntegerParameter original) : base(original)
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


            if (Value < ValueMin || Value > ValueMax)
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
            return new SimIntegerParameter(this);
        }

        /// <inheritdoc />
        public override void ConvertValueFrom(object value)
        {
            this.Value = ConvertFromValue(value);
        }

        /// <summary>
        /// Converts the data value of any other parameters to the value of a <see cref="SimIntegerParameter"/>
        /// </summary>
        /// <param name="value">The value of the other parameter</param>
        /// <returns>The converted integer. 0 for everything that can't be converted</returns>
        public static int ConvertFromValue(object value)
        {
            switch (value)
            {
                case int i:
                    return i;
                case double d:
                    {
                        if (double.IsPositiveInfinity(d))
                            return int.MaxValue;
                        else if (double.IsNegativeInfinity(d))
                            return int.MinValue;
                        else if (double.IsNaN(d))
                            return -1;
                        return (int)d;
                    }
                case string s:
                    if (int.TryParse(s, out var sv))
                    {
                        return sv;
                    }
                    else
                    {
                        return 0;
                    }
                case bool b:
                    return b ? 1 : 0;
                default:
                    return 0;
            }
        }

        /// <inheritdoc />
        internal override bool IsSameValue(int value1, int value2)
        {
            return value1 == value2;
        }

        /// <inheritdoc />
        public override void SetToNeutral()
        {
            this.Value = int.MinValue;
        }
    }
}
