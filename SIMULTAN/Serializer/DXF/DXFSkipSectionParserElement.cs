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
    /// Skips a whole section without investigating its content
    /// </summary>
    public class DXFSkipSectionParserElement : DXFParserElement
    {
        private string SectionName { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DXFSkipSectionParserElement"/> class
        /// </summary>
        /// <param name="sectionName">The name of the section</param>
        public DXFSkipSectionParserElement(string sectionName)
        {
            this.SectionName = sectionName;
        }

        /// <summary>
        /// Parse the section and returns a list of all entities in this section
        /// </summary>
        /// <param name="reader">The reader from which the section should be read</param>
        /// <param name="info">Additional info for the parser</param>
        /// <param name="isOptional">When set to True, the parsing continues even if the section isn't found</param>
        /// <returns>A list of all entities found in this section</returns>
        internal void Skip(DXFStreamReader reader, DXFParserInfo info, bool isOptional)
        {
            bool skip = false;

            (var key, var entityName) = reader.Peek();
            if (key == -1)
                throw new EndOfStreamException(string.Format("Reached end of stream while looking for section \"{0}\"", SectionName));

            //Entity Start
            if (key != (int)ParamStructCommonSaveCode.ENTITY_START)
            {
                throw new Exception(string.Format(
                    "Expected Code \"{0}\", but found \"{1}\"", (int)ParamStructCommonSaveCode.ENTITY_START, key));
            }
            if (entityName != ParamStructTypes.SECTION_START)
            {
                if (isOptional)
                    skip = true;
                else
                {
                    throw new Exception(string.Format(
                        "Expected Entity Name \"{0}\", but found \"{1}\"", ParamStructTypes.SECTION_START, entityName));
                }
            }

            if (!skip)
            {
                //Section Name
                (key, entityName) = reader.Peek();
                if (key != (int)ParamStructCommonSaveCode.ENTITY_NAME)
                {
                    throw new Exception(string.Format(
                        "Expected Code \"{0}\", but found \"{1}\"", (int)ParamStructCommonSaveCode.ENTITY_NAME, key));
                }
                if (entityName != this.SectionName)
                {
                    if (isOptional)
                        skip = true;
                    else
                    {
                        throw new Exception(string.Format(
                            "Expected Entity Name \"{0}\", but found \"{1}\"", this.SectionName, entityName));
                    }
                }
            }

            if (!skip)
            {
                reader.ClearPeek();

                while (key != (int)ParamStructCommonSaveCode.ENTITY_START || entityName != ParamStructTypes.SECTION_END)
                {
                    (key, entityName) = reader.Read();
                    if (key == -1)
                    {
                        throw new EndOfStreamException(String.Format(
                            "Reached end of stream while parsing Section \"{0}\" elements", this.SectionName));
                    }
                }
            }
        }
    }
}
