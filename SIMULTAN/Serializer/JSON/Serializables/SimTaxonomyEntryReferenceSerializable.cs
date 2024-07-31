using SIMULTAN.Data.Taxonomy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Serializer.JSON
{
    /// <summary>
    /// JSON serializable for a <see cref="SimTaxonomyEntryReference"/>
    /// </summary>
    public class SimTaxonomyEntryReferenceSerializable
    {
        /// <summary>
        /// Key of the taxonomy
        /// </summary>
        public string TaxonomyKey { get; set; }
        /// <summary>
        /// Key of the taxonomy entry
        /// </summary>
        public string TaxonomyEntryKey { get; set; }

        /// <summary>
        /// Creates a new <see cref="SimTaxonomyEntryReferenceSerializable"/>
        /// </summary>
        /// <param name="reference">The taxonomy entry reference to serialize</param>
        public SimTaxonomyEntryReferenceSerializable(SimTaxonomyEntryReference reference)
        {
            TaxonomyKey = reference.Target?.Taxonomy?.Key;
            TaxonomyEntryKey = reference.Target?.Key;
        }

        //DO NOT USE. Only required for the XMLSerializer class to operate on this type
        private SimTaxonomyEntryReferenceSerializable() { throw new NotImplementedException(); }
    }
}
