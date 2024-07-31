using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SIMULTAN.Data.SimMath;

namespace SIMULTAN.Data.Geometry
{
    /// <summary>
    /// Stores data for one face in an offset surface mesh
    /// </summary>
    public class OffsetFace
    {
        /// <summary>
        /// The associated face
        /// </summary>
        public Face Face { get; }

        /// <summary>
        /// Stores which side of the face is represented by this offset surface
        /// </summary>
        public GeometricOrientation Orientation { get; }

        /// <summary>
        /// The office that was used for this offset face
        /// </summary>
        public double Offset { get; set; }

        /// <summary>
        /// Stores initial opening loops. The key is the <see cref="EdgeLoop"/> which represents the opening in the reference face.
        /// </summary>
        public Dictionary<EdgeLoop, List<SimPoint3D>> Openings { get; }

        /// <summary>
        /// Stores the final boundary polygon
        /// </summary>
        public List<SimPoint3D> Boundary { get; }

        /// <summary>
        /// Initializes a new instance of the OffsetFace class
        /// </summary>
        /// <param name="face">The face which is associated with this offset face</param>
        /// <param name="orientation">Defines which side of the face is represented by this offset surface</param>
        public OffsetFace(Face face, GeometricOrientation orientation)
        {
            this.Face = face;
            this.Orientation = orientation;
            this.Openings = new Dictionary<EdgeLoop, List<SimPoint3D>>();
            this.Boundary = new List<SimPoint3D>();
        }
    }
}
