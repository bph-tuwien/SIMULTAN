using SIMULTAN.Serializer.DXF;
using System;
using System.Collections;
using System.Collections.Generic;

namespace SIMULTAN.Data.Components
{
    /// <summary>
    /// Stores the instances values for parameters. This is an associative container which uses the Parameter itself as a key.
    /// </summary>
    [DXFSerializerTypeNameAttribute("ParameterStructure.Instances.InstanceParameterCollection")]
    public abstract class SimInstanceParameterCollection : IEnumerable<KeyValuePair<SimBaseParameter, object>>
    {
        private Dictionary<SimBaseParameter, object> data;
        /// <summary>
        /// Stores the instance to which this collection belongs
        /// </summary>
        protected SimComponentInstance Owner { get; }

        /// <summary>
        /// Initializes a new instance of the SimInstanceParameterCollection class
        /// </summary>
        /// <param name="owner">The instance to which this collection belongs</param>
        protected SimInstanceParameterCollection(SimComponentInstance owner)
        {
            data = new Dictionary<SimBaseParameter, object>();
            this.Owner = owner;
        }

        /// <summary>
        /// Adds a new parameter to the collection.
        /// </summary>
        /// <param name="parameter">The new parameter. May not be contained in the collection before.</param>
        /// <param name="value">The initial value</param>
        internal void Add(SimBaseParameter parameter, object value)
        {
            if (parameter == null)
                throw new ArgumentNullException(nameof(parameter));

            if (!data.ContainsKey(parameter))
                this.data.Add(parameter, value);
        }



        /// <summary>
        /// Removes a parameter from the collection
        /// </summary>
        /// <param name="parameter">The parameter to remove</param>
        /// <returns>True when the parameter was in the collection, otherwise False</returns>
        internal bool Remove(SimBaseParameter parameter)
        {
            if (parameter == null)
                throw new ArgumentNullException(nameof(parameter));

            return data.Remove(parameter);
        }

        /// <summary>
        /// Tests whether a parameter is part of the collection
        /// </summary>
        /// <param name="parameter">The parameter to test</param>
        /// <returns>True when the parameter is part of the collection, otherwise False</returns>
        public bool Contains(SimBaseParameter parameter)
        {
            if (parameter == null)
                throw new ArgumentNullException(nameof(parameter));

            return data.ContainsKey(parameter);
        }

        /// <summary>
        /// Accesses the collection and either gets or sets the value of a parameter.
        /// This method may not be used to add new parameters to the collection and will throw a KeyNotFoundException
        /// when the parameter isn't found.
        /// </summary>
        /// <param name="key">The parameter to access</param>
        /// <returns>The instance value of the parameter</returns>
        public object this[SimBaseParameter key]
        {
            get
            {
                if (key == null)
                    throw new ArgumentNullException(nameof(key));

                return data[key];
            }
            set
            {
                SetWithoutNotify(key, value);
                NotifyGeometryExchange(key);
            }
        }

        /// <summary>
        /// Returns the value of a parameter if the parameter is contained in the collection
        /// </summary>
        /// <param name="key">The parameter to query</param>
        /// <param name="value">The resulting value. The value is undefined when the method returns false</param>
        /// <returns>True when the parameter is part of the collection, otherwise False</returns>
        public bool TryGetValue(SimBaseParameter key, out dynamic value)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            return data.TryGetValue(key, out value);
        }


