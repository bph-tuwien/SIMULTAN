using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SIMULTAN.Data.SimMath;

namespace SIMULTAN.Data.ValueMappings
{
    /// <summary>
    /// A colormap that uses the color of the next smaller color marker
    /// </summary>
    public class SimThresholdColorMap : SimColorMap
    {
        /// <summary>
        /// A list of color marker in this ColorMap
        /// </summary>
        public SimColorMarkerCollection ColorMarkers { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SimThresholdColorMap"/> class
        /// </summary>
        public SimThresholdColorMap()
        {
            this.ColorMarkers = new SimColorMarkerCollection(this);
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="SimThresholdColorMap"/> class
        /// </summary>
        /// <param name="marker">Initial set of markers. The must need to be sorted by value</param>
        public SimThresholdColorMap(IEnumerable<SimColorMarker> marker)
        {
            this.ColorMarkers = new SimColorMarkerCollection(this, marker);
        }

        /// <inheritdoc />
        public override SimColor Map(double value)
        {
            for (int i = 0; i < ColorMarkers.Count - 1; i++)
            {
                if (value >= ColorMarkers[i].Value && value < ColorMarkers[i + 1].Value)
                    return ColorMarkers[i].Color;
            }

            //Check if the value is exactly at the end of the last marker
            if (ColorMarkers.Count > 0 && value == ColorMarkers[ColorMarkers.Count - 1].Value)
                return ColorMarkers[ColorMarkers.Count - 1].Color;

            return DefaultColorMappingColors.OutOfRangeColor;
        }
    }
}
