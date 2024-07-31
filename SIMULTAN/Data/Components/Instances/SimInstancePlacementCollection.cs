using SIMULTAN.Exceptions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.Components
{
    public partial class SimComponentInstance
    {
        /// <summary>
        /// Collection for managing placements of the instance. 
        /// Automatically set/unsets properties of the placement to ensure a valid two-way connection
        /// </summary>
        public class SimInstancePlacementCollection : ObservableCollection<SimInstancePlacement>
        {
            private SimComponentInstance owner;

            /// <summary>
            /// Initializes a new instance of the PlacementCollection class
            /// </summary>
            /// <param name="owner">The instance to which this collection belongs</param>
            public SimInstancePlacementCollection(SimComponentInstance owner)
            {
                this.owner = owner;
            }

            #region Collection Implementation

            /// <inheritdoc />
            protected override void InsertItem(int index, SimInstancePlacement item)
            {
                if (item == null)
                    throw new ArgumentNullException(nameof(item));

                this.owner.NotifyWriteAccess();

                this.SetValue(item);
                base.InsertItem(index, item);

                this.owner.OnInstanceStateChanged();
                this.owner.NotifyChanged();

                if (this.owner.Factory != null && item is SimInstancePlacementGeometry gp)
                    this.owner.Factory.ProjectData.ComponentGeometryExchange.OnPlacementAdded(gp);
            }
            /// <inheritdoc />
            protected override void RemoveItem(int index)
            {
                this.owner.NotifyWriteAccess();

                var oldItem = this[index];

                if (this.owner.Factory != null && oldItem is SimInstancePlacementGeometry gp)
                    this.owner.Factory.ProjectData.ComponentGeometryExchange.OnPlacementRemoved(gp);

                this.UnsetValue(this[index]);
                base.RemoveItem(index);
                this.owner.OnInstanceStateChanged();
                this.owner.NotifyChanged();
            }
            /// <inheritdoc />
            protected override void ClearItems()
            {
                this.owner.NotifyWriteAccess();

                foreach (var pl in this)
                {
                    if (this.owner.Factory != null && pl is SimInstancePlacementGeometry gp)
                        this.owner.Factory.ProjectData.ComponentGeometryExchange.OnPlacementRemoved(gp);
                    this.UnsetValue(pl);
                }

                base.ClearItems();
                this.owner.OnInstanceStateChanged();
                this.owner.NotifyChanged();
            }
            /// <inheritdoc />
            protected override void SetItem(int index, SimInstancePlacement item)
            {
                if (item == null)
                    throw new ArgumentNullException(nameof(item));

                this.owner.NotifyWriteAccess();

                if (this.owner.Factory != null && this[index] is SimInstancePlacementGeometry gp)
                    this.owner.Factory.ProjectData.ComponentGeometryExchange.OnPlacementRemoved(gp);

                this.UnsetValue(this[index]);
                this.SetValue(item);
                base.SetItem(index, item);

                this.owner.OnInstanceStateChanged();
                this.owner.NotifyChanged();

                if (this.owner.Factory != null && item is SimInstancePlacementGeometry gpNew)
                    this.owner.Factory.ProjectData.ComponentGeometryExchange.OnPlacementAdded(gpNew);
            }

            #endregion

            private void SetValue(SimInstancePlacement placement)
            {
                placement.Instance = this.owner;
            }
            private void UnsetValue(SimInstancePlacement placement)
            {
                placement.Instance = null;
            }
        }
    }
}
