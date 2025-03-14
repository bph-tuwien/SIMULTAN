using SIMULTAN.Exceptions;
using SIMULTAN.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Security;

namespace SIMULTAN.Data.Taxonomy
{
    /// <summary>
    /// Data class for a Taxonomy
    /// </summary>
    [DebuggerDisplay("[Taxonomy] {Key}")]
    public class SimTaxonomy : SimObjectNew<SimTaxonomyCollection>, ISimTaxonomyElement
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
        /// Localization languages supported by the taxonomy.
        /// Changing languages propagates to the taxonomy's and its entries' localizations.
        /// </summary>
        public SimTaxonomyLanguageCollection Languages
        {
            get;
        }

        /// <summary>
        /// Localization of the Taxonomy.
        /// </summary>
        public SimTaxonomyLocalization Localization { get; }

        /// <inheritdoc />
        public SimTaxonomy Taxonomy => this;



        #region .CTOR

        /// <summary>
        /// Creates a new Taxonomy with a default translation
        /// </summary>
        /// <param name="key">The key of the taxonomy</param>
        /// <param name="name">The name of the taxonomy</param>
        /// <param name="description">The description of the taxonomy</param>
        /// <param name="culture">The culture to set the name and description for, uses the invariant culture if null</param>
        public SimTaxonomy(string key, string name, string description, CultureInfo culture = null) : this(name, description, culture)
        {
            Key = key;
        }
        /// <summary>
        /// Creates a new Taxonomy with a default translation
        /// </summary>
        /// <param name="name">The name of the taxonomy</param>
        /// <param name="description">The description of the taxonomy</param>
        /// <param name="culture">The culture to set the name and description for, uses the invariant culture if null</param>
        public SimTaxonomy(string name, string description = "", CultureInfo culture = null) : this()
        {
            culture = culture ?? CultureInfo.InvariantCulture;
            Languages.Add(culture);
            Localization.SetLanguage(new SimTaxonomyLocalizationEntry(culture, name, description));
        }

        /// <summary>
        /// Creates a new Taxonomy
        /// </summary>
        public SimTaxonomy() : base()
        {
            Entries = new SimTaxonomyEntryCollection(this);
            allEntries = new Dictionary<string, SimTaxonomyEntry>();
            Languages = new SimTaxonomyLanguageCollection(this);
            Localization = new SimTaxonomyLocalization(this);
        }

        /// <summary>
        /// Creates a new Taxonomy with given id
        /// </summary>
        /// <param name="id">The id</param>
        public SimTaxonomy(SimId id) : base(id)
        {
            Entries = new SimTaxonomyEntryCollection(this);
            allEntries = new Dictionary<string, SimTaxonomyEntry>();
            Languages = new SimTaxonomyLanguageCollection(this);
            Localization = new SimTaxonomyLocalization(this);
        }
        #endregion

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

        /// <inheritdoc />
        protected override void OnFactoryChanged(SimTaxonomyCollection newFactory, SimTaxonomyCollection oldFactory)
        {
            Entries.NotifyFactoryChanged(newFactory, oldFactory);

            base.OnFactoryChanged(newFactory, oldFactory);
        }

