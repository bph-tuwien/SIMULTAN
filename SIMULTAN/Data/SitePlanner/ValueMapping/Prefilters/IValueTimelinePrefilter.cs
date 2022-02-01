using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.SitePlanner
{
    /// <summary>
    /// Extends IValuePrefilter by requiring a sorted range of possible indices from which to choose
    /// </summary>
    public interface IValueTimelinePrefilter : IValuePrefilter
    {
        /// <summary>
        /// Sets the range of possible indices from which to choose
        /// </summary>
        /// <param name="headers">Headers to display</param>
        void SetRange(List<string> headers);
    }
}
