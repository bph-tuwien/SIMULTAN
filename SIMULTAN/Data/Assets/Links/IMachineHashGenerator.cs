using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.Assets
{
    /// <summary>
    /// Generator that returns the unique hash of a machine.
    /// </summary>
    public interface IMachineHashGenerator
    {
        /// <summary>
        /// Return the Sha256 has as a hex string that uniquely identifies the userers machine.
        /// </summary>
        /// <returns>The Sha256 has as a hex string that uniquely identifies the userers machine.</returns>
        string GetMachineHash();
    }
}
