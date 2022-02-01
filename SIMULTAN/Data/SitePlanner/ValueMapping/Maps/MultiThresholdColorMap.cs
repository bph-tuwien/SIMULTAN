using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace SIMULTAN.Data.SitePlanner
{
    /// <summary>
    /// Decides between multiple colors based on threshold operations on the given values, i.e. binning of values
    /// If multiple values are provided in Map(..), only the first one is used
    /// </summary>
    public class MultiThresholdColorMap : BaseColorMap<MarkerColorMapParameters>
    {
        /// <summary>
        /// Initializes a new instance of this class
        /// </summary>
        /// <param name="parameters">The parameters</param>
        public MultiThresholdColorMap(MarkerColorMapParameters parameters)
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
                    return markers[i].Color;
            }

            return BaseColorMapColors.OutOfRangeColor;
        }
    }
}
