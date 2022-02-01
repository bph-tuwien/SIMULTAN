using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Exceptions
{
    /// <summary>
    /// Exception which gets throws whenever an operation is performed which the current user isn't allowed to do.
    /// </summary>
    public class AccessDeniedException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the AccessDeniedException class
        /// </summary>
        public AccessDeniedException() : base() { }

        /// <summary>
        /// Initializes a new instance of the AccessDeniedException class
        /// </summary>
        /// <param name="message">The message that describes the error</param>
        public AccessDeniedException(string message) : base(message) { }
    }
}
