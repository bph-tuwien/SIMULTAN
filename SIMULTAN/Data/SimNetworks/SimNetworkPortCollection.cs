using System;
using System.Collections.ObjectModel;

namespace SIMULTAN.Data.SimNetworks
{
    public partial class SimNetworkPort
    {

        /// <summary>
        /// Collection for the Ports contains in SimNetworkBlocks and SimNetworks
        /// </summary>
        public class SimNetworkPortCollection : ObservableCollection<SimNetworkPort>
        {
            private BaseSimNetworkElement parentElement;

            /// <summary>
            /// Initializes a new SimNetworkPortCollection
            /// </summary>
            /// <param name="parentElement">The BaseSimNetworkElement which containes the ports</param>
            public SimNetworkPortCollection(BaseSimNetworkElement parentElement)
            {
                if (parentElement == null) { throw new ArgumentNullException(nameof(parentElement)); }
                this.parentElement = parentElement;
            }
            /// <summary>
            /// Adds a new SimNetworkPort to this collection
            /// </summary>
            /// <param name="index">The index where the new port is added</param>
            /// <param name="item">The new port</param>
            protected override void InsertItem(int index, SimNetworkPort item)
            {
                this.SetValues(item);
                base.InsertItem(index, item);

                (parentElement as SimNetworkBlock)?.OnPortAdded(item);
            }
            /// <summary>
            /// Removes a SimNetworkPort from this collection at the index
            /// </summary>
            /// <param name="index">The index of the port which is removed</param>
            protected override void RemoveItem(int index)
            {
                var oldItem = this[index];
                this.UnsetValues(oldItem);
                base.RemoveItem(index);

                (parentElement as SimNetworkBlock)?.OnPortRemoved(oldItem);
            }

            /// <summary>
            ///  Replaces the element at the specified index.
            /// </summary>
            /// <param name="index">The index where the new item should be placed</param>
            /// <param name="item">The new item</param>
            protected override void SetItem(int index, SimNetworkPort item)
            {
                this.Items[index].ParentNetworkElement = null;
                if (item == null)
                    throw new ArgumentNullException(nameof(item));

                var oldItem = this[index];
                this.UnsetValues(oldItem);
                this.SetValues(item);

                base.SetItem(index, item);

                (parentElement as SimNetworkBlock)?.OnPortRemoved(oldItem);
                (parentElement as SimNetworkBlock)?.OnPortAdded(item);
            }

            /// <summary>
            /// Deletes all the items in the collection
            /// </summary>
            protected override void ClearItems()
            {
                foreach (var item in this)
                {
                    this.UnsetValues(item);
                    (parentElement as SimNetworkBlock)?.OnPortRemoved(item);
                }
                base.ClearItems();
            }

            private void SetValues(SimNetworkPort item)
            {
                if (item.Factory != null)
                    throw new ArgumentException("item already belongs to a factory");

                item.ParentNetworkElement = this.parentElement;

                if (this.parentElement.Factory != null) // New component
                {
                    item.Factory = this.parentElement.Factory;
                    if (item.Id != SimId.Empty) //Used pre-stored id (only possible during loading)
                    {
                        if (this.parentElement.Factory.ProjectData.SimNetworks.IsLoading)
                        {
                            item.Id = new SimId(this.parentElement.Factory.CalledFromLocation, item.Id.LocalId);
                            this.parentElement.Factory.ProjectData.IdGenerator.Reserve(item, item.Id);
                        }
                        else
                            throw new NotSupportedException("Existing Ids may only be used during a loading operation");
                    }
                    else
                        item.Id = this.parentElement.Factory.ProjectData.IdGenerator.NextId(item, this.parentElement.Factory.CalledFromLocation);
                }
            }
            private void UnsetValues(SimNetworkPort item)
            {
                item.RemoveConnections();
                item.Dispose();

                if (item.Factory != null)
                {
                    item.Factory.ProjectData.IdGenerator.Remove(item);
                    item.Factory = null;
                }

                item.Id = new SimId(item.Id.GlobalId, item.Id.LocalId);
                item.ParentNetworkElement = null;
            }

            internal void NotifyFactoryChanged(ISimManagedCollection newFactory, ISimManagedCollection oldFactory)
            {
                if (oldFactory != null)
                {
                    foreach (var item in this)
                        UnsetValues(item);
                }

                if (newFactory != null)
                {
                    foreach (var item in this)
                        SetValues(item);
                }
            }
        }
    }
}
