using SIMULTAN.Data.Taxonomy;
using SIMULTAN.Utils.UndoRedo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.Geometry
{
    /// <summary>
    /// UndoItem to set the type of a <see cref="SimGeometryRelation"/>
    /// </summary>
    public class SetGeometryRelationTypeUndoItem : IUndoItem
    {
        private SimGeometryRelation relation;
        private SimTaxonomyEntry oldRelationType;
        private SimTaxonomyEntry newRelationType;

        /// <summary>
        /// Creates a new <see cref="SetGeometryRelationTypeUndoItem"/>
        /// </summary>
        /// <param name="relation">The relation to set the type for</param>
        /// <param name="relationType">The type</param>
        /// <exception cref="ArgumentNullException">If an argument is null</exception>
        public SetGeometryRelationTypeUndoItem(SimGeometryRelation relation, SimTaxonomyEntry relationType)
        {
            if (relation == null)
                throw new ArgumentNullException(nameof(relation));

            this.relation = relation;
            this.newRelationType = relationType;
            this.oldRelationType = relation.RelationType == null ? null : relation.RelationType.Target;
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
            relation.RelationType = newRelationType == null ? null : new SimTaxonomyEntryReference(newRelationType);
        }

        /// <inheritdoc/>
        public void Undo()
        {
            relation.RelationType = oldRelationType == null ? null : new SimTaxonomyEntryReference(oldRelationType);
        }
    }
}
