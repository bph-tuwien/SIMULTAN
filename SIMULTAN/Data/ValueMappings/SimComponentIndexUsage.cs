using SIMULTAN.Data.MultiValues;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.ValueMappings
{
    /// <summary>
    /// States whether the index stored in the parameter is interpreted as a row or column index
    /// </summary>
    public enum SimComponentIndexUsage
    {
        /// <summary>
        /// The parameter in the component that parameterizes a building will be read as a row index
        /// </summary>
        Row = 0,
        /// <summary>
        /// The parameter in the component that parameterizes a building will be read as a column index
        /// </summary>
        Column = 1
    }
}
