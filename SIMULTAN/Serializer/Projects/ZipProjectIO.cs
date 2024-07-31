using SIMULTAN.Data.Assets;
using SIMULTAN.Data.Components;
using SIMULTAN.Data.Users;
using SIMULTAN.Exceptions;
using SIMULTAN.Projects;
using SIMULTAN.Projects.ManagedFiles;
using SIMULTAN.Serializer.DXF;
using SIMULTAN.Serializer.PPATH;
using SIMULTAN.Utils;
using SIMULTAN.Utils.Files;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;

namespace SIMULTAN.Serializer.Projects
{
    /// <summary>
    /// Manages instances of <see cref="HierarchicalProject"/> in a Zip archive.
    /// </summary>
    public class ZipProjectIO
    {
        /// <summary>
        /// Stores the encryption key for all encrypted files.
        /// This is not a safe method. This key should only be used when we want to prevent easy file editing. But not to store privacy relevant data
        /// </summary>
        public static byte[] EncryptionKey { get { return Encoding.ASCII.GetBytes("ThWmZq4t6w9z$C&F"); } }

        #region New Project

        /// <summary>
        /// Creates a project with a few default values and components.
        /// </summary>
        /// <returns>he created project or Null</returns>
        /// <param name="_project_file">the target file for saving the project</param>
        /// <param name="_path_to_local_tmp_folder">the path to a temporary folder for saving temporary data</param>
        /// <param name="_project_data_manager">the manager of all relevant data</param>
        /// <param name="initialUser">
        /// The default user for this project. Has to be an Administrator.
        /// </param>
        public static HierarchicalProject NewProject(FileInfo _project_file, string _path_to_local_tmp_folder,
                                                ExtendedProjectData _project_data_manager,
                                                SimUser initialUser)
        {
            if (initialUser == null)
                throw new ArgumentNullException(nameof(initialUser));
            if (initialUser.Role != SimUserRole.ADMINISTRATOR)
                throw new ArgumentException(string.Format("{0} has to be an administrator.", nameof(initialUser)));

            //0 .add user
            _project_data_manager.UsersManager.Clear();
            _project_data_manager.UsersManager.Users.Add(initialUser);

            // 1. create a minimal project
            HierarchicalProject created = null;
            using (AccessCheckingDisabler.Disable(_project_data_manager.Components))
            {
                try
                {
                    created = ProjectIO.CreateMinimalProject(_project_file, _path_to_local_tmp_folder, _project_data_manager);
                    if (created != null)
                    {
                        // Zip the project
                        created.OnProjectLoaded();
                        created.OnProjectAuthenticated(true);
                        created.OnProjectOpened();

                        ZipProjectIO.Save(created, false);

                        // Close and unload the project
                        created.AuthenticationSkipped = true;
                        Close(created, true);
                        Unload(created);
                    }
                }
                catch (CreateProjectException)
                {
                    return null;
                }
            }

            return created;
        }

        #endregion



        #region Open

        /// <summary>
        /// Loads and creates a project from its project file. The contained data loaded into the provided data manager
        /// includes only the public values and public components.
        /// </summary>
        /// <param name="_project_file">The project file containing all information</param>
        /// <param name="_data_manager">The data manager capable of loading all types of project data</param>
        /// <returns>the loaded project</returns>
        public static HierarchicalProject Load(FileInfo _project_file, ExtendedProjectData _data_manager)
        {
            // Debug.WriteLine("-LOADING START...");
            // 1. check for an existing folder and offer recovery

            // 2. [ZIP] do the unpacking
            var extensions = ExtensionsToIncludeForLoading();
            //DirectoryInfo unpacked_folder = FileUtils.CreateUnpackingDirectory(_project_file);
            DirectoryInfo unpacked_folder = ProjectIO.CreateUnpackingDirectory(_project_file);

            IEnumerable<FileInfo> files = ZipUtils.PartialUnpackArchive(_project_file, extensions, unpacked_folder);
            // do partial unpacking of public resources
            FileInfo additional_stuff_to_unpack_file = files.FirstOrDefault(x => string.Equals(x.Extension,
                ParamStructFileExtensions.FILE_EXT_PUBLIC_PROJECT_PATHS, StringComparison.InvariantCultureIgnoreCase));
            if (additional_stuff_to_unpack_file != null && additional_stuff_to_unpack_file.Exists)
            {
                List<string> paths_to_unpack = PPathIO.Read(additional_stuff_to_unpack_file);

                var additional_stuff = ZipUtils.PartialUnpackPaths(_project_file, paths_to_unpack, unpacked_folder);
            }

            // Check if TaxonomyRecord exists, otherwise create it
            if (!files.Any(x => x.Extension == ParamStructFileExtensions.FILE_EXT_TAXONOMY))
            {
                var taxFile = ProjectIO.CreateInitialTaxonomyFile(unpacked_folder.FullName);
                var tmpFiles = files.ToList();
                tmpFiles.Add(taxFile);
                files = tmpFiles;
            }

            // 3. create the project instance and the managed file infrastructure
            ManagedFileCollection managed_files = new ManagedFileCollection(files.ToList(), _data_manager);
            var metaDataFile = managed_files.Files.FirstOrDefault(x => x is ManagedMetaData);
            HierarchicProjectMetaData metaData = ProjectIO.OpenMetaDataFile(metaDataFile.File);

            var project = new CompactProject(metaData.ProjectId, _project_file, _data_manager, managed_files,
                null, null, null, unpacked_folder);
            _data_manager.ImportLogFile = new FileInfo(string.Format(@".\ImportLog_{0}_{1:dd_MM_yyyy-HH_mm_ss}.txt",
                Path.GetFileNameWithoutExtension(_project_file.Name), DateTime.Now));

            using (AccessCheckingDisabler.Disable(_data_manager.Components))
            {
                // 4. perform the actual loading
                project.Load();
            }

            // 5. done (the meta data, public value and public component files are in the project unpack folder)
            return project;
        }


