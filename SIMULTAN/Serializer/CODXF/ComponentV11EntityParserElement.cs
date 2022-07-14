using SIMULTAN.Data.Components;
using SIMULTAN.Serializer.DXF;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace SIMULTAN.Serializer.CODXF
{
    /// <summary>
    /// Special entity parser element that fixes bugs in components for pre-version 12 files
    /// </summary>
    internal class ComponentV11EntityParserElement : DXFEntityParserElementBase<(SimSlot, SimComponent)>
    {
        private Func<DXFParserResultSet, DXFParserInfo, (SimSlot, SimComponent)> parser;

        /// <summary>
        /// Initializes a new instance of the <see cref="ComponentV11EntityParserElement"/> class
        /// </summary>
        /// <param name="entityName">The name of the entity</param>
        /// <param name="parser">The parser method</param>
        /// <param name="entries">The DXF entries in this entity</param>
        internal ComponentV11EntityParserElement(string entityName, Func<DXFParserResultSet, DXFParserInfo, (SimSlot, SimComponent)> parser,
            IEnumerable<DXFEntryParserElement> entries)
            : base(entityName, entries)
        {
            this.parser = parser;
        }

        /// <inheritdoc/>
        internal override (SimSlot, SimComponent) Parse(DXFStreamReader reader, DXFParserInfo info)
        {
            //Entity start is already handled by parent
            int key = -1; string value = "";
            DXFParserResultSet resultSet = new DXFParserResultSet();

            bool nextEntityIsColor = false;

            while (key != (int)ParamStructCommonSaveCode.ENTITY_START)
            {
                (key, value) = reader.Read();
                if (key == -1)
                {
                    throw new EndOfStreamException(String.Format(
                        "Reached end of stream while parsing Entity \"{0}\" elements", this.EntityName));
                }

                if (key != (int)ParamStructCommonSaveCode.ENTITY_START)
                {
                    //Bugfix for wrong color/sorting order
                    if (info.FileVersion <= 11 && info.FileVersion >= 1 && key == (int)ComponentSaveCode.COLOR)
                    {
                        (key, value) = reader.Read();
                        nextEntityIsColor = true;
                    }

                    var entry = Entries.FirstOrDefault(e => e.Code == key && e.MinVersion <= info.FileVersion && e.MaxVersion >= info.FileVersion);
                    if (entry != null)
                        entry.Parse(reader, resultSet, info);
                    else
                    {
                        //Ignore all entries that are not in the expected set
                        //Debug.WriteLine("Skipping element {0}", key);
                    }

                    //Color order bug handling
                    if (nextEntityIsColor)
                    {
                        var colorEntry = Entries.FirstOrDefault(e => e.Code == (int)ComponentSaveCode.COLOR && e.MinVersion <= info.FileVersion && e.MaxVersion >= info.FileVersion)
                            as DXFEntitySequenceEntryParserElement<Color>;
                        if (colorEntry != null)
                            resultSet.Add((int)ComponentSaveCode.COLOR, colorEntry.ParseBody(reader, info, 1));

                        nextEntityIsColor = false;
                    }
                }
                //Handling of a serializer bug before Version 12
                else if (value == ParamStructTypes.ACCESS_PROFILE && info.FileVersion <= 11)
                {
                    //Continue parsing
                    key = -1;
                }
            }

            return parser(resultSet, info);
        }
    }
}
