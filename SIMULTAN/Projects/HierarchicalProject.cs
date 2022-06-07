using SIMULTAN.Data;
using SIMULTAN.Data.Assets;
using SIMULTAN.Data.Components;
using SIMULTAN.Data.Geometry;
using SIMULTAN.Data.MultiValues;
using SIMULTAN.Data.SitePlanner;
using SIMULTAN.Data.Users;
using SIMULTAN.Exceptions;
using SIMULTAN.Projects.ManagedFiles;
using SIMULTAN.Serializer.DXF;
using SIMULTAN.Serializer.Projects;
using SIMULTAN.Serializer.SimGeo;
using SIMULTAN.Utils;
using SIMULTAN.Utils.Files;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Windows.Media;
using System.Windows.Threading;

namespace SIMULTAN.Projects
{
    /// <summary>
    /// The base class for handling both distributed and non-distributed (i.e. compact) projects
    /// and the relationships between them.
    /// </summary>
    public abstract class HierarchicalProject : IReferenceLocation
    {
        #region Collections

        public class ChildProjectCollection : ObservableCollection<HierarchicalProject>
        {
            private HierarchicalProject owner;

            public ChildProjectCollection(HierarchicalProject owner)
            {
                this.owner = owner;
            }

            #region Collection Implementation

            /// <inheritdoc />
            protected override void InsertItem(int index, HierarchicalProject item)
            {
                if (item == null)
                    throw new ArgumentNullException(nameof(item));

                SetValues(item);
                base.InsertItem(index, item);
            }
            /// <inheritdoc />
            protected override void RemoveItem(int index)
            {
                var oldItem = this[index];

                UnsetValues(oldItem);
                base.RemoveItem(index);
            }
            /// <inheritdoc />
            protected override void ClearItems()
            {
                foreach (var item in this)
                    UnsetValues(item);
                base.ClearItems();
            }
            /// <inheritdoc />
            protected override void SetItem(int index, HierarchicalProject item)
            {
                if (item == null)
                    throw new ArgumentNullException(nameof(item));

                var oldItem = this[index];
                UnsetValues(oldItem);
                SetValues(item);
                base.SetItem(index, item);
            }

            #endregion

            private void SetValues(HierarchicalProject item)
            {
                item.parentProjects.Add(this.owner);
            }
            private void UnsetValues(HierarchicalProject item)
            {
                item.parentProjects.Remove(this.owner);
            }
        }

        #endregion


        #region PROPERTIES: project hierarchy

        /// <summary>
        /// The parents of this project. They have reading and writing access to 
        /// the *public* components and values of this project.
        /// </summary>
        public IReadOnlyList<HierarchicalProject> ParentProjects { get { return parentProjects; } }
        private List<HierarchicalProject> parentProjects;

        /// <summary>
        /// All projects to which this project is a parent. Multiple parents per project are possible.
        /// </summary>
        public ChildProjectCollection Children { get; }

        /// <summary>
        /// The service provider of this project
        /// </summary>
        public IServicesProvider ServiceProvider { get; }

        internal IEnumerable<string> loadingChildren;

        #endregion

        #region PROPERTIES: project management

        /// <summary>
        /// The directory where the project file is unpacked on load or open.
        /// </summary>
        public DirectoryInfo ProjectUnpackFolder { get; protected set; }
        private FileSystemWatcher projectUnpackFolderWatcher;
        private Dictionary<string, DateTime> projectUnpackFolderLastChange;
        private Dispatcher localDispatcher;

        /// <summary>
        /// Internal field for the Property <see cref="ContainsUnsavedChanges"/>.
        /// </summary>
        protected bool contains_unsaved_changes;
        /// <summary>
        /// Indicates if any of the managed project files needs to be saved:
        /// if there is such a file, its value is True; otherwise False.
        /// </summary>
        public bool ContainsUnsavedChanges
        {
            get { return this.contains_unsaved_changes; }
            protected set
            {
                if (this.contains_unsaved_changes != value)
                {
                    this.contains_unsaved_changes = value;
                    this.OnProjectUpToDateChanged(!this.contains_unsaved_changes);
                }
            }
        }

        /// <summary>
        /// Returns the id of the currently loaded meta data.
        /// </summary>
        public Guid GlobalID { get; }
        /// <summary>
        /// The hierarchical project may not have an absolute path, so this property return null.
        /// </summary>
        public virtual string AbsolutePath
        {
            get
            {
                return string.Empty;
            }
        }

        /// <summary>
        ///  The project name
        /// </summary>
        public virtual string Name { get; }

        #endregion

        #region PROPERTIES: file management

        /// <summary>
        /// Holds all files managed by the project with their respective data managers. 
        /// Some of those files are saved with the project and represented by contained resources.
        /// Others are only linked and represented by linked resources. The full collection is 
        /// therefore available only after opening the project and parsing all resources.
        /// </summary>
        /// <remarks>NO OVERLAP with nonManagedFiles, SOME OVERLAP with associatedFiles</remarks>
        public ManagedFileCollection ManagedFiles
        {
            get { return this.managed_files; }
            protected set
            {
                if (this.managed_files != null)
                {
                    this.managed_files.Project = null;
                    this.managed_files.ManagedFileUpToDateStateChanged -= ManagedFiles_ManagedFileUpToDateStateChanged;
                }
                this.managed_files = value;
                if (this.managed_files != null)
                {
                    this.managed_files.Project = this;
                    this.managed_files.ManagedFileUpToDateStateChanged += ManagedFiles_ManagedFileUpToDateStateChanged;
                }
            }
        }
        /// <summary>
        /// Filed corresponding to Property ManagedFiles.
        /// </summary>
        protected ManagedFileCollection managed_files;

        public IReadOnlyList<FileInfo> NonManagedFiles { get { return nonManagedFiles; } }
        /// <summary>
        /// Holds all files that are saved in the project, but w/o special status - e.g. pdf, word, etc.
        /// They correspond to resource files of type <see cref="ContainedResourceFileEntry"/>. 
        /// </summary>
        /// <remarks>NO OVERLAP with ManagedFiles, NO OVERLAP with associatedFiles</remarks>
        private List<FileInfo> nonManagedFiles;

        /// <summary>
        /// Holds all resource folders that are saved in the project. Those can contain both files with 
        /// special status (e.g., geometry files) as well as any other contained resource.
        /// Corresponds to resources of type <see cref="ResourceDirectoryEntry"/>.
        /// </summary>
        /// <remarks>each folder can or does contain: other folders, SOME of the files in ManagedFiles, ALL of the files in nonManagedFiles, NONE of the files in associatedFiles</remarks>
        protected List<DirectoryInfo> containedDirectories;
        /// <summary>
        /// Gets a copy of containedDirectories.
        /// </summary>
        public List<DirectoryInfo> ContainedDirectoriesCopy { get { return (this.containedDirectories == null) ? null : new List<DirectoryInfo>(this.containedDirectories); } }


        /// <summary>
        /// Holds all files that are just linked to the project and correspond to resources
        /// of type <see cref="LinkedResourceFileEntry"/>. Some of those files
        /// are managed by the <see cref="ManagedFiles"/> property.
        /// </summary>
        /// <remarks>SOME OVERLAP with ManagedFiles, NO OVERLAP with nonManagedFiles</remarks>
        protected ObservableCollection<FileInfo> associatedFiles;
        /// <summary>
        /// Gets the number of associated files.
        /// </summary>
        public int AssociatedFilesCount { get { return (this.associatedFiles == null) ? 0 : this.associatedFiles.Count; } }

        /// <summary>
        /// A list of all file watchers - one per associated file.
        /// </summary>
        protected Dictionary<FileSystemWatcher, List<FileInfo>> associatedWatchers;
        private Dictionary<string, DateTime> associatedLastChange;
        /// <summary>
        /// A list of all file watchers for associated files that were deleted while the project is open.
        /// </summary>
        protected Dictionary<FileSystemWatcher, List<string>> associatedDeletedWatchers;

        /// <summary>
        /// Holds a collection of all data managers used in the project's managed files.
        /// </summary>
        public ExtendedProjectData AllProjectDataManagers
        {
            get { return this.all_project_data_managers; }
            internal set
            {
                if (this.all_project_data_managers != null)
                {
                    ((INotifyCollectionChanged)this.all_project_data_managers.AssetManager.Resources).CollectionChanged -= Resources_CollectionChanged;
                    this.all_project_data_managers.AssetManager.ChildResourceCollectionChanged -= Resources_CollectionChanged;
                    this.all_project_data_managers.AssetManager.ResourceRenamed -= Resource_Renamed;
                    this.all_project_data_managers.ParameterLibraryManager.ParameterRecord.CollectionChanged -= ParameterRecord_CollectionChanged;
                    this.all_project_data_managers.ExcelToolMappingManager.RegisteredTools.CollectionChanged -= ExcelToolRecord_CollectionChanged;
                }

                this.all_project_data_managers = value;

                if (this.all_project_data_managers != null)
                {
                    this.all_project_data_managers.Project = this;

                    ((INotifyCollectionChanged)this.all_project_data_managers.AssetManager.Resources).CollectionChanged += Resources_CollectionChanged;
                    this.all_project_data_managers.AssetManager.ChildResourceCollectionChanged += Resources_CollectionChanged;
                    this.all_project_data_managers.AssetManager.ResourceRenamed += Resource_Renamed;
                    this.all_project_data_managers.ParameterLibraryManager.ParameterRecord.CollectionChanged += ParameterRecord_CollectionChanged;
                    this.all_project_data_managers.ExcelToolMappingManager.RegisteredTools.CollectionChanged += ExcelToolRecord_CollectionChanged;
                }
            }
        }
        private ExtendedProjectData all_project_data_managers;

        public FileInfo ImportLogFile { get; set; }

        #endregion

        #region PROPERTIES: project state

        /// <summary>
        /// If True, the project has been loaded into the application. Its data may not be loaded in the data managers yet,
        /// so the project may not be editable yet.
        /// </summary>
        public bool IsLoaded { get; private set; }

        /// <summary>
        /// If True, the user file contained in the project has been loaded and the user can be authenticated.
        /// </summary>
        public bool IsReadyForAuthentication { get; private set; }
        /// <summary>
        /// It Ture, a valid user has been successfully authenticated.
        /// </summary>
        public bool IsAuthenticated { get; private set; }
        /// <summary>
        /// If True, the software environment did not allow authentication.
        /// </summary>
        public bool AuthenticationSkipped { get; internal set; }

        /// <summary>
        /// If True, the project has been loaded from the file system and its data has been loaded into the data managers.
        /// A valid user is logged in and can edit the project.
        /// </summary>
        public bool IsOpened { get; private set; }


        /// <summary>
        /// Indicates that the project is in a dynamic state - loading data.
        /// </summary>
        internal bool IsLoadingData { get; set; }
        /// <summary>
        /// Indicates that the project is in a dynamic state - unloading data.
        /// </summary>
        internal bool IsUnloadingData { get; set; }

        #endregion

        #region EVENTS

