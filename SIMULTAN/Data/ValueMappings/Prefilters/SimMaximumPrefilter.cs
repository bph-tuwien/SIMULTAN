using MathNet.Numerics.Statistics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.ValueMappings
{
    /// <summary>
    /// Prefilter which returns the maximum of all values
    /// </summary>
    public class SimMaximumPrefilter : SimPrefilter
    {
        /// <inheritdoc />
        public override IEnumerable<double> Filter(IEnumerable<double> values, int timelineCurrentIndex)
        {
            if (values == null)
                throw new ArgumentNullException(nameof(values));
            if (!values.Any())
                yield return double.NaN;
            else
                yield return values.Max();
        }
    }
}
