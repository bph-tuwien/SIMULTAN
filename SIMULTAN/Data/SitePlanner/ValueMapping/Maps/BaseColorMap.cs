using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace SIMULTAN.Data.SitePlanner
{
    /// <summary>
    /// Contains some statics available to all color maps
    /// </summary>
    public class BaseColorMapColors
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

    /// <summary>
    /// Base class for color maps
    /// </summary>
    public abstract class BaseColorMap<T> : IValueToColorMap where T : ValueToColorMapParameters
    {
        /// <inheritdoc />
        public float LightingFactor => BaseColorMapColors.BaseLightingFactor;

        /// <inheritdoc />
        public ValueToColorMapParameters Parameters { get; private set; }

        /// <summary>
        /// Color map parameters with specialized type
        /// </summary>
        public T DerivedParameters => (T)Parameters;

        /// <summary>
        /// Initializes a new instance of this class
        /// </summary>
        /// <param name="parameters">ValueToColorMapParameters</param>
        protected BaseColorMap(T parameters)
        {
            this.Parameters = parameters;
        }

        /// <inheritdoc />
        public abstract Color Map(IEnumerable<double> values);
    }
}
