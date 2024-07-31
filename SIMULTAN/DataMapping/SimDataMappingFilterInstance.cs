using SIMULTAN.Data.Components;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SIMULTAN.DataMapping
{
    /// <summary>
    /// Properties that can be filtered in a <see cref="SimComponentInstance"/>
    /// </summary>
    public enum SimDataMappingInstanceFilterProperties
    {
        /// <summary>
        /// The name of the instance.
        /// Supports string or Regex
        /// </summary>
        Name = 0,
        /// <summary>
        /// The type of the instance.
        /// Supports <see cref="SimInstanceType"/>
        /// </summary>
        InstanceType = 1,
    }

    /// <summary>
    /// Filter for component instances. Used by the <see cref="SimDataMappingRuleInstance"/>
    /// </summary>
    public class SimDataMappingFilterInstance : SimDataMappingFilterBase<SimDataMappingInstanceFilterProperties>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SimDataMappingFilterComponent"/> class
        /// </summary>
        /// <param name="property">The property that should be filtered</param>
        /// <param name="value">The value the property is compared to. See description of <see cref="SimDataMappingInstanceFilterProperties"/>
        /// to see which value types are supported</param>
        public SimDataMappingFilterInstance(SimDataMappingInstanceFilterProperties property, object value)
            : base(property, value) { }

        /// <summary>
        /// Creates a deep copy of the filter. Uses the <see cref="SimDataMappingFilterBase{TPropertyEnum}.CloneFilterValue(object)"/> method to clone the filter value.
        /// </summary>
        /// <returns>A deep copy of the filter</returns>
        public SimDataMappingFilterInstance Clone()
        {
            return new SimDataMappingFilterInstance(this.Property, CloneFilterValue(this.Value));
        }

        /// <summary>
        /// Returns True when the filtered object matches the filter
        /// </summary>
        /// <param name="instance">The object the filter should be applied to</param>
        /// <returns>True when the filter matches the object, otherwise False</returns>
        public bool Match(SimComponentInstance instance)
        {
            switch (this.Property)
            {
                case SimDataMappingInstanceFilterProperties.Name:
                    if (this.Value is string sname)
                        return instance.Name == sname;
                    else if (this.Value is Regex rname)
                        return rname.IsMatch(instance.Name);
                    else
                        throw new NotSupportedException("Unsupported value type");
                case SimDataMappingInstanceFilterProperties.InstanceType:
                    if (this.Value is SimInstanceType itype)
                        return instance.Placements.Any(x => x.InstanceType.HasFlag(itype));
                    else
                        throw new NotSupportedException("Unsupported value type");
                default:
                    throw new NotImplementedException();
            }
        }
    }
}
