using SIMULTAN.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using SIMULTAN.Data.SimMath;

namespace SIMULTAN.Data.MultiValues
{
    /// <summary>
    /// Stores a 3 (or less) dimensional field of values that can be interpolated.
    /// Axes are always sorted ascending.
    /// </summary>
    public class SimMultiValueField3D : SimMultiValue
    {
        #region Helper Classes & Enums
        /// <summary>
        /// Enumeration to address axis
        /// </summary>
        public enum Axis
        {
            /// <summary>
            /// The X-Axis
            /// </summary>
            X = 0,
            /// <summary>
            /// The Y-Axis
            /// </summary>
            Y = 1,
            /// <summary>
            /// The Z-Axis
            /// </summary>
            Z = 2
        }

        /// <summary>
        /// Stores a sorted list of axis entries in a SimMultiValueField3D.
        /// Automatically resorts the axis and the related table data when changes are made
        /// </summary>
        public class AxisCollection : ObservableCollection<double>
        {
            private Axis axis;
            private SimMultiValueField3D owner;

            /// <summary>
            /// Initializes a new instance of the AxisCollection class
            /// </summary>
            /// <param name="owner">The table this collection belongs to</param>
            /// <param name="axis">The axis which is described by this collection</param>
            /// <param name="values">The initial values of the collection (have to be sorted). When empty, a 0 entry is added</param>
            internal AxisCollection(SimMultiValueField3D owner, Axis axis, IEnumerable<double> values) : base(values.OrderBy(x => x))
            {
                this.owner = owner;
                this.axis = axis;

                //Fill data with 0
                if (this.Count == 0)
                    this.Add(0.0);
            }

            /// <inheritdoc />
            protected override void InsertItem(int index, double item)
            {
                //Find actual insert location (based on ordering)
                int insertIndex = 0;
                while (insertIndex < this.Count && this[insertIndex] < item)
                    insertIndex++;

                //Shift data >= insertIndex
                Dictionary<IntIndex3D, double> newData = new Dictionary<IntIndex3D, double>();
                foreach (var entry in owner.field)
                {
                    var key = entry.Key;
                    if (key[(int)axis] >= insertIndex)
                        key[(int)axis]++;
                    newData[key] = entry.Value;
                }

                InsertZeros(insertIndex, newData);

                owner.field = newData;

                base.InsertItem(insertIndex, item);
                owner.NotifyAxisChanged();
                owner.NotifyChanged();
            }
            /// <inheritdoc />
            protected override void RemoveItem(int index)
            {
                //Remove data entries
                Dictionary<IntIndex3D, double> newData = new Dictionary<IntIndex3D, double>();

                bool isLastRemoved = this.Count == 1;

                if (isLastRemoved)
                {
                    //Make sure that at least one element exists
                    InsertZeros(0, newData);
                }
                else
                {
                    foreach (var entry in owner.field)
                    {
                        var key = entry.Key;
                        if (key[(int)axis] < index)
                        {
                            newData[key] = entry.Value;
                        }
                        else if (key[(int)axis] > index)
                        {
                            key[(int)axis]--;
                            newData[key] = entry.Value;
                        }
                    }
                }

                owner.field = newData;

                if (isLastRemoved)
                    base.SetItem(0, 0.0);
                else
                    base.RemoveItem(index);

                owner.NotifyAxisChanged();
                owner.NotifyChanged();
            }
            /// <inheritdoc />
            protected override void ClearItems()
            {
                owner.field.Clear();
                base.ClearItems();

                this.Add(0.0);

                //No notify needed since add notifies anyway
            }
            /// <inheritdoc />
            protected override void MoveItem(int oldIndex, int newIndex)
            {
                throw new NotSupportedException("This collection is ordered, moving items isn't supported");
            }
            /// <inheritdoc />
            protected override void SetItem(int index, double item)
            {
                //Find actual now position according to sorting
                bool resortRequired = false;
                if (index > 0 && this[index - 1] > item ||
                    index < this.Count - 1 && this[index + 1] < item)
                    resortRequired = true;

                if (resortRequired)
                {
                    base.RemoveItem(index);

                    //Find insert index
                    int insertIndex = 0;
                    while (insertIndex < this.Count && this[insertIndex] < item)
                        insertIndex++;

                    base.InsertItem(insertIndex, item);

                    //Move data
                    Dictionary<IntIndex3D, double> newData = new Dictionary<IntIndex3D, double>();

                    foreach (var entry in owner.field)
                    {
                        var key = entry.Key;
                        if (key[(int)axis] < index && key[(int)axis] < insertIndex)
                            newData[key] = entry.Value;
                        else if (key[(int)axis] > index && key[(int)axis] > insertIndex)
                            newData[key] = entry.Value;
                        else if (key[(int)axis] == index)
                        {
                            //Moving item
                            key[(int)axis] = insertIndex;
                            newData[key] = entry.Value;
                        }
                        else
                        {
                            //Item between move locations, adjust by one
                            if (insertIndex < index)
                                key[(int)axis]++;
                            else
                                key[(int)axis]--;

                            newData[key] = entry.Value;
                        }
                    }

                    owner.field = newData;
                }
                else
                    base.SetItem(index, item);

                owner.NotifyAxisChanged();
                owner.NotifyChanged();
            }

