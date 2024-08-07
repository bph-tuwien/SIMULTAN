﻿using SIMULTAN.Data.Components;
using SIMULTAN.Data.MultiValues;
using SIMULTAN.Exceptions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;

namespace SIMULTAN.Excel
{
    //Needs to be completely refactored.

    public class ExcelStandardImporter
    {
        #region STATC

        public const int MAX_NR_TABLE_ENTRIES = 100000; //10000; //8763;
        public const int COL_OFFSET = 5;
        public const int MAX_NR_VALUE_COLUMNS = 100000; //1000; //100;
        public const int ROW_OFFSET = 3;

        public const int COMPONENT_NR_VALUE_COLUMNS = 11;
        public const int COMPONENT_COL_OFFSET = 2;

        public const string TABLE_NAME = "Tabelle1";

        private static readonly IFormatProvider FORMAT_NEUTRAL = new NumberFormatInfo();
        private static readonly IFormatProvider FORMAT_DE = CultureInfo.GetCultureInfo("de-DE");

        #endregion

        #region IMPORT: Big Table

        public void ImportBigTableFromFile(FileInfo file, SimMultiValueCollection factory,
            string unitX, string unitY, string rowNameFormat, string rowUnit,
            int maxRowCount = 0)
        {
            if (file == null)
                throw new ArgumentNullException(nameof(file));
            if (!file.Exists)
                throw new FileNotFoundException();
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            int nr_rows_to_read = (maxRowCount <= 0) ? ExcelStandardImporter.MAX_NR_TABLE_ENTRIES : maxRowCount + ExcelStandardImporter.ROW_OFFSET;

            ExcelStandardImporter.CheckFileLocked(file); //After this, the file can be opened and is valid

            List<List<string>> raw_record = this.ImportFromFile(file, ExcelStandardImporter.TABLE_NAME, nr_rows_to_read);
            List<string> names, units;
            List<List<object>> values;
            ExcelStandardImporter.ParseData(raw_record, ExcelStandardImporter.MAX_NR_TABLE_ENTRIES,
                out names, out units, out values);

            // get the table name
            string[] file_path_comps = file.FullName.Split(new string[] { "\\", "/", "." }, StringSplitOptions.RemoveEmptyEntries);
            int nr_comps = file_path_comps.Length;
            string tableName = "table";
            if (nr_comps > 1)
                tableName = file_path_comps[nr_comps - 2];
            else if (nr_comps > 0)
                tableName = file_path_comps[0];

            List<SimMultiValueBigTableHeader> columnHeaders = new List<SimMultiValueBigTableHeader>(names.Count);
            for (int i = 0; i < names.Count; ++i)
            {
                columnHeaders.Add(new SimMultiValueBigTableHeader(names[i], units[i]));
            }
            var rowHeaders = values.Select((x, xi) => new SimMultiValueBigTableHeader(
                string.Format(rowNameFormat, xi), rowUnit)).ToList();

            var table = new SimMultiValueBigTable(tableName, unitX, unitY,
                columnHeaders, rowHeaders, values);
            factory.Add(table);

            raw_record = null;
            values = null;
            //GC.Collect();
        }

        public void ImportBigTableWNamesFromFile(FileInfo file, SimMultiValueCollection factory,
            string unitX, string unitY, string rowUnit,
            int maxRowCount = 0)
        {
            if (file == null)
                throw new ArgumentNullException(nameof(file));
            if (!file.Exists)
                throw new FileNotFoundException();
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            int nr_rows_to_read = (maxRowCount <= 0) ? ExcelStandardImporter.MAX_NR_TABLE_ENTRIES : maxRowCount + ExcelStandardImporter.ROW_OFFSET;

            ExcelStandardImporter.CheckFileLocked(file);

            List<List<string>> raw_record = this.ImportFromFile(file, ExcelStandardImporter.TABLE_NAME, nr_rows_to_read);

            List<string> names, units;
            List<List<object>> values;
            List<string> row_names;
            ExcelStandardImporter.ParseDataNamedRows(raw_record, ExcelStandardImporter.MAX_NR_TABLE_ENTRIES,
                out names, out units, out values, out row_names);

            // get the table name
            string[] file_path_comps = file.FullName.Split(new string[] { "\\", "/", "." }, StringSplitOptions.RemoveEmptyEntries);
            int nr_comps = file_path_comps.Length;
            string table_name = "table";
            if (nr_comps > 1)
                table_name = file_path_comps[nr_comps - 2];
            else if (nr_comps > 0)
                table_name = file_path_comps[0];

            names.RemoveAt(0);
            units.RemoveAt(0);

            List<SimMultiValueBigTableHeader> columnHeaders = new List<SimMultiValueBigTableHeader>(names.Count);
            for (int i = 0; i < names.Count; ++i)
                columnHeaders.Add(new SimMultiValueBigTableHeader(names[i], units[i]));

            List<SimMultiValueBigTableHeader> rowHeaders = row_names.Select(x => new SimMultiValueBigTableHeader(x, rowUnit)).ToList();

            var table = new SimMultiValueBigTable(table_name, unitX, unitY,
                columnHeaders, rowHeaders, values);
            factory.Add(table);

            raw_record = null;
            values = null;
        }

