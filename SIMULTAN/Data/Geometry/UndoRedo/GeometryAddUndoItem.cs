using SIMULTAN.Utils.UndoRedo;
using System.Collections.Generic;

namespace SIMULTAN.Data.Geometry
{
    /// <summary>
    /// UndoItem  for undoing addition of geometries (for example, during connect or extrude)
    /// The geometries have to be attached to the model already. They are NOT touched by Execute.
    /// </summary>
    public class GeometryAddUndoItem : IUndoItem
    {
        /// <summary>
        /// Returns the created geometry that is undone/redone by this item
        /// </summary>
        public List<BaseGeometry> CreatedGeometry { get { return createdGeometry; } }
        private List<BaseGeometry> createdGeometry;

        private GeometryModelData model;

        /// <summary>
        /// Initializes a new instance of the GeometryAddUndoItem class
        /// </summary>
        /// <param name="createdGeometry">The geometries created by this instance. The geometries have to be attached to the model.</param>
        /// <param name="model">The model on which add and remove should operate</param>
        public GeometryAddUndoItem(List<BaseGeometry> createdGeometry, GeometryModelData model)
        {
            this.createdGeometry = createdGeometry;
            this.model = model;
        }

        /// <inheritdoc/>
        public UndoExecutionResult Execute()
        {
            return UndoExecutionResult.Executed;
        }

        /// <inheritdoc/>
        public void Redo()
        {
            model.StartBatchOperation();
            for (int i = 0; i < this.createdGeometry.Count; ++i)
                this.createdGeometry[i].AddToModel();
            model.EndBatchOperation();
        }

        /// <inheritdoc/>
        public void Undo()
        {
            model.StartBatchOperation();
            for (int i = this.createdGeometry.Count - 1; i >= 0; --i)
                this.createdGeometry[i].RemoveFromModel();
            model.EndBatchOperation();
        }
    }
}
