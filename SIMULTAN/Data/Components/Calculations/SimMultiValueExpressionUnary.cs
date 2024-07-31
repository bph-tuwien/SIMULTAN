using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.Components
{
    /// <summary>
    /// A unary function in the MultiValue expression tree
    /// </summary>
    public class SimMultiValueExpressionUnary : SimMultiValueExpression
    {
        /// <summary>
        /// The operand of the unary function
        /// </summary>
        public SimMultiValueExpression Operand { get; set; }

        /// <summary>
        /// The operation to perform
        /// </summary>
        public MultiValueCalculationUnaryOperation Operation { get { return operation; } set { operation = value; } }
        private MultiValueCalculationUnaryOperation operation;

        /// <summary>
        /// Initializes a new instance of the SimMultiValueExpressionUnary class
        /// </summary>
        /// <param name="operation">The operation this node should perform</param>
        public SimMultiValueExpressionUnary(MultiValueCalculationUnaryOperation operation)
        {
            this.Operation = operation;
        }

        /// <inheritdoc/>
        public override double[,] Calculate(SimCalculation calculation)
        {
            if (Operand != null)
            {
                var opResult = Operand.Calculate(calculation);

                switch (Operation)
                {
                    case MultiValueCalculationUnaryOperation.Transpose:
                        return MultiValueCalculationsNEW.Transpose(opResult);
                    case MultiValueCalculationUnaryOperation.Negate:
                        return MultiValueCalculationsNEW.Negate(opResult);
                }
            }

            return new double[0, 0];
        }

        /// <inheritdoc/>
        public override SimMultiValueExpression Clone()
        {
            return new SimMultiValueExpressionUnary(Operation)
            {
                Operand = Operand.Clone(),
            };
        }
    }
}
