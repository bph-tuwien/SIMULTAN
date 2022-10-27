using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.Taxonomy
{
    internal interface ITaxonomyReferenceDeleteEntry
    {
        void OnDelete();
    }

    /// <summary>
    /// Represents a reference to a <see cref="SimTaxonomyEntry"/>.
    /// Also used to restore the reference to the entry after loading.
    /// </summary>
    public class SimTaxonomyEntryReference
    {
        #region Properties

        /// <summary>
        /// The Target <see cref="SimTaxonomyEntry"/>
        /// </summary>
        public SimTaxonomyEntry Target { get; }

        /// <summary>
        /// The ID of the Taxonomy that the target is in.
        /// If this is set and the Target is null, RestoreReferences will find the Target.
        /// </summary>
        public SimId TaxonomyId { get; }

        /// <summary>
        /// The ID of the TaxonomyEntry.
        /// If this is set and the Target is null, RestoreReferences will find the Target.
        /// </summary>
        public SimId TaxonomyEntryId { get; }

        #endregion

        #region .CTOR

        /// <summary>
        /// Creates a reference to a <see cref="SimTaxonomyEntry"/>
        /// </summary>
        /// <param name="target">The target entry</param>
        public SimTaxonomyEntryReference(SimTaxonomyEntry target)
        {
            Target = target;
            TaxonomyEntryId = target.Id;
            if(target.Taxonomy != null)
            {
                TaxonomyId = target.Taxonomy.Id;
            }
            else 
            {
                TaxonomyId = SimId.Empty;
            }
        }

        /// <summary>
        /// Creates a reference to the Id of an <see cref="SimTaxonomyEntry"/>
        /// Used for loading.
        /// </summary>
        /// <param name="taxonomyEntryId">The id of the entry</param>
        /// <param name="taxonomyId">The taxonomy id</param>
        public SimTaxonomyEntryReference(SimId taxonomyEntryId, SimId taxonomyId)
        {
            TaxonomyEntryId = taxonomyEntryId;
            TaxonomyId = taxonomyId;
            Target = null;
        }

        #endregion

        /// <summary>
        /// Registers a delegate which will be called when the taxonomy entry gets removed
        /// </summary>
        /// <param name="deleter">The delegate that should be called</param>
        internal void SetDeleteAction(TaxonomyReferenceDeleter deleter)
        {
            if(Target != null)
            {
                Target.AddDeleteReference(this, deleter);
            }
        }

        /// <summary>
        /// Removes the delete delegate from the taxonomy entry
        /// </summary>
        internal void RemoveDeleteAction()
        {
            if(Target != null)
            {
                Target.RemoveDeleteReference(this);
            }
        }

        /// <summary>
        /// Delegate type for delete handlers
        /// </summary>
        internal delegate void TaxonomyReferenceDeleter();
    }


}
