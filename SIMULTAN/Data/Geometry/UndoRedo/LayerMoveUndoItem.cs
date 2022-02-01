using SIMULTAN.Utils.UndoRedo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.Geometry
{
    /// <summary>
    /// UndoItem for moving a layer to a new parent
    /// </summary>
    public class LayerMoveUndoItem : IUndoItem
    {
        private Layer layer;
        private Layer oldParent, newParent;

        /// <summary>
        /// Initializes a new instance of the LayerMoveUndoItem class. The class handles the initial move
        /// </summary>
        /// <param name="layer">The layer</param>
        /// <param name="oldParent">The old parent (can be null when directly attached to a GeometryModel)</param>
        /// <param name="newParent">The new parent (can be null when directly attached to a GeometryModel)</param>
        public LayerMoveUndoItem(Layer layer, Layer oldParent, Layer newParent)
        {
            this.layer = layer;
            this.oldParent = oldParent;
            this.newParent = newParent;
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
            MoveFromTo(oldParent, newParent);
        }
        /// <inheritdoc/>
        public void Undo()
        {
            MoveFromTo(newParent, oldParent);
        }

        private void MoveFromTo(Layer from, Layer to)
        {
            bool colorFromParent = layer.Color.IsFromParent;

            if (from == null)
                layer.Model.Layers.Remove(layer);
            else
                from.Layers.Remove(layer);

            if (to == null)
                layer.Model.Layers.Add(layer);
            else
            {
                to.Layers.Add(layer);
                layer.Color.IsFromParent = colorFromParent;
            }
        }
    }
}
