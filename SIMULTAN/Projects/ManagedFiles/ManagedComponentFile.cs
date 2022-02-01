using SIMULTAN.Data.Components;
using SIMULTAN.Serializer.Projects;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Projects.ManagedFiles
{
    /// <summary>
    /// Single <see cref="SimComponent"/> file management class.
    /// </summary>
    public class ManagedComponentFile : ManagedFile
    {
        /// <summary>
        /// The managed file holding the public parts of the components managed by this file.
        /// </summary>
        public ManagedPublicComponentFile PublicCounterpart
        {
            get { return this.public_counterpart; }
            internal set
            {
                this.public_counterpart = value;
                // TODO: here comes the synchronization btw public and all (if the loaded project is not in a read-only state)
            }
        }
        private ManagedPublicComponentFile public_counterpart;

        /// <summary>
        /// Initializes a ManagedComponentFile.
        /// </summary>
        /// <param name="projectData">The project's data</param>
        /// <param name="_owner">the managing collection</param>
        /// <param name="_file">the file as it is in the file system</param>
        public ManagedComponentFile(ExtendedProjectData projectData, ManagedFileCollection _owner, FileInfo _file)
            : base(projectData, _owner, _file)
        { }

        /// <summary>
        /// Creates a deep copy of the original managed file. Caution: both files reference THE SAME DATA MANAGER!
        /// </summary>
        /// <param name="_original">the original managed file</param>
        /// <param name="_new_file_location">the location to which the actual file is to be copied</param>
        internal ManagedComponentFile(ManagedComponentFile _original, FileInfo _new_file_location)
            : base(_original, _new_file_location)
        { }

        /// <inheritdoc/>
        public override void Save()
        {
            // general save
            ProjectIO.SaveComponentFile(this.File, ProjectData);
            this.OnFileUpToDateChanged(true);
            // public save
            if (this.PublicCounterpart != null && System.IO.File.Exists(this.PublicCounterpart.File.FullName))
            {
                ProjectIO.SavePublicComponentFile(this.PublicCounterpart.File, ProjectData);
                this.PublicCounterpart.OnFileUpToDateChanged(true);
            }
        }

        /// <inheritdoc/>
        public override void Open(bool _clear_before_open)
        {
            if (_clear_before_open)
            {
                ProjectData.Components.Clear();
                ProjectData.NetworkManager.ClearRecord();
                ProjectData.AssetManager.Reset();
            }

            ProjectIO.OpenComponentFile(this.File, ProjectData,
                this.owner.Project.GlobalID, this.owner.Project.ImportLogFile, this.owner.Project);
            ProjectData.AssetManager.WorkingDirectory = this.File.DirectoryName;
            // this is caused by the factory receiving its location only after the loading of all components
        }
    }
}
