using SIMULTAN.Data.Taxonomy;

namespace SIMULTAN.Data.Components
{

    /// <summary>
    /// Parameter storing bool values
    /// </summary>
    public class SimBoolParameter : SimBaseParameter<bool>
    {
        #region .CTOR


        /// <summary>
        /// Initializes a new instance of the SimParameter class
        /// </summary>
        /// <param name="nameTaxonomyEntry">The name taxonomy entry of the parameter</param>
        /// <param name="value">The current value of the parameter</param>
        /// <param name="allowedOperations">The operations the user is expected to perform on this parameter</param>
        public SimBoolParameter(SimTaxonomyEntry nameTaxonomyEntry, bool value,
            SimParameterOperations allowedOperations = SimParameterOperations.All)
            : base(nameTaxonomyEntry, value, allowedOperations)
        {
            UpdateState();
        }

        /// <summary>
        /// Initializes a new instance of the SimParameter class
        /// </summary>
        /// <param name="name">The name of the parameter</param>
        /// <param name="value">The current value of the parameter</param>
        /// <param name="allowedOperations">The operations the user is expected to perform on this parameter</param>
        public SimBoolParameter(string name, bool value, SimParameterOperations allowedOperations = SimParameterOperations.All)
            : base(name, value, allowedOperations)
        {
            UpdateState();
        }


        /// <summary>
        /// Initializes a new instance of the SimParameter class
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
        internal SimBoolParameter(long localId, string name, SimCategory category, SimInfoFlow propagation,
                           bool value,
                           string description, SimParameterValueSource valueFieldPointer,
                           SimParameterOperations allowedOperations = SimParameterOperations.All,
                           SimParameterInstancePropagation instancePropagationMode = SimParameterInstancePropagation.PropagateIfInstance,
                           bool isAutomaticallyGenerated = false)
            : base(localId, name, category, propagation, value, description, valueFieldPointer,
                  allowedOperations, instancePropagationMode, isAutomaticallyGenerated)
        {
            UpdateState();
        }

        /// <summary>
        /// Initializes a new instance of the Parameter class by copying all settings from another parameter
        /// </summary>
        /// <param name="original">The parameter to copy from</param>
        protected SimBoolParameter(SimBoolParameter original) : base(original)
        {
            UpdateState();
        }


        #endregion


        /// <inheritdoc/>
        public override SimBaseParameter Clone()
        {
            return new SimBoolParameter(this);
        }

        /// <summary>
        /// Converts the data value of any other parameters to the value of a <see cref="SimBoolParameter"/>
        /// </summary>
        /// <param name="value">The value of the other parameter</param>
        /// <returns>The converted boolean. False for everything that can't be converted</returns>
        public static bool ConvertFromValue(object value)
        {
            switch (value)
            {
                case double d:
                    return (d != 0.0);
                case bool b:
                    return b;
                case int i:
                    return (i != 0);
                case string s:
                    if (bool.TryParse(s, out var sv))
                    {
                        return sv;
                    }
                    else
                    {
                        return false;
                    }
                default:
                    return false;
            }
        }
        /// <inheritdoc />
        public override void ConvertValueFrom(object value)
        {
            this.Value = ConvertFromValue(value);
        }
        /// <inheritdoc />
        internal override bool IsSameValue(bool value1, bool value2)
        {
            return value1 == value2;
        }
        /// <inheritdoc />
        public override void SetToNeutral()
        {
            this.Value = false;
        }
    }
}
