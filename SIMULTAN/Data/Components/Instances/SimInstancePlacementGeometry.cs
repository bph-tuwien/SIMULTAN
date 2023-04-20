using SIMULTAN.Data.Components;
using SIMULTAN.Data.FlowNetworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.Components
{
    /// <summary>
    /// Describes the usage of an instance by a geometry. Geometry is specified by a <see cref="FileId"/> (the resource key) and a 
    /// <see cref="GeometryId"/> inside the file.
    /// </summary>
	public class SimInstancePlacementGeometry : SimInstancePlacement
    {
        #region Properties

        /// <summary>
        /// The Id of the underlying geometry model file
        /// </summary>
        public int FileId { get; set; }
        /// <summary>
        /// Id of the geometry in the geometry model file
        /// </summary>
        public ulong GeometryId { get; set; }

        /// <summary>
        /// Returns True if the placements points to a valid geometry id.
        /// Note: This does not mean that the geometry exists or that the geometry itself is valid. 
        /// It only checks if the id might point to a valid target.
        /// </summary>
        public bool IsValid { get { return GeometryId != ulong.MaxValue && FileId >= 0; } }

        #endregion

        /// <summary>
        /// Initializes a new instance of the SimInstancePlacementGeometry class
        /// </summary>
        /// <param name="fileId">The key of the resource file</param>
        /// <param name="geometryId">The id of the geometry</param>
        /// <param name="state">State of the placement</param>
        /// <param name="relatedIds">A list of related Ids</param>
        public SimInstancePlacementGeometry(int fileId, ulong geometryId, SimInstancePlacementState state = SimInstancePlacementState.Valid, IEnumerable<ulong> relatedIds = null)
        {
            this.FileId = fileId;
            this.GeometryId = geometryId;
            this.State = state;
        }

        /// <inheritdoc />
        public override void AddToTarget()
        {

        }
        /// <inheritdoc />
        public override void RemoveFromTarget()
        {
            this.Instance.Component.RemoveAsset(this.FileId, this.GeometryId.ToString());
        }

        internal override bool RestoreReferences(Dictionary<SimObjectId, SimFlowNetworkElement> networkElements)
        {
            //No init to do
            return true;
        }
    }
}