        #endregion

        #region IMPORT: General

        private List<List<string>> ImportFromFile(FileInfo file, string tableName, int maxRowCount)
        {
            if (!OperatingSystem.IsWindows())
                throw new PlatformNotSupportedException("Only windows is supported");

            if (file == null)
                throw new ArgumentNullException(string.Format("{0} may not be null", nameof(file)));
            if (string.IsNullOrEmpty(tableName))
                throw new ArgumentNullException(string.Format("{0} may not be null or empty", nameof(tableName)));

            List<List<string>> fields = new List<List<string>>();

            var sConnection = "Provider=Microsoft.ACE.OLEDB.12.0;Data Source=" +
                               file.FullName +
                               ";Extended Properties=\"Excel 12.0;HDR=No;IMEX=1\"";
            var oleExcelConnection = new OleDbConnection(sConnection);
            oleExcelConnection.Open();

            var dtTablesList = oleExcelConnection.GetSchema("Tables");
            int nrTRows = dtTablesList.Rows.Count;
            if (nrTRows > 0)
            {
                for (int r = 0; r < nrTRows; r++)
                {
                    var sSheetName = dtTablesList.Rows[r]["TABLE_NAME"].ToString();
                    if (!string.IsNullOrEmpty(sSheetName) && string.Equals(sSheetName, tableName + "$"))
                    {
                        int nr_chunks = MAX_NR_VALUE_COLUMNS / 255;
                        bool break_next_time = false;
                        for (int chunk = 0; chunk < nr_chunks; chunk++)
                        {
                            if (break_next_time)
                                break;
                            Process proc = Process.GetCurrentProcess();
                            if (proc.PrivateMemorySize64 / 1000000000.0 > 10.0)
                            {
                                break; // 10 GB to avoid out of memory exception on a 16GB RAM machine
                            }
                            // -------------------------------------------------------------------------- //
                            var oleExcelCommand = oleExcelConnection.CreateCommand();
                            var range = TranslateRange(1, maxRowCount, chunk * 255 + 1, 255);
                            oleExcelCommand.CommandText = "Select * From [" + sSheetName + range.range_start + ":" + range.range_end + "]";
                            oleExcelCommand.CommandType = CommandType.Text;
                            Debug.WriteLine("Execute Reader for chunk {0} at {1}", chunk, DateTime.Now);
                            var oleExcelReader = oleExcelCommand.ExecuteReader();
                            Debug.WriteLine("Execute Reader done for chunk {0} at {1}", chunk, DateTime.Now);

                            int nOutputRow = 0;
                            while (oleExcelReader.Read() && nOutputRow < maxRowCount)
                            {
                                int nrF = oleExcelReader.FieldCount;
                                if (nrF == 0)
                                    break;
                                if (nrF < 255)
                                    break_next_time = true;

                                if (chunk == 0)
                                {
                                    List<string> row = new List<string>();
                                    for (int i = 0; i < nrF; i++)
                                    {
                                        row.Add(oleExcelReader.GetValue(i).ToString());
                                    }
                                    fields.Add(row);
                                }
                                else
                                {
                                    var row = fields[nOutputRow];
                                    for (int i = 0; i < nrF; i++)
                                    {
                                        row.Add(oleExcelReader.GetValue(i).ToString());
                                    }
                                }
                                nOutputRow++;
                            }
                            oleExcelReader.Close();
                            // -------------------------------------------------------------------------- //
                        }

                        break;
                    }
                }
            }
            dtTablesList.Clear();
            dtTablesList.Dispose();

            oleExcelConnection.Close();
            oleExcelConnection.Dispose();

            // remove empty rows
            List<List<string>> fields_final = new List<List<string>>();
            foreach (List<string> row in fields)
            {
                bool empty = row.All(x => string.IsNullOrEmpty(x));
                if (empty)
                    continue;
                else
                    fields_final.Add(row);
            }
            fields = null;
            GC.Collect();

            return fields_final;
        }

