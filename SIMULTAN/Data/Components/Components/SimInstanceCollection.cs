using SIMULTAN.Exceptions;
using System;
using System.Collections.ObjectModel;

namespace SIMULTAN.Data.Components
{
    public partial class SimComponent
    {
        /// <summary>
        /// Stores the instances of a component.
        /// Handles setting of properties like <see cref="SimComponentInstance.Component"/> and assigns Ids to the instances
        /// </summary>
        public class SimInstanceCollection : ObservableCollection<SimComponentInstance>
        {
            private readonly SimComponent owner;

            /// <summary>
            /// Initializes a new instance of the SimInstanceCollection class
            /// </summary>
            /// <param name="owner">The component in which the instances should be stored</param>
            public SimInstanceCollection(SimComponent owner)
            {
                this.owner = owner;
            }

            #region Collection Implementation

            /// <inheritdoc />
            protected override void InsertItem(int index, SimComponentInstance item)
            {
                if (item == null)
                    throw new ArgumentNullException(nameof(item));
                if (item.Factory != null)
                    throw new ArgumentException("item already belongs to a factory");

                this.owner.RecordWriteAccess();

                this.SetValues(item);
                base.InsertItem(index, item);

                this.owner.OnInstanceStateChanged();

                if (item.Factory != null)
                {
                    SimComponentInstance.AddAutoParameters(this.owner);
                    SimComponentInstance.UpdateAutoParameters(this.owner);

                    //Notify geometry
                    item.Factory.ProjectData.ComponentGeometryExchange.OnInstanceAdded(item);
                }
            }
            /// <inheritdoc />
            protected override void RemoveItem(int index)
            {
                var oldItem = this[index];

                this.owner.RecordWriteAccess();

                //Notify geometry
                if (oldItem.Factory != null)
                    oldItem.Factory.ProjectData.ComponentGeometryExchange.OnInstanceRemoved(oldItem);

                UnsetValues(oldItem);

                base.RemoveItem(index);
                this.owner.OnInstanceStateChanged();

                SimComponentInstance.UpdateAutoParameters(this.owner);
            }
            /// <inheritdoc />
            protected override void ClearItems()
            {
                this.owner.RecordWriteAccess();

                foreach (var item in this)
                {
                    //Notify geometry
                    if (item.Factory != null)
                        item.Factory.ProjectData.ComponentGeometryExchange.OnInstanceRemoved(item);
                    UnsetValues(item);
                }
                base.ClearItems();
                this.owner.OnInstanceStateChanged();

                SimComponentInstance.UpdateAutoParameters(this.owner);
            }
            /// <inheritdoc />
            protected override void SetItem(int index, SimComponentInstance item)
            {
                if (item == null)
                    throw new ArgumentNullException(nameof(item));

                this.owner.RecordWriteAccess();

                var oldItem = this[index];

                //Notify geometry
                if (item.Factory != null)
                    item.Factory.ProjectData.ComponentGeometryExchange.OnInstanceRemoved(item);

                UnsetValues(oldItem);
                SetValues(item);
                base.SetItem(index, item);

                this.owner.OnInstanceStateChanged();

                SimComponentInstance.UpdateAutoParameters(this.owner);

                //Notify geometry
                if (item.Factory != null)
                    item.Factory.ProjectData.ComponentGeometryExchange.OnInstanceAdded(item);
            }


            #endregion

            private void SetValues(SimComponentInstance item)
            {
                if (this.owner.Factory != null) //Ids are only possible when the component is already attached to a parent/factory.
                                                //If not the case, Id's have to be handed out when the component get's attached
                {
                    if (item.Id != SimId.Empty) //Use pre-stored id (only possible inside the same global location)
                    {
                        if (item.Id.GlobalId != Guid.Empty && item.Id.GlobalId != this.owner.Factory.CalledFromLocation.GlobalID)
                            throw new InvalidOperationException("Ids are not transferable between Factories. Please reset the Id before adding");

                        item.Id = new SimId(this.owner.Factory.CalledFromLocation, item.Id.LocalId);
                        this.owner.Factory.ProjectData.IdGenerator.Reserve(item, item.Id);
                    }
                    else
                    {
                        item.Id = this.owner.Factory.ProjectData.IdGenerator.NextId(item, this.owner.Factory.CalledFromLocation);
                    }

                    item.Factory = this.owner.Factory;
                }

                item.Component = this.owner;
            }

            private void UnsetValues(SimComponentInstance item)
            {
                item.OnIsBeingDeleted();

                if (this.owner.Factory != null && this.owner.Factory.ProjectData.IdGenerator != null)
                    this.owner.Factory.ProjectData.IdGenerator.Remove(item);
                item.Id = new SimId(item.Id.GlobalId, item.Id.LocalId);
                item.Factory = null;
                item.Component = null;
            }

            internal void NotifyFactoryChanged(SimComponentCollection newFactory, SimComponentCollection oldFactory)
            {
                if (this.owner.Factory != null)
                {
                    foreach (var item in this)
                        this.SetValues(item);
                }
                else
                {
                    foreach (var item in this)
                        this.UnsetValues(item);
                }
            }

            #region Updates from Parameters

            internal void OnParameterAdded(SimBaseParameter parameter)
            {
                if (parameter == null)
                    throw new ArgumentNullException(nameof(parameter));

                foreach (var gr in this.owner.Instances)
                    gr.AddParameter(parameter);
            }

            internal void OnParameterRemoved(SimBaseParameter parameter)
            {
                if (parameter == null)
                    throw new ArgumentNullException(nameof(parameter));

                foreach (var gr in this.owner.Instances)
                    gr.RemoveParameter(parameter);
            }

            internal void OnParameterValueChanged(SimBaseParameter parameter)
            {
                if (parameter == null)
                    throw new ArgumentNullException(nameof(parameter));

                foreach (var instance in owner.Instances)
                    instance.ChangeParameterValue(parameter);
            }

            #endregion
        }
    }
}
