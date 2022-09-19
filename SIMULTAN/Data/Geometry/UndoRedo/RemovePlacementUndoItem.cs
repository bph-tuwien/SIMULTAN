using SIMULTAN.Data.Components;
using SIMULTAN.Utils.UndoRedo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.Geometry
{
    /// <summary>
    /// UndoItem for removing a placement from a component instance
    /// </summary>
    public class RemovePlacementUndoItem : IUndoItem
    {

        private SimInstancePlacement placement;
        private SimComponentInstance instance;
        private SimComponent component;

        /// <summary>
        /// Creates the new UndoItem
        /// </summary>
        /// <param name="placement">The placement that should be removed</param>
        public RemovePlacementUndoItem(SimInstancePlacement placement)
        {
            this.placement = placement;
            // remember instance and component
            instance = placement.Instance;
            component = instance.Component;
        }

        /// <inheritdoc/>
        public UndoExecutionResult Execute()
        {
            Redo();
            return UndoExecutionResult.Executed;
        }

        /// <inheritdoc/>
        public void Redo()
        {
            // remove placement
            if (!instance.Placements.Remove(placement))
            {
                throw new Exception("Placement could not be removed");
            }
            // remove instance if it does not haven any placements anymore
            if(!instance.Placements.Any())
            {
                if(!component.Instances.Remove(instance))
                {
                    throw new Exception("Instance could not be removed");
                }
            }
        }

        /// <inheritdoc/>
        public void Undo()
        {
            // re-add the instance if it was removed
            if(!component.Instances.Contains(instance))
            {
                component.Instances.Add(instance);
            }
            // add the placement again
            instance.Placements.Add(placement);
        }
    }
}
