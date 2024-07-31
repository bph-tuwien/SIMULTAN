using System.Collections.Generic;
using SIMULTAN.Data.SimMath;

namespace SIMULTAN.Data.Geometry
{

    /// <summary>
    /// Result of an GeometryImporter Import operation.
    /// </summary>
    public class SimMeshGeometryData
    {
        /// <summary>
        /// The vertices of the imported model.
        /// </summary>
        public List<SimPoint3D> Vertices
        {
            get;
        }

        /// <summary>
        /// The normals of the imported model.
        /// </summary>
        public List<SimVector3D> Normals
        {
            get;
        }

        /// <summary>
        /// The indices of the imported model.
        /// </summary>
        public List<int> Indices
        {
            get;
        }

        /// <summary>
        /// Creates a new GeometryImporterResult with empty vertex/normal/index lists.
        /// </summary>
        public SimMeshGeometryData()
        {
            Vertices = new List<SimPoint3D>();
            Normals = new List<SimVector3D>();
            Indices = new List<int>();
        }
    }
}
