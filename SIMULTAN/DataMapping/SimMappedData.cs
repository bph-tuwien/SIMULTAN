using SIMULTAN.Data.MultiValues;
using SIMULTAN.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.DataMapping
{
    /// <summary>
    /// The type of warning that the DataMapping produced
    /// </summary>
    public enum SimDataMappingWarningType
    {
        /// <summary>
        /// A cell that has previously been written is overwritten by another rule
        /// </summary>
        DataOverriden
    }

    /// <summary>
    /// Stores information about a warning produced by the DataMapping
    /// </summary>
    public class SimDataMappingWarning
    {
        /// <summary>
        /// The type of the warning
        /// </summary>
        public SimDataMappingWarningType Type { get; }
        /// <summary>
        /// The rule which produced the warning
        /// </summary>
        public ISimDataMappingRuleBase Rule { get; }
        /// <summary>
        /// The name of the worksheet in which the warning was produced (only if applicable)
        /// </summary>
        public string SheetName { get; }
        /// <summary>
        /// The index of the cell in which the warning was produced (only if applicable)
        /// </summary>
        public RowColumnIndex Index { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SimDataMappingWarning"/> class
        /// </summary>
        /// <param name="type">The type of the warning</param>
        /// <param name="rule">The rule which produced the warning</param>
        /// <param name="sheetName">The name of the worksheet in which the warning was produced (only if applicable)</param>
        /// <param name="index">The index of the cell in which the warning was produced (only if applicable)</param>
        public SimDataMappingWarning(SimDataMappingWarningType type, ISimDataMappingRuleBase rule, string sheetName, RowColumnIndex index)
        {
            this.Type = type;
            this.Rule = rule;
            this.SheetName = sheetName;
            this.Index = index;
        }
    }

    /// <summary>
    /// Stores mapped results from a <see cref="SimDataMappingTool"/>.
    /// </summary>
    public class SimMappedData
    {
        /// <summary>
        /// Stores all warnings that were produced during the mapping
        /// </summary>
        public IEnumerable<SimDataMappingWarning> Warnings => warnings;
        private List<SimDataMappingWarning> warnings { get; } = new List<SimDataMappingWarning>();

        /// <summary>
        /// The mapped data
        /// The key stores the name of the worksheet into which the data has been mapped.
        /// The value stores the data of one worksheet, with the row/column as index.
        /// </summary>
        public Dictionary<string, Dictionary<RowColumnIndex, object>> Data { get; } = new Dictionary<string, Dictionary<RowColumnIndex, object>>();

        /// <summary>
        /// Adds an entry to the data
        /// </summary>
        /// <param name="sheet">Name of the worksheet</param>
        /// <param name="position">Index inside the worksheet</param>
        /// <param name="data">The data to write into the worksheet</param>
        /// <param name="source">The rule which adds the data</param>
        public void AddData(string sheet, RowColumnIndex position, object data, ISimDataMappingRuleBase source)
        {
            if (!Data.TryGetValue(sheet, out var sheetData))
            {
                sheetData = new Dictionary<RowColumnIndex, object>();
                Data.Add(sheet, sheetData);
            }

            if (sheetData.ContainsKey(position))
            {
                if (warnings.Count < 100) //Just to make sure that there aren't infinite many errors
                    warnings.Add(new SimDataMappingWarning(SimDataMappingWarningType.DataOverriden, source, sheet, position));
            }
            sheetData[position] = data;
        }
        /// <summary>
        /// Converts the data of one worksheet into a table
        /// </summary>
        /// <param name="sheetName">Name of the worksheet</param>
        /// <param name="existingTable">When set to Null, a new table is created. When a valid instance is supplied, the data in the table is updated</param>
        /// <returns>Depending on the existingTable parameter, either a new table or the existing table</returns>
        public SimMultiValueBigTable ConvertToTable(string sheetName, SimMultiValueBigTable existingTable = null)
        {
            if (this.Data.TryGetValue(sheetName, out var sheetData))
            {
                //Find size of table
                int maxRow = 0, maxColumn = 0;
                foreach (var entry in sheetData.Keys)
                {
                    maxColumn = Math.Max(maxColumn, entry.Column);
                    maxRow = Math.Max(maxRow, entry.Row);
                }

                List<List<object>> tableData = new List<List<object>>(maxRow + 1);
                List<SimMultiValueBigTableHeader> rowHeaders = new List<SimMultiValueBigTableHeader>(maxRow + 1);
                for (int i = 0; i < maxRow + 1; i++)
                {
                    List<object> row = new List<object>(maxColumn + 1);
                    for (int j = 0; j < maxColumn + 1; j++)
                        row.Add(null);
                    tableData.Add(row);

                    rowHeaders.Add(new SimMultiValueBigTableHeader(i.ToString(), string.Empty));
                }

                List<SimMultiValueBigTableHeader> columnHeaders = new List<SimMultiValueBigTableHeader>(maxColumn + 1);
                for (int j = 0; j < maxColumn + 1; j++)
                {
                    columnHeaders.Add(new SimMultiValueBigTableHeader(ToExcelColumn(j), string.Empty));
                }

                //Move data to table data
                foreach (var entry in sheetData)
                {
                    tableData[entry.Key.Row][entry.Key.Column] = entry.Value;
                }

                if (existingTable == null)
                {
                    existingTable = new SimMultiValueBigTable("", string.Empty, string.Empty,
                        columnHeaders, rowHeaders, tableData, false);
                }
                else
                {
                    existingTable.ReplaceData(columnHeaders, rowHeaders, tableData, false);
                }

                return existingTable;
            }
            else
                throw new ArgumentException("Data does not contain any data with this key");
        }

        /// <summary>
        /// Converts an index to an Excel column name.
        /// </summary>
        /// <param name="index">The column index</param>
        /// <returns>The name of the column</returns>
        public static string ToExcelColumn(int index)
        {
            const byte BASE = 'Z' - 'A' + 1;
            string name = string.Empty;

            do
            {
                name = Convert.ToChar('A' + index % BASE) + name;
                index = index / BASE - 1;
            }
            while (index >= 0);

            return name;
        }
    }
}
