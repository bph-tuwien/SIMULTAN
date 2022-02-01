using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.Components
{
    /// <summary>
    /// Specifies when a parameter value should be propagated to instances
    /// </summary>
    public enum SimParameterInstancePropagation : int
    {
        /// <summary>
        /// This parameter is never propagated
        /// </summary>
        PropagateNever = 1,
        /// <summary>
        /// This parameter is propagated if the <see cref="SimComponentInstance.PropagateParameterChanges"/> is True
        /// </summary>
        PropagateIfInstance = 0,
        /// <summary>
        /// Always propagate values of this parameter, not matter what <see cref="SimComponentInstance.PropagateParameterChanges"/> is set to
        /// </summary>
        PropagateAlways = 2
    }
}
