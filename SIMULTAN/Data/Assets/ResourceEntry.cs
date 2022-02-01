using SIMULTAN.Data.Components;
using SIMULTAN.Data.Users;
using SIMULTAN.Serializer.DXF;
using SIMULTAN.Utils.Files;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.Assets
{
    /// <summary>
    /// The base class for all resource entries - contained and linked files as well as directories.
    /// </summary>
    public abstract class ResourceEntry : INotifyPropertyChanged, IEquatable<ResourceEntry>
    {
        #region PROPERTIES: INotifyPropertyChanged

        /// <summary>
        /// Handler for the PropertyChanged event.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;
        /// <summary>
        /// Emits the PropertyChanged event.
        /// </summary>
        /// <param name="_propName">the name of the property</param>
        protected void NotifyPropertyChanged(string _propName)
        {
            if (_propName == null)
                return;

            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(_propName));
        }

        #endregion

        #region EVENTS: FileSystemWatcher

        /// <summary>
        /// Event handler to listen to changes to of the Resource entry. 
        /// Called when the underlying resource changes and needs to be reimported.
        /// For example when a file is updated on the file system level.
        /// </summary>
        /// <param name="sender">The ResourceEntry that called the event.</param>
        public delegate void ResourceChangedEventHandler(object sender);
        /// <summary>
        /// Event handler to listen to changes to of the Resource entry. 
        /// Called when the underlying resource changes and needs to be reimported.
        /// For example when a file is updated on the file system level.
        /// </summary>
        public event ResourceChangedEventHandler ResourceChanged;


        /// <summary>
        /// Event handler to listen to the deletion of the resource
        /// </summary>
        /// <param name="sender">The ResourceEntry that called the event.</param>
        public delegate void ResourceEntryDeletingEventHandler(object sender);
        /// <summary>
        /// Event to listen to the deletion of the resource
        /// </summary>
        public event ResourceEntryDeletingEventHandler Deleting;
        /// <summary>
        /// Invokes the deleting event
        /// </summary>
        public void OnDeleting()
        {
            this.Deleting?.Invoke(this);
        }

        /// <summary>
        /// The event handler to listen to finished delete operation
        /// </summary>
        /// <param name="sender">The ResourceEntry that called the event</param>
        public delegate void ResourceEntryDeletedEventHandler(object sender);
        /// <summary>
        ///  Event to listen to finished delete operation
        /// </summary>
        public event ResourceEntryDeletedEventHandler Deleted;
        /// <summary>
        /// Invokes the Deleted event
        /// </summary>
        public void OnDeleted()
        {
            this.Deleted?.Invoke(this);
        }

        /// <summary>
        /// Emits the ResourceChanged Event.
        /// </summary>
        public void OnResourceChanged()
        {
            ResourceChanged?.Invoke(this);
        }

        #endregion

        #region PROPERTIES

        /// <summary>
        /// The owner of the resource file.
        /// </summary>
        public SimUserRole UserWithWritingAccess
        {
            get { return this.user_with_writing_access; }
            set
            {
                this.user_with_writing_access = value;
                this.NotifyPropertyChanged(nameof(UserWithWritingAccess));
            }
        }
        /// <summary>
        /// The field for the property UserWithWritingAccess.
        /// </summary>
        protected SimUserRole user_with_writing_access;

        /// <summary>
        /// The index of the resource file entry in the <see cref="AssetManager"/>.
        /// </summary>
        public int Key { get; }

        /// <summary>
        /// Returns the name of the resource.
        /// </summary>
        public virtual string Name { get; protected set; }

        /// <summary>
        /// Saves the relative path of the currently loaded resource (file or directory). If the resource is only linked it can be relative to a folder outside the working directory.
        /// Otherwise it must be relative to the working directory.
        /// </summary>
        public string CurrentRelativePath
        {
            get { return this.current_relative_path; }
            set
            {
                if (this.current_relative_path != value)
                {
                    this.current_relative_path = value;
                    this.NotifyPropertyChanged(nameof(CurrentRelativePath));
                    this.SetFullResourcePath(this.current_relative_path, this.GetType() != typeof(LinkedResourceFileEntry), false);
                }
            }
        }
        /// <summary>
        /// The filed corresponding to property CurrentRelativePath.
        /// </summary>
        protected string current_relative_path;
        /// <summary>
        /// The path relative to which the relative path is calculated.
        /// </summary>
        protected string current_anchor_of_relative_path;

        /// <summary>
        /// Saves the full path of the currently loaded resource (file or directory). If the resource is only linked it can be outside the working directory.
        /// Otherwise it must be in the working directory. If this path is not valid, it contains an invalid token "?".
        /// </summary>
        public string CurrentFullPath
        {
            get { return this.current_full_path; }
            protected set
            {
                if (this.current_full_path != value)
                {
                    var old_value = this.current_full_path;
                    this.current_full_path = value;
                    this.NotifyPropertyChanged(nameof(CurrentFullPath));

                    this.manager.OnResourceRenamed(old_value, current_full_path);
                }
            }
        }
        /// <summary>
        /// The filed corresponding to the property CurrentFullPath.
        /// </summary>
        protected string current_full_path;

        /// <summary>
        /// True if the resource exists in the file system. False otherwise.
        /// </summary>
		public abstract bool Exists { get; }

        /// <summary>
        /// Indicates if the resource can be renamed.
        /// </summary>
        public abstract bool CanBeRenamed { get; }
        /// <summary>
        /// Indicates if the resource can be moved to a different location.
        /// </summary>
        public abstract bool CanBeMoved { get; }


        /// <summary>
        /// The visibility of the resource within and beyond a project. Only top-level resources can have their visibility set.
        /// It is then propagated to all contained resources.
        /// </summary>
        public SimComponentVisibility Visibility
        {
            get { return this.visibility; }
            set
            {
                if (this.Parent == null && this.visibility != value)
                {
                    this.visibility = value;
                    this.NotifyPropertyChanged(nameof(Visibility));
                    this.PropagateVisibilityToChildren();
                }
            }
        }
        /// <summary>
        /// The field corresponding to the property Visibility.
        /// </summary>
        protected SimComponentVisibility visibility = SimComponentVisibility.VisibleInProject;

        /// <summary>
        /// Propagates the visibility of the parent resource to all contained resources.
        /// </summary>
        protected virtual void PropagateVisibilityToChildren() { }

        #endregion

        #region PROPERTIES: Structure

        /// <summary>
        /// The containing resource. If there is none, this property has value NULL.
        /// </summary>
        public ResourceEntry Parent
        {
            get { return this.parent; }
            internal set
            {
                if (this.parent != null)
                {
                    this.parent.PropertyChanged -= Parent_PropertyChanged;
                }
                this.parent = value;
                if (this.parent != null)
                {
                    this.parent.PropertyChanged += Parent_PropertyChanged;
                }
                this.NotifyPropertyChanged(nameof(Parent));
            }
        }
        private ResourceEntry parent;

        /// <summary>
        /// Reacts to changes in the parent.
        /// </summary>
        /// <param name="sender">the parent</param>
        /// <param name="e">the event information</param>
        protected virtual void Parent_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            //Needs to stay because it gets overriden
        }

        #endregion

        #region FIELDS

        /// <summary>
        /// The manager of all resources and the assets contained in them.
        /// </summary>
        protected AssetManager manager;

        #endregion

        /// <summary>
        /// Initializes the resource entry.
        /// </summary>
        /// <param name="_manger">the manager for the entry</param>
        /// <param name="_user">the user who created it</param>
        /// <param name="_key">the key (or index) in the manager</param>
        protected ResourceEntry(AssetManager _manger, SimUserRole _user, int _key)
        {
            this.UserWithWritingAccess = _user;
            this.Key = _key;
            this.manager = _manger;
            this.current_relative_path = AssetManager.PATH_NOT_FOUND;
            this.current_full_path = AssetManager.PATH_NOT_FOUND;

            this.visibility = SimComponentVisibility.AlwaysVisible;
        }

        #region METHODS: Path handling

        /// <summary>
        /// Calculates the path to the resource relative to the given directory path.
        /// </summary>
        /// <param name="_absolute_path">the absolute path to the file or directory</param>
        /// <param name="_from_absolute_path">the absolute path from which to calculate the relative one</param>
        /// <param name="_notify">if true, trigger the emission of events</param>
        internal void SetRelativeResourcePath(string _absolute_path, string _from_absolute_path, bool _notify = true)
        {
            string path = FileSystemNavigation.GetRelativePath(_from_absolute_path, _absolute_path);

            if (_notify)
                this.CurrentRelativePath = path;
            else
                this.current_relative_path = path;
            this.current_anchor_of_relative_path = _from_absolute_path;
        }

        /// <summary>
        /// Searches for the full path based on the given relative path, on the current working directory and on the
        /// fallback resource paths. The first valid path is taken.
        /// </summary>
        /// <param name="_relative_path">the relative path to the file or directory to look for</param>
        /// <param name="_look_only_in_working_dir">if true, look for the file only in the working directory, if false, use the fallback resource paths as well</param>
        /// <param name="_notify">if true, trigger the emission of events</param>
        /// <returns>true if the path was distinct, false if there were multiple choices for a valid path</returns>
        internal bool SetFullResourcePath(string _relative_path, bool _look_only_in_working_dir, bool _notify = true)
        {
            string path = string.IsNullOrEmpty(this.current_full_path) ? AssetManager.PATH_NOT_FOUND : this.current_full_path;
            bool path_is_distinct = true;

            // check if the path is valid already
            if (File.Exists(path) || Directory.Exists(path))
            {
                if (_notify)
                    this.CurrentFullPath = path;
                else
                    this.current_full_path = path;
                return path_is_distinct;
            }

            // if not, look in the current directory first...
            path = FileSystemNavigation.ReconstructFullPath(this.manager.WorkingDirectory, _relative_path, false);
            bool path_was_set = !string.Equals(path, _relative_path, StringComparison.InvariantCultureIgnoreCase);
            if (!path_was_set && !_look_only_in_working_dir)
            {
                // ... then in each fallback directory
                foreach (string fallback in this.manager.PathsToResourceFiles)
                {
                    if (!Directory.Exists(fallback))
                        continue;
                    string path_new = FileSystemNavigation.ReconstructFullPath(fallback, _relative_path, false);
                    if (!string.Equals(path_new, _relative_path, StringComparison.InvariantCultureIgnoreCase))
                    {
                        if (!path_was_set)
                        {
                            path = path_new;
                            path_was_set = true;
                        }
                        else
                        {
                            path_is_distinct = false;
                        }
                    }
                }
            }

            // set the full path only if it is valid
            if (File.Exists(path) || Directory.Exists(path))
            {
                if (_notify)
                    this.CurrentFullPath = path;
                else
                    this.current_full_path = path;
            }
            //else
            //    throw new ArgumentException("Could not reconstruct the full path using the given input!");

            return path_is_distinct;
        }

        internal virtual void AdaptPathToWorkingDirectory(string _working_directory_full_path_new)
        {
            if (!Directory.Exists(_working_directory_full_path_new))
                throw new ArgumentException("Invalid working directory!");
        }

        /// <summary>
        /// Checks if the path to the resource can be replaced. For directories it should not be possible,
        /// for files only if the new path is in the manager's working directory,
        /// for links only if the new path is in one of the fallback directories.
        /// </summary>
        /// <param name="_replacement_path">the replacement candidtae</param>
        /// <returns>true id replacement is possible, false otherwise</returns>
        public virtual bool CanReplacePath(string _replacement_path)
        {
            return false;
        }

        internal virtual void ReplacePath(string _new_full_path, bool _notify)
        { }


        /// <inheritdoc/>
        public override string ToString()
        {
            return this.GetType() + " " + this.Name + ": r\"" + this.CurrentRelativePath + "\" a\"" + this.CurrentFullPath + "\"";
        }

        #endregion

        #region METHODS: Persistence

        internal virtual void ExportTo(StringBuilder _sb, int _key)
        {
            if (_sb == null) return;

            _sb.AppendLine(((int)AssetSaveCode.APATH_USER).ToString());
            _sb.AppendLine(ComponentUtils.ComponentManagerTypeToLetter(this.UserWithWritingAccess));

            _sb.AppendLine(((int)AssetSaveCode.APATH_KEY).ToString());
            _sb.AppendLine(_key.ToString());
        }

        internal virtual void ExportAsObjectTo(StringBuilder _sb)
        {
            if (_sb == null) return;
        }

        /// <summary>
        /// Exports the common properties of all subtypes of ResourceEntry.
        /// </summary>
        /// <param name="_sb">the responsible string builder</param>
        /// <param name="_export_full_path">if true, export the full path (i.e. only for linked resources)</param>
        protected void ExportCommon(StringBuilder _sb, bool _export_full_path)
        {
            string tmp;

            _sb.AppendLine(((int)ResourceSaveCode.RESOURCE_USER).ToString());
            _sb.AppendLine(ComponentUtils.ComponentManagerTypeToLetter(this.UserWithWritingAccess));

            _sb.AppendLine(((int)ResourceSaveCode.RESOURCE_KEY).ToString());
            _sb.AppendLine(this.Key.ToString());

            _sb.AppendLine(((int)ResourceSaveCode.RESOURCE_NAME).ToString());
            _sb.AppendLine(this.Name);

            _sb.AppendLine(((int)ResourceSaveCode.RESOURCE_RELATIVE).ToString());
            _sb.AppendLine(this.CurrentRelativePath);

            _sb.AppendLine(((int)ResourceSaveCode.RESOURCE_ANCHOR).ToString());
            tmp = (_export_full_path) ? this.current_anchor_of_relative_path : AssetManager.PATH_NOT_FOUND;
            _sb.AppendLine(tmp);

            _sb.AppendLine(((int)ResourceSaveCode.RESOURCE_FULL).ToString());
            tmp = (_export_full_path) ? this.CurrentFullPath : AssetManager.PATH_NOT_FOUND;
            _sb.AppendLine(tmp);

            _sb.AppendLine(((int)ResourceSaveCode.RESOURCE_HAS_PARENT).ToString());
            tmp = (this.Parent != null) ? "1" : "0";
            _sb.AppendLine(tmp);

            _sb.AppendLine(((int)ResourceSaveCode.RESOURCE_VISIBILITY).ToString());
            _sb.AppendLine(this.Visibility.ToString());
        }

        #endregion

        #region METHODS: rename, copy

        // ------------------------------------------------ CHECKS -------------------------------------------------- //

        /// <summary>
        /// Checks if the location of the resource can be replaced by the given new location.
        /// </summary>
        /// <param name="_new_location">the new location to replace the old</param>
        /// <param name="nameCollisionFormat">The format of the new filename. Used with a string.Format call.
        /// The format is only used when the original name of the file is not admissible.
        /// Arguments:
        ///   {0}: The original filename without extension
        ///   {1}: A running counter
        /// </param>
        /// <returns>a boolean to indicate if the given name is admissible, if it is not proposed_name contains an admissible alternative</returns>
        public virtual (bool admissible, string proposed_name) CanChangeLocation(DirectoryInfo _new_location, string nameCollisionFormat)
        {
            return (false, null);
        }

        /// <summary>
        /// Checks if the path of the resource can be replaced by the given new path.
        /// </summary>
        /// <param name="_new_data">the new name to replace the old</param>
        /// <param name="nameCollisionFormat">The format of the new filename. Used with a string.Format call.
        /// The format is only used when the original name of the file is not admissible.
        /// Arguments:
        ///   {0}: The original filename without extension
        ///   {1}: A running counter
        /// </param>
        /// <returns>a boolean to indicate if the given name is admissible, if it is not proposed_name contains an admissible alternative</returns>
        public virtual (bool admissible, string proposed_name) CanChangePath(FileSystemInfo _new_data, string nameCollisionFormat)
        {
            return (false, null);
        }

        // ------------------------------------------------ UTILS --------------------------------------------------- //

        /// <summary>
        /// Changes the entire path of the resource. This could mean a new path and / or extension.
        /// </summary>
        /// <param name="_new_data">the new name to replace the old</param>
        /// <param name="nameCollisionFormat">The format of the new filename. Used with a string.Format call.
        /// The format is only used when the original name of the file is not admissible.
        /// Arguments:
        ///   {0}: The original filename without extension
        ///   {1}: A running counter
        /// </param>
        /// <param name="_check_admissibility">if true, call the <see cref="CanChangePath(FileSystemInfo, string)"/> function first and use the alternative name, if necessary</param>
        /// <returns>a list of directory resources if a structural change was necessary</returns>
        internal virtual List<ResourceDirectoryEntry> ChangePath_Internal(FileSystemInfo _new_data, string nameCollisionFormat, bool _check_admissibility)
        {
            return null;
        }

        /// <summary>
        /// Changes the name of the resources, *not* the extension or the entire path.
        /// </summary>
        /// <param name="_new_name">the new name (not the new path!)</param>
        /// <param name="nameCollisionFormat">The format of the new filename. Used with a string.Format call.
        /// The format is only used when the original name of the file is not admissible.
        /// Arguments:
        ///   {0}: The original filename without extension
        ///   {1}: A running counter
        /// </param>
        internal virtual void ChangeName_Internal(string _new_name, string nameCollisionFormat)
        { }

        /// <summary>
        /// Gives information about the given location relating to the working directory.
        /// </summary>
        /// <param name="_location">the location</param>
        /// <param name="_location_exists">if true, the location can be found in the file system</param>
        /// <returns>if the location is the working directory and if it is an actual subdirectory of the working directory</returns>
        protected (bool is_working_dir, bool is_subdir_of_working_dir, bool location_parent_match) CheckLocation(DirectoryInfo _location, bool _location_exists)
        {
            bool is_working_dir = string.Equals(_location.FullName, this.manager.WorkingDirectory, StringComparison.InvariantCultureIgnoreCase);
            bool is_subdir_of_working_dir = FileSystemNavigation.IsSubdirectoryOf(this.manager.WorkingDirectory, _location.FullName, _location_exists);

            bool location_parent_match = false;
            if (_location_exists)
                location_parent_match = is_working_dir ? (this.Parent == null) : string.Equals(this.Parent.CurrentFullPath, _location.FullName, StringComparison.InvariantCultureIgnoreCase);

            return (is_working_dir, is_subdir_of_working_dir, location_parent_match);
        }

        // ---------------------------------- METHODS THAT CAUSE EVENT EMISSION ------------------------------------- //

        /// <summary>
        /// Changes the location of the resource.
        /// </summary>
        /// <param name="_new_location">the new location to replace the old, null indicates the working directory of the AssetManager</param>
        /// <param name="_check_admissibility">if true, call the <see cref="CanChangePath(FileSystemInfo, string)"/> function first and use the alternative name, if necessary</param>
        /// <param name="nameCollisionFormat">The format of the new filename. Used with a string.Format call.
        /// The format is only used when the original name of the file is not admissible.
        /// Arguments:
        ///   {0}: The original filename without extension
        ///   {1}: A running counter
        /// </param>
        /// <returns>a list of directory resources if a structural change was necessary</returns>
        public virtual void ChangeLocation(DirectoryInfo _new_location, string nameCollisionFormat, bool _check_admissibility)
        { }

        /// <summary>
        /// Changes the entire path of the resource. This could mean a new path and / or extension. The asset manager parent
        /// emits the ResourceManipulated event.
        /// </summary>
        /// <param name="_new_data">the new name to replace the old</param>
        /// <param name="nameCollisionFormat">The format of the new filename. Used with a string.Format call.
        /// The format is only used when the original name of the file is not admissible.
        /// Arguments:
        ///   {0}: The original filename without extension
        ///   {1}: A running counter
        /// </param>
        /// <param name="_check_admissibility">if true, call the <see cref="CanChangePath(FileSystemInfo, string)"/> function 
        /// first and use the alternative name, if necessary</param>
        public virtual void ChangePath(FileSystemInfo _new_data, string nameCollisionFormat, bool _check_admissibility)
        { }

        /// <summary>
        /// Used when the resource changed externally on a file system level.
        /// Changes the entire path of the resource. This could mean a new path and / or extension. The asset manager parent
        /// emits the ResourceManipulated event.
        /// </summary>
        /// <param name="_new_data">the new name to replace the old</param>
        public virtual void PathChangedExternally(FileSystemInfo _new_data)
        { }

        /// <summary>
        /// Changes the name of the resources, *not* the extension or the entire path. The asset manager parent
        /// emits the ResourceManipulated event.
        /// </summary>
        /// <param name="_new_name">the new name (not the new path!)</param>
        /// <param name="nameCollisionFormat">The format of the new filename. Used with a string.Format call.
        /// The format is only used when the original name of the file is not admissible.
        /// Arguments:
        ///   {0}: The original filename without extension
        ///   {1}: A running counter
        /// </param>
        public virtual void ChangeName(string _new_name, string nameCollisionFormat)
        { }

        #endregion

        #region METHODS: very bad terrible unspeakable OO

        internal bool IsMyManager(AssetManager _manager)
        {
            if (_manager == null)
                return false;

            return this.manager == _manager;
        }

        #endregion


        #region Equality

        /// <inheritdoc />
        public bool Equals(ResourceEntry other)
        {
            if (other != null)
            {
                return other.Key == this.Key && other.manager == this.manager;
            }

            return false;
        }
        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (obj is ResourceEntry re)
                return this.Equals(re);
            return false;
        }
        /// <inheritdoc />
        public static bool operator ==(ResourceEntry e1, ResourceEntry e2)
        {
            if (e1 is null && e2 is null)
                return true;
            if (e1 is null || e2 is null)
                return false;
            return e1.Equals(e2);
        }
        /// <inheritdoc />
        public static bool operator !=(ResourceEntry e1, ResourceEntry e2)
        {
            return !(e1 == e2);
        }
        /// <inheritdoc />
        public override int GetHashCode()
        {
            return this.Key ^ this.manager.GetHashCode();
        }

        #endregion
    }



}
