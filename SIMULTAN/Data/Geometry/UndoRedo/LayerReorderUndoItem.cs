using SIMULTAN.Utils.UndoRedo;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.Geometry
{
    /// <summary>
    /// UndoItem for moving a layer to a new parent
    /// </summary>
    public class LayerReorderUndoItem : IUndoItem
    {
        /// <summary>
        /// The Old Position of the UndoItem. Used to correct sorting when multiple of these are
        /// used in a GroupItem.
        /// </summary>
        public int OldPos
        {
            get { return oldPos; }
        }

        private Layer layer;
        private Layer oldParent, newParent;
        private int oldPos, newPos;

        /// <summary>
        /// Initializes a new instance of the LayerReorderUndoItem class. The class handles reordering layers.
        /// If used in a GroupUndoItem, soft descending by the OldPos property for correct undo.
        /// </summary>
        /// <param name="layer">The layer</param>
        /// <param name="oldParent">The old parent (can be null when directly attached to a GeometryModel)</param>
        /// <param name="oldPos">The old position (index) of the item in the original list.</param>
        /// <param name="newParent">The new parent (can be null when directly attached to a GeometryModel)</param>
        /// <param name="newPos">The new position (index) of the layer. Inserts at this index.</param>
        public LayerReorderUndoItem(Layer layer, Layer oldParent, int oldPos, Layer newParent, int newPos)
        {
            this.layer = layer;
            this.oldParent = oldParent;
            this.newParent = newParent;
            this.oldPos = oldPos;
            this.newPos = newPos;
            if (oldParent == newParent && oldPos < newPos)
            {
                this.newPos--;
            }
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
            MoveFromTo(oldParent, oldPos, newParent, newPos);
        }
        /// <inheritdoc/>
        public void Undo()
        {
            MoveFromTo(newParent, newPos, oldParent, oldPos);
        }

        private void MoveFromTo(Layer from, int fromPos, Layer to, int toPos)
        {
            bool colorFromParent = layer.Color.IsFromParent;


            if (from == to)
            {
                if (fromPos != toPos)
                {
                    if (from == null && to == null)
                    {
                        layer.Model.Layers.Move(fromPos, toPos);
                    }
                    else
                    {
                        from.Layers.Move(fromPos, toPos);
                    }
                }
            }
            else
            {
                if (from == null)
                {
                    layer.Model.Layers.Remove(layer);
                }
                else
                {
                    from.Layers.Remove(layer);
                }

                if (to == null)
                {

                    layer.Model.Layers.Insert(toPos, layer);
                }
                else
                {
                    to.Layers.Insert(toPos, layer);
                    layer.Color.IsFromParent = colorFromParent;
                }
            }
        }
    }
}
