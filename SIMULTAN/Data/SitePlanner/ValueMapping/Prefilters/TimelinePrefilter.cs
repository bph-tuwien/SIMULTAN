using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.SitePlanner
{
    /// <summary>
    /// Filters out a subrange of values 
    /// </summary>
    public class TimelinePrefilter : BasePrefilter<TimelinePrefilterParameters>, IValueTimelinePrefilter
    {
        /// <summary>
        /// Initializes a new instance of this class
        /// </summary>
        public TimelinePrefilter(TimelinePrefilterParameters parameters)
            : base(parameters)
        {
        }

        /// <inheritdoc />
        public override IEnumerable<double> Filter(IEnumerable<double> values)
        {
            return new List<double> { values.ElementAt(DerivedParameters.Current) };
        }

        /// <inheritdoc />
        public void SetRange(List<string> headers)
        {
            if (this.DerivedParameters.Current >= headers.Count)
            {
                this.DerivedParameters.Current = headers.Count - 1;
            }
            this.DerivedParameters.Headers = new System.Collections.ObjectModel.ObservableCollection<string>(headers);
        }
    }
}