        /// <summary>
        /// Handler for the ProjectUpToDateChanged event.
        /// </summary>
        /// <param name="sender">the emitting object</param>
        /// <param name="isUpToDate">true = project is uptodate, false = there is an unsaved change</param>
        public delegate void ProjectUpToDateChangedEventHandler(object sender, bool isUpToDate);
        /// <summary>
        /// Emitted when a chanage in at least one of the files occurs after saving,
        /// or after saving or loading, when there is no change to be saved.
        /// </summary>
        public event ProjectUpToDateChangedEventHandler ProjectUpToDateChanged;
        /// <summary>
        /// Emits the ProjectUpToDateChanged event.
        /// </summary>
        /// <param name="isUpToDate">true = project is uptodate, false = there is an unsaved change</param>
        public void OnProjectUpToDateChanged(bool isUpToDate)
        {
            this.ProjectUpToDateChanged?.Invoke(this, isUpToDate);
        }

        /// <summary>
        /// Handler for the ProjectLoaded event.
        /// </summary>
        /// <param name="sender">the emitting object</param>
        public delegate void ProjectLoadedEventHandler(object sender);
        /// <summary>
        /// Emitted when the project was loaded from the file system. The opening of a project emits a separate event.
        /// </summary>
        public event ProjectLoadedEventHandler ProjectLoaded;
        /// <summary>
        /// Emits the ProjectLoaded event.
        /// </summary>
        public void OnProjectLoaded()
        {
            this.IsLoaded = true;
            this.ProjectLoaded?.Invoke(this);
        }

        /// <summary>
        /// Handler for the ProjectEditingPermitted event.
        /// </summary>
        /// <param name="sender">the emitting object</param>
        public delegate void ProjectReadyForAuthenticationEventHandler(object sender);
        /// <summary>
        /// Emitted when the project user file has been loaded and a valid user has logged in.
        /// </summary>
        public event ProjectReadyForAuthenticationEventHandler ProjectReadyForAuthentication;
        /// <summary>
        /// Emits the ProjectEditingPermitted event.
        /// </summary>
        public void OnProjectReadyForAuthentication()
        {
            this.IsReadyForAuthentication = true;
            this.ProjectReadyForAuthentication?.Invoke(this);
        }

        /// <summary>
        /// Handler for the ProjectAuthenticated event.
        /// </summary>
        /// <param name="sender">the emitting object</param>
        public delegate void ProjectAuthenticatedEventHandler(object sender);
        /// <summary>
        /// Emitted after a valid user has been successfully authenticated.
        /// </summary>
        public event ProjectAuthenticatedEventHandler ProjectAuthenticated;
        /// <summary>
        /// Emits the  ProjectAuthenticated event.
        /// </summary>
        /// <param name="_successfully">if True, the authentication was successful</param>
        public void OnProjectAuthenticated(bool _successfully)
        {
            this.IsReadyForAuthentication = false;
            if (_successfully)
                this.IsAuthenticated = true;
            else
                this.IsAuthenticated = false;

            this.ProjectAuthenticated?.Invoke(this);
        }

        /// <summary>
        /// Handler for the ProjectOpened event.
        /// </summary>
        /// <param name="sender">the emitting object</param>
        public delegate void ProjectOpenedEventHandler(object sender);
        /// <summary>
        /// Emitted when the project is loaded and its data was parsed into data containers. After this event, the project is
        /// ready for editing.
        /// </summary>
        public event ProjectOpenedEventHandler ProjectOpened;
        /// <summary>
        /// Sets the <see cref="IsOpened"/> property to True. Emits the ProjectOpened event.
        /// </summary>
        public void OnProjectOpened()
        {
            this.IsOpened = true;
            this.InitializeAssociatedWatchers();
            this.InitializeProjectUnpackFolderWatcher();
            this.ProjectOpened?.Invoke(this);
        }

        /// <summary>
        /// Handler for the ProjectClosed event.
        /// </summary>
        /// <param name="sender">the emitting object</param>
        public delegate void ProjectClosedEventHandler(object sender);
        /// <summary>
        /// Emitted when the project is closed. I.e., its data has been unloaded from the data containers. The project cannot be edited any longer.
        /// </summary>
        public event ProjectClosedEventHandler ProjectClosed;
        /// <summary>
        /// Emits the ProjectClosed event.
        /// </summary>
        public void OnProjectClosed()
        {
            this.IsOpened = false;
            this.IsAuthenticated = false;
            this.ReleaseAssociatedWatchers();
            this.ProjectClosed?.Invoke(this);
        }

        /// <summary>
        /// Handler for the ProjectUnloaded event.
        /// </summary>
        /// <param name="sender">the emitting object</param>
        public delegate void ProjectUnloadedEventHandler(object sender);
        /// <summary>
        /// Emitted when the project is unloaded from the application.
        /// </summary>
        public event ProjectUnloadedEventHandler ProjectUnloaded;
        /// <summary>
        /// Emits the ProjectUnloaded event.
        /// </summary>
        public void OnProjectUnloaded()
        {
            this.IsLoaded = false;
            this.ProjectUnloaded?.Invoke(this);
        }

        /// <summary>
        /// Handler for the FileImportTimeout event.
        /// </summary>
        /// <param name="sender">the emitting object</param>
        /// <param name="file">the file that timed out</param>
        public delegate void FileImportTimeoutEventHandler(object sender, FileInfo file);
        /// <summary>
        /// Emitted when the importing a file because of file system events times out.
        /// </summary>
        public event FileImportTimeoutEventHandler FileImportTimeout;
        /// <summary>
        /// Emits the ProjectUnloaded event.
        /// </summary>
        public void OnFileImportTimeout(FileInfo file)
        {
            this.FileImportTimeout?.Invoke(this, file);
        }

        #endregion


        #region .CTOR

        /// <summary>
        /// Initializes an instance of a hierarchical project.
        /// </summary>
        /// <param name="id">The id of the project</param>
        /// <param name="_files">the files that are to be managed by this project instance</param>
        /// <param name="projectData">a container holding all data managers used in the project</param>
        /// <param name="unpackFolder">the folder for unpacking the project's contents</param>
        /// <param name="_non_managed_files">the files saved but not managed by the project instance</param>
        /// <param name="_contained_dirs">the resource directories</param>
        /// <param name="_associated_files">the files that are simply associated but not saved or managed by the project instance</param>
        /// <param name="serviceProvider">The service provider</param>
        protected HierarchicalProject(Guid id, ManagedFileCollection _files, ExtendedProjectData projectData,
            DirectoryInfo unpackFolder,
            IEnumerable<FileInfo> _non_managed_files, IEnumerable<DirectoryInfo> _contained_dirs,
            IEnumerable<FileInfo> _associated_files, IServicesProvider serviceProvider)
        {
            this.GlobalID = id;
            this.ProjectUnpackFolder = unpackFolder;

            this.AllProjectDataManagers = projectData;
            this.AllProjectDataManagers.AssetManager.WorkingDirectory = ProjectUnpackFolder.FullName;

            this.ServiceProvider = serviceProvider;
            this.parentProjects = new List<HierarchicalProject>();

            this.Children = new ChildProjectCollection(this);
            this.ManagedFiles = _files;
            this.nonManagedFiles = (_non_managed_files == null) ? new List<FileInfo>() : new List<FileInfo>(_non_managed_files);
            this.associatedFiles = (_associated_files == null) ? new ObservableCollection<FileInfo>() : new ObservableCollection<FileInfo>(_associated_files);
            this.associatedFiles.CollectionChanged += AssociatedFiles_CollectionChanged;
            this.containedDirectories = (_contained_dirs == null) ? new List<DirectoryInfo>() : new List<DirectoryInfo>(_contained_dirs);

            this.IsLoaded = false;
            this.IsReadyForAuthentication = false;
            this.IsAuthenticated = false;
            this.AuthenticationSkipped = false;
            this.IsOpened = false;

            this.IsLoadingData = false;
            this.IsUnloadingData = false;
        }

        #endregion


        #region METHODS: hierarchy management



        #endregion

        #region Methods: Query

        /// <summary>
        /// Checks if the given component is part of the project.
        /// </summary>
        /// <param name="_component">the querying component</param>
        /// <returns>true if the component is in the component factory of this project</returns>
        public bool IsInProject(SimComponent _component)
        {
            if (_component == null)
                throw new ArgumentNullException(nameof(_component));

            if (_component.Factory == null)
                return false;

            if (_component.Factory == this.AllProjectDataManagers.Components)
                return true;

            return false;
        }
        /// <summary>
        /// Checks if the given parameter is part of the project.
        /// </summary>
        /// <param name="_parameter">the querying parameter</param>
        /// <returns>true if the containing parameter is in this project</returns>
        public bool IsInProject(SimParameter _parameter)
        {
            if (_parameter == null)
                throw new ArgumentNullException(nameof(_parameter));
            if (_parameter.Component == null)
                return false;
            return this.IsInProject(_parameter.Component);
        }
        /// <summary>
        /// Checks if the given calculation is part of the project.
        /// </summary>
        /// <param name="_calculation">the querying calculation</param>
        /// <returns>true if the containing calculation is in this project</returns>
        public bool IsInProject(SimCalculation _calculation)
        {
            if (_calculation == null)
                throw new ArgumentNullException(nameof(_calculation));
            if (_calculation.Component == null)
                return false;
            return this.IsInProject(_calculation.Component);
        }
        /// <summary>
        /// Checks if the given resource is part of the project.
        /// </summary>
        /// <param name="resource">the querying resource</param>
        /// <returns>true if the containing resource is in this project</returns>
        public bool IsInProject(ResourceEntry resource)
        {
            if (resource == null)
                throw new ArgumentNullException(nameof(resource));

            var localResource = this.AllProjectDataManagers.AssetManager.GetResource(resource.Key);
            return localResource == resource; //localResource can be null, but that doesn't matter here
        }
        /// <summary>
        /// Checks if the given value field is part of the project.
        /// </summary>
        /// <param name="multiValue">the querying value field</param>
        /// <returns>true if the value field is in this project</returns>
        public bool IsInProject(SimMultiValue multiValue)
        {
            if (multiValue == null)
                throw new ArgumentNullException(nameof(multiValue));

            return this.AllProjectDataManagers.ValueManager.Contains(multiValue);
        }

        #endregion

        #region METHODS: data management

        /// <summary>
        /// Empties the lists of non-managed, associated files and contained folders.
        /// </summary>
        public void ResetNonManaged()
        {
            this.nonManagedFiles.Clear();
            this.associatedFiles.Clear();
            this.containedDirectories.Clear();
        }

        /// <summary>
        /// Transfers non-managed and associated files, including all resource directories, to the project.
        /// </summary>
        /// <param name="_non_managed_files">the non-managed files (get saved in the project file)</param>
        /// <param name="_contained_dirs">the resource directories that contain both managed and non-managed files</param>
        public void PassNonManagedAndAllDirs(IEnumerable<FileInfo> _non_managed_files, IEnumerable<DirectoryInfo> _contained_dirs)
        {
            this.nonManagedFiles = (_non_managed_files == null) ? new List<FileInfo>() : new List<FileInfo>(_non_managed_files);
            this.containedDirectories = (_contained_dirs == null) ? new List<DirectoryInfo>() : new List<DirectoryInfo>(_contained_dirs);
        }