        /// <summary>
        /// Performs user authentication on an already loaded project. If the project is open or not properly loaded it does nothing.
        /// </summary>
        /// <param name="_project">the loaded project</param>
        /// <param name="_data_manager">the corresponding data manager</param>
        /// <param name="serviceProvider">The service provider that should be used to query the <see cref="IAuthenticationService"/></param>
        /// <returns>true, if the project can be opened; false otherwise</returns>
        public static bool AuthenticateUserAfterLoading(HierarchicalProject _project, ExtendedProjectData _data_manager,
            IServicesProvider serviceProvider)
        {
            if (!(_project is CompactProject cproject))
                throw new ArgumentException("The project must be of type compact!");

            // 1. check project state
            if (!IsCorrectlyLoaded(_project))
                throw new ProjectIOException(ProjectErrorCode.ERR_INVALID_STATE, "Project has to be in loaded state");

            // 2a. attempt authentication
            var extensions = ExtensionsToIncludeForAuthentication();
            IEnumerable<FileInfo> files = ZipUtils.PartialUnpackArchive(cproject.ProjectFile, extensions, _project.ProjectUnpackFolder);
            FileInfo user_file = files.FirstOrDefault(x => string.Equals(x.Extension, ParamStructFileExtensions.FILE_EXT_USERS, StringComparison.InvariantCultureIgnoreCase));

            if (user_file != null)
            {
                _project.PreAuthenticate(user_file, _data_manager, ZipProjectIO.EncryptionKey);
                var authService = serviceProvider.GetService<IAuthenticationService>();
                if (authService == null)
                    throw new ProjectIOException(ProjectErrorCode.ERR_AUTHSERVICE_NOT_FOUND, "IAuthService not found in service provider");

                var user = authService.Authenticate(_data_manager.UsersManager, cproject.ProjectFile);

                if (user != null && IsCorrectlyInPreAuthetication(_project))
                {
                    // user authentication successful -> load the rest of the files
                    _project.PostAuthenticate(user);
                }
                else
                {
                    // user authentication NOT successful -> still load the rest of the files,
                    // but with least possible access
                    _project.PostAuthenticate(null);
                }
            }
            _project.AuthenticationSkipped = false;

            // done (user file is in the project unpack folder, in addition to the 3 others - see Load)
            return IsCorrectlyInPostAuthentication(_project);
        }

        /// <summary>
        /// Performs user authentication on an already loaded project. If the project is open or not properly loaded it does nothing.
        /// </summary>
        /// <param name="_project">the loaded project</param>
        /// <param name="_data_manager">the corresponding data manager</param>
        /// <param name="user">The user that should be authenticated in the project (only username and passwordhash are used)</param>
        /// <returns>true, if the project can be opened; false otherwise</returns>
        public static SimUser AuthenticateUserAfterLoading(HierarchicalProject _project, ExtendedProjectData _data_manager, SimUser user)
        {
            if (!(_project is CompactProject cproject))
                throw new ArgumentException("The project must be of type compact!");

            // 1. check project state
            if (!IsCorrectlyLoaded(_project))
                throw new ProjectIOException(ProjectErrorCode.ERR_INVALID_STATE, "Project has to be in loaded state");

            // 2a. attempt authentication
            var extensions = ExtensionsToIncludeForAuthentication();
            IEnumerable<FileInfo> files = ZipUtils.PartialUnpackArchive(cproject.ProjectFile, extensions, _project.ProjectUnpackFolder);
            FileInfo user_file = files.FirstOrDefault(x => string.Equals(x.Extension, ParamStructFileExtensions.FILE_EXT_USERS, StringComparison.InvariantCultureIgnoreCase));

            SimUser authenticatedUser = null;

            if (user_file != null)
            {
                _project.PreAuthenticate(user_file, _data_manager, ZipProjectIO.EncryptionKey);
                authenticatedUser = _data_manager.UsersManager.Authenticate(user.Name, user.PasswordHash, true);

                if (authenticatedUser != null && IsCorrectlyInPreAuthetication(_project))
                {
                    // user authentication successful -> load the rest of the files
                    _project.PostAuthenticate(user);
                }
                else
                {
                    // user authentication NOT successful -> still load the rest of the files,
                    // but with least possible access
                    _project.PostAuthenticate(null);
                }
            }
            else
            {
                // 2b. open project w/o authentication
                // this option does not change the Caller in the ComponentFactory
                _project.AuthenticationSkipped = true;
                _project.OnProjectAuthenticated(true);
            }

            // done (user file is in the project unpack folder, in addition to the 3 others - see Load)
            if (IsCorrectlyInPostAuthentication(_project))
                return authenticatedUser;
            else
                return null;
        }

