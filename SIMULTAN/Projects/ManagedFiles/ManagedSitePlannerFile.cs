using SIMULTAN.Data.Assets;
using SIMULTAN.Data.SitePlanner;
using SIMULTAN.Serializer.Projects;
using System.Collections.Specialized;
using System.IO;

namespace SIMULTAN.Projects.ManagedFiles
{
    /// <summary>
    /// Single <see cref="SitePlannerProject"/> file management class.
    /// </summary>
    public class ManagedSitePlannerFile : ManagedFile
    {
        /// <summary>
        /// Initializes a ManagedSitePlannerFile.
        /// </summary>
        /// <param name="projectData">The project's data</param>
        /// <param name="_owner">the managing collection</param>
        /// <param name="_file">the file as it is in the file system</param>
        public ManagedSitePlannerFile(ExtendedProjectData projectData, ManagedFileCollection _owner, FileInfo _file)
            : base(projectData, _owner, _file)
        { }

        /// <summary>
        /// Creates a deep copy of the original managed file. Caution: both files reference THE SAME DATA MANAGER!
        /// </summary>
        /// <param name="_original">the original managed file</param>
        /// <param name="_new_file_location">the location to which the actual file is to be copied</param>
        internal ManagedSitePlannerFile(ManagedSitePlannerFile _original, FileInfo _new_file_location)
            : base(_original, _new_file_location)
        {
            this.CorrespondingResourceIndex = _original.CorrespondingResourceIndex;
        }

        /// <inheritdoc />
        public override void Save()
        {
            ProjectIO.SaveSitePlannerFile(this.File, ProjectData.SitePlannerManager, ProjectData);
            this.OnFileUpToDateChanged(true);
        }

        /// <inheritdoc />
        public override void Open(bool _clear_before_open)
        {
            var resource = ProjectData.AssetManager.GetResource(this.File);
            if (resource != null)
                ProjectIO.OpenSitePlannerFile(resource, ProjectData);
            else
            {
                ((INotifyCollectionChanged)ProjectData.AssetManager.Resources).CollectionChanged += assetManager_CollectionChanged_OnAddingThisFile;
                ProjectData.AssetManager.ChildResourceCollectionChanged += assetManager_CollectionChanged_OnAddingThisFile;
                ProjectData.AssetManager.UpToDate += assetManager_UpToDate_OnAddingThisFile;
            }
        }




        #region OPENING LOGIC (have to wait for an event)

        private void ExecuteOpen(ResourceFileEntry fileResource)
        {
            // open
            ProjectIO.OpenSitePlannerFile(fileResource, ProjectData);
            // reset
            ProjectData.AssetManager.UpToDate -= assetManager_UpToDate_OnAddingThisFile;
            ProjectData.AssetManager.ChildResourceCollectionChanged -= assetManager_CollectionChanged_OnAddingThisFile;
            ((INotifyCollectionChanged)ProjectData.AssetManager.Resources).CollectionChanged -= assetManager_CollectionChanged_OnAddingThisFile;
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
