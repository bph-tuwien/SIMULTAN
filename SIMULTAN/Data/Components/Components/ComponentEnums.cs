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
    /// <summary>
    /// Contains default base slots and methods to work with them
    /// </summary>
    public static class SimDefaultSlots
    {
        /// <summary>
        /// Default slot for an Item
        /// </summary>
        public const string Item = "Element";
        /// <summary>
        /// Default slot for a List
        /// </summary>
        public const string List = "Liste";
        /// <summary>
        /// Default slot for a Tuple
        /// </summary>
        public const string Tuple = "Tupel";
        /// <summary>
        /// Default slot for Imports
        /// </summary>
        public const string Import = "Import";

        /// <summary>
        /// Default slot for Communications
        /// </summary>
        public const string Communication = "Kommunikation";
        /// <summary>
        /// Default slot for Costs
        /// </summary>
        public const string Cost = "Kosten";
        /// <summary>
        /// Default slot for Regulations
        /// </summary>
        public const string Regulation = "Anforderungen";
        /// <summary>
        /// Default slot for Specifications
        /// </summary>
        public const string Specification = "Leistungsbeschr";
        /// <summary>
        /// Default slot for Calculations
        /// </summary>
        public const string Calculation = "Berechnung";

        /// <summary>
        /// Default slot for Geometric Objects
        /// </summary>
        public const string Object = "Geometrisches_Objekt";
        /// <summary>
        /// Default slot for Geometric Sizes
        /// </summary>
        public const string Size = "Geometrische_Maße";
        /// <summary>
        /// Default slot for Geometric Length
        /// </summary>
        public const string Length = "Geometrische_Längen";
        /// <summary>
        /// Default slot for Geometric Areas
        /// </summary>
        public const string Areas = "Geometrische_Flächen";
        /// <summary>
        /// Default slot for Geometric Volumes
        /// </summary>
        public const string Volumes = "Geometrische_Volumina";
        /// <summary>
        /// Default slot for Geometric Positions
        /// </summary>
        public const string Position = "Verortung";

        /// <summary>
        /// Default slot for Materials
        /// </summary>
        public const string Material = "Material";
        /// <summary>
        /// Default slot for Layers
        /// </summary>
        public const string Layer = "Schicht";
        /// <summary>
        /// Default slot for Composites
        /// </summary>
        public const string Composite = "Aufbau";
        /// <summary>
        /// Default slot for Joints
        /// </summary>
        public const string Joint = "Anschluss";
        /// <summary>
        /// Default slot for Openings
        /// </summary>
        public const string Opening = "Öffnung";

        /// <summary>
        /// Default slot for (Pipe/Network) Systems
        /// </summary>
        public const string System = "System";
        /// <summary>
        /// Default slot for Producers in a System
        /// </summary>
        public const string Producer = "Erzeuger";
        /// <summary>
        /// Default slot for Splitter in a System
        /// </summary>
        public const string Splitter = "Verteiler";
        /// <summary>
        /// Default slot for ?
        /// </summary>
        public const string SplitterPipe = "Verteiler_Kanal";
        /// <summary>
        /// Default slot for ?
        /// </summary>
        public const string SplitterPart = "Verteiler_Teil";
        /// <summary>
        /// Default slot for a Distributer in a System
        /// </summary>
        public const string Distributer = "Abgabe";
        /// <summary>
        /// Default slot for ?
        /// </summary>
        public const string ConnectsTo = "Angeschlossen_an";

        /// <summary>
        /// Default slot for Heating Components
        /// </summary>
        public const string Heating = "Heizung";
        /// <summary>
        /// Default slot for Cooling Components
        /// </summary>
        public const string Cooling = "Kühlung";
        /// <summary>
        /// Default slot for Humidity Components
        /// </summary>
        public const string Humidity = "Feuchte";
        /// <summary>
        /// Default slot for Acoustics Components
        /// </summary>
        public const string Acoustics = "Akustik";
        /// <summary>
        /// Default slot for natural Lighting
        /// </summary>
        public const string NaturalLight = "Naturlicht";
        /// <summary>
        /// Default slot for artificial Lighting
        /// </summary>
        public const string ArtificialLight = "Kunstlicht";
        /// <summary>
        /// Default slot for Water
        /// </summary>
        public const string Water = "Wasser";
        /// <summary>
        /// Default slot for Sewage
        /// </summary>
        public const string Sewage = "Abwasser";
        /// <summary>
        /// Default slot for Electrical Components
        /// </summary>
        public const string Electrical = "Elektro";
        /// <summary>
        /// Default slot for Fire Safety
        /// </summary>
        public const string FireSafety = "Brandschutz";
        /// <summary>
        /// Default slot for MSR Components
        /// </summary>
        public const string MSR = "MSR";

        /// <summary>
        /// Undefined Slot
        /// </summary>
        public const string Undefined = "Undefined Slot";

        /// <summary>
        /// Delimiter between slot base and slot extension
        /// </summary>
        public const string COMP_SLOT_DELIMITER = "_0";

        internal static readonly HashSet<string> AllSlots = new HashSet<string>
        {
            Item,
            List,
            Tuple,
            Import,

            Communication,
            Cost,
            Regulation,
            Specification,
            Calculation,

            Object,
            Size,
            Length,
            Areas,
            Volumes,
            Position,

            Material,
            Layer,
            Composite,
            Joint,
            Opening,

            System,
            Producer,
            Splitter,
            SplitterPipe,
            SplitterPart,
            Distributer,
            ConnectsTo,

            Heating,
            Cooling,
            Humidity,
            Acoustics,
            NaturalLight,
            ArtificialLight,
            Water,
            Sewage,
            Electrical,
            FireSafety,
            MSR,

            Undefined
        };


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
            int splitIdx = storedSlot.IndexOf(SimDefaultSlots.COMP_SLOT_DELIMITER);
            if (splitIdx == -1)
                return (storedSlot, "", false);
            else
                return (storedSlot.Substring(0, splitIdx), storedSlot.Substring(splitIdx + SimDefaultSlots.COMP_SLOT_DELIMITER.Length), true);
        }
    }
}
