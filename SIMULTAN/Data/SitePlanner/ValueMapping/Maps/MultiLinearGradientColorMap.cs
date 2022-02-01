using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace SIMULTAN.Data.SitePlanner
{
    /// <summary>
    /// Color map using linear gradient between adjacent markers
    /// </summary>
    public class MultiLinearGradientColorMap : BaseColorMap<MarkerColorMapParameters>
    {
        /// <summary>
        /// Initializes a new instance of this class
        /// </summary>
        /// <param name="parameters">The parameters</param>
        public MultiLinearGradientColorMap(MarkerColorMapParameters parameters)
            : base(parameters)
        {
        }

        /// <inheritdoc />
        public override Color Map(IEnumerable<double> values)
        {
            if (values.Count() == 0)
                return BaseColorMapColors.OutOfRangeColor;

            var val = values.First();
            var markers = DerivedParameters.Markers;

            for (int i = 0; i < markers.Count - 1; i++)
            {
                if (val >= markers[i].Value && val < markers[i + 1].Value)
                    return Lerp(markers[i], markers[i + 1], val);
            }

            return BaseColorMapColors.OutOfRangeColor;
        }

        /// <summary>
        /// Linearly interpolates between two color markers. Marker b is assumed to have a value greater than the value of marker a.
        /// </summary>
        /// <param name="a">Marker a</param>
        /// <param name="b">Marker b</param>
        /// <param name="val">Value between values of markers a and b</param>
        /// <returns>Interpolated color</returns>
        public static Color Lerp(ColorMapMarker a, ColorMapMarker b, double val)
        {
            if (double.IsInfinity(a.Value)) return b.Color;
            if (double.IsInfinity(b.Value)) return a.Color;
            var t = (val - a.Value) / (b.Value - a.Value);
            var cr = a.Color.R * (1.0 - t) + b.Color.R * t;
            var cg = a.Color.G * (1.0 - t) + b.Color.G * t;
            var cb = a.Color.B * (1.0 - t) + b.Color.B * t;
            var ca = a.Color.A * (1.0 - t) + b.Color.A * t;
            return Color.FromArgb((byte)ca, (byte)cr, (byte)cg, (byte)cb);
        }
    }
}
