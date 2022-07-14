using SIMULTAN.Data.Assets.Links;
using SIMULTAN.Data.Users;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.Assets
{
    /// <summary>
    /// Manager for links to files outside a project.
    /// </summary>
    public class MultiLinkManager
    {
        internal static IMachineHashGenerator MachineHashGenerator { get; set; } = new DefaultMachineHashGenerator();

        /// <summary>
        /// Contains all links for a project.
        /// </summary>
        public ObservableCollection<MultiLink> Links { get; private set; }
        private Dictionary<string, MultiLink> local_links;

        /// <summary>
        /// The resource data manager of the component factory.
        /// </summary>
        public AssetManager SecondaryDataManager
        {
            get { return this.secondary_data_manager; }
            set
            {
                if (this.secondary_data_manager != null)
                    this.secondary_data_manager.PathsToResourceFiles.CollectionChanged -= PathsToResourceFiles_CollectionChanged;
                this.secondary_data_manager = value;
                this.secondary_data_manager_paths_backup = (this.secondary_data_manager == null) ? new List<string>() : new List<string>(this.secondary_data_manager.PathsToResourceFiles);
                if (this.secondary_data_manager != null)
                    this.secondary_data_manager.PathsToResourceFiles.CollectionChanged += PathsToResourceFiles_CollectionChanged;
            }
        }
        private AssetManager secondary_data_manager;
        private List<string> secondary_data_manager_paths_backup;

        /// <summary>
        /// The manager of all users that supplies the encryption key for the serialization functionality.
        /// </summary>
        public SimUsersManager UserEncryptionUtiliy { get; set; }

        /// <summary>
        /// Initializes the multilink manager.
        /// </summary>
        public MultiLinkManager()
        {
            this.Links = new ObservableCollection<MultiLink>();
            this.Links.CollectionChanged += Links_CollectionChanged;
            this.local_links = new Dictionary<string, MultiLink>();
        }

        /// <summary>
        /// Removes all MulitLinks from the manager.
        /// </summary>
        public void Clear()
        {
            this.Links.Clear();
            this.local_links.Clear();
        }

        /// <summary>
        /// Forces a synchronization where the links in the asset manager are added to the current content.
        /// </summary>
        public void GetLinksFromAssetManager()
        {
            foreach (string path in this.SecondaryDataManager.PathsToResourceFiles)
                this.PathsToResourceFiles_CollectionChanged(null, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, path));
        }

        #region EVENT HANDLER

        private void Links_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (var item in e.NewItems.OfType<MultiLink>())
                {
                    string local_path = item.GetLink();
                    if (!string.IsNullOrEmpty(local_path))
                        this.local_links.Add(local_path, item as MultiLink);
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (var item in e.OldItems.OfType<MultiLink>())
                {
                    string local_path = (item as MultiLink).GetLink();
                    if (!string.IsNullOrEmpty(local_path))
                        this.local_links.Remove(local_path);
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                this.local_links.Clear();
            }
            else
            {
                throw new NotSupportedException("Operation not supported");
            }
        }

        private void PathsToResourceFiles_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            object old_item = (e.OldItems == null) ? null : e.OldItems[0];
            object new_item = (e.NewItems == null) ? null : e.NewItems[0];
            if (e.Action == NotifyCollectionChangedAction.Add && new_item is string)
            {
                foreach (var item in e.NewItems)
                {
                    // try to add to the multi-link
                    string new_path = item.ToString();
                    if (!this.local_links.ContainsKey(new_path))
                    {
                        // check if a link has the path under a different key
                        MultiLink equialent = this.Links.FirstOrDefault(x => x.HasPath(new_path));
                        if (equialent != null)
                            equialent.AddRepresentation(new_path);
                        else
                            this.Links.Add(new MultiLink(new_path));
                    }
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove && old_item is string)
            {
                foreach (var item in e.OldItems)
                {
                    // try to delete from the multi-link
                    string old_path = item.ToString();
                    if (this.local_links.ContainsKey(old_path))
                    {
                        this.local_links[old_path].RemoveRepresentation(old_path);
                        if (this.local_links[old_path].IsEmpty)
                            this.Links.Remove(this.local_links[old_path]);
                    }
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Replace && new_item is string && old_item is string)
            {
                foreach (var item in e.OldItems)
                {
                    // try to delete from the multi-link
                    string old_path = item.ToString();
                    if (this.local_links.ContainsKey(old_path))
                    {
                        this.local_links[old_path].RemoveRepresentation(old_path);
                        if (this.local_links[old_path].IsEmpty)
                            this.Links.Remove(this.local_links[old_path]);
                    }
                }
                foreach (var item in e.NewItems)
                {
                    // try to add to the multi-link
                    string new_path = item.ToString();
                    if (!this.local_links.ContainsKey(new_path))
                    {
                        // check if a link has the path under a different key
                        MultiLink equialent = this.Links.FirstOrDefault(x => x.HasPath(new_path));
                        if (equialent != null)
                            equialent.AddRepresentation(new_path);
                        else
                            this.Links.Add(new MultiLink(new_path));
                    }
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                // remove only the paths currently in the AssetManager, and only if they are actually for the correct hash
                List<string> to_remove = new List<string>();
                foreach (string path in this.secondary_data_manager_paths_backup)
                {
                    if (this.local_links.ContainsKey(path))
                        to_remove.Add(path);
                }
                foreach (string path in to_remove)
                {
                    MultiLink ml = this.local_links[path];
                    if (ml != null)
                    {
                        ml.RemoveRepresentation(path);
                        if (ml.IsEmpty)
                            this.Links.Remove(ml);
                    }
                }
            }
            this.secondary_data_manager_paths_backup = new List<string>(this.secondary_data_manager.PathsToResourceFiles);
        }

        #endregion
    }
}
