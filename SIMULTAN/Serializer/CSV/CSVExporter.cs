using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Serializer.CSV
{
    /// <summary>
    /// Utility class for exporting data into the CSV format
    /// </summary>
    public class CSVExporter
    {
        /// <summary>
        /// The delimiter used to separate columns
        /// </summary>
        public string CSV_DELIMITER { get; set; } = ",";

        /// <summary>
        /// The delimiter used to separate rows
        /// </summary>
        public static readonly string CSV_LINEDELIMITER = Environment.NewLine;

        private const string CSV_DQ = "\"";
        private const string CSV_DQDQ = "\"\"";

        private int nrColumns;
        private StringBuilder recordBuilder;

        /// <summary>
        /// Initializes a new instance of the CSVExporter class
        /// </summary>
        /// <param name="nrColumns">The number of columns in the dataset</param>
        /// <param name="csvDelimiter">The delimiter used to separated columns</param>
        public CSVExporter(int nrColumns, string csvDelimiter = ",")
        {
            this.CSV_DELIMITER = csvDelimiter;
            this.nrColumns = nrColumns;
            this.recordBuilder = new StringBuilder();
        }



        #region RECORDS

        /// <summary>
        /// Adds a new row to the exporter
        /// </summary>
        /// Warning: This method does NOT write the data to a file. 
        /// Call <see cref="WriteFile"/> to start the writing operation.
        /// <param name="record">The column entries for this row</param>
        public void AddRecord(List<string> record)
        {
            if (record == null || record.Count != this.nrColumns)
                return;

            bool escaping_necessary = false;
            foreach (string h in record)
            {
                if (string.IsNullOrEmpty(h))
                    continue;

                if (h.Contains(CSVExporter.CSV_DQ) || h.Contains(this.CSV_DELIMITER) || h.Contains(CSVExporter.CSV_LINEDELIMITER))
                {
                    escaping_necessary = true;
                    break;
                }
            }

            StringBuilder escaped_record = new StringBuilder();
            for (int i = 0; i < record.Count; i++)
            {
                string h = record[i];
                if (escaping_necessary)
                {
                    if (!(string.IsNullOrEmpty(h)))
                        escaped_record.Append(CSVExporter.CSV_DQ + h + CSVExporter.CSV_DQ);
                    else
                        escaped_record.Append(CSVExporter.CSV_DQDQ);
                    if (i < record.Count - 1)
                        escaped_record.Append(this.CSV_DELIMITER);
                }
                else
                {
                    if (!(string.IsNullOrEmpty(h)))
                        escaped_record.Append(h);
                    if (i < record.Count - 1)
                        escaped_record.Append(this.CSV_DELIMITER);
                }
            }
            escaped_record.Append(CSVExporter.CSV_LINEDELIMITER);

            // save internally
            this.recordBuilder.Append(escaped_record.ToString());
        }
        /// <summary>
        /// Adds a number of rows to the exporter
        /// </summary>
        /// Warning: This method does NOT write the data to a file. 
        /// Call <see cref="WriteFile"/> to start the writing operation.
        /// <param name="records">The rows</param>
        public void AddMultipleRecords(List<List<string>> records)
        {
            if (records == null || records.Count == 0) return;

            foreach (List<string> r in records)
            {
                this.AddRecord(r);
            }
        }

        #endregion

        #region WRITE FILE

        /// <summary>
        /// Writes the stored record to a file
        /// </summary>
        /// <param name="filePath">Path to the file into which the data should be written</param>
        /// <returns></returns>
        public void WriteFile(string filePath)
        {
            string content = this.recordBuilder.ToString();
            using (FileStream fs = File.Create(filePath))
            {
                byte[] content_B = System.Text.Encoding.UTF8.GetBytes(content);
                fs.Write(content_B, 0, content_B.Length);
            }
        }

        #endregion

    }
}
