using SIMULTAN.Serializer.Geometry;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.Geometry
{
    /// <summary>
    /// A class used to cache the results of geometry import operations.
    /// </summary>
    public class GeometryImporterCache
    {
        private Dictionary<string, CacheItem> cache;

        /// <summary>
        /// Creates a new GeometryImporterCache instance.
        /// </summary>
        public GeometryImporterCache()
        {
            cache = new Dictionary<string, CacheItem>();
        }

        /// <summary>
        /// Tries to retrieve the cached value import result of the provided file.
        /// Return null if there is no cached version or if the file changed.
        /// </summary>
        /// <param name="file">The file to retrieve the results for.</param>
        /// <returns>The cached import result or null if there is no cache entry or if the entry is dirty and needs reimporting.</returns>
        public SimMeshGeometryData TryGetCachedImportedGeometry(FileInfo file)
        {
            if (file == null)
            {
                throw new ArgumentNullException();
            }

            CacheItem result;
            if (cache.TryGetValue(file.FullName, out result))
            {
                if (result.LastChanged != file.LastWriteTime)
                {
                    //Console.WriteLine("cache dirty "+file.LastWriteTime+" "+file.FullName);
                    return null;
                }
                {
                    //Console.WriteLine("cache hit " + file.LastWriteTime + " " + file.FullName);
                    return result.Geometry;
                }
            }
            //Console.WriteLine("cache miss "+file.LastWriteTime+" "+file.FullName);
            return null;
        }

        /// <summary>
        /// Adds the import result of a specified file to the cache.
        /// If the entry is already present it is overridden.
        /// </summary>
        /// <param name="file">The file to create the entry for.</param>
        /// <param name="geometry">The import result of the file.</param>
        public void CacheImportedGeometry(FileInfo file, SimMeshGeometryData geometry)
        {
            if (file == null || geometry == null)
            {
                throw new ArgumentNullException();
            }

            if (cache.ContainsKey(file.FullName))
            {
                cache[file.FullName] = new CacheItem(file.LastWriteTime, geometry);
            }
            else
            {
                cache.Add(file.FullName, new CacheItem(file.LastWriteTime, geometry));
            }
        }

        /// <summary>
        /// Cache entry data model.
        /// </summary>
        private class CacheItem
        {
            public DateTime LastChanged;
            public SimMeshGeometryData Geometry;

            public CacheItem(DateTime lastModifiedDate, SimMeshGeometryData geometry)
            {
                this.LastChanged = lastModifiedDate;
                this.Geometry = geometry;
            }
        }

    }
}
