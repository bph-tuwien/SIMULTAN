using SIMULTAN.Utils.UndoRedo;
using System.Collections.Generic;

namespace SIMULTAN.Data.Geometry
{
    /// <summary>
    /// Removes previously generated geometry. 
    /// </summary>
    public class GeometryRemoveUndoItem : IUndoItem
    {
        /// <summary>
        /// Returns the list of deleted geometries
        /// </summary>
        public List<BaseGeometry> DeletedGeometry { get { return deletedGeometry; } }
        private List<BaseGeometry> deletedGeometry;
        private GeometryModelData model;

        /// <summary>
        /// Initializes a new instance of the GeometryRemoveUndoItem class
        /// </summary>
        /// <param name="deletedGeometry">The geometries deleted by this instance. The geometries have to be removed from the model before attaching them here.</param>
        /// <param name="model">The model on which add and remove should operate</param>
        public GeometryRemoveUndoItem(List<BaseGeometry> deletedGeometry, GeometryModelData model)
        {
            this.deletedGeometry = deletedGeometry;
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
            for (int i = 0; i < this.deletedGeometry.Count; ++i)
                this.deletedGeometry[i].RemoveFromModel();
            model.EndBatchOperation();
        }

        /// <inheritdoc/>
        public void Undo()
        {
            model.StartBatchOperation();
            for (int i = this.deletedGeometry.Count - 1; i >= 0; --i)
                this.deletedGeometry[i].AddToModel();
            model.EndBatchOperation();
        }
    }
}
