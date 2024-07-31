using SIMULTAN.Data.Assets;
using SIMULTAN.Data.FlowNetworks;
using SIMULTAN.Data.Taxonomy;
using SIMULTAN.Data.Users;
using SIMULTAN.Exceptions;
using SIMULTAN.Projects;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace SIMULTAN.Data.Components
{
    /// <summary>
    /// Stores a component and a slot for being used as a child component
    /// A entry always has a slot, but it can either be empty (a placeholder for a component) or have a valid component attached.
    /// </summary>
    public class SimChildComponentEntry : INotifyPropertyChanged
    {
        #region Properties

        /// <summary>
        /// The component in this entry. When set to null, the entry is a placeholder for a component.
        /// A component assigned to this property has to have a <see cref="SimComponent.Slots"/> matching the <see cref="Slot"/> base.
        /// </summary>
        public SimComponent Component
        {
            get { return component; }
            set
            {
                if (component != value)
                {
                    if (value != null && (!value.Slots.Any(t => t.Target == slot.SlotBase.Target)))
                        throw new ArgumentException("Slot does not match component.CurrentSlot");

                    //Check access
                    SimUser cu = null;
                    if (this.Parent != null && this.Parent.Factory != null)
                    {
                        cu = this.Parent.Factory.ProjectData.UsersManager.CurrentUser;
                        if (this.Parent.Factory.EnableAccessChecking && cu != null)
                        {
                            if (!this.Parent.HasAccess(cu, SimComponentAccessPrivilege.Write))
                                throw new AccessDeniedException("Current user does not have access to the parent component");
                            if (component != null && !component.HasSubtreeAccess(cu, SimComponentAccessPrivilege.Write))
                                throw new AccessDeniedException("Current user does not have access to the old child component or to one of it's children");
                        }
                    }

                    if (component != null)
                    {
                        component.RecordWriteAccess();

                        if (component.Factory != null && Parent != null && Parent.Factory != null)
                            RemoveFromFactory(Parent.Factory, component);
                    }

                    component = value;

                    if (component != null)
                    {
                        if (cu != null)
                            component.ForceRecordWriteAccess(cu);

                        if (Parent != null && Parent.Factory != null)
                            AddToFactory(Parent.Factory, component);
                    }

                    this.Parent?.RecordWriteAccess();
                    this.Parent?.Factory?.NotifyChanged();

                    NotifyPropertyChanged(nameof(Component));
                }
            }
        }
        private SimComponent component;

        /// <summary>
        /// The slot for the entry
        /// </summary>
        public SimSlot Slot
        {
            get { return slot; }
            set
            {
                if (slot != value)
                {
                    if (slot.SlotBase != null)
                        slot.SlotBase.RemoveDeleteAction();

                    var oldSlot = slot;
                    slot = value;

                    if (slot.SlotBase != null)
                        slot.SlotBase.SetDeleteAction(SlotBaseTaxonomyEntryDeleted);

                    if (this.Component != null && !this.Component.Slots.Any(t => t.TaxonomyEntryId == this.slot.SlotBase.TaxonomyEntryId))
                    {
                        this.Component.Slots.Add(new SimTaxonomyEntryReference(this.slot.SlotBase));
                        if (oldSlot.SlotBase != null)
                        {
                            // remove old slot cause it was replaced
                            Component.Slots.Remove(oldSlot.SlotBase);
                        }
                    }


                    NotifyPropertyChanged(nameof(Slot));
                }
            }
        }
        private SimSlot slot;

        /// <summary>
        /// The parent component. Set automatically when the entry is added to a <see cref="SimComponent.Components"/> collection.
        /// When the entry is not included in a parent component, this property will return null.
        /// </summary>
        public SimComponent Parent
        {
            get { return parent; }
            internal set
            {
                if (parent != value)
                {
                    var oldParent = parent;
                    parent = value;

                    if (Component != null)
                    {
                        if (parent != null) //Add to a parent
                        {
                            //Id handling
                            if (parent.Factory != null) //When empty entry, nothing to do, same when parent is not part of a factory
                                AddToFactory(parent.Factory, Component);
                            Component.ParentContainer = this;
                        }
                        else //Remove from parent
                        {
                            if (oldParent.Factory != null)
                                RemoveFromFactory(oldParent.Factory, Component);

                            Component.ParentContainer = null;
                        }
                    }

                    NotifyPropertyChanged(nameof(Parent));
                }
            }
        }
        private SimComponent parent;

        #endregion

        #region Events

        /// <inheritdoc />
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(string prop)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        #endregion

        /// <summary>
        /// Initializes a new instance of the ChildComponentEntry class
        /// </summary>
        /// <param name="slot">The slot for this entry</param>
        /// <param name="component">The component stored in this entry. May be null.
        /// When not null, one element of <see cref="SimComponent.Slots"/> has to match the slot parameters
        /// base</param>
        public SimChildComponentEntry(SimSlot slot, SimComponent component)
        {
            if (component != null && (!component.Slots.Any(t => t.Target == slot.SlotBase.Target)))
                throw new ArgumentException("Slot does not match component.CurrentSlot");

            this.Slot = slot;
            this.Component = component;
        }


        /// <summary>
        /// Initializes a new, empty instance of the ChildComponentEntry class
        /// </summary>
        /// <param name="slot">The slot for this entry</param>
        public SimChildComponentEntry(SimSlot slot) : this(slot, null) { }

        /// <summary>
        /// Restores the references to other objects in the project after loading. This method needs to be called when all objects have been create and registered
        /// in their appropriate factories. Especially, this method may only be called when ALL components have been created.
        /// Restores referenced components, asset references, objects referenced by instances and calculator mappings.
        /// The method calls itself recursively for all child components.
        /// </summary>
        /// <param name="networkElements">A list of all network elements. This parameter will be obsolete when network elements are registered in the IdGenerator</param>
        /// <param name="assetManager">The asset manager in this project</param>
        public void RestoreReferences(Dictionary<SimObjectId, SimFlowNetworkElement> networkElements, AssetManager assetManager)
        {
            if (this.Parent == null)
                throw new InvalidOperationException("May only be called on child component entries that do not have a parent");


            if (!(Slot.SlotBase is SimPlaceholderTaxonomyEntryReference))
            {
                var entry = Parent.Factory.ProjectData.IdGenerator.GetById<SimTaxonomyEntry>(Slot.SlotBase.TaxonomyEntryId);
                if (entry == null)
                    throw new TaxonomyEntryNotFoundException(String.Format("Slot taxonomy entry with id {0} of component {1} could not be found", Slot.SlotBase.TaxonomyEntryId, component.ToString()));
                Slot = new SimSlot(new SimTaxonomyEntryReference(entry), Slot.SlotExtension);
            }
            Component?.RestoreReferences(networkElements, assetManager);
        }


        internal void NotifyFactoryChanged(SimComponentCollection newValue, SimComponentCollection oldValue)
        {
            if (Parent != null && Component != null)
            {
                if (oldValue != null)
                    RemoveFromFactory(oldValue, Component, true, false);

                //Id handling
                if (newValue != null)
                    AddToFactory(newValue, Component);

            }
        }


        internal void OnRemoveWithoutDelete()
        {
            if (Component != null)
            {
                this.parent = null;
                NotifyPropertyChanged(nameof(Parent));

                if (Component.Factory != null)
                    RemoveFromFactory(Component.Factory, Component, false);
            }
        }

        internal void RemoveComponentWithoutDelete()
        {
            this.component.ParentContainer = null;
            this.component = null;
            NotifyPropertyChanged(nameof(Component));
        }

        private void RemoveFromFactory(SimComponentCollection factory, SimComponent component, bool deleteComponent = true, bool recordWrite = true)
        {
            if (deleteComponent)
            {
                factory.ProjectData.ComponentGeometryExchange.OnComponentRemoved(component);
                factory.ProjectData.DataMappingTools.OnComponentRemoved(component);
                factory.ProjectData.IdGenerator.Remove(component);
                component.OnIsBeingDeleted();
            }

            if (recordWrite)
                component.RecordWriteAccess();

            if (deleteComponent)
            {
                component.Id = new SimId(component.Id.GlobalId, component.Id.LocalId);
                component.Factory = null;
                component.ParentContainer = null;
            }
        }

        private void AddToFactory(SimComponentCollection factory, SimComponent component)
        {
            if (component.Factory == null) //This is a component that is currently not part of the system
            {
                if (component.Id != SimId.Empty) //Used pre-stored id (only possible during loading)
                {
                    if (factory.IsLoading)
                    {
                        component.Id = new SimId(factory.CalledFromLocation, component.Id.LocalId);
                        factory.ProjectData.IdGenerator.Reserve(component, component.Id);
                    }
                    else
                        throw new NotSupportedException("Existing Ids may only be used during a loading operation");
                }
                else
                    component.Id = factory.ProjectData.IdGenerator.NextId(component, factory.CalledFromLocation);

                component.Factory = factory;
                factory.ProjectData.ComponentGeometryExchange.OnComponentAdded(component);
            }
            else //This component is moved from somewhere else
            {
                if (component.Factory != factory)
                    throw new ArgumentException("Child components must be part of the same factory as the parent");

                if (component.ParentContainer == null) // Moved from top level
                {
                    component.Factory.RemoveWithoutDelete(component);
                }
                else if (component.ParentContainer != this) //Moved from another parent 
                {
                    component.ParentContainer.RemoveComponentWithoutDelete();
                }
            }

            component.ParentContainer = this;

            var cu = factory.ProjectData.UsersManager.CurrentUser;
            if (cu != null)
                component.ForceRecordWriteAccess(cu);
        }

        private void SlotBaseTaxonomyEntryDeleted(SimTaxonomyEntry caller)
        {
            if (Parent != null && parent.Factory != null && Slot.SlotBase.Target == caller) // could have been changed by component taxonomy handling before
            {
                var candidate = Component?.Slots.FirstOrDefault(x => x.Target != Slot.SlotBase.Target)?.Target;
                var newSlot = candidate ?? Parent.GetDefaultSlotTaxonomyEntry(SimDefaultSlotKeys.Undefined);
                Slot = new SimSlot(new SimTaxonomyEntryReference(newSlot), Slot.SlotExtension);
            }
        }
    }
}
