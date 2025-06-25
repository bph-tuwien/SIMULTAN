using SIMULTAN.Data.SimMath;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;

namespace SIMULTAN.Utils
{
    /// <summary>
    /// Provides some useful extensions for Collections
    /// </summary>
    public static class CollectionExtensions
    {
        /// <summary>
        /// Executes an action for all elements in the collection
        /// </summary>
        /// <typeparam name="T">The datatype</typeparam>
        /// <param name="collection">The collection</param>
        /// <param name="action">The action</param>
        public static void ForEach<T>(this ObservableCollection<T> collection, Action<T> action)
        {
            foreach (var x in collection)
                action(x);
        }
        /// <summary>
        /// Executes an action for all elements in the collection
        /// </summary>
        /// <param name="collection">The collection</param>
        /// <param name="action">The action</param>
        public static void ForEach(this System.Collections.IList collection, Action<object> action)
        {
            foreach (var x in collection)
                action(x);
        }
        /// <summary>
        /// Executes an action for all elements in the collection
        /// </summary>
        /// <typeparam name="T">The datatype</typeparam>
        /// <param name="collection">The collection</param>
        /// <param name="action">The action</param>
        public static void ForEach<T>(this IReadOnlyCollection<T> collection, Action<T> action)
        {
            foreach (var element in collection)
                action(element);
        }
        /// <summary>
        /// Executes an action for all elements in the collection
        /// </summary>
        /// <typeparam name="T">The datatype</typeparam>
        /// <param name="collection">The collection</param>
        /// <param name="action">The action</param>
        [DebuggerStepThrough]
        public static void ForEach<T>(this IEnumerable<T> collection, Action<T> action)
        {
            foreach (var element in collection)
                action(element);
        }



        /// <summary>
        /// Adds a range of items to the collection
        /// </summary>
        /// <typeparam name="T">Item type</typeparam>
        /// <param name="collection">The collection</param>
        /// <param name="items">The items to add</param>
        public static void AddRange<T>(this HashSet<T> collection, IEnumerable<T> items)
        {
            foreach (var i in items)
                collection.Add(i);
        }

        /// <summary>
        /// Adds a range of items to the collection
        /// </summary>
        /// <typeparam name="T">Item type</typeparam>
        /// <param name="collection">The collection</param>
        /// <param name="items">The items to add</param>
        public static void AddRange<T>(this ObservableCollection<T> collection, IEnumerable<T> items)
        {
            foreach (var i in items)
                collection.Add(i);
        }

        /// <summary>
        /// Returns the element with the lowest key value
        /// </summary>
        /// <typeparam name="T">Type of the elements</typeparam>
        /// <typeparam name="Key">Type of the key. Has to implement IComparable</typeparam>
        /// <param name="collection">The collection</param>
        /// <param name="key">Function that calculates the key</param>
        /// <returns>The element with the lowest key value</returns>
        public static (T value, Key key, int index) ArgMin<T, Key>(this IEnumerable<T> collection, Func<T, Key> key) where Key : IComparable
        {
            if (collection.Count() == 0)
                throw new ArgumentException("Collection may not be empty");

            var result = Enumerable.Range(0, int.MaxValue).Zip(collection, (idx, c) => (key(c), idx, c)).Min();
            return (result.c, result.Item1, result.idx);
        }
        /// <summary>
        /// Returns the element with the highest key value
        /// </summary>
        /// <typeparam name="T">Type of the elements</typeparam>
        /// <typeparam name="Key">Type of the key. Has to implement IComparable</typeparam>
        /// <param name="collection">The collection</param>
        /// <param name="key">Function that calculates the key</param>
        /// <returns>The element with the highest key value</returns>
        public static (T value, Key key, int index) ArgMax<T, Key>(this IEnumerable<T> collection, Func<T, Key> key) where Key : IComparable
        {
            if (collection.Count() == 0)
                throw new ArgumentException("Collection may not be empty");

            var result = Enumerable.Range(0, int.MaxValue).Zip(collection, (idx, c) => (key(c), idx, c)).Max();
            return (result.c, result.Item1, result.idx);
        }

