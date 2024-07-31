using SIMULTAN.Data.SimMath;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.ValueMappings
{
    /// <summary>
    /// Interface to unify mapping a set of values to a color
    /// </summary>
    public abstract class SimColorMap
    {
        /// <summary>
        /// The ValueMapping this color map belongs to
        /// </summary>
        public SimValueMapping Owner { get; internal set; } = null;

        /// <summary>
        /// Maps a given values to a single color
        /// </summary>
        /// <param name="value">The value</param>
        /// <returns>Mapped color</returns>
        public abstract SimColor Map(double value);

        /// <summary>
        /// Invokes the <see cref="SimValueMapping.ValueMappingChanged"/> event in the <see cref="Owner"/>
        /// </summary>
        internal void NotifyMappingChanged()
        {
            Owner?.NotifyValueMappingChanged();
        }
    }
}
