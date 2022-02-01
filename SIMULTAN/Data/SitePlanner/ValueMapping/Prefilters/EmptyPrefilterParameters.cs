using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.SitePlanner
{
    /// <summary>
    /// Empty prefilter parameters
    /// </summary>
    public class EmptyPrefilterParameters : ValuePrefilterParameters
    {
        /// <summary>
        /// Initializes a new instance of this class
        /// </summary>
        public EmptyPrefilterParameters() { }

        /// <inheritdoc />
        public override void Deserialize(string obj) { }

        /// <inheritdoc />
        public override string Serialize()
        {
            return "";
        }
    }
}
