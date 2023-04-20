using SIMULTAN.Exceptions;
using SIMULTAN.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.Taxonomy
{
    /// <summary>
    /// Data class for a Taxonomy
    /// </summary>
    [DebuggerDisplay("[Taxonomy] {Name}, {Key}")]
    public class SimTaxonomy : SimNamedObject<SimTaxonomyCollection>
    {
        private Dictionary<string, SimTaxonomyEntry> allEntries;

        /// <summary>
        /// Key of the taxonomy.
        /// Does not need to be unique.
        /// Should be used to lookup the taxonomy independent of possible name localizations.
        /// SimId is only used internally in the project to restore references.
        /// </summary>
        public string Key
        {
            get => key;
            set
            {
                if (key != value)
                {
                    NotifyWriteAccess();
                    key = value;
                    NotifyPropertyChanged(nameof(Key));
                }
            }
        }
        private string key;

        /// <summary>
        /// The <see cref="SimTaxonomyEntry"/> entries
        /// </summary>
        public SimTaxonomyEntryCollection Entries { get; }

        /// <summary>
        /// If the taxonomy is read only
        /// </summary>
        public bool IsReadonly
        {
            get => isReadonly;
            set
            {
                if (isReadonly != value)
                {
                    isReadonly = value;
                    NotifyPropertyChanged(nameof(IsReadonly));
                }
            }
        }
        private bool isReadonly = false;

        /// <summary>
        /// If the taxonomy can be deleted
        /// </summary>
        public bool IsDeletable
        {
            get => isDeletable;
            set
            {
                if (isDeletable != value)
                {
                    isDeletable = value;
                    NotifyPropertyChanged(nameof(isDeletable));
                }
            }
        }
        private bool isDeletable = true;


        /// <summary>
        /// Creates a new Taxonomy
        /// </summary>
        /// <param name="key">The key of the taxonomy</param>
        /// <param name="name">The name of the taxonomy</param>
        /// <param name="description">The description of the taxonomy</param>
        public SimTaxonomy(string key, string name, string description) : this(name, description)
        {
            Key = key;
        }
        /// <summary>
        /// Creates a new Taxonomy
        /// </summary>
        /// <param name="name">The name of the taxonomy</param>
        /// <param name="description">The description of the taxonomy</param>
        public SimTaxonomy(string name, string description) : this(name)
        {
            Description = description;
        }

        /// <summary>
        /// Creates a new Taxonomy
        /// </summary>
        /// <param name="name">The name of the taxonomy</param>
        public SimTaxonomy(string name) : this()
        {
            Name = name;
        }

        /// <summary>
        /// Creates a new Taxonomy
        /// </summary>
        public SimTaxonomy() : base()
        {
            Entries = new SimTaxonomyEntryCollection(this);
            allEntries = new Dictionary<string, SimTaxonomyEntry>();
        }

        /// <summary>
        /// Returns if a key is already in use.
        /// </summary>
        /// <param name="key">the key to check</param>
        /// <returns>true if the key is already used</returns>
        public bool IsKeyInUse(String key)
        {
            return allEntries.ContainsKey(key);
        }

        /// <summary>
        /// Tries to find a <see cref="SimTaxonomyEntry"/> by its key.
        /// </summary>
        /// <param name="key">The key</param>
        /// <returns>The <see cref="SimTaxonomyEntry"/> with the provided key if found, null otherwise</returns>
        public SimTaxonomyEntry GetTaxonomyEntryByKey(String key)
        {
            if (allEntries.TryGetValue(key, out SimTaxonomyEntry entry))
            {
                return entry;
            }
            return null;
        }

        /// <summary>
        /// Registers a Taxonomy entry with its key to the Taxonomy
        /// </summary>
        /// <param name="entry">The entry to register.</param>
        /// <exception cref="ArgumentNullException">If the entry is null</exception>
        /// <exception cref="Exception">If the entry's key is null or empty, or if the key is already in use.</exception>
        internal void RegisterEntry(SimTaxonomyEntry entry)
        {
            if (entry == null)
            {
                throw new ArgumentNullException(nameof(entry));
            }
            if (string.IsNullOrEmpty(entry.Key))
            {
                throw new Exception("Taxonomy entry key cannot be null or empty");
            }

            if (IsKeyInUse(entry.Key))
            {
                throw new Exception("Taxonomy entry key is already in use");
            }
            allEntries.Add(entry.Key, entry);
        }

        /// <summary>
        /// Unregisters an entry from this taxonomy.
        /// Frees up the entry's key for reuse.
        /// </summary>
        /// <param name="entry">The entry to unregister.</param>
        /// <exception cref="ArgumentNullException">If the entry is null.</exception>
        internal void UnregisterEntry(SimTaxonomyEntry entry)
        {
            if (entry == null)
            {
                throw new ArgumentNullException(nameof(entry));
            }
            UnregisterEntry(entry.Key);
        }

        /// <summary>
        /// Unregisters an entry's key from this taxonomy.
        /// Frees up the entry's key for reuse.
        /// </summary>
        /// <param name="key">The entry key to unregister.</param>
        /// <exception cref="ArgumentNullException">If the key is null or empty.</exception>
        internal void UnregisterEntry(String key)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentNullException(nameof(key));
            }
            allEntries.Remove(key);
        }

        /// <summary>
        /// Creates a new Taxonomy with given id
        /// </summary>
        /// <param name="id">The id</param>
        public SimTaxonomy(SimId id) : base(id)
        {
            Entries = new SimTaxonomyEntryCollection(this);
            allEntries = new Dictionary<string, SimTaxonomyEntry>();
        }

        /// <inheritdoc />
        protected override void OnFactoryChanged(SimTaxonomyCollection newFactory, SimTaxonomyCollection oldFactory)
        {
            Entries.NotifyFactoryChanged(newFactory, oldFactory);

            base.OnFactoryChanged(newFactory, oldFactory);
        }

        /// <summary>
        /// Returns true if this taxonomy is identical to another taxonomy.
        /// A taxonomy is identical with another if their keys (or if no keys are available, their names) are identical and
        /// if the whole hierarchy is the same in terms of taxonomy entry keys and names.
        /// </summary>
        /// <param name="other">The other taxonomy</param>
        /// <returns>True if the other taxonomy is identical to this one.</returns>
        public bool IsIdentical(SimTaxonomy other)
        {
            if (other == null)
                return false;
            if (other.key != this.key)
                return false;

            if (allEntries.Count != other.allEntries.Count)
                return false;
            if (IsReadonly != other.IsReadonly)
                return false;
            if (IsDeletable != other.IsDeletable)
                return false;

            // compare all entries to check if hierarchy is identical
            foreach (var entry in other.allEntries.Values)
            {
                var foundEntry = GetTaxonomyEntryByKey(entry.Key);
                if (foundEntry == null)
                    return false;
                if (foundEntry.Name != entry.Name)
                    return false;
                // if one or the others parent is null but not both -> different hierarchy
                if (foundEntry.Parent == null ^ entry.Parent == null)
                    return false;
                // if parent keys are not identical -> different hierarchy (cause keys are unique)
                if (foundEntry.Parent != null && foundEntry.Parent.Key != entry.Parent.Key)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Returns a collection of all the entires in a flat hierarchy.
        /// </summary>
        /// <returns>a collection of all the entires in a flat hierarchy.</returns>
        public IEnumerable<SimTaxonomyEntry> GetAllEntriesFlat()
        {
            return allEntries.Values;
        }

        /// <inheritdoc/>
        protected override void NotifyWriteAccess()
        {
            if (IsReadonly)
                throw new AccessDeniedException("Cannot change read only taxonomy.");

            base.NotifyWriteAccess();
        }

    }
}
