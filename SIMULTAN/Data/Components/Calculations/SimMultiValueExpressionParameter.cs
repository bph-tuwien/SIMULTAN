using SIMULTAN.Data.MultiValues;
using SIMULTAN.Utils;

namespace SIMULTAN.Data.Components
{
    /// <summary>
    /// A variable in the MultiValue expression tree.
    /// Actual values are taken from the input parameters of the calculation
    /// </summary>
    public class SimMultiValueExpressionParameter : SimMultiValueExpression
    {
        /// <summary>
        /// The variable symbol
        /// </summary>
        public string Symbol { get; set; }

        /// <summary>
        /// Initializes a new instance of the SimMultiValueExpressionParameter class
        /// </summary>
        /// <param name="symbol">The variable symbol</param>
        public SimMultiValueExpressionParameter(string symbol)
        {
            this.Symbol = symbol;
        }

        /// <inheritdoc />
        public override double[,] Calculate(SimCalculation calculation)
        {
            //Handle range, randomize
            if (calculation.InputParams.TryGetValue(Symbol, out var param) && param != null)
            {
                double[,] values = null;
                var metaData = calculation.InputParams.GetMetaData(Symbol);

                //Handle reference
                if (param.Propagation == SimInfoFlow.FromReference)
                {
                    var referencedParameter = param.GetReferencedParameter();
                    if (referencedParameter is SimDoubleParameter referenceDoubleParam)
                        param = referenceDoubleParam;
                }

                //Return ValueCurrent unless a Table is attached
                if (param.ValueSource != null && param.ValueSource is SimMultiValueBigTableParameterSource ptr)
                {
                    values = ptr.Table.GetDoubleRange(metaData.Range);
                }
                else
                {
                    values = new double[,] { { param.Value } };
                }

                if (metaData.IsRandomized)
                {
                    for (int i = 0; i < values.GetLength(0); i++)
                    {
                        for (int j = 0; j < values.GetLength(1); j++)
                        {
                            values[i, j] = Randomize(values[i, j], metaData);
                        }
                    }
                }

                return values;
            }
            else
                return new double[,] { }; //Should actually never happen
        }

        private double Randomize(double value, CalculationParameterMetaData meta)
        {
            var rand = CalculationParameterMetaData.Randomizer.Next();

            var mean = meta.RandomizeRelativeMean * value;
            var deviation = meta.RandomizeDeviation;
            if (meta.RandomizeDeviationMode == CalculationParameterMetaData.DeviationModeType.Relative)
                deviation *= value;

            var result = mean + rand * deviation;

            if (meta.RandomizeIsClamping)
                result = result.Clamp(mean - meta.RandomizeClampDeviation * deviation, mean + meta.RandomizeClampDeviation * deviation);

            return result;
        }

        /// <inheritdoc />
        public override SimMultiValueExpression Clone()
        {
            return new SimMultiValueExpressionParameter(Symbol);
        }
    }
}
