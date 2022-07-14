using SIMULTAN.Data.Components;
using SIMULTAN.Serializer.Projects;
using System.IO;

namespace SIMULTAN.Projects.ManagedFiles
{
    /// <summary>
    /// Single <see cref="SimComponent"/> file management class for PUBLIC components.
    /// </summary>
    public class ManagedPublicComponentFile : ManagedFile
    {
        /// <summary>
        /// Initializes a ManagedPublicComponentFile.
        /// </summary>
        /// <param name="_owner">the owning collection</param>
        /// <param name="_file">the file as it is in the file system</param>
        /// <param name="projectData">The project data for this instance</param>
        public ManagedPublicComponentFile(ExtendedProjectData projectData, ManagedFileCollection _owner, FileInfo _file)
            : base(projectData, _owner, _file)
        { }

        /// <summary>
        /// Creates a deep copy of the original managed file. Each file has its own data manager.
        /// </summary>
        /// <param name="_original">the original managed file</param>
        /// <param name="_new_file_location">the location to which the actual file is to be copied</param>
        internal ManagedPublicComponentFile(ManagedPublicComponentFile _original, FileInfo _new_file_location)
            : base(_original, _new_file_location)
        { }

        /// <summary>
        /// Does not save the data, because it is read-only.
        /// The data is saved only when the managed file of all components is saved.
        /// </summary>
        public override void Save()
        { }

        /// <inheritdoc/>
        public override void Open(bool _clear_before_open)
        {
            if (_clear_before_open)
            {
                ProjectData.NetworkManager.ClearRecord();
                ProjectData.AssetManager.Reset();
                ProjectData.Components.Clear();
            }

            //DO NOTHING

            //ProjectIO.OpenComponentFile(this.File, ProjectData,
            //    this.owner.Project.GlobalID, this.owner.Project.ImportLogFile, this.owner.Project);
        }
    }
}
