using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace SIMULTAN.Exchange.NetworkConnectors
{
    /// <summary>
    /// Stores pre-defined colors for network element states
    /// </summary>
    internal static class NetworkColors
    {
        /// <summary>
        /// The color of an empty network element representation.
        /// </summary>
        public static readonly Color COL_EMPTY = (Color)ColorConverter.ConvertFromString("#FF404040");
        /// <summary>
        /// The color of an unassigned network element representation.
        /// </summary>
        public static readonly Color COL_UNASSIGNED = (Color)ColorConverter.ConvertFromString("#FFA0A0A0");
        /// <summary>
        /// The default color of a network element representation.
        /// </summary>
        public static readonly Color COL_NEUTRAL = (Color)ColorConverter.ConvertFromString("#FFffffff");
    }
}
