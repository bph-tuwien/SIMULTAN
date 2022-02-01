using SIMULTAN.Serializer.Projects;
using System.IO;

namespace SIMULTAN.Projects.ManagedFiles
{
    /// <summary>
    /// File that manages linked directories for linked resources.
    /// </summary>
    public class ManagedLinksFile : ManagedFile
    {
        /// <summary>
        /// Initializes a ManagedLinksFile.
        /// </summary>
        /// <param name="projectData">The project's data</param>
        /// <param name="_owner">the managing collection</param>
        /// <param name="_file">the file as it is in the file system</param>
        public ManagedLinksFile(ExtendedProjectData projectData, ManagedFileCollection _owner, FileInfo _file)
            : base(projectData, _owner, _file)
        { }

        /// <summary>
        /// Creates a deep copy of the original managed file. Caution: both files reference THE SAME DATA MANAGER!
        /// </summary>
        /// <param name="_original"></param>
        /// <param name="_new_file_location"></param>
        internal ManagedLinksFile(ManagedLinksFile _original, FileInfo _new_file_location)
            : base(_original, _new_file_location)
        { }

        /// <inheritdoc/>
        public override void Save()
        {
            ProjectIO.SaveLinksFile(this.File, ProjectData.MultiLinkManager);
            this.OnFileUpToDateChanged(true);
        }

        /// <inheritdoc/>
        public override void Open(bool _clear_before_open)
        {
            if (_clear_before_open)
                ProjectData.MultiLinkManager.Clear();

            ProjectIO.OpenLinksFile(this.File, ProjectData.MultiLinkManager);

            // TODO: synchronize the links with the asset manager of the component factory
        }

        /// <inheritdoc/>
        public override void Reset()
        {
            base.Reset();
        }
    }
}
