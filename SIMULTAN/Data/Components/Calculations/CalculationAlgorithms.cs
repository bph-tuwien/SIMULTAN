using SIMULTAN.Utils;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.Components
{
    /// <summary>
    /// Enumerations for calculation validation results
    /// </summary>
    public enum CalculationValidationResult
    {
        /// <summary>
        /// A parameter bound to this calculation is not in the same subtree
        /// </summary>
        PARAMS_NOT_OF_THIS_OR_CHILD_COMP = 2,
        /// <summary>
        /// The parameter is already bound to the output of this or any other calculation
        /// </summary>
        PARAMS_OUT_DUPLICATE = 4,
        /// <summary>
        /// The parameter is also used as input to the calculation
        /// </summary>
        PARAMS_IN_OUT_SAME = 5,
        /// <summary>
        /// Happens when adding a new parameter would cause a loop in the calculation chain
        /// </summary>
        CAUSES_CALCULATION_LOOP = 7,
        /// <summary>
        /// Everything is fine with this calculation
        /// </summary>
        VALID = 8,
        /// <summary>
        /// A parameter with a wrong infoflow is used. (Happens only during parameter validation)
        /// </summary>
        PARAM_WRONG_INFOFLOW = 10,
    }


    /// <summary>
    /// Contains methods to adjust calculations
    /// </summary>
    public static class CalculationAlgorithms
    {
        /// <summary>
        /// Replaces a variable with a scalar value.
        /// The algorithm reconstructs the expression from the expression tree. This might introduce new brackets or change the formatting
        /// </summary>
        /// <param name="calculation">The calculation</param>
        /// <param name="parameter">The variable symbol which should be replaced</param>
        /// <param name="value">The value which should be used to replace the variable</param>
        public static void ReplaceParameter(this SimCalculation calculation, string parameter, double value)
        {
            if (parameter == null)
                throw new ArgumentNullException(nameof(parameter));

            ReplaceParameter(calculation, new Dictionary<string, double> { { parameter, value } });
        }
        /// <summary>
        /// Replaces variables with scalar values
        /// The algorithm reconstructs the expression from the expression tree. This might introduce new brackets or change the formatting
        /// </summary>
        /// <param name="calculation">The calculation</param>
        /// <param name="replaceParameters">Dictionary with replacements. Key is the variable symbol, value is the scalar replacement</param>
        public static void ReplaceParameter(this SimCalculation calculation, Dictionary<string, double> replaceParameters)
        {
            if (replaceParameters == null)
                throw new ArgumentNullException(nameof(replaceParameters));

            //Parse old expression
            try
            {
                var expression = new CalculationParser(CalculationParserFlags.None).ParseFunction(calculation.Expression);
                calculation.Expression = SpracheExpressionToString(expression, replaceParameters);
            }
            catch (Exception)
            {
                //Happens when the original expression wasn't valid
            }
        }

        /// <summary>
        /// Moves a calculation from the current component to another component.
        /// Tries to find matching parameters in the new component and replaces input as well as output if found
        /// </summary>
        /// <param name="calculation">The calculation to move</param>
        /// <param name="target">The component to which the calculation should be moved</param>
        public static void MoveCalculation(SimCalculation calculation, SimComponent target)
        {
            if (calculation == null)
                throw new ArgumentNullException(nameof(calculation));
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            //Remove from old location (if necessary)
            if (calculation.Component != null)
                calculation.Component.Calculations.Remove(calculation);

            //Add to new location
            target.Calculations.Add(calculation);

            //Try to find matching parameters in the new component
            UpdateParameters(calculation);
        }

        /// <summary>
        /// Duplicates a calculation into another component.
        /// Tries to find matching parameters in the new component and replaces input as well as output if found
        /// </summary>
        /// <param name="calculation">The calculation to copy</param>
        /// <param name="target">The component to which the calculation should be copied</param>
        /// <returns>The copied calculation</returns>
        public static SimCalculation CopyCalculation(SimCalculation calculation, SimComponent target)
        {
            if (calculation == null)
                throw new ArgumentNullException(nameof(calculation));
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            //Clone
            SimCalculation copy = new SimCalculation(calculation);

            //Add to new location
            target.Calculations.Add(copy);

            //Try to find matching parameters in the new component (unless it's the same component)
            if (target != calculation.Component)
                UpdateParameters(copy);

            return copy;
        }

        //Checks only for loops in the current component. Does not find loops involving calculations in multiple components
        /// <summary>
        /// Tests whether a calculation can be modified to use the replaced input and output parameters
        /// </summary>
        /// <param name="calculation"></param>
        /// <param name="replaceInput"></param>
        /// <param name="replaceReturn"></param>
        /// <returns></returns>
        public static CalculationValidationResult ValidateCalculationParameters(SimCalculation calculation,
            IEnumerable<KeyValuePair<string, SimParameter>> replaceInput,
            IEnumerable<KeyValuePair<string, SimParameter>> replaceReturn)
        {
            foreach (var entry in replaceInput)
            {
                SimParameter p = entry.Value;
                if (p != null)
                {
                    bool valid = IsInSubtree(calculation.Component, p);
                    if (!valid)
                        return CalculationValidationResult.PARAMS_NOT_OF_THIS_OR_CHILD_COMP;

                    if (p.Propagation == SimInfoFlow.Output)
                        return CalculationValidationResult.PARAM_WRONG_INFOFLOW;
                }
            }

            //Find output parameters of all equations here and above
            HashSet<SimParameter> usedOutParameters = new HashSet<SimParameter>();
            var parent = calculation.Component;
            while (parent != null)
            {
                foreach (var calc in parent.Calculations.Where(x => x != calculation))
                    foreach (var param in calc.ReturnParams.Where(x => x.Value != null))
                        usedOutParameters.Add(param.Value);
                parent = (SimComponent)parent.Parent;
            }

            foreach (var entry in replaceReturn)
            {
                SimParameter p = entry.Value;
                if (p != null)
                {
                    // output parameters cannot be input
                    if (replaceInput.Any(x => x.Value == p))
                    {
                        return CalculationValidationResult.PARAMS_IN_OUT_SAME;
                    }

                    bool valid = IsInSubtree(calculation.Component, p);
                    if (!valid)
                    {
                        return CalculationValidationResult.PARAMS_NOT_OF_THIS_OR_CHILD_COMP;
                    }

                    if (p.Propagation != SimInfoFlow.Output && p.Propagation != SimInfoFlow.Mixed)
                        return CalculationValidationResult.PARAM_WRONG_INFOFLOW;

                    if (usedOutParameters.Contains(p))
                        return CalculationValidationResult.PARAMS_OUT_DUPLICATE;

                    var otherCalculations = p.GetReferencingCalculations();
                    if (otherCalculations.Any(x => x != calculation && (x.ReturnParams.ContainsValue(p))))
                        return CalculationValidationResult.PARAMS_OUT_DUPLICATE;
                }
            }

            //Check if loops would be present in the calculation tree
            var ordered = OrderCalculationsByDependency(calculation.Component.Calculations,
                x => x == calculation ? (IEnumerable<KeyValuePair<string, SimParameter>>)replaceInput : x.InputParams,
                x => x == calculation ? (IEnumerable<KeyValuePair<string, SimParameter>>)replaceReturn : x.ReturnParams);

            if (ordered == null)
                return CalculationValidationResult.CAUSES_CALCULATION_LOOP;

            return CalculationValidationResult.VALID;
        }

        private static bool IsInSubtree(SimComponent rootComponent, SimParameter parameter)
        {
            var comp = parameter.Component;

            while (comp != null)
            {
                if (comp == rootComponent)
                    return true;
                comp = (SimComponent)comp.Parent;
            }

            return false;
        }

        /// <summary>
        /// Reorders the Calculations in this component such that calculations that depend on the output of others are stored first.
        /// </summary>
        /// <param name="component">The component</param>
        /// <returns>When the calculations can be ordered, True, otherwise False</returns>
        public static bool OrderCalculations(SimComponent component)
        {
            var sortedList = OrderCalculationsByDependency(component.Calculations,
                x => x.InputParams, x => x.ReturnParams);

            if (sortedList != null)
            {
                //Move items into correct locations
                for (int i = 0; i < sortedList.Count; ++i)
                {
                    component.Calculations.Move(component.Calculations.IndexOf(sortedList[i]), i);
                }

                return true;
            }

            return false;
        }



        #region Utils

        private static List<SimCalculation> OrderCalculationsByDependency(IEnumerable<SimCalculation> calculations,
            Func<SimCalculation, IEnumerable<KeyValuePair<string, SimParameter>>> inputSelector,
            Func<SimCalculation, IEnumerable<KeyValuePair<string, SimParameter>>> returnSelector)
        {
            //Gather return parameters
            Dictionary<SimParameter, List<SimCalculation>> returnParameters = new Dictionary<SimParameter, List<SimCalculation>>();
            foreach (var calc in calculations)
            {
                foreach (var returnParam in returnSelector(calc).Where(x => x.Value != null))
                {
                    if (returnParameters.TryGetValue(returnParam.Value, out var paramCalcList))
                        paramCalcList.Add(calc);
                    else
                        returnParameters.Add(returnParam.Value, new List<SimCalculation> { calc });
                }
            }

            HashSet<SimCalculation> remainingCalculations = calculations.ToHashSet();

            //Build dependency list
            Dictionary<SimCalculation, HashSet<SimCalculation>> dependencies = new Dictionary<SimCalculation, HashSet<SimCalculation>>();
            foreach (var calc in calculations)
            {
                dependencies.Add(calc, inputSelector(calc).Where(x => x.Value != null && returnParameters.ContainsKey(x.Value))
                    .SelectMany(x => returnParameters[x.Value]).ToHashSet());
            }


            List<SimCalculation> result = new List<SimCalculation>();
            //Add all calculations where no input is contained in output
            while (dependencies.Count > 0)
            {
                if (!dependencies.TryFirstOrDefault(x => x.Value.Count == 0, out var firstWithoutDependency))
                    return null;

                result.Add(firstWithoutDependency.Key);

                dependencies.Remove(firstWithoutDependency.Key);
                dependencies.ForEach(x => x.Value.Remove(firstWithoutDependency.Key));
            }

            return result;
        }


        private static string SpracheExpressionToString(Expression<Func<Dictionary<string, double>, double>> expression,
            Dictionary<string, double> replaceParameters)
        {
            var expr = SpracheNodeToString(expression.Body, replaceParameters);
            return expr.Substring(1, expr.Length - 2);
        }
        private static string SpracheNodeToString(Expression expression, Dictionary<string, double> replaceParameters)
        {
            if (expression is BinaryExpression binExpr)
                return SpracheBinaryToString(binExpr, replaceParameters);
            else if (expression is MethodCallExpression methodExpr)
                return SpracheMethodCallToString(methodExpr, replaceParameters);
            else
                return expression.ToString();
        }
        private static string SpracheBinaryToString(BinaryExpression expression, Dictionary<string, double> replaceParameters)
        {
            string expr = expression.ToString();

            string leftOriginal = expression.Left.ToString();
            string leftExpr = SpracheNodeToString(expression.Left, replaceParameters);
            string rightOriginal = expression.Right.ToString();
            string rightExpr = SpracheNodeToString(expression.Right, replaceParameters);

            expr = ReplaceFirst(expr, leftOriginal, leftExpr);
            expr = ReplaceLast(expr, rightOriginal, rightExpr);

            return expr;
        }
        private static string SpracheMethodCallToString(MethodCallExpression expression, Dictionary<string, double> replaceParameters)
        {
            //Parameter
            if (expression.Method.Name == "get_Item" && expression.Object.NodeType == ExpressionType.Parameter)
            {
                if (expression.Arguments[0] is ConstantExpression constant)
                {
                    var paramKey = constant.Value.ToString();
                    if (replaceParameters.TryGetValue(paramKey, out var paramValue))
                        return paramValue.ToString(CultureInfo.InvariantCulture);
                    else
                        return paramKey;
                }
                return expression.Arguments[0].ToString();
            }
            else if (expression.Method.Name == nameof(CalculationParser.GetMathConstant) && expression.Object == null)
            {
                if (expression.Arguments[0] is ConstantExpression constant)
                    return constant.Value.ToString();
                return expression.Arguments[0].ToString();
            }
            //Other method call
            else
            {
                string expr = expression.ToString();

                foreach (var arg in expression.Arguments)
                {
                    string orig = arg.ToString();
                    string repl = SpracheNodeToString(arg, replaceParameters);

                    expr = expr.Replace(orig, repl);
                }

                return expr;
            }
        }

        private static string ReplaceFirst(string source, string search, string replace)
        {
            return ReplaceOnce(source, search, replace, source.IndexOf(search));
        }
        private static string ReplaceLast(string source, string search, string replace)
        {
            return ReplaceOnce(source, search, replace, source.LastIndexOf(search));
        }
        private static string ReplaceOnce(string source, string search, string replace, int index)
        {
            return source.Substring(0, index) + replace + source.Substring(index + search.Length);
        }

        private static void UpdateParameters(SimCalculation calculation)
        {
            foreach (var entry in calculation.InputParams.ToList())
            {
                if (entry.Value != null)
                {
                    SimParameter p = calculation.Component.GetCorresponding(entry.Value);
                    if (p != null)
                        calculation.InputParams[entry.Key] = p;
                    else if (!IsInSubtree(calculation.Component, entry.Value))
                        calculation.InputParams[entry.Key] = null;
                }
            }

            foreach (var entry in calculation.ReturnParams.ToList())
            {
                if (entry.Value != null)
                {
                    SimParameter p = calculation.Component.GetCorresponding(entry.Value);
                    if (p != null && !IsUsedAsOutputBySelfOrParent(calculation.Component, p, calculation))
                        calculation.ReturnParams[entry.Key] = p;
                    else if (!IsInSubtree(calculation.Component, entry.Value) || IsUsedAsOutputBySelfOrParent(calculation.Component, entry.Value, calculation))
                        calculation.ReturnParams[entry.Key] = null;


                }
            }
        }

        private static bool IsUsedAsOutputBySelfOrParent(SimComponent _comp, SimParameter _p, SimCalculation _calc_to_exclude)
        {
            if (_p == null)
                return false;

            foreach (var calc in _comp.Calculations)
            {
                if (calc != _calc_to_exclude && calc.ReturnParams.ContainsValue(_p))
                    return true;
            }

            if (_comp.Parent != null)
            {
                bool in_parent = IsUsedAsOutputBySelfOrParent((SimComponent)_comp.Parent, _p, _calc_to_exclude);
                if (in_parent)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Looks for the first parameter in the component or one of its subcomponents,
        /// whose name, unit and propagation match those of the query parameter.
        /// </summary>
        /// <param name="_comp">The component instance</param>
        /// <param name="_p">The parameter, whose corresponding we are looking for</param>
        /// <returns>a parameter or null</returns>
        private static SimParameter GetCorresponding(this SimComponent _comp, SimParameter _p)
        {
            if (_p == null || _p.Component == null)
                return null;

            // look locally first
            SimParameter p_corresp = _comp.Parameters.FirstOrDefault(
                x => x.Name == _p.Name &&
                     x.Unit == _p.Unit &&
                     x.Propagation == _p.Propagation
                );

            if (p_corresp != null)
                return p_corresp;

            // look in the sub-components
            foreach (var entry in _comp.Components)
            {
                SimComponent sComp = entry.Component;
                if (sComp == null) continue;

                p_corresp = sComp.GetCorresponding(_p);
                if (p_corresp != null)
                    return p_corresp;
            }

            return p_corresp;
        }

        #endregion
    }
}
