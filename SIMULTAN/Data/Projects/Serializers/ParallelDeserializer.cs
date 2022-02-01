using SIMULTAN.Serializer.DXF;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Projects.Serializers
{
    /// <summary>
    /// Fast method to deserialize a large number of doubles from string.
    /// Used to load SimMultiValueBigTables
    /// </summary>
    public class ParallelBigTableDeserializer
    {
        /// <summary>
        /// Parses a list of rows.
        /// Each row contains a number of serialized doubles, separated by delimiter
        /// </summary>
        /// <param name="rows">List of serialized rows</param>
        /// <param name="delimiter">The delimiter string</param>
        /// <param name="rowCount">Number of rows</param>
        /// <param name="columnCount">The number of columns in each row</param>
        /// <returns></returns>
        public List<List<double>> Parse(List<string> rows, string delimiter, int rowCount, int columnCount)
        {
            if (rows == null)
                throw new NullReferenceException("The data cannot be null!");
            if (rows.Count != rowCount)
                throw new ArgumentException("The number of rows to deserialize does not correspond to the passed data!");

            int maxChunkSize = (365 * 24 * 4);
            int maxNumberOfChunks = int.MaxValue;
            int numberOfChunks = Math.Min(Math.Min(maxNumberOfChunks - 1, (int)Math.Ceiling(rowCount * columnCount / (double)maxChunkSize)), rowCount - 1);
            if (numberOfChunks <= 1)
            {
                List<List<double>> result = new List<List<double>>(rowCount);
                foreach (var row in rows)
                {
                    result.Add(ParseRow(row, columnCount, delimiter));
                }
                return result;
            }
            else
            {
                List<Task<List<List<double>>>> tasks = new List<Task<List<List<double>>>>(numberOfChunks);
                double rowsPerChunk = rowCount / (double)numberOfChunks;
                for (int i = 0; i < numberOfChunks; ++i)
                {
                    int localI = i;
                    tasks.Add(Task.Run(async () => await ParseRowsAsync(localI, rowsPerChunk, rows, rowCount, columnCount, delimiter)));
                }

                List<List<double>> result = new List<List<double>>(rowCount);

                Task.WhenAll(tasks);

                foreach (var task in tasks)
                    result.AddRange(task.Result);

                return result;
            }
        }

        private List<double> ParseRow(string row, int columnCount, string delimiter)
        {
            List<double> values = new List<double>(columnCount);

            int match = row.IndexOf(delimiter);
            int lastMatchEnd = 0;
            while (match != -1)
            {
                string cell = row.Substring(lastMatchEnd, match - lastMatchEnd);
                values.Add(DXFDecoder.StringToDouble(cell));
                lastMatchEnd = match + delimiter.Length;
                match = row.IndexOf(delimiter, lastMatchEnd);
            }
            //last part
            if (lastMatchEnd < row.Length)
                values.Add(DXFDecoder.StringToDouble(row.Substring(lastMatchEnd)));

            return values;
        }

        private async Task<List<List<double>>> ParseRowsAsync(int chunk, double chunkSizeInRows, List<string> rows, int rowCount, int columnCount, string delimiter)
        {
            int startRow = (int)Math.Floor(chunk * chunkSizeInRows);
            int endRow = Math.Min((int)Math.Floor((chunk + 1) * chunkSizeInRows), rowCount);

            List<List<double>> result = new List<List<double>>(endRow - startRow);

            for (int i = startRow; i < endRow; ++i)
                result.Add(ParseRow(rows[i], columnCount, delimiter));

            return result;
        }
    }
}
