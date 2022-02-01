using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;

namespace SIMULTAN.Utils.Collections
{
    /// <summary>
    /// Provides a dictionary that notifies on Add, Remove, Reset.
    /// </summary>
    [DebuggerDisplay("Count={Count}")]
    public class ObservableDictionary<TKey, TValue> :
        ICollection<KeyValuePair<TKey, TValue>>, IDictionary<TKey, TValue>,
        INotifyPropertyChanged,
        IReadonlyObservableDictionary<TKey, TValue>
    {
        /// <summary>
        /// The internal container holding the dictionary data.
        /// </summary>
        protected readonly Dictionary<TKey, TValue> dictionary;

        #region .CTOR
        /// <summary>
        /// Initalizes an empty observable dictionary.
        /// </summary>
        public ObservableDictionary()
        {
            this.dictionary = new Dictionary<TKey, TValue>();
        }

        /// <summary>
        /// Uses a copy of the input dictionary to intialize an instance of ObservableDictionary.
        /// </summary>
        /// <param name="dictionary">the given input dictionary</param>
        public ObservableDictionary(IDictionary<TKey, TValue> dictionary)
        {
            this.dictionary = new Dictionary<TKey, TValue>(dictionary);
        }

        /// <summary>
        /// Initializes an instance of ObservableDictionary from an enumerable collection of key-value pairs.
        /// </summary>
        /// <param name="input">the key-value pairs to be translated</param>
        public ObservableDictionary(IEnumerable<KeyValuePair<TKey, TValue>> input)
        {
            this.dictionary = input.ToDictionary(x => x.Key, x => x.Value);
        }

        /// <summary>
        /// Initalizes an instance of ObservableDictionary from separate collections of keys and values.
        /// Throws an exception if one of the collections is empty, or the number of elements in the collections differ.
        /// </summary>
        /// <param name="keys">the keys</param>
        /// <param name="values">the values aligned with the keys</param>
        public ObservableDictionary(IEnumerable<TKey> keys, IEnumerable<TValue> values)
        {
            if (keys == null)
                throw new ArgumentNullException(nameof(keys), " Parameter cannot be Null!");
            if (values == null)
                throw new ArgumentNullException(nameof(values), " Parameter cannot be Null!");

            if (keys.Count() != values.Count())
                throw new ArgumentException("The number of keys and values does not match!");

            this.dictionary = keys.Zip(values, (x, y) => new KeyValuePair<TKey, TValue>(x, y)).ToDictionary(x => x.Key, x => x.Value);
        }

        #endregion 

        /// <summary>
        /// Emitted when the collection changes.
        /// </summary>
        public event NotifyCollectionChangedEventHandler CollectionChanged;
        /// <summary>
        /// Emitted when a property on the collection changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Notifies observers of CollectionChanged or PropertyChanged of an update to the dictionary.
        /// </summary>
        /// <param name="_action">indicates the type of change</param>
        /// <param name="_new_key">the new key</param>
        /// <param name="_new_value">the new value</param>
        /// <param name="_old_key">the old key</param>
        /// <param name="_old_value">the old value</param>
        protected virtual void NotifyObserversOfChange(NotifyCollectionChangedAction _action, TKey _new_key, TValue _new_value, TKey _old_key, TValue _old_value)
        {
            var collectionHandler = this.CollectionChanged;
            var propertyHandler = this.PropertyChanged;

            if (collectionHandler != null)
            {
                switch (_action)
                {
                    case NotifyCollectionChangedAction.Add:
                        collectionHandler(this, new NotifyCollectionChangedEventArgs(_action, new KeyValuePair<TKey, TValue>(_new_key, _new_value)));
                        break;
                    case NotifyCollectionChangedAction.Move:
                        collectionHandler(this, new NotifyCollectionChangedEventArgs(_action, new KeyValuePair<TKey, TValue>(_new_key, _new_value), new KeyValuePair<TKey, TValue>(_old_key, _old_value)));
                        break;
                    case NotifyCollectionChangedAction.Remove:
                        collectionHandler(this, new NotifyCollectionChangedEventArgs(_action, new KeyValuePair<TKey, TValue>(_old_key, _old_value)));
                        break;
                    case NotifyCollectionChangedAction.Replace:
                        collectionHandler(this, new NotifyCollectionChangedEventArgs(_action, new KeyValuePair<TKey, TValue>(_new_key, _new_value), new KeyValuePair<TKey, TValue>(_old_key, _old_value)));
                        break;
                    case NotifyCollectionChangedAction.Reset:
                        throw new NotImplementedException("Use the other overload for reset events");
                }
            }
            if (propertyHandler != null)
            {
                propertyHandler(this, new PropertyChangedEventArgs(nameof(Dictionary<TKey, TValue>.Count)));
                propertyHandler(this, new PropertyChangedEventArgs(nameof(Dictionary<TKey, TValue>.Keys)));
                propertyHandler(this, new PropertyChangedEventArgs(nameof(Dictionary<TKey, TValue>.Values)));
            }
        }
        /// <summary>
        /// Notifies observers of CollectionChanged or PropertyChanged of an update to the dictionary.
        /// </summary>
        /// <param name="_action">indicates the type of change. Has to be Reset or Add</param>
        /// <param name="modifiedObjects">A list of objects that were modified.</param>
        protected virtual void NotifyObserversOfChange(NotifyCollectionChangedAction _action, IList modifiedObjects)
        {
            var collectionHandler = this.CollectionChanged;
            var propertyHandler = this.PropertyChanged;

            if (collectionHandler != null)
            {
                switch (_action)
                {
                    case NotifyCollectionChangedAction.Reset:
                        collectionHandler(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove,
                            modifiedObjects));
                        collectionHandler(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                        break;
                    case NotifyCollectionChangedAction.Add:
                        collectionHandler(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add,
                            modifiedObjects));
                        break;
                    default:
                        throw new NotImplementedException("Use the other overload for this events");
                }
            }
            if (propertyHandler != null)
            {
                propertyHandler(this, new PropertyChangedEventArgs(nameof(Dictionary<TKey, TValue>.Count)));
                propertyHandler(this, new PropertyChangedEventArgs(nameof(Dictionary<TKey, TValue>.Keys)));
                propertyHandler(this, new PropertyChangedEventArgs(nameof(Dictionary<TKey, TValue>.Values)));
            }
        }

