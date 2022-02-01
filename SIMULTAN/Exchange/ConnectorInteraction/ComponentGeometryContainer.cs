using SIMULTAN.Data.Components;
using SIMULTAN.Data.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Exchange.ConnectorInteraction
{
    /// <summary>
    /// For recording parallel hierarchies between components and geometry.
    /// </summary>
    internal struct ComponentGeometryContainer
    {
        /// <summary>
        /// The data carrying component
        /// </summary>
        public SimComponent Data { get; set; }
        /// <summary>
        /// The geometry corresponding to the component
        /// </summary>
        public BaseGeometry Geometry { get; set; }
        /// <summary>
        /// The nodes containing a sub-component of 'Data' and a contained geometry of 'Geometry' (e.g. the faces of a volume)
        /// </summary>
        public List<ComponentGeometryContainer> Childen { get; set; }
    }
}
