using System;
using System.Diagnostics;

namespace SIMULTAN.Data.Taxonomy
{
    /// <summary>
    /// Represents a reference to a <see cref="SimTaxonomyEntry"/>.
    /// Also used to restore the reference to the entry after loading.
    /// </summary>
    [DebuggerDisplay("[TaxonomyEntryReference] {Target}, {TaxonomyEntryId}")]
    public class SimTaxonomyEntryReference : IComparable<SimTaxonomyEntryReference>, IEquatable<SimTaxonomyEntryReference>, ICloneable
    {
        #region Properties

        /// <summary>
        /// The Target <see cref="SimTaxonomyEntry"/>
        /// </summary>
        public SimTaxonomyEntry Target { get; }

        /// <summary>
        /// The ID of the TaxonomyEntry.
        /// If the Target is not null, returns the Targets ID, otherwise returns the ID set during construction.
        /// If this is set and the Target is null, RestoreReferences will find the Target.
        /// </summary>
        public SimId TaxonomyEntryId
        {
            get
            {
                if (Target == null)
                    return SimId.Empty;
                return Target.Id;
            }
        }

        #endregion

        #region .CTOR

        /// <summary>
        /// Creates a copy of another <see cref="SimTaxonomyEntryReference"/>
        /// </summary>
        /// <param name="other">The reference to copy</param>
        public SimTaxonomyEntryReference(SimTaxonomyEntryReference other)
        {
            this.Target = other.Target;
        }

        /// <summary>
        /// Creates a reference to a <see cref="SimTaxonomyEntry"/>
        /// </summary>
        /// <param name="target">The target entry</param>
        public SimTaxonomyEntryReference(SimTaxonomyEntry target)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            Target = target;
        }

        /// <summary>
        /// Creates a reference to a <see cref="SimTaxonomyEntry"/>
        /// </summary>
        protected SimTaxonomyEntryReference() { Target = null; }


        #endregion

        /// <summary>
        /// Registers a delegate which will be called when the taxonomy entry gets removed
        /// </summary>
        /// <param name="deleter">The delegate that should be called</param>
        internal void SetDeleteAction(TaxonomyReferenceDeleter deleter)
        {
            if (Target != null)
            {
                Target.AddDeleteReference(this, deleter);
            }
        }

        /// <summary>
        /// Removes the delete delegate from the taxonomy entry
        /// </summary>
        internal void RemoveDeleteAction()
        {
            if (Target != null)
            {
                Target.RemoveDeleteReference(this);
            }
        }

        /// <summary>
        /// Obsolete, use localized names instead.
        /// Only compares the <see cref="Target"/>, have a look at <see cref="SimTaxonomyEntry.CompareTo(SimTaxonomyEntry)"/>
        /// </summary>
        /// <param name="other">The other reference to compare to</param>
        /// <returns>See <see cref="SimTaxonomyEntry.CompareTo(SimTaxonomyEntry)"/></returns>
        [Obsolete("Use compare manually using localized names")]
        public int CompareTo(SimTaxonomyEntryReference other)
        {
            if (Debugger.IsAttached)
                throw new NotImplementedException("Should not be used anymore, replace with localized compare");

            return Target.CompareTo(other.Target);
        }

        /// <summary>
        /// Only compares the <see cref="Target"/> of the references.
        /// </summary>
        /// <param name="other">The other reference to compare to</param>
        /// <returns>True it the others references target equals this references target.</returns>
        public bool Equals(SimTaxonomyEntryReference other)
        {
            return Target.Equals(other.Target);
        }

        /// <summary>
        /// Equals with another object, compares the references and not the target.
        /// </summary>
        /// <param name="obj">The object to check equality with</param>
        /// <returns>True if it is the same reference.</returns>
        public override bool Equals(object obj)
        {
            if (obj is SimTaxonomyEntryReference taxRef)
            {
                return this.Equals(taxRef);
            }
            return false;
        }

        /// <summary>
        /// Returns the hash code of the <see cref="Target"/>
        /// Will throw a <see cref="NullReferenceException"/> otherwise.
        /// </summary>
        /// <returns>The hash code of <see cref="Target"/></returns>
        public override int GetHashCode()
        {
            return Target.GetHashCode();
        }

        /// <summary>
        /// Compares the targets of the references
        /// </summary>
        /// <param name="a">The left reference</param>
        /// <param name="b">The right reference</param>
        /// <returns>True if a and b have the same target</returns>
        public static bool operator ==(SimTaxonomyEntryReference a, SimTaxonomyEntryReference b)
        {
            if (Object.ReferenceEquals(a, null) && Object.ReferenceEquals(b, null))
                return true;
            if (Object.ReferenceEquals(a, null) || Object.ReferenceEquals(b, null)) // only one reference is null
                return false;
            return a.Target == b.Target;
        }

        /// <summary>
        /// Compares the targets of the references
        /// </summary>
        /// <param name="a">The left reference</param>
        /// <param name="b">The right reference</param>
        /// <returns>True if a and b don't have the same target</returns>
        public static bool operator !=(SimTaxonomyEntryReference a, SimTaxonomyEntryReference b)
        {
            return !(a == b);
        }

        /// <inheritdoc/>
        public object Clone()
        {
            return new SimTaxonomyEntryReference(this);
        }

        /// <summary>
        /// Delegate type for delete handlers
        /// </summary>
        internal delegate void TaxonomyReferenceDeleter(SimTaxonomyEntry caller);

        /// <summary>
        /// Returns true when the entry key as well as the taxonomy key of this entry match the parameters
        /// </summary>
        /// <param name="taxonomyKey">The key of the taxonomy</param>
        /// <param name="entryKey">The key of the entry</param>
        /// <returns>Returns true when the entry key as well as the taxonomy key of this entry match the parameters, otherwise False</returns>
        public bool Matches(string taxonomyKey, string entryKey)
        {
            if (Target == null)
                return false;
            return Target.Matches(taxonomyKey, entryKey);
        }
    }
}
