using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.Geometry
{
    /// <summary>
    /// Interface for managing the information exchange between components and geometry and resource file management.
    /// </summary>
    public interface ILoaderGeometryExchange : IOffsetQueryable
    {
        /// <summary>
        /// Checks if the resource file with the given path already exists.
        /// </summary>
        /// <param name="file">The file with file extension, *not* the path</param>
        /// <returns>true, if the resource file was found on record</returns>
        bool ResourceFileExists(FileInfo file);

        /// <summary>
        /// Checks if the file can be added as a resource file.
        /// </summary>
        /// <param name="fi">the info about the file</param>
        /// <param name="isContained">if true, the file is contained in the project's working directory</param>
        /// <returns>true, if the file can be used as a resource, false otherwise</returns>
        bool IsValidResourcePath(FileInfo fi, bool isContained);

        /// <summary>
        /// Adds the file name to the list of resources.
        /// </summary>
        /// <param name="fileName">the name of the file to add</param>
        void AddResourceFile(FileInfo fileName);

        /// <summary>
        /// Returns the index of the given file in the resource manager.
        /// </summary>
        /// <param name="fi">the info about the file</param>
        /// <returns>Either the index or -1</returns>
        int GetResourceFileIndex(FileInfo fi);

        /// <summary>
        /// Returns the FileInfo for a given resource index
        /// </summary>
        /// <param name="resourceIndex"></param>
        /// <returns></returns>
        FileInfo GetFileFromResourceIndex(int resourceIndex);
    }
}
