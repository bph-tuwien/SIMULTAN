using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data.Geometry;
using System;
using System.Windows.Media.Media3D;

namespace SIMULTAN.Tests.Geometry.Algorithms
{
    [TestClass]
    public class GeoReferenceAlgorithmsTests
    {
        private bool PointEqual(Point3D a, Point3D b, double epsilon = 0.00001)
        {
            bool equal = a.X - b.X < epsilon;
            equal = equal && a.Y - b.Y < epsilon;
            equal = equal && a.Z - b.Z < epsilon;
            return equal;
        }

        [TestMethod]
        public void CalculateBackAndForthTest()
        {
            double stepsize = 1;
            for (double lon = -180; lon <= 180; lon += stepsize)
            {
                for (double lat = -90; lat <= 90; lat += stepsize)
                {
                    var startPoint = new Point3D(lon, lat, 0.0);
                    var cartPoint = GeoReferenceAlgorithms.WGS84ToCart(startPoint);
                    var testPoint = GeoReferenceAlgorithms.CartToWGS84(cartPoint);
                    bool equal = PointEqual(startPoint, testPoint);
                    Assert.IsTrue(equal);
                }
            }
        }
    }
}