            private void InsertZeros(int insertIndex, Dictionary<IntIndex3D, double> data)
            {
                //insert 0 for inserted axis index
                int[] min = new int[] { 0, 0, 0 };
                int[] max = new int[] { owner.Count(Axis.X), owner.Count(Axis.Y), owner.Count(Axis.Z) };

                min[(int)axis] = insertIndex;
                max[(int)axis] = insertIndex + 1;

                for (int x = min[0]; x < max[0]; x++)
                {
                    for (int y = min[1]; y < max[1]; y++)
                    {
                        for (int z = min[2]; z < max[2]; z++)
                        {
                            data[new IntIndex3D(x, y, z)] = 0.0;
                        }
                    }
                }
            }
        }

        #endregion


        #region Properties & Members

        /// <summary>
        /// Returns the data entries in the field. Index is the index along the axis, the value is the value at that index.
        /// </summary>
        public IEnumerable<KeyValuePair<IntIndex3D, double>> Field => field;
        private Dictionary<IntIndex3D, double> field; //x => index in XAxis, y => index in YAxis, z => index ZAxis

        /// <summary>
        /// Returns the number of elements along an axis
        /// </summary>
        /// <param name="dimension">0 means X, 1 means Y, 2 means Z</param>
        /// <returns>The number of entries along the selected axis</returns>
        public int Count(int dimension)
        {
            if (dimension < 0 || dimension > 2)
                throw new ArgumentOutOfRangeException("dimension must be positive and smaller than 3");

            switch (dimension)
            {
                case 0:
                    return XAxis.Count;
                case 1:
                    return YAxis.Count;
                case 2:
                    return ZAxis.Count;
                default: //Never called, would throw an exception in the first lines
                    return -1;
            }
        }
        /// <summary>
        /// Returns the number of elements along an axis
        /// </summary>
        /// <param name="axis">The axis for which the count should be returned</param>
        /// <returns>The number of entries along the selected axis</returns>
        public int Count(Axis axis)
        {
            return Count((int)axis);
        }

        /// <summary>
        /// Returns the total number of values stored in the ValueField
        /// </summary>
        public int Length
        {
            get { return field.Count; }
        }

        /// <inheritdoc />
        public override SimMultiValueType MVType => SimMultiValueType.Field3D;

        /// <summary>
        /// Stores the axis entries along the X-Axis
        /// </summary>
        public AxisCollection XAxis { get; }

        /// <summary>
        /// Stores the axis entries along the Y-Axis
        /// </summary>
        public AxisCollection YAxis { get; }

