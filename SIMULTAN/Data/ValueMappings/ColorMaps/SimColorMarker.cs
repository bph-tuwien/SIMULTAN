using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace SIMULTAN.Data.ValueMappings
{
    /// <summary>
    /// Stores a marker in <see cref="SimColorMap"/>. Each marker consists of a value (the position of the marker) and a Color.
    /// </summary>
    public class SimColorMarker : INotifyPropertyChanged
    {
        #region Properties

        /// <summary>
        /// The collection in which this marker is stored
        /// </summary>
        internal SimColorMarkerCollection Owner { get; set; }

        /// <summary>
        /// The color of the marker
        /// </summary>
        public Color Color 
        {
            get { return color; }
            set
            {
                if (this.color != value)
                {
                    this.color = value;
                    NotifyPropertyChanged(nameof(Color));
                    Owner?.Owner.NotifyMappingChanged();
                }
            }
        }
        private Color color;

        /// <summary>
        /// The value (position) of the marker
        /// </summary>
        public double Value
        {
            get { return value; }
            set
            {
                if (this.value != value)
                {
                    this.value = value;
                    NotifyPropertyChanged(nameof(Value));
                    Owner?.Owner.NotifyMappingChanged();
                }
            }
        }
        private double value;

        #endregion

        #region Events

        /// <inheritdoc />
        public event PropertyChangedEventHandler PropertyChanged;
        /// <summary>
        /// Invokes the <see cref="PropertyChanged"/> event
        /// </summary>
        /// <param name="property">Name of the modified property</param>
        protected void NotifyPropertyChanged(string property)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="SimColorMarker"/> class
        /// </summary>
        /// <param name="value">The value of the marker (position)</param>
        /// <param name="color">The color of the marker</param>
        public SimColorMarker(double value, Color color)
        {
            this.value = value;
            this.color = color;
        }
    }
}
