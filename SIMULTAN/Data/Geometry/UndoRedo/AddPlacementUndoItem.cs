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
    /// UndoItem for adding an InstancePlacementGeometry to a Component
    /// </summary>
    public class AddPlacementUndoItem : IUndoItem
    {

        private SimComponentInstance instance;
        private BaseGeometry geometry;
        private SimComponent component;
        private SimInstancePlacementGeometry placement;

        /// <summary>
        /// Creates the new undo item
        /// </summary>
        /// <param name="geometry">The geometry to add the placement for</param>
        /// <param name="component">The component to add the placement and instance to</param>
        public AddPlacementUndoItem(BaseGeometry geometry, SimComponent component)
        {
            this.geometry = geometry;
            this.component = component;
        }

        /// <inheritdoc/>
        public UndoExecutionResult Execute()
        {
            // first create the new instance and remember the placement
            instance = new SimComponentInstance(component.InstanceType, geometry.ModelGeometry.Model.File.Key, geometry.Id);
            placement = instance.Placements.FirstOrDefault() as SimInstancePlacementGeometry;
            Redo();
            return UndoExecutionResult.Executed;
        }

        /// <inheritdoc/>
        public void Redo()
        {
            component.Instances.Add(instance);
        }

        /// <inheritdoc/>
        public void Undo()
        {
            // lookup instance again, could have changed due to undo/redo
            instance = component.Instances.FirstOrDefault(x => x.Placements.Any(p => p is SimInstancePlacementGeometry geop
                && geop.FileId == placement.FileId && geop.GeometryId == placement.GeometryId));
            if (!component.Instances.Remove(instance))
            {
                throw new Exception("Instance could not be removed.");
            }
        }
    }
}
