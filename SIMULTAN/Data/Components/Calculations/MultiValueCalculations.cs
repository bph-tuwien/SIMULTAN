using SIMULTAN.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.Components
{
    /// <summary>
    /// Describes the different binary operations that can be performed in a multi value calculation
    /// </summary>
    public enum MultiValueCalculationBinaryOperation
    {
        //Addition

        /// <summary>
        /// Element-wise addition. Missing values are treated as being 0.
        /// </summary>
        MATRIX_SUM = 20, //This is the default add operation
        /// <summary>
        /// Element-wise addition. Missing value handling: 0 for rows, repeating the last entry for columns
        /// </summary>
        MATRIX_SUM_REPEAT_COLUMN = 6,
        /// <summary>
        /// Element-wise addition. Missing value handling: repeats the last entry in each direction
        /// </summary>
        MATRIX_SUM_REPEAT_ROWCOLUMN = 12,
        /// <summary>
        /// Obsolete entry. Use <see cref="MATRIX_SUM_REPEAT_ROWCOLUMN"/> instead
        /// </summary>
        [Obsolete("Same as MATRIX_SUM_REPEAT_ROWCOLUMN (repeat the scalar in all directions")]
        MATRIX_SCALAR_SUM = 9,
        /// <summary>
        /// Obsolete entry. Use <see cref="MATRIX_SUM"/> instead
        /// </summary>
        [Obsolete("Same as MATRIX_SUM")]
        VECTOR_SUM = 5,

        //Vector Multiply

        /// <summary>
        /// Inner Product of two vectors. Only the first column of each operand is used. Missing values are treated as being 1.
        /// </summary>
        INNER_PRODUCT = 0,
        /// <summary>
        /// Outer Product of two vectors. Only the first column of each operand is used.
        /// </summary>
        OUTER_PRODUCT = 1,
        /// <summary>
        /// Outer Product of two vectors, The columns are appended to a single vector. Only the first column of each operand is used.
        /// </summary>
        OUTER_PRODUCT_FLAT = 2,

        //Matrix Multiplication

        /// <summary>
        /// Regular matrix-matrix multiplication. Returns double.NaN when the dimensions are not matching
        /// </summary>
        MATRIX_PRODUCT = 7, //This is the default multiplication operation
        /// <summary>
        /// Obsolete entry. Use <see cref="MATRIX_PRODUCT_PERELEMENT_REPEAT"/> instead
        /// </summary>
        [Obsolete("Same as MATRIX_PRODUCT_PERELEMENT_REPEAT")]
        MATRIX_SCALAR_PRODUCT = 8,
        /// <summary>
        /// Element-wise multiplication of two matrices. Missing values are treated as 1.
        /// </summary>
        MATRIX_PRODUCT_PERELEMENT = 3,
        /// <summary>
        /// Element-wise multiplication of two matrices. For missing values, the last column/row is repeated
        /// </summary>
        MATRIX_PRODUCT_PERELEMENT_REPEAT = 11,

        //Matrix Selection
        /// <summary>
        /// Selects columns from the first operand depending on 1-based indices given in the second operand.
        /// Only the first column of the second operand is used.
        /// Appends all result columns to a single vector.
        /// </summary>
        COLUMN_SELECTION = 4,
        /// <summary>
        /// Selects columns from the first operand depending on 1-based indices given in the second operand.
        /// Only the first column of the second operand is used.
        /// Appends all resulting columns in a new matrix
        /// </summary>
        COLUMN_SELECTION_AS_MATRIX = 10,
        /// <summary>
        /// Selects columns from the first operand depending on 1-based indices given in the second operand.
        /// Only the first column of the second operand is used.
        /// Stores the result columns along a pseudo-diagonal in the result matrix.
        /// </summary>
        COLUMN_SELECTION_AS_DIAGONAL = 19,

        //Category operations

        /// <summary>
        /// Assigns a category number from the second operand to each row in the first operand. 
        /// Then groups by this number and sums items in the same group.
        /// Results are stored in a matrix, rows ordered by category number.
        /// </summary>
        CATEGORY_SUM = 13,
        /// <summary>
        /// Assigns a category number from the second operand to each row in the first operand. 
        /// Then groups by this number and calculates the average of all items in the same group.
        /// Results are stored in a matrix, rows ordered by category number.
        /// </summary>
        CATEGORY_AVERAGE = 14,
        /// <summary>
        /// Assigns a category number from the second operand to each row in the first operand. 
        /// Then groups by this number and returns the minimum of each group.
        /// Results are stored in a matrix, rows ordered by category number.
        /// </summary>
        CATEGORY_MIN = 15,
        /// <summary>
        /// Assigns a category number from the second operand to each row in the first operand. 
        /// Then groups by this number and returns the maximum of each group.
        /// Results are stored in a matrix, rows ordered by category number.
        /// </summary>
        CATEGORY_MAX = 16,

        //Min/Max

        /// <summary>
        /// Selects the first c (first number of second operand) minimal values from the first operand.
        /// Stores the result in a single column, three rows per entry.
        /// First entry: Value, Second entry: Row index of Value, Third entry: Column index of value
        /// </summary>
        EXTREME_MIN_OF_MATRIX = 17,
        /// <summary>
        /// Selects the first c (first number of second operand) maximum values from the first operand.
        /// Stores the result in a single column, three rows per entry.
        /// First entry: Value, Second entry: Row index of Value, Third entry: Column index of value
        /// </summary>
        EXTREME_MAX_OF_MATRIX = 18,
    }

    /// <summary>
    /// Describes the different unary operations that can be performed in a MultiValue calculation
    /// </summary>
    public enum MultiValueCalculationUnaryOperation
    {
        /// <summary>
        /// Transposes the matrix (exchanges row and column indices)
        /// </summary>
        Transpose,
        /// <summary>
        /// Multiplies each element in the matrix with -1
        /// </summary>
        Negate
    }

    /// <summary>
    /// Provides calculation algorithms on 2D double arrays
    /// </summary>
    internal static class MultiValueCalculationsNEW
    {
        #region Addition

        /// <summary>
        /// Calculates the element-wise addition of to arrays.
        /// In case the two fields are not equally sized, missing values are padded according to repeatRows and repeatColumns.
        /// If False, missing values in that direction are assumed to be 0, otherwise the last entry is repeated.
        /// </summary>
        /// <param name="lhs">First array</param>
        /// <param name="rhs">Second array</param>
        /// <param name="repeatRows">Describes which values should be used when outside a given matrix along the row axis. 
        /// True means that the last value in the same row should be used</param>
        /// <param name="repeatColumns">Describes which values should be used when outside a given matrix along the column axis. 
        /// True means that the last value in the same column should be used</param>
        /// <returns>2D array with the maximum size of the two inputs</returns>
        internal static double[,] MatrixSum(double[,] lhs, double[,] rhs, bool repeatRows, bool repeatColumns)
        {
            CheckMatrix(lhs, nameof(lhs));
            CheckMatrix(rhs, nameof(rhs));

            int resultRows = Math.Max(lhs.GetLength(0), rhs.GetLength(0));
            int resultColumns = Math.Max(lhs.GetLength(1), rhs.GetLength(1));

            double[,] result = new double[resultRows, resultColumns];

            // calculate the MATRIX SUM
            for (int row = 0; row < resultRows; row++)
            {
                for (int col = 0; col < resultColumns; col++)
                {
                    result[row, col] = GetValueOrDefault(lhs, row, col, 0.0, repeatRows, repeatColumns)
                        + GetValueOrDefault(rhs, row, col, 0.0, repeatRows, repeatColumns);
                }
            }

            return result;
        }

        #endregion

        #region Vector - Vector

        /// <summary>
        /// Calculates the inner product of two vectors.
        /// When matrices are supplied, only the first column is used.
        /// When the two matrices have a different row-count, the last entry is repeated in the smaller sized matrix
        /// </summary>
        /// <param name="lhs">First vector</param>
        /// <param name="rhs">Second vector</param>
        /// <returns>A 1x1 matrix containing the scalar product</returns>
        internal static double[,] InnerProduct(double[,] lhs, double[,] rhs)
        {
            CheckMatrix(lhs, nameof(lhs));
            CheckMatrix(rhs, nameof(rhs));

            int rowCount = Math.Max(lhs.GetLength(0), rhs.GetLength(0));

            double result = 0;

            for (int i = 0; i < rowCount; i++)
            {
                result += (GetValueOrDefault(lhs, i, 0, 0.0, true, false) * GetValueOrDefault(rhs, i, 0, 0.0, true, false));
            }

            return new double[,] { { result } };
        }
        /// <summary>
        /// Calculates the outer product of two vectors.
        /// When matrices are supplied, only the first column is used.
        /// </summary>
        /// <param name="lhs">First vector</param>
        /// <param name="rhs">Second vector</param>
        /// <returns>A (lhs.GetLength(0) x rhs.GetLength(0)) matrix</returns>
        internal static double[,] OuterProduct(double[,] lhs, double[,] rhs)
        {
            CheckMatrix(lhs, nameof(lhs));
            CheckMatrix(rhs, nameof(rhs));

            double[,] result = new double[lhs.GetLength(0), rhs.GetLength(0)];

            for (int r = 0; r < lhs.GetLength(0); r++)
            {
                for (int c = 0; c < rhs.GetLength(0); c++)
                {
                    result[r, c] = lhs[r, 0] * rhs[c, 0];
                }
            }

            return result;
        }
        /// <summary>
        /// Calculates the outer product of two vectors and appends all columns of the result to a single column matrix
        /// When matrices are supplied, only the first column is used.
        /// </summary>
        /// <param name="lhs">First vector</param>
        /// <param name="rhs">Second vector</param>
        /// <returns>A (lhs.GetLength(0) * rhs.GetLength(0) x 1) matrix</returns>
        internal static double[,] OuterProductFlat(double[,] lhs, double[,] rhs)
        {
            CheckMatrix(lhs, nameof(lhs));
            CheckMatrix(rhs, nameof(rhs));

            double[,] result = new double[lhs.GetLength(0) * rhs.GetLength(0), 1];

            for (int c = 0; c < rhs.GetLength(0); c++)
            {
                for (int r = 0; r < lhs.GetLength(0); r++)
                {

                    result[c * lhs.GetLength(0) + r, 0] = lhs[r, 0] * rhs[c, 0];
                }
            }

            return result;
        }

        #endregion

        #region Matrix - Matrix

        /// <summary>
        /// Performs a matrix * matrix multiplications. Returns a matrix with just a NaN value when the dimensions do not match
        /// </summary>
        /// <param name="lhs">First vector</param>
        /// <param name="rhs">Second vector</param>
        /// <returns>A rhs.GetLength(1) x lhs.GetLength(0) matrix</returns>
        internal static double[,] MatrixProduct(double[,] lhs, double[,] rhs)
        {
            CheckMatrix(lhs, nameof(lhs));
            CheckMatrix(rhs, nameof(rhs));

            if (lhs.GetLength(1) != rhs.GetLength(0))
                return new double[,] { { double.NaN } };

            double[,] result = new double[lhs.GetLength(0), rhs.GetLength(1)];

            for (int r = 0; r < lhs.GetLength(0); r++)
            {
                for (int c = 0; c < rhs.GetLength(1); c++)
                {
                    var sp = 0.0;

                    for (int i = 0; i < lhs.GetLength(1); i++)
                        sp += (lhs[r, i] * rhs[i, c]);

                    result[r, c] = sp;
                }
            }

            return result;
        }

        /// <summary>
        /// Performs and element-wise multiplication of two matrices.
        /// In case the two fields are not equally sized, missing values are padded according to repeatRows and repeatColumns.
        /// If False, missing values in that direction are assumed to be 1, otherwise the last entry is repeated.
        /// </summary>
        /// <param name="lhs">First array</param>
        /// <param name="rhs">Second array</param>
        /// <param name="repeatRows">Describes which values should be used when outside a given matrix along the row axis. 
        /// True means that the last value in the same row should be used</param>
        /// <param name="repeatColumns">Describes which values should be used when outside a given matrix along the column axis. 
        /// True means that the last value in the same column should be used</param>
        /// <returns>2D array with the maximum size of the two inputs</returns>
        internal static double[,] MatrixElementwiseProduct(double[,] lhs, double[,] rhs, bool repeatRows, bool repeatColumns)
        {
            CheckMatrix(lhs, nameof(lhs));
            CheckMatrix(rhs, nameof(rhs));

            int resultRows = Math.Max(lhs.GetLength(0), rhs.GetLength(0));
            int resultColumns = Math.Max(lhs.GetLength(1), rhs.GetLength(1));

            double[,] result = new double[resultRows, resultColumns];

            // calculate the MATRIX SUM
            for (int row = 0; row < resultRows; row++)
            {
                for (int col = 0; col < resultColumns; col++)
                {
                    result[row, col] = GetValueOrDefault(lhs, row, col, 1.0, repeatRows, repeatColumns)
                        * GetValueOrDefault(rhs, row, col, 1.0, repeatRows, repeatColumns);
                }
            }

            return result;
        }

        #endregion

        #region Aggregation

        /// <summary>
        /// Calculates the pointwise average of all input arrays
        /// </summary>
        /// <param name="values">The values to average. All arrays have to have the same size</param>
        /// <returns>The element-wise average of all fields</returns>
        public static double[,] Average(List<double[,]> values)
        {
            if (values == null)
                throw new ArgumentNullException(nameof(values));
            if (values.Count == 0)
                throw new ArgumentException("Requires at least one array");

            //Check if all have same size
            int rows = values[0].GetLength(0);
            int columns = values[0].GetLength(1);

            if (values.Any(x => x.GetLength(0) != rows || x.GetLength(1) != columns))
                throw new ArgumentException("Input arrays have to have the same size");

            double[,] aggregated_values = new double[rows, columns];

            for (int r = 0; r < rows; ++r)
            {
                for (int c = 0; c < columns; ++c)
                {
                    aggregated_values[r, c] = values.Average(x => x[r, c]);
                }
            }

            return aggregated_values;
        }

        #endregion

        #region Selection

        /// <summary>
        /// Selects columns from a matrix and append them to a single vector. The 1-BASED indices of the columns are given by rhs
        /// When rhs contains a matrices, only the first column is used.
        /// When an index is outside the matrix (&lt; 0 or &gt; than column count), NaN values are returned
        /// </summary>
        /// <param name="lhs">The matrix</param>
        /// <param name="rhs">Vector with 1-BASED column indices</param>
        /// <returns>A rhs.GetLength(0) * lhs.GetLength(0) x 1 matrix containing the appended columns</returns>
        internal static double[,] SelectColumns(double[,] lhs, double[,] rhs)
        {
            CheckMatrix(lhs, nameof(lhs));
            CheckMatrix(rhs, nameof(rhs));

            int columnLength = lhs.GetLength(0);
            double[,] result = new double[columnLength * rhs.GetLength(0), 1];

            for (int rhsi = 0; rhsi < rhs.GetLength(0); rhsi++)
            {
                int columnIndex = (int)rhs[rhsi, 0] - 1;
                for (int r = 0; r < columnLength; r++)
                {
                    if (columnIndex < 0 || columnIndex >= lhs.GetLength(1))
                        result[rhsi * columnLength + r, 0] = double.NaN;
                    else
                        result[rhsi * columnLength + r, 0] = lhs[r, columnIndex];
                }
            }

            return result;
        }

        /// <summary>
        /// Selects columns from a matrix and append them column-wise (resulting in a matrix). The 1-BASED indices of the columns are given by rhs
        /// When rhs contains a matrices, only the first column is used.
        /// When an index is outside the matrix (&lt; 0 or &gt; than column count), NaN values are returned
        /// </summary>
        /// <param name="lhs">The matrix</param>
        /// <param name="rhs">Vector with 1-BASED column indices</param>
        /// <returns>A lhs.GetLength(0) x rhs.GetLength(0) matrix containing the appended columns</returns>
        internal static double[,] SelectColumnsAsMatrix(double[,] lhs, double[,] rhs)
        {
            CheckMatrix(lhs, nameof(lhs));
            CheckMatrix(rhs, nameof(rhs));

            int columnLength = lhs.GetLength(0);
            double[,] result = new double[columnLength, rhs.GetLength(0)];

            for (int rhsi = 0; rhsi < rhs.GetLength(0); rhsi++)
            {
                int columnIndex = (int)rhs[rhsi, 0] - 1;
                for (int r = 0; r < columnLength; r++)
                {
                    if (columnIndex < 0 || columnIndex >= lhs.GetLength(1))
                        result[r, rhsi] = double.NaN;
                    else
                        result[r, rhsi] = lhs[r, columnIndex];
                }
            }

            return result;
        }

        /// <summary>
        /// Selects columns from a matrix and append them as diagonal matrix elements. The 1-BASED indices of the columns are given by rhs
        /// When rhs contains a matrices, only the first column is used.
        /// When an index is outside the matrix (&lt; 0 or &gt; than column count), NaN values are returned
        /// </summary>
        /// <param name="lhs">The matrix</param>
        /// <param name="rhs">Vector with 1-BASED column indices</param>
        /// <returns>A rhs.GetLength(0) * lhs.GetLength(0) x rhs.GetLength(0)  matrix containing the appended columns</returns>
        internal static double[,] SelectColumnsAsDiagonalMatrix(double[,] lhs, double[,] rhs)
        {
            CheckMatrix(lhs, nameof(lhs));
            CheckMatrix(rhs, nameof(rhs));

            int columnLength = lhs.GetLength(0);
            double[,] result = new double[columnLength * rhs.GetLength(0), rhs.GetLength(0)];

            for (int rhsi = 0; rhsi < rhs.GetLength(0); rhsi++)
            {
                int columnIndex = (int)rhs[rhsi, 0] - 1;
                for (int r = 0; r < columnLength; r++)
                {
                    if (columnIndex < 0 || columnIndex >= lhs.GetLength(1))
                        result[rhsi * columnLength + r, rhsi] = double.NaN;
                    else
                        result[rhsi * columnLength + r, rhsi] = lhs[r, columnIndex];
                }
            }

            return result;
        }
        #endregion

        #region Grouping

        internal static double[,] GroupBySum(double[,] data, double[,] categories)
        {
            var result = GroupByInternal(data, categories, (x, y) => x + y);
            return result.result;
        }

        internal static double[,] GroupByAverage(double[,] data, double[,] categories)
        {
            var result = GroupByInternal(data, categories, (x, y) => x + y);

            foreach (var cat in result.categories.Values)
            {
                for (int c = 0; c < result.result.GetLength(1); c++)
                    result.result[cat.index, c] /= cat.count;
            }

            return result.result;
        }

        internal static double[,] GroupByMin(double[,] data, double[,] categories)
        {
            var result = GroupByInternal(data, categories, (x, y) => Math.Min(x, y), double.PositiveInfinity);
            return result.result;
        }

        internal static double[,] GroupByMax(double[,] data, double[,] categories)
        {
            var result = GroupByInternal(data, categories, (x, y) => Math.Max(x, y), double.NegativeInfinity);
            return result.result;
        }

        private static (double[,] result, Dictionary<int, (int count, int index)> categories)
            GroupByInternal(double[,] data, double[,] categories, Func<double, double, double> groupingFunction, double initValue = 0.0)
        {
            CheckMatrix(data, nameof(data));
            CheckMatrix(categories, nameof(categories));

            var validEntryCount = Math.Min(data.GetLength(0), categories.GetLength(0));

            //Count number of groups
            int[] catVector = new int[validEntryCount];
            for (int i = 0; i < validEntryCount; i++)
                catVector[i] = (int)categories[i, 0];

            Dictionary<int, (int count, int index)> countPerCat = catVector.GroupBy(x => (int)x).OrderBy(x => x.Key).Select((x, xi) => (x.Key, x.Count(), xi))
                .ToDictionary(x => x.Key, x => (x.Item2, x.xi));

            double[,] result = new double[countPerCat.Count, data.GetLength(1)];

            //Init result set
            if (initValue != 0.0)
            {
                for (int r = 0; r < result.GetLength(0); r++)
                    for (int c = 0; c < result.GetLength(1); c++)
                        result[r, c] = initValue;
            }

            for (int r = 0; r < validEntryCount; r++)
            {
                int cat = catVector[r];
                int targetRowIndex = countPerCat[cat].index;

                for (int c = 0; c < data.GetLength(1); c++)
                {
                    result[targetRowIndex, c] = groupingFunction(result[targetRowIndex, c], data[r, c]);
                }
            }

            return (result, countPerCat);
        }

        #endregion

        #region Min/Max

        /// <summary>
        /// Returns the first N minimal values from a matrix.
        /// If both inputs are matrices, lhs is treated as matrix and N is taken from rhs[0,0].
        /// If rhs is a matrix and lhs is a 1x1 matrix, the two operands are exchanged
        /// </summary>
        /// <param name="lhs">First operand</param>
        /// <param name="rhs">Second operand</param>
        /// <returns>A vector containing 3 * N entries. For each match, the value, then the row index and then the column index are appended</returns>
        internal static double[,] SelectMinIndex(double[,] lhs, double[,] rhs)
        {
            return SelectIndex(lhs, rhs, (x, y) => x < y, double.NegativeInfinity);
        }

        /// <summary>
        /// Returns the first N maximal values from a matrix.
        /// If both inputs are matrices, lhs is treated as matrix and N is taken from rhs[0,0].
        /// If rhs is a matrix and lhs is a 1x1 matrix, the two operands are exchanged
        /// </summary>
        /// <param name="lhs">First operand</param>
        /// <param name="rhs">Second operand</param>
        /// <returns>A vector containing 3 * N entries. For each match, the value, then the row index and then the column index are appended</returns>
        internal static double[,] SelectMaxIndex(double[,] lhs, double[,] rhs)
        {
            return SelectIndex(lhs, rhs, (x, y) => x > y, double.PositiveInfinity);
        }


        private static double[,] SelectIndex(double[,] lhs, double[,] rhs, Func<double, double, bool> operation, double initialValue)
        {
            CheckMatrix(lhs, nameof(lhs));
            CheckMatrix(rhs, nameof(rhs));

            if (lhs.GetLength(0) == 1 && lhs.GetLength(1) == 1 && (rhs.GetLength(0) != 1 || rhs.GetLength(1) != 1))
                (lhs, rhs) = (rhs, lhs);

            int N = (int)rhs[0, 0];
            List<(double value, int r, int c)> entries = new List<(double value, int r, int c)>(N);

            double currentExtreme = initialValue;
            int extremeEntryIndex = -1;

            var comparer = new MinEntrySorter(operation);

            //Iterate over matrix and add values when smaller than prev values
            for (int r = 0; r < lhs.GetLength(0); r++)
            {
                for (int c = 0; c < lhs.GetLength(1); c++)
                {
                    if (entries.Count < N) //First elements, build initial list
                    {
                        if (operation(currentExtreme, lhs[r, c]))
                        {
                            currentExtreme = lhs[r, c];
                            extremeEntryIndex = entries.Count;
                        }
                        entries.Add((lhs[r, c], r, c));
                    }
                    else //Check if additional values is smaller than the existing largest one
                    {
                        if (operation(lhs[r, c], currentExtreme))
                        {
                            entries[extremeEntryIndex] = (lhs[r, c], r, c);

                            //Find new max index/value
                            int maxR = int.MaxValue, maxC = int.MaxValue;
                            currentExtreme = initialValue;

                            for (int i = 0; i < N; i++)
                            {
                                bool isExtremer = comparer.Compare(entries[i], (currentExtreme, maxR, maxC)) == 1;
                                if (isExtremer)
                                {
                                    currentExtreme = entries[i].value;
                                    extremeEntryIndex = i;
                                    maxR = entries[i].r;
                                    maxC = entries[i].c;
                                }
                            }
                        }
                        //Else do nothing
                    }
                }
            }

            //Order resultset
            entries.Sort(comparer);

            var result = new double[3 * N, 1];

            for (int i = 0; i < N; i++)
            {
                if (i < entries.Count)
                {
                    result[i * 3, 0] = entries[i].value;
                    result[i * 3 + 1, 0] = entries[i].r;
                    result[i * 3 + 2, 0] = entries[i].c;
                }
                else //Not enough entries in matrix
                {
                    result[i * 3, 0] = double.NaN;
                    result[i * 3 + 1, 0] = double.NaN;
                    result[i * 3 + 2, 0] = double.NaN;
                }
            }

            return result;
        }

        private class MinEntrySorter : IComparer<(double value, int r, int c)>
        {
            private Func<double, double, bool> operation;

            internal MinEntrySorter(Func<double, double, bool> operation)
            {
                this.operation = operation;
            }

            public int Compare((double value, int r, int c) x, (double value, int r, int c) y)
            {
                if (operation(x.value, y.value))
                    return -1;
                if (x.value == y.value)
                {
                    if (x.r < y.r)
                        return -1;
                    if (x.r == y.r)
                        if (x.c < y.c)
                            return -1;
                }

                return 1;
            }
        }

        #endregion

        #region Unary

        /// <summary>
        /// Returns a transposed matrix
        /// </summary>
        /// <param name="matrix">The input matrix</param>
        /// <returns>The transposed matrix</returns>
        internal static double[,] Transpose(double[,] matrix)
        {
            CheckMatrix(matrix, nameof(matrix));

            double[,] result = new double[matrix.GetLength(1), matrix.GetLength(0)];

            for (int r = 0; r < matrix.GetLength(1); r++)
            {
                for (int c = 0; c < matrix.GetLength(0); c++)
                {
                    result[r, c] = matrix[c, r];
                }
            }

            return result;
        }

        internal static double[,] Negate(double[,] matrix)
        {
            CheckMatrix(matrix, nameof(matrix));

            double[,] result = new double[matrix.GetLength(0), matrix.GetLength(1)];

            for (int r = 0; r < matrix.GetLength(0); r++)
            {
                for (int c = 0; c < matrix.GetLength(1); c++)
                {
                    result[r, c] = -matrix[r, c];
                }
            }

            return result;
        }

        #endregion

        private static double GetValueOrDefault(double[,] values, int row, int column, double defaultValue, bool repeatRows, bool repeatColumns)
        {
            var realRow = row;
            var realColumn = column;

            if (repeatRows)
                realRow = row.Clamp(0, values.GetLength(0) - 1);
            if (repeatColumns)
                realColumn = column.Clamp(0, values.GetLength(1) - 1);

            if (realRow < 0 || realColumn < 0 || realRow >= values.GetLength(0) || realColumn >= values.GetLength(1))
                return defaultValue;
            return values[realRow, realColumn];
        }

        /// <summary>
        /// Checks if the matrix is not-null, and not 0 sized
        /// </summary>
        /// <returns></returns>
        private static void CheckMatrix(double[,] matrix, string name)
        {
            if (matrix == null)
                throw new ArgumentNullException(name);
            if (matrix.GetLength(0) == 0 || matrix.GetLength(1) == 0)
                throw new ArgumentException("Matrix has to have at least one element");
        }
    }
}
