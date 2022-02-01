using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Exceptions
{
    /// <summary>
    /// Error codes for events during the opening and closing of a project.
    /// </summary>
    public enum ProjectErrorCode
    {
        /// <summary>
        /// Exception on project close or unload. A file or directory could not be properly deleted. No recovery is possible.
        /// </summary>
        ERR_ON_FILE_DELETE,
        /// <summary>
        /// Happens when a file couldn't be deleted because another process is using the file
        /// </summary>
        ERR_ON_FILE_DELETE_FILEINUSE,
        /// <summary>
        /// Happens when a file couldn't be deleted
        /// </summary>
        ERR_ON_FILE_DELETE_GENERIC,
        /// <summary>
        /// A file or directory could not be found on closing or unloading the project.
        /// </summary>
        ERR_ON_FILE_DELETE_FILENOTFOUND,
        /// <summary>
        /// Happens when an operation is executed but the project isn't in the right state for it.
        /// For example, Authentication is called but the project hasn't been loaded yet.
        /// </summary>
        ERR_INVALID_STATE,
        /// <summary>
        /// Happens when the service provider does not contain an authentication service
        /// </summary>
        ERR_AUTHSERVICE_NOT_FOUND,
    }

    /// <summary>
    /// Exception that gets throws when an error happens during project related operations
    /// </summary>
    public class ProjectIOException : Exception
    {
        /// <summary>
        /// The error code.
        /// </summary>
        public ProjectErrorCode Code { get; }

        /// <summary>
        /// Initializes a new instance of the ProjectIOException class
        /// </summary>
        /// <param name="errorCode">The error code (reason) for this error</param>
        /// <param name="message">An additional message</param>
        public ProjectIOException(ProjectErrorCode errorCode, string message) : base(message)
        {
            this.Code = errorCode;
        }
        /// <summary>
        /// Initializes a new instance of the ProjectIOException class
        /// </summary>
        /// <param name="errorCode">The error code (reason) for this error</param>
        /// <param name="message">An additional message</param>
        /// <param name="innerException">The exception that caused this exception</param>
        public ProjectIOException(ProjectErrorCode errorCode, string message, Exception innerException) : base(message, innerException)
        {
            this.Code = errorCode;
        }
    }
}
