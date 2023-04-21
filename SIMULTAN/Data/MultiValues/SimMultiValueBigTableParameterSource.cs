using SIMULTAN.Data.Components;
using System;
using System.Collections.Specialized;
using static SIMULTAN.Data.MultiValues.SimMultiValueBigTable;

namespace SIMULTAN.Data.MultiValues
{
    /// <summary>
    /// A parameter source that points to a cell in a <see cref="SimMultiValueBigTable"/>
    /// </summary>
    public sealed class SimMultiValueBigTableParameterSource : SimMultiValueParameterSource
    {
        //~SimMultiValueBigTableParameterSource() { Console.WriteLine("~MultiValueBigTablePointer"); }

        /// <summary>
        /// Returns the Table this pointer is pointing to
        /// </summary>
        public SimMultiValueBigTable Table { get; }

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
            get { return Table; }
        }

        /// <summary>
        /// Initializes a new instance of the MultiValueBigTablePointer class
        /// </summary>
        /// <param name="table">The table into which the pointer points</param>
        /// <param name="row">Row index of the target cell</param>
        /// <param name="column">Column index of the target cell</param>
        public SimMultiValueBigTableParameterSource(SimMultiValueBigTable table, int row, int column)
        {
            if (table == null)
                throw new ArgumentNullException(nameof(table));
            if (table.Factory == null) //Not in any factory
                throw new ArgumentException("Table must be part of a project");

            this.Table = table;
            this.Row = row;
            this.Column = column;

            if (this.Row >= table.RowHeaders.Count || this.Row < -1 || this.Column >= table.ColumnHeaders.Count || this.Column < -1)
                SetRowColumn(-1, -1);

            AttachEvents();

            RegisterParameter(ReservedParameters.MVBT_OFFSET_X_FORMAT, table.UnitX);
            RegisterParameter(ReservedParameters.MVBT_OFFSET_Y_FORMAT, table.UnitY);
        }


        /// <inheritdoc />
        public override object GetValue()
        {
            if (IsDisposed)
                throw new InvalidOperationException("You're trying to get the value of an unsubscribed value pointer");

            if (Row == -1 && Column == -1)
            {
                return this.GetNullVal();
            }

            var offset = GetParameterOffsets();

            if (Row + offset.rowOffset >= Table.Count(0) || Column + offset.columnOffset >= Table.Count(1))
            {
                return this.GetNullVal();
            }

            object cell = Table[Row + offset.rowOffset, Column + offset.columnOffset];

            if (cell is double d)
                return d;
            else if (cell is int i)
                return i;
            else if (cell is bool b)
                return b;
            else if (cell is string s)
                return s;
            else
            {
                return this.GetNullVal();
            }
        }

        private object GetNullVal()
        {
            switch (this.TargetParameter)
            {
                case SimDoubleParameter dparam:
                    return double.NaN;
                case SimIntegerParameter iParam:
                    return null;
                case SimStringParameter sParam:
                    return null;
                case SimBoolParameter bParam:
                    return null;
                default:
                    return null;
            }
        }
        /// <inheritdoc />
        public override SimParameterValueSource Clone()
        {
            return new SimMultiValueBigTableParameterSource(Table, Row, Column);
        }
        /// <inheritdoc />
        public override void SetFromParameters(double axisValueX, double axisValueY, double axisValueZ, string gs)
        {
            if (axisValueY >= Table.RowHeaders.Count)
                Row = Table.RowHeaders.Count - 1;
            else
                Row = (int)axisValueY;

            if (axisValueX >= Table.ColumnHeaders.Count)
                Column = Table.ColumnHeaders.Count - 1;
            else
                Column = (int)axisValueX;
        }
        /// <inheritdoc />
        public override bool IsSamePointer(SimMultiValueParameterSource other)
        {
            if (other is SimMultiValueBigTableParameterSource o)
            {
                return (Table == o.Table && Row == o.Row && Column == o.Column);
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
                Table.RowHeaders.CollectionChanged += RowHeaders_CollectionChanged;
                Table.ColumnHeaders.CollectionChanged += ColumnHeaders_CollectionChanged;
                Table.ValueChanged += Table_ValueChanged;
                Table.CollectionReplaced += Table_ContentReplaced;
                Table.Deleting += this.Table_Deleting;

                isAttached = true;
            }
        }

        private void Table_Deleting(object sender, EventArgs e)
        {
            if (this.TargetParameter != null)
            {
                this.TargetParameter.ValueSource = null;
            }
        }

        internal override void DetachEvents()
        {
            base.DetachEvents();

            if (isAttached)
            {
                isAttached = false;
                Table.RowHeaders.CollectionChanged -= RowHeaders_CollectionChanged;
                Table.ColumnHeaders.CollectionChanged -= ColumnHeaders_CollectionChanged;
                Table.ValueChanged -= Table_ValueChanged;
                Table.CollectionReplaced -= Table_ContentReplaced;
                Table.Deleting -= this.Table_Deleting;
            }
        }

        private (int rowOffset, int columnOffset) GetParameterOffsets()
        {
            int paramAddX = 0, paramAddY = 0;
            var paramX = GetValuePointerParameter(ReservedParameters.MVBT_OFFSET_X_FORMAT);
            if (paramX != null && paramX is SimDoubleParameter dX)
            {
                paramAddX = (int)dX.Value;
            }


            var paramY = GetValuePointerParameter(ReservedParameters.MVBT_OFFSET_Y_FORMAT);
            if (paramY != null && paramY is SimDoubleParameter dY)
            {
                paramAddY = (int)dY.Value;
            }


            return (paramAddY, paramAddX);
        }
    }
}
