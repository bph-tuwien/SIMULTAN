using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.Components
{
    /// <summary>
    /// Defines the role of an object in the context of another object.
    /// </summary>
    [DebuggerDisplay("Slot {SlotBase} [{SlotExtension}]")]
    public struct SimSlot : IComparable<SimSlot>, IEquatable<SimSlot>
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
        public SimSlotBase SlotBase { get; }
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
        public SimSlot(SimSlotBase slotBase, string slotExtension)
        {
            this.SlotBase = slotBase;
            this.SlotExtension = slotExtension == null ? string.Empty : slotExtension;
        }

        /// <summary>
        /// Initializes an object of type Slot.
        /// </summary>
        /// <param name="slotBase">The slot base</param>
        /// <param name="slotExtension">The slot extension</param>
        public SimSlot(string slotBase, string slotExtension)
        {
            this.SlotBase = new SimSlotBase(slotBase);
            this.SlotExtension = slotExtension == null ? string.Empty : slotExtension;
        }

        /// <summary>
        /// Copies a slot into a new instance.
        /// </summary>
        /// <param name="original">the original slot</param>
        public SimSlot(SimSlot original)
        {
            this.SlotBase = original.SlotBase;
            this.SlotExtension = original.SlotExtension;
        }

        #region Interfaces

        /// <summary>
        /// Implementation of the IComparable interface.
        /// </summary>
        /// <param name="other">the other DisplayableProductDefinition to compare to</param>
        /// <returns>the result</returns>
        public int CompareTo(SimSlot other)
        {
            var baseCompare = this.SlotBase.CompareTo(other.SlotBase);
            if (baseCompare == 0)
                return this.SlotExtension.CompareTo(other.SlotExtension);
            return baseCompare;
        }

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

        /// <summary>
        /// Serializes the slot into a string. Use <see cref="FromSerializerString(string)"/> to deserialize the result.
        /// </summary>
        /// <returns>A serialized representation of the slot</returns>
        public string ToSerializerString()
        {
            return this.SlotBase + ComponentUtils.COMP_SLOT_DELIMITER + this.SlotExtension;
        }

        /// <summary>
        /// Deserializes a string created by the <see cref="ToSerializerString"/> method into a <see cref="SimSlot"/>
        /// </summary>
        /// <param name="serializerString">The serialized string representation</param>
        /// <returns>The slot described by the serialized string</returns>
        public static SimSlot FromSerializerString(string serializerString)
        {
            if (string.IsNullOrEmpty(serializerString))
                throw new ArgumentException("Invalid slot format");

            var splited = ComponentUtils.SplitExtensionSlot(serializerString);

            if (!ComponentUtils.COMP_SLOTS_ALL.Contains(splited.slot))
                throw new ArgumentException("Invalid base slot");

            return new SimSlot(new SimSlotBase(splited.slot), splited.extension);
        }
    }
}
