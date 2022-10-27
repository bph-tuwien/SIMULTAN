using MathNet.Numerics.Statistics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.ValueMappings
{
    /// <summary>
    /// Prefilter which returns the minimum of all values
    /// </summary>
    public class SimMinimumPrefilter : SimPrefilter
    {
        /// <inheritdoc />
        public override IEnumerable<double> Filter(IEnumerable<double> values, int timelineCurrentIndex)
        {
            if (values == null)
                throw new ArgumentNullException(nameof(values));
            if (!values.Any())
                yield return double.NaN;
            else
                yield return values.Min();
        }
    }
}
