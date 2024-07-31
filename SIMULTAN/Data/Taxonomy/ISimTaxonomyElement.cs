using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.Taxonomy
{
    /// <summary>
    /// Interface which identifies all elements that can be present in a taxonomy
    /// </summary>
    public interface ISimTaxonomyElement
    {
        /// <summary>
        /// The taxonomy this element belongs to
        /// </summary>
        SimTaxonomy Taxonomy { get; }

        /// <summary>
        /// Gets called by the <see cref="SimTaxonomyLocalization"/> class when it changes
        /// </summary>
        void NotifyLocalizationChanged();
    }
}
