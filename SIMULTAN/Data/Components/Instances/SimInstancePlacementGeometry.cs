using SIMULTAN.Data.FlowNetworks;
using System;
using System.Collections.Generic;
using System.Linq;

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
        /// <param name = "instanceType" > The instance type of this placement. Must be one of
        /// <see cref="SimInstanceType.AttributesPoint"/>, <see cref="SimInstanceType.AttributesEdge"/>, 
        /// <see cref="SimInstanceType.AttributesFace"/>, <see cref="SimInstanceType.Entity3D"/>,
        /// <see cref="SimInstanceType.BuiltStructure"/>.
        /// Also supports <see cref="SimInstanceType.None"/>, but then the InstanceType has to be set by the
        /// <see cref="RestoreReferences(Dictionary{SimObjectId, SimFlowNetworkElement})"/> method.
        /// </param>
        public SimInstancePlacementGeometry(int fileId, ulong geometryId, SimInstanceType instanceType,
            SimInstancePlacementState state = SimInstancePlacementState.Valid)
            : base(instanceType)
        {
            if (instanceType != SimInstanceType.AttributesPoint && instanceType != SimInstanceType.AttributesEdge &&
                instanceType != SimInstanceType.AttributesFace && instanceType != SimInstanceType.Entity3D &&
                instanceType != SimInstanceType.BuiltStructure && instanceType != SimInstanceType.None)
                throw new ArgumentException("Instance type not supported for this placement type");

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
            if (this.InstanceType == SimInstanceType.None)
            {
                //Check if the instance has a valid type. Valid means there is a single geometry type
                var geometryFlags = SimInstanceType.AttributesPoint | SimInstanceType.AttributesEdge | SimInstanceType.AttributesFace
                    | SimInstanceType.Entity3D | SimInstanceType.BuiltStructure;
                var geometryFlagsFromInstance = this.Instance.Component.InstanceType & geometryFlags;

                if (((geometryFlagsFromInstance - 1) & geometryFlagsFromInstance) == 0)
                    this.InstanceType = geometryFlagsFromInstance;
                else
                    throw new Exception("Instance type for SimPlacementGeometry could not be restored");
            }

            //No further initialization to do
            return true;
        }
    }
}
