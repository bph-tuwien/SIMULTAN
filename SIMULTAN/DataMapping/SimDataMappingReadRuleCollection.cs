using SIMULTAN.Data.Components;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.DataMapping
{
    /// <summary>
    /// A collection of <see cref="SimDataMappingReadRule"/> instances in a <see cref="SimDataMappingTool"/>
    /// </summary>
    public class SimDataMappingReadRuleCollection : ObservableCollection<SimDataMappingReadRule>
    {
        /// <summary>
        /// The tool to which this collection belongs
        /// </summary>
        public SimDataMappingTool Owner { get; }

        #region ObservableCollection Overrides

        /// <inheritdoc />
        protected override void InsertItem(int index, SimDataMappingReadRule item)
        {
            SetValues(item);
            base.InsertItem(index, item);
        }
        /// <inheritdoc />
        protected override void RemoveItem(int index)
        {
            var item = this[index];
            UnsetValues(item);
            base.RemoveItem(index);
        }
        /// <inheritdoc />
        protected override void SetItem(int index, SimDataMappingReadRule item)
        {
            var oldItem = this[index];

            UnsetValues(oldItem);
            SetValues(item);

            base.SetItem(index, item);
        }
        /// <inheritdoc />
        protected override void ClearItems()
        {
            foreach (var item in this)
                UnsetValues(item);
            base.ClearItems();
        }

        private void SetValues(SimDataMappingReadRule rule)
        {
            rule.Tool = this.Owner;
        }

        private void UnsetValues(SimDataMappingReadRule rule)
        {
            rule.Tool = null;
        }

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="SimDataMappingReadRuleCollection"/> class
        /// </summary>
        /// <param name="owner"></param>
        public SimDataMappingReadRuleCollection(SimDataMappingTool owner)
        {
            if (owner == null)
                throw new ArgumentNullException(nameof(owner));

            this.Owner = owner;
        }
    }
}
