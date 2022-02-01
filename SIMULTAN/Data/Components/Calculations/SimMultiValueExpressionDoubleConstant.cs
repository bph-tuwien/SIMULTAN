using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.Components
{
    /// <summary>
    /// A scalar constant in the MultiValue expression tree
    /// </summary>
    public class SimMultiValueExpressionDoubleConstant : SimMultiValueExpression
    {
        private double[,] value;

        /// <summary>
        /// The value of the constant
        /// </summary>
        public double Value { get { return value[0, 0]; } }

        /// <summary>
        /// Initializes a new instance of the SimMultiValueExpressionDoubleConstant class
        /// </summary>
        /// <param name="value">The scalar value</param>
        public SimMultiValueExpressionDoubleConstant(double value)
        {
            this.value = new double[,] { { value } };
        }

        /// <inheritdoc />
        public override double[,] Calculate(SimCalculation calculation)
        {
            return value;
        }
    }
}
