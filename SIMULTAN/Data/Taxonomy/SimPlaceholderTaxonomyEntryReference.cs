using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.Taxonomy
{
    /// <summary>
    /// Placeholder reference used for migration of older versions where strings were used previously.
    /// Replace with an actual reference after the project is loaded.
    /// </summary>
    [DebuggerDisplay("[PlaceholderTaxonomyEntryReference] {PlaceholderName}")]
    public class SimPlaceholderTaxonomyEntryReference : SimTaxonomyEntryReference
    {
        /// <summary>
        /// The placeholder name used in the older version.
        /// Used to identify the <see cref="SimTaxonomyEntry"/> to migrate to.
        /// </summary>
        public string PlaceholderName { get; }

        /// <summary>
        /// Creates a new Placeholder reference with the given name.
        /// </summary>
        /// <param name="placeholderName">The placeholder name that will be used to find the <see cref="SimTaxonomyEntry"/>.</param>
        public SimPlaceholderTaxonomyEntryReference(string placeholderName) : base(SimId.Empty)
        {
            this.PlaceholderName = placeholderName;
        }
    }
}
