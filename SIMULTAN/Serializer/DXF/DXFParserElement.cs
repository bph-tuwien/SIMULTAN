using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Serializer.DXF
{
    /// <summary>
    /// Base class for all DXF elements
    /// </summary>
    abstract public class DXFParserElement
    {
        /// <summary>
        /// The parent element
        /// </summary>
        internal DXFParserElement Parent { get; set; } = null;

        /// <summary>
        /// A string identifier used to identify the type
        /// </summary>
        internal string Identifier { get; set; } = string.Empty;
    }
}
