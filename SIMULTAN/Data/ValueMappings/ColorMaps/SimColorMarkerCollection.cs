using SIMULTAN.Data.SitePlanner;
using SIMULTAN.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.ValueMappings
{
    /// <summary>
    /// Stores a number of <see cref="SimColorMarker"/> inside a <see cref="SimColorMap"/>
    /// </summary>
    public class SimColorMarkerCollection : ObservableCollection<SimColorMarker>
    {
        /// <summary>
        /// The ColorMap to which this collection belongs
        /// </summary>
        public SimColorMap Owner { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SimColorMarkerCollection"/> class
        /// </summary>
        /// <param name="owner">The ColorMap to which this collection belongs</param>
        /// <exception cref="ArgumentNullException"></exception>
        public SimColorMarkerCollection(SimColorMap owner)
        {
            if (owner == null)
                throw new ArgumentNullException(nameof(owner));

            this.Owner = owner;
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="SimColorMarkerCollection"/> class
        /// </summary>
        /// <param name="owner">The ColorMap to which this collection belongs</param>
        /// <param name="marker">Initial set of <see cref="SimColorMarker"/>. The markers need to be sorted by value.</param>
        public SimColorMarkerCollection(SimColorMap owner, IEnumerable<SimColorMarker> marker) : base(marker)
        {
            if (owner == null)
                throw new ArgumentNullException(nameof(owner));

            if (marker != null)
            {
                foreach (var m in marker)
                    m.Owner = this;
            }

            this.Owner = owner;
        }

        #region Collection Implementation

        /// <inheritdoc />
        protected override void InsertItem(int index, SimColorMarker item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            //Find real index
            int idx = 0;
            while (idx < this.Count() && this[idx].Value < item.Value)
                idx++;

            item.Owner = this;
            base.InsertItem(idx, item);
            this.Owner.NotifyMappingChanged();
        }
        /// <inheritdoc />
        protected override void ClearItems()
        {
            this.Items.ForEach(x => x.Owner = null);
            base.ClearItems();
            this.Owner.NotifyMappingChanged();
        }
        /// <inheritdoc />
        protected override void RemoveItem(int index)
        {
            var oldItem = this[index];
            base.RemoveItem(index);

            oldItem.Owner = null;
            Owner.NotifyMappingChanged();
        }
        /// <inheritdoc />
        protected override void SetItem(int index, SimColorMarker item)
        {
            throw new NotSupportedException("This collection does not support this operation");
        }
        /// <inheritdoc />
        protected override void MoveItem(int oldIndex, int newIndex)
        {
            throw new NotSupportedException("This collection does not support this operation");
        }

        #endregion
    }
}
