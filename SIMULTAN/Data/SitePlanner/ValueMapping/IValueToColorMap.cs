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
    /// Interface to unify mapping a set of values to a color
    /// </summary>
    public interface IValueToColorMap
    {
        /// <summary>
        /// This directly modifies the lighting by lerping between unlit and lit color 
        /// Value should be in the range [0, 1], 0 = unlit, 1 = fully lit
        /// </summary>
        float LightingFactor { get; }

        /// <summary>
        /// Stores all the color map related parameters
        /// </summary>
        ValueToColorMapParameters Parameters { get; }

        /// <summary>
        /// Maps the given set of values to a single color
        /// </summary>
        /// <param name="values">The values</param>
        /// <returns>Mapped color</returns>
        Color Map(IEnumerable<double> values);
    }
}
