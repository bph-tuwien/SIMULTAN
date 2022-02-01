using System.Collections.Generic;
using System.Windows.Media.Media3D;

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
        public List<Point3D> Vertices
        {
            get;
        }

        /// <summary>
        /// The normals of the imported model.
        /// </summary>
        public List<Vector3D> Normals
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
            Vertices = new List<Point3D>();
            Normals = new List<Vector3D>();
            Indices = new List<int>();
        }
    }
}
