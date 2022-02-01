using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace SIMULTAN.Exchange.ConnectorInteraction
{
    /// <summary>
    /// Provides methods for geometric transformations
    /// </summary>
    internal static class GeometricTransformations
    {
        public static Matrix3D PackUCS(Point3D _origin_WC, Vector3D _vecX_WC, Vector3D _vecY_WC, Vector3D _vecZ_WC)
        {
            return new Matrix3D(_vecX_WC.X, _vecX_WC.Y, _vecX_WC.Z, 0,
                                _vecY_WC.X, _vecY_WC.Y, _vecY_WC.Z, 0,
                                _vecZ_WC.X, _vecZ_WC.Y, _vecZ_WC.Z, 0,
                                _origin_WC.X, _origin_WC.Y, _origin_WC.Z, 1);
        }
    }
}
