using SIMULTAN;
using SIMULTAN.Data.Users;
using SIMULTAN.Serializer.DXF;
using SIMULTAN.Utils;
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
    /// Manages the entry for a resource file in the working directory.
    /// </summary>
    public class ContainedResourceFileEntry : ResourceFileEntry
    {
        /// <summary>
        /// Indicates that the file is not in the current working directory. Should not really happen!
        /// </summary>
        public bool FileIsNotReallyContained { get; private set; }
        /// <inheritdoc/>
        public override bool CanBeRenamed => true;
        /// <inheritdoc/>
        public override bool CanBeMoved => true;

        /// <inheritdoc/>
        public override bool Exists
        {
            get
            {
                return base.Exists && !IsMissing;
            }
        }

        /// <summary>
        /// Determines if the file is temporarily. 
        /// If it is marked as missing, Exists will be false.
        /// </summary>
        public bool IsMissing
        {
            get { return isMissing; }
            set
            {
                isMissing = value;
                NotifyPropertyChanged(nameof(IsMissing));
                NotifyPropertyChanged(nameof(Exists));
            }
        }
        private bool isMissing;

        /// <summary>
        /// Can be called with the absolute path (when first being set by the user) and with the relative
        /// path (during parsing).
        /// </summary>
        /// <param name="_manger">the asset manager that holds this resource</param>
        /// <param name="_user">the user with writing access</param>
        /// <param name="_file_path">the path to the file</param>
        /// <param name="_path_is_absolute">indicates if the path is absolute or relative</param>
        /// <param name="_key">the resource key, determined in advance by the asset manager or read from a file</param>
        /// <param name="_exists">indicates if the resource exists in the file system at the moment of instantiation</param>
        internal ContainedResourceFileEntry(AssetManager _manger, SimUserRole _user, string _file_path, bool _path_is_absolute, int _key, bool _exists = true)
            : base(_manger, _user, _file_path, _path_is_absolute, _key)
        {
            if (_path_is_absolute)
            {
                this.SetFullPath(_file_path, _exists);
            }
            else
            {
                this.current_relative_path = _file_path;
                this.SetFullResourcePath(_file_path, true, false);
                if (this.current_full_path == AssetManager.PATH_NOT_FOUND)
                {
                    this.FileIsNotReallyContained = true;
                    this.File = null;
                }
                else
                {
                    this.FileIsNotReallyContained = false;
                    this.File = new FileInfo(this.current_full_path);
                }
            }
            this.Name = (this.File == null) ? FileSystemNavigation.ExtractNameFromPath(_file_path) : this.File.Name;
        }

        #region METHODS: Paths

        /// <inheritdoc/>
        protected override void Parent_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (this.Parent == null) return;

            if (e.PropertyName == nameof(CurrentFullPath))
            {
                // react to change in the full path of the parent resource:
                string new_full_path = this.Parent.CurrentFullPath + Path.DirectorySeparatorChar + this.File.Name;
                this.SetFullPath(new_full_path, true, true);
                this.Name = (this.File == null) ? FileSystemNavigation.ExtractNameFromPath(new_full_path) : this.File.Name;
            }
        }

        internal override void AdaptPathToWorkingDirectory(string _working_directory_full_path_new)
        {
            base.AdaptPathToWorkingDirectory(_working_directory_full_path_new);

            // recalculate the absolute path
            if (!this.FileIsNotReallyContained)
            {
                this.SetFullResourcePath(this.current_relative_path, true, false);
                if (this.current_full_path == AssetManager.PATH_NOT_FOUND)
                    this.File = null;
                else
                    this.File = new FileInfo(this.current_full_path);
            }
            this.Name = (this.File == null) ? FileSystemNavigation.ExtractNameFromPath(this.current_relative_path) : this.File.Name;
        }

        /// <inheritdoc/>
        public override bool CanReplacePath(string _replacement_path)
        {
            if (!System.IO.File.Exists(_replacement_path))
                return false;

            FileInfo fi = new FileInfo(_replacement_path);
            DirectoryInfo di = fi.Directory;
            DirectoryInfo diW = new DirectoryInfo(this.manager.WorkingDirectory);
            bool replacement_contained = FileSystemNavigation.IsSubdirectoryOf(diW.FullName, di.FullName) || string.Equals(diW.FullName, di.FullName, StringComparison.InvariantCultureIgnoreCase);
            if (this.Parent != null)
            {
                DirectoryInfo diP = new DirectoryInfo(this.Parent.CurrentFullPath);
                replacement_contained &= FileSystemNavigation.IsSubdirectoryOf(diP.FullName, di.FullName) || string.Equals(diP.FullName, di.FullName, StringComparison.InvariantCultureIgnoreCase);
            }
            return replacement_contained;
        }

        internal override void ReplacePath(string _new_full_path, bool _notify)
        {
            base.ReplacePath(_new_full_path, _notify);
            this.SetFullPath(_new_full_path, _notify);
            this.Name = (this.File == null) ? FileSystemNavigation.ExtractNameFromPath(_new_full_path) : this.File.Name;
        }

        private void SetFullPath(string _file_path, bool _path_exists = true, bool _notify = false)
        {
            if (_notify)
                this.CurrentFullPath = _file_path;
            else
                this.current_full_path = _file_path;

            this.File = new FileInfo(_file_path);
            this.FileIsNotReallyContained = true;

            bool set_rel_path = false;
            DirectoryInfo di = this.File.Directory;
            DirectoryInfo diW = new DirectoryInfo(this.manager.WorkingDirectory);
            if (FileSystemNavigation.IsSubdirectoryOf(diW.FullName, di.FullName, _path_exists) || string.Equals(diW.FullName, di.FullName, StringComparison.InvariantCultureIgnoreCase))
            {
                this.SetRelativeResourcePath(this.File.FullName, this.manager.WorkingDirectory, false);
                set_rel_path = true;
                this.FileIsNotReallyContained = false;
            }
            else
            {
                // this shouldn't really happen!
                foreach (string fallback in this.manager.PathsToResourceFiles)
                {
                    if (!Directory.Exists(fallback))
                        continue;
                    DirectoryInfo diFB = new DirectoryInfo(fallback);
                    if (FileSystemNavigation.IsSubdirectoryOf(diFB.FullName, di.FullName, _path_exists) || string.Equals(diFB.FullName, di.FullName, StringComparison.InvariantCultureIgnoreCase))
                    {
                        this.SetRelativeResourcePath(this.File.FullName, diFB.FullName, false);
                        set_rel_path = true;
                        break;
                    }
                }
            }
            // if all else fails, set the relative path from the current working directory
            if (!set_rel_path)
                this.SetRelativeResourcePath(this.File.FullName, this.manager.WorkingDirectory, false);
        }

        #endregion

        #region METHODS: serialization

        internal override void ExportTo(StringBuilder _sb, int _key)
        {
            base.ExportTo(_sb, _key);
            _sb.AppendLine(((int)AssetSaveCode.APATH_ISCONTAINED).ToString());
            _sb.AppendLine("1");

            _sb.AppendLine(((int)AssetSaveCode.APATH_FULL_PATH).ToString());
            _sb.AppendLine(this.CurrentFullPath);

            _sb.AppendLine(((int)AssetSaveCode.APATH_REL_PATH).ToString());
            _sb.AppendLine(this.Name);
        }

        internal override void ExportAsObjectTo(StringBuilder _sb)
        {
            base.ExportAsObjectTo(_sb);

            _sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString()); // 0
            _sb.AppendLine(ParamStructTypes.RESOURCE_FILE);                           // RESOURCE_CONTAINED_FILE

            _sb.AppendLine(((int)ParamStructCommonSaveCode.CLASS_NAME).ToString());
            _sb.AppendLine(this.GetType().ToString());

            // export common part
            base.ExportCommon(_sb, false);

            _sb.AppendLine(((int)ResourceSaveCode.RESOURCE_PROBLEM).ToString());
            string tmp = (this.FileIsNotReallyContained) ? "1" : "0";
            _sb.AppendLine(tmp);

            // NOT A COMPLEX ENTITY - but necessary for correct traversal
            _sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString()); // 0
            _sb.AppendLine(ParamStructTypes.SEQUENCE_END);                            // SEQEND
        }

        #endregion

        #region METHODS: rename, copy

        // ------------------------------------------------ CHECKS -------------------------------------------------- //

        /// <inheritdoc/>
        public override (bool admissible, string proposed_name) CanChangeLocation(DirectoryInfo _new_location, string nameCollisionFormat)
        {
            string new_path = (_new_location == null) ? Path.Combine(this.manager.WorkingDirectory, this.File.Name) : Path.Combine(_new_location.FullName, this.File.Name);
            return this.CanChangePath(new FileInfo(new_path), nameCollisionFormat);
        }

        /// <inheritdoc/>
        public override (bool admissible, string proposed_name) CanChangePath(FileSystemInfo _new_data,
            string nameCollisionFormat)
        {
            if (_new_data == null)
                throw new ArgumentNullException(nameof(_new_data));
            if (!(_new_data is FileInfo))
                throw new ArgumentException("The new name has to be packed in a FileInfo instance!", nameof(_new_data));

            FileInfo new_file = _new_data as FileInfo;
            DirectoryInfo parent_dir = new_file.Directory;
            if (!FileSystemNavigation.IsSubdirectoryOf(this.manager.WorkingDirectory, parent_dir.FullName, false) && !string.Equals(this.manager.WorkingDirectory, parent_dir.FullName, StringComparison.InvariantCultureIgnoreCase))
                throw new ArgumentException("A contained resource cannot be located outside the project!", nameof(_new_data));

            return AdmissibilityQueries.FileNameIsAdmissible(new_file, x => !System.IO.File.Exists(x), nameCollisionFormat);
        }

        // ------------------------------------------------ UTILS --------------------------------------------------- //

        /// <inheritdoc/>
        internal override List<ResourceDirectoryEntry> ChangePath_Internal(FileSystemInfo _new_data, string nameCollisionFormat, bool _check_admissibility)
        {
            List<ResourceDirectoryEntry> new_dirs = new List<ResourceDirectoryEntry>();
            FileInfo file_new = null;
            if (_check_admissibility)
            {
                var test = this.CanChangePath(_new_data, nameCollisionFormat);
                file_new = new FileInfo(test.proposed_name);
            }
            else
            {
                if (_new_data == null)
                    throw new ArgumentNullException(nameof(_new_data));
                if (!(_new_data is FileInfo))
                    throw new ArgumentException("The new name has to be packed in a FileInfo instance!", nameof(_new_data));

                file_new = new FileInfo(_new_data.FullName);
            }

            // check if the renaming causes a change in the structure
            DirectoryInfo parent_old = this.File.Directory;
            DirectoryInfo parent_new = file_new.Directory;

            if (string.Equals(parent_old.FullName, parent_new.FullName, StringComparison.InvariantCultureIgnoreCase))
            {
                // actually rename the file
                System.IO.File.Move(this.CurrentFullPath, file_new.FullName);

                // no change in structure
                this.SetFullPath(file_new.FullName, true, true);
                this.Name = (this.File == null) ? FileSystemNavigation.ExtractNameFromPath(file_new.FullName) : this.File.Name;
            }
            else
            {
                // 1. check the admissibility of the location change
                var parent_old_check = this.CheckLocation(parent_old, true);
                var parent_new_check = this.CheckLocation(parent_new, false);

                if (!parent_new_check.is_working_dir && !parent_new_check.is_subdir_of_working_dir)
                    throw new ArgumentException("Contained resources may not be moved outside the working directory", nameof(_new_data));
                if (!parent_old_check.location_parent_match)
                    throw new Exception("Inconsistency with the resource parent. This should not happen!");

                // 2.  before making structural changes...
                // 2a. remove from the old parent
                if (parent_old_check.is_working_dir)
                    this.manager.RemoveAsTopLevelResource(this);
                else
                    (this.Parent as ResourceDirectoryEntry).Children.Remove(this);

                // 2b. actually rename the file
                System.IO.File.Move(this.CurrentFullPath, file_new.FullName);

                // 2c. change the entry itself
                this.SetFullPath(file_new.FullName);
                this.Name = (this.File == null) ? FileSystemNavigation.ExtractNameFromPath(file_new.FullName) : this.File.Name;

                // 2d. add to the new parent
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
            if (this.File == null)
                throw new Exception("The resource corresponds to no valid file! It cannot be renamed.");

            // construct the new full path
            DirectoryInfo parent = this.File.Directory;
            string path = parent.FullName + Path.DirectorySeparatorChar + _new_name;
            FileInfo new_file = new FileInfo(path);

            if (!string.Equals(new_file.Directory.FullName, parent.FullName, StringComparison.InvariantCultureIgnoreCase))
                throw new ArgumentException("Changing the name may not move the file to a new folder");



            var admissible = this.CanChangePath(new_file, nameCollisionFormat);
            this.ChangePath_Internal(new FileInfo(admissible.proposed_name), nameCollisionFormat, false);
        }

        // ---------------------------------- METHODS THAT CAUSE EVENT EMISSION ------------------------------------- //

        /// <inheritdoc/>
        public override void ChangeLocation(DirectoryInfo _new_location, string nameCollisionFormat, bool _check_admissibility)
        {
            string new_path = (_new_location == null) ? Path.Combine(this.manager.WorkingDirectory, this.File.Name) : Path.Combine(_new_location.FullName, this.File.Name);
            if (string.Equals(new_path, this.CurrentFullPath))
                return;

            this.ChangePath(new FileInfo(new_path), nameCollisionFormat, _check_admissibility);
        }

        /// <inheritdoc/>
        public override void ChangePath(FileSystemInfo _new_data, string nameCollisionFormat, bool _check_admissibility)
        {
            FileInfo file_old = new FileInfo(this.CurrentFullPath);
            List<ResourceDirectoryEntry> additional_dir_res = this.ChangePath_Internal(_new_data, nameCollisionFormat, _check_admissibility);
            IEnumerable<DirectoryInfo> additional_dirs = additional_dir_res.Select(x => new DirectoryInfo(x.CurrentFullPath));
            FileInfo file_new = new FileInfo(this.CurrentFullPath);
            //if (this.manager != null)
            //    this.manager.OnResourceManipulated(new ManipulatedResourceEventArgs(file_old, file_new, additional_dirs));
        }

        /// <inheritdoc/>
        public override void ChangeName(string _new_name, string nameCollisionFormat)
        {
            this.ChangeName_Internal(_new_name, nameCollisionFormat);
        }

        #endregion
    }
}
