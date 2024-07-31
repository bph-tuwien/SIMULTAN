using SIMULTAN.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;

namespace SIMULTAN.Data.Taxonomy
{
    /// <summary>
    /// Collection of supported languages used by the <see cref="SimTaxonomy"/>
    /// Adding and removing languages will also add/remove the languages from the taxonomy's and entries' localizations.
    /// </summary>
    public class SimTaxonomyLanguageCollection : ObservableCollection<CultureInfo>
    {

        private SimTaxonomy taxonomy;

        /// <summary>
        /// Creates a new <see cref="SimTaxonomyLanguageCollection"/>
        /// </summary>
        /// <param name="owner">The owner <see cref="SimTaxonomy"/> of the languages</param>
        public SimTaxonomyLanguageCollection(SimTaxonomy owner)
            : base()
        {
            taxonomy = owner;
        }


        #region Collection Overrides

        /// <inheritdoc />
        protected override void InsertItem(int index, CultureInfo item)
        {
            SetValues(item);

            base.InsertItem(index, item);
        }

        /// <inheritdoc />
        protected override void RemoveItem(int index)
        {
            UnsetValues(this.ElementAt(index));

            base.RemoveItem(index);
        }

        /// <inheritdoc />
        protected override void ClearItems()
        {
            foreach (var entry in this)
                UnsetValues(entry);
            base.ClearItems();
        }

        /// <summary>
        /// Not supported
        /// </summary>
        /// <param name="index">-</param>
        /// <param name="item">-</param>
        protected override void SetItem(int index, CultureInfo item)
        {
            throw new NotSupportedException();
        }

        #endregion

        private void SetValues(CultureInfo value)
        {
            if (!this.Contains(value))
            {
                this.taxonomy.Localization.AddLanguage(value);
                var traversalStack = new Stack<SimTaxonomyEntry>(taxonomy.Entries);

                while (traversalStack.Any())
                {
                    var entry = traversalStack.Pop();
                    // adds it only if it does not contain it
                    entry.Localization.AddLanguage(value);

                    entry.Children.ForEach(x => traversalStack.Push(x));
                }
            }
            else
                throw new ArgumentException("Language is already present in the collection");
        }

        private void UnsetValues(CultureInfo value)
        {
            if (this.Contains(value))
            {
                this.taxonomy.Localization.RemoveLanguage(value);
                var traversalStack = new Stack<SimTaxonomyEntry>(taxonomy.Entries);

                while (traversalStack.Any())
                {
                    var entry = traversalStack.Pop();
                    // removes it only if it contains it
                    entry.Localization.RemoveLanguage(value);

                    entry.Children.ForEach(x => traversalStack.Push(x));
                }
            }
        }
    }
}
