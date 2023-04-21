using Assimp;
using SIMULTAN.Data.Geometry;
using SIMULTAN.Excel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Serializer.Geometry
{
    /// <summary>
    /// An Importer for generic meshes using the Assimp Library.
    /// </summary>
    public class AssimpGeometryImporter : IGeometryImporter
    {
        /// <summary>
        /// Provides an Instance to this class.
        /// </summary>
        public static AssimpGeometryImporter Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new AssimpGeometryImporter();
                }
                return instance;
            }
        }
        private static AssimpGeometryImporter instance;

        private AssimpGeometryImporter() { }

        /// <inheritdoc/>
        public SimMeshGeometryData Import(string path)
        {
            var result = new SimMeshGeometryData();

            AssimpContext aContext = new AssimpContext();
            try
            {
                using (FileStream fs = new FileStream(path, FileMode.Open))
                {
                    var extension = Path.GetExtension(path);
                    if (extension.Length > 0)
                        extension = extension.Substring(1); //Remove .

                    var aScene = aContext.ImportFileFromStream(fs,
                        PostProcessSteps.Triangulate | PostProcessSteps.PreTransformVertices | PostProcessSteps.GenerateNormals,
                        extension);

                    foreach (var aMesh in aScene.Meshes)
                    {
                        var indexOffset = result.Vertices.Count;
                        result.Vertices.AddRange(aMesh.Vertices.Select(x => new System.Windows.Media.Media3D.Point3D(x.X, x.Y, x.Z)));
                        if (aMesh.HasNormals)
                        {
                            result.Normals.AddRange(aMesh.Normals.Select(x => new System.Windows.Media.Media3D.Vector3D(x.X, x.Y, x.Z)));
                        }
                        else
                        {
                            for (var i = 0; i < aMesh.Vertices.Count; i++)
                            {
                                result.Normals.Add(new System.Windows.Media.Media3D.Vector3D(0, 0, 0));
                            }
                        }
                        result.Indices.AddRange(new List<int>(aMesh.GetIndices()).Select(x => x + indexOffset));
                    }
                }
            }
            catch (AssimpException e)
            {
                throw new GeometryImporterException(path, e);
            }
            catch (FileNotFoundException e)
            {
                throw e;
            }

            return result;
        }
    }
}
