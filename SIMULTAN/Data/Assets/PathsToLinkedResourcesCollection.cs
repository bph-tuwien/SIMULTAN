using SIMULTAN.Utils.Collections;
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
    /// Event handler delegate for the AttemptedToAddInadmissible event.
    /// </summary>
    /// <typeparam name="T">the type of the inadmissible item</typeparam>
    /// <param name="sender">the emitting object</param>
    /// <param name="index">the index at which the operation was attempted</param>
    /// <param name="item">the inadmissible item</param>
    public delegate void AttemptedToAddInadmissibleEventHandler<T>(object sender, int index, T item);

    /// <summary>
    /// A collection that accepts only the absolute paths of folders outside the forbidden folder. Neither forlder
    /// paths must exist in the local file system.
    /// </summary>
    public class PathsToLinkedResourcesCollection : ElectivelyObservableCollection<string>
    {
        /// <summary>
        /// All paths in the collection should lie outside this folder. This is its absolute path. The resetting
        /// of this property results in the removal of all inadmissible paths!
        /// </summary>
        public string ForbiddenFolder
        {
            get { return this.forbidden_folder; }
            set
            {
                this.forbidden_folder = value;
                this.RemoveInadmissible();
            }
        }
        private string forbidden_folder;

        /// <summary>
        /// If set to false, the class throws an exception on an attempt to add or set an inadmissible path.
        /// Otherwise nothing happens.
        /// </summary>
        public bool OnInadmissibleElementDoNothing { get; }
        /// <summary>
        /// The handler for the AttemptedToAddInadmissible event.
        /// </summary>
        public event AttemptedToAddInadmissibleEventHandler<string> AttemptedToAddInadmissible;

        #region .CTOR

        /// <summary>
        /// Initializes a new instance of PathsToLinkedResourcesCollection that is empty and has default initial capacity.
        /// </summary>
        /// <param name="forbiddenFolder">all paths in the collection should lie outside this folder</param>
        /// <param name="onInadmissibleElementDoNothing">false = the class throws an exception on an attempt to add or set an inadmissible path, true = do nothing</param>
        public PathsToLinkedResourcesCollection(string forbiddenFolder, bool onInadmissibleElementDoNothing)
            : base()
        {
            this.ForbiddenFolder = forbiddenFolder ?? throw new ArgumentNullException(nameof(forbiddenFolder));
            this.OnInadmissibleElementDoNothing = onInadmissibleElementDoNothing;
        }

        /// <summary>
        /// Initializes a new instance of the PathsToLinkedResourcesCollection class 
        /// that contains elements copied from the given list.
        /// </summary>
        /// <param name="list">the list whose elements are copied to the collection</param>
        /// <param name="forbiddenFolder">all paths in the collection should lie outside this folder</param>
        /// <param name="onInadmissibleElementDoNothing">false = the class throws an exception on an attempt to add or set an inadmissible path, true = do nothing</param>
        /// <exception cref="ArgumentNullException">list is a null reference</exception>
        protected PathsToLinkedResourcesCollection(List<string> list, string forbiddenFolder, bool onInadmissibleElementDoNothing)
            : base(list)
        {
            this.ForbiddenFolder = forbiddenFolder ?? throw new ArgumentNullException(nameof(forbiddenFolder));
            this.OnInadmissibleElementDoNothing = onInadmissibleElementDoNothing;
        }

        /// <summary>
        /// Creates a new instance of the PathsToLinkedResourcesCollection class 
        /// if the given list contains absolute paths lying outside the given forbidden folder.
        /// </summary>
        /// <param name="list">the list whose elements are copied to the collection</param>
        /// <param name="forbiddenFolder">all paths in the collection should lie outside this folder</param>
        /// <param name="onInadmissibleElementDoNothing">false = the class throws an exception on an attempt to add or set an inadmissible path, true = do nothing</param>
        /// <returns>the created instance</returns>
        /// <exception cref="ArgumentException">list contains inadmissible paths</exception>
        public static PathsToLinkedResourcesCollection CreateInstance(List<string> list, string forbiddenFolder, bool onInadmissibleElementDoNothing)
        {
            if (!onInadmissibleElementDoNothing)
            {
                if (list != null && !EntriesAdmissibleForCollection(list, forbiddenFolder))
                    throw new ArgumentException("The list contains paths inside the forbidden folder!");
            }

            return new PathsToLinkedResourcesCollection(list, forbiddenFolder, onInadmissibleElementDoNothing);
        }

        /// <summary>
        /// Creates a new instance of the PathsToLinkedResourcesCollection class 
        /// if the given list contains absolute paths lying outside the given forbidden folder. The instance throws no exceptions on 
        /// adding or setting an inadmissible path.
        /// </summary>
        /// <param name="list">the list whose elements are copied to the collection</param>
        /// <param name="forbiddenFolder">all paths in the collection should lie outside this folder</param>
        /// <param name="instance">the created instance, can be null</param>
        /// <returns>true if the instance was created successfully, false otherwise</returns>
        public static bool TryCreateInstance(List<string> list, string forbiddenFolder, out PathsToLinkedResourcesCollection instance)
        {
            bool success = (list != null && EntriesAdmissibleForCollection(list, forbiddenFolder));
            if (success)
                instance = new PathsToLinkedResourcesCollection(list, forbiddenFolder, true);
            else
                instance = null;
            return success;
        }

        /// <summary>
        /// Initializes a new instance of the PathsToLinkedResourcesCollection class that contains
        /// elements copied from the given collection and has sufficient capactiy.
        /// </summary>
        /// <param name="collection">the collection whose elements are copied to the instance</param>
        /// <param name="forbiddenFolder">all paths in the collection should lie outside this folder</param>
        /// <param name="onInadmissibleElementDoNothing">false = the class throws an exception on an attempt to add or set an inadmissible path, true = do nothing</param>
        /// <exception cref="ArgumentNullException">collection is a null reference</exception>
        protected PathsToLinkedResourcesCollection(IEnumerable<string> collection, string forbiddenFolder, bool onInadmissibleElementDoNothing)
            : base(collection)
        {
            this.ForbiddenFolder = forbiddenFolder ?? throw new ArgumentNullException(nameof(forbiddenFolder));
            this.OnInadmissibleElementDoNothing = onInadmissibleElementDoNothing;
        }

        /// <summary>
        /// Creates a new instance of the PathsToLinkedResourcesCollection class 
        /// if the given collection contains absolute paths lying outside the given forbidden folder.
        /// </summary>
        /// <param name="collection">the collection whose elements are copied to the instance</param>
        /// <param name="forbiddenFolder">all paths in the collection should lie outside this folder</param>
        /// <param name="onInadmissibleElementDoNothing">false = the class throws an exception on an attempt to add or set an inadmissible path, true = do nothing</param>
        /// <returns>the created instance</returns>
        /// <exception cref="ArgumentException">collection contains inadmissible paths</exception>
        public static PathsToLinkedResourcesCollection CreateInstance(IEnumerable<string> collection, string forbiddenFolder, bool onInadmissibleElementDoNothing)
        {
            if (!onInadmissibleElementDoNothing)
            {
                if (collection != null && !EntriesAdmissibleForCollection(collection, forbiddenFolder))
                    throw new ArgumentException("The collection contains elements of inadmissible type!");
            }

            return new PathsToLinkedResourcesCollection(collection, forbiddenFolder, onInadmissibleElementDoNothing);
        }

        /// <summary>
        /// Creates a new instance of the PathsToLinkedResourcesCollection class 
        /// if the given list contains absolute paths lying outside the given forbidden folder. The instance throws no exceptions on 
        /// adding or setting an inadmissible path.
        /// </summary>
        /// <param name="collection">the collection whose elements are copied to the collection</param>
        /// <param name="forbiddenFolder">all paths in the collection should lie outside this folder</param>
        /// <param name="instance">the created instance, can be null</param>
        /// <returns>true if the instance was created successfully, false otherwise</returns>
        public static bool TryCreateInstance(IEnumerable<string> collection, string forbiddenFolder, out PathsToLinkedResourcesCollection instance)
        {
            bool success = (collection != null && EntriesAdmissibleForCollection(collection, forbiddenFolder));
            if (success)
                instance = new PathsToLinkedResourcesCollection(collection, forbiddenFolder, true);
            else
                instance = null;
            return success;
        }

        #endregion

        #region Protected Methods

        /// <inheritdoc/>
        protected override void InsertItem(int index, string item)
        {
            bool admissible = IsAdmissible(item, this.ForbiddenFolder);
            if (!admissible)
                this.AttemptedToAddInadmissible?.Invoke(this, index, item);

            if (!this.OnInadmissibleElementDoNothing && !admissible)
                throw new ArgumentException("Item inadmissible for the existing forbidden folder!", nameof(item));
            if (admissible)
                base.InsertItem(index, item);
        }

        /// <inheritdoc/>
        protected override void SetItem(int index, string item)
        {
            bool admissible = IsAdmissible(item, this.ForbiddenFolder);
            if (!admissible)
                this.AttemptedToAddInadmissible?.Invoke(this, index, item);

            if (!this.OnInadmissibleElementDoNothing && !admissible)
                throw new ArgumentException("Item inadmissible for the existing forbidden folder!", nameof(item));
            if (admissible)
                base.SetItem(index, item);
        }

        #endregion

        #region UTILS

        private static bool IsAdmissible(string s, string forbiddenFolder)
        {
            bool equal = string.Equals(s, forbiddenFolder, StringComparison.InvariantCultureIgnoreCase);
            bool contained = FileSystemNavigation.IsSubdirectoryOf(forbiddenFolder, s, false);
            return !equal && !contained;
        }

        private static bool EntriesAdmissibleForCollection(List<string> list, string forbiddenFolder)
        {
            bool admissible = true;
            foreach (var entry in list)
            {
                admissible = IsAdmissible(entry, forbiddenFolder);
            }
            return admissible;
        }

        private static bool EntriesAdmissibleForCollection(IEnumerable<string> collection, string forbiddenFolder)
        {
            bool admissible = true;
            if (collection != null)
            {
                using (IEnumerator<string> enumerator = collection.GetEnumerator())
                {
                    while (enumerator.MoveNext())
                    {
                        admissible = IsAdmissible(enumerator.Current, forbiddenFolder);
                        if (!admissible)
                            return false;
                    }
                }
            }
            return admissible;
        }

        private void RemoveInadmissible()
        {
            List<string> to_remove = new List<string>();
            foreach (string s in this)
            {
                if (!IsAdmissible(s, this.ForbiddenFolder))
                    to_remove.Add(s);
            }
            foreach (string s in to_remove)
            {
                this.Remove(s);
            }
        }
        #endregion
    }
}
