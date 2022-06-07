using SIMULTAN.Data.Components;
using SIMULTAN.Serializer.DXF;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.Components
{
    /// <summary>
    /// Stores the instances values for parameters. This is an associative container which uses the Parameter itself as a key.
    /// </summary>
    [DXFSerializerTypeNameAttribute("ParameterStructure.Instances.InstanceParameterCollection")]
    public abstract class SimInstanceParameterCollection : IEnumerable<KeyValuePair<SimParameter, double>>
    {
        private Dictionary<SimParameter, double> data;
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
            data = new Dictionary<SimParameter, double>();
            this.Owner = owner;
        }

        /// <summary>
        /// Adds a new parameter to the collection.
        /// </summary>
        /// <param name="parameter">The new parameter. May not be contained in the collection before.</param>
        /// <param name="value">The initial value</param>
        internal void Add(SimParameter parameter, double value)
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
        internal bool Remove(SimParameter parameter)
        {
            if (parameter == null)
                throw new ArgumentNullException(nameof(parameter));

            return data.Remove(parameter);
        }

        /// <summary>
        /// Tests whether a parameter is part of the collection
        /// </summary>
        /// <param name="parameter">The parametr to test</param>
        /// <returns>True when the parameter is part of the collection, otherwise False</returns>
        public bool Contains(SimParameter parameter)
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
        public double this[SimParameter key]
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
        public bool TryGetValue(SimParameter key, out double value)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            return data.TryGetValue(key, out value);
        }

        /// <summary>
        /// Returns the number of elements in the collection
        /// </summary>
        public int Count { get { return data.Count; } }

        /// <summary>
        /// Returns the key collection
        /// </summary>
        public IEnumerable<SimParameter> Keys { get { return data.Keys; } }

        #region IEnumerable

        /// <inheritdoc />
        public IEnumerator<KeyValuePair<SimParameter, double>> GetEnumerator()
        {
            return data.GetEnumerator();
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
        protected abstract void NotifyGeometryExchange(SimParameter parameter);
    
        internal void SetWithoutNotify(SimParameter key, double value)
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
            protected override void NotifyGeometryExchange(SimParameter parameter)
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
            protected override void NotifyGeometryExchange(SimParameter parameter)
            {
                //Do nothing
            }
        }
    }
}
