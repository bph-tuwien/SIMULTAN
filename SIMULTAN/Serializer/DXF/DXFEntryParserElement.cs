using SIMULTAN.Serializer.DXF;
using SIMULTAN.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Serializer.DXF
{
    /// <summary>
    /// Represents a entry inside a <see cref="DXFEntityParserElement{T}"/>
    /// </summary>
    public abstract class DXFEntryParserElement : DXFParserElement
    {
        /// <summary>
        /// The DXF code of the section
        /// </summary>
        public int Code { get; }

        /// <summary>
        /// The minimal <see cref="DXFParserInfo.FileVersion"/> in which this element is valid
        /// </summary>
        internal ulong MinVersion { get; set; } = 0;
        /// <summary>
        /// The maximum <see cref="DXFParserInfo.FileVersion"/> in which this element is valid
        /// </summary>
        internal ulong MaxVersion { get; set; } = ulong.MaxValue;
        /// <summary>
        /// When set to True, the entry may be skipped during parsing
        /// </summary>
        internal bool IsOptional { get; set; } = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="DXFEntryParserElement"/> class
        /// </summary>
        /// <param name="code">The code of the entry</param>
        protected DXFEntryParserElement(int code)
        {
            this.Code = code;
        }

        /// <summary>
        /// Parses the entry. This section assumes that the code of this entry has already been read and that <see cref="DXFStreamReader.GetLast"/>
        /// returns this entry. Calls <see cref="ParseInternal(DXFStreamReader,DXFParserInfo)"/> to perform the reading and parsing.
        /// </summary>
        /// <param name="reader">The reader to read from</param>
        /// <param name="resultSet">The result set into which the parsed data should be stored</param>
        /// <param name="info">Info for the parser</param>
        internal virtual void Parse(DXFStreamReader reader, DXFParserResultSet resultSet, DXFParserInfo info)
        {
            resultSet.Add(Code, ParseInternal(reader, info));
        }
        /// <summary>
        /// Implement this method to read from the DXF stream and parse the current entry
        /// </summary>
        /// <param name="reader">The reader to read from. <see cref="DXFStreamReader.GetLast"/> has to contain the current entry</param>
        /// <param name="info">Info for the parser</param>
        /// <returns>The parsed entry</returns>
        internal abstract object ParseInternal(DXFStreamReader reader, DXFParserInfo info);
    }
}
