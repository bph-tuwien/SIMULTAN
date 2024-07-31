using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Utils
{
    /// <summary>
    /// Interface for a dispatcher timer factory used to create dispatcher timers.
    /// </summary>
    [Obsolete("Only used in OffsetSurfaceGenerator which should be refactored")]
    public interface IDispatcherTimerFactory
    {
        /// <summary>
        /// Creates a new DispatcherTimer
        /// </summary>
        /// <returns>The newly created DispatcherTimer</returns>
        IDispatcherTimer Create();
    }
}
