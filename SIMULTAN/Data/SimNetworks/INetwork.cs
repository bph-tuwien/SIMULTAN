namespace SIMULTAN.Data.SimNetworks
{
    /// <summary>
    /// Merely used for representing SimNetworks and SimFlownetworks in the UI
    /// </summary>
    public interface INetwork
    {
        /// <summary>
        /// Tells whether the network is a subnetwork
        /// </summary>
        bool HasParent { get; }
    }
}
