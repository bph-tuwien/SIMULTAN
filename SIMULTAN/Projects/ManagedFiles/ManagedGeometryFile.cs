using SIMULTAN.Data.Components;
using SIMULTAN.Serializer.Projects;
using System.IO;

namespace SIMULTAN.Projects.ManagedFiles
{
    /// <summary>
    /// A management class for a single file containing geometry.
    /// </summary>
    public class ManagedGeometryFile : ManagedFile
    {
        /// <summary>
        /// Initializes a ManagedGeometryFile.
        /// </summary>
        /// <param name="projectData">The project's data</param>
        /// <param name="_owner">the managing collection</param>
        /// <param name="_file">the file as it is in the file system</param>
        public ManagedGeometryFile(ExtendedProjectData projectData, ManagedFileCollection _owner, FileInfo _file)
            : base(projectData, _owner, _file)
        { }

        /// <summary>
        /// Creates a deep copy of the original managed file. Caution: both files reference THE SAME DATA MANAGER!
        /// </summary>
        /// <param name="_original">the original managed file</param>
        /// <param name="_new_file_location">the location to which the actual file is to be copied</param>
        internal ManagedGeometryFile(ManagedGeometryFile _original, FileInfo _new_file_location)
            : base(_original, _new_file_location)
        {
            this.CorrespondingResourceIndex = _original.CorrespondingResourceIndex;
        }

        /// <inheritdoc/>
        public override void Save()
        {
            if (ProjectIO.SaveGeometryFile(this))
                this.OnFileUpToDateChanged(true);
        }

        /// <inheritdoc/>
        public override void OnDeleted(int _resource_id)
        {
            base.OnDeleted(_resource_id);
            ProjectData.NetworkManager.DisconnectAllInstances(_resource_id);
            ProjectData.SimNetworks.DisconnectAllInstances(_resource_id);
            ProjectData.Components.OnGeometryResourceDeleted(_resource_id);
        }

        /// <summary>
        /// Checks, if the managed file has a valid path. The data manager can be Null.
        /// Saving is possible only via the data manager.
        /// </summary>
        /// <returns>true, if valid; false, if invalid</returns>
        public override bool IsValid()
        {
            bool exists_and_valid = this.File != null && System.IO.File.Exists(this.File.FullName);
            bool does_not_exist = this.File == null;
            return exists_and_valid || does_not_exist;
        }
    }
}
