using SIMULTAN.Data;
using SIMULTAN.Data.Components;
using SIMULTAN.Data.MultiValues;
using SIMULTAN.Projects;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.DataMapping
{
    /// <summary>
    /// Collection for storing <see cref="SimDataMappingTool"/> in a project
    /// </summary>
    public class SimDataMappingToolCollection : SimManagedCollection<SimDataMappingTool>
    {
        #region ObservableCollection Overloads

        /// <inheritdoc />
        protected override void InsertItem(int index, SimDataMappingTool item)
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
        protected override void SetItem(int index, SimDataMappingTool item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            var oldItem = this[index];
            UnsetValues(oldItem);
            SetValues(item);
            base.SetItem(index, item);
            NotifyChanged();
        }

        private void SetValues(SimDataMappingTool item)
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

        private void UnsetValues(SimDataMappingTool item)
        {
            ProjectData.IdGenerator.Remove(item);
            item.Id = new SimId(item.LocalID);
            item.Factory = null;
        }

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="SimDataMappingToolCollection"/> class
        /// </summary>
        /// <param name="owner">The project data to which this collection belongs to</param>
        public SimDataMappingToolCollection(ProjectData owner) : base(owner) { }


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
        /// Notifies the collection that a component has been removed from the project
        /// </summary>
        /// <param name="component">The component that got removed</param>
        internal void OnComponentRemoved(SimComponent component)
        {
            if (component == null)
                throw new ArgumentNullException(nameof(component));

            foreach (var tool in this)
                tool.Rules.RemoveMapping(component);
        }

        /// <summary>
        /// Looks up taxonomy entries for default slot by their name.
        /// Do this if the default taxonomies changed, could mean that the project is migrated.
        /// </summary>
        internal void RestoreDefaultTaxonomyReferences()
        {
            foreach (var tool in this)
                tool.RestoreDefaultTaxonomyReferences();
        }
    }
}
