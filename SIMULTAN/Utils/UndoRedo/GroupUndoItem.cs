using System.Collections.Generic;

namespace SIMULTAN.Utils.UndoRedo
{
    /// <summary>
    /// A IUndoItem for grouping other items for a common undo operation
    /// </summary>
    public class GroupUndoItem : IUndoItem
    {
        private List<IUndoItem> items;
        /// <summary>
        /// Returns a list of contanied undo items. Do not change this after registering to the undo manager
        /// </summary>
        public List<IUndoItem> Items { get { return items; } }

        /// <summary>
        /// Initializes a new instance of the GroupUndoItem class
        /// </summary>
        public GroupUndoItem() : this(new List<IUndoItem>()) { }
        /// <summary>
        /// Initializes a new instance of the GroupUndoItem class
        /// </summary>
        /// <param name="items">The list of IIUndoItems</param>
        public GroupUndoItem(List<IUndoItem> items)
        {
            this.items = items;
        }

        /// <inheritdoc/>
        public UndoExecutionResult Execute()
        {
            bool anyRemoved = false;

            for (int i = 0; i < items.Count; ++i)
            {
                var result = items[i].Execute();
                if (result == UndoExecutionResult.Failed)
                {
                    anyRemoved = true;
                    items.RemoveAt(i);
                    i--;
                }
            }

            if (anyRemoved && items.Count > 0)
                return UndoExecutionResult.PartiallyExecuted;
            else if (items.Count > 0)
                return UndoExecutionResult.Executed;
            else
                return UndoExecutionResult.Failed; //Needed to remove the group item when non of the children could be executed
        }
        /// <inheritdoc/>
        public void Redo()
        {
            for (int i = 0; i < items.Count; ++i)
                items[i].Redo();
        }
        /// <inheritdoc/>
        public void Undo()
        {
            for (int i = items.Count - 1; i >= 0; --i)
                items[i].Undo();
        }
    }
}
