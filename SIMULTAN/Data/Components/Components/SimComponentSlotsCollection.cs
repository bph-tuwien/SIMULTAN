using SIMULTAN.Data.SitePlanner;
using SIMULTAN.Data.Taxonomy;
using SIMULTAN.Utils;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace SIMULTAN.Data.Components
{
    /// <summary>
    /// Collection storing the SImTaxonomyEntry references of the component
    /// </summary>
    public class SimComponentSlotsCollection : ObservableCollection<SimTaxonomyEntryReference>
    {
        private SimComponent component;

        /// <summary>
        /// Creates a new instance of the <see cref="SimComponentSlotsCollection"/> class
        /// </summary>
        public SimComponentSlotsCollection(SimComponent component)
        {
            if (component == null)
                throw new ArgumentNullException(nameof(component));

            this.component = component;
        }

        #region Collection overrides

        /// <inheritdoc/>
        protected override void InsertItem(int index, SimTaxonomyEntryReference item)
        {
            if (!(item is SimPlaceholderTaxonomyEntryReference) && (item.Target == null || item.Target.Taxonomy == null || item.Target.Factory == null))
            {
                throw new Exception("Taxonomy entry of a slot needs to be in a taxonomy and project");
            }

            if (this.Any(x => x.Target == item.Target))
                throw new Exception("Slot is already contained in the collection");

            component.RecordWriteAccess();
            base.InsertItem(index, item);

            item.SetDeleteAction(CurrentSlotTaxonomyEntryDeleted);
            component.ReactSlotsChanged();
        }

        /// <inheritdoc/>
        protected override void RemoveItem(int index)
        {
            var oldItem = this[index];

            if (component.Factory != null)
            {
                if (this.component.ParentContainer != null && this[index].Target == this.component.ParentContainer.Slot.SlotBase.Target)
                    throw new NotSupportedException("Can not remove ParentContainer´s slot");
                else if (this.Count == 1)
                    throw new NotSupportedException("Unable to remove last slot from component");
            }

            component.RecordWriteAccess();

            oldItem.RemoveDeleteAction();
            component.ReactSlotsChanged();

            base.RemoveItem(index);
        }

        /// <inheritdoc/>
        protected override void ClearItems()
        {
            throw new NotSupportedException("Not supported");
        }
        /// <inheritdoc/>
        protected override void MoveItem(int oldIndex, int newIndex)
        {
            base.MoveItem(oldIndex, newIndex);
            component.ReactSlotsChanged();
        }
        /// <inheritdoc/>
        protected override void SetItem(int index, SimTaxonomyEntryReference item)
        {
            if (!(item is SimPlaceholderTaxonomyEntryReference) && (item.Target == null || item.Target.Taxonomy == null || item.Target.Factory == null))
            {
                throw new Exception("Taxonomy entry of a slot needs to be in a taxonomy and project");
            }
            if (this.component.ParentContainer != null)
            {
                if (this.component.ParentContainer.Slot.SlotBase.Target != null &&
                    this[index].Target == this.component.ParentContainer.Slot.SlotBase.Target
                    && this.component.ParentContainer.Slot.SlotBase.Target.Id != item.Target.Id)
                {
                    throw new NotSupportedException("Can not remove ParentContainer´s slot");
                }
            }

            var oldItem = this[index];

            component.RecordWriteAccess();

            base.SetItem(index, item);

            item.SetDeleteAction(CurrentSlotTaxonomyEntryDeleted);
            oldItem.RemoveDeleteAction();

            component.ReactSlotsChanged();
        }

        #endregion

        private void CurrentSlotTaxonomyEntryDeleted(SimTaxonomyEntry caller)
        {
            if (this.Count == 1 && component.Factory != null) //When last slot, add undefined slot
            {
                var undefinedTax = component.GetDefaultSlotTaxonomyEntry(SimDefaultSlotKeys.Undefined);
                this.Add(new SimTaxonomyEntryReference(undefinedTax));
            }

            if (component.ParentContainer != null && component.ParentContainer.Slot.SlotBase.Target == caller)
            {
                var otherSlot = this.FirstOrDefault(x => x.Target != caller).Target;
                component.ParentContainer.Slot = new SimSlot(otherSlot, component.ParentContainer.Slot.SlotExtension);
            }

            this.RemoveFirst(t => t.Target == caller);
        }
    }
}
