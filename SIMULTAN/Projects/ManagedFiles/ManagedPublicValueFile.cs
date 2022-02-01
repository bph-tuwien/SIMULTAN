using SIMULTAN.Data.MultiValues;
using SIMULTAN.Serializer.Projects;
using System.IO;

namespace SIMULTAN.Projects.ManagedFiles
{
    /// <summary>
    /// Single <see cref="SimMultiValue"/> file management class for PUBLIC values.
    /// </summary>
    public class ManagedPublicValueFile : ManagedFile
    {
        /// <summary>
        /// Initializes a ManagedPublicValueFile.
        /// </summary>
        /// <param name="projectData">The project's data</param>
        /// <param name="_owner">the managing collection</param>
        /// <param name="_file">the file as it is in the file system</param>
        public ManagedPublicValueFile(ExtendedProjectData projectData, ManagedFileCollection _owner, FileInfo _file)
            : base(projectData, _owner, _file)
        { }

        /// <summary>
        /// Creates a deep copy of the original managed file. Each file has its own data manager.
        /// </summary>
        /// <param name="_original">the original managed file</param>
        /// <param name="_new_file_location">the location to which the actual file is to be copied</param>
        internal ManagedPublicValueFile(ManagedPublicValueFile _original, FileInfo _new_file_location)
            : base(_original, _new_file_location)
        {
        }

        /// <summary>
        /// Does not save the data, because it is read-only.
        /// The data is saved only when the managed file of all values is saved.
        /// </summary>
        public override void Save()
        { }

        /// <inheritdoc/>
        public override void Open(bool _clear_before_open)
        {
            if (_clear_before_open)
            {
                ProjectData.ValueManager.Clear();
                ProjectData.ValueManager.SetCallingLocation(null);
            }

            ProjectIO.OpenMultiValueFile(this.File, ProjectData, this.owner.Project.GlobalID,
                this.owner.Project.ImportLogFile, this.owner.Project);
        }
    }
}
