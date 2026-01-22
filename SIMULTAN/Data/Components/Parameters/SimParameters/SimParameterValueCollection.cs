using MathNet.Numerics;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.Components
{
    /// <summary>
    /// Interface for a parameter value collection for list parameters
    /// </summary>
    public interface ISimParameterValueCollection
    {
        /// <summary>
        /// Clones the value collection with the given parameter
        /// </summary>
        /// <param name="parameter">The parameter</param>
        /// <returns>A clone of the collection for the given parameter</returns>
        object CloneWith(SimBaseParameter parameter);
        /// <summary>
        /// Clones the value collection with the given instance
        /// </summary>
        /// <param name="instance">The instance</param>
        /// <returns>A clone of the collection for the given instance</returns>
        object CloneWith(SimComponentInstance instance);
    }

    /// <summary>
    /// A value collection for <see cref="SimBaseListParameter{T}"/>
    /// </summary>
    /// <typeparam name="T">The type of the collection elements</typeparam>
    public class SimParameterValueCollection<T> : ObservableCollection<T>, ISimParameterValueCollection, ICloneable
    {
        /// <summary>
        /// The parameter this collection belongs to.
        /// Can only belong to a parameter or an instance.
        /// Can only belong to a single parameter. Otherwise clone the value.
        /// </summary>
        public SimBaseListParameter<T> Parameter
        {
            get => parameter;
            internal set
            {
                if (instance != null)
                    throw new NotSupportedException("SimParameterValueCollection cannot belong to a parameter and an Instance");
                if (parameter != null && value != this.parameter)
                {
                    throw new NotSupportedException("SimParameterValueCollection already belongs to a different parameter");
                }
                parameter = value;
            }
        }
        private SimBaseListParameter<T> parameter;

        /// <summary>
        /// The instance this collection belongs to.
        /// Can only belong to a parameter or an instance.
        /// Can only belong to a single instance. Otherwise clone the value.
        /// </summary>
        public SimComponentInstance Instance
        {
            get => instance;
            set
            {
                if (parameter != null)
                    throw new NotSupportedException("SimParameterValueCollection cannot belong to a parameter and an Instance");
                if (instance != null && value != this.instance)
                {
                    throw new NotSupportedException("SimParameterValueCollection already belongs to a different parameter");
                }
                instance = value;
            }
        }
        private SimComponentInstance instance;

        internal bool HandleInstanceCollectionChanged { get; set; } = true;

        /// <summary>
        /// Creates a new empty <see cref="SimParameterValueCollection{T}"/>
        /// </summary>
        public SimParameterValueCollection()
        {
        }

        /// <summary>
        /// Creates a new <see cref="SimParameterValueCollection{T}"/> with the contents of the given collection
        /// </summary>
        /// <param name="collection">The data to fill this collection with</param>
        public SimParameterValueCollection(IEnumerable<T> collection) : base(collection)
        {
        }

        /// <summary>
        /// Creates a new <see cref="SimParameterValueCollection{T}"/> with the contents of the given collection
        /// </summary>
        /// <param name="list">The data to fill this collection with</param>
        public SimParameterValueCollection(List<T> list) : base(list)
        {
        }

        /// <inheritdoc/>
        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            base.OnCollectionChanged(e);
            if (Parameter != null)
            {
                Parameter.NotifyCollectionChanged();
                Parameter.Component?.Parameters.OnListParameterCollectionChanged(Parameter, e);
            }
            else if (Instance != null && HandleInstanceCollectionChanged)
            {
                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                    case NotifyCollectionChangedAction.Remove:
                    case NotifyCollectionChangedAction.Reset:
                        throw new InvalidOperationException("Cannot Add, Remove or Reset a instance parameter value collection");
                }
            }
        }

        /// <inheritdoc/>
        public object CloneWith(SimBaseParameter parameter)
        {
            var clone = new SimParameterValueCollection<T>(this);
            if (parameter is not SimBaseListParameter<T> listParameter)
                throw new NotSupportedException("Clone with wrong parameter type");
            clone.Parameter = listParameter;
            return clone;
        }

        /// <inheritdoc/>
        public object CloneWith(SimComponentInstance instance)
        {
            var clone = new SimParameterValueCollection<T>(this);
            clone.Instance = instance;
            return clone;
        }

        /// <inheritdoc/>
        public object Clone()
        {
            return new SimParameterValueCollection<T>(this);
        }
    }
}
