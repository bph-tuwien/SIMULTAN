using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace SIMULTAN.Data.Components
{
    /// <summary>
    /// Class that encapsulates a Component and listens to changes in the collection that is containing the component.
    /// Automatically removes itself from the UserComponentList collection if it got removed from the parent component collection.
    /// </summary>
    internal class SimUserComponentRef
    {
        /// <summary>
        /// The Component of the reference object
        /// </summary>
        public SimComponent Component { get; private set; }

        private ObservableCollection<SimUserComponentRef> components;

        /// <summary>
        /// Constructs an new UserComponentRef for the given component and collection.
        /// </summary>
        /// <param name="component">The component to reference.</param>
        /// <param name="components">The collection that the reference will be part of so it can remove itself from it.</param>
        public SimUserComponentRef(SimComponent component, ObservableCollection<SimUserComponentRef> components)
        {
            Component = component;
            this.components = components;
        }

        private void Component_IsBeingDeleted(object sender)
        {
            DetachEvents();
            components.Remove(this);
        }

        internal void AttachEvents()
        {
            Component.IsBeingDeleted += Component_IsBeingDeleted;
        }
        internal void DetachEvents()
        {
            Component.IsBeingDeleted -= Component_IsBeingDeleted;
        }
    }
}
