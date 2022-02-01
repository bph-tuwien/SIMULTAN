using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Utils.Collections
{
    /// <summary>
    /// Observable collection that allows suppressing the CollectionChanged event w/o side-effects. The default state does not suppress events.
	/// The Clear method sends a remove event in addition to the reset event.
    /// </summary>
    public class ElectivelyObservableCollection<T> : ObservableCollection<T>, IReadOnlyObservableCollection<T>
    {
        private bool suppress_notification = false;
        /// <summary>
        /// When true, the ElectiveCollectionChanged event is not emitted at all. The CollectionChanged is emitted always.
        /// </summary>
        public bool SuppressNotification
        {
            get { return this.suppress_notification; }
            set
            {
                if (this.suppress_notification != value)
                {
                    this.suppress_notification = value;
                }
            }
        }

        /// <summary>
        /// This event is emitted only when <see cref="SuppressNotification"/> is set to True.
        /// </summary>
        public event NotifyCollectionChangedEventHandler ElectiveCollectionChanged;

        /// <inheritdoc />
        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            base.OnCollectionChanged(e);
            if (!this.SuppressNotification)
            {
                this.ElectiveCollectionChanged?.Invoke(this, e);
            }
        }

        /// <inheritdoc />
        protected override void ClearItems()
        {
            List<T> removed = new List<T>(this);
            base.ClearItems();
            foreach (var item in removed)
                base.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item));
        }

        /// <summary>
        /// Initializes an empty ElectivelyObservableCollection.
        /// </summary>
        public ElectivelyObservableCollection()
            : base()
        { }

        /// <summary>
        /// Initializes a ElectivelyObservableCollection with the given collection.
        /// </summary>
        /// <param name="collection"></param>
        public ElectivelyObservableCollection(IEnumerable<T> collection)
            : base(collection)
        { }
    }
}
