using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace SIMULTAN.Data.SitePlanner
{
    /// <summary>
    /// Represents a marker in a color map consisting of a value and a color
    /// </summary>
    public class ColorMapMarker : INotifyPropertyChanged
    {
        /// <summary>
        /// Value of the marker
        /// </summary>
        public double Value
        {
            get => val;
            set
            {
                if (value != val)
                {
                    val = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
                }
            }
        }
        private double val;

        /// <summary>
        /// Color of the marker
        /// </summary>
        public Color Color
        {
            get => color;
            set
            {
                if (value != color)
                {
                    color = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Color)));
                }
            }
        }
        private Color color;

        /// <inheritdoc />
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Initializes a new instance of this class
        /// </summary>
        /// <param name="value">Value</param>
        /// <param name="color">Color</param>
        public ColorMapMarker(double value, Color color)
        {
            this.val = value;
            this.color = color;
        }
    }
}