        /// <summary>
        /// Transfers associated files from the data managers to the project.
        /// </summary>
        public void GetAssociatedFiles()
        {
            if (this.AllProjectDataManagers != null)
            {
                if (this.associatedFiles != null)
                    this.associatedFiles.CollectionChanged -= AssociatedFiles_CollectionChanged;
                this.associatedFiles = new ObservableCollection<FileInfo>(this.AllProjectDataManagers.AssetManager.GetAllLinkedFiles());
                this.associatedFiles.CollectionChanged += AssociatedFiles_CollectionChanged;
            }
        }

        /// <summary>
        /// Adds a new resource file to the project. If the given file can be managed it is also added to the ManagedFileCollection.
        /// </summary>
        /// <param name="file">File to add</param>
        /// <param name="projectDataManager">The ProjectData</param>
        public void AddResourceFile(FileInfo file, ExtendedProjectData projectDataManager)
        {
            AssetManager asset_manager = this.AllProjectDataManagers.AssetManager;
            if (asset_manager == null)
                throw new Exception("The Asset Manager cannot be accessed!");

            DisableProjectUnpackFolderWatcher();
            asset_manager.AddResourceEntry(file);
            EnableProjectUnpackFolderWatcher();
        }

        /// <summary>
        /// To be used during the conversion of master into project files. Adds contained resources w/o synchronizing with the asset manager.
        /// </summary>
        /// <param name="_files">the files to add</param>
        public void AddUndifferentiated(IEnumerable<FileInfo> _files)
        {
            List<string> managed_exts = ParamStructFileExtensions.GetAllManagedFileExtensions();

            foreach (FileInfo entry in _files)
            {
                if (!File.Exists(entry.FullName))
                    continue;

                if (string.Equals(entry.Extension, ParamStructFileExtensions.FILE_EXT_COMPONENTS, StringComparison.InvariantCultureIgnoreCase) ||
                    string.Equals(entry.Extension, ParamStructFileExtensions.FILE_EXT_COMPONENTS_PUBLIC, StringComparison.InvariantCultureIgnoreCase) ||
                    string.Equals(entry.Extension, ParamStructFileExtensions.FILE_EXT_MULTIVALUES, StringComparison.InvariantCultureIgnoreCase) ||
                    string.Equals(entry.Extension, ParamStructFileExtensions.FILE_EXT_MULTIVALUES_PUBLIC, StringComparison.InvariantCultureIgnoreCase) ||
                    string.Equals(entry.Extension, ParamStructFileExtensions.FILE_EXT_EXCEL_TOOL_COLLECTION, StringComparison.InvariantCultureIgnoreCase) ||
                    string.Equals(entry.Extension, ParamStructFileExtensions.FILE_EXT_USERS, StringComparison.InvariantCultureIgnoreCase) ||
                    string.Equals(entry.Extension, ParamStructFileExtensions.FILE_EXT_LINKS, StringComparison.InvariantCultureIgnoreCase) ||
                    string.Equals(entry.Extension, ParamStructFileExtensions.FILE_EXT_META, StringComparison.InvariantCultureIgnoreCase) ||
                    string.Equals(entry.Extension, ParamStructFileExtensions.FILE_EXT_MASTER, StringComparison.InvariantCultureIgnoreCase) ||
                    string.Equals(entry.Extension, ParamStructFileExtensions.FILE_EXT_PROJECT, StringComparison.InvariantCultureIgnoreCase) ||
                    string.Equals(entry.Extension, ParamStructFileExtensions.FILE_EXT_PROJECT_COMPACT, StringComparison.InvariantCultureIgnoreCase))
                {
                    // ignore CODXF, CODXFP, MVDXF, MVDXFP, ETDXF, USRDXF, SIMLINKS, METADXF, master, smn, simultan

                    continue;
                }

                string found_ext = managed_exts.FirstOrDefault(fe => string.Equals(entry.Extension, fe, StringComparison.InvariantCultureIgnoreCase));
                bool is_managed = !string.IsNullOrEmpty(found_ext);
                if (is_managed)
                    this.ManagedFiles.AddFile(entry, this.AllProjectDataManagers);
                else
                {
                    FileInfo corresponding = this.nonManagedFiles.FirstOrDefault(x => string.Equals(x.FullName, entry.FullName));
                    if (corresponding == null)
                        this.nonManagedFiles.Add(entry);
                }

            }

        }

        /// <summary>
        /// Replaces non-associated (i.e., contained) managed files of the same type in the project.
        /// </summary>
        /// <param name="_old_files">a collection of old files to be replaced</param>
        /// <param name="_new_files">a collection of new files as a replacement</param>
        public void ReplaceManagedFiles(IEnumerable<FileInfo> _old_files, IEnumerable<FileInfo> _new_files)
        {
            if (_old_files == null || _new_files == null)
                throw new ArgumentException("The files to replace and their replacements cannot be null!");

            string ext_old = _old_files.First().Extension;
            if (!_old_files.All(x => string.Equals(x.Extension, ext_old, StringComparison.InvariantCultureIgnoreCase)))
                throw new ArgumentException("The old files have different extensions!");

            string ext_new = _new_files.First().Extension;
            if (!_new_files.All(x => string.Equals(x.Extension, ext_new, StringComparison.InvariantCultureIgnoreCase)))
                throw new ArgumentException("The new files have different extensions!");

            if (!string.Equals(ext_old, ext_new, StringComparison.InvariantCultureIgnoreCase))
                throw new ArgumentException("The extensions of the old and the new files do not match!");

            _old_files.ForEach(x => this.managed_files.RemoveFile(x));
            _new_files.ForEach(x => this.managed_files.AddFile(x, this.AllProjectDataManagers, true));

        }

        #endregion

        #region RESOURCE METHODS: create, delete, copy

        /// <summary>
        /// Copies an existing file into the given resource folder. The folder can be NULL.
        /// </summary>
        /// <param name="file">the file to copy</param>
        /// <param name="targetDir">the resource folder to copy the file to</param>
        /// <param name="copyNameFormat">Format used for the name of a copied resource.
        /// Arguments:
        ///   {0}: The original filename without extension
        ///   {1}: A running counter
        /// </param>
        /// <returns>the created resource</returns>
        public ResourceEntry CopyResourceAsContainedFileEntry(FileInfo file, DirectoryInfo targetDir,
            string copyNameFormat)
        {
            //AssetManager asset_manager = this.ManagedFiles.ComponentEntry?.DataManager?.GeneralAssetManager;
            AssetManager asset_manager = this.AllProjectDataManagers.AssetManager;
            if (asset_manager == null)
                throw new Exception("The Asset Manager cannot be accessed!");

            DisableProjectUnpackFolderWatcher();
            var created = asset_manager.CopyResourceAsContainedFileEntry(file, targetDir, copyNameFormat);
            //this.nonManagedFiles.Add(created.resource);
            EnableProjectUnpackFolderWatcher();
            return asset_manager.GetResource(created.key);
        }

        /// <summary>
        /// Creates a resource folder with the given name as a subfolder of the given target directory. The target directory can be NULL. 
        /// </summary>
        /// <param name="_dir_name">the name of the folder</param>
        /// <param name="_target_dir">the containing folder</param>
        /// <param name="collisionNameFormat">Format used for the name of a copied resource.
        /// Arguments:
        ///   {0}: The original filename without extension
        ///   {1}: A running counter
        /// </param>
        /// <returns>the created resource</returns>
        public ResourceEntry CreateResourceDirIn(string _dir_name, DirectoryInfo _target_dir, string collisionNameFormat)
        {
            AssetManager asset_manager = this.AllProjectDataManagers.AssetManager;
            if (asset_manager == null)
                throw new Exception("The Asset Manager cannot be accessed!");

            DisableProjectUnpackFolderWatcher();
            var created = asset_manager.CreateResourceDirIn(_dir_name, _target_dir, collisionNameFormat);
            //this.containedDirectories.Add(created.resource);
            EnableProjectUnpackFolderWatcher();

            return asset_manager.GetResource(created.key);
        }

        /// <summary>
        /// Tests if the given file can be linked to the project as a resource.
        /// </summary>
        /// <param name="_file">the resource file</param>
        /// <returns>feedback, including problems with the file's location</returns>
        public ResourceLocationError CanLinkAsResource(FileInfo _file)
        {
            AssetManager asset_manager = this.AllProjectDataManagers.AssetManager;
            if (asset_manager == null)
                throw new Exception("The Asset Manager cannot be accessed!");

            return asset_manager.CanLinkAsResource(_file);
        }


        /// <summary>
        /// Linkes a file outside the project's unpack directory as a resource. The target directory can be NULL.
        /// </summary>
        /// <param name="_file">the file to be linked</param>
        /// <param name="_target_dir">the directory where the link is deposited</param>
        /// <param name="_allow_duplicates">if true, the same file can be linked multiple times</param>
        /// <returns>the created resource</returns>
        public (ResourceEntry resource, bool isDuplicate) LinkResourceFile(FileInfo _file, DirectoryInfo _target_dir, bool _allow_duplicates)
        {
            //AssetManager asset_manager = this.ManagedFiles.ComponentEntry?.DataManager?.GeneralAssetManager;
            AssetManager asset_manager = this.AllProjectDataManagers.AssetManager;
            if (asset_manager == null)
                throw new Exception("The Asset Manager cannot be accessed!");

            var created = asset_manager.LinkResourceAsLinkedFileEntry(_file, _target_dir, _allow_duplicates);
            var resource = asset_manager.GetResource(created.key);
            return (resource, created.isDuplicate);
        }

        /// <summary>
        /// Deletes the given resource and the corresponding file and folder entries in the project. 
        /// Returns true if the deletion was successful. Returns false if the file
        /// is internally managed (e.g. geometry) and is currently open.
        /// </summary>
        /// <param name="_resource_to_delete">the resources that should be deleted</param>
        /// <returns>feedback about the success of the deletion</returns>
        public (bool found, bool deleted) DeleteResource(ResourceEntry _resource_to_delete)
        {
            AssetManager asset_manager = this.AllProjectDataManagers.AssetManager;
            if (asset_manager == null)
                throw new Exception("The Asset Manager cannot be accessed!");
            if (_resource_to_delete == null)
                throw new ArgumentNullException(nameof(_resource_to_delete));

            // for internally managed files (e.g. geometry): check if the file is open
            if (_resource_to_delete is ContainedResourceFileEntry rfEntry)
            {
                // geometry
                if (string.Equals(rfEntry.Extension, ParamStructFileExtensions.FILE_EXT_GEOMETRY_INTERNAL, StringComparison.InvariantCultureIgnoreCase))
                {
                    if (this.AllProjectDataManagers.GeometryModels.TryGetGeometryModel(rfEntry, out _, false))
                        return (true, false);
                }
                // site planner
                if (_resource_to_delete.CurrentFullPath.EndsWith(ParamStructFileExtensions.FILE_EXT_SITEPLANNER) || _resource_to_delete.CurrentFullPath.EndsWith(ParamStructFileExtensions.FILE_EXT_GEOMAP))
                {
                    if (this.AllProjectDataManagers.SitePlannerManager.IsFileOpen(new FileInfo(_resource_to_delete.CurrentFullPath)))
                        return (true, false);
                }
            }

            // test if deletion is possible, if not inform the user
            var del_test = asset_manager.DeleteResourceEntryPossible(_resource_to_delete);
            if (!del_test.exists || !del_test.can_delete)
                return (del_test.exists, del_test.can_delete);

            //Remove from siteplanner (in case of siteplanner file)
            if (_resource_to_delete is ContainedResourceFileEntry fe && fe.Extension == ParamStructFileExtensions.FILE_EXT_SITEPLANNER)
            {
                var spProject = this.AllProjectDataManagers.SitePlannerManager.SitePlannerProjects.FirstOrDefault(x => x.SitePlannerFile == fe);
                if (spProject != null)
                {
                    this.AllProjectDataManagers.ComponentGeometryExchange.RemoveSiteplannerProject(spProject);
                    this.AllProjectDataManagers.SitePlannerManager.SitePlannerProjects.Remove(spProject);
                }
            }

            DisableProjectUnpackFolderWatcher();

            ManagedFile corresponding =
                this.ManagedFiles.Files.FirstOrDefault(x => x.File != null && string.Equals(x.File.FullName, _resource_to_delete.CurrentFullPath, StringComparison.InvariantCultureIgnoreCase));
            if (corresponding != null)
                corresponding.OnDeleted(_resource_to_delete.Key);
            var deleted = asset_manager.DeleteResourceEntryAny(_resource_to_delete);

            EnableProjectUnpackFolderWatcher();

            return (true, deleted);
        }



