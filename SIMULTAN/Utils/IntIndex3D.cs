using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Utils
{
    /// <summary>
    /// Class for storing a 3D integer index
    /// </summary>
    [DebuggerDisplay("{X}, {Y}, {Z}")]
    public struct IntIndex3D : IEquatable<IntIndex3D>
    {
        #region Properties and Members

        private int[] data;

        /// <summary>
        /// Returns the X-axis value
        /// </summary>
        public int X { get { return data[0]; } set { data[0] = value; } }
        /// <summary>
        /// Returns the Y-axis value
        /// </summary>
        public int Y { get { return data[1]; } set { data[1] = value; } }
        /// <summary>
        /// Returns the Z-axis value
        /// </summary>
        public int Z { get { return data[2]; } set { data[2] = value; } }

        #endregion

        /// <summary>
        /// Initializes a new instance of the IntIndex3D class
        /// </summary>
        /// <param name="x">Value on the x-axis</param>
        /// <param name="y">Value on the y-axis</param>
        /// <param name="z">Value on the z-axis</param>
        public IntIndex3D(int x, int y, int z)
        {
            data = new int[3] { x, y, z };
        }

        /// <summary>
        /// Returns The value along and index (0=X, 1=Y, 2=Z)
        /// </summary>
        /// <param name="key">The index</param>
        /// <returns>The value along the corresponding axis</returns>
        public int this[int key]
        {
            get
            {
                if (key < 0 || key > 2)
                    throw new IndexOutOfRangeException("Key out of range");

                return data[key];
            }
            set
            {
                if (key < 0 || key > 2)
                    throw new IndexOutOfRangeException("Key out of range");

                data[key] = value;
            }
        }

        /// <summary>
        /// Returns True when the data in both IntIndex3D instances is the same
        /// </summary>
        /// <param name="other">The instance to compare with</param>
        /// <returns>True when the data in both IntIndex3D instances is the same, otherwise False</returns>
        public bool Equals(IntIndex3D other)
        {
            return X == other.X && Y == other.Y && Z == other.Z;
        }
        /// <inheritdoc />
        public override int GetHashCode()
        {
            int result = (int)(X ^ (X >> 32));
            result = 31 * result + (int)(Y ^ (Y >> 32));
            result = 31 * result + (int)(Z ^ (Z >> 32));
            return result;
        }
    }
}
