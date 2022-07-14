using SIMULTAN.Data.Components;
using SIMULTAN.Data.MultiValues;
using SIMULTAN.Data.Users;
using SIMULTAN.Data.SitePlanner;
using SIMULTAN.Projects;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using static SIMULTAN.Data.Components.CalculationParameterMetaData;
using SIMULTAN.Data.FlowNetworks;
using SIMULTAN.Excel;
using SIMULTAN.Data.SimNetworks;

namespace SIMULTAN.Serializer.DXF
{
    /// <summary>
    /// Interface for converting a data element from/to DXF serializer strings
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal interface IDXFDataConverter<T>
    {
        /// <summary>
        /// Returns a string representation of the value
        /// </summary>
        /// <param name="value">The value that should be serialized</param>
        /// <returns>A string representation of value</returns>
        string ToDXFString(T value);
        /// <summary>
        /// Returns the parsed data from a DXF serializer string
        /// </summary>
        /// <param name="value">The string representation</param>
        /// <param name="info">Additional info for the parsing (contains FileVersion)</param>
        /// <returns>The parsed data</returns>
        T FromDXFString(string value, DXFParserInfo info);
    }

    /// <summary>
    /// Default implementation of the <see cref="IDXFDataConverter{T}"/> interface. Only throws exceptions and gets
    /// called whenever <see cref="DXFDataConverter"/> doesn't have a matching implementation for a specific type
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class DXFDataConverter<T> : IDXFDataConverter<T>
    {
        /// <summary>
        /// The data converter
        /// </summary>
        internal static readonly IDXFDataConverter<T> P = DXFDataConverter.P as IDXFDataConverter<T> ?? new DXFDataConverter<T>();

        /// <inheritdoc />
        public T FromDXFString(string value, DXFParserInfo info)
        {
            throw new NotImplementedException();
        }
        /// <inheritdoc />
        public string ToDXFString(T value)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Implementation of the <see cref="IDXFDataConverter{T}"/> interface for all supported types
    /// </summary>
    internal class DXFDataConverter : IDXFDataConverter<string>, IDXFDataConverter<Type>,
        IDXFDataConverter<Guid>, IDXFDataConverter<bool>,
        IDXFDataConverter<long>, IDXFDataConverter<ulong>, IDXFDataConverter<int>, IDXFDataConverter<byte>,
        IDXFDataConverter<double>,
        IDXFDataConverter<SimMultiValueType>, IDXFDataConverter<SimCategory>, IDXFDataConverter<SimInfoFlow>,
        IDXFDataConverter<SimParameterInstancePropagation>, IDXFDataConverter<SimParameterOperations>,
        IDXFDataConverter<ResourceReference>,
        IDXFDataConverter<ComponentIndexUsage>,
        IDXFDataConverter<MultiValueCalculationBinaryOperation>, IDXFDataConverter<DeviationModeType>,
        IDXFDataConverter<SimResultAggregationMethod>, IDXFDataConverter<SimInstanceType>,
        IDXFDataConverter<SimInstanceConnectionState>, IDXFDataConverter<SimInstanceSizeTransferSource>,
        IDXFDataConverter<SimComponentValidity>, IDXFDataConverter<SimUserRole>,
        IDXFDataConverter<SimComponentAccessPrivilege>, IDXFDataConverter<SimChatItemType>,
        IDXFDataConverter<SimChatItemState>,
        IDXFDataConverter<SimSlot>, IDXFDataConverter<SimComponentVisibility>,
        IDXFDataConverter<SimComponentContentSorting>,
        IDXFDataConverter<Color>, IDXFDataConverter<Quaternion>, IDXFDataConverter<DateTime>,
        IDXFDataConverter<SimFlowNetworkOperator>, IDXFDataConverter<SimFlowNetworkCalcDirection>,
        IDXFDataConverter<MappingSubject>, IDXFDataConverter<ExcelMappingRange>,
        IDXFDataConverter<TraversalStrategy>,
        IDXFDataConverter<PortType>
    {
        /// <summary>
        /// Instance of the data converter
        /// </summary>
        internal static DXFDataConverter P = new DXFDataConverter();

        private const string DOUBLE_NAN = "NaN";
        private const string DOUBLE_POSITIVINFINITY = "+\U0000221E";
        private const string DOUBLE_NEGATIVEINFINITY = "-\U0000221E";
        private const string SLOT_DELIMITER = "_0";

        /// <inheritdoc />
        public string ToDXFString(string value)
        {
            return value;
        }
        /// <inheritdoc />
        string IDXFDataConverter<string>.FromDXFString(string value, DXFParserInfo info)
        {
            return value;
        }

        /// <inheritdoc />
        public string ToDXFString(Type value)
        {
            return value.ToString();
        }
        /// <inheritdoc />
        Type IDXFDataConverter<Type>.FromDXFString(string value, DXFParserInfo info)
        {
            return Type.GetType(value);
        }

        /// <inheritdoc />
        public string ToDXFString(Guid value)
        {
            return value.ToString("D"); //lower case 32bit with hyphens
        }
        /// <inheritdoc />
        Guid IDXFDataConverter<Guid>.FromDXFString(string value, DXFParserInfo info)
        {
            return Guid.Parse(value);
        }

        /// <inheritdoc />
        public string ToDXFString(bool value)
        {
            return value ? "1" : "0";
        }
        /// <inheritdoc />
        bool IDXFDataConverter<bool>.FromDXFString(string value, DXFParserInfo info)
        {
            if (value == "1")
                return true;
            else if (value == "0")
                return false;
            else
                throw new FormatException(String.Format("Value \"{0}\" cannot be converted to bool", value));
        }

        #region Numbers

        /// <inheritdoc />
        public string ToDXFString(long value)
        {
            return value.ToString();
        }
        /// <inheritdoc />
        long IDXFDataConverter<long>.FromDXFString(string value, DXFParserInfo info)
        {
            return long.Parse(value);
        }

        /// <inheritdoc />
        public string ToDXFString(ulong value)
        {
            return value.ToString();
        }
        /// <inheritdoc />
        ulong IDXFDataConverter<ulong>.FromDXFString(string value, DXFParserInfo info)
        {
            return ulong.Parse(value);
        }

        /// <inheritdoc />
        public string ToDXFString(int value)
        {
            return value.ToString();
        }
        /// <inheritdoc />
        int IDXFDataConverter<int>.FromDXFString(string value, DXFParserInfo info)
        {
            return Int32.Parse(value);
        }

        /// <inheritdoc />
        public string ToDXFString(byte value)
        {
            return value.ToString();
        }
        /// <inheritdoc />
        byte IDXFDataConverter<byte>.FromDXFString(string value, DXFParserInfo info)
        {
            return byte.Parse(value);
        }

        /// <inheritdoc />
        public string ToDXFString(double value)
        {
            string strVal = null;
            if (double.IsNaN(value))
                strVal = DOUBLE_NAN;
            else if (double.IsPositiveInfinity(value) || value == double.MaxValue)
                strVal = DOUBLE_POSITIVINFINITY;
            else if (double.IsNegativeInfinity(value) || value == double.MinValue)
                strVal = DOUBLE_NEGATIVEINFINITY;
            else
                strVal = value.ToString("F8", CultureInfo.InvariantCulture);

            return strVal;

        }
        /// <inheritdoc />
        double IDXFDataConverter<double>.FromDXFString(string value, DXFParserInfo info)
        {
            switch (value)
            {
                case DOUBLE_NAN:
                    return double.NaN;
                case DOUBLE_POSITIVINFINITY:
                    return double.PositiveInfinity;
                case DOUBLE_NEGATIVEINFINITY:
                    return double.NegativeInfinity;
                default:
                    double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double f);
                    return f;
            }
        }

        #endregion

        #region Enums

        /// <inheritdoc />
        public string ToDXFString(SimMultiValueType value)
        {
            return ToDXFString((int)value);
        }
        /// <inheritdoc />
        SimMultiValueType IDXFDataConverter<SimMultiValueType>.FromDXFString(string value, DXFParserInfo info)
        {
            return (SimMultiValueType)Int32.Parse(value);
        }

        private static Dictionary<char, SimCategory> categoryCharacters = new Dictionary<char, SimCategory>
        {
            { 'B', SimCategory.Geometry },
            { 'C', SimCategory.Costs },
            { 'D', SimCategory.Regulations },
            { 'E', SimCategory.Heating },
            { 'F', SimCategory.Cooling },
            { 'G', SimCategory.Humidity },
            { 'H', SimCategory.Air },
            { 'I', SimCategory.Acoustics },
            { 'J', SimCategory.Light_Natural },
            { 'K', SimCategory.Light_Artificial },
            { 'L', SimCategory.Water },
            { 'M', SimCategory.Waste },
            { 'N', SimCategory.Electricity },
            { 'O', SimCategory.FireSafety },
            { 'P', SimCategory.MSR },
            { 'Q', SimCategory.Communication },
        };
        /// <inheritdoc />
        public string ToDXFString(SimCategory value)
        {
            return ((uint)value).ToString();
        }
        SimCategory IDXFDataConverter<SimCategory>.FromDXFString(string value, DXFParserInfo info)
        {
            if (info.FileVersion >= 12)
                return (SimCategory)uint.Parse(value);
            else
            {
                SimCategory cat = SimCategory.None;

                foreach (var c in value)
                    if (categoryCharacters.TryGetValue(c, out var ccat))
                        cat |= ccat;

                return cat;
            }
        }

        private static Dictionary<char, SimInfoFlow> infoFlowCharacters = new Dictionary<char, SimInfoFlow>
        {
            { '!', SimInfoFlow.Input },
            { '?', SimInfoFlow.Output },
            { '@', SimInfoFlow.Mixed },
            { '\"', SimInfoFlow.FromReference },
            { '&', SimInfoFlow.Automatic },
            { '/', SimInfoFlow.FromExternal },
        };
        /// <inheritdoc />
        public string ToDXFString(SimInfoFlow value)
        {
            return ((uint)value).ToString();
        }
        /// <inheritdoc />
        public SimInfoFlow FromDXFString(string value, DXFParserInfo info)
        {
            if (info.FileVersion >= 12)
                return (SimInfoFlow)uint.Parse(value);
            else
            {
                if (infoFlowCharacters.TryGetValue(value.First(), out var flow))
                    return flow;
                throw new ArgumentOutOfRangeException("value does not contain a valid InfoFlow character");
            }
        }

        /// <inheritdoc />
        public string ToDXFString(SimParameterInstancePropagation value)
        {
            return ((uint)value).ToString();
        }
        /// <inheritdoc />
        SimParameterInstancePropagation IDXFDataConverter<SimParameterInstancePropagation>.FromDXFString(string value, DXFParserInfo info)
        {
            return (SimParameterInstancePropagation)uint.Parse(value);
        }

        private static Dictionary<string, SimParameterOperations> stringToParameterOperation = new Dictionary<string, SimParameterOperations>
        {
            { "None", SimParameterOperations.None },
            { "EditValue", SimParameterOperations.EditValue },
            { "EditName", SimParameterOperations.EditName },
            { "Move", SimParameterOperations.Move },
            { "All", SimParameterOperations.All }
        };
        /// <inheritdoc />
        public string ToDXFString(SimParameterOperations value)
        {
            return ((uint)value).ToString();
        }
        /// <inheritdoc />
        SimParameterOperations IDXFDataConverter<SimParameterOperations>.FromDXFString(string value, DXFParserInfo info)
        {
            if (info.FileVersion >= 12)
                return (SimParameterOperations)uint.Parse(value);
            else
            {
                var splitted = value.Split(new string[] { ", " }, StringSplitOptions.None);
                SimParameterOperations result = SimParameterOperations.None;

                foreach (var part in splitted)
                {
                    if (stringToParameterOperation.TryGetValue(part, out var operation))
                        result |= operation;
                    else
                        throw new ArgumentOutOfRangeException("value does not contain a valid SimParameterOperations string");
                }
                return result;                
            }
        }

        /// <inheritdoc />
        public string ToDXFString(MultiValueCalculationBinaryOperation value)
        {
            return ((uint)value).ToString();
        }
        /// <inheritdoc />
        MultiValueCalculationBinaryOperation IDXFDataConverter<MultiValueCalculationBinaryOperation>.FromDXFString(string value, DXFParserInfo info)
        {
            return (MultiValueCalculationBinaryOperation)uint.Parse(value);
        }

        /// <inheritdoc />
        public string ToDXFString(DeviationModeType value)
        {
            return ((uint)value).ToString();
        }
        /// <inheritdoc />
        DeviationModeType IDXFDataConverter<DeviationModeType>.FromDXFString(string value, DXFParserInfo info)
        {
            return (DeviationModeType)uint.Parse(value);
        }

        /// <inheritdoc />
        public string ToDXFString(SimResultAggregationMethod value)
        {
            return ((uint)value).ToString();
        }
        /// <inheritdoc />
        SimResultAggregationMethod IDXFDataConverter<SimResultAggregationMethod>.FromDXFString(string value, DXFParserInfo info)
        {
            return (SimResultAggregationMethod)uint.Parse(value);
        }

        private static Dictionary<string, SimInstanceType> stringToInstanceType = new Dictionary<string, SimInstanceType>
        {
            { "NONE", SimInstanceType.None },
            { "DESCRIBES", SimInstanceType.Entity3D },
            { "DESCRIBES_3D", SimInstanceType.GeometricVolume },
            { "DESCRIBES_2DorLESS", SimInstanceType.GeometricSurface },
            { "ALIGNED_WITH", SimInstanceType.AttributesFace },
            { "CONTAINED_IN", SimInstanceType.NetworkNode },
            { "CONNECTS", SimInstanceType.NetworkEdge },
            { "GROUPS", SimInstanceType.Group },
            { "PARAMETERIZES", SimInstanceType.BuiltStructure },
            { "ATTRIBUTES_EDGE", SimInstanceType.AttributesEdge },
            { "ATTRIBUTES_POINT", SimInstanceType.AttributesPoint },
            //only temporarily needed (due to merge):
            { "SimNetworkBlock", SimInstanceType.SimNetworkBlock },
            { "InPort", SimInstanceType.InPort },
            { "OutPort", SimInstanceType.OutPort },
            { "PORT_OUT", SimInstanceType.InPort },
            { "PORT_IN", SimInstanceType.OutPort },
        };
        /// <inheritdoc />
        public string ToDXFString(SimInstanceType value)
        {
            return ((uint)value).ToString();
        }
        /// <inheritdoc />
        SimInstanceType IDXFDataConverter<SimInstanceType>.FromDXFString(string value, DXFParserInfo info)
        {
            if (info.FileVersion >= 12)
                return (SimInstanceType)uint.Parse(value);
            else
            {
                if (stringToInstanceType.TryGetValue(value, out var instType))
                    return instType;
                throw new ArgumentOutOfRangeException("value does not contain a valid SimInstanceType string");
            }
        }

        private static Dictionary<string, SimInstanceConnectionState> stringToConnectionState = new Dictionary<string, SimInstanceConnectionState>
        {
            { "Ok", SimInstanceConnectionState.Ok },
            { "OK", SimInstanceConnectionState.Ok },
            { "GEOMETRY_DELETED", SimInstanceConnectionState.GeometryDeleted },
            { "GeometryDeleted", SimInstanceConnectionState.GeometryDeleted },
            { "GEOMETRY_NOT_FOUND", SimInstanceConnectionState.GeometryNotFound },
            { "GeometryNotFound", SimInstanceConnectionState.GeometryNotFound },
        };
        /// <inheritdoc />
        public string ToDXFString(SimInstanceConnectionState value)
        {
            return ((uint)value).ToString();
        }
        /// <inheritdoc />
        SimInstanceConnectionState IDXFDataConverter<SimInstanceConnectionState>.FromDXFString(string value, DXFParserInfo info)
        {
            if (info.FileVersion >= 12)
                return (SimInstanceConnectionState)uint.Parse(value);
            else
            {
                if (stringToConnectionState.TryGetValue(value, out var state))
                    return state;
                throw new ArgumentOutOfRangeException("value does not contain a valid SimInstanceConnectionState string");
            }
        }

        private static Dictionary<string, SimInstanceSizeTransferSource> stringToTransferSource = new Dictionary<string, SimInstanceSizeTransferSource>
        {
            { "USER", SimInstanceSizeTransferSource.User },
            { "PARAMETER", SimInstanceSizeTransferSource.Parameter },
            { "PATH", SimInstanceSizeTransferSource.Path },
        };
        /// <inheritdoc />
        public string ToDXFString(SimInstanceSizeTransferSource value)
        {
            return ((uint)value).ToString();
        }
        /// <inheritdoc />
        SimInstanceSizeTransferSource IDXFDataConverter<SimInstanceSizeTransferSource>.FromDXFString(string value, DXFParserInfo info)
        {
            if (info.FileVersion >= 12)
                return (SimInstanceSizeTransferSource)uint.Parse(value);
            else
            {
                if (stringToTransferSource.TryGetValue(value, out var source))
                    return source;
                throw new ArgumentOutOfRangeException("value does not contain a valid SimInstanceSizeTransferSource string");
            }
        }

        private static Dictionary<string, SimComponentValidity> stringToComponentValidity = new Dictionary<string, SimComponentValidity>
        {
            { "WRITE_AFTER_SUPERVIZE", SimComponentValidity.WriteAfterSupervize },
            { "WRITE_AFTER_RELEASE", SimComponentValidity.WriteAfterRelease },
            { "SUPERVIZE_AFTER_RELEASE", SimComponentValidity.SupervizeAfterRelease },
            { "VALID", SimComponentValidity.Valid },
        };
        /// <inheritdoc />
        public string ToDXFString(SimComponentValidity value)
        {
            return ((uint)value).ToString();
        }
        /// <inheritdoc />
        SimComponentValidity IDXFDataConverter<SimComponentValidity>.FromDXFString(string value, DXFParserInfo info)
        {
            if (info.FileVersion >= 12)
                return (SimComponentValidity)uint.Parse(value);
            else
            {
                if (stringToComponentValidity.TryGetValue(value, out var source))
                    return source;
                throw new ArgumentOutOfRangeException("value does not contain a valid SimComponentValidity string");
            }
        }

        private static Dictionary<string, SimUserRole> stringToUserRole = new Dictionary<string, SimUserRole>
        {
            { "@", SimUserRole.ADMINISTRATOR },
            { "A", SimUserRole.MODERATOR },
            { "B", SimUserRole.ENERGY_NETWORK_OPERATOR },
            { "C", SimUserRole.ENERGY_SUPPLIER },
            { "D", SimUserRole.BUILDING_DEVELOPER },
            { "E", SimUserRole.BUILDING_OPERATOR },
            { "F", SimUserRole.ARCHITECTURE },
            { "G", SimUserRole.FIRE_SAFETY },
            { "H", SimUserRole.BUILDING_PHYSICS },
            { "I", SimUserRole.MEP_HVAC },
            { "J", SimUserRole.PROCESS_MEASURING_CONTROL },
            { "K", SimUserRole.BUILDING_CONTRACTOR },
            { "L", SimUserRole.GUEST },
        };
        /// <inheritdoc />
        public string ToDXFString(SimUserRole value)
        {
            return ((uint)value).ToString();
        }
        /// <inheritdoc />
        SimUserRole IDXFDataConverter<SimUserRole>.FromDXFString(string value, DXFParserInfo info)
        {
            if (info.FileVersion >= 12)
                return (SimUserRole)uint.Parse(value);
            else
            {
                if (stringToUserRole.TryGetValue(value, out var source))
                    return source;
                throw new ArgumentOutOfRangeException("value does not contain a valid SimUserRole string");
            }
        }


        private static Dictionary<char, SimComponentAccessPrivilege> accessPrivilegeCharacters = new Dictionary<char, SimComponentAccessPrivilege>
        {
            { 'W', SimComponentAccessPrivilege.Read },
            { 'X', SimComponentAccessPrivilege.Write },
            { 'Y', SimComponentAccessPrivilege.Supervize },
            { 'Z', SimComponentAccessPrivilege.Release },
        };
        /// <inheritdoc />
        public string ToDXFString(SimComponentAccessPrivilege value)
        {
            return ((uint)value).ToString();
        }
        /// <inheritdoc />
        SimComponentAccessPrivilege IDXFDataConverter<SimComponentAccessPrivilege>.FromDXFString(string value, DXFParserInfo info)
        {
            if (info.FileVersion >= 12)
                return (SimComponentAccessPrivilege)uint.Parse(value);
            else
            {
                SimComponentAccessPrivilege cat = SimComponentAccessPrivilege.None;

                foreach (var c in value)
                    if (accessPrivilegeCharacters.TryGetValue(c, out var ccat))
                        cat |= ccat;

                return cat;
            }
        }

        /// <inheritdoc />
        public string ToDXFString(ComponentIndexUsage value)
        {
            return ((uint)value).ToString();
        }
        /// <inheritdoc />
        ComponentIndexUsage IDXFDataConverter<ComponentIndexUsage>.FromDXFString(string value, DXFParserInfo info)
        {
            if (info.FileVersion < 12)
            {
                return (ComponentIndexUsage) Enum.Parse(typeof(ComponentIndexUsage), value, true);
            }
            else
            {
                return (ComponentIndexUsage)uint.Parse(value);
            }
        }

        private static Dictionary<string, SimChatItemState> stringToChatItemState = new Dictionary<string, SimChatItemState>
        {
            { "OPEN", SimChatItemState.OPEN },
            { "CLOSED", SimChatItemState.CLOSED },
        };
        /// <inheritdoc />
        public string ToDXFString(SimChatItemState value)
        {
            return ((uint)value).ToString();
        }
        /// <inheritdoc />
        SimChatItemState IDXFDataConverter<SimChatItemState>.FromDXFString(string value, DXFParserInfo info)
        {
            if (info.FileVersion >= 12)
                return (SimChatItemState)uint.Parse(value);
            else
            {
                if (stringToChatItemState.TryGetValue(value, out var state))
                    return state;
                throw new ArgumentOutOfRangeException("value does not contain a valid SimChatItemState string");
            }
        }

        private static Dictionary<string, SimChatItemType> stringToChatItemType = new Dictionary<string, SimChatItemType>
        {
            { "QUESTION", SimChatItemType.QUESTION },
            { "ANSWER", SimChatItemType.ANSWER },
            { "ANSWER_ACCEPT", SimChatItemType.ANSWER_ACCEPT },
            { "ANSWER_REJECT", SimChatItemType.ANSWER_REJECT },
            { "VOTING_SESSION", SimChatItemType.VOTING_SESSION },
            { "VOTE_ACCEPT", SimChatItemType.VOTE_ACCEPT },
            { "VOTE_REJECT", SimChatItemType.VOTE_REJECT },
        };
        /// <inheritdoc />
        public string ToDXFString(SimChatItemType value)
        {
            return ((uint)value).ToString();
        }
        /// <inheritdoc />
        SimChatItemType IDXFDataConverter<SimChatItemType>.FromDXFString(string value, DXFParserInfo info)
        {
            if (info.FileVersion >= 12)
                return (SimChatItemType)uint.Parse(value);
            else
            {
                if (stringToChatItemType.TryGetValue(value, out var state))
                    return state;
                throw new ArgumentOutOfRangeException("value does not contain a valid SimChatItemType string");
            }
        }

        private static Dictionary<string, SimComponentVisibility> stringToComponentVisibility = new Dictionary<string, SimComponentVisibility>
        {
            { "AlwaysVisible", SimComponentVisibility.AlwaysVisible },
            { "VisibleInProject", SimComponentVisibility.VisibleInProject },
            { "Hidden", SimComponentVisibility.Hidden },
            { "VISIBLE_ALWAYS", SimComponentVisibility.AlwaysVisible },
            { "VISIBLE_IN_PROJECT", SimComponentVisibility.VisibleInProject },
            { "HIDDEN", SimComponentVisibility.Hidden },
        };
        /// <inheritdoc />
        public string ToDXFString(SimComponentVisibility value)
        {
            return ((uint)value).ToString();
        }
        /// <inheritdoc />
        SimComponentVisibility IDXFDataConverter<SimComponentVisibility>.FromDXFString(string value, DXFParserInfo info)
        {
            if (info.FileVersion >= 12)
                return (SimComponentVisibility)uint.Parse(value);
            else
            {
                if (stringToComponentVisibility.TryGetValue(value, out var state))
                    return state;
                throw new ArgumentOutOfRangeException("value does not contain a valid SimChatItemType string");
            }
        }

        private static Dictionary<string, SimComponentContentSorting> stringToComponentSorting = new Dictionary<string, SimComponentContentSorting>
        {
            { "ByName", SimComponentContentSorting.ByName },
            { "BySlot", SimComponentContentSorting.BySlot },
            { "BY_NAME", SimComponentContentSorting.ByName },
            { "BY_SLOT", SimComponentContentSorting.BySlot },
        };
        /// <inheritdoc />
        public string ToDXFString(SimComponentContentSorting value)
        {
            return ((uint)value).ToString();
        }
        /// <inheritdoc />
        SimComponentContentSorting IDXFDataConverter<SimComponentContentSorting>.FromDXFString(string value, DXFParserInfo info)
        {
            if (info.FileVersion >= 12)
                return (SimComponentContentSorting)uint.Parse(value);
            else
            {
                if (stringToComponentSorting.TryGetValue(value, out var state))
                    return state;
                throw new ArgumentOutOfRangeException("value does not contain a valid SimChatItemType string");
            }
        }

        private static Dictionary<string, SimFlowNetworkOperator> stringToNetworkOperator = new Dictionary<string, SimFlowNetworkOperator>
        {
            { "+", SimFlowNetworkOperator.Addition },
            { "-", SimFlowNetworkOperator.Subtraction },
            { "*", SimFlowNetworkOperator.Multiplication },
            { "/", SimFlowNetworkOperator.Division },
            { "Min", SimFlowNetworkOperator.Minimum },
            { "Max", SimFlowNetworkOperator.Maximum },
            { ":=", SimFlowNetworkOperator.Assignment },
        };
        /// <inheritdoc />
        public string ToDXFString(SimFlowNetworkOperator value)
        {
            return ((uint)value).ToString();
        }
        /// <inheritdoc />
        SimFlowNetworkOperator IDXFDataConverter<SimFlowNetworkOperator>.FromDXFString(string value, DXFParserInfo info)
        {
            if (info.FileVersion >= 12)
                return (SimFlowNetworkOperator)uint.Parse(value);
            else
            {
                if (stringToNetworkOperator.TryGetValue(value, out var op))
                    return op;
                throw new ArgumentOutOfRangeException("value does not contain a valid SimFlowNetworkOperator string");
            }
        }

        /// <inheritdoc />
        public string ToDXFString(SimFlowNetworkCalcDirection value)
        {
            return ((uint)value).ToString();
        }
        /// <inheritdoc />
        SimFlowNetworkCalcDirection IDXFDataConverter<SimFlowNetworkCalcDirection>.FromDXFString(string value, DXFParserInfo info)
        {
            if (info.FileVersion >= 12)
                return (SimFlowNetworkCalcDirection)uint.Parse(value);
            else
            {
                return value == "1" ? SimFlowNetworkCalcDirection.Forward : SimFlowNetworkCalcDirection.Backward;
            }
        }

        private static Dictionary<string, MappingSubject> stringToMappingSubject = new Dictionary<string, MappingSubject>
        {
            { "COMPONENT", MappingSubject.Component },
            { "PARAMETER", MappingSubject.Parameter },
            { "GEOMETRY", MappingSubject.Geometry },
            { "GEOMETRY_POINT", MappingSubject.GeometryPoint },
            { "GEOMETRY_AREA", MappingSubject.GeometryArea },
            { "GEOMETRY_ORIENTATION", MappingSubject.GeometricOrientation },
            { "GEOMETRY_INCLINE", MappingSubject.GeometricIncline },
            { "INSTANCE", MappingSubject.Instance },
        };
        /// <inheritdoc />
        public string ToDXFString(MappingSubject value)
        {
            return ((uint)value).ToString();
        }
        /// <inheritdoc />
        MappingSubject IDXFDataConverter<MappingSubject>.FromDXFString(string value, DXFParserInfo info)
        {
            if (info.FileVersion >= 12)
                return (MappingSubject)uint.Parse(value);
            else
            {
                if (stringToMappingSubject.TryGetValue(value, out var subject))
                    return subject;
                throw new ArgumentOutOfRangeException("value does not contain a valid MappingSubject string");
            }
        }

        private static Dictionary<string, ExcelMappingRange> stringToExcelMappingRange = new Dictionary<string, ExcelMappingRange>
        {
            { "SINGLE_VALUE", ExcelMappingRange.SingleValue },
            { "VECTOR_VALUES", ExcelMappingRange.VectorValues },
            { "MATRIX_VALUES", ExcelMappingRange.MatrixValues },
        };
        /// <inheritdoc />
        public string ToDXFString(ExcelMappingRange value)
        {
            return ((uint)value).ToString();
        }
        /// <inheritdoc />
        ExcelMappingRange IDXFDataConverter<ExcelMappingRange>.FromDXFString(string value, DXFParserInfo info)
        {
            return (ExcelMappingRange)uint.Parse(value);
        }

        private static Dictionary<string, TraversalStrategy> stringToTraversalStrategy = new Dictionary<string, TraversalStrategy>
        {
            { "SUBTREE_ONLY", TraversalStrategy.SUBTREE_ONLY },
            { "REFERENCES_ONLY", TraversalStrategy.REFERENCES_ONLY },
            { "SUBTREE_AND_REFERENCES", TraversalStrategy.SUBTREE_AND_REFERENCES },
        };
        /// <inheritdoc />
        public string ToDXFString(TraversalStrategy value)
        {
            return ((uint)value).ToString();
        }
        /// <inheritdoc />
        TraversalStrategy IDXFDataConverter<TraversalStrategy>.FromDXFString(string value, DXFParserInfo info)
        {
            if (info.FileVersion >= 12)
                return (TraversalStrategy)uint.Parse(value);
            else
            {
                if (stringToTraversalStrategy.TryGetValue(value, out var strategy))
                    return strategy;
                throw new ArgumentOutOfRangeException("value does not contain a valid ExcelMappingRange string");
            }
        }


        public string ToDXFString(PortType value)
        {
            return ((uint)value).ToString();
        }

        PortType IDXFDataConverter<PortType>.FromDXFString(string value, DXFParserInfo info)
        {
            return (PortType)uint.Parse(value);
        }


        #endregion

        #region Structs

        /// <inheritdoc />
        public string ToDXFString(SimSlot value)
        {
            return value.SlotBase + SLOT_DELIMITER + value.SlotExtension;
        }
        /// <inheritdoc />
        SimSlot IDXFDataConverter<SimSlot>.FromDXFString(string value, DXFParserInfo info)
        {
            var splited = SimDefaultSlots.SplitExtensionSlot(value);

            if (!SimDefaultSlots.AllSlots.Contains(splited.slot))
                throw new ArgumentException("Invalid base slot");

            return new SimSlot(new SimSlotBase(splited.slot), splited.extension);
        }

        #endregion

        #region Others

        /// <inheritdoc />
        public string ToDXFString(Color value)
        {
            return String.Format("#{0:X2}{1:X2}{2:X2}{3:X2}", value.A, value.R, value.G, value.B);
        }
        /// <inheritdoc />
        Color IDXFDataConverter<Color>.FromDXFString(string value, DXFParserInfo info)
        {
            return (Color)ColorConverter.ConvertFromString(value);
        }

        /// <inheritdoc />
        public string ToDXFString(Quaternion value)
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }
        /// <inheritdoc />
        Quaternion IDXFDataConverter<Quaternion>.FromDXFString(string value, DXFParserInfo info)
        {
            return Quaternion.Parse(value);
        }

