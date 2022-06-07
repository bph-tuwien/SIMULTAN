using SIMULTAN.Exceptions;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.Components
{
    public partial class SimComponent
    {
        /// <summary>
        /// Stores child components inside a component. Handles id assignment for children.
        /// </summary>
        public class SimChildComponentCollection : ObservableCollection<SimChildComponentEntry>
        {
            private readonly SimComponent owner;

            /// <summary>
            /// Initializes a new instance of the ChildComponentCollection class
            /// </summary>
            /// <param name="owner">The component in which the components should be stored</param>
            internal SimChildComponentCollection(SimComponent owner)
            {
                this.owner = owner;
            }

            #region Collection Implementation

            /// <inheritdoc />
            protected override void InsertItem(int index, SimChildComponentEntry item)
            {
                if (item == null)
                    throw new ArgumentNullException(nameof(item));

                //Check access
                if (this.owner.Factory != null)
                {
                    var cu = this.owner.Factory.ProjectData.UsersManager.CurrentUser;
                    if (cu == null)
                        throw new AccessDeniedException("Please authenticate a user before performing operations");
                    else if (cu != null && this.owner.Factory.EnableAccessChecking)
                    {
                        if (!owner.HasAccess(cu, SimComponentAccessPrivilege.Write))
                            throw new AccessDeniedException("User does not have write access to the parent component");
                        if (item.Parent != null && !item.Parent.HasAccess(cu, SimComponentAccessPrivilege.Write))
                            throw new AccessDeniedException("Component is part of another component but the user does not have access rights to remove it");
                        if (item.Parent != null && item.Component != null && !item.Component.HasAccess(cu, SimComponentAccessPrivilege.Write))
                            throw new AccessDeniedException("Component is part of another component but the user does not have access rights to remove it");
                    }
                }

                if (item.Parent != null)
                {
                    if (item.Parent == this.owner)
                        throw new ArgumentException("Item is already part of the collection");

                    item.Parent.Components.RemoveWithoutDelete(item);
                }

                this.owner.RecordWriteAccess();
                this.owner.NotifyChanged();

                SetValues(item);
                base.InsertItem(index, item);

            }
            /// <inheritdoc />
            protected override void RemoveItem(int index)
            {
                var oldItem = this[index];

                //Check access
                if (this.owner.Factory != null)
                {
                    var cu = this.owner.Factory.ProjectData.UsersManager.CurrentUser;
                    if (cu == null)
                        throw new AccessDeniedException("Please authenticate a user before performing operations");
                    else if (cu != null && this.owner.Factory.EnableAccessChecking)
                    {
                        if (oldItem.Component != null && !oldItem.Component.HasSubtreeAccess(cu, SimComponentAccessPrivilege.Write))
                            throw new AccessDeniedException("User does not have write access on the removed component or on one of its subcomponents");
                        if (!this.owner.HasAccess(cu, SimComponentAccessPrivilege.Write))
                            throw new AccessDeniedException("User does not have write access to the parent component");
                    }
                }

                this.owner.RecordWriteAccess();
                this.owner.NotifyChanged();

                UnsetValues(oldItem);
                base.RemoveItem(index);
            }
            /// <inheritdoc />
            protected override void ClearItems()
            {
                //Check access
                if (this.owner.Factory != null)
                {
                    var cu = this.owner.Factory.ProjectData.UsersManager.CurrentUser;
                    if (cu == null)
                        throw new AccessDeniedException("Please authenticate a user before performing operations");
                    else if (cu != null && this.owner.Factory.EnableAccessChecking)
                    {
                        foreach (var entry in this)
                        {
                            if (entry.Component != null && !entry.Component.HasSubtreeAccess(cu, SimComponentAccessPrivilege.Write))
                                throw new AccessDeniedException("User does not have write access on the removed component or to one of its subcomponents");
                        }
                        if (!this.owner.HasAccess(cu, SimComponentAccessPrivilege.Write))
                            throw new AccessDeniedException("User does not have write access to the parent component");
                    }
                }

                this.owner.RecordWriteAccess();

                foreach (var item in this)
                {
                    UnsetValues(item);
                }
                base.ClearItems();
                owner.OnInstanceStateChanged();
                this.owner.NotifyChanged();
            }
            /// <inheritdoc />
            protected override void SetItem(int index, SimChildComponentEntry item)
            {
                if (item == null)
                    throw new ArgumentNullException(nameof(item));

                var oldItem = this[index];

                //Check access
                if (this.owner.Factory != null)
                {
                    var cu = this.owner.Factory.ProjectData.UsersManager.CurrentUser;
                    if (cu == null)
                        throw new AccessDeniedException("Please authenticate a user before performing operations");
                    else if (cu != null && this.owner.Factory.EnableAccessChecking)
                    {
                        if (oldItem.Component != null && !oldItem.Component.HasSubtreeAccess(cu, SimComponentAccessPrivilege.Write))
                            throw new AccessDeniedException("User does not have write access on the removed component or to one of its subcomponents");
                        if (!this.owner.HasAccess(cu, SimComponentAccessPrivilege.Write))
                            throw new AccessDeniedException("User does not have write access to the parent component");
                    }
                }

                this.owner.RecordWriteAccess();
                UnsetValues(oldItem);
                SetValues(item);
                base.SetItem(index, item);

                this.owner.NotifyChanged();
            }

            #endregion

            #region Additional Collection Methods

            /// <summary>
            /// Removes the component from the collection without deleting or unregistering it.
            /// This method may only be used when the component is added to another location in the same component factory immediately afterwards,
            /// e.g. when a component is moved from the current location to another parent.
            /// 
            /// This method preserves the Id and does not issue an <see cref="SimComponent.IsBeingDeleted"/> event.
            /// </summary>
            /// <param name="entry">The entry to remove</param>
            /// <returns>Always true</returns>
            internal bool RemoveWithoutDelete(SimChildComponentEntry entry)
            {
                var index = this.IndexOf(entry);
                if (index < 0)
                    return false;

                //Check access
                if (this.owner.Factory != null)
                {
                    var cu = this.owner.Factory.ProjectData.UsersManager.CurrentUser;
                    if (cu == null)
                        throw new AccessDeniedException("Please authenticate a user before performing operations");
                    else if (cu != null && this.owner.Factory.EnableAccessChecking)
                    {
                        if (entry.Component != null && !entry.Component.HasAccess(cu, SimComponentAccessPrivilege.Write))
                            throw new AccessDeniedException("User does not have write access on the removed component");
                        if (!this.owner.HasAccess(cu, SimComponentAccessPrivilege.Write))
                            throw new AccessDeniedException("User does not have write access to the parent component");
                    }
                }

                this.owner.RecordWriteAccess();
                this.owner.NotifyChanged();

                entry.OnRemoveWithoutDelete();

                base.RemoveItem(index);

                return true;
            }

            #endregion

            private void SetValues(SimChildComponentEntry item)
            {
                item.Parent = owner;
            }

            private void UnsetValues(SimChildComponentEntry item)
            {
                item.Parent = null;
            }

            internal void NotifyFactoryChanged(SimComponentCollection newValue, SimComponentCollection oldValue)
            {
                foreach (var item in this)
                    item.NotifyFactoryChanged(newValue, oldValue);
            }

            /// <summary>
            /// Returns a slot with the given base and the first available extension possible.
            /// </summary>
            /// <param name="slotBase">The base of the slot</param>
            /// <param name="extensionFormat">Extension format for the extension for <see cref="string.Format(string, object)"/>.
            /// The first placeholder will be filled with a integer (gets increased to find a free slot).</param>
            /// <returns>A slot containing the given slot base and an extension which hasn't been used in this collection</returns>
            public SimSlot FindAvailableSlot(SimSlotBase slotBase, string extensionFormat = "{0}")
            {
                var alreadyUsedSlots = this.Select(x => x.Slot).ToHashSet();
                int i = 0;

                while (true)
                {
                    var slotWithExtension = new SimSlot(slotBase, string.Format(extensionFormat, i));

                    if (!alreadyUsedSlots.Contains(slotWithExtension))
                        return slotWithExtension;

                    i++;
                }
            }
        }
    }
}
