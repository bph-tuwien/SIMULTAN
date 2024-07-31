using SIMULTAN.Data.Taxonomy;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Serializer.JSON
{
    /// <summary>
    /// JSON serializable for <see cref="SimTaxonomyEntryOrString"/>
    /// </summary>
    public class SimTaxonomyEntryOrStringSerializable
    {
        /// <summary>
        /// The text or null if a taxonomy entry is used
        /// </summary>
        public string Text { get; set; }
        /// <summary>
        /// The taxonomy entry reference or null if text is used
        /// </summary>
        public SimTaxonomyEntryReferenceSerializable TaxonomyEntry { get; set; }

        /// <summary>
        /// Creates a new <see cref="SimTaxonomyEntryOrStringSerializable"/>
        /// </summary>
        /// <param name="entry">The entry to serialize</param>
        public SimTaxonomyEntryOrStringSerializable(SimTaxonomyEntryOrString entry)
        {
            Text = entry.Text;
            TaxonomyEntry = entry.HasTaxonomyEntry ? new SimTaxonomyEntryReferenceSerializable(entry.TaxonomyEntryReference) : null;
        }

        //DO NOT USE. Only required for the XMLSerializer class to operate on this type
        private SimTaxonomyEntryOrStringSerializable() { throw new NotImplementedException(); }
    }
}
