using SIMULTAN.Data.FlowNetworks;
using SIMULTAN.Data.SimNetworks;
using System;

namespace SIMULTAN.Data.Components
{
    /// <summary>
    /// Describes the type of relationship possible for the component
    /// and each of it Geometric Relationships instances.
    /// </summary>
    [Flags]
    public enum SimInstanceType : uint
    {
        /// <summary>
        /// The component and its instances are not connected to any geometry. Default.
        /// </summary>
        None = 0u,
        /// <summary>
        /// The component describes an architectural space (or zone) represented by a 3d volume.
        /// This component contains the geometric hierarchy in its sub-components (e.g., the faces of the volume).
        /// </summary>
        Entity3D = (1u << 0),
        /// <summary>
        /// The component describes the volumetric properties of a 3d volume. This type 
        /// can only be assigned automatically, it cannot be set manually by the user.
        /// </summary>
        /// <summary>
        /// The component prescribes the offsets from the reference plane of all 2d faces its instances are associated with.
        /// It represents the concept of wall construction (Aufbau) in building physics.
        /// </summary>
        AttributesFace = (1u << 1),
        /// <summary>
        /// The component's instances represent the placement of HVAC+MEP components within an architectural space (Verortung).
        /// The instances carry reserved size parameters that govern the representing geometry's size.
        /// </summary>
        NetworkNode = (1u << 2),
        /// <summary>
        /// The component's instances represent the ducts and pipes connecting HVAC+MEP components of type CONTAINED_IN.
        /// The instances carry reserved parameters that govern the representing geometry's run and size.
        /// </summary>
        NetworkEdge = (1u << 3),
        /// <summary>
        /// The component and its default instance accumulate values from other types (including type NONE).
        /// Not yet implemented: grouping of components of type DESCRIBES into larger zones (e.g., thermal hull)
        /// </summary>
        Group = (1u << 4),
        /// <summary>
        /// The component holds (pointer) parameters that can be applied to the target object.
        /// </summary>
        BuiltStructure = (1u << 5),
        /// <summary>
        /// Component which represents an InPort
        /// </summary>
        InPort = (1u << 6),
        /// <summary>
        /// Component which represents an OutPort
        /// </summary>
        OutPort = (1u << 7),
        /// <summary>
        /// Attached to a geometric edge
        /// </summary>
        AttributesEdge = (1u << 8),
        /// <summary>
        /// Attached to a geometric vertex
        /// </summary>
        AttributesPoint = (1u << 9),
        /// <summary>
        /// Attached to a SimNetworkBlock
        /// </summary>
        SimNetworkBlock = (1u << 10),

        /// <summary>
        /// The component describes a 3d volume. This type 
        /// can only be assigned automatically, it cannot be set manually by the user.
        /// </summary>
        [Obsolete]
        GeometricVolume = (1u << 30),
        /// <summary>
        /// The component describes the properties of a 2d face, 1d edge or 0d vertex. This type 
        /// can only be assigned automatically, it cannot be set manually by the user.
        /// </summary>
        [Obsolete]
        GeometricSurface = (1u << 31),


        /// <summary>
        /// Contains all types that describe a connection to a geometry. Having a valid instance or placement of this type 
        /// causes the component to be considered realized
        /// </summary>
        ActiveTypes = Entity3D | AttributesFace | AttributesEdge | AttributesPoint | BuiltStructure,
        /// <summary>
        /// Contains all types that describe the connection to a network (either <see cref="SimNetwork"/> or <see cref="SimFlowNetwork"/>
        /// </summary>
        NetworkTypes = NetworkNode | NetworkEdge | InPort | OutPort | SimNetworkBlock,
    }
}
