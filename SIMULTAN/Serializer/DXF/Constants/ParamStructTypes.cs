using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Serializer.DXF
{
    // entity names (to be placed after code ENTITY_START)
    public static class ParamStructTypes
    {
        //SimNetwork specific struct types
        public const string SIMNETWORK = "SIMNETWORK";
        public const string SIMNETWORK_BLOCK = "SIMNETWORK_BLOCK";
        public const string SIMNETWORK_PORT = "SIMNETWORK_PORT";
        public const string SIMNETWORK_CONNECTOR = "SIMNETWORK_CONNECTOR";

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
        public const string COMPONENT_INSTANCE = "GEOM_RELATIONSHIP";// custom
        public const string INSTANCE_PLACEMENT_NETWORK = "INSTANCE_PLACEMENT_NETWORK";
        public const string INSTANCE_PLACEMENT_GEOMETRY = "INSTANCE_PLACEMENT_GEOMETRY";
        public const string INSTANCE_PLACEMENT_SIMNETWORK = "INSTANCE_PLACEMENT_SIMNETWORK";
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
        public const string COLOR_MAP_MULTI_THRESHOLD = "COLOR_MAP_MULTI_THRESHOLD";
        public const string COLOR_MAP_MULTI_LINEAR_GRADIENT = "COLOR_MAP_MULTI_LINEAR_GRADIENT";
        public const string PREFILTER_TIMELINE = "PREFILTER_TIMELINE";
        public const string PREFILTER_MAXIMUM = "PREFILTER_MAXIMUM";
        public const string PREFILTER_MINIMUM = "PREFILTER_MINIMUM";
        public const string PREFILTER_AVERAGE = "PREFILTER_AVERAGE";
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
        public const string SIMNETWORK_SECTION = "SIMNETWORKS";       // custom
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
        public const string USERCOMPONENTLIST_SECTION = "USER_COMPONENT_LISTS"; // for saving the version information
        public const string SECTION_START = "SECTION";          // DXF specs
        public const string SECTION_END = "ENDSEC";             // DXF specs
        public const string SEQUENCE_END = "SEQEND";            // DXF specs
        public const string EOF = "EOF";                        // DXF specs

        public const string ENTITY_SEQUENCE = "ENTSEQ";         // custom
        public const string ENTITY_CONTINUE = "ENTCTN";         // custom

        public const string COLOR_IN_BYTES = "BYTE_COLOR";      // for saving colors with byte-color components

        // public const string DOUBLE_NAN = "NaN";                 // custom
        public const int LIST_CONTINUE = 1;                   // custom
        public const int LIST_END = -1;                      // custom
        public const int END_OF_SUBLIST = -11;                  // custom
        public const string DELIMITER_WITHIN_ENTRY = "_|_";
    }
}
