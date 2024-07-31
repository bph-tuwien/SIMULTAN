using System;
using System.Text.Json.Serialization;

namespace SIMULTAN.Serializer.JSON
{
    /// <summary>
    /// Interface for all the Serializable classes to have something in common
    /// </summary>
    [JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
    [JsonDerivedType(typeof(SimInstancePlacementGeometrySerializable), typeDiscriminator: "SimInstancePlacementGeometry")]
    [JsonDerivedType(typeof(SimInstancePlacementNetworkSerializable), typeDiscriminator: "SimInstancePlacementNetwork")]
    [JsonDerivedType(typeof(SimInstancePlacementSimNetworkSerializable), typeDiscriminator: "SimInstancePlacementSimNetwork")]
    public abstract class SimInstancePlacementSerializable
    {
        /// <summary>
        /// Type of the instance
        /// </summary>
        public string InstanceType { get; set; }
    }
}
