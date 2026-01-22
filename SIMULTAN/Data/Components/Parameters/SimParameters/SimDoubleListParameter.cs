using SIMULTAN.Data.Taxonomy;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.Components
{
    /// <summary>
    /// A double list parameter
    /// </summary>
    public class SimDoubleListParameter : SimBaseListParameter<double>
    {
        /// <summary>
        /// Creates a copy of another <see cref="SimDoubleListParameter"/>
        /// </summary>
        /// <param name="original">The original</param>
        /// <param name="copyValue">If the value should be copied</param>
        public SimDoubleListParameter(SimBaseParameter<SimParameterValueCollection<double>> original, bool copyValue = true)
            : base(original, copyValue)
        {
        }

        /// <summary>
        /// Creates a new <see cref="SimDoubleListParameter"/>
        /// </summary>
        /// <param name="nameTaxonomyEntry">The name</param>
        /// <param name="value">The value</param>
        /// <param name="allowedOperations">The allowed operations</param>
        public SimDoubleListParameter(SimTaxonomyEntry nameTaxonomyEntry, SimParameterValueCollection<double> value, SimParameterOperations allowedOperations = SimParameterOperations.All)
            : base(nameTaxonomyEntry, value, allowedOperations)
        {
        }

        /// <summary>
        /// Creates a new <see cref="SimDoubleListParameter"/>
        /// </summary>
        /// <param name="name">The name</param>
        /// <param name="value">The value</param>
        /// <param name="allowedOperations">The allowed operations</param>
        public SimDoubleListParameter(string name, SimParameterValueCollection<double> value, SimParameterOperations allowedOperations = SimParameterOperations.All)
            : base(name, value, allowedOperations)
        {
        }

        /// <summary>
        /// Creates a new <see cref="SimDoubleListParameter"/>
        /// </summary>
        /// <param name="localId">The local ID</param>
        /// <param name="name">The name</param>
        /// <param name="category">The category</param>
        /// <param name="propagation">The propagation mode</param>
        /// <param name="value">The value</param>
        /// <param name="description">The description</param>
        /// <param name="valueFieldPointer">The value field pointer</param>
        /// <param name="allowedOperations">Allowed operations</param>
        /// <param name="instancePropagationMode">The instance propagation mode</param>
        /// <param name="isAutomaticallyGenerated">If the parameter was automatically generated</param>
        public SimDoubleListParameter(long localId, string name, SimCategory category, SimInfoFlow propagation, SimParameterValueCollection<double> value, string description, SimParameterValueSource valueFieldPointer, SimParameterOperations allowedOperations = SimParameterOperations.All, SimParameterInstancePropagation instancePropagationMode = SimParameterInstancePropagation.PropagateIfInstance, bool isAutomaticallyGenerated = false)
            : base(localId, name, category, propagation, value, description, valueFieldPointer, allowedOperations, instancePropagationMode, isAutomaticallyGenerated)
        {
        }

        /// <inheritdoc/>
        public override SimBaseParameter Clone()
        {
            return new SimDoubleListParameter(this);
        }

        /// <inheritdoc/>
        public override void ConvertValueFrom(object value)
        {
            if (value is SimParameterValueCollection<double> coll)
            {
                Value = new SimParameterValueCollection<double>(coll);
            }
            else if (value is double dValue)
            {
                Value = new SimParameterValueCollection<double> { dValue };
            }
            else if (value is int iValue)
            {
                Value = new SimParameterValueCollection<double> { (double)iValue };
            }
            else if (value is bool bValue)
            {
                Value = new SimParameterValueCollection<double> { bValue ? 1.0 : 0.0 };
            }
            else if (value is string sValue && double.TryParse(sValue, out var dpVal))
            {
                Value = new SimParameterValueCollection<double> { dpVal };
            }
            else
            {
                SetToNeutral();
            }
        }

        /// <inheritdoc/>
        public override void SetToNeutral()
        {
            Value = new SimParameterValueCollection<double>();
        }

        internal override bool IsSameValue(SimParameterValueCollection<double> value1, SimParameterValueCollection<double> value2)
        {
            if (value1 == null && value2 == null)
                return true;
            if (value1 == null || value2 == null)
                return false;
            if (value1.Count != value2.Count)
                return false;
            for (int i = 0; i < value1.Count; i++)
            {
                if (value1[i] != value2[i])
                    return false;
            }
            return true;
        }
    }
}
