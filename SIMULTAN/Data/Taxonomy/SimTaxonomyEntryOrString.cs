using System;
using System.CodeDom;
using System.Collections.Generic;
using System.ComponentModel;
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
    public readonly struct SimTaxonomyEntryOrString 
    {
        /// <summary>
        /// The name.
        /// Either returns the set name or the name of the taxonomy entry if one is set.
        /// </summary>
        public String Name
        {
            get
            {
                if (HasTaxonomyEntry())
                {
                    return TaxonomyEntryReference.Target.Name;
                }
                return name;
            }
        }
        private readonly String name;

        /// <summary>
        /// The reference to the taxonomy entry.
        /// </summary>
        public SimTaxonomyEntryReference TaxonomyEntryReference
        {
            get
            {
                return taxonomyEntry;
            }
        }
        private readonly SimTaxonomyEntryReference taxonomyEntry;

        /// <summary>
        /// Creates a new <see cref="SimTaxonomyEntryOrString"/> representing just a string.
        /// </summary>
        /// <param name="name">The name</param>
        public SimTaxonomyEntryOrString(string name) : this()
        {
            this.name = name;
            this.taxonomyEntry = null;
        }

        /// <summary>
        /// Creates a new <see cref="SimTaxonomyEntryOrString"/> representing a <see cref="SimTaxonomyEntry"/>.
        /// Automatically creates a reference to the entry.
        /// </summary>
        /// <param name="taxonomyEntry">The taxonomy entry</param>
        public SimTaxonomyEntryOrString(SimTaxonomyEntry taxonomyEntry)
        {
            this.taxonomyEntry = new SimTaxonomyEntryReference(taxonomyEntry);
            this.name = null;
        }

        /// <summary>
        /// Creates a new <see cref="SimTaxonomyEntryOrString"/> representing a <see cref="SimTaxonomyEntry"/>.
        /// </summary>
        /// <param name="taxonomyEntry">The taxonomy entry</param>
        public SimTaxonomyEntryOrString(SimTaxonomyEntryReference taxonomyEntry) : this()
        {
            this.taxonomyEntry = taxonomyEntry;
            this.name = null;
        }

        /// <summary>
        /// Creates a copy of the original <see cref="SimTaxonomyEntryOrString"/>.
        /// </summary>
        /// <param name="original">The original to copy.</param>
        public SimTaxonomyEntryOrString(SimTaxonomyEntryOrString original) : this()
        {
            if(original.HasTaxonomyEntry())
            {
                this.taxonomyEntry = new SimTaxonomyEntryReference(original.TaxonomyEntryReference.Target);
                this.name = null;
            }
            else
            {
                this.taxonomyEntry = null;
                this.name = original.name;
            }
        }


        /// <summary>
        /// Returns if there is a taxonomy entry set in the taxonomy entry reference.
        /// </summary>
        /// <returns>if there is a taxonomy entry set in the taxonomy entry reference.</returns>
        public bool HasTaxonomyEntry()
        {
            return TaxonomyEntryReference != null && TaxonomyEntryReference.Target != null;
        }

        /// <summary>
        /// Returns if there is just a taxonomy reference set, does not consider if the reference actually has a <see cref="SimTaxonomyEntry"/>.
        /// </summary>
        /// <returns>if there is just a taxonomy reference set, does not consider if the reference actually has a <see cref="SimTaxonomyEntry"/>.</returns>
        public bool HasTaxonomyEntryReference()
        {
            return taxonomyEntry != null;
        }

        /// <summary>
        /// Returns true if either the names or the taxonomy entries are the same in both.
        /// </summary>
        /// <param name="other">The other to check equality with.</param>
        /// <returns>True if either the names or the taxonomy entries are the same in both</returns>
        public bool Equals(SimTaxonomyEntryOrString other)
        {
            if (HasTaxonomyEntry() != other.HasTaxonomyEntry())
                return false;
            if(HasTaxonomyEntry())
            {
                return TaxonomyEntryReference.Target == other.TaxonomyEntryReference.Target;
            }
            else
            {
                return Name == other.Name;
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
            if(HasTaxonomyEntry())
                hashCode = hashCode * -1521134295 + EqualityComparer<SimTaxonomyEntryReference>.Default.GetHashCode(this.TaxonomyEntryReference);
            else 
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(this.name);
            return hashCode;
        }
    }
}