        /// <summary>
        /// Opens the project after a user has been authenticated. 
        /// All (non-public) project information is loaded into the data manager. The component permissions depend on the user role. 
        /// </summary>
        /// <param name="_project">the project to open</param>
        /// <param name="_data_manager">the data manager for loading the project information</param>
        public static void OpenAfterAuthentication(HierarchicalProject _project, ExtendedProjectData _data_manager)
        {
            // Debug.WriteLine("-OPENING START...");

            if (!(_project is CompactProject cproject))
                throw new ArgumentException("The project must be of type compact!");

            // 1. check project state
            if (!IsCorrectlyInPostAuthentication(_project))
                throw new ProjectIOException(ProjectErrorCode.ERR_INVALID_STATE, "Project has to be in post authentication state");

            // 2. unpack the project files and split them in managed and non-managed
            var extensions_to_skip = ZipProjectIO.ExtensionsToSkipForOpening();
            IEnumerable<FileSystemInfo> files = ZipUtils.PartialUnpackArchiveSkip(cproject.ProjectFile, extensions_to_skip, _project.ProjectUnpackFolder);
            var data_acc_to_type = SeparateFilesForManagement(files);

            using (AccessCheckingDisabler.Disable(_data_manager.Components))
            {
                // 3. load the data into the data manager
                _project.Open(data_acc_to_type.manageable, data_acc_to_type.nonmanageable, data_acc_to_type.contained_dirs, _data_manager);

                // at this point all project files are in its unpack folder

                // load all child projects into new data managers
                // locate each child project and load it
                foreach (var entry in cproject.ManagedFiles.MetaDataEntry.Data.ChildProjects)
                {
                    string full_path = FileSystemNavigation.ReconstructFullPath(cproject.ProjectUnpackFolder.FullName, entry.Value, false);
                    if (File.Exists(full_path))
                    {
                        FileInfo child_file = new FileInfo(full_path);
                        var child_pdm = new ExtendedProjectData(_data_manager.SynchronizationContext, _data_manager.DispatcherTimerFactory);
                        HierarchicalProject child_project = Load(child_file, child_pdm);
                        cproject.Children.Add(child_project);
                    }
                    else
                    {
                        // look for the project
                        FileInfo project_candidate = FindProject(entry.Key, entry.Value, cproject.AllProjectDataManagers.AssetManager,
                            _data_manager.SynchronizationContext, _data_manager.DispatcherTimerFactory);
                        if (project_candidate == null)
                        {
                            // ask the user for the correct target folder? ... TODO
                        }
                    }
                }

                _project.AllProjectDataManagers.ValueManager.ResetChanges();
                _project.AllProjectDataManagers.Components.ResetChanges();
                _project.AllProjectDataManagers.GeometryRelations.ResetChanges();
            }
        }

        #endregion

        #region Close

        /// <summary>
        /// Closes the project. This includes saving (if required) of the data in the data manager to the corresponding 
        /// project structures, closing those structures and clean-up.
        /// </summary>
        /// <param name="_project">the project to close</param>
        /// <param name="_unloading_follows">if True, the project is immediately unloaded after closing</param>
        /// <exception cref="ProjectFileDeleteException">In case access to a project file is denied</exception>
        public static void Close(HierarchicalProject _project, bool _unloading_follows)
        {
            // 1. check project state
            if (_project == null) return;

            // 4. unload all child projects          
            foreach (var childP in _project.Children)
            {
                Unload(childP);
            }

            // 5. clear and reset the data managers
            _project.Close(_unloading_follows);

            // 6. delete the no longer need files from the temporary folder
            var extensions_to_skip = ExtensionsToIncludeForLoading();

            try
            {
                DeleteFiles(_project.ProjectUnpackFolder, extensions_to_skip);
            }
            catch (ProjectFileDeleteException) { }
        }

