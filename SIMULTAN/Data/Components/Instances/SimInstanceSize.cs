using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SIMULTAN.Data.SimMath;

namespace SIMULTAN.Data.Components
{
    /// <summary>
    /// Represents a size with minimum and maximum extents
    /// </summary>
    public struct SimInstanceSize : IEquatable<SimInstanceSize>
    {
        /// <summary>
        /// Minimum extents
        /// </summary>
        public SimVector3D Min { get; }
        /// <summary>
        /// Maximum extents
        /// </summary>
        public SimVector3D Max { get; }

        /// <summary>
        /// Returns a default size of 0s
        /// </summary>
        public static SimInstanceSize Default
        {
            get => new SimInstanceSize(new SimVector3D(), new SimVector3D(1.0, 1.0, 1.0));
        }

        /// <summary>
        /// Instantiates a new instance of this class
        /// </summary>
        /// <param name="min">Minimum extents</param>
        /// <param name="max">Maximum extents</param>
        public SimInstanceSize(SimVector3D min, SimVector3D max)
        {
            Min = min;
            Max = max;
        }

        /// <summary>
        /// Clones this object
        /// </summary>
        /// <returns>Cloned object</returns>
        public SimInstanceSize Clone()
        {
            return (SimInstanceSize)this.MemberwiseClone();
        }

        /// <summary>
        /// Compares the minimum and maximum values for equality
        /// </summary>
        /// <param name="other">Other size</param>
        /// <returns>true, if sizes are equal</returns>
        public bool Equals(SimInstanceSize other)
        {
            return this.Min == other.Min && this.Max == other.Max;
        }
        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (obj is SimInstanceSize other)
                return this.Equals(other);
            return false;
        }
        /// <inheritdoc/>
        public override int GetHashCode()
        {
            int hashCode = 1537547080;
            hashCode = hashCode * -1521134295 + Min.GetHashCode();
            hashCode = hashCode * -1521134295 + Max.GetHashCode();
            return hashCode;
        }


        /// <inheritdoc/>
        public static bool operator ==(SimInstanceSize lhs, SimInstanceSize rhs)
        {
            return lhs.Equals(rhs);
        }
        /// <inheritdoc/>
        public static bool operator !=(SimInstanceSize lhs, SimInstanceSize rhs)
        {
            return !lhs.Equals(rhs);
        }

        /// <summary>
        /// Returns the size as list of doubles beginning with minimum and followed by maximum
        /// </summary>
        public List<double> ToList()
        {
            return new List<double> { Min.X, Min.Y, Min.Z, Max.X, Max.Y, Max.Z };
        }
        /// <summary>
        /// Sets minimum and maximum from a given list of values
        /// </summary>
        /// <param name="values">List with exactly 6 values</param>
        public static SimInstanceSize FromList(List<double> values)
        {
            if (values == null)
                throw new ArgumentNullException(nameof(values));
            if (values.Count != 6)
                throw new ArgumentException("values has to contain exactly 6 entries");

            return new SimInstanceSize(
                new SimVector3D(values[0], values[1], values[2]),
                new SimVector3D(values[3], values[4], values[5])
                );
        }
    }
}
