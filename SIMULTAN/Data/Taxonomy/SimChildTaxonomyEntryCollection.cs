using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.Taxonomy
{
    /// <summary>
    /// Collection of child <see cref="SimTaxonomyEntry"/> for a <see cref="SimTaxonomyEntry"/>
    /// </summary>
    public class SimChildTaxonomyEntryCollection : ObservableCollection<SimTaxonomyEntry>
    {
        private readonly SimTaxonomyEntry owner;

        /// <summary>
        /// Creates a new <see cref="SimChildTaxonomyEntryCollection"/>
        /// </summary>
        /// <param name="owner">The owner <see cref="SimTaxonomyEntry"/> of the collection.</param>
        public SimChildTaxonomyEntryCollection(SimTaxonomyEntry owner) : base()
        {
            this.owner = owner;
        }

        #region Collection overrides
        /// <inheritdoc />
        protected override void InsertItem(int index, SimTaxonomyEntry item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));
            if (owner.Taxonomy != null && owner.Taxonomy.IsReadonly)
                throw new InvalidOperationException("Cannot add taxonomy entry because the taxonomy is read only.");

            // is already in taxonomy, so must be moved
            if (item.Taxonomy != null)
            {
                if (item.Parent != null)
                {
                    if (item.Parent == owner)
                        throw new ArgumentException("Item is already part of this collection");
                    item.Parent.Children.RemoveWithoutDelete(item);
                }
                else
                {
                    item.Taxonomy.Entries.RemoveWithoutDelete(item);
                }
            }

            SetValues(item);
            base.InsertItem(index, item);
            owner.NotifyChildrenChanged();
        }

        /// <inheritdoc />
        protected override void RemoveItem(int index)
        {
            if (owner.Taxonomy != null && owner.Taxonomy.IsReadonly)
                throw new InvalidOperationException("Cannot remove taxonomy entry because the taxonomy is read only.");

            var oldItem = this[index];

            UnsetValues(oldItem, owner.Factory?.ProjectData.IdGenerator);
            base.RemoveItem(index);
            owner.NotifyChildrenChanged();
        }

        /// <inheritdoc />
        protected override void ClearItems()
        {
            if (owner.Taxonomy != null && owner.Taxonomy.IsReadonly)
                throw new InvalidOperationException("Cannot clear taxonomy entry children because the taxonomy is read only.");

            foreach (var item in this)
            {
                UnsetValues(item, owner.Factory?.ProjectData.IdGenerator);
            }

            base.ClearItems();
            owner.NotifyChildrenChanged();
        }

        /// <inheritdoc />
        protected override void SetItem(int index, SimTaxonomyEntry item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));
            if (owner.Taxonomy != null && owner.Taxonomy.IsReadonly)
                throw new InvalidOperationException("Cannot set taxonomy entry because the taxonomy is read only.");

            var oldItem = this[index];
            UnsetValues(oldItem, owner.Factory?.ProjectData.IdGenerator);

            SetValues(item);
            base.SetItem(index, item);
            owner.NotifyChildrenChanged();
        }

        #endregion

        private void UnsetValues(SimTaxonomyEntry item, SimIdGenerator idGenerator)
        {
            if (idGenerator != null)
                idGenerator.Remove(item);

            item.Id = new SimId(item.GlobalID, item.LocalID);
            item.Taxonomy = null;
            item.Factory = null;
            item.Parent = null;
        }

        private void SetValues(SimTaxonomyEntry item)
        {
            item.Taxonomy = owner.Taxonomy;
            item.Parent = owner;
        }

        /// <summary>
        /// Just removes an entry from the collection without triggering parameter resets (Actually
        /// deleting it)
        /// </summary>
        /// <param name="entry">The entry to remove.</param>
        /// <returns>True if found and delete, false otherwise.</returns>
        internal bool RemoveWithoutDelete(SimTaxonomyEntry entry)
        {
            int i = IndexOf(entry);
            if (i < 0)
                return false;

            base.RemoveItem(i);

            return true;
        }

        /// <summary>
        /// Notifies the collection that the component has been attached to a new ComponentFactory
        /// </summary>
        internal void NotifyFactoryChanged(SimTaxonomyCollection newValue, SimTaxonomyCollection oldValue)
        {
            foreach (var item in Items)
            {
                item.NotifyFactoryChanged(newValue, oldValue);
            }
        }
    }
}
