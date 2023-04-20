using SIMULTAN.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Media.Media3D;

namespace SIMULTAN.Data.MultiValues
{
    /// <summary>
    /// A SimMultiValue field based on graphs. Only provides values on graphs.
    /// X and Y axis are given as a ranges, Z axis stores discrete values.
    /// A graph is always located at a single z-value.
    /// Supports interpolation along the function graphs, but only along X and Y.
    /// </summary>
    public class SimMultiValueFunction : SimMultiValue
    {
        #region Collections

        /// <summary>
        /// Collection for storing graphs inside a SimMultiValueFunction. Makes sure that the Function is always set correctly
        /// </summary>
        public class GraphsCollection : ObservableCollection<SimMultiValueFunctionGraph>
        {
            private SimMultiValueFunction owner;

            /// <summary>
            /// Initializes a new instance of the GraphsCollection class
            /// </summary>
            /// <param name="owner">The SimMultiValueFunction which owns this collection</param>
            /// <param name="graphs">A list of initial graphs in the SimMultiValueFunction</param>
            public GraphsCollection(SimMultiValueFunction owner, IEnumerable<SimMultiValueFunctionGraph> graphs) : base(graphs)
            {
                this.owner = owner;

                foreach (var item in this)
                {
                    item.Function = owner;
                    item.ClampToValidRange();
                }
            }

            /// <inheritdoc />
            protected override void InsertItem(int index, SimMultiValueFunctionGraph item)
            {
                item.Function = owner;
                item.ClampToValidRange();

                base.InsertItem(index, item);
                owner.NotifyChanged();
            }
            /// <inheritdoc />
            protected override void RemoveItem(int index)
            {
                var item = this[index];
                item.Function = null;

                base.RemoveItem(index);
                owner.NotifyChanged();
            }
            /// <inheritdoc />
            protected override void ClearItems()
            {
                foreach (var item in this)
                    item.Function = null;

                base.ClearItems();

                owner.NotifyChanged();
            }
            /// <inheritdoc />
            protected override void SetItem(int index, SimMultiValueFunctionGraph item)
            {
                throw new NotSupportedException("Operation not supported");
            }
        };

        /// <summary>
        /// Stores an ordered collection of entries along the Z-Axis. 
        /// Automatically restorts items on changes and also moves/removes graphs when necessary.
        /// </summary>
        public class ZAxisCollection : ObservableCollection<double>
        {
            private SimMultiValueFunction owner;

            /// <summary>
            /// Initializes a new instance of the ZAxisCollection class
            /// </summary>
            /// <param name="owner">The function this collection belongs to</param>
            /// <param name="initialValues">A set of initial (unsorted) values</param>
            public ZAxisCollection(SimMultiValueFunction owner, IEnumerable<double> initialValues) : base(initialValues.OrderBy(x => x))
            {
                this.owner = owner;
            }

            /// <inheritdoc />
            protected override void InsertItem(int index, double item)
            {
                //Find actual insert location (based on ordering)
                int insertIndex = 0;
                while (insertIndex < this.Count && this[insertIndex] < item)
                    insertIndex++;

                base.InsertItem(insertIndex, item);

                owner.NotifyChanged();

                owner.Range = new Range3D(
                    new Point3D(owner.Range.Minimum.X, owner.Range.Minimum.Y, this.Min()),
                    new Point3D(owner.Range.Maximum.X, owner.Range.Maximum.Y, this.Max())
                    );
            }
            /// <inheritdoc />
            protected override void RemoveItem(int index)
            {
                var removeZ = this[index];

                //Remove graphs
                for (int i = 0; i < owner.Graphs.Count; ++i)
                {
                    if (Math.Abs(removeZ - owner.Graphs[i].Points[0].Z) < 0.0001)
                    {
                        owner.Graphs.RemoveAt(i);
                        i--;
                    }
                }

                bool isLastRemoved = this.Count == 1;
                if (isLastRemoved)
                    base.SetItem(0, 0);
                else
                    base.RemoveItem(index);

                owner.Range = new Range3D(
                    new Point3D(owner.Range.Minimum.X, owner.Range.Minimum.Y, this.Min()),
                    new Point3D(owner.Range.Maximum.X, owner.Range.Maximum.Y, this.Max())
                    );
            }
            /// <inheritdoc />
            protected override void MoveItem(int oldIndex, int newIndex)
            {
                throw new NotSupportedException("This collection is ordered, moving items isn't supported");
            }
            /// <inheritdoc />
            protected override void SetItem(int index, double item)
            {
                var oldZ = this[index];

                //Move graphs to new  Z
                foreach (var g in owner.Graphs.Where(x => Math.Abs(oldZ - x.Points[0].Z) < 0.0001))
                {
                    for (int i = 0; i < g.Points.Count; ++i)
                    {
                        g.Points[i] = new Point3D(g.Points[i].X, g.Points[i].Y, item);
                    }
                }

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
                }
                else
                    base.SetItem(index, item);

