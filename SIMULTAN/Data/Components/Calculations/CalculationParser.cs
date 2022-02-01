using Sprache.Calc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using ParameterList = System.Collections.Generic.Dictionary<string, double>;

namespace SIMULTAN.Data.Components
{
    /// <summary>
    /// Flags which control the behavior of the CalculationParser
    /// </summary>
    [Flags]
    public enum CalculationParserFlags
    {
        /// <summary>
        /// No special behavior
        /// </summary>
        None = 0,
        /// <summary>
        /// Optimizes the resulting expression tree by converting System.Math constants into their numeric values instead of querying them each time
        /// </summary>
        OptimizeConstants = 1,
        /// <summary>
        /// Enables all other optimizations
        /// </summary>
        FullOptimization = OptimizeConstants,
    }

    /// <summary>
    /// Extended parser which supports Math constants and adds a Transpose method
    /// </summary>
    public class CalculationParser : XtensibleCalculator
    {
        private CalculationParserFlags flags;
        private static Dictionary<string, double> mathConstants = new Dictionary<string, double>();

        /// <summary>
        /// Initializes a new instance of the CalculationParser class
        /// </summary>
        /// <param name="flags">Flags to enable/disable specific features</param>
        public CalculationParser(CalculationParserFlags flags = CalculationParserFlags.None)
        {
            this.flags = flags;
            this.RegisterFunction("Transpose", TransposePlaceholder);
        }

        /// <summary>
        /// A list of all parameter symbols used in the expression
        /// </summary>
        public List<string> Parameters { get; } = new List<string>();

        /// <inheritdoc />
        protected override Expression GetParameterExpression(string name)
        {
            //Code from https://github.com/yallie/Sprache.Calc/blob/master/Sprache.Calc/XtensibleCalculator.cs

            //Check if known constant
            if (mathConstants.ContainsKey(name))
            {
                return ConstantExpression(name);
            }
            else
            {
                // try to find a constant in System.Math
                var systemMathConstants = typeof(System.Math).GetFields(BindingFlags.Public | BindingFlags.Static);
                var constant = systemMathConstants.FirstOrDefault(c => c.Name == name);
                if (constant != null)
                {
                    mathConstants.Add(name, (double)constant.GetValue(null));
                    // return System.Math constant value
                    return ConstantExpression(name);
                }
                else
                {
                    //It's a parameter
                    Parameters.Add(name);

                    // return parameter value: Parameters[name]
                    var getItemMethod = typeof(ParameterList).GetMethod("get_Item");
                    return Expression.Call(ParameterExpression, getItemMethod, Expression.Constant(name));
                }
            }
        }

        /// <summary>
        /// Returns the value of a Math namespace constant
        /// </summary>
        /// <param name="constant">The constant</param>
        /// <returns>The numerical value of the constant, or NaN when no such constant exists</returns>
        public static double GetMathConstant(string constant)
        {
            if (mathConstants.TryGetValue(constant, out var constVal))
                return constVal;

            return double.NaN;
        }

        private Expression ConstantExpression(string name)
        {
            if (this.flags.HasFlag(CalculationParserFlags.OptimizeConstants))
            {
                return Expression.Constant(mathConstants[name]);
            }
            else
            {
                return Expression.Call(null,
                    typeof(CalculationParser).GetMethod(nameof(GetMathConstant), BindingFlags.Static | BindingFlags.Public),
                    Expression.Constant(name));
            }
        }

        /// <summary>
        /// Returns True when the expression is a parameter query expression
        /// </summary>
        /// <param name="expression">The sub expression to check</param>
        /// <returns>True when this expression is a parameter query expression, otherwise False</returns>
        public static bool IsParameterExpression(Expression expression)
        {
            if (expression is MethodCallExpression methodExpr)
                return methodExpr.Method.Name == "get_Item" && methodExpr.Object.NodeType == ExpressionType.Parameter;
            return false;
        }

        /// <summary>
        /// Used when Transpose is called in a non-vector equation
        /// </summary>
        /// <param name="d">Input value</param>
        /// <returns>The input value (unmodified)</returns>
        public static double TransposePlaceholder(double d)
        {
            return d;
        }
    }
}