        /// <summary>
        /// Unloads the project entirely. Even the public and meta-data are removed from the 
        /// temporary folder and it is deleted.
        /// </summary>
        /// <param name="_project">the project to unload</param>
        /// <returns>True, if the project state after unloading is valid</returns>
        /// <exception cref="ProjectFileDeleteException">in case access to a project file is denied</exception>
        public static bool Unload(HierarchicalProject _project)
        {
            // 1. check project state
            if (_project == null) return false;

            // 3. clear and reset the data managers
            _project.Unload();

            // 4. delete the temporary folder
            try
            {
                if (Directory.Exists(_project.ProjectUnpackFolder.FullName))
                {
                    // delete
                    Directory.Delete(_project.ProjectUnpackFolder.FullName, true);
                }
            }
            catch (IOException) { }

            // 5. unload the children projects
            foreach (var child in _project.Children)
            {
                if (child.IsLoaded)
                    Unload(child);
            }

            // 6. done
            return IsCorrectlyUnloaded(_project);
        }

        #endregion

        #region Save

        /// <summary>
        /// Transfers the data in the data manager to the project file.
        /// Caution: At the moment, saving is possible only if the project is open, loaded is not enough!!!
        /// </summary>
        /// <param name="_project">the project to save</param>
        /// <param name="_save_changes_only">if True only changes are saved, this is relevant in case of large files whose saving might take a long time</param>
        public static void Save(HierarchicalProject _project, bool _save_changes_only)
        {
            if (_project == null)
                throw new ArgumentNullException(string.Format("{0} may not be null", nameof(_project)));

            if (!(_project is CompactProject cproject))
                throw new ArgumentException("The project must be of type compact!");

            // 1. check project state
            if (!IsCorrectlyOpened(_project))
                throw new ProjectIOException(ProjectErrorCode.ERR_INVALID_STATE, "Project has to be in open state");

            if (_save_changes_only)
            {
                // --- PARTIAL SAVE ---
                // 3. save all managed files
                var files_to_save = _project.ManagedFiles.GetFilesWithUnsavedChanges().ToList();
                _project.ManagedFiles.Save(true);
                foreach (var child in _project.Children)
                {
                    child.ManagedFiles.SavePublic(true);
                }

                // 4. gather the information about all managed files with unsaved changes
                //    and replace them in the Zip Archive of the project
                FileInfo public_paths_file = GetFileContainingPublicResourcePaths(files_to_save);
                if (public_paths_file != null)
                    files_to_save.Add(public_paths_file);
                ZipUtils.UpdateArchiveFrom(cproject.ProjectFile, files_to_save, _project.ProjectUnpackFolder.FullName);
            }
            else
            {
                // --- FULL SAVE ---
                // 1. save all managed files
                _project.ManagedFiles.Save(false);
                foreach (var child in _project.Children)
                {
                    child.ManagedFiles.SavePublic(false);
                }

                // 2. gather the information from all managed and unmanaged files and save as a Zip Archive
                List<FileInfo> files_to_zip = GatherAllActiveProjectFiles(_project);
                FileInfo public_paths_file = GetFileContainingPublicResourcePaths(files_to_zip);
                if (public_paths_file != null)
                    files_to_zip.Add(public_paths_file);
                // 3. check if the user file is unpacked!
                FileInfo user_file_to_zip = files_to_zip.FirstOrDefault(x => string.Equals(x.Extension, ParamStructFileExtensions.FILE_EXT_USERS, StringComparison.InvariantCultureIgnoreCase));
                if (user_file_to_zip == null)
                {
                    var extensions = ExtensionsToIncludeForAuthentication();
                    IEnumerable<FileInfo> files = ZipUtils.PartialUnpackArchive(cproject.ProjectFile, extensions, _project.ProjectUnpackFolder);
                    if (files != null)
                        files_to_zip = new List<FileInfo>(files);
                }
                // 4. delete the old project file
                if (File.Exists(cproject.ProjectFile.FullName))
                    File.Delete(cproject.ProjectFile.FullName);
                // 5. save the new project file
                ZipUtils.CreateArchiveFrom(cproject.ProjectFile, _project.ContainedDirectoriesCopy, files_to_zip, _project.ProjectUnpackFolder.FullName);
            }
        }