                owner.Range = new Range3D(
                    new Point3D(owner.Range.Minimum.X, owner.Range.Minimum.Y, this.Min()),
                    new Point3D(owner.Range.Maximum.X, owner.Range.Maximum.Y, this.Max())
                    );
            }
            /// <inheritdoc />
            protected override void ClearItems()
            {
                owner.Graphs.Clear();
                base.ClearItems();

                this.Add(0.0);

                owner.Range = new Range3D(
                    new Point3D(owner.Range.Minimum.X, owner.Range.Minimum.Y, 0),
                    new Point3D(owner.Range.Maximum.X, owner.Range.Maximum.Y, 0)
                    );
            }
        }

        #endregion


        #region Properties

        /// <summary>
        /// Stores the definition space of the function field. Only X and Y may be modified by the user.
        /// Z values are derived from the ZAxis collection
        /// </summary>
        public Range3D Range
        {
            get { return range; }
            set
            {
                if (range != value)
                {
                    range = new Range3D(
                        new Point3D(value.Minimum.X, value.Minimum.Y, ZAxis.Min()),
                        new Point3D(value.Maximum.X, value.Maximum.Y, ZAxis.Max())
                        );
                    ClampPointsToValidXYRange();
                    NotifyPropertyChanged(nameof(Range));
                    this.NotifyChanged();
                }
            }
        }
        private Range3D range;

        /// <inheritdoc />
        public override bool CanInterpolate
        {
            get => true;
            set { }
        }

        /// <inheritdoc />
        public override SimMultiValueType MVType => SimMultiValueType.Function;

        /// <summary>
        /// Stores a list of all graphs in this ValueField
        /// </summary>
        public GraphsCollection Graphs { get; }

        /// <summary>
        /// Stores the values along the Z-axis
        /// </summary>
        public ZAxisCollection ZAxis { get; }

        #endregion


        /// <summary>
        /// Initializes a new instance of the SimMultiValueFunction class
        /// </summary>
        /// <param name="name">Name of the SimMultiValue</param>
        /// <param name="unitX">Unit description of the X-axis. Just a text, does not influence any calculations.</param>
        /// <param name="unitY">Unit description of the Y-axis. Just a text, does not influence any calculations.</param>
        /// <param name="unitZ">Unit description of the Z-axis. Just a text, does not influence any calculations.</param>
        /// <param name="bounds">Bounds of the definition region along X and Y.</param>
        /// <param name="zaxis">Discrete values along the Z axis</param>
        /// <param name="graphs">List of all graphs in the field</param>
        public SimMultiValueFunction(string name,
                                    string unitX, string unitY, string unitZ, Rect bounds,
                                    IEnumerable<double> zaxis, IEnumerable<SimMultiValueFunctionGraph> graphs)
            : base(name, unitX, unitY, unitZ)
        {
            if (name == null)
                throw new ArgumentNullException(string.Format("{0} may not be null", nameof(name)));
            if (zaxis == null)
                throw new ArgumentNullException(string.Format("{0} may not be null", nameof(zaxis)));
            if (graphs == null)
                throw new ArgumentNullException(string.Format("{0} may not be null", nameof(graphs)));

            this.ZAxis = new ZAxisCollection(this, zaxis);
            this.Range = new Range3D(new Point3D(bounds.Left, bounds.Top, this.ZAxis.Min()), new Point3D(bounds.Right, bounds.Bottom, this.ZAxis.Max()));

            this.Graphs = new GraphsCollection(this, graphs);
        }

