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
    public class InvalidPathException : Exception
    {
        public InvalidPathException() : base("Item may not be located in the current working directory folder!") { }
    }

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

        #region .CTOR

        /// <summary>
        /// Initializes a new instance of PathsToLinkedResourcesCollection that is empty and has default initial capacity.
        /// </summary>
        /// <param name="forbiddenFolder">all paths in the collection should lie outside this folder</param>
        public PathsToLinkedResourcesCollection(string forbiddenFolder)
            : base()
        {
            this.ForbiddenFolder = forbiddenFolder ?? throw new ArgumentNullException(nameof(forbiddenFolder));
        }

        #endregion

        #region Protected Methods

        /// <inheritdoc/>
        protected override void InsertItem(int index, string item)
        {
            bool admissible = IsAdmissible(item, this.ForbiddenFolder);
            if (!admissible)
                throw new InvalidPathException();
            
            base.InsertItem(index, item);
        }

        /// <inheritdoc/>
        protected override void SetItem(int index, string item)
        {
            bool admissible = IsAdmissible(item, this.ForbiddenFolder);

            if (!admissible)
                throw new InvalidPathException();
            
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
