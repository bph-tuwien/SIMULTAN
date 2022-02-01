using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.Components
{
    /// <summary>
    /// Collection for the <see cref="SimUserComponentList"/> in the factory.
    /// Automatically handles un/setting the factory of the lists so the events are properly de/attached.
    /// </summary>
    public class SimUserComponentListCollection : ObservableCollection<SimUserComponentList>
    {
        #region Collection Implementation

        /// <inheritdoc />
        protected override void InsertItem(int index, SimUserComponentList item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));
            if (item.Factory != null)
                throw new ArgumentException("item already belongs to a factory");

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
                UnsetValues(item);
            base.ClearItems();
        }
        /// <inheritdoc />
        protected override void SetItem(int index, SimUserComponentList item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            var oldItem = this[index];
            UnsetValues(oldItem);
            SetValues(item);
            base.SetItem(index, item);
        }

        #endregion

        private void SetValues(SimUserComponentList item)
        {
            item.Factory = this;
        }

        private void UnsetValues(SimUserComponentList item)
        {
            item.Factory = null;
        }
    }
}
