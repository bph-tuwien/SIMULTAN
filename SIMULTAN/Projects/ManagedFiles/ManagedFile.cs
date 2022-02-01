using System;
using System.IO;

namespace SIMULTAN.Projects.ManagedFiles
{
    /// <summary>
    /// Single file management class. 
    /// </summary>
    public abstract class ManagedFile
    {
        #region PROPERTIES

        /// <summary>
        /// The file as it is in the file system.
        /// </summary>
        public FileInfo File { get; protected set; }

        /// <summary>
        /// Derived: Shows if there have been changes to the data in the data manager since the last save.
        /// </summary>
        public bool IsUpToDate { get; private set; }

        /// <summary>
        /// Saves the index of the resource file corresponding to this managed file, if it exists. Returns -1 if it doesn't.
        /// </summary>
        public int CorrespondingResourceIndex { get; internal set; }

        /// <summary>
        /// The collection which manages this file.
        /// </summary>
        protected ManagedFileCollection owner;

        /// <summary>
        /// The project data on which this file operates
        /// </summary>
        public ExtendedProjectData ProjectData { get; }

        #endregion

        #region EVENTS

        /// <summary>
        /// Handler for the FiletUpToDateChanged event.
        /// </summary>
        /// <param name="sender">the emitting object</param>
        /// <param name="isUpToDate">true = file is uptodate, false = there is an unsaved change</param>
        public delegate void FileUpToDateChangedEventHandler(object sender, bool isUpToDate);
        /// <summary>
        /// Emitted when a chanage in any of the data read from the file occurs after the last save,
        /// or after saving or loading, when there is no change to be saved.
        /// </summary>
        public event FileUpToDateChangedEventHandler FileUpToDateChanged;
        /// <summary>
        /// Emits the FileUpToDateChanged event.
        /// </summary>
        /// <param name="isUpToDate">true = file is uptodate, false = there is an unsaved change</param>
        public void OnFileUpToDateChanged(bool isUpToDate)
        {
            this.IsUpToDate = isUpToDate;
            this.FileUpToDateChanged?.Invoke(this, isUpToDate);
        }

        #endregion


        #region .CTOR

        /// <summary>
        /// Initializes a ManagedFile.
        /// </summary>
        /// <param name="projectData">The project data this file belongs to</param>
        /// <param name="_owner">the managing collection</param>
        /// <param name="_file">the file as it is in the file system</param>
        protected ManagedFile(ExtendedProjectData projectData, ManagedFileCollection _owner, FileInfo _file)
        {
            if (projectData == null)
                throw new ArgumentNullException(nameof(projectData));
            if (_owner == null)
                throw new ArgumentNullException(nameof(owner));
            if (_file == null)
                throw new ArgumentNullException(nameof(_file));

            this.owner = _owner;
            this.ProjectData = projectData;
            this.File = _file;
            this.CorrespondingResourceIndex = -1;
        }


        /// <summary>
        /// Copy .ctor. Initializes a deep copy of the original.
        /// </summary>
        /// <param name="_original">the original managed file</param>
        /// <param name="_new_file_location">the location to wihich the actual file is to be copied</param>
        protected ManagedFile(ManagedFile _original, FileInfo _new_file_location)
        {
            // copy the file to the new location
            System.IO.File.Copy(_original.File.FullName, _new_file_location.FullName, true);
            this.owner = _original.owner;
            this.File = new FileInfo(_new_file_location.FullName);
            this.CorrespondingResourceIndex = _original.CorrespondingResourceIndex;
        }

        #endregion


        /// <summary>
        /// Saves the data residing in the according data manager to the <see cref="File"/>.
        /// </summary>
        public virtual void Save()
        {
            this.OnFileUpToDateChanged(true);
        }
        /// <summary>
        /// Opens the file by means of the data manager.
        /// </summary>
        /// <param name="_clear_before_open">true = empty the data manager before loading new data, false = do not empty the data manager</param>
        public virtual void Open(bool _clear_before_open) { }

        /// <summary>
        /// Checks, if the managed file has a valid path and a data manager.
        /// </summary>
        /// <returns>true, if valid; false, if invalid</returns>
        public virtual bool IsValid()
        {
            bool exists_and_valid = this.File != null && System.IO.File.Exists(this.File.FullName);
            return exists_and_valid;
        }

        /// <summary>
        /// Resets the file and removes all connections to other parts of the application.
        /// The file may not be used afterwards and no further methods may be called.
        /// </summary>
        public virtual void Reset() { }

        /// <summary>
        /// Deletes connections to other data managers.
        /// </summary>
        /// <param name="_resource_id">the id of the file as a resource</param>
        public virtual void OnDeleted(int _resource_id)
        {
            if (_resource_id < 0)
                throw new ArgumentException("The resource index must be non-negative!");
        }
        /// <summary>
        /// Called when the file gets renamed
        /// </summary>
        /// <param name="newFile">The new location of the file</param>
        public virtual void OnRenamed(FileInfo newFile)
        {
            if (newFile != null)
                this.File = newFile;
            else
                this.owner.RemoveFile(this.File);
        }
    }
}
