using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Serializer.DXF
{
    public static class ParamStructFileExtensions
    {
        public const string FILE_EXT_COMPONENTS = ".codxf";
        public const string FILE_EXT_PARAMETERS = ".padxf";
        public const string FILE_EXT_MULTIVALUES = ".mvdxf";
        public const string FILE_EXT_EXCEL_TOOL_COLLECTION = ".etdxf";
        public const string FILE_EXT_IMAGES = ".bin";

        public const string FILE_EXT_USERS = ".simuser";
        public const string FILE_EXT_COMPONENTS_PUBLIC = ".codxfp";
        public const string FILE_EXT_MULTIVALUES_PUBLIC = ".mvdxfp";
        public const string FILE_EXT_META = ".metadxf";
        public const string FILE_EXT_LINKS = ".simlinks";

        [Obsolete]
        public const string FILE_EXT_MASTER = ".master";
        public const string FILE_EXT_GEOMETRY_INTERNAL = ".simgeo";

        [Obsolete]
        public const string FILE_EXT_PROJECT = ".smn";
        public const string FILE_EXT_PROJECT_COMPACT = ".simultan";
        public const string FILE_EXT_PROJECT_COMPACT_BACKUP = ".simultanbackup";

        public const string FILE_EXT_GEOMAP = ".gmdxf";
        public const string FILE_EXT_SITEPLANNER = ".spdxf";
        public const string FILE_EXT_TAXONOMY = ".txdxf";

        /// <summary>
        /// The file extension for the file saving all public resource paths.
        /// </summary>
        public const string FILE_EXT_PUBLIC_PROJECT_PATHS = ".ppaths";
        /// <summary>
        /// The file suffix for the file saving all public resource paths.
        /// </summary>
        public const string PUBLIC_PROJECT_PATHS_SUFFIX = "_PATHS";

        public static List<string> GetAllManagedFileExtensions()
        {
            List<string> exts = new List<string>()
            {
                FILE_EXT_COMPONENTS,
                FILE_EXT_PARAMETERS,
                FILE_EXT_MULTIVALUES,
                FILE_EXT_EXCEL_TOOL_COLLECTION,
                FILE_EXT_IMAGES,

                FILE_EXT_USERS,
                FILE_EXT_COMPONENTS_PUBLIC,
                FILE_EXT_MULTIVALUES_PUBLIC,
                FILE_EXT_META,
                FILE_EXT_LINKS,
                FILE_EXT_GEOMAP,
                FILE_EXT_SITEPLANNER,
                FILE_EXT_TAXONOMY,

                FILE_EXT_GEOMETRY_INTERNAL,
            };
            return exts;
        }
    }
}
