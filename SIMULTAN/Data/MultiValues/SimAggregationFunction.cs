using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.MultiValues
{
    /// <summary>
    /// Provides different functions which can be used to aggregate data in ValueFields
    /// </summary>
    public enum SimAggregationFunction
    {
        /// <summary>
        /// Sums up all values
        /// </summary>
        Sum,
        /// <summary>
        /// Calculates the average of all values
        /// </summary>
        Average,
        /// <summary>
        /// Calculates the minimum of all values
        /// </summary>
        Min,
        /// <summary>
        /// Calculates the maximum of all values
        /// </summary>
        Max,
        /// <summary>
        /// Calculates the number of values present
        /// </summary>
        Count,
    }

    /// <summary>
    /// Provides extension methods for the SimAggregationFunction Type
    /// </summary>
    public static class SimAggregationFunctionExtensions
    {
        private static Dictionary<SimAggregationFunction, string> enumToString = new Dictionary<SimAggregationFunction, string>
        {
            { SimAggregationFunction.Sum, "SUM" },
            { SimAggregationFunction.Average, "AVG" },
            { SimAggregationFunction.Min, "MIN" },
            { SimAggregationFunction.Max, "MAX" },
            { SimAggregationFunction.Count, "COUNT" },
        };
        private static Dictionary<string, SimAggregationFunction> stringToEnum = new Dictionary<string, SimAggregationFunction>()
        {
            { "SUM", SimAggregationFunction.Sum },
            { "AVG", SimAggregationFunction.Average },
            { "MIN", SimAggregationFunction.Min },
            { "MAX", SimAggregationFunction.Max },
            { "COUNT", SimAggregationFunction.Count },
        };

        /// <summary>
        /// Returns a string representation for a given  SimAggregationFunction
        /// </summary>
        /// <param name="aggregationFunction">The aggregation function which should be converted to a string</param>
        /// <returns>A string representation of aggregationFunction</returns>
        public static string ToStringRepresentation(this SimAggregationFunction aggregationFunction)
        {
            return enumToString[aggregationFunction];
        }
        /// <summary>
        /// Returns a SimAggregationFunction based on their string representation
        /// </summary>
        /// <param name="representation">The string representation</param>
        /// <returns>The SimAggregationFunction which is described by representation</returns>
        public static SimAggregationFunction FromStringRepresentation(string representation)
        {
            return stringToEnum[representation];
        }
    }
}
