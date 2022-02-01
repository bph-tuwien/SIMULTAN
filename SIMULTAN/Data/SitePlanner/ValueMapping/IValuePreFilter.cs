using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.SitePlanner
{
    /// <summary>
    /// Interface for value prefilters
    /// </summary>
    public interface IValuePrefilter
    {
        /// <summary>
        /// Filters the given list of values and only returns relevant values
        /// </summary>
        /// <param name="values">List of values</param>
        /// <returns>Filtered list</returns>
        IEnumerable<double> Filter(IEnumerable<double> values);

        /// <summary>
        /// Stores all the prefilter related parameters
        /// </summary>
        ValuePrefilterParameters Parameters { get; }
    }
}
