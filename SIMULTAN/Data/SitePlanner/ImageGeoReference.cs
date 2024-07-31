using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using SIMULTAN.Data.SimMath;

namespace SIMULTAN.Data.SitePlanner
{
    /// <summary>
    /// Stores a georeference in a map image
    /// </summary>
    public class ImageGeoReference : INotifyPropertyChanged
    {
        #region EVENTS: tracing

        /// <inheritdoc />
        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        /// <summary>
        /// Normalized image positions in the range [0, 1]
        /// </summary>
        public SimPoint ImagePosition
        {
            get => imagePosition;
            set
            {
                var oldValue = imagePosition;
                imagePosition = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ImagePosition)));
            }
        }
        private SimPoint imagePosition;

        /// <summary>
        /// GeoReference coordinates (WGS84)
        /// </summary>
        public SimPoint3D ReferencePoint
        {
            get => geoReference;
            set
            {
                var oldValue = geoReference;
                geoReference = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ReferencePoint)));
            }
        }
        private SimPoint3D geoReference;

        /// <summary>
        /// Initializes a new instance of the ImageGeoReference class
        /// </summary>
        /// <param name="imagePosition">Pixel position in the map image</param>
        /// <param name="geoReference">GeoReference coordinates (WGS84)</param>
        public ImageGeoReference(SimPoint imagePosition, SimPoint3D geoReference)
        {
            ImagePosition = imagePosition;
            ReferencePoint = geoReference;
        }

        /// <summary>
        /// Initializes a new instance of the ImageGeoReference class with default values
        /// </summary>
        public ImageGeoReference()
            : this(new SimPoint(0.0, 0.0), new SimPoint3D(0.0, 0.0, 0.0))
        {
        }
    }
}
