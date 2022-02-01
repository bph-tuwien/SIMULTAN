using SIMULTAN.Data.Assets;
using SIMULTAN.Data.FlowNetworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.Components
{
    /// <summary>
    /// Contains methods that operate on component collections
    /// </summary>
    public static class ComponentFactoryAlgorithms
    {
        #region COMPONENTS W Geometry

        /// <summary>
        /// Examines the component record recursively for any components associated with
        /// geometry and returns a flat list of those who are.
        /// </summary>
        /// <param name="components">the calling component factory</param>
        /// <returns>a collection of all found components on record, associated with geometry</returns>
        public static IEnumerable<SimComponent> GetAllAssociatedWithGeometry(this SimComponentCollection components)
        {
            List<SimComponent> all_comps = new List<SimComponent>();
            foreach (SimComponent c in components)
            {
                var c_found = c.GetAllAssociatedWithGeometry();
                all_comps.AddRange(c_found);
            }

            return all_comps;
        }

        #endregion

        #region Component Structure Update: from external sources (used in the NEW GeometryViewer)

        /// <summary>
        /// Adds a reference to component '_to_be_referenced' in component '_comp' with a slot
        /// name based on its declared relationship to geometry (e.g. DESCRIBES or ALIGNED_WITH, etc.).
        /// If the component is already referenced, nothing happens. Self-references are not admissible.
        /// </summary>
        /// <param name="_factory">the calling component factory</param>
        /// <param name="_comp">the component receiving the reference</param>
        /// <param name="_to_be_referenced">the component that is to be referenced</param>
        public static void AddReferenceBasedOnGeometry(this SimComponentCollection _factory, SimComponent _comp, SimComponent _to_be_referenced)
        {
            if (_comp == null || _to_be_referenced == null) return;

            SimSlot reference_slot = _comp.GenerateFullSlotFor(_to_be_referenced.InstanceType);
            if (reference_slot == SimSlot.Invalid) return; // something went very wrong here

            // check the necessity for adding the reference
            var fitting_ref = _comp.ReferencedComponents.FirstOrDefault(x => x.TargetId == _to_be_referenced.Id);
            if (fitting_ref == null)
            {
                _comp.ReferencedComponents.Add(new SimComponentReference(reference_slot, _to_be_referenced));
            }
        }

        [Obsolete("Should be integrated in the future reference collection")]
        private static SimSlot GenerateFullSlotFor(this SimComponent _comp, SimInstanceType _type)
        {
            int slot_counter = 0;
            int iteration_guard = 100;

            SimSlot slot = new SimSlot(ComponentUtils.InstanceTypeToSlotBase(_type), "AG" + slot_counter.ToString());
            var fitting_slot = _comp.ReferencedComponents.FirstOrDefault(x => x.Slot == slot);
            while (fitting_slot != null && slot_counter < iteration_guard)
            {
                slot_counter++;
                slot = new SimSlot(ComponentUtils.InstanceTypeToSlotBase(_type), "AG" + slot_counter.ToString());
                fitting_slot = _comp.ReferencedComponents.FirstOrDefault(x => x.Slot == slot);
            }
            if (fitting_slot != null)
                return SimSlot.Invalid; // something went very wrong!

            return slot;
        }

        /// <summary>
        /// Removes the given component from the references of the calling component.
        /// </summary>
        /// <param name="_factory">the calling component factory</param>
        /// <param name="_comp">the calling component</param>
        /// <param name="_to_be_unreferenced">the component to be un-referenced</param>
        public static void RemoveReferenceBasedOnGeometry(this SimComponentCollection _factory, SimComponent _comp, SimComponent _to_be_unreferenced)
        {
            if (_comp == null || _to_be_unreferenced == null) return;

            var fitting_ref = _comp.ReferencedComponents.FirstOrDefault(x => x.TargetId == _to_be_unreferenced.Id);
            if (fitting_ref != null)
                _comp.ReferencedComponents.Remove(fitting_ref);
        }

        #endregion

        #region Component Structure Update: from placement in geometry

        /// <summary>
        /// Propagates the realization state from the components with relationship to geometry
        /// type CONTAINED_IN to those of type CONNECTING.
        /// </summary>
        /// <param name="_comp">the affected component with relationship to geometry of type CONTAINED_IN</param>
        public static void UpdateConnectivity(this SimComponent _comp)
        {
            foreach (SimComponentInstance inst in _comp.Instances)
            {
                var networkPlacement = (SimInstancePlacementNetwork)inst.Placements.FirstOrDefault(x => x is SimInstancePlacementNetwork);

                if (networkPlacement != null && networkPlacement.NetworkElement is SimFlowNetworkNode n)
                    n.UpdateAdjacentEdgeRealization();
            }
        }

        #endregion
    }
}
