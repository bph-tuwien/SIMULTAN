using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.SitePlanner
{
    /// <summary>
    /// Stores a list of <see cref="SitePlannerProject"/> inside the <see cref="SitePlannerManager"/>
    /// </summary>
    public class SitePlannerProjectsCollection : ObservableCollection<SitePlannerProject>
    {
        private SitePlannerManager owner;

        /// <summary>
        /// Initializes a new instance of the <see cref="SitePlannerBuildingCollection"/> class
        /// </summary>
        /// <param name="owner">The <see cref="SitePlannerManager"/> this collection belongs to</param>
        public SitePlannerProjectsCollection(SitePlannerManager owner)
        {
            this.owner = owner;
        }

        #region Collection Implementation

        /// <inheritdoc />
        protected override void InsertItem(int index, SitePlannerProject item)
        {
            item.Factory = owner;
            base.InsertItem(index, item);
        }
        /// <inheritdoc />
        protected override void RemoveItem(int index)
        {
            var oldItem = this[index];
            base.RemoveItem(index);
            oldItem.Factory = null;
        }
        /// <inheritdoc />
        protected override void SetItem(int index, SitePlannerProject item)
        {
            var oldItem = this[index];

            item.Factory = this.owner;
            base.SetItem(index, item);
            oldItem.Factory = null;
        }
        /// <inheritdoc />
        protected override void ClearItems()
        {
            foreach (var item in this)
                item.Factory = null;

            base.ClearItems();
        }

        #endregion
    }
}