        /// <summary>
        /// Removes all items from a collection that fulfill the predicate
        /// </summary>
        /// <typeparam name="T">Item type of the collection</typeparam>
        /// <param name="collection">The collection</param>
        /// <param name="predicate">Predicate to determine which elements will be removed</param>
        public static void RemoveWhere<T>(this Collection<T> collection, Func<T, bool> predicate)
        {
            for (int i = 0; i < collection.Count; i++)
            {
                if (predicate(collection[i]))
                {
                    collection.RemoveAt(i);
                    i--;
                }
            }
        }
        /// <summary>
        /// Removes all items from a collection that fulfill the predicate
        /// </summary>
        /// <typeparam name="T">Item type of the collection</typeparam>
        /// <param name="collection">The collection</param>
        /// <param name="predicate">Predicate to determine which elements will be removed</param>
        public static void RemoveWhere<T>(this IList<T> collection, Func<T, bool> predicate)
        {
            for (int i = 0; i < collection.Count; i++)
            {
                if (predicate(collection[i]))
                {
                    collection.RemoveAt(i);
                    i--;
                }
            }
        }
        /// <summary>
        /// Removes all items from a collection that fulfill the predicate
        /// </summary>
        /// <typeparam name="T">Item type of the collection</typeparam>
        /// <param name="collection">The collection</param>
        /// <param name="predicate">Predicate to determine which elements will be removed</param> 
        /// <param name="removeAction">Action that gets called on all elements that are removed</param> 
        public static void RemoveWhere<T>(this Collection<T> collection, Func<T, bool> predicate, Action<T> removeAction)
        {
            for (int i = 0; i < collection.Count; i++)
            {
                if (predicate(collection[i]))
                {
                    removeAction(collection[i]);
                    collection.RemoveAt(i);
                    i--;
                }
            }
        }
        /// <summary>
        /// Removes all items from a collection that fulfill the predicate
        /// </summary>
        /// <typeparam name="T">Item type of the collection</typeparam>
        /// <param name="collection">The collection</param>
        /// <param name="predicate">Predicate to determine which elements will be removed</param> 
        /// <param name="removeAction">Action that gets called on all elements that are removed</param> 
        public static void RemoveWhere<T>(this IList<T> collection, Func<T, bool> predicate, Action<T> removeAction)
        {
            for (int i = 0; i < collection.Count; i++)
            {
                if (predicate(collection[i]))
                {
                    removeAction(collection[i]);
                    collection.RemoveAt(i);
                    i--;
                }
            }
        }

        /// <summary>
        /// Removes the first occurence that matches the predicate.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection">The collection</param>
        /// <param name="predicate">Predicate to determine which elements will be removed</param>
        /// <returns>True when an item has been removed, otherwise False</returns>
        public static bool RemoveFirst<T>(this Collection<T> collection, Func<T, bool> predicate)
        {
            return RemoveFirst(collection, predicate, out _);
        }
        /// <summary>
        /// Removes the first occurrence that matches the predicate.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection">The collection</param>
        /// <param name="predicate">Predicate to determine which elements will be removed</param>
        /// <param name="removed">The removed item</param>
        /// <returns>True when an item has been removed, otherwise False</returns>
        public static bool RemoveFirst<T>(this Collection<T> collection, Func<T, bool> predicate, out T removed)
        {
            removed = default;

            for (int i = 0; i < collection.Count; i++)
            {
                if (predicate(collection[i]))
                {
                    removed = collection[i];
                    collection.RemoveAt(i);
                    return true;
                }
            }

            return false;
        }


        /// <summary>
        /// Converts an enumerable to a observable collection
        /// </summary>
        /// <typeparam name="T">Type of the items</typeparam>
        /// <param name="enumerable">The enumerable to convert</param>
        /// <returns>An observable collection</returns>
        public static ObservableCollection<T> ToObservableCollection<T>(this IEnumerable<T> enumerable)
        {
            return new ObservableCollection<T>(enumerable);
        }

