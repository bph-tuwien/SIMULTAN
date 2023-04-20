using SIMULTAN.Data.Taxonomy;
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
    /// A management class for a single file containing <see cref="SimTaxonomy"/> instance.
    /// </summary>
    public class ManagedTaxonomyFile : ManagedFile
    {
        /// <summary>
        /// Stores the version of the last loaded file. Might be used to handle restore operations
        /// specific for a version.
        /// </summary>
        public ulong LoadedFileVersion { get; private set; }

        /// <inheritdoc/>
        public ManagedTaxonomyFile(ManagedFile _original, FileInfo _new_file_location) : base(_original, _new_file_location)
        {
        }

        /// <inheritdoc/>
        public ManagedTaxonomyFile(ExtendedProjectData projectData, ManagedFileCollection _owner, FileInfo _file) : base(projectData, _owner, _file)
        {
        }

        /// <inheritdoc/>
        public override void Open(bool _clear_before_open)
        {
            LoadedFileVersion = ProjectIO.OpenTaxonomyFile(File, ProjectData);
        }

        /// <inheritdoc/>
        public override void Save()
        {
            ProjectIO.SaveTaxonomyFile(File, ProjectData);
            this.OnFileUpToDateChanged(true);
        }

    }
}
