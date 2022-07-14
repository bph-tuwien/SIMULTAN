using SIMULTAN.Serializer.DXF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Serializer.DXF
{
    /// <summary>
    /// DXF entry for a single key-value pair data. Uses the <see cref="DXFDataConverter"/> to parse the data
    /// </summary>
    public class DXFBase64SingleEntryParserElement : DXFEntryParserElement
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DXFSingleEntryParserElement{T}"/> class
        /// </summary>
        /// <param name="code">The code of the entry</param>
        internal DXFBase64SingleEntryParserElement(UserSaveCode code) : base((int)code) { }

        /// <inheritdoc />
        internal override object ParseInternal(DXFStreamReader reader, DXFParserInfo info)
        {
            (var key, var value) = reader.GetLast();
            return Convert.FromBase64String(value);
        }
    }
}
