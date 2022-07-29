using System;
using System.Collections.ObjectModel;

namespace SIMULTAN.Data.SimNetworks
{
    /// <summary>
    /// Represents a connection between two SimNetworkPorts
    /// </summary>
    public partial class SimNetworkConnector
    {
        /// <summary>
        /// Collection storing the connectors occuring in a network
        /// </summary>
        public class SimNetworkConnectorCollection : ObservableCollection<SimNetworkConnector>
        {
            /// <summary>
            /// The parent network where the connectors occure
            /// </summary>
            private SimNetwork parentNetwork;

            /// <summary>
            /// Initializes a new SimNetworkConnectorCollection
            /// </summary>
            /// <param name="parentNetwork">The parent network</param>
            public SimNetworkConnectorCollection(SimNetwork parentNetwork)
            {
                if (parentNetwork == null)
                    throw new ArgumentNullException(nameof(parentNetwork));
                this.parentNetwork = parentNetwork;
            }

            /// <summary>
            /// Inserts an item into the collection at the specified index.
            /// </summary>
            /// <param name="index">The index</param>
            /// <param name="item">The new item</param>
            protected override void InsertItem(int index, SimNetworkConnector item)
            {
                if (item == null)
                    throw new ArgumentNullException(nameof(item));
                if (this.Contains(item))
                {
                    throw new Exception("Connector is already contained in this collection");
                }
                base.InsertItem(index, item);
                this.SetValues(item);
            }
            /// <summary>
            /// Removes an item at index
            /// </summary>
            /// <param name="index">The index</param>
            protected override void RemoveItem(int index)
            {
                var oldItem = this[index];
                base.RemoveItem(index);
                this.UnsetValues(oldItem);
            }

            /// <inheritdoc />
            protected override void SetItem(int index, SimNetworkConnector item)
            {
                if (item == null)
                    throw new ArgumentNullException(nameof(item));

                var oldItem = this[index];
                this.UnsetValues(oldItem);
                this.SetValues(item);
                base.SetItem(index, item);
            }
            /// <summary>
            /// Clears all the items from the collection   
            /// </summary>
            protected override void ClearItems()
            {
                foreach (var item in this)
                {
                    this.UnsetValues(item);
                }
                base.ClearItems();
            }


            private void SetValues(SimNetworkConnector item)
            {
                if (item.Factory != null)
                    throw new ArgumentException("item already belongs to a factory");

                item.ParentNetwork = this.parentNetwork;

                if (this.parentNetwork.Factory != null) // New component
                {
                    item.Factory = this.parentNetwork.Factory;
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
                }


                if (item.Source != null)
                {
                    item.Source.NotifyIsConnectedChanged();
                }
                if (item.Target != null)
                {
                    item.Target.NotifyIsConnectedChanged();
                }


            }

            private void UnsetValues(SimNetworkConnector item)
            {
                item.ParentNetwork = null;

                if (item.Factory != null)
                {
                    item.Factory.ProjectData.IdGenerator?.Remove(item);
                    item.Factory = null;
                }

                item.Id = new SimId(item.Id.GlobalId, item.Id.LocalId);



                if (item.Source != null)
                {
                    item.Source.NotifyIsConnectedChanged();
                }
                if (item.Target != null)
                {
                    item.Target.NotifyIsConnectedChanged();
                }

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
