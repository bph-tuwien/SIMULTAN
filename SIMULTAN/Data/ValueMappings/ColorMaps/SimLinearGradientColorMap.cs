using SIMULTAN.Data.SimMath;
using SIMULTAN.Data.SitePlanner;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.ValueMappings
{
    /// <summary>
    /// A colormap which uses a linear interpolation between <see cref="SimColorMarker"/>s to calculate the final color
    /// </summary>
    public class SimLinearGradientColorMap : SimColorMap
    {
        /// <summary>
        /// A list of color markers in this ColorMap
        /// </summary>
        public SimColorMarkerCollection ColorMarkers { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SimLinearGradientColorMap"/> class
        /// </summary>
        public SimLinearGradientColorMap()
        {
            this.ColorMarkers = new SimColorMarkerCollection(this);
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="SimLinearGradientColorMap"/> class
        /// </summary>
        /// <param name="marker">Initial set of markers. The must need to be sorted by value</param>
        public SimLinearGradientColorMap(IEnumerable<SimColorMarker> marker)
        {
            if (marker == null)
                throw new ArgumentNullException(nameof(marker));

            this.ColorMarkers = new SimColorMarkerCollection(this, marker);
        }

        /// <inheritdoc />
        public override SimColor Map(double value)
        {
            for (int i = 0; i < ColorMarkers.Count - 1; i++)
            {
                if (value >= ColorMarkers[i].Value && value <= ColorMarkers[i + 1].Value)
                    return Lerp(ColorMarkers[i], ColorMarkers[i + 1], value);
            }

            return DefaultColorMappingColors.OutOfRangeColor;
        }

        /// <summary>
        /// Linearly interpolates between two color markers. Marker b is assumed to have a value greater than the value of marker a.
        /// </summary>
        /// <param name="a">Marker a</param>
        /// <param name="b">Marker b</param>
        /// <param name="val">Value between values of markers a and b</param>
        /// <returns>Interpolated color</returns>
        public static SimColor Lerp(SimColorMarker a, SimColorMarker b, double val)
        {
            if (double.IsInfinity(a.Value)) return b.Color;
            if (double.IsInfinity(b.Value)) return a.Color;

            var t = (val - a.Value) / (b.Value - a.Value);
            var cr = a.Color.R * (1.0 - t) + b.Color.R * t;
            var cg = a.Color.G * (1.0 - t) + b.Color.G * t;
            var cb = a.Color.B * (1.0 - t) + b.Color.B * t;
            var ca = a.Color.A * (1.0 - t) + b.Color.A * t;

            return SimColor.FromArgb((byte)ca, (byte)cr, (byte)cg, (byte)cb);
        }
    }
}
