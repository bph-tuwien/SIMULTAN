using SIMULTAN.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Utils
{
    /// <summary>
    /// Provides services
    /// </summary>
    public class ServicesProvider : IServicesProvider
    {
        private Dictionary<Type, object> services;

        /// <summary>
        /// Initializes a new instance of the ServicesProvider class
        /// </summary>
        public ServicesProvider()
        {
            this.services = new Dictionary<Type, object>();
        }

        /// <summary>
        /// Initializes a new instance of the ServicesProvider class.
        /// Copies all services from the parent service provider
        /// </summary>
        /// <param name="serviceProvider">The service provider from which the services should be copied</param>
        public ServicesProvider(IServicesProvider serviceProvider)
        {
            if (serviceProvider is ServicesProvider sp)
            {
                this.services = new Dictionary<Type, object>(sp.services);
            }
            else
                throw new NotImplementedException("ServicesProvider only supports copying from other ServicesProviders");
        }

        /// <inheritdoc />
        public T GetService<T>()
        {
            object service = null;
            if (this.services.TryGetValue(typeof(T), out service))
                return (T)service;
            return default(T);
        }

        /// <summary>
        /// Adds a service to the provider
        /// </summary>
        /// <typeparam name="TServiceScope">The service type</typeparam>
        /// <param name="service">The service</param>
        public void AddService<TServiceScope>(TServiceScope service)
        {
            services[typeof(TServiceScope)] = service;
        }
    }
}
