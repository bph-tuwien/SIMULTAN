using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.SitePlanner
{
    /// <summary>
    /// Stores a list of buildings in a <see cref="SitePlannerProject"/>
    /// </summary>
    public class SitePlannerBuildingCollection : ObservableCollection<SitePlannerBuilding>
    {
        private SitePlannerProject owner;

        /// <summary>
        /// Initializes a new instance of the SitePlannerBuildingCollection class
        /// </summary>
        /// <param name="owner">The project this instance belongs to</param>
        public SitePlannerBuildingCollection(SitePlannerProject owner)
        {
            this.owner = owner;
        }


        #region Collection Implementation

        /// <inheritdoc />
        protected override void InsertItem(int index, SitePlannerBuilding item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));
            if (item.Project != null)
                throw new ArgumentException("item already belongs to a project");

            SetValues(item);
            base.InsertItem(index, item);
        }
        /// <inheritdoc />
        protected override void RemoveItem(int index)
        {
            var oldItem = this[index];

            UnsetValues(oldItem);
            base.RemoveItem(index);
        }
        /// <inheritdoc />
        protected override void ClearItems()
        {
            foreach (var item in this)
            {
                UnsetValues(item);
            }

            base.ClearItems();
        }
        /// <inheritdoc />
        protected override void SetItem(int index, SitePlannerBuilding item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            var oldItem = this[index];
            UnsetValues(oldItem);
            SetValues(item);
            base.SetItem(index, item);
        }

        #endregion

        private void SetValues(SitePlannerBuilding item)
        {
            item.Project = this.owner;
        }

        private void UnsetValues(SitePlannerBuilding item)
        {
            item.Project = null;
        }
    }
}
