using SIMULTAN.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.ValueMappings
{
    /// <summary>
    /// Prefilter which returns the value at timeline index or 0 when the value is out of range
    /// </summary>
    public class SimDefaultPrefilter : SimPrefilter
    {
        /// <inheritdoc />
        public override IEnumerable<double> Filter(IEnumerable<double> values, int timelineCurrentIndex)
        {
            if (values == null)
                throw new ArgumentNullException(nameof(values));

            if (values.TryGetElementAt(timelineCurrentIndex, out var value))
                yield return value;
            else
                yield return 0;
        }
    }
}
