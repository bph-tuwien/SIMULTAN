using SIMULTAN.Serializer.GRDXF;
using SIMULTAN.Serializer.Projects;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Projects.ManagedFiles
{
    /// <summary>
    /// Managed file for GeometryRelations
    /// </summary>
    public class ManagedGeometryRelationsFile : ManagedFile
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="ManagedGeometryRelationsFile"/> class.
        /// </summary>
        /// <param name="_original">The original managed file.</param>
        /// <param name="_new_file_location">The new file location.</param>
        public ManagedGeometryRelationsFile(ManagedFile _original, FileInfo _new_file_location) : base(_original, _new_file_location)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ManagedGeometryRelationsFile"/> class.
        /// </summary>
        /// <param name="projectData">The project data.</param>
        /// <param name="_owner">The owner.</param>
        /// <param name="_file">The file.</param>
        public ManagedGeometryRelationsFile(ExtendedProjectData projectData, ManagedFileCollection _owner, FileInfo _file) : base(projectData, _owner, _file)
        {
        }

        /// <inheritdoc/>
        public override void Open(bool _clear_before_open)
        {
            ProjectIO.OpenGeometryRelationsFile(File, ProjectData);
        }

        /// <inheritdoc/>
        public override void Save()
        {
            SimGeometryRelationsDxfIO.Write(File, ProjectData.GeometryRelations);
            File.LastWriteTime = DateTime.Now;
            this.OnFileUpToDateChanged(true);
        }
    }
}
