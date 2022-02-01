using SIMULTAN.Serializer.DXF;
using SIMULTAN.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Media3D;

namespace SIMULTAN.Excel
{
    public class ExcelMappedData : INotifyPropertyChanged
    {
        #region PROPERTIES

        /// <summary>
        /// Name of the excel worksheet
        /// </summary>
        public string SheetName
        {
            get { return this.sheet_name; }
            set
            {
                if (this.sheet_name != value)
                {
                    this.sheet_name = value;
                    NotifyPropertyChanged(nameof(SheetName));
                }
            }
        }
        private string sheet_name;

        /// <summary>
        /// Stores the range in the excel sheet which should be used.
        /// X = Start Column, Y = Start Row, Z = Size in Columns, W = Size in Rows
        /// </summary>
        public Point4D Range
        {
            get { return this.range; }
            set
            {
                if (this.range != value)
                {
                    this.range = value;
                    NotifyPropertyChanged(nameof(Range));
                }
            }
        }
        private Point4D range;

        public List<List<string>> TextData { get; protected set; }
        public double[,] NumericData { get; protected set; }

        // derived
        public int SizeInColumns
        {
            get
            {
                if (this.TextData != null && this.TextData.Count > 0 && this.TextData[0] != null)
                    return this.TextData[0].Count;
                else if (this.NumericData != null && this.NumericData.GetLength(0) > 0)
                    return this.NumericData.GetLength(1);
                else
                    return 0;
            }
        }

        public int SizeInRows
        {
            get
            {
                if (this.TextData != null)
                    return this.TextData.Count;
                else if (this.NumericData != null)
                    return this.NumericData.GetLength(0);
                else
                    return 0;
            }
        }
        #endregion

        #region EVENTS

        public event PropertyChangedEventHandler PropertyChanged;
        protected void NotifyPropertyChanged(string prop)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        #endregion

        #region .CTOR
        protected ExcelMappedData(string _sheet_name, Point4D _range)
        {
            this.sheet_name = _sheet_name;
            this.range = _range;
            this.TextData = null;
            this.NumericData = null;
        }
        protected ExcelMappedData(string _sheet_name, Point4D _range, List<List<string>> _text_data)
        {
            this.sheet_name = _sheet_name;
            this.range = _range;
            this.TextData = _text_data;
            this.NumericData = null;
        }
        protected ExcelMappedData(string _sheet_name, Point4D _range, double[,] _numeric_data)
        {
            this.sheet_name = _sheet_name;
            this.range = _range;
            this.TextData = null;
            this.NumericData = _numeric_data;
        }
        #endregion

        #region COPY .CTOR
        internal ExcelMappedData(ExcelMappedData _original)
        {
            this.sheet_name = _original.sheet_name;
            this.range = new Point4D(_original.range.X, _original.range.Y, _original.range.Z, _original.range.W);
            this.TextData = (_original.TextData == null) ? null : _original.TextData.DeepCopy();
            this.NumericData = (_original.NumericData == null) ? null : (double[,])_original.NumericData.Clone();
        }

        public static List<ExcelMappedData> Copy(IEnumerable<ExcelMappedData> _originals)
        {
            if (_originals == null) return null;

            List<ExcelMappedData> copies = new List<ExcelMappedData>();
            foreach (var m in _originals)
            {
                copies.Add(new ExcelMappedData(m));
            }

            return copies;
        }

        #endregion

        #region MAPPING
        public static ExcelMappedData MapStringsTo(string _sheet_name, Point4D _range, List<List<string>> _text_data)
        {
            // check if the range and the data match
            if (_text_data == null) return null;
            if (_text_data.Count == 0) return null;

            bool row_size_OK = _text_data.Count == _range.W;
            bool col_size_OK = _text_data.All(x => x.Count == _range.Z);
            if (!row_size_OK || !col_size_OK) return null;

            return new ExcelMappedData(_sheet_name, _range, _text_data);
        }

