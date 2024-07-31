using SIMULTAN.Data;
using SIMULTAN.Data.Assets;
using SIMULTAN.Utils;
using SIMULTAN.Utils.Files;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace SIMULTAN.Projects
{
    /// <summary>
    /// Holds all data managers - for components and networks, for excel mapping rules, for value fields etc.
    /// </summary>
    public class ExtendedProjectData : ProjectData
    {
        #region PROPERTIES: Owning project

        /// <summary>
        /// The project owning all managers. It can be set only once.
        /// </summary>
        public HierarchicalProject Project
        {
            get { return this.Owner as HierarchicalProject; }
            internal set
            {
                if (this.Owner is HierarchicalProject project)
                {
                    //Remove from old project
                    project.AllProjectDataManagers = null;
                }

                this.Owner = value;
            }
        }

        #endregion



        /// <summary>
        /// All watchers of the fallback paths of the asset managers (paths used by linked resources).
        /// </summary>
        private Dictionary<FileSystemWatcher, List<string>> assetManagerFallbackWatchers = new Dictionary<FileSystemWatcher, List<string>>();
        /// <summary>
        /// All watchers of lost linked files.
        /// </summary>
        private Dictionary<LinkedResourceFileEntry, FileSystemWatcher> assetManagerMissingLinkWatchers = new Dictionary<LinkedResourceFileEntry, FileSystemWatcher>();


        #region .CTOR

        /// <summary>
        /// Initializes all data managers and attaches their respective event handlers.
        /// </summary>
        /// <param name="synchronizationContext">Synchronization context used to run events on the main thread for thread safety.</param>
        /// <param name="dispatcherTimer">Dispatcher timer factory used for the OffsetSurfaceGenerator.</param>
        public ExtendedProjectData(ISynchronizeInvoke synchronizationContext, IDispatcherTimerFactory dispatcherTimer) 
            : base(synchronizationContext, dispatcherTimer)
        {
            this.AssetManager.PathsToResourceFiles.CollectionChanged += AssetManager_PathsToResourceFiles_CollectionChanged;
            this.AssetManager.UpToDate += AssetManager_UpToDate;
        }

        /// <summary>
        /// Initializes all data managers and attaches their respective event handlers.
        /// </summary>
        public ExtendedProjectData() : this(new UnsyncedSynchronizationContext(), new SystemTimerFactory()) { }

        #endregion


        #region METHODS

        /// <summary>
        /// Clears the content and resets the state of all data managers.
        /// </summary>
        public void Reset()
        {
            base.Clear();

            this.ReleaseAssetManagerWatchers();
            this.Owner = null;
        }

        #endregion

        #region EVENT HANDLERS: resources general

        private void AssetManager_PathsToResourceFiles_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (var item in e.NewItems)
                    {
                        if (item is string)
                            this.AddWatcherForAssetManagerPath(item.ToString());
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    foreach (var item in e.OldItems)
                    {
                        if (item is string)
                            this.RemoveWatcherForAssetManagerPath(item.ToString());
                    }
                    break;
                case NotifyCollectionChangedAction.Replace:
                    foreach (var item in e.OldItems)
                    {
                        if (item is string)
                            this.RemoveWatcherForAssetManagerPath(item.ToString());
                    }
                    foreach (var item in e.NewItems)
                    {
                        if (item is string)
                            this.AddWatcherForAssetManagerPath(item.ToString());
                    }
                    break;
                case NotifyCollectionChangedAction.Reset:
                    this.ReleaseAssetManagerWatchers();
                    break;
            }
        }

        private void AssetManager_UpToDate(object sender)
        {
            var missing_links = this.AssetManager.MissingLinkedResources.Where(x => x.CurrentRelativePath != AssetManager.PATH_NOT_FOUND &&
                                                                                    !string.IsNullOrEmpty(x.CurrentRelativePath) &&
                                                                                    new FileInfo(x.CurrentRelativePath).Directory.Exists);
            // remove redundant watchers
            var redundant = this.assetManagerMissingLinkWatchers.Where(x => !missing_links.Contains(x.Key)).Select(x => x.Key).ToList();
            foreach (var r in redundant)
            {
                RemoveWatcherForMissingLink(r);
            }
            // add missing watchers
            foreach (LinkedResourceFileEntry ml in missing_links)
            {
                this.AddWatcherForMissingLink(ml);
            }
        }

        #endregion

        #region FILE WATCHERS: Search Path

        private void AddWatcherForAssetManagerPath(string _path)
        {
            //Debug.WriteLine("AddWatcherForAssetManagerPath called by \"{0}\"", _path);
            DirectoryInfo dir = new DirectoryInfo(_path);
            if (!dir.Exists) return;

            var duplicate = this.assetManagerFallbackWatchers.FirstOrDefault(x => string.Equals(x.Key.Path, dir.Parent.FullName, StringComparison.InvariantCultureIgnoreCase));
            if (duplicate.Key == null)
            {
                var watcher = new FileSystemWatcher()
                {
                    SynchronizingObject = SynchronizationContext
                };

                //Debug.WriteLine("ADDING watcher for path \"{0}\"", dir.Parent.FullName);
                watcher.Path = dir.Parent.FullName;
                watcher.Renamed += Watcher_Renamed;
                watcher.Deleted += Watcher_Deleted;
                watcher.EnableRaisingEvents = true;
                //watcher.IncludeSubdirectories = true;
                //Debug.WriteLine("watcher [{0}] ASSIGNED to path \"{1}\"", watcher.Path, _path);
                this.assetManagerFallbackWatchers.Add(watcher, new List<string> { _path });
            }
            else
            {
                //Debug.WriteLine("watcher [{0}] ASSIGNED to path \"{1}\"", duplicate.Key.Path, _path);
                this.assetManagerFallbackWatchers[duplicate.Key].Add(_path);
            }
        }

        private void RemoveWatcherForAssetManagerPath(string _path)
        {
            // should be able to remove entries that do not exist any more
            DirectoryInfo dir = new DirectoryInfo(_path);

            var found = this.assetManagerFallbackWatchers.FirstOrDefault(x => string.Equals(x.Key.Path, dir.Parent.FullName, StringComparison.InvariantCultureIgnoreCase));
            if (found.Key != null)
            {
                found.Value.Remove(_path);
                if (found.Value.Count == 0)
                {
                    found.Key.Renamed -= Watcher_Renamed;
                    found.Key.Deleted -= Watcher_Deleted;
                    found.Key.EnableRaisingEvents = false;
                    this.assetManagerFallbackWatchers.Remove(found.Key);
                }
            }
        }

        private void ReleaseAssetManagerWatchers()
        {
            foreach (var watcher in this.assetManagerFallbackWatchers)
            {
                watcher.Key.Renamed -= Watcher_Renamed;
                watcher.Key.Deleted -= Watcher_Deleted;
                watcher.Key.EnableRaisingEvents = false;
            }
            this.assetManagerFallbackWatchers.Clear();
        }

        private void Watcher_Renamed(object sender, FileSystemEventArgs e)
        {
            RenamedEventArgs rename = e as RenamedEventArgs;
            if (rename == null)
                return;
            FileSystemWatcher watcher = sender as FileSystemWatcher;
            if (watcher == null)
                return;

            // find the correct entry in the lookup
            var found = this.assetManagerFallbackWatchers.FirstOrDefault(x => string.Equals(x.Key.Path, watcher.Path, StringComparison.InvariantCultureIgnoreCase));
            if (found.Key == null)
                return;

            if (found.Value.Contains(rename.OldFullPath) || found.Value.Any(x => FileSystemNavigation.IsContainedIn(x, rename.OldFullPath, false)))
            {
                Debug.WriteLine("- - - Watcher [{0}]: \"{1}\" was RENAMED into \"{2}\".", watcher.Path, rename.OldFullPath, e.FullPath);
                // communicate to the asset manager
                int index = this.AssetManager.PathsToResourceFiles.IndexOf(rename.OldFullPath);
                this.AssetManager.PathsToResourceFiles[index] = e.FullPath;
            }


        }

        private void Watcher_Deleted(object sender, FileSystemEventArgs e)
        {
            FileSystemWatcher watcher = sender as FileSystemWatcher;
            if (watcher == null)
                return;

            // find the correct entry in the lookup
            var found = this.assetManagerFallbackWatchers.FirstOrDefault(x => string.Equals(x.Key.Path, watcher.Path, StringComparison.InvariantCultureIgnoreCase));
            if (found.Key == null)
                return;

            if (found.Value.Contains(e.FullPath) || found.Value.Any(x => FileSystemNavigation.IsContainedIn(x, e.FullPath, false)))
            {
                Debug.WriteLine("- - - Watcher [{0}]: \"{1}\" was DELETED.", watcher.Path, e.FullPath);
                this.AssetManager.PathsToResourceFiles.Remove(e.FullPath);
            }

        }

        #endregion

        #region FILE WATCHERS: missing links

        private void AddWatcherForMissingLink(LinkedResourceFileEntry _missing)
        {
            if (!this.assetManagerMissingLinkWatchers.ContainsKey(_missing))
            {
                var watcher = new FileSystemWatcher() { SynchronizingObject = SynchronizationContext };
                FileInfo file = new FileInfo(_missing.CurrentRelativePath);
                watcher.Path = file.DirectoryName;
                watcher.Created += LinkWatcher_Created;
                watcher.Renamed += LinkWatcher_Renamed;
                watcher.EnableRaisingEvents = true;
                this.assetManagerMissingLinkWatchers.Add(_missing, watcher);
            }
        }

        private void RemoveWatcherForMissingLink(LinkedResourceFileEntry _missing)
        {
            var watcher = this.assetManagerMissingLinkWatchers[_missing];
            watcher.Created -= LinkWatcher_Created;
            watcher.Renamed -= LinkWatcher_Renamed;
            watcher.EnableRaisingEvents = false;
            this.assetManagerMissingLinkWatchers.Remove(_missing);
        }

        private void LinkWatcher_Renamed(object sender, RenamedEventArgs e)
        {
            FileSystemWatcher watcher = sender as FileSystemWatcher;
            if (watcher == null)
                return;

            this.TryRelink(e.FullPath, watcher);
        }

        private void LinkWatcher_Created(object sender, FileSystemEventArgs e)
        {
            FileSystemWatcher watcher = sender as FileSystemWatcher;
            if (watcher == null)
                return;

            this.TryRelink(e.FullPath, watcher);
        }

        private void TryRelink(string _full_path, FileSystemWatcher watcher)
        {
            var found = this.assetManagerMissingLinkWatchers.FirstOrDefault(x => x.Value == watcher);
            if (found.Key != null)
            {
                // check if the renaming / creation caused the missing linked resource to recover
                if (string.Equals(_full_path, found.Key.CurrentRelativePath, StringComparison.InvariantCultureIgnoreCase))
                {
                    this.AssetManager.ReLinkLinkedFileEntry(found.Key, new FileInfo(_full_path), true);
                }
            }
        }

        #endregion
    }
}
