using System.Collections.Generic;

namespace SIMULTAN.Utils.UndoRedo
{
    /// <summary>
    /// Defines which operation has to be reverted
    /// </summary>
    public enum UndoRedoAction
    {
        /// <summary>
        /// A insert (or add) operation
        /// </summary>
        Insert,
        /// <summary>
        /// A remove action
        /// </summary>
        Remove,
        /// <summary>
        /// A clear action
        /// </summary>
        Clear
    }

    /// <summary>
    /// Methods for creating CollectionUndoItem instances
    /// </summary>
    public static class CollectionUndoItem
    {
        /// <summary>
        /// Creates an UndoItem for a Clear operation
        /// </summary>
        /// <typeparam name="T">The type of the collection items</typeparam>
        /// <param name="list">The collection</param>
        /// <returns>The created collection item</returns>
        public static CollectionUndoItem<T> Clear<T>(IList<T> list)
        {
            return new CollectionUndoItem<T>(list, UndoRedoAction.Clear, 0, new List<T>(list), -1, null);
        }
        /// <summary>
        /// Creates an UndoItem for a Add operation
        /// </summary>
        /// <typeparam name="T">The type of the collection items</typeparam>
        /// <param name="list">The collection</param>
        /// <param name="item">The item to add</param>
        /// <returns>The created collection item</returns>
        public static CollectionUndoItem<T> Add<T>(IList<T> list, T item)
        {
            return new CollectionUndoItem<T>(list, UndoRedoAction.Insert, -1, null, list.Count, new List<T> { item });
        }
        /// <summary>
        /// Creates an UndoItem for a Insert operation
        /// </summary>
        /// <typeparam name="T">The type of the collection items</typeparam>
        /// <param name="list">The collection</param>
        /// <param name="item">The item to insert</param>
        /// <param name="idx">The position where the item will be inserted</param>
        /// <returns>The created collection item</returns>
        public static CollectionUndoItem<T> Insert<T>(IList<T> list, T item, int idx)
        {
            return new CollectionUndoItem<T>(list, UndoRedoAction.Insert, -1, null, idx, new List<T> { item });
        }
        /// <summary>
        /// Creates an UndoItem for a Remove operation
        /// </summary>
        /// <typeparam name="T">The type of the collection items</typeparam>
        /// <param name="list">The collection</param>
        /// <param name="item">The item to remove</param>
        /// <returns>The created collection item</returns>
        public static CollectionUndoItem<T> Remove<T>(IList<T> list, T item)
        {
            var idx = list.IndexOf(item);
            return new CollectionUndoItem<T>(list, UndoRedoAction.Remove, idx, new List<T> { item }, -1, null);
        }
        /// <summary>
        /// Creates an UndoItem for a RemoveAt operation
        /// </summary>
        /// <typeparam name="T">The type of the collection items</typeparam>
        /// <param name="list">The collection</param>
        /// <param name="idx">The position where the item will be removed</param>
        /// <returns>The created collection item</returns>
        public static CollectionUndoItem<T> RemoveAt<T>(IList<T> list, int idx)
        {
            return new CollectionUndoItem<T>(list, UndoRedoAction.Remove, idx, new List<T> { list[idx] }, -1, null);
        }
    }

    /// <summary>
    /// A IUndoItem for collection modifications.
    /// </summary>
    /// <typeparam name="T">The item type of the collection</typeparam>
    public class CollectionUndoItem<T> : IUndoItem
    {
        private UndoRedoAction action;
        private IList<T> list;
        private IList<T> newItems, oldItems;
        private int newStartIndex;
        private int oldStartIndex;

        /// <summary>
        /// Initializes a new instance of the CollectionUndoItem class
        /// </summary>
        /// <param name="list">The collection</param>
        /// <param name="action">The action that should be undo-able</param>
        /// <param name="oldStartIndex">Old start index (for clear, remove)</param>
        /// <param name="oldItems">A list of old items</param>
        /// <param name="newStartIndex">New start index</param>
        /// <param name="newItems">A list of new items</param>
        public CollectionUndoItem(IList<T> list, UndoRedoAction action, int oldStartIndex, IList<T> oldItems, int newStartIndex, IList<T> newItems)
        {
            this.list = list;
            this.action = action;
            this.oldStartIndex = oldStartIndex;
            this.newStartIndex = newStartIndex;
            this.oldItems = oldItems;
            this.newItems = newItems;

            Redo();
        }

        /// <inheritdoc/>
        public UndoExecutionResult Execute()
        {
            //Redo();
            return UndoExecutionResult.Executed;
        }

        /// <inheritdoc/>
        public void Redo()
        {
            switch (action)
            {
                case UndoRedoAction.Clear:
                    list.Clear();
                    break;
                case UndoRedoAction.Insert:
                    {
                        for (int i = 0; i < this.newItems.Count; ++i)
                            list.Insert(this.newStartIndex + i, this.newItems[i]);
                    }
                    break;
                case UndoRedoAction.Remove:
                    {
                        for (int i = 0; i < oldItems.Count; ++i)
                            list.RemoveAt(this.oldStartIndex);
                    }
                    break;
            }
        }

        /// <inheritdoc/>
        public void Undo()
        {
            switch (action)
            {
                case UndoRedoAction.Clear:
                    {
                        for (int i = 0; i < this.oldItems.Count; ++i)
                            list.Insert(this.oldStartIndex + i, this.oldItems[i]);
                    }
                    break;
                case UndoRedoAction.Insert:
                    {
                        for (int i = 0; i < this.newItems.Count; ++i)
                            list.RemoveAt(this.newStartIndex);
                    }
                    break;
                case UndoRedoAction.Remove:
                    {
                        for (int i = 0; i < this.oldItems.Count; ++i)
                            list.Insert(this.oldStartIndex + i, this.oldItems[i]);
                    }
                    break;
            }
        }
    }
}
