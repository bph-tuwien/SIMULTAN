using SIMULTAN.Data;
using SIMULTAN.Data.MultiValues;
using SIMULTAN.Projects.Serializers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SIMULTAN.Serializer.DXF.DXFEntities
{
    /// <summary>
    /// wrapper of class MultiValueBigTable
    /// </summary>
    internal class DXFMultiValueBigTable : DXFField
    {
        #region CLASS MEMBER

        //specific info
        protected int dxf_nr_names;
        protected int dxf_nr_names_read;
        protected int dxf_nr_units;
        protected int dxf_nr_units_read;

        public List<string> dxf_Names { get; protected set; }
        public List<string> dxf_Units { get; protected set; }

        // actual values
        protected int dxf_nr_values_per_row;
        protected int dxf_nr_table_rows;
        protected int dxf_nr_table_rows_read;

        protected List<double> dxf_table_row_values;
        protected List<bool> dxf_table_row_values_read;

        public List<List<double>> dxf_Values { get; protected set; }
        private List<string> dxf_not_parsed_Values;
        private int col_index_in_current_row;

        // row names
        protected int dxf_nr_row_names;
        protected int dxf_nr_row_names_read;
        protected int dxf_nr_row_units;
        protected int dxf_nr_row_units_read;

        public List<string> dxf_RowNames { get; protected set; }
        public List<string> dxf_RowUnits { get; protected set; }

        // additional info
        public string dxf_AdditionalInfo { get; protected set; }

        #endregion

        public DXFMultiValueBigTable()
        {
            this.dxf_nr_names = 0;
            this.dxf_nr_names_read = 0;
            this.dxf_nr_units = 0;
            this.dxf_nr_units_read = 0;

            this.dxf_Names = new List<string>();
            this.dxf_Units = new List<string>();

            this.dxf_nr_values_per_row = 0;
            this.dxf_nr_table_rows = 0;
            this.dxf_nr_table_rows_read = 0;
            this.dxf_table_row_values = Enumerable.Repeat(double.NaN, 2).ToList();
            this.dxf_table_row_values_read = Enumerable.Repeat(false, 2).ToList();

            this.dxf_Values = new List<List<double>>();
            this.dxf_not_parsed_Values = new List<string>();
            this.col_index_in_current_row = -1;

            this.dxf_nr_row_names = 0;
            this.dxf_nr_row_names_read = 0;

            this.dxf_nr_row_units = 0;
            this.dxf_nr_row_units_read = 0;

            this.dxf_RowNames = new List<string>();
            this.dxf_RowUnits = new List<string>();

            this.dxf_AdditionalInfo = null;
        }

        #region METHODS

        protected void AddEntryToTableRowAt(int _index)
        {
            // add value to the row
            if (!this.dxf_table_row_values_read[_index])
            {
                this.dxf_table_row_values[_index] = this.Decoder.DoubleValue();
                this.dxf_table_row_values_read[_index] = true;
            }

            // check if the row has been completed
            int nr_values_read = this.dxf_table_row_values_read.Sum(x => x ? 1 : 0);
            if (nr_values_read == this.dxf_nr_values_per_row)
            {
                this.dxf_Values.Add(this.dxf_table_row_values.Take(this.dxf_nr_values_per_row).ToList());
                this.dxf_nr_table_rows_read++;

                this.dxf_table_row_values = Enumerable.Repeat(double.NaN, this.dxf_nr_values_per_row).ToList();
                this.dxf_table_row_values_read = Enumerable.Repeat(false, this.dxf_nr_values_per_row).ToList();
            }
        }

        protected void AddEntireRow(string _row_str)
        {
            if (string.IsNullOrEmpty(_row_str)) return;

            this.dxf_not_parsed_Values.Add(_row_str);
            this.dxf_nr_table_rows_read++;
        }

        #endregion

        #region OVERRIDES: Read Property

        public override void ReadPoperty()
        {
            switch (this.Decoder.FCode)
            {
                case (int)ParamStructCommonSaveCode.NUMBER_OF:
                    this.dxf_nr_values_per_row = this.Decoder.IntValue();
                    this.dxf_table_row_values = Enumerable.Repeat(double.NaN, this.dxf_nr_values_per_row).ToList();
                    this.dxf_table_row_values_read = Enumerable.Repeat(false, this.dxf_nr_values_per_row).ToList();
                    break;
                case (int)MultiValueSaveCode.XS:
                    // marks the start of the sequence of names (column headers)
                    this.dxf_nr_names = this.Decoder.IntValue();
                    break;
                case (int)MultiValueSaveCode.YS:
                    // marks the start of the sequence of units (also column headers)
                    this.dxf_nr_units = this.Decoder.IntValue();
                    break;
                case (int)MultiValueSaveCode.FIELD:
                    // marks the start of the sequence of values in the BIG TABLE
                    this.dxf_nr_table_rows = this.Decoder.IntValue();
                    break;
                case (int)MultiValueSaveCode.ROW_NAMES:
                    // marks the start of the sequence of row names in the BIG TABLE
                    this.dxf_nr_row_names = this.Decoder.IntValue();
                    if (this.dxf_nr_row_names == 0)
                        this.dxf_RowNames = null;
                    break;
                case (int)MultiValueSaveCode.ROW_UNITS:
                    this.dxf_nr_row_units = this.Decoder.IntValue();
                    if (this.dxf_nr_row_units == 0)
                        this.dxf_RowUnits = null;
                    break;
                case (int)MultiValueSaveCode.ADDITIONAL_INFO:
                    this.dxf_AdditionalInfo = this.Decoder.FValue;
                    break;
                case (int)MultiValueSaveCode.MVBT_COMPLETE_VALUE_ROW:
                    this.AddEntireRow(this.Decoder.FValue);
                    break;
                case (int)ParamStructCommonSaveCode.X_VALUE:
                    if (this.dxf_nr_names > this.dxf_nr_names_read)
                    {
                        this.dxf_Names.Add(this.Decoder.FValue);
                        this.dxf_nr_names_read++;
                    }
                    if (this.dxf_nr_units > this.dxf_nr_units_read)
                    {
                        this.dxf_Units.Add(this.Decoder.FValue);
                        this.dxf_nr_units_read++;
                    }
                    else if (this.dxf_nr_table_rows > this.dxf_nr_table_rows_read)
                    {
                        this.col_index_in_current_row = 0;
                        this.AddEntryToTableRowAt(0);
                    }
                    break;
                case (int)ParamStructCommonSaveCode.Y_VALUE:
                    if (this.dxf_nr_table_rows > this.dxf_nr_table_rows_read)
                        this.AddEntryToTableRowAt(1);
                    break;
                case (int)ParamStructCommonSaveCode.Z_VALUE:
                    if (this.dxf_nr_table_rows > this.dxf_nr_table_rows_read)
                        this.AddEntryToTableRowAt(2);
                    break;
                case (int)ParamStructCommonSaveCode.W_VALUE:
                    if (this.dxf_nr_table_rows > this.dxf_nr_table_rows_read)
                        this.AddEntryToTableRowAt(3);
                    break;
                case (int)ParamStructCommonSaveCode.V5_VALUE:
                    if (this.dxf_nr_table_rows > this.dxf_nr_table_rows_read)
                        this.AddEntryToTableRowAt(4);
                    break;
                case (int)ParamStructCommonSaveCode.V6_VALUE:
                    if (this.dxf_nr_table_rows > this.dxf_nr_table_rows_read)
                        this.AddEntryToTableRowAt(5);
                    break;
                case (int)ParamStructCommonSaveCode.V7_VALUE:
                    if (this.dxf_nr_table_rows > this.dxf_nr_table_rows_read)
                        this.AddEntryToTableRowAt(6);
                    break;
                case (int)ParamStructCommonSaveCode.V8_VALUE:
                    if (this.dxf_nr_table_rows > this.dxf_nr_table_rows_read)
                        this.AddEntryToTableRowAt(7);
                    break;
                case (int)ParamStructCommonSaveCode.V9_VALUE:
                    if (this.dxf_nr_table_rows > this.dxf_nr_table_rows_read)
                        this.AddEntryToTableRowAt(8);
                    break;
                case (int)ParamStructCommonSaveCode.V10_VALUE:
                    if (this.dxf_nr_table_rows > this.dxf_nr_table_rows_read)
                    {
                        if (this.col_index_in_current_row < 9)
                            this.col_index_in_current_row = 9;
                        else
                            this.col_index_in_current_row++;
                        this.AddEntryToTableRowAt(this.col_index_in_current_row);
                    }
                    break;
                case (int)ParamStructCommonSaveCode.STRING_VALUE:
                    if (this.dxf_nr_row_names > this.dxf_nr_row_names_read) //Names
                    {
                        this.dxf_RowNames.Add(this.Decoder.FValue);
                        this.dxf_nr_row_names_read++;
                    }
                    if (this.dxf_nr_row_units > this.dxf_nr_row_units_read) // Units
                    {
                        this.dxf_RowUnits.Add(this.Decoder.FValue);
                        this.dxf_nr_row_units_read++;
                    }
                    break;
                default:
                    // DXFField: ENTITY_NAME, ID, MVType, Name, CanInterpolate,
                    // MVDisplayVector_NUMDIM, MVDisplayVector_CELL_INDEX_X, MVDisplayVector_CELL_INDEX_Y, MVDisplayVector_CELL_INDEX_Z, MVDisplayVector_CELL_INDEX_W,
                    // MVDisplayVector_POS_IN_CELL_REL_X, MVDisplayVector_POS_IN_CELL_REL_Y, MVDisplayVector_POS_IN_CELL_REL_Z,
                    // MVDisplayVector_POS_IN_CELL_ABS_X, MVDisplayVector_POS_IN_CELL_ABS_Y, MVDisplayVector_POS_IN_CELL_ABS_Z,
                    // MVDisplayVector_VALUE,
                    // MVUnitX, MVUnitY, MVUnitZ
                    //
                    // DXFEntity: CLASS_NAME, ENT_ID, ENT_KEY
                    base.ReadPoperty();
                    break;
            }
        }

        #endregion

        #region OVERRIDES: Post-Processing

        internal override void OnLoaded()
        {
            base.OnLoaded();

            // check value lists for consistency
            bool data_consistent = true;
            data_consistent &= (this.dxf_nr_names == this.dxf_nr_names_read);
            data_consistent &= (this.dxf_nr_names == this.dxf_Names.Count);
            data_consistent &= (this.dxf_nr_units == this.dxf_nr_units_read);
            data_consistent &= (this.dxf_nr_units == this.dxf_Units.Count);
            data_consistent &= (this.dxf_nr_table_rows == this.dxf_nr_table_rows_read);
            data_consistent &= (this.dxf_nr_table_rows == this.dxf_not_parsed_Values.Count);
            data_consistent &= (this.dxf_nr_row_names == dxf_nr_row_units || dxf_nr_row_units == 0);

            if (!data_consistent)
            {
                this.dxf_not_parsed_Values = null;
                return;
            }

            ParallelBigTableDeserializer pds = new ParallelBigTableDeserializer();
            this.dxf_Values = pds.Parse(this.dxf_not_parsed_Values, ParamStructTypes.DELIMITER_WITHIN_ENTRY, this.dxf_nr_table_rows, this.dxf_nr_values_per_row);
            data_consistent &= (this.dxf_nr_table_rows == this.dxf_Values.Count);
            data_consistent &= (this.dxf_nr_values_per_row == this.dxf_Values[0].Count);
            this.dxf_not_parsed_Values = null;
            GC.Collect();

            if (!data_consistent) return;

            //Column Headers
            int nrColumnHeaders = Math.Min(dxf_Names.Count, dxf_Units.Count);
            List<SimMultiValueBigTableHeader> columnHeader = new List<SimMultiValueBigTableHeader>(nrColumnHeaders);
            for (int i = 0; i < nrColumnHeaders; ++i)
                columnHeader.Add(new SimMultiValueBigTableHeader(dxf_Names[i], dxf_Units[i]));

            if (dxf_Values != null && dxf_Values.Count > 0) //Fixes that old tables store column headers for the row-headers column
                if (dxf_Values[0].Count == columnHeader.Count - 1)
                    columnHeader.RemoveAt(0);

            //Row Headers
            List<SimMultiValueBigTableHeader> rowHeaders = null;
            if (dxf_RowNames != null && dxf_RowUnits != null)
            {
                int nrRowHeaders = Math.Min(dxf_RowNames.Count, dxf_RowUnits.Count);
                if (dxf_RowUnits.Count == 0 && dxf_RowNames.Count > 0)
                    nrRowHeaders = dxf_RowNames.Count;
                rowHeaders = new List<SimMultiValueBigTableHeader>(nrRowHeaders);
                for (int i = 0; i < nrRowHeaders; ++i)
                {
                    if (dxf_RowUnits.Count == 0)
                        rowHeaders.Add(new SimMultiValueBigTableHeader(dxf_RowNames[i], "-"));
                    else
                        rowHeaders.Add(new SimMultiValueBigTableHeader(dxf_RowNames[i], dxf_RowUnits[i]));
                }
            }
            else
            {
                if (dxf_Values != null)
                {
                    rowHeaders = new List<SimMultiValueBigTableHeader>(dxf_Values.Count);
                    for (int i = 0; i < dxf_Values.Count; ++i)
                        rowHeaders.Add(new SimMultiValueBigTableHeader("-", "-"));
                }
            }

            if (Decoder.CurrentFileVersion < 6) //Id translation
            {
                if (DXFDecoder.MultiValueCount > 1000000)
                    throw new Exception("Too many ValueFields");

                var newId = DXFDecoder.MultiValueIdOffset + DXFDecoder.MultiValueCount;
                DXFDecoder.MultiValueCount++;

                if (!DXFDecoder.IdTranslation.ContainsKey((typeof(SimMultiValue), this.ENT_ID)))
                    DXFDecoder.IdTranslation.Add((typeof(SimMultiValue), this.ENT_ID), newId);
                else
                {
                    Decoder.Log(string.Format("Multiple ValueFields with Id {0} found. Name=\"{1}\" Original Name=\"{2}\"",
                        this.ENT_ID, this.dxf_MVName,
                        this.Decoder.ProjectData.IdGenerator.GetById<SimMultiValue>(
                            new SimId(this.Decoder.ProjectData.Owner, DXFDecoder.IdTranslation[(typeof(SimMultiValue), this.ENT_ID)])
                            ).Name
                        ));
                }

                this.ENT_ID = newId;
            }

            // construct the table
            if (this.ENT_ID >= 0)
            {
                if (dxf_AdditionalInfo != null)
                    dxf_AdditionalInfo.Replace(SimMultiValue.NEWLINE_PLACEHOLDER, Environment.NewLine);

                this.Decoder.ProjectData.ValueManager.StartLoading();
                var table = new SimMultiValueBigTable(ENT_ID, dxf_MVName, dxf_MVUnitX, dxf_MVUnitY, columnHeader, rowHeaders, dxf_Values, dxf_AdditionalInfo);
                this.Decoder.ProjectData.ValueManager.Add(table);
                this.Decoder.ProjectData.ValueManager.EndLoading();
            }
        }

        #endregion
    }
}