        #endregion

        #region INTERPRET DATA

        internal static void ParseData(List<List<string>> _excel_strings, int _nr_data_rows,
                            out List<string> names, out List<string> units, out List<List<object>> values)
        {
            names = new List<string>();
            units = new List<string>();
            values = new List<List<object>>();

            if (_excel_strings == null || _excel_strings.Count < 3 || _nr_data_rows < 1) return;



            for (int i = 0; i < _excel_strings.Count && i < _nr_data_rows; i++)
            {
                List<string> row = _excel_strings[i];
                if (row == null || row.Count > ExcelStandardImporter.MAX_NR_VALUE_COLUMNS + ExcelStandardImporter.COL_OFFSET) continue;
                if (i == 0)
                {
                    names = row.Skip(ExcelStandardImporter.COL_OFFSET).Where(x => !string.IsNullOrEmpty(x)).ToList();
                }
                else if (i == 1)
                {
                    units = row.Skip(ExcelStandardImporter.COL_OFFSET).Where(x => !string.IsNullOrEmpty(x)).ToList();
                }
                else if (i > 2)
                {
                    List<string> row_vals_str = row.Skip(ExcelStandardImporter.COL_OFFSET).Where(x => !string.IsNullOrEmpty(x)).ToList();
                    List<object> row_vals = new List<object>();
                    foreach (string val_candidate in row_vals_str)
                    {
                        bool success = false;

                        if (val_candidate.Contains('.'))
                        {
                            success = double.TryParse(val_candidate,
                                NumberStyles.Float, ExcelStandardImporter.FORMAT_NEUTRAL, out var dnvalue);
                            if (success)
                            {
                                row_vals.Add(dnvalue);
                                continue;
                            }
                        }

                        success = double.TryParse(val_candidate,
                            NumberStyles.Float, ExcelStandardImporter.FORMAT_DE, out var dvalue);
                        if (success)
                        {
                            row_vals.Add(dvalue);
                            continue;
                        }

                        success = int.TryParse(val_candidate, NumberStyles.Integer, ExcelStandardImporter.FORMAT_NEUTRAL, out var ivalue);
                        if (success)
                        {
                            row_vals.Add(ivalue);
                            continue;
                        }

                        success = bool.TryParse(val_candidate, out var bvalue);
                        if (success)
                        {
                            row_vals.Add(bvalue);
                            continue;
                        }

                        if (string.IsNullOrEmpty(val_candidate))
                            row_vals.Add(null);
                        else
                            row_vals.Add(val_candidate); //String
                    }
                    if (row_vals_str.Count > 0 && row_vals.Count == row_vals_str.Count)
                        values.Add(row_vals);
                }
            }
        }


