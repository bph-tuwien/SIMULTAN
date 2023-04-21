using SIMULTAN.Data.Components;
using SIMULTAN.Data.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SIMULTAN.DataMapping
{
    /// <summary>
    /// Properties that can be filtered on a <see cref="Volume"/>
    /// </summary>
    public enum SimDataMappingVolumeFilterProperties
    {
        /// <summary>
        /// The name of the value.
        /// Supports string and Regex
        /// </summary>
        Name,
        /// <summary>
        /// Filters based on the key of the resource file the face is stored in
        /// </summary>
        FileKey //Special handling outside of the filter to prevent geometry model loading when not necessary
    }

    /// <summary>
    /// Filter for parameters. Used by the <see cref="SimDataMappingRuleVolume"/>
    /// </summary>
    public class SimDataMappingFilterVolume : SimDataMappingFilterBase<SimDataMappingVolumeFilterProperties>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SimDataMappingFilterVolume"/> class
        /// </summary>
        /// <param name="property">The property that should be filtered</param>
        /// <param name="value">The value the property is compared to. See description of <see cref="SimDataMappingParameterFilterProperties"/>
        /// to see which value types are supported</param>
        public SimDataMappingFilterVolume(SimDataMappingVolumeFilterProperties property, object value) 
            : base(property, value) { }

        /// <summary>
        /// Creates a deep copy of the filter. Uses the <see cref="SimDataMappingFilterBase{TPropertyEnum}.CloneFilterValue(object)"/> method to clone the filter value.
        /// </summary>
        /// <returns>A deep copy of the filter</returns>
        public SimDataMappingFilterVolume Clone()
        {
            return new SimDataMappingFilterVolume(this.Property, CloneFilterValue(this.Value));
        }

        /// <summary>
        /// Returns True when the filtered object matches the filter
        /// </summary>
        /// <param name="volume">The object the filter should be applied to</param>
        /// <returns>True when the filter matches the object, otherwise False</returns>
        public bool Match(Volume volume)
        {
            switch (this.Property)
            {
                case SimDataMappingVolumeFilterProperties.Name:
                    if (this.Value is string sname)
                        return volume.Name == sname;
                    else if (this.Value is Regex rname)
                        return rname.IsMatch(volume.Name);
                    else
                        throw new NotSupportedException("Unsupported value type");
                case SimDataMappingVolumeFilterProperties.FileKey:
                    return true; //Has already been handled before arriving here
                default:
                    throw new NotImplementedException();
            }
        }
    }
}
