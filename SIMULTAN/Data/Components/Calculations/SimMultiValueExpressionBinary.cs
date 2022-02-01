using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SIMULTAN.Data.Components
{
    /// <summary>
    /// A binary function in the MultiValue expression tree
    /// </summary>
    public class SimMultiValueExpressionBinary : SimMultiValueExpression, INotifyPropertyChanged
    {
        /// <summary>
        /// The lefthand side subexpression
        /// </summary>
        public SimMultiValueExpression Left { get; set; }
        /// <summary>
        /// The righthand side subexpression
        /// </summary>
        public SimMultiValueExpression Right { get; set; }

        /// <summary>
        /// The operation this function should perform
        /// </summary>
        public MultiValueCalculationBinaryOperation Operation { get { return operation; } set { if (operation != value) { operation = value; NotifyPropertyChanged(nameof(Operation)); } } }
        private MultiValueCalculationBinaryOperation operation;

        /// <summary>
        /// Initializes a new instance of the SimMultiValueExpressionBinary class
        /// </summary>
        /// <param name="operation">The operation this function performs</param>
        public SimMultiValueExpressionBinary(MultiValueCalculationBinaryOperation operation)
        {
            this.Operation = operation;
        }

        /// <inheritdoc />
		public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(string prop)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        /// <inheritdoc />
        public override double[,] Calculate(SimCalculation calculation)
        {
            if (Left != null && Right != null)
            {
                var leftResult = Left.Calculate(calculation);
                var rightResult = Right.Calculate(calculation);

                switch (Operation)
                {
                    case MultiValueCalculationBinaryOperation.MATRIX_SUM:
                        return MultiValueCalculationsNEW.MatrixSum(leftResult, rightResult, false, false);
                    case MultiValueCalculationBinaryOperation.MATRIX_SUM_REPEAT_COLUMN:
                        return MultiValueCalculationsNEW.MatrixSum(leftResult, rightResult, false, true);
                    case MultiValueCalculationBinaryOperation.MATRIX_SUM_REPEAT_ROWCOLUMN:
                        return MultiValueCalculationsNEW.MatrixSum(leftResult, rightResult, true, true);

                    case MultiValueCalculationBinaryOperation.INNER_PRODUCT:
                        return MultiValueCalculationsNEW.InnerProduct(leftResult, rightResult);
                    case MultiValueCalculationBinaryOperation.OUTER_PRODUCT:
                        return MultiValueCalculationsNEW.OuterProduct(leftResult, rightResult);
                    case MultiValueCalculationBinaryOperation.OUTER_PRODUCT_FLAT:
                        return MultiValueCalculationsNEW.OuterProductFlat(leftResult, rightResult);

                    case MultiValueCalculationBinaryOperation.MATRIX_PRODUCT:
                        return MultiValueCalculationsNEW.MatrixProduct(leftResult, rightResult);
                    case MultiValueCalculationBinaryOperation.MATRIX_PRODUCT_PERELEMENT:
                        return MultiValueCalculationsNEW.MatrixElementwiseProduct(leftResult, rightResult, false, false);
                    case MultiValueCalculationBinaryOperation.MATRIX_PRODUCT_PERELEMENT_REPEAT:
                        return MultiValueCalculationsNEW.MatrixElementwiseProduct(leftResult, rightResult, true, true);

                    case MultiValueCalculationBinaryOperation.COLUMN_SELECTION:
                        return MultiValueCalculationsNEW.SelectColumns(leftResult, rightResult);
                    case MultiValueCalculationBinaryOperation.COLUMN_SELECTION_AS_MATRIX:
                        return MultiValueCalculationsNEW.SelectColumnsAsMatrix(leftResult, rightResult);
                    case MultiValueCalculationBinaryOperation.COLUMN_SELECTION_AS_DIAGONAL:
                        return MultiValueCalculationsNEW.SelectColumnsAsDiagonalMatrix(leftResult, rightResult);

                    case MultiValueCalculationBinaryOperation.CATEGORY_SUM:
                        return MultiValueCalculationsNEW.GroupBySum(leftResult, rightResult);
                    case MultiValueCalculationBinaryOperation.CATEGORY_AVERAGE:
                        return MultiValueCalculationsNEW.GroupByAverage(leftResult, rightResult);
                    case MultiValueCalculationBinaryOperation.CATEGORY_MIN:
                        return MultiValueCalculationsNEW.GroupByMin(leftResult, rightResult);
                    case MultiValueCalculationBinaryOperation.CATEGORY_MAX:
                        return MultiValueCalculationsNEW.GroupByMax(leftResult, rightResult);

                    case MultiValueCalculationBinaryOperation.EXTREME_MIN_OF_MATRIX:
                        return MultiValueCalculationsNEW.SelectMinIndex(leftResult, rightResult);
                    case MultiValueCalculationBinaryOperation.EXTREME_MAX_OF_MATRIX:
                        return MultiValueCalculationsNEW.SelectMaxIndex(leftResult, rightResult);
                }
            }

            return new double[0, 0];
        }
    }
}
