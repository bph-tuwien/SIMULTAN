using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Exceptions
{
    /// <summary>
    /// Exception which get's thrown in case a parameter couldn't be found
    /// </summary>
    public class ParameterNotFoundException : Exception
    {
        /// <summary>
        /// The id of the missing parameter
        /// </summary>
        public long ParameterID { get; }

        /// <summary>
        /// Invokes a new instance of the ParameterNotFoundException class
        /// </summary>
        /// <param name="id">The id of the missing parameter</param>
        public ParameterNotFoundException(long id)
            : base(string.Format("Parameter with Id {0} not found", id))
        {
            this.ParameterID = id;
        }
    }
}
