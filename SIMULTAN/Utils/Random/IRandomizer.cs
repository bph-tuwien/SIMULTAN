using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Utils.Randomize
{
    /// <summary>
    /// Interface for random number generators
    /// </summary>
    public interface IRandomizer
    {
        /// <summary>
        /// Returns the next random number
        /// </summary>
        /// <returns>A random number</returns>
        double Next();
    }
}