        /// <summary>
        /// Stores the axis entries along the Z-Axis
        /// </summary>
        public AxisCollection ZAxis { get; }

        /// <inheritdoc />
        public override bool CanInterpolate
        {
            get => canInterpolate;
            set
            {
                var old_value = this.canInterpolate;
                this.canInterpolate = value;
                this.NotifyPropertyChanged(nameof(CanInterpolate));
                NotifyChanged();
            }
        }
        private bool canInterpolate;

        #endregion

        #region Events

        /// <summary>
        /// Event args for the ValueChanged event
        /// </summary>
        public class ValueChangedEventArgs : EventArgs
        {
            /// <summary>
            /// The range (value) in which the values are invalidated
            /// </summary>
            public Range3D Range { get; private set; }
            /// <summary>
            /// Initializes a new instance of the ValueChangedEventArgs class
            /// </summary>
            /// <param name="range">The invalidated range</param>
            public ValueChangedEventArgs(Range3D range)
            {
                this.Range = range;
            }
        }
        /// <summary>
        /// EventHandler for the ValueChanged event
        /// </summary>
        /// <param name="sender">The object calling this event</param>
        /// <param name="args">The event args</param>
        public delegate void ValueChangedEventHandler(object sender, ValueChangedEventArgs args);
        /// <summary>
        /// Invoked when the values in the table have changed. The invalidated range is sent in the EventArgs
        /// </summary>
        public event ValueChangedEventHandler ValueChanged;
        /// <summary>
        /// Invokes the ValueChanged event
        /// </summary>
        /// <param name="range"></param>
        protected void NotifyValueChanged(Range3D range)
        {
            this.ValueChanged?.Invoke(this, new ValueChangedEventArgs(range));
        }


        /// <summary>
        /// Invoked when one of the axis has changed (either by adding/removing elements or when resorting)
        /// </summary>
        public event EventHandler AxisChanged;
        /// <summary>
        /// Invokes the AxisChanged event
        /// </summary>
        protected void NotifyAxisChanged()
        {
            this.AxisChanged?.Invoke(this, EventArgs.Empty);
        }

        #endregion


        #region .CTOR

        /// <summary>
        /// Initializes a new instance of the SimMultiValueField3D class
        /// </summary>
        /// <param name="xaxis">Entries on the X-axis</param>
        /// <param name="unitX">Unit of the X-axis</param>
        /// <param name="yaxis">Entries on the Y-axis</param>
        /// <param name="unitY">Unit of the Y-axis</param>
        /// <param name="zaxis">Entries on the Z-axis</param>
        /// <param name="unitZ">Unit of the Z-axis</param>
        /// <param name="data">Data in this value field. Indices into this array are calculated as z * pageCount * rowCount + y * rowCount + x</param>
        /// <param name="canInterpolate">True: Linear interpolation is used, False: Nearest neighbor interpolation</param>
        /// <param name="name">Name of the value field</param>
        public SimMultiValueField3D(string name, IEnumerable<double> xaxis, string unitX, IEnumerable<double> yaxis, string unitY, IEnumerable<double> zaxis, string unitZ,
                                 IEnumerable<double> data, bool canInterpolate)
            : base(name, unitX, unitY, unitZ)
        {
            if (data == null)
                throw new ArgumentNullException(string.Format("{0} may not be null", nameof(data)));

            this.canInterpolate = canInterpolate;

            var sortedX = CreateSortedAxis(xaxis, Axis.X);
            var sortedY = CreateSortedAxis(yaxis, Axis.Y);
            var sortedZ = CreateSortedAxis(zaxis, Axis.Z);

            this.XAxis = sortedX.axis;
            this.YAxis = sortedY.axis;
            this.ZAxis = sortedZ.axis;

            List<double> dataList = data.ToList();
            if (dataList.Count() < this.XAxis.Count * this.YAxis.Count * this.ZAxis.Count)
                throw new ArgumentException("data has to contain at least xaxis.Count() * yaxis.Count() * zaxis.Count() elements");

            this.field = new Dictionary<IntIndex3D, double>();
            for (int z = 0; z < this.ZAxis.Count; z++)
            {
                for (int y = 0; y < this.YAxis.Count; y++)
                {
                    for (int x = 0; x < this.XAxis.Count; x++)
                    {
                        var key = new IntIndex3D(sortedX.reordering[x], sortedY.reordering[y], sortedZ.reordering[z]);
                        this.field.Add(key, dataList[z * this.YAxis.Count * this.XAxis.Count + y * this.XAxis.Count + x]);
                    }
                }
            }
        }

