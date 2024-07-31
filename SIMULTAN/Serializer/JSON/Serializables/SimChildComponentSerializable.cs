using SIMULTAN.Data.Components;
using System;

namespace SIMULTAN.Serializer.JSON
{

    /// <summary>
    /// Serializable class for the <see cref="SimChildComponentEntry"/>
    /// </summary>
    public class SimChildComponentSerializable
    {
        /// <summary>
        /// Containing slot
        /// </summary>
        public SimSlotSerializable Slot { get; set; }
        /// <summary>
        /// Parent component
        /// </summary>
        public SimComponentSerializable Component { get; set; }
        /// <summary>
        /// Creates a new instance of SimChildComponentSerializable
        /// </summary>
        /// <param name="childComp">The instance itself</param>
        public SimChildComponentSerializable(SimChildComponentEntry childComp)
        {
            this.Slot = new SimSlotSerializable(childComp.Slot);
            this.Component = new SimComponentSerializable(childComp.Component);
        }

        //DO NOT USE. Only required for the XMLSerializer class to operate on this type
        private SimChildComponentSerializable() { throw new NotImplementedException(); }
    }

}
