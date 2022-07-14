using SIMULTAN.Data.Users;
using SIMULTAN.Serializer.Projects;
using System.IO;

namespace SIMULTAN.Projects.ManagedFiles
{
    /// <summary>
    /// Single <see cref="SimUser"/> file management class.
    /// </summary>
    public class ManagedUserFile : ManagedFile
    {
        /// <summary>
        /// Initializes a ManagedUserFile.
        /// </summary>
        /// <param name="projectData">The project's data</param>
        /// <param name="_owner">the managing collection</param>
        /// <param name="_file">the file as it is in the file system</param>
        public ManagedUserFile(ExtendedProjectData projectData, ManagedFileCollection _owner, FileInfo _file)
            : base(projectData, _owner, _file)
        { }

        /// <summary>
        /// Creates a deep copy of the original managed file. Caution: both files reference THE SAME DATA MANAGER!
        /// </summary>
        /// <param name="_original">the original managed file</param>
        /// <param name="_new_file_location">the location to which the actual file is to be copied</param>
        internal ManagedUserFile(ManagedUserFile _original, FileInfo _new_file_location)
            : base(_original, _new_file_location)
        { }

        /// <inheritdoc/>
        public override void Save()
        {
            ProjectIO.SaveUserFile(this.File, ProjectData.UsersManager, ProjectIO.ENCR_KEY);
            this.OnFileUpToDateChanged(true);
        }

        /// <inheritdoc/>
        public override void Open(bool _clear_before_open)
        {
            if (_clear_before_open)
                ProjectData.UsersManager.Clear();

            ProjectIO.OpenUserFile(this.File, ProjectData);
        }
    }
}
