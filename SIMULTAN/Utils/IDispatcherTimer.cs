using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Utils
{
    /// <summary>
    /// Interface for a dispatcher timer to run timer events on the main thread.
    /// Currently used only in the OffsetSurfaceGenerator and will be removed once it is refactored.
    /// </summary>
    [Obsolete("Only used in OffsetSurfaceGenerator which should be refactored")]
    public interface IDispatcherTimer
    {
        /// <summary>
        /// Interval between timer inovkes.
        /// </summary>
        TimeSpan Interval { get; set; }

        /// <summary>
        /// Starts the timer.
        /// </summary>
        void Start();
        /// <summary>
        /// Stops the timer.
        /// </summary>
        void Stop();
        /// <summary>
        /// Adds an event handler to the timer tick.
        /// </summary>
        /// <param name="handler">The event handler</param>
        void AddTickEventHandler(EventHandler handler);
        /// <summary>
        /// Removes an event handler from the timer tick.
        /// </summary>
        /// <param name="handler">The event handler</param>
        void RemoveTickEventHandler(EventHandler handler);
    }
}
