using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Exceptions
{
    /// <summary>
    /// Exception that is thrown when the georeferencing of an object is invalid, i.e. if ValidatieGeoReferences(..) returns false
    /// </summary>
    public class InvalidGeoReferencingException : Exception
    {
        /// <summary>
        /// Creates a new instance of this class
        /// </summary>
        public InvalidGeoReferencingException() : base("Could not perform Geo-Referencing. Please make sure that enough Geo-References (at least 3) are present and they are not placed on the same line.")
        { }
    }
}
