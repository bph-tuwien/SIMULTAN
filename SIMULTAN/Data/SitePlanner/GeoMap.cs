using SIMULTAN.Data.Assets;
using SIMULTAN.Projects;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.SitePlanner
{
    /// <summary>
    /// Represents a georeferenced map in the SitePlanner
    /// </summary>
    public class GeoMap : INotifyPropertyChanged
    {
        #region EVENTS: tracing

        /// <inheritdoc />
        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        /// <summary>
        /// ResourceFileEntry of DXF file that contains this GeoMap
        /// </summary>
        public ResourceFileEntry GeoMapFile { get; private set; }

        /// <summary>
        /// Referenced resource containing the map image
        /// </summary>
        public ResourceReference MapImageRes
        {
            get { return mapImageRes; }
            set
            {
                var oldValue = mapImageRes;
                mapImageRes = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MapImageRes)));
            }
        }
        private ResourceReference mapImageRes;

        /// <summary>
        /// List of points which are georeferenced in the map
        /// </summary>
        public ObservableCollection<ImageGeoReference> GeoReferences { get; private set; }

        /// <summary>
        /// Initializes a new instance of the GeoMap class
        /// </summary>
        /// <param name="geoMapFile">ResourceFileEntry of GeoMap file (gmdxf)</param>
        public GeoMap(ResourceFileEntry geoMapFile)
        {
            this.GeoMapFile = geoMapFile;

            GeoReferences = new ObservableCollection<ImageGeoReference>();
            GeoReferences.CollectionChanged += GeoReferences_CollectionChanged;
        }

        private void GeoReferences_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            var old_item = (e.OldItems == null) ? null : e.OldItems[0] as ImageGeoReference;
            var new_item = (e.NewItems == null) ? null : e.NewItems[0] as ImageGeoReference;

            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                new_item.PropertyChanged += ImageGeoReference_PropertyChanged;
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                old_item.PropertyChanged -= ImageGeoReference_PropertyChanged;
            }
            else if (e.Action == NotifyCollectionChangedAction.Replace)
            {
                old_item.PropertyChanged -= ImageGeoReference_PropertyChanged;
                new_item.PropertyChanged += ImageGeoReference_PropertyChanged;
            }

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(GeoReferences)));
        }

        private void ImageGeoReference_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(e.PropertyName));
        }
    }
}
