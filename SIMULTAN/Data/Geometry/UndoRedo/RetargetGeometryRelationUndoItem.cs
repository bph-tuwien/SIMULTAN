using SIMULTAN.Utils.UndoRedo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.Geometry
{
    /// <summary>
    /// UndoItem to re-target a <see cref="SimGeometryRelation"/>
    /// </summary>
    public class RetargetGeometryRelationUndoItem : IUndoItem
    {
        /// <summary>
        /// Which property to re-target
        /// </summary>
        public enum TargetProperty
        {
            /// <summary>
            /// Source property will be set
            /// </summary>
            Source,
            /// <summary>
            /// Target property will be set
            /// </summary>
            Target
        }

        private SimGeometryRelation relation;
        private SimBaseGeometryReference oldRelationReference;
        private SimBaseGeometryReference newRelationReference;
        private TargetProperty targetProperty;

        /// <summary>
        /// Creates a new <see cref="RetargetGeometryRelationUndoItem"/>
        /// </summary>
        /// <param name="relation">The relation to re-target</param>
        /// <param name="newRelationReference">The new target reference</param>
        /// <param name="targetProperty">Which property to re-target</param>
        public RetargetGeometryRelationUndoItem(SimGeometryRelation relation, SimBaseGeometryReference newRelationReference, TargetProperty targetProperty)
        {
            this.relation = relation;
            this.newRelationReference = newRelationReference;
            this.targetProperty = targetProperty;
            this.oldRelationReference = targetProperty == TargetProperty.Source ? relation.Source : relation.Target;
        }

        /// <inheritdoc/>
        public UndoExecutionResult Execute()
        {
            Redo();
            return UndoExecutionResult.Executed;
        }

        /// <inheritdoc/>
        public void Undo()
        {
            if (targetProperty == TargetProperty.Source)
            {
                relation.Source = oldRelationReference;
            }
            else
            {
                relation.Target = oldRelationReference;
            }
        }

        /// <inheritdoc/>
        public void Redo()
        {
            if (targetProperty == TargetProperty.Source)
            {
                relation.Source = newRelationReference;
            }
            else
            {
                relation.Target = newRelationReference;
            }
        }
    }
}
