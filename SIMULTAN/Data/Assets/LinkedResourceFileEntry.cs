using SIMULTAN.Data.Users;
using SIMULTAN.Serializer.DXF;
using SIMULTAN.Utils.Files;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.Assets
{
    /// <summary>
    /// Manages the entry for a resource file outside the working directory.
    /// </summary>
    public class LinkedResourceFileEntry : ResourceFileEntry
    {
        /// <summary>
        /// Indicates if the relative path can be resolved in relation to multiple fallback
        /// resource paths (see <see cref="AssetManager.PathsToResourceFiles"/>).
        /// </summary>
        public bool MoreThanOneValidPathDetected { get; private set; }
        /// <inheritdoc/>
        public override bool CanBeRenamed => false;
        /// <inheritdoc/>
        public override bool CanBeMoved => true;

        /// <summary>
        /// Can be called with the absolute path (when first being set by the user) and with the relative
        /// path (during parsing).
        /// </summary>
        /// <param name="_manger">the asset manager that holds this resource</param>
        /// <param name="_user">the user with writing access</param>
        /// <param name="_file_path">the path to the file</param>
        /// <param name="_path_is_absolute">indicates if the path is absolute or relative</param>
        /// <param name="_key">the resource key, determined in advance by the asset manager or read from a file</param>
        internal LinkedResourceFileEntry(AssetManager _manger, SimUserRole _user, string _file_path, bool _path_is_absolute, int _key)
            : base(_manger, _user, _file_path, _path_is_absolute, _key)
        {
            if (_path_is_absolute && System.IO.File.Exists(_file_path))
            {
                this.SetFullPath(_file_path, false);
            }
            else
            {
                this.current_relative_path = _file_path;
                this.MoreThanOneValidPathDetected = !this.SetFullResourcePath(_file_path, false, false);
                if (this.current_full_path == AssetManager.PATH_NOT_FOUND)
                    this.File = null;
                else
                    this.File = new FileInfo(this.current_full_path);
            }
            this.Name = (this.File == null) ? FileSystemNavigation.ExtractNameFromPath(_file_path) : this.File.Name;
        }

        #region METHODS: Path

        internal override void AdaptPathToWorkingDirectory(string _working_directory_full_path_new)
        {
            base.AdaptPathToWorkingDirectory(_working_directory_full_path_new);

            // changes only if the relative path is calculated relative to the working directory
            this.SetFullResourcePath(this.current_relative_path, false, false);
            if (this.current_full_path == AssetManager.PATH_NOT_FOUND)
                this.File = null;
            else
                this.File = new FileInfo(this.current_full_path);

            this.Name = (this.File == null) ? FileSystemNavigation.ExtractNameFromPath(this.current_relative_path) : this.File.Name;
        }

        /// <inheritdoc/>
        public override bool CanReplacePath(string _replacement_path)
        {
            bool replacement_linkable = false;
            if (!System.IO.File.Exists(_replacement_path))
                return false;

            FileInfo fi = new FileInfo(_replacement_path);
            DirectoryInfo di = fi.Directory;
            foreach (string fallback in this.manager.PathsToResourceFiles)
            {
                if (!Directory.Exists(fallback))
                    continue;
                DirectoryInfo diFB = new DirectoryInfo(fallback);
                if (FileSystemNavigation.IsSubdirectoryOf(diFB.FullName, di.FullName) || string.Equals(diFB.FullName, di.FullName, StringComparison.InvariantCultureIgnoreCase))
                {
                    replacement_linkable = true;
                    break;
                }
            }
            return replacement_linkable;
        }

        internal override void ReplacePath(string _new_full_path, bool _notify = false)
        {
            base.ReplacePath(_new_full_path, _notify);
            this.SetFullPath(_new_full_path, _notify);
            this.Name = (this.File == null) ? FileSystemNavigation.ExtractNameFromPath(_new_full_path) : this.File.Name;
        }

        private void SetFullPath(string _file_path, bool _notify)
        {
            if (_notify)
                this.CurrentFullPath = _file_path;
            else
                this.current_full_path = _file_path;

            if (!System.IO.File.Exists(_file_path))
                throw new ArgumentException("The given absolute path is not valid!");

            this.File = new FileInfo(_file_path);

            bool set_rel_path = false;
            DirectoryInfo di = this.File.Directory;
            foreach (string fallback in this.manager.PathsToResourceFiles)
            {
                if (!Directory.Exists(fallback))
                    continue;
                DirectoryInfo diFB = new DirectoryInfo(fallback);
                if (FileSystemNavigation.IsSubdirectoryOf(diFB.FullName, di.FullName) || string.Equals(diFB.FullName, di.FullName, StringComparison.InvariantCultureIgnoreCase))
                {
                    this.SetRelativeResourcePath(this.File.FullName, diFB.FullName, false);
                    set_rel_path = true;
                    break;
                }
            }

            // if all else fails, set the relative path from the current working directory
            if (!set_rel_path)
                this.SetRelativeResourcePath(this.File.FullName, this.manager.WorkingDirectory, false);
        }

        /// <summary>
        /// Called on deletion of the file in the file system to reflect that the resource is missing.
        /// </summary>
        internal void SetRelativePathOnDelete()
        {
            this.OnDeleting();
            this.current_relative_path = this.CurrentFullPath;
            this.CurrentFullPath = AssetManager.PATH_NOT_FOUND;
            this.File = null;
            this.Name = FileSystemNavigation.ExtractNameFromPath(this.current_relative_path);
            this.OnDeleted();
        }

        #endregion

        #region METHODS: serialization

        internal override void ExportTo(StringBuilder _sb, int _key)
        {
            base.ExportTo(_sb, _key);
            _sb.AppendLine(((int)AssetSaveCode.APATH_ISCONTAINED).ToString());
            _sb.AppendLine("0");

            _sb.AppendLine(((int)AssetSaveCode.APATH_FULL_PATH).ToString());
            _sb.AppendLine(this.CurrentFullPath);

            _sb.AppendLine(((int)AssetSaveCode.APATH_REL_PATH).ToString());
            _sb.AppendLine(this.Name);
        }

        internal override void ExportAsObjectTo(StringBuilder _sb)
        {
            base.ExportAsObjectTo(_sb);

            _sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString()); // 0
            _sb.AppendLine(ParamStructTypes.RESOURCE_LINK);                           // RESOURCE_LINKED_FILE

            _sb.AppendLine(((int)ParamStructCommonSaveCode.CLASS_NAME).ToString());
            _sb.AppendLine(this.GetType().ToString());

            // export common part
            base.ExportCommon(_sb, true);

            _sb.AppendLine(((int)ResourceSaveCode.RESOURCE_PROBLEM).ToString());
            string tmp = (this.MoreThanOneValidPathDetected) ? "1" : "0";
            _sb.AppendLine(tmp);

            // NOT A COMPLEX ENTITY - but necessary for correct traversal
            _sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString()); // 0
            _sb.AppendLine(ParamStructTypes.SEQUENCE_END);                            // SEQEND
        }

        #endregion

        #region METHODS: rename

        /// <inheritdoc/>
        public override (bool admissible, string proposed_name) CanChangeLocation(DirectoryInfo _new_location, string nameCollisionFormat)
        {
            return (true, this.CurrentFullPath);
        }

        /// <inheritdoc/>
        public override void ChangeLocation(DirectoryInfo _new_location, string nameCollisionFormat, bool _check_admissibility)
        {
            ResourceDirectoryEntry parent_new = null;
            if (_new_location != null)
            {
                parent_new = this.manager.GetResource(_new_location);
                if (parent_new == null)
                    throw new Exception("The target resource directory does not exist!");
            }

            ResourceDirectoryEntry parent_old = this.Parent as ResourceDirectoryEntry;
            if (parent_old == null)
                this.manager.RemoveAsTopLevelResource(this);
            else
                parent_old.Children.Remove(this);

            if (parent_new == null)
                this.manager.AddAsTopLevelResource(this);
            else
                parent_new.Children.Add(this);
        }

        #endregion
    }
}
