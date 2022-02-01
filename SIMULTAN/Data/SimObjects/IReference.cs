using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data
{
    /// <summary>
    /// Can be a reference in a type derived from BaseReference.
    /// </summary>
    public interface IReference
    {
        /// <summary>
        /// An id of type long.
        /// </summary>
        long LocalID { get; }
        /// <summary>
        /// A global id / location of type Guid.
        /// </summary>
        Guid GlobalID { get; }
    }
}