        #region ICollection<KeyValuePair<TKey,TValue>>

        /// <inheritdoc/>
        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item)
        {
            this.dictionary.Add(item.Key, item.Value);
            this.NotifyObserversOfChange(NotifyCollectionChangedAction.Add, item.Key, item.Value, default, default);
        }

        /// <inheritdoc/>
        void ICollection<KeyValuePair<TKey, TValue>>.Clear()
        {
            var oldItems = this.dictionary.ToList();
            this.dictionary.Clear();

            this.NotifyObserversOfChange(NotifyCollectionChangedAction.Reset,
                oldItems);
        }

        /// <summary>
        /// Public clear method.
        /// </summary>
        public void Clear()
        {
            var oldItems = this.dictionary.ToList();
            this.dictionary.Clear();

            this.NotifyObserversOfChange(NotifyCollectionChangedAction.Reset,
                oldItems);
        }

        /// <inheritdoc/>
        bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item)
        {
            return this.dictionary.Contains(item);
        }

        /// <inheritdoc/>
        void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            ((ICollection<KeyValuePair<TKey, TValue>>)this.dictionary).CopyTo(array, arrayIndex);
        }

        /// <inheritdoc/>
        int ICollection<KeyValuePair<TKey, TValue>>.Count
        {
            get { return this.dictionary.Count; }
        }

        /// <inheritdoc/>
        bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly
        {
            get { return ((ICollection<KeyValuePair<TKey, TValue>>)this.dictionary).IsReadOnly; }
        }

        /// <inheritdoc/>
        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item)
        {
            bool removed = this.dictionary.Remove(item.Key);
            this.NotifyObserversOfChange(NotifyCollectionChangedAction.Remove, default, default, item.Key, item.Value);
            return removed;
        }

        #endregion

        #region IEnumerable<KeyValuePair<TKey,TValue>>

        /// <inheritdoc/>
        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
        {
            return this.dictionary.GetEnumerator();
        }

        /// <inheritdoc/>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.dictionary.GetEnumerator();
        }

        #endregion

        #region IDictionary<TKey,TValue>

        /// <summary>
        /// Gives the number of elements in the dictionary.
        /// </summary>
        public int Count
        {
            get { return (this.dictionary as ICollection<KeyValuePair<TKey, TValue>>).Count; }
        }

        /// <inheritdoc/>
        public void Add(TKey key, TValue value)
        {
            this.dictionary.Add(key, value);
            this.NotifyObserversOfChange(NotifyCollectionChangedAction.Add, key, value, default, default);
        }

        /// <summary>
        /// Adds a range of key-value pairs to the dictionary.
        /// </summary>
        /// <param name="range">the key-value pairs to be added</param>
        public void AddRange(IEnumerable<KeyValuePair<TKey, TValue>> range)
        {
            if (range == null) return;

            foreach (var kvp in range)
            {
                this.dictionary.Add(kvp.Key, kvp.Value);
            }
            this.NotifyObserversOfChange(NotifyCollectionChangedAction.Add, range.ToList());
        }

        /// <summary>
        /// Adds a range of key-value pairs to the dictionary.
        /// Throws an exception if one of the collections is empty, or the number of elements in the collections differ.
        /// </summary>
        /// <param name="keys">the keys in the correct order</param>
        /// <param name="values">the values in the correct order.</param>
        public void AddRange(IEnumerable<TKey> keys, IEnumerable<TValue> values)
        {
            if (keys == null)
                throw new ArgumentNullException(nameof(keys), " Parameter cannot be Null!");
            if (values == null)
                throw new ArgumentNullException(nameof(values), " Parameter cannot be Null!");

            if (keys.Count() != values.Count())
                throw new ArgumentException("The number of keys and values does not match!");

            var kvps = keys.Zip(values, (x, y) => new KeyValuePair<TKey, TValue>(x, y));
            this.AddRange(kvps);
        }

        /// <inheritdoc/>
        public bool ContainsKey(TKey key)
        {
            return this.dictionary.ContainsKey(key);
        }

        /// <inheritdoc/>
        public ICollection<TKey> Keys
        {
            get { return this.dictionary.Keys; }
        }

        /// <inheritdoc/>
        public bool Remove(TKey key)
        {
            TValue removed_value = (this.dictionary.ContainsKey(key)) ? this.dictionary[key] : default;
            bool removed = this.dictionary.Remove(key);
            this.NotifyObserversOfChange(NotifyCollectionChangedAction.Remove, default, default, key, removed_value);
            return removed;
        }

        /// <inheritdoc/>
        public bool TryGetValue(TKey key, out TValue value)
        {
            return this.dictionary.TryGetValue(key, out value);
        }

        /// <inheritdoc/>
        public ICollection<TValue> Values
        {
            get { return this.dictionary.Values; }
        }

        /// <inheritdoc/>
        public TValue this[TKey key]
        {
            get { return this.dictionary[key]; }
            set
            {
                TValue old_value = default(TValue);
                if (this.dictionary.ContainsKey(key))
                    old_value = dictionary[key];

                this.dictionary[key] = value;
                this.NotifyObserversOfChange(NotifyCollectionChangedAction.Replace, key, value, key, old_value);
            }
        }

        #endregion

        #region IReadonlyDictionary

        IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys => ((IReadOnlyDictionary<TKey, TValue>)dictionary).Keys;

        IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values => ((IReadOnlyDictionary<TKey, TValue>)dictionary).Values;

        #endregion

        /// <summary>
        /// Determines if the given value is contained here.
        /// </summary>
        /// <param name="value">the value we are looking for</param>
        /// <returns>true if found, false otherwise</returns>
        public bool ContainsValue(TValue value)
        {
            return this.dictionary.ContainsValue(value);
        }
    }
}
