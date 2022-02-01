using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data
{
    /// <summary>
    /// Can be the location of a reference in a type derived from BaseReference.
    /// </summary>
    public interface IReferenceLocation
    {
        /// <summary>
        /// An id of type Guid.
        /// </summary>
        Guid GlobalID { get; }

        /// <summary>
        /// The absolute path in the file system.
        /// </summary>
        string AbsolutePath { get; }
    }
}
