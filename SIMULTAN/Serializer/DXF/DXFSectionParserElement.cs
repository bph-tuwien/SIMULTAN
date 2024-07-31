using SIMULTAN.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SIMULTAN.Serializer.DXF
{
    /// <summary>
    /// A DXF section that contains other DXF entities.
    /// A section starts with code "0" with value "SECTION", followed by the code "2" which contains the section name.
    /// </summary>
    /// <typeparam name="T">The type of the objects in this section. Entities have to be of this type or of a subtype</typeparam>
    public class DXFSectionParserElement<T> : DXFParserElement
    {
        /// <summary>
        /// The entities in this section
        /// </summary>
        public Dictionary<string, DXFEntityParserElementBase<T>> Entities { get; private set; }

        private string SectionName { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DXFSectionParserElement{T}"/> class
        /// </summary>
        /// <param name="sectionName">The name of the section</param>
        /// <param name="entities">The entities in this section</param>
        internal DXFSectionParserElement(string sectionName, IEnumerable<DXFEntityParserElementBase<T>> entities)
        {
            this.SectionName = sectionName;
            entities.ForEach(x => x.Parent = this);
            this.Entities = entities.ToDictionary(x => x.EntityName, x => x);
        }

        /// <summary>
        /// Checks if a Section is parsable (has the correct section name).
        /// </summary>
        /// <param name="reader">The reader</param>
        /// <param name="info">the info</param>
        /// <returns>True if the section is parseable, false otherwise</returns>
        /// <exception cref="EndOfStreamException">If the end of the stream was reached</exception>
        /// <exception cref="Exception">If any of the entities could not be found</exception>
        public bool IsParsable(DXFStreamReader reader, DXFParserInfo info)
        {
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
                throw new Exception(string.Format(
                    "Expected Entity Name \"{0}\", but found \"{1}\"", ParamStructTypes.SECTION_START, entityName));
            }

            //Section Name
            (key, entityName) = reader.Peek();
            if (key != (int)ParamStructCommonSaveCode.ENTITY_NAME)
            {
                throw new Exception(string.Format(
                    "Expected Code \"{0}\", but found \"{1}\"", (int)ParamStructCommonSaveCode.ENTITY_NAME, key));
            }
            if (entityName != this.SectionName)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Parse the section and returns a list of all entities in this section
        /// </summary>
        /// <param name="reader">The reader from which the section should be read</param>
        /// <param name="info">Additional info for the parser</param>
        /// <returns>A list of all entities found in this section</returns>
        internal List<T> Parse(DXFStreamReader reader, DXFParserInfo info)
        {
            (var key, var entityName) = reader.Read();
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
                throw new Exception(string.Format(
                    "Expected Entity Name \"{0}\", but found \"{1}\"", ParamStructTypes.SECTION_START, entityName));
            }

            //Section Name
            (key, entityName) = reader.Read();
            if (key != (int)ParamStructCommonSaveCode.ENTITY_NAME)
            {
                throw new Exception(string.Format(
                    "Expected Code \"{0}\", but found \"{1}\"", (int)ParamStructCommonSaveCode.ENTITY_NAME, key));
            }
            if (entityName != this.SectionName)
            {
                throw new Exception(string.Format(
                    "Expected Section Name \"{0}\", but found \"{1}\"", this.SectionName, entityName));
            }

            List<T> results = new List<T>();
            //Section Name
            (key, entityName) = reader.Read();
            if (key != (int)ParamStructCommonSaveCode.NUMBER_OF)
            {
                //Old files do not have this saved
            }
            else
            {
                //Move to next, afterwards done by the entity
                if (int.TryParse(entityName, out var number) && number > 0)
                {
                    results = new List<T>(number);
                }
                reader.Read();
            }

            //Entities in Section
            //List<T> results = new List<T>();
            //Move to next, afterwards done by the entity
            //reader.Read();

            while (key != (int)ParamStructCommonSaveCode.ENTITY_START || entityName != ParamStructTypes.SECTION_END)
            {
                (key, entityName) = reader.GetLast();
                if (key == -1)
                {
                    throw new EndOfStreamException(String.Format(
                        "Reached end of stream while parsing Section \"{0}\" elements", this.SectionName));
                }

                if (key != (int)ParamStructCommonSaveCode.ENTITY_START || entityName != ParamStructTypes.SECTION_END)
                {
                    if (!this.Entities.TryGetValue(entityName, out var entity))
                    {
                        throw new EndOfStreamException(String.Format(
                            "Unsupported EntitiyName \"{0}\" in Section \"{1}\"", entityName, this.SectionName));
                    }

                    results.Add(entity.Parse(reader, info));
                }
            }

            //Section End
            return results;
        }
    }
}
