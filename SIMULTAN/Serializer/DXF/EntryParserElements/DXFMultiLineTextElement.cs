using SIMULTAN.Serializer.DXF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Serializer.DXF
{
    /// <summary>
    /// DXF entry containing multi-line text
    /// </summary>
    public class DXFMultiLineTextElement : DXFEntryParserElement
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DXFMultiLineTextElement"/> class
        /// </summary>
        /// <param name="code">The code of the count entry</param>
        internal DXFMultiLineTextElement(ComponentSaveCode code) : base((int)code) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="DXFMultiLineTextElement"/> class
        /// </summary>
        /// <param name="code">The code of the count entry</param>
        internal DXFMultiLineTextElement(ParamStructCommonSaveCode code) : base((int)code) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="DXFMultiLineTextElement"/> class
        /// </summary>
        /// <param name="code">The code of the count entry</param>
        internal DXFMultiLineTextElement(MultiValueSaveCode code) : base((int)code) { }

        /// <inheritdoc/>
        internal override object ParseInternal(DXFStreamReader reader, DXFParserInfo info)
        {
            (var key, var value) = reader.GetLast();
            return value.Replace(DXFStreamWriter.NEWLINE_PLACEHOLDER, Environment.NewLine);
        }
    }
}
