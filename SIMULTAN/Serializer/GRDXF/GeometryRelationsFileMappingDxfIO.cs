using SIMULTAN.Data.Geometry;
using SIMULTAN.Serializer.DXF;
using SIMULTAN.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SIMULTAN.Serializer.GRDXF
{
    /// <summary>
    /// DXF IO class for the <see cref="SimGeometryRelation"/> file mappings.
    /// Used to serialize fileId to geometry file mapping for the geometry with relations import migration.
    /// Used to change the linked file ids to the new ones after import
    /// </summary>
    internal class GeometryRelationsFileMappingDxfIO
    {
        private static DXFEntityParserElement<List<GeometryRelationsFileMapping>> mappingEntity =
            new DXFEntityParserElement<List<GeometryRelationsFileMapping>>(ParamStructTypes.GEOMETRY_RELATION_FILE_MAPPING, ParseFileMappings,
                new DXFEntryParserElement[]
                {
                    new DXFStructArrayEntryParserElement<GeometryRelationsFileMapping>(GeometryRelationSaveCode.GEOMETRY_RELATION_FILE_MAPPINGS, ParseFileMapping,
                        new DXFEntryParserElement[]
                        {
                            new DXFSingleEntryParserElement<int>(GeometryRelationSaveCode.GEOMETRY_RELATION_FILE_MAPPING_FILE_ID),
                            new DXFSingleEntryParserElement<string>(GeometryRelationSaveCode.GEOMETRY_RELATION_FILE_MAPPING_PATH),
                        })
                });


        private static DXFSectionParserElement<List<GeometryRelationsFileMapping>> mappingSection =
            new DXFSectionParserElement<List<GeometryRelationsFileMapping>>(ParamStructTypes.GEOMETRY_RELATION_FILE_MAPPING_SECTION,
            new DXFEntityParserElementBase<List<GeometryRelationsFileMapping>>[]
            {
                mappingEntity,
            });

        private static List<GeometryRelationsFileMapping> ParseFileMappings(DXFParserResultSet result, DXFParserInfo info)
        {
            var mappings = result.Get<GeometryRelationsFileMapping[]>(GeometryRelationSaveCode.GEOMETRY_RELATION_FILE_MAPPINGS, new GeometryRelationsFileMapping[] { });
            return mappings.ToList();
        }

        private static GeometryRelationsFileMapping ParseFileMapping(DXFParserResultSet result, DXFParserInfo info)
        {
            var id = result.Get<int>(GeometryRelationSaveCode.GEOMETRY_RELATION_FILE_MAPPING_FILE_ID, -1);
            var path = result.Get<string>(GeometryRelationSaveCode.GEOMETRY_RELATION_FILE_MAPPING_PATH, "");
            if (id < 0 || string.IsNullOrEmpty(path))
                throw new Exception("Error parsing relation mapping");

            return new GeometryRelationsFileMapping(id, path);
        }

        /// <summary>
        /// Reads the provided GRFMDXF file and parses the contents and returns the mappings.
        /// </summary>
        /// <param name="file">The file.</param>
        /// <param name="info">The info.</param>
        /// <returns>A list of geometry file Id and file path pairs</returns>
        public static List<GeometryRelationsFileMapping> Read(FileInfo file, DXFParserInfo info)
        {
            info.CurrentFile = file;
            using (var fs = file.OpenRead())
            {
                if (fs.Length == 0)
                    return new List<GeometryRelationsFileMapping>();
                using (var reader = new DXFStreamReader(fs))
                {
                    return Read(reader, info);
                }
            }
        }

        /// <summary>
        /// Reads the provided GRFMDXF file and parses the contents and returns the mappings.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <param name="info">The info.</param>
        /// <returns>A list of geometry file Id and file path pairs</returns>
        public static List<GeometryRelationsFileMapping> Read(DXFStreamReader reader, DXFParserInfo info)
        {
            if (CommonParserElements.VersionSectionElement.IsParsable(reader, info))
            {
                info = CommonParserElements.VersionSectionElement.Parse(reader, info).First();
            }

            return mappingSection.Parse(reader, info).First();
        }

        /// <summary>
        /// Writes the file mappings into the provided file in the GRFMDXF format.
        /// </summary>
        /// <param name="file">The file.</param>
        /// <param name="mappings">The geometry file resource Id to file path mappings. Paths are relative to the project.</param>
        public static void Write(FileInfo file, IEnumerable<GeometryRelationsFileMapping> mappings)
        {
            using (var fs = file.Open(FileMode.Create, FileAccess.Write))
            {
                using (var writer = new DXFStreamWriter(fs))
                {
                    Write(writer, mappings);
                }
            }
        }

        /// <summary>
        /// Writes the file mappings into the provided file in the GRFMDXF format.
        /// </summary>
        /// <param name="writer">The writer.</param>
        /// <param name="mappings">The geometry file resource Id to file path mappings. Paths are relative to the project.</param>
        public static void Write(DXFStreamWriter writer, IEnumerable<GeometryRelationsFileMapping> mappings)
        {
            writer.WriteVersionSection();

            writer.StartSection(ParamStructTypes.GEOMETRY_RELATION_FILE_MAPPING_SECTION, -1);

            // mappings entity
            writer.Write(ParamStructCommonSaveCode.ENTITY_START, ParamStructTypes.GEOMETRY_RELATION_FILE_MAPPING);

            writer.WriteArray(GeometryRelationSaveCode.GEOMETRY_RELATION_FILE_MAPPINGS, mappings, (x, w) =>
            {
                w.Write<int>(GeometryRelationSaveCode.GEOMETRY_RELATION_FILE_MAPPING_FILE_ID, x.FileId);
                w.Write<string>(GeometryRelationSaveCode.GEOMETRY_RELATION_FILE_MAPPING_PATH, x.FilePath);
            });

            writer.EndSection();

            writer.WriteEOF();
        }
    }
}