        /// <summary>
        /// Deletes the given resource. Only used when the resource was deleted on a file system level.
        /// Only non managed files supported 
        /// </summary>
        /// <param name="_resource_to_delete">the resources that should be deleted</param>
        /// <returns>feedback about the success of the deletion</returns>
        private (bool found, bool deleted) ResourceDeletedExternally(ResourceEntry _resource_to_delete)
        {
            AssetManager asset_manager = this.AllProjectDataManagers.AssetManager;
            if (asset_manager == null)
                throw new Exception("The Asset Manager cannot be accessed!");
            if (_resource_to_delete == null)
                throw new ArgumentNullException(nameof(_resource_to_delete));

            var deleted = asset_manager.ResourceEntryDeltedExternally(_resource_to_delete);
            return (true, deleted);
        }

        /// <summary>
        /// Copies the contained resource to a new target directory within the same project.
        /// </summary>
        /// <param name="source">the resource file to copy</param>
        /// <param name="_target">the target folder</param>
        /// <param name="nameCollisionFormat">Format used for the name of a copied resource.
        /// Arguments:
        ///   {0}: The original filename without extension
        ///   {1}: A running counter
        /// </param>
        /// <param name="newOwner">The user who performs the copy operation. The created resource will be owned by this user. 
        /// When set to null, the original owner is kept</param>
        public void CopyResource(ResourceEntry source, ResourceDirectoryEntry _target, string nameCollisionFormat, SimUser newOwner = null)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            AssetManager asset_manager = this.AllProjectDataManagers.AssetManager;
            if (asset_manager == null)
                throw new Exception("The Asset Manager cannot be accessed!");

            DisableProjectUnpackFolderWatcher();
            (int key, _) = asset_manager.CopyResourceEntry(source, _target, nameCollisionFormat, newOwner);
            EnableProjectUnpackFolderWatcher();
        }

        #endregion

        #region RESOURCE METHODS: new

        public ResourceFileEntry AddEmptyResource(FileInfo file)
        {
            if (file == null)
                throw new ArgumentNullException(nameof(file));
            if (file.Exists)
                throw new ArgumentException("File exists already");

            DisableProjectUnpackFolderWatcher();

            //Needed because resource cannot be created for non existing files. Needs to be reworked at some point
            // Dispose immediately so the FileStream is closed again, else it causes a file lock
            file.Create().Dispose();

            int newResourceKey = this.AllProjectDataManagers.AssetManager.AddResourceEntry(file);
            var resource = this.AllProjectDataManagers.AssetManager.GetResource(newResourceKey) as ResourceFileEntry;

            EnableProjectUnpackFolderWatcher();

            return resource;
        }

        /// <summary>
        /// Adds an empty geometry resource to the given folder. If the folder is Null, it adds it directly 
        /// to the project's unpack folder.
        /// </summary>
        /// <param name="_target">the target folder, can be Null</param>
        /// <param name="_initial_file_name">the initial name of the file</param>
        /// /// <param name="nameCollisionFormat">Format used for the name of a copied resource.
        /// Arguments:
        ///   {0}: The original filename without extension
        ///   {1}: A running counter
        /// </param>
        /// <returns>the created resource</returns>
        public ResourceFileEntry AddEmptyGeometryResource(DirectoryInfo _target, string _initial_file_name,
            string nameCollisionFormat)
        {
            DisableProjectUnpackFolderWatcher();

            DirectoryInfo parent = (_target == null) ? this.ProjectUnpackFolder : _target;
            var files_in_parent = parent.GetFiles();
            var targetPath = Path.Combine(parent.FullName, _initial_file_name + ParamStructFileExtensions.FILE_EXT_GEOMETRY_INTERNAL);
            FileInfo fi = new FileInfo(targetPath);

            (_, var fileNameUnique) = AdmissibilityQueries.FileNameIsAdmissible(fi, x => !files_in_parent.Any(f => f.FullName == x),
                nameCollisionFormat);
            var uniqueFile = new FileInfo(fileNameUnique);

            var resource = AddEmptyResource(uniqueFile);

            var geometry = new GeometryModelData();
            geometry.Layers.Add(new Layer(geometry, "0") { Color = new DerivedColor(Colors.White) });
            var model = new GeometryModel(Guid.NewGuid(), _initial_file_name, resource, OperationPermission.DefaultWallModelPermissions, geometry);

            try
            {
                SimGeoIO.Save(model, resource, SimGeoIO.WriteMode.Plaintext);
            }
            catch (Exception e)
            {
                ExceptionToFileWriter.Write(e);
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
                throw;
            }

            EnableProjectUnpackFolderWatcher();

            return resource;
        }

        /// <summary>
        /// Adds an empty site planner resource to the given folder. If the folder is Null, it adds it directly 
        /// to the project's unpack folder.
        /// </summary>
        /// <param name="_target">the target folder, can be Null</param>
        /// <param name="_initial_file_name">the initial name of the file</param>
        /// <param name="nameCollisionFormat">Format used for the name of a copied resource.
        /// Arguments:
        ///   {0}: The original filename without extension
        ///   {1}: A running counter
        /// </param>
        /// <returns>the created resource</returns>
        public ResourceFileEntry AddEmptySitePlannerResource(DirectoryInfo _target, string _initial_file_name, string nameCollisionFormat)
        {
            DisableProjectUnpackFolderWatcher();

            // set up the file
            DirectoryInfo parent = (_target == null) ? this.ProjectUnpackFolder : _target;
            var files_in_parent = parent.GetFiles();
            var targetPath = Path.Combine(parent.FullName, _initial_file_name + ParamStructFileExtensions.FILE_EXT_SITEPLANNER);
            FileInfo fi = new FileInfo(targetPath);

            (_, var fileNameUnique) = AdmissibilityQueries.FileNameIsAdmissible(fi, x => !files_in_parent.Any(f => f.FullName == x),
                nameCollisionFormat);

            // create the file
            FileInfo file = new FileInfo(fileNameUnique);

            SitePlannerIO.CreateEmptySitePlannerProject(file);

            // add as a resource
            AssetManager asset_manager = this.AllProjectDataManagers.AssetManager;
            if (asset_manager == null)
                throw new Exception("The Asset Manager cannot be accessed!");
            int newResourceKey = asset_manager.AddResourceEntry(file);
            var resFileEntry = asset_manager.GetResource(newResourceKey) as ResourceFileEntry;

            EnableProjectUnpackFolderWatcher();

            return resFileEntry;
        }

        /// <summary>
        /// Adds an empty geomap resource to the given folder. If the folder is Null, it adds it directly 
        /// to the project's unpack folder.
        /// </summary>
        /// <param name="_target">the target folder, can be Null</param>
        /// <param name="_initial_file_name">the initial name of the file</param>
        /// <param name="nameCollisionFormat">Format used for the name of a copied resource.
        /// Arguments:
        ///   {0}: The original filename without extension
        ///   {1}: A running counter
        /// </param>
        /// <returns>the created resource</returns>
        public ResourceFileEntry AddEmptyGeoMapResource(DirectoryInfo _target, string _initial_file_name, string nameCollisionFormat)
        {
            DisableProjectUnpackFolderWatcher();
            // set up the file
            DirectoryInfo parent = (_target == null) ? this.ProjectUnpackFolder : _target;
            var files_in_parent = parent.GetFiles();
            var targetPath = Path.Combine(parent.FullName, _initial_file_name + ParamStructFileExtensions.FILE_EXT_GEOMAP);
            FileInfo fi = new FileInfo(targetPath);

            (_, var fileNameUnique) = AdmissibilityQueries.FileNameIsAdmissible(fi, x => !files_in_parent.Any(f => f.FullName == x),
                nameCollisionFormat);

            // create the file
            FileInfo file = new FileInfo(fileNameUnique);
            SitePlannerIO.CreateEmptyGeoMap(file);

            // add as a resource
            AssetManager asset_manager = this.AllProjectDataManagers.AssetManager;
            if (asset_manager == null)
                throw new Exception("The Asset Manager cannot be accessed!");
            int newResourceKey = asset_manager.AddResourceEntry(file);
            var resFileEntry = asset_manager.GetResource(newResourceKey) as ResourceFileEntry;

            // create GeoMap
            GeoMap gm = new GeoMap(resFileEntry);
            this.AllProjectDataManagers.SitePlannerManager.GeoMaps.Add(gm);

            EnableProjectUnpackFolderWatcher();

            return resFileEntry;
        }

        #endregion

        #region RESOURCE METHODS: rename, move

        /// <summary>
        /// Checks if the name of the given resource can be changed. This can also mean a change of the entire path.
        /// </summary>
        /// <param name="_resource">the resource that is to be renamed</param>
        /// <param name="_name">the new full name of the resource including file extension</param>
        /// <param name="nameCollisionFormat">Format used for the name of a copied resource.
        /// Arguments:
        ///   {0}: The original filename without extension
        ///   {1}: A running counter
        /// </param>
        /// <param name="_name_is_full_name">if true, use the method that creates additional directories; otherwise no structural changes expected</param>
        /// <returns>the result from the check and possibly an alternative name</returns>
        public (bool valid, string proposed_name) CanRenameResourceEntry(ResourceEntry _resource, string _name, string nameCollisionFormat,
            bool _name_is_full_name = false)
        {
            if (_resource == null)
                throw new ArgumentNullException("The resource cannot be Null!", nameof(_resource));

            // check if the path contains invalid characters
            var invalid_chars = Path.GetInvalidFileNameChars();
            bool valid = !invalid_chars.Any(x => _name.Contains(x));
            if (!valid)
                return (false, null);

            if (_name_is_full_name)
            {
                if (_resource is LinkedResourceFileEntry)
                    return (valid, null);
                else if (_resource is ContainedResourceFileEntry)
                {
                    var check = _resource.CanChangePath(new FileInfo(_name), nameCollisionFormat);
                    return (valid, check.proposed_name);
                }
                else if (_resource is ResourceDirectoryEntry)
                {
                    var check = _resource.CanChangePath(new DirectoryInfo(_name), nameCollisionFormat);
                    return (valid, check.proposed_name);
                }
            }
            else
            {
                DirectoryInfo parent = null;
                if (_resource is ContainedResourceFileEntry)
                {
                    parent = new FileInfo(_resource.CurrentFullPath).Directory;
                    var files_in_P = parent.GetFiles();
                    FileInfo fi = new FileInfo(_resource.CurrentFullPath.Replace(_resource.Name, _name));
                    (var admissible, var fileNameUnique) = AdmissibilityQueries.FileNameIsAdmissible(fi, x => !files_in_P.Any(f => f.FullName == x),
                        nameCollisionFormat);
                    return (valid, new FileInfo(fileNameUnique).Name);
                }
                else if (_resource is ResourceDirectoryEntry)
                {
                    parent = new DirectoryInfo(_resource.CurrentFullPath).Parent;
                    var dirs_in_P = parent.GetDirectories();
                    DirectoryInfo di = new DirectoryInfo(_resource.CurrentFullPath.Replace(_resource.Name, _name));
                    (var admissible, var dirNameUnique) = AdmissibilityQueries.DirectoryNameIsAdmissible(di, x => !dirs_in_P.Any(f => f.FullName == x),
                        nameCollisionFormat);
                    return (valid, new DirectoryInfo(dirNameUnique).Name);
                }
            }

            return (valid, null);
        }

