using SIMULTAN.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace SIMULTAN.Data.MultiValues
{
    /// <summary>
    /// Stores a list of points in a SimMultiValueFunctionGraph.
    /// Ensures that all points lie inside the definition space when the graph belongs to a SimMultiValueFunction
    /// </summary>
    public class SimMultiValueFunctionPointList : IEnumerable<Point3D>, INotifyCollectionChanged
    {
        private List<Point3D> values;
        private SimMultiValueFunctionGraph graph;

        /// <inheritdoc />
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        /// <summary>
        /// Initializes a new instance of the SimMultiValueFunctionPointList class
        /// </summary>
        /// <param name="graph">The graph this collection belongs to</param>
        /// <param name="points">Initial set of points</param>
        public SimMultiValueFunctionPointList(SimMultiValueFunctionGraph graph, IEnumerable<Point3D> points)
        {
            this.graph = graph;
            this.values = points.ToList();
        }

        /// <summary>
        /// Gets or sets a value in the collection. Clamps newly added points to the SimMultiValueFunction.Range
        /// </summary>
        /// <param name="index">The index of the point</param>
        /// <returns>The point at the given index</returns>
        public Point3D this[int index]
        {
            get
            {
                return values[index];
            }
            set
            {
                var old = values[index];
                values[index] = Clamp(value);
                this.CollectionChanged?.Invoke(this,
                    new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, values[index], old, index));
                graph?.Function?.Factory?.NotifyChanged();
            }
        }

        private Point3D Clamp(Point3D value)
        {
            if (graph.Function == null)
                return value;

            return new Point3D(
                value.X.Clamp(graph.Function.Range.Minimum.X, graph.Function.Range.Maximum.X),
                value.Y.Clamp(graph.Function.Range.Minimum.Y, graph.Function.Range.Maximum.Y),
                value.Z
                );
        }

        /// <summary>
        /// Adds a point to the collection. Clamps newly added points to the SimMultiValueFunction.Range
        /// </summary>
        /// <param name="item">The new point</param>
        public void Add(Point3D item)
        {
            var clamped = Clamp(item);
            values.Add(clamped);
            this.CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, clamped));
            graph?.Function?.Factory?.NotifyChanged();
        }
        /// <summary>
        /// Removes a point from the collection
        /// </summary>
        /// <param name="index">The index of the point which should be removed</param>
        public void RemoveAt(int index)
        {
            var removed = values[index];
            values.RemoveAt(index);

            this.CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, removed, index));
            graph?.Function?.Factory?.NotifyChanged();
        }
        /// <summary>
        /// Inserts a point at a specific location into the collection. Clamps newly added points to the SimMultiValueFunction.Range
        /// </summary>
        /// <param name="index">The zero-based index at which value should be inserted</param>
        /// <param name="item">The new point</param>
        public void Insert(int index, Point3D item)
        {
            var clamped = Clamp(item);
            values.Insert(index, clamped);
            this.CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, index));
            graph?.Function?.Factory?.NotifyChanged();
        }

        /// <summary>
        /// Returns the number of elements in the collection
        /// </summary>
        public int Count { get { return values.Count; } }


        internal void Clamp()
        {
            for (int i = 0; i < values.Count; ++i)
                values[i] = Clamp(values[i]);

            this.CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        #region IEnumerable<Point3D>

        /// <inheritdoc />
        public IEnumerator<Point3D> GetEnumerator()
        {
            return values.GetEnumerator();
        }
        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator()
        {
            return values.GetEnumerator();
        }

        #endregion
    }
}
