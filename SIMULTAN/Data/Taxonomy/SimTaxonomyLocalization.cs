using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;

namespace SIMULTAN.Data.Taxonomy
{
    /// <summary>
    /// Entry for the <see cref="SimTaxonomyLocalization"/>
    /// </summary>
    [DebuggerDisplay("[LocalizationEntry] {Culture.Name}, {Name}, {Description}")]
    public struct SimTaxonomyLocalizationEntry
    {
        /// <summary>
        /// Culture of the entry
        /// </summary>
        public CultureInfo Culture { get; }

        /// <summary>
        /// Name of the entry
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Description of the Entry
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// Returns False when the culture is missing, otherwise True
        /// </summary>
        public bool IsValid { get { return Culture != null; } }

        /// <summary>
        /// Creates a new <see cref="SimTaxonomyLocalizationEntry"/>
        /// </summary>
        /// <param name="culture">The culture</param>
        /// <param name="name">The name</param>
        /// <param name="description">The description</param>
        public SimTaxonomyLocalizationEntry(CultureInfo culture, string name = "", string description = "")
        {
            this.Culture = culture;
            this.Name = name;
            this.Description = description;
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return obj is SimTaxonomyLocalizationEntry entry &&
                   EqualityComparer<CultureInfo>.Default.Equals(this.Culture, entry.Culture) &&
                   this.Name == entry.Name &&
                   this.Description == entry.Description;
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            int hashCode = 598068992;
            hashCode = hashCode * -1521134295 + EqualityComparer<CultureInfo>.Default.GetHashCode(this.Culture);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(this.Name);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(this.Description);
            return hashCode;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return String.Format("Culture: \"{0}\", Name: \"{1}\", Description: \"{2}\"", Culture.Name, Name, Description);
        }
    }

    /// <summary>
    /// Class for managing localization of <see cref="SimTaxonomy"/> and <see cref="SimTaxonomyEntry"/>
    /// </summary>
    public class SimTaxonomyLocalization
    {
        private ISimTaxonomyElement owner;

        /// <summary>
        /// Event is called when a translation is added, removed or updated.
        /// </summary>
        public event EventHandler Changed;

        /// <summary>
        /// Read only dictionary of the localization entries
        /// </summary>
        public ReadOnlyDictionary<CultureInfo, SimTaxonomyLocalizationEntry> Entries
        {
            get => new ReadOnlyDictionary<CultureInfo, SimTaxonomyLocalizationEntry>(entries);
        }
        private Dictionary<CultureInfo, SimTaxonomyLocalizationEntry> entries;

        /// <summary>
        /// Creates a new <see cref="SimTaxonomyLocalization"/>
        /// </summary>
        public SimTaxonomyLocalization(ISimTaxonomyElement owner)
        {
            if (owner == null)
                throw new ArgumentNullException(nameof(owner));

            this.owner = owner;
            entries = new Dictionary<CultureInfo, SimTaxonomyLocalizationEntry>();
        }


        /// <summary>
        /// Sets the localization for a specific culture.
        /// When the entry is part of a Taxonomy, the Language has to be added to the <see cref="SimTaxonomy.Languages"/> before it can be set,
        /// otherwise throws an <see cref="ArgumentException"/>.
        /// </summary>
        /// <param name="culture">The culture to update</param>
        /// <param name="name">The updated name</param>
        /// <param name="description">The updated description</param>
        public void SetLanguage(CultureInfo culture, String name, String description)
        {
            SetLanguage(new SimTaxonomyLocalizationEntry(culture, name, description));
        }

        /// <summary>
        /// Sets the localization for a specific culture.
        /// When the entry is part of a Taxonomy, the Language has to be added to the <see cref="SimTaxonomy.Languages"/> before it can be set,
        /// otherwise throws an <see cref="ArgumentException"/>.
        /// </summary>
        /// <param name="entry">The entry to update</param>
        /// <exception cref="ArgumentException">T</exception>
        public void SetLanguage(SimTaxonomyLocalizationEntry entry)
        {
            if (this.owner.Taxonomy != null && !entries.ContainsKey(entry.Culture))
                throw new ArgumentException("Taxonomy Languages does not contain culture " + entry.Culture.Name);

            entries[entry.Culture] = entry;
            NotifyChanged();
        }


        /// <summary>
        /// Adds a language to the localization if it is not added already.
        /// </summary>
        /// <param name="culture">The culture to add</param>
        internal void AddLanguage(CultureInfo culture)
        {
            if (!entries.ContainsKey(culture))
            {
                entries.Add(culture, new SimTaxonomyLocalizationEntry(culture));
                NotifyChanged();
            }
        }

        /// <summary>
        /// Removes a language and its translation from the localization.
        /// </summary>
        /// <param name="culture">The culture to remove</param>
        internal void RemoveLanguage(CultureInfo culture)
        {
            if (entries.ContainsKey(culture))
            {
                entries.Remove(culture);
                NotifyChanged();
            }
        }

        /// <summary>
        /// Returns the localized entry that best fits the given culture.
        /// First returns the translation that directly fits the culture.
        /// If that is not found it looks up the languages parent languages up to the invariant cultue.
        /// If none matches, the first entry is returned.
        /// If there are no entries, an empty entry with null culture is returned.
        /// </summary>
        /// <param name="culture">The culture to get the localized entry for</param>
        /// <returns>The localization entry best fitting the given culture.</returns>
        public SimTaxonomyLocalizationEntry Localize(CultureInfo culture = null)
        {
            // use invariant culture if none provided
            var c = culture ?? CultureInfo.InvariantCulture;
            if (entries.TryGetValue(c, out var localization))
            {
                return localization;
            }
            // if not even the invariant culture is present, try to return the first localization
            else if (c == CultureInfo.InvariantCulture)
            {
                if (entries.Any())
                {
                    return entries.Values.First();
                }
            }
            // check if we can localize with the parent culture (e.g. de-at -> de -> invariant)
            else if (c.Parent != null)
            {
                return Localize(c.Parent);
            }

            // there are no translations
            return new SimTaxonomyLocalizationEntry(null, "", "");
        }

        private void NotifyChanged()
        {
            Changed?.Invoke(this, new EventArgs());
            this.owner.NotifyLocalizationChanged();
        }

        /// <summary>
        /// Checks if the contents of this localization are equal to each other.
        /// Checks if it contains the same languages with the same translations.
        /// Used for merging taxonomies.
        /// </summary>
        /// <param name="other">The other localization to compare to.</param>
        /// <returns>If the this localization has the same translations as the other one.</returns>
        public bool IsIdenticalTo(SimTaxonomyLocalization other)
        {
            // check if they have the same keys
            if (!entries.Keys.ToHashSet().SetEquals(other.entries.Keys))
                return false;

            // compare entries
            foreach (var entry in entries.Values)
            {
                var oval = other.entries[entry.Culture];
                if (entry.Name != oval.Name)
                    return false;
                if (entry.Description != oval.Description)
                    return false;
            }

            return true;
        }
    }
}
