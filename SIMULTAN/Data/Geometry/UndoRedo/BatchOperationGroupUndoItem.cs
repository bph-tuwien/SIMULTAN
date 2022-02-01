using SIMULTAN.Utils.UndoRedo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.Geometry
{
    /// <summary>
    /// A group undo item that handles it's children in a batch-operation
    /// </summary>
    public class BatchOperationGroupUndoItem : IUndoItem
    {
        /// <summary>
        /// Returns a list of contained undo items. Do not change this after registering to the undo manager
        /// </summary>
        public List<IUndoItem> Items { get; private set; }

        /// <summary>
        /// Stores the GeometryModel this undoitem operates on
        /// </summary>
        public GeometryModelData Model { get; private set; }


        /// <summary>
        /// Initializes a new instance of the GroupUndoItem class
        /// </summary>
        public BatchOperationGroupUndoItem(GeometryModelData model) : this(model, new List<IUndoItem>()) { }
        /// <summary>
        /// Initializes a new instance of the GroupUndoItem class
        /// </summary>
        /// <param name="model">The model in which the batch operation is started</param>
        /// <param name="items">The list of IUndoItems</param>
        public BatchOperationGroupUndoItem(GeometryModelData model, List<IUndoItem> items)
        {
            this.Items = items;
            this.Model = model;
        }

        /// <inheritdoc/>
        public UndoExecutionResult Execute()
        {
            var anyRemoved = false;

            Model.StartBatchOperation();
            for (int i = 0; i < Items.Count; ++i)
            {
                var result = Items[i].Execute();
                if (result == UndoExecutionResult.Failed)
                {
                    anyRemoved = true;
                    Items.RemoveAt(i);
                    i--;
                }
            }
            Model.EndBatchOperation();

            if (anyRemoved && Items.Count > 0)
                return UndoExecutionResult.PartiallyExecuted;
            else if (Items.Count > 0)
                return UndoExecutionResult.Executed;
            else
                return UndoExecutionResult.Failed;
        }
        /// <inheritdoc/>
        public void Redo()
        {
            Model.StartBatchOperation();
            for (int i = 0; i < Items.Count; ++i)
                Items[i].Redo();
            Model.EndBatchOperation();
        }
        /// <inheritdoc/>
        public void Undo()
        {
            Model.StartBatchOperation();
            for (int i = Items.Count - 1; i >= 0; --i)
                Items[i].Undo();
            Model.EndBatchOperation();
        }
    }
}