        /// <summary>
        /// Copies a project to the target location. Does not change the state of the open project.
        /// Does not open the copied project.
        /// </summary>
        /// <param name="_project">the project to be copied</param>
        /// <param name="_new_project_file">the target location</param>
        public static void SaveAs(HierarchicalProject _project, FileInfo _new_project_file)
        {
            if (_project == null)
                throw new ArgumentNullException(nameof(_project));
            if (!(_project is CompactProject cproject))
                throw new ArgumentException("The project must be of type compact!");

            // 2. save the managed files (we cannot force the save of the non-managed or associated ones
            cproject.ManagedFiles.Save(false);

            // 3. copy the unpack folder
            DirectoryInfo unpack_dir_orig = cproject.ProjectUnpackFolder;
            DirectoryInfo unpack_dir_copy = ProjectIO.CreateUnpackingDirectory(_new_project_file);
            DirectoryOperations.DirectoryCopy(unpack_dir_orig.FullName, unpack_dir_copy.FullName, true);

            // 4. modify the metadata
            Guid location_new = Guid.Empty;
            var files_copy = unpack_dir_copy.GetFiles();
            FileInfo md_file_copy = files_copy.FirstOrDefault(x => string.Equals(x.Extension, ParamStructFileExtensions.FILE_EXT_META, StringComparison.InvariantCultureIgnoreCase));
            if (md_file_copy != null)
            {
                HierarchicProjectMetaData md = ProjectIO.OpenMetaDataFile(md_file_copy);
                HierarchicProjectMetaData md_copy = new HierarchicProjectMetaData(md); //Creates a new Id
                ProjectIO.SaveMetaDataFile(md_file_copy, md_copy);
                location_new = md_copy.ProjectId;
            }

            // ~5a. unpack the values file in order to change the locations of the values
            FileInfo value_file_copy = files_copy.FirstOrDefault(x => string.Equals(x.Extension, ParamStructFileExtensions.FILE_EXT_MULTIVALUES, StringComparison.InvariantCultureIgnoreCase));
            FileInfo comp_file_copy = files_copy.FirstOrDefault(x => string.Equals(x.Extension, ParamStructFileExtensions.FILE_EXT_COMPONENTS, StringComparison.InvariantCultureIgnoreCase));

            // ~5b. unpack the component file in order to change the paths for the contained resources and the locations of the components
            /*if (comp_file_copy != null && value_file_copy != null)
            {
                var projectData = new ExtendedProjectData();
                projectData.Project = cproject;

                projectData.AssetManager.WorkingDirectory = unpack_dir_copy.FullName;

#warning Parser Rework: Check if this is necessary
                ProjectIO.OpenMultiValueFileAlone(cproject, value_file_copy, projectData);
                projectData.ValueManager.SetCallingLocation(new DummyReferenceLocation(location_new));
                ProjectIO.SaveMultiValueFile(value_file_copy, projectData.ValueManager);

                ProjectIO.OpenComponentFileAlone(cproject, comp_file_copy, projectData);
                projectData.NetworkManager.SetCallingLocation(new DummyReferenceLocation(location_new));
                ProjectIO.SaveComponentFile(comp_file_copy, projectData);
            }*/

            // 6. pack everything in the new project
            if (File.Exists(_new_project_file.FullName))
                File.Delete(_new_project_file.FullName);

            // 7. save the new project file
            var file_copies_to_zip = GetFilesRecursive(unpack_dir_copy);
            var dir_copies_to_zip = GetSubDirsRecursive(unpack_dir_copy);
            ZipUtils.CreateArchiveFrom(_new_project_file, dir_copies_to_zip, file_copies_to_zip, unpack_dir_copy.FullName);

            // 8. clean-up
            Directory.Delete(unpack_dir_copy.FullName, true);

            // done
        }

        #endregion


        #region Util

        private static FileInfo GetFileContainingPublicResourcePaths(IList<FileInfo> _files)
        {
            FileInfo public_comps = _files.FirstOrDefault(x => string.Equals(x.Extension, ParamStructFileExtensions.FILE_EXT_COMPONENTS_PUBLIC, StringComparison.InvariantCultureIgnoreCase));
            if (public_comps != null)
            {
                // look for the file containing the public resource paths for unpacking
                string public_paths_file_name = public_comps.FullName.Substring(0, public_comps.FullName.Length - ParamStructFileExtensions.FILE_EXT_COMPONENTS_PUBLIC.Length) +
                                 ParamStructFileExtensions.PUBLIC_PROJECT_PATHS_SUFFIX +
                                 ParamStructFileExtensions.FILE_EXT_PUBLIC_PROJECT_PATHS;
                if (File.Exists(public_paths_file_name))
                    return new FileInfo(public_paths_file_name);
            }

            return null;
        }

