using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.Components
{
    /// <summary>
    /// Stores the base of a slot, usually a string describing it's function.
    /// Only values contained in <see cref="ComponentUtils.COMP_SLOTS_ALL"/> are accepted.
    /// This class is often used in combination with the <see cref="SimSlot"/> class.
    /// </summary>
    [DebuggerDisplay("SlotBase {Base}")]
    public struct SimSlotBase : IComparable<SimSlotBase>, IEquatable<SimSlotBase>
    {
        /// <summary>
        /// Returns an invalid slot. The <see cref="Base"/> is set to null.
        /// </summary>
        public static SimSlotBase Invalid { get { return new SimSlotBase(); } }

        /// <summary>
        /// Stores the string value of the slot base
        /// </summary>
        public string Base { get; }

        /// <summary>
        /// Initializes a new instance of the SlotBase class
        /// </summary>
        /// <param name="slotBase">The slot base string. Only values contained in <see cref="ComponentUtils.COMP_SLOTS_ALL"/> are accepted.</param>
        public SimSlotBase(string slotBase)
        {
            if (slotBase == null)
                throw new ArgumentNullException(nameof(slotBase));
            if (!ComponentUtils.COMP_SLOTS_ALL.Contains(slotBase))
                throw new ArgumentException("Invalid base slot");

            this.Base = slotBase;
        }

        /// <inheritdoc/>
        public int CompareTo(SimSlotBase other)
        {
            return this.Base.CompareTo(other.Base);
        }
        /// <inheritdoc/>
        public bool Equals(SimSlotBase other)
        {
            return Base == other.Base;
        }
        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (obj is SimSlotBase sb)
                return this.Equals(sb);
            return false;
        }
        /// <inheritdoc/>
        public static bool operator ==(SimSlotBase rhs, SimSlotBase lhs)
        {
            return rhs.Equals(lhs);
        }
        /// <inheritdoc/>
        public static bool operator !=(SimSlotBase rhs, SimSlotBase lhs)
        {
            return !(rhs == lhs);
        }
        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return Base.GetHashCode();
        }
        /// <inheritdoc/>
        public override string ToString()
        {
            return this.Base;
        }
    }
}