        /// <summary>
        /// Initializes a new instance of the SimMultiValueFunction class
        /// </summary>
        /// <param name="id"></param>
        /// <param name="name">Name of the SimMultiValue</param>
        /// <param name="unitX">Unit description of the X-axis. Just a text, does not influence any calculations.</param>
        /// <param name="unitY">Unit description of the Y-axis. Just a text, does not influence any calculations.</param>
        /// <param name="unitZ">Unit description of the Z-axis. Just a text, does not influence any calculations.</param>
        /// <param name="bounds">Bounds of the definition region along X and Y</param>
        /// <param name="zaxis">Discrete values along the Z axis</param>
        /// <param name="graphs">List of all graphs in the field</param>
        public SimMultiValueFunction(long id, string name,
                                    string unitX, string unitY, string unitZ, Rect bounds,
                                    IEnumerable<double> zaxis, IEnumerable<SimMultiValueFunctionGraph> graphs)
            : base(id, name, unitX, unitY, unitZ)
        {
            if (name == null)
                throw new ArgumentNullException(string.Format("{0} may not be null", nameof(name)));
            if (zaxis == null)
                throw new ArgumentNullException(string.Format("{0} may not be null", nameof(zaxis)));
            if (graphs == null)
                throw new ArgumentNullException(string.Format("{0} may not be null", nameof(graphs)));

            this.ZAxis = new ZAxisCollection(this, zaxis);
            this.Range = new Range3D(new Point3D(bounds.Left, bounds.Top, this.ZAxis.Min()), new Point3D(bounds.Right, bounds.Bottom, this.ZAxis.Max()));

            this.Graphs = new GraphsCollection(this, graphs);
        }

        /// <summary>
        /// Creates a deep copy of a SimMultiValueFunction
        /// </summary>
        /// <param name="original">The SimMultiValueFunction from which the data is copied</param>
        protected SimMultiValueFunction(SimMultiValueFunction original)
            : base(original)
        {
            if (original == null)
                throw new ArgumentNullException(string.Format("{0} may not be null", nameof(original)));

            this.ZAxis = new ZAxisCollection(this, original.ZAxis);
            this.Range = new Range3D(original.Range);
            this.Graphs = new GraphsCollection(this, original.Graphs.Select(x => x.Clone()));
        }

        /// <inheritdoc />
        public override SimMultiValue Clone()
        {
            return new SimMultiValueFunction(this);
        }

        #region METHODS: External Pointer

        /// <summary>
        /// Creates a pointer pointing to a default location (no graph, NaN coordinates)
        /// </summary>
        public SimMultiValueParameterSource DefaultPointer
        {
            get
            {
                return new SimMultiValueFunctionParameterSource(this, null, double.NaN, double.NaN);
            }
        }
        /// <inheritdoc />
        public override SimMultiValueParameterSource CreateNewPointer()
        {
            return DefaultPointer;
        }
        /// <inheritdoc />
        public override SimMultiValueParameterSource CreateNewPointer(SimMultiValueParameterSource source)
        {
            if (source is SimMultiValueFunctionParameterSource ptr)
            {
                return new SimMultiValueFunctionParameterSource(this, ptr.GraphName, ptr.AxisValueX, ptr.AxisValueY);
            }
            return DefaultPointer;
        }

        #endregion


        #region Lookup

        /// <summary>
        /// Returns the value on a graph
        /// </summary>
        /// <param name="position">The position at which the value should be returned</param>
        /// <returns>The (Y-) value on the graph, Or NaN when there is no graph at that point</returns>
        public double GetValue(Point3D position)
        {
            return GetValue(position, 0.001);
        }
        /// <summary>
        /// Returns the value on a graph
        /// </summary>
        /// <param name="position">The position at which the value should be returned</param>
        /// <param name="tolerance">The maximum distance between the point and the graph until no match is found</param>
        /// <returns>The (Y-) value on the graph, Or NaN when there is no graph at that point</returns>
        public double GetValue(Point3D position, double tolerance)
        {
            return GetValue(position, tolerance, out var isValid, out var cp, out var cg);
        }
        /// <summary>
        /// Returns the value on a graph
        /// </summary>
        /// <param name="position">The position at which the value should be returned</param>
        /// <param name="tolerance">The maximum distance between the point and the graph until no match is found</param>
        /// <param name="isValid">Returns true when a point closer to the tolerance is found, otherwise False</param>
        /// <param name="closestPoint">Returns the closest point on the graph (only valid when isValid equals True)</param>
        /// <param name="closestGraph">Returns the graph on which the closest point is located</param>
        /// <returns>The (Y-) value on the graph, Or NaN when there is no graph at that point</returns>
        public double GetValue(Point3D position, double tolerance, out bool isValid, out Point3D closestPoint, out SimMultiValueFunctionGraph closestGraph)
        {
            isValid = false;
            closestPoint = new Point3D(double.NaN, double.NaN, double.NaN);
            closestGraph = null;

            if (this.Graphs.Count == 0)
                return double.NaN;

            double minDistance = double.PositiveInfinity;
            double minValue = double.NaN;
            Point3D minPoint = new Point3D(double.NaN, double.NaN, double.NaN);
            SimMultiValueFunctionGraph minGraph = null;

            foreach (var graph in Graphs)
            {
                var cp = graph.ClosestPoint(position);
                if (!double.IsNaN(cp.distance) && cp.distance < minDistance)
                {
                    minDistance = cp.distance;
                    minValue = cp.value;
                    minPoint = cp.closestPoint;
                    minGraph = graph;
                }
            }

            if (!double.IsNaN(minValue) && minDistance < tolerance)
            {
                isValid = true;
                closestPoint = minPoint;
                closestGraph = minGraph;
                return minValue;
            }
            else
            {
                return double.NaN;
            }
        }

