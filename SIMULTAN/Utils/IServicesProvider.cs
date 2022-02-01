using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Utils
{
    /// <summary>
    /// Interface for a services provider
    /// </summary>
    public interface IServicesProvider
    {
        /// <summary>
        /// Returns a service
        /// </summary>
        /// <typeparam name="T">Type of the service</typeparam>
        /// <returns>The service, or Null when no such service exists</returns>
        T GetService<T>();
    }
}
