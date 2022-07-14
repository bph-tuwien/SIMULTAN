using SIMULTAN.Data;
using SIMULTAN.Data.Assets;
using SIMULTAN.Data.Components;
using SIMULTAN.Data.MultiValues;
using SIMULTAN.Data.SitePlanner;
using SIMULTAN.Data.Users;
using SIMULTAN.Projects;
using SIMULTAN.Projects.ManagedFiles;
using SIMULTAN.Serializer.CODXF;
using SIMULTAN.Serializer.DXF;
using SIMULTAN.Serializer.GMDXF;
using SIMULTAN.Serializer.METADXF;
using SIMULTAN.Serializer.MVDXF;
using SIMULTAN.Serializer.PADXF;
using SIMULTAN.Serializer.PPATH;
using SIMULTAN.Serializer.SimGeo;
using SIMULTAN.Serializer.SIMLINKS;
using SIMULTAN.Serializer.SIMUSER;
using SIMULTAN.Serializer.SPDXF;
using SIMULTAN.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace SIMULTAN.Serializer.Projects
{
    /// <summary>
    /// Provides saving and opening functionality for the project contents.
    /// </summary>
    public static class ProjectIO
    {
        #region META-DATA

        internal static HierarchicProjectMetaData OpenMetaDataFile(FileInfo _file)
        {
            if (_file == null || !File.Exists(_file.FullName))
                return null;

            DXFParserInfo info = new DXFParserInfo(Guid.Empty, null); //Does not exist at this point

            return MetaDxfIO.Read(_file, info);
        }

        internal static void SaveMetaDataFile(FileInfo _file, HierarchicProjectMetaData _meta_data)
        {
            MetaDxfIO.Write(_file, _meta_data);
        }

        #endregion

        #region VALUES

        internal static void OpenMultiValueFile(FileInfo _file, ExtendedProjectData _projectData,
                                                Guid _calling_global_id, FileInfo logFile, IReferenceLocation _calling_reference = default)
        {
            if (_file == null || !File.Exists(_file.FullName))
                return;

            if (_projectData.ValueManager.CalledFromLocation == null)
            {
                if (_calling_reference != default && _calling_reference.GlobalID == _calling_global_id)
                    _projectData.ValueManager.SetCallingLocation(_calling_reference);
                else
                    _projectData.ValueManager.SetCallingLocation(new DummyReferenceLocation(_calling_global_id));
            }

            DXFParserInfo parserInfo = new DXFParserInfo(_calling_global_id, _projectData);
            MultiValueDxfIO.Read(_file, parserInfo);
        }

        internal static void SaveMultiValueFile(FileInfo _file, SimMultiValueCollection _value_factory)
        {
            MultiValueDxfIO.Write(_file, _value_factory);
            _file.LastWriteTime = DateTime.Now;
        }

        internal static void SavePublicMultiValueFile(FileInfo _file, ProjectData projectData)
        {
            HashSet<SimMultiValue> exportedMVs = new HashSet<SimMultiValue>();

            ComponentWalker.ForeachComponent(projectData.Components, x =>
            {
                if (x.Visibility == SimComponentVisibility.AlwaysVisible)
                {
                    foreach (var param in x.Parameters)
                    {
                        if (param.MultiValuePointer != null && !exportedMVs.Contains(param.MultiValuePointer.ValueField))
                        {
                            exportedMVs.Add(param.MultiValuePointer.ValueField);
                        }
                    }
                }
            });

            MultiValueDxfIO.Write(_file, exportedMVs);
            _file.LastWriteTime = DateTime.Now;
        }

        internal static void OpenMultiValueFileAlone(HierarchicalProject _caller, FileInfo _file, ExtendedProjectData targetData)
        {
            targetData.SetCallingLocation(_caller);
            DXFParserInfo info = new DXFParserInfo(_caller.GlobalID, targetData);
            MultiValueDxfIO.Read(_file, info);
        }

        #endregion

        #region COMPONENTS

        internal static void OpenComponentFile(FileInfo _file, ExtendedProjectData projectData, Guid _calling_global_id,
                                               FileInfo logFile, IReferenceLocation _calling_reference = default)
        {
            if (_file == null)
                throw new ArgumentNullException(string.Format("{0} may not be null", nameof(_file)));
            if (projectData == null)
                throw new ArgumentNullException(string.Format("{0} may not be null", nameof(projectData)));

            if (!File.Exists(_file.FullName))
                throw new ArgumentException(string.Format("File {0} does not exist", _file.FullName));

            //imports the component file and restore consistent state
            if (projectData.NetworkManager.CalledFromLocation == null)
            {
                if (_calling_reference != default && _calling_reference.GlobalID == _calling_global_id)
                    projectData.NetworkManager.SetCallingLocation(_calling_reference);
                else
                    projectData.NetworkManager.SetCallingLocation(new DummyReferenceLocation(_calling_global_id));
            }

            ComponentDxfIO.Read(_file, new DXFParserInfo(projectData.Owner.GlobalID, projectData));
        }

        internal static void SaveComponentFile(FileInfo _file, ProjectData projectData)
        {
            // create the export string
            /*StringBuilder export = ComponentFactorySerialization.ExportRecord(projectData.NetworkManager, projectData.Components,
                projectData.AssetManager, projectData.UserComponentLists, true);
            string content = export.ToString();
            using (FileStream fs = File.Create(_file.FullName))
            {
                byte[] content_B = Encoding.UTF8.GetBytes(content);
                fs.Write(content_B, 0, content_B.Length);
            }
            _file.LastWriteTime = DateTime.Now;*/
        }

        internal static void SavePublicComponentFile(FileInfo _file, ProjectData projectData)
        {
            // create the export string and save the component file
            //(StringBuilder export, List<string> public_paths) = ComponentFactorySerialization.ExportPublic(projectData.NetworkManager,
            //    projectData.Components,
            //    projectData.AssetManager, true);
            //string content = export.ToString();
            //using (FileStream fs = File.Create(_file.FullName))
            //{
            //    byte[] content_B = System.Text.Encoding.UTF8.GetBytes(content);
            //    fs.Write(content_B, 0, content_B.Length);
            //}
            ComponentDxfIO.WritePublic(_file, projectData);
            _file.LastWriteTime = DateTime.Now;


            var file_paths_name = new FileInfo(_file.FullName.Substring(0, _file.FullName.Length - ParamStructFileExtensions.FILE_EXT_COMPONENTS_PUBLIC.Length) +
                                     ParamStructFileExtensions.PUBLIC_PROJECT_PATHS_SUFFIX +
                                     ParamStructFileExtensions.FILE_EXT_PUBLIC_PROJECT_PATHS);
            PPathIO.Write(file_paths_name, projectData);

            // save a separate text file containing the paths to the public resources that need to be unpacked during loading
            //string path_content = string.Empty;
            //if (public_paths.Count == 1)
            //    path_content += public_paths[0];
            //else if (public_paths.Count >= 2)
            //    path_content = public_paths.Aggregate((x, y) => x + Environment.NewLine + y);
            //string file_paths_name = _file.FullName.Substring(0, _file.FullName.Length - ParamStructFileExtensions.FILE_EXT_COMPONENTS_PUBLIC.Length) +
            //                         ParamStructFileExtensions.PUBLIC_PROJECT_PATHS_SUFFIX +
            //                         ParamStructFileExtensions.FILE_EXT_PUBLIC_PROJECT_PATHS;
            //using (FileStream fs = File.Create(file_paths_name))
            //{
            //    byte[] content_P = System.Text.Encoding.UTF8.GetBytes(path_content);
            //    fs.Write(content_P, 0, content_P.Length);
            //}
        }

        #endregion

        #region EXCEL TOOLS

        internal static void OpenExcelToolCollectionFile(FileInfo _file, ExtendedProjectData projectData)
        {
            if (projectData == null)
                throw new ArgumentNullException(string.Format("{0} may not be null", nameof(projectData)));
            if (_file == null)
                throw new ArgumentNullException(string.Format("{0} may not be null", nameof(_file)));

            if (!File.Exists(_file.FullName))
                throw new ArgumentException(string.Format("File {0} does not exist", _file.FullName));


            projectData.ExcelToolMappingManager.ClearRecord();
            var info = new DXFParserInfo(projectData.Owner.GlobalID, projectData)
            {
                //FileVersion = ComponentDxfIO.LastParsedFileVersion
                FileVersion = Math.Min(11, ComponentDxfIO.LastParsedFileVersion) //This is needed because version sections are only present starting from version 12
            };
            ExcelDxfIO.Read(_file, info);
        }

        internal static void SaveExcelToolCollectionFile(FileInfo _file, ProjectData projectData)
        {
            ExcelDxfIO.Write(_file, projectData);
            _file.LastWriteTime = DateTime.Now;
        }

        #endregion

        #region PARAMETERS

        internal static void OpenParameterLibraryFile(FileInfo _file, ExtendedProjectData projectData)
        {
            if (_file == null || !File.Exists(_file.FullName))
                return;

            //imports the DXF file
            DXFParserInfo info = new DXFParserInfo(projectData.Owner.GlobalID, projectData);
            ParameterDxfIO.Read(_file, info);
        }

        internal static void SaveParameterLibraryFile(FileInfo _file, ExtendedProjectData projectData)
        {
            ParameterDxfIO.Write(_file, projectData);
            _file.LastWriteTime = DateTime.Now;
        }

        #endregion

        #region GEOMETRY

        internal static bool SaveGeometryFile(ManagedGeometryFile managedFile)
        {
            var resource = managedFile.ProjectData.AssetManager.GetResource(managedFile.File);

            var exists = managedFile.ProjectData.GeometryModels.TryGetGeometryModel(resource, out var rootModel, false);
            if (exists && rootModel.Geometry.HandleConsistency)
            {
                if (!SimGeoIO.Save(rootModel, resource, SimGeoIO.WriteMode.Plaintext))
                    return false;
            }
            return true;
        }

        #endregion

        #region USERS

        // TODO... solve better (otherwise key is in memory until the application closes!)
        internal static byte[] ENCR_KEY = new byte[0];

        internal static void OpenUserFile(FileInfo _file, ExtendedProjectData _projectData)
        {
            if (_file == null)
                throw new ArgumentNullException(string.Format("{0} may not be null", nameof(_file)));
            if (_projectData == null)
                throw new ArgumentNullException(string.Format("{0} may not be null", nameof(_projectData)));

            if (!File.Exists(_file.FullName))
                throw new ArgumentException(string.Format("File {0} does not exist", _file.FullName));

            var userManager = _projectData.UsersManager;

            var users = SimUserDxfIO.Read(_file, ProjectIO.ENCR_KEY, new DXFParserInfo(_projectData.Project.GlobalID, _projectData));

            if (users.Any(x => x.EncryptedEncryptionKey == null)) //For legacy projects where no key was present -> reset passwords of ALL users and create key
            {
                //Generate encryption key for project
                byte[] key = new byte[32];
                RandomNumberGenerator.Create().GetBytes(key);
                var passwd = Encoding.UTF8.GetBytes("guest");

                foreach (var u in users)
                {
                    u.PasswordHash = SimUsersManager.HashPassword(passwd);
                    u.EncryptedEncryptionKey = SimUsersManager.EncryptEncryptionKey(key, passwd);
                }
            }

            foreach (SimUser u in users)
            {
                userManager.Users.Add(u);
            }

            if (userManager.Users.Count == 0)
            {
                //Generate encryption key for project
                byte[] key = new byte[32];
                RandomNumberGenerator.Create().GetBytes(key);
                var passwd = Encoding.UTF8.GetBytes("admin");

                userManager.Users.Add(
                    new SimUser(Guid.NewGuid(), "Admin",
                    SimUsersManager.HashPassword(passwd),
                    SimUsersManager.EncryptEncryptionKey(key, passwd),
                    SimUserRole.ADMINISTRATOR)
                    );
            }
        }

        internal static void SaveUserFile(FileInfo _file, SimUsersManager _user_manager, byte[] _encryption_key)
        {
            // copied from SaveProjectVM
            ProjectIO.ENCR_KEY = _encryption_key;
            SimUserDxfIO.Write(_user_manager.Users, _file, ProjectIO.ENCR_KEY);

            _file.LastWriteTime = DateTime.Now;
        }

        #endregion

        #region LINKS FILE

        internal static void OpenLinksFile(FileInfo _file, Guid _calling_global_id, ExtendedProjectData projectData)
        {
            if (_file == null)
                throw new ArgumentNullException(string.Format("{0} may not be null", nameof(_file)));
            if (projectData == null)
                throw new ArgumentNullException(string.Format("{0} may not be null", nameof(projectData)));

            if (!File.Exists(_file.FullName))
                throw new ArgumentException(string.Format("File {0} does not exist", _file.FullName));

            DXFParserInfo info = new DXFParserInfo(_calling_global_id, projectData);
            var links = SimLinksDxfIO.Read(_file, projectData.MultiLinkManager.UserEncryptionUtiliy.EncryptionKey, info);
            foreach (var link in links)
            {
                projectData.MultiLinkManager.Links.Add(link);
            }
        }

        internal static void SaveLinksFile(FileInfo _file, ExtendedProjectData projectData)
        {
            FileCanBeOverwritten(_file); // throws exceptions, if there are time stamp inconsistencies

            if (projectData.MultiLinkManager.UserEncryptionUtiliy == null)
                throw new Exception("Encryption key utility cannot be found!");

            SimLinksDxfIO.Write(projectData.MultiLinkManager.Links, _file, projectData.MultiLinkManager.UserEncryptionUtiliy.EncryptionKey);

            _file.LastWriteTime = DateTime.Now;
        }

        #endregion

        #region SITEPLANNER

        internal static void OpenGeoMapFile(FileInfo file, ResourceFileEntry fileResource, ExtendedProjectData projectData)
        {
            if (file == null)
                throw new ArgumentNullException(nameof(file));
            if (!File.Exists(file.FullName))
                throw new ArgumentException("file must exist");
            if (fileResource == null)
                throw new ArgumentNullException(nameof(fileResource));
            if (projectData == null)
                throw new ArgumentNullException(nameof(projectData));

            var parseInfo = new DXFParserInfo(projectData.Project.GlobalID, projectData) { CurrentFile = file};
            GeoMapDxfIO.Read(file, parseInfo);
        }

        internal static void SaveGeoMapFile(FileInfo _file, SitePlannerManager _manager, ProjectData _projectData)
        {
            GeoMapDxfIO.Write(_file, _manager.GetGeoMapByFile(_file), _projectData);

            _file.LastWriteTime = DateTime.Now;
        }

        internal static void OpenSitePlannerFile(ResourceFileEntry fileResource, ExtendedProjectData projectData)
        {
            if (fileResource == null || !File.Exists(fileResource.CurrentFullPath))
                return;

            var file = new FileInfo(fileResource.CurrentFullPath);
            var parseInfo = new DXFParserInfo(projectData.Owner.GlobalID, projectData) { CurrentFile = file};
            SiteplannerDxfIO.Read(file, parseInfo);
        }

        internal static void SaveSitePlannerFile(FileInfo _file, SitePlannerManager _manager, ProjectData _projectData)
        {
            SiteplannerDxfIO.Write(_file, _manager.GetSitePlannerProjectByFile(_file), _projectData);

            _file.LastWriteTime = DateTime.Now;
        }

        #endregion

        #region PROJECTS: create from files

        /// <summary>
        /// Creates a compact project from separate component, values, etc. files.
        /// </summary>
        /// <param name="_project_file">the target file of saving the project</param>
        /// <param name="_files_to_convert_to_project">the files to be included in the project and managed by it</param>
        /// <param name="_non_managed_files">the files included but not managed by the project (e.g. PDF)</param>
        /// <param name="_associated_files">the files linked in the project but not associated with it</param>
        /// <param name="_project_data_manager">the manager of all relevant data</param>
        /// <param name="_encryption_key">the key for encrypting the user file</param>
        /// <returns>The created project</returns>
        /// <exception cref="CreateProjectException">Thrown when one of the required files is missing</exception>
        public static CompactProject CreateFromSeparateFiles(FileInfo _project_file, IEnumerable<FileInfo> _files_to_convert_to_project,
            IEnumerable<FileInfo> _non_managed_files, IEnumerable<FileInfo> _associated_files,
            ExtendedProjectData _project_data_manager, byte[] _encryption_key)
        {
            if (_files_to_convert_to_project == null)
                throw new ArgumentNullException(nameof(_files_to_convert_to_project));

            // 0. check viability of project
            FileInfo values_file = _files_to_convert_to_project.FirstOrDefault(x => string.Equals(x.Extension, ParamStructFileExtensions.FILE_EXT_MULTIVALUES, StringComparison.InvariantCultureIgnoreCase));
            if (values_file == null)
            {
                throw new CreateProjectException(CreateProjectExceptionReason.MissingMultiValuesFile);
            }
            FileInfo comps_file = _files_to_convert_to_project.FirstOrDefault(x => string.Equals(x.Extension, ParamStructFileExtensions.FILE_EXT_COMPONENTS, StringComparison.InvariantCultureIgnoreCase));
            if (comps_file == null)
            {
                throw new CreateProjectException(CreateProjectExceptionReason.MissingComponentFile);
            }

            DirectoryInfo unpacking_dir = CreateUnpackingDirectory(_project_file);
            List<FileInfo> project_content_files = new List<FileInfo>();

            // 1a. SAVE the project metadata
            HierarchicProjectMetaData metadata = new HierarchicProjectMetaData();

            string file_path_meta = Path.Combine(unpacking_dir.FullName, "MetaData" + ParamStructFileExtensions.FILE_EXT_META);
            FileInfo file_meta = new FileInfo(file_path_meta);
            ProjectIO.SaveMetaDataFile(file_meta, metadata);
            project_content_files.Add(file_meta);

            // 1b. SAVE the user file
            SimUsersManager users = _project_data_manager.UsersManager ?? new SimUsersManager();
            if (users.Users.Count == 0)
            {
                SimUser newUser = SimUser.DefaultUser;
                users.Users.Add(newUser);

                (var user, var userkey) = users.Authenticate(newUser.Name, SimUser.DefaultUserPassword);
                users.EncryptionKey = userkey;
                users.CurrentUser = user;
            }

            string file_path_users = Path.Combine(unpacking_dir.FullName, "Users" + ParamStructFileExtensions.FILE_EXT_USERS);
            FileInfo file_users = new FileInfo(file_path_users);
            ProjectIO.SaveUserFile(file_users, users, _encryption_key);
            project_content_files.Add(file_users);

            // 1c. SAVE the multi-link file
            MultiLinkManager links = _project_data_manager.MultiLinkManager ?? new MultiLinkManager();
            links.UserEncryptionUtiliy = users;

            string file_path_links = Path.Combine(unpacking_dir.FullName, "Links" + ParamStructFileExtensions.FILE_EXT_LINKS);
            FileInfo file_links = new FileInfo(file_path_links);
            ProjectIO.SaveLinksFile(file_links, _project_data_manager);
            project_content_files.Add(file_links);

            // 1d. SAVE the public values file
            string file_path_public_values = Path.Combine(unpacking_dir.FullName, "PublicValueRecord" + ParamStructFileExtensions.FILE_EXT_MULTIVALUES_PUBLIC);
            FileInfo file_public_values = new FileInfo(file_path_public_values);
            ProjectIO.SavePublicMultiValueFile(file_public_values, _project_data_manager);
            project_content_files.Add(file_public_values);

            // 1e. SAVE the public component file
            string file_path_public_comps = Path.Combine(unpacking_dir.FullName, "PublicComponentRecord" + ParamStructFileExtensions.FILE_EXT_COMPONENTS_PUBLIC);
            FileInfo file_public_comps = new FileInfo(file_path_public_comps);
            ProjectIO.SavePublicComponentFile(file_public_comps, _project_data_manager);
            project_content_files.Add(file_public_comps);

            // 2. copy the other files to the project's directory            
            foreach (FileInfo existing_file in _files_to_convert_to_project)
            {
                FileInfo target = CreateUniqueFileCopyPath(unpacking_dir, existing_file);
                File.Copy(existing_file.FullName, target.FullName, false);
                project_content_files.Add(target);
            }

            // 3. copy the non-managed resource files to the project's directory
            List<FileInfo> non_managed_files = new List<FileInfo>();
            foreach (FileInfo nm_file in _non_managed_files)
            {
                FileInfo target = CreateUniqueFileCopyPath(unpacking_dir, nm_file);
                File.Copy(nm_file.FullName, target.FullName, false);
                non_managed_files.Add(target);
            }

            // 4. DO NOT COPY the associated files (pdf, xml, docx, etc.) to the project's directory!

            // 5. create the project
            ManagedFileCollection managed_files = new ManagedFileCollection(project_content_files.ToList(), _project_data_manager);
            var metaDataFile = managed_files.Files.FirstOrDefault(x => x is ManagedMetaData);
            HierarchicProjectMetaData metaData = ProjectIO.OpenMetaDataFile(metaDataFile.File);

            CompactProject project = new CompactProject(metaData.ProjectId, _project_file, _project_data_manager, managed_files,
                non_managed_files, new List<DirectoryInfo>(),
                _associated_files, unpacking_dir);

            return project;
        }

        /// <summary>
        /// Creates an (almost) empty project containing some example data.
        /// </summary>
        /// <param name="_project_file">the target file of saving the project</param>
        /// <param name="_path_to_local_tmp_folder">the directory for saving temporary files</param>
        /// <param name="_project_data_manager">the manager of all relevant data</param>
        /// <param name="_encryption_key">The key for encrypting the user file</param>
        /// <param name="serviceProvider">The service provider</param>
        /// <returns>the created project and feedback, if there was a problem</returns>
        public static CompactProject CreateMinimalProject(FileInfo _project_file, string _path_to_local_tmp_folder,
            ExtendedProjectData _project_data_manager, byte[] _encryption_key,
            IServicesProvider serviceProvider)
        {
            // create the minimally required files in a temporary folder

            string file_path_public_values = Path.Combine(_path_to_local_tmp_folder, "ValueRecord" + ParamStructFileExtensions.FILE_EXT_MULTIVALUES);
            FileInfo file_values = new FileInfo(file_path_public_values);
            File.Create(file_values.FullName).Dispose();
            file_values.LastWriteTime = DateTime.Now;

            string file_path_public_comps = Path.Combine(_path_to_local_tmp_folder, "ComponentRecord" + ParamStructFileExtensions.FILE_EXT_COMPONENTS);
            FileInfo file_comps = new FileInfo(file_path_public_comps);
            File.Create(file_comps.FullName).Dispose();
            file_comps.LastWriteTime = DateTime.Now;

            IEnumerable<FileInfo> files_to_convert_to_project = new List<FileInfo> { file_values, file_comps };

            // load the data in the project manager
            OpenMultiValueFile(file_values, _project_data_manager, Guid.Empty, null);
            OpenComponentFile(file_comps, _project_data_manager, Guid.Empty, null);

            var created = ProjectIO.CreateFromSeparateFiles(_project_file, files_to_convert_to_project,
                new List<FileInfo>(), new List<FileInfo>(), _project_data_manager, _encryption_key);

            // delete the files from the temporary folder
            file_values.Delete();
            file_comps.Delete();

            return created;
        }

        /// <summary>
        /// Creates the multi-link file for the project.
        /// </summary>
        /// <param name="_project">the project, for which to create the file</param>
        public static void CreateMissingLinkFile(HierarchicalProject _project)
        {
            if (_project == null)
                throw new ArgumentNullException(nameof(_project));

            MultiLinkManager links = _project.AllProjectDataManagers.MultiLinkManager;

            // update the users file
            links.UserEncryptionUtiliy = _project.AllProjectDataManagers.UsersManager;
            if (links.Links.Count == 0)
                links.GetLinksFromAssetManager();
            string file_path_links = Path.Combine(_project.ProjectUnpackFolder.FullName, "Links" + ParamStructFileExtensions.FILE_EXT_LINKS);
            FileInfo file_links = new FileInfo(file_path_links);
            ProjectIO.SaveLinksFile(file_links, _project.AllProjectDataManagers);
            _project.ManagedFiles.AddFile(file_links, _project.AllProjectDataManagers);
        }

        #endregion

        #region PROJECTS: create from master file (transition from old UI)

        /// <summary>
        /// Extracts all information from a master file in order to create a project.
        /// </summary>
        /// <param name="_path_master_file">the path to the master file</param>
        /// <param name="_paths">the paths to the relevant partial files (e.g., components and values)</param>
        /// <returns>lists of files to include in the project</returns>
        public static (List<FileInfo> managed, List<FileInfo> non_managed) ExtractFromMasterFile(string _path_master_file, params string[] _paths)
        {
            // --------------------------------- 0. check input -------------------------------------
            if (string.IsNullOrEmpty(_path_master_file))
                throw new ArgumentException("The path to the master file is not valid!");
            if (!File.Exists(_path_master_file))
                throw new ArgumentException("The path to the master file is not valid!");

            DirectoryInfo master_dir = new FileInfo(_path_master_file).Directory;

            if (_paths == null)
                throw new ArgumentNullException(nameof(_paths));
            if (_paths.Length < 2)
                throw new ArgumentException("At least the path to the values and to the components necessary!");

            // ---------------------------- 1. find and the main files ------------------------------
            FileInfo file_values = null;
            FileInfo file_components = null;
            foreach (string p in _paths)
            {
                if (string.IsNullOrEmpty(p)) continue;

                FileInfo pfile = new FileInfo(p);
                if (string.Equals(pfile.Extension, ParamStructFileExtensions.FILE_EXT_MULTIVALUES, StringComparison.InvariantCultureIgnoreCase))
                {
                    if (File.Exists(p))
                        file_values = new FileInfo(p);
                    else
                        throw new ArgumentException("The values file is not valid!");
                }
                else if (string.Equals(pfile.Extension, ParamStructFileExtensions.FILE_EXT_COMPONENTS, StringComparison.InvariantCultureIgnoreCase))
                {
                    if (File.Exists(p))
                        file_components = new FileInfo(p);
                    else
                        throw new ArgumentException("The component file is not valid!");
                }
            }

            if (file_values == null || file_components == null)
                throw new ArgumentException("Could not find the required minimum of files to create a project!");

            // ------------------------ 2. find all other relevant files ----------------------------
            // ... from the resources
            List<FileInfo> managed = new List<FileInfo>();
            List<FileInfo> non_managed = new List<FileInfo>();

            // ... guess the rest from the master file folder            
            List<FileInfo> master_dir_content = ZipProjectIO.GetFilesRecursive(master_dir);
            foreach (FileInfo entry in master_dir_content)
            {
                // exclude files from the temporary project dir (if it is in the master folder)
                if (entry.FullName.Contains("~"))
                    continue;

                if (string.Equals(entry.Extension, ParamStructFileExtensions.FILE_EXT_COMPONENTS, StringComparison.InvariantCultureIgnoreCase) ||
                    string.Equals(entry.Extension, ParamStructFileExtensions.FILE_EXT_MULTIVALUES, StringComparison.InvariantCultureIgnoreCase) ||
                    string.Equals(entry.Extension, ParamStructFileExtensions.FILE_EXT_EXCEL_TOOL_COLLECTION, StringComparison.InvariantCultureIgnoreCase))
                {
                    // regular managed files : CODXF, MVDXF, ETDXF
                    managed.Add(entry);
                }
                else if (string.Equals(entry.Extension, ParamStructFileExtensions.FILE_EXT_COMPONENTS_PUBLIC, StringComparison.InvariantCultureIgnoreCase) ||
                         string.Equals(entry.Extension, ParamStructFileExtensions.FILE_EXT_MULTIVALUES_PUBLIC, StringComparison.InvariantCultureIgnoreCase) ||
                         string.Equals(entry.Extension, ParamStructFileExtensions.FILE_EXT_USERS, StringComparison.InvariantCultureIgnoreCase) ||
                         string.Equals(entry.Extension, ParamStructFileExtensions.FILE_EXT_LINKS, StringComparison.InvariantCultureIgnoreCase) ||
                         string.Equals(entry.Extension, ParamStructFileExtensions.FILE_EXT_META, StringComparison.InvariantCultureIgnoreCase) ||
                         string.Equals(entry.Extension, ParamStructFileExtensions.FILE_EXT_MASTER, StringComparison.InvariantCultureIgnoreCase) ||
                         string.Equals(entry.Extension, ParamStructFileExtensions.FILE_EXT_PROJECT, StringComparison.InvariantCultureIgnoreCase) ||
                         string.Equals(entry.Extension, ParamStructFileExtensions.FILE_EXT_PROJECT_COMPACT, StringComparison.InvariantCultureIgnoreCase))
                {
                    // ignore automatically generated managed files: CODXFP, MVDXFP, USRDXF, SIMLINKS, METADXF, master, smn, simultan
                    continue;
                }
                else if (string.Equals(entry.Extension, ParamStructFileExtensions.FILE_EXT_GEOMETRY_INTERNAL, StringComparison.InvariantCultureIgnoreCase) ||
                         string.Equals(entry.Extension, ParamStructFileExtensions.FILE_EXT_PARAMETERS, StringComparison.InvariantCultureIgnoreCase) ||
                         string.Equals(entry.Extension, ParamStructFileExtensions.FILE_EXT_IMAGES, StringComparison.InvariantCultureIgnoreCase) ||
                         string.Equals(entry.Extension, ParamStructFileExtensions.FILE_EXT_GEOMAP, StringComparison.InvariantCultureIgnoreCase) ||
                         string.Equals(entry.Extension, ParamStructFileExtensions.FILE_EXT_SITEPLANNER, StringComparison.InvariantCultureIgnoreCase))
                {
                    // take additional managed files: SIMGEO, PADXF, BIN, GMDXF, SPDXF
                    managed.Add(entry);
                }
                else
                {
                    // take any other files (e.g., PDF, XML, XLSX, TXT, DOCX, PNG, JPG, etc.) as non-managed
                    non_managed.Add(entry);
                }
            }

            return (managed, non_managed);
        }

        #endregion

        #region UTILS

        private static void FileCanBeOverwritten(FileInfo _file)
        {
            FileInfo file_current_state = new FileInfo(_file.FullName);

            if (file_current_state.LastWriteTime > _file.LastWriteTime)
                throw new System.IO.IOException("Another user has written to this file!");

            if (file_current_state.LastWriteTime > DateTime.Now)
                throw new ArgumentOutOfRangeException(nameof(_file), "This file's last write access lies in the future!");
        }

        /// <summary>
        /// Creates a unique copying path for a file.
        /// </summary>
        /// <param name="_target_directory">the target folder for the copying</param>
        /// <param name="_original">the file to copy</param>
        /// <returns>a valid file target</returns>
        private static FileInfo CreateUniqueFileCopyPath(DirectoryInfo _target_directory, FileInfo _original)
        {
            if (_target_directory == null) return null;

            FileInfo[] files_in_di = _target_directory.GetFiles();
            var existing_file_names = files_in_di.Select(x => x.FullName);

            int counter = 0;
            string name_new = Path.Combine(_target_directory.FullName, _original.Name.Substring(0, _original.Name.Length - _original.Extension.Length) + _original.Extension);
            while (existing_file_names.Contains(name_new))
            {
                counter++;
                name_new = Path.Combine(_target_directory.FullName, _original.Name.Substring(0, _original.Name.Length - _original.Extension.Length) + "_" + counter.ToString() + _original.Extension);
            }

            return new FileInfo(name_new);
        }

        internal static DirectoryInfo CreateUnpackingDirectory(FileInfo _for_file)
        {
            DirectoryInfo parent_dir = _for_file.Directory;
            string sd_name = @"~" + _for_file.Name;
            DirectoryInfo[] subdirs = parent_dir.GetDirectories();
            while (subdirs.FirstOrDefault(x => x.FullName == Path.Combine(parent_dir.FullName, sd_name)) != null)
            {
                sd_name = @"~" + sd_name;
            }

            DirectoryInfo unpacking_dir = parent_dir.CreateSubdirectory(sd_name);
            unpacking_dir.Attributes = FileAttributes.Directory | FileAttributes.Hidden;

            return unpacking_dir;
        }

        #endregion
    }
}