        #endregion

        #region Data Integrety

        private void ClampPointsToValidXYRange()
        {
            if (this.Graphs != null) //During Ctor
            {
                foreach (var graph in this.Graphs)
                {
                    for (int i = 0; i < graph.Points.Count; ++i)
                    {
                        //Check if in range
                        var p = graph.Points[i];
                        if (p.X < this.Range.Minimum.X || p.X > this.Range.Maximum.X || p.Y < this.Range.Minimum.Y || p.Y > this.Range.Maximum.Y)
                        {
                            graph.Points[i] = new Point3D(
                                p.X.Clamp(this.Range.Minimum.X, this.Range.Maximum.X),
                                p.Y.Clamp(this.Range.Minimum.Y, this.Range.Maximum.Y),
                                p.Z);
                        }
                    }
                }
            }
        }

        #endregion


        #region Sampling

        /// <summary>
        /// Samples all graphs with a specific step size.
        /// Note, that the last step might be smaller than stepSize.
        /// </summary>
        /// <param name="start">Start of the sampling range on the X-Axis</param>
        /// <param name="end">End of the sampling range on the X-Axis</param>
        /// <param name="stepSize">Step-Size along the X-Axis</param>
        /// <returns>A row-major sampling of all graphs. Graphs are in columns, steps are in rows</returns>
        public (List<List<double>> graphsSamples, List<string> columnNames) Sample(double start, double end, double stepSize)
        {
            return Sample(start, end, stepSize, Graphs);
        }
        /// <summary>
        /// Samples all graphs on a specific z-layer with a specific step size.
        /// Note, that the last step might be smaller than stepSize.
        /// </summary>
        /// <param name="start">Start of the sampling range on the X-Axis</param>
        /// <param name="end">End of the sampling range on the X-Axis</param>
        /// <param name="stepSize">Step-Size along the X-Axis</param>
        /// <param name="zIndex">The index of the z entry which should be sampled</param>
        /// <returns>A row-major sampling of all graphs. Graphs are in columns, steps are in rows</returns>
        public (List<List<double>> graphsSamples, List<string> columnNames) Sample(double start, double end, double stepSize, int zIndex)
        {
            double zValue = ZAxis[zIndex];
            return Sample(start, end, stepSize, Graphs.Where(x => x.Points.Count > 0 && Math.Abs(x.Points[0].Z - zValue) < 0.00001));
        }

        private (List<List<double>> graphsSamples, List<string> columnNames) Sample(double start, double end, double stepSize,
            IEnumerable<SimMultiValueFunctionGraph> graphs)
        {
            double length = end - start;
            int numSamples = (int)Math.Floor(length / stepSize) + 1;
            int graphCount = graphs.Count();

            List<List<double>> rowMajorResult = new List<List<double>>(graphCount);
            for (int i = 0; i < numSamples; ++i)
                rowMajorResult.Add(new List<double>(graphCount));

            foreach (var graph in graphs)
            {
                var graphSamples = graph.Sample(start, end, stepSize);
                for (int i = 0; i < numSamples; ++i)
                    rowMajorResult[i].Add(graphSamples[i]);
            }

            return (rowMajorResult, graphs.Select(x => x.Name).ToList());
        }

        #endregion
    }
}
