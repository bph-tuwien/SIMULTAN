using SIMULTAN.Data.Taxonomy;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace SIMULTAN.Data.Components
{
    public partial class SimComponent
    {
        /// <summary>
        /// Stores a list of parameters inside a component.
        /// Also handles cross-parameter operations
        /// </summary>
        public class SimParameterCollection : ObservableCollection<SimParameter>
        {
            private SimComponent owner;


            /// <summary>
            /// Initializes a new instance of the SimParameterCollection class
            /// </summary>
            /// <param name="owner">The component this collection belongs to</param>
            public SimParameterCollection(SimComponent owner)
            {
                this.owner = owner;
            }


            #region Collection Implementation

            /// <inheritdoc />
            protected override void InsertItem(int index, SimParameter item)
            {
                if (item == null)
                    throw new ArgumentNullException(nameof(item));
                if (item.Factory != null)
                    throw new ArgumentException("item already belongs to a factory");

                this.owner.RecordWriteAccess();

                SetValues(item, true);
                base.InsertItem(index, item);
                
                SynchronizeParameterAdd(item);
            }
            /// <inheritdoc />
            protected override void RemoveItem(int index)
            {
                var oldItem = this[index];

                this.owner.RecordWriteAccess();

                UnsetValues(oldItem, owner.Factory?.ProjectData.IdGenerator, true);
                base.RemoveItem(index);

                SynchronizeParameterRemove(oldItem);

            }
            /// <inheritdoc />
            protected override void ClearItems()
            {
                this.owner.RecordWriteAccess();

                foreach (var item in this)
                {
                    UnsetValues(item, owner.Factory?.ProjectData.IdGenerator, true);
                }

                foreach (var item in this)
                    SynchronizeParameterRemove(item);

                base.ClearItems();
            }
            /// <inheritdoc />
            protected override void SetItem(int index, SimParameter item)
            {
                if (item == null)
                    throw new ArgumentNullException(nameof(item));

                this.owner.RecordWriteAccess();

                var oldItem = this[index];
                UnsetValues(oldItem, owner.Factory?.ProjectData.IdGenerator, true);
                SynchronizeParameterRemove(oldItem);                

                SynchronizeParameterRemove(oldItem);

                SetValues(item, true);
                base.SetItem(index, item);
                
                SynchronizeParameterAdd(item);
            }

            #endregion

            private void SetValues(SimParameter item, bool isAdded)
            {
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

                if (isAdded)
                {
                    item.Component = owner;
                }
            }

            private void UnsetValues(SimParameter item, SimIdGenerator idGenerator, bool isRemoved)
            {
                if (idGenerator != null)
                    idGenerator.Remove(item);

                item.OnIsBeingDeleted();
                item.Id = new SimId(item.Id.GlobalId, item.Id.LocalId);
                item.Factory = null;

                if (isRemoved)
                {
                    item.Component = null;
                }
            }

            /// <summary>
            /// Notifies the collection that the component has been attached to a new ComponentFactory
            /// </summary>
            internal void NotifyFactoryChanged(SimComponentCollection newValue, SimComponentCollection oldValue)
            {
                if (oldValue != null)
                {
                    foreach (var item in this)
                        UnsetValues(item, oldValue.ProjectData.IdGenerator, false);
                }

                if (newValue != null)
                {
                    foreach (var item in this)
                        SetValues(item, false);
                }
            }

            #region Delayed add/remove (has to be reworked completely)

            private void SynchronizeParameterAdd(SimParameter param)
            {
                owner.GatherCategoryInfo();

                if (param.Propagation == SimInfoFlow.FromReference)
                {
                    var refTarget = param.GetReferencedParameter();
                    if (refTarget != null)
                        ComponentParameters.PropagateParameterValueChange(param, refTarget);
                }

                owner.Instances?.OnParameterAdded(param); //Null during parsing constructor
            }

            private void SynchronizeParameterRemove(SimParameter param)
            {
                owner.GatherCategoryInfo();

                var comp = owner;
                owner.Instances?.OnParameterRemoved(param);

                while (comp != null)
                {
                    //Remove parameter from all mappings
                    foreach (var mapping in owner.CalculatorMappings)
                        RemoveMappingIfNecessary(mapping, param);
                    foreach (var dataComp in owner.MappedToBy)
                        foreach (var mapping in dataComp.CalculatorMappings)
                            RemoveMappingIfNecessary(mapping, param);

                    comp = (SimComponent)comp.Parent;
                }
            }

            private void RemoveMappingIfNecessary(CalculatorMapping mapping, SimParameter removedParameter)
            {
                for (int i = 0; i < mapping.InputMapping.Count; ++i)
                {
                    if (mapping.InputMapping[i].CalculatorParameter == removedParameter || mapping.InputMapping[i].DataParameter == removedParameter)
                    {
                        mapping.InputMapping.RemoveAt(i);
                        --i;
                    }
                }
                for (int i = 0; i < mapping.OutputMapping.Count; ++i)
                {
                    if (mapping.OutputMapping[i].CalculatorParameter == removedParameter || mapping.OutputMapping[i].DataParameter == removedParameter)
                    {
                        mapping.OutputMapping.RemoveAt(i);
                        --i;
                    }
                }
            }

            #endregion

            #region Delayed property changed (has to be reworked completely)

            private static readonly HashSet<string> propagatingParameterProperties = new HashSet<string>
            {
                nameof(SimParameter.ValueCurrent),
                nameof(SimParameter.ValueMin),
                nameof(SimParameter.ValueMax),
                nameof(SimParameter.TextValue),
                nameof(SimParameter.Category),
                nameof(SimParameter.TaxonomyEntry),
                nameof(SimParameter.Propagation)
            };

            internal void OnParameterPropertyChanged(object sender, string property)
            {
                var parameter = (SimParameter)sender;

                bool propagationEnabled = owner.Factory == null || owner.Factory.EnableReferencePropagation;

                if (propagationEnabled &&
                    propagatingParameterProperties.Contains(property))
                {
                    if (property == nameof(SimParameter.ValueCurrent))
                    {
                        owner.OnParameterValueChanged(parameter);
                    }
                    else if (property == nameof(SimParameter.TextValue))
                    {
                        owner.PropagateRefParamValueFromClosestRef(parameter);
                    }
                    else if (property == nameof(SimParameter.Category))
                    {
                        owner.GatherCategoryInfo();
                    }
                    else if (property == nameof(SimParameter.TaxonomyEntry))
                    {
                        owner.ReactToParameterPropagationChanged(parameter);
                    }
                    else if (property == nameof(SimParameter.Propagation) && parameter.Propagation == SimInfoFlow.FromReference && parameter.MultiValuePointer == null)
                    {
                        owner.ReactToParameterPropagationChanged(parameter);
                    }
                }
            }

            #endregion
        }
    }
}
