using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.Geometry
{
    /// <summary>
    /// Mapping struct from geometry file Id to file path. Used to look up the file ids from exported geometry files
    /// with relations so the linked files can be migrated properly.
    /// </summary>
    public struct GeometryRelationsFileMapping
    {
        /// <summary>
        /// The file Id of the geometry file.
        /// </summary>
        public int FileId { get; }
        /// <summary>
        /// The file path relative to the project of the geometry file.
        /// </summary>
        public string FilePath { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="GeometryRelationsFileMapping"/> class.
        /// </summary>
        /// <param name="fileId">The file id.</param>
        /// <param name="path">The path relative to the project.</param>
        public GeometryRelationsFileMapping(int fileId, string path)
        {
            this.FileId = fileId;
            this.FilePath = path;
        }
    }
}
