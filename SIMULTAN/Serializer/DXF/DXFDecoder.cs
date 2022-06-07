using SIMULTAN.Data.Components;
using SIMULTAN.Data.FlowNetworks;
using SIMULTAN.Data.MultiValues;
using SIMULTAN.Projects;
using SIMULTAN.Serializer.DXF.DXFEntities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace SIMULTAN.Serializer.DXF
{
    public enum DXFDecoderMode
    {
        Unknown, Components, MultiValue, ExcelTools, Parameters
    }

    /// <summary>
    /// The base class for all DXF decoders. Manages the parsing of values, parameters, networks, assets and chat items.
    /// Is used in distributed parsing.
    /// </summary>
    public class DXFDecoder
    {
        /// <summary>
        /// Indicates that the parsed file has no version entry.
        /// </summary>
        public const ulong NO_FILE_VERSION = 0;
        /// <summary>
        /// The last (most recent) version of the component model.
        /// </summary>
        /// <remarks>
        /// 26.05.2020: switched from 1 to 2
        /// 03.08.2020: switched from 2 to 3
        /// 05.03.2021: switched from 3 to 4
        /// 12.05.2021: switched from 4 to 5
        /// 14.05.2021: version 6
        /// 08.06.2021: version 7 (instance parameters)
        /// 15.06.2021: version 8 (transferable sizes)
        /// 15.12.2021: version 9 (components now have unique ids too)
        /// 13.04.2022: version 11 (save PropagateParameterInstance of instances)
        /// </remarks>
        public const ulong CurrentFileFormatVersion = 11L;

        public bool HasValidData { get; private set; } = false;

        #region CLASS MEMBERS
        public StreamReader FStream { get; private set; }
        public NumberFormatInfo N { get; private set; }
        public string FValue { get; private set; }
        public int FCode { get; private set; }

        internal DXFSection FMainSect { get; set; }   // only for parsing purposes (contains nothing)
        internal DXFSection FEntities { get; set; }   // contains the parsed entities

        public ProjectData ProjectData { get; }

        // separate loading of chats
        internal bool AttachChatToComponent { get; set; } = true;
        internal Dictionary<long, SimChat> for_ChatMerging { get; private set; }

        // temporary loading w/o accompanying resource files
        internal bool CheckForResourceExistence { get; } = true;

        /// <summary>
        /// The parsed version for the current file. It is set per default to NO_FILE_VERSION.
        /// </summary>
        public ulong CurrentFileVersion { get; internal set; } = DXFDecoder.NO_FILE_VERSION;

        public DXFDecoderMode DecoderMode { get; } = DXFDecoderMode.Unknown;

        #endregion

        #region .CTOR

        protected DXFDecoder()
        {
            this.N = new NumberFormatInfo();
            N.NumberDecimalSeparator = ".";
            N.NumberGroupSeparator = " ";
        }

        public DXFDecoder(ProjectData projectData, DXFDecoderMode mode, bool _check_for_resource_existence = true) : this()
        {
            if (projectData == null)
                throw new ArgumentNullException(nameof(projectData));

            this.CheckForResourceExistence = _check_for_resource_existence;
            this.ProjectData = projectData;
            this.DecoderMode = mode;

            switch (mode)
            {
                case DXFDecoderMode.Components:
                    {
                        this.ProjectData.AssetManager.Reset();

                        //Reset calculation ids
                        var removeKeys = IdTranslation.Keys.Where(x => x.Item1 == typeof(SimCalculation)).ToList();
                        removeKeys.ForEach(x => IdTranslation.Remove(x));
                        removeKeys = IdTranslation.Keys.Where(x => x.Item1 == typeof(SimParameter)).ToList();
                        removeKeys.ForEach(x => IdTranslation.Remove(x));

                        ParameterCount = 0;
                        CalculationCount = 0;
                    }
                    break;
                case DXFDecoderMode.MultiValue:
                    {
                        //Reset MultiValues
                        var removeKeys = IdTranslation.Keys.Where(x => x.Item1 == typeof(SimMultiValue)).ToList();
                        removeKeys.ForEach(x => IdTranslation.Remove(x));
                        MultiValueCount = 0;
                    }
                    break;
            }
        }

        #endregion

        #region METHODS: Decoding

        internal virtual DXFEntity CreateEntity()
        {
            //Console.WriteLine(this.FValue);
            DXFEntity E;
            switch (this.FValue)
            {
                case ParamStructTypes.SECTION_END:
                case ParamStructTypes.SEQUENCE_END:
                    return null;
                case ParamStructTypes.SECTION_START:
                    E = new DXFSection();
                    break;
                case ParamStructTypes.ENTITY_SEQUENCE:
                    E = new DXFComponentSubContainer(); // helper for COMPONENT, FLOWNETWORK, EXCEL_TOOL, EXCEL_RULE
                    break;
                case ParamStructTypes.ENTITY_CONTINUE:
                    E = new DXFContinue(); // helper for COMPONENT, FLOWNETWORK, EXCEL_TOOL, EXCEL RULE
                    break;
                case ParamStructTypes.COMPONENT:
                    E = new DXFComponent();
                    break;
                case ParamStructTypes.USER_LIST:
                    E = new DXFUserComponentList();
                    break;
                case ParamStructTypes.ACCESS_PROFILE:
                    E = new DXFAccessProfile();
                    break;
                case ParamStructTypes.ACCESS_TRACKER:
                    E = new DXFAccessTracker();
                    break;
                case ParamStructTypes.CALCULATION:
                    E = new DXFCalculation();
                    break;
                case ParamStructTypes.PARAMETER:
                    E = new DXFParameter();
                    break;
                case ParamStructTypes.VALUE_FIELD:
                    E = new DXFMultiValueField3D();
                    break;
                case ParamStructTypes.FUNCTION_FIELD:
                    E = new DXFMultiValueFunction();
                    break;
                case ParamStructTypes.BIG_TABLE:
                    E = new DXFMultiValueBigTable();
                    break;
                case ParamStructTypes.FLOWNETWORK:
                    E = new DXF_FlowNetwork();
                    break;
                case ParamStructTypes.FLOWNETWORK_NODE:
                    E = new DXF_FlNetNode();
                    break;
                case ParamStructTypes.FLOWNETWORK_EDGE:
                    E = new DXF_FlNetEdge();
                    break;
                case ParamStructTypes.GEOM_RELATION:
                    E = new DXFComponentInstance();
                    break;
                case ParamStructTypes.MAPPING_TO_COMP:
                    E = new DXFMapping2Component();
                    break;
                case ParamStructTypes.EXCEL_RULE:
                    E = new DXFExcelMappingNode();
                    break;
                case ParamStructTypes.EXCEL_TOOL:
                    E = new DXFExcelTool();
                    break;
                case ParamStructTypes.EXCEL_MAPPING:
                    E = new DXFComponent2ExcelMappingRule();
                    break;
                case ParamStructTypes.EXCEL_UNMAPPING:
                    E = new DXFExcelUnmappingRule();
                    break;
                case ParamStructTypes.EXCEL_DATA_RESULT:
                    E = new DXFDataToExcelMapping();
                    break;
                case ParamStructTypes.ASSET_MANAGER:
                    E = new DXFAssetManager();
                    break;
                case ParamStructTypes.ASSET_GEOM:
                    E = new DXFGeometricAsset();
                    break;
                case ParamStructTypes.ASSET_DOCU:
                    E = new DXFDocumentAsset();
                    break;
                case ParamStructTypes.RESOURCE_DIR:
                    E = new DXFResourceDirectoryEntry();
                    break;
                case ParamStructTypes.RESOURCE_FILE:
                    E = new DXFContainedResourceFileEntry();
                    break;
                case ParamStructTypes.RESOURCE_LINK:
                    E = new DXFLinkedResourceFileEntry();
                    break;
                case ParamStructTypes.CHAT_ITEM:
                    E = new DXFChatItem();
                    break;
                case ParamStructTypes.CHAT_SEQ:
                    E = new DXFChatSequenceForComponent();
                    break;
                case ParamStructTypes.COLOR_IN_BYTES:
                    E = new DXFByteColor();
                    break;
                case ParamStructTypes.FILE_VERSION:
                    E = new DXFFileVersion();
                    break;
                default:
                    E = new DXFDummy(this.FValue);
                    break;
            }
            E.Decoder = this;
            return E;
        }

        #endregion

        #region METHODS: Reading File

        public void LoadFromFile(string _fileName, bool _lock_parsed_components = false)
        {
            if (File.Exists(_fileName))
            {
                LoadFromFile(new FileStream(_fileName, FileMode.Open), _lock_parsed_components);
            }
            this.ReleaseRessources();
        }

        public void LoadFromFile(Stream _stream, bool _lock_parsed_components = false)
        {
            if (this.DecoderMode == DXFDecoderMode.Components)
            {
                ProjectData.Components.EnableReferencePropagation = false;
                ProjectData.Components.StartLoading();
            }

            this.FMainSect = new DXFSection();
            this.FMainSect.Decoder = this;

            if (this.FStream == null)
            {
                this.FStream = new StreamReader(_stream, Encoding.UTF8, false);
            }
            this.FMainSect.ParseNext();


            if (this.DecoderMode == DXFDecoderMode.Components)
            {
                ProjectData.Components.EndLoading();
                ProjectData.Components.EnableReferencePropagation = true;
            }
        }

        // processes 2 lines: 
        // 1. the line containing the DXF CODE
        // 2. the line containing the INFORMATION saved under said code
        public void Next()
        {
            int code;
            var line = this.FStream.ReadLine();
            bool success = Int32.TryParse(line, out code);
            if (success)
            {
                this.FCode = code;
                this.HasValidData = true;
            }
            else
                this.FCode = (int)ParamStructCommonSaveCode.INVALID_CODE;

            this.FValue = this.FStream.ReadLine();
        }

        public bool HasNext()
        {
            if (this.FStream == null) return false;
            if (this.FStream.Peek() < 0) return false;
            return true;
        }

        public void ReleaseRessources()
        {
            if (this.FStream != null)
            {
                this.FStream.Close();
                try
                {
                    FStream.Dispose();
                }
                catch (Exception ex)
                {
                    string message = ex.Message;
                }
            }
            this.FStream = null;

            if (this.logFileWriter != null)
                this.logFileWriter.Dispose();
            this.logFileWriter = null;
        }


        #endregion

        #region READING Chat File

        public void LoadChatsFromFile(string _fileName)
        {
            this.AttachChatToComponent = false;
            this.for_ChatMerging = new Dictionary<long, SimChat>();

            this.FMainSect = new DXFSection();
            this.FMainSect.Decoder = this;
            if (File.Exists(_fileName))
            {
                if (this.FStream == null)
                {
                    this.FStream = new StreamReader(_fileName);
                }
                this.FMainSect.ParseNext();
            }
            this.ReleaseRessources();
            this.AttachChatToComponent = true;
        }

        #endregion

        #region Utility METHODS: Simple Value Parsing

        public double DoubleValue()
        {
            return StringToDouble(this.FValue);
        }

        public int IntValue()
        {
            int i;
            bool success = Int32.TryParse(this.FValue, out i);
            if (success)
                return i;
            else
                return 0;
        }

        public long LongValue()
        {
            long l;
            bool success = Int64.TryParse(this.FValue, out l);
            if (success)
                return l;
            else
                return 0;
        }

        public ulong UlongValue()
        {
            ulong ul;
            bool success = UInt64.TryParse(this.FValue, out ul);
            if (success)
                return ul;
            else
                return 0UL;
        }

        public byte ByteValue()
        {
            byte b;
            bool success = Byte.TryParse(this.FValue, out b);
            if (success)
                return b;
            else
                return 0;
        }

        public (Guid global, long local, int err) GlobalAndLocalValue()
        {
            return SimObjectId.FromString(this.FValue);
        }

        #endregion


        #region Number Encoding/Decoding

        private const string SERIALIZER_NAN = "NaN";
        private const string SERIALIZER_POSITIVINFINITY = "+\U0000221E";
        private const string SERIALIZER_NEGATIVEINFINITY = "-\U0000221E";

        public static double StringToDouble(string value)
        {
            if (value == null) return 0.0;

            switch (value)
            {
                case SERIALIZER_NAN:
                    return double.NaN;
                case SERIALIZER_POSITIVINFINITY:
                    return double.MaxValue;
                case SERIALIZER_NEGATIVEINFINITY:
                    return double.MinValue;
                default:
                    double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double f);
                    return f;
            }
        }
        public static string DoubleToString(double d, string formatString = "F2")
        {
            if (string.IsNullOrEmpty(formatString))
                throw new ArgumentNullException(formatString);

            if (double.IsNaN(d))
                return DXFDecoder.SERIALIZER_NAN;
            else if (double.IsPositiveInfinity(d) || d == double.MaxValue)
                return DXFDecoder.SERIALIZER_POSITIVINFINITY;
            else if (double.IsNegativeInfinity(d) || d == double.MinValue)
                return DXFDecoder.SERIALIZER_NEGATIVEINFINITY;
            else
                return d.ToString(formatString, CultureInfo.InvariantCulture);
        }


        #endregion



        #region Logging

        public FileInfo LogFile { get; set; }

        private StreamWriter logFileWriter;

        public void Log(string message)
        {
            if (logFileWriter == null && LogFile != null)
                logFileWriter = new StreamWriter(LogFile.FullName, true);

            if (logFileWriter != null)
            {
                logFileWriter.WriteLine(message);
            }
        }

        #endregion


        #region ID Translation (V5, V9)

        /// <summary>
        /// Translates old ids (per Type based) to V5-Ids (project-wide unique ids)
        /// Also used for V9 Id translation of component Ids (same problem as V5)
        /// </summary>
        public static Dictionary<(Type, long), long> IdTranslation = new Dictionary<(Type, long), long>();

        public static long ComponentReservedIds { get { return 1073741824; } } //2^30
        public static long MultiValueIdOffset { get { return ComponentReservedIds + 1000000; } } //1M reserved Ids
        public static long CalculationIdOffset { get { return MultiValueIdOffset + 1000000; } } //1M reserved Ids
        public static long ParameterIdOffset { get { return CalculationIdOffset + 1000000; } } //1M reserved Ids
        public static long InstanceIdOffset { get { return ParameterIdOffset + 1000000; } } //2^30 reserved Ids

        public static long MaxTranslationId { get { return InstanceIdOffset + 1073741824; } } //2^30 reserved Ids

        public static int MultiValueCount { get; set; } = 0;
        public static int CalculationCount { get; set; } = 0;
        public static int ParameterCount { get; set; } = 0;
        public static int InstanceCount { get; set; } = 0;
        public static int ComponentCount { get; set; } = 1; //Set to 1 because id 0 is reserved for the empty Id

        public static void ResetV5IdTranslation()
        {
            MultiValueCount = 0;
            CalculationCount = 0;
            ParameterCount = 0;
            InstanceCount = 0;
            ComponentCount = 1;

            IdTranslation.Clear();
        }

        public long TranslateComponentIdV8(long id)
        {
            if (this.CurrentFileVersion <= 8)
            {
                if (id == -1)
                    return 0;

                if (!IdTranslation.TryGetValue((typeof(SimComponent), id), out var v9Id))
                {
                    //Not translated yet, translate and store
                    if (ComponentCount > ComponentReservedIds)
                        throw new Exception("Too many components");

                    v9Id = ComponentCount;
                    ComponentCount++;
                    IdTranslation.Add((typeof(SimComponent), id), v9Id);
                }

                return v9Id;
            }

            return id;
        }

        #endregion
    }
}
