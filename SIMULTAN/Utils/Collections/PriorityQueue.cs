using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Utils.Collections
{
    /// <summary>
    /// A queue which sorts by priority
    /// Taken from https://gist.github.com/paralleltree/31045ab26f69b956052c
    /// </summary>
    /// <typeparam name="T">Type of the elements</typeparam>
    public class PriorityQueue<T>
    {
        private List<T> list;
        private Comparison<T> comparison;

        /// <summary>
        /// Number of elements int the queue
        /// </summary>
        public int Count { get { return list.Count; } }
        /// <summary>
        /// Defines whether the collection sorts ascending or descending.
        /// False by default
        /// </summary>
        public readonly bool IsDescending = false;

        /// <summary>
        /// Initializes a new ascending instance of the priority queue
        /// </summary>
        /// <param name="comparison">Comparison operator</param>
        public PriorityQueue(Comparison<T> comparison)
        {
            this.comparison = comparison;
            this.list = new List<T>();
        }
        /// <summary>
        /// Initializes a new ascending instance of the priority queue
        /// </summary>
        /// <param name="isdesc">True when the collection should be sorted descending, False for ascending</param>
        /// <param name="comparison">Comparison operator</param>
        public PriorityQueue(bool isdesc, Comparison<T> comparison)
            : this(comparison)
        {
            this.IsDescending = isdesc;
        }
        /// <summary>
        /// Initializes a new ascending instance of the priority queue
        /// </summary>
        /// <param name="capacity">The initial capacity of the queue</param>
        /// <param name="comparison">Comparison operator</param>
        public PriorityQueue(int capacity, Comparison<T> comparison)
            : this(capacity, false, comparison)
        { }
        /// <summary>
        /// Initializes a new ascending instance of the priority queue
        /// </summary>
        /// <param name="collection">The initial elements for the queue</param>
        /// <param name="comparison">Comparison operator</param>
        public PriorityQueue(IEnumerable<T> collection, Comparison<T> comparison)
            : this(collection, false, comparison)
        { }
        /// <summary>
        /// Initializes a new ascending instance of the priority queue
        /// </summary>
        /// <param name="capacity">The initial capacity of the queue</param>
        /// <param name="isdesc">True when the collection should be sorted descending, False for ascending</param>
        /// <param name="comparison">Comparison operator</param>
        public PriorityQueue(int capacity, bool isdesc, Comparison<T> comparison)
        {
            this.list = new List<T>(capacity);
            this.IsDescending = isdesc;
            this.comparison = comparison;
        }
        /// <summary>
        /// Initializes a new ascending instance of the priority queue
        /// </summary>
        /// <param name="collection">The initial elements for the queue</param>
        /// <param name="isdesc">True when the collection should be sorted descending, False for ascending</param>
        /// <param name="comparison">Comparison operator</param>
        public PriorityQueue(IEnumerable<T> collection, bool isdesc, Comparison<T> comparison)
            : this(comparison)
        {
            IsDescending = isdesc;
            foreach (var item in collection)
                Enqueue(item);
        }

        /// <summary>
        /// Adds a new item to the queue
        /// </summary>
        /// <param name="x">The new item</param>
        public void Enqueue(T x)
        {
            list.Add(x);
            int i = Count - 1;

            while (i > 0)
            {
                int p = (i - 1) / 2;
                if ((IsDescending ? -1 : 1) * this.comparison(list[p], x) <= 0) break;

                list[i] = list[p];
                i = p;
            }

            if (Count > 0) list[i] = x;
        }

        /// <summary>
        /// Dequeues the item with the highest (or lowest depending on sort order) priority
        /// </summary>
        /// <returns>The item with the highest (or lowest depending on sort order) priority</returns>
        public T Dequeue()
        {
            T target = Peek();
            T root = list[Count - 1];
            list.RemoveAt(Count - 1);

            int i = 0;
            while (i * 2 + 1 < Count)
            {
                int a = i * 2 + 1;
                int b = i * 2 + 2;
                int c = b < Count && (IsDescending ? -1 : 1) * this.comparison(list[b], list[a]) < 0 ? b : a;

                if ((IsDescending ? -1 : 1) * this.comparison(list[c], root) >= 0) break;
                list[i] = list[c];
                i = c;
            }

            if (Count > 0) list[i] = root;
            return target;
        }

        /// <summary>
        /// Returns the item with the highest (or lowest depending on sort order) priority without removing it from the queue
        /// </summary>
        /// <returns>The item with the highest (or lowest depending on sort order) priority</returns>
        public T Peek()
        {
            if (Count == 0) throw new InvalidOperationException("Queue is empty.");
            return list[0];
        }
        /// <summary>
        /// Clears the collection
        /// </summary>
        public void Clear()
        {
            list.Clear();
        }
    }
}
