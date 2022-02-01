using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.Components
{
    /// <summary>
    /// Specifies the state in which a <see cref="SimInstancePlacement"/> is in
    /// </summary>
    [Flags]
    public enum SimInstancePlacementState
    {
        /// <summary>
        /// The instance placement is valid and doesn't have any problems
        /// </summary>
        Valid = 0,
        /// <summary>
        /// The target of the instance placement is missing
        /// </summary>
        InstanceTargetMissing = 1,
    }
}
