using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.SitePlanner
{

    /// <summary>
    /// Base class for value prefilters
    /// </summary>
    public abstract class BasePrefilter<T> : IValuePrefilter where T : ValuePrefilterParameters
    {
        /// <inheritdoc />
        public ValuePrefilterParameters Parameters { get; private set; }

        /// <summary>
        /// Prefilter parameters with specialized type
        /// </summary>
        public T DerivedParameters => (T)Parameters;

        /// <summary>
        /// Initializes a new instance of this class
        /// </summary>
        /// <param name="parameters">ValuePrefilterParameters</param>
        protected BasePrefilter(T parameters)
        {
            this.Parameters = parameters;
        }

        /// <inheritdoc />
        public abstract IEnumerable<double> Filter(IEnumerable<double> values);
    }
}
