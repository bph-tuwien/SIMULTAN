using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SIMULTAN.Data.SimMath;

namespace SIMULTAN.Utils
{
    /// <summary>
    /// Stores a 3 dimensional range (AABB)
    /// </summary>
    public class Range3D
    {
        /// <summary>
        /// The minimum value along each axis
        /// </summary>
        public SimPoint3D Minimum { get; }
        /// <summary>
        /// The maximum value along each axis
        /// </summary>
        public SimPoint3D Maximum { get; }

        /// <summary>
        /// Initializes a new instance of the Range3D class
        /// </summary>
        /// <param name="minimum">The minimum value along each axis</param>
        /// <param name="maximum">The maximum value along each axis</param>
        public Range3D(SimPoint3D minimum, SimPoint3D maximum)
        {
            this.Minimum = minimum;
            this.Maximum = maximum;
        }
        /// <summary>
        /// Initializes a new instance of the Range3D class by copying values from another instance
        /// </summary>
        /// <param name="original">The instance from which values should be taken</param>
        public Range3D(Range3D original)
        {
            this.Minimum = original.Minimum;
            this.Maximum = original.Maximum;
        }

        /// <summary>
        /// Tests if a point is inside the range or not. (between min and max along all axis)
        /// </summary>
        /// <param name="position">The position</param>
        /// <returns>True when the position lies between minimum and maximum, otherwise False</returns>
        public bool Contains(SimPoint3D position)
        {
            return position.X.InRange(Minimum.X, Maximum.X) && position.Y.InRange(Minimum.Y, Maximum.Y) && position.Z.InRange(Minimum.Z, Maximum.Z);
        }
        /// <summary>
        /// Tests if a point is inside the range or not. (between min and max along all axis)
        /// </summary>
        /// <param name="position">The position</param>
        /// <returns>True when the position lies between minimum and maximum, otherwise False</returns>
        public bool Contains(IntIndex3D position)
        {
            return ((double)position.X).InRange(Minimum.X, Maximum.X) &&
                ((double)position.Y).InRange(Minimum.Y, Maximum.Y) &&
                ((double)position.Z).InRange(Minimum.Z, Maximum.Z);
        }
    }
}