        /// <summary>
        /// Renames a contained resource and synchronizes it with the file structure in the project's unpack directory.
        /// </summary>
        /// <param name="_resource">the resource that is to be renamed</param>
        /// <param name="_name">the new name of the resource including file extension</param>
        /// <param name="nameCollisionFormat">Format used for the name of a copied resource.
        /// Arguments:
        ///   {0}: The original filename without extension
        ///   {1}: A running counter
        /// </param>
        public void RenameResourceEntry(ResourceEntry _resource, string _name, string nameCollisionFormat)
        {
            if (_resource == null)
                throw new ArgumentNullException(nameof(_resource));

            if ((_resource is ContainedResourceFileEntry) || (_resource is ResourceDirectoryEntry))
            {
                DisableProjectUnpackFolderWatcher();
                _resource.ChangeName(_name, nameCollisionFormat);
                EnableProjectUnpackFolderWatcher();
            }
        }

        /// <summary>
        /// Checks if a resource can be moved to another resource folder within the same project.
        /// </summary>
        /// <param name="_resource">the resource to be moved</param>
        /// <param name="_target">the target resource folder, null stands for the project's unpack directory</param>
        /// <param name="nameCollisionFormat">Format used for the name of a copied resource.
        /// Arguments:
        ///   {0}: The original filename without extension
        ///   {1}: A running counter
        /// </param>
        /// <returns>the admissiblity of the change and a possible alternative name for the resource</returns>
        public (bool admissible, string proposed_name) CanMoveResourceEntry(ResourceEntry _resource, ResourceDirectoryEntry _target,
            string nameCollisionFormat)
        {
            if (_resource == null)
                throw new ArgumentNullException("The resource cannot be Null!", nameof(_resource));

            AssetManager asset_manager = this.AllProjectDataManagers.AssetManager;
            if (asset_manager == null)
                throw new Exception("The Asset Manager cannot be accessed!");

            // the resource to be moved does not have to exist on the file system (so the CurrentFullPath can be a unknown)!
            if (asset_manager.GetKey(_resource.Name) < 0)
                throw new ArgumentException("The resource cannot be found among the project's resources!");

            if (_target != null && asset_manager.GetResourceKey(new FileInfo(_target.CurrentFullPath)) < 0)
                throw new ArgumentException("The target cannot be found among the project's resources!");
            if (_target != null && !Directory.Exists(_target.CurrentFullPath))
                throw new ArgumentException("The target folder does not exist in the file system!");

            DirectoryInfo target_dir = (_target == null) ? null : new DirectoryInfo(_target.CurrentFullPath);
            return _resource.CanChangeLocation(target_dir, nameCollisionFormat);
        }

        /// <summary>
        /// Can be used to move a resource from one resource folder to another within the same project.
        /// </summary>
        /// <param name="_resource">the resource that is to be moved</param>
        /// <param name="_target">the resource directory to which to move the resource, null stands for the project's unpack directory</param>
        /// <param name="nameCollisionFormat">Format used for the name of a copied resource.
        /// Arguments:
        ///   {0}: The original filename without extension
        ///   {1}: A running counter
        /// </param>
        /// <param name="_check_admissibility">if true, calls the checking function first and uses the alternative name, if necessary</param>
        public void MoveResourceEntry(ResourceEntry _resource, ResourceDirectoryEntry _target, string nameCollisionFormat, bool _check_admissibility)
        {
            if (_resource == null)
                throw new ArgumentNullException("The resource cannot be Null!", nameof(_resource));

            AssetManager asset_manager = this.AllProjectDataManagers.AssetManager;
            if (asset_manager == null)
                throw new Exception("The Asset Manager cannot be accessed!");

            DisableProjectUnpackFolderWatcher();
            DirectoryInfo target_folder = (_target == null) ? null : new DirectoryInfo(_target.CurrentFullPath);
            if (_resource is ResourceDirectoryEntry)
            {
                if (string.Equals((_resource as ResourceDirectoryEntry).CurrentFullPath, target_folder.FullName, StringComparison.InvariantCultureIgnoreCase))
                    return;
            }
            _resource.ChangeLocation(target_folder, nameCollisionFormat, _check_admissibility);
            EnableProjectUnpackFolderWatcher();
        }

        /// <summary>
        /// Replaces an existing linked file with another w/o deleting the resource.
        /// </summary>
        /// <param name="_new_file">the relacement file</param>
        /// <param name="_resource">the resource to be replaced</param>
        /// <param name="_allow_duplicates">if true, the same file can be linked multiple times</param>
        /// <returns>true if the file was a duplicate</returns>
        public bool ReplaceLinkedResource(ResourceEntry _resource, FileInfo _new_file, bool _allow_duplicates)
        {
            if (_resource == null)
                throw new ArgumentNullException(nameof(_resource));
            if (!(_resource is LinkedResourceFileEntry))
                throw new ArgumentException("The given resource is not linked!");
            if (_new_file == null)
                throw new ArgumentNullException(nameof(_new_file));
            if (!File.Exists(_new_file.FullName))
                throw new ArgumentException("The replacement file does not exist!");

            AssetManager asset_manager = this.AllProjectDataManagers.AssetManager;
            if (asset_manager == null)
                throw new Exception("The Asset Manager cannot be accessed!");

            var is_duplicate = asset_manager.ReLinkLinkedFileEntry(_resource as LinkedResourceFileEntry, _new_file, _allow_duplicates);
            return is_duplicate;
        }

        /// <summary>
        /// Converts an existing contained resource into a linked one. The target folder should be outside the
        /// working directory and contained in one of the fallbacks.
        /// </summary>
        /// <param name="_original">the resource to convert</param>
        /// <param name="_target_folder">the target folder for the linked resource</param>
        /// <param name="copyNameFormat">Format used for the name of a copied resource.
        /// Arguments:
        ///   {0}: The original filename without extension
        ///   {1}: A running counter
        /// </param>
        /// <returns>the converted resource or null</returns>
        public LinkedResourceFileEntry ContainedToLinked(ContainedResourceFileEntry _original, DirectoryInfo _target_folder,
            string copyNameFormat)
        {
            AssetManager asset_manager = this.AllProjectDataManagers.AssetManager;
            if (asset_manager == null)
                throw new Exception("The Asset Manager cannot be accessed!");

            return asset_manager.ContainedToLinked(_original, _target_folder, copyNameFormat);
        }

        /// <summary>
        /// Converts a linked resource into a contained one in the same resource folder where the linked resource used to be.
        /// </summary>
        /// <param name="_original">the resource to convert</param>
        /// <param name="nameFormat">Format used for the name of a copied resource.
        /// Arguments:
        ///   {0}: The original filename without extension
        ///   {1}: A running counter
        /// </param>
        /// <returns>the converted resource</returns>
        public ContainedResourceFileEntry LinkedToContained(LinkedResourceFileEntry _original,
            string nameFormat)
        {
            AssetManager asset_manager = this.AllProjectDataManagers.AssetManager;
            if (asset_manager == null)
                throw new Exception("The Asset Manager cannot be accessed!");

            return asset_manager.LinkedToContained(_original, nameFormat);
        }

        #endregion

        #region METHODS: querying

        /// <summary>
        /// Finds the managed file corresponding to the given resource entry.
        /// </summary>
        /// <param name="_resource">the resource entry</param>
        /// <returns>the found managed file or Null</returns>
        public ManagedFile GetCorresponding(ResourceEntry _resource)
        {
            if (_resource == null)
                throw new ArgumentNullException("The resource cannot be Null!", nameof(_resource));

            return this.ManagedFiles.GetFileWithResourceIndex(_resource.Key);
        }


        public IEnumerable<HierarchicalProject> FindProjects(Predicate<HierarchicalProject> predicate)
        {
            if (predicate(this))
                yield return this;

            foreach (var child in this.Children)
                foreach (var result in child.FindProjects(predicate))
                    yield return result;
        }

        #endregion

        #region EVENT HANDLERS: resource management

        private void ManagedFiles_ManagedFileUpToDateStateChanged(object sender, IEnumerable<ManagedFile> files, bool all_upToDate)
        {
            this.ContainsUnsavedChanges = !all_upToDate;
        }

        // --------------------------------------------------------------------------------------- //

        private void Resources_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (!this.IsLoaded || this.IsLoadingData || this.IsUnloadingData) return;

