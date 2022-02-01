using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Exceptions
{
    /// <summary>
    /// Thrown when a file is already in use
    /// </summary>
    public class FileInUseException : Exception
    {
        /// <summary>
        /// The file in question
        /// </summary>
        public FileInfo File { get; private set; }

        /// <summary>
        /// Initializes a new instance of the FileInUseException class
        /// </summary>
        /// <param name="file">The file that has already been in use</param>
        public FileInUseException(FileInfo file) : base(string.Format("File {0} is used by another thread or process.", file.FullName))
        {
            this.File = file;
        }
    }
}
