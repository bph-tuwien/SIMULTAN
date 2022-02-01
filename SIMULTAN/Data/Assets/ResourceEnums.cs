using SIMULTAN;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.Assets
{
    /// <summary>
    /// Indicates what problem arouse during the parsing of a resource.
    /// </summary>
    [Flags]
    public enum ResourceLocationError
    {
        /// <summary>
        /// No errors.
        /// </summary>
        OK = 0,
        /// <summary>
        /// A contained resource resides outside of the working directory.
        /// </summary>
        CONTAINED_NOT_IN_WORKING_DIR = 1,
        /// <summary>
        /// A linked resource resides inside the working directory.
        /// </summary>
        LINKED_IN_WORKING_DIR = 2,
        /// <summary>
        /// A linked resource is not in any of the fallback directories of the asset manager.
        /// </summary>
        LINKED_NOT_IN_FALLBACKS = 4,
        /// <summary>
        /// Any type of resource is in a valid directory, but not known to the asset manager.
        /// </summary>
        RESOURCE_IN_UNFAMILIAR_DIR = 8,
        /// <summary>
        /// The resource could not be retrieved from the local file system.
        /// </summary>
        RESOURCE_NOT_FOUND = 16,
        /// <summary>
        /// There is not enough information to look for the source (e.g., no relative path).
        /// </summary>
        RESOURCE_CANNOT_BE_LOOKED_FOR = 32
    }
}
