using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace SIMULTAN.Data.SitePlanner
{
    /// <summary>
    /// Color map parameters with an ordered list of ColorMapMarkers
    /// Used for MultiThreshold- and MulitLinearGradientColorMap
    /// </summary>
    public class MarkerColorMapParameters : ValueToColorMapParameters
    {
        /// <summary>
        /// Ordered list of color map markers, minimum and maximum are at first and last position respectively
        /// </summary>
        public ObservableCollection<ColorMapMarker> Markers { get; private set; }

        /// <summary>
        /// Initializes a new instance of this class
        /// </summary>
        public MarkerColorMapParameters()
        {
            Markers = new ObservableCollection<ColorMapMarker> { new ColorMapMarker(0.0, Colors.White), new ColorMapMarker(100.0, Colors.White) };

            Markers.CollectionChanged += Markers_CollectionChanged;
            foreach (var marker in Markers)
                marker.PropertyChanged += Marker_PropertyChanged;
        }

        private void Markers_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)
            {
                foreach (var m in e.NewItems)
                    ((ColorMapMarker)m).PropertyChanged += Marker_PropertyChanged;
            }
            else if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
            {
                foreach (var m in e.OldItems)
                    ((ColorMapMarker)m).PropertyChanged -= Marker_PropertyChanged;

            }
            else if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Replace)
            {
                foreach (var m in e.OldItems)
                    ((ColorMapMarker)m).PropertyChanged -= Marker_PropertyChanged;

                foreach (var m in e.NewItems)
                    ((ColorMapMarker)m).PropertyChanged += Marker_PropertyChanged;
            }
            NotifyColorMapParametersChanged();
        }

        private void Marker_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            NotifyColorMapParametersChanged();
        }

        /// <inheritdoc />
        public override void Deserialize(string obj)
        {
            string[] markers = obj.Split(';');
            this.Markers.Clear();
            ColorConverter colorConverter = new ColorConverter();
            foreach (var m in markers)
            {
                var parameters = m.Split('|');
                var val = double.Parse(parameters[0], System.Globalization.CultureInfo.InvariantCulture);
                var col = (Color)colorConverter.ConvertFromInvariantString(parameters[1]);
                this.Markers.Add(new ColorMapMarker(val, col));
            }
        }

        /// <inheritdoc />
        public override string Serialize()
        {
            ColorConverter colorConverter = new ColorConverter();

            var markersStr = Markers.Select(x => string.Format("{0}|{1}", x.Value.ToString(System.Globalization.CultureInfo.InvariantCulture), colorConverter.ConvertToInvariantString(x.Color))).ToList();
            return string.Join(";", markersStr);
        }
    }
}