        internal static void ParseDataNamedRows(List<List<string>> _excel_strings, int _nr_data_rows,
                            out List<string> names, out List<string> units, out List<List<object>> values, out List<string> row_names)
        {
            names = new List<string>();
            units = new List<string>();
            values = new List<List<object>>();
            row_names = new List<string>();

            if (_excel_strings == null || _excel_strings.Count < 3 || _nr_data_rows < 1) return;



            for (int i = 0; i < _excel_strings.Count && i < _nr_data_rows; i++)
            {
                List<string> row = _excel_strings[i];
                if (row == null || row.Count > ExcelStandardImporter.MAX_NR_VALUE_COLUMNS + ExcelStandardImporter.COL_OFFSET) continue;
                if (i == 0)
                {
                    names = row.Skip(ExcelStandardImporter.COL_OFFSET).Where(x => !string.IsNullOrEmpty(x)).ToList();
                }
                else if (i == 1)
                {
                    units = row.Skip(ExcelStandardImporter.COL_OFFSET).Where(x => !string.IsNullOrEmpty(x)).ToList();
                }
                else if (i > 2)
                {
                    List<string> row_str = row.Skip(ExcelStandardImporter.COL_OFFSET).Where(x => !string.IsNullOrEmpty(x)).ToList();
                    string row_name = (row_str.Count > 0) ? row_str[0] : "name";
                    List<string> row_vals_str = row_str.Skip(1).ToList();
                    List<object> row_vals = new List<object>();
                    foreach (string val_candidate in row_vals_str)
                    {
                        double value = double.NaN;
                        bool success = false;
                        if (val_candidate.Contains('.'))
                            success = double.TryParse(val_candidate,
                                NumberStyles.Float, ExcelStandardImporter.FORMAT_NEUTRAL, out value);
                        else if (val_candidate.StartsWith("#"))
                            success = true; // added 28.10.2019 for error messages in Excel (e.g. #DIV/0!, #REF!, etc.)
                        else
                            success = double.TryParse(val_candidate,
                                NumberStyles.Float, ExcelStandardImporter.FORMAT_DE, out value);

                        if (success)
                            row_vals.Add(value);
                    }
                    if (row_vals_str.Count > 0 && row_vals.Count == row_vals_str.Count)
                    {
                        values.Add(row_vals);
                        row_names.Add(row_name);
                    }
                }
            }
        }

        #endregion

        #region UTILS

        private static bool IsFileLocked(FileInfo file)
        {
            FileStream stream = null;

            try
            {
                stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.None);
            }
            catch (IOException)
            {
                //the file is unavailable because it is:
                //still being written to
                //or being processed by another thread
                //or does not exist (has already been processed)
                return true;
            }
            finally
            {
                if (stream != null)
                    stream.Close();
            }

            //file is not locked
            return false;
        }

        private static void CheckFileLocked(FileInfo file)
        {
            if (IsFileLocked(file))
                throw new FileInUseException(file);
        }

        internal static (string range_start, string range_end) TranslateRange(int _start_row, int _nr_rows, int _start_col, int _nr_cols)
        {
            string range_start = LettersFromAnyNr(_start_col) + _start_row.ToString();
            string range_end = LettersFromAnyNr(_start_col + _nr_cols - 1) + (_start_row + _nr_rows - 1).ToString();
            return (range_start, range_end);
        }

        internal static string LettersFromAnyNr(int _nr)
        {
            List<int> figures = new List<int>();
            int figure = _nr;
            while (figure > 26)
            {
                int figure_new = figure / 26;
                int rest = figure % 26;
                if (rest == 0)
                    figure_new--;
                figure = figure_new;
                figures.Add(rest);
            }
            figures.Add(figure);
            figures.Reverse();

            if (figures.Count <= 1)
                return LetterFromNr(figures[0]);
            else
                return figures.Select(x => LetterFromNr(x)).Aggregate((x, y) => x + y);
        }



        private static string LetterFromNr(int _nr)
        {
            string letter = "A";
            switch (_nr)
            {
                case 1:
                    letter = "A";
                    break;
                case 2:
                    letter = "B";
                    break;
                case 3:
                    letter = "C";
                    break;
                case 4:
                    letter = "D";
                    break;
                case 5:
                    letter = "E";
                    break;
                case 6:
                    letter = "F";
                    break;
                case 7:
                    letter = "G";
                    break;
                case 8:
                    letter = "H";
                    break;
                case 9:
                    letter = "I";
                    break;
                case 10:
                    letter = "J";
                    break;
                case 11:
                    letter = "K";
                    break;
                case 12:
                    letter = "L";
                    break;
                case 13:
                    letter = "M";
                    break;
                case 14:
                    letter = "N";
                    break;
                case 15:
                    letter = "O";
                    break;
                case 16:
                    letter = "P";
                    break;
                case 17:
                    letter = "Q";
                    break;
                case 18:
                    letter = "R";
                    break;
                case 19:
                    letter = "S";
                    break;
                case 20:
                    letter = "T";
                    break;
                case 21:
                    letter = "U";
                    break;
                case 22:
                    letter = "V";
                    break;
                case 23:
                    letter = "W";
                    break;
                case 24:
                    letter = "X";
                    break;
                case 25:
                    letter = "Y";
                    break;
                case 0:
                case 26:
                    letter = "Z";
                    break;
            }
            return letter;
        }

        #endregion

    }
}
