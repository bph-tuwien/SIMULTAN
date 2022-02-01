using SIMULTAN.Serializer.DXF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.Components
{
    /// <summary>
    /// Enumeration with different categories for components/parameters
    /// </summary>
    [Flags]
    [DXFSerializerTypeNameAttribute("ParameterStructure.Component.Category")]
    public enum SimCategory
    {
        /// <summary>
        /// No special category
        /// </summary>
        None = 0,
        /// <summary>
        /// The general category
        /// </summary>
        General = 1,                // 0  Aa
        /// <summary>
        /// Belongs to geometry
        /// </summary>
        Geometry = 2,               // 1  Bb
        /// <summary>
        /// Stores costs
        /// </summary>
        Costs = 4,                  // 2  Cc
        /// <summary>
        /// Contains regulations
        /// </summary>
        Regulations = 8,            // 3  Dd
        /// <summary>
        /// Contains information about heating systems
        /// </summary>
        Heating = 16,               // 4  Ee
        /// <summary>
        /// Contains information about HVAC systems
        /// </summary>
        Cooling = 32,               // 5  Ff
        /// <summary>
        /// Contains information about the humidity
        /// </summary>
        Humidity = 64,              // 6  Gg
        /// <summary>
        /// Contains information about air handling
        /// </summary>
        Air = 128,                  // 7  Hh
        /// <summary>
        /// Contains information about the acoustic behavior
        /// </summary>
        Acoustics = 256,            // 8  Ii
        /// <summary>
        /// Contains information about natural light
        /// </summary>
        Light_Natural = 512,        // 9  Jj
        /// <summary>
        /// Contains information about artificial light
        /// </summary>
        Light_Artificial = 1024,    // 10 Kk
        /// <summary>
        /// Contains information about water/water flow
        /// </summary>
        Water = 2048,               // 11 Ll
        /// <summary>
        /// Contains information about waste management
        /// </summary>
        Waste = 4096,               // 12 Mm
        /// <summary>
        /// Contains information about electrical grids
        /// </summary>
        Electricity = 8192,         // 13 Nn
        /// <summary>
        /// Contains information about fire safety measures
        /// </summary>
        FireSafety = 16384,         // 14 Oo
        /// <summary>
        /// Contains information about MSR systems
        /// </summary>
        MSR = 32768,                // 15 Pp
        /// <summary>
        /// Contains information about communication between parties
        /// </summary>
        Communication = 65536,       // 16 Qq
    }
}
