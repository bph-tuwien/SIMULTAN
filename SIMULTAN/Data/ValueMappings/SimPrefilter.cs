using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.ValueMappings
{
    /// <summary>
    /// Base class for all prefilters.
    /// Prefilters select a subset of the available values and/or applies statistical operations to it
    /// before the ColorMap is applied
    /// </summary>
    public abstract class SimPrefilter
    {
        /// <summary>
        /// The <see cref="SimValueMapping"/> this prefilter belongs to
        /// </summary>
        public SimValueMapping Owner { get; internal set; } = null;

        /// <summary>
        /// Filters the given list of values and only returns relevant values
        /// </summary>
        /// <param name="values">List of values</param>
        /// <param name="timelineCurrentIndex">The current index inside the timeline. This is an index into the values enumerable</param>
        /// <returns>Filtered list</returns>
        public abstract IEnumerable<double> Filter(IEnumerable<double> values, int timelineCurrentIndex);
    }
}
