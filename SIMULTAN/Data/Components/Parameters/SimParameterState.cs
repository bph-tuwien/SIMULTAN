using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.Components
{
    /// <summary>
    /// Describes the state of a parameter
    /// </summary>
    [Flags]
    public enum SimParameterState
    {
        /// <summary>
        /// The parameter is valid, no problems were found
        /// </summary>
        Valid = 0,
        /// <summary>
        /// The current numerical value is set to NaN
        /// </summary>
        ValueNaN = 1,
        /// <summary>
        /// The numerical value is outside of the expected range
        /// </summary>
        ValueOutOfRange = 2,
        /// <summary>
        /// The parameter hides a parameter in a referenced component
        /// </summary>
        HidesReference = 4,
        /// <summary>
        /// The parameter references another paramter but there is no valid target
        /// </summary>
        ReferenceNotFound = 8
    }
}
