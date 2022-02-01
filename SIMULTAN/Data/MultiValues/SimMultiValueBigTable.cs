using SIMULTAN;
using SIMULTAN.Data.Components;
using SIMULTAN.Projects.Serializers;
using SIMULTAN.Serializer.DXF;
using SIMULTAN.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace SIMULTAN.Data.MultiValues
{
    /// <summary>
    /// A ValueField that can handle a lot of 2D data, but does not support interpolation.
    /// Allows to add units/names for columns and rows
    /// </summary>
    public class SimMultiValueBigTable : SimMultiValue
    {
        #region Helper Classes

        /// <summary>
        /// Specifies which dimensions were affected in a resize operation
        /// </summary>
        [Flags]
        public enum ResizeDirection
        {
            /// <summary>
            /// Only rows are affected
            /// </summary>
            Rows = 1,
            /// <summary>
            /// Only columns are affected
            /// </summary>
            Columns = 2,
            /// <summary>
            /// Both directions are affected
            /// </summary>
            Both = Rows | Columns,
        }

        /// <summary>
        /// Collection for Row/Column Headers. Automatically updates the properties of the headers
        /// </summary>
        public class HeaderCollection : ObservableCollection<SimMultiValueBigTableHeader>
        {
            private SimMultiValueBigTableHeader.AxisEnum axis;
            private SimMultiValueBigTable owner;

            /// <summary>
            /// Initializes a new instance of the HeaderCollection class
            /// </summary>
            /// <param name="owner">The table in which this collection exists</param>
            /// <param name="axis">The axis (row/column) which is stored in this collection</param>
            internal HeaderCollection(SimMultiValueBigTable owner, SimMultiValueBigTableHeader.AxisEnum axis)
            {
                this.axis = axis;
                this.owner = owner;
            }
            /// <summary>
            /// Initializes a new instance of the HeaderCollection class
            /// </summary>
            /// <param name="owner">The table in which this collection exists</param>
            /// <param name="axis">The axis (row/column) which is stored in this collection</param>
            /// <param name="values">The initial values for this collection</param>
            internal HeaderCollection(SimMultiValueBigTable owner, SimMultiValueBigTableHeader.AxisEnum axis, IEnumerable<SimMultiValueBigTableHeader> values)
                : base(values)
            {
                this.axis = axis;
                this.owner = owner;

                for (int i = 0; i < this.Count; ++i)
                {
                    var item = this[i];
                    item.Index = i;
                    item.Table = owner;
                    item.Axis = axis;
                }
            }

            /// <inheritdoc />
            protected override void InsertItem(int index, SimMultiValueBigTableHeader item)
            {
                if (item == null)
                    throw new ArgumentNullException(nameof(item));

                if (owner.resizeHandlingEnabled)
                {
                    if (axis == SimMultiValueBigTableHeader.AxisEnum.Rows)
                        owner.values.Insert(index, new List<double>(Enumerable.Repeat(0.0, owner.Count(1))));
                    else if (axis == SimMultiValueBigTableHeader.AxisEnum.Columns)
                    {
                        foreach (var row in owner.values)
                            row.Insert(index, 0.0);
                    }

                    item.Index = index;
                    item.Table = owner;
                    item.Axis = axis;

                    for (int i = index; i < this.Count; ++i)
                        this[i].Index = i + 1;
                }

                base.InsertItem(index, item);

                NotifyResized(index);
            }
            /// <inheritdoc />
            protected override void RemoveItem(int index)
            {
                if (owner.resizeHandlingEnabled)
                {
                    var oldItem = this[index];

                    oldItem.Axis = SimMultiValueBigTableHeader.AxisEnum.Undefined;
                    oldItem.Table = null;

                    if (axis == SimMultiValueBigTableHeader.AxisEnum.Rows)
                        owner.values.RemoveAt(index);
                    else if (axis == SimMultiValueBigTableHeader.AxisEnum.Columns)
                    {
                        foreach (var row in owner.values)
                            row.RemoveAt(index);
                    }


                    for (int i = index + 1; i < this.Count; ++i)
                        this[i].Index = i - 1;
                }

                base.RemoveItem(index);

                NotifyResized(index);
            }
            /// <inheritdoc />
            protected override void ClearItems()
            {
                if (owner.resizeHandlingEnabled)
                {
                    foreach (var item in this)
                    {
                        item.Axis = SimMultiValueBigTableHeader.AxisEnum.Undefined;
                        item.Table = null;
                    }

                    owner.values.Clear();
                }

                base.ClearItems();

                NotifyResized(0);
            }
            /// <inheritdoc />
            protected override void SetItem(int index, SimMultiValueBigTableHeader item)
            {
                if (item == null)
                    throw new ArgumentNullException(nameof(item));

                if (owner.resizeHandlingEnabled)
                {
                    var oldItem = this[index];

                    oldItem.Axis = SimMultiValueBigTableHeader.AxisEnum.Undefined;
                    oldItem.Table = null;

                    item.Axis = axis;
                    item.Table = owner;
                    item.Index = index;
                }

                base.SetItem(index, item);

            }

            private void NotifyResized(int start)
            {
                if (owner.resizeHandlingEnabled)
                {
                    if (this.axis == SimMultiValueBigTableHeader.AxisEnum.Columns)
                        owner.NotifyResized(ResizeDirection.Columns, -1, start);
                    else
                        owner.NotifyResized(ResizeDirection.Rows, start, -1);
                }
            }
        }

        #endregion


        #region Events

        /// <summary>
        /// EventArgs for the Resized event
        /// </summary>
        public class ResizeEventArgs : EventArgs
        {
            /// <summary>
            /// The affected directions
            /// </summary>
            public ResizeDirection ResizeDirection { get; }
            /// <summary>
            /// The start index along the rows. -1 when rows are not affected
            /// </summary>
            public int RowStartIndex { get; }
            /// <summary>
            /// The start index along the columns. -1 when columns are not affected
            /// </summary>
            public int ColumnStartIndex { get; }

            /// <summary>
            /// Initializes a new instance of the ResizeEventArgs class
            /// </summary>
            /// <param name="direction">The affected directions</param>
            /// <param name="rowStartIndex">The start index along the rows. -1 when rows are not affected</param>
            /// <param name="columnStartIndex">The start index along the columns. -1 when columns are not affected</param>
            public ResizeEventArgs(ResizeDirection direction, int rowStartIndex, int columnStartIndex)
            {
                this.ResizeDirection = direction;
                this.RowStartIndex = rowStartIndex;
                this.ColumnStartIndex = columnStartIndex;
            }
        }
        /// <summary>
        /// Invoked when the dimensions of the table have changed
        /// </summary>
        public event EventHandler<ResizeEventArgs> Resized;
        private void NotifyResized(ResizeDirection direction, int rowStartIndex, int columnStartIndex)
        {
            this.Resized?.Invoke(this, new ResizeEventArgs(direction, rowStartIndex, columnStartIndex));
            this.NotifyChanged();
        }


        /// <summary>
        /// EventArgs for Value change of Headers 
        /// </summary>
        public class HeaderValueChangedEventArgs : EventArgs
        {
            /// <summary>
            /// The affected header´s index
            /// </summary>
            public int Index { get; }
            /// <summary>
            /// The axes which contains the header
            /// </summary>
            public SimMultiValueBigTableHeader.AxisEnum Axis { get; }
            /// <summary>
            /// Initializes a new instance of the HeaderValueChangedEventArgs class
            /// </summary>
            /// <param name="index">The affected header´s index</param>
            /// <param name="axis">The axes which contains the header</param>
            public HeaderValueChangedEventArgs(int index, SimMultiValueBigTableHeader.AxisEnum axis)
            {
                this.Index = index;
                this.Axis = axis;
            }

        }
        /// <summary>
        /// Invoked when one of the Headers of the BigTable changed
        /// </summary>
        public event EventHandler<HeaderValueChangedEventArgs> HeaderValueChanged;
        internal void NotifyHeaderValueChanged(int index, SimMultiValueBigTableHeader.AxisEnum axis)
        {
            this.HeaderValueChanged?.Invoke(this, new HeaderValueChangedEventArgs(index, axis));
            this.NotifyChanged();
        }


        /// <summary>
        /// EventArgs for the ValueChanged event
        /// </summary>
        public class ValueChangedEventArgs : EventArgs
        {
            /// <summary>
            /// Row of the changed cell
            /// </summary>
            public int Row { get; private set; }
            /// <summary>
            /// Column of the changed cell
            /// </summary>
            public int Column { get; private set; }

            /// <summary>
            /// Initializes a new instance of the ValueChangedEventArgs class
            /// </summary>
            /// <param name="row">Row index of the changed cell</param>
            /// <param name="column">Column index of the changed cell</param>
            public ValueChangedEventArgs(int row, int column)
            {
                this.Row = row;
                this.Column = column;
            }
        }
        /// <summary>
        /// EventHandler for the ValueChanged event
        /// </summary>
        /// <param name="sender">The instance which sent the event</param>
        /// <param name="args">The event args</param>
        public delegate void ValueChangedEventHandler(object sender, ValueChangedEventArgs args);
        /// <summary>
        /// Invoked when a value in the table has changed
        /// </summary>
        public event ValueChangedEventHandler ValueChanged;
        /// <summary>
        /// Invokes the ValueChanged event
        /// </summary>
        /// <param name="row">Row index of the changed cell</param>
        /// <param name="column">Column index of the changed cell</param>
        protected void NotifyValueChanged(int row, int column)
        {
            this.ValueChanged?.Invoke(this, new ValueChangedEventArgs(row, column));
            this.NotifyChanged();
        }

        /// <summary>
        /// EventArgs for the CollectionReplaced event. Stores old and new collection for the modified headers.
        /// Note that OldXXXHeader and NewXXXHeader may contain the same values
        /// </summary>
        public class CollectionsReplacedEventArgs : EventArgs
        {
            /// <summary>
            /// The old row header collection
            /// </summary>
            public ObservableCollection<SimMultiValueBigTableHeader> OldRowHeaders { get; }
            /// <summary>
            /// The new row header collection
            /// </summary>
            public ObservableCollection<SimMultiValueBigTableHeader> NewRowHeaders { get; }

            /// <summary>
            /// The old column header collection
            /// </summary>
            public ObservableCollection<SimMultiValueBigTableHeader> OldColumnHeaders { get; }
            /// <summary>
            /// The new column header collection
            /// </summary>
            public ObservableCollection<SimMultiValueBigTableHeader> NewColumnHeaders { get; }


            /// <summary>
            /// Initializes a new instance of the CollectionsReplacedEventArgs class
            /// </summary>
            /// <param name="oldRowHeaders">The row header collection before the modification</param>
            /// <param name="newRowHeaders">The row header collection after the modification</param>
            /// <param name="oldColumnHeaders">The column header collection before the modification</param>
            /// <param name="newColumnHeaders">The column header collection after the modification</param>
            public CollectionsReplacedEventArgs(ObservableCollection<SimMultiValueBigTableHeader> oldRowHeaders,
                ObservableCollection<SimMultiValueBigTableHeader> newRowHeaders,
                ObservableCollection<SimMultiValueBigTableHeader> oldColumnHeaders,
                ObservableCollection<SimMultiValueBigTableHeader> newColumnHeaders)
            {
                this.OldRowHeaders = oldRowHeaders;
                this.NewRowHeaders = newRowHeaders;
                this.OldColumnHeaders = oldColumnHeaders;
                this.NewColumnHeaders = newColumnHeaders;
            }
        }
        /// <summary>
        /// Invoked when either the RowHeader or the ColumnHeader collection (or both) has been replaced
        /// </summary>
        private event EventHandler<CollectionsReplacedEventArgs> CollectionReplaced;
        private void NotifyCollectionReplaced(CollectionsReplacedEventArgs args)
        {
            this.CollectionReplaced?.Invoke(this, args);
        }

        #endregion


        #region PROPERTIES

        private bool resizeHandlingEnabled = true;

        /// <summary>
        /// Headers for rows
        /// </summary>
        public HeaderCollection RowHeaders
        {
            get { return rowHeaders; }
            private set
            {
                rowHeaders = value;
                NotifyPropertyChanged(nameof(RowHeaders));
                NotifyChanged();
            }
        }
        private HeaderCollection rowHeaders;

        /// <summary>
        /// Headers for columns
        /// </summary>
        public HeaderCollection ColumnHeaders
        {
            get { return columnHeaders; }
            private set
            {
                columnHeaders = value;
                NotifyPropertyChanged(nameof(ColumnHeaders));
                NotifyChanged();
            }
        }
        private HeaderCollection columnHeaders;

        /// <inheritdoc />
        public override bool CanInterpolate
        {
            get => false;
            set { }
        }

        /// <summary>
        /// The table values organized row-wise (a list of consecutive rows)
        /// </summary>
        [Obsolete("Use the indexer instead")]
        public List<List<double>> Values { get { return values; } }
        private List<List<double>> values;

        /// <summary>
        /// Stores additional information about the valuefield. This can be a multiline text
        /// </summary>
        public string AdditionalInfo
        {
            get { return this.additionalInfo; }
            set
            {
                if (this.additionalInfo != value)
                {
                    this.additionalInfo = value;
                    NotifyPropertyChanged(nameof(AdditionalInfo));
                    NotifyChanged();
                }
            }
        }
        private string additionalInfo;

        /// <inheritdoc />
        public override MultiValueType MVType => MultiValueType.TABLE;

        #endregion


        #region .CTOR

        /// <summary>
        /// Initializes a new instance of the MultiValueBigTable class.
        /// rowHeaders.Count has to match values.Count
        /// columnHeaders.Count has to match the count of each entry in values
        /// </summary>
        /// <param name="name">Name of the SimMultiValue field</param>
        /// <param name="unitColumns">Unit description for the x-axis (columns)</param>
        /// <param name="unitRows">Unit description for the y-axis (rows)</param>
        /// <param name="columnHeaders">List of column headers</param>
        /// <param name="rowHeaders">List of row headers</param>
        /// <param name="values">The values in this field</param>
        public SimMultiValueBigTable(string name, string unitColumns, string unitRows,
            ICollection<SimMultiValueBigTableHeader> columnHeaders, ICollection<SimMultiValueBigTableHeader> rowHeaders,
            List<List<double>> values)
            : base(name, unitColumns, unitRows, "")
        {
            if (name == null)
                throw new ArgumentNullException("Name may not be null or empty");
            if (columnHeaders == null)
                throw new ArgumentNullException("columnHeaders may not be null");
            if (rowHeaders == null)
                throw new ArgumentNullException("rowNames may not be null");
            if (values == null)
                throw new ArgumentNullException("values may not be null");

            if (values.Count != rowHeaders.Count)
                throw new ArgumentException("Row header count has to match number of rows in values");
            for (int i = 0; i < values.Count; ++i)
                if (values[i].Count != columnHeaders.Count)
                    throw new ArgumentException(string.Format("Number of elements in Column {0} does not match column header count", i));

            this.ColumnHeaders = new HeaderCollection(this, SimMultiValueBigTableHeader.AxisEnum.Columns, columnHeaders);
            this.RowHeaders = new HeaderCollection(this, SimMultiValueBigTableHeader.AxisEnum.Rows, rowHeaders);
            this.values = values.Select(x => x.ToList()).ToList();
        }

        /// <summary>
        /// Initializes a new instance of the MultiValueBigTable class.
        /// rowHeaders.Count has to match values.GetLength(0)
        /// columnHeaders.Count has to match values.GetLength(0)
        /// </summary>
        /// <param name="name">Name of the SimMultiValue field</param>
        /// <param name="unitColumns">Unit description for the x-axis (columns)</param>
        /// <param name="unitRows">Unit description for the y-axis (rows)</param>
        /// <param name="columnHeaders">List of column headers</param>
        /// <param name="rowHeaders">List of row headers</param>
        /// <param name="values">The values in this field</param>
        public SimMultiValueBigTable(string name, string unitColumns, string unitRows,
            ICollection<SimMultiValueBigTableHeader> columnHeaders, ICollection<SimMultiValueBigTableHeader> rowHeaders,
            double[,] values)
            : base(name, unitColumns, unitRows, "")
        {
            if (name == null)
                throw new ArgumentNullException("Name may not be null or empty");
            if (columnHeaders == null)
                throw new ArgumentNullException("columnHeaders may not be null");
            if (rowHeaders == null)
                throw new ArgumentNullException("rowNames may not be null");
            if (values == null)
                throw new ArgumentNullException("values may not be null");

            if (values.GetLength(0) != rowHeaders.Count)
                throw new ArgumentException("Row header count has to match number of rows in values");
            if (values.GetLength(1) != columnHeaders.Count)
                throw new ArgumentException("Column header count has to match number of rows in values");

            this.ColumnHeaders = new HeaderCollection(this, SimMultiValueBigTableHeader.AxisEnum.Columns, columnHeaders);
            this.RowHeaders = new HeaderCollection(this, SimMultiValueBigTableHeader.AxisEnum.Rows, rowHeaders);

            List<List<double>> valueList = new List<List<double>>(values.GetLength(0));
            for (int r = 0; r < values.GetLength(0); r++)
            {
                List<double> row = new List<double>(values.GetLength(1));
                for (int c = 0; c < values.GetLength(1); c++)
                {
                    row.Add(values[r, c]);
                }
                valueList.Add(row);
            }
            this.values = valueList;
        }

        #endregion

        #region .CTOR for Parsing

        /// <summary>
        /// Initializes a new instance of the MultiValueBigTable class.
        /// rowHeaders.Count has to match values.Count
        /// columnHeaders.Count has to match the count of each entry in values
        /// </summary>
        /// <param name="localId">ID of the SimMultiValue (used when loading fields from a, e.g., a file)</param>
        /// <param name="name">Name of the MultiValue field</param>
        /// <param name="unitColumns">Unit description of the X-axis. Just a text, does not influence any calculations.</param>
        /// <param name="unitRows">Unit description of the Y-axis. Just a text, does not influence any calculations.</param>
        /// <param name="columnHeaders">List of column headers</param>
        /// <param name="rowHeaders">List of row headers</param>
        /// <param name="values">The values in this field</param>
        /// <param name="additionalInfo">Additional info text</param>
        internal SimMultiValueBigTable(long localId, string name,
                                    string unitColumns, string unitRows,
                                    ICollection<SimMultiValueBigTableHeader> columnHeaders, ICollection<SimMultiValueBigTableHeader> rowHeaders,
                                    List<List<double>> values, string additionalInfo)
            : base(localId, name, unitColumns, unitRows, string.Empty)
        {
            if (name == null)
                throw new ArgumentNullException("Name may not be null");
            if (name == string.Empty)
                throw new ArgumentException("Name may not be empty");
            if (columnHeaders == null)
                throw new ArgumentNullException("columnHeaders may not be null");
            if (rowHeaders == null)
                throw new ArgumentNullException("rowNames may not be null");
            if (values == null)
                throw new ArgumentNullException("values may not be null");

            if (values.Count != rowHeaders.Count)
                throw new ArgumentException("rowNames.Count has to match number of rows in values");
            for (int i = 0; i < values.Count; ++i)
                if (values[i].Count != columnHeaders.Count)
                    throw new ArgumentException(string.Format("Number of elements in Row {0} does not match columnheaders.Count", i));

            this.ColumnHeaders = new HeaderCollection(this, SimMultiValueBigTableHeader.AxisEnum.Columns, columnHeaders);
            this.values = new List<List<double>>(values);
            if (rowHeaders != null)
                this.RowHeaders = new HeaderCollection(this, SimMultiValueBigTableHeader.AxisEnum.Rows, rowHeaders);
            else
                this.RowHeaders = new HeaderCollection(this, SimMultiValueBigTableHeader.AxisEnum.Rows);

            this.additionalInfo = additionalInfo;
        }

        #endregion

        #region .CTOR for COPYING

        /// <summary>
        /// Creates a copy of the table
        /// </summary>
        /// <param name="original">The original table</param>
        protected SimMultiValueBigTable(SimMultiValueBigTable original) : base(original)
        {
            if (original == null)
                throw new ArgumentNullException(string.Format("{0} may not be null", nameof(original)));

            this.RowHeaders = new HeaderCollection(this, SimMultiValueBigTableHeader.AxisEnum.Rows, original.rowHeaders.Select(x => x.Clone()));
            this.ColumnHeaders = new HeaderCollection(this, SimMultiValueBigTableHeader.AxisEnum.Columns, original.columnHeaders.Select(x => x.Clone()));
            this.values = original.values.Select(x => new List<double>(x)).ToList();
            this.additionalInfo = original.additionalInfo;
        }

        /// <inheritdoc />
        public override SimMultiValue Clone()
        {
            return new SimMultiValueBigTable(this);
        }

        #endregion


        #region METHODS: Info

        /// <summary>
        /// rows-lower, rows-upper, columns-lower, columns-upper limits
        /// </summary>
        /// <returns></returns>
        [Obsolete]
        public Point4D GetSize()
        {
            int size_rows = Count(0);
            int size_cols = Count(1);

            if (size_rows == 0)
                return new Point4D(0, 0, 1, size_cols);
            else
                return new Point4D(1, size_rows, 1, size_cols);
        }

        /// <summary>
        /// Returns the count along an axis
        /// </summary>
        /// <param name="dimension">The count along the selected axis. 0 means rows, 1 means columns</param>
        /// <returns></returns>
        public int Count(int dimension)
        {
            if (dimension < 0 || dimension > 1)
                throw new ArgumentOutOfRangeException("dimension has to be 0 or 1");

            if (dimension == 0)
            {
                return this.values.Count;
            }
            else
            {
                if (values.Count == 0)
                    return 0;
                else
                    return values[0].Count;
            }
        }


        #endregion

        #region METHODS: Get Range of Values

        /// <summary>
        /// Gets a rectangular range of values out of the table.
        /// </summary>
        /// <param name="_range_definition">gives the 1-based indices of the start and end row, and the start and end column
        /// X: Row Start
        /// Y: Row End
        /// Z: Column Start
        /// W: Column End
        /// </param>
        /// <returns>the extracted values</returns>
        public List<List<double>> GetRange(Point4D _range_definition)
        {
            List<List<double>> range = new List<List<double>>();

            if (this.values.Count == 0) return range;
            if (this.values[0].Count == 0) return range;

            int row_start = (int)_range_definition.X - 1;
            int row_end = (int)_range_definition.Y - 1;
            int col_start = (int)_range_definition.Z - 1;
            int col_end = (int)_range_definition.W - 1;

            // check validity of range definition
            if (row_start < 0 || row_start >= this.values.Count ||
                row_end < 0 || row_end >= this.values.Count ||
                row_start > row_end)
                return range;
            if (col_start < 0 || col_start >= this.values[0].Count ||
                col_end < 0 || col_end >= this.values[0].Count ||
                col_start > col_end)
                return range;

            // extract range
            for (int row = row_start; row <= row_end; row++)
            {
                List<double> current_row = new List<double>();
                for (int col = col_start; col <= col_end; col++)
                {
                    current_row.Add(this.values[row][col]);
                }
                range.Add(current_row);
            }

            return range;
        }

        /// <summary>
        /// Gets a rectangular range of values out of the table. X-Axis describes Rows, Y-Axis describes Columns
        /// </summary>
        /// <param name="range">The 0-based range which should be extracted</param>
        /// <returns>A rectangular double matrix, limited either by the size of the field or by the defined range.
        /// Ranges outside the valuefield are ignored</returns>
        public double[,] GetRange(RowColumnRange range)
        {
            int columnFrom = range.ColumnStart.Clamp(0, Count(1));
            int rowFrom = range.RowStart.Clamp(0, Count(0));

            int columnTo = (range.ColumnStart + range.ColumnCount).Clamp(columnFrom, Count(1));
            int rowTo = (range.RowStart + range.RowCount).Clamp(columnFrom, Count(0));

            double[,] result = new double[rowTo - rowFrom, columnTo - columnFrom];

            for (var row = rowFrom; row < rowTo; row++)
                for (var col = columnFrom; col < columnTo; col++)
                    result[row - rowFrom, col - columnFrom] = values[row][col];

            return result;
        }

        /// <summary>
        /// Returns the row with given index.
        /// Throws an argument exception if index is out of bounds.
        /// </summary>
        /// <param name="idx">Row index</param>
        /// <returns>Row values</returns>
        public IEnumerable<double> GetRow(int idx)
        {
            if (idx < 0 || idx >= rowHeaders.Count)
                throw new IndexOutOfRangeException("Index must be non-negative and smaller than the row count.");

            return values[idx];
        }

        /// <summary>
        /// Returns the column with given index.
        /// Throws an argument exception if index is out of bounds.
        /// </summary>
        /// <param name="idx">Column index</param>
        /// <returns>Column values</returns>
        public IEnumerable<double> GetColumn(int idx)
        {
            if (idx < 0 || idx >= ColumnHeaders.Count)
                throw new IndexOutOfRangeException("Index must be non-negative and smaller than the column count.");

            for (int i = 0; i < RowHeaders.Count; i++)
                yield return this[i, idx];
        }

        /// <summary>
        /// Returns the full data of this table. The first dimension equals rows, second dimension describes columns
        /// </summary>
        /// <returns>An array containing the full data of this table</returns>
        public double[,] GetData()
        {
            return GetRange(new RowColumnRange(0, 0, Count(0), Count(1)));
        }

        #endregion


        #region To String

        /// <inheritdoc />
        [Obsolete]
        public override string ToString()
        {
            string t_string = "(" + this.Id + ") "
                                + this.MVType.ToString() + " [";
            for (int i = 0; i < this.ColumnHeaders.Count; i++)
            {
                if (i < this.ColumnHeaders.Count - 1)
                    t_string += this.ColumnHeaders[i].Unit + ", ";
                else
                    t_string += this.ColumnHeaders[i].Unit + "]";
            }
            t_string += ": [" + this.values.Count + " rows]";

            return t_string;
        }

        /// <inheritdoc />
        public override void AddToExport(ref StringBuilder _sb)
        {
            if (_sb == null) return;

            _sb.AppendLine(((int)ParamStructCommonSaveCode.ENTITY_START).ToString()); // 0
            _sb.AppendLine(ParamStructTypes.BIG_TABLE);                               // BIG_TABLE

            _sb.AppendLine(((int)ParamStructCommonSaveCode.CLASS_NAME).ToString());
            _sb.AppendLine(this.GetType().ToString());

            // common, common display, common info   
            base.AddToExport(ref _sb);

            // names
            _sb.AppendLine(((int)MultiValueSaveCode.XS).ToString());
            _sb.AppendLine(this.ColumnHeaders.Count.ToString());
            for (int i = 0; i < this.ColumnHeaders.Count; i++)
            {
                _sb.AppendLine(((int)ParamStructCommonSaveCode.X_VALUE).ToString());
                _sb.AppendLine(this.ColumnHeaders[i].Name);
            }

            // units
            _sb.AppendLine(((int)MultiValueSaveCode.YS).ToString());
            _sb.AppendLine(this.ColumnHeaders.Count.ToString());
            for (int i = 0; i < this.ColumnHeaders.Count; i++)
            {
                _sb.AppendLine(((int)ParamStructCommonSaveCode.X_VALUE).ToString());
                _sb.AppendLine(this.ColumnHeaders[i].Unit);
            }

            // values
            _sb.AppendLine(((int)MultiValueSaveCode.FIELD).ToString());
            _sb.AppendLine(this.values.Count.ToString());
            _sb.AppendLine(((int)ParamStructCommonSaveCode.NUMBER_OF).ToString());
            _sb.AppendLine(this.values[0].Count.ToString());

            // NEW: updated 29.10.2019 to speed up saving (takes about 10 sec for 900 MB)
            ParallelBigTableSerializer ps = new ParallelBigTableSerializer(this.values, 1000, "F8", (int)MultiValueSaveCode.MVBT_COMPLETE_VALUE_ROW);
            StringBuilder sb1 = _sb; // passes a pointer, does not copy
            Task value_task = Task.Run(async () => await ps.SerializeValuesDXFStyleAsync(sb1));
            value_task.Wait();

            // row names/units
            if (this.RowHeaders != null)
            {
                _sb.AppendLine(((int)MultiValueSaveCode.ROW_NAMES).ToString());
                _sb.AppendLine(this.RowHeaders.Count.ToString());
                foreach (var rh in this.RowHeaders)
                {
                    _sb.AppendLine(((int)ParamStructCommonSaveCode.STRING_VALUE).ToString());
                    _sb.AppendLine(rh.Name);
                }

                _sb.AppendLine(((int)MultiValueSaveCode.ROW_UNITS).ToString());
                _sb.AppendLine(this.RowHeaders.Count.ToString());
                foreach (var rh in this.RowHeaders)
                {
                    _sb.AppendLine(((int)ParamStructCommonSaveCode.STRING_VALUE).ToString());
                    _sb.AppendLine(rh.Unit);
                }
            }
            else
            {
                _sb.AppendLine(((int)MultiValueSaveCode.ROW_NAMES).ToString());
                _sb.AppendLine("0");
            }

            // additional info
            if (this.AdditionalInfo != null)
            {
                _sb.AppendLine(((int)MultiValueSaveCode.ADDITIONAL_INFO).ToString());
                string all_lines = this.AdditionalInfo.Replace(Environment.NewLine, SimMultiValue.NEWLINE_PLACEHOLDER);
                _sb.AppendLine(all_lines);
            }
        }

        #endregion


        #region METHODS: External Pointer

        /// <summary>
        /// A ValuePointer that points to a cell in a MultiValueBigTable
        /// </summary>
        public class SimMultiValueBigTablePointer : SimMultiValuePointer
        {
            //~MultiValueBigTablePointer() { Console.WriteLine("~MultiValueBigTablePointer"); }

            private SimMultiValueBigTable table;

            /// <summary>
            /// The row index of the cell
            /// </summary>
            public int Row { get; private set; }
            /// <summary>
            /// The column index of the cell
            /// </summary>
            public int Column { get; private set; }

            /// <inheritdoc />
            public override SimMultiValue ValueField
            {
                get { return table; }
                set
                {
                    if (value is SimMultiValueBigTable)
                        table = (SimMultiValueBigTable)value;
                    else
                        throw new ArgumentException("MultiValueBigTablePointer only supports MultiValueBigTable fields");
                }
            }

            /// <summary>
            /// Initializes a new instance of the MultiValueBigTablePointer class
            /// </summary>
            /// <param name="table">The table into which the pointer points</param>
            /// <param name="row">Row index of the target cell</param>
            /// <param name="column">Column index of the target cell</param>
            public SimMultiValueBigTablePointer(SimMultiValueBigTable table, int row, int column) : base(table)
            {
                this.Row = row;
                this.Column = column;

                if (this.Row >= table.RowHeaders.Count || this.Row < -1 || this.Column >= table.ColumnHeaders.Count || this.Column < -1)
                    SetRowColumn(-1, -1);

                AttachEvents();

                RegisterParameter(ReservedParameters.MVBT_OFFSET_X_FORMAT, table.UnitX);
                RegisterParameter(ReservedParameters.MVBT_OFFSET_Y_FORMAT, table.UnitY);
            }

            /// <inheritdoc />
            public override double GetValue()
            {
                if (IsDisposed)
                    throw new InvalidOperationException("You're trying to get the value of an unsubscribed value pointer");

                if (Row == -1 && Column == -1)
                    return double.NaN;

                var offset = GetParameterOffsets();

                if (Row + offset.rowOffset >= table.Count(0) || Column + offset.columnOffset >= table.Count(1))
                    return double.NaN;

                return table[Row + offset.rowOffset, Column + offset.columnOffset];
            }
            /// <inheritdoc />
            public override SimMultiValuePointer Clone()
            {
                return new SimMultiValueBigTablePointer(table, Row, Column);
            }
            /// <inheritdoc />
            public override void AddToExport(ref StringBuilder sb)
            {
                base.AddToExport(ref sb, Column, Row, 0, "");
            }
            /// <inheritdoc />
            public override void SetFromParameters(double axisValueX, double axisValueY, double axisValueZ, string gs)
            {
                if (axisValueY >= table.RowHeaders.Count)
                    Row = table.rowHeaders.Count - 1;
                else
                    Row = (int)axisValueY;

                if (axisValueX >= table.ColumnHeaders.Count)
                    Column = table.ColumnHeaders.Count - 1;
                else
                    Column = (int)axisValueX;
            }
            /// <inheritdoc />
            public override bool IsSamePointer(SimMultiValuePointer other)
            {
                if (other is SimMultiValueBigTablePointer o)
                {
                    return (table == o.table && Row == o.Row && Column == o.Column);
                }
                return false;
            }

            private void Table_ContentReplaced(object sender, CollectionsReplacedEventArgs e)
            {
                e.OldColumnHeaders.CollectionChanged -= ColumnHeaders_CollectionChanged;
                e.OldRowHeaders.CollectionChanged -= RowHeaders_CollectionChanged;

                if (this.Row >= e.NewRowHeaders.Count || this.Column >= e.NewColumnHeaders.Count)
                    SetRowColumn(-1, -1);
                this.NotifyValueChanged();

                e.NewColumnHeaders.CollectionChanged += ColumnHeaders_CollectionChanged;
                e.NewRowHeaders.CollectionChanged += RowHeaders_CollectionChanged;
            }

            private void Table_ValueChanged(object sender, ValueChangedEventArgs args)
            {
                var offset = GetParameterOffsets();

                if (args.Row == this.Row + offset.rowOffset && args.Column == this.Column + offset.columnOffset)
                    this.NotifyValueChanged();
            }

            private void RowHeaders_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
            {
                if (e.Action == NotifyCollectionChangedAction.Add)
                {
                    if (e.NewStartingIndex <= this.Row)
                    {
                        SetRowColumn(this.Row + e.NewItems.Count, this.Column);
                    }
                }
                else if (e.Action == NotifyCollectionChangedAction.Remove)
                {
                    if (e.OldStartingIndex <= this.Row)
                    {
                        if (e.OldStartingIndex + e.OldItems.Count < this.Row) //Removed rows are completely before -> keep pointer
                            SetRowColumn(this.Row - e.OldItems.Count, this.Column);
                        else //pointer position got deleted
                            SetRowColumn(-1, -1);
                    }
                }
            }

            private void ColumnHeaders_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
            {
                if (e.Action == NotifyCollectionChangedAction.Add)
                {
                    if (e.NewStartingIndex <= this.Column)
                    {
                        SetRowColumn(this.Row, this.Column + e.NewItems.Count);
                    }
                }
                else if (e.Action == NotifyCollectionChangedAction.Remove)
                {
                    if (e.OldStartingIndex <= this.Column)
                    {
                        if (e.OldStartingIndex + e.OldItems.Count < this.Column) //Removed columns are completely before -> keep pointer
                            SetRowColumn(this.Row, this.Column - e.OldItems.Count);
                        else //pointer position got deleted
                            SetRowColumn(-1, -1);
                    }
                }
            }

            private void SetRowColumn(int row, int column)
            {
                this.Row = row;
                this.Column = column;
                NotifyValueChanged();
            }

            /// <inheritdoc />
            protected override void Dispose(bool isDisposing)
            {
                if (!IsDisposed)
                {
                    DetachEvents();
                }

                base.Dispose(isDisposing);
            }

            private bool isAttached = false;

            internal override void AttachEvents()
            {
                base.AttachEvents();

                if (!isAttached && !IsDisposed)
                {
                    table.RowHeaders.CollectionChanged += RowHeaders_CollectionChanged;
                    table.ColumnHeaders.CollectionChanged += ColumnHeaders_CollectionChanged;
                    table.ValueChanged += Table_ValueChanged;
                    table.CollectionReplaced += Table_ContentReplaced;

                    isAttached = true;
                }
            }

            internal override void DetachEvents()
            {
                base.DetachEvents();

                if (isAttached)
                {
                    isAttached = false;
                    table.RowHeaders.CollectionChanged -= RowHeaders_CollectionChanged;
                    table.ColumnHeaders.CollectionChanged -= ColumnHeaders_CollectionChanged;
                    table.ValueChanged -= Table_ValueChanged;
                    table.CollectionReplaced -= Table_ContentReplaced;
                }
            }

            private (int rowOffset, int columnOffset) GetParameterOffsets()
            {
                int paramAddX = 0, paramAddY = 0;
                var paramX = GetValuePointerParameter(ReservedParameters.MVBT_OFFSET_X_FORMAT);
                if (paramX != null)
                    paramAddX = (int)paramX.ValueCurrent;

                var paramY = GetValuePointerParameter(ReservedParameters.MVBT_OFFSET_Y_FORMAT);
                if (paramY != null)
                    paramAddY = (int)paramY.ValueCurrent;

                return (paramAddY, paramAddX);
            }
        }

        /// <summary>
        /// Returns a default pointer for this table
        /// </summary>
        public SimMultiValuePointer DefaultPointer
        {
            get { return new SimMultiValueBigTablePointer(this, 0, 0); }
        }

        /// <inheritdoc />
        public override SimMultiValuePointer CreateNewPointer()
        {
            return DefaultPointer;
        }

        /// <inheritdoc />
        public override SimMultiValuePointer CreateNewPointer(SimMultiValuePointer source)
        {
            if (source is SimMultiValueBigTablePointer ptr)
            {
                return new SimMultiValueBigTablePointer(this, ptr.Row, ptr.Column);
            }
            return DefaultPointer;
        }

        #endregion

        #region Aggregate

        /// <summary>
        /// Applies an aggregation function to this table and replaces the data in this table by the result.
        /// </summary>
        /// <param name="function">The function to apply</param>
        /// <param name="transpose">When set to False, the aggregation is performed to each columns. When set to True, the aggregation is applied to each rows</param>
        internal void ApplyAggregationFunction(SimAggregationFunction function, bool transpose)
        {
            //Does not handle units correctly

            if (transpose) //Aggregate along rows
            {
                // extract named columns
                List<List<double>> columns = this.values.Transpose();

                List<KeyValuePair<string, List<double>>> named_columns = new List<KeyValuePair<string, List<double>>>();
                for (int i = 0; i < this.ColumnHeaders.Count; i++)
                {
                    named_columns.Add(new KeyValuePair<string, List<double>>(this.ColumnHeaders[i].Name, columns[i]));
                }
                // group named columns
                var groups = named_columns.GroupBy(x => x.Key).ToDictionary(gr => gr.Key, gr => gr.Select(x => x.Value).ToList());
                // aggregate rows
                var aggregation = SimMultiValueBigTable.ApplyAggregationTo(groups, function);


                var resultData = new List<List<double>>(aggregation.Count);

                foreach (var group in aggregation)
                {
                    resultData.Add(group.values);
                }

                resultData = resultData.Transpose();

                var columnHeaders = new List<SimMultiValueBigTableHeader>(
                    aggregation.Select((x, xi) => new SimMultiValueBigTableHeader(
                        string.Format("{0} {1}", function.ToStringRepresentation(), x.key),
                        ColumnHeaders.First(ch => ch.Name == x.key).Unit)
                    ));

                ReplaceData(columnHeaders, this.RowHeaders, resultData);
            }
            else
            {
                // use rows
                List<KeyValuePair<string, List<double>>> named_rows = new List<KeyValuePair<string, List<double>>>();
                for (int i = 0; i < this.RowHeaders.Count; i++)
                {
                    named_rows.Add(new KeyValuePair<string, List<double>>(this.RowHeaders[i].Name, this.GetRow(i).ToList()));
                }
                // group named rows
                Dictionary<string, List<List<double>>> groups = named_rows.GroupBy(x => x.Key).ToDictionary(gr => gr.Key, gr => gr.Select(x => x.Value).ToList());
                // aggregate rows
                var aggregation = SimMultiValueBigTable.ApplyAggregationTo(groups, function);
                var rowHeaders = new ObservableCollection<SimMultiValueBigTableHeader>(
                    aggregation.Select((x, xi) => new SimMultiValueBigTableHeader(
                        string.Format("{0} {1}", function.ToStringRepresentation(), x.key),
                        this.RowHeaders.First(rh => rh.Name == x.key).Unit))
                    );
                var resultData = aggregation.Select(x => x.values).ToList();

                ReplaceData(this.ColumnHeaders, rowHeaders, resultData);
            }
        }

        private static List<(string key, List<double> values)> ApplyAggregationTo(Dictionary<string, List<List<double>>> _input,
            SimAggregationFunction function, bool _order = true)
        {
            List<(string, List<double>)> result = new List<(string, List<double>)>(_input.Count);

            IEnumerable<KeyValuePair<string, List<List<double>>>> orderedInput = _input;
            if (_order)
                orderedInput = orderedInput.OrderBy(x => x.Key);

            foreach (var group in orderedInput)
            {
                List<double> groupResult = new List<double>();
                double[] row = new double[group.Value.Count];

                for (int r = 0; r < group.Value[0].Count; r++)
                {
                    for (int c = 0; c < group.Value.Count; c++)
                        row[c] = group.Value[c][r];

                    double rowResult = double.NaN;
                    switch (function)
                    {
                        case SimAggregationFunction.Sum:
                            rowResult = row.Sum();
                            break;
                        case SimAggregationFunction.Average:
                            rowResult = row.Sum() / row.Length;
                            break;
                        case SimAggregationFunction.Max:
                            rowResult = row.Max();
                            break;
                        case SimAggregationFunction.Min:
                            rowResult = row.Min();
                            break;
                        case SimAggregationFunction.Count:
                            rowResult = row.Length;
                            break;
                    }

                    groupResult.Add(rowResult);
                }

                result.Add((group.Key, groupResult));
            }

            return result;
        }

        #endregion


        #region Data Access

        /// <summary>
        /// Returns the value at a specific location. Throws an exception when out of range
        /// </summary>
        /// <param name="row">The row</param>
        /// <param name="column">The column</param>
        /// <returns>The value at row/column</returns>
        public double this[int row, int column]
        {
            get
            {
                if (row >= this.RowHeaders.Count || row < 0)
                    throw new IndexOutOfRangeException("row must be positive and smaller than the row count");
                if (column >= this.ColumnHeaders.Count || column < 0)
                    throw new IndexOutOfRangeException("column must be positive or smaller than the column count");

                return values[row][column];
            }
            set
            {
                if (row >= this.RowHeaders.Count || row < 0)
                    throw new IndexOutOfRangeException("row must be positive and smaller than the row count");
                if (column >= this.ColumnHeaders.Count || column < 0)
                    throw new IndexOutOfRangeException("column must be positive or smaller than the column count");

                values[row][column] = value;
                NotifyValueChanged(row, column);
            }
        }

        /// <summary>
        /// Replaces the data in this instance with new data
        /// </summary>
        /// <param name="columnHeaders">New row headers</param>
        /// <param name="rowHeaders">New column headers</param>
        /// <param name="values">New values headers</param>
        public void ReplaceData(ICollection<SimMultiValueBigTableHeader> columnHeaders, ICollection<SimMultiValueBigTableHeader> rowHeaders,
            List<List<double>> values)
        {
            if (columnHeaders == null)
                throw new ArgumentNullException("columnHeaders may not be null");
            if (rowHeaders == null)
                throw new ArgumentNullException("rowNames may not be null");
            if (values == null)
                throw new ArgumentNullException("values may not be null");

            if (values.Count != rowHeaders.Count)
                throw new ArgumentException("rowNames.Count has to match number of rows in values");
            for (int i = 0; i < values.Count; ++i)
                if (values[i].Count != columnHeaders.Count)
                    throw new ArgumentException(string.Format("Number of elements in Column {0} does not match columnheaders.Count", i));

            var oldColumnHeaders = this.ColumnHeaders;
            var oldRowHeaders = this.RowHeaders;

            this.ColumnHeaders = new HeaderCollection(this, SimMultiValueBigTableHeader.AxisEnum.Columns, columnHeaders);
            this.RowHeaders = new HeaderCollection(this, SimMultiValueBigTableHeader.AxisEnum.Rows, rowHeaders);
            this.values = values.Select(x => x.ToList()).ToList();

            NotifyResized(ResizeDirection.Both, 0, 0);
            NotifyCollectionReplaced(new CollectionsReplacedEventArgs(oldRowHeaders, this.RowHeaders, oldColumnHeaders, this.ColumnHeaders));
        }

        /// <summary>
        /// Replaces the data in this instance with data from another table.
        /// </summary>
        /// <param name="source">The source table</param>
        public void ReplaceData(SimMultiValueBigTable source)
        {
            var rowHeaderCopies = source.RowHeaders.Select(x => new SimMultiValueBigTableHeader(x.Name, x.Unit)).ToList();
            var columnHeaderCopies = source.ColumnHeaders.Select(x => new SimMultiValueBigTableHeader(x.Name, x.Unit)).ToList();

            ReplaceData(rowHeaderCopies, columnHeaderCopies, values); //values gets copied in the ReplaceData method anyway
        }

        private void AdjustHeader(SimMultiValueBigTableHeader header, int index, SimMultiValueBigTableHeader.AxisEnum axis)
        {
            header.Index = index;
            header.Table = this;
            header.Axis = axis;
        }

        /// <summary>
        /// Resizes the value field. Fills newly created cells with the supplied value
        /// </summary>
        /// <param name="rows">New number of rows</param>
        /// <param name="columns">New number of columns</param>
        /// <param name="value">The value used for newly created cells</param>
        public void Resize(int rows, int columns, double value = 0.0)
        {
            if (rows < 1)
                throw new ArgumentOutOfRangeException("Row Count must be greater than 0");
            if (columns < 1)
                throw new ArgumentOutOfRangeException("Column Count must be greater than 0");

            int rowStartIdx = Math.Min(rows, RowHeaders.Count);
            int colStartIdx = Math.Min(columns, ColumnHeaders.Count);

            resizeHandlingEnabled = false;

            //Row Headers
            while (this.RowHeaders.Count < rows)
            {
                var newHeader = new SimMultiValueBigTableHeader(string.Empty, string.Empty);
                AdjustHeader(newHeader, this.RowHeaders.Count, SimMultiValueBigTableHeader.AxisEnum.Rows);
                this.RowHeaders.Add(newHeader);
            }
            while (this.RowHeaders.Count > rows)
                this.RowHeaders.RemoveAt(this.RowHeaders.Count - 1);

            //Column Headers
            while (this.ColumnHeaders.Count < columns)
            {
                var newHeader = new SimMultiValueBigTableHeader(string.Empty, string.Empty);
                AdjustHeader(newHeader, ColumnHeaders.Count, SimMultiValueBigTableHeader.AxisEnum.Columns);
                this.ColumnHeaders.Add(newHeader);
            }
            while (this.ColumnHeaders.Count > columns)
                this.ColumnHeaders.RemoveAt(this.ColumnHeaders.Count - 1);

            //Values Rows
            while (this.values.Count < rows)
                this.values.Add(new List<double>(Enumerable.Repeat(value, columns)));
            while (this.values.Count > rows)
                this.values.RemoveAt(this.values.Count - 1);

            //Values Columns
            foreach (var row in this.values)
            {
                while (row.Count < columns)
                    row.Add(value);
                while (row.Count > columns)
                    row.RemoveAt(row.Count - 1);
            }
            resizeHandlingEnabled = true;
            NotifyResized(ResizeDirection.Both, rowStartIdx, colStartIdx);
        }

        #endregion
    }
}
