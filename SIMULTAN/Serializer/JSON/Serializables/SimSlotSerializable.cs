using SIMULTAN.Data.Components;
using System;

namespace SIMULTAN.Serializer.JSON
{
    /// <summary>
    /// Serializable class for the <see cref="SimSlot"/>
    /// </summary>
    public class SimSlotSerializable
    {
        /// <summary>
        /// Slot
        /// </summary>
        public SimTaxonomyEntryReferenceSerializable Slot { get; set; }
        /// <summary>
        /// Slot extension
        /// </summary>
        public string SlotExtension { get; set; }

        /// <summary>
        /// Creates a new instance of SimSlotSerializable
        /// </summary>
        /// <param name="slot">The instance itself</param>
        public SimSlotSerializable(SimSlot slot)
        {
            this.Slot = new SimTaxonomyEntryReferenceSerializable(slot.SlotBase);
            this.SlotExtension = slot.SlotExtension;
        }

        //DO NOT USE. Only required for the XMLSerializer class to operate on this type
        private SimSlotSerializable() { throw new NotImplementedException(); }
    }
}
