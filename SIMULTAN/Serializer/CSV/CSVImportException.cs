using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Serializer.CSV
{
    /// <summary>
    /// Reasons why the CSV import has failed
    /// </summary>
    public enum CSVImportExceptionReason
    {
        /// <summary>
        /// The file does not contain any row.
        /// </summary>
        EmptyFile,
        /// <summary>
        /// The row count doesn't match the settings.
        /// </summary>
        /// Data: {0} rowCount, {1} columnCount
        NotEnoughRows,
        /// <summary>
        /// The column count doesn't match the settings.
        /// </summary>
        /// Data: {0} rowCount, {1} columnCount
        NotEnoughColumns,
        /// <summary>
        /// The columns in different rows do not match
        /// </summary>
        /// Data: {0} the row, {1} column count in row, {2} expected column count
        ColumnCountMismatch,
        /// <summary>
        /// Happens when an entry cannot be parsed into a double
        /// </summary>
        /// Data: {0} the string, {1} the row, {2} the column, 
        ParseError
    }

    /// <summary>
    /// Exception that gets thrown when the CSV importer fails
    /// </summary>
    public class CSVImportException : Exception
    {
        /// <summary>
        /// The reason why the CSV import failed.
        /// </summary>
        public CSVImportExceptionReason Reason { get; }
        /// <summary>
        /// Additional data for the reason.
        /// See <see cref="CSVImportExceptionReason"/> for details about what data is expected for which reason.
        /// </summary>
        public object[] ReasonData { get; }

        /// <summary>
        /// Initializes a new instance of the CSVImportException class
        /// </summary>
        /// <param name="reason">The reason why the import failed</param>
        /// <param name="data">Additional data for the reason</param>
        public CSVImportException(CSVImportExceptionReason reason, object[] data)
        {
            this.Reason = reason;
            this.ReasonData = data;
        }
    }
}
