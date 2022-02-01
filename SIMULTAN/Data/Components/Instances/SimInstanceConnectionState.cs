using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.Components
{
    /// <summary>
    /// Describes the state of an instance
    /// </summary>
    public enum SimInstanceConnectionState
    {
        /// <summary>
        /// The target was found and corresponds to a saved id.
        /// </summary>
        Ok = 0,
        /// <summary>
        /// The geometry was explicitly deleted by the user.
        /// </summary>
        GeometryDeleted = 1,
        /// <summary>
        /// The geometry could not be found in the currently open geometry file.
        /// </summary>
        GeometryNotFound = 2,
    }
}
