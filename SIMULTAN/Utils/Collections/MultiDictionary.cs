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
    public class MultiDictionary<TKey, TValue> : IEnumerable<KeyValuePair<TKey, IEnumerable<TValue>>>
    {
        private Dictionary<TKey, List<TValue>> dictionary;

        /// <summary>
        /// Initializes a new instance of the <see cref="MultiDictionary{TKey, TValue}"/> class
        /// </summary>
        public MultiDictionary() 
        {
            this.dictionary = new Dictionary<TKey, List<TValue>>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MultiDictionary{TKey, TValue}"/> class
        /// </summary>
        /// <param name="original">The initial data for the dictionary</param>
        public MultiDictionary(MultiDictionary<TKey, TValue> original)
        {
            dictionary = new Dictionary<TKey, List<TValue>>(original.dictionary);            
        }

        /// <summary>
        /// Adds a new entry to the dictionary
        /// </summary>
        /// <param name="key">The key</param>
        /// <param name="value">The value</param>
        public void Add(TKey key, TValue value)
        {
            if (dictionary.TryGetValue(key, out var values))
                values.Add(value);
            else
                dictionary.Add(key, new List<TValue> { value });
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
            if (dictionary.TryGetValue(key, out var values))
            {
                bool removed = values.Remove(value);
                if (values.Count == 0)
                    dictionary.Remove(key);
                return removed;
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
        public IEnumerable<TValue> this[TKey key]
        {
            get
            {
                return dictionary[key];
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
                values = tmpValues;
                return true;
            }

            values = null;
            return false;
        }

        /// <summary>
        /// Removes all entries from the dictionary
        /// </summary>
        public void Clear()
        {
            this.dictionary.Clear();
        }

        /// <inheritdoc/>
        public IEnumerator<KeyValuePair<TKey, IEnumerable<TValue>>> GetEnumerator()
        {
            foreach (var entry in dictionary)
            {
                yield return new KeyValuePair<TKey, IEnumerable<TValue>>(entry.Key, entry.Value);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
