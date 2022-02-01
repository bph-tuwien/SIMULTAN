namespace SIMULTAN.Utils.UndoRedo
{
    /// <summary>
    /// Defines the result of a undo/redo operation
    /// </summary>
    public enum UndoExecutionResult
    {
        /// <summary>
        /// The operation has been fully executed
        /// </summary>
        Executed,
        /// <summary>
        /// Parts of the operation have been executed (e.g. in a GroupOperation)
        /// </summary>
        PartiallyExecuted,
        /// <summary>
        /// No operation has been executed
        /// </summary>
        Failed,
    }

    /// <summary>
    /// Interface for all undo items
    /// </summary>
    public interface IUndoItem
    {
        /// <summary>
        /// Called when the item is executed for the first time
        /// </summary>
        /// <returns>The execution result. Partially should only be returned in case of grouping undo items.</returns>
        UndoExecutionResult Execute();
        /// <summary>
        /// Executed when the item is undone
        /// </summary>
        void Undo();
        /// <summary>
        /// Executed when the item is redone
        /// </summary>
        void Redo();
    }
}