        #endregion

        #region .CTOR for PARSING

        /// <summary>
        /// Initializes a new instance of the SimMultiValueField3D class
        /// </summary>
        /// <param name="id">Id of the table (used for loading from files)</param>
        /// <param name="xaxis">Entries on the X-axis</param>
        /// <param name="unitX">Unit of the X-axis</param>
        /// <param name="yaxis">Entries on the Y-axis</param>
        /// <param name="unitY">Unit of the Y-axis</param>
        /// <param name="zaxis">Entries on the Z-axis</param>
        /// <param name="unitZ">Unit of the Z-axis</param>
        /// <param name="data">Data in this value field</param>
        /// <param name="canInterpolate">True: Linear interpolation is used, False: Nearest neighbor interpolation</param>
        /// <param name="name">Name of the value field</param>
        public SimMultiValueField3D(long id, string name,
                                 IEnumerable<double> xaxis, string unitX, IEnumerable<double> yaxis, string unitY, IEnumerable<double> zaxis, string unitZ,
                                 IDictionary<SimPoint3D, double> data, bool canInterpolate)
            : base(id, name, unitX, unitY, unitZ)
        {
            if (data == null)
                throw new ArgumentNullException("data may not be null");

            this.canInterpolate = canInterpolate;

            if (xaxis != null && xaxis.Count() > 0)
                this.XAxis = new AxisCollection(this, Axis.X, xaxis);
            else
                this.XAxis = new AxisCollection(this, Axis.X, new double[] { 0 });

            if (yaxis != null && yaxis.Count() > 0)
                this.YAxis = new AxisCollection(this, Axis.Y, yaxis);
            else
                this.YAxis = new AxisCollection(this, Axis.Y, new double[] { 0 });

            if (zaxis != null && zaxis.Count() > 0)
                this.ZAxis = new AxisCollection(this, Axis.Z, zaxis);
            else
                this.ZAxis = new AxisCollection(this, Axis.Z, new double[] { 0 });

            this.field = new Dictionary<IntIndex3D, double>();

            foreach (var entry in data)
            {
                if (entry.Key.X >= 0 || entry.Key.Y >= 0 || entry.Key.Z >= 0 || entry.Key.X < Count(Axis.X) || entry.Key.Y < Count(Axis.Y) || entry.Key.Z < Count(Axis.Z))
                    this.field.Add(new IntIndex3D((int)entry.Key.X, (int)entry.Key.Y, (int)entry.Key.Z), entry.Value);
            }

            FillWithZeros(field);
        }

        #endregion

        #region .CTOR for COPYING

        /// <summary>
        /// Initializes a new instance of the SimMultiValueField3D class with values from another SimMultiValueField3D
        /// </summary>
        /// <param name="original">The original data</param>
        protected SimMultiValueField3D(SimMultiValueField3D original)
            : base(original)
        {
            if (original == null)
                throw new ArgumentNullException(string.Format("{0} may not be null", nameof(original)));

            this.XAxis = new AxisCollection(this, Axis.X, original.XAxis);
            this.YAxis = new AxisCollection(this, Axis.Y, original.YAxis);
            this.ZAxis = new AxisCollection(this, Axis.Z, original.ZAxis);

            this.field = new Dictionary<IntIndex3D, double>(original.field);
            this.canInterpolate = original.canInterpolate;
        }

