using SIMULTAN.Projects;
using SIMULTAN.Serializer.DXF;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Serializer.METADXF
{
    /// <summary>
    /// Provides methods for serializing project meta data into a MetaDXF File
    /// </summary>
    public static class MetaDxfIO
    {

        #region Syntax

        /// <summary>
        /// Syntax for a HierarchicProjectMetaData
        /// </summary>
        internal static DXFEntityParserElementBase<HierarchicProjectMetaData> MetaDataEntityElement =
            new DXFEntityParserElement<HierarchicProjectMetaData>(ParamStructTypes.PROJECT_METADATA, ParseMetaData,
                new DXFEntryParserElement[]
                {
                    new DXFSingleEntryParserElement<Guid>(ProjectSaveCode.PROJECT_ID),
                });

        /// <summary>
        /// Syntax for a MetaData section
        /// </summary>
        internal static DXFSectionParserElement<HierarchicProjectMetaData> MetaDataSectionEntityElement =
            new DXFSectionParserElement<HierarchicProjectMetaData>(ParamStructTypes.META_SECTION,
                new DXFEntityParserElementBase<HierarchicProjectMetaData>[]
                {
                    MetaDataEntityElement
                });

        #endregion

        /// <summary>
        /// Writes the project meta data into a file
        /// </summary>
        /// <param name="file">The target file</param>
        /// <param name="metaData">The meta data to serialize</param>
        public static void Write(FileInfo file, HierarchicProjectMetaData metaData)
        {
            if (file == null)
                throw new ArgumentNullException(nameof(file));
            if (metaData == null)
                throw new ArgumentNullException(nameof(metaData));

            using (FileStream fs = new FileStream(file.FullName, FileMode.Create, FileAccess.Write))
            {
                using (DXFStreamWriter writer = new DXFStreamWriter(fs))
                {
                    Write(writer, metaData);
                }
            }
        }

        internal static void Write(DXFStreamWriter writer, HierarchicProjectMetaData metaData)
        {
            //File header
            writer.WriteVersionSection();

            //Data
            writer.StartSection(ParamStructTypes.META_SECTION);

            WriteMetaData(writer, metaData);

            writer.EndSection();

            //EOF
            writer.WriteEOF();
        }


        /// <summary>
        /// Reads project meta data from a file
        /// </summary>
        /// <param name="file">The file to read</param>
        /// <param name="parserInfo">Additonal parser info. Not used by the serializer</param>
        /// <returns>The parsed meta data, or Null when an error happened</returns>
        public static HierarchicProjectMetaData Read(FileInfo file, DXFParserInfo parserInfo)
        {
            using (FileStream stream = file.OpenRead())
            {
                if (stream.Length == 0)
                    return null;

                using (DXFStreamReader reader = new DXFStreamReader(stream))
                {
                    return Read(reader, parserInfo);
                }
            }
        }

        internal static HierarchicProjectMetaData Read(DXFStreamReader reader, DXFParserInfo parserInfo)
        {
            //Version section
            if (CommonParserElements.VersionSectionElement.IsParsable(reader, parserInfo))
            {
                parserInfo = CommonParserElements.VersionSectionElement.Parse(reader, parserInfo).First();
            }
            else
            {
                parserInfo.FileVersion = 11;
            }

            //Data section
            var metaData = MetaDataSectionEntityElement.Parse(reader, parserInfo);

            //EOF
            EOFParserElement.Element.Parse(reader);

            parserInfo.FinishLog();

            return metaData.FirstOrDefault();
        }



        internal static void WriteMetaData(DXFStreamWriter writer, HierarchicProjectMetaData metaData)
        {
            writer.Write(ParamStructCommonSaveCode.ENTITY_START, ParamStructTypes.PROJECT_METADATA);
            writer.Write(ParamStructCommonSaveCode.CLASS_NAME, typeof(HierarchicProjectMetaData));

            writer.Write(ProjectSaveCode.PROJECT_ID, metaData.ProjectId);

            if (metaData.ChildProjects.Count != 0)
                throw new NotSupportedException("Child projects are currently not supported");

            writer.Write(ProjectSaveCode.NR_OF_CHILD_PROJECTS, 0);
        }

        private static HierarchicProjectMetaData ParseMetaData(DXFParserResultSet data, DXFParserInfo info)
        {
            var guid = data.Get<Guid>(ProjectSaveCode.PROJECT_ID, Guid.Empty);

            return new HierarchicProjectMetaData(guid, new Dictionary<Guid, string> { });
        }
    }
}
