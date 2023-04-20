using SIMULTAN.Data.Components;
using SIMULTAN.Projects;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SIMULTAN.Data.MultiValues
{
    /// <summary>
    /// Manages a number of MultiValues which belong to the same source location.
    /// Ids inside the factory are unique.
    /// MultiValues that are added to the factory have to have an empty Id unless loading mode is enabled first.
    /// </summary>
    public class SimMultiValueCollection : SimManagedCollection<SimMultiValue>
    {
        #region ObservableCollection Overrides

        /// <inheritdoc />
        protected override void InsertItem(int index, SimMultiValue item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            SetValues(item);
            base.InsertItem(index, item);
            NotifyChanged();
        }
        /// <inheritdoc />
        protected override void RemoveItem(int index)
        {
            var oldItem = this[index];
            UnsetValues(oldItem);
            base.RemoveItem(index);
            NotifyChanged();
        }
        /// <inheritdoc />
        protected override void ClearItems()
        {
            foreach (var item in this)
                UnsetValues(item);
            base.ClearItems();
            NotifyChanged();
        }
        /// <inheritdoc />
        protected override void SetItem(int index, SimMultiValue item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            var oldItem = this[index];
            UnsetValues(oldItem);
            SetValues(item);
            base.SetItem(index, item);
            NotifyChanged();
        }

        internal void ClearWithoutDelete()
        {
            foreach (var item in this)
                UnsetValues(item, false);
            base.ClearItems();
            NotifyChanged();
        }


        private void SetValues(SimMultiValue item)
        {
            if (item.Factory != null)
                throw new ArgumentException("item already belongs to a factory");

            if (item.Id != SimId.Empty) //Used pre-stored id (only possible during loading)
            {
                if (isLoading)
                {
                    item.Id = new SimId(CalledFromLocation, item.Id.LocalId);
                    ProjectData.IdGenerator.Reserve(item, item.Id);
                }
                else
                    throw new NotSupportedException("Existing Ids may only be used during a loading operation");
            }
            else
                item.Id = ProjectData.IdGenerator.NextId(item, CalledFromLocation);

            item.Factory = this;
        }

        private void UnsetValues(SimMultiValue item, bool delete = true)
        {
            ProjectData.IdGenerator.Remove(item);
            item.Id = SimId.Empty;
            item.Factory = null;

            if (delete)
                item.NotifyDeleting();
        }

        #endregion

        #region Loading

        private bool isLoading = false;

        /// <summary>
        /// Sets the factory in loading mode which allows to add MultiValues with a pre-defined Id
        /// </summary>
        public void StartLoading()
        {
            isLoading = true;
        }
        /// <summary>
        /// Ends the loading operation and reenables Id checking
        /// </summary>
        public void EndLoading()
        {
            isLoading = false;
        }

        #endregion

        /// <summary>
        /// Initializes a new instance of the SimMultiValueCollection class
        /// </summary>
        public SimMultiValueCollection(ProjectData owner) : base(owner) { }


        #region METHODS: Record Management

        /// <summary>
        /// Checks which values are used in the given component factory and removes all unused ones.
        /// If the factory is null this results in removing all values.
        /// </summary>
        /// <param name="components">the component factory using the value fields</param>
        /// <param name="_excluded_from_removal">multi values that should not be removed</param>
        public void RemoveUnused(SimComponentCollection components, IEnumerable<SimMultiValue> _excluded_from_removal)
        {
            if (components == null)
                throw new ArgumentNullException(nameof(components));

            HashSet<SimMultiValue> usedMultiValues = new HashSet<SimMultiValue>();
            ComponentWalker.ForeachComponent(components, x =>
            {
                foreach (var param in x.Parameters)
                {
                    if (param.ValueSource != null && param.ValueSource is SimMultiValueParameterSource mvp && !usedMultiValues.Contains(mvp.ValueField))
                    {
                        usedMultiValues.Add(mvp.ValueField);
                    }
                }
            });

            if (_excluded_from_removal != null)
                foreach (var excl in _excluded_from_removal)
                    usedMultiValues.Add(excl);

            for (int i = this.Count - 1; i >= 0; i--)
            {
                var mv = this.ElementAt(i);
                if (!usedMultiValues.Contains(mv))
                {
                    this.RemoveAt(i);
                }
            }
        }

        /// <inheritdoc />
        protected override void OnCalledFromLocationChanged()
        {
            base.OnCalledFromLocationChanged();

            foreach (var mv in this)
                mv.Id = new SimId(this.CalledFromLocation != null ? this.CalledFromLocation.GlobalID : Guid.Empty, mv.Id.LocalId);
        }

        #endregion

        #region METHODS: Merge Records

        /// <summary>
        /// Merges another factory into this factory. 
        /// The source factory gets emptied and all items are transfered into this factory while assigning new Ids.
        /// </summary>
        /// <returns>
        /// Returns a dictionary which maps old SimMultiValue Ids (from source) to new Ids (in this factory).
        /// Key = old Id, Value = new Id.
        /// </returns>
        public Dictionary<long, long> Merge(SimMultiValueCollection source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            List<(SimMultiValue value, long id)> oldIds = source.Select(x => (x, x.LocalID)).ToList();
            source.ClearWithoutDelete();

            Dictionary<long, long> id_change_record = new Dictionary<long, long>();

            foreach ((var mv, var oldId) in oldIds)
            {
                this.Add(mv);
                id_change_record.Add(oldId, mv.LocalID);
            }

            return id_change_record;
        }

        #endregion

        #region METHODS: Getter

        /// <summary>
        /// Returns the SimMultiValue with a given Id
        /// </summary>
        /// <param name="_location">The global Id</param>
        /// <param name="_id">The local Id</param>
        /// <returns>
        /// When _location equals the current CalledFromLocation or equals Guid.Empty, the SimMultiValue with the given local Id is returned.
        /// Returns null when either the global Id doesn't match or when no SimMultiValue with the local Id exists.
        /// </returns>
        public SimMultiValue GetByID(Guid _location, long _id)
        {
            if (this.CalledFromLocation != default && _location != Guid.Empty && this.CalledFromLocation.GlobalID != _location)
                return null;
            return this.FirstOrDefault(x => x.Id.LocalId == _id);
        }

        #endregion
    }
}
