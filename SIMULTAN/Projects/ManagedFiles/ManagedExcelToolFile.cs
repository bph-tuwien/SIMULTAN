using SIMULTAN.Excel;
using SIMULTAN.Serializer.DXF;
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
    /// A management class for a single file containing <see cref="ExcelTool"/> instances.
    /// </summary>
    public class ManagedExcelToolFile : ManagedFile
    {
        /// <summary>
        /// Initializes a ManagedExcelToolFile.
        /// </summary>
        /// <param name="projectData">The project's data</param>
        /// <param name="_owner">the managing collection</param>
        /// <param name="_file">the file as it is in the file system</param>
        public ManagedExcelToolFile(ExtendedProjectData projectData, ManagedFileCollection _owner, FileInfo _file)
            : base(projectData, _owner, _file)
        { }

        /// <summary>
        /// Creates a deep copy of the original managed file. Caution: both files reference THE SAME DATA MANAGER!
        /// </summary>
        /// <param name="_original">the original managed file</param>
        /// <param name="_new_file_location">the location to which the actual file is to be copied</param>
        internal ManagedExcelToolFile(ManagedExcelToolFile _original, FileInfo _new_file_location)
            : base(_original, _new_file_location)
        { }

        /// <inheritdoc/>
        public override void Save()
        {
            ProjectIO.SaveExcelToolCollectionFile(this.File, this.ProjectData);
            this.OnFileUpToDateChanged(true);
        }

        /// <inheritdoc/>
        public override void Open(bool _clear_before_open)
        {
            ProjectIO.OpenExcelToolCollectionFile(this.File, this.ProjectData);
        }
    }
}
