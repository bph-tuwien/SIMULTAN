﻿using SIMULTAN.Data.Components;
using SIMULTAN.Data.SimNetworks;
using static SIMULTAN.Serializer.CODXF.ComponentDxfIOComponents;

namespace SIMULTAN.Serializer.DXF
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

    // [0    - 1000]: DXF Specs and general custom codes
    // [1001 - 1100]: Parameter
    // [1101 - 1300]: MultiValue
    // [1301 - 1400]: Calculation
    // [1401 - 1500]: Component
    //      [[1421 - 1430]]: Component -> AccessTracker
    //      [[1433 - 1440]]: Component -> AccessProfile, no longer used
    //      [[1451 - 1460]]: TypedComponent
    //      [[1461 - ....]]: Additional Component save codes
    // [1501 - 1600]: FlowNetwork
    // [1601 - 1700]: GeometicRelationship
    // [1701 - 1800]: Mapping2Component
    // [5001 - 5050]: Important Parameter visualizations
    // [6001 - 7000]: Excel Mapping
    // [7001 - 8000]: Assets
    // [8001 - 9000]: ChatItmes
    // [9001 - 10000]: Users
    // [20001 - 21000]: HierarchicalProjects
    // [21001 - 22000]: UserComponentList
    // [22001 - 23000]: Taxonomies
    // [23001 - 24000]: Geometry Relations

    /// <summary>
    /// Save codes for storing <see cref="SimNetwork"/> instances
    /// </summary>
    public enum SimNetworkSaveCode
    {
        BLOCKS = 9001,
        POSITION_X = 9503,
        POSITION_Y = 9504,
        PORTS = 9505,
        PORT_TYPE = 9506,
        SUBNETWORKS = 9509,
        SOURCE_PORT = 9511,
        TARGET_PORT = 9512,
        CONNECTORS = 9513,
        GEOM_REP_FILE_KEY = 9514,
        GEOM_REP_GEOM_ID = 9515,
        GEOM_REP_INDEX = 9516,
        SIMBLOCK_ISSTATIC = 9517,
        COLOR = 9518,
        WIDTH = 9519,
        HEIGHT = 9520,
        CONTROL_POINTS = 9521,
    }

    /// <summary>
    /// Save Codes for storing Components and Instances
    /// </summary>
    public enum ComponentSaveCode
    {
        NAME = 1401,
        DESCRIPTION = 1402,
        CATEGORY = 1403,                    // as string
        MAIN_SLOT = 1406,

        CONTAINED_COMPONENTS = 1407,        // saved as DXF Entities
        CONTAINED_COMPONENT_SLOTS = 1408,   // saved as a sequence of strings
        REFERENCED_COMPONENTS = 1409,       // saved as pairs of STRING and LONG
        CONTAINED_PARAMETERS = 1410,        // saved as DXF Entities
        CONTAINED_CALCULATIONS = 1411,      // saved as DXF Entities

        INSTANCES = 1414,   // saved as DXF Entities
        INSTANCE_TYPE = 1428,               // InstanceType
        GENERATED_AUTOMATICALLY = 1415,     // from exchange with the GeometryViewer
        CALCULATOR_MAPPINGS = 1416,      // for deferred calculations
        MAPPINGS_TO_EXCEL_TOOLS = 1418,

        VISIBILTY = 1419,                   // in projects
        COLOR = 1420,                       // for the new interface
        SORTING_TYPE = 1421,                 // for the sub-components
        SLOTS = 1422,
        ACCESS_STATE = 1431,
        PROFILE = 1432,

        SLOT_TAXONOMY_ENTRY_ID = 1461,
        SLOT_TAXONOMY_ENTRY_PROJECT_ID = 1462,


        //Unused SaveCodes (which were used in old versions)

        [SaveCodeNotInUse("saved as DXF Entities within a DXF entity")]
        ACCESS_RECORD = 1404,               // NOT USED -> saved as DXF Entities within a DXF entity
        [SaveCodeNotInUse("Only FUNCTION_SLOT_CURRENT is in use")]
        FUNCTION_SLOTS_ALL = 1405,          // as a sequence of strings
        [SaveCodeNotInUse]
        TIME_STAMP = 1412,
        [SaveCodeNotInUse]
        SYMBOL_ID = 1413,
        [SaveCodeNotInUse]
        REFERENCES_INTACT = 1417,           // in case references were removed and the user with writing access does not accept or agree


    }

    /// <summary>
    /// [[1451 - 1460]]: TypedComponent
    /// Not used anymore
    /// </summary>
    public enum TypedComponentSaveCode
    {
        [SaveCodeNotInUse]
        TYPE_NAME = 1451,
        [SaveCodeNotInUse]
        TYPE_VALIDATOR_NAME = 1452
    }

    /// <summary>
    /// Save Codes to store <see cref="SimAccessProfileEntry"/>
    /// </summary>
    public enum ComponentAccessTrackerSaveCode
    {
        FLAGS = 1421,
        WRITE_LAST = 1423,
        SUPERVIZE_LAST = 1425,
        RELEASE_LAST = 1427,

        //Unused Save Codes

        [SaveCodeNotInUse]
        WRITE_PREV = 1422,
        [SaveCodeNotInUse]
        SUPERVIZE_PREV = 1424,
        [SaveCodeNotInUse]
        RELEASE_PREV = 1426,
    }

    /// <summary>
    /// Save Codes to store <see cref="SimAccessProfile"/>
    /// All values are unused
    /// </summary>
    public enum ComponentAccessProfileSaveCode
    {
        [SaveCodeNotInUse]
        MANAGER = 1433,

        [SaveCodeNotInUse]
        ACTION_TARGET_ID = 1434,
        [SaveCodeNotInUse]
        ACTION_SUPERVIZE = 1435,
        [SaveCodeNotInUse]
        ACTION_RELEASE = 1436,
        [SaveCodeNotInUse]
        ACTION_TIME_STAMP = 1437,
        [SaveCodeNotInUse]
        ACTION_ACTOR = 1438,
    }

    /// <summary>
    /// Save Codes to store <see cref="SimBaseParameter"/>
    /// [1001 - 1100]
    /// </summary>
    public enum ParameterSaveCode
    {


        NAME = 1001,
        UNIT = 1002,
        CATEGORY = 1003,
        PROPAGATION = 1004,
        INSTANCE_PROPAGATION = 1014,

        VALUE_MIN = 1005,
        VALUE_MAX = 1006,
        VALUE_CURRENT = 1007,
        VALUE_FIELD_REF = 1009,
        VALUE_TEXT = 1010,


        ALLOWED_OPERATIONS = 1012,
        IS_AUTOGENERATED = 1013,

        /// <summary>
        ///  For returning the current selected value of the EnumParam
        /// <see cref="SimEnumParameter.Value"/>
        /// </summary>
        ENUMPARAM_TAXONOMYENTRY_VALUE_LOCALID = 1030,
        /// <summary>
        /// / For returning the current selected value of the EnumParam
        /// <see cref="SimEnumParameter.Value"/>
        /// </summary>
        ENUMPARAM_TAXOMYENTRY_VALUE_GLOBALID = 1031,
        /// <summary>
        /// To retrieve the Taxonomy providing the possible values of the enum
        /// <see cref="SimEnumParameter.ParentTaxonomyEntryRef"/>
        /// </summary>
        ENUMPARAM_PARENT_TAXONOMYENTRY_LOCALID = 1032,
        /// <summary>
        /// To retrieve the Taxonomy providing the possible values of the enum
        /// <see cref="SimEnumParameter.ParentTaxonomyEntryRef"/>
        /// </summary>
        ENUMPARAM_PARENT_TAXONOMYENTRY_GLOBALID = 1033,
        /// <summary>
        /// Type of the parameter <see cref="ParameterType"/>  
        /// </summary>
        PARAMTYPE = 1034,

        TAXONOMY_PROJECT_ID = 1015, // global id of taxonomy entry
        [SaveCodeNotInUse] // was unnecessary for restoring taxonomy entry references
        TAXONOMY_ID = 1016,
        TAXONOMY_ENTRY_ID = 1017,


        [SaveCodeNotInUse]
        IS_WITHIN_BOUNDS = 1008,
        [SaveCodeNotInUse]
        IS_IMPORTANT = 1011, // for the user



    }

    /// <summary>
    /// Save Codes for storing MultiValues
    /// [1101 - 1300]
    /// </summary>
    public enum MultiValueSaveCode
    {
        MV_TYPE = 1101,
        MV_CANINTERPOLATE = 1102,
        MV_NAME = 1103,

        MVDisplayVector_CELL_INDEX_X = 1201, //Old
        MVDisplayVector_CELL_INDEX_Y = 1202, //Old
        MVDisplayVector_CELL_INDEX_Z = 1203, //Old

        MVSRC_GRAPHNAME = 1214,
        MVSRC_AXIS_X = 1215,
        MVSRC_AXIS_Y = 1216,
        MVSRC_AXIS_Z = 1217,
        MVSRC_LOCALID = 1218,
        MVSRC_GLOBALID = 1219,
        GEOSRC_PROPERTY = 1220,
        GEOSRC_FILTER = 1221,
        GEOSRC_FILTER_ENTRY_GLOBAL_ID = 1222,
        GEOSRC_FILTER_ENTRY_LOCAL_ID = 1223,

        MV_UNIT_X = 1104,
        MV_UNIT_Y = 1105,
        MV_UNIT_Z = 1106,

        MV_NRX = 1107,
        MV_MIN_X = 1108,
        MV_MAX_X = 1109,

        MV_NRY = 1110,
        MV_MIN_Y = 1111,
        MV_MAX_Y = 1112,

        MV_NRZ = 1113,
        MV_MIN_Z = 1114,
        MV_MAX_Z = 1115,

        MV_XAXIS = 1116,
        MV_COL_NAMES = 1116,
        MV_YAXIS = 1117,
        MV_COL_UNITS = 1117,
        MV_ZAXIS = 1118,

        MVDATA_ROW_COUNT = 1119,
        MVDATA_COLUMN_COUNT = 901,
        MV_ROW_NAMES = 1120,

        ADDITIONAL_INFO = 1121,
        MV_DATA = 1122,

        MV_ROW_UNITS = 1123,



        [SaveCodeNotInUse]
        MVDisplayVector_NUMDIM = 1200,
        [SaveCodeNotInUse]
        MVDisplayVector_CELL_INDEX_W = 1204, //Old
        [SaveCodeNotInUse]
        MVDisplayVector_POS_IN_CELL_REL_X = 1205, //Old
        [SaveCodeNotInUse]
        MVDisplayVector_POS_IN_CELL_REL_Y = 1206, //Old
        [SaveCodeNotInUse]
        MVDisplayVector_POS_IN_CELL_REL_Z = 1207, //Old
        [SaveCodeNotInUse]
        MVDisplayVector_POS_IN_CELL_ABS_X = 1208, //Old
        [SaveCodeNotInUse]
        MVDisplayVector_POS_IN_CELL_ABS_Y = 1209, //Old
        [SaveCodeNotInUse]
        MVDisplayVector_POS_IN_CELL_ABS_Z = 1210, //Old
        [SaveCodeNotInUse]
        MVDisplayVector_VALUE = 1211, //Old
        [SaveCodeNotInUse]
        MVDisplayVector_CELL_SIZE_W = 1212, //Old
        [SaveCodeNotInUse]
        MVDisplayVector_CELL_SIZE_H = 1213, //Old
    }

    /// <summary>
    /// Save Codes for storing <see cref="SimCalculation"/>
    /// [1301 - 1400]
    /// </summary>
    public enum CalculationSaveCode
    {
        NAME = 1301,
        EXPRESSION = 1302,
        PARAMS_INPUT = 1303,
        PARAMS_OUTPUT = 1304,
        VECTOR_CALC_OPERATIONS = 1307,
        VECTOR_CALC_RANGES = 1308,
        VECTOR_CALC_ITERATION_COUNT = 1309,
        VECTOR_CALC_AGGREGATION = 1310,
        VECTOR_CALC_RANDOM = 1311,
        VECTOR_CALC_OVERRIDE = 1312,


        [SaveCodeNotInUse("Removed during refactoring since calculations do not store parameter names anymore")]
        LOST_REFS = 1305,
        [SaveCodeNotInUse("Removed during refactoring since calculations do not store parameter names anymore")]
        LOST_REFS_MSG = 1306,
    }

    /// <summary>
    /// Save Codes for storing FlowNetworks
    /// [1501 - 1600]
    /// </summary>
    public enum FlowNetworkSaveCode
    {
        IS_VALID = 1502,
        POSITION_X = 1503,
        POSITION_Y = 1504,
        START_NODE_LOCALID = 1505,      // only SimFlowNetworkEdge: id of node
        START_NODE_GLOBALID = 1535,
        END_NODE_LOCALID = 1506,        // only SimFlowNetworkEdge: id of node
        END_NODE_GLOBALID = 1536,
        NAME = 1507,
        DESCRIPTION = 1508,
        MANAGER = 1509,

        CONTAINED_NODES = 1511, // only FlowNetwork: saved as DXF Entities
        CONTAINED_EDGES = 1512, // only FlowNetwork: saved as DXF Entities
        CONTAINED_NETWORKS = 1513,  // only FlowNetwork: saved as DXF Entities
        NODE_SOURCE = 1514,     // only FlowNetwork (id of the start node)
        NODE_SINK = 1515,       // only FlowNetwork (id of the end node)
        CALC_RULES = 1516,
        CALC_RULE_SUFFIX_OPERANDS = 1517,
        CALC_RULE_SUFFIX_RESULT = 1518,
        CALC_RULE_DIRECTION = 1519,
        CALC_RULE_OPERATOR = 1520,
        GEOM_REP = 1521,        // only FlowNetwork (resource index of the geometry file representing the NW)
        GEOM_REP_FILE_KEY = 1531, // SimFlowNetworkElement: representation file index
        GEOM_REP_GEOM_ID = 1532,     // SimFlowNetworkElement: representation geometry object id
        IS_DIRECTED = 1533,      // only FlowNetwork: indicates if the direction of the edges should be taken into account


        [SaveCodeNotInUse]
        CONTENT_ID = 1501,      // local id of associated component, otherwise -1
        [SaveCodeNotInUse]
        TIMESTAMP = 1510,
        [SaveCodeNotInUse]
        CONTENT_LOCATION = 1534,  // global id of associated component, otherwise Guid.Empty
    }

    /// <summary>
    /// Save Codes for storing Instances
    /// [1601 - 1700]
    /// </summary>
    public enum ComponentInstanceSaveCode
    {
        NAME = 1601,
        INSTANCE_TYPE = 1602,
        STATE_ISREALIZED = 1609,
        INST_ROTATION = 1619,
        INST_SIZE = 1620,
        INST_NETWORKELEMENT_ID = 1621,

        INST_PATH = 1623,
        INST_SIZE_TRANSFERSETTINGS = 1640,
        INST_SIZE_TS_SOURCE = 1641,
        INST_SIZE_TS_INITVAL = 1642,
        INST_SIZE_TS_PARAMETER_LOCALID = 1643,
        INST_SIZE_TS_PARAMETER_GLOBALID = 1645,
        INST_SIZE_TS_ADDEND = 1644,
        INST_PARAMS = 1650,
        INST_PARAM_ID = 1651,
        INST_PARAM_VAL = 1652,

        GEOM_REF_FILE = 1663,
        GEOM_REF_ID = 1664,
        STATE_CONNECTION_STATE = 1665,
        INST_NETWORKELEMENT_LOCATION = 1666,
        INST_PROPAGATE_PARAM_CHANGES = 1667,
        INST_PLACEMENTS = 1670,
        PLACEMENT_STATE = 1672,
        PLACEMENT_INSTANCE_TYPE = 1673,


        [SaveCodeNotInUse]
        GEOM_IDS_X = 1603,
        [SaveCodeNotInUse]
        GEOM_IDS_Y = 1604,
        [SaveCodeNotInUse]
        GEOM_IDS_Z = 1605,
        [SaveCodeNotInUse]
        GEOM_IDS_W = 1630,
        [SaveCodeNotInUse]
        GEOM_CS = 1606,
        [SaveCodeNotInUse]
        TRANSF_WC2LC = 1607,
        [SaveCodeNotInUse]
        TRANSF_LC2WC = 1608,
        [SaveCodeNotInUse]
        INST_NWE_NAME = 1622,
        [SaveCodeNotInUse]
        INST_REFS = 1660,
        [SaveCodeNotInUse]
        INST_REFS_KEY = 1661,
        [SaveCodeNotInUse]
        INST_REFS_VAL = 1662,
        [SaveCodeNotInUse]
        INST_SIMNWE_NAME = 1668,
        [SaveCodeNotInUse]
        INST_SIMNWE_LOCATION = 1671,
    }

    /// <summary>
    /// Save Codes for storing Calculator Mappings
    /// </summary>
    public enum CalculatorMappingSaveCode
    {
        NAME = 1701,
        CALCULATOR_LOCALID = 1702,
        CALCULATOR_GLOBALID = 1709,
        INPUT_MAPPING = 1703,
        INPUT_DATAPARAMETER_LOCALID = 1704,
        INPUT_DATAPARAMETER_GLOBALID = 1710,
        INPUT_CALCULATORPARAMETER_LOCALID = 1705,
        INPUT_CALCULATORPARAMETER_GLOBALID = 1711,
        OUTPUT_MAPPING = 1706,
        OUTPUT_DATAPARAMETER_LOCALID = 1707,
        OUTPUT_DATAPARAMETER_GLOBALID = 1712,
        OUTPUT_CALCULATORPARAMETER_LOCALID = 1708,
        OUTPUT_CALCULATORPARAMETER_GLOBALID = 1713,
    }

    /// <summary>
    /// Save Codes for storing meta information.
    /// All Values are unused
    /// </summary>
    public enum ComponentFileMetaInfoSaveCode
    {
        [SaveCodeNotInUse]
        MAX_CALCULATION_ID = 1801,
    }

    /// <summary>
    /// Save Codes for saving important parameters.
    /// All Values are unused
    /// [5001 - 5050]
    /// </summary>
    public enum ImportantParamVisSaveCode
    {
        [SaveCodeNotInUse]
        ID = 5001,
        [SaveCodeNotInUse]
        POSITION_X = 5002,
        [SaveCodeNotInUse]
        POSITION_Y = 5003,
        [SaveCodeNotInUse]
        COLOR_R = 5004,
        [SaveCodeNotInUse]
        COLOR_G = 5005,
        [SaveCodeNotInUse]
        COLOR_B = 5006,
        [SaveCodeNotInUse]
        COLOR_A = 5007,
        [SaveCodeNotInUse]
        INDEX = 5008
    }

    /// <summary>
    /// Save Code for storing ExcelMappings
    /// [6001 - 7000]: Excel Mapping
    /// </summary>
    public enum DataMappingSaveCode
    {
        RULE_NAME = 6001,
        RULE_SHEETNAME = 6002,
        RULE_OFFSETPARENT_X = 6003,
        RULE_OFFSETPARENT_Y = 6004,
        RULE_SUBJECT = 6005,
        RULE_PROPERTIES = 6006,
        RULE_MAPPINGRANGE = 6007,
        RULE_ORDER_HORIZONTALLY = 6008,
        RULE_PREPEND_CONTENT_TO_CHILDREN = 6009,
        RULE_FILTER = 6010,
        RULE_OFFSETCONSECUTIVE_X = 6011,
        RULE_OFFSETCONSECUTIVE_Y = 6012,
        RULE_CHILDREN = 6013,
        RULE_MAPPED_COMPONENTS = 6020,
        RULE_PARAMETER_GLOBALID = 6021,
        RULE_PARAMETER_LOCALID = 6022,
        RULE_RANGE_COLUMNSTART = 6023,
        RULE_RANGE_ROWSTART = 6024,
        RULE_RANGE_COLUMNCOUNT = 6025,
        RULE_RANGE_ROWCOUNT = 6026,



        RULE_DIRECTION = 6014,
        RULE_REFERENCEPOSITIONPARENT = 6015,
        RULE_FILTER_PROPERTY = 6016,
        RULE_FILTER_TYPE = 6017,
        RULE_FILTER_VALUE = 6018,
        RULE_FILTER_VALUE2 = 6019,
        RULE_REFERENCEPOSITIONCONSECUTIVE = 6027,

        TOOL_NAME = 6101,
        TOOL_MACRO_NAME = 6102,
        TOOL_MAPPINGRULES = 6103,
        TOOL_OUTPUTRANGERULES = 6105,
        TOOL_OUTPUTRULES = 6106,

        MAP_PATH = 6201,
        MAP_TOOL_NAME = 6202,
        MAP_RULE_NAME = 6204,
        MAP_RULE_INDEX = 6205,

        DATA_MAP_SHEET_NAME = 6501,
        DATA_MAP_RANGE_X = 6502,
        DATA_MAP_RANGE_Y = 6503,
        DATA_MAP_RANGE_Z = 6504,
        DATA_MAP_RANGE_W = 6505,
        DATA_MAP_TYPE = 6506,

        UNMAP_FILTER_SWITCH = 6521,
        UNMAP_TARGET_COMP_ID = 6522,
        UNMAP_TARGET_PARAM = 6523,
        UNMAP_FILTER_COMP = 6524,
        UNMAP_FILTER_PARAM = 6525,
        UNMAP_PARAM_POINTER_X = 6526,
        UNMAP_PARAM_POINTER_Y = 6527,
        UNMAP_DATA = 6528,
        UNMAP_TARGET_COMP_LOCATION = 6529,

        RULE_MAXMATCHES = 6601,
        RULE_MAXDEPTH = 6602,
        RULE_TRAVERSE_STRATEGY = 6603,
        TRAVERSAL_ACTIVATED = 6604,

        VERSION = 6701,

        [SaveCodeNotInUse]
        TOOL_LAST_PATH_TO_FILE = 6104,
        [SaveCodeNotInUse]
        MAP_TOOL_FILE_PATH = 6203,
        [SaveCodeNotInUse]
        MAP_COL_OWNER_ID = 6301,
        [SaveCodeNotInUse]
        MAP_COL_OWNER_MANAGER = 6302,
        [SaveCodeNotInUse]
        MAP_COL_RULES = 6303,
        [SaveCodeNotInUse]
        MAP_COL_TODO = 6304,
    }

    /// <summary>
    /// Mixes resource and asset save codes. Does not handle resources as objects.
    /// </summary>
    public enum AssetSaveCode
    {
        RESOURCE_KEY = 7001,
        CONTENT = 7002,
        REFERENCE_COL = 7003,
        REFERENCE_LOCALID = 7004,
        REFERENCE_GLOBALID = 7005,

        /// <summary>
        /// The number of top-level resources contained in the asset manager.
        /// </summary>
        APATH_COLLECTION = 7101,
        /// <summary>
        /// The start of the asset collection. Indicates their number.
        /// </summary>
        ASSET_COLLECTION = 7111,

        [SaveCodeNotInUse]
        WORKING_DIR = 7090,
        [SaveCodeNotInUse]
        WORKING_PATHS = 7091,
        [SaveCodeNotInUse]
        APATH_USER = 7102,
        [SaveCodeNotInUse]
        APATH_REL_PATH = 7104,
        [SaveCodeNotInUse]
        APATH_KEY = 7103,
        [SaveCodeNotInUse]
        APATH_ISCONTAINED = 7105,
        [SaveCodeNotInUse]
        APATH_FULL_PATH = 7106,
        [SaveCodeNotInUse]
        APATHS_AS_OBJECTS = 7107,
    }

    /// <summary>
    /// Strictly for resource serialization as objects. Assets not included.
    /// </summary>
    public enum ResourceSaveCode
    {
        RESOURCE_USER = 7201,
        RESOURCE_KEY = 7202,
        RESOURCE_RELATIVE_PATH = 7204,
        RESOURCE_CHILDREN = 7207,
        RESOURCE_VISIBILITY = 7212,
        RESOURCE_TAGS = 7213,
        RESOURCE_TAGS_ENTRY_GLOBAL_ID = 7214,
        RESOURCE_TAGS_ENTRY_LOCAL_ID = 7215,

        [SaveCodeNotInUse]
        RESOURCE_NAME = 7203,
        [SaveCodeNotInUse]
        RESOURCE_ANCHOR = 7205,
        [SaveCodeNotInUse]
        RESOURCE_FULL_PATH = 7206,
        [SaveCodeNotInUse]
        RESOURCE_PROBLEM = 7208,
        [SaveCodeNotInUse]
        RESOURCE_HAS_PARENT = 7211,
    }

    /// <summary>
    /// Save Codes for Chat Items
    /// </summary>
    public enum ChatItemSaveCode
    {
        TYPE = 8001,
        AUTHOR = 8002,
        VR_ADDRESS = 8003,
        VR_PASSWORD = 8004,
        GIT_COMMIT = 8005,
        TIMESTAMP = 8006,
        MESSAGE = 8007,
        STATE = 8008,
        EXPECTED_REACTIONS_FROM = 8009,
        CHILDREN = 8010,

        CONVERSATION = 8101
    }

    /// <summary>
    /// Save Codes for storing Users
    /// </summary>
    public enum UserSaveCode
    {
        USER_ID = 9001,
        USER_NAME = 9002,
        USER_PSW_HASH = 9003,
        USER_ROLE = 9004,
        USER_ENCRYPTION_KEY = 9005,
    }

    /// <summary>
    /// Save Codes for storing GeoMaps
    /// </summary>
    public enum GeoMapSaveCode
    {
        MAP_PATH = 10001,
        GEOREFS = 10002,
        IMAGEPOS_X = 10003,
        IMAGEPOS_Y = 10004,
        LONGITUDE = 10005,
        LATITUDE = 10006,
        HEIGHT = 10007,
        MAP_PROJECT_ID = 10008,
        MAP_RESOURCE_ID = 10009,
    }

    /// <summary>
    /// Save Codes for storing SitePlanner
    /// </summary>
    public enum SitePlannerSaveCode
    {
        GEOMAPS = 11001,
        GEOMAP_PATH = 11002,
        BUILDINGS = 11003,
        BUILDING_INDEX = 11004,
        BUILDING_GEOMETRYMODEL_PATH = 11005,
        BUILDING_CUSTOM_COLOR = 11006,
        BUILDING_ID = 11007,
        ELEVATION_PROVIDER_TYPE = 11008,
        GRID_CELL_SIZE = 11009,
        VALUE_MAPPING_INDEX_USAGE = 11010,
        VALUE_MAPPING_COLOR_MAP_TYPE = 11011,
        VALUE_MAPPING_COLOR_MAP_PARAMS = 11012,
        VALUE_MAPPING_PREFILTER_TYPE = 11013,
        VALUE_MAPPING_PREFILTER_PARAMS = 11014,
        VALUE_MAPPING_VALUE_TABLE_KEY = 11015,
        VALUE_MAPPING_ASSOCIATIONS = 11016,
        VALUE_MAPPING_ASSOCIATION_INDEX = 11017,
        VALUE_MAPPING_ASSOCIATION_NAME = 11018,
        VALUE_MAPPING_ACTIVE_ASSOCIATION_INDEX = 11019,
        VALUE_MAPPING_VALUE_TABLE_LOCATION = 11020,
        GEOMAP_PROJECT_ID = 11021,
        GEOMAP_RESOURCE_ID = 11022,
        BUILDING_GEOMETRYMODEL_PROJECT_ID = 11023,
        BUILDING_GEOMETRYMODEL_RESOURCE_ID = 11024,
        VALUE_MAPPING_COLOR_MAP = 11025,
        VALUE_MAPPING_PREFILTER = 11026,
        VALUE_MAPPING_COLOR_MAP_MARKER = 11027,
        VALUE_MAPPING_COLOR_MAP_MARKER_VALUE = 11028,
        VALUE_MAPPING_COLOR_MAP_MARKER_COLOR = 11029,
        VALUE_MAPPING_PREFILTER_TIMELINE_CURRENT = 11030,
        VALUE_MAPPING_GLOBAL_ID = 11031,
        VALUE_MAPPING_LOCAL_ID = 11032,
    }

    /// <summary>
    /// Save Codes for storing ValueMappings
    /// </summary>
    public enum ValueMappingSaveCode
    {
        VALUE_MAPPING_NAME = 11018,
        VALUE_MAPPING_INDEX_USAGE = 11010,
        VALUE_MAPPING_TABLE_LOCALID = 11015,
        VALUE_MAPPING_TABLE_GLOBALID = 11020,
        VALUE_MAPPING_COLOR_MAP = 11025,
        VALUE_MAPPING_PREFILTER = 11026,
        VALUE_MAPPING_COLOR_MAP_MARKER = 11027,
        VALUE_MAPPING_COLOR_MAP_MARKER_VALUE = 11028,
        VALUE_MAPPING_COLOR_MAP_MARKER_COLOR = 11029,
    }

    /// <summary>
    /// Save Codes for project related data
    /// </summary>
    public enum ProjectSaveCode
    {
        PROJECT_ID = 200001,
        NR_OF_CHILD_PROJECTS = 200004,


        [SaveCodeNotInUse]
        NAME_OF_PUBLIC_VALUE_FILE = 200002,
        [SaveCodeNotInUse]
        NAME_OF_PUBLIC_COMPS_FILE = 200003,
        [SaveCodeNotInUse]
        CHILD_PROJECT_ID = 200005,
        [SaveCodeNotInUse]
        CHILD_PROJECT_REL_PATH = 200006
    }

    /// <summary>
    /// Save Codes for User Component Lists
    /// [21001 - 22000]: UserComponentList
    /// </summary>
    public enum UserComponentListSaveCode
    {
        NAME = 21001,
        ROOT_COMPONENTS = 21002
    }

    // [22001 - 23000]: Taxonomies
    public enum TaxonomySaveCode
    {
        TAXONOMY_ENTRIES = 22001,
        TAXONOMY_DESCRIPTION = 22002,
        TAXONOMY_ENTRY_KEY = 22003,
        TAXONOMY_KEY = 22004,
        TAXONOMY_IS_READONLY = 22005,
        TAXONOMY_IS_DELETABLE = 22006,

        TAXONOMY_SUPPORTED_LANGUAGES = 22100,
        TAXONOMY_LANGUAGE = 22101,

        TAXONOMY_LOCALIZATIONS = 22102,
        TAXONOMY_LOCALIZATION_CULTURE = 22103,
        TAXONOMY_LOCALIZATION_NAME = 22104,
        TAXONOMY_LOCALIZATION_DESCRIPTION = 22105,
    }

    // [23001 - 24000]: Geometry Relations
    public enum GeometryRelationSaveCode
    {
        GEOMETRY_RELATION_TYPE_GLOBAL_ID = 23001,
        GEOMETRY_RELATION_TYPE_LOCAL_ID = 23002,
        GEOMETRY_RELATION_SOURCE_PROJECT_ID = 23003,
        GEOMETRY_RELATION_SOURCE_FILE_ID = 23004,
        GEOMETRY_RELATION_SOURCE_GEOMETRY_ID = 23005,
        GEOMETRY_RELATION_TARGET_PROJECT_ID = 23006,
        GEOMETRY_RELATION_TARGET_FILE_ID = 23007,
        GEOMETRY_RELATION_TARGET_GEOMETRY_ID = 23008,
        GEOMETRY_RELATION_IS_AUTOGENERATED = 23009,

        // for the mapping of relation file ids to paths for the export
        GEOMETRY_RELATION_FILE_MAPPINGS = 23101,
        GEOMETRY_RELATION_FILE_MAPPING_FILE_ID = 23102,
        GEOMETRY_RELATION_FILE_MAPPING_PATH = 23103,
    }

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
