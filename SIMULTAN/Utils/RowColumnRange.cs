using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Utils
{
    /// <summary>
    /// Specifies a range of columns and rows
    /// </summary>
    public struct RowColumnRange : IEquatable<RowColumnRange>
    {
        /// <summary>
        /// The first row in the range
        /// </summary>
        public int RowStart { get; }
        /// <summary>
        /// The number of rows in the range
        /// </summary>
        public int RowCount { get; }
        /// <summary>
        /// The first column in the range
        /// </summary>
        public int ColumnStart { get; }
        /// <summary>
        /// The number of columns in the range
        /// </summary>
        public int ColumnCount { get; }

        /// <summary>
        /// Initializes a new instance of the RowColumnRange class
        /// </summary>
        /// <param name="rowStart">The first row in the range</param>
        /// <param name="columnStart">The first column in the range</param>
        /// <param name="rowCount">The number of rows in the range</param>
        /// <param name="columnCount">The number of columns in the range</param>
        public RowColumnRange(int rowStart, int columnStart, int rowCount, int columnCount)
        {
            this.RowStart = rowStart;
            this.ColumnStart = columnStart;

            this.RowCount = rowCount;
            this.ColumnCount = columnCount;
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (obj is RowColumnRange r)
                return Equals(r);
            return false;
        }
        /// <inheritdoc />
        public override int GetHashCode()
        {
            int hash = 17;
            // Suitable nullity checks etc, of course :)
            hash = hash * 23 + RowStart.GetHashCode();
            hash = hash * 23 + RowCount.GetHashCode();
            hash = hash * 23 + ColumnStart.GetHashCode();
            hash = hash * 23 + ColumnCount.GetHashCode();
            return hash;
        }
        /// <inheritdoc />
        public bool Equals(RowColumnRange other)
        {
            return this.RowStart == other.RowStart && this.RowCount == other.RowCount &&
                this.ColumnStart == other.ColumnStart && this.ColumnCount == other.ColumnCount;
        }

        /// <summary>
        /// Tests whether two instances describe the same range
        /// </summary>
        /// <param name="lhs">First instance</param>
        /// <param name="rhs">Second instance</param>
        /// <returns>True when the two instances describe the same range</returns>
        public static bool operator ==(RowColumnRange lhs, RowColumnRange rhs)
        {
            return lhs.Equals(rhs);
        }
        /// <summary>
        /// Tests whether two instances describe the same range
        /// </summary>
        /// <param name="lhs">First instance</param>
        /// <param name="rhs">Second instance</param>
        /// <returns>True when the two instances do not describe the same range</returns>
        public static bool operator !=(RowColumnRange lhs, RowColumnRange rhs)
        {
            return !lhs.Equals(rhs);
        }
    
        /// <summary>
        /// Merges to ranges and computes a range that fits both
        /// </summary>
        /// <param name="lhs">First range</param>
        /// <param name="rhs">Second range</param>
        /// <returns>A range that contains both input ranges</returns>
        public static RowColumnRange Merge(RowColumnRange lhs, RowColumnRange rhs)
        {
            var startColumn = Math.Min(lhs.ColumnStart, rhs.ColumnStart);
            var startRow = Math.Min(lhs.RowStart, rhs.RowStart);

            var lhsEndColumn = lhs.ColumnStart + lhs.ColumnCount;
            var lhsEndRow = lhs.RowStart + lhs.RowCount;

            var rhsEndColumn = rhs.ColumnStart + rhs.ColumnCount;
            var rhsEndRow = rhs.RowStart + rhs.RowCount;

            return new RowColumnRange(
                startRow, startColumn,
                Math.Max(lhsEndRow, rhsEndRow) - startRow,
                Math.Max(lhsEndColumn, rhsEndColumn) - startColumn
                );
        }
        /// <summary>
        /// Merges an range with an index and computes a range that fits both
        /// </summary>
        /// <param name="lhs">First range</param>
        /// <param name="rhs">Second range</param>
        /// <returns>A range that contains both inputs</returns>
        public static RowColumnRange Merge(RowColumnRange lhs, RowColumnIndex rhs)
        {
            var startColumn = Math.Min(lhs.ColumnStart, rhs.Column);
            var startRow = Math.Min(lhs.RowStart, rhs.Row);

            var endColumn = Math.Max(lhs.ColumnStart + lhs.ColumnCount, rhs.Column + 1);
            var endRow = Math.Max(lhs.RowStart + lhs.RowCount, rhs.Row + 1);

            return new RowColumnRange(startRow, startColumn, endRow - startRow, endColumn - startColumn);
        }
    }
}
