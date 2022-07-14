using SIMULTAN.Data.Components;
using System.ComponentModel;

namespace SIMULTAN.Data.SimNetworks
{
    /// <summary>
    /// Interface for elements in the SimNetwork which can contain SimComponents
    /// </summary>
    public interface IElementWithComponent : INotifyPropertyChanged
    {
        /// <summary>
        /// The component instance
        /// </summary>
        SimComponentInstance ComponentInstance { get; set; }

        /// <summary>
        /// The Id
        /// </summary>
        SimId Id { get; }
    }
}
