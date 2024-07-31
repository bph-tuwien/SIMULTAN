using SIMULTAN.Data.FlowNetworks;
using SIMULTAN.Data.SimNetworks;
using System;
using System.Collections.Generic;

namespace SIMULTAN.Data.Components
{
    /// <summary>
    /// Describes the usage of an instance by a SimNetwork element
    /// </summary>
    public class SimInstancePlacementSimNetwork : SimInstancePlacement
    {
        #region Properties

        private SimId loadingElementId;

        /// <summary>
        /// The network element in which the instance is placed
        /// </summary>
        public IElementWithComponent NetworkElement
        {
            get { return this.networkElement; }
            set
            {
                if (this.networkElement != value)
                {
                    if (this.networkElement != null && this.Instance != null && this.Instance.Component != null) //Remove from old
                        this.networkElement.ComponentInstance = null;

                    this.networkElement = value;

                    if (this.networkElement != null)
                    {
                        this.State &= ~SimInstancePlacementState.InstanceTargetMissing;

                        if (this.Instance != null && this.Instance.Component != null)
                            this.networkElement.ComponentInstance = this.Instance;
                    }
                    else
                        this.State |= SimInstancePlacementState.InstanceTargetMissing;

                    this.NotifyPropertyChanged(nameof(this.NetworkElement));
                    this.Instance?.OnInstanceStateChanged();
                }
            }
        }
        private IElementWithComponent networkElement = null;

        #endregion




        /// <summary>
        /// Initializes a new instance of the InstancePlacementSimNetwork class
        /// </summary>
        /// <param name="networkElement">The network element</param>
        /// <param name="instanceType">The instance type of this placement. Must be one of
        /// <see cref="SimInstanceType.SimNetworkBlock"/> or <see cref="SimInstanceType.InPort"/> or <see cref="SimInstanceType.OutPort"/></param>
        public SimInstancePlacementSimNetwork(IElementWithComponent networkElement, SimInstanceType instanceType)
            : base(instanceType)
        {
            if (networkElement == null)
                throw new ArgumentNullException(nameof(networkElement));
            if (instanceType != SimInstanceType.SimNetworkBlock && instanceType != SimInstanceType.InPort && instanceType != SimInstanceType.OutPort)
                throw new ArgumentException("Instance type not supported for this placement type");

            this.NetworkElement = networkElement;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SimInstancePlacementSimNetwork"/> class.
        /// May only be used during loading.
        /// </summary>
        /// <param name="loadingElementId">The id of the network element this placement references</param>
        /// <param name="instanceType">The instance type of this placement. Must be one of
        /// <see cref="SimInstanceType.SimNetworkBlock"/> or <see cref="SimInstanceType.InPort"/> or 
        /// <see cref="SimInstanceType.OutPort"/>. Also accepts <see cref="SimInstanceType.None"/>,
        /// but in that case the instance type must be set from by
        /// the <see cref="RestoreReferences(Dictionary{SimObjectId, SimFlowNetworkElement})"/> method.</param>
        internal SimInstancePlacementSimNetwork(SimId loadingElementId, SimInstanceType instanceType)
            : base(instanceType)
        {
            if (instanceType != SimInstanceType.SimNetworkBlock && instanceType != SimInstanceType.InPort && instanceType != SimInstanceType.OutPort &&
                instanceType != SimInstanceType.None) //When the InstanceType is set to None, it has to be set from the restore reference method
                throw new ArgumentException("Instance type not supported for this placement type");

            this.loadingElementId = loadingElementId;
        }


        /// <inheritdoc />
        public override void AddToTarget()
        {

            if (this.Instance != null && this.Instance.Component != null && this.NetworkElement != null)
            {
                this.NetworkElement.ComponentInstance = this.Instance;
            }
        }
        /// <inheritdoc />
        public override void RemoveFromTarget()
        {
            if (this.Instance != null && this.NetworkElement != null)
                this.NetworkElement.ComponentInstance = null;
        }

        internal override bool RestoreReferences(Dictionary<SimObjectId, SimFlowNetworkElement> networkElements)
        {
            if (this.loadingElementId != SimId.Empty)
            {
                var element = this.Instance.Component.Factory.ProjectData.IdGenerator.GetById<SimObjectNew>(this.loadingElementId)
                    as IElementWithComponent;
                this.loadingElementId = SimId.Empty;

                if (element != null)
                {
                    this.NetworkElement = element;

                    if (InstanceType == SimInstanceType.None)
                    {
                        switch (element)
                        {
                            case SimNetworkBlock _:
                                this.InstanceType = SimInstanceType.SimNetworkBlock;
                                break;
                            case SimNetworkPort p:
                                this.InstanceType = p.PortType == PortType.Input ? SimInstanceType.InPort : SimInstanceType.OutPort;
                                break;
                        }
                    }

                    return true;
                }
                else
                    return false;
            }

            return true;
        }
    }
}