        /// <summary>
        /// Calculates the average SimPoint3D
        /// </summary>
        /// <typeparam name="T">Type of the collection</typeparam>
        /// <param name="enumerable">The collection</param>
        /// <param name="selector">Calculates a SimPoint3D from a collection element</param>
        /// <returns>The average SimPoint3D</returns>
        public static SimPoint3D Average<T>(this IEnumerable<T> enumerable, Func<T, SimPoint3D> selector)
        {
            SimVector3D sum = new SimVector3D(0, 0, 0);
            foreach (var i in enumerable)
                sum += (SimVector3D)selector(i);

            return (SimPoint3D)(sum / enumerable.Count());
        }
        /// <summary>
        /// Calculates the average SimVector3D
        /// </summary>
        /// <typeparam name="T">Type of the collection</typeparam>
        /// <param name="enumerable">The collection</param>
        /// <param name="selector">Calculates a SimVector3D from a collection element</param>
        /// <returns>The average SimVector3D</returns>
        public static SimVector3D Average<T>(this IEnumerable<T> enumerable, Func<T, SimVector3D> selector)
        {
            SimVector3D sum = new SimVector3D(0, 0, 0);
            foreach (var i in enumerable)
                sum += selector(i);

            return (sum / enumerable.Count());
        }

        /// <summary>
        /// Returns a distinct list where the equality is determined by the selector
        /// </summary>
        /// <typeparam name="T">Type of the elements</typeparam>
        /// <param name="enumerable">The collection</param>
        /// <param name="selector">Method that selects the object to compare</param>
        /// <returns>A list where no two items have the same selector value</returns>
        public static IEnumerable<T> DistinctBy<T>(this IEnumerable<T> enumerable, Func<T, object> selector)
        {
            return enumerable.GroupBy(selector).Select(g => g.First());
        }




        /// <summary>
        /// Converts an IList into a typed list
        /// </summary>
        /// <typeparam name="T">Type of the items</typeparam>
        /// <param name="iList">The original list</param>
        /// <returns>The typed list</returns>
        public static IList<T> ToList<T>(this IList iList)
        {
            IList<T> result = new List<T>();
            foreach (T value in iList)
                result.Add(value);

            return result;
        }


        /// <summary>
        /// Handles a collection change event translation
        /// Basically, this method duplicates the CollectionChanged operation onto another list
        /// </summary>
        /// <typeparam name="TSource">Source element type</typeparam>
        /// <typeparam name="TTarget">Target element type</typeparam>
        /// <param name="args">CollectionChanged event args</param>
        /// <param name="target">Target list</param>
        /// <param name="convert">Function to convert from source to target</param>
        public static void HandleCollectionChanged<TSource, TTarget>(
            NotifyCollectionChangedEventArgs args, IList<TTarget> target,
            Func<TSource, TTarget> convert)
        {
            if (args.Action == NotifyCollectionChangedAction.Add)
            {
                target.Insert(args.NewStartingIndex, convert((TSource)args.NewItems[0]));
            }
            else if (args.Action == NotifyCollectionChangedAction.Move)
            {
                if (target is ObservableCollection<TTarget>)
                    ((ObservableCollection<TTarget>)target).Move(args.OldStartingIndex, args.NewStartingIndex);
                else
                    throw new NotSupportedException("The target collection does not support move operations");
            }
            else if (args.Action == NotifyCollectionChangedAction.Remove)
            {
                (target[args.OldStartingIndex] as IDisposable)?.Dispose();
                target.RemoveAt(args.OldStartingIndex);
            }
            else if (args.Action == NotifyCollectionChangedAction.Replace)
            {
                (target[args.OldStartingIndex] as IDisposable)?.Dispose();
                target[args.OldStartingIndex] = convert((TSource)args.NewItems[0]);
            }
            else if (args.Action == NotifyCollectionChangedAction.Reset)
            {
                target.ForEach(x => (x as IDisposable)?.Dispose());
                target.Clear();
            }
        }

