using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.Geometry
{
    /// <summary>
    /// Event handler delegate for the GeometryInvalidated event.
    /// </summary>
    /// <param name="sender">the sending object</param>
    /// <param name="affected_geometry">the geometry affected by the change</param>
    public delegate void GeometryInvalidatedEventHandler(object sender, IEnumerable<BaseGeometry> affected_geometry);

    /// <summary>
    /// Interface for a class that provides offset surface thicknesses
    /// </summary>
    public interface IOffsetQueryable
    {
        /// <summary>
        /// Gets the outer and inner offsets of the given face according to 
        /// the associated component representing a wall construction. If there is no
        /// such component, both offsets are 0.0.
        /// </summary>
        /// <param name="_face">the face whose offsets we are looking for</param>
        /// <returns>a tuple of doubles: the outer and inner offsets</returns>
        (double outer, double inner) GetFaceOffset(Face _face);

        /// <summary>
        /// Invoked when a change in the source of one or more connectors causes
        /// the target geometry to become invalid (e.g., size change for placed components, or wall construction thickness).
        /// </summary>
        event GeometryInvalidatedEventHandler GeometryInvalidated;
    }
}
