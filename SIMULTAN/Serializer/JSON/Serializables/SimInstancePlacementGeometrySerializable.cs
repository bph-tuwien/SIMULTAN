using SIMULTAN.Data.Components;
using System;

namespace SIMULTAN.Serializer.JSON
{

    /// <summary>
    /// Serializable class for the <see cref="SimInstancePlacementGeometry"/>
    /// </summary>
    public class SimInstancePlacementGeometrySerializable : SimInstancePlacementSerializable
    {
        /// <summary>
        /// ID of the file
        /// </summary>
        public int FileId { get; set; }
        /// <summary>
        /// ID of the geometry
        /// </summary>
        public ulong GeometryId { get; set; }        


        /// <summary>
        /// Creates a new instance of SimInstancePlacementNetworkSerializable
        /// </summary>
        /// <param name="placement">The created instance</param>
        public SimInstancePlacementGeometrySerializable(SimInstancePlacementGeometry placement)
        {
            this.InstanceType = placement.Instance.ToString();
            this.FileId = placement.FileId;
            this.GeometryId = placement.GeometryId;
        }

        //DO NOT USE. Only required for the XMLSerializer class to operate on this type
        private SimInstancePlacementGeometrySerializable() { throw new NotImplementedException(); }
    }

}
