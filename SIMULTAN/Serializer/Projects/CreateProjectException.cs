using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Serializer.Projects
{
    /// <summary>
    /// A list of reasons why project creation has failed
    /// </summary>
    public enum CreateProjectExceptionReason
    {
        /// <summary>
        /// Happens when no MultiValue file could be found.
        /// </summary>
        MissingMultiValuesFile,
        /// <summary>
        /// Happens when no Component file could be found.
        /// </summary>
        MissingComponentFile,
    }

    /// <summary>
    /// Exception that gets thrown when creating a project has failed
    /// </summary>
    public class CreateProjectException : Exception
    {
        /// <summary>
        /// The reason why project creation has failed
        /// </summary>
        public CreateProjectExceptionReason Reason { get; }

        /// <summary>
        /// Initializes a new instance of the CreateProjectException class
        /// </summary>
        /// <param name="reason">The reason why project creation has failed</param>
        public CreateProjectException(CreateProjectExceptionReason reason)
        {
            this.Reason = reason;
        }
    }
}
