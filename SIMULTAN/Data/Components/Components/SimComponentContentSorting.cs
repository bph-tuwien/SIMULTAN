using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.Components
{
    /// <summary>
    /// Defines the types of sorting applied to the sub-components of a component.
    /// </summary>
    public enum SimComponentContentSorting
    {
        /// <summary>
        /// Sort by name.
        /// </summary>
        ByName = 0,
        /// <summary>
        /// Sort by the current slot name.
        /// </summary>
        BySlot = 1
    }
}
