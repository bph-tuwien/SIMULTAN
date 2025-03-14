using SIMULTAN.Data.Taxonomy;
using SIMULTAN.Utils;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Serializer.JSON.Serializables
{
    /// <summary>
    /// JSON import/export model of the <see cref="SimTaxonomy"/>
    /// </summary>
    public class SimTaxonomySerializable
    {
        /// <summary>
        /// The key of the taxonomy
        /// </summary>
        public string Key { get; set; }
        /// <summary>
        /// The invariant culture name of the taxonomy
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// The invariant culture description of the taxonomy
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        /// List of supported language culture codes. If null, only InvariantCulture is supported.
        /// If this does not contain the InvariantCulture, Name and Description are from one of the localizations.
        /// </summary>
        public List<string> SupportedLanguages { get; set; }
        /// <summary>
        /// The localizations of the taxonomy. If null, only InvariantCulture is used.
        /// </summary>
        public List<SimTaxonomyLocalizationSerializable> Localization { get; set; }
        /// <summary>
        /// The children of the taxonomy
        /// </summary>
        public List<SimTaxonomyEntrySerializable> Children { get; set; }

        public SimTaxonomySerializable() { }

        /// <summary>
        /// Converts the <see cref="SimTaxonomy"/> to its equivalent JSON model <see cref="SimTaxonomySerializable"/>
        /// </summary>
        /// <param name="taxonomy">The taxonomy</param>
        /// <returns>The JSON model of the taxonomy</returns>
        public SimTaxonomySerializable(SimTaxonomy taxonomy)
        {
            // try get invariant language
            var invariant = taxonomy.Localization.Entries.Values.FirstOrDefault(x => x.Culture.Equals(CultureInfo.InvariantCulture));
            if (invariant.Name == null) // otherwise get first root language
                invariant = taxonomy.Localization.Entries.Values.FirstOrDefault(x => x.Culture.Parent.Equals(CultureInfo.InvariantCulture));
            if (invariant.Name == null) // otherwise get first
                invariant = taxonomy.Localization.Entries.Values.FirstOrDefault();

            Key = taxonomy.Key;
            Name = invariant.Name;
            Description = invariant.Description;
            SupportedLanguages = taxonomy.Languages.Select(x => x.Name).ToList();
            var filteredLoc = taxonomy.Localization.Entries.Where(x => !x.Key.Equals(CultureInfo.InvariantCulture));
            Localization = filteredLoc.Any() ? filteredLoc.Select(x =>
                new SimTaxonomyLocalizationSerializable(x.Key.Name, x.Value.Name, x.Value.Description)).ToList() : null;
            Children = taxonomy.Entries.Any() ? taxonomy.Entries.Select(x => new SimTaxonomyEntrySerializable(x)).ToList() : null;
        }

        /// <summary>
        /// Converts the <see cref="SimTaxonomySerializable"/> to its equivalent SIMULTAN model <see cref="SimTaxonomy"/>
        /// </summary>
        /// <param name="jsonTaxonomy">The taxonomy</param>
        /// <returns>The SIMULTAN model of the taxonomy</returns>
        public SimTaxonomy ToSimTaxonomy()
        {
            var taxonomy = new SimTaxonomy()
            {
                Key = this.Key
            };

            bool supportsInvariantCulture = false;
            if (SupportedLanguages == null || !SupportedLanguages.Any())
            {
                taxonomy.Languages.Add(CultureInfo.InvariantCulture);
                taxonomy.Localization.AddLanguage(CultureInfo.InvariantCulture);
                taxonomy.Localization.SetLanguage(CultureInfo.InvariantCulture, Name, Description);
                supportsInvariantCulture = true;
            }
            else
            {
                foreach (var sl in SupportedLanguages)
                {
                    taxonomy.Languages.Add(new CultureInfo(sl));
                }
            }

            if (Localization != null)
            {
                foreach (var locale in this.Localization)
                {
                    var culture = new CultureInfo(locale.CultureCode);
                    if (!taxonomy.Languages.Contains(culture))
                        taxonomy.Languages.Add(culture);
                    taxonomy.Localization.AddLanguage(culture);
                    taxonomy.Localization.SetLanguage(culture, locale.Name, locale.Description);
                }
            }
            if (Children != null)
            {
                foreach (var child in this.Children)
                {
                    taxonomy.Entries.Add(child.ToSimTaxonomyEntry(supportsInvariantCulture));
                }
            }
            return taxonomy;
        }
    }
}
