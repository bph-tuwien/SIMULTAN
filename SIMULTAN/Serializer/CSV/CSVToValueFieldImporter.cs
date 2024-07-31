using Microsoft.VisualBasic.FileIO;
using SIMULTAN.Data.MultiValues;
using SIMULTAN.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SIMULTAN.Serializer.CSV
{
    /// <summary>
    /// Stores settings for the <see cref="CSVToValueFieldImporter"/>
    /// </summary>
    public class CSVToValueFieldImporterSettings
    {
        /// <summary>
        /// The name of the table
        /// </summary>
        public string Name { get; set; } = "";

        /// <summary>
        /// The character used as a delimiter between consecutive values
        /// </summary>
        public string Delimiter { get; set; } = "";
        /// <summary>
        /// When set to True, the first column is treated as a header
        /// </summary>
        public bool HasRowHeaders { get; set; } = false;
        /// <summary>
        /// When set to True, the first column (or second column in case <see cref="HasRowHeaders"/> is also True) is treated as a unit
        /// </summary>
        public bool HasRowUnits { get; set; } = false;
        /// <summary>
        /// When set to True, the first row is treated as a header
        /// </summary>
        public bool HasColumnHeaders { get; set; } = false;
        /// <summary>
        /// When set to True, the first row (or second row in case <see cref="HasColumnHeaders"/> is also True) is treated as a unit
        /// </summary>
        public bool HasColumnUnits { get; set; } = false;
        /// <summary>
        /// The character used as a decimal separator
        /// </summary>
        public string DecimalSeparator { get; set; } = ".";
    }

    /// <summary>
    /// This class is a helper class to hold the different logical
    /// components of a MultiValueBigTable without creating one
    /// </summary>
    public class MultiValueBigTableComponents
    {
        /// <summary>
        /// The column headers
        /// </summary>
        public List<SimMultiValueBigTableHeader> ColumnHeaders;
        /// <summary>
        /// The row headers
        /// </summary>
        public List<SimMultiValueBigTableHeader> RowHeaders;
        /// <summary>
        /// The values in the table
        /// </summary>
        public List<List<object>> Values;

        /// <summary>
        /// Initializes an instance of a MultiValueBigTableComponents class
        /// </summary>
        /// <param name="columnHeaders">The column headers</param>
        /// <param name="rowHeaders">The row headers</param>
        /// <param name="values">Values</param>
        public MultiValueBigTableComponents(List<SimMultiValueBigTableHeader> columnHeaders, List<SimMultiValueBigTableHeader> rowHeaders,
            List<List<object>> values)
        {
            this.ColumnHeaders = columnHeaders;
            this.RowHeaders = rowHeaders;
            this.Values = values;
        }
    }

    /// <summary>
    /// Class which holds functions to import CSV to MultiValueBigTable
    /// </summary>
    public static class CSVToValueFieldImporter
    {
        /// <summary>
        /// Loads a CSV file and parses it's content according to the settings provided
        /// </summary>
        /// <param name="collection">The collection to which the ValueField should be added</param>
        /// <param name="file">The CSV file to import</param>
        /// <param name="settings">The settings according to which the data should be parsed</param>
        /// <param name="unitRows">The row unit text</param>
        /// <param name="unitColumns">The column unit text</param>
        /// <param name="rowHeaderFormat">The format of new row headers in case they are not loaded from the file. {0}: index of the row </param>
        /// <param name="rowHeaderUnit">The unit of new row headers in case they are not loaded from the file.</param>
        /// <param name="columnHeaderFormat">The format of new column headers in case they are not loaded from the file. {0}: index of the row </param>
        /// <param name="columnHeaderUnit">The unit of new column headers in case they are not loaded from the file.</param>
        /// <param name="dispatcher">The dispatcher in which the import has to happen</param>
        /// <returns>The imported table</returns>
        public static SimMultiValueBigTable Import(SimMultiValueCollection collection, FileInfo file, CSVToValueFieldImporterSettings settings,
            string unitRows, string unitColumns, string rowHeaderFormat, string rowHeaderUnit, string columnHeaderFormat, string columnHeaderUnit,
            ISynchronizeInvoke dispatcher = null)
        {
            var components = ConvertCsvToMultiValueBigTableComponents(file, settings,
                rowHeaderFormat, rowHeaderUnit, columnHeaderFormat, columnHeaderUnit);

            var result = new SimMultiValueBigTable(settings.Name, unitRows, unitColumns,
                components.ColumnHeaders, components.RowHeaders, components.Values);
            if (dispatcher == null)
            {
                collection.Add(result);
            }
            else
            {
                dispatcher.Invoke(() => collection.Add(result), null);
            }
            return result;
        }

        /// <summary>
        /// Creates the MultiValueBigTableComponents class out of a .csv for later use. 
        /// </summary>
        /// <param name="file">The CSV file which should be loaded</param>
        /// <param name="settings">The settings for loading the file</param>
        /// <param name="rowHeaderFormat">The format of new row headers in case they are not loaded from the file. {0}: index of the row </param>
        /// <param name="rowHeaderUnit">The unit of new row headers in case they are not loaded from the file.</param>
        /// <param name="columnHeaderFormat">The format of new column headers in case they are not loaded from the file. {0}: index of the row </param>
        /// <param name="columnHeaderUnit">The unit of new column headers in case they are not loaded from the file.</param>
        /// <returns>MultiValueBigTableComponents - temporary class to hold the row headers and column headers and values
        /// of a MultiValueBigtable without actually creating it</returns>
        public static MultiValueBigTableComponents ConvertCsvToMultiValueBigTableComponents(
            FileInfo file, CSVToValueFieldImporterSettings settings,
            string rowHeaderFormat, string rowHeaderUnit, string columnHeaderFormat, string columnHeaderUnit)
        {
            if (file == null)
                throw new ArgumentNullException(nameof(file));
            if (!file.Exists)
                throw new FileNotFoundException(file.FullName);
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));
            if (string.IsNullOrWhiteSpace(settings.Delimiter))
                throw new ArgumentException("delimiter may not be empty");
            if (string.IsNullOrWhiteSpace(settings.DecimalSeparator))
                throw new ArgumentException("DecimalSeparator may not be empty");
            if (string.IsNullOrWhiteSpace(settings.Name))
                throw new ArgumentException("Name may not be empty");


            List<List<string>> table = new List<List<string>>();
            var encoding = FileHelper.GetEncoding(file.FullName);

            using (TextFieldParser parser = new TextFieldParser(file.FullName, encoding))
            {
                parser.TextFieldType = FieldType.Delimited;
                parser.SetDelimiters(settings.Delimiter);
                while (!parser.EndOfData)
                {
                    List<string> row = new List<string>();
                    //Processing row
                    string[] fields = parser.ReadFields();
                    foreach (string field in fields)
                    {
                        row.Add(field);
                    }
                    table.Add(row);
                }
            }

            int rowStartIndex = 0;
            int columnStartIndex = 0;

            //Setting the start index for columns and rows 
            if (settings.HasRowHeaders)
                columnStartIndex = columnStartIndex + 1;
            if (settings.HasRowUnits)
                columnStartIndex = columnStartIndex + 1;
            if (settings.HasColumnHeaders)
                rowStartIndex = rowStartIndex + 1;
            if (settings.HasColumnUnits)
                rowStartIndex = rowStartIndex + 1;

            List<SimMultiValueBigTableHeader> columnHeaders = new List<SimMultiValueBigTableHeader>();
            List<SimMultiValueBigTableHeader> rowHeaders = new List<SimMultiValueBigTableHeader>();

            //when the .csv contains nothing
            if (table.Count == 0)
            {
                throw new CSVImportException(CSVImportExceptionReason.EmptyFile, new object[] { });
            }
            //when there is not enough rows in the .csv
            if (rowStartIndex >= table.Count)
            {
                throw new CSVImportException(CSVImportExceptionReason.NotEnoughRows, new object[] {
                    table.Count, table.Count > 0 ? table[0].Count : 0
                });
            }
            //when there is not enough columns in the .csv
            if (columnStartIndex >= table.FirstOrDefault().Count)
            {
                throw new CSVImportException(CSVImportExceptionReason.NotEnoughColumns, new object[] {
                    table.Count, table.Count > 0 ? table[0].Count : 0
                });
            }
            //When a row does not contain enough items 
            if (table.Any(n => n.Count != table.FirstOrDefault().Count))
            {
                int errorRowIndex = table.FindIndex(t => t.Count != table.FirstOrDefault().Count);
                int errorColumnIndex = table[errorRowIndex].Count;

                throw new CSVImportException(CSVImportExceptionReason.ColumnCountMismatch, new object[]
                {
                    errorRowIndex, errorColumnIndex, table.First().Count
                });
            }

            if (settings.HasRowUnits && settings.HasColumnUnits)
            {
                //IsColumnHeaders 
                if (settings.HasColumnHeaders)
                {
                    for (int i = columnStartIndex; i < table.FirstOrDefault().Count; ++i)
                        columnHeaders.Add(new SimMultiValueBigTableHeader(table.FirstOrDefault()[i], table[rowStartIndex - 1][i]));
                }
                else
                {
                    for (int i = columnStartIndex; i < table.FirstOrDefault().Count; ++i)
                        columnHeaders.Add(new SimMultiValueBigTableHeader(string.Format(columnHeaderFormat, i), table[rowStartIndex - 1][i]));
                }
                //Row Headers
                if (settings.HasRowHeaders)
                {
                    for (int i = rowStartIndex; i < table.Count; i++)
                        rowHeaders.Add(new SimMultiValueBigTableHeader(table[i].FirstOrDefault(), table[i][columnStartIndex - 1]));
                }
                else
                {
                    for (int i = rowStartIndex; i < table.Count; i++)
                        rowHeaders.Add(new SimMultiValueBigTableHeader(string.Format(rowHeaderFormat, i), table[i][columnStartIndex - 1]));
                }
            }

            if (settings.HasRowUnits && !settings.HasColumnUnits)
            {
                //IsColumnHeaders 
                if (settings.HasColumnHeaders)
                {
                    for (int i = columnStartIndex; i < table.FirstOrDefault().Count; ++i)
                        columnHeaders.Add(new SimMultiValueBigTableHeader(table.FirstOrDefault()[i], columnHeaderUnit));
                }
                else
                {
                    for (int i = columnStartIndex; i < table.FirstOrDefault().Count; ++i)
                        columnHeaders.Add(new SimMultiValueBigTableHeader(string.Format(columnHeaderFormat, i), columnHeaderUnit));
                }
                //Row Headers
                if (settings.HasRowHeaders)
                {
                    for (int i = rowStartIndex; i < table.Count; i++)
                        rowHeaders.Add(new SimMultiValueBigTableHeader(table[i].FirstOrDefault(), table[i][columnStartIndex - 1]));
                }
                else
                {
                    for (int i = rowStartIndex; i < table.Count; i++)
                        rowHeaders.Add(new SimMultiValueBigTableHeader(string.Format(rowHeaderFormat, i), table[i][columnStartIndex - 1]));
                }
            }

            if (settings.HasColumnUnits && !settings.HasRowUnits)
            {
                //IsColumnHeaders 
                if (settings.HasColumnHeaders)
                {
                    for (int i = columnStartIndex; i < table.FirstOrDefault().Count; ++i)
                        columnHeaders.Add(new SimMultiValueBigTableHeader(table.FirstOrDefault()[i], table[rowStartIndex - 1][i]));
                }
                else
                {
                    for (int i = columnStartIndex; i < table.FirstOrDefault().Count; ++i)
                        columnHeaders.Add(new SimMultiValueBigTableHeader(string.Format(columnHeaderFormat, i), table[rowStartIndex - 1][i]));
                }
                //Row Headers
                if (settings.HasRowHeaders)
                {
                    for (int i = rowStartIndex; i < table.Count; i++)
                        rowHeaders.Add(new SimMultiValueBigTableHeader(table[i].FirstOrDefault(), rowHeaderUnit));
                }
                else
                {
                    for (int i = rowStartIndex; i < table.Count; i++)
                        rowHeaders.Add(new SimMultiValueBigTableHeader(string.Format(rowHeaderFormat, i), rowHeaderUnit));
                }
            }

            if (!settings.HasRowUnits && !settings.HasColumnUnits)
            {
                //IsColumnHeaders 
                if (settings.HasColumnHeaders)
                {
                    for (int i = columnStartIndex; i < table.FirstOrDefault().Count; ++i)
                        columnHeaders.Add(new SimMultiValueBigTableHeader(table.FirstOrDefault()[i], columnHeaderUnit));
                }
                else
                {
                    for (int i = columnStartIndex; i < table.FirstOrDefault().Count; ++i)
                        columnHeaders.Add(new SimMultiValueBigTableHeader(string.Format(columnHeaderFormat, i), columnHeaderUnit));
                }
                //Row Headers
                if (settings.HasRowHeaders)
                {
                    for (int i = rowStartIndex; i < table.Count; i++)
                        rowHeaders.Add(new SimMultiValueBigTableHeader(table[i].FirstOrDefault(), rowHeaderUnit));
                }
                else
                {
                    for (int i = rowStartIndex; i < table.Count; i++)
                        rowHeaders.Add(new SimMultiValueBigTableHeader(string.Format(rowHeaderFormat, i), rowHeaderUnit));
                }
            }

            int rowCount = table.Count - rowStartIndex;
            int maxChunkSize = 5000; // in rows
            int maxNumberOfChunks = int.MaxValue;
            int numberOfChunks = Math.Min(Math.Min(maxNumberOfChunks - 1, (int)Math.Ceiling(rowCount / (float)maxChunkSize)), rowCount - 1);
            List<List<object>> values = new List<List<object>>(rowCount);
            NumberFormatInfo formatProvider = new NumberFormatInfo();
            formatProvider.NumberDecimalSeparator = settings.DecimalSeparator;

            if (numberOfChunks <= 1)
            {
                for (int i = 0; i < rowCount; i++)
                {
                    var vals = ParseRow(table[i + rowStartIndex], columnStartIndex, formatProvider);
                    values.Add(vals);
                }
            }
            else
            {
                List<Task<(List<List<object>> values, int errColIndex, int errRowIndex)>> tasks =
                    new List<Task<(List<List<object>>, int, int)>>(numberOfChunks);
                double rowsPerChunk = rowCount / (double)numberOfChunks;
                for (int i = 0; i < numberOfChunks; ++i)
                {
                    int localI = i;
                    tasks.Add(Task.Run(() => ParseRowsAsync(localI, rowsPerChunk, table, rowCount, rowStartIndex, columnStartIndex, formatProvider)));
                }

                Task.WhenAll(tasks);

                foreach (var task in tasks)
                {
                    values.AddRange(task.Result.values);
                }
            }

            if (values.Any(n => n.Count == 0))
            {
                throw new CSVImportException(CSVImportExceptionReason.NotEnoughColumns, new object[] {
                    table.Count, table.Count > 0 ? table[0].Count : 0
                });
            }

            var result = new MultiValueBigTableComponents(columnHeaders, rowHeaders, values);
            return result;
        }

        private static List<object> ParseRow(List<string> cells, int columnStartIndex, IFormatProvider formatProvider)
        {
            int columnCount = cells.Count - columnStartIndex;
            List<object> row = new List<object>(columnCount);
            for (int j = 0; j < columnCount; j++)
            {
                var val = ParseCell(cells[j + columnStartIndex], formatProvider);
                row.Add(val);
            }
            return row;
        }

        private static object ParseCell(string cell, IFormatProvider formatProvider)
        {
            if (string.IsNullOrEmpty(cell))
                return null;
            else if (double.TryParse(cell, NumberStyles.Float, formatProvider, out var dval))
                return dval;
            else if (int.TryParse(cell, NumberStyles.Integer, formatProvider, out var ival))
                return ival;
            else if (bool.TryParse(cell, out var bval))
                return bval;
            else
                return cell;
        }

        private static (List<List<object>>, int errColIndex, int errRowIndex) ParseRowsAsync(int chunk, double chunkSizeInRows, List<List<string>> table, int rowCount, int rowStartIndex, int columnStartIndex, IFormatProvider formatProvider)
        {
            int startRow = (int)Math.Floor(chunk * chunkSizeInRows) + rowStartIndex;
            int endRow = Math.Min((int)Math.Floor((chunk + 1) * chunkSizeInRows), rowCount) + rowStartIndex;

            List<List<object>> result = new List<List<object>>(endRow - startRow);

            int firstErrColIndex = -1;
            int firstErrRowIndex = -1;
            for (int i = startRow; i < endRow; ++i)
            {
                var vals = ParseRow(table[i], columnStartIndex, formatProvider);
                result.Add(vals);
            }

            return (result, firstErrColIndex, firstErrRowIndex);
        }
    }
}
