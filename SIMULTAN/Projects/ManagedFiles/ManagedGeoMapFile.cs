using SIMULTAN.Data.Assets;
using SIMULTAN.Data.SitePlanner;
using SIMULTAN.Serializer.DXF;
using SIMULTAN.Serializer.Projects;
using System;
using System.Collections.Specialized;
using System.IO;

namespace SIMULTAN.Projects.ManagedFiles
{
    /// <summary>
    /// Single <see cref="GeoMap"/> file management class.
    /// </summary>
    public class ManagedGeoMapFile : ManagedFile
    {
        /// <summary>
        /// Initializes a ManagedGeoMapFile.
        /// </summary>
        /// <param name="projectData">The project's data</param>
        /// <param name="_owner">the managing collection</param>
        /// <param name="_file">the file as it is in the file system</param>
        public ManagedGeoMapFile(ExtendedProjectData projectData, ManagedFileCollection _owner, FileInfo _file)
            : base(projectData, _owner, _file)
        { }

        /// <summary>
        /// Creates a deep copy of the original managed file. Caution: both files reference THE SAME DATA MANAGER!
        /// </summary>
        /// <param name="_original">the original managed file</param>
        /// <param name="_new_file_location">the location to which the actual file is to be copied</param>
        internal ManagedGeoMapFile(ManagedGeoMapFile _original, FileInfo _new_file_location)
            : base(_original, _new_file_location)
        {
            this.CorrespondingResourceIndex = _original.CorrespondingResourceIndex;
        }

        /// <inheritdoc />
        public override void Save()
        {
            ProjectIO.SaveGeoMapFile(this.File, ProjectData.SitePlannerCommunicator.Manager);
            this.OnFileUpToDateChanged(true);
        }

        /// <inheritdoc />
        public override void Open(bool _clear_before_open)
        {
            var resource = ProjectData.AssetManager.GetResource(this.File);
            if (resource != null)
            {
                ProjectIO.OpenGeoMapFile(this.File, resource, ProjectData.SitePlannerCommunicator.Manager, ProjectData.AssetManager);
            }
            else
            {
                //Register to asset manager in order to find resource when it gets added 
                ((INotifyCollectionChanged)ProjectData.AssetManager.Resources).CollectionChanged += assetManager_CollectionChanged_OnAddingThisFile;
                ProjectData.AssetManager.ChildResourceCollectionChanged += assetManager_CollectionChanged_OnAddingThisFile;
                ProjectData.AssetManager.UpToDate += assetManager_UpToDate_OnAddingThisFile;
            }
        }

        #region OPENING LOGIC

        private void ExecuteOpen(ResourceFileEntry resourceFile)
        {
            // open
            ProjectIO.OpenGeoMapFile(this.File, resourceFile, ProjectData.SitePlannerCommunicator.Manager, ProjectData.AssetManager);
            // reset
            ((INotifyCollectionChanged)ProjectData.AssetManager.Resources).CollectionChanged -= assetManager_CollectionChanged_OnAddingThisFile;
            ProjectData.AssetManager.ChildResourceCollectionChanged -= assetManager_CollectionChanged_OnAddingThisFile;
            ProjectData.AssetManager.UpToDate -= assetManager_UpToDate_OnAddingThisFile;
        }


        private void assetManager_CollectionChanged_OnAddingThisFile(object sender, NotifyCollectionChangedEventArgs args)
        {
            var resource = ProjectData.AssetManager.GetResource(this.File);
            if (resource != null)
                this.ExecuteOpen(resource);
        }

        private void assetManager_UpToDate_OnAddingThisFile(object sender)
        {
            var resource = ProjectData.AssetManager.GetResource(this.File);
            if (resource != null)
                this.ExecuteOpen(resource);
        }

        #endregion

        /// <inheritdoc />
        public override bool IsValid()
        {
            bool exists_and_valid = this.File != null && System.IO.File.Exists(this.File.FullName);
            bool does_not_exist = this.File == null;
            return exists_and_valid || does_not_exist;
        }
    }
}
