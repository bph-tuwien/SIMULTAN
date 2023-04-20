using SIMULTAN.Data.Taxonomy;
using System.Collections.Generic;

namespace SIMULTAN.Data.Components
{

    /// <summary>
    /// Base class for the numeric parameters <see cref="SimDoubleParameter"/> <seealso cref="SimIntegerParameter"/>
    /// Compared to the other parameters, they have <see cref="SimBaseNumericParameter{T}.Unit"/>,  <see cref="SimBaseNumericParameter{T}.ValueMax"/> 
    /// and <see cref="SimBaseNumericParameter{T}.ValueMin"/>
    /// </summary>
    /// <typeparam name="T">The type, either a double or an int</typeparam>
    public abstract class SimBaseNumericParameter<T> : SimBaseParameter<T>
    {
        #region Properties

        /// <summary>
        /// The unit of the parameter
        /// </summary>
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
        /// The maximum value of the parameter. Does not prevent setting a higher ValueCurrent, but sets the State to OutOfRange
        /// </summary>
        public T ValueMax
        {
            get { return valueMax; }
            set
            {
                if (!IsSameValue(this.valueMax, value))
                {
                    this.NotifyWriteAccess();

                    this.valueMax = value;
                    this.NotifyPropertyChanged(nameof(ValueMax));

                    UpdateState();
                    this.NotifyChanged();
                }
            }
        }
        private T valueMax;
        /// <summary>
        /// The minimum value of the parameter. Does not prevent setting a lower ValueCurrent, but sets the State to OutOfRange
        /// </summary>
        public T ValueMin

        {
            get { return valueMin; }
            set
            {
                if (!IsSameValue(this.valueMin, value))
                {
                    this.NotifyWriteAccess();

                    this.valueMin = value;
                    this.NotifyPropertyChanged(nameof(ValueMin));

                    UpdateState();
                    this.NotifyChanged();
                }
            }
        }
        private T valueMin;

        #endregion

        #region Calculations

        /// <summary>
        /// Stores all calculations referencing this parameter either as an input or as an output
        /// </summary>
        public IReadOnlyList<SimCalculation> ReferencingCalculations { get { return this.referencingCalculations; } }
        /// <summary>
        /// Stores all calculations referencing this parameter either as an input or as an output.
        /// Same as <see cref="ReferencingCalculations"/>, but allows for writing access.
        /// </summary>
        internal List<SimCalculation> ReferencingCalculations_Internal { get { return this.referencingCalculations; } }
        private List<SimCalculation> referencingCalculations = new List<SimCalculation>();

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="SimBaseNumericParameter{T}"/> class
        /// </summary>
        /// <param name="name">The name taxonomy entry of the parameter</param>
        /// <param name="unit">Unit of the parameter</param>
        /// <param name="value">The current value of the parameter</param>
        /// <param name="minValue">The minimum valid value</param>
        /// <param name="maxValue">The maximum valid value</param>
        /// <param name="allowedOperations">The operations the user is expected to perform on this parameter</param>
        protected SimBaseNumericParameter(SimTaxonomyEntry name, string unit, T value, T minValue, T maxValue,
            SimParameterOperations allowedOperations = SimParameterOperations.All)
            : base(name, value, allowedOperations)
        {
            this.Unit = unit;
            this.ValueMin = minValue;
            this.ValueMax = maxValue;
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="SimBaseNumericParameter{T}"/> class
        /// </summary>
        /// <param name="name">The name taxonomy entry of the parameter</param>
        /// <param name="unit">Unit of the parameter</param>
        /// <param name="value">The current value of the parameter</param>
        /// <param name="minValue">The minimum valid value</param>
        /// <param name="maxValue">The maximum valid value</param>
        /// <param name="allowedOperations">The operations the user is expected to perform on this parameter</param>
        protected SimBaseNumericParameter(string name, string unit, T value, T minValue, T maxValue,
            SimParameterOperations allowedOperations = SimParameterOperations.All)
            : base(name, value, allowedOperations)
        {
            this.Unit = unit;
            this.ValueMin = minValue;
            this.ValueMax = maxValue;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SimBaseNumericParameter{T}"/> class
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
        protected SimBaseNumericParameter(long localId, string name, string unit, SimCategory category, SimInfoFlow propagation,
            T value, T minValue, T maxValue,
            string description, SimParameterValueSource valueFieldPointer,
            SimParameterOperations allowedOperations = SimParameterOperations.All,
            SimParameterInstancePropagation instancePropagationMode = SimParameterInstancePropagation.PropagateIfInstance,
            bool isAutomaticallyGenerated = false)
            : base(localId, name, category, propagation, value, description, valueFieldPointer, allowedOperations, instancePropagationMode, isAutomaticallyGenerated)
        {
            this.Unit = unit;
            this.ValueMin = minValue;
            this.ValueMax = maxValue;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SimBaseNumericParameter{T}"/> class by copying all settings from another parameter
        /// </summary>
        /// <param name="original">The parameter to copy from</param>
        protected SimBaseNumericParameter(SimBaseNumericParameter<T> original) : base(original)
        {
            this.Unit = original.Unit;
            this.ValueMin = original.ValueMin;
            this.ValueMax = original.ValueMax;
        }
    }
}
