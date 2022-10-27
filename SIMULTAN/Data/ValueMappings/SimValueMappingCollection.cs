using SIMULTAN.Data.MultiValues;
using SIMULTAN.Projects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.ValueMappings
{
    /// <summary>
    /// Stores a list of <see cref="SimColorMap"/> instances inside a Project
    /// </summary>
    public class SimValueMappingCollection : SimManagedCollection<SimValueMapping>
    {
        #region Collection Overrides

        /// <inheritdoc />
        protected override void InsertItem(int index, SimValueMapping item)
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
        protected override void SetItem(int index, SimValueMapping item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            var oldItem = this[index];
            UnsetValues(oldItem);
            SetValues(item);
            base.SetItem(index, item);
            NotifyChanged();
        }


        private void SetValues(SimValueMapping item)
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

        private void UnsetValues(SimValueMapping item)
        {
            ProjectData.IdGenerator.Remove(item);
            item.Id = new SimId(item.Id.LocalId);
            item.Factory = null;
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
        /// Initializes a new instance of the <see cref="SimValueMappingCollection"/> class
        /// </summary>
        /// <param name="projectData">The <see cref="ProjectData"/> this collection belongs to</param>
        public SimValueMappingCollection(ProjectData projectData) : base(projectData)
        { 
        }
    }
}
