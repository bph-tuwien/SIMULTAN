using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.Components
{
    // Based on the idea of http://www.interact-sw.co.uk/iangblog/2004/03/23/locking

    /// <summary>
    /// Disables access checking for a given block of code. Always use this in a using block.
    /// </summary>
    /// <example>
    /// using (AccessCheckingDisabler.Disable(manager)
    /// {
    ///     //Perform operation without access checking
    /// }
    /// </example>
    public sealed class AccessCheckingDisabler
    {
        /// <summary>
        /// Disables access checking for the collection. Make sure to call this method only inside the head of a using block.
        /// </summary>
        /// <param name="manager">The component collection for which access checking should be disabled</param>
        /// <returns>An access checking guard which frees the access after being collected</returns>
        public static IDisposable Disable(SimComponentCollection manager)
        {
            if (manager != null)
                return SimComponentCollection.AccessDisablingGuard.Disable(manager);
            return null;
        }
    }

    public partial class SimComponentCollection
    {
        internal sealed class AccessDisablingGuard : IDisposable
        {
            internal static AccessDisablingGuard Disable(SimComponentCollection manager)
            {
                return new AccessDisablingGuard(manager);
            }

            public void Dispose()
            {
                //Debug.WriteLine("Access Checking: {0}", this.manager.EnableAccessCheckingCounter - 1);
                this.manager.EnableAccessCheckingCounter--;

#if DEBUG
                GC.SuppressFinalize(this);
#endif
            }

            private SimComponentCollection manager;

            private AccessDisablingGuard(SimComponentCollection manager)
            {
                this.manager = manager;
                //Debug.WriteLine("Access Checking: {0}", this.manager.EnableAccessCheckingCounter + 1);
                this.manager.EnableAccessCheckingCounter++;
            }

#if DEBUG
            ~AccessDisablingGuard()
            {
                // If this finalizer runs, someone somewhere failed to
                // call Dispose, which means we've failed to leave
                // a monitor!
                throw new InvalidOperationException("Undisposed AccessDisablingGuard");
            }
#endif
        }
    }
}