        /// <summary>
        /// Searches for a project with the given Guid and relative path.
        /// </summary>
        /// <param name="projectId">the Guid of the project</param>
        /// <param name="projectRelPath">the last known relative path of the project (relative to the parent)</param>
        /// <param name="resources">the resource manager of the parent project</param>
        /// <param name="synchronize">the synchronization context</param>
        /// <param name="dispatcherTimer">The dispatcher timer factory</param>
        /// <returns>A file info pointing to the matching project file, or Null when no project could be found</returns>
        private static FileInfo FindProject(Guid projectId, string projectRelPath, AssetManager resources, ISynchronizeInvoke synchronize, IDispatcherTimerFactory dispatcherTimer)
        {
            if (projectId == Guid.Empty)
                throw new ArgumentException(string.Format("The {0} has its default value!", nameof(projectId)));
            if (string.IsNullOrEmpty(projectRelPath) || projectRelPath == AssetManager.PATH_NOT_FOUND)
                throw new ArgumentException(string.Format("{0} is undefined!", nameof(projectRelPath)));
            if (resources == null)
                throw new ArgumentNullException(string.Format("The provided AssetMamanger {0} is Null!", nameof(resources)));

            // search in the fallback resource folders
            foreach (var path in resources.PathsToResourceFiles)
            {
                var reconstructed = FileSystemNavigation.ReconstructFullPath(path, projectRelPath, true);
                if (!string.IsNullOrEmpty(reconstructed))
                {
                    if (File.Exists(reconstructed))
                    {
                        FileInfo projectFile = new FileInfo(reconstructed);
                        bool found = ProjectHasExpectedId(projectId, projectFile, synchronize, dispatcherTimer);
                        if (found)
                            return projectFile;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Checks if the project at the given location has the given Guid.
        /// </summary>
        /// <param name="projectId">the expected project Id</param>
        /// <param name="projectFile">the project file location</param>
        /// <param name="synchronize">the synchronization context</param>
        /// <param name="dispatcherTimer">The dispatcher timer factory</param>
        /// <returns>True, if the project could be located, loaded and its id was the expected one; False otherwise</returns>
        private static bool ProjectHasExpectedId(Guid projectId, FileInfo projectFile, ISynchronizeInvoke synchronize, IDispatcherTimerFactory dispatcherTimer)
        {
            if (projectFile == null)
                throw new ArgumentNullException(string.Format("The project file {0} cannot be null", nameof(projectFile)));
            if (!projectFile.Exists)
                throw new ArgumentException(string.Format("The project file {0} is invalid!", nameof(projectFile)));

            // attempt to load and read out the meta-data
            var pdm = new ExtendedProjectData(synchronize, dispatcherTimer);
            HierarchicalProject project = ZipProjectIO.Load(projectFile, pdm);
            if (project != null)
            {
                if (project.ManagedFiles.MetaDataEntry != null)
                {
                    Guid id = project.GlobalID;
                    if (id == projectId)
                    {
                        // project found!
                        ZipProjectIO.Unload(project);
                        pdm.Reset();
                        pdm = null;
                        return true;
                    }
                }
                ZipProjectIO.Unload(project);
                pdm.Reset();
                pdm = null;
            }

            return false;
        }


        /// <summary>
        /// Splits a collection of files into two file collections - 
        /// one of files that can be managed (e.g., *.codxf, *.padxf, *.bin, *.simgeo, etc.),
        /// one of files that cannot be managed (e.g., *.pdf, *.xml, *xslx, *.txt, *.docx, etc.)
        /// and one *flat* directory collection,
        /// and one of files that have no relevance for the open project (*.ppaths)
        /// </summary>
        /// <param name="_data">the file and directory collection to split</param>
        /// <returns>a triple of file collections</returns>
        private static (IEnumerable<FileInfo> manageable, IEnumerable<FileInfo> nonmanageable, IEnumerable<DirectoryInfo> contained_dirs, IEnumerable<FileInfo> auxiliary)
            SeparateFilesForManagement(IEnumerable<FileSystemInfo> _data)
        {
            List<FileInfo> managed = new List<FileInfo>();
            List<FileInfo> non_managed = new List<FileInfo>();
            List<DirectoryInfo> contained_dirs = new List<DirectoryInfo>();
            List<FileInfo> auxiliary_files = new List<FileInfo>();
            if (_data == null)
                return (new List<FileInfo>(), new List<FileInfo>(), new List<DirectoryInfo>(), new List<FileInfo>());

            List<string> file_ext = ParamStructFileExtensions.GetAllManagedFileExtensions();

            foreach (var entry in _data)
            {
                string epath = entry.FullName;
                if (File.Exists(epath))
                {
                    FileInfo efile = new FileInfo(epath);
                    string found_ext = file_ext.FirstOrDefault(fe => string.Equals(efile.Extension, fe, StringComparison.InvariantCultureIgnoreCase));
                    if (!string.IsNullOrEmpty(found_ext))
                        managed.Add(efile);
                    else if (string.Equals(efile.Extension, ParamStructFileExtensions.FILE_EXT_PUBLIC_PROJECT_PATHS, StringComparison.InvariantCultureIgnoreCase))
                        auxiliary_files.Add(efile);
                    else
                        non_managed.Add(efile);
                }
                else if (Directory.Exists(epath))
                {
                    var di = new DirectoryInfo(epath);
                    contained_dirs.Add(di);
                    var content = SeparateFilesForManagement(di.GetFileSystemInfos());
                    managed.AddRange(content.manageable);
                    non_managed.AddRange(content.nonmanageable);
                    contained_dirs.AddRange(content.contained_dirs);
                }
            }

            return (managed, non_managed, contained_dirs, auxiliary_files);
        }

        #endregion

        #region Project State Checks

        /// <summary>
        /// Determines if the given project has the correct state after loading.
        /// </summary>
        /// <param name="_project">the project</param>
        /// <returns>if True the project is correctly loaded</returns>
        private static bool IsCorrectlyLoaded(HierarchicalProject _project)
        {
            if (_project == null) return false;
            if (!_project.IsLoaded) return false;
            if (!Directory.Exists(_project.ProjectUnpackFolder.FullName)) return false;
            if (!_project.ManagedFiles.IsInLoadedState()) return false;

            return true;
        }

        /// <summary>
        /// Determines if the given project has the correct state before user authentication.
        /// </summary>
        /// <param name="_project">the project</param>
        /// <returns>True if the project is in the correct state</returns>
        private static bool IsCorrectlyInPreAuthetication(HierarchicalProject _project)
        {
            if (_project == null) return false;
            if (!_project.IsLoaded || !_project.IsReadyForAuthentication) return false;
            if (!Directory.Exists(_project.ProjectUnpackFolder.FullName)) return false;
            if (!_project.ManagedFiles.IsInPreAuthenticationState()) return false;

            return true;
        }

        /// <summary>
        /// Checks if the project state, the temporary project folder and the files in it are valid.
        /// </summary>
        /// <param name="_project">the project</param>
        /// <returns>True, if writing to the project files in the temporary directory is possible; False otherwise</returns>
        private static bool IsCorrectlyOpened(HierarchicalProject _project)
        {
            if (_project == null) return false;

            bool ok = (_project.IsLoaded && _project.IsAuthenticated && _project.IsOpened);
            ok &= (_project.ProjectUnpackFolder != null && Directory.Exists(_project.ProjectUnpackFolder.FullName));
            ok &= _project.ManagedFiles.HasValidEntries();
            ok &= _project.ManagedFiles.FulfillsMinimumForOpenProject();
            return ok;
        }

        /// <summary>
        /// Determines if the given project has the correct state after user authentication.
        /// </summary>
        /// <param name="_project">the project</param>
        /// <returns>True if the project is in the correct state</returns>
        private static bool IsCorrectlyInPostAuthentication(HierarchicalProject _project)
        {
            if (_project == null) return false;
            if (!_project.IsLoaded || !_project.IsAuthenticated) return false;
            if (!Directory.Exists(_project.ProjectUnpackFolder.FullName)) return false;
            if (!_project.ManagedFiles.IsInPostAuthenticationState()) return false;

            return true;
        }

        /// <summary>
        /// Checks the project state and if the project temporary folder exists.
        /// </summary>
        /// <param name="_project">the project</param>
        /// <returns>True, if the project has been fully reset and its temporary folder deleted</returns>
        private static bool IsCorrectlyUnloaded(HierarchicalProject _project)
        {
            if (_project == null) return false;

            bool ok = (!_project.IsLoaded && !_project.IsAuthenticated && !_project.IsReadyForAuthentication && !_project.IsOpened);
            //ok &= (!Directory.Exists(_project.ProjectUnpackFolder.FullName));
            ok &= (_project.NonManagedFiles.Count == 0 && _project.AssociatedFilesCount == 0);
            return ok;
        }

        #endregion

        #region Files

        /// <summary>
        /// Returns the file extensions - i.e., the file types to be used for loading a project.
        /// </summary>
        /// <returns>a collection of extensions w/o point</returns>
        private static HashSet<string> ExtensionsToIncludeForLoading()
        {
            return new HashSet<string>
            {
                ParamStructFileExtensions.FILE_EXT_META,
                ParamStructFileExtensions.FILE_EXT_MULTIVALUES_PUBLIC,
                ParamStructFileExtensions.FILE_EXT_COMPONENTS_PUBLIC,
                ParamStructFileExtensions.FILE_EXT_PUBLIC_PROJECT_PATHS,
                ParamStructFileExtensions.FILE_EXT_TAXONOMY,
            };
        }

        /// <summary>
        /// Returns the file extensions - i.e., the file types to be used for authentication within a project.
        /// </summary>
        /// <returns>the extension of the user record file</returns>
        private static HashSet<string> ExtensionsToIncludeForAuthentication()
        {
            return new HashSet<string> { ParamStructFileExtensions.FILE_EXT_USERS };
        }

        /// <summary>
        /// Returns the file extensions - i.e., the file types to be excluded from opening a project.
        /// </summary>
        /// <returns>a collection of extensions w/o point</returns>
        private static HashSet<string> ExtensionsToSkipForOpening()
        {
            return new HashSet<string>
            {
                ParamStructFileExtensions.FILE_EXT_META,
                ParamStructFileExtensions.FILE_EXT_USERS,
                ParamStructFileExtensions.FILE_EXT_MULTIVALUES_PUBLIC,
                ParamStructFileExtensions.FILE_EXT_COMPONENTS_PUBLIC,
                ParamStructFileExtensions.FILE_EXT_TAXONOMY,
            };
        }


        /// <summary>
        /// Removes all files from the given directory except those with the given extensions.
        /// </summary>
        /// <param name="directory">The file directory</param>
        /// <param name="excludedExtensions">The file extensions to skip during delete</param>
        private static void DeleteFiles(DirectoryInfo directory, HashSet<string> excludedExtensions)
        {
            string not_deleteable_path = string.Empty;
            List<(string path, ProjectErrorCode reason, object additionalData)> errs = new List<(string path, ProjectErrorCode reason, object additionalData)>();
            List<FileInfo> all_files = GetFilesRecursive(directory);
            foreach (FileInfo fi in all_files)
            {
                string ext = Path.GetExtension(fi.FullName);
                if (excludedExtensions.Contains(ext))
                    continue;
                try
                {
                    not_deleteable_path = fi.FullName;
                    File.Delete(fi.FullName);
                }
                catch (Exception)
                {
                    if (File.Exists(not_deleteable_path))
                    {
                        var locking_proc = LockTools.FindLockers(not_deleteable_path);
                        errs.Add((not_deleteable_path, ProjectErrorCode.ERR_ON_FILE_DELETE_FILEINUSE, locking_proc));
                    }
                    else
                    {
                        errs.Add((not_deleteable_path, ProjectErrorCode.ERR_ON_FILE_DELETE_FILENOTFOUND, null));
                    }
                }
            }
            if (errs.Count > 0)
            {
                throw new ProjectFileDeleteException(errs);
            }
        }

        internal static List<FileInfo> GetFilesRecursive(DirectoryInfo _dir)
        {
            List<FileInfo> files = new List<FileInfo>();
            FileInfo[] local_files = _dir.GetFiles();
            if (local_files != null)
                files.AddRange(local_files);

            DirectoryInfo[] local_folders = _dir.GetDirectories();
            if (local_folders != null)
            {
                foreach (DirectoryInfo di in local_folders)
                {
                    List<FileInfo> di_files = GetFilesRecursive(di);
                    files.AddRange(di_files);
                }
            }

            return files;
        }

        internal static List<DirectoryInfo> GetSubDirsRecursive(DirectoryInfo _dir)
        {
            List<DirectoryInfo> dirs = new List<DirectoryInfo>();

            DirectoryInfo[] local_folders = _dir.GetDirectories();
            if (local_folders != null)
            {
                dirs.AddRange(local_folders);
                foreach (DirectoryInfo di in local_folders)
                {
                    List<DirectoryInfo> di_subdirs = GetSubDirsRecursive(di);
                    dirs.AddRange(di_subdirs);
                }
            }

            return dirs;
        }


        /// <summary>
        /// Checks if any file in the given directory is locked. Throws <see cref="ProjectFileDeleteException"/> if it finds any.
        /// </summary>
        /// <param name="_dir">the directory to search for locked files</param>
        private static void CheckLockOnFiles(DirectoryInfo _dir)
        {
            string locked_path = string.Empty;
            List<(string path, ProjectErrorCode reason, object additionalData)> errs = new List<(string path, ProjectErrorCode reason, object additionalData)>();
            List<FileInfo> all_files = GetFilesRecursive(_dir);
            foreach (FileInfo fi in all_files)
            {
                try
                {
                    bool in_use = FileState.IsInUse(fi);
                    if (in_use)
                    {
                        var locking_proc = LockTools.FindLockers(fi.FullName);
                        errs.Add((fi.FullName, ProjectErrorCode.ERR_ON_FILE_DELETE_FILEINUSE, locking_proc));
                    }
                }
                catch (Exception e)
                {
                    errs.Add((fi.FullName, ProjectErrorCode.ERR_ON_FILE_DELETE_GENERIC, e));
                }

            }
            if (errs.Count > 0)
            {
                throw new ProjectFileDeleteException(errs);
            }
        }

        #endregion

        #region HierarchicalProject

        /// <summary>
        /// Gathers the information of all files currently managed or contained in a project.
        /// </summary>
        /// <param name="_project">the project</param>
        /// <returns>a list of files</returns>
        private static List<FileInfo> GatherAllActiveProjectFiles(HierarchicalProject _project)
        {
            List<FileInfo> files = new List<FileInfo>();
            foreach (ManagedFile mf in _project.ManagedFiles.Files)
            {
                if (mf is ManagedPublicComponentFile || mf is ManagedPublicValueFile || mf.IsValid())
                    files.Add(mf.File);
            }
            files.AddRange(_project.NonManagedFiles);
            return files;
        }

        #endregion
    }
}