        /// <summary>
        /// Tries to find the first element that matches the predicate. Returns True when one is found or False when non is found.
        /// </summary>
        /// <typeparam name="T">Type of the elements in the collection</typeparam>
        /// <param name="source">The collection</param>
        /// <param name="predicate">The matching function. Has to return True for a match</param>
        /// <param name="value">The found value. Only valid when the method returns True</param>
        /// <returns>True when one is found or False when non is found</returns>
        public static bool TryFirstOrDefault<T>(this IEnumerable<T> source, Predicate<T> predicate, out T value)
        {
            value = default(T);
            foreach (var val in source)
            {
                if (predicate(val))
                {
                    value = val;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns the index of the first item that matches the predicate, or -1 when no matching item exists
        /// </summary>
        /// <typeparam name="T">Type of the collection elements</typeparam>
        /// <param name="list">The collection</param>
        /// <param name="predicate">The predicate to search for</param>
        /// <returns>The index of the first item that matches the predicate, or -1 when no matching item exists</returns>
        public static int FindIndex<T>(this IEnumerable<T> list, Predicate<T> predicate)
        {
            int idx = 0;
            foreach (var item in list)
            {
                if (predicate(item))
                    return idx;
                idx++;
            }

            return -1;
        }

        /// <summary>
        /// Splits a list into two lists: One with elements matching the predicate, the other with elements not matching.
        /// </summary>
        /// <typeparam name="T">Type of the list elements</typeparam>
        /// <param name="list">The input list</param>
        /// <param name="predicate">Predicate to decide into which list elements should be sorted</param>
        /// <returns>Two lists:
        ///  - trueItems contains all items where predicate returns true. 
        ///  - falseItems returns all items where predicate returns false.
        /// </returns>
        public static (IEnumerable<T> trueItems, IEnumerable<T> falseItems) Split<T>(this IEnumerable<T> list, Predicate<T> predicate)
        {
            List<T> trueList = new List<T>();
            List<T> falseList = new List<T>();

            foreach (var item in list)
            {
                if (predicate(item))
                    trueList.Add(item);
                else
                    falseList.Add(item);
            }

            return (trueList, falseList);
        }



        /// <summary>
        /// Creates a deep copy of a nested list. Does NOT create a copy of the elements in the lists
        /// </summary>
        /// <typeparam name="T">The type of the list elements</typeparam>
        /// <param name="source">The source collection</param>
        /// <returns></returns>
        public static List<List<T>> DeepCopy<T>(this List<List<T>> source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            List<List<T>> copy = new List<List<T>>();

            foreach (List<T> sub_list in source)
            {
                copy.Add(new List<T>(sub_list));
            }

            return copy;
        }

        /// <summary>
        /// Transposes the input list (exchange rows with columns)
        /// </summary>
        /// <typeparam name="T">The type of the list elements</typeparam>
        /// <param name="input">The collection which should be transposed</param>
        /// <returns>The transposed input collection</returns>
        public static List<List<T>> Transpose<T>(this List<List<T>> input)
        {
            List<List<T>> transposed = new List<List<T>>();
            if (input == null) return transposed;

            for (int col = 0; col < input[0].Count; col++)
            {
                List<T> col_transposed = new List<T>();
                for (int row = 0; row < input.Count; row++)
                {
                    col_transposed.Add(input[row][col]);
                }
                transposed.Add(col_transposed);
            }

            return transposed;
        }


        /// <summary>
        /// Tries to get an item with a specific index from an IEnumerable
        /// </summary>
        /// <typeparam name="T">Type of the items</typeparam>
        /// <param name="list">The collection</param>
        /// <param name="index">The index to look for</param>
        /// <param name="result">Contains the element at this index (or the default value when no such item exists)</param>
        /// <returns>True when the collection contains an item with the given index, otherwise False</returns>
        public static bool TryGetElementAt<T>(this IEnumerable<T> list, int index, out T result)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index));

            int idx = 0;
            foreach (var item in list)
            {
                if (idx == index)
                {
                    result = item;
                    return true;
                }

                idx++;
            }

            result = default(T);
            return false;
        }

        /// <summary>
        /// Searches through a collection and returns the indices at which the predicate matches
        /// </summary>
        /// <typeparam name="T">Type of the collection items</typeparam>
        /// <param name="source">The collection</param>
        /// <param name="predicate">Predicate to search for</param>
        /// <returns>The indicies in the collection at which the predicate returned True</returns>
        public static IEnumerable<int> IndicesWhere<T>(this IEnumerable<T> source, Func<T, bool> predicate)
        {
            int index = 0;
            foreach (T element in source)
            {
                if (predicate(element))
                {
                    yield return index;
                }
                index++;
            }
        }
    }
}
