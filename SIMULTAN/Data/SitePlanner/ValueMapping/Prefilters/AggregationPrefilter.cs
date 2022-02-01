using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.SitePlanner
{
    /// <summary>
    /// Prefilter that performs an aggregation operation (max, min, avg)
    /// </summary>
    public abstract class AggregationPrefilter : BasePrefilter<EmptyPrefilterParameters>
    {
        /// <summary>
        /// Initializes a new instance of this class
        /// </summary>
        public AggregationPrefilter()
            : base(new EmptyPrefilterParameters())
        { }
    }

    /// <summary>
    /// Prefilter that returns the maximum value
    /// </summary>
    public class MaximumPrefilter : AggregationPrefilter
    {
        /// <summary>
        /// Initializes a new instance of this class
        /// </summary>
        public MaximumPrefilter() { }

        /// <inheritdoc />
        public override IEnumerable<double> Filter(IEnumerable<double> values)
        {
            return new List<double> { values.Max() };
        }
    }

    /// <summary>
    /// Prefilter that returns the minimum value
    /// </summary>
    public class MinimumPrefilter : AggregationPrefilter
    {
        /// <summary>
        /// Initializes a new instance of this class
        /// </summary>
        public MinimumPrefilter() { }

        /// <inheritdoc />
        public override IEnumerable<double> Filter(IEnumerable<double> values)
        {
            return new List<double> { values.Min() };
        }
    }

    /// <summary>
    /// Prefilter that returns the average value
    /// </summary>
    public class AveragePrefilter : AggregationPrefilter
    {
        /// <summary>
        /// Initializes a new instance of this class
        /// </summary>
        public AveragePrefilter() { }

        /// <inheritdoc />
        public override IEnumerable<double> Filter(IEnumerable<double> values)
        {
            return new List<double> { values.Average() };
        }
    }
}
