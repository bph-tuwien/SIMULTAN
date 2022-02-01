using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Exceptions
{
    /// <summary>
    /// Exception thrown whenever an operation sets the datamodel in an invalid state
    /// </summary>
    public class InvalidStateException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the InvalidStateException class
        /// </summary>
        /// <param name="message">An error message for the user</param>
        public InvalidStateException(string message) : base(message) { }

        /// <summary>
        /// Initializes a new instance of the InvalidStateException class
        /// </summary>
        /// <param name="message">An error message for the user</param>
        /// <param name="innerException">The exception which caused this exception</param>
        public InvalidStateException(string message, Exception innerException) : base(message, innerException) { }
    }
}
