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
        /// Updates the geometry data of a proxy geometry to a cube
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
        /// Generates a double pyramid
        /// </summary>
        /// <param name="layer">The layer</param>
        /// <param name="name">The name</param>
        /// <param name="baseVertex">The base vertex</param>
        /// <param name="size">The size</param>
        /// <param name="invert">If inverted, the two tips will touch in the middle</param>
        /// <returns>The proxy geometry</returns>
        public static ProxyGeometry GenerateDoublePyramid(Layer layer, string name, Vertex baseVertex, SimPoint3D size, bool invert = false)
        {
            ProxyGeometry proxy = new ProxyGeometry(layer, name, baseVertex);

            UpdateDoublePyramid(proxy, size, invert);

            return proxy;
        }
        /// <summary>
        /// Updates a double pyramid
        /// </summary>
        /// <param name="proxy">The proxy to update</param>
        /// <param name="size">The size</param>
        /// <param name="invert">If inverted, the two tips will touch in the middle</param>
        public static void UpdateDoublePyramid(ProxyGeometry proxy, SimPoint3D size, bool invert = false)
        {
            var s = ((SimVector3D)size) / 2.0;

            if (invert)
            {
                proxy.Positions = new List<SimPoint3D>()
                {
                    // bottom front
                    new SimPoint3D(),
                    new SimPoint3D(-s.X, -s.Y, -s.Z), // front left
                    new SimPoint3D(s.X, -s.Y, -s.Z), // front right
                    // bottom right
                    new SimPoint3D(),
                    new SimPoint3D(s.X, -s.Y, -s.Z), // front right
                    new SimPoint3D(s.X, -s.Y, s.Z), // back right
                    // bottom back
                    new SimPoint3D(),
                    new SimPoint3D(s.X, -s.Y, s.Z), // back right
                    new SimPoint3D(-s.X, -s.Y, s.Z), // back left
                    // bottom left
                    new SimPoint3D(),
                    new SimPoint3D(-s.X, -s.Y, s.Z), // back left
                    new SimPoint3D(-s.X, -s.Y, -s.Z), // front left
                    // bottom
                    new SimPoint3D(-s.X, -s.Y, -s.Z), // front left
                    new SimPoint3D(-s.X, -s.Y, s.Z), // back left
                    new SimPoint3D(s.X, -s.Y, s.Z), // back right
                    new SimPoint3D(s.X, -s.Y, s.Z), // back right
                    new SimPoint3D(s.X, -s.Y, -s.Z), // front right
                    new SimPoint3D(-s.X, -s.Y, -s.Z), // front left

                    // top front
                    new SimPoint3D(),
                    new SimPoint3D(-s.X, s.Y, -s.Z), // front left
                    new SimPoint3D(s.X, s.Y, -s.Z), // front right
                    // top right
                    new SimPoint3D(),
                    new SimPoint3D(s.X, s.Y, -s.Z), // front right
                    new SimPoint3D(s.X, s.Y, s.Z), // back right
                    // top back
                    new SimPoint3D(),
                    new SimPoint3D(s.X, s.Y, s.Z), // back right
                    new SimPoint3D(-s.X, s.Y, s.Z), // back left
                    // top left
                    new SimPoint3D(),
                    new SimPoint3D(-s.X, s.Y, s.Z), // back left
                    new SimPoint3D(-s.X, s.Y, -s.Z), // front left
                    // top
                    new SimPoint3D(-s.X, s.Y, -s.Z), // front left
                    new SimPoint3D(s.X, s.Y, -s.Z), // front right
                    new SimPoint3D(s.X, s.Y, s.Z), // back right
                    new SimPoint3D(s.X, s.Y, s.Z), // back right
                    new SimPoint3D(-s.X, s.Y, s.Z), // back left
                    new SimPoint3D(-s.X, s.Y, -s.Z), // front left
                };
            }
            else
            {
                proxy.Positions = new List<SimPoint3D>()
                {
                    // top front
                    new SimPoint3D(0, s.Y, 0),
                    new SimPoint3D(-s.X, 0, -s.Z), // front left
                    new SimPoint3D(s.X, 0, -s.Z), // front right
                    // top right
                    new SimPoint3D(0, s.Y, 0),
                    new SimPoint3D(s.X, 0, -s.Z), // front right
                    new SimPoint3D(s.X, 0, s.Z), // back right
                    // top back
                    new SimPoint3D(0, s.Y, 0),
                    new SimPoint3D(s.X, 0, s.Z), // back right
                    new SimPoint3D(-s.X, 0, s.Z), // back left
                    // top back
                    new SimPoint3D(0, s.Y, 0),
                    new SimPoint3D(-s.X, 0, s.Z), // back left
                    new SimPoint3D(-s.X, 0, -s.Z), // front left
                    // bottom front
                    new SimPoint3D(0, -s.Y, 0),
                    new SimPoint3D(s.X, 0, -s.Z), // front right
                    new SimPoint3D(-s.X, 0, -s.Z), // front left
                    // top right
                    new SimPoint3D(0, -s.Y, 0),
                    new SimPoint3D(s.X, 0, s.Z), // back right
                    new SimPoint3D(s.X, 0, -s.Z), // front right
                    // top back
                    new SimPoint3D(0, -s.Y, 0),
                    new SimPoint3D(-s.X, 0, s.Z), // back left
                    new SimPoint3D(s.X, 0, s.Z), // back right
                    // top back
                    new SimPoint3D(0, -s.Y, 0),
                    new SimPoint3D(-s.X, 0, -s.Z), // front left
                    new SimPoint3D(-s.X, 0, s.Z), // back left
                };
            }

            // positions are already in index order
            proxy.Indices = Enumerable.Range(0, proxy.Positions.Count).ToList();

            CalculateNormals(proxy);

            proxy.NotifyGeometryChanged();
        }

        /// <summary>
        /// Generates a pyramid
        /// </summary>
        /// <param name="layer">The layer</param>
        /// <param name="name">The name</param>
        /// <param name="baseVertex">The base vertex</param>
        /// <param name="size">The size</param>
        /// <returns>The proxy geometry</returns>
        public static ProxyGeometry GeneratePyramid(Layer layer, string name, Vertex baseVertex, SimPoint3D size)
        {
            ProxyGeometry proxy = new ProxyGeometry(layer, name, baseVertex);

            UpdateDoublePyramid(proxy, size);

            return proxy;
        }
        /// <summary>
        /// Updates a pyramid
        /// </summary>
        /// <param name="proxy">The proxy to update</param>
        /// <param name="size">The size</param>
        public static void UpdatePyramid(ProxyGeometry proxy, SimPoint3D size)
        {
            var s = ((SimVector3D)size) / 2.0;

            proxy.Positions = new List<SimPoint3D>()
            {
                // front
                new SimPoint3D(0, s.Y, 0),
                new SimPoint3D(-s.X, -s.Y, -s.Z), // front left
                new SimPoint3D(s.X, -s.Y, -s.Z), // front right
                // right
                new SimPoint3D(0, s.Y, 0),
                new SimPoint3D(s.X, -s.Y, -s.Z), // front right
                new SimPoint3D(s.X, -s.Y, s.Z), // back right
                // back
                new SimPoint3D(0, s.Y, 0),
                new SimPoint3D(s.X, -s.Y, s.Z), // back right
                new SimPoint3D(-s.X, -s.Y, s.Z), // back left
                // left
                new SimPoint3D(0, s.Y, 0),
                new SimPoint3D(-s.X, -s.Y, s.Z), // back left
                new SimPoint3D(-s.X, -s.Y, -s.Z), // front left
                // bottom
                new SimPoint3D(-s.X, -s.Y, -s.Z), // front left
                new SimPoint3D(-s.X, -s.Y, s.Z), // back left
                new SimPoint3D(s.X, -s.Y, s.Z), // back right
                new SimPoint3D(s.X, -s.Y, s.Z), // back right
                new SimPoint3D(s.X, -s.Y, -s.Z), // front right
                new SimPoint3D(-s.X, -s.Y, -s.Z), // front left
            };

            // positions are already in index order
            proxy.Indices = Enumerable.Range(0, proxy.Positions.Count).ToList();

            CalculateNormals(proxy);

            proxy.NotifyGeometryChanged();
        }

        private static void CalculateNormals(ProxyGeometry proxy)
        {
            SimVector3D[] normals = new SimVector3D[proxy.Positions.Count];
            for (int i = 0; i < proxy.Indices.Count; i += 3)
            {
                var d1 = proxy.Positions[proxy.Indices[i + 1]] - proxy.Positions[proxy.Indices[i]];
                var d2 = proxy.Positions[proxy.Indices[i + 2]] - proxy.Positions[proxy.Indices[i + 1]];
                var normal = SimVector3D.CrossProduct(d1, d2);
                normal.Normalize();
                // set for each vertex (only works on flat triangles)
                normals[proxy.Indices[i + 0]] = normal;
                normals[proxy.Indices[i + 1]] = normal;
                normals[proxy.Indices[i + 2]] = normal;
            }
            proxy.Normals = normals.ToList();
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
