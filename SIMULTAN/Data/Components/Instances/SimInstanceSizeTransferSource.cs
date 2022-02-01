using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.Components
{
    /// <summary>
    /// Specifies the source of a <see cref="SimInstanceSizeTransferDefinitionItem"/>
    /// </summary>
    public enum SimInstanceSizeTransferSource
    {
        /// <summary>
        /// The size is given by the user
        /// </summary>
        User = 0,
        /// <summary>
        /// The size is derived from the parameter value plus the <see cref="SimInstanceSizeTransferDefinitionItem.Addend"/>
        /// </summary>
        Parameter = 1,
        /// <summary>
        /// The size is defined by the length of the instance path
        /// </summary>
        Path = 2
    }
}
