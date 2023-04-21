using SIMULTAN.Utils.UndoRedo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.Geometry
{
    /// <summary>
    /// UndoItem to remove a <see cref="SimGeometryRelation"/>
    /// </summary>
    public class RemoveGeometryRelationUndoItem : IUndoItem
    {
        private SimGeometryRelation relation;
        private SimGeometryRelationCollection relations;

        /// <summary>
        /// Creates a new <see cref="RemoveGeometryRelationUndoItem"/>
        /// </summary>
        /// <param name="relation">The relation to remove</param>
        public RemoveGeometryRelationUndoItem(SimGeometryRelation relation)
        {
            if (relation == null)
                throw new ArgumentNullException(nameof(relation));
            if (relation.Factory == null)
                throw new ArgumentException("Relation must be in a factory to be able to remove it.");

            this.relation = relation;
            this.relations = relation.Factory;
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
            relations.Remove(relation);
        }

        /// <inheritdoc/>
        public void Undo()
        {
            relations.Add(relation);
        }
    }
}
