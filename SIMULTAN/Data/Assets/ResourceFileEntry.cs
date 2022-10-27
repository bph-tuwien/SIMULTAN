using SIMULTAN.Data.Users;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.Assets
{
    /// <summary>
    /// Manages the entry for a resource file - linked or contained.
    /// </summary>
    public abstract class ResourceFileEntry : ResourceEntry
    {
        #region PROPERTIES

        /// <summary>
        /// The field corresponding to property FileName.
        /// </summary>
        protected string file_name_internal;
        /// <summary>
        /// The file corresponding to property FileName.
        /// </summary>
        protected FileInfo File
        {
            get { return this.file_internal; }
            set
            {
                this.file_internal = value;
                this.NotifyPropertyChanged(nameof(Exists));
            }
        }
        private FileInfo file_internal;

        ///<inheritdoc/>
        public override string Name
        {
            get { return this.file_name_internal; }
            protected set
            {
                this.file_name_internal = value;
                this.NotifyPropertyChanged(nameof(Name));
                this.NotifyPropertyChanged(nameof(NameWithoutExtension));
            }
        }

        /// <summary>
        /// Returns the name of this file without extension 
        /// </summary>
        [Obsolete("Better calculate it in place from the Name")]
        public string NameWithoutExtension
        {
            get
            {
                var lastDot = Name.LastIndexOf('.');
                if (lastDot == -1) return Name;
                else return Name.Substring(0, lastDot);
            }
        }

        ///<inheritdoc/>
		public override bool Exists
        {
            get
            {
                if (this.File == null)
                    return false;
                return this.File.Exists;
            }
        }

        /// <summary>
        /// The extension of the resource file.
        /// </summary>
		[Obsolete("Better calculate it in place from the Name")]
        public string Extension
        {
            get
            {
                if (this.File == null)
                    return null;
                return this.File.Extension;
            }
        }

        #endregion

        internal ResourceFileEntry(AssetManager _manger, SimUserRole _user, string _file_path, bool _path_is_absolute, int _key)
            : base(_manger, _user, _key)
        { }
    }
}
