using SIMULTAN.Data.Taxonomy;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Serializer.JSON
{
    /// <summary>
    /// JSON import/export model of the <see cref="SimTaxonomyEntry"/>
    /// </summary>
    public class SimTaxonomyEntrySerializable
    {
        /// <summary>
        /// The key of the taxonomy entry
        /// </summary>
        public string Key { get; set; }
        /// <summary>
        /// The invariant culture name of the taxonomy entry
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// The invariant culture description of the taxonomy entry
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        /// The localizations of the taxonomy entry
        /// </summary>
        public List<SimTaxonomyLocalizationSerializable> Localization { get; set; }
        /// <summary>
        /// The children of the taxonomy entry
        /// </summary>
        public List<SimTaxonomyEntrySerializable> Children { get; set; }

        /// <summary>
        /// Creates an empty <see cref="SimTaxonomyEntrySerializable"/>
        /// </summary>
        public SimTaxonomyEntrySerializable() { }

        /// <summary>
        /// Creates a new <see cref="SimTaxonomyEntrySerializable"/> from the given <see cref="SimTaxonomyEntry"/>.
        /// Name and Description will be set to the invariant culture or the first localization if not found.
        /// Localization will be null if only invariant culture is used.
        /// Children will be null if the are none.
        /// </summary>
        /// <param name="entry">The entry to convert</param>
        public SimTaxonomyEntrySerializable(SimTaxonomyEntry entry)
        {
            // try get invariant language
            var invariant = entry.Localization.Entries.Values.FirstOrDefault(x => x.Culture.Equals(CultureInfo.InvariantCulture));
            if (invariant.Name == null) // otherwise get first root language
                invariant = entry.Localization.Entries.Values.FirstOrDefault(x => x.Culture.Parent.Equals(CultureInfo.InvariantCulture));
            if (invariant.Name == null) // otherwise get first
                invariant = entry.Localization.Entries.Values.FirstOrDefault();
            Key = entry.Key;
            Name = invariant.Name;
            Description = invariant.Description;
            var filteredLoc = entry.Localization.Entries.Where(x => !x.Key.Equals(CultureInfo.InvariantCulture));
            Localization = filteredLoc.Any() ? filteredLoc.Select(x =>
                new SimTaxonomyLocalizationSerializable(x.Key.Name, x.Value.Name, x.Value.Description)).ToList() : null;
            Children = entry.Children.Any() ? entry.Children.Select(x => new SimTaxonomyEntrySerializable(x)).ToList() : null;
        }

        /// <summary>
        /// Converts this into a <see cref="SimTaxonomyEntry"/>
        /// </summary>
        /// <param name="supportsInvariantCulture">If the taxonomy supports the invariant culture. 
        /// If false and localization is not null, the name and description will be ignored and only the localizations will be converted.</param>
        /// <returns>The converted entry</returns>
        public SimTaxonomyEntry ToSimTaxonomyEntry(bool supportsInvariantCulture)
        {
            var entry = new SimTaxonomyEntry(Key);
            if (supportsInvariantCulture || Localization == null || !Localization.Any())
            {
                entry.Localization.AddLanguage(CultureInfo.InvariantCulture);
                entry.Localization.SetLanguage(CultureInfo.InvariantCulture, Name, Description);
            }
            if (Localization != null)
            {
                foreach (var locale in this.Localization)
                {
                    var culture = new CultureInfo(locale.CultureCode);
                    entry.Localization.AddLanguage(culture);
                    entry.Localization.SetLanguage(culture, locale.Name, locale.Description);
                }
            }
            if (Children != null)
            {
                foreach (var child in this.Children)
                {
                    entry.Children.Add(child.ToSimTaxonomyEntry(supportsInvariantCulture));
                }
            }
            return entry;
        }
    }
}
