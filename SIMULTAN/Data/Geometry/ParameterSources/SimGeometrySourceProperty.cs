using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.Geometry
{
    /// <summary>
    /// Geometric properties that can be used as source for a <see cref="SimGeometrySourceProperty"/>
    /// </summary>
    public enum SimGeometrySourceProperty
    {
        /// <summary>
        /// No property
        /// </summary>
        Invalid = 0,

        /// <summary>
        /// The area of a face. Aggregation: Sum
        /// </summary>
        FaceArea = 1,
        /// <summary>
        /// The geometric incline (angle to the XZ-plane) of a face. Aggregation: Average
        /// </summary>
        FaceIncline = 2,
        /// <summary>
        /// The geometric orientation (angle to north) of a face. Aggregation: Average
        /// </summary>
        FaceOrientation = 3,

        /// <summary>
        /// The elevation (absolute Y) of the floor polygon of a Volume. Aggregation: Average
        /// </summary>
        VolumeFloorElevation = 100,
        /// <summary>
        /// The elevation (absolute Y) of the ceiling polygon of a Volume. Aggregation: Average
        /// </summary>
        VolumeCeilingElevation = 101,
        /// <summary>
        /// The height of the volume. Aggregation: Average
        /// </summary>
        VolumeHeight = 102,
        /// <summary>
        /// The area of all floor polygons of a Volume. Aggregation: Sum
        /// </summary>
        VolumeFloorArea = 103,
        /// <summary>
        /// The volume of the Volume. Aggregation: Sum
        /// </summary>
        VolumeVolume = 104,

        /// <summary>
        /// The length of an Edge. Aggregation: Sum
        /// </summary>
        EdgeLength = 200,
    }
}
