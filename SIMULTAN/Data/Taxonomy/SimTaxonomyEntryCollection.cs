using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.Taxonomy
{
    /// <summary>
    /// Collection of <see cref="SimTaxonomyEntry"/> for the <see cref="SimTaxonomy"/>
    /// </summary>
    public class SimTaxonomyEntryCollection : ObservableCollection<SimTaxonomyEntry>
    {
        private readonly SimTaxonomy owner;

        /// <summary>
        /// Creates a new <see cref="SimTaxonomyEntryCollection"/>
        /// </summary>
        /// <param name="owner">The owner taxonomy of the collection.</param>
        public SimTaxonomyEntryCollection(SimTaxonomy owner) : base()
        {
            this.owner = owner;
        }

        #region Collection overrides
        /// <inheritdoc />
        protected override void InsertItem(int index, SimTaxonomyEntry item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));
            if (owner.IsReadonly)
                throw new InvalidOperationException("Cannot add Taxonomy entries to a read only taxonomy");

            // is already in taxonomy, so must be moved
            if (item.Taxonomy != null)
            {
                if (item.Taxonomy != owner)
                    throw new ArgumentException("Item belongs to a different Taxonomy");

                if (item.Parent == null)
                {
                    throw new ArgumentException("Item is already part of this collection");
                }
                else
                {
                    item.Parent.Children.RemoveWithoutDelete(item);
                }
            }

            SetValues(item);
            base.InsertItem(index, item);
        }

        /// <inheritdoc />
        protected override void RemoveItem(int index)
        {
            if (owner.IsReadonly)
                throw new InvalidOperationException("Cannot remove Taxonomy entry from read only Taxonomy.");

            var oldItem = this[index];

            UnsetValues(oldItem, owner.Factory?.ProjectData.IdGenerator);
            base.RemoveItem(index);
        }

        /// <inheritdoc />
        protected override void ClearItems()
        {
            if (owner.IsReadonly)
                throw new InvalidOperationException("Cannot clear Taxonomy entries of a read only Taxonomy.");

            foreach (var item in this)
            {
                UnsetValues(item, owner.Factory?.ProjectData.IdGenerator);
            }

            base.ClearItems();
        }

        /// <inheritdoc />
        protected override void SetItem(int index, SimTaxonomyEntry item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));
            if (owner.IsReadonly)
                throw new InvalidOperationException("Cannot set taxonomy entry because the taxonomy is read only.");

            var oldItem = this[index];
            UnsetValues(oldItem, owner.Factory?.ProjectData.IdGenerator);

            SetValues(item);
            base.SetItem(index, item);
        }

        #endregion

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
            item.Taxonomy = owner;
            item.Parent = null; // not parent cause parent is Taxonomy
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
