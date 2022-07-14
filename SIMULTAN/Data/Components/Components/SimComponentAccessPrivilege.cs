using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.Components
{
    /// <summary>
    /// The different access privileges a user may have on a component
    /// </summary>
    [Flags]
    public enum SimComponentAccessPrivilege
    {
        /// <summary>
        /// The user does not have access to this component
        /// </summary>
        None = 0,
        /// <summary>
        /// The user may only read from this component, but not modify it
        /// </summary>
        Read = 1,
        /// <summary>
        /// The user may modify the component
        /// </summary>
        Write = 2,
        /// <summary>
        /// The user may supervize the component
        /// </summary>
        Supervize = 4,
        /// <summary>
        /// The user may release/publish the component
        /// </summary>
        Release = 8,
        /// <summary>
        /// The user has all of the other privileges in this enumeration
        /// </summary>
        All = Read | Write | Supervize | Release,
    }
}
