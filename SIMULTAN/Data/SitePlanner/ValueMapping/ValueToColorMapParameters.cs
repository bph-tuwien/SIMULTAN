using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.SitePlanner
{
    /// <summary>
    /// EventHandler for the ColorMapParametersChanged event
    /// </summary>
    /// <param name="sender">The sender</param>
    public delegate void ColorMapParametersChangedEventHandler(object sender);

    /// <summary>
    /// Base class for value to color map parameters
    /// </summary>
    public abstract class ValueToColorMapParameters : INotifyPropertyChanged
    {
        /// <inheritdoc />
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Invoked when the color map parameters are changed or any parameter contained in them
        /// </summary>
        public event ColorMapParametersChangedEventHandler ColorMapParametersChanged;

        /// <summary>
        /// Serializes this object into a string representation
        /// </summary>
        /// <returns>String representation of the parameters of this object</returns>
        public abstract string Serialize();

        /// <summary>
        /// Deserealizes the given string and sets its according parameters
        /// </summary>
        /// <param name="obj">String representation of object</param>
        public abstract void Deserialize(string obj);

        /// <summary>
        /// Notifies the ColorMapParametersChanged event
        /// </summary>
        protected void NotifyColorMapParametersChanged()
        {
            ColorMapParametersChanged?.Invoke(this);
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
