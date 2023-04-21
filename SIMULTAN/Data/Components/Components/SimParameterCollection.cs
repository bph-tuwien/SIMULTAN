using SIMULTAN.Data.Geometry;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace SIMULTAN.Data.Components
{
    public partial class SimComponent
    {
        /// <summary>
        /// Stores a list of parameters inside a component.
        /// Also handles cross-parameter operations
        /// </summary>
        public class SimParameterCollection : ObservableCollection<SimBaseParameter>
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
            protected override void InsertItem(int index, SimBaseParameter item)
            {
                if (item == null)
                    throw new ArgumentNullException(nameof(item));
                if (item.Component == this.owner && this.Contains(item))
                    throw new ArgumentException("Parameter is already part of this collection");

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

                if (owner.Factory != null && oldItem is SimDoubleParameter numeric && numeric.ValueSource is SimGeometryParameterSource gpsAdd)
                    owner.Factory.ProjectData.ComponentGeometryExchange.OnParameterSourceRemoved(gpsAdd);

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
                    if (owner.Factory != null && item is SimDoubleParameter numeric && numeric.ValueSource is SimGeometryParameterSource gpsAdd)
                        owner.Factory.ProjectData.ComponentGeometryExchange.OnParameterSourceRemoved(gpsAdd);

                    UnsetValues(item, owner.Factory?.ProjectData.IdGenerator, true);
                }

                foreach (var item in this)
                    SynchronizeParameterRemove(item);

                base.ClearItems();
            }
            /// <inheritdoc />
            protected override void SetItem(int index, SimBaseParameter item)
            {
                if (item == null)
                    throw new ArgumentNullException(nameof(item));

                this.owner.RecordWriteAccess();

                var oldItem = this[index];
                if (owner.Factory != null && oldItem is SimDoubleParameter numeric && numeric.ValueSource is SimGeometryParameterSource gpsAdd)
                    owner.Factory.ProjectData.ComponentGeometryExchange.OnParameterSourceRemoved(gpsAdd);

                UnsetValues(oldItem, owner.Factory?.ProjectData.IdGenerator, true);
                SynchronizeParameterRemove(oldItem);

                SetValues(item, true);
                base.SetItem(index, item);

                SynchronizeParameterAdd(item);
            }

            #endregion

            private void SetValues(SimBaseParameter item, bool isAdded)
            {
                if (owner.Factory != null)
                {
                    if (item.Factory == null)
                    {
                        if (item.Id != SimId.Empty) //Use pre-stored id (only possible during loading)
                        {
                            if (owner.Factory.IsLoading)
                            {
                                item.Id = new SimId(owner.Factory.CalledFromLocation, item.Id.LocalId);
                                owner.Factory.ProjectData.IdGenerator.Reserve(item, item.Id);
                            }
                            else
                                throw new NotSupportedException("Existing Ids may only be used during a loading operation");
                        }
                        else //New Id
                        {
                            item.Id = owner.Factory.ProjectData.IdGenerator.NextId(item, owner.Factory.CalledFromLocation);
                        }

                        item.Factory = owner.Factory;
                    }
                    else //Move
                    {
                        if (item.Factory != owner.Factory)
                            throw new NotSupportedException("Child components must be part of the same factory as the parent");

                        item.Component.Parameters.RemoveWithoutDelete(item);
                    }
                }

                if (isAdded)
                {
                    item.Component = owner;
                }


            }

            private bool RemoveWithoutDelete(SimBaseParameter item)
            {
                var index = this.IndexOf(item);
                if (index < 0)
                    return false;

                this.owner.RecordWriteAccess();
                base.RemoveItem(index);

                return true;
            }

            private void UnsetValues(SimBaseParameter item, SimIdGenerator idGenerator, bool isRemoved)
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

            private void SynchronizeParameterAdd(SimBaseParameter param)
            {
                owner.GatherCategoryInfo();

                if (param.Propagation == SimInfoFlow.FromReference)
                {
                    var refTarget = param.GetReferencedParameter();
                    if (refTarget != null)
                        ComponentParameters.PropagateParameterValueChange(param, refTarget);
                }

                owner.Instances?.OnParameterAdded(param); //Null during parsing constructor

                if (owner.Factory != null && param is SimDoubleParameter numeric && numeric.ValueSource is SimGeometryParameterSource gpsAdd)
                    owner.Factory.ProjectData.ComponentGeometryExchange.OnParameterSourceAdded(gpsAdd);
            }

            private void SynchronizeParameterRemove(SimBaseParameter param)
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

                    comp = comp.Parent;
                }
            }

            private void RemoveMappingIfNecessary(CalculatorMapping mapping, SimBaseParameter removedParameter)
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
                "Value",
                nameof(SimBaseNumericParameter<ValueType>.ValueMin),
                nameof(SimBaseNumericParameter<ValueType>.ValueMax),
                nameof(SimBaseParameter.Description),
                nameof(SimBaseParameter.Category),
                nameof(SimBaseParameter.NameTaxonomyEntry),
                nameof(SimBaseParameter.Propagation),
                nameof(SimBaseParameter.InstancePropagationMode)
            };

            internal void OnParameterPropertyChanged(object sender, string property)
            {
                var parameter = (SimBaseParameter)sender;

                bool propagationEnabled = owner.Factory == null || owner.Factory.EnableReferencePropagation;

                if (propagationEnabled &&
                    propagatingParameterProperties.Contains(property))
                {
                    if (property == nameof(SimBaseParameter.Value)) // object is a placeholder, because Value appears in the SimBaseParameter<T>
                    {
                        owner.OnParameterValueChanged(parameter);
                    }
                    else if (property == nameof(SimBaseParameter.Description))
                    {
                        owner.PropagateRefParamValueFromClosestRef(parameter);
                    }
                    else if (property == nameof(SimBaseParameter.Category))
                    {
                        owner.GatherCategoryInfo();
                    }
                    else if (property == nameof(SimBaseParameter.NameTaxonomyEntry))
                    {
                        owner.ReactToParameterPropagationChanged(parameter);
                    }
                    else if (property == nameof(SimBaseParameter.Propagation) && parameter.Propagation == SimInfoFlow.FromReference)
                    {
                        if (parameter.ValueSource == null)
                            owner.ReactToParameterPropagationChanged(parameter);

                    }
                    //Update the instance values if needed
                    else if (property == nameof(SimBaseParameter.InstancePropagationMode))
                    {
                        if (parameter.InstancePropagationMode == SimParameterInstancePropagation.PropagateAlways ||
                            parameter.InstancePropagationMode == SimParameterInstancePropagation.PropagateIfInstance)
                        {
                            owner.OnParameterValueChanged(parameter);
                        }
                    }
                }
            }

            #endregion
        }
    }
}
