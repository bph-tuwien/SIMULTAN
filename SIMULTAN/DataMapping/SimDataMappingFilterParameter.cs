using SIMULTAN.Data.Components;
using SIMULTAN.Data.Taxonomy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SIMULTAN.DataMapping
{
    /// <summary>
    /// Properties that can be filtered on a <see cref="SimBaseParameter"/>
    /// </summary>
    public enum SimDataMappingParameterFilterProperties
    {
        /// <summary>
        /// The name of the parameter.
        /// Supports string, Regex or <see cref="SimTaxonomyEntryReference"/>
        /// </summary>
        Name = 0,
        /// <summary>
        /// Filters for the unit.
        /// Supports strings
        /// </summary>
        Unit = 1,
        /// <summary>
        /// Filters for the <see cref="SimBaseParameter.Propagation"/>.
        /// Supports <see cref="SimInfoFlow"/>
        /// </summary>
        Propagation = 2,
        /// <summary>
        /// Filters for the category of a parameter. Performs a check if all filter flags are present in the parameter.
        /// Supports <see cref="SimCategory"/>
        /// </summary>
        Category = 3,
    }

    /// <summary>
    /// Filter for parameters. Used by the <see cref="SimDataMappingRuleParameter"/>
    /// </summary>
    public class SimDataMappingFilterParameter : SimDataMappingFilterBase<SimDataMappingParameterFilterProperties>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SimDataMappingFilterParameter"/> class
        /// </summary>
        /// <param name="property">The property that should be filtered</param>
        /// <param name="value">The value the property is compared to. See description of <see cref="SimDataMappingParameterFilterProperties"/>
        /// to see which value types are supported</param>
        public SimDataMappingFilterParameter(SimDataMappingParameterFilterProperties property, object value) 
            : base(property, value) { }

        /// <summary>
        /// Creates a deep copy of the filter. Uses the <see cref="SimDataMappingFilterBase{TPropertyEnum}.CloneFilterValue(object)"/> method to clone the filter value.
        /// </summary>
        /// <returns>A deep copy of the filter</returns>
        public SimDataMappingFilterParameter Clone()
        {
            return new SimDataMappingFilterParameter(this.Property, CloneFilterValue(this.Value));
        }

        /// <summary>
        /// Returns True when the filtered object matches the filter
        /// </summary>
        /// <param name="parameter">The object the filter should be applied to</param>
        /// <returns>True when the filter matches the object, otherwise False</returns>
        public bool Match(SimBaseParameter parameter)
        {
            switch (this.Property)
            {
                case SimDataMappingParameterFilterProperties.Name:
                    if (this.Value is string sname)
                    {
                        var paramName = parameter.NameTaxonomyEntry.Name;
                        if (parameter.NameTaxonomyEntry.HasTaxonomyEntry())
                            paramName = parameter.NameTaxonomyEntry.TaxonomyEntryReference.Target.Key;

                        return paramName == sname;
                    }
                    else if (this.Value is Regex rname)
                    {
                        var paramName = parameter.NameTaxonomyEntry.Name;
                        if (parameter.NameTaxonomyEntry.HasTaxonomyEntry())
                            paramName = parameter.NameTaxonomyEntry.TaxonomyEntryReference.Target.Key;

                        return rname.IsMatch(paramName);
                    }
                    else if (this.Value is SimTaxonomyEntryReference tref)
                    {
                        if (parameter.NameTaxonomyEntry.HasTaxonomyEntry())
                            return parameter.NameTaxonomyEntry.TaxonomyEntryReference == tref;
                    }
                    else
                        throw new NotSupportedException("Unsupported value type");
                    return false;
                case SimDataMappingParameterFilterProperties.Unit:
                    if (this.Value is string sunit)
                    {
                        if (parameter is SimIntegerParameter intParameter)
                            return intParameter.Unit == sunit;
                        else if (parameter is SimDoubleParameter doubleParameter)
                            return doubleParameter.Unit == sunit;
                        return false;
                    }
                    else
                        throw new NotSupportedException("Unsupported value type");
                case SimDataMappingParameterFilterProperties.Propagation:
                    if (this.Value is SimInfoFlow flow)
                        return parameter.Propagation == flow;
                    else
                        throw new NotSupportedException("Unsupported value type");
                case SimDataMappingParameterFilterProperties.Category:
                    if (this.Value is SimCategory cat)
                        return parameter.Category.HasFlag(cat);
                    else
                        throw new NotSupportedException("Unsupported value type");
                default:
                    throw new NotImplementedException();
            }
        }
    }
}
