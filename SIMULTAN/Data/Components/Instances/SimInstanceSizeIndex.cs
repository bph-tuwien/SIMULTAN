using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.Components
{
    /// <summary>
    /// Enumeration with indices into a <see cref="ISimInstanceSizeTransferDefinition"/>
    /// </summary>
    public enum SimInstanceSizeIndex : int
    {
        /// <summary>
        /// The minimum size along the X-Axis
        /// </summary>
        MinX = 0,
        /// <summary>
        /// The minimum size along the Y-Axis
        /// </summary>
        MinY = 1,
        /// <summary>
        /// The minimum size along the Z-Axis
        /// </summary>
        MinZ = 2,

        /// <summary>
        /// The maximum size along the X-Axis
        /// </summary>
        MaxX = 3,
        /// <summary>
        /// The maximum size along the Y-Axis
        /// </summary>
        MaxY = 4,
        /// <summary>
        /// The maximum size along the Z-Axis
        /// </summary>
        MaxZ = 5
    }
}