            object old_item = e.OldItems?[0];
            object new_item = e.NewItems?[0];
            if (e.Action == NotifyCollectionChangedAction.Add && new_item is ResourceEntry)
            {
                foreach (var item in e.NewItems)
                {
                    this.AddSingleResourceRecord(item as ResourceEntry);
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove && old_item is ResourceEntry)
            {
                foreach (var item in e.OldItems)
                {
                    this.DeleteSingleResourceRecord(item as ResourceEntry);
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Replace && new_item is ResourceEntry && old_item is ResourceEntry)
            {
                foreach (var item in e.OldItems)
                {
                    this.DeleteSingleResourceRecord(item as ResourceEntry);
                }
                foreach (var item in e.NewItems)
                {
                    this.AddSingleResourceRecord(item as ResourceEntry);
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                if (e.OldItems != null)
                {
                    foreach (var item in e.OldItems)
                    {
                        this.DeleteSingleResourceRecord(item as ResourceEntry);
                    }
                }
                if (e.NewItems != null)
                {
                    foreach (var item in e.NewItems)
                    {
                        this.AddSingleResourceRecord(item as ResourceEntry);
                    }
                }
            }
        }

        private void AddSingleResourceRecord(ResourceEntry _resource)
        {
            List<string> managed_exts = ParamStructFileExtensions.GetAllManagedFileExtensions();
            FileInfo rfile = new FileInfo(_resource.CurrentFullPath);
            string found_ext = managed_exts.FirstOrDefault(fe => string.Equals(rfile.Extension, fe, StringComparison.InvariantCultureIgnoreCase));
            bool is_managed = !string.IsNullOrEmpty(found_ext);
            bool to_be_openend = is_managed && (_resource.CurrentFullPath.EndsWith(ParamStructFileExtensions.FILE_EXT_GEOMAP) || _resource.CurrentFullPath.EndsWith(ParamStructFileExtensions.FILE_EXT_SITEPLANNER));

            if (_resource is ContainedResourceFileEntry)
            {
                if (File.Exists(_resource.CurrentFullPath))
                {
                    if (is_managed)
                        this.managed_files.AddFile(new FileInfo(_resource.CurrentFullPath), this.AllProjectDataManagers, to_be_openend);
                    else
                        this.nonManagedFiles.Add(new FileInfo(_resource.CurrentFullPath));
                }
            }
            else if (_resource is LinkedResourceFileEntry)
            {
                if (File.Exists(_resource.CurrentFullPath))
                {
                    if (is_managed)
                        this.managed_files.AddFile(new FileInfo(_resource.CurrentFullPath), this.AllProjectDataManagers, to_be_openend);

                    // check for duplicates because the same linked file can be present in multiple resource directories!
                    FileInfo corresponding = this.associatedFiles.FirstOrDefault(x => string.Equals(x.FullName, _resource.CurrentFullPath));
                    if (corresponding == null)
                        this.associatedFiles.Add(new FileInfo(_resource.CurrentFullPath));
                }
            }
            else if (_resource is ResourceDirectoryEntry)
            {
                if (Directory.Exists(_resource.CurrentFullPath))
                {
                    this.containedDirectories.Add(new DirectoryInfo(_resource.CurrentFullPath));
                    foreach (ResourceEntry child in (_resource as ResourceDirectoryEntry).Children)
                    {
                        this.AddSingleResourceRecord(child);
                    }
                }
            }
        }


        private void DeleteSingleResourceRecord(ResourceEntry _resource)
        {
            List<string> managed_exts = ParamStructFileExtensions.GetAllManagedFileExtensions();
            FileInfo rfile = (_resource.CurrentFullPath == AssetManager.PATH_NOT_FOUND || string.IsNullOrEmpty(_resource.CurrentFullPath))
                ? null : new FileInfo(_resource.CurrentFullPath);
            string found_ext = (rfile == null)
                ? string.Empty : managed_exts.FirstOrDefault(fe => string.Equals(rfile.Extension, fe, StringComparison.InvariantCultureIgnoreCase));
            bool is_managed = !string.IsNullOrEmpty(found_ext);

            if (_resource is ContainedResourceFileEntry)
            {
                if (is_managed)
                    this.managed_files.RemoveFile(new FileInfo(_resource.CurrentFullPath));
                else
                {
                    FileInfo corresponding = this.nonManagedFiles.FirstOrDefault(x => string.Equals(x.FullName, _resource.CurrentFullPath));
                    if (corresponding != null)
                        this.nonManagedFiles.Remove(corresponding);
                }
            }
            else if (_resource is LinkedResourceFileEntry)
            {
                if (is_managed)
                    this.managed_files.RemoveFile(new FileInfo(_resource.CurrentFullPath));

                FileInfo corresponding = this.associatedFiles.FirstOrDefault(x => string.Equals(x.FullName, _resource.CurrentFullPath));
                if (corresponding != null)
                    this.associatedFiles.Remove(corresponding);
            }
            else if (_resource is ResourceDirectoryEntry)
            {
                DirectoryInfo corresponding = this.containedDirectories.FirstOrDefault(x => string.Equals(x.FullName, _resource.CurrentFullPath));
                if (corresponding != null)
                    this.containedDirectories.Remove(corresponding);
                foreach (ResourceEntry child in (_resource as ResourceDirectoryEntry).Children)
                {
                    this.DeleteSingleResourceRecord(child);
                }
            }
        }

        private void Resource_Renamed(object sender, string oldName, string newName)
        {
            // management for files
            bool is_managed_old = false;
            bool is_managed_new = false;
            if (File.Exists(newName))
            {
                List<string> managed_exts = ParamStructFileExtensions.GetAllManagedFileExtensions();
                FileInfo rfile_old = (oldName == AssetManager.PATH_NOT_FOUND) ? null : new FileInfo(oldName);
                if (rfile_old != null)
                {
                    string found_ext_old = managed_exts.FirstOrDefault(fe => string.Equals(rfile_old.Extension, fe, StringComparison.InvariantCultureIgnoreCase));
                    is_managed_old = !string.IsNullOrEmpty(found_ext_old);
                }
                else
                    is_managed_old = false;

                FileInfo rfile_new = (newName == AssetManager.PATH_NOT_FOUND) ? null : new FileInfo(newName);
                if (rfile_new != null)
                {
                    string found_ext_new = managed_exts.FirstOrDefault(fe => string.Equals(rfile_new.Extension, fe, StringComparison.InvariantCultureIgnoreCase));
                    is_managed_new = !string.IsNullOrEmpty(found_ext_new);
                }
                else
                    is_managed_new = false;

                if (is_managed_old && is_managed_new)
                    this.ManagedFiles.OnManagedFileRenaming(rfile_old, rfile_new);
                else if (!is_managed_old && is_managed_new)
                    this.managed_files.AddFile(rfile_new, this.AllProjectDataManagers);
                else if (is_managed_old && !is_managed_new)
                    this.managed_files.RemoveFile(rfile_old);
            }

            // housekeeping
            DirectoryInfo di = this.containedDirectories.FirstOrDefault(x => string.Equals(x.FullName, oldName));
            if (di != null)
            {
                this.containedDirectories.Remove(di);
                if (newName != AssetManager.PATH_NOT_FOUND)
                    this.containedDirectories.Add(new DirectoryInfo(newName));
            }
            else
            {

                FileInfo non_managed_file_old = this.nonManagedFiles.FirstOrDefault(x => string.Equals(x.FullName, oldName));
                if (non_managed_file_old != null)
                {
                    this.nonManagedFiles.Remove(non_managed_file_old);
                    if (!is_managed_new && newName != AssetManager.PATH_NOT_FOUND)
                        this.nonManagedFiles.Add(new FileInfo(newName));
                }

                FileInfo associated_file_old = this.associatedFiles.FirstOrDefault(x => string.Equals(x.FullName, oldName));
                if (associated_file_old != null)
                {
                    this.associatedFiles.Remove(associated_file_old);
                    if (newName != AssetManager.PATH_NOT_FOUND)
                        this.associatedFiles.Add(new FileInfo(newName));
                }
                else
                {
                    if (oldName == AssetManager.PATH_NOT_FOUND && newName != AssetManager.PATH_NOT_FOUND && !string.IsNullOrEmpty(newName) &&
                        new FileInfo(newName).Exists)
                    {
                        // file that was missing on project opening was just restored
                        this.associatedFiles.Add(new FileInfo(newName));
                    }
                }
            }

        }

        #endregion

        #region EVENT HANDLER: data management

        private void ParameterRecord_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (!this.IsLoaded || this.IsLoadingData || this.IsUnloadingData) return;

            // check for adding a managed file
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                if (this.AllProjectDataManagers.ParameterLibraryManager.ParameterRecord.Count > 0)
                {
                    if (this.ManagedFiles.ParameterLibraryEntries.Count() == 0)
                    {
                        // a parameter file needs to be created and saved
                        string file_path_param_lib = Path.Combine(this.ProjectUnpackFolder.FullName, "ParameterRecord" + ParamStructFileExtensions.FILE_EXT_PARAMETERS);
                        FileInfo file_param_lib = new FileInfo(file_path_param_lib);
                        ProjectIO.SaveParameterLibraryFile(file_param_lib, this.AllProjectDataManagers.ParameterLibraryManager);
                        this.ManagedFiles.AddFile(file_param_lib, this.AllProjectDataManagers);
                    }
                }
            }

        }

        private void ExcelToolRecord_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (!this.IsLoaded || this.IsLoadingData || this.IsUnloadingData) return;