        /// <inheritdoc />
        public string ToDXFString(DateTime value)
        {
            return ToDXFString(value.ToUniversalTime().Ticks);
        }
        /// <inheritdoc />
        DateTime IDXFDataConverter<DateTime>.FromDXFString(string value, DXFParserInfo info)
        {
            long ticks = -1;
            if (long.TryParse(value, out ticks))
                return new DateTime(ticks);

            if (info.FileVersion == 0)
            {
                if (DateTime.TryParse(value, new DateTimeFormatInfo(), DateTimeStyles.None, out var dt_tmp))
                    return dt_tmp;
            }
            

            throw new ArgumentOutOfRangeException("value does not contain a valid DateTime object");
        }


        /// <summary>
        /// Not implemented, only for migration. Instead write project id and resource id separately.
        /// </summary>
        /// <param name="value">The value</param>
        /// <returns>The string to write</returns>
        /// <exception cref="NotImplementedException">Not implemented.</exception>
        public string ToDXFString(ResourceReference value)
        {
            throw new NotImplementedException("ResourceRefernces should not be serialized directly, serializer their project id and resource id separately.");
        }
        /// <inheritdoc />
        ResourceReference IDXFDataConverter<ResourceReference>.FromDXFString(string value, DXFParserInfo info)
        {
            if (value == string.Empty)
                return null;

            try
            {
                string[] parts = value.Split(' ');
                Guid projectId = new Guid(parts[0]);
                int resourceIndex = int.Parse(parts[1]);

                return new ResourceReference(projectId, resourceIndex, info.ProjectData.AssetManager);
            }
            catch (Exception)
            {
                return null;
            }
        }

        #endregion

    }
}