        /// <summary>
        /// Creates a copy of the ValueField
        /// </summary>
        /// <returns>A new value field with the same data</returns>
        public override SimMultiValue Clone()
        {
            return new SimMultiValueField3D(this);
        }
        #endregion

        #region External Pointer

        /// <summary>
        /// Returns an default pointer for this ValueField
        /// </summary>
        public SimMultiValueParameterSource DefaultPointer
        {
            get { return new SimMultiValueField3DParameterSource(this, this.XAxis[0], this.YAxis[0], this.ZAxis[0]); }
        }

        /// <inheritdoc />
        public override SimMultiValueParameterSource CreateNewPointer()
        {
            return DefaultPointer;
        }
        /// <inheritdoc />
        public override SimMultiValueParameterSource CreateNewPointer(SimMultiValueParameterSource source)
        {
            if (source is SimMultiValueField3DParameterSource ptr)
            {
                return new SimMultiValueField3DParameterSource(this, ptr.AxisValueX, ptr.AxisValueY, ptr.AxisValueZ);
            }
            return DefaultPointer;
        }

        #endregion

        #region Lookup

        /// <summary>
        /// Queries one of the defined values at a specific index. This method does not support interpolation.
        /// The parameters are INDICES, not axis values
        /// </summary>
        /// <param name="xIndex">The index on the X-axis</param>
        /// <param name="yIndex">The index on the Y-axis</param>
        /// <param name="zIndex">The index on the Z-axis</param>
        /// <returns>The (non-interpolated) value of the field at the given index</returns>
        public double this[int xIndex, int yIndex, int zIndex]
        {
            get
            {
                return this[new IntIndex3D(xIndex, yIndex, zIndex)];
            }
            set
            {
                this[new IntIndex3D(xIndex, yIndex, zIndex)] = value;
            }
        }

        /// <summary>
        /// Queries one of the defined values at a specific index. This method does not support interpolation.
        /// The parameters are INDICES, not axis values
        /// </summary>
        /// <param name="index">The index in the field</param>
        /// <returns>The (non-interpolated) value of the field at the given index</returns>
        public double this[IntIndex3D index]
        {
            get
            {
                if (!IsInRange(index))
                    throw new ArgumentOutOfRangeException("Index has to be between 0 and Count(axis)");

                if (this.field.TryGetValue(index, out var result))
                    return result;

                return 0;
            }
            set
            {
                if (!IsInRange(index))
                    throw new ArgumentOutOfRangeException("Index has to be between 0 and Count(axis)");

                field[index] = value;

                //Notify which range has changed
                var xminus = AxisRangeValueFromPosition(Axis.X, index.X - 1, double.NegativeInfinity);
                var xplus = AxisRangeValueFromPosition(Axis.X, index.X + 1, double.PositiveInfinity);

                var yminus = AxisRangeValueFromPosition(Axis.Y, index.Y - 1, double.NegativeInfinity);
                var yplus = AxisRangeValueFromPosition(Axis.Y, index.Y + 1, double.PositiveInfinity);

                var zminus = AxisRangeValueFromPosition(Axis.Z, index.Z - 1, double.NegativeInfinity);
                var zplus = AxisRangeValueFromPosition(Axis.Z, index.Z + 1, double.PositiveInfinity);

                NotifyValueChanged(new Range3D(new SimPoint3D(xminus, yminus, zminus), new SimPoint3D(xplus, yplus, zplus)));
                this.NotifyChanged();
            }
        }

        private bool IsInRange(IntIndex3D index)
        {
            return index.X >= 0 && index.Y >= 0 && index.Z >= 0 &&
                index.X < XAxis.Count && index.Y < YAxis.Count && index.Z < ZAxis.Count;
        }

