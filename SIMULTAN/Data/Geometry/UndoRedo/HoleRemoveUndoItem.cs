using SIMULTAN.Utils.UndoRedo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.Geometry
{
    /// <summary>
    /// Undo Item for removing a hole from a face
    /// </summary>
    public class HoleRemoveUndoItem : IUndoItem
    {
        private Face face;
        private EdgeLoop hole;

        /// <summary>
        /// Initializes a new instance of the HoleRemoveUndoItem class. The hole has to be removed before creating the undo model
        /// </summary>
        /// <param name="face">The face from which the hole was removed</param>
        /// <param name="hole">The removed hole</param>
        public HoleRemoveUndoItem(Face face, EdgeLoop hole)
        {
            this.face = face;
            this.hole = hole;
        }

        /// <inheritdoc/>
        public UndoExecutionResult Execute()
        {
            return UndoExecutionResult.Executed;
        }

        /// <inheritdoc/>
        public void Redo()
        {
            this.face.Holes.Remove(hole);
        }
        /// <inheritdoc/>
        public void Undo()
        {
            this.face.Holes.Add(hole);
        }
    }
}
