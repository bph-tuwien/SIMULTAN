using SIMULTAN.Data.FlowNetworks;
using System;
using System.Collections.Generic;

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

        #endregion

        /// <summary>
        /// Initializes a new instance of the SimInstancePlacementNetwork class
        /// </summary>
        /// <param name="networkElement">The network element</param>
        /// <param name="instanceType">The instance type of this placement. Must be one of
        /// <see cref="SimInstanceType.NetworkNode"/> or <see cref="SimInstanceType.NetworkEdge"/></param>
        public SimInstancePlacementNetwork(SimFlowNetworkElement networkElement, SimInstanceType instanceType)
            : base(instanceType)
        {
            if (networkElement == null)
                throw new ArgumentNullException(nameof(networkElement));
            if (instanceType != SimInstanceType.NetworkNode && instanceType != SimInstanceType.NetworkEdge)
                throw new ArgumentException("Instance type not supported for this placement type");

            this.NetworkElement = networkElement;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SimInstancePlacementNetwork"/> class.
        /// May only be used during loading.
        /// </summary>
        /// <param name="networkElementId">The id of the network element this placement references</param>
        /// <param name="instanceType">The instance type of this placement. Must be one of
        /// <see cref="SimInstanceType.NetworkNode"/> or <see cref="SimInstanceType.NetworkEdge"/>. Also accepts
        /// <see cref="SimInstanceType.None"/>, but in that case the instance type must be set from by
        /// the <see cref="RestoreReferences(Dictionary{SimObjectId, SimFlowNetworkElement})"/> method.</param>
        internal SimInstancePlacementNetwork(SimObjectId networkElementId, SimInstanceType instanceType)
            : base(instanceType)
        {
            if (instanceType != SimInstanceType.NetworkNode && instanceType != SimInstanceType.NetworkEdge &&
                instanceType != SimInstanceType.None) //When the InstanceType is set to None, it has to be set from the restore reference method
                throw new ArgumentException("Instance type not supported for this placement type");

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

                    if (this.InstanceType == SimInstanceType.None)
                    {
                        if (nwElement is SimFlowNetworkNode)
                            this.InstanceType = SimInstanceType.NetworkNode;
                        else if (nwElement is SimFlowNetworkEdge)
                            this.InstanceType = SimInstanceType.NetworkEdge;
                    }

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
