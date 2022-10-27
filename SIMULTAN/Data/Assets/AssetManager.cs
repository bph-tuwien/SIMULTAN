using SIMULTAN.Data.Components;
using SIMULTAN.Data.Users;
using SIMULTAN.Projects;
using SIMULTAN.Serializer.DXF;
using SIMULTAN.Utils;
using SIMULTAN.Utils.Collections;
using SIMULTAN.Utils.Files;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.Assets
{
    /// <summary>
    /// Manages resources and assets for an entire project.
    /// </summary>
    public class AssetManager
    {
        #region STATIC

        /// <summary>
        /// A reserved string that indicates a missing path.
        /// </summary>
        public const string PATH_NOT_FOUND = "?";
        private static int LAST_KEY = -1;

        #endregion

        #region CLASS MEMBERS

        public ProjectData ProjectData { get; }

        #endregion

        #region PROPERTIES: Search Paths for Resource Files

        /// <summary>
        /// Saves all paths, ordered by priority, for searching for resource files.
        /// </summary>
        public PathsToLinkedResourcesCollection PathsToResourceFiles { get; private set; }

        private void PathsToResourceFiles_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (this.isResetting) return;

            object old_item = (e.OldItems == null) ? null : e.OldItems[0];
            object new_item = (e.NewItems == null) ? null : e.NewItems[0];
            if (e.Action == NotifyCollectionChangedAction.Add && new_item is string)
            {
                // update the linked resource files
                foreach (var entry in this.resource_look_up)
                {
                    if (!entry.Value.Exists && (entry.Value is LinkedResourceFileEntry))
                    {
                        //entry.Value.ReplacePath(_new_file.FullName, true);
                    }
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove && old_item is string)
            {
                // update the linked resource files
                foreach (var entry in this.resource_look_up)
                {
                    if (entry.Value.Exists && (entry.Value is LinkedResourceFileEntry))
                    {
                        if (FileSystemNavigation.IsContainedIn(old_item as string, entry.Value.CurrentFullPath, false))
                        {
                            // the linked file or a folder that contained it was deleted
                            (entry.Value as LinkedResourceFileEntry).SetRelativePathOnDelete();
                        }
                    }
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Replace && new_item is string && old_item is string)
            {
                // update the linked resource files
                foreach (var entry in this.resource_look_up)
                {
                    if (entry.Value.Exists && (entry.Value is LinkedResourceFileEntry))
                    {
                        if (FileSystemNavigation.IsContainedIn(old_item as string, entry.Value.CurrentFullPath, false))
                        {
                            string new_full_path = entry.Value.CurrentFullPath.Replace(old_item as string, new_item as string);
                            this.ReLinkLinkedFileEntry((entry.Value as LinkedResourceFileEntry), new FileInfo(new_full_path));
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Indicates that the asset manager is being reset (e.g. all delete actions should not be propagated to the objects it contains).
        /// </summary>
        private bool isResetting = false;

        #endregion

        #region PROPERTIES: Working directory

        /// <summary>
        /// The directory containing the *.master file or the temporary directory in which a project is unpacked´.
        /// </summary>
        public string WorkingDirectory
        {
            get { return this.working_directory; }
            set
            {
                if (this.working_directory != value)
                {
                    if (!string.IsNullOrEmpty(value) && Directory.Exists(value))
                    {
                        var old_value = this.working_directory;
                        this.working_directory = value;
                        this.PathsToResourceFiles.ForbiddenFolder = this.working_directory;
                        foreach (var entry in this.resource_look_up)
                        {
                            entry.Value.AdaptPathToWorkingDirectory(this.working_directory);
                        }
                    }
                }
            }
        }
        private string working_directory;

        #endregion

        #region COLLECTION PROPERTIES: Resources

        /// <summary>
        /// The collection of all top-level resource entries.
        /// </summary>
        public ReadOnlyObservableCollection<ResourceEntry> Resources { get { return resourcesReadOnly; } }
        /// <summary>
        /// Synchronizes with the Resources property.
        /// </summary>
		protected ElectivelyObservableCollection<ResourceEntry> Resources_Internal
        {
            get { return resources; }
            set
            {
                resources = value;
                resourcesReadOnly = new ReadOnlyObservableCollection<ResourceEntry>(value);
            }
        }

        private ElectivelyObservableCollection<ResourceEntry> resources;
        private ReadOnlyObservableCollection<ResourceEntry> resourcesReadOnly;

        private void Resources_ElectiveCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (var item in e.NewItems.OfType<ResourceEntry>())
                {
                    this.AddResourceToLookup(item);
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (var item in e.OldItems.OfType<ResourceEntry>())
                {
                    this.RemoveResourceFromLookup(item);
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Replace)
            {
                foreach (var item in e.OldItems.OfType<ResourceEntry>())
                {
                    this.RemoveResourceFromLookup(item);
                }
                foreach (var item in e.NewItems.OfType<ResourceEntry>())
                {
                    this.AddResourceToLookup(item);
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                if (e.OldItems != null)
                {
                    foreach (var item in e.OldItems.OfType<ResourceEntry>())
                    {
                        this.RemoveResourceFromLookup(item);
                    }
                }
                if (e.NewItems != null)
                {
                    foreach (var item in e.NewItems.OfType<ResourceEntry>())
                    {
                        this.AddResourceToLookup(item);
                    }
                }
            }
        }

        private void DirectoryChildren_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            object old_item = e.OldItems?[0];
            object new_item = e.NewItems?[0];
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (var item in e.NewItems.OfType<ResourceEntry>())
                {
                    this.AddResourceToLookup(item);

                    //var item_childen = (item is ResourceDirectoryEntry) ? new List<ResourceEntry>((item as ResourceDirectoryEntry).Children) : new List<ResourceEntry>();
                    //if (item_childen.Count > 0)
                    //    this.OnChildResourceCollectionChanged(new NotifyCollectionChangedEventArgs(e.Action, item_childen));
                    this.OnChildResourceCollectionChanged(new NotifyCollectionChangedEventArgs(e.Action, item));
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove && old_item is ResourceEntry)
            {
                foreach (var item in e.OldItems.OfType<ResourceEntry>())
                {
                    this.RemoveResourceFromLookup(item);

                    //var item_childen = (item is ResourceDirectoryEntry) ? new List<ResourceEntry>((item as ResourceDirectoryEntry).Children) : new List<ResourceEntry>();
                    //if (item_childen.Count > 0)
                    //    this.OnChildResourceCollectionChanged(new NotifyCollectionChangedEventArgs(e.Action, item_childen));
                    this.OnChildResourceCollectionChanged(new NotifyCollectionChangedEventArgs(e.Action, item));
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Replace && new_item is ResourceEntry && old_item is ResourceEntry)
            {
                foreach (var item in e.OldItems.OfType<ResourceEntry>())
                {
                    this.RemoveResourceFromLookup(item);
                    this.OnChildResourceCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item));
                }
                foreach (var item in e.NewItems.OfType<ResourceEntry>())
                {
                    this.AddResourceToLookup(item);
                    this.OnChildResourceCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item));
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                if (e.OldItems != null)
                {
                    foreach (var item in e.OldItems.OfType<ResourceEntry>())
                    {
                        this.RemoveResourceFromLookup(item);
                        this.OnChildResourceCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item));
                    }
                }
                if (e.NewItems != null)
                {
                    foreach (var item in e.NewItems.OfType<ResourceEntry>())
                    {
                        this.AddResourceToLookup(item);
                        this.OnChildResourceCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item));
                    }
                }
            }
        }

        private void RemoveResourceFromLookup(ResourceEntry _item)
        {
            List<int> contained_keys = new List<int>();
            if (_item is ResourceDirectoryEntry dir)
            {
                contained_keys = dir.GetContainedKeys();
                dir.Children.CollectionChanged -= DirectoryChildren_CollectionChanged;
            }
            this.resource_look_up.Remove(_item.Key);
            foreach (int key in contained_keys)
                this.resource_look_up.Remove(key);
            this.OnUpToDate();
        }

        private void AddResourceToLookup(ResourceEntry _item, bool isRootCaller = true)
        {
            if (!this.resource_look_up.ContainsKey(_item.Key))
                this.resource_look_up.Add(_item.Key, _item);

            if (_item is ResourceDirectoryEntry dir)
            {
                dir.Children.CollectionChanged += DirectoryChildren_CollectionChanged;
                foreach (var item in dir.Children)
                    AddResourceToLookup(item, false);
            }

            if (isRootCaller)
                this.OnUpToDate();
        }

        /// <summary>
        /// A derived dictionary of all top-level resource entries.
        /// </summary>
        private Dictionary<int, ResourceEntry> resource_look_up;

        /// <summary>
        /// Derived property: gets a list of all unresolved linked resource entries.
        /// </summary>
        public List<LinkedResourceFileEntry> MissingLinkedResources
        {
            get
            {
                var missing = this.resource_look_up.Where(x => x.Value is LinkedResourceFileEntry && !x.Value.Exists).Select(x => x.Value as LinkedResourceFileEntry).ToList();
                return missing;
            }
        }

        #endregion

        #region COLLECTION PROPERTIES: Assets

        /// <summary>
        /// A look-up of all assets organizes according to resource they refer to.
        /// </summary>
        public Dictionary<int, ElectivelyObservableCollection<Asset>> Assets { get; private set; }

        #endregion

        #region EVENTS: management

        public delegate void UpToDateEventHandler(object sender);

        public event UpToDateEventHandler UpToDate;

        public void OnUpToDate()
        {
            this.UpToDate?.Invoke(this);
        }


        /// <summary>
        /// Handler for the ChildResourceCollectionChanged event.
        /// </summary>
        /// <param name="sender">object which emitted the event</param>
        /// <param name="args">information about the event</param>
        public delegate void ChildResourceCollectionChangedEventHandler(object sender, NotifyCollectionChangedEventArgs args);
        /// <summary>
        /// Emitted when the children of a resource were changed.
        /// </summary>
        public event ChildResourceCollectionChangedEventHandler ChildResourceCollectionChanged;
        /// <summary>
        /// Emits the ChildResourceCollectionChanged event.
        /// </summary>
        /// <param name="args">information about the event</param>
        public void OnChildResourceCollectionChanged(NotifyCollectionChangedEventArgs args)
        {
            this.ChildResourceCollectionChanged?.Invoke(this, args);
        }

        /// <summary>
        /// Handler for the ResourceRenamed event.
        /// </summary>
        /// <param name="sender">object which emitted the vent</param>
        /// <param name="oldName">the old name</param>
        /// <param name="newName">the new name</param>
        public delegate void ResourceRenamedEventHandler(object sender, string oldName, string newName);
        /// <summary>
        /// Emitted when a resource changes its name.
        /// </summary>
        public event ResourceRenamedEventHandler ResourceRenamed;
        /// <summary>
        /// Emits the ResourceRenamed event.
        /// </summary>
        /// <param name="oldName">the old name</param>
        /// <param name="newName">the new name</param>
        public void OnResourceRenamed(string oldName, string newName)
        {
            this.ResourceRenamed?.Invoke(this, oldName, newName);
        }

        public event PropertyChangedEventHandler ResourcePropertyChanged;
        internal void NotifyResourcePropertyChanged(ResourceEntry entry, [CallerMemberName] string propertyName = null)
        {
            ResourcePropertyChanged?.Invoke(entry, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region .CTOR, Reset

        /// <summary>
        /// Initializes the asset manager for the given component factory.
        /// </summary>
        /// <param name="projectData">The project data this manager belongs to</param>
        public AssetManager(ProjectData projectData)
        {
            if (projectData == null)
                throw new ArgumentNullException(nameof(projectData));

            this.ProjectData = projectData;
            this.PathsToResourceFiles = new PathsToLinkedResourcesCollection(this.WorkingDirectory ?? AssetManager.PATH_NOT_FOUND);
            this.PathsToResourceFiles.CollectionChanged += PathsToResourceFiles_CollectionChanged;
            this.Resources_Internal = new ElectivelyObservableCollection<ResourceEntry>();
            this.Resources_Internal.ElectiveCollectionChanged += Resources_ElectiveCollectionChanged;
            this.resource_look_up = new Dictionary<int, ResourceEntry>();
            this.Assets = new Dictionary<int, ElectivelyObservableCollection<Asset>>();
        }

        /// <summary>
        /// Clears the internal containers before a reload of the component factory.
        /// </summary>
        public void Reset()
        {
            this.Assets.Clear();
            foreach (var entry in this.Resources)
            {
                this.RemoveResourceFromLookup(entry);
            }
            this.Resources_Internal.Clear();
            this.PathsToResourceFiles.Clear();
        }

        #endregion

        #region LEGACY PARSING: resources (undifferentiated) - - - CLEAN-UP? (4 unused methods)

        private Dictionary<long, List<Asset>> tmp_parsed_asset_record;

        internal void AddParsedUndifferentiatedResourceEntry(SimUserRole _user, string _rel_path, string _full_path, int _key, bool _is_contained)
        {
            AssetManager.LAST_KEY = Math.Max(AssetManager.LAST_KEY, _key);
            this.Resources_Internal.SuppressNotification = true;

            ResourceEntry re = null;
            //var correction = GetCorrectedPath(_rel_path, _full_path, _is_contained);
            var check1 = this.CorrectPathInput(_full_path, _rel_path, true); // test1
            var check2 = this.CorrectPathInput(_full_path, _rel_path, false); // test2

            if (check1.corrected_full.EndsWith(Path.DirectorySeparatorChar.ToString()) || check1.corrected_full.EndsWith(Path.AltDirectorySeparatorChar.ToString()))
                re = new ResourceDirectoryEntry(this, _user, check1.corrected_full, true, _key);
            else
            {
                if (check1.consistent && check1.corrected_is_contained.HasValue && check1.corrected_is_contained.Value)
                    re = new ContainedResourceFileEntry(this, _user, check1.corrected_full, true, _key);
                else
                {
                    if (check2.consistent && check2.corrected_is_contained.HasValue && !check2.corrected_is_contained.Value)
                        re = new LinkedResourceFileEntry(this, _user, check2.corrected_full, true, _key);
                    else
                        re = new LinkedResourceFileEntry(this, _user, check2.corrected_relative, false, _key);
                }

            }

            this.Resources_Internal.Add(re);
            if (!this.resource_look_up.ContainsKey(re.Key))
                this.resource_look_up.Add(re.Key, re);
            this.Resources_Internal.SuppressNotification = false;
        }

        #endregion

        #region PARSING: resources according to type


        internal void AddParsedResource(ResourceEntry resource)
        {
            this.Resources_Internal.SuppressNotification = true;
            this.Resources_Internal.Add(resource);

            AddResourceToLookup(resource);
            
            this.OnUpToDate();

            this.Resources_Internal.SuppressNotification = false;
        }

        internal ResourceDirectoryEntry ParseResourceDirectoryEntry(SimUserRole _user, string _rel_path, int _key, 
            SimComponentVisibility visibility)
        {
            var path = this.CorrectPathInput(AssetManager.PATH_NOT_FOUND, _rel_path, true);

            AssetManager.LAST_KEY = Math.Max(AssetManager.LAST_KEY, _key);
            this.Resources_Internal.SuppressNotification = true;

            ResourceDirectoryEntry rde = new ResourceDirectoryEntry(this, _user, path.corrected_full, true, _key, true)
            {
                Visibility = visibility
            };

            this.Resources_Internal.SuppressNotification = false;

            return rde;
        }

        internal ContainedResourceFileEntry ParseContainedResourceFileEntry(SimUserRole _user, string _rel_path, int _key,
            SimComponentVisibility visibility)
        {
            // bool consistent, string corrected_full, string corrected_relative, bool? corrected_is_contained
            var check = this.CorrectPathInput(AssetManager.PATH_NOT_FOUND, _rel_path, true);

            AssetManager.LAST_KEY = Math.Max(AssetManager.LAST_KEY, _key);
            this.Resources_Internal.SuppressNotification = true;

            ContainedResourceFileEntry cre = new ContainedResourceFileEntry(this, _user, check.corrected_full, true, _key, true)
            {
                Visibility = visibility
            };
            this.Resources_Internal.SuppressNotification = false;

            return cre;
        }

        internal LinkedResourceFileEntry ParseLinkedResourceFileEntry(SimUserRole _user, string _rel_path, int _key,
            SimComponentVisibility visibility)
        {
            var check = this.CorrectPathInput(AssetManager.PATH_NOT_FOUND, _rel_path, false);

            AssetManager.LAST_KEY = Math.Max(AssetManager.LAST_KEY, _key);
            this.Resources_Internal.SuppressNotification = true;

            string path = check.corrected_full;
            bool path_is_absolute = true;
            if (string.IsNullOrEmpty(path) || path == AssetManager.PATH_NOT_FOUND)
            {
                path = check.corrected_relative;
                path_is_absolute = false;
            }
            LinkedResourceFileEntry lre = new LinkedResourceFileEntry(this, _user, path, path_is_absolute, _key)
            {
                Visibility = visibility
            };


            this.Resources_Internal.SuppressNotification = false;

            return lre;
        }


        internal ResourceDirectoryEntry AddParsedResourceDirectoryEntry(SimUserRole _user, string _rel_path, string _full_path, int _key, bool _add_to_record, bool _check_for_existence = true)
        {
            // check and correct the input, if necessary           
            bool rel_is_invalid = string.IsNullOrEmpty(_rel_path) || _rel_path == AssetManager.PATH_NOT_FOUND;
            bool full_is_invalid = string.IsNullOrEmpty(_full_path) || _full_path == AssetManager.PATH_NOT_FOUND;
            if (rel_is_invalid && full_is_invalid) return null;
            // bool consistent, string corrected_full, string corrected_relative, bool? corrected_is_contained
            var check = (_check_for_existence) ? this.CorrectPathInput(_full_path, _rel_path, true) : (true, _full_path, _rel_path, null);

            AssetManager.LAST_KEY = Math.Max(AssetManager.LAST_KEY, _key);
            this.Resources_Internal.SuppressNotification = true;

            ResourceDirectoryEntry rde = (_check_for_existence) ? new ResourceDirectoryEntry(this, _user, check.corrected_full, true, _key, _check_for_existence) :
                                                                  new ResourceDirectoryEntry(this, _user, check.corrected_relative, false, _key, _check_for_existence);

            if (_add_to_record)
            {
                this.Resources_Internal.Add(rde);
                rde.Children.CollectionChanged += DirectoryChildren_CollectionChanged;
                this.resource_look_up.Add(rde.Key, rde);
                this.OnUpToDate();
            }

            this.Resources_Internal.SuppressNotification = false;

            return rde;
        }

        internal (ContainedResourceFileEntry parsed, LinkedResourceFileEntry alternative) AddParsedContainedResourceFileEntry(SimUserRole _user, string _rel_path, string _full_path, int _key, bool _add_to_record, bool _check_for_existence = true)
        {
            // check and correct the input, if necessary
            bool rel_is_invalid = string.IsNullOrEmpty(_rel_path) || _rel_path == AssetManager.PATH_NOT_FOUND;
            bool full_is_invalid = string.IsNullOrEmpty(_full_path) || _full_path == AssetManager.PATH_NOT_FOUND;
            if (rel_is_invalid && full_is_invalid) return (null, null);
            // bool consistent, string corrected_full, string corrected_relative, bool? corrected_is_contained
            var check = (_check_for_existence) ? this.CorrectPathInput(_full_path, _rel_path, true) : (true, _full_path, _rel_path, null);

            // for artefacts from earlier versions, which were actually linked but were saved as contained
            if (_check_for_existence && (!check.corrected_is_contained.HasValue || !check.corrected_is_contained.Value))
            {
                // REDIRECT -->
                return (null, this.AddParsedLinkedResourceFileEntry(_user, _rel_path, _full_path, _key, _add_to_record, _check_for_existence).parsed);
            }

            AssetManager.LAST_KEY = Math.Max(AssetManager.LAST_KEY, _key);
            this.Resources_Internal.SuppressNotification = true;

            ContainedResourceFileEntry cre = (_check_for_existence) ? new ContainedResourceFileEntry(this, _user, check.corrected_full, true, _key, _check_for_existence) :
                                                                      new ContainedResourceFileEntry(this, _user, check.corrected_relative, false, _key, _check_for_existence);
            if (_add_to_record)
            {
                this.Resources_Internal.Add(cre);
                this.resource_look_up.Add(cre.Key, cre);
                this.OnUpToDate();
            }

            this.Resources_Internal.SuppressNotification = false;

            return (cre, null);
        }

        internal (LinkedResourceFileEntry parsed, ContainedResourceFileEntry alternative) AddParsedLinkedResourceFileEntry(SimUserRole _user, string _rel_path, string _full_path, int _key, bool _add_to_record, bool _check_for_existence = true)
        {
            // check and correct the input, if necessary
            bool rel_is_invalid = string.IsNullOrEmpty(_rel_path) || _rel_path == AssetManager.PATH_NOT_FOUND;
            bool full_is_invalid = string.IsNullOrEmpty(_full_path) || _full_path == AssetManager.PATH_NOT_FOUND;
            if (rel_is_invalid && full_is_invalid) return (null, null);
            var check = this.CorrectPathInput(_full_path, _rel_path, false);

            // for artefacts from earlier versions, which are actually contained but were saved as linked
            if (_check_for_existence && (!check.corrected_is_contained.HasValue || check.corrected_is_contained.Value))
            {
                var check1 = this.CorrectPathInput(_full_path, _rel_path, true);
                // REDIRECT -->
                if (check1.consistent && check1.corrected_is_contained.HasValue && check1.corrected_is_contained.Value && File.Exists(check1.corrected_full))
                    return (null, this.AddParsedContainedResourceFileEntry(_user, _rel_path, _full_path, _key, _add_to_record, _check_for_existence).parsed);
            }

            AssetManager.LAST_KEY = Math.Max(AssetManager.LAST_KEY, _key);
            this.Resources_Internal.SuppressNotification = true;

            string path = check.corrected_full;
            bool path_is_absolute = true;
            if (string.IsNullOrEmpty(path) || path == AssetManager.PATH_NOT_FOUND)
            {
                path = check.corrected_relative;
                path_is_absolute = false;
            }
            LinkedResourceFileEntry lre = new LinkedResourceFileEntry(this, _user, path, path_is_absolute, _key);
            if (_add_to_record)
            {
                this.Resources_Internal.Add(lre);
                this.resource_look_up.Add(lre.Key, lre);
                this.OnUpToDate();
            }

            this.Resources_Internal.SuppressNotification = false;

            return (lre, null);
        }


        /// <summary>
        /// Makes sure the resource files and directories are properly organised. Post-processing for resources that were not saved as objects.
        /// </summary>
        internal void OrganizeResourceFileEntries()
        {
            // split the entries
            List<ResourceDirectoryEntry> dirs = new List<ResourceDirectoryEntry>();
            List<ResourceFileEntry> files = new List<ResourceFileEntry>();
            foreach (var re in this.resource_look_up)
            {
                if (re.Value is ResourceDirectoryEntry)
                    dirs.Add(re.Value as ResourceDirectoryEntry);
                else
                    files.Add(re.Value as ResourceFileEntry);
            }

            // organise
            List<ResourceEntry> to_remove_from_main_record = new List<ResourceEntry>();
            foreach (ResourceDirectoryEntry d in dirs)
            {
                if (!d.Exists) continue;

                bool was_added_to_container = this.PlaceResourceEntryInHierarchy(d);
                if (was_added_to_container)
                    to_remove_from_main_record.Add(d);
            }
            foreach (ResourceFileEntry f in files)
            {
                if (!f.Exists) continue;

                bool was_added_to_container = this.PlaceResourceEntryInHierarchy(f);
                if (was_added_to_container)
                    to_remove_from_main_record.Add(f);
            }
            foreach (var item in to_remove_from_main_record)
            {
                this.Resources_Internal.Remove(item);
            }
        }

        /// <summary>
        /// Finds the correct container for the given resource entry and places it as a child of that container.
        /// </summary>
        /// <param name="_entry">the entry to be placed</param>
        /// <returns>true, if there was a suitable container; flase otherwise</returns>
        private bool PlaceResourceEntryInHierarchy(ResourceEntry _entry)
        {
            DirectoryInfo entry_dir = null;
            if (_entry is ResourceDirectoryEntry)
                entry_dir = new DirectoryInfo(_entry.CurrentFullPath);
            else
            {
                if (_entry.CurrentFullPath == "?")
                {
                    entry_dir = new DirectoryInfo(this.WorkingDirectory);
                    Console.WriteLine("BS to GP: Please fix non-existing resources. They crash CG and RM");
                }
                else
                    entry_dir = new FileInfo(_entry.CurrentFullPath).Directory;
            }

            foreach (var re in this.resource_look_up)
            {
                if (_entry.Key == re.Key)
                    continue;
                if (re.Value is ResourceDirectoryEntry)
                {
                    string dir_path = re.Value.CurrentFullPath;
                    bool is_contained = FileSystemNavigation.IsSubdirectoryOf(dir_path, entry_dir.FullName) || string.Equals(dir_path, entry_dir.FullName, StringComparison.InvariantCultureIgnoreCase);
                    if (is_contained)
                    {
                        ResourceDirectoryEntry container = (re.Value as ResourceDirectoryEntry).DeepestContainingEntry(entry_dir);
                        container.Children.Add(_entry);
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Necessary due to the late attachment of the event handlers for the CollectionChanged event in nested ResourceDirectory entries.
        /// </summary>
        internal void SyncLookupAfterLoading()
        {
            foreach (ResourceEntry re in this.Resources)
            {
                SyncLookupAfterLoading(re);
            }
        }

        private void SyncLookupAfterLoading(ResourceEntry _re)
        {
            if (!this.resource_look_up.ContainsKey(_re.Key))
                this.resource_look_up.Add(_re.Key, _re);

            if (_re is ResourceDirectoryEntry)
            {
                foreach (var child in (_re as ResourceDirectoryEntry).Children)
                {
                    this.SyncLookupAfterLoading(child);
                }
            }
        }

        #endregion

        #region PARSING: assets

        internal GeometricAsset AddParsedGeometricAsset(IEnumerable<long> _caller_ids, int _path_code_to_asset, string _id)
        {
            GeometricAsset ga = new GeometricAsset(this, _caller_ids, _path_code_to_asset, _id);
            this.AddAssetToInternalContainers(ga, _caller_ids);
            return ga;
        }

        internal DocumentAsset AddParsedDocumentAsset(IEnumerable<long> _caller_ids, int _path_code_to_asset, string _id)
        {
            DocumentAsset da = new DocumentAsset(this, _caller_ids, _path_code_to_asset, _id);
            this.AddAssetToInternalContainers(da, _caller_ids);
            return da;
        }

        private void AddAssetToInternalContainers(Asset _a, IEnumerable<long> _caller_ids)
        {
            // fill in the lookup table
            if (this.Assets == null)
            {
                this.Assets = new Dictionary<int, ElectivelyObservableCollection<Asset>>();
            }

            if (this.Assets.ContainsKey(_a.ResourceKey))
            {
                this.Assets[_a.ResourceKey].SuppressNotification = true;
                this.Assets[_a.ResourceKey].Add(_a);
                this.Assets[_a.ResourceKey].SuppressNotification = false;
            }
            else
            {
                var asset_list = new ElectivelyObservableCollection<Asset> { _a };
                this.Assets.Add(_a.ResourceKey, asset_list);
            }

            // fill in the temporary record for parsing
            if (this.tmp_parsed_asset_record == null)
                this.tmp_parsed_asset_record = new Dictionary<long, List<Asset>>();

            foreach (long id in _caller_ids)
            {
                if (this.tmp_parsed_asset_record.ContainsKey(id))
                    this.tmp_parsed_asset_record[id].Add(_a);
                else
                    this.tmp_parsed_asset_record.Add(id, new List<Asset> { _a });
            }
        }

        internal void RestoreAssetsToComponent(SimComponent _comp)
        {
            if (this.tmp_parsed_asset_record == null) return;
            if (!this.tmp_parsed_asset_record.ContainsKey(_comp.Id.LocalId)) return;

            _comp.ReferencedAssets_Internal.AddRange(this.tmp_parsed_asset_record[_comp.Id.LocalId]);
        }

        internal void ReleaseTmpParseRecord()
        {
            this.tmp_parsed_asset_record = null;
        }

        #endregion

        #region RESOURCE Management: UTILS


        /// <summary>
        /// Adds a resource to the manager via the user interface.
        /// </summary>
        /// <param name="file">The resource file</param>
        /// <returns>The key assigned to the resource, which is unique within the project</returns>
        public int AddResourceEntry(FileInfo file)
        {

            var preconditions = this.CanAddResourceEntry(file.FullName);
            if (preconditions.duplicate_key > -1)
            {
                // if the resource already exists, return its key
                return preconditions.duplicate_key;
            }
            else
            {
                // create the resource
                AssetManager.LAST_KEY++;
                this.AddResourceEntry(file.FullName, preconditions.is_file, preconditions.is_contained, preconditions.is_properly_linked, AssetManager.LAST_KEY);
                return AssetManager.LAST_KEY;
            }
        }

        public void AddResourceEntry(ResourceFileEntry file, ResourceDirectoryEntry parent)
        {
            if (file is LinkedResourceFileEntry)
            {
                if (parent != null)
                    parent.Children.Add(file);
                else
                    this.Resources_Internal.Add(file);
            }
            else //Contained
            {
                bool attached_as_child = this.PlaceResourceEntryInHierarchy(file);
                if (!attached_as_child)
                    this.Resources_Internal.Add(file);
            }
        }
        public void AddResourceEntry(ResourceDirectoryEntry directory)
        {
            bool attached_as_child = this.PlaceResourceEntryInHierarchy(directory);
            if (!attached_as_child)
                this.Resources_Internal.Add(directory);
        }

        /// <summary>
        /// Checks if the given path exists, where it is located and if there are duplicate resources.
        /// </summary>
        /// <param name="_full_path_to_resource">the path to be checked</param>
        /// <returns>the result from the check</returns>
        /// <exception cref="ArgumentException">if the path does not exist</exception>
        internal (bool is_file, bool is_contained, bool is_properly_linked, int duplicate_key) CanAddResourceEntry(string _full_path_to_resource)
        {
            // check if the path is valid
            bool is_file = File.Exists(_full_path_to_resource);
            bool is_dir = Directory.Exists(_full_path_to_resource);
            if (!is_file && !is_dir)
                throw new ArgumentException("Invalid path to resource!");

            // check if the file is contained in the working directory
            bool is_contained = false;
            bool is_properly_linked = false;
            if (is_file)
            {
                FileInfo fi = new FileInfo(_full_path_to_resource);
                DirectoryInfo di = fi.Directory;
                is_contained = FileSystemNavigation.IsSubdirectoryOf(this.working_directory, di.FullName) || string.Equals(this.working_directory, di.FullName, StringComparison.InvariantCultureIgnoreCase);
                if (!is_contained)
                {
                    // check if the file is contained in a valid fallback path
                    foreach (var path in this.PathsToResourceFiles)
                    {
                        if (!string.IsNullOrEmpty(path))
                            continue;
                        is_properly_linked = FileSystemNavigation.IsSubdirectoryOf(path, di.FullName, false) || string.Equals(path, di.FullName, StringComparison.InvariantCultureIgnoreCase);
                        if (is_properly_linked)
                            break;
                    }
                }
            }
            else
            {
                DirectoryInfo di = new DirectoryInfo(_full_path_to_resource);
                is_contained = FileSystemNavigation.IsSubdirectoryOf(this.working_directory, di.FullName) || string.Equals(this.working_directory, di.FullName, StringComparison.InvariantCultureIgnoreCase);
                if (!is_contained)
                {
                    // check if the folder is in a valid fallback path
                    foreach (var path in this.PathsToResourceFiles)
                    {
                        if (!string.IsNullOrEmpty(path))
                            continue;
                        is_properly_linked = FileSystemNavigation.IsSubdirectoryOf(path, di.FullName, false) || string.Equals(path, di.FullName, StringComparison.InvariantCultureIgnoreCase);
                        if (is_properly_linked)
                            break;
                    }
                }
            }

            // check for duplicates
            var duplicate = this.resource_look_up.FirstOrDefault(x => x.Value.CurrentFullPath != AssetManager.PATH_NOT_FOUND && x.Value.CurrentFullPath == _full_path_to_resource);
            int duplicate_key = (duplicate.Value == null) ? -1 : duplicate.Key;

            return (is_file, is_contained, is_properly_linked, duplicate_key);
        }

        /// <summary>
        /// The main method for adding resources and updating the resource hierarchy. 
        /// Can check if the resource path is valid. It triggers all the internal housekeeping.
        /// </summary>
        /// <param name="_full_path_to_resource">the path to the resource</param>
        /// <param name="_is_file">true in case of a file, false in case of a folder</param>
        /// <param name="_is_contained">true in case of containment in the working directory, false otherwise</param>
        /// <param name="_is_properly_linked">true if there is a fallback folder containing the given path, false otherwise</param>
        /// <param name="_key">the resource key</param>
        /// <param name="_direct_parent">the parent folder, can be null which indicates the working directory</param>
        private ResourceEntry AddResourceEntry(string _full_path_to_resource, bool _is_file, bool _is_contained, bool _is_properly_linked, int _key, DirectoryInfo _direct_parent = null)
        {
            // check, if adding a fallback path is necessary
            if (!_is_contained && !_is_properly_linked)
            {
                bool is_linked = false;
                DirectoryInfo di = null;
                if (_is_file)
                    di = new FileInfo(_full_path_to_resource).Directory;
                else
                    di = new DirectoryInfo(_full_path_to_resource);
                foreach (var fallback in this.PathsToResourceFiles)
                {
                    if (!Directory.Exists(fallback))
                        continue;
                    is_linked = FileSystemNavigation.IsSubdirectoryOf(fallback, di.FullName, false) || string.Equals(fallback, di.FullName, StringComparison.InvariantCultureIgnoreCase);
                    if (is_linked)
                        break;
                }
                if (!is_linked)
                    this.PathsToResourceFiles.Add(di.FullName);
            }

            // create the resource
            ResourceEntry re = null;
            if (_is_file)
            {
                if (_is_contained)
                    re = new ContainedResourceFileEntry(this, this.ProjectData.UsersManager.CurrentUser.Role, _full_path_to_resource, true, _key);
                else
                    re = new LinkedResourceFileEntry(this, this.ProjectData.UsersManager.CurrentUser.Role, _full_path_to_resource, true, _key);
            }
            else
            {
                re = new ResourceDirectoryEntry(this, this.ProjectData.UsersManager.CurrentUser.Role, _full_path_to_resource, true, _key);
            }

            // attach it properly
            if (_is_contained)
            {
                bool attached_as_child = this.PlaceResourceEntryInHierarchy(re);
                if (!attached_as_child)
                    this.Resources_Internal.Add(re);
            }
            else
            {
                var parent_res = (_direct_parent == null) ? null : GetResource(_direct_parent);
                if (parent_res != null)
                    parent_res.Children.Add(re);
                else
                    this.Resources_Internal.Add(re);
            }

            return re;
        }

        /// <summary>
        /// Adds a resource to the top-level resource list. Can be used while changing the location of a resource.
        /// </summary>
        /// <param name="_resource">the resource to add</param>
        internal void AddAsTopLevelResource(ResourceEntry _resource)
        {
            if (_resource == null)
                return;
            if (_resource.IsMyManager(this))
                this.Resources_Internal.Add(_resource);
        }
        /// <summary>
        /// Removes a resource from the top-level resource list. Can be used while changing the location of a resource.
        /// </summary>
        /// <param name="_resource">the resource to remove</param>
        internal void RemoveAsTopLevelResource(ResourceEntry _resource)
        {
            if (_resource == null)
                return;
            if (_resource.IsMyManager(this))
                this.Resources_Internal.Remove(_resource);
        }

        #endregion

        #region RESOURCE Management: ADD and related checks

        /// <summary>
        /// Copies a file to the working directory and converts it to a contained file resource.
        /// </summary>
        /// <param name="sourceFile">the source file</param>
        /// <param name="targetDir">the parent directory, can be null</param>
        /// <param name="copyNameFormat">Format used for the name of a copied resource.
        /// Arguments:
        ///   {0}: The original filename without extension
        ///   {1}: A running counter
        /// </param>
        /// <returns>the index of newly created resource file</returns>
        public (int key, FileInfo resource) CopyResourceAsContainedFileEntry(FileInfo sourceFile, DirectoryInfo targetDir,
            string copyNameFormat)
        {
            if (sourceFile == null)
                throw new ArgumentException("The name of the file cannot be Null!");
            if (!File.Exists(sourceFile.FullName))
                throw new ArgumentException("The source file does not exist!");
            if (targetDir != null && !Directory.Exists(targetDir.FullName))
                throw new ArgumentException("The parent resource directory does not exist!");
            if (copyNameFormat == null)
                copyNameFormat = "{0}";

            // check if the parent is located in the working directory
            if (targetDir != null)
            {
                bool is_contained = FileSystemNavigation.IsSubdirectoryOf(this.working_directory, targetDir.FullName) || string.Equals(this.working_directory, targetDir.FullName, StringComparison.InvariantCultureIgnoreCase);
                if (!is_contained)
                    throw new ArgumentException("The parent directory is not in the working directory!");
            }
            DirectoryInfo parent_di = (targetDir == null) ? new DirectoryInfo(this.WorkingDirectory) : targetDir;

            // check for duplicates
            var files_in_parent = parent_di.GetFiles();

            var fileName = sourceFile.Name;
            var targetPath = Path.Combine(parent_di.FullName, fileName);
            FileInfo fi = new FileInfo(targetPath);

            (_, var fileNameUnique) = AdmissibilityQueries.FileNameIsAdmissible(
                fi, x => !files_in_parent.Any(f => f.FullName == x), copyNameFormat);
            fi = new FileInfo(fileNameUnique);

            // copy the file to the working directory
            File.Copy(sourceFile.FullName, fi.FullName, true);

            // create the resource
            AssetManager.LAST_KEY++;
            this.AddResourceEntry(fi.FullName, true, true, false, AssetManager.LAST_KEY);
            return (AssetManager.LAST_KEY, fi);
        }

        /// <summary>
        /// Checks if the given file can be linked to as a resource.
        /// </summary>
        /// <param name="_file">the file</param>
        /// <returns>possible problems preventing the linking</returns>
        public ResourceLocationError CanLinkAsResource(FileInfo _file)
        {
            if (_file == null)
                throw new ArgumentNullException(nameof(_file));

            ResourceLocationError feedback = ResourceLocationError.OK;
            if (!File.Exists(_file.FullName))
            {
                feedback |= ResourceLocationError.RESOURCE_NOT_FOUND;
                return feedback;
            }

            // check if the file is in the working directory
            bool is_contained = FileSystemNavigation.IsSubdirectoryOf(this.working_directory, _file.DirectoryName) ||
                                string.Equals(this.working_directory, _file.DirectoryName, StringComparison.InvariantCultureIgnoreCase);
            if (is_contained)
            {
                feedback |= ResourceLocationError.LINKED_IN_WORKING_DIR;
                return feedback;
            }

            // check if the file lies in a known link directory
            bool is_properly_linked = false;
            foreach (string lpath in this.PathsToResourceFiles)
            {
                if (!Directory.Exists(lpath))
                    continue;
                is_properly_linked = FileSystemNavigation.IsSubdirectoryOf(lpath, _file.DirectoryName) ||
                                string.Equals(lpath, _file.DirectoryName, StringComparison.InvariantCultureIgnoreCase);
                if (is_properly_linked)
                    break;
            }
            if (!is_properly_linked)
                feedback |= ResourceLocationError.LINKED_NOT_IN_FALLBACKS;

            return feedback;
        }


        /// <summary>
        /// Adds a link to a file as a linked resource to the resource corresponding to the given directory.
        /// If a link to the same file already exists nothing happens, unless _allow_duplicates is set to true.
        /// </summary>
        /// <param name="_source_file">the file to link as a resource</param>
        /// <param name="_parent">the directory where the link should be deposited</param>
        /// <param name="_allow_duplicates">if true, the same file can be linked multiple times as different resources</param>
        /// <returns>the key of the generated resource or -1, if it could not be generated</returns>
        public (int key, bool isDuplicate) LinkResourceAsLinkedFileEntry(FileInfo _source_file, DirectoryInfo _parent, bool _allow_duplicates = false)
        {
            ResourceLocationError ok_to_add = CanLinkAsResource(_source_file);
            if (ok_to_add != ResourceLocationError.OK)
                return (-1, false);

            // check for duplicates
            if (!_allow_duplicates)
            {
                var duplicate = this.resource_look_up.FirstOrDefault(x => string.Equals(x.Value.CurrentFullPath, _source_file.FullName, StringComparison.InvariantCultureIgnoreCase));
                if (duplicate.Value != null)
                    return (duplicate.Key, true);
            }

            // create the resoure
            AssetManager.LAST_KEY++;
            this.AddResourceEntry(_source_file.FullName, true, false, true, AssetManager.LAST_KEY, _parent);
            return (AssetManager.LAST_KEY, false);
        }

        /// <summary>
        /// Replaces the file referenced by a linked resource. If not duplicates are allowed and the 
        /// proposed path is a duplicate, nothing happens.
        /// </summary>
        /// <param name="_resource">the resource</param>
        /// <param name="_new_file">the new path</param>
        /// <param name="_allow_duplicates">if false and the file is a duplicate, no replacement takes place</param>
        /// <returns></returns>
        public bool ReLinkLinkedFileEntry(LinkedResourceFileEntry _resource, FileInfo _new_file, bool _allow_duplicates = false)
        {
            ResourceLocationError ok_to_add = CanLinkAsResource(_new_file);
            if (ok_to_add != ResourceLocationError.OK)
                return false;

            // check for duplicates
            var duplicate = this.resource_look_up.FirstOrDefault(x => string.Equals(x.Value.CurrentFullPath, _new_file.FullName, StringComparison.InvariantCultureIgnoreCase));
            bool is_duplicate = (duplicate.Value != null);
            if (!_allow_duplicates && is_duplicate)
                return true;

            // replace the resource
            _resource.ReplacePath(_new_file.FullName, true);
            return is_duplicate;
        }

        /// <summary>
        /// Call when the file behind this entry was deleted in the file system (i.e., when the user did not explicitly unlink the resource).
        /// </summary>
        /// <param name="_resource"></param>
        public void UnLinkLinkedFileEntry(LinkedResourceFileEntry _resource)
        {
            _resource.SetRelativePathOnDelete();
        }

        /// <summary>
        /// Creates a resource directory in the given one.
        /// </summary>
        /// <param name="_name">the name of the new resource directory</param>
        /// <param name="_parent">the parent directory, can be null</param>
        /// <param name="collisionNameFormat">Format used for the name of a copied resource.
        /// Arguments:
        ///   {0}: The original filename without extension
        ///   {1}: A running counter
        /// </param>
        /// <returns>the newly created resource</returns>
        /// <exception cref="ArgumentException"></exception>
        public (int key, DirectoryInfo resource) CreateResourceDirIn(string _name, DirectoryInfo _parent, string collisionNameFormat)
        {
            // check the correctness of the input
            if (string.IsNullOrEmpty(_name))
                throw new ArgumentException("The name of the resource directory cannot be Null or empty!");
            if (_parent != null && !Directory.Exists(_parent.FullName))
                throw new ArgumentException("The parent resource directory does not exist!");

            // check if the parent is located in the working directory
            if (_parent != null)
            {
                bool is_contained = FileSystemNavigation.IsSubdirectoryOf(this.working_directory, _parent.FullName) || string.Equals(this.working_directory, _parent.FullName, StringComparison.InvariantCultureIgnoreCase);
                if (!is_contained)
                    throw new ArgumentException("The parent directory is not in the working directory!");
            }
            DirectoryInfo parent_di = (_parent == null) ? new DirectoryInfo(this.WorkingDirectory) : _parent;

            // check for duplicates
            var dirs_in_parent = parent_di.GetDirectories();
            (_, var sanitizedName) = AdmissibilityQueries.PropertyNameIsAdmissible(_name, x => !dirs_in_parent.Any(y => y.FullName == _name),
                collisionNameFormat);

            // create the new resource directory
            var createdDir = parent_di.CreateSubdirectory(sanitizedName);

            // create the resource
            AssetManager.LAST_KEY++;
            this.AddResourceEntry(createdDir.FullName, false, true, false, AssetManager.LAST_KEY);
            return (AssetManager.LAST_KEY, createdDir);
        }

        /// <summary>
        /// Creates a new folder resource in the course of complete path change of a resource.
        /// </summary>
        /// <param name="_dir">the folder</param>
        /// <returns>the created resource and all other resource folders that might have been necessary</returns>
        /// <exception cref="ArgumentException"></exception>
        internal (int key, ResourceDirectoryEntry created, List<ResourceDirectoryEntry> all_created)
            CreateResourceDirFrom(DirectoryInfo _dir)
        {
            // check the correctness of the input
            if (_dir == null)
                throw new ArgumentException("The directory cannot be Null!", nameof(_dir));

            bool is_working_dir = string.Equals(_dir.FullName, this.WorkingDirectory, StringComparison.InvariantCultureIgnoreCase);
            if (is_working_dir)
                throw new ArgumentException("The given directory is the working directory!", nameof(_dir));

            bool is_subdir_of_working_dir = FileSystemNavigation.IsSubdirectoryOf(this.WorkingDirectory, _dir.FullName, false);
            if (!is_subdir_of_working_dir)
                throw new ArgumentException("The given directory lies outside the working directory!", nameof(_dir));

            // look for duplicates 
            if (this.resource_look_up.TryFirstOrDefault(x => x.Value is ResourceDirectoryEntry && string.Equals(x.Value.CurrentFullPath, _dir.FullName, StringComparison.InvariantCultureIgnoreCase), out var value))
                return (value.Key, value.Value as ResourceDirectoryEntry, new List<ResourceDirectoryEntry>());

            // look for / create the correct parent           
            List<DirectoryInfo> parents_to_create = new List<DirectoryInfo>();
            DirectoryInfo top_existing_parent = null;

            DirectoryInfo parent = _dir.Parent;
            while (parent != null)
            {
                if (!this.resource_look_up.TryFirstOrDefault(x => x.Value is ResourceDirectoryEntry && string.Equals(x.Value.CurrentFullPath, parent.FullName, StringComparison.InvariantCultureIgnoreCase), out var parent_res))
                {
                    parents_to_create.Add(parent);
                }
                else
                {
                    top_existing_parent = new DirectoryInfo(parent_res.Value.CurrentFullPath);
                    break;
                }
                parent = parent.Parent;
            }

            parents_to_create.Reverse();
            List<ResourceDirectoryEntry> all_created = new List<ResourceDirectoryEntry>();
            foreach (DirectoryInfo p in parents_to_create)
            {
                (int p_key, var p_res) = this.CreateResourceDirIn(p.Name, top_existing_parent, "");
                all_created.Add(this.resource_look_up[p_key] as ResourceDirectoryEntry);
                top_existing_parent = p_res;
            }

            // finally, create the directory itself
            (var key, var dir_created) = this.CreateResourceDirIn(_dir.Name, top_existing_parent, "");
            all_created.Add(this.resource_look_up[key] as ResourceDirectoryEntry);
            return (key, this.resource_look_up[key] as ResourceDirectoryEntry, all_created);
        }

        private List<int> ConvertToResource(DirectoryInfo _dir, bool _check_input_validity = true)
        {
            if (_dir == null)
                throw new ArgumentNullException(nameof(_dir));

            // check validity of input
            if (_check_input_validity)
            {
                if (!Directory.Exists(_dir.FullName))
                    throw new ArgumentException("The source folder does not exist!");
                bool is_working_dir = string.Equals(_dir.FullName, this.WorkingDirectory, StringComparison.InvariantCultureIgnoreCase);
                if (is_working_dir)
                    throw new ArgumentException("The given directory is the working directory!", nameof(_dir));

                bool is_subdir_of_working_dir = FileSystemNavigation.IsSubdirectoryOf(this.WorkingDirectory, _dir.FullName, false);
                if (!is_subdir_of_working_dir)
                    throw new ArgumentException("The given directory lies outside the working directory!", nameof(_dir));
            }

            // look for duplicates
            if (this.resource_look_up.TryFirstOrDefault(x => x.Value is ResourceDirectoryEntry && string.Equals(x.Value.CurrentFullPath, _dir.FullName, StringComparison.InvariantCultureIgnoreCase), out var value))
                return new List<int> { value.Key };

            List<int> created_keys = new List<int>();

            // create the resource itself
            AssetManager.LAST_KEY++;
            this.AddResourceEntry(_dir.FullName, false, true, false, AssetManager.LAST_KEY);
            created_keys.Add(AssetManager.LAST_KEY);

            // create subdirectories and files
            FileInfo[] files = _dir.GetFiles();
            foreach (var f in files)
            {
                int key = this.ConvertToResource(f, false);
                created_keys.Add(key);
            }
            DirectoryInfo[] sdirs = _dir.GetDirectories();
            foreach (var d in sdirs)
            {
                List<int> skeys = this.ConvertToResource(d, false);
                created_keys.AddRange(skeys);
            }

            // done
            return created_keys;
        }

        private int ConvertToResource(FileInfo _file, bool _check_input_validity = true)
        {
            if (_file == null)
                throw new ArgumentNullException(nameof(_file));

            // check validity of input
            if (_check_input_validity)
            {
                if (!File.Exists(_file.FullName))
                    throw new ArgumentException("The source file does not exist!");

                bool is_contained = FileSystemNavigation.IsSubdirectoryOf(this.working_directory, _file.Directory.FullName) || string.Equals(this.working_directory, _file.Directory.FullName, StringComparison.InvariantCultureIgnoreCase);
                if (!is_contained)
                    throw new ArgumentException("The source file's parent directory is not in the working directory!");
            }

            // look for duplicates
            if (this.resource_look_up.TryFirstOrDefault(x => x.Value is ContainedResourceFileEntry && string.Equals(x.Value.CurrentFullPath, _file.FullName, StringComparison.InvariantCultureIgnoreCase), out var value))
                return value.Key;

            // create the resource
            AssetManager.LAST_KEY++;
            this.AddResourceEntry(_file.FullName, true, true, false, AssetManager.LAST_KEY);
            return AssetManager.LAST_KEY;
        }


        #endregion

        #region RESOURCE Management: COPY

        /// <summary>
        /// Checks if a resource file, folder or link can be copied within the project.
        /// </summary>
        /// <param name="_original">the resource to copy</param>
        /// <param name="_target">the target resource folders, can be null, which corresponds to the working directory</param>
        /// <param name="copyNameFormat">Format used for the name of a copied resource.
        /// Arguments:
        ///   {0}: The original filename without extension
        ///   {1}: A running counter
        /// </param>
        /// <returns>admissibility of the copying and a proposal for the target file name</returns>
        /// <exception cref="ArgumentException"></exception>
        public (bool admissible, string proposed_name) CanCopyResourceEntry(ResourceEntry _original, ResourceDirectoryEntry _target,
            string copyNameFormat)
        {
            if (_original == null)
                return (false, string.Empty);
            if (!_original.IsMyManager(this))
                throw new ArgumentException("The asset manager of the original differs from the current one!");
            if (!File.Exists(_original.CurrentFullPath) && !Directory.Exists(_original.CurrentFullPath))
                throw new ArgumentException("The original is corrupted: it does not exist as a file!");
            if (_target != null && !_target.IsMyManager(this))
                throw new ArgumentException("The asset manager of the target folder differs from the current one!");
            if (_target != null && !Directory.Exists(_target.CurrentFullPath))
                throw new ArgumentException("The target folder is corrupt: it does not exist as an actual folder!");

            DirectoryInfo target_dir = (_target == null) ? new DirectoryInfo(this.WorkingDirectory) : new DirectoryInfo(_target.CurrentFullPath);
            FileInfo[] files_in_target = target_dir.GetFiles();
            DirectoryInfo[] dirs_in_target = target_dir.GetDirectories();

            if (_original is ContainedResourceFileEntry)
            {
                var targetPath = Path.Combine(target_dir.FullName, new FileInfo(_original.CurrentFullPath).Name);
                FileInfo fi = new FileInfo(targetPath);
                (_, var fileNameUnique) = AdmissibilityQueries.FileNameIsAdmissible(fi, x => !files_in_target.Any(f => f.FullName == x), copyNameFormat);
                return (true, fileNameUnique);
            }
            else if (_original is ResourceDirectoryEntry)
            {
                var targetPath = Path.Combine(target_dir.FullName, new DirectoryInfo(_original.CurrentFullPath).Name);
                DirectoryInfo di = new DirectoryInfo(targetPath);
                (_, var dirNameUnique) = AdmissibilityQueries.DirectoryNameIsAdmissible(di, x => !dirs_in_target.Any(d => d.FullName == x), copyNameFormat);
                return (true, dirNameUnique);
            }
            else if (_original is LinkedResourceFileEntry)
            {
                return (true, _original.Name);
            }

            return (false, null);
        }

        /// <summary>
        /// Copies a resource within the same project. Copied folders only contain files and subfolders. Linked files 
        /// in that structure do *not* get copied.
        /// </summary>
        /// <param name="_original">the resource to copy</param>
        /// <param name="_target">the target folder, can be null, which corresponds to the working directory</param>
        /// <param name="newOwner">The user who performs the copy operation. The created resource will be owned by this user. 
        /// When set to null, the original owner is kept</param>
        /// <param name="copyNameFormat">Format used for the name of a copied resource.
        /// Arguments:
        ///   {0}: The original filename without extension
        ///   {1}: A running counter
        /// </param>
        /// <returns>the top-level copy</returns>
        public (int key, FileSystemInfo system_entry) CopyResourceEntry(ResourceEntry _original, ResourceDirectoryEntry _target,
            string copyNameFormat,
            SimUser newOwner = null)
        {
            (var admissible, var proposed_name) = this.CanCopyResourceEntry(_original, _target, copyNameFormat);

            if (string.IsNullOrEmpty(proposed_name))
                return (-1, null);

            DirectoryInfo target_dir = (_target == null) ? new DirectoryInfo(this.WorkingDirectory) : new DirectoryInfo(_target.CurrentFullPath);
            if (_original is ContainedResourceFileEntry)
            {
                // copy file
                FileInfo fi = new FileInfo(proposed_name);
                File.Copy(_original.CurrentFullPath, fi.FullName, true);

                // create the resource
                AssetManager.LAST_KEY++;
                var copiedResource = this.AddResourceEntry(fi.FullName, true, true, false, AssetManager.LAST_KEY);

                if (newOwner != null)
                    copiedResource.UserWithWritingAccess = newOwner.Role;
                else
                    copiedResource.UserWithWritingAccess = _original.UserWithWritingAccess;

                return (AssetManager.LAST_KEY, fi);
            }
            else if (_original is LinkedResourceFileEntry)
            {
                FileInfo fi = new FileInfo(_original.CurrentFullPath);
                // create the resource
                AssetManager.LAST_KEY++;
                var copiedResource = this.AddResourceEntry(fi.FullName, true, false, true, AssetManager.LAST_KEY, target_dir);

                if (newOwner != null)
                    copiedResource.UserWithWritingAccess = newOwner.Role;
                else
                    copiedResource.UserWithWritingAccess = _original.UserWithWritingAccess;

                return (AssetManager.LAST_KEY, fi);
            }
            else if (_original is ResourceDirectoryEntry)
            {
                // copy directory with all subdirectories and files
                DirectoryInfo di_original = new DirectoryInfo(_original.CurrentFullPath);
                DirectoryInfo di_copy = new DirectoryInfo(proposed_name);
                DirectoryOperations.DirectoryCopy(di_original.FullName, di_copy.FullName, true);
                // convert the copied contents into resources
                var keys = this.ConvertToResource(di_copy);

                foreach (var key in keys)
                    if (newOwner != null)
                        this.GetResource(key).UserWithWritingAccess = newOwner.Role;
                    else
                        this.GetResource(key).UserWithWritingAccess = _original.UserWithWritingAccess;

                return (keys[0], di_copy);
            }

            return (-1, null);
        }

        /// <summary>
        /// Checks if a contained file can be copied to an external folder.
        /// </summary>
        /// <param name="_original">the resource file to copy</param>
        /// <param name="_target">the target folder</param>
        /// <param name="copyNameFormat">Format used for the name of a copied resource.
        /// Arguments:
        ///   {0}: The original filename without extension
        ///   {1}: A running counter
        /// </param>
        /// <returns>admissibility of the copying and a proposal for the target file name</returns>
        /// <exception cref="ArgumentException"></exception>
        internal (bool admissible, string proposed_name) CanCopyContainedResourceFileEntryToExternal(
            ContainedResourceFileEntry _original, DirectoryInfo _target, string copyNameFormat)
        {
            if (_original == null)
                return (false, string.Empty);
            if (!File.Exists(_original.CurrentFullPath))
                throw new ArgumentException("The original is corrupted: it does not exist as a file!");
            if (_target == null)
                throw new ArgumentNullException(nameof(_target));
            if (!Directory.Exists(_target.FullName))
                throw new ArgumentException("The target folder does not exist!");

            bool is_working_dir = string.Equals(_target.FullName, this.WorkingDirectory, StringComparison.InvariantCultureIgnoreCase);
            if (is_working_dir)
                throw new ArgumentException("The given directory is the working directory!", nameof(_target));

            bool is_subdir_of_working_dir = FileSystemNavigation.IsSubdirectoryOf(this.WorkingDirectory, _target.FullName, false);
            if (is_subdir_of_working_dir)
                throw new ArgumentException("The given directory lies inside the working directory!", nameof(_target));

            FileInfo[] files_in_target = _target.GetFiles();

            var targetPath = Path.Combine(_target.FullName, new FileInfo(_original.CurrentFullPath).Name);
            FileInfo fi = new FileInfo(targetPath);

            (_, var fileNameUnique) = AdmissibilityQueries.FileNameIsAdmissible(fi, x => !files_in_target.Any(f => f.FullName == x),
                copyNameFormat);
            return (true, fileNameUnique);
        }

        /// <summary>
        /// Converts a contained resource into a linked one. The target folder should be outside the
        /// working directory and contained in one of the fallbacks. Note: involves deletion of the old and initialization of the new object.
        /// </summary>
        /// <param name="_original">the resource to convert</param>
        /// <param name="_target">the target folder for the linked resource</param>
        /// <param name="copyNameFormat">Format used for the name of a copied resource.
        /// Arguments:
        ///   {0}: The original filename without extension
        ///   {1}: A running counter
        /// </param>
        /// <returns>the converted resource or Null</returns>
        public LinkedResourceFileEntry ContainedToLinked(ContainedResourceFileEntry _original, DirectoryInfo _target,
            string copyNameFormat)
        {
            // check if the input is valid
            (var admissible, var proposed_name) = this.CanCopyContainedResourceFileEntryToExternal(_original, _target, copyNameFormat);
            if (string.IsNullOrEmpty(proposed_name))
                return null;

            // copy the file
            int key = _original.Key;
            DirectoryInfo parent = (_original.Parent == null) ? new DirectoryInfo(this.WorkingDirectory) : new DirectoryInfo(_original.Parent.CurrentFullPath);
            FileInfo fi = new FileInfo(proposed_name);
            File.Copy(_original.CurrentFullPath, fi.FullName, true);

            // delete the old resource (involves the deletion of the file)
            this.DeleteResourceEntryAny(_original);

            // create the new linked resource
            ResourceLocationError ok_to_add = CanLinkAsResource(fi);
            if (ok_to_add != ResourceLocationError.OK)
                throw new ArgumentException("Invalid input data!");

            // ... no check for duplicates

            return this.AddResourceEntry(fi.FullName, true, false, true, key, parent) as LinkedResourceFileEntry;
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
        public ContainedResourceFileEntry LinkedToContained(LinkedResourceFileEntry _original, string nameFormat)
        {
            if (_original == null)
                throw new ArgumentNullException(nameof(_original));
            if (!File.Exists(_original.CurrentFullPath))
                throw new ArgumentException("The resource file does not exist!");

            DirectoryInfo parent_dir = (_original.Parent == null) ? new DirectoryInfo(this.WorkingDirectory) : new DirectoryInfo(_original.Parent.CurrentFullPath);

            // check for duplicates
            var files_in_parent = parent_dir.GetFiles();

            var original_file = new FileInfo(_original.CurrentFullPath);
            var targetPath = Path.Combine(parent_dir.FullName, original_file.Name);
            FileInfo fi = new FileInfo(targetPath);

            (_, var fileNameUnique) = AdmissibilityQueries.FileNameIsAdmissible(fi, x => !files_in_parent.Any(f => f.FullName == x),
                nameFormat);
            fi = new FileInfo(fileNameUnique);

            // copy the file to the working directory
            int key = _original.Key;
            File.Copy(original_file.FullName, fi.FullName, true);

            // delete the old resource (does not delete the file)
            this.DeleteResourceEntryAny(_original);

            // create the resource
            return this.AddResourceEntry(fi.FullName, true, true, false, key, parent_dir) as ContainedResourceFileEntry;
        }

        #endregion

        #region RESOURCE Management: DELETE, REPLACE, incl. complete path change

        /// <summary>
        /// Deletes the resource with the given full path and name, including children.
        /// </summary>
        /// <param name="_current_full_path_to_resource">the full path to the resource(file or folder)</param>
        /// <param name="_name_of_resource">the name of the resource</param>
        /// <returns>the key of the deleted entry</returns>
        public int DeleteResourceFileEntry(string _current_full_path_to_resource, string _name_of_resource)
        {
            var entry = this.resource_look_up.FirstOrDefault(x => (x.Value.CurrentFullPath == AssetManager.PATH_NOT_FOUND && x.Value.Name == _name_of_resource) || x.Value.CurrentFullPath == _current_full_path_to_resource);
            int key = (entry.Value == null) ? -1 : entry.Key;
            if (key > -1)
                DeleteResourceEntryAny(entry.Value);
            return key;
        }

        /// <summary>
        /// Checks if the deletion of the resource and all its children in
        /// the file system is possible. Attention: The file system state can change 
        /// immediately after the check and still cause an exception.
        /// </summary>
        /// <param name="_to_delete">the resource to delete</param>
        /// <returns>feedback from the attempt to delete</returns>
        public (bool exists, bool can_delete) DeleteResourceEntryPossible(ResourceEntry _to_delete)
        {
            if (_to_delete == null)
                return (false, false);
            if (!this.resource_look_up.ContainsKey(_to_delete.Key))
                return (false, false);

            bool exists = true;
            bool can_delete = true;
            // check if any children can be deleted
            if (_to_delete is ResourceDirectoryEntry)
            {
                foreach (var child in (_to_delete as ResourceDirectoryEntry).Children)
                {
                    var child_test = DeleteResourceEntryPossible(child);
                    if (!child_test.exists)
                        return (false, false);
                    else if (!child_test.can_delete)
                        return (true, false);
                }
            }
            // check self
            if (_to_delete is ContainedResourceFileEntry)
            {
                FileInfo file = new FileInfo(_to_delete.CurrentFullPath);
                exists = File.Exists(_to_delete.CurrentFullPath);
                can_delete = !FileState.IsInUse(file);
            }
            else if (_to_delete is ResourceDirectoryEntry)
            {
                exists = Directory.Exists(_to_delete.CurrentFullPath);
            }
            return (exists, can_delete);
        }

        /// <summary>
        /// Deletes the given resource, regardless of whether it exists in the file system or not.
        /// Linked resources are not removed from the file system.
        /// </summary>
        /// <param name="_to_delete">the resource to delete</param>
        /// <returns>ture, if the deletion was successful; false otherwise</returns>
        public bool DeleteResourceEntryAny(ResourceEntry _to_delete)
        {
            bool deletion_ok = true;
            _to_delete.OnDeleting();
            if (this.resource_look_up.ContainsKey(_to_delete.Key))
            {
                // 1. remove all children
                // even if the removal of any of the children is not successful, carry on with the other children
                if (_to_delete is ResourceDirectoryEntry)
                {
                    int n = (_to_delete as ResourceDirectoryEntry).Children.Count;
                    for (int i = 0; i < n; i++)
                    {
                        var child = (_to_delete as ResourceDirectoryEntry).Children[0];
                        var child_deleted = DeleteResourceEntryAny(child);
                        deletion_ok &= child_deleted;
                    }
                }
                // 2. remove the file / folder from the file system only if the removal of all children was successful
                if (deletion_ok)
                {
                    // if this step is not successful, ABORT w/o changing the resource and asset records for this resource
                    try
                    {
                        if (_to_delete is ContainedResourceFileEntry)
                        {
                            File.Delete(_to_delete.CurrentFullPath);
                        }
                        else if (_to_delete is ResourceDirectoryEntry)
                        {
                            Directory.Delete(_to_delete.CurrentFullPath);
                        }
                    }
                    catch
                    {
                        deletion_ok = false;
                        return deletion_ok;
                    }

                    // 3. remove all dependent assets
                    if (this.Assets.ContainsKey(_to_delete.Key))
                    {
                        foreach (var a in this.Assets[_to_delete.Key])
                        {
                            this.RemoveReferencedAsset(a);
                        }
                        this.Assets.Remove(_to_delete.Key);
                    }
                    // 4. remove the resource itself
                    if (_to_delete.Parent != null)
                        (_to_delete.Parent as ResourceDirectoryEntry).Children.Remove(_to_delete);
                    else
                        this.Resources_Internal.Remove(_to_delete);
                }
            }
            _to_delete.OnDeleted();
            return deletion_ok;
        }

        /// <summary>
        /// Deletes the given resource.
        /// Used when the resource was deleted on a file system level.
        /// Therefore files and directories are not deleted on the file system, cause that should already have happened.
        /// </summary>
        /// <param name="_to_delete">the resource to delete</param>
        /// <returns>ture, if the deletion was successful; false otherwise</returns>
        public bool ResourceEntryDeltedExternally(ResourceEntry _to_delete)
        {
            bool deletion_ok = true;
            _to_delete.OnDeleting();
            if (this.resource_look_up.ContainsKey(_to_delete.Key))
            {
                // 1. remove all children
                // even if the removal of any of the children is not successful, carry on with the other children
                if (_to_delete is ResourceDirectoryEntry del)
                {
                    int n = del.Children.Count;
                    for (int i = 0; i < n; i++)
                    {
                        var child = del.Children[0];
                        var child_deleted = ResourceEntryDeltedExternally(child);
                        deletion_ok &= child_deleted;
                    }
                }
                // 2. remove the file / folder from the file system only if the removal of all children was successful
                if (deletion_ok)
                {
                    // 3. remove all dependent assets
                    if (this.Assets.ContainsKey(_to_delete.Key))
                    {
                        foreach (var a in this.Assets[_to_delete.Key])
                        {
                            this.RemoveReferencedAsset(a);
                        }
                        this.Assets.Remove(_to_delete.Key);
                    }
                    // 4. remove the resource itself
                    if (_to_delete.Parent != null)
                        (_to_delete.Parent as ResourceDirectoryEntry).Children.Remove(_to_delete);
                    else
                        this.Resources_Internal.Remove(_to_delete);
                }
            }
            _to_delete.OnDeleted();
            return deletion_ok;
        }


        /// <summary>
        /// Clears the saved links in the asset managers of the calling component factory.
        /// </summary>
        public void ResetLinks()
        {
            this.isResetting = true;
            this.PathsToResourceFiles.Clear();
            this.isResetting = false;
        }

        #endregion

        #region RESOURCE MANAGEMENT: Queries

        /// <summary>
        /// Checks if a resource by the given name exists. It does not check if the file exists in the file system.
        /// </summary>
        /// <param name="file">The resource</param>
        /// <returns>true, if it exists, false otherwise</returns>
        public bool ResourceFileEntryExists(FileInfo file)
        {
            var duplicate = this.resource_look_up.FirstOrDefault(x => x.Value.CurrentFullPath != AssetManager.PATH_NOT_FOUND && x.Value.CurrentFullPath == file.FullName);
            int duplicate_key = (duplicate.Value == null) ? -1 : duplicate.Key;
            return (duplicate_key > -1);
        }

        /// <summary>
        /// Checks the validity of the file represented by the given file info.
        /// This method does not check if there is a valid corresponding resource entry!
        /// </summary>
        /// <param name="_fi">the file info representing the resource</param>
        /// <param name="_is_contained"></param>
        /// <returns>true if the resource is valid, false otherwise</returns>
        public bool IsValidResourcePath(FileInfo _fi, bool _is_contained)
        {
            if (_is_contained && !string.IsNullOrEmpty(this.WorkingDirectory))
            {
                DirectoryInfo di = new DirectoryInfo(this.WorkingDirectory);
                var file_list = di.GetFiles(_fi.Name, SearchOption.AllDirectories);
                if (file_list != null && file_list.Length > 0)
                {
                    return _fi.FullName == file_list[0].FullName;
                }
            }
            else if (!_is_contained)
            {
                return File.Exists(_fi.FullName);
            }
            return false;
        }

        /// <summary>
        /// Gets the extension of the file with the given key in the resource file list.
        /// </summary>
        /// <param name="_key">the key in the resource list</param>
        /// <param name="_with_dot">if false the extension is w/o the dot</param>
        /// <returns>the extension as a string w/o a dot</returns>
        public string GetFileExtension(int _key, bool _with_dot = false)
        {
            string file_path = AssetManager.PATH_NOT_FOUND;
            if (this.resource_look_up.ContainsKey(_key))
                file_path = (this.resource_look_up[_key].CurrentFullPath == AssetManager.PATH_NOT_FOUND) ?
                                this.resource_look_up[_key].CurrentRelativePath : this.resource_look_up[_key].CurrentFullPath;

            if (file_path == AssetManager.PATH_NOT_FOUND) return null;
            //if (!File.Exists(file_path)) return null; // prevents replacement of files that could not be found

            FileInfo fi = new FileInfo(file_path);
            string file_ext = fi.Extension;
            if (string.IsNullOrEmpty(file_ext))
                return null;

            if (_with_dot)
                return file_ext;
            else
                return file_ext.Substring(file_ext.LastIndexOf(".") + 1);
        }

        /// <summary>
        /// Gets all resource files with one of the given extensions.
        /// </summary>
        /// <param name="_extensions">the wanted extensions</param>
        /// <param name="_contained">True = contained files, False = only linked files</param>
        /// <returns>a file collection</returns>
        public List<FileInfo> GetAllResourceFiles(IEnumerable<string> _extensions, bool _contained)
        {
            List<FileInfo> files = new List<FileInfo>();
            List<string> file_ext = new List<string>(_extensions);

            foreach (var entry in this.resource_look_up)
            {
                if (entry.Value is ResourceDirectoryEntry) continue;
                if (_contained && entry.Value is LinkedResourceFileEntry)
                    continue;
                else if (!_contained && entry.Value is ContainedResourceFileEntry)
                    continue;

                string fpath = entry.Value.CurrentFullPath;
                if (fpath != AssetManager.PATH_NOT_FOUND && File.Exists(fpath))
                {
                    FileInfo file = new FileInfo(fpath);
                    string found_ext = file_ext.FirstOrDefault(fe => string.Equals(file.Extension, fe, StringComparison.InvariantCultureIgnoreCase));
                    if (!string.IsNullOrEmpty(found_ext))
                        files.Add(file);
                }
            }
            return files;
        }

        /// <summary>
        /// Retrieves the full path of a resource with the given key.
        /// </summary>
        /// <param name="_key">the resource key</param>
        /// <returns>the full path to the resource</returns>
        public string GetPath(int _key)
        {
            if (this.resource_look_up.ContainsKey(_key))
                return this.resource_look_up[_key].CurrentFullPath;
            else
                return string.Empty;
        }

        /// <summary>
        /// Retrieves the key of a resource file with the given name.
        /// </summary>
        /// <param name="_file_name">the resource file name</param>
        /// <returns>the index of the resource</returns>
        [Obsolete("Does not work when multiple files with the same name but different folder exist")]
        public int GetKey(string _file_name)
        {
            var entry = this.resource_look_up.FirstOrDefault(x => x.Value.Name == _file_name);
            if (entry.Value != null)
                return entry.Value.Key;
            else
                return -1;
        }

        /// <summary>
        /// Retrieves a resource entry by its key.
        /// </summary>
        /// <param name="_key">The key of the resource</param>
        /// <returns>The corresponding resource or Null</returns>
		public ResourceEntry GetResource(int _key)
        {
            if (this.resource_look_up.TryGetValue(_key, out var value))
                return value;
            return null;
        }

        /// <summary>
        /// Retrieves the key of the resource representing the given file.
        /// </summary>
        /// <param name="_file">the file</param>
        /// <returns>the key, if found; otherwise -1</returns>
        public int GetResourceKey(FileInfo _file)
        {
            if (_file == null)
                throw new ArgumentNullException(nameof(_file));

            if (this.resource_look_up.TryFirstOrDefault(x => string.Equals(x.Value.CurrentFullPath, _file.FullName, StringComparison.InvariantCultureIgnoreCase), out var value))
                return value.Key;
            return -1;
        }

        /// <summary>
        /// Retrieves a resource directory by its full path.
        /// </summary>
        /// <param name="_dir">the directory</param>
        /// <returns>the retrieved resource or null</returns>
        public ResourceDirectoryEntry GetResource(DirectoryInfo _dir)
        {
            if (this.resource_look_up.TryFirstOrDefault(x => x.Value is ResourceDirectoryEntry && string.Equals(x.Value.CurrentFullPath, _dir.FullName, StringComparison.InvariantCultureIgnoreCase), out var value))
                return value.Value as ResourceDirectoryEntry;
            return null;
        }

        /// <summary>
        /// Retrieves a resource file by its full path.
        /// </summary>
        /// <param name="_file">he file</param>
        /// <returns>the retrieved resource or null</returns>
        public ResourceFileEntry GetResource(FileInfo _file)
        {
            if (this.resource_look_up.TryFirstOrDefault(x => x.Value is ResourceFileEntry && string.Equals(x.Value.CurrentFullPath, _file.FullName, StringComparison.InvariantCultureIgnoreCase), out var value))
                return value.Value as ResourceFileEntry;
            return null;
        }

        #endregion

        #region RESOURCE SYNCHRONIZATION W FILE SYSTEM

        /// <summary>
        /// Checks if the given files and resources are represented by resources. Can create missing resources. 
        /// Can remove superfluous resources.
        /// </summary>
        /// <param name="_resource_files">the files used as resources</param>
        /// <param name="_resource_dirs">the directories in which those files reside</param>
        /// <param name="_create_missing">if true create the missing resource entries</param>
        /// <param name="_delete_superfluous">if true deletes the superfluous resource entries</param>
        /// <returns>a list of all resources w/o correponding file or dir, a list of all files w/o corresponding resource, and a list of all directories w/o a corresponding resource</returns>
        public (List<ResourceEntry> resources_wo_file, List<FileInfo> files_wo_res, List<DirectoryInfo> dirs_wo_res)
            SynchronizeResources(IEnumerable<FileInfo> _resource_files, IEnumerable<DirectoryInfo> _resource_dirs, bool _create_missing, bool _delete_superfluous)
        {
            if (_resource_files == null && _resource_dirs == null)
                throw new ArgumentNullException("Files or directories necessary for the synchronization!");

            List<ResourceEntry> to_delete = new List<ResourceEntry>();
            List<FileInfo> files_entries_to_create = new List<FileInfo>();
            List<DirectoryInfo> dir_entries_to_create = new List<DirectoryInfo>();

            if (_resource_files != null)
            {
                foreach (FileInfo fi in _resource_files)
                {
                    bool found_corresponding = this.resource_look_up.TryFirstOrDefault(x => string.Equals(x.Value.CurrentFullPath, fi.FullName, StringComparison.InvariantCultureIgnoreCase),
                                                                                        out KeyValuePair<int, ResourceEntry> value);
                    if (!found_corresponding)
                        files_entries_to_create.Add(fi);

                }
            }

            if (_resource_dirs != null)
            {
                foreach (DirectoryInfo di in _resource_dirs)
                {
                    bool found_corresponding = this.resource_look_up.TryFirstOrDefault(x => string.Equals(x.Value.CurrentFullPath, di.FullName, StringComparison.InvariantCultureIgnoreCase),
                                                                                        out KeyValuePair<int, ResourceEntry> value);
                    if (!found_corresponding)
                        dir_entries_to_create.Add(di);
                }
            }

            foreach (var entry in this.resource_look_up)
            {
                if (entry.Value is ContainedResourceFileEntry && _resource_files != null)
                {
                    bool found_corresponding = _resource_files.TryFirstOrDefault(x => string.Equals(x.FullName, entry.Value.CurrentFullPath, StringComparison.InvariantCultureIgnoreCase),
                                                                                    out FileInfo found);
                    if (!found_corresponding)
                        to_delete.Add(entry.Value);
                }
                else if (entry.Value is ResourceDirectoryEntry && _resource_dirs != null)
                {
                    bool found_corresponding = _resource_dirs.TryFirstOrDefault(x => string.Equals(x.FullName, entry.Value.CurrentFullPath, StringComparison.InvariantCultureIgnoreCase),
                                                                                    out DirectoryInfo found);
                    if (!found_corresponding)
                        to_delete.Add(entry.Value);
                }
            }

            if (_delete_superfluous)
            {
                foreach (ResourceEntry re in to_delete)
                {
                    this.DeleteResourceEntryAny(re);
                }
            }

            if (_create_missing)
            {
                foreach (DirectoryInfo di in dir_entries_to_create)
                {
                    AssetManager.LAST_KEY++;
                    this.AddResourceEntry(di.FullName, false, true, false, AssetManager.LAST_KEY);
                }
                foreach (FileInfo fi in files_entries_to_create)
                {
                    AssetManager.LAST_KEY++;
                    this.AddResourceEntry(fi.FullName, true, true, false, AssetManager.LAST_KEY);
                }
            }

            return (to_delete, files_entries_to_create, dir_entries_to_create);
        }

        /// <summary>
        /// Retrieves all found linked files from the entire resource hierarchy.
        /// </summary>
        /// <returns>a list of existing files</returns>
        public List<FileInfo> GetAllLinkedFiles()
        {
            List<FileInfo> links = new List<FileInfo>();
            foreach (ResourceEntry re in this.Resources_Internal)
            {
                if (re is LinkedResourceFileEntry && File.Exists(re.CurrentFullPath))
                    links.Add(new FileInfo(re.CurrentFullPath));
                else if (re is ResourceDirectoryEntry)
                    links.AddRange((re as ResourceDirectoryEntry).GetFlatLinks());
            }
            return links;
        }

        #endregion

        #region RESOURCE UTILS

        private (bool consistent, string corrected_full, string corrected_relative, bool? corrected_is_contained)
            CorrectPathInput(string _full_path, string _rel_path, bool _is_contained)
        {
            bool consistent = false;
            string corrected_full = _full_path;
            string corrected_relative = _rel_path;
            bool? corrected_is_contained = null;
            if (string.IsNullOrEmpty(this.WorkingDirectory))
                return (consistent, corrected_full, corrected_relative, corrected_is_contained);

            // 0. determine actual relative and full paths
            var check = FileSystemNavigation.CheckPaths(_full_path, _rel_path, AssetManager.PATH_NOT_FOUND);
            consistent = check.pathsMatch;
            corrected_full = check.actualFullPath;
            corrected_relative = check.actualRelPath;

            // 1. check if the paths are correct as resources
            if (check.fullIsValid)
            {
                if (_is_contained)
                {
                    corrected_is_contained = FileSystemNavigation.IsContainedIn(this.WorkingDirectory, check.actualFullPath, false)
                                        || string.Equals(this.WorkingDirectory, check.actualFullPath, StringComparison.InvariantCultureIgnoreCase);
                    if (corrected_is_contained.Value && (string.IsNullOrEmpty(corrected_relative) || corrected_relative == AssetManager.PATH_NOT_FOUND))
                        corrected_relative = corrected_full.Replace(this.WorkingDirectory + Path.DirectorySeparatorChar, string.Empty);
                }
                else
                {
                    foreach (string link in this.PathsToResourceFiles)
                    {
                        if (!Directory.Exists(link))
                            continue;
                        bool not_contained = FileSystemNavigation.IsContainedIn(link, check.actualFullPath, false)
                                        || string.Equals(link, check.actualFullPath, StringComparison.InvariantCultureIgnoreCase);
                        if (not_contained)
                        {
                            corrected_is_contained = false;
                            if (string.IsNullOrEmpty(corrected_relative) || corrected_relative == AssetManager.PATH_NOT_FOUND)
                                corrected_relative = corrected_full.Replace(link + Path.DirectorySeparatorChar, string.Empty);
                            break;
                        }
                    }
                }
            }
            else if (check.relIsValid)
            {
                if (_is_contained)
                {
                    corrected_full = Path.Combine(this.WorkingDirectory, check.actualRelPath);
                    corrected_is_contained = File.Exists(corrected_full) || Directory.Exists(corrected_full);
                    if (!consistent)
                        consistent = corrected_is_contained.Value;
                }
                else
                {
                    foreach (string link in this.PathsToResourceFiles)
                    {
                        string test_full = Path.Combine(link, check.actualRelPath);
                        bool test_fit = File.Exists(test_full) || Directory.Exists(test_full);
                        if (test_fit)
                        {
                            corrected_full = test_full;
                            corrected_is_contained = false;
                            consistent = true;
                            break;
                        }
                    }
                }
            }

            // 2. if no problems were detected, DONE
            if (consistent && corrected_is_contained.HasValue && corrected_is_contained.Value == _is_contained)
                return (consistent, corrected_full, corrected_relative, corrected_is_contained);

            // 3. otherwise, attempt correction of the full path, if it is a valid path
            if (!string.IsNullOrEmpty(corrected_full) && corrected_full != AssetManager.PATH_NOT_FOUND)
            {
                if (this.ProjectData.NetworkManager.CalledFromLocation != null)
                {
                    // the working dir contained at least one ~
                    string old_working_dir = FileSystemNavigation.GetSubPathContaining(corrected_full, "~", false);
                    if (!string.IsNullOrEmpty(old_working_dir))
                    {
                        string path_candidate = corrected_full.Replace(old_working_dir, this.WorkingDirectory);
                        if (File.Exists(path_candidate) || Directory.Exists(path_candidate))
                        {
                            corrected_full = path_candidate;
                            corrected_is_contained = true;
                            if (string.IsNullOrEmpty(corrected_relative) || corrected_relative == AssetManager.PATH_NOT_FOUND)
                                corrected_relative = corrected_full.Replace(this.WorkingDirectory + Path.DirectorySeparatorChar, string.Empty);
                        }
                    }
                }
            }

            if (consistent && !corrected_is_contained.HasValue &&
                !string.IsNullOrEmpty(corrected_full) && corrected_full != AssetManager.PATH_NOT_FOUND &&
                !string.IsNullOrEmpty(corrected_relative) && corrected_relative != AssetManager.PATH_NOT_FOUND)
            {
                // if not successful, determine the difference btw the full and the relative paths 
                string diff = corrected_full.Replace(corrected_relative, string.Empty);
                // replace the guessed working directory with the current one
                string path_candidate = (string.IsNullOrEmpty(diff)) ? corrected_full : corrected_full.Replace(diff, this.WorkingDirectory + Path.DirectorySeparatorChar);
                if (File.Exists(path_candidate) || Directory.Exists(path_candidate))
                {
                    corrected_full = path_candidate;
                    corrected_is_contained = true;
                }
                else
                {
                    // replace the guessed working directory with each of the fallbacks
                    foreach (string fallback in this.PathsToResourceFiles)
                    {
                        if (!Directory.Exists(fallback))
                            continue;
                        path_candidate = (string.IsNullOrEmpty(diff)) ? corrected_full : _full_path.Replace(diff, fallback + Path.DirectorySeparatorChar);
                        if (File.Exists(path_candidate) || Directory.Exists(path_candidate))
                        {
                            corrected_full = path_candidate;
                            corrected_is_contained = false;
                        }
                    }
                }
            }

            // 4. last attempt to recover a falsely saved contained resource
            if (consistent && _is_contained && corrected_is_contained.HasValue && !corrected_is_contained.Value && !string.IsNullOrEmpty(corrected_relative))
            {
                string diff = corrected_full.Replace(corrected_relative, string.Empty);
                if (!string.IsNullOrEmpty(diff))
                {
                    string path_candidate = corrected_full.Replace(diff, this.WorkingDirectory + Path.DirectorySeparatorChar);
                    if (File.Exists(path_candidate) || Directory.Exists(path_candidate))
                    {
                        corrected_full = path_candidate;
                        corrected_is_contained = true;
                    }
                }
            }

            return (consistent, corrected_full, corrected_relative, corrected_is_contained);
        }

        #endregion

        #region ASSET MANAGEMENT

        /// <summary>
        /// Creates a geometric asset from a resource file with the given key and referring to the given geometric id.
        /// The asset is included in the component with the given id.
        /// </summary>
        /// <param name="componentId">the id of the component asking for the asset</param>
        /// <param name="resourceKey">the resource key</param>
        /// <param name="geometryId">the id of the geometry the asset refers to</param>
        /// <returns>the created asset</returns>
        public GeometricAsset CreateGeometricAsset(long componentId, int resourceKey, string geometryId)
        {
            if (!this.resource_look_up.ContainsKey(resourceKey))
                throw new ArgumentException("Resource key does not exist");

            // look for a duplicate...
            GeometricAsset duplicate = null;
            if (this.Assets.TryGetValue(resourceKey, out var potentialDuplicates))
            {
                duplicate = (GeometricAsset)potentialDuplicates.FirstOrDefault(
                    x => x is GeometricAsset && x.ResourceKey == resourceKey && x.ContainedObjectId == geometryId);
            }
            else
                this.Assets.Add(resourceKey, new ElectivelyObservableCollection<Asset>());

            if (duplicate != null)
            {
                // ... and pass it on
                duplicate.AddReferencing(componentId);
                return duplicate;
            }
            else
            {
                GeometricAsset created = new GeometricAsset(this, componentId, resourceKey, geometryId);
                this.Assets[resourceKey].Add(created);
                return created;
            }
        }

        public DocumentAsset CreateDocumentAsset(SimComponent Component, ResourceFileEntry file, string _id_contained)
        {
            if (!this.resource_look_up.ContainsKey(file.Key))
                throw new ArgumentException("Resource key does not exist");

            if (Component == null)
                throw new ArgumentNullException(string.Format("{0} may not be null", nameof(Component)));
            if (file == null)
                throw new ArgumentNullException(string.Format("{0} may not be null", nameof(file)));

            ElectivelyObservableCollection<Asset> assetList = null;
            if (!this.Assets.TryGetValue(file.Key, out assetList))
            {
                assetList = new ElectivelyObservableCollection<Asset>();
                this.Assets.Add(file.Key, assetList);
            }

            var duplicate = (DocumentAsset)assetList.FirstOrDefault(
                x => x is DocumentAsset && x.ResourceKey == file.Key && x.ContainedObjectId == _id_contained
                );
            if (duplicate != null)
            {
                duplicate.AddReferencing(Component.Id.LocalId);
                return duplicate;
            }
            else
            {
                DocumentAsset created = new DocumentAsset(this, Component.Id.LocalId, file.Key, _id_contained);
                assetList.Add(created);
                return created;
            }
        }

        /// <summary>
        /// Removes the given asset from the manager, if no component references it.
        /// </summary>
        /// <param name="_a">the asset to be removed</param>
        public void RemoveAsset(Asset _a)
        {
            if (_a == null) return;
            // do not remove still referenced assets
            if (_a.ReferencingComponentIds.Count > 0) return;
            if (this.Assets.ContainsKey(_a.ResourceKey))
            {
                this.Assets[_a.ResourceKey].Remove(_a);
            }
        }

        /// <summary>
        /// Removes the given asset from all the referencing components, NOT from the manager.
        /// </summary>
        /// <param name="_a">the asset to remove</param>
        internal void RemoveReferencedAsset(Asset _a)
        {
            if (_a == null) return;
            List<long> ref_ids = new List<long>(_a.ReferencingComponentIds);
            foreach (long id in ref_ids)
            {
                SimComponent comp = this.ProjectData.IdGenerator.GetById<SimComponent>(new SimId(this.ProjectData.Owner, id));
                if (comp != null)
                {
                    comp.RemoveAsset(_a);
                }
            }
        }

        #endregion

        #region SPLITTING for PUBLIC and PRIVATE SAVING

        internal (List<ResourceEntry> resPublic, List<ResourceEntry> resPrivate, List<Asset> assetsPublic, List<Asset> assetsPrivate) SplitIntoPublicAndPrivateItems()
        {
            var resPublic = new List<ResourceEntry>();
            var resPrivate = new List<ResourceEntry>();
            var assetsPublic = new List<Asset>();
            var assetsPrivate = new List<Asset>();

            // evaluate the resources - the public ones are public, others may become so due to usage by a public component
            foreach (var entry in this.resource_look_up)
            {
                if (entry.Value.Visibility == SimComponentVisibility.AlwaysVisible)
                    resPublic.Add(entry.Value);
                else
                    resPrivate.Add(entry.Value);
            }

            // evaluate the assets
            foreach (var entry in this.Assets)
            {
                ResourceEntry res = this.GetResource(entry.Key);
                if (res != null && res.Visibility == SimComponentVisibility.AlwaysVisible)
                {
                    assetsPublic.AddRange(entry.Value);
                    continue;
                }

                foreach (Asset a in entry.Value)
                {
                    if (a.ReferencingComponentIds.Count > 0)
                    {
                        var cs = a.ReferencingComponentIds.Select(x => this.ProjectData.IdGenerator.GetById<SimComponent>(new SimId(x)))
                            .Where(x => x != null).Distinct();
                        foreach (SimComponent c in cs)
                        {
                            bool isPublic = false;
                            if (c.Parent == null)
                                isPublic = c.Visibility == SimComponentVisibility.AlwaysVisible;
                            else
                                isPublic = ComponentWalker.GetParents(c).Last().Visibility == SimComponentVisibility.AlwaysVisible;

                            if (isPublic)
                            {
                                if (!assetsPublic.Contains(a))
                                    assetsPublic.Add(a);
                                if (a.Resource.Visibility != SimComponentVisibility.AlwaysVisible && !resPublic.Contains(a.Resource))
                                {
                                    resPublic.Add(a.Resource);
                                    resPrivate.Remove(a.Resource);
                                }
                            }
                            else
                                assetsPrivate.Add(a);
                        }
                    }
                }
            }

            return (resPublic, resPrivate, assetsPublic, assetsPrivate);
        }

        #endregion
    }
}
