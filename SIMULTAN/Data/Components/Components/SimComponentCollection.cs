using SIMULTAN.Data.FlowNetworks;
using SIMULTAN.Data.SimNetworks;
using SIMULTAN.Data.Users;
using SIMULTAN.Exceptions;
using SIMULTAN.Projects;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SIMULTAN.Data.Components
{
    /// <summary>
    /// Manages a number of components. The collection itself contains root components.
    /// </summary>
    public partial class SimComponentCollection : SimManagedCollection<SimComponent>
    {
        #region Properties

        /// <summary>
        /// Enables/Disables whether parameter values are propagated to references.
        /// Forces a reevaluation of all referncing parameters when changing from False to True
        /// </summary>
        public bool EnableReferencePropagation 
        {
            get { return enableReferencePropagation; }
            set
            {
                if (enableReferencePropagation != value)
                {
                    enableReferencePropagation = value;
                    if (enableReferencePropagation)
                        InvalidateReferenceParameters();
                }
            }
        }
        private bool enableReferencePropagation = true;

        /// <summary>
        /// Enables/Disables access management checkings.
        /// Checking is enabled when the checking counter is 0 and at least one user exists
        /// </summary>
        internal bool EnableAccessChecking { get { return EnableAccessCheckingCounter == 0 && ProjectData.UsersManager.Users.Count > 0; } }
        private int EnableAccessCheckingCounter = 0;

        #endregion

        #region ObservableCollection Overrides

        /// <inheritdoc />
        protected override void InsertItem(int index, SimComponent item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            //Check if current user is not guest
            var cu = this.ProjectData.UsersManager.CurrentUser;
            if (this.EnableAccessChecking && cu != null)
            {
                if (cu.Role == SimUserRole.GUEST)
                    throw new AccessDeniedException("Guests may not add root components");
            }

            this.SetValues(item);
            if (cu != null)
                item.ForceRecordWriteAccess(cu);

            base.InsertItem(index, item);
            this.NotifyChanged();

            ProjectData.ComponentGeometryExchange.OnComponentAdded(item);
        }
        /// <inheritdoc />
        protected override void RemoveItem(int index)
        {
            var oldItem = this[index];

            var cu = this.ProjectData.UsersManager.CurrentUser;
            if (cu == null)
                throw new AccessDeniedException("Please authenticate a user before performing operations");
            if (this.EnableAccessChecking && cu != null && !oldItem.HasSubtreeAccess(cu, SimComponentAccessPrivilege.Write))
                throw new AccessDeniedException("User does not have write access on the removed component or on one of the subcomponents");

            oldItem.RecordWriteAccess();

            ProjectData.ComponentGeometryExchange.OnComponentRemoved(oldItem);

            UnsetValues(oldItem, true);
            base.RemoveItem(index);
            this.NotifyChanged();
        }
        /// <inheritdoc />
        protected override void ClearItems()
        {
            //Check access
            if (this.EnableAccessChecking)
            {
                var cu = this.ProjectData.UsersManager.CurrentUser;
                if (cu != null)
                {
                    foreach (var comp in this)
                    {
                        if (!comp.HasSubtreeAccess(cu, SimComponentAccessPrivilege.Write))
                            throw new AccessDeniedException("User does not have write access on the removed component");
                    }
                }
                else
                    throw new AccessDeniedException("Please authenticate a user before performing operations");
            }

            foreach (var item in this)
            {
                item.RecordWriteAccess();

                ProjectData.ComponentGeometryExchange.OnComponentRemoved(item);

                UnsetValues(item, true);
            }
            base.ClearItems();
            this.NotifyChanged();
        }
        /// <inheritdoc />
        protected override void SetItem(int index, SimComponent item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            var oldItem = this[index];

            var cu = this.ProjectData.UsersManager.CurrentUser;
            if (cu == null)
                throw new AccessDeniedException("Please authenticate a user before performing operations");
            else if (this.EnableAccessChecking && cu != null)
            {
                if (!oldItem.HasSubtreeAccess(cu, SimComponentAccessPrivilege.Write))
                    throw new AccessDeniedException("User does not have write access on the removed component or on one of the subcomponents");
                if (cu.Role == SimUserRole.GUEST)
                    throw new AccessDeniedException("Guests may not add root components");
            }

            oldItem.RecordWriteAccess();
            ProjectData.ComponentGeometryExchange.OnComponentRemoved(oldItem);
            UnsetValues(oldItem, true);

            if (cu != null)
                item.ForceRecordWriteAccess(cu);
            this.SetValues(item);

            base.SetItem(index, item);
            this.NotifyChanged();

            ProjectData.ComponentGeometryExchange.OnComponentAdded(item);
        }

        private void SetValues(SimComponent item)
        {
            if (item.Factory != null && item.Factory != this)
                throw new ArgumentException("item already belongs to a factory");

            if (item.Factory == null) // New component
            {
                if (item.Id != SimId.Empty) //Used pre-stored id (only possible during loading)
                {
                    if (this.IsLoading)
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
                if (item.ParentContainer != null) //Remove from old parent
                {
                    item.ParentContainer.RemoveComponentWithoutDelete();
                }
                else //Is already root -> exception
                    throw new ArgumentException("Component is already part of the collection");

            }
        }

        private void UnsetValues(SimComponent item, bool deleteComponent)
        {
            if (deleteComponent)
                item.OnIsBeingDeleted();

            this.ProjectData.IdGenerator.Remove(item);
            item.Id = new SimId(item.Id.GlobalId, item.Id.LocalId);
            item.Factory = null;
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
        /// <param name="component">The component to remove</param>
        /// <returns>Always true</returns>
        internal bool RemoveWithoutDelete(SimComponent component)
        {
            var index = this.IndexOf(component);
            if (index < 0)
                return false;

            component.RecordWriteAccess();
            base.RemoveItem(index);
            this.NotifyChanged();

            return true;
        }

        #endregion

        #region Loading

        /// <summary>
        /// When set to True, it is possible to add components with existing ids to the collection.
        /// </summary>
        internal bool IsLoading { get; private set; } = false;

        /// <summary>
        /// Sets the factory in loading mode which allows to add MultiValues with a pre-defined Id
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

        /// <summary>
        /// Initializes a new instance of the SimComponentCollection class
        /// </summary>
        /// <param name="owner">The project data to which this collection belongs</param>
        public SimComponentCollection(ProjectData owner) : base(owner) { }


        #region Asset & References

        /// <summary>
        /// Restores all types of references. Needs to be called when all managers are filled after loading a project
        /// </summary>
        /// <param name="networkElements">A list of all network elements ordered by id. This is necessary since network elements are not part of the new system</param>
        public void RestoreReferences(Dictionary<SimObjectId, SimFlowNetworkElement> networkElements)
        {
            foreach (var comp in this)
            {
                comp?.RestoreReferences(networkElements, this.ProjectData.AssetManager);
            }
        }





        /// <summary>
        /// Unbinds all assets from all components stored in this collection
        /// </summary>
        public void RemoveAllAssets()
        {
            foreach (var component in this)
                component.RemoveAllAssets();
        }

        /// <summary>
        /// Notifies the collection the a geometry file has been deleted.
        /// Removes references to the geometry from all components recursively
        /// </summary>
        /// <param name="resourceId">The id of the deleted resource</param>
        public void OnGeometryResourceDeleted(int resourceId)
        {
            foreach (SimComponent c in this)
            {
                this.OnGeometryResourceDeleted(c, resourceId);
            }
        }

        private void OnGeometryResourceDeleted(SimComponent component, int resourceId)
        {
            for (int insti = 0; insti < component.Instances.Count; ++insti)
            {
                var inst = component.Instances[insti];
                for (int i = 0; i < inst.Placements.Count; ++i)
                {
                    if (inst.Placements[i] is SimInstancePlacementGeometry gp && gp.FileId == resourceId)
                    {
                        inst.Placements.RemoveAt(i);
                        i--;
                    }
                }

                if (inst.Placements.Count == 0)
                {
                    component.Instances.RemoveAt(insti);
                    insti--;
                }
            }

            foreach (var child in component.Components.Where(x => x.Component != null))
            {
                this.OnGeometryResourceDeleted(child.Component, resourceId);
            }
        }

        private void InvalidateReferenceParameters()
        {
            foreach (var component in this)
                InvalidateReferenceParameters(component);
        }
        private void InvalidateReferenceParameters(SimComponent component)
        {
            foreach (var param in component.Parameters)
            {
                var target = param.GetReferencedParameter();
                if (target != null && target != param) // which means it's referencing
                    ComponentParameters.PropagateParameterValueChange(param, target);
            }

            foreach (var child in component.Components)
                if (child.Component != null)
                    InvalidateReferenceParameters(child.Component);
        }

        #endregion

        #region Merge

        /// <summary>
        /// Merges a list of components from another manager into this manager.
        /// The imported components will get new ids assigned. References are restored correctly
        /// </summary>
        /// <param name="source">The source collection</param>
        public void Merge(IEnumerable<SimComponent> source)
        {
            this.ResetIds(source);

            // 3. add to the record
            foreach (SimComponent c in source)
            {
                this.Add(c);
            }
        }

        private void ResetIds(IEnumerable<SimComponent> components)
        {
            foreach (var component in components.Where(x => x != null))
            {
                component.Id = SimId.Empty;
                component.Factory = null;

                foreach (var param in component.Parameters)
                    param.Id = SimId.Empty;
                foreach (var calc in component.Calculations)
                    calc.Id = SimId.Empty;
                foreach (var inst in component.Instances)
                    inst.Id = SimId.Empty;

                this.ResetIds(component.Components.Select(x => x.Component));
            }
        }

        #endregion
    }
}
