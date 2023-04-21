using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Utils
{
    /// <summary>
    /// A 2D integer index
    /// </summary>
    public struct IntIndex2D : IEquatable<IntIndex2D>
    {
        #region Properties and Members

        private int[] data;

        /// <summary>
        /// The X index
        /// </summary>
        public int X { get { return data[0]; } set { data[0] = value; } }
        /// <summary>
        /// The Y index
        /// </summary>
        public int Y { get { return data[1]; } set { data[1] = value; } }

        #endregion

        /// <summary>
        /// Initializes a new instance of the IntIndex2D class
        /// </summary>
        /// <param name="x">The x index</param>
        /// <param name="y">The y index</param>
        public IntIndex2D(int x, int y)
        {
            data = new int[2] { x, y };
        }

        /// <summary>
        /// Returns the index for a given dimension (0 means X, 1 means Y)
        /// </summary>
        /// <param name="key">The dimension (0 means X, 1 means Y)</param>
        /// <returns>The index along the given dimension</returns>
        public int this[int key]
        {
            get
            {
                if (key < 0 || key > 1)
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

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (obj == null || !(obj is IntIndex2D))
                return false;

            return this.Equals((IntIndex2D)obj);
        }

        /// <summary>
        /// Returns true when both instances point to the same index
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(IntIndex2D other)
        {
            return X == other.X && Y == other.Y;
        }
        /// <inheritdoc />
        public override int GetHashCode()
        {
            return X * 31 + Y;
        }

        /// <summary>
        /// Returns True when the two instances are Equal, otherwise False
        /// </summary>
        /// <param name="lhs">First argument</param>
        /// <param name="rhs">Second argument</param>
        /// <returns>True when the two instances are Equal, otherwise False</returns>
        public static bool operator ==(IntIndex2D lhs, IntIndex2D rhs)
        {
            return lhs.Equals(rhs);
        }
        /// <summary>
        /// Returns True when the two instances are not Equal, otherwise False
        /// </summary>
        /// <param name="lhs">First argument</param>
        /// <param name="rhs">Second argument</param>
        /// <returns>True when the two instances are not Equal, otherwise False</returns>
        public static bool operator !=(IntIndex2D lhs, IntIndex2D rhs)
        {
            return !lhs.Equals(rhs);
        }

        /// <summary>
        /// Computes the addition of the indices
        /// </summary>
        /// <param name="lhs">First operand</param>
        /// <param name="rhs">Second operand</param>
        /// <returns>The sum of the two indices</returns>
        public static IntIndex2D operator+(IntIndex2D lhs, IntIndex2D rhs)
        {
            return new IntIndex2D(lhs.X + rhs.X, lhs.Y + rhs.Y);
        }
        /// <summary>
        /// Computes the subtraction of the indices
        /// </summary>
        /// <param name="lhs">First operand</param>
        /// <param name="rhs">Second operand</param>
        /// <returns>The difference of the two indices</returns>
        public static IntIndex2D operator -(IntIndex2D lhs, IntIndex2D rhs)
        {
            return new IntIndex2D(lhs.X - rhs.X, lhs.Y - rhs.Y);
        }
    }
}
