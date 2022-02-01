using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

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
        public Face Face { get; private set; }

        /// <summary>
        /// The office that was used for this offset face
        /// </summary>
        public double Offset { get; set; }

        /// <summary>
        /// Stores initial opening loops. The key is the <see cref="EdgeLoop"/> which represents the opening in the reference face.
        /// </summary>
        public Dictionary<EdgeLoop, List<Point3D>> Openings { get; private set; }

        /// <summary>
        /// Stores the final boundary polygon
        /// </summary>
        public List<Point3D> Boundary { get; private set; }

        /// <summary>
        /// Stores additional edges for this face (e.g., side-closing edges, edges of openings, ...)
        /// </summary>
        public List<Point3D> AdditionalEdges { get; private set; }

        /// <summary>
        /// Stores a list of additional quads.
        /// </summary>
        public List<List<Point3D>> AdditionalQuads { get; private set; }

        /// <summary>
        /// Initializes a new instance of the OffsetFace class
        /// </summary>
        /// <param name="face">The face which is associated with this offset face</param>
        public OffsetFace(Face face)
        {
            this.Face = face;
            this.Openings = new Dictionary<EdgeLoop, List<Point3D>>();
            this.Boundary = new List<Point3D>();

            this.AdditionalQuads = new List<List<Point3D>>();
            this.AdditionalEdges = new List<Point3D>();
        }
    }
}
