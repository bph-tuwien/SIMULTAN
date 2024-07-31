using SIMULTAN.Data.Taxonomy;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Utils
{
    /// <summary>
    /// Utils for taxonomies
    /// </summary>
    public static class TaxonomyUtils
    {

        /// <summary>
        /// Used to get the localized name of a <see cref="SimTaxonomyEntryOrString"/>.
        /// Don't use in new code, only needed for legacy flow networks which will be removed at some point.
        /// </summary>
        /// <param name="entry">The entry</param>
        /// <param name="culture">The culture</param>
        /// <returns>The localized name</returns>
        [Obsolete("Remove once all usages of SimTaxonomyEntryOrString that need translations are removed in the data model. This belongs in the UI.")]
        public static string GetLocalizedName(this SimTaxonomyEntryOrString entry, CultureInfo culture)
        {
            if (entry.HasTaxonomyEntry)
            {
                return entry.TaxonomyEntryReference.Target.Localization.Localize(culture).Name;
            }
            else
            {
                return entry.Text;
            }
        }

        /// <summary>
        /// Duplicates the given source language and its localization entries of the taxonomy and its entries to the given target culture.
        /// </summary>
        /// <param name="taxonomy">The taxonomy to duplicate the language for</param>
        /// <param name="source">The source culture</param>
        /// <param name="target">The target culture</param>
        /// <returns>True if the language could be duplicated, false otherwise.</returns>
        public static bool DuplicateLanguage(this SimTaxonomy taxonomy, CultureInfo source, CultureInfo target)
        {
            if (taxonomy.Languages.Contains(source) && !taxonomy.Languages.Contains(target))
            {
                // add new language
                taxonomy.Languages.Add(target);
                // set new translation to old
                var loc = taxonomy.Localization.Entries[source];
                taxonomy.Localization.SetLanguage(target, loc.Name, loc.Description);

                // traverse entries
                var traversalStack = new Stack<SimTaxonomyEntry>(taxonomy.Entries);
                while (traversalStack.Any())
                {
                    var entry = traversalStack.Pop();

                    // update translation
                    loc = entry.Localization.Entries[source];
                    entry.Localization.SetLanguage(target, loc.Name, loc.Description);

                    entry.Children.ForEach(x => traversalStack.Push(x));
                }
                return true;
            }

            return false;
        }
    }
}
