using SIMULTAN.Serializer.DXF;
using SIMULTAN.Utils.Collections;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.Assets
{
    /// <summary>
    /// Saves information about directories for finding linked resources.
    /// </summary>
    public class MultiLink : IEquatable<MultiLink>
    {
        #region IEquatable<MultiLink>
        /// <inheritdoc/>
        public bool Equals(MultiLink _ml)
        {
            //Check whether the compared objects reference the same data. 
            if (Object.ReferenceEquals(this, _ml)) return true;

            //Check whether the object is null. 
            if (Object.ReferenceEquals(_ml, null)) return false;

            return (this.GetHashCode() == _ml.GetHashCode());
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            //Check whether the compared objects reference the same data. 
            if (Object.ReferenceEquals(this, obj)) return true;

            //Check whether the object is null. 
            if (Object.ReferenceEquals(obj, null)) return false;

            MultiLink ml = obj as MultiLink;
            if (ml == null)
                return false;
            else
                return (this.GetHashCode() == ml.GetHashCode());
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            string text_rep = string.Empty;
            foreach (var rep in this.representations)
            {
                text_rep += rep.Value.ToString() + "_" + rep.Value + "|";
            }
            return text_rep.GetHashCode();
        }

        /// <summary>
        /// The equality operator for MultiLink instances.
        /// </summary>
        /// <param name="_mL1">left operand</param>
        /// <param name="_mL2">right operand</param>
        /// <returns>true, if the to MultiLink instances are equal, false otherwise</returns>
        public static bool operator ==(MultiLink _mL1, MultiLink _mL2)
        {
            if (Object.ReferenceEquals(_mL1, null) || Object.ReferenceEquals(_mL2, null))
                return Object.Equals(_mL1, _mL2);

            return _mL1.Equals(_mL2);
        }

        /// <summary>
        /// The inequality operator for MultiLink instances.
        /// </summary>
        /// <param name="_mL1">left operand</param>
        /// <param name="_mL2">right operand</param>
        /// <returns>true, if the to MultiLink instances are NOT equal, false otherwise</returns>
        public static bool operator !=(MultiLink _mL1, MultiLink _mL2)
        {
            if (Object.ReferenceEquals(_mL1, null) || Object.ReferenceEquals(_mL2, null))
                return !Object.Equals(_mL1, _mL2);

            return !(_mL1.Equals(_mL2));
        }
        #endregion

        /// <summary>
        /// Holds the different paths for the same Link on different machines.
        /// </summary>
        public IReadonlyObservableDictionary<string, string> Representations { get { return representations; } }
        private ObservableDictionary<string, string> representations;

        /// <summary>
        /// Indicates if the instance contains any link representations at all.
        /// </summary>
        public bool IsEmpty { get; private set; }

        /// <summary>
        /// Initializes a LinkPath object and adds the first path entry for the current machine, domain and user.
        /// </summary>
        /// <param name="_full_path">the full path of the link in the local file system</param>
        public MultiLink(string _full_path)
        {
            this.representations = new ObservableDictionary<string, string>();
            this.representations.CollectionChanged += Representations_CollectionChanged;
            AddRepresentation(_full_path);
        }

        /// <summary>
        /// The parsing constructor takes a dictionary of existing representations.
        /// </summary>
        /// <param name="_parsed_representations">the parsed representations</param>
        internal MultiLink(IDictionary<string, string> _parsed_representations)
        {
            this.representations = new ObservableDictionary<string, string>();
            this.representations.CollectionChanged += Representations_CollectionChanged;
            foreach (var entry in _parsed_representations)
            {
                this.representations.Add(entry.Key, entry.Value);
            }
        }

        #region Management
        /// <summary>
        /// Adds a new path entry for the current machine, domain and user. If there already is such an entry, it does nothing.
        /// </summary>
        /// <param name="_full_path">the full path of the link in the local file system</param>
        /// <returns>true if the entry was actually added, false if it already existed</returns>
        public bool AddRepresentation(string _full_path)
        {
            if (string.IsNullOrEmpty(_full_path))
                throw new ArgumentNullException("The new path cannot be Null!", nameof(_full_path));

            var local_hash = MultiLinkManager.MachineHashGenerator.GetMachineHash();
            if (representations.ContainsKey(local_hash))
                return false;

            representations.Add(local_hash, _full_path);
            return true;
        }

        /// <summary>
        /// Removes a path entry from the instance. 
        /// If there are no more representations left, it will be marked as empty (see event handler for representations).
        /// </summary>
        /// <param name="_full_path">the path entry to remove for this environment</param>
        /// <returns>true if the removal was successful</returns>
        public bool RemoveRepresentation(string _full_path)
        {
            if (string.IsNullOrEmpty(_full_path))
                throw new ArgumentNullException("The path to remove cannot be Null!", nameof(_full_path));

            var local_hash = MultiLinkManager.MachineHashGenerator.GetMachineHash();
            return representations.Remove(local_hash);
        }

        /// <summary>
        /// Returns the link for the local machine, if it exists.
        /// </summary>
        /// <returns>the path to the link, or Null if it could not be found</returns>
        public string GetLink()
        {
            // construct the hash for the look-up
            var local_hash = MultiLinkManager.MachineHashGenerator.GetMachineHash();
            if (representations.ContainsKey(local_hash))
                return representations[local_hash];
            else
                return null;
        }

        /// <summary>
        /// Establishes equivalency to the given MultiLink instance. If it shares 
        /// at least one representation's path with this one and there are no path collisions for the same user hash,
        /// these MultiLink instance are equivalent.
        /// </summary>
        /// <param name="_other">the instance to which we want to establish equivalency</param>
        /// <returns>true if equivalent, false otherwise</returns>
        public bool IsEquivalent(MultiLink _other)
        {
            if (_other == null)
                throw new ArgumentNullException(nameof(_other));

            // test for path equality and for collisions
            bool equivalence_found = false;
            bool collision_found = false;
            foreach (var other_entry in _other.Representations)
            {
                foreach (var entry in this.Representations)
                {
                    if (string.Equals(other_entry.Value, entry.Value, StringComparison.InvariantCultureIgnoreCase))
                        equivalence_found = true;
                    else
                        collision_found = (other_entry.Key == entry.Key);

                    if (collision_found)
                        break;
                }
            }

            return (!collision_found && equivalence_found);
        }

        /// <summary>
        /// Checks if at least one representation contains the given path.
        /// </summary>
        /// <param name="_full_path">the absolute path to a folder</param>
        /// <returns>true, if the path was found; false otherwise</returns>
        public bool HasPath(string _full_path)
        {
            foreach (var entry in this.Representations)
            {
                if (string.Equals(_full_path, entry.Value, StringComparison.InvariantCultureIgnoreCase))
                    return true;
            }

            return false;
        }

        #endregion

        #region EVENT HANDLERS

        private void Representations_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (this.representations.Count == 0)
                this.IsEmpty = true;
            else
                this.IsEmpty = false;
        }

        #endregion

    }
}
