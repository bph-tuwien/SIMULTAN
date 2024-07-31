using SIMULTAN.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.Components
{
    public partial class SimComponent
    {
        /// <summary>
        /// Stores referenced components inside a component
        /// </summary>
        public class SimReferenceCollection : ObservableCollection<SimComponentReference>
        {
            private readonly SimComponent owner;

            internal SimReferenceCollection(SimComponent owner)
            {
                this.owner = owner;
            }

            #region Collection Implementation

            /// <inheritdoc />
            protected override void InsertItem(int index, SimComponentReference item)
            {
                if (item == null)
                    throw new ArgumentNullException(nameof(item));
                if (item.Owner != null)
                    throw new ArgumentException("item already belongs to a collection");

                //Check access
                this.owner.RecordWriteAccess();
                this.owner.NotifyChanged();

                base.InsertItem(index, item);
                SetValues(item);

                this.owner.Parameters?.ForEach(x => x.UpdateState());
            }
            /// <inheritdoc />
            protected override void RemoveItem(int index)
            {
                var oldItem = this[index];

                this.owner.RecordWriteAccess();
                this.owner.NotifyChanged();

                UnsetValues(oldItem);
                base.RemoveItem(index);

                this.owner.Parameters?.ForEach(x => x.UpdateState());
            }
            /// <inheritdoc />
            protected override void ClearItems()
            {
                this.owner.RecordWriteAccess();

                foreach (var item in this)
                {
                    UnsetValues(item);
                }
                base.ClearItems();
                owner.OnInstanceStateChanged();
                this.owner.NotifyChanged();

                this.owner.Parameters?.ForEach(x => x.UpdateState());
            }
            /// <inheritdoc />
            protected override void SetItem(int index, SimComponentReference item)
            {
                if (item == null)
                    throw new ArgumentNullException(nameof(item));

                var oldItem = this[index];

                this.owner.RecordWriteAccess();

                UnsetValues(oldItem);
                base.SetItem(index, item);
                SetValues(item);

                this.owner.NotifyChanged();
                this.owner.Parameters?.ForEach(x => x.UpdateState());
            }

            #endregion

            private void SetValues(SimComponentReference reference)
            {
                reference.Owner = this.owner;
            }

            private void UnsetValues(SimComponentReference reference)
            {
                reference.Owner = null;
            }


            internal void NotifyFactoryChanged(SimComponentCollection newValue, SimComponentCollection oldValue)
            {
                this.ForEach(x => x.NotifyFactoryChanged(newValue, oldValue));
            }
        }
    }
}
