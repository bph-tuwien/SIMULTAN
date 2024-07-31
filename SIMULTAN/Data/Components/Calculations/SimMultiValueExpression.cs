using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SIMULTAN.Data.Components
{
    /// <summary>
    /// Base class for MulitValue expression tree elements
    /// </summary>
    public abstract class SimMultiValueExpression
    {
        /// <summary>
        /// Calculates the result of the subtree
        /// </summary>
        /// <param name="calculation">The calculation to which the expression tree belongs</param>
        /// <returns>A matrix containing the result of the subtree. Scalar values are returned as a 1x1 matrix.
        /// Has to return a non-empty matrix in all valid use cases</returns>
        public abstract double[,] Calculate(SimCalculation calculation);

        /// <summary>
        /// Creates a deep copy of the expression
        /// </summary>
        /// <returns>A copy of the expression</returns>
        public abstract SimMultiValueExpression Clone();
    }
}
