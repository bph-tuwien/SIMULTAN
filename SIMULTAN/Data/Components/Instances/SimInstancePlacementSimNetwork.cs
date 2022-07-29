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
        public SimInstancePlacementSimNetwork(IElementWithComponent networkElement)
        {
            if (networkElement == null)
                throw new ArgumentNullException(nameof(networkElement));

            this.NetworkElement = networkElement;
        }

        internal SimInstancePlacementSimNetwork(SimId loadingElementId)
        {
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
            if (this.loadingElementId != null && this.loadingElementId != SimId.Empty)
            {
                var element = this.Instance.Component.Factory.ProjectData.IdGenerator.GetById<SimObjectNew>(this.loadingElementId)
                    as IElementWithComponent;
                this.loadingElementId = SimId.Empty;

                if (element != null)
                {
                    this.NetworkElement = element;
                    return true;
                }
                else
                    return false;
            }

            return true;
        }
    }
}
