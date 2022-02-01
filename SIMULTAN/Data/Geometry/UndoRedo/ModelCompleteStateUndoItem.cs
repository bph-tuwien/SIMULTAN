using SIMULTAN;
using SIMULTAN.Utils.UndoRedo;
using System;
using System.Collections.ObjectModel;

namespace SIMULTAN.Data.Geometry
{
    /// <summary>
    /// Undos a model operation by replacing the geometry in the model with an old state.
    /// </summary>
    /// Usage: Clone the model before the operation, perform the operation and pass the old state to the undo item
    public class ModelCompleteStateUndoItem : IUndoItem
    {
        private GeometryModelData newGeometry, oldGeometry;
        private GeometryModel model;

        /// <summary>
        /// Initializes a new instance of the ModelCompleteStateUndoItem class
        /// </summary>
        /// <param name="newGeometry">The new geometry data</param>
        /// <param name="targetModel">An old geometry data</param>
        public ModelCompleteStateUndoItem(GeometryModelData newGeometry, GeometryModel targetModel)
        {
            if (newGeometry == null)
                throw new ArgumentNullException(string.Format("{0} may not be null", nameof(newGeometry)));
            if (targetModel == null)
                throw new ArgumentNullException(string.Format("{0} may not be null", nameof(targetModel)));

            this.newGeometry = newGeometry;
            this.oldGeometry = targetModel.Geometry;
            this.model = targetModel;
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
            model.Geometry = newGeometry;
        }

        /// <inheritdoc/>
        public void Undo()
        {
            model.Geometry = oldGeometry;
        }
    }
}
