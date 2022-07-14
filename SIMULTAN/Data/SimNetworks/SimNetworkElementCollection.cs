using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace SIMULTAN.Data.SimNetworks
{
    public partial class BaseSimNetworkElement
    {
        /// <summary>
        /// Collection for the BaseSimNetworkElements
        /// </summary>
        public class SimNetworkElementCollection : ObservableCollection<BaseSimNetworkElement>
        {
            private SimNetwork parentNetwork;

            /// <summary>
            /// Initializes a new SimNetworkElementCollection
            /// </summary>
            /// <param name="parentNetwork">The SimNetwork containing these elements</param>
            public SimNetworkElementCollection(SimNetwork parentNetwork)
            {
                if (parentNetwork == null)
                    throw new ArgumentNullException(nameof(parentNetwork));
                this.parentNetwork = parentNetwork;
            }

            /// <summary>
            /// Adds a new item to the collection at the index
            /// </summary>
            /// <param name="index">The index</param>
            /// <param name="item">The new item</param>
            protected override void InsertItem(int index, BaseSimNetworkElement item)
            {
                if (item == null)
                    throw new ArgumentNullException(nameof(item));

                this.SetValues(item);
                base.InsertItem(index, item);
            }

            /// <summary>
            /// Removes an item at the index
            /// </summary>
            /// <param name="index">The index </param>
            protected override void RemoveItem(int index)
            {
                var oldItem = this[index];
                oldItem.OnIsBeingDeleted();
                this.UnsetValues(oldItem);
                base.RemoveItem(index);
                oldItem.OnIsDeleted();
            }
            /// <summary>
            /// Replaces an element in the collection at index
            /// </summary>
            /// <param name="index">The zero-based index</param>
            /// <param name="item">The new item</param>
            protected override void SetItem(int index, BaseSimNetworkElement item)
            {
                if (index == -1)
                    throw new ArgumentNullException(nameof(index));
                if (item == null)
                    throw new ArgumentNullException(nameof(item));


                this.Items[index].ParentNetwork = null;
                var oldItem = this[index];
                this.UnsetValues(oldItem);
                this.SetValues(item);
                base.SetItem(index, item);
            }
            /// <summary>
            /// Clears all the elements from the collection
            /// </summary>
            protected override void ClearItems()
            {
                foreach (var item in this)
                {
                    item.OnIsBeingDeleted();
                    this.UnsetValues(item);
                    item.OnIsDeleted();
                }
                base.ClearItems();
            }




            private void SetValues(BaseSimNetworkElement item)
            {
                if (item.Factory != null)
                    throw new ArgumentException("item already belongs to a factory");

                item.ParentNetwork = this.parentNetwork;

                if (this.parentNetwork.Factory != null)
                {
                    if (item.Id != SimId.Empty) //Used pre-stored id (only possible during loading)
                    {
                        if (this.parentNetwork.Factory.ProjectData.SimNetworks.IsLoading)
                        {
                            item.Id = new SimId(this.parentNetwork.Factory.CalledFromLocation, item.Id.LocalId);
                            this.parentNetwork.Factory.ProjectData.IdGenerator.Reserve(item, item.Id);
                        }
                        else
                            throw new NotSupportedException("Existing Ids may only be used during a loading operation");
                    }
                    else
                        item.Id = this.parentNetwork.Factory.ProjectData.IdGenerator.NextId(item, this.parentNetwork.Factory.CalledFromLocation);

                    item.Factory = item.ParentNetwork.Factory;
                }
            }

            private void UnsetValues(BaseSimNetworkElement item)
            {
                if (item is SimNetworkBlock block && block.ComponentInstance != null)
                {
                    block.ComponentInstance.Component.Instances.Remove(block.ComponentInstance);
                    foreach (var port in block.Ports.Where(t => t.ComponentInstance != null))
                    {
                        port.ComponentInstance.Component.Instances.Remove(port.ComponentInstance);
                    }
                }
                
                if (item.Factory != null)
                {
                    item.Factory.ProjectData.IdGenerator.Remove(item);
                    item.Factory = null;
                }

                item.Id = new SimId(item.Id.GlobalId, item.Id.LocalId);
                item.ParentNetwork = null;
                
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