        /// <summary>
        /// Returns the value of a parameter if the parameter is contained in the collection
        /// </summary>
        /// <param name="key">The parameter to query</param>
        /// <param name="value">The resulting value. The value is undefined when the method returns false</param>
        /// <returns>True when the parameter is part of the collection, otherwise False</returns>
        public bool TryGetValue<T>(SimBaseParameter<T> key, out T value)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            if (data.TryGetValue(key, out var tempValue))
            {
                value = ((T)tempValue);
                return true;
            }
            else
            {
                value = default(T);
            }
            return false;
        }


        /// <summary>
        /// Returns the number of elements in the collection
        /// </summary>
        public int Count { get { return data.Count; } }

        /// <summary>
        /// Returns the key collection
        /// </summary>
        public IEnumerable<SimBaseParameter> Keys { get { return data.Keys; } }

        #region IEnumerable

        /// <inheritdoc />
        public IEnumerator<KeyValuePair<SimBaseParameter, dynamic>> GetEnumerator()
        {
            return data.GetEnumerator();
        }

        /// <summary>
        /// Returns the values based on T type
        /// </summary>
        /// <typeparam name="T">The type of literal</typeparam>
        /// <returns>List of KeyValuePair containing the parameter  as the key and the literal with type T as value</returns>
        public List<KeyValuePair<SimBaseParameter, T>> GetRecords<T>()
        {
            var returnList = new List<KeyValuePair<SimBaseParameter, T>>();
            foreach (var item in data)
            {
                if (item is KeyValuePair<SimBaseParameter, T> castedItem)
                {
                    returnList.Add(castedItem);
                }
            }
            return returnList;
        }

        /// <summary>
        /// Returns the values based on T type, only from Parameters type K
        /// </summary>
        /// <typeparam name="K">The type of the SimBaseParameter</typeparam>
        /// <typeparam name="T">The type of literal</typeparam>
        /// <returns>List of KeyValuePair containing the parameter type K as the key and the literal with type T as value</returns>
        public List<KeyValuePair<K, T>> GetRecords<K, T>() where K : SimBaseParameter
        {
            var returnList = new List<KeyValuePair<K, T>>();
            foreach (var item in data)
            {
                if (item is KeyValuePair<K, T> castedItem)
                {
                    returnList.Add(castedItem);
                }
            }
            return returnList;
        }


        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator()
        {
            return data.GetEnumerator();
        }

        #endregion

        /// <summary>
        /// Notifies the instance that a write access has happened
        /// </summary>
        protected abstract void NotifyWriteAccess();

        /// <summary>
        /// Notifies the Geometry Exchange that a parameter has been modified
        /// </summary>
        /// <param name="parameter">The modified parameter</param>
        protected abstract void NotifyGeometryExchange(SimBaseParameter parameter);

        internal void SetWithoutNotify(SimBaseParameter key, object value)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            this.NotifyWriteAccess();

            if (!data.ContainsKey(key))
                throw new KeyNotFoundException("operator may not be used to add additional entries");

            data[key] = value;
        }
    }

    public partial class SimComponentInstance
    {
        /// <summary>
        /// Collection for persistent instance parameters.
        /// </summary>
        public class SimInstanceParameterCollectionPersistent : SimInstanceParameterCollection
        {
            /// <summary>
            /// Initializes a new instance of the SimInstanceParameterCollectionPersistent class
            /// </summary>
            /// <param name="owner">The instance to which this collection belongs</param>
            public SimInstanceParameterCollectionPersistent(SimComponentInstance owner) : base(owner) { }

            /// <inheritdoc />
            protected override void NotifyWriteAccess()
            {
                Owner.NotifyWriteAccess();
            }
            /// <inheritdoc />
            protected override void NotifyGeometryExchange(SimBaseParameter parameter)
            {
                if (Owner.Component != null && Owner.Component.Factory != null)
                    Owner.Component.Factory.ProjectData.ComponentGeometryExchange.OnParameterValueChanged(parameter, Owner);
            }
        }

        /// <summary>
        /// Collection for temporary instance parameters
        /// </summary>
        public class SimInstanceParameterCollectionTemporary : SimInstanceParameterCollection
        {
            /// <summary>
            /// Initializes a new instance of the SimInstanceParameterCollectionTemporary class
            /// </summary>
            /// <param name="owner">The instance to which this collection belongs</param>
            public SimInstanceParameterCollectionTemporary(SimComponentInstance owner) : base(owner) { }

            /// <inheritdoc />
            protected override void NotifyWriteAccess()
            {
                //Do nothing. Temporary parameters are exempt from access management
            }
            /// <inheritdoc />
            protected override void NotifyGeometryExchange(SimBaseParameter parameter)
            {
                //Do nothing
            }
        }
    }
}
