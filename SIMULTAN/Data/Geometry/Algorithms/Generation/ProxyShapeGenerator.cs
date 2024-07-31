using SIMULTAN.Data.SimMath;
using SIMULTAN.Projects;
using SIMULTAN.Serializer.Geometry;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SIMULTAN.Data.Geometry
{
    /// <summary>
    /// Algorithms for creating predefined proxy shapes
    /// </summary>
    public static class ProxyShapeGenerator
    {
        /// <summary>
        /// Updates the geoemtry data of a proxy geometry to a cube
        /// </summary>
        /// <param name="proxy">The proxy geometry that should be updated.</param>
        /// <param name="size">Size of the cube geometry (NOT: size of the proxy. This size is used to calculate vertex coordinates)</param>
        /// <returns>A proxygeometry containing a cube</returns>
        public static void UpdateCube(ProxyGeometry proxy, SimPoint3D size)
        {
            var s2 = ((SimVector3D)size) / 2.0;

            proxy.Positions = new List<SimPoint3D>()
            {
				//Front
				new SimPoint3D(-s2.X, -s2.Y, s2.Z),
                new SimPoint3D( s2.X, -s2.Y, s2.Z),
                new SimPoint3D( s2.X,  s2.Y, s2.Z),
                new SimPoint3D(-s2.X,  s2.Y, s2.Z),

				//Back
				new SimPoint3D(-s2.X, -s2.Y, -s2.Z),
                new SimPoint3D(-s2.X,  s2.Y, -s2.Z),
                new SimPoint3D( s2.X,  s2.Y, -s2.Z),
                new SimPoint3D( s2.X, -s2.Y, -s2.Z),

				//Left
				new SimPoint3D(-s2.X, -s2.Y, -s2.Z),
                new SimPoint3D(-s2.X, -s2.Y,  s2.Z),
                new SimPoint3D(-s2.X,  s2.Y,  s2.Z),
                new SimPoint3D(-s2.X,  s2.Y, -s2.Z),

				//Right
				new SimPoint3D(s2.X, -s2.Y, -s2.Z),
                new SimPoint3D(s2.X,  s2.Y, -s2.Z),
                new SimPoint3D(s2.X,  s2.Y,  s2.Z),
                new SimPoint3D(s2.X, -s2.Y,  s2.Z),

				//Bottom
				new SimPoint3D(-s2.X, -s2.Y, -s2.Z),
                new SimPoint3D(-s2.X, -s2.Y,  s2.Z),
                new SimPoint3D( s2.X, -s2.Y,  s2.Z),
                new SimPoint3D( s2.X, -s2.Y, -s2.Z),

				//Top
				new SimPoint3D(-s2.X, s2.Y, -s2.Z),
                new SimPoint3D( s2.X, s2.Y, -s2.Z),
                new SimPoint3D( s2.X, s2.Y,  s2.Z),
                new SimPoint3D(-s2.X, s2.Y,  s2.Z),
            };

            proxy.Normals = new List<SimVector3D>()
            {
                new SimVector3D(0, 0, 1),
                new SimVector3D(0, 0, 1),
                new SimVector3D(0, 0, 1),
                new SimVector3D(0, 0, 1),

                new SimVector3D(0, 0, -1),
                new SimVector3D(0, 0, -1),
                new SimVector3D(0, 0, -1),
                new SimVector3D(0, 0, -1),

                new SimVector3D(-1, 0, 0),
                new SimVector3D(-1, 0, 0),
                new SimVector3D(-1, 0, 0),
                new SimVector3D(-1, 0, 0),

                new SimVector3D(1, 0, 0),
                new SimVector3D(1, 0, 0),
                new SimVector3D(1, 0, 0),
                new SimVector3D(1, 0, 0),

                new SimVector3D(0, 1, 0),
                new SimVector3D(0, 1, 0),
                new SimVector3D(0, 1, 0),
                new SimVector3D(0, 1, 0),

                new SimVector3D(0, -1, 0),
                new SimVector3D(0, -1, 0),
                new SimVector3D(0, -1, 0),
                new SimVector3D(0, -1, 0),
            };

            proxy.Indices = new List<int>()
            {
                0, 1, 2,
                0, 2, 3,

                4, 5, 6,
                4, 6, 7,

                8, 9, 10,
                8, 10, 11,

                12, 13, 14,
                12, 14, 15,

                16, 17, 18,
                16, 18, 19,

                20, 21, 22,
                20, 22, 23,
            };

            proxy.NotifyGeometryChanged();
        }

        /// <summary>
        /// Generates a proxy geometry cube
        /// </summary>
        /// <param name="layer">Layer on which the cube is placed</param>
        /// <param name="name">The name of the proxy geometry</param>
        /// <param name="baseVertex">Vertex to which the cube should be attached</param>
        /// <param name="size">Size of the cube geometry (NOT: size of the proxy. This size is used to calculate vertex coordinates)</param>
        /// <returns>A proxy geometry containing a cube</returns>
        public static ProxyGeometry GenerateCube(Layer layer, string name, Vertex baseVertex, SimPoint3D size)
        {
            ProxyGeometry proxy = new ProxyGeometry(layer, name, baseVertex);

            UpdateCube(proxy, size);

            return proxy;
        }

        /// <summary>
        /// Loads a list of meshes and combines them into a single proxy geometry.
        /// </summary>
		/// <param name="layer">Layer on which the model is placed</param>
        /// <param name="name">The name of the resulting proxy geometry</param>
		/// <param name="baseVertex">Vertex to which the model should be attached</param>
        /// <param name="paths">A List of file paths to the meshes that should be combined.</param>
        /// <param name="projectData">A ProjectData used to cache the imported results. Before the geometry is imported from file the cache is checked if it already contains the data.</param>
		/// <returns>A proxy geometry containing the imported models.</returns>
        public static ProxyGeometry LoadModelsCombined(Layer layer, string name, Vertex baseVertex, IEnumerable<FileInfo> paths, ProjectData projectData)
        {
            ProxyGeometry proxy = new ProxyGeometry(layer, name, baseVertex);

            UpdateProxyGeometryCombined(proxy, paths, projectData);

            return proxy;
        }

        /// <summary>
        /// Updates the geometry data of a proxy with the combined meshes of the provided mesh list.
        /// </summary>
        /// <param name="proxy">The proxy geometry that should be updated.</param>
        /// <param name="paths">A List of file paths to the meshes that should be combined.</param>
        /// <param name="projectData">ProjectData used as cache for the geometry data. Before the geometry is imported from file the cache is checked if it already contains the data.</param>
        public static void UpdateProxyGeometryCombined(ProxyGeometry proxy, IEnumerable<FileInfo> paths, ProjectData projectData)
        {
            if (proxy.ModelGeometry != null)
                proxy.ModelGeometry.StartBatchOperation();

            if (proxy.Positions == null)
                proxy.Positions = new List<SimPoint3D>();
            else
                proxy.Positions.Clear();
            if (proxy.Normals == null)
                proxy.Normals = new List<SimVector3D>();
            else
                proxy.Normals.Clear();
            if (proxy.Indices == null)
                proxy.Indices = new List<int>();
            else
                proxy.Indices.Clear();

            foreach (var path in paths)
            {
                SimMeshGeometryData result = projectData.GeometryModels.TryGetCachedImportedGeometry(path);
                if (result == null)
                {
                    result = AssimpGeometryImporter.Instance.Import(path.FullName);
                    projectData.GeometryModels.CacheImportedGeometry(path, result);
                }

                int lastIndex = proxy.Positions.Count;
                proxy.Positions.AddRange(result.Vertices);
                proxy.Normals.AddRange(result.Normals);
                proxy.Indices.AddRange(result.Indices.Select(x => x + lastIndex));
            }

            proxy.NotifyGeometryChanged();

            if (proxy.ModelGeometry != null)
                proxy.ModelGeometry.EndBatchOperation();
        }
    }
}
