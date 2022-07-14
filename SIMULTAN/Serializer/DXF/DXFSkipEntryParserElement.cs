using SIMULTAN.Serializer.DXF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Serializer.DXF
{
    /// <summary>
    /// Entry used to skip a specific number. Useful when parsing struct arrays with unknown elements
    /// </summary>
    public class DXFSkipEntryParserElement : DXFEntryParserElement
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DXFSkipEntryParserElement"/> class
        /// </summary>
        /// <param name="code">The element code</param>
        public DXFSkipEntryParserElement(ComponentInstanceSaveCode code) : this((int)code) { }

        private DXFSkipEntryParserElement(int code) : base(code) { }

        /// <inheritdoc/>
        internal override void Parse(DXFStreamReader reader, DXFParserResultSet resultSet, DXFParserInfo info)
        {
            //DO NOTHING
        }
        /// <inheritdoc/>
        internal override object ParseInternal(DXFStreamReader reader, DXFParserInfo info)
        {
            //NEVER CALLED
            return null;
        }
    }
}
