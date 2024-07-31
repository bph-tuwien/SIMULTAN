using SIMULTAN.Projects;
using SIMULTAN.Serializer.DXF;
using SIMULTAN.Serializer.TXDXF;
using SIMULTAN.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.Taxonomy
{
    /// <summary>
    /// The main collection for taxonomies of a project
    /// </summary>
    public class SimTaxonomyCollection : SimManagedCollection<SimTaxonomy>
    {
        /// <summary>
        /// If the project is currently being loaded
        /// </summary>
        public bool IsLoading { get; private set; }

        /// <summary>
        /// If the taxonomy collection is being merged into another one
        /// </summary>
        public bool IsMergeInProgress { get; private set; }
        /// <summary>
        /// Disables certain notifications while a project is closed
        /// </summary>
        public bool IsClosing { get; set; }

        /// <summary>
        /// Creates a new <see cref="SimTaxonomyCollection"/>
        /// </summary>
        /// <param name="owner">The project this collection belongs to</param>
        public SimTaxonomyCollection(ProjectData owner) : base(owner)
        {
        }

        #region Collection overrides

        /// <inheritdoc />
        protected override void InsertItem(int index, SimTaxonomy item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));
            if (item.Factory != null)
                throw new ArgumentException("SimTaxonomyEntry already belongs to a Factory");

            SetValues(item);
            base.InsertItem(index, item);
        }

        /// <inheritdoc />
        protected override void RemoveItem(int index)
        {
            RemoveItem(index, false);
        }

        internal void RemoveItem(int index, bool ignoreIsDeletable)
        {
            var oldItem = this[index];
            if (!ignoreIsDeletable && !oldItem.IsDeletable)
                throw new InvalidOperationException(String.Format("Cannot delete taxonomy \"{0}\" because it is not deletable.", oldItem.Localization.Localize().Name));

            UnsetValues(oldItem);
            base.RemoveItem(index);
        }

        /// <inheritdoc />
        protected override void ClearItems()
        {
            ClearAllItems();
        }

        /// <summary>
        /// Clears the collection.
        /// </summary>
        /// <param name="ignoreIsDeletable">If set to true, ignores if it contains taxonomies that are marked as non deletable</param>
        /// <exception cref="InvalidOperationException">If ignoreIsDeletable is set to false and it contains a taxonomy that is non deletable</exception>
        internal void ClearAllItems(bool ignoreIsDeletable = false)
        {
            foreach (var item in this)
            {
                if (!ignoreIsDeletable && !item.IsDeletable)
                    throw new InvalidOperationException("Cannot clear taxonomies as they contain non deletable taxonomies");
                UnsetValues(item);
            }

            base.ClearItems();
        }

        /// <inheritdoc />
        protected override void SetItem(int index, SimTaxonomy item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            var oldItem = this[index];
            UnsetValues(oldItem);

            SetValues(item);
            base.SetItem(index, item);
        }

        #endregion

        private void UnsetValues(SimTaxonomy item)
        {
            if (ProjectData.IdGenerator != null)
                ProjectData.IdGenerator.Remove(item);

            // remove location from SimId but key global/local Ids
            item.Id = new SimId(item.Id.GlobalId, item.Id.LocalId);
            item.Factory = null;
        }
        private void SetValues(SimTaxonomy item)
        {
            if (item.Factory != null && item.Factory != this)
                throw new ArgumentException("Taxonomy already belongs to another Factory");


            if (item.Factory == null)
            {
                if (item.Id == SimId.Empty)
                {
                    item.Id = ProjectData.IdGenerator.NextId(item, CalledFromLocation);
                }
                else
                {
                    if (!IsLoading)
                        throw new NotSupportedException("Existing Ids may only be used during a loading operation");
                    // register the taxonomies
                    item.Id = new SimId(CalledFromLocation, item.Id.LocalId);
                    ProjectData.IdGenerator.Reserve(item, item.Id);
                }

                item.Factory = this;
            }
            else
            {
                if (item.Factory != this)
                {
                    throw new ArgumentException("Taxonomy entries must be part of the same factory as the taxonomy");
                }
            }
        }

        private void ResetIDs(IEnumerable<SimTaxonomy> taxonomies)
        {
            foreach (var tax in taxonomies)
            {
                tax.Id = SimId.Empty;

                ResetIDs(tax.Entries);
            }
        }
        private void ResetIDs(IEnumerable<SimTaxonomyEntry> entries)
        {
            foreach (var entry in entries)
            {
                entry.Id = SimId.Empty;

                ResetIDs(entry.Children);
            }
        }

        /// <summary>
        /// Tries to find a taxonomy entry with the provided key in a taxonomy with the provided key.
        /// If isReadonly is true, only read only taxonomies are considered, otherwise all are included.
        /// </summary>
        /// <param name="taxonomyKey">Key of the taxonomy</param>
        /// <param name="entryKey">Key of the taxonomy entry</param>
        /// <param name="isReadonly">If only read only taxonomies should be considered</param>
        /// <returns>The entry if found by the provided keys, null if not.</returns>
        public SimTaxonomyEntry FindEntry(String taxonomyKey, String entryKey, bool isReadonly = false)
        {
            foreach (var tax in this.Where(x => (isReadonly ? x.IsReadonly : true) && x.Key == taxonomyKey))
            {
                var entry = tax.GetTaxonomyEntryByKey(entryKey);
                if (entry != null)
                    return entry;
            }

            return null;
        }

        /// <summary>
        /// Tries to find a taxonomy either by key or name.
        /// If none is found matching the key, it tries to find one with the same name.
        /// Return null if none is found.
        /// </summary>
        /// <param name="taxonomies">lookup taxonomies</param>
        /// <param name="key">Key to look for</param>
        /// <param name="name">Name to look for</param>
        /// <param name="culture">Culture used to compare names, uses invariant culture if null</param>
        /// <returns>A taxonomy matching either the key or the name. Key takes precedence. Null if not found.</returns>
        public static SimTaxonomy GetTaxonomyByKeyOrName(IEnumerable<SimTaxonomy> taxonomies, String key, String name = null, CultureInfo culture = null)
        {
            culture = culture ?? CultureInfo.InvariantCulture;
            SimTaxonomy tax = null;
            if (!String.IsNullOrEmpty(key))
            {
                tax = taxonomies.FirstOrDefault(x => x.Key == key);
            }

            if (tax == null && !String.IsNullOrEmpty(name))
            {
                tax = taxonomies.FirstOrDefault(x => x.Localization.Localize(culture).Name == name);
            }

            return tax;
        }

        /// <summary>
        /// Tries to find a taxonomy either by key or name.
        /// If none is found matching the key, it tries to find one with the same name.
        /// Return null if none is found.
        /// </summary>
        /// <param name="key">Key to look for</param>
        /// <param name="name">Name to look for</param>
        /// <param name="culture">Culture used to compare names, uses invariant culture if null</param>
        /// <returns>A taxonomy matching either the key or the name. Key takes precedence. Null if not found.</returns>
        public SimTaxonomy GetTaxonomyByKeyOrName(String key, String name = null, CultureInfo culture = null)
        {
            return GetTaxonomyByKeyOrName(this, key, name, culture);
        }

        /// <summary>
        /// Merges this taxonomy collection with another and returns the taxonomies that already existed.
        /// References to taxonomy entries of the existing taxonomies need to be manually reset to the existing ones after import.
        /// </summary>
        /// <param name="defaults">The default taxonomies to merge into this collection.</param>
        /// <returns>If changes had to be made to the existing default taxonomies.</returns>
        /// <exception cref="ArgumentNullException">if an argument is null</exception>
        public bool MergeWithDefaults(SimTaxonomyCollection defaults)
        {
            if (defaults == null)
                throw new ArgumentNullException(nameof(defaults));

            IsMergeInProgress = true;
            defaults.IsMergeInProgress = true;
            StartLoading();
            var existingTaxonomies = new Dictionary<SimTaxonomy, SimTaxonomy>();
            var newTaxonomies = new List<SimTaxonomy>();
            bool hasChanges = false;
            foreach (var tax in defaults)
            {
                var mergeTax = this.FirstOrDefault(x => x.Key == tax.Key && x.IsReadonly);
                if (mergeTax != null)
                {
                    existingTaxonomies.Add(tax, mergeTax);
                }
                else
                {
                    newTaxonomies.Add(tax);
                    hasChanges = true;
                }
                // defaults need to be read only, make sure so
                tax.IsReadonly = true;
            }

            List<(SimTaxonomy value, long id)> oldIds = newTaxonomies.Select(x => (x, x.LocalID)).ToList();
            Dictionary<SimTaxonomyEntry, long> oldEntryIds = new Dictionary<SimTaxonomyEntry, long>();

            // gather all entry ids
            Stack<SimTaxonomyEntry> entryStack = new Stack<SimTaxonomyEntry>();
            foreach (var tax in newTaxonomies)
            {
                foreach (var entry in tax.Entries)
                    entryStack.Push(entry);
            }

            while (entryStack.Count > 0)
            {
                var e = entryStack.Pop();
                oldEntryIds.Add(e, e.LocalID);
                foreach (var entry in e.Children)
                {
                    entryStack.Push(entry);
                }
            }

            ResetIDs(defaults);
            defaults.ClearAllItems(true);

            // add new taxonomies
            foreach (var tax in newTaxonomies)
            {
                this.Add(tax);
            }

            // merge existing ones
            foreach (var exEntry in existingTaxonomies)
            {
                var newTax = exEntry.Key;
                var existingTax = exEntry.Value;

                // only need to merge if there was a change
                if (!existingTax.IsIdentical(newTax))
                {
                    hasChanges = true;
                    foreach (var existingEntry in existingTax.GetAllEntriesFlat())
                    {
                        var newEntry = newTax.GetTaxonomyEntryByKey(existingEntry.Key);

                        // entry still exists
                        if (newEntry != null)
                        {
                            // copy the id, so it is found again when restoring references
                            newEntry.Id = new SimId(existingEntry.LocalID);
                        }
                        // entry was removed from the new default taxonomies
                        else
                        {
                            // call delete event to remove dangling TaxonomyEntryReferences
                            existingEntry.OnIsBeingDeleted();
                        }
                    }
                    newTax.Id = new SimId(existingTax.LocalID);
                    // remove the existing taxonomy
                    this.RemoveItem(this.IndexOf(existingTax), true);
                    // add the new one with the updated ids
                    this.Add(newTax);
                }
            }

            IsMergeInProgress = false;
            defaults.IsMergeInProgress = false;
            StopLoading();

            return hasChanges;
        }

        /// <summary>
        /// Merges this taxonomy collection with another and returns the taxonomies that already existed.
        /// References to taxonomy entries of the existing taxonomies need to be manually reset to the existing ones after import.
        /// </summary>
        /// <param name="other">The other taxonomies to merge into this collection.</param>
        /// <returns>taxonomies that already existed in this collection.</returns>
        /// <exception cref="ArgumentNullException">if an argument is null</exception>
        public Dictionary<SimTaxonomy, SimTaxonomy> Merge(SimTaxonomyCollection other)
        {
            if (other == null)
                throw new ArgumentNullException(nameof(other));

            IsMergeInProgress = true;
            other.IsMergeInProgress = true;
            var existingTaxonomies = new Dictionary<SimTaxonomy, SimTaxonomy>();
            var newTaxonomies = new List<SimTaxonomy>();
            foreach (var tax in other)
            {
                SimTaxonomy mergeTax = null;
                // check all localizations if we find a match in one
                foreach (var loc in tax.Localization.Entries.Values)
                {
                    mergeTax = GetTaxonomyByKeyOrName(tax.Key, loc.Name, loc.Culture);
                    if (mergeTax != null)
                        break;
                }

                if (mergeTax != null && mergeTax.IsIdentical(tax))
                {
                    existingTaxonomies.Add(tax, mergeTax);
                }
                else
                {
                    newTaxonomies.Add(tax);
                }
            }

            List<(SimTaxonomy value, long id)> oldIds = newTaxonomies.Select(x => (x, x.LocalID)).ToList();
            Dictionary<SimTaxonomyEntry, long> oldEntryIds = new Dictionary<SimTaxonomyEntry, long>();

            // gather all entry ids
            Stack<SimTaxonomyEntry> entryStack = new Stack<SimTaxonomyEntry>();
            foreach (var tax in newTaxonomies)
            {
                foreach (var entry in tax.Entries)
                    entryStack.Push(entry);
            }

            while (entryStack.Count > 0)
            {
                var e = entryStack.Pop();
                oldEntryIds.Add(e, e.LocalID);
                foreach (var entry in e.Children)
                {
                    entryStack.Push(entry);
                }
            }

            ResetIDs(other);
            other.ClearAllItems(true);

            Dictionary<long, long> id_change_record = new Dictionary<long, long>();

            // get changed taxonomy ids
            foreach ((var tax, var oldId) in oldIds)
            {
                this.Add(tax);
                id_change_record.Add(oldId, tax.LocalID);
                foreach (var entry in tax.Entries)
                    entryStack.Push(entry);
            }

            // get changed taxonomy entry ids
            while (entryStack.Count > 0)
            {
                var e = entryStack.Pop();
                var oldId = oldEntryIds[e];
                id_change_record.Add(oldId, e.LocalID);

                foreach (var entry in e.Children)
                {
                    entryStack.Push(entry);
                }
            }

            IsMergeInProgress = false;
            other.IsMergeInProgress = false;

            return existingTaxonomies;
        }

        /// <summary>
        /// Call when loading of the project started
        /// </summary>
        public void StartLoading()
        {
            IsLoading = true;
        }

        /// <summary>
        /// Call when loading of the project stopped
        /// </summary>
        public void StopLoading()
        {
            IsLoading = false;
        }

        /// <summary>
        /// Invoked whenever a property of a <see cref="SimTaxonomyEntry"/> has changed.
        /// This event is invoked at the same time as the PropertyChanged event in the <see cref="SimTaxonomyEntry"/>, but
        /// allows to attach just a single event handler to track multiple entries.
        /// Prefer this event over the PropertyChanged event whenever manual tracking of a large number of entries is possible.
        /// </summary>
        public event PropertyChangedEventHandler TaxonomyEntryPropertyChanged;
        /// <summary>
        /// Invokes the <see cref="TaxonomyEntryPropertyChanged"/> event
        /// </summary>
        /// <param name="entry">The <see cref="SimTaxonomyEntry"/> in which the property has been changed</param>
        /// <param name="property">The name of the modified property</param>
        internal void NotifyTaxonomyEntryPropertyChanged(object entry, [CallerMemberName] string property = null)
        {
            TaxonomyEntryPropertyChanged?.Invoke(entry, new PropertyChangedEventArgs(property));
        }
    }
}
