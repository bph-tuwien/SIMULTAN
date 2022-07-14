using SIMULTAN.Data.FlowNetworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace SIMULTAN.Data.Components
{
    /// <summary>
    /// Describes the usage of an instance by a network element
    /// </summary>
    public class SimInstancePlacementNetwork : SimInstancePlacement
    {
        /// <summary>
        /// The default scaling factor used to convert pixel coordinates into metric coordinates 
        /// </summary>
        public static double SCALE_PIXEL_TO_M { get { return 0.05; } }

        #region Properties

        private SimObjectId loadingNetworkElement;
        internal SimObjectId LoadingNetworkElement => loadingNetworkElement;

        /// <summary>
        /// The network element in which the instance is placed
        /// </summary>
        public SimFlowNetworkElement NetworkElement
        {
            get { return networkElement; }
            set
            {
                if (networkElement != value)
                {
                    if (networkElement != null && this.Instance != null && this.Instance.Component != null) //Remove from old
                        networkElement.Content = null;

                    networkElement = value;

                    if (networkElement != null)
                    {
                        this.State &= ~SimInstancePlacementState.InstanceTargetMissing;

                        if (this.Instance != null && this.Instance.Component != null)
                            networkElement.Content = this.Instance;
                    }
                    else
                        this.State |= SimInstancePlacementState.InstanceTargetMissing;

                    NotifyPropertyChanged(nameof(NetworkElement));
                    Instance?.OnInstanceStateChanged();
                }
            }
        }
        private SimFlowNetworkElement networkElement = null;

        /// <summary>
        /// Scaling factor to convert network element coordinates into meters for the <see cref="SimComponentInstance.InstancePath"/>
        /// </summary>
        public double PathScale { get; } = SimInstancePlacementNetwork.SCALE_PIXEL_TO_M;

        #endregion

        /// <summary>
        /// Initializes a new instance of the SimInstancePlacementNetwork class
        /// </summary>
        /// <param name="networkElement">The network element</param>
        public SimInstancePlacementNetwork(SimFlowNetworkElement networkElement)
        {
            if (networkElement == null)
                throw new ArgumentNullException(nameof(networkElement));

            this.NetworkElement = networkElement;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SimInstancePlacementNetwork"/> class.
        /// May only be used during loading.
        /// </summary>
        /// <param name="networkElementId">The id of the network element this placement references</param>
        public SimInstancePlacementNetwork(SimObjectId networkElementId)
        {
            this.loadingNetworkElement = networkElementId;
        }

        /// <inheritdoc />
        public override void AddToTarget()
        {
            if (this.Instance != null && this.Instance.Component != null && this.NetworkElement != null)
            {
                this.NetworkElement.Content = this.Instance;
            }
        }
        /// <inheritdoc />
        public override void RemoveFromTarget()
        {
            if (this.Instance != null && this.NetworkElement != null)
                this.NetworkElement.Content = null;
        }

        internal override bool RestoreReferences(Dictionary<SimObjectId, SimFlowNetworkElement> networkElements)
        {
            if (this.loadingNetworkElement != SimObjectId.Empty)
            {
                bool found = networkElements.TryGetValue(this.loadingNetworkElement, out var nwElement);
                if (found)
                {
                    this.NetworkElement = nwElement;
                    AddToTarget(); //Make sure that connection is restored
                }
                this.loadingNetworkElement = SimObjectId.Empty;
                return found;
            }
            else
                return true;
        }
    }
}
