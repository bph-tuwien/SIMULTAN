using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Utils
{
    /// <summary>
    /// A 2D integer index
    /// </summary>
    [DebuggerDisplay("RowColumnIndex (Row = {Row}, Column = {Column})")]
    public struct RowColumnIndex : IEquatable<RowColumnIndex>
    {
        #region Properties and Members

        private int row, column;

        /// <summary>
        /// The row index
        /// </summary>
        public int Row { get { return row; } set { row = value; } }
        /// <summary>
        /// The column index
        /// </summary>
        public int Column { get { return column; } set { column = value; } }

        #endregion

        /// <summary>
        /// Initializes a new instance of the IntIndex2D class
        /// </summary>
        /// <param name="row">The row index</param>
        /// <param name="column">The column index</param>
        public RowColumnIndex(int row, int column)
        {
            this.row = row;
            this.column = column;
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (obj == null || !(obj is RowColumnIndex))
                return false;

            return this.Equals((RowColumnIndex)obj);
        }

        /// <summary>
        /// Returns true when both instances point to the same index
        /// </summary>
        /// <param name="other"></param>
        /// <returns>True when both instances point to the same row/column, otherwise False</returns>
        public bool Equals(RowColumnIndex other)
        {
            return Row == other.Row && Column == other.Column;
        }
        /// <inheritdoc />
        public override int GetHashCode()
        {
            return Row * 31 + Column;
        }

        /// <summary>
        /// Returns True when the two instances are Equal, otherwise False
        /// </summary>
        /// <param name="lhs">First argument</param>
        /// <param name="rhs">Second argument</param>
        /// <returns>True when the two instances are Equal, otherwise False</returns>
        public static bool operator ==(RowColumnIndex lhs, RowColumnIndex rhs)
        {
            return lhs.Equals(rhs);
        }
        /// <summary>
        /// Returns True when the two instances are not Equal, otherwise False
        /// </summary>
        /// <param name="lhs">First argument</param>
        /// <param name="rhs">Second argument</param>
        /// <returns>True when the two instances are not Equal, otherwise False</returns>
        public static bool operator !=(RowColumnIndex lhs, RowColumnIndex rhs)
        {
            return !lhs.Equals(rhs);
        }

        /// <summary>
        /// Computes the addition of the indices
        /// </summary>
        /// <param name="lhs">First operand</param>
        /// <param name="rhs">Second operand</param>
        /// <returns>The sum of the two indices</returns>
        public static RowColumnIndex operator +(RowColumnIndex lhs, RowColumnIndex rhs)
        {
            return new RowColumnIndex(lhs.Row + rhs.Row, lhs.Column + rhs.Column);
        }
        /// <summary>
        /// Computes the subtraction of the indices
        /// </summary>
        /// <param name="lhs">First operand</param>
        /// <param name="rhs">Second operand</param>
        /// <returns>The difference of the two indices</returns>
        public static RowColumnIndex operator -(RowColumnIndex lhs, RowColumnIndex rhs)
        {
            return new RowColumnIndex(lhs.Row - rhs.Row, lhs.Column - rhs.Column);
        }

        /// <summary>
        /// Converts this instance to a <see cref="IntIndex2D"/>, the column index is placed on the X-axis, the row index on the y-axis
        /// </summary>
        /// <returns>
        /// A <see cref="IntIndex2D"/> containing the column index on the X-axis and the row index on the y-axis
        /// </returns>
        public IntIndex2D ToIndex2D()
        {
            return new IntIndex2D(Column, Row);
        }
        /// <summary>
        /// Converts an <see cref="IntIndex2D"/> to a <see cref="RowColumnIndex"/>.
        /// The X-axis is treated as column index, the Y-axis as row index
        /// </summary>
        /// <param name="index">The int index</param>
        /// <returns>a <see cref="RowColumnIndex"/> containing the inputs X-axis as column index and the Y-axis value as row index</returns>
        public static RowColumnIndex FromIndex2D(IntIndex2D index)
        {
            return new RowColumnIndex(index.Y, index.X);
        }
    }
}
