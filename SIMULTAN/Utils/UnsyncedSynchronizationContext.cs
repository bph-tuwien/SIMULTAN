using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Utils
{
    public class UnsyncedSynchronizationContext : ISynchronizeInvoke
    {
        public bool InvokeRequired => true;

        public IAsyncResult BeginInvoke(Delegate method, object[] args)
        {
            return Task.FromResult(method.DynamicInvoke(args));
        }

        public object EndInvoke(IAsyncResult result)
        {
            var task = (Task<object>)result;
            task.Wait();
            return task.Result;
        }

        public object Invoke(Delegate method, object[] args)
        {
            return method.DynamicInvoke(args);
        }
    }
}
