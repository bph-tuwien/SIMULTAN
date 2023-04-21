using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Utils.Collections
{
    /// <summary>
    /// Dictionary which can contain multiple values for a key
    /// </summary>
    /// <typeparam name="TKey">The type of the keys</typeparam>
    /// <typeparam name="TValue">The type of the values</typeparam>
    public class MultiDictionaryOptimized<TKey, TValue>// : IEnumerable<KeyValuePair<TKey, IEnumerable<TValue>>>
    {
        private class ItemList : List<TValue> { }

        private Dictionary<TKey, object> dictionary;

        /// <summary>
        /// Initializes a new instance of the <see cref="MultiDictionary{TKey, TValue}"/> class
        /// </summary>
        public MultiDictionaryOptimized()
        {
            this.dictionary = new Dictionary<TKey, object>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MultiDictionary{TKey, TValue}"/> class
        /// </summary>
        /// <param name="original">The initial data for the dictionary</param>
        public MultiDictionaryOptimized(MultiDictionaryOptimized<TKey, TValue> original)
        {
            dictionary = new Dictionary<TKey, object>(original.dictionary);
        }

        /// <summary>
        /// Adds a new entry to the dictionary
        /// </summary>
        /// <param name="key">The key</param>
        /// <param name="value">The value</param>
        public void Add(TKey key, TValue value)
        {
            if (dictionary.TryGetValue(key, out var entry))
            {
                if (entry is ItemList il)
                    il.Add(value);
                else
                    dictionary[key] = new ItemList { (TValue)entry, value };
            }
            else
                dictionary.Add(key, value);
        }
        /// <summary>
        /// Removes a specific key-value pair from the dictionary
        /// </summary>
        /// <param name="key">The key</param>
        /// <param name="value">The value</param>
        /// <returns>true if the element is successfully found and removed; otherwise, false. 
        /// This method returns false if key is not found in the <see cref="MultiDictionary{TKey, TValue}"/></returns>
        public bool Remove(TKey key, TValue value)
        {
            if (dictionary.TryGetValue(key, out var entry))
            {
                if (entry is ItemList il) //More than one
                {
                    bool removed = il.Remove(value);
                    if (il.Count == 1)
                    {
                        dictionary[key] = il[0];
                    }
                    return removed;
                }
                else //Only one
                {
                    return dictionary.Remove(key);
                }
            }
            return false;
        }

        /// <summary>
        /// Removes all values associated with a key from the dictionary
        /// </summary>
        /// <param name="key">The key</param>
        /// <returns>true if the element is successfully found and removed; otherwise, false. 
        /// This method returns false if key is not found in the <see cref="MultiDictionary{TKey, TValue}"/></returns>
        public bool Remove(TKey key)
        {
            return dictionary.Remove(key);
        }

        /// <summary>
        /// Returns the values associated with a key. Throws an exception when the key is not found
        /// </summary>
        /// <param name="key">The key</param>
        /// <returns>The values associated with the key</returns>
        public IReadOnlyCollection<TValue> this[TKey key]
        {
            get
            {
                var entry = dictionary[key];
                if (entry is ItemList il)
                    return il;
                else
                    return new TValue[] { (TValue)entry };
            }
        }
        /// <summary>
        /// Gets the value associated with the specified key.
        /// </summary>
        /// <param name="key">The key</param>
        /// <param name="values">When this method returns, contains the value associated with the specified key, 
        /// if the key is found; otherwise, the null. This parameter is passed uninitialized.
        /// </param>
        /// <returns>true if the <see cref="MultiDictionary{TKey, TValue}"/> contains an element with the specified key; otherwise, false</returns>
        public bool TryGetValues(TKey key, out IEnumerable<TValue> values)
        {
            if (dictionary.TryGetValue(key, out var tmpValues))
            {
                if (tmpValues is ItemList il)
                    values = il;
                else
                    values = new TValue[] { (TValue)tmpValues };
                return true;
            }

            values = null;
            return false;
        }

        /// <summary>
        /// Return true if the dictionary contais the given key.
        /// </summary>
        /// <param name="key">key to check</param>
        /// <returns>true if the dictionary contais the given key</returns>
        public bool ContainsKey(TKey key)
        {
            return dictionary != null && dictionary.ContainsKey(key);
        }

        /// <summary>
        /// Removes all entries from the dictionary
        /// </summary>
        public void Clear()
        {
            this.dictionary.Clear();
        }

        /*/// <inheritdoc/>
        public IEnumerator<KeyValuePair<TKey, IReado<TValue>>> GetEnumerator()
        {
            foreach (var entry in dictionary)
            {
                yield return new KeyValuePair<TKey, IEnumerable<TValue>>(entry.Key, entry.Value);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }*/
    }
}
