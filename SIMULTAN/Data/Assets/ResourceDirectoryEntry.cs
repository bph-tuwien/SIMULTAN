using SIMULTAN;
using SIMULTAN.Data.Users;
using SIMULTAN.Serializer.DXF;
using SIMULTAN.Utils;
using SIMULTAN.Utils.Collections;
using SIMULTAN.Utils.Files;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.Assets
{
    /// <summary>
    /// Manages the entry for the directory in which contained resource files reside. Such directories should be in the working directory.
    /// </summary>
    public class ResourceDirectoryEntry : ResourceEntry
    {
        #region PROPERTIES

        private DirectoryInfo directory;

        /// <summary>
        /// Holds the entries corresponding to files contained in the directory.
        /// </summary>
        public ElectivelyObservableCollection<ResourceEntry> Children { get; }

        /// <inheritdoc/>
        public override string Name
        {
            get { return directory.Name; }
            protected set
            {
                
            }
        }
        /// <inheritdoc/>
        public override bool CanBeRenamed => true;
        /// <inheritdoc/>
        public override bool CanBeMoved => true;

        /// <inheritdoc/>
        protected override void PropagateVisibilityToChildren()
        {
            foreach (var child in this.Children)
            {
                child.Visibility = this.Visibility;
            }
        }

        #endregion

        internal ResourceDirectoryEntry(AssetManager _manger, SimUserRole _user, string _dir_path, bool _path_is_absolute, int _key, bool _exists = true)
            : base(_manger, _user, _key)
        {
            if (_path_is_absolute)
            {
                this.directory = new DirectoryInfo(_dir_path);
                if (_exists)
                {
                    if (Directory.Exists(_dir_path))
                        this.current_full_path = _dir_path;
                    else
                        throw new ArgumentException("A folder with the given name does not exist");
                }
                else
                {
                    this.current_full_path = _dir_path;
                }
                this.SetRelativeResourcePath(_dir_path, this.manager.WorkingDirectory, false);
            }
            else
            {
                this.current_relative_path = _dir_path;
                // this case comes up only if the project containing the resource in only partially parsed (e.g., during SaveAs)
                this.current_full_path = FileSystemNavigation.ReconstructFullPath(this.manager.WorkingDirectory, _dir_path, false);
                if (this.current_full_path == AssetManager.PATH_NOT_FOUND)
                    this.directory = null;
                else
                    this.directory = new DirectoryInfo(this.current_full_path);
            }

            this.Children = new ElectivelyObservableCollection<ResourceEntry>();
            this.Children.ElectiveCollectionChanged += Children_ElectiveCollectionChanged;
        }

        /// <inheritdoc/>
		public override bool Exists
        {
            get
            {
                if (this.directory == null)
                    return false;
                return this.directory.Exists;
            }
        }


        #region METHODS: containment

        /// <summary>
        /// Finds the child on the lowest possible level that can act as a container for a newly created resource entry 
        /// that resides in the given directory and has not been placed in the hierarchy yet.
        /// </summary>
        /// <param name="_dir_of_new_entry">the immediate containing directory of the new resource entry</param>
        /// <returns>the container candidate at lowest level</returns>
        public ResourceDirectoryEntry DeepestContainingEntry(DirectoryInfo _dir_of_new_entry)
        {
            bool is_contained = FileSystemNavigation.IsSubdirectoryOf(this.CurrentFullPath, _dir_of_new_entry.FullName) || string.Equals(this.CurrentFullPath, _dir_of_new_entry.FullName, StringComparison.InvariantCultureIgnoreCase);
            if (!is_contained)
                return null;
            else
            {
                foreach (ResourceEntry child in this.Children)
                {
                    if (!(child is ResourceDirectoryEntry))
                        continue;
                    ResourceDirectoryEntry container = (child as ResourceDirectoryEntry).DeepestContainingEntry(_dir_of_new_entry);
                    if (container != null)
                        return container;
                }
                return this;
            }
        }

        /// <summary>
        /// Returns a list of the keys of all resources contained in this directory.
        /// </summary>
        /// <returns>a list of unique integer keys</returns>
        public List<int> GetContainedKeys()
        {
            List<int> all_keys = new List<int>();
            foreach (var child in this.Children)
            {
                all_keys.Add(child.Key);
                if (child is ResourceDirectoryEntry)
                {
                    all_keys.AddRange((child as ResourceDirectoryEntry).GetContainedKeys());
                }
            }
            return all_keys;
        }

        internal ResourceEntry GetChildWithKey(int _key)
        {
            foreach (var child in this.Children)
            {
                if (child.Key == _key)
                    return child;
                if (child is ResourceDirectoryEntry)
                {
                    var found = (child as ResourceDirectoryEntry).GetChildWithKey(_key);
                    if (found != null)
                        return found;
                }
            }
            return null;
        }

        /// <summary>
        /// Returns a collection of all contained files and directories corresponding to this resource's children recursively.
        /// </summary>
        /// <returns>a dictionary of resource keys coupled with file system info objects</returns>
        public Dictionary<int, FileSystemInfo> GetFlatContainedContent()
        {
            Dictionary<int, FileSystemInfo> all_content = new Dictionary<int, FileSystemInfo>();
            foreach (var child in this.Children)
            {
                if (child is ContainedResourceFileEntry)
                {
                    if (child.Exists)
                        all_content.Add(child.Key, new FileInfo(child.CurrentFullPath));
                }
                else if (child is ResourceDirectoryEntry)
                {
                    if (child.Exists)
                    {
                        all_content.Add(child.Key, new DirectoryInfo(child.CurrentFullPath));
                        var child_content = (child as ResourceDirectoryEntry).GetFlatContainedContent();
                        foreach (var entry in child_content)
                            all_content.Add(entry.Key, entry.Value);
                    }
                }
            }
            return all_content;
        }

        /// <summary>
        /// Retrieves all existing linked files in the children hierarchy.
        /// </summary>
        /// <returns>a list of existing files</returns>
        public List<FileInfo> GetFlatLinks()
        {
            List<FileInfo> links = new List<FileInfo>();
            foreach (ResourceEntry re in this.Children)
            {
                if (re is LinkedResourceFileEntry && File.Exists(re.CurrentFullPath))
                    links.Add(new FileInfo(re.CurrentFullPath));
                else if (re is ResourceDirectoryEntry)
                    links.AddRange((re as ResourceDirectoryEntry).GetFlatLinks());
            }
            return links;
        }

        #endregion

        #region METHOD: path handling
        /// <inheritdoc/>
        public override bool CanReplacePath(string _replacement_path)
        {
            return false;
        }

        /// <inheritdoc/>
        protected override void Parent_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (this.Parent == null) return;

            if (e.PropertyName == nameof(CurrentFullPath))
            {
                // react to change in the full path of the parent resource:
                string new_full_path = this.Parent.CurrentFullPath + Path.DirectorySeparatorChar + this.directory.Name;

                this.directory = new DirectoryInfo(new_full_path);
                this.CurrentFullPath = new_full_path;
                this.SetRelativeResourcePath(new_full_path, this.manager.WorkingDirectory, false);
            }
        }

        #endregion

        #region METHODS: rename, copy

        // ------------------------------------------------ CHECKS -------------------------------------------------- //

        /// <inheritdoc/>
        public override (bool admissible, string proposed_name) CanChangeLocation(DirectoryInfo _new_location, string nameCollisionFormat)
        {
            string new_path = (_new_location == null) ? Path.Combine(this.manager.WorkingDirectory, this.directory.Name) : Path.Combine(_new_location.FullName, this.directory.Name);
            return this.CanChangePath(new DirectoryInfo(new_path), nameCollisionFormat);
        }

        /// <inheritdoc/>
        public override (bool admissible, string proposed_name) CanChangePath(FileSystemInfo _new_data, string nameCollisionFormat)
        {
            if (_new_data == null)
                throw new ArgumentNullException(nameof(_new_data));
            if (!(_new_data is DirectoryInfo))
                throw new ArgumentException("The new name has to be packed in a DirectoryInfo instance!", nameof(_new_data));

            DirectoryInfo new_dir = _new_data as DirectoryInfo;
            if (string.Equals(this.manager.WorkingDirectory, new_dir.FullName, StringComparison.InvariantCultureIgnoreCase))
                throw new ArgumentException("A resource directory cannot duplicate the project working directory!", nameof(_new_data));
            if (!FileSystemNavigation.IsSubdirectoryOf(this.manager.WorkingDirectory, new_dir.FullName, false))
                throw new ArgumentException("A resource directory cannot be located outside the project!", nameof(_new_data));

            return AdmissibilityQueries.DirectoryNameIsAdmissible(new_dir, x => !Directory.Exists(x), nameCollisionFormat);
        }

        // ------------------------------------------------ UTILS --------------------------------------------------- //

        /// <inheritdoc/>
        internal override List<ResourceDirectoryEntry> ChangePath_Internal(FileSystemInfo _new_data, string nameCollisionFormat, bool _check_admissibility)
        {
            List<ResourceDirectoryEntry> new_dirs = new List<ResourceDirectoryEntry>();
            DirectoryInfo dir_new = null;
            if (_check_admissibility)
            {
                var test = this.CanChangePath(_new_data, nameCollisionFormat);
                dir_new = new DirectoryInfo(test.proposed_name);
            }
            else
            {
                if (_new_data == null)
                    throw new ArgumentNullException(nameof(_new_data));
                if (!(_new_data is DirectoryInfo))
                    throw new ArgumentException("The new name has to be packed in a DirectoryInfo instance!", nameof(_new_data));

                dir_new = new DirectoryInfo(_new_data.FullName);
            }

            // check if the renaming causes a change in the structure
            DirectoryInfo parent_old = this.directory.Parent;
            DirectoryInfo parent_new = dir_new.Parent;

            if (string.Equals(parent_old.FullName, parent_new.FullName, StringComparison.InvariantCultureIgnoreCase))
            {
                // actually rename the directory
                Directory.Move(this.CurrentFullPath, dir_new.FullName);

                // no change in structure
                this.directory = dir_new;
                this.CurrentFullPath = dir_new.FullName;
                this.SetRelativeResourcePath(dir_new.FullName, this.manager.WorkingDirectory, false);

                // change for the children happens in the Parent setter called in the CollectionChanged Event Handler for Children
            }
            else
            {
                // 1. check the admissibility of the local change
                var parent_old_check = this.CheckLocation(parent_old, true);
                var parent_new_check = this.CheckLocation(parent_new, false);

                if (!parent_old_check.is_working_dir && !parent_old_check.is_subdir_of_working_dir)
                    throw new ArgumentException("Folders may not be moved to the working directory");
                if (!parent_new_check.is_working_dir && !parent_new_check.is_subdir_of_working_dir)
                    throw new ArgumentException("Folders may not be moved outside the working directory", nameof(_new_data));
                if (!parent_old_check.location_parent_match)
                    throw new Exception("Inconsistency with the resource parent. This should not happen!");

                // 2. before making structural changes...
                // 2a. remove from the old parent
                if (parent_old_check.is_working_dir)
                    this.manager.RemoveAsTopLevelResource(this);
                else
                    (this.Parent as ResourceDirectoryEntry).Children.Remove(this);

                // 2b. actually rename the directory
                Directory.Move(this.CurrentFullPath, dir_new.FullName);

                // 2c. change the entry itself
                this.directory = dir_new;
                this.CurrentFullPath = dir_new.FullName;
                this.SetRelativeResourcePath(dir_new.FullName, this.manager.WorkingDirectory, false);
                // change for the children happens in the Parent setter called in the CollectionChanged Event Handler for Children

                // 2d. add to new parent, if it is not the working directory
                if (parent_new_check.is_working_dir)
                {
                    this.manager.AddAsTopLevelResource(this);
                }
                else
                {
                    ResourceDirectoryEntry p = this.manager.GetResource(parent_new);
                    if (p == null || !(p is ResourceDirectoryEntry))
                    {
                        // create the directory
                        (_, var p_new, var all_new) = this.manager.CreateResourceDirFrom(parent_new);
                        if (p_new != null)
                        {
                            p_new.Children.Add(this);
                            new_dirs.AddRange(all_new);
                        }
                    }
                    else
                    {
                        p.Children.Add(this);
                    }
                }
            }

            // done
            return new_dirs;
        }

        /// <inheritdoc/>
        internal override void ChangeName_Internal(string _new_name, string nameCollisionFormat)
        {
            if (this.directory == null)
                throw new Exception("The resource corresponds to no valid file! It cannot be renamed.");

            // construct the new full path
            DirectoryInfo parent = this.directory.Parent;
            string path = parent.FullName + Path.DirectorySeparatorChar + _new_name;
            DirectoryInfo new_dir = null;
            try
            {
                new_dir = new DirectoryInfo(path);
            }
            catch (NotSupportedException ex)
            {
                throw new ArgumentException("An absolute path may not be used as a name", ex);
            }

            if (!string.Equals(new_dir.Parent.FullName, parent.FullName, StringComparison.InvariantCultureIgnoreCase))
                throw new ArgumentException("Changing the name may not move the file to a new folder");

            var admissible = this.CanChangePath(new_dir, nameCollisionFormat);
            this.ChangePath_Internal(new DirectoryInfo(admissible.proposed_name), nameCollisionFormat, false);
        }

        // ---------------------------------- METHODS THAT CAUSE EVENT EMISSION ------------------------------------- //

        /// <inheritdoc/>
        public override void ChangeLocation(DirectoryInfo _new_location, string nameCollisionFormat, bool _check_admissibility)
        {
            string new_path = (_new_location == null) ? Path.Combine(this.manager.WorkingDirectory, this.directory.Name) : Path.Combine(_new_location.FullName, this.directory.Name);
            if (string.Equals(new_path, this.CurrentFullPath))
                return;

            this.ChangePath(new DirectoryInfo(new_path), nameCollisionFormat, _check_admissibility);
        }

        /// <inheritdoc/>
        public override void ChangePath(FileSystemInfo _new_data, string nameCollisionFormat, bool _check_admissibility)
        {
            DirectoryInfo dir_old = new DirectoryInfo(this.CurrentFullPath);
            List<ResourceDirectoryEntry> additional_dir_res = this.ChangePath_Internal(_new_data, nameCollisionFormat, _check_admissibility);
            Dictionary<int, FileSystemInfo> content_old = this.GetFlatContainedContent();

            IEnumerable<DirectoryInfo> additional_dirs = additional_dir_res.Select(x => new DirectoryInfo(x.CurrentFullPath));

            DirectoryInfo dir_new = new DirectoryInfo(this.CurrentFullPath);
            Dictionary<int, FileSystemInfo> content_new = this.GetFlatContainedContent();

            //if (this.manager != null)
            //    this.manager.OnResourceManipulated(new ManipulatedResourceEventArgs(dir_old, dir_new, additional_dirs, content_old, content_new));
        }

        /// <inheritdoc/>
        public override void ChangeName(string _new_name, string nameCollisionFormat)
        {
            this.ChangeName_Internal(_new_name, nameCollisionFormat);
        }

        #endregion

        #region EVENT HANDLERS

        private void Children_ElectiveCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            object old_item = e.OldItems?[0];
            object new_item = e.NewItems?[0];
            if (e.Action == NotifyCollectionChangedAction.Add && new_item is ResourceEntry)
            {
                foreach (var item in e.NewItems)
                {
                    (item as ResourceEntry).Parent = this;
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove && old_item is ResourceEntry)
            {
                foreach (var item in e.OldItems)
                {
                    (item as ResourceEntry).Parent = null;
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Replace && new_item is ResourceEntry && old_item is ResourceEntry)
            {
                foreach (var item in e.OldItems)
                {
                    (item as ResourceEntry).Parent = null;
                }
                foreach (var item in e.NewItems)
                {
                    (item as ResourceEntry).Parent = this;
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                if (e.OldItems != null)
                {
                    foreach (var item in e.OldItems)
                    {
                        (item as ResourceEntry).Parent = null;
                    }
                }
                if (e.NewItems != null)
                {
                    foreach (var item in e.NewItems)
                    {
                        (item as ResourceEntry).Parent = this;
                    }
                }
            }
        }

        #endregion
    }
}
