using System.Linq;
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
        /// Tells whether any of the ports of the block is connected 
        /// </summary>
        public bool IsConnected
        {
            get
            {
                return this.Ports.Any(p => p.IsConnected);
            }
        }

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


        #region PROPERTIES: for geometric representation

        private GeometricReference geom_representation_ref;
        /// <summary>
        /// Saves the reference to the *representing* geometry.
        /// </summary>
        public GeometricReference RepresentationReference
        {
            get { return this.geom_representation_ref; }
            set
            {
                if (this.geom_representation_ref != value)
                {
                    this.geom_representation_ref = value;
                    this.NotifyPropertyChanged(nameof(RepresentationReference));
                }
            }
        }

        #endregion


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
