using SIMULTAN.Exceptions;
using SIMULTAN.Utils;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace SIMULTAN.Data.Components
{
    public partial class SimCalculation
    {
        /// <summary>
        /// Base class for input/return parameter collections in a calculation
        /// </summary>
        public abstract class BaseCalculationParameterCollections : INotifyCollectionChanged, IEnumerable<KeyValuePair<string, SimDoubleParameter>>
        {
            /// <summary>
            /// The calculation this collection belongs to
            /// </summary>
            public SimCalculation Owner { get; }
            /// <summary>
            /// The data in this collection
            /// </summary>
            protected Dictionary<string, CalculationParameterReference> Data { get; }

            /// <summary>
            /// Initializes a new instance of the BaseCalculationParameterCollections class
            /// </summary>
            /// <param name="owner">The calculation this collection belongs to</param>
            public BaseCalculationParameterCollections(SimCalculation owner)
            {
                this.Owner = owner;
                this.Data = new Dictionary<string, CalculationParameterReference>();
            }
            /// <summary>
            /// Initializes a new instance of the BaseCalculationParameterCollections class
            /// </summary>
            /// <param name="owner">The calculation this collection belongs to</param>
            /// <param name="data">Initial data</param>
            public BaseCalculationParameterCollections(SimCalculation owner, IEnumerable<KeyValuePair<string, SimDoubleParameter>> data)
                : this(owner)
            {
                foreach (var item in data)
                {
                    var referenceItem = new CalculationParameterReference(this, item.Value);
                    this.Data.Add(item.Key, referenceItem);
                    referenceItem.RegisterReferences();
                }
            }

            /// <summary>
            /// Finalizer for this class. Disposes all entries
            /// </summary>
            ~BaseCalculationParameterCollections()
            {
                foreach (var item in Data)
                    item.Value.Dispose();
            }

            #region Access

            /// <summary>
            /// Returns the parameter for a given variable symbol
            /// </summary>
            /// <param name="parameter">The variable</param>
            /// <returns></returns>
            public SimDoubleParameter this[string parameter]
            {
                get { return Data[parameter].Parameter; }
                set
                {
                    if (this.Data.TryGetValue(parameter, out var oldValue))
                    {
                        try
                        {
                            CheckParameter(value);
                            Owner.NotifyWriteAccess();

                            var oldParameter = Data[parameter];
                            var newParameter = new CalculationParameterReference(this, value);
                            Data[parameter] = newParameter;

                            oldParameter?.UnregisterReferences();
                            newParameter.RegisterReferences();

                            Owner.UpdateState();
                            Owner.NotifyComponentReordering();

                            NotifyCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace,
                                new KeyValuePair<string, SimDoubleParameter>(parameter, value),
                                new KeyValuePair<string, SimDoubleParameter>(parameter, oldValue.Parameter)));

                            if (oldValue != null)
                                oldValue.Dispose();

                            Owner.NotifyChanged();
                        }
                        catch (InvalidStateException e)
                        {
                            throw new InvalidStateException("Unable to add Parameter to Calculation", e);
                        }
                    }
                    else
                        throw new KeyNotFoundException();
                }
            }

            /// <summary>
            /// Checks if the parameter is a valid parameter for this collection. Has to be implemented by derived classes.
            /// Has to throw an InvalidStateException when the parameter does not satisfy the requirements.
            /// </summary>
            /// <param name="parameter">The parameter to test</param>
            internal abstract void CheckParameter(SimDoubleParameter parameter);

            /// <summary>
            /// Returns True when the variable symbol is present in the collection
            /// </summary>
            /// <param name="parameter">The variable symbol</param>
            /// <returns>Returns True when the parameter is in the list of symbols, otherwise False</returns>
            public bool ContainsKey(string parameter)
            {
                return Data.ContainsKey(parameter);
            }
            /// <summary>
            /// Returns True when the parameter is part of the collection (e.g., when it is bound to a variable symbol)
            /// </summary>
            /// <param name="parameter">The parameter</param>
            /// <returns>True when the parameter is part of the collection, otherwise False</returns>
            public bool ContainsValue(SimDoubleParameter parameter)
            {
                return Data.Values.Any(x => x.Parameter == parameter);
            }
            /// <summary>
            /// Returns the number of elements in the collection
            /// </summary>
            public int Count => Data.Count;

            /// <summary>
            /// Returns True when the variable symbol is part of the collection. Also returns the parameter bound to the variable in an out parameter
            /// </summary>
            /// <param name="key">The variable symbol</param>
            /// <param name="parameter">Out parameter for the resulting parameter. Undefined when the method returns False</param>
            /// <returns>True when the symbol is contained in the collection, otherwise False</returns>
            public bool TryGetValue(string key, out SimDoubleParameter parameter)
            {
                if (Data.TryGetValue(key, out var item))
                {
                    parameter = item.Parameter;
                    return true;
                }
                else
                {
                    parameter = null;
                    return false;
                }
            }

            /// <summary>
            /// Converts the collection into a dictionary.
            /// </summary>
            /// <returns>A dictionionary where key is the variable symbol and the value contains the parameter bound to this symbol (may be null)</returns>
            public Dictionary<string, SimDoubleParameter> ToDictionary()
            {
                Dictionary<string, SimDoubleParameter> dict = new Dictionary<string, SimDoubleParameter>();

                foreach (var entry in Data)
                    dict.Add(entry.Key, entry.Value?.Parameter);

                return dict;
            }


            internal void AddInternal(string key, SimDoubleParameter parameter)
            {
                try
                {
                    CheckParameter(parameter);
                    Owner.NotifyWriteAccess();

                    var newItem = new CalculationParameterReference(this, parameter);
                    Data.Add(key, newItem);
                    newItem.RegisterReferences();

                    Owner.UpdateState();
                    Owner.NotifyComponentReordering();

                    NotifyCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add,
                        new KeyValuePair<string, SimDoubleParameter>(key, parameter)));
                    Owner.NotifyChanged();
                }
                catch (InvalidStateException e) { throw new InvalidStateException("Unable to add parameter", e); }
            }

            internal void AddRangeInternal(IEnumerable<KeyValuePair<string, SimDoubleParameter>> values)
            {
                try
                {
                    foreach (var item in values)
                        CheckParameter(item.Value);

                    Owner.NotifyWriteAccess();

                    foreach (var item in values)
                    {
                        var newItem = new CalculationParameterReference(this, item.Value);
                        Data.Add(item.Key, newItem);
                        newItem.RegisterReferences();
                    }

                    Owner.UpdateState();
                    Owner.NotifyComponentReordering();

                    NotifyCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, values));
                    Owner.NotifyChanged();
                }
                catch (InvalidStateException e)
                {
                    throw new InvalidStateException("Unable to add parameters", e);
                }
            }

            internal void RemoveInternal(string key)
            {
                if (Data.TryGetValue(key, out var oldValue))
                {
                    Owner.NotifyWriteAccess();
                    Data.Remove(key);
                    oldValue.UnregisterReferences();

                    Owner.UpdateState();
                    Owner.NotifyComponentReordering();

                    NotifyCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove,
                        new KeyValuePair<string, SimDoubleParameter>(key, oldValue.Parameter)));
                    oldValue.Dispose();
                    Owner.NotifyChanged();
                }
            }

            internal void ClearInternal()
            {
                Owner.NotifyWriteAccess();

                var removeItems = Data.Values.ToList();

                Data.ForEach(x => x.Value.Dispose());
                Data.Clear();

                removeItems.ForEach(x => x.UnregisterReferences());

                NotifyCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                Owner.NotifyChanged();
            }

            internal string GetKey(CalculationParameterReference value)
            {
                return Data.FirstOrDefault(x => x.Value == value).Key;
            }

            /// <summary>
            /// Returns the meta data for a given symbol.
            /// </summary>
            /// <param name="key">The variable symbol</param>
            /// <returns>The meta data for this symbol, or Null when the symbol is not part of the collection</returns>
            public CalculationParameterMetaData GetMetaData(string key)
            {
                if (this.Data.TryGetValue(key, out var data))
                    return data.MetaData;
                return null;
            }

            internal IEnumerable<KeyValuePair<string, CalculationParameterMetaData>> MetaData
            {
                get
                {
                    foreach (var entry in Data)
                        yield return new KeyValuePair<string, CalculationParameterMetaData>(entry.Key, entry.Value.MetaData);
                }
            }

            #endregion

            #region INotifyCollectionChanged

            /// <inheritdoc />
            public event NotifyCollectionChangedEventHandler CollectionChanged;
            /// <summary>
            /// Invokes the CollectionChanged event
            /// </summary>
            /// <param name="args">The arguments for the event</param>
            protected void NotifyCollectionChanged(NotifyCollectionChangedEventArgs args)
            {
                this.CollectionChanged?.Invoke(this, args);
            }

            #endregion

            #region IEnumerable

            /// <summary>
            /// Enumerator for a parameter collection
            /// </summary>
            public sealed class Enumerator : IEnumerator<KeyValuePair<string, SimDoubleParameter>>
            {
                private IEnumerator<KeyValuePair<string, CalculationParameterReference>> enumerator;
                private KeyValuePair<string, SimDoubleParameter> current;

                /// <summary>
                /// The element this iterator currently points to
                /// </summary>
                public KeyValuePair<string, SimDoubleParameter> Current => current;
                /// <inheritdoc />
                object IEnumerator.Current => Current;

                /// <summary>
                /// Initializes a new instance of the Enumerator class
                /// </summary>
                /// <param name="enumerator">The internal enumerator of the collection</param>
                public Enumerator(IEnumerator<KeyValuePair<string, CalculationParameterReference>> enumerator)
                {
                    this.enumerator = enumerator;
                    UpdateCurrent();
                }

                /// <inheritdoc />
                public void Dispose()
                {
                    enumerator.Dispose();
                    current = new KeyValuePair<string, SimDoubleParameter>();
                }
                /// <inheritdoc />
                public bool MoveNext()
                {
                    var next = enumerator.MoveNext();
                    if (next)
                        UpdateCurrent();
                    else
                        current = new KeyValuePair<string, SimDoubleParameter>();

                    return next;
                }
                /// <inheritdoc />
                public void Reset()
                {
                    enumerator.Reset();
                    UpdateCurrent();
                }

                private void UpdateCurrent()
                {
                    current = new KeyValuePair<string, SimDoubleParameter>(enumerator.Current.Key, enumerator.Current.Value?.Parameter);
                }
            }

            /// <inheritdoc />
            public IEnumerator<KeyValuePair<string, SimDoubleParameter>> GetEnumerator()
            {
                return new Enumerator(Data.GetEnumerator());
            }
            /// <inheritdoc />
            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            #endregion

            /// <summary>
            /// Registers the calculation in the affected parameter's <see cref="SimBaseNumericParameter{T}.ReferencingCalculations"/> collection
            /// </summary>
            internal void RegisterReferences()
            {
                foreach (var item in Data.Values)
                    item.RegisterReferences();
            }
            /// <summary>
            /// Removes the calculation from the affected parameter's <see cref="SimBaseNumericParameter{T}.ReferencingCalculations"/> collection
            /// </summary>
            internal void UnregisterReferences()
            {
                foreach (var item in Data.Values)
                    item.UnregisterReferences();
            }
        }

        /// <summary>
        /// Specialized collection for input parameters of a calculation
        /// </summary>
        public sealed class SimCalculationInputParameterCollection : BaseCalculationParameterCollections
        {
            /// <summary>
            /// Initializes a new instance of the InputParameterCollection class
            /// </summary>
            /// <param name="calculation">The calculation this collection belongs to</param>
            public SimCalculationInputParameterCollection(SimCalculation calculation) : base(calculation) { }
            /// <summary>
            /// Initializes a new instance of the InputParameterCollection class
            /// </summary>
            /// <param name="calculation">The calculation this collection belongs to</param>
            /// <param name="data">The initial entries for the collection</param>
            public SimCalculationInputParameterCollection(SimCalculation calculation, IEnumerable<KeyValuePair<string, SimDoubleParameter>> data) : base(calculation, data) { }

            internal override void CheckParameter(SimDoubleParameter parameter)
            {
                if (parameter != null && parameter.Propagation == SimInfoFlow.Output)
                    throw new InvalidStateException("Parameter Propagation conflicts with use in Calculation");
            }
        }

        /// <summary>
        /// Specialized collection for output parameters of a calculation
        /// </summary>
        public sealed class SimCalculationOutputParameterCollection : BaseCalculationParameterCollections
        {
            /// <summary>
            /// Initializes a new instance of the OutputParameterCollection class
            /// </summary>
            /// <param name="calculation">The calculation this collection belongs to</param>
            public SimCalculationOutputParameterCollection(SimCalculation calculation) : base(calculation) { }
            /// <summary>
            /// Initializes a new instance of the OutputParameterCollection class
            /// </summary>
            /// <param name="calculation">The calculation this collection belongs to</param>
            /// <param name="data">The initial entries for the collection</param>
            public SimCalculationOutputParameterCollection(SimCalculation calculation, IEnumerable<KeyValuePair<string, SimDoubleParameter>> data) : base(calculation, data) { }

            internal override void CheckParameter(SimDoubleParameter parameter)
            {
                if (parameter != null && parameter.Propagation != SimInfoFlow.Output && parameter.Propagation != SimInfoFlow.Mixed)
                    throw new InvalidStateException("Parameter Propagation conflicts with use in Calculation");
            }

            #region Access

            /// <summary>
            /// Adds a new entry to the collection
            /// </summary>
            /// <param name="key">Variable symbol</param>
            /// <param name="parameter">The parameter which is bound to the symbol. May be null when no parameter is bound</param>
            public void Add(string key, SimDoubleParameter parameter)
            {
                this.AddInternal(key, parameter);
            }
            /// <summary>
            /// Adds a number of entries ot the collection
            /// </summary>
            /// <param name="values">The values to add. 
            /// Key: Variable symbol
            /// Value: The parameter which is bound to the symbol. May be null when no parameter is bound</param>
            public void AddRange(IEnumerable<KeyValuePair<string, SimDoubleParameter>> values)
            {
                this.AddRangeInternal(values);
            }
            /// <summary>
            /// Removes an entry from the collection
            /// </summary>
            /// <param name="key">The variable symbol to remove</param>
            public void Remove(string key)
            {
                this.RemoveInternal(key);
            }
            /// <summary>
            /// Clears the collection
            /// </summary>
            public void Clear()
            {
                this.ClearInternal();
            }

            #endregion
        }
    }
}
