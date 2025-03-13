using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Utils
{
    /// <summary>
    /// Provides a default (OS independent) implementation of the <see cref="ISynchronizeInvoke"/> interface that doesn't perform any synchronization.
    /// This is mainly needed when working with user interfaces that need to run code asynchronously and need to be reimplemented for the respective
    /// UI system.
    /// This default implementation doesn't perform any synchronization and runs the code immediately.
    /// </summary>
    public class UnsyncedSynchronizationContext : ISynchronizeInvoke
    {
        /// <inheritdoc />
        public bool InvokeRequired => true;

        /// <inheritdoc />
        public IAsyncResult BeginInvoke(Delegate method, object[] args)
        {
            return Task.FromResult(method.DynamicInvoke(args));
        }

        /// <inheritdoc />
        public object EndInvoke(IAsyncResult result)
        {
            var task = (Task<object>)result;
            task.Wait();
            return task.Result;
        }

        /// <inheritdoc />
        public object Invoke(Delegate method, object[] args)
        {
            return method.DynamicInvoke(args);
        }
    }
}
