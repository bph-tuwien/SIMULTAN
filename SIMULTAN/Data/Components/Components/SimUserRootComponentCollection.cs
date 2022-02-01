using SIMULTAN.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;

namespace SIMULTAN.Data.Components
{

    /// <summary>
    /// A collection of components that tracks changes of parent components, so it correctly handles component CRUD in the main component list.
    /// </summary>
    public class SimUserRootComponentCollection : IList<SimComponent>, INotifyCollectionChanged
    {

        /// <summary>
        /// Custom enumerator to decapsulate the component refs.
        /// </summary>
        private class CompEnumerator : IEnumerator<SimComponent>
        {

            private IEnumerator<SimUserComponentRef> enumerator;

            public CompEnumerator(IEnumerator<SimUserComponentRef> enumerator)
            {
                this.enumerator = enumerator;
            }

            public SimComponent Current
            {
                get
                {
                    return enumerator.Current.Component;
                }
            }

            object IEnumerator.Current
            {
                get
                {
                    return enumerator.Current.Component;
                }
            }

            public void Dispose()
            {
                enumerator.Dispose();
            }

            public bool MoveNext()
            {
                return enumerator.MoveNext();
            }

            public void Reset()
            {
                enumerator.Reset();
            }
        }
        internal ObservableCollection<SimUserComponentRef> RootComponents { get; private set; }

        /// <inheritdoc/>
        public SimComponent this[int index]
        {
            get
            {
                return RootComponents[index].Component;
            }

            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException();
                }
                if (owner.Factory != null)
                {
                    RootComponents[index].DetachEvents();
                }
                RootComponents[index] = new SimUserComponentRef(value, RootComponents);
                if (owner.Factory != null)
                {
                    RootComponents[index].AttachEvents();
                }
            }
        }


        /// <inheritdoc/>
        public int Count
        {
            get
            {
                return RootComponents.Count;
            }
        }

        /// <inheritdoc/>
        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        /// <inheritdoc/>
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        private SimUserComponentList owner;

        #region .CTOR
        /// <summary>
        /// Creates a new UserRootComponentCollection
        /// </summary>
        public SimUserRootComponentCollection(SimUserComponentList owner)
        {
            if (owner == null)
            {
                throw new ArgumentNullException(nameof(owner));
            }
            this.RootComponents = new ObservableCollection<SimUserComponentRef>();
            RootComponents.CollectionChanged += RootComponents_CollectionChanged;
            this.owner = owner;
        }

        /// <summary>
        /// Creates a new UserRootComponentCollection
        /// </summary>
        /// <param name="owner">The owner of this collection.</param>
        /// <param name="components">Starting components to initialize it with</param>
        public SimUserRootComponentCollection(SimUserComponentList owner, ObservableCollection<SimComponent> components) : this(owner)
        {
            if (components == null)
            {
                throw new ArgumentNullException(nameof(components));
            }

            foreach (var comp in components)
            {
                Add(comp);
            }
        }

        private void RootComponents_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            List<SimComponent> newItems = null;
            List<SimComponent> oldItems = null;

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    if (e.NewItems != null)
                    {
                        newItems = new List<SimComponent>();
                        foreach (var comp in e.NewItems)
                        {
                            newItems.Add(((SimUserComponentRef)comp).Component);
                        }
                        CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, newItems, e.NewStartingIndex));
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    if (e.OldItems != null)
                    {
                        oldItems = new List<SimComponent>();
                        foreach (var comp in e.OldItems)
                        {
                            var compref = (SimUserComponentRef)comp;
                            oldItems.Add(compref.Component);
                        }
                        CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, oldItems));
                    }
                    break;
                case NotifyCollectionChangedAction.Replace:
                    if (e.NewItems != null && e.OldItems != null)
                    {
                        newItems = new List<SimComponent>();
                        foreach (var comp in e.NewItems)
                        {
                            newItems.Add(((SimUserComponentRef)comp).Component);
                        }
                        oldItems = new List<SimComponent>();
                        foreach (var comp in e.OldItems)
                        {
                            var compref = (SimUserComponentRef)comp;
                            oldItems.Add(compref.Component);
                        }
                        CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, newItems, oldItems, e.NewStartingIndex));
                    }
                    break;
                case NotifyCollectionChangedAction.Reset:
                    CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
                    break;
                case NotifyCollectionChangedAction.Move:
                    if (e.NewItems != null)
                    {
                        newItems = new List<SimComponent>();
                        foreach (var comp in e.NewItems)
                        {
                            newItems.Add(((SimUserComponentRef)comp).Component);
                        }
                        CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Move, newItems, e.NewStartingIndex, e.OldStartingIndex));
                    }
                    break;
            }
        }

        #endregion


        /// <inheritdoc/>
        public void Add(SimComponent item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }
            var compref = new SimUserComponentRef(item, RootComponents);
            if (owner.Factory != null)
                compref.AttachEvents();
            RootComponents.Add(compref);
        }

        /// <inheritdoc/>
        public void Clear()
        {
            if (owner.Factory != null)
            {
                foreach (var compref in RootComponents)
                {
                    compref.DetachEvents();
                }
            }
            RootComponents.Clear();
        }

        /// <inheritdoc/>
        public bool Contains(SimComponent item)
        {
            return RootComponents.Any(x => x.Component == item);
        }

        /// <summary>
        /// Not implemented.
        /// </summary>
        /// <param name="array">-</param>
        /// <param name="arrayIndex">-</param>
        public void CopyTo(SimComponent[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public IEnumerator<SimComponent> GetEnumerator()
        {
            return new CompEnumerator(RootComponents.GetEnumerator());
        }

        /// <inheritdoc/>
        public int IndexOf(SimComponent item)
        {
            for (int i = 0; i < RootComponents.Count; i++)
            {
                if (RootComponents[i].Component == item)
                    return i;
            }
            return -1;
        }


        /// <inheritdoc/>
        public void Insert(int index, SimComponent item)
        {
            var compref = new SimUserComponentRef(item, RootComponents);
            if (owner.Factory != null)
            {
                compref.AttachEvents();
            }
            RootComponents.Insert(index, compref);
        }

        /// <inheritdoc/>
        public bool Remove(SimComponent item)
        {
            var comindex = RootComponents.FindIndex(x => x.Component == item);
            if (comindex >= 0)
            {
                if (owner.Factory != null)
                {
                    RootComponents[comindex].DetachEvents();
                }
                RootComponents.RemoveAt(comindex);
                return true;
            }
            return false;
        }

        /// <inheritdoc/>
        public void RemoveAt(int index)
        {
            if (owner.Factory != null)
            {
                RootComponents[index].DetachEvents();
            }
            RootComponents.RemoveAt(index);
        }

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return new CompEnumerator(RootComponents.GetEnumerator());
        }
    }
}
