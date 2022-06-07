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

                SetValues(item);
                base.InsertItem(index, item);

                owner.OnInstanceStateChanged();

                SimComponentInstance.AddAutoParameters(owner);
                SimComponentInstance.UpdateAutoParameters(owner);

                //Notify geometry
                if (item.Factory != null)
                    item.Factory.ProjectData.ComponentGeometryExchange.OnInstanceAdded(item);
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
                owner.OnInstanceStateChanged();

                SimComponentInstance.UpdateAutoParameters(owner);
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
                owner.OnInstanceStateChanged();

                SimComponentInstance.UpdateAutoParameters(owner);
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

                owner.OnInstanceStateChanged();

                SimComponentInstance.UpdateAutoParameters(owner);

                //Notify geometry
                if (item.Factory != null)
                    item.Factory.ProjectData.ComponentGeometryExchange.OnInstanceAdded(item);
            }

            #endregion

            private void SetValues(SimComponentInstance item)
            {
                if (owner.InstanceType != item.InstanceType)
                    throw new InvalidStateException("Instance type has to match component's instance type");

                if (owner.Factory != null) //Ids are only possible when the component is already attached to a parent/factory.
                                           //If not the case, Id's have to be handed out when the component get's attached
                {
                    if (item.Id != SimId.Empty) //Use pre-stored id (only possible inside the same global location)
                    {
                        if (item.Id.GlobalId != Guid.Empty && item.Id.GlobalId != owner.Factory.CalledFromLocation.GlobalID)
                            throw new InvalidOperationException("Ids are not transferable between Factories. Please reset the Id before adding");

                        item.Id = new SimId(owner.Factory.CalledFromLocation, item.Id.LocalId);
                        owner.Factory.ProjectData.IdGenerator.Reserve(item, item.Id);
                    }
                    else
                        item.Id = owner.Factory.ProjectData.IdGenerator.NextId(item, owner.Factory.CalledFromLocation);

                    item.Factory = owner.Factory;
                }

                item.Component = owner;
            }

            private void UnsetValues(SimComponentInstance item)
            {
                item.OnIsBeingDeleted();

                if (owner.Factory != null && owner.Factory.ProjectData.IdGenerator != null)
                    owner.Factory.ProjectData.IdGenerator.Remove(item);
                item.Id = new SimId(item.Id.GlobalId, item.Id.LocalId);
                item.Factory = null;
                item.Component = null;
            }

            internal void NotifyFactoryChanged()
            {
                if (owner.Factory != null)
                {
                    foreach (var item in this)
                        SetValues(item);
                }
                else
                {
                    foreach (var item in this)
                        UnsetValues(item);
                }
            }

            #region Updates from Parameters

            internal void OnParameterAdded(SimParameter parameter)
            {
                if (parameter == null)
                    throw new ArgumentNullException(nameof(parameter));

                foreach (var gr in owner.Instances)
                    gr.AddParameter(parameter);
            }

            internal void OnParameterRemoved(SimParameter parameter)
            {
                if (parameter == null)
                    throw new ArgumentNullException(nameof(parameter));

                foreach (var gr in owner.Instances)
                    gr.RemoveParameter(parameter);
            }

            internal void OnParameterValueChanged(SimParameter parameter)
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
