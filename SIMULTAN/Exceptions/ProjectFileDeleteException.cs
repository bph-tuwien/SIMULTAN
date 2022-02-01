using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Exceptions
{
    /// <summary>
    /// Exception thrown because the project failed to delete a file
    /// </summary>
    public class ProjectFileDeleteException : ProjectIOException
    {
        /// <summary>
        /// The files which could not be deleted
        /// path: The full path to the file
        /// reason: The exact reason why the file couldn't be deleted
        /// additionalInfo: Additional data, depends on the reason
        /// </summary>
        public List<(string path, ProjectErrorCode reason, object additionalInfo)> Files { get; }

        /// <summary>
        /// Initializes an instance of the ProjectFileDeleteException class.
        /// </summary>
        /// <param name="files">A list of files which could not be deleted, together with a reason and additional data</param>
        public ProjectFileDeleteException(List<(string path, ProjectErrorCode reason, object additionalInfo)> files)
            : base(ProjectErrorCode.ERR_ON_FILE_DELETE, "Could not delete one or more files")
        {
            this.Files = files;
        }
    }
}
