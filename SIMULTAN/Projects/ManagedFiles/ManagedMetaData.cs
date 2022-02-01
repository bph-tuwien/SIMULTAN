using SIMULTAN.Serializer.Projects;
using SIMULTAN.Utils.Files;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Projects.ManagedFiles
{
    /// <summary>
    /// The meta-data management file for a project.
    /// </summary>
    public class ManagedMetaData : ManagedFile
    {
        /// <summary>
        /// The data of the project to which this meta data belongs to
        /// </summary>
        public HierarchicProjectMetaData Data { get; }

        /// <summary>
        /// Initializes a ManagedMetaData. No copy .ctor present.
        /// </summary>
        /// <param name="projectData">The project's data</param>
        /// <param name="_owner">the managing collection</param>
        /// <param name="_file">the file as it is in the file system</param>
        public ManagedMetaData(ExtendedProjectData projectData, ManagedFileCollection _owner, FileInfo _file)
            : base(projectData, _owner, _file)
        {
            Data = ProjectIO.OpenMetaDataFile(_file);
        }

        /// <inheritdoc/>
        public override void Save()
        {
            var unpackFolder = owner.Project.ProjectUnpackFolder.FullName;

            var metaData = new HierarchicProjectMetaData(owner.Project.GlobalID,
                owner.Project.Children.ToDictionary(x => x.GlobalID, x => FileSystemNavigation.GetRelativePath(unpackFolder, x.ProjectUnpackFolder.FullName))
                );
            ProjectIO.SaveMetaDataFile(this.File, metaData);
            this.OnFileUpToDateChanged(true);
        }

        /// <inheritdoc/>
        public override void Open(bool _clear_before_open)
        {
        }

        /// <summary>
        /// Removes the meta-information when the project is being closed. 
        /// </summary>
        public void Close()
        {
            this.Reset();
        }
    }
}
