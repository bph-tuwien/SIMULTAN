using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.Components
{
    /// <summary>
    /// Defines which operations a user is expected to perform on a parameter
    /// </summary>
    [Flags]
    public enum SimParameterOperations
    {
        /// <summary>
        /// No operations are allowed
        /// </summary>
        None = 0,
        /// <summary>
        /// The user may edit the value of the parameter
        /// </summary>
        EditValue = 1,
        /// <summary>
        /// The user may edit the name of the parameter
        /// </summary>
        EditName = 2,
        /// <summary>
        /// The user may move the parameter to a different component
        /// </summary>
        Move = 4,
        /// <summary>
        /// The user may perform all possible operations
        /// </summary>
        All = EditValue | EditName | Move,
    }
}