            // check for adding a managed file
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                if (this.AllProjectDataManagers.ExcelToolMappingManager.RegisteredTools.Count > 0)
                {
                    if (this.ManagedFiles.ExcelToolEntries.Count() == 0)
                    {
                        // an excel tool lobrary file needs to be created and saved
                        string file_path_excel_lib = Path.Combine(this.ProjectUnpackFolder.FullName, "ExcelToolLibrary" + ParamStructFileExtensions.FILE_EXT_EXCEL_TOOL_COLLECTION);
                        FileInfo file_excel_lib = new FileInfo(file_path_excel_lib);
                        ProjectIO.SaveExcelToolCollectionFile(file_excel_lib, this.AllProjectDataManagers);
                        this.ManagedFiles.AddFile(file_excel_lib, this.AllProjectDataManagers);
                    }
                }
            }
        }

        #endregion

        #region FILE WATCHERS

        private void AssociatedFiles_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (var item in e.NewItems)
                    {
                        if (item is FileInfo)
                            this.AddWatcherForAssociatedPath((item as FileInfo));
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    foreach (var item in e.OldItems)
                    {
                        if (item is FileInfo)
                            this.RemoveWatcherForAssociatedPath((item as FileInfo));
                    }
                    break;
                case NotifyCollectionChangedAction.Replace:
                    foreach (var item in e.OldItems)
                    {
                        if (item is FileInfo)
                            this.RemoveWatcherForAssociatedPath((item as FileInfo));
                    }
                    foreach (var item in e.NewItems)
                    {
                        if (item is FileInfo)
                            this.AddWatcherForAssociatedPath((item as FileInfo));
                    }
                    break;
                case NotifyCollectionChangedAction.Reset:
                    this.ReleaseAssociatedWatchers();
                    break;
            }
        }

        private void InitializeProjectUnpackFolderWatcher()
        {
            projectUnpackFolderLastChange = new Dictionary<string, DateTime>();
            localDispatcher = Dispatcher.CurrentDispatcher;

            projectUnpackFolderWatcher = new FileSystemWatcher();
            projectUnpackFolderWatcher.Path = ProjectUnpackFolder.FullName;
            projectUnpackFolderWatcher.Filter = "";
            // Name filters required for the Renamed event to work
            projectUnpackFolderWatcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName;
            projectUnpackFolderWatcher.IncludeSubdirectories = true;
            projectUnpackFolderWatcher.Changed += ProjectUnpackFolderWatcher_Changed;
            projectUnpackFolderWatcher.Renamed += ProjectUnpackFolderWatcher_Renamed;
            projectUnpackFolderWatcher.Deleted += ProjectUnpackFolderWatcher_Deleted;
            projectUnpackFolderWatcher.Created += ProjectUnpackFolderWatcher_Created;
            projectUnpackFolderWatcher.EnableRaisingEvents = true;
        }

        /// <summary>
        /// Disables the SystemFileWatcher on the project unpack folder.
        /// Should be disabled when internal changes to the files happen.
        /// </summary>
        public void DisableProjectUnpackFolderWatcher()
        {
            if (projectUnpackFolderWatcher != null)
            {
                projectUnpackFolderWatcher.EnableRaisingEvents = false;
            }
        }

        /// <summary>
        /// Enables the SystemFileWatcher on the project unpack folder.
        /// </summary>
        public void EnableProjectUnpackFolderWatcher()
        {
            if (projectUnpackFolderWatcher != null)
            {
                projectUnpackFolderWatcher.EnableRaisingEvents = true;
            }
        }

        private void ProjectUnpackFolderWatcher_Deleted(object sender, FileSystemEventArgs e)
        {
            FileInfo info = new FileInfo(e.FullPath);

            int key = this.AllProjectDataManagers.AssetManager.GetResourceKey(new FileInfo(e.FullPath));
            var resource = this.AllProjectDataManagers.AssetManager.GetResource(key);
            if (resource != null)
            {
                localDispatcher.Invoke(new Action(() =>
                {
                    ResourceDeletedExternally(resource);
                }));
            }
        }

        private void ProjectUnpackFolderWatcher_Created(object sender, FileSystemEventArgs e)
        {
            // Check if the created file already existed and set it to non missing
            var oldUnmanagedFile = nonManagedFiles.FirstOrDefault(x => x.FullName == e.FullPath);
            if (oldUnmanagedFile != null)
            {
                int key = this.AllProjectDataManagers.AssetManager.GetResourceKey(new FileInfo(e.FullPath));
                ResourceFileEntry resource = this.AllProjectDataManagers.AssetManager.GetResource(key) as ResourceFileEntry;
                if (resource != null)
                {
                    localDispatcher.Invoke(new Action(() =>
                    {
                        if (resource is ContainedResourceFileEntry cre)
                        {
                            cre.IsMissing = false;
                        }
                    }));
                }
            }
        }

        private void ProjectUnpackFolderWatcher_Renamed(object sender, RenamedEventArgs e)
        {
            FileInfo info = new FileInfo(e.OldFullPath);
            var unmanagedFile = nonManagedFiles.FirstOrDefault(x => x.FullName == e.OldFullPath);
            if (unmanagedFile != null)
            {
                int key = this.AllProjectDataManagers.AssetManager.GetResourceKey(new FileInfo(e.OldFullPath));
                ResourceFileEntry resource = this.AllProjectDataManagers.AssetManager.GetResource(key) as ResourceFileEntry;
                if (resource != null)
                {
                    localDispatcher.Invoke(new Action(() =>
                    {
                        if (resource is ContainedResourceFileEntry cre)
                        {
                            // Do not rename the resource or change its path, just mark it as missing
                            // This is because some programs do a whole "rename file -> recreate file as temp -> rename temp file to original -> delete backup"
                            // cycle when saving. so when it get renamed back to the original we just set it to not missing again.
                            // If that does not happen it is simply missing.
                            cre.IsMissing = true;
                        }
                    }));
                }
            }

            // check if some filed was renamed back to its original name. Sometimes happen when some external program saves the file.
            var oldUnmanagedFile = nonManagedFiles.FirstOrDefault(x => x.FullName == e.FullPath);
            if (oldUnmanagedFile != null)
            {
                int key = this.AllProjectDataManagers.AssetManager.GetResourceKey(new FileInfo(e.FullPath));
                ResourceFileEntry resource = this.AllProjectDataManagers.AssetManager.GetResource(key) as ResourceFileEntry;
                if (resource != null)
                {
                    localDispatcher.Invoke(new Action(() =>
                    {
                        if (resource is ContainedResourceFileEntry cre)
                        {
                            cre.IsMissing = false;
                        }
                    }));
                }
            }
        }

        private void ResourceChangedExternally(FileInfo resource_file)
        {
            int key = this.AllProjectDataManagers.AssetManager.GetResourceKey(resource_file);
            ResourceFileEntry resource = this.AllProjectDataManagers.AssetManager.GetResource(key) as ResourceFileEntry;
            if (resource != null)
            {
                try
                {
                    FileState.WaitFile(resource_file);
                    localDispatcher.Invoke(new Action(() =>
                    {
                        resource.OnResourceChanged();
                    }));
                }
                catch (TimeoutException)
                {
                    OnFileImportTimeout(resource_file);
                }
            }
        }

        private void ProjectUnpackFolderWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            FileInfo info = new FileInfo(e.FullPath);
            var unmanagedFile = nonManagedFiles.FirstOrDefault(x => x.FullName == e.FullPath);
            if (unmanagedFile != null)
            {
                DateTime lastChange;
                if (projectUnpackFolderLastChange.TryGetValue(e.FullPath, out lastChange))
                {
                    if ((DateTime.Now - lastChange).TotalMilliseconds > 500)
                    {
                        ResourceChangedExternally(new FileInfo(e.FullPath));
                    }
                    projectUnpackFolderLastChange[e.FullPath] = DateTime.Now;
                }
                else
                {
                    ResourceChangedExternally(new FileInfo(e.FullPath));

                    projectUnpackFolderLastChange.Add(e.FullPath, DateTime.Now);
                }

            }
        }

        private void InitializeAssociatedWatchers()
        {
            this.associatedWatchers = new Dictionary<FileSystemWatcher, List<FileInfo>>();
            this.associatedDeletedWatchers = new Dictionary<FileSystemWatcher, List<string>>();
            associatedLastChange = new Dictionary<string, DateTime>();
            foreach (var file in this.associatedFiles)
            {
                this.AddWatcherForAssociatedPath(file);
            }
        }

        private void ReleaseAssociatedWatchers()
        {
            if (this.associatedWatchers != null)
            {
                foreach (var watcher in this.associatedWatchers)
                {
                    watcher.Key.Renamed -= Watcher_Renamed;
                    watcher.Key.Deleted -= Watcher_Deleted;
                    watcher.Key.Changed -= Watcher_Changed;
                    watcher.Key.EnableRaisingEvents = false;
                }
                this.associatedWatchers.Clear();
            }
            if (associatedDeletedWatchers != null)
            {
                foreach (var watcher in this.associatedDeletedWatchers)
                {
                    watcher.Key.Created -= Watcher_Created;
                    watcher.Key.EnableRaisingEvents = false;
                }
                this.associatedDeletedWatchers.Clear();
            }
        }

        private void AddWatcherForAssociatedPath(FileInfo _file)
        {
            if (!_file.Exists) return;
            // add to live watchers
            var duplicate = this.associatedWatchers.FirstOrDefault(x => string.Equals(x.Key.Path, _file.Directory.FullName, StringComparison.InvariantCultureIgnoreCase));
            if (duplicate.Key == null)
            {
                var watcher = new FileSystemWatcher();
                watcher.Path = _file.Directory.FullName;
                watcher.Renamed += Watcher_Renamed;
                watcher.Deleted += Watcher_Deleted;
                watcher.Changed += Watcher_Changed;
                watcher.EnableRaisingEvents = true;
                this.associatedWatchers.Add(watcher, new List<FileInfo> { _file });
            }
            else
            {
                this.associatedWatchers[duplicate.Key].Add(_file);
            }
            // remove from dead watchers
            var found = this.associatedDeletedWatchers.FirstOrDefault(x => string.Equals(x.Key.Path, _file.Directory.FullName, StringComparison.InvariantCultureIgnoreCase));
            if (found.Key != null)
            {
                found.Value.Remove(_file.FullName);
                if (found.Value.Count == 0)
                {
                    found.Key.Created -= Watcher_Created;
                    found.Key.EnableRaisingEvents = false;
                    this.associatedDeletedWatchers.Remove(found.Key);
                }
            }
        }

        private void RemoveWatcherForAssociatedPath(FileInfo _file)
        {
            // remove from the live watchers
            var found = this.associatedWatchers.FirstOrDefault(x => string.Equals(x.Key.Path, _file.Directory.FullName, StringComparison.InvariantCultureIgnoreCase));
            if (found.Key != null)
            {
                found.Value.Remove(_file);
                if (found.Value.Count == 0)
                {
                    found.Key.Changed -= Watcher_Renamed;
                    found.Key.Changed -= Watcher_Changed;
                    found.Key.Deleted -= Watcher_Deleted;
                    found.Key.EnableRaisingEvents = false;
                    this.associatedWatchers.Remove(found.Key);
                }
            }
            // add to dead watchers
            var duplicate = this.associatedDeletedWatchers.FirstOrDefault(x => string.Equals(x.Key.Path, _file.Directory.FullName, StringComparison.InvariantCultureIgnoreCase));
            if (duplicate.Key == null)
            {
                var watcher = new FileSystemWatcher();
                watcher.Path = _file.Directory.FullName;
                watcher.Created += Watcher_Created;
                watcher.EnableRaisingEvents = true;
                this.associatedDeletedWatchers.Add(watcher, new List<string> { _file.FullName });
            }
            else
            {
                this.associatedDeletedWatchers[duplicate.Key].Add(_file.FullName);
            }
        }

        private void Watcher_Changed(object sender, FileSystemEventArgs e)
        {
            FileSystemWatcher watcher = sender as FileSystemWatcher;
            if (watcher == null)
                return;

            // find the correct entry in the lookup
            var found = this.associatedWatchers.FirstOrDefault(x => string.Equals(x.Key.Path, watcher.Path, StringComparison.InvariantCultureIgnoreCase));
            if (found.Key == null)
                return;

            bool watcher_contains = found.Value.TryFirstOrDefault(x => string.Equals(x.FullName, e.FullPath, StringComparison.InvariantCultureIgnoreCase), out var f);
            if (watcher_contains)
            {
                DateTime lastChange;
                if (associatedLastChange.TryGetValue(e.FullPath, out lastChange))
                {
                    if ((DateTime.Now - lastChange).TotalMilliseconds > 500)
                    {
                        ResourceChangedExternally(new FileInfo(e.FullPath));
                    }
                    associatedLastChange[e.FullPath] = DateTime.Now;
                }
                else
                {
                    ResourceChangedExternally(new FileInfo(e.FullPath));

                    associatedLastChange.Add(e.FullPath, DateTime.Now);
                }
            }
        }

        private void Watcher_Renamed(object sender, FileSystemEventArgs e)
        {
            RenamedEventArgs rename = e as RenamedEventArgs;
            if (rename == null)
                return;
            FileSystemWatcher watcher = sender as FileSystemWatcher;
            if (watcher == null)
                return;

            // find the correct entry in the lookup
            var found = this.associatedWatchers.FirstOrDefault(x => string.Equals(x.Key.Path, watcher.Path, StringComparison.InvariantCultureIgnoreCase));
            if (found.Key == null)
                return;

            bool watcher_contains = found.Value.TryFirstOrDefault(x => string.Equals(x.FullName, rename.OldFullPath, StringComparison.InvariantCultureIgnoreCase), out var f);
            if (watcher_contains)
            {
                // communicate to the asset manager
                int key = this.AllProjectDataManagers.AssetManager.GetResourceKey(new FileInfo(rename.OldFullPath));
                LinkedResourceFileEntry resource = this.AllProjectDataManagers.AssetManager.GetResource(key) as LinkedResourceFileEntry;
                if (resource != null)
                {
                    localDispatcher.Invoke(new Action(() =>
                    {
                        this.AllProjectDataManagers.AssetManager.ReLinkLinkedFileEntry(resource, new FileInfo(e.FullPath), true);
                    }));
                }
            }
        }

        private void Watcher_Deleted(object sender, FileSystemEventArgs e)
        {
            FileSystemWatcher watcher = sender as FileSystemWatcher;
            if (watcher == null)
                return;

            // find the correct entry in the lookup
            var found = this.associatedWatchers.FirstOrDefault(x => string.Equals(x.Key.Path, watcher.Path, StringComparison.InvariantCultureIgnoreCase));
            if (found.Key == null)
                return;

            bool watcher_contains = found.Value.TryFirstOrDefault(x => string.Equals(x.FullName, e.FullPath, StringComparison.InvariantCultureIgnoreCase), out var f);
            if (watcher_contains)
            {
                int key = this.AllProjectDataManagers.AssetManager.GetResourceKey(new FileInfo(e.FullPath));
                LinkedResourceFileEntry resource = this.AllProjectDataManagers.AssetManager.GetResource(key) as LinkedResourceFileEntry;
                if (resource != null)
                {
                    localDispatcher.Invoke(new Action(() =>
                    {
                        this.AllProjectDataManagers.AssetManager.UnLinkLinkedFileEntry(resource);
                    }));
                }
            }
        }

        private void Watcher_Created(object sender, FileSystemEventArgs e)
        {
            FileSystemWatcher watcher = sender as FileSystemWatcher;
            if (watcher == null)
                return;

            // find the correct entry in the lookup
            var found = this.associatedDeletedWatchers.FirstOrDefault(x => string.Equals(x.Key.Path, watcher.Path, StringComparison.InvariantCultureIgnoreCase));
            if (found.Key == null)
                return;

            if (found.Value.Contains(e.FullPath))
            {
                int key = this.AllProjectDataManagers.AssetManager.GetKey(e.Name);
                LinkedResourceFileEntry resource = this.AllProjectDataManagers.AssetManager.GetResource(key) as LinkedResourceFileEntry;
                if (resource != null)
                {
                    localDispatcher.Invoke(new Action(() =>
                    {
                        this.AllProjectDataManagers.AssetManager.ReLinkLinkedFileEntry(resource, new FileInfo(e.FullPath));
                    }));
                }
            }
        }

        #endregion



        #region State Changes

        /// <summary>
        /// Loads the metadata, and the public values and components into the data managers. Does not include
        /// the changes to the managed file collection - those have to occur beforehand - e.g. in the <see cref="ManagedFileCollection"/> .Ctor.
        /// </summary>
        internal void Load()
        {
            this.IsLoadingData = true;

            if (this.ManagedFiles != null)
                this.ManagedFiles.OnLoad();

            this.OnProjectLoaded();
            this.IsLoadingData = false;

            this.AllProjectDataManagers.ValueManager.ResetChanges();
            this.AllProjectDataManagers.Components.ResetChanges();
        }

        /// <summary>
        /// Loads the user file to the managed file collection. 
        /// Loads the users into the UsersManager of the managed file collection.
        /// Allows for user authentication to take place.
        /// </summary>
        /// <param name="_user_file">the encrypted file containing user information</param>
        /// <param name="_data_manager">the data manager containing the UsersManager</param>
        /// <param name="_encryption_key">the key for the user file</param>
        internal void PreAuthenticate(FileInfo _user_file, ExtendedProjectData _data_manager, byte[] _encryption_key)
        {
            this.IsLoadingData = true;

            // add user file to the managed files
            this.ManagedFiles.AddFile(_user_file, _data_manager);

            // adapt data manager state
            if (this.ManagedFiles != null)
                this.ManagedFiles.OnPreAuthenticate(_encryption_key);

            this.OnProjectReadyForAuthentication();
            this.IsLoadingData = false;
        }

        /// <summary>
        /// Unloads the user data from the UsersManager and deletes the managed file.
        /// The actual file on disc has to be deleted by the caller.
        /// </summary>
        /// <param name="_user">the authenticated user</param>
        internal void PostAuthenticate(SimUser _user)
        {
            this.OnProjectAuthenticated(_user != null);
        }

        /// <summary>
        /// Can be called after a successful authentication. Unloads the public data from the data managers
        /// and, instead, loads ALL project data into the corresponding containers.
        /// </summary>
        /// <param name="_files">the files containing managed data</param>
        /// <param name="_non_managed_files">the files containing non-managed data (e.g. PDF)</param>
        /// <param name="_conteined_dirs">the flat collection of resource directories</param>
        /// <param name="_data_manager">the data manager</param>
        internal void Open(IEnumerable<FileInfo> _files, IEnumerable<FileInfo> _non_managed_files, IEnumerable<DirectoryInfo> _conteined_dirs,
            ExtendedProjectData _data_manager)
        {
            this.IsLoadingData = true;

            // 1. Reset id provider
            _data_manager.IdGenerator.Reset();


            // 2. stop, if there are no valid files for the opening
            if (_files == null) return;
            if (_files.Count() == 0) return;

            // 3. add the appropriate managed files
            foreach (FileInfo fi in _files)
            {
                if (string.Equals(fi.Extension, ParamStructFileExtensions.FILE_EXT_COMPONENTS_PUBLIC, StringComparison.InvariantCultureIgnoreCase) ||
                    string.Equals(fi.Extension, ParamStructFileExtensions.FILE_EXT_MULTIVALUES_PUBLIC, StringComparison.InvariantCultureIgnoreCase) ||
                    string.Equals(fi.Extension, ParamStructFileExtensions.FILE_EXT_META, StringComparison.InvariantCultureIgnoreCase) ||
                    string.Equals(fi.Extension, ParamStructFileExtensions.FILE_EXT_USERS, StringComparison.InvariantCultureIgnoreCase))
                    continue;


                this.ManagedFiles.AddFile(fi, _data_manager);
            }

            // 4. add the appropriate data managers
            _data_manager.AssetManager.WorkingDirectory = ProjectUnpackFolder.FullName;

            // 5. interconnect the dependent managed files
            if (ManagedFiles.ComponentEntry != null)
                ManagedFiles.ComponentEntry.PublicCounterpart = ManagedFiles.PublicComponentsEntry;
            if (ManagedFiles.ValueEntry != null)
            {
                ManagedFiles.ValueEntry.PublicCounterpart = ManagedFiles.PublicValuesEntry;
                ManagedFiles.ValueEntry.PublicDependence = ManagedFiles.PublicComponentsEntry;
            }

            // 6. fill the data managers with project data
            if (ManagedFiles != null)
                ManagedFiles.OnOpen(true, true, _data_manager);

            //_project.OnProjectOpened();

            // 7. pass the non-managed files and resource directories
            PassNonManagedAndAllDirs(_non_managed_files, _conteined_dirs);

            // 8. synchronize the content of the project files with the resource records
            List<FileInfo> resource_files = new List<FileInfo>();
            resource_files.AddRange(_files.Where(x => x.Extension == ParamStructFileExtensions.FILE_EXT_GEOMAP ||
                                                 x.Extension == ParamStructFileExtensions.FILE_EXT_SITEPLANNER ||
                                                 x.Extension == ParamStructFileExtensions.FILE_EXT_GEOMETRY_INTERNAL));
            resource_files.AddRange(_non_managed_files);
            var synch = AllProjectDataManagers.AssetManager.SynchronizeResources(resource_files, _conteined_dirs, true, true);
            ManagedFiles.SyncWithResources();
            GetAssociatedFiles();

            // done
            OnProjectOpened();
            IsLoadingData = false;
        }

        internal void LoadDataIntoProjectDataManager()
        {
            this.IsLoadingData = true;
            this.OnProjectLoaded();
            this.OnProjectAuthenticated(true);

            // 4. add the appropriate data managers
            this.AllProjectDataManagers.AssetManager.WorkingDirectory = this.ProjectUnpackFolder.FullName;

            // 5. interconnect the dependent managed files
            if (this.ManagedFiles.ComponentEntry != null)
                this.ManagedFiles.ComponentEntry.PublicCounterpart = this.ManagedFiles.PublicComponentsEntry;
            if (this.ManagedFiles.ValueEntry != null)
            {
                this.ManagedFiles.ValueEntry.PublicCounterpart = this.ManagedFiles.PublicValuesEntry;
                this.ManagedFiles.ValueEntry.PublicDependence = this.ManagedFiles.PublicComponentsEntry;
            }

            // 6. fill the data managers with project data
            if (this.ManagedFiles != null)
                this.ManagedFiles.OnOpen(true, true, this.AllProjectDataManagers);

            this.OnProjectOpened();

            this.IsLoadingData = false;
        }

        /// <summary>
        /// Unloads the project data from the respective data managers and loads the public information instead.
        /// </summary>
        /// <param name="_unloading_follows">if True the project will be immediately unloaded after closing</param>
        internal void Close(bool _unloading_follows)
        {
            this.IsUnloadingData = true;

            // 1. empty all data managers
            if (this.ManagedFiles != null)
                this.ManagedFiles.OnPreClose();

            // 2. disconnect the dependent managed files
            this.ManagedFiles.ComponentEntry.PublicCounterpart = null;
            this.ManagedFiles.ValueEntry.PublicCounterpart = null;
            this.ManagedFiles.ValueEntry.PublicDependence = null;

            // 3. remove the unnecessary managed files (leave the meta-data)
            if (this.ManagedFiles.ParameterLibraryEntry != null)
                this.ManagedFiles.RemoveFile(ParamStructFileExtensions.FILE_EXT_PARAMETERS);

            int nr_geom_entries = this.ManagedFiles.GeometryEntries.Count();
            for (int i = 0; i < nr_geom_entries; i++)
            {
                this.ManagedFiles.RemoveFile(ParamStructFileExtensions.FILE_EXT_GEOMETRY_INTERNAL);
            }

            if (this.ManagedFiles.ExcelToolEntry != null)
                this.ManagedFiles.RemoveFile(ParamStructFileExtensions.FILE_EXT_EXCEL_TOOL_COLLECTION);

            if (this.ManagedFiles.ImageLibraryEntry != null)
                this.ManagedFiles.RemoveFile(ParamStructFileExtensions.FILE_EXT_IMAGES);

            if (this.ManagedFiles.ComponentEntry != null)
                this.ManagedFiles.RemoveFile(ParamStructFileExtensions.FILE_EXT_COMPONENTS);

            if (this.ManagedFiles.ValueEntry != null)
                this.ManagedFiles.RemoveFile(ParamStructFileExtensions.FILE_EXT_MULTIVALUES);

            if (this.ManagedFiles.UserFileEntry != null)
                this.ManagedFiles.RemoveFile(ParamStructFileExtensions.FILE_EXT_USERS);

            // 5. fill the data managers with the public data
            if (this.ManagedFiles != null)
                this.ManagedFiles.OnClose(_unloading_follows);

            this.OnProjectClosed();
            this.IsUnloadingData = false;
        }

        /// <summary>
        /// Unloads all project data, including the meta-information, from the data managers.
        /// </summary>
        internal void Unload()
        {
            this.IsUnloadingData = true;

            // 1. unload the data from the data managers
            if (this.ManagedFiles != null)
                this.ManagedFiles.OnUnload();

            // 2. remove the managed files
            if (this.ManagedFiles.PublicValuesEntry != null)
                this.ManagedFiles.RemoveFile(ParamStructFileExtensions.FILE_EXT_MULTIVALUES_PUBLIC);
            if (this.ManagedFiles.PublicComponentsEntry != null)
                this.ManagedFiles.RemoveFile(ParamStructFileExtensions.FILE_EXT_COMPONENTS_PUBLIC);
            if (this.ManagedFiles.MetaDataEntry != null)
                this.ManagedFiles.RemoveFile(ParamStructFileExtensions.FILE_EXT_META);

            // 3. remove all other files
            this.ResetNonManaged();

            this.OnProjectUnloaded();
            this.IsUnloadingData = false;
        }

        #endregion
    }
}
