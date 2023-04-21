using SIMULTAN.Utils.UndoRedo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.Geometry
{
    /// <summary>
    /// UndoItem to add a geometry relation
    /// </summary>
    public class AddGeometryRelationUndoItem : IUndoItem
    {
        private SimGeometryRelation relation;
        private SimGeometryRelationCollection relations;

        /// <summary>
        /// Creates a new <see cref="AddGeometryRelationUndoItem"/>
        /// </summary>
        /// <param name="relation">The relation to add</param>
        /// <param name="relations">The relations where it should be added to</param>
        /// <exception cref="ArgumentNullException">If an argument is null</exception>
        public AddGeometryRelationUndoItem(SimGeometryRelation relation, SimGeometryRelationCollection relations)
        {
            if (relation == null)
                throw new ArgumentNullException(nameof(relation));
            if (relations == null)
                throw new ArgumentNullException(nameof(relations));

            this.relation = relation;
            this.relations = relations;
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
            relations.Add(relation);
        }

        /// <inheritdoc/>
        public void Undo()
        {
            relations.Remove(relation);
        }
    }
}
