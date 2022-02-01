using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.Geometry
{
    /// <summary>
    /// Describes an Orientation
    /// </summary>
    public enum GeometricOrientation
    {
        /// <summary>
        /// Orientated Forward
        /// </summary>
        Forward = 1,
        /// <summary>
        /// Orientated Backward
        /// </summary>
        Backward = -1,
        /// <summary>
        /// Undefined Orientation
        /// </summary>
        Undefined = 0
    }
}
