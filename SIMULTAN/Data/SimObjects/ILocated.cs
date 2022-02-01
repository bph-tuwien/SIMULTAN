using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data
{
    /// <summary>
    /// Is located via an IReferenceLocation.
    /// </summary>
    public interface ILocated
    {
        /// <summary>
        /// The location that called / owns the object.
        /// </summary>
        IReferenceLocation CalledFromLocation { get; }

        /// <summary>
        /// Passes information about the calling location to the located object.
        /// </summary>
        /// <param name="_calling_location">the location of the call (project or master file)</param>
        void SetCallingLocation(IReferenceLocation _calling_location);
    }
}
