using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.Components
{
    /// <summary>
    /// The visibility of a component on a project level.
    /// </summary>
    public enum SimComponentVisibility
    {
        /// <summary>
        /// Public: always visible.
        /// </summary>
        AlwaysVisible = 0,
        /// <summary>
        /// Internal: visible only within the project.
        /// </summary>
        VisibleInProject = 1,
        /// <summary>
        /// Private: can be hidden in certain views. Currently not used in UI
        /// </summary>
        Hidden = 2
    }
}
