using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace SIMULTAN.Data.ValueMappings
{
    /// <summary>
    /// Contains some statics available to all color maps
    /// </summary>
    public class DefaultColorMappingColors
    {
        /// <summary>
        /// If values cannot be mapped during Map(..), this color is returned instead
        /// </summary>
        public static Color OutOfRangeColor = Colors.DarkGray;
        /// <summary>
        /// Color for objects that are not parameterized (i.e. have no component assigned)
        /// </summary>
        public static Color NotParameterizedColor = Colors.DarkGray;
        /// <summary>
        /// Base lighting factor
        /// </summary>
        public static float BaseLightingFactor = 0.5f;
    }
}
