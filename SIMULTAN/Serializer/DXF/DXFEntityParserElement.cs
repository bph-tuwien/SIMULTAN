using SIMULTAN.Serializer.DXF;
using SIMULTAN.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Serializer.DXF
{
    /// <summary>
    /// Interface for all entity parser elements
    /// </summary>
    public interface IDXFEntityParserElementBase
    {
        /// <summary>
        /// A list of all entry elements
        /// </summary>
        IEnumerable<DXFEntryParserElement> Entries { get; }
    }

    /// <summary>
    /// Base class for all Entity elements
    /// An Entity always consists of the entity start code "0" followed by the name of the entity
    /// and a number of <see cref="DXFEntryParserElement"/>.
    /// </summary>
    public abstract class DXFEntityParserElementBase<T> : DXFParserElement, IDXFEntityParserElementBase
    {
        /// <summary>
        /// The entries in the entity
        /// </summary>
        public IEnumerable<DXFEntryParserElement> Entries { get; }

        /// <summary>
        /// Name of the Entity
        /// </summary>
        internal string EntityName { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DXFEntryParserElement"/> class
        /// </summary>
        /// <param name="entityName">The name of the entity</param>
        /// <param name="entries">The entries inside the entity</param>
        protected DXFEntityParserElementBase(string entityName, IEnumerable<DXFEntryParserElement> entries)
        {
            this.EntityName = entityName;
            this.Entries = entries;
            this.Entries.ForEach(x => x.Parent = this);
        }
        /// <summary>
        /// Parses the content of the entity. Should be called from derived classes which are later on responsible for parsing the result
        /// </summary>
        /// <param name="reader">The reader from which the content should be read</param>
        /// <param name="info">Supporting information for the parsing</param>
        /// <returns>A set containing all entries inside the entity</returns>
        protected DXFParserResultSet ParseContent(DXFStreamReader reader, DXFParserInfo info)
        {
            //Entity start is already handled by parent
            int key = -1;
            DXFParserResultSet resultSet = new DXFParserResultSet();

            while (key != (int)ParamStructCommonSaveCode.ENTITY_START)
            {
                (key, _) = reader.Read();
                if (key == -1)
                {
                    throw new EndOfStreamException(String.Format(
                        "Reached end of stream while parsing Entity \"{0}\" elements", this.EntityName));
                }

                if (key != (int)ParamStructCommonSaveCode.ENTITY_START)
                {
                    var entry = Entries.FirstOrDefault(e => e.Code == key && e.MinVersion <= info.FileVersion && e.MaxVersion >= info.FileVersion);
                    if (entry != null)
                        entry.Parse(reader, resultSet, info);
                    else
                    {
                        //Debug.WriteLine("Skipping element {0}", key);
                    }
                    //Ignore all entries that are not in the expected set
                }
            }

            return resultSet;
        }

        /// <summary>
        /// Parses the content of the entity. May be overridden in derived classes if the default parsing behavior is not sufficient. 
        /// By default <see cref="ParseContent(DXFStreamReader, DXFParserInfo)"/> should be used.
        /// </summary>
        /// <param name="reader">The reader from which the content should be read</param>
        /// <param name="info">Supporting information for the parsing</param>
        /// <returns>The parsed object</returns>
        internal abstract T Parse(DXFStreamReader reader, DXFParserInfo info);
    }

    /// <summary>
    /// Parser element for a DXF entity. Handles reading and parsing of the entity content
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class DXFEntityParserElement<T> : DXFEntityParserElementBase<T>
    {
        private Func<DXFParserResultSet, DXFParserInfo, T> parser;

        /// <summary>
        /// Initializes a new instance of the <see cref="DXFEntityParserElement{T}"/> class
        /// </summary>
        /// <param name="entityName">The name of the entity</param>
        /// <param name="parser">The method used to parse the read data into an object</param>
        /// <param name="entries">The entries in this entity</param>
        internal DXFEntityParserElement(string entityName, Func<DXFParserResultSet, DXFParserInfo, T> parser,
            IEnumerable<DXFEntryParserElement> entries)
            : base(entityName, entries)
        {
            this.parser = parser;
        }

        /// <summary>
        /// Reads and parses the content of the entity
        /// </summary>
        /// <param name="reader">The reader from which the content should be read</param>
        /// <param name="info">Supporting information for the parsing</param>
        /// <returns>The parsed object</returns>
        internal override T Parse(DXFStreamReader reader, DXFParserInfo info)
        {
            var entrySet = ParseContent(reader, info);

            //Convert result set to object
            return parser(entrySet, info);
        }
    }
}
