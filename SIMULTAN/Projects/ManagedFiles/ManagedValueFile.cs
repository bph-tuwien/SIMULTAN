using SIMULTAN.Data.MultiValues;
using SIMULTAN.Serializer.MVDXF;
using SIMULTAN.Serializer.Projects;
using System;
using System.IO;

namespace SIMULTAN.Projects.ManagedFiles
{
    /// <summary>
    /// A management class for a single file containing <see cref="SimMultiValue"/> instances.
    /// </summary>
    public class ManagedValueFile : ManagedFile
    {
        /// <summary>
        /// The managed file holding the public parts of the values managed by this file.
        /// </summary>
        public ManagedPublicValueFile PublicCounterpart
        {
            get { return this.public_counterpart; }
            internal set
            {
                this.public_counterpart = value;
                // TODO: here comes the synchronization between public and all (if the loaded project is not in a read-only state)
            }
        }
        private ManagedPublicValueFile public_counterpart;

        /// <summary>
        /// The managed file whose content depends on the <see cref="PublicCounterpart"/>.
        /// </summary>
        public ManagedPublicComponentFile PublicDependence { get; internal set; }

        /// <summary>
        /// Initializes a ManagedValueFile.
        /// </summary>
        /// <param name="projectData">The project's data</param>
        /// <param name="_owner">the managing collection</param>
        /// <param name="_file">the file as it is in the file system</param>
        public ManagedValueFile(ExtendedProjectData projectData, ManagedFileCollection _owner, FileInfo _file)
            : base(projectData, _owner, _file)
        { }

        /// <summary>
        /// Creates a deep copy of the original managed file. Caution: both files reference THE SAME DATA MANAGER!
        /// </summary>
        /// <param name="_original">the original managed file</param>
        /// <param name="_new_file_location">the location to which the actual file is to be copied</param>
        internal ManagedValueFile(ManagedValueFile _original, FileInfo _new_file_location)
            : base(_original, _new_file_location)
        { }

        /// <inheritdoc/>
        public override void Save()
        {
            // general save
            MultiValueDxfIO.Write(this.File, ProjectData.ValueManager);
            this.File.LastWriteTime = DateTime.Now;

            this.OnFileUpToDateChanged(true);
            // public save
            if (this.PublicCounterpart != null && System.IO.File.Exists(this.PublicCounterpart.File.FullName))
            {
                ProjectIO.SavePublicMultiValueFile(this.PublicCounterpart.File, ProjectData);
                this.PublicCounterpart.OnFileUpToDateChanged(true);
            }
        }

        /// <inheritdoc/>
        public override void Open(bool _clear_before_open)
        {
            if (_clear_before_open)
            {
                this.ProjectData.ValueManager.Clear();
                this.ProjectData.ValueManager.SetCallingLocation(null);
            }

            ProjectIO.OpenMultiValueFile(this.File, ProjectData, this.owner.Project.GlobalID,
                this.owner.Project);
        }
    }
}
