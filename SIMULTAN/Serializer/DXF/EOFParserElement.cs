using SIMULTAN.Serializer.DXF;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Serializer.DXF
{
    /// <summary>
    /// Parser element for the end of file marker
    /// </summary>
    public class EOFParserElement
    {
        /// <summary>
        /// The default EOF element
        /// </summary>
        internal static EOFParserElement Element { get; } = new EOFParserElement();

        private EOFParserElement()
        { }

        /// <summary>
        /// Reads the EOF marker
        /// </summary>
        /// <param name="reader">Ther reader from which the data should be read</param>
        public void Parse(DXFStreamReader reader)
        {
            (var key, var value) = reader.Read();
            if (key == -1)
            {
                throw new EndOfStreamException(String.Format(
                    "Reached end of stream while searching for EOF Entity"));
            }

            if (key != (int)ParamStructCommonSaveCode.ENTITY_START)
            {
                throw new Exception(string.Format(
                    "Expected Code \"{0}\", but found \"{1}\"", (int)ParamStructCommonSaveCode.ENTITY_START, key));
            }
            if (value != ParamStructTypes.EOF)
            {
                throw new Exception(string.Format(
                    "Expected Entity Name \"{0}\", but found \"{1}\"", ParamStructTypes.EOF, value));
            }
        }
    }
}
