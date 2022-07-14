using SIMULTAN.Projects.ManagedFiles;
using SIMULTAN.Serializer.DXF;
using SIMULTAN.Utils;
using System;
using System.Collections.Generic;
using System.IO;

namespace SIMULTAN.Projects
{
    /// <summary>
    /// Handles the compact (i.e., not distributed) saving of a project. All partial files, such as 
    /// the component, the values, the excel tool definitions files, the internal resources, 
    /// are saved in a Zip archive, managed by this class.
    /// </summary>
    public class CompactProject : HierarchicalProject
    {
        #region PROPERTIES

        /// <summary>
		/// The file holding the project (a Zip Archive file).
		/// </summary>
		public FileInfo ProjectFile { get; }

        /// <inheritdoc/>
        public override string Name => this.ProjectFile.Name;

        #endregion

        #region .CTOR

        /// <summary>
        /// Instantiates a compact project from separate files, including not managed and associated files.
        /// </summary>
        /// <param name="id">The id of the project</param>
        /// <param name="_project_file">the file holding the project</param>
        /// <param name="_all_managers">a container holding all data managers used in the project</param>
        /// <param name="_files"></param>
        /// <param name="_non_managed_files"></param>
        /// <param name="_contained_dirs"></param>
        /// <param name="_associated_files"></param>
        /// <param name="_unpack_folder">the folder for unpacking the project's contents</param>
        public CompactProject(Guid id, FileInfo _project_file, ExtendedProjectData _all_managers, ManagedFileCollection _files,
            IEnumerable<FileInfo> _non_managed_files, IEnumerable<DirectoryInfo> _contained_dirs, IEnumerable<FileInfo> _associated_files,
            DirectoryInfo _unpack_folder)
            : base(id, _files, _all_managers, _unpack_folder, _non_managed_files, _contained_dirs, _associated_files)
        {
            this.ProjectFile = _project_file;
        }

        #endregion

        #region BACKUP

        /// <summary>
        /// Creates a backup on each save.
        /// </summary>
        public void UpdateBackupFile()
        {
            int index = this.ProjectFile.FullName.LastIndexOf(ParamStructFileExtensions.FILE_EXT_PROJECT_COMPACT);

            string path = this.ProjectFile.FullName.Substring(0, index) + ParamStructFileExtensions.FILE_EXT_PROJECT_COMPACT_BACKUP;
            File.Delete(path);
            File.Copy(this.ProjectFile.FullName, path, false);
        }

        #endregion
    }
}
