using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.SitePlanner
{
    /// <summary>
    /// EventHandler for ValuePrefilterParametersChanged event
    /// </summary>
    /// <param name="sender">The sender</param>
    public delegate void ValuePrefilterParametersChangedEventHandler(object sender);

    /// <summary>
    /// Base class for prefilter parameters
    /// </summary>
    public abstract class ValuePrefilterParameters : INotifyPropertyChanged
    {
        /// <inheritdoc />
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Invoked when the prefilter parameters are changed or any parameter contained in them
        /// </summary>
        public event ValuePrefilterParametersChangedEventHandler ValuePrefilterParametersChanged;

        /// <summary>
        /// Serializes this object into a string representation
        /// </summary>
        /// <returns>String representation of the parameters of this object</returns>
        // ToDo: remove
        public abstract string Serialize();

        /// <summary>
        /// Deserealizes the given string and sets its according parameters
        /// </summary>
        /// <param name="obj">String representation of object</param>
        // ToDo: remove
        public abstract void Deserialize(string obj);

        /// <summary>
        /// Notifies the ValuePrefilterParametersChanged event
        /// </summary>
        protected void NotifyValuePrefilterParametersChanged()
        {
            ValuePrefilterParametersChanged?.Invoke(this);
        }

        /// <summary>
        /// Notifies the PropertyChanged event
        /// </summary>
        /// <param name="prop">Name of changed property</param>
        protected void NotifyPropertyChanged(string prop)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }
    }
}
