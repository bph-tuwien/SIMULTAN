using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.Components
{
    /// <summary>
    /// Describes the type of relationship possible for the component
    /// and each of it Geometric Relationships instances.
    /// </summary>
    public enum SimInstanceType
    {
        /// <summary>
        /// The component and its instances are not connected to any geometry. Default.
        /// </summary>
        None = 0,
        /// <summary>
        /// The component describes an architectural space (or zone) represented by a 3d volume.
        /// This component contains the geometric hierarchy in its sub-components (e.g., the faces of the volume).
        /// </summary>
        Entity3D = 1,
        /// <summary>
        /// The component describes the volumetric properties of a 3d volume. This type 
        /// can only be assigned automatically, it cannot be set manually by the user.
        /// </summary>
        GeometricVolume = 2,
        /// <summary>
        /// The component describes the properties of a 2d face, 1d edge or 0d vertex. This type 
        /// can only be assigned automatically, it cannot be set manually by the user.
        /// </summary>
        GeometricSurface = 3,
        /// <summary>
        /// The component prescribes the offsets from the reference plane of all 2d faces its instances are associated with.
        /// It represents the concept of wall construction (Aufbau) in building physics.
        /// </summary>
        Attributes2D = 4,
        /// <summary>
        /// The component's instances represent the placement of HVAC+MEP components within an architectural space (Verortung).
        /// The instances carry reserved size parameters that govern the representing geometry's size.
        /// </summary>
        NetworkNode = 5,
        /// <summary>
        /// The component's instances represent the ducts and pipes connecting HVAC+MEP components of type CONTAINED_IN.
        /// The instances carry reserved parameters that govern the representing geometry's run and size.
        /// </summary>
        NetworkEdge = 6,
        /// <summary>
        /// The component and its default instance accumulate values from other types (including type NONE).
        /// Not yet implemented: grouping of components of type DESCRIBES into larger zones (e.g., thermal hull)
        /// </summary>
        Group = 7,
        /// <summary>
        /// The component holds (pointer) parameters that can be applied to the target object.
        /// </summary>
        BuiltStructure = 8
    }
}