        public static ExcelMappedData MapDoublesTo(string _sheet_name, Point4D _range, double[,] _numeric_data)
        {
            // check if the range and the data match
            if (_numeric_data == null) return null;
            if (_numeric_data.GetLength(0) == 0) return null;

            bool row_size_OK = _numeric_data.GetLength(0) == _range.W;
            bool col_size_OK = _numeric_data.GetLength(1) == _range.Z;
            if (!row_size_OK || !col_size_OK) return null;

            return new ExcelMappedData(_sheet_name, _range, _numeric_data);
        }

        public static ExcelMappedData MapOneStringTo(string _sheet_name, Point4D _range, string _content)
        {
            if (_range.W != 1 || _range.Z != 1) return null;
            string to_map = (_content == null) ? string.Empty : _content;

            return new ExcelMappedData(_sheet_name, _range, new List<List<string>> { new List<string> { to_map } });
        }

        public static ExcelMappedData MapOneDoubleTo(string _sheet_name, Point4D _range, double _content)
        {
            if (_range.W != 1 || _range.Z != 1) return null;

            return new ExcelMappedData(_sheet_name, _range, new double[,] { { _content } });
        }

        public static ExcelMappedData CreateEmpty(string _sheet_name, Point4D _range)
        {
            return new ExcelMappedData(_sheet_name, _range);
        }

        #endregion

        #region ToString
        public override string ToString()
        {
            string info = this.range.ToString();
            if (this.NumericData != null)
                info += ": " + this.NumericData[0,0].ToString();
            else if (this.TextData != null)
                info += ": " + this.TextData[0][0];

            return info;
        }

        internal string GetContent()
        {
            string content = string.Empty;
            if (this.NumericData != null && this.NumericData.GetLength(0) >= 1 && this.NumericData.GetLength(1) >= 1)
                content = this.NumericData[0,0].ToString();
            else if (this.TextData != null && this.TextData.Count >= 1 && this.TextData[0].Count >= 1)
                content = this.TextData[0][0];

            return content;
        }

        internal void AddToExport(ref StringBuilder _sb, Type _type)
        {
            // general
            _sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString()); // 0
            _sb.AppendLine(ParamStructTypes.EXCEL_DATA_RESULT);                       // EXCEL_RESULT

            _sb.AppendLine(((int)ParamStructCommonSaveCode.CLASS_NAME).ToString());
            _sb.AppendLine(this.GetType().ToString());

            // mapping: sheet name
            _sb.AppendLine(((int)ExcelMappingSaveCode.DATA_MAP_SHEET_NAME).ToString());
            _sb.AppendLine(this.sheet_name);

            // mapping: Range
            _sb.AppendLine(((int)ExcelMappingSaveCode.DATA_MAP_RANGE_X).ToString());
            _sb.AppendLine(this.range.X.ToString());

            _sb.AppendLine(((int)ExcelMappingSaveCode.DATA_MAP_RANGE_Y).ToString());
            _sb.AppendLine(this.range.Y.ToString());

            _sb.AppendLine(((int)ExcelMappingSaveCode.DATA_MAP_RANGE_Z).ToString());
            _sb.AppendLine(this.range.Z.ToString());

            _sb.AppendLine(((int)ExcelMappingSaveCode.DATA_MAP_RANGE_W).ToString());
            _sb.AppendLine(this.range.W.ToString());

            // mapping: type
            _sb.AppendLine(((int)ExcelMappingSaveCode.DATA_MAP_TYPE).ToString());
            _sb.AppendLine(_type.FullName);

        }

        #endregion

        #region UTILS

        public void OffsetBy(Point _offset)
        {
            this.Range = new Point4D(this.range.X + _offset.X, this.range.Y + _offset.Y, this.range.Z, this.range.W);
        }

        public static Point GetTotalOffset(List<ExcelMappedData> _mappings)
        {
            Point offset = new Point(0, 0);
            if (_mappings == null) return offset;
            if (_mappings.Count == 0) return offset;

            foreach (var m in _mappings)
            {
                offset = new Point(Math.Max(offset.X, m.range.X + m.range.Z), Math.Max(offset.Y, m.range.Y + m.range.W));
            }
            return offset;
        }

        #endregion
    }
}
