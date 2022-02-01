using SIMULTAN.Data.FlowNetworks;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.Components
{
    /// <summary>
    /// Describes the usage of an instance. For example, a <see cref="SimInstancePlacementGeometry"/> specifies that
    /// an instance is attached/used by a geometric object. A <see cref="SimInstancePlacementNetwork"/> specifies that the instance
    /// is placed in a <see cref="SimFlowNetwork"/>.
    /// 
    /// This class is the base for all placements. Derive from it if you want to define a new type of usage for instances.
    /// Derived classes may override the <see cref="AddToTarget"/> and <see cref="RemoveFromTarget"/> methods to handle when
    /// the usage is added to an instance or removed from one.
    /// </summary>
    public abstract class SimInstancePlacement : INotifyPropertyChanged
    {
        /// <summary>
        /// Gets or sets the state in which this placement is in. 
        /// At the moment, this state does NOT automatically influence the <see cref="SimComponentInstance.State"/>
        /// </summary>
        public SimInstancePlacementState State
        {
            get { return state; }
            set
            {
                if (state != value)
                {
                    state = value;
                    NotifyPropertyChanged(nameof(State));
                    Instance?.OnInstanceStateChanged();
                }
            }
        }
        private SimInstancePlacementState state = SimInstancePlacementState.Valid;

        /// <summary>
        /// The instance to which this placement belongs.
        /// Automatically set when the placement is added to <see cref="SimComponentInstance.Placements"/>.
        /// </summary>
        public SimComponentInstance Instance
        {
            get { return instance; }
            internal set
            {
                if (instance != value)
                {
                    if (instance != null && instance.Component != null)
                        RemoveFromTarget();

                    this.instance = value;
                    NotifyPropertyChanged(nameof(Instance));

                    if (instance != null && instance.Component != null)
                        AddToTarget();
                }
            }
        }
        private SimComponentInstance instance = null;

        /// <inheritdoc />
        public event PropertyChangedEventHandler PropertyChanged;
        /// <summary>
        /// Invokes the <see cref="PropertyChanged"/> event
        /// </summary>
        /// <param name="prop">The name of the property</param>
        protected void NotifyPropertyChanged(string prop)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        /// <summary>
        /// Called when the placement is attached to an active component tree.
        /// Happens when:
        ///  - The placement is added to an instance which is already attached to a component
        ///  - An instance which contains this placement is added to a component
        ///  - When an instance is moved to another component
        ///  
        /// This method is never called twice without calling <see cref="RemoveFromTarget"/> in between.
        /// </summary>
        public abstract void AddToTarget();
        /// <summary>
        /// Called when the placement is detached to an active component tree.
        /// Happens when:
        ///  - The placement is removed from an instance which is attached to a component
        ///  - An instance which contains this placement is removed from a component
        ///  - When an instance is moved to another component
        ///  
        /// This method is never called twice without calling <see cref="AddToTarget"/> in between.
        /// </summary>
        public abstract void RemoveFromTarget();
    }
}
