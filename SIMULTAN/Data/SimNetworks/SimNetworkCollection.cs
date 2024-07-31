using SIMULTAN.Projects;
using System;
using System.Collections.Generic;

namespace SIMULTAN.Data.SimNetworks
{
    public partial class SimNetwork : BaseSimNetworkElement, INetwork
    {
        /// <summary>
        /// Collection storing the <see cref="SimNetwork"/>, and also serves as the Factory for the contained elements, handling the ID assignment
        /// </summary>
        public class SimNetworkCollection : SimManagedCollection<SimNetwork>
        {

            /// <summary>
            /// Sets IndexOfGeometricRepFile to -1 for SimNetworks in the collection which has resource_id as the IndexOfGeometricRepFile
            /// </summary>
            /// <param name="resource_id">The id of the resource</param>
            public void DisconnectAllInstances(int resource_id)
            {
                foreach (SimNetwork nw in this)
                {
                    if (nw.IndexOfGeometricRepFile == resource_id)
                        nw.IndexOfGeometricRepFile = -1;
                }
            }


            /// <summary>
            /// Initializes a new instance of the SimNetworkCollection class
            /// </summary>
            public SimNetworkCollection(ProjectData owner) : base(owner)
            {
            }

            /// <inheritdoc />
            protected override void InsertItem(int index, SimNetwork item)
            {
                if (item == null)
                    throw new ArgumentNullException(nameof(item));

                this.SetValues(item);
                base.InsertItem(index, item);
                this.NotifyChanged();
            }
            /// <inheritdoc />
            protected override void RemoveItem(int index)
            {
                var oldItem = this[index];
                this.UnsetValues(oldItem);
                base.RemoveItem(index);
                this.NotifyChanged();
            }

            /// <summary>
            /// Removes a SimNetwork from the collection, but does not delete the contained collections (connectors, ports and contained elements)
            /// And does not fired "IsBeingDeleted" event and "IsDeleted" event. 
            /// Used during load operation
            /// </summary>
            /// <param name="item">The SimNetwork to remove</param>
            public void RemoveItemSoft(SimNetwork item)
            {
                var index = this.IndexOf(item);
                this.ProjectData.IdGenerator.Remove(item);
                item.Id = new SimId(item.Id.GlobalId, item.Id.LocalId);
                item.Factory = null;
                base.RemoveItem(index);
            }

            /// <inheritdoc />
            protected override void ClearItems()
            {
                foreach (var item in this)
                    this.UnsetValues(item);
                base.ClearItems();
                this.NotifyChanged();
            }
            /// <inheritdoc />
            protected override void SetItem(int index, SimNetwork item)
            {
                if (item == null)
                    throw new ArgumentNullException(nameof(item));

                var oldItem = this[index];
                this.UnsetValues(oldItem);
                this.SetValues(item);
                base.SetItem(index, item);
                this.NotifyChanged();
            }


            private void SetValues(SimNetwork item)
            {
                if (item.Factory != null)
                    throw new ArgumentException("item already belongs to a factory");


                if (item.Factory == null) // New Network
                {
                    if (item.Id != SimId.Empty) //Used pre-stored id (only possible during loading)
                    {
                        if (this.ProjectData.SimNetworks.IsLoading)
                        {
                            item.Id = new SimId(this.CalledFromLocation, item.Id.LocalId);
                            this.ProjectData.IdGenerator.Reserve(item, item.Id);
                        }
                        else
                            throw new NotSupportedException("Existing Ids may only be used during a loading operation");
                    }
                    else
                        item.Id = this.ProjectData.IdGenerator.NextId(item, this.CalledFromLocation);

                    item.Factory = this;
                }
                else if (item.Factory == this) //Move operation
                {
                    throw new ArgumentException("Component is already part of the collection");
                }
            }



            private void UnsetValues(SimNetwork item)
            {
                item.OnIsBeingDeleted();
                this.ProjectData.IdGenerator.Remove(item);
                item.Id = new SimId(item.Id.GlobalId, item.Id.LocalId);

                item.ParentNetwork = null;

                item.Factory = null;
                item.NotifyIsDeleted();
            }




            /// <summary>
            /// Gets the elements in the network which are IElementWithComponent
            /// </summary>
            /// <returns>List of IElementWithComponent</returns>
            public List<IElementWithComponent> GetElementsWithComponents()
            {
                var elements = new List<IElementWithComponent>();
                foreach (var be in this)
                {
                    if (be is SimNetwork nw)
                    {
                        foreach (var item in nw.ContainedElements)
                        {
                            if (item is SimNetworkBlock block)
                            {
                                elements.Add(block);
                                foreach (var port in block.Ports)
                                {
                                    elements.Add(port);
                                }
                            }
                        }
                        foreach (var port in nw.Ports)
                        {
                            elements.Add(port);
                        }
                    }
                }
                return elements;
            }

            #region Loading

            /// <summary>
            /// Whenever this collection is being loaded during an I/O operation
            /// </summary>
            public bool IsLoading { get; private set; }

            /// <summary>
            /// Sets the factory in loading mode
            /// </summary>
            public void StartLoading()
            {
                this.IsLoading = true;
            }
            /// <summary>
            /// Ends the loading operation and re-enables Id checking
            /// </summary>
            public void EndLoading()
            {
                this.IsLoading = false;
            }

            #endregion


            internal void RestoreReferences()
            {
                foreach (var nw in this)
                    nw.RestoreReferences();
            }
        }
    }
}

