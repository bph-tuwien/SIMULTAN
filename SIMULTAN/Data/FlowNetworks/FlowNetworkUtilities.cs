using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.FlowNetworks
{
    #region ENUMS

    public enum SimFlowNetworkOperator
    {
        Addition = 0,
        Subtraction = 1,
        Multiplication = 2,
        Division = 3,
        Minimum = 4,
        Maximum = 5,
        Assignment = 6
    }

    public enum SimFlowNetworkCalcDirection
    {
        Forward = 0,
        Backward = 1
    }

    #endregion

    #region HELPER CLASSES

    public struct SimFlowNetworkCalcRule
    {
        public string Suffix_Result { get; set; }
        public string Suffix_Operands { get; set; }
        public SimFlowNetworkCalcDirection Direction { get; set; }
        public SimFlowNetworkOperator Operator { get; set; }

        #region STATIC

        public static string OperatorToString(SimFlowNetworkOperator _op)
        {
            switch (_op)
            {
                case SimFlowNetworkOperator.Addition:
                    return "+";
                case SimFlowNetworkOperator.Subtraction:
                    return "-";
                case SimFlowNetworkOperator.Multiplication:
                    return "*";
                case SimFlowNetworkOperator.Division:
                    return "/";
                case SimFlowNetworkOperator.Minimum:
                    return "Min";
                case SimFlowNetworkOperator.Maximum:
                    return "Max";
                case SimFlowNetworkOperator.Assignment:
                    return ":=";
                default:
                    return "+";
            }
        }

        public static SimFlowNetworkOperator StringToOperator(string _op_as_str)
        {
            if (string.IsNullOrEmpty(_op_as_str)) return SimFlowNetworkOperator.Addition;

            switch (_op_as_str)
            {
                case "+":
                    return SimFlowNetworkOperator.Addition;
                case "-":
                    return SimFlowNetworkOperator.Subtraction;
                case "*":
                    return SimFlowNetworkOperator.Multiplication;
                case "/":
                    return SimFlowNetworkOperator.Division;
                case "Min":
                    return SimFlowNetworkOperator.Minimum;
                case "Max":
                    return SimFlowNetworkOperator.Maximum;
                case ":=":
                    return SimFlowNetworkOperator.Assignment;
                default:
                    return SimFlowNetworkOperator.Addition;
            }
        }

        #endregion

        #region METHODS: Calculate

        public double Calculate(double _v1, double _v2)
        {
            switch (this.Operator)
            {
                case SimFlowNetworkOperator.Addition:
                    return _v1 + _v2;
                case SimFlowNetworkOperator.Subtraction:
                    return _v1 - _v2;
                case SimFlowNetworkOperator.Multiplication:
                    return _v1 * _v2;
                case SimFlowNetworkOperator.Division:
                    return _v1 / _v2;
                case SimFlowNetworkOperator.Minimum:
                    return Math.Min(_v1, _v2);
                case SimFlowNetworkOperator.Maximum:
                    return Math.Max(_v1, _v2);
                case SimFlowNetworkOperator.Assignment:
                    return _v2; // a hack, but works when called iteratively
                default:
                    return _v1 + _v2;
            }
        }

        #endregion
    }

    #endregion
}
