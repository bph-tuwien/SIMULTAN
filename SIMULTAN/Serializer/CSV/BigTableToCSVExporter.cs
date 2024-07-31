using SIMULTAN.Data.MultiValues;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace SIMULTAN.Serializer.CSV
{

    /// <summary>S
    /// This class contains the exporter for BigTables to CSV file format. 
    /// </summary>
    public static class BigTableToCSVExporter
    {
        /// <summary>
        /// Exports a MultiValueBigTable to a CSV file.
        /// </summary>
        public static void ExportToCSV(SimMultiValueBigTable table, string filePathAndName, string delimiter, string decimalSeparator,
            string rowHeaderHeader,
            bool columnHeaders,
            bool rowHeaders,
            bool columnUnits,
            bool rowUnits)
        {

            //CultureInfo selectedCulture = CultureInfo.InvariantCulture;
            // Gets a NumberFormatInfo associated with the en-US culture.
            NumberFormatInfo cNf = new NumberFormatInfo();
            if (decimalSeparator == null || decimalSeparator == "" || delimiter == null || delimiter == "")
                return;

            cNf.NumberDecimalSeparator = decimalSeparator;

            if (table == null)
                return;

            int exporterColCount = table.ColumnHeaders.Count;
            int exporterRowCount = table.RowHeaders.Count;


            List<string> col_headers = new List<string>();
            List<string> col_units = new List<string>();
            List<string> row_headers = new List<string>();
            List<string> row_units = new List<string>();



            if (rowHeaders)
            {
                exporterColCount += 1;

                if (rowHeaderHeader != null)
                {
                    col_headers.Add(rowHeaderHeader);
                }
                else
                {
                    col_headers.Add("");
                }
                col_units.Add("");
            }
            if (rowUnits)
            {
                exporterColCount += 1;
                col_headers.Add("");
                col_units.Add("");
            }
            if (columnHeaders)
            {
                exporterRowCount += +1;
                foreach (var item in table.ColumnHeaders)
                {
                    col_headers.Add(item.Name);
                }
            }

            if (columnUnits)
            {
                exporterRowCount += +1;
                foreach (var item in table.ColumnHeaders)
                {
                    col_units.Add(item.Unit);
                }
            }
            CSVExporter exporter = new CSVExporter(exporterColCount, delimiter);

            List<List<string>> dataRows = new List<List<string>>();
            //Scraping the data together without the units or headers
            for (int j = 0; j < table.Count(0); j++)
            {
                List<string> data_row_list_record = new List<string>();
                if (rowHeaders)
                {
                    data_row_list_record.Add(table.RowHeaders[j].Name);
                }
                if (rowUnits)
                {
                    data_row_list_record.Add(table.RowHeaders[j].Unit);
                }
                //adding each value to a row 
                for (int i = 0; i < table.ColumnHeaders.Count; i++)
                {
                    var cel = table[j, i];

                    if (cel is double d)
                        data_row_list_record.Add(d.ToString(cNf));
                    else if (cel is int n)
                        data_row_list_record.Add(n.ToString(cNf));
                    else if (cel is bool b)
                        data_row_list_record.Add(b.ToString());
                    else if (cel is string s)
                        data_row_list_record.Add(s);
                    else
                        data_row_list_record.Add(string.Empty);
                }
                //data_row_list.Add(data_col_list);

                dataRows.Add(data_row_list_record);
            }

            exporter.AddRecord(col_headers);
            exporter.AddRecord(col_units);
            exporter.AddMultipleRecords(dataRows);

            // save to file
            exporter.WriteFile(filePathAndName);
        }
    }
}
