using SIMULTAN.Data.Assets;
using SIMULTAN.Data.Components;
using SIMULTAN.Data.Users;
using SIMULTAN.Serializer.DXF;
using SIMULTAN.Serializer.Projects;
using SIMULTAN.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Projects.ManagedFiles
{
    /// <summary>
    /// Manages multiple files of different types - e.g., component, value, geometry etc.
    /// </summary>
    public class ManagedFileCollection
    {
        #region FIELDS

        private List<ManagedFile> files;

        /// <summary>
        /// Returns the file extensions that have to be handled both as resources and as managed file.
        /// </summary>
        /// <returns>a collection of extensions</returns>
        private static readonly IEnumerable<string> extensionsToIncludeInManagement = new string[]
        {
            ParamStructFileExtensions.FILE_EXT_GEOMETRY_INTERNAL,
            ParamStructFileExtensions.FILE_EXT_GEOMAP,
            ParamStructFileExtensions.FILE_EXT_SITEPLANNER
        };

        #endregion

        #region .CTOR

        /// <summary>
        /// Instantiates a ManagedFileCollection and assigns the data managers.
        /// </summary>
        /// <param name="_files">the files to manage</param>
        /// <param name="_project_data_manager">holds all data managers in the project</param>
        public ManagedFileCollection(List<FileInfo> _files, ExtendedProjectData _project_data_manager)
        {
            this.files = new List<ManagedFile>();
            if (_files != null)
            {
                foreach (FileInfo fi in _files)
                {
                    if (fi != null && File.Exists(fi.FullName))
                    {
                        ManagedFile created = ManagedFileCollection.CreateFrom(this, fi, _project_data_manager);
                        if (created != null)
                        {
                            created.FileUpToDateChanged -= File_FileUpToDateChanged;
                            created.FileUpToDateChanged += File_FileUpToDateChanged;
                            this.files.Add(created);
                        }
                    }
                }
                this.DiscoverFileTypes();
            }
        }

        #endregion

        #region EVENTS

        /// <summary>
        /// Handler for the <see cref="ManagedFileUpToDateStateChanged"/> event.
        /// </summary>
        /// <param name="sender">the emitting object</param>
        /// <param name="files">the files that raised the event</param>
        /// <param name="all_upToDate">the combined state of all managed files</param>
        public delegate void ManagedFileUpToDateStateChangedEventHandler(object sender, IEnumerable<ManagedFile> files, bool all_upToDate);
        /// <summary>
        /// Emitted when one of the files in the collection changes its UpToDate state.
        /// </summary>
        public event ManagedFileUpToDateStateChangedEventHandler ManagedFileUpToDateStateChanged;
        /// <summary>
        /// Emits the <see cref="ManagedFileUpToDateStateChanged"/> event
        /// </summary>
        /// <param name="files">the managed files that raised the event</param>
        /// <param name="all_upToDate">the combined state of all managed files</param>
        public void OnManagedFileUpToDateStateChanged(IEnumerable<ManagedFile> files, bool all_upToDate)
        {
            this.ManagedFileUpToDateStateChanged?.Invoke(this, files, all_upToDate);
        }


        internal void OnManagedFileRenaming(FileInfo oldName, FileInfo newName)
        {
            //Find managed file
            var affectedFiles = this.Files.Where(x => string.Equals(x.File.FullName, oldName.FullName, StringComparison.InvariantCultureIgnoreCase)).ToList();
            foreach (var file in affectedFiles)
                file.OnRenamed(newName);
        }


        #endregion

        #region METHODS: management

        /// <summary>
        /// Adds the given file to the collection. If it is already contained in it, does nothing.
        /// </summary>
        /// <param name="_file">the file</param>
        /// <param name="projectDataManager">The data managers</param>
        /// <param name="_load_data">if true, immediately open the file and load the data into the appropriate data manager</param>
        public void AddFile(FileInfo _file, ExtendedProjectData projectDataManager, bool _load_data = false)
        {
            if (_file == null) return;
            if (!File.Exists(_file.FullName)) return;

            // check for duplicates
            ManagedFile duplicate = this.files.FirstOrDefault(x => x.File.FullName == _file.FullName);
            if (duplicate != null) return;

            // add
            ManagedFile created = ManagedFileCollection.CreateFrom(this, _file, projectDataManager);
            if (created != null && !(created is ManagedMetaData))
            {
                created.FileUpToDateChanged -= File_FileUpToDateChanged;
                created.FileUpToDateChanged += File_FileUpToDateChanged;
            }
            this.files.Add(created);
            this.DiscoverFileTypes();

            // load
            if (_load_data)
            {
                created.Open(true);
            }
        }

        /// <summary>
        /// Removes the file from the collection. If there is no such file in it, does nothing.
        /// </summary>
        /// <param name="_file">the file</param>
        internal void RemoveFile(FileInfo _file)
        {
            if (_file == null) return;

            // find file to remove
            ManagedFile found = this.files.FirstOrDefault(x => x.File.FullName == _file.FullName);
            if (found == null) return;

            // detach from its respective data manager and event handler
            found.Reset();
            found.FileUpToDateChanged -= File_FileUpToDateChanged;

            // remove
            this.files.Remove(found);
            this.DiscoverFileTypes();
        }

        /// <summary>
        /// Removes the file with the given extension from the collection.  If there is no such file in it, does nothing.
        /// </summary>
        /// <param name="_extension">the file extension w/o point</param>
        public void RemoveFile(string _extension)
        {
            ManagedFile found = this.files.FirstOrDefault(x => string.Equals(x.File.Extension, _extension, StringComparison.InvariantCultureIgnoreCase));
            if (found != null)
                this.RemoveFile(found.File);
        }

        private void DiscoverFileTypes()
        {
            this.ComponentEntry = this.files.FirstOrDefault(x => x is ManagedComponentFile) as ManagedComponentFile;
            this.ComponentEntries = this.files.Where(x => x is ManagedComponentFile).Select(x => x as ManagedComponentFile).ToList();

            this.ValueEntry = this.files.FirstOrDefault(x => x is ManagedValueFile) as ManagedValueFile;
            this.ValueEntries = this.files.Where(x => x is ManagedValueFile).Select(x => x as ManagedValueFile).ToList();

            this.TaxonomyEntry = this.files.FirstOrDefault(x => x is ManagedTaxonomyFile) as ManagedTaxonomyFile;
            this.TaxonomyEntries = this.files.Where(x => x is ManagedTaxonomyFile).Select(x => x as ManagedTaxonomyFile).ToList();

            this.GeometryEntry = this.files.FirstOrDefault(x => x is ManagedGeometryFile) as ManagedGeometryFile;
            this.GeometryEntries = this.files.Where(x => x is ManagedGeometryFile).Select(x => x as ManagedGeometryFile).ToList();

            this.GeometryRelationsEntry = this.files.FirstOrDefault(x => x is ManagedGeometryRelationsFile) as ManagedGeometryRelationsFile;
            this.GeometryRelationsEntries = this.files.Where(x => x is ManagedGeometryRelationsFile).Select(x => x as ManagedGeometryRelationsFile).ToList();

            this.ExcelToolEntry = this.files.FirstOrDefault(x => x is ManagedExcelToolFile) as ManagedExcelToolFile;
            this.ExcelToolEntries = this.files.Where(x => x is ManagedExcelToolFile).Select(x => x as ManagedExcelToolFile).ToList();

            this.ParameterLibraryEntry = this.files.FirstOrDefault(x => x is ManagedParameterFile) as ManagedParameterFile;
            this.ParameterLibraryEntries = this.files.Where(x => x is ManagedParameterFile).Select(x => x as ManagedParameterFile).ToList();

            this.UserFileEntry = this.files.FirstOrDefault(x => x is ManagedUserFile) as ManagedUserFile;
            this.LinksFileEntry = this.files.FirstOrDefault(x => x is ManagedLinksFile) as ManagedLinksFile;
            this.MetaDataEntry = this.files.FirstOrDefault(x => x is ManagedMetaData) as ManagedMetaData;
            this.PublicComponentsEntry = this.files.FirstOrDefault(x => x is ManagedPublicComponentFile) as ManagedPublicComponentFile;
            this.PublicValuesEntry = this.files.FirstOrDefault(x => x is ManagedPublicValueFile) as ManagedPublicValueFile;

            this.GeoMapEntries = this.files.Where(x => x is ManagedGeoMapFile).Select(x => x as ManagedGeoMapFile);
            this.SitePlannerEntries = this.files.Where(x => x is ManagedSitePlannerFile).Select(x => x as ManagedSitePlannerFile);
        }

        #endregion

        #region METHODS: save

        /// <summary>
        /// Traverses all managed files and calls and saves the content of the data manager to the respective file.
        /// </summary>
        /// <param name="_only_changes">if True save only the files whose data managers reported an unsaved change, if False save all</param>
        internal void Save(bool _only_changes)
        {
            foreach (ManagedFile mf in this.Files)
            {
                if (!_only_changes || !mf.IsUpToDate)
                {
                    mf.Save();
                }
            }
        }

        /// <summary>
        /// Gathers all files managed by this instance containing unsaved changes.
        /// </summary>
        /// <returns>a collection of files</returns>
        internal IEnumerable<FileInfo> GetFilesWithUnsavedChanges()
        {
            return this.files.Where(x => !x.IsUpToDate).Select(x => x.File);
        }

        /// <summary>
        /// Saves only the changes to the public component and multivalue files.
        /// </summary>
        /// <param name="_only_changes">if True save only the files whose data managers reported an unsaved change, if False save all</param>
        internal void SavePublic(bool _only_changes)
        {
            if (this.PublicValuesEntry != null)
                this.PublicValuesEntry.Save();
            if (this.PublicComponentsEntry != null)
                this.PublicComponentsEntry.Save();
        }

        #endregion

        #region METHODS: state

        /// <summary>
        /// Checks, if all entries in the collection are valid (i.e., can be opened or saved).
        /// </summary>
        /// <returns>true, if all are valid; false, if at least one is invalid</returns>
        public bool HasValidEntries()
        {
            bool public_valid = true;
            bool private_valid = true;
            foreach (var f in this.files)
            {
                if (f is ManagedComponentFile || f is ManagedValueFile)
                {
                    private_valid &= f.IsValid();
                    continue;
                }
                else if (f is ManagedPublicComponentFile || f is ManagedPublicValueFile)
                {
                    public_valid &= f.IsValid();
                    continue;
                }

                if (!f.IsValid())
                    return false;
            }

            return public_valid || private_valid;
        }

        /// <summary>
        /// Checks, if it contains at least one valid instance of <see cref="ManagedComponentFile"/> and 
        /// one valid instance of <see cref="ManagedValueFile"/>. This is the minimal requirement to define a project.
        /// </summary>
        /// <returns>true, if the managed files exist and are valid; false otherwise</returns>
        public bool FulfillsMinimumForOpenProject()
        {
            bool ok = this.ComponentEntry != null && this.ComponentEntry.IsValid();
            ok &= this.ValueEntry != null && this.ValueEntry.IsValid();
            return ok;
        }

        /// <summary>
        /// Checks, if the managed files are in a valid post-loading state.
        /// </summary>
        /// <returns>True, if they are in a valid state</returns>
        public bool IsInLoadedState()
        {
            bool ok = this.MetaDataEntry != null && this.MetaDataEntry.Data != null;
            ok &= this.PublicComponentsEntry != null && this.PublicComponentsEntry.IsValid();
            ok &= this.PublicValuesEntry != null && this.PublicValuesEntry.IsValid();
            return ok;
        }

        /// <summary>
        /// Checks, if the managed files a in a valid state for authentication to take place.
        /// </summary>
        /// <returns>True, if they are in a valid state</returns>
        public bool IsInPreAuthenticationState()
        {
            return this.IsInLoadedState() && this.UserFileEntry != null && this.UserFileEntry.IsValid();
        }

        /// <summary>
        /// Checks, if the managed files a in a valid state. Users may or may not be loaded.
        /// </summary>
        /// <returns></returns>
        public bool IsInPostAuthenticationState()
        {
            return this.IsInLoadedState();
        }

        #endregion

        #region METHODS: loading data into the data managers

        /// <summary>
        /// Loads only the meta data and the public components and values.
        /// </summary>
        internal void OnLoad()
        {
            // 1. load the project meta data
            if (this.MetaDataEntry != null)
                this.MetaDataEntry.Open(true);
            // 2.load the public values
            if (this.PublicValuesEntry != null)
                this.PublicValuesEntry.Open(true);
            // 3. load the public components
            if (this.PublicComponentsEntry != null)
                this.PublicComponentsEntry.Open(true);
        }

        /// <summary>
        /// Loads the user data into the correct data manager.
        /// </summary>
        internal void OnPreAuthenticate(byte[] _encryption_key)
        {
            // load the project users
            if (this.UserFileEntry != null)
            {
                ProjectIO.ENCR_KEY = _encryption_key;
                this.UserFileEntry.Open(true);
            }
        }

        /// <summary>
        /// Loads all project data into the respective data containers. The user data is not loaded.
        /// </summary>
        /// <param name="_clear_before_open">if True the data managers a cleared before the loading</param>
        /// <param name="_user_defined_visibility_ON">if True the loaded components are filtered according to the visibility settings</param>
        /// <param name="_data_manager">for additional managed files that may need to be created from linked resources</param>
        internal void OnOpen(bool _clear_before_open, bool _user_defined_visibility_ON, ExtendedProjectData _data_manager)
        {
            // 1. load the project meta data, if necessary
            if (this.MetaDataEntry != null && this.MetaDataEntry.Data == null)
                this.MetaDataEntry.Open(_clear_before_open);

            // 2. load the values that are used in parameters, both in the library and in components
            if (this.ValueEntry != null)
                this.ValueEntry.Open(_clear_before_open);

            if (this.TaxonomyEntry != null)
                this.TaxonomyEntry.Open(_clear_before_open);

            // 3a. load the links for the external resources
            if (this.LinksFileEntry == null)
                ProjectIO.CreateMissingLinkFile(this.Project);
            if (this.LinksFileEntry != null)
                this.LinksFileEntry.Open(_clear_before_open);

            // 3. load the components and networks
            if (this.ComponentEntry != null)
                this.ComponentEntry.Open(_clear_before_open);
            // 5. load the mapping rules to excel tools and restore the components' associations to them
            if (this.ExcelToolEntry != null)
                this.ExcelToolEntry.Open(_clear_before_open);
            // 6. load the geometry...
            // transferred to other method...

            // 7. load the parameter library (it has no connection to any of the components, but parameters may reference some of the values)
            if (this.ParameterLibraryEntry != null)
                this.ParameterLibraryEntry.Open(_clear_before_open);

            // 8. load SitePlanner and GeoMap projects
            // GeoMaps have to be loaded before SitePlannerProjects!
            if (GeoMapEntries != null)
                foreach (var gm in GeoMapEntries)
                    gm.Open(_clear_before_open);

            if (SitePlannerEntries != null)
                foreach (var sp in SitePlannerEntries)
                    sp.Open(_clear_before_open);

            if (GeometryRelationsEntry == null)
                ProjectIO.CreateInitialGeometryRelationsFile(this.Project);
            if (GeometryRelationsEntry != null)
                GeometryRelationsEntry.Open(_clear_before_open);

            // 9. check for linked resources of type geometry, site planner or geomaps
            // possible only after loading the component file
            List<FileInfo> additional_files_for_managing = this.Project.AllProjectDataManagers.AssetManager.GetAllResourceFiles(extensionsToIncludeInManagement, false);
            // add the files w/o opening them yet
            foreach (FileInfo f in additional_files_for_managing)
            {
                this.AddFile(f, _data_manager);
            }
        }

        /// <summary>
        /// Unloads the project data from the respective data managers. Resets the caller of the ComponentFactory.
        /// </summary>
        internal void OnPreClose()
        {
            //Unload all geometry models
            using (AccessCheckingDisabler.Disable(Project.AllProjectDataManagers.Components))
            {

                Project.AllProjectDataManagers.SitePlannerManager.ClearRecord();

                // 7. unload the parameter library
                Project.AllProjectDataManagers.ParameterLibraryManager.ParameterRecord.Clear();

                // pre 3. unload the links for linked resources (happens in 3 anyway...)
                Project.AllProjectDataManagers.AssetManager.ResetLinks();

                // 3. unload the components and networks
                Project.AllProjectDataManagers.Components.Clear();
                Project.AllProjectDataManagers.NetworkManager.ClearRecord();
                Project.AllProjectDataManagers.AssetManager.Reset();

                // 2. unload the values
                Project.AllProjectDataManagers.ValueManager.Clear();
                Project.AllProjectDataManagers.ValueManager.SetCallingLocation(null);

                // 1. unload the users
                Project.AllProjectDataManagers.UsersManager.Clear();

                // 0. the metadata is NOT unloaded
            }
        }

        /// <summary>
        ///  Loads the public information instead of the data removed in <see cref="OnPreClose"/> if the
        ///  unloading does not follow immediately
        /// </summary>
        /// <param name="_unloading_follows">if True the project will be immediately unloaded after closing</param>
        internal void OnClose(bool _unloading_follows)
        {
            if (!_unloading_follows)
            {
                // 2a. load the public values
                if (this.PublicValuesEntry != null)
                    this.PublicValuesEntry.Open(true);

                // 3a. load the public components
                if (this.PublicComponentsEntry != null)
                    this.PublicComponentsEntry.Open(true);
            }
        }

        /// <summary>
        /// Unload all remaining data from the data managers.
        /// </summary>
        internal void OnUnload()
        {
            // 3. unload the public components
            using (AccessCheckingDisabler.Disable(Project.AllProjectDataManagers.Components))
            {
                Project.AllProjectDataManagers.Components.Clear();
            }

            Project.AllProjectDataManagers.NetworkManager.ClearRecord();
            Project.AllProjectDataManagers.AssetManager.Reset();

            // 2. unload the public values
            Project.AllProjectDataManagers.ValueManager.Clear();
            Project.AllProjectDataManagers.ValueManager.SetCallingLocation(null);

            // 1. unload the meta information
            if (this.MetaDataEntry != null)
                this.MetaDataEntry.Close();
        }

        #endregion

        #region PROPERIES: files, project

        /// <summary>
        /// Returns a copy of the collection of the managed files.
        /// </summary>
        public List<ManagedFile> Files { get { return new List<ManagedFile>(this.files); } }

        /// <summary>
        /// Holds the parent project of the managed collection.
        /// </summary>
        internal HierarchicalProject Project { get; set; }

        #endregion

        #region PROPERTIES: derived - specific entries

        /// <summary>
        /// Returns the first component file entry or Null.
        /// </summary>
        public ManagedComponentFile ComponentEntry { get; private set; }
        /// <summary>
        /// Returns all component file entries or an empty collection.
        /// </summary>
        public IEnumerable<ManagedComponentFile> ComponentEntries { get; private set; }

        /// <summary>
        /// Returns the first multi-value file entry or Null.
        /// </summary>
        public ManagedValueFile ValueEntry { get; private set; }
        /// <summary>
        /// Returns all multi-value file entries or an empty collection.
        /// </summary>
        public IEnumerable<ManagedValueFile> ValueEntries { get; private set; }

        /// <summary>
        /// Returns the first Taxonomy file entry or null;
        /// </summary>
        public ManagedTaxonomyFile TaxonomyEntry { get; private set; }
        /// <summary>
        /// Returns all taxonomy file entries or an empty collection.
        /// </summary>
        public IEnumerable<ManagedTaxonomyFile> TaxonomyEntries { get; private set; }

        /// <summary>
        /// Returns the first geometry file entry or Null.
        /// </summary>
        public ManagedGeometryFile GeometryEntry { get; private set; }
        /// <summary>
        /// Returns all geometry file entries or an empty collection.
        /// </summary>
        public IEnumerable<ManagedGeometryFile> GeometryEntries { get; private set; }

        /// <summary>
        /// Returns the first geometry file entry or Null.
        /// </summary>
        public ManagedGeometryRelationsFile GeometryRelationsEntry { get; private set; }
        /// <summary>
        /// Returns all geometry file entries or an empty collection.
        /// </summary>
        public IEnumerable<ManagedGeometryRelationsFile> GeometryRelationsEntries { get; private set; }

        /// <summary>
        /// Returns the first excel tool file entry or Null.
        /// </summary>
        public ManagedExcelToolFile ExcelToolEntry { get; private set; }
        /// <summary>
        /// Returns all excel tool file entries or an empty collection.
        /// </summary>
        public IEnumerable<ManagedExcelToolFile> ExcelToolEntries { get; private set; }

        /// <summary>
        /// Returns the first parameter library file entry or Null.
        /// </summary>
        public ManagedParameterFile ParameterLibraryEntry { get; private set; }
        /// <summary>
        /// Returns all parameter library file entries or an empty collection.
        /// </summary>
        public IEnumerable<ManagedParameterFile> ParameterLibraryEntries { get; private set; }

        /// <summary>
        /// Returns the first user file entry or Null.
        /// </summary>
        public ManagedUserFile UserFileEntry { get; private set; }

        /// <summary>
        /// Returns the first links file entry of Null.
        /// </summary>
        public ManagedLinksFile LinksFileEntry { get; private set; }

        /// <summary>
        /// Returns the first metadata file entry or Null.
        /// </summary>
        public ManagedMetaData MetaDataEntry { get; private set; }

        /// <summary>
        /// Returns the first public component file entry.
        /// </summary>
        public ManagedPublicComponentFile PublicComponentsEntry { get; private set; }
        /// <summary>
        /// Returns the first public value file entry.
        /// </summary>
        public ManagedPublicValueFile PublicValuesEntry { get; private set; }

        /// <summary>
        /// Returns all GeoMap file entries.
        /// </summary>
        public IEnumerable<ManagedGeoMapFile> GeoMapEntries { get; private set; }
        /// <summary>
        /// Returns all SitePlanner file entries.
        /// </summary>
        public IEnumerable<ManagedSitePlannerFile> SitePlannerEntries { get; private set; }

        #endregion

        #region QUERIES

        /// <summary>
        /// Returns the managed file corresponding to the given resource index.
        /// </summary>
        /// <param name="_index">the index of the resource</param>
        /// <returns>the file, or null</returns>
        public ManagedFile GetFileWithResourceIndex(int _index)
        {
            return this.files.FirstOrDefault(x => x.CorrespondingResourceIndex == _index);
        }

        #endregion

        #region UTILS: create single managed file, sync

        private static ManagedFile CreateFrom(ManagedFileCollection _owner, FileInfo _file, ExtendedProjectData projectDataManager)
        {
            ManagedFile created = null;
            if (string.Equals(_file.Extension, ParamStructFileExtensions.FILE_EXT_COMPONENTS, StringComparison.InvariantCultureIgnoreCase))
            {
                // CODXF
                created = new ManagedComponentFile(projectDataManager, _owner, _file);
            }
            else if (string.Equals(_file.Extension, ParamStructFileExtensions.FILE_EXT_COMPONENTS_PUBLIC, StringComparison.InvariantCultureIgnoreCase))
            {
                // CODXFP
                created = new ManagedPublicComponentFile(projectDataManager, _owner, _file);
            }
            else if (string.Equals(_file.Extension, ParamStructFileExtensions.FILE_EXT_MULTIVALUES, StringComparison.InvariantCultureIgnoreCase))
            {
                // MVDXF
                created = new ManagedValueFile(projectDataManager, _owner, _file);
            }
            else if (string.Equals(_file.Extension, ParamStructFileExtensions.FILE_EXT_MULTIVALUES_PUBLIC, StringComparison.InvariantCultureIgnoreCase))
            {
                // MVDXFP
                created = new ManagedPublicValueFile(projectDataManager, _owner, _file);
            }
            else if (string.Equals(_file.Extension, ParamStructFileExtensions.FILE_EXT_GEOMETRY_INTERNAL, StringComparison.InvariantCultureIgnoreCase))
            {
                // SIMGEO
                created = new ManagedGeometryFile(projectDataManager, _owner, _file);
            }
            else if (string.Equals(_file.Extension, ParamStructFileExtensions.FILE_EXT_EXCEL_TOOL_COLLECTION, StringComparison.InvariantCultureIgnoreCase))
            {
                // ETDXF
                created = new ManagedExcelToolFile(projectDataManager, _owner, _file);
            }
            else if (string.Equals(_file.Extension, ParamStructFileExtensions.FILE_EXT_PARAMETERS, StringComparison.InvariantCultureIgnoreCase))
            {
                // PADXF
                created = new ManagedParameterFile(projectDataManager, _owner, _file);
            }
            else if (string.Equals(_file.Extension, ParamStructFileExtensions.FILE_EXT_IMAGES, StringComparison.InvariantCultureIgnoreCase))
            {
                // BIN
                // Obsolete
            }
            else if (string.Equals(_file.Extension, ParamStructFileExtensions.FILE_EXT_USERS, StringComparison.InvariantCultureIgnoreCase))
            {
                // USRDXF
                created = new ManagedUserFile(projectDataManager, _owner, _file);
            }
            else if (string.Equals(_file.Extension, ParamStructFileExtensions.FILE_EXT_LINKS, StringComparison.InvariantCultureIgnoreCase))
            {
                //SIMLINKS
                created = new ManagedLinksFile(projectDataManager, _owner, _file);
            }
            else if (string.Equals(_file.Extension, ParamStructFileExtensions.FILE_EXT_META, StringComparison.InvariantCultureIgnoreCase))
            {
                // METADXF
                created = new ManagedMetaData(projectDataManager, _owner, _file);
            }
            else if (string.Equals(_file.Extension, ParamStructFileExtensions.FILE_EXT_GEOMAP, StringComparison.InvariantCultureIgnoreCase))
            {
                // GMDXF
                created = new ManagedGeoMapFile(projectDataManager, _owner, _file);
            }
            else if (string.Equals(_file.Extension, ParamStructFileExtensions.FILE_EXT_SITEPLANNER, StringComparison.InvariantCultureIgnoreCase))
            {
                // SPDXF               
                created = new ManagedSitePlannerFile(projectDataManager, _owner, _file);
            }
            else if (string.Equals(_file.Extension, ParamStructFileExtensions.FILE_EXT_TAXONOMY, StringComparison.InvariantCultureIgnoreCase))
            {
                // TXDXF               
                created = new ManagedTaxonomyFile(projectDataManager, _owner, _file);
            }
            else if (string.Equals(_file.Extension, ParamStructFileExtensions.FILE_EXT_GEOMETRY_RELATIONS, StringComparison.InvariantCultureIgnoreCase))
            {
                // GRDXF               
                created = new ManagedGeometryRelationsFile(projectDataManager, _owner, _file);
            }
            return created;
        }

        /// <summary>
        /// Synchronizes resources with the managed files that are also regarded as resources.
        /// </summary>
        public void SyncWithResources()
        {
            AssetManager asset_manager = Project.AllProjectDataManagers.AssetManager;
            foreach (var file in this.GeometryEntries)
            {
                file.CorrespondingResourceIndex = asset_manager.GetResourceKey(file.File);
            }
            foreach (var file in this.SitePlannerEntries)
            {
                file.CorrespondingResourceIndex = asset_manager.GetResourceKey(file.File);
            }
            foreach (var file in this.GeoMapEntries)
            {
                file.CorrespondingResourceIndex = asset_manager.GetResourceKey(file.File);
            }
        }

        #endregion

        #region EVENT HANDLERS

        private Dictionary<ManagedFile, bool> files_up2date;
        private List<ManagedFile> files_deferred_up2date_notification;

        private void File_FileUpToDateChanged(object sender, bool isUpToDate)
        {
            ManagedFile mf = sender as ManagedFile;
            if (mf == null) return;

            if (isUpToDate)
            {
                // saving in progress...
                if (this.files_up2date == null)
                {
                    this.files_up2date = this.files.ToDictionary(x => x, y => y.IsUpToDate);
                    this.files_deferred_up2date_notification = new List<ManagedFile>();
                }
                else
                {
                    this.files_up2date[mf] = true;
                    this.files_deferred_up2date_notification.Add(mf);
                }

                // wait until all files have sent notifications
                bool all_areUpToDate = this.files_up2date.Select(x => x.Value).Aggregate((x, y) => x & y);
                if (all_areUpToDate)
                {
                    this.OnManagedFileUpToDateStateChanged(new List<ManagedFile>(this.files_deferred_up2date_notification), true);
                    this.files_up2date = null;
                    this.files_deferred_up2date_notification = null;
                }
                else
                    return;
            }
            else
            {
                // notify of presense of unsaved changes
                this.OnManagedFileUpToDateStateChanged(new List<ManagedFile> { mf }, false);
                this.files_up2date = null;
                this.files_deferred_up2date_notification = null;
            }
        }

        #endregion
    }
}
