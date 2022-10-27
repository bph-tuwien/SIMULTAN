using System.Text;
using System.Windows;
using static SIMULTAN.Data.SimNetworks.SimNetworkPort;

namespace SIMULTAN.Data.SimNetworks
{
    /// <summary>
    /// A Base element for the elements constructing a network
    /// </summary>
    public abstract partial class BaseSimNetworkElement : SimNamedObject<ISimManagedCollection>
    {
        /// <summary>
        /// Representing the parent network.
        /// </summary>
        public SimNetwork ParentNetwork { get; internal set; }
        /// <summary>
        /// Representing incoming ports 
        /// </summary>
        public SimNetworkPortCollection Ports { get; internal set; }
        /// <summary>
        /// Position of the network element on the network editor canvas
        /// </summary>
        public Point Position
        {
            get { return this.position; }
            set
            {
                this.position = value;
                this.NotifyPropertyChanged(nameof(this.Position));
            }
        }
        private Point position;


        /// <summary>
        /// Handler for the <see cref="IsBeingDeleted"/> event.
        /// </summary>
        /// <param name="sender">The object which invoked the command</param>
        public delegate void IsBeingDeletedEventHandler(object sender);
        /// <summary>
        /// Emitted just before the instance is being deleted.
        /// </summary>
        public event IsBeingDeletedEventHandler IsBeingDeleted;
        /// <summary>
        /// Invokes the <see cref="IsBeingDeleted"/> event.
        /// </summary>
        public void OnIsBeingDeleted()
        {
            this.IsBeingDeleted?.Invoke(this);
        }


        /// <summary>
        /// Handler for the <see cref="IsDeleted"/> event.
        /// </summary>
        /// <param name="sender">The object which invoked the command</param>
        public delegate void IsDeletedEventHandler(object sender);
        /// <summary>
        /// Emitted just after the instance is deleted.
        /// </summary>
        public event IsBeingDeletedEventHandler IsDeleted;
        /// <summary>
        /// Invokes the <see cref="IsDeleted"/> event.
        /// </summary>
        public void OnIsDeleted()
        {
            this.IsDeleted?.Invoke(this);
        }

        /// <inheritdoc />
        protected override void OnFactoryChanged(ISimManagedCollection newFactory, ISimManagedCollection oldFactory)
        {
            this.Ports.NotifyFactoryChanged(newFactory, oldFactory);

            base.OnFactoryChanged(newFactory, oldFactory);
        }

        internal abstract void RestoreReferences();
    }
}