        /// <summary>
        /// Returns the value of this field at a specific location. Data is interpolated according to CanInterpolate
        /// </summary>
        /// <param name="position">Position in the value field. position is clamped to the value field borders.</param>
        /// <returns>The value at position in the value field</returns>
        public double GetValue(SimPoint3D position)
        {
            (double x, int xint, double fractX) = ClampAxis(position.X, this.XAxis);
            (double y, int yint, double fractY) = ClampAxis(position.Y, this.YAxis);
            (double z, int zint, double fractZ) = ClampAxis(position.Z, this.ZAxis);

            if (this.CanInterpolate)
            {
                //Interpolate
                double topLeft = GetValue(xint, yint, zint);
                double topRight = GetValue(xint + 1, yint, zint);
                double bottomLeft = GetValue(xint, yint + 1, zint);
                double bottomRight = GetValue(xint + 1, yint + 1, zint);

                double topLeftPlus = GetValue(xint, yint, zint + 1);
                double topRightPlus = GetValue(xint + 1, yint, zint + 1);
                double bottomLeftPlus = GetValue(xint, yint + 1, zint + 1);
                double bottomRightPlus = GetValue(xint + 1, yint + 1, zint + 1);



                //X Interpolation
                double top = topLeft * (1.0 - fractX) + topRight * fractX;
                double bottom = bottomLeft * (1.0 - fractX) + bottomRight * fractX;
                double topPlus = topLeftPlus * (1.0 - fractX) + topRightPlus * fractX;
                double bottomPlus = bottomLeftPlus * (1.0 - fractX) + bottomRightPlus * fractX;

                //Y Interpolation
                double front = top * (1.0 - fractY) + bottom * fractY;
                double back = topPlus * (1.0 - fractY) + bottomPlus * fractY;

                //Z Interpolation
                double value = front * (1.0 - fractZ) + back * fractZ;

                return value;
            }
            else
            {
                if (fractX >= 0.5)
                    xint++;
                if (fractY >= 0.5)
                    yint++;
                if (fractZ >= 0.5)
                    zint++;

                return GetValue(xint, yint, zint);
            }
        }

        /// <summary>
        /// Returns the value at the index but clamps to the border.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <returns></returns>
        private double GetValue(int x, int y, int z)
        {
            return field[new IntIndex3D(ClampAxis(x, XAxis), ClampAxis(y, YAxis), ClampAxis(z, ZAxis))];
        }
        private int ClampAxis(int value, IList<double> axis)
        {
            if (axis == null)
                return 0;
            return Math.Max(0, Math.Min(value, axis.Count - 1));
        }
        private (double val, int valint, double fract) ClampAxis(double value, IList<double> axis)
        {
            if (axis == null)
                return (0, 0, 0);

            var clamped = Math.Max(0, Math.Min(value, axis.Count - 1));
            int iclamped = (int)clamped;

            double fract = clamped - iclamped;

            if (fract < 0.001)
            {
                clamped = iclamped;
                fract = 0;
            }
            else if (fract > 0.999)
            {
                clamped = iclamped + 1;
                fract = 0;
            }

            return (clamped, iclamped, fract);
        }

        /// <summary>
        /// Returns the index along an axis based on the value on this axis.
        /// When value lies between two values on the axis, the result will be the linearly interpolated index
        /// </summary>
        /// <param name="axis">The axis</param>
        /// <param name="value">Value on that axis</param>
        /// <returns>The interpolated index into the axis array, clamped to [0, Count-1]</returns>
        public double AxisPositionFromValue(Axis axis, double value)
        {
            var axisList = GetAxis(axis);
            int idx = 0;
            while (idx < axisList.Count && axisList[idx] < value)
                idx++;


            if (idx < axisList.Count && idx > 0) //Somewhere in the middle
            {
                var valBefore = axisList[idx - 1];
                var valAfter = axisList[idx];

                var positionInRange = (value - valBefore) / (valAfter - valBefore);
                return (idx - 1) + positionInRange;
            }
            else if (idx == 0) //Smaller than smallest value
                return 0;
            else //Larger than largest value
                return axisList.Count - 1;
        }

