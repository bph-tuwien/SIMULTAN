using SIMULTAN;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SIMULTAN.Data.Geometry
{
    /// <summary>
    /// Provides methods that operate on BaseGeometries (on all types of Geometry)
    /// </summary>
    public static class BaseGeometryAlgorithms
    {
        /// <summary>
        /// Returns a list of all vertices in a Geometry. Only supports Vertex, Edge, Face, Volume
        /// </summary>
        /// <param name="geometry">The geometry for which the vertices should be returned</param>
        /// <returns>A list of all vertices in hte geometry</returns>
        public static List<Vertex> GetVertices(BaseGeometry geometry)
        {
            if (geometry is Vertex)
                return new List<Vertex> { geometry as Vertex };
            else if (geometry is Edge)
                return ((Edge)geometry).Vertices.ToList();
            else if (geometry is Face)
                return ((Face)geometry).Boundary.Edges.Select(x => x.StartVertex).ToList();
            else if (geometry is Volume)
                return VerticesFromVolume(geometry as Volume);

            return new List<Vertex>();
        }

        private static List<Vertex> VerticesFromVolume(Volume volume)
        {
            List<Vertex> result = new List<Vertex>();

            foreach (var f in volume.Faces)
            {
                result.AddRange(f.Face.Boundary.Edges.Select(x => x.StartVertex));
            }

            return result.Distinct().ToList();
        }
    }
}
