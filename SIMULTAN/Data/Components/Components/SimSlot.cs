using SIMULTAN.Data.Taxonomy;
using System;
using System.Diagnostics;

namespace SIMULTAN.Data.Components
{
    /// <summary>
    /// Defines the role of an object in the context of another object.
    /// </summary>
    [DebuggerDisplay("Slot {SlotBase} [{SlotExtension}]")]
    public struct SimSlot : IEquatable<SimSlot>
    {
        /// <summary>
        /// Represents an invalid slot where both the base and the extension are Null.
        /// </summary>
		public static SimSlot Invalid
        {
            get { return new SimSlot(); }
        }

        #region PROPERTIES

        /// <summary>
        /// Main function (e.g. Material).
        /// </summary>
        public SimTaxonomyEntryReference SlotBase { get; }
        /// <summary>
        /// Sub-function (e.g. 23 - the 23rd material).
        /// </summary>
        public string SlotExtension { get; }

        #endregion

        /// <summary>
        /// Initializes an object of type Slot.
        /// </summary>
        /// <param name="slotBase">The slot base</param>
        /// <param name="slotExtension">The slot extension</param>
        public SimSlot(SimTaxonomyEntry slotBase, string slotExtension) :
            this(slotBase == null ? null : new SimTaxonomyEntryReference(slotBase), slotExtension)
        { }

        /// <summary>
        /// Initializes an object of type Slot.
        /// </summary>
        /// <param name="slotBase">A reference to the taxonomy entry for the slot base. The Reference may not already be in use anywhere else.</param>
        /// <param name="slotExtension">The slot extension</param>
        public SimSlot(SimTaxonomyEntryReference slotBase, string slotExtension)
        {
            if (slotBase == null)
                throw new ArgumentNullException(nameof(slotBase));
            if (!(slotBase is SimPlaceholderTaxonomyEntryReference) && (slotBase.Target == null || slotBase.Target.Taxonomy == null || slotBase.Target.Factory == null))
                throw new Exception("Slot base taxonomy entry needs to be in a taxonomy and project");
            this.SlotBase = slotBase;
            this.SlotExtension = slotExtension == null ? string.Empty : slotExtension;
        }

        /// <summary>
        /// Copies a slot into a new instance.
        /// </summary>
        /// <param name="original">the original slot</param>
        public SimSlot(SimSlot original)
        {
            this.SlotBase = new SimTaxonomyEntryReference(original.SlotBase);
            this.SlotExtension = original.SlotExtension;
        }

        #region Interfaces

        /// <inheritdoc/>
		public bool Equals(SimSlot other)
        {
            return this.SlotBase == other.SlotBase && this.SlotExtension == other.SlotExtension;
        }

        /// <inheritdoc/>
		public override bool Equals(object obj)
        {
            if (obj is SimSlot other)
                return Equals(other);
            return false;
        }

        /// <inheritdoc/>
		public override int GetHashCode()
        {
            return SlotBase.GetHashCode() ^ SlotExtension.GetHashCode();
        }

        /// <summary>
        /// Equality operator for slots.
        /// </summary>
        /// <param name="lhs">left hand side slot</param>
        /// <param name="rhs">right hand side slot</param>
        /// <returns>true in case both the base and the extension are equal, false otherwise</returns>
		public static bool operator ==(SimSlot lhs, SimSlot rhs)
        {
            return lhs.Equals(rhs);
        }
        /// <summary>
        /// Inequality operator for slots.
        /// </summary>
        /// <param name="lhs">left hand side slot</param>
        /// <param name="rhs">right hand side slot</param>
        /// <returns>true in case either the base or the extension of the slots are not equal, false otherwise</returns>
		public static bool operator !=(SimSlot lhs, SimSlot rhs)
        {
            return !lhs.Equals(rhs);
        }

        #endregion

    }
}