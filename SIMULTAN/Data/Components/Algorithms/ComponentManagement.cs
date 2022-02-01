using SIMULTAN.Data.Users;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SIMULTAN.Data.Components
{
    /// <summary>
    /// Provides helper methods for managing components.
    /// For Example:
    ///  - Finding slot names, managing access, ...
    /// </summary>
    public static class ComponentManagement
    {
        #region Referenced component Management: adding, removing, slot renaming

        /// <summary>
		/// Returns a list of available slots
		/// </summary>
        /// <param name="component">The component</param>
		/// <param name="availableSlotsWithoutExtension">The slot types which are available to choose from (slots without extension)</param>
		/// <returns>An unused slot (slot with extension)</returns>
        public static SimSlot FindAvailableReferenceSlot(this SimComponent component, List<SimSlotBase> availableSlotsWithoutExtension)
        {
            var alreadyUsedSlots = component.ReferencedComponents.Select(x => x.Slot).ToHashSet();

            int i = 0;
            while (true)
            {
                foreach (var avSlot in availableSlotsWithoutExtension)
                {
                    var slotWithExtension = new SimSlot(avSlot, i.ToString());
                    if (!alreadyUsedSlots.Contains(slotWithExtension))
                        return slotWithExtension;
                }

                i++;
            }
        }

        #endregion
    }
}