        /// <summary>
        /// Returns true if this taxonomy is identical to another taxonomy.
        /// A taxonomy is identical with another if their keys and localization are identical and
        /// if the whole hierarchy is the same in terms of taxonomy entry keys and localizations.
        /// Used for merging taxonomies.
        /// </summary>
        /// <param name="other">The other taxonomy</param>
        /// <returns>True if the other taxonomy is identical to this one.</returns>
        public bool IsIdentical(SimTaxonomy other)
        {
            if (other == null)
                return false;
            if (other.key != this.key)
                return false;
            if (!other.Localization.IsIdenticalTo(this.Localization))
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
                if (!foundEntry.Localization.IsIdenticalTo(entry.Localization))
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

        /// <inheritdoc />
        public void NotifyLocalizationChanged()
        {
            Factory?.NotifyTaxonomyEntryPropertyChanged(this, nameof(Localization));
        }

        /// <summary>
        /// Merges this taxonomy with another taxonomy. (add or update)
        /// All entries get the localization merged with the other taxonomies entries (update or add).
        /// Hierarchy will be adjusted to that of the other taxonomy. 
        /// Missing entries (that were in original but not in other)
        /// will stay at their original locations in the hierarchy.
        /// If deleteMissing is set to true, all missing entries in the other taxonomy and missing localizations 
        /// will be removed from the original.
        /// </summary>
        /// <param name="other">The other taxonomy to merge into this.</param>
        /// <param name="deleteMissing">If missing entries and localizations should be removed</param>
        /// <exception cref="Exception">If taxonomy was changed while merging and cause and error.</exception>
        public void MergeWith(SimTaxonomy other, bool deleteMissing = false)
        {
            if (Key != other.Key)
                throw new ArgumentException("Merging taxonomies do not have the same key");

            // check if merge was in progress on the outside so we don't accidentally turn off the flag at the end
            bool wasMergeInProgress = false;
            bool wasOtherMergeInProgress = false;
            if (Factory != null)
            {
                wasMergeInProgress = Factory.IsMergeInProgress;
                Factory.IsMergeInProgress = true;
            }
            if (other.Factory != null)
            {
                wasOtherMergeInProgress = other.Factory.IsMergeInProgress;
                other.Factory.IsMergeInProgress = true;
            }

            // Merge languages and localization
            foreach (var loc in other.Localization.Entries.Keys)
            {
                if (!Languages.Contains(loc))
                    Languages.Add(loc);
            }
            if (deleteMissing)
            {
                foreach (var loc in Localization.Entries.Keys.Except(other.Localization.Entries.Keys))
                {
                    Languages.Remove(loc);
                }
            }
            Localization.MergeWith(other.Localization, deleteMissing);

            var updatedKeys = new HashSet<string>();
            var entryStack = new Stack<SimTaxonomyEntry>();
            other.Entries.ForEach(x => entryStack.Push(x));
            // first traversal updates existing entries 
            while (entryStack.Any())
            {
                var entry = entryStack.Pop();
                if (updatedKeys.Contains(entry.Key)) // already handled
                    continue;
                updatedKeys.Add(entry.Key);

                // found existing entry, update and check hierarchy
                if (allEntries.TryGetValue(entry.Key, out var existing))
                {
                    existing.Localization.MergeWith(entry.Localization);
                    // not same hierarchy (both parents are not null or both keys don't match)
                    if (existing.Parent?.Key != entry.Parent?.Key)
                    {
                        // move to taxonomy directly
                        if (entry.Parent == null)
                        {
                            Entries.Add(existing);
                        }
                        // move to other child entry
                        else
                        {
                            if (allEntries.TryGetValue(entry.Parent.Key, out var parent))
                            {
                                parent.Children.Add(existing);
                            }
                            else
                            {
                                // parent must have been migrated already
                                throw new Exception("Taxonomy changed while migrating.");
                            }
                        }
                    }
                }
                // is a new entry
                else
                {
                    // create a copy of the entry instead of reusing it, is simpler here (would also break traversal probably)
                    var newEntry = new SimTaxonomyEntry(entry.Key);
                    newEntry.Localization.MergeWith(entry.Localization);
                    // add to taxonomy
                    if (entry.Parent == null)
                    {
                        Entries.Add(newEntry);
                    }
                    // find and add to parent entry
                    else
                    {
                        if (allEntries.TryGetValue(entry.Parent.Key, out var parent))
                        {
                            parent.Children.Add(newEntry);
                        }
                        else
                        {
                            // parent must have been migrated already
                            throw new Exception("Taxonomy changed while migrating.");
                        }
                    }
                }
                entry.Children.ForEach(x => entryStack.Push(x));
            }

            // delete all missing entries
            if (deleteMissing)
            {
                var missingKeys = allEntries.Keys.Except(other.allEntries.Keys);
                foreach (var missing in missingKeys)
                {
                    // may have been removed by other parent entry
                    if (allEntries.TryGetValue(missing, out var entry))
                    {
                        if (entry.Parent == null)
                            entry.Taxonomy?.Entries.Remove(entry);
                        else
                            entry.Parent.Children.Remove(entry);
                    }
                }
            }

            if (Factory != null && !wasMergeInProgress)
                Factory.IsMergeInProgress = false;
            if (other.Factory != null && !wasOtherMergeInProgress)
                other.Factory.IsMergeInProgress = false;
        }
    }
}
