using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Serializer.DXF
{
    // ALL SAVE CODES
    // [0    - 1000]: DXF Specs and general custom codes
    // [1001 - 1100]: Parameter
    // [1101 - 1300]: MultiValue
    // [1301 - 1400]: Calculation
    // [1401 - 1500]: Component
    //      [[1421 - 1430]]: Component -> AccessTracker
    //      [[1431 - 1440]]: Component -> AccessProfile
    // [1501 - 1600]: FlowNetwork
    // [1601 - 1700]: ComponentInstance

    // general codes for all types
    // the more specific codes are saved with their respective types (i.e. Parameter)
    public enum ParamStructCommonSaveCode
    {
        INVALID_CODE = -11, // random, has to be negative (DXF convention)
        ENTITY_START = 0,   // DXF specs
        ENTITY_NAME = 2,    // DXF specs
        COORDS_X = 10,      // DXF specs
        COORDS_Y = 20,      // DXF specs
        CLASS_NAME = 100,   // AutoCAD specs
        ENTITY_LOCATION = 899, // custom
        ENTITY_ID = 900,    // custom
        NUMBER_OF = 901,    // ...
        TIME_STAMP = 902,
        ENTITY_REF = 903,   // saves the ID of a referenced entity (can be in another file)
        ENTITY_KEY = 904,   // for saving dictionaries

        STRING_VALUE = 909,
        X_VALUE = 910,
        Y_VALUE = 920,
        Z_VALUE = 930,
        W_VALUE = 940,
        V5_VALUE = 950,
        V6_VALUE = 960,
        V7_VALUE = 970,
        V8_VALUE = 980,
        V9_VALUE = 990,
        V10_VALUE = 1000
    }

    // entity names (to be placed after code ENTITY_START)
    public static class ParamStructTypes
    {
        public const string FLOWNETWORK = "FLOWNETWORK";        // custom
        public const string FLOWNETWORK_NODE = "FLOWNETWORK_NODE";// custom
        public const string FLOWNETWORK_EDGE = "FLOWNETWORK_EDGE";// custom
        public const string COMPONENT = "COMPONENT";            // custom
        public const string USER_LIST = "USER_LIST";
        [Obsolete]
        public const string TYPED_COMPONENT = "TYPED_COMPONENT";// custom
        public const string CALCULATION = "CALCULATION";        // custom
        public const string PARAMETER = "PARAMETER";            // custom
        [Obsolete]
        public const string IPARAM_VIS = "IPARAM_VIS";          // custom
        public const string VALUE_FIELD = "VALUE_FIELD";        // custom
        public const string FUNCTION_FIELD = "FUNCTION_FIELD";  // custom
        public const string BIG_TABLE = "BIG_TABLE";            // custom
        public const string GEOM_RELATION = "GEOM_RELATIONSHIP";// custom
        public const string MAPPING_TO_COMP = "MAPPING2COMPONENT"; // custom
        public const string EXCEL_RULE = "EXCEL_RULE";
        public const string EXCEL_TOOL = "EXCEL_TOOL";
        public const string EXCEL_MAPPING = "EXCEL_MAPPING";
        public const string EXCEL_UNMAPPING = "EXCEL_UN_MAPPING";
        public const string EXCEL_DATA_RESULT = "EXCEL_RESULT";
        [Obsolete]
        public const string EXCEL_MAPPING_COL = "EXCEL_MAPPING_COLLECTION";
        public const string ASSET_MANAGER = "ASSET_MANAGER";
        public const string ASSET_GEOM = "ASSET_GEOMETRIC";
        public const string ASSET_DOCU = "ASSET_DOCUMENT";
        public const string CHAT_ITEM = "CHAT_ITEM";
        public const string CHAT_SEQ = "CHAT_SEQUENCE_FOR_COMPONENT"; // only for distributed saving of component chats
        public const string USER = "USER";
        public const string MULTI_LINK = "MULTI_LINK";
        public const string PROJECT_METADATA = "PROJECT_METADATA";
        public const string GEOMAP = "GEOMAP";
        public const string SITEPLANNER = "SITEPLANNER";
        public const string RESOURCE_DIR = "RESOURCE_DIRECTORY";
        public const string RESOURCE_FILE = "RESOURCE_CONTAINED_FILE";
        public const string RESOURCE_LINK = "RESOURCE_LINKED_FILE";
        public const string FILE_VERSION = "FILE_VERSION";

        public const string ACCESS_TRACKER = "ACCESS_TRACKER";  // custom helper
        public const string ACCESS_PROFILE = "ACCESS_PROFILE";  // custom helper
        [Obsolete("Was used for distributed save/load")]
        public const string ACCESS_ACTION = "ACCESS_ACTION";  // custom helper

        public const string ENTITY_SECTION = "ENTITIES";        // DXF specs
        public const string NETWORK_SECTION = "NETWORKS";       // custom
        [Obsolete]
        public const string IMPORTANT_SECTION = "IMPORTANT";    // custom
        public const string EXCEL_SECTION = "EXCEL_TOOLS";      // custom
        public const string COMMON_EXCEL_SECTION = "COMMON_EXCEL_MAP";// custom
        public const string COMMON_ACCESS_MARKER_SECTION = "COMMON_ACCESS_MARKERS"; // custom
        public const string ASSET_SECTION = "ASSETS";           // custom
        public const string CONVERSATIONS_SECTION = "CONVERSATIONS"; // custom: for distributed saving
        public const string USER_SECTION = "USERS";
        public const string MULTI_LINK_SECTION = "MULTI_LINKS";
        public const string META_SECTION = "METADATA";          // custom: for project metadata
        public const string GEOMAP_SECTION = "GEOMAPS";         // custom: for geomaps
        public const string SITEPLANNER_SECTION = "SITEPLANNER";// custom: for siteplanner
        public const string VERSION_SECTION = "VERSION_SECTION"; // for saving the version information
        public const string SECTION_START = "SECTION";          // DXF specs
        public const string SECTION_END = "ENDSEC";             // DXF specs
        public const string SEQUENCE_END = "SEQEND";            // DXF specs
        public const string EOF = "EOF";                        // DXF specs

        public const string ENTITY_SEQUENCE = "ENTSEQ";         // custom
        public const string ENTITY_CONTINUE = "ENTCTN";         // custom

        public const string COLOR_IN_BYTES = "BYTE_COLOR";      // for saving colors with byte-color components

        // public const string DOUBLE_NAN = "NaN";                 // custom
        public const int NOT_END_OF_LIST = 1;                   // custom
        public const int END_OF_LIST = -1;                      // custom
        public const int END_OF_SUBLIST = -11;                  // custom
        public const string DELIMITER_WITHIN_ENTRY = "_|_";

        public static readonly DateTimeFormatInfo DT_FORMATTER = new DateTimeFormatInfo();
    }

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

                FILE_EXT_GEOMETRY_INTERNAL,
            };
            return exts;
        }
    }
}