        /// <summary>
        /// Returns the value of an axis at a specific index.
        /// If axisPoisition is not a full number, the result will be a linear interpolation of the surrounding values.
        /// Returns either the first or the last element when the index is outside of the allowed range.
        /// </summary>
        /// <param name="axis">The axis</param>
        /// <param name="axisPosition">The position along that axis</param>
        /// <returns>The value on the axis</returns>
        public double ValueFromAxisPosition(Axis axis, double axisPosition)
        {
            return ValueFromAxisPosition(axis, axisPosition, out var _);
        }
        /// <summary>
        /// Returns the value of an axis at a specific index.
        /// If axisPoisition is not a full number, the result will be a linear interpolation of the surrounding values.
        /// Returns either the first or the last element when the index is outside of the allowed range.
        /// </summary>
        /// <param name="axis">The axis</param>
        /// <param name="axisPosition">The position along that axis</param>
        /// <param name="isOutside">Returns true when the index was outside of the valid range [0, Count-1], otherwise False</param>
        /// <returns>The value on the axis</returns>
        public double ValueFromAxisPosition(Axis axis, double axisPosition, out bool isOutside)
        {
            int idx = (int)axisPosition;
            var axisList = GetAxis(axis);
            isOutside = true;

            if (idx < 0)
                return axisList[0];
            if (idx >= axisList.Count - 1)
                return axisList[axisList.Count - 1];

            isOutside = false;
            double valueBefore = axisList[idx];
            double valueAfter = axisList[idx + 1];
            double interp = axisPosition - idx;

            return valueBefore * (1.0 - interp) + valueAfter * interp;
        }

        #endregion


        #region Data Integrety

        private double AxisRangeValueFromPosition(Axis axis, double position, double outsideValue)
        {
            var result = ValueFromAxisPosition(axis, position, out var isOutside);
            if (isOutside)
                return outsideValue;
            return result;
        }


        private void FillWithZeros(Dictionary<IntIndex3D, double> data)
        {
            for (int x = 0; x < XAxis.Count; ++x)
            {
                for (int y = 0; y < YAxis.Count; ++y)
                {
                    for (int z = 0; z < ZAxis.Count; ++z)
                    {
                        if (!data.ContainsKey(new IntIndex3D(x, y, z)))
                            data.Add(new IntIndex3D(x, y, z), 0.0);
                    }
                }
            }
        }

        private (AxisCollection axis, Dictionary<int, int> reordering) CreateSortedAxis(IEnumerable<double> axisData, Axis axis)
        {
            if (axisData == null || !axisData.Any())
                return (new AxisCollection(this, axis, new double[] { 0.0 }), new Dictionary<int, int> { { 0, 0 } });

            var sorted = axisData.Select((x, xi) => (x, xi)).OrderBy(x => x.x).ToList();
            Dictionary<int, int> reordering = new Dictionary<int, int>();

            for (int i = 0; i < sorted.Count; ++i)
                reordering.Add(sorted[i].xi, i);

            return (new AxisCollection(this, axis, sorted.Select(x => x.x)), reordering);
        }

        /// <summary>
        /// Returns the values along an axis (XAxis, YAxis, ZAxis)
        /// </summary>
        /// <param name="axis">The axis for which the values are returned</param>
        /// <returns>Depending on axis, one of (XAxis, YAxis, ZAxis)</returns>
        public ObservableCollection<double> GetAxis(Axis axis)
        {
            switch (axis)
            {
                case Axis.X:
                    return XAxis;
                case Axis.Y:
                    return YAxis;
                case Axis.Z:
                    return ZAxis;
            }

            return null;
        }

        #endregion
    }
}
