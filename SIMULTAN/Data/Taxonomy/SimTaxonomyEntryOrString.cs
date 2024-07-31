using System;
using System.CodeDom;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.Taxonomy
{
    /// <summary>
    /// Struct that represents either a text OR an <see cref="SimTaxonomyEntry"/>.
    /// Used when a name, for example, could also be represented by a taxonomy entry.
    /// </summary>
    [DebuggerDisplay("{TextOrKey}")]
    public readonly struct SimTaxonomyEntryOrString
    {

        /// <summary>
        /// The text used instead of the <see cref="SimTaxonomyEntry"/>. 
        /// Null if a taxonomy entry is set.
        /// </summary>
        public string Text { get; }

        /// <summary>
        /// The reference to the taxonomy entry.
        /// </summary>
        public SimTaxonomyEntryReference TaxonomyEntryReference { get; }

        /// <summary>
        /// If there is a taxonomy entry set in the taxonomy entry reference.
        /// </summary>
        public bool HasTaxonomyEntry
        {
            get => TaxonomyEntryReference != null && TaxonomyEntryReference.Target != null;
        }

        /// <summary>
        /// If there is just a taxonomy reference set, does not consider if the reference actually has a <see cref="SimTaxonomyEntry"/>.
        /// </summary>
        public bool HasTaxonomyEntryReference
        {
            get => TaxonomyEntryReference != null;
        }

        /// <summary>
        /// If there is no taxonomy entry, the Text otherwise the taxonomy entry's key.
        /// </summary>
        public string TextOrKey
        {
            get => HasTaxonomyEntry ? TaxonomyEntryReference.Target.Key : Text;
        }

        /// <summary>
        /// Creates a new <see cref="SimTaxonomyEntryOrString"/> representing just a string.
        /// </summary>
        /// <param name="text">The text</param>
        public SimTaxonomyEntryOrString(string text) : this()
        {
            this.Text = text;
            this.TaxonomyEntryReference = null;
        }

        /// <summary>
        /// Creates a new <see cref="SimTaxonomyEntryOrString"/> representing a <see cref="SimTaxonomyEntry"/>.
        /// Automatically creates a reference to the entry.
        /// </summary>
        /// <param name="taxonomyEntry">The taxonomy entry</param>
        public SimTaxonomyEntryOrString(SimTaxonomyEntry taxonomyEntry)
        {
            this.TaxonomyEntryReference = new SimTaxonomyEntryReference(taxonomyEntry);
            this.Text = null;
        }

        /// <summary>
        /// Creates a new <see cref="SimTaxonomyEntryOrString"/> representing a <see cref="SimTaxonomyEntry"/>.
        /// </summary>
        /// <param name="taxonomyEntry">The taxonomy entry</param>
        public SimTaxonomyEntryOrString(SimTaxonomyEntryReference taxonomyEntry) : this()
        {
            this.TaxonomyEntryReference = taxonomyEntry;
            this.Text = null;
        }

        /// <summary>
        /// Creates a copy of the original <see cref="SimTaxonomyEntryOrString"/>.
        /// </summary>
        /// <param name="original">The original to copy.</param>
        public SimTaxonomyEntryOrString(SimTaxonomyEntryOrString original) : this()
        {
            if (original.HasTaxonomyEntry)
            {
                this.TaxonomyEntryReference = new SimTaxonomyEntryReference(original.TaxonomyEntryReference.Target);
            }
            else
            {
                this.TaxonomyEntryReference = null;
                this.Text = original.Text;
            }
        }

        /// <summary>
        /// Returns true if either the names or the taxonomy entries are the same in both.
        /// </summary>
        /// <param name="other">The other to check equality with.</param>
        /// <returns>True if either the names or the taxonomy entries are the same in both</returns>
        public bool Equals(SimTaxonomyEntryOrString other)
        {
            if (HasTaxonomyEntry != other.HasTaxonomyEntry)
                return false;
            if (HasTaxonomyEntry)
            {
                return TaxonomyEntryReference.Target == other.TaxonomyEntryReference.Target;
            }
            else
            {
                return Text == other.Text;
            }
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (obj is SimTaxonomyEntryOrString other)
                return Equals(other);
            return false;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            int hashCode = -1651061955;
            if (HasTaxonomyEntry)
                hashCode = hashCode * -1521134295 + EqualityComparer<SimTaxonomyEntryReference>.Default.GetHashCode(this.TaxonomyEntryReference);
            else
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(this.Text);
            return hashCode;
        }

    }
}
