using SIMULTAN;
using SIMULTAN.Data.Users;
using SIMULTAN.Serializer.DXF;
using SIMULTAN.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace SIMULTAN.Data.Components
{
    #region ENUMS

    /// <summary>
    /// The different access privileges a user may have on a component
    /// </summary>
    [Flags]
    public enum SimComponentAccessPrivilege
    {
        /// <summary>
        /// The user does not have access to this component
        /// </summary>
        None = 0,
        /// <summary>
        /// The user may only read from this component, but not modify it
        /// </summary>
        Read = 1,
        /// <summary>
        /// The user may modify the component
        /// </summary>
        Write = 2,
        /// <summary>
        /// The user may supervize the component
        /// </summary>
        Supervize = 4,
        /// <summary>
        /// The user may release/publish the component
        /// </summary>
        Release = 8,
        /// <summary>
        /// The user has all of the other privileges in this enumeration
        /// </summary>
        All = Read | Write | Supervize | Release,
    }

    /// <summary>
    /// Specifies the access state of a <see cref="SimComponent"/> or <see cref="SimAccessProfile"/>
    /// </summary>
    public enum SimComponentValidity
    {
        /// <summary>
        /// The component state is not valid. The write operation has happened after the last supervize access
        /// </summary>
        WriteAfterSupervize = 1,
        /// <summary>
        /// The component state is not valid. The write operation has happened after the last release access
        /// </summary>
        WriteAfterRelease = 2,
        /// <summary>
        /// The component state is not valid. The supervize operation has happened after the last release access
        /// </summary>
        SupervizeAfterRelease = 3,
        /// <summary>
        /// The component state is valid. The order of operation is WRITE, then SUPERVIZE, then RELEASE
        /// </summary>
        Valid = 4
    }

    /// <summary>
    /// Defines the types of sorting applied to the sub-components of a component.
    /// </summary>
    public enum SimComponentContentSorting
    {
        /// <summary>
        /// Sort by name.
        /// </summary>
        ByName = 0,
        /// <summary>
        /// Sort by the current slot name.
        /// </summary>
        BySlot = 1
    }

    #endregion

    /// <summary>
    /// Contains helper methods for working with components and related types
    /// </summary>
    [Obsolete("Most of the methods should be integrated into the serializer")]
    public static class ComponentUtils
    {
        #region CATEGORY

        private const string CATEGORY_NONE_AS_STR = "bcdefghijklmnopq";
        public static string CategoryToString(SimCategory _input)
        {
            string output = ComponentUtils.CATEGORY_NONE_AS_STR;

            if (_input.HasFlag(SimCategory.Geometry))
                output = output.Replace('b', 'B');
            if (_input.HasFlag(SimCategory.Costs))
                output = output.Replace('c', 'C');
            if (_input.HasFlag(SimCategory.Regulations))
                output = output.Replace('d', 'D');
            if (_input.HasFlag(SimCategory.Heating))
                output = output.Replace('e', 'E');
            if (_input.HasFlag(SimCategory.Cooling))
                output = output.Replace('f', 'F');
            if (_input.HasFlag(SimCategory.Humidity))
                output = output.Replace('g', 'G');
            if (_input.HasFlag(SimCategory.Air))
                output = output.Replace('h', 'H');
            if (_input.HasFlag(SimCategory.Acoustics))
                output = output.Replace('i', 'I');
            if (_input.HasFlag(SimCategory.Light_Natural))
                output = output.Replace('j', 'J');
            if (_input.HasFlag(SimCategory.Light_Artificial))
                output = output.Replace('k', 'K');
            if (_input.HasFlag(SimCategory.Water))
                output = output.Replace('l', 'L');
            if (_input.HasFlag(SimCategory.Waste))
                output = output.Replace('m', 'M');
            if (_input.HasFlag(SimCategory.Electricity))
                output = output.Replace('n', 'N');
            if (_input.HasFlag(SimCategory.FireSafety))
                output = output.Replace('o', 'O');
            if (_input.HasFlag(SimCategory.MSR))
                output = output.Replace('p', 'P');
            if (_input.HasFlag(SimCategory.Communication))
                output = output.Replace('q', 'Q');

            return output;
        }
        public static SimCategory StringToCategory(string _input)
        {
            SimCategory output = SimCategory.None;
            if (string.IsNullOrEmpty(_input)) return output;

            if (_input.Contains('B'))
                output |= SimCategory.Geometry;
            if (_input.Contains('C'))
                output |= SimCategory.Costs;
            if (_input.Contains('D'))
                output |= SimCategory.Regulations;
            if (_input.Contains('E'))
                output |= SimCategory.Heating;
            if (_input.Contains('F'))
                output |= SimCategory.Cooling;
            if (_input.Contains('G'))
                output |= SimCategory.Humidity;
            if (_input.Contains('H'))
                output |= SimCategory.Air;
            if (_input.Contains('I'))
                output |= SimCategory.Acoustics;
            if (_input.Contains('J'))
                output |= SimCategory.Light_Natural;
            if (_input.Contains('K'))
                output |= SimCategory.Light_Artificial;
            if (_input.Contains('L'))
                output |= SimCategory.Water;
            if (_input.Contains('M'))
                output |= SimCategory.Waste;
            if (_input.Contains('N'))
                output |= SimCategory.Electricity;
            if (_input.Contains('O'))
                output |= SimCategory.FireSafety;
            if (_input.Contains('P'))
                output |= SimCategory.MSR;
            if (_input.Contains('Q'))
                output |= SimCategory.Communication;

            return output;
        }

        #endregion

        #region InfoFlow

        private const string INPUT = "!";
        private const string OUTPUT = "?";
        private const string MIXED = "@";
        private const string REF_IN = "\"";
        private const string CALC_IN = "&";
        private const string TYPE = "*";
        private const string EXTERNAL_IN = "/";

        [Obsolete("Use the InfoFlowToCharConverter instead")]
        public static string InfoFlowToString(SimInfoFlow _input)
        {
            switch (_input)
            {
                case SimInfoFlow.Input:
                    return ComponentUtils.INPUT;
                case SimInfoFlow.Output:
                    return ComponentUtils.OUTPUT;
                case SimInfoFlow.FromReference:
                    return ComponentUtils.REF_IN;
                case SimInfoFlow.Automatic:
                    return ComponentUtils.CALC_IN;
                case SimInfoFlow.FromExternal:
                    return ComponentUtils.EXTERNAL_IN;
                default:
                    return ComponentUtils.MIXED;
            }
        }

        public static SimInfoFlow StringToInfoFlow(string _input)
        {
            if (string.IsNullOrEmpty(_input)) return SimInfoFlow.Mixed;

            switch (_input)
            {
                case ComponentUtils.INPUT:
                    return SimInfoFlow.Input;
                case ComponentUtils.OUTPUT:
                    return SimInfoFlow.Output;
                case ComponentUtils.REF_IN:
                    return SimInfoFlow.FromReference;
                case ComponentUtils.CALC_IN:
                    return SimInfoFlow.Automatic;
                case ComponentUtils.EXTERNAL_IN:
                    return SimInfoFlow.FromExternal;
                default:
                    return SimInfoFlow.Mixed;
            }
        }

        #endregion

        #region COMPONENT MANAGEMENT

        public static string ComponentManagerTypeToLetter(SimUserRole _type)
        {
            switch (_type)
            {
                case SimUserRole.ADMINISTRATOR:
                    return "@";
                case SimUserRole.MODERATOR:
                    return "A";
                case SimUserRole.ENERGY_NETWORK_OPERATOR:
                    return "B";
                case SimUserRole.ENERGY_SUPPLIER:
                    return "C";
                case SimUserRole.BUILDING_DEVELOPER:
                    return "D";
                case SimUserRole.BUILDING_OPERATOR:
                    return "E";
                case SimUserRole.ARCHITECTURE:
                    return "F";
                case SimUserRole.FIRE_SAFETY:
                    return "G";
                case SimUserRole.BUILDING_PHYSICS:
                    return "H";
                case SimUserRole.MEP_HVAC:
                    return "I";
                case SimUserRole.PROCESS_MEASURING_CONTROL:
                    return "J";
                case SimUserRole.BUILDING_CONTRACTOR:
                    return "K";
                case SimUserRole.GUEST:
                    return "L";
                default:
                    return "L";
            }
        }

        public static SimUserRole StringToComponentManagerType(string _type)
        {
            if (_type == null) return SimUserRole.GUEST;

            switch (_type)
            {
                case "@":
                    return SimUserRole.ADMINISTRATOR;
                case "A":
                    return SimUserRole.MODERATOR;
                case "B":
                    return SimUserRole.ENERGY_NETWORK_OPERATOR;
                case "C":
                    return SimUserRole.ENERGY_SUPPLIER;
                case "D":
                    return SimUserRole.BUILDING_DEVELOPER;
                case "E":
                    return SimUserRole.BUILDING_OPERATOR;
                case "F":
                    return SimUserRole.ARCHITECTURE;
                case "G":
                    return SimUserRole.FIRE_SAFETY;
                case "H":
                    return SimUserRole.BUILDING_PHYSICS;
                case "I":
                    return SimUserRole.MEP_HVAC;
                case "J":
                    return SimUserRole.PROCESS_MEASURING_CONTROL;
                case "K":
                    return SimUserRole.BUILDING_CONTRACTOR;
                case "L":
                    return SimUserRole.GUEST;
                default:
                    return SimUserRole.GUEST;
            }
        }

        #endregion

        #region COMPONENT ACCESS

        public static string ComponentAccessTypeToString(SimComponentAccessPrivilege _input)
        {
            string output = "wxyz";

            if (_input.HasFlag(SimComponentAccessPrivilege.Read))
                output = output.Replace('w', 'W');
            if (_input.HasFlag(SimComponentAccessPrivilege.Write))
                output = output.Replace('x', 'X');
            if (_input.HasFlag(SimComponentAccessPrivilege.Supervize))
                output = output.Replace('y', 'Y');
            if (_input.HasFlag(SimComponentAccessPrivilege.Release))
                output = output.Replace('z', 'Z');

            return output;
        }

        public static SimComponentAccessPrivilege StringToComponentAccessType(string _input)
        {
            SimComponentAccessPrivilege output = SimComponentAccessPrivilege.None;
            if (string.IsNullOrEmpty(_input)) return output;

            if (_input.Contains('W') || _input.Contains('s') || _input.Contains('S'))
                output |= SimComponentAccessPrivilege.Read;
            if (_input.Contains('X') || _input.Contains('t') || _input.Contains('T'))
                output |= SimComponentAccessPrivilege.Write;
            if (_input.Contains('Y') || _input.Contains('u') || _input.Contains('U'))
                output |= SimComponentAccessPrivilege.Supervize;
            if (_input.Contains('Z') || _input.Contains('v') || _input.Contains('V'))
                output |= SimComponentAccessPrivilege.Release;

            return output;
        }

        #endregion

        #region COMPONENT ACCESS : Predefined Profiles

        /// <summary>
        /// Sets the access profile to the default access for a given slot
        /// </summary>
        /// <param name="slotBase">The slot</param>
        /// <param name="profile">The profile which should be modified</param>
        /// <param name="currentUser">The user that calls the action</param>
        public static void SetStandardProfile(SimSlotBase slotBase, SimAccessProfile profile, SimUserRole currentUser)
        {
            profile.ResetAccessFlags(currentUser);

            if (slotBase.Base.StartsWith(COMP_SLOT_COMMUNICATION))
            {
                profile.Where(x => x.Role != SimUserRole.GUEST).ForEach(x => x.Access |= SimComponentAccessPrivilege.Read | SimComponentAccessPrivilege.Supervize);
                profile[SimUserRole.BUILDING_DEVELOPER].Access |= SimComponentAccessPrivilege.Release;
            }
            else if (slotBase.Base.StartsWith(COMP_SLOT_COST))
            {
                profile[SimUserRole.BUILDING_DEVELOPER].Access = SimComponentAccessPrivilege.All;
                profile[SimUserRole.BUILDING_PHYSICS].Access = SimComponentAccessPrivilege.Read;
                profile[SimUserRole.ARCHITECTURE].Access = SimComponentAccessPrivilege.Read;
                profile[SimUserRole.MEP_HVAC].Access = SimComponentAccessPrivilege.Read;
            }
            else if (slotBase.Base.StartsWith(COMP_SLOT_REGULATION))
            {
                profile.ForEach(x => x.Access |= SimComponentAccessPrivilege.Read);
                profile[SimUserRole.ARCHITECTURE].Access = SimComponentAccessPrivilege.Supervize;
                profile[SimUserRole.BUILDING_DEVELOPER].Access = SimComponentAccessPrivilege.Release;
            }
            else if (slotBase.Base.EndsWith(COMP_SLOT_OBJECT) ||
                slotBase.Base.StartsWith(COMP_SLOT_SIZE) ||
                slotBase.Base.StartsWith(COMP_SLOT_LENGTHS) ||
                slotBase.Base.StartsWith(COMP_SLOT_AREAS) ||
                slotBase.Base.StartsWith(COMP_SLOT_VOLUMES) ||
                slotBase.Base.StartsWith(COMP_SLOT_POSITION))
            {
                profile.Where(x => x.Role != SimUserRole.GUEST).ForEach(x => x.Access |= SimComponentAccessPrivilege.Read);
                profile[SimUserRole.ARCHITECTURE].Access = SimComponentAccessPrivilege.Supervize;
                profile[SimUserRole.BUILDING_DEVELOPER].Access = SimComponentAccessPrivilege.Release;
            }
            else if (slotBase.Base.StartsWith(COMP_SLOT_MATERIAL) ||
                slotBase.Base.StartsWith(COMP_SLOT_LAYER) ||
                slotBase.Base.StartsWith(COMP_SLOT_COMPOSITE) ||
                slotBase.Base.StartsWith(COMP_SLOT_JOINT) ||
                slotBase.Base.StartsWith(COMP_SLOT_OPENING))
            {
                profile.Where(x => x.Role != SimUserRole.GUEST).ForEach(x => x.Access |= SimComponentAccessPrivilege.Read);
                profile[SimUserRole.ARCHITECTURE].Access = SimComponentAccessPrivilege.Supervize;
                profile[SimUserRole.BUILDING_DEVELOPER].Access = SimComponentAccessPrivilege.Release;
            }
            else if (slotBase.Base.StartsWith(COMP_SLOT_SYSTEM) ||
                slotBase.Base.StartsWith(COMP_SLOT_ERZEUGER) ||
                slotBase.Base.StartsWith(COMP_SLOT_VERTEILER) ||
                slotBase.Base.StartsWith(COMP_SLOT_VERTEILER_PIPE) ||
                slotBase.Base.StartsWith(COMP_SLOT_VERTEILER_PART) ||
                slotBase.Base.StartsWith(COMP_SLOT_ABGABE) ||
                slotBase.Base.StartsWith(COMP_SLOT_CONNECTED_TO) ||
                slotBase.Base.StartsWith(COMP_SLOT_SINGLE_HEATIG) ||
                slotBase.Base.StartsWith(COMP_SLOT_SINGLE_COOLING) ||
                slotBase.Base.StartsWith(COMP_SLOT_SINGLE_HUMIDITY) ||
                slotBase.Base.StartsWith(COMP_SLOT_SINGLE_ACOUSTICS) ||
                slotBase.Base.StartsWith(COMP_SLOT_SINGLE_LIGHT_NATURAL) ||
                slotBase.Base.StartsWith(COMP_SLOT_SINGLE_LIGHT_ARTIF) ||
                slotBase.Base.StartsWith(COMP_SLOT_SINGLE_WATER) ||
                slotBase.Base.StartsWith(COMP_SLOT_SINGLE_WASTE) ||
                slotBase.Base.StartsWith(COMP_SLOT_SINGLE_ELECTRICAL) ||
                slotBase.Base.StartsWith(COMP_SLOT_SINGLE_FIRE_SAFETY) ||
                slotBase.Base.StartsWith(COMP_SLOT_SINGLE_MSR))
            {
                profile.Where(x => x.Role != SimUserRole.GUEST).ForEach(x => x.Access |= SimComponentAccessPrivilege.Read);
                profile[SimUserRole.ARCHITECTURE].Access = SimComponentAccessPrivilege.Supervize;
                profile[SimUserRole.BUILDING_DEVELOPER].Access = SimComponentAccessPrivilege.Release;
            }
        }

        #endregion

        #region COMPONENT SLOTS: String definitions

        // usage: 
        // SYSTEM -> SYSTEM_Air_Conditioning, SYSTEM_Heating, etc. (i.e. begin the string with 'SYSTEM')
        public const string COMP_SLOT_ITEM = "Element";
        public const string COMP_SLOT_LIST = "Liste";
        public const string COMP_SLOT_TUPLE = "Tupel";
        public const string COMP_SLOT_IMPORT = "Import";

        public const string COMP_SLOT_COMMUNICATION = "Kommunikation";
        public const string COMP_SLOT_COST = "Kosten";
        public const string COMP_SLOT_REGULATION = "Anforderungen";
        public const string COMP_SLOT_SPECIFICATION = "Leistungsbeschr";
        public const string COMP_SLOT_CALCULATION = "Berechnung";

        public const string COMP_SLOT_OBJECT = "Geometrisches_Objekt";
        public const string COMP_SLOT_SIZE = "Geometrische_Maße";
        public const string COMP_SLOT_LENGTHS = "Geometrische_Längen";
        public const string COMP_SLOT_AREAS = "Geometrische_Flächen";
        public const string COMP_SLOT_VOLUMES = "Geometrische_Volumina";
        public const string COMP_SLOT_POSITION = "Verortung";

        public const string COMP_SLOT_MATERIAL = "Material";
        public const string COMP_SLOT_LAYER = "Schicht";
        public const string COMP_SLOT_COMPOSITE = "Aufbau";
        public const string COMP_SLOT_JOINT = "Anschluss";
        public const string COMP_SLOT_OPENING = "Öffnung";

        public const string COMP_SLOT_SYSTEM = "System";
        public const string COMP_SLOT_ERZEUGER = "Erzeuger";
        public const string COMP_SLOT_VERTEILER = "Verteiler";
        public const string COMP_SLOT_VERTEILER_PIPE = "Verteiler_Kanal";
        public const string COMP_SLOT_VERTEILER_PART = "Verteiler_Teil";
        public const string COMP_SLOT_ABGABE = "Abgabe";
        public const string COMP_SLOT_CONNECTED_TO = "Angeschlossen_an";

        public const string COMP_SLOT_SINGLE_HEATIG = "Heizung";
        public const string COMP_SLOT_SINGLE_COOLING = "Kühlung";
        public const string COMP_SLOT_SINGLE_HUMIDITY = "Feuchte";
        public const string COMP_SLOT_SINGLE_ACOUSTICS = "Akustik";
        public const string COMP_SLOT_SINGLE_LIGHT_NATURAL = "Naturlicht";
        public const string COMP_SLOT_SINGLE_LIGHT_ARTIF = "Kunstlicht";
        public const string COMP_SLOT_SINGLE_WATER = "Wasser";
        public const string COMP_SLOT_SINGLE_WASTE = "Abwasser";
        public const string COMP_SLOT_SINGLE_ELECTRICAL = "Elektro";
        public const string COMP_SLOT_SINGLE_FIRE_SAFETY = "Brandschutz";
        public const string COMP_SLOT_SINGLE_MSR = "MSR";

        public const string COMP_SLOT_DELIMITER = "_0";
        public const string COMP_SLOT_UNDEFINED = "Undefined Slot";

        public static readonly List<string> COMP_SLOTS_ALL = new List<string>
        {
            COMP_SLOT_ITEM,
            COMP_SLOT_LIST,
            COMP_SLOT_TUPLE,
            COMP_SLOT_IMPORT,

            COMP_SLOT_COMMUNICATION,
            COMP_SLOT_COST,
            COMP_SLOT_REGULATION,
            COMP_SLOT_SPECIFICATION,
            COMP_SLOT_CALCULATION,

            COMP_SLOT_OBJECT,
            COMP_SLOT_SIZE,
            COMP_SLOT_LENGTHS,
            COMP_SLOT_AREAS,
            COMP_SLOT_VOLUMES,
            COMP_SLOT_POSITION,

            COMP_SLOT_MATERIAL,
            COMP_SLOT_LAYER,
            COMP_SLOT_COMPOSITE,
            COMP_SLOT_JOINT,
            COMP_SLOT_OPENING,

            COMP_SLOT_SYSTEM,
            COMP_SLOT_ERZEUGER,
            COMP_SLOT_VERTEILER,
            COMP_SLOT_VERTEILER_PIPE,
            COMP_SLOT_VERTEILER_PART,
            COMP_SLOT_ABGABE,
            COMP_SLOT_CONNECTED_TO,

            COMP_SLOT_SINGLE_HEATIG,
            COMP_SLOT_SINGLE_COOLING,
            COMP_SLOT_SINGLE_HUMIDITY,
            COMP_SLOT_SINGLE_ACOUSTICS,
            COMP_SLOT_SINGLE_LIGHT_NATURAL,
            COMP_SLOT_SINGLE_LIGHT_ARTIF,
            COMP_SLOT_SINGLE_WATER,
            COMP_SLOT_SINGLE_WASTE,
            COMP_SLOT_SINGLE_ELECTRICAL,
            COMP_SLOT_SINGLE_FIRE_SAFETY,
            COMP_SLOT_SINGLE_MSR,

            COMP_SLOT_UNDEFINED
        };

        public static SimSlotBase InstanceTypeToSlotBase(SimInstanceType _type)
        {
            switch (_type)
            {
                case SimInstanceType.Group:
                case SimInstanceType.Entity3D:
                    return new SimSlotBase(COMP_SLOT_OBJECT);
                case SimInstanceType.GeometricVolume:
                    return new SimSlotBase(COMP_SLOT_VOLUMES);
                case SimInstanceType.GeometricSurface:
                    return new SimSlotBase(COMP_SLOT_AREAS);
                case SimInstanceType.Attributes2D:
                    return new SimSlotBase(COMP_SLOT_COMPOSITE);
                case SimInstanceType.NetworkNode:
                case SimInstanceType.NetworkEdge:
                    return new SimSlotBase(COMP_SLOT_POSITION);
                default:
                    return new SimSlotBase(COMP_SLOT_UNDEFINED);
            }
        }

        /// <summary>
        /// Splits a slot with (or without) extension into it's parts.
        /// </summary>
        /// <param name="storedSlot">The full slot (including extension)</param>
        /// <returns>
        /// slot: The slot base
        /// extension: The extension string, or an empty string when no extension was found
        /// hasExtension: True when an extensions string exists, otherwise False
        /// </returns>
        public static (string slot, string extension, bool hasExtension) SplitExtensionSlot(string storedSlot)
        {
            int splitIdx = storedSlot.IndexOf(ComponentUtils.COMP_SLOT_DELIMITER);
            if (splitIdx == -1)
                return (storedSlot, "", false);
            else
                return (storedSlot.Substring(0, splitIdx), storedSlot.Substring(splitIdx + ComponentUtils.COMP_SLOT_DELIMITER.Length), true);
        }

        #endregion

        #region COMPONENT VALIDITY



        [Obsolete("Serializer functionality")]
        public static string ComponentValidityToString(SimComponentValidity _validity)
        {
            switch (_validity)
            {
                case SimComponentValidity.WriteAfterSupervize:
                    return "WRITE_AFTER_SUPERVIZE";
                case SimComponentValidity.WriteAfterRelease:
                    return "WRITE_AFTER_RELEASE";
                case SimComponentValidity.SupervizeAfterRelease:
                    return "SUPERVIZE_AFTER_RELEASE";
                case SimComponentValidity.Valid:
                    return "VALID";
                default:
                    throw new NotImplementedException("Did you forget to add a new value to serialization?");
            }
        }
        [Obsolete("Serializer functionality")]
        public static SimComponentValidity StringToComponentValidity(string _validity_as_str)
        {
            switch (_validity_as_str)
            {
                case "WRITE_AFTER_SUPERVIZE":
                    return SimComponentValidity.WriteAfterSupervize;
                case "WRITE_AFTER_RELEASE":
                    return SimComponentValidity.WriteAfterRelease;
                case "SUPERVIZE_AFTER_RELEASE":
                    return SimComponentValidity.SupervizeAfterRelease;
                case "VALID":
                    return SimComponentValidity.Valid;
                case "NOT_CALCULATED":
                    return SimComponentValidity.Valid;
                default:
                    throw new NotImplementedException("Invalid string. Did you forget to add a new value to serialization?");
            }
        }

        #endregion

        #region COMPONENT COLOR

        public static void AddColorToExport(System.Windows.Media.Color _color, ref StringBuilder _sb)
        {
            if (_sb == null) return;

            _sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString()); // 0
            _sb.AppendLine(ParamStructTypes.COLOR_IN_BYTES);                          // BYTE_COLOR

            _sb.AppendLine(((int)ParamStructCommonSaveCode.CLASS_NAME).ToString());
            _sb.AppendLine(_color.GetType().ToString());

            _sb.AppendLine(((int)ParamStructCommonSaveCode.V5_VALUE).ToString());
            _sb.AppendLine(_color.A.ToString());
            _sb.AppendLine(((int)ParamStructCommonSaveCode.V6_VALUE).ToString());
            _sb.AppendLine(_color.R.ToString());
            _sb.AppendLine(((int)ParamStructCommonSaveCode.V7_VALUE).ToString());
            _sb.AppendLine(_color.G.ToString());
            _sb.AppendLine(((int)ParamStructCommonSaveCode.V8_VALUE).ToString());
            _sb.AppendLine(_color.B.ToString());
        }

        #endregion
    }
}
