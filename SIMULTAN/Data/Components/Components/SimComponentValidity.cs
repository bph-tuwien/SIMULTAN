using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.Components
{
    /// <summary>
    /// Specifies the access state of a <see cref="SimComponent"/> or <see cref="SimAccessProfile"/>
    /// </summary>
    public enum SimComponentValidity
    {
        /// <summary>
        /// The component state is not valid. The write operation has happened after the last supervize access
        /// </summary>
        WriteAfterSupervize = 1,
        /// <summary>
        /// The component state is not valid. The write operation has happened after the last release access
        /// </summary>
        WriteAfterRelease = 2,
        /// <summary>
        /// The component state is not valid. The supervize operation has happened after the last release access
        /// </summary>
        SupervizeAfterRelease = 3,
        /// <summary>
        /// The component state is valid. The order of operation is WRITE, then SUPERVIZE, then RELEASE
        /// </summary>
        Valid = 4
    }
}
