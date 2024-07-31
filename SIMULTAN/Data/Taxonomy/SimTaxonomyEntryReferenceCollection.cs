using SIMULTAN.Projects;
using SIMULTAN.Serializer.SimGeo;
using SIMULTAN.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.Taxonomy
{
    /// <summary>
    /// An ObservableCollection for <see cref="SimTaxonomyEntryReference"/> that automatically removes a reference if the entry gets deleted.
    /// Make sure added references are newly created so the deleter can be hooked up.
    /// </summary>
    public class SimTaxonomyEntryReferenceCollection : ObservableCollection<SimTaxonomyEntryReference>
    {

        /// <inheritdoc/>
        public SimTaxonomyEntryReferenceCollection()
        {
        }

        /// <inheritdoc/>
        public SimTaxonomyEntryReferenceCollection(IEnumerable<SimTaxonomyEntryReference> collection) : base(collection)
        {
        }

        #region Collection overrides

        /// <inheritdoc/>
        protected override void InsertItem(int index, SimTaxonomyEntryReference item)
        {
            item.SetDeleteAction(TaxonomyEntryDeleted);
            base.InsertItem(index, item);
        }

        /// <inheritdoc/>
        protected override void RemoveItem(int index)
        {
            this[index].RemoveDeleteAction();
            base.RemoveItem(index);
        }

        /// <inheritdoc/>
        protected override void ClearItems()
        {
            this.ForEach(x => x.RemoveDeleteAction());
            base.ClearItems();
        }

        /// <inheritdoc/>
        protected override void SetItem(int index, SimTaxonomyEntryReference item)
        {
            item.SetDeleteAction(TaxonomyEntryDeleted);
            base.SetItem(index, item);
        }

        #endregion

        private void TaxonomyEntryDeleted(SimTaxonomyEntry caller)
        {
            this.RemoveWhere(x => x.Target == caller);
        }

        /// <summary>
        /// Restores all taxonomy entry references after the default taxonomies were updated.
        /// </summary>
        /// <param name="projectData">The ProjectData</param>
        public void RestoreDefaultTaxonomyReferences(ProjectData projectData)
        {
            var copy = this.ToList();
            this.Clear();
            foreach (var item in copy)
            {
                var entry = projectData.IdGenerator.GetById<SimTaxonomyEntry>(item.TaxonomyEntryId);
                this.Add(new SimTaxonomyEntryReference(entry));
            }
        }
    }
}
