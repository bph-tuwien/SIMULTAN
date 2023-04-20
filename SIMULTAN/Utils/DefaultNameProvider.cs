using SIMULTAN.Data.Geometry;
using System;
using System.Collections.Generic;

namespace SIMULTAN.Utils
{
    internal delegate string GeometryNameFormatProvider(Type geometryType);

    internal static class DefaultNameProvider
    {
        private static Dictionary<Type, string> defaultGeometryNames = new Dictionary<Type, string>()
        {
            { typeof(Vertex), "Vertex {0}" },
            { typeof(Edge), "Edge {0}" },
            { typeof(EdgeLoop), "EdgeLoop {0}" },
            { typeof(Polyline), "Polyline {0}" },
            { typeof(Face), "Face {0}" },
            { typeof(Volume), "Volume {0}" },
            { typeof(ProxyGeometry), "ProxyGeometry {0}" },
        };

        internal static string DefaultGeometryName(Type geometryType)
        {
            if (defaultGeometryNames.TryGetValue(geometryType, out var result))
                return result;
            return string.Empty;
        }

        internal static string GeometryNameProviderOrDefault(Type geometryType, GeometryNameFormatProvider provider)
        {
            return provider != null ? provider(geometryType) : DefaultGeometryName(geometryType);
        }
    }
}
