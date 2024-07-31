using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data.Geometry;
using SIMULTAN.Data.SimMath;
using SIMULTAN.Exceptions;
using System;
using System.Collections.Generic;

namespace SIMULTAN.Tests.Geometry.Algorithms
{
    [TestClass]
    public class GeoReferenceAlgorithmsTests
    {
        private bool PointEqual(SimPoint3D a, SimPoint3D b, double epsilon = 0.00001)
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
                    var startPoint = new SimPoint3D(lon, lat, 0.0);
                    var cartPoint = GeoReferenceAlgorithms.WGS84ToCart(startPoint);
                    var testPoint = GeoReferenceAlgorithms.CartToWGS84(cartPoint);
                    bool equal = PointEqual(startPoint, testPoint);
                    Assert.IsTrue(equal);
                }
            }
        }


        [TestMethod]
        public void TestGeoReferenceMeshForValidInput()
        {
            var _point1 = new SimPoint3D(25.4418334960938, 0, 32.8202209472656);
            var _point2 = new SimPoint3D(15.7350358963013, 0, 51.0720024108887);
            var _point3 = new SimPoint3D(9.70774459838867, 0, 18.2517261505127);
            var _point4 = new SimPoint3D(0, 0, 0);
            var _point5 = new SimPoint3D(0, 0, 0);
            var _point6 = new SimPoint3D(25.4418334960938, 0, 32.8202209472656);
            var _point7 = new SimPoint3D(25.4418334960938, 5, 32.8202209472656);

            var pointList = new List<SimPoint3D>() { _point1, _point2, _point3, _point4, _point5, _point6, _point7 };


            var OS1 = new SimPoint3D(0, 0, 0);
            var point1 = new SimPoint3D(0, 0, 0);
            var OS2 = new SimPoint3D(25.4418328497559, 0, 32.8202218152583);
            var point2 = new SimPoint3D(16.7878353623782, 48.0350439443506, 0);
            var OS3 = new SimPoint3D(15.7350362855941, 0, 51.0720016753767);
            var point3 = new SimPoint3D(16.7879923330669, 48.0349567467135, 0);


            var geoRef1 = new GeoRefPoint(OS1, point1);
            var geoRef2 = new GeoRefPoint(OS2, point2);
            var geoRef3 = new GeoRefPoint(OS3, point3);


            var list = new List<GeoRefPoint>() { geoRef1, geoRef2, geoRef3 };


            GeoReferenceAlgorithms.GeoReferenceMesh(pointList, list);
        }



        [TestMethod]
        public void TestGeoReferenceMeshForValidInputButWronglyAligned()
        {
            var _point1 = new SimPoint3D(25.4418334960938, 0, 32.8202209472656);
            var _point2 = new SimPoint3D(15.7350358963013, 0, 51.0720024108887);
            var _point3 = new SimPoint3D(9.70774459838867, 0, 18.2517261505127);
            var _point4 = new SimPoint3D(0, 0, 0);
            var _point5 = new SimPoint3D(0, 0, 0);
            var _point6 = new SimPoint3D(25.4418334960938, 0, 32.8202209472656);
            var _point7 = new SimPoint3D(25.4418334960938, 5, 32.8202209472656);

            var pointList = new List<SimPoint3D>() { _point1, _point2, _point3, _point4, _point5, _point6, _point7 };


            var OS1 = new SimPoint3D(25.4418328497559, 0, 0);
            var point1 = new SimPoint3D(0, 0, 0);
            var OS2 = new SimPoint3D(25.4418328497559, 0, 0);
            var point2 = new SimPoint3D(16.7878353623782, 48.0349567467135, 0);
            var OS3 = new SimPoint3D(15.7350362855941, 0, 0);
            var point3 = new SimPoint3D(16.7879923330669, 48.0349567467135, 0);


            var geoRef1 = new GeoRefPoint(OS1, point1);
            var geoRef2 = new GeoRefPoint(OS2, point2);
            var geoRef3 = new GeoRefPoint(OS3, point3);


            var list = new List<GeoRefPoint>() { geoRef1, geoRef2, geoRef3 };


            //Wrong propagation in output
            Assert.ThrowsException<InvalidGeoReferencingException>(() =>
            {
                GeoReferenceAlgorithms.GeoReferenceMesh(pointList, list);
            });

        }

        [TestMethod]
        public void TestGeoReferenceMeshForInvalidInput()
        {

            var _point1 = new SimPoint3D(2, 55, 11);
            var _point2 = new SimPoint3D(2, 55, 11);
            var _point3 = new SimPoint3D(2, 55, 11);
            var _point4 = new SimPoint3D(2, 55, 11);
            var _point5 = new SimPoint3D(2, 55, 11);

            var pointList = new List<SimPoint3D>() { _point1, _point2, _point3, _point4, _point5 };

            var OS1 = new SimPoint3D(0, 0, 1);
            var point1 = new SimPoint3D(0, 0, 0);
            var OS2 = new SimPoint3D(0, 1, 0);
            var point2 = new SimPoint3D(0, 0, 0);
            var OS3 = new SimPoint3D(1, 0, 0);
            var point3 = new SimPoint3D(0, 0, 0);


            var geoRef1 = new GeoRefPoint(OS1, point1);
            var geoRef2 = new GeoRefPoint(OS2, point2);
            var geoRef3 = new GeoRefPoint(OS3, point3);

            var list = new List<GeoRefPoint>() { geoRef1, geoRef2, geoRef3 };



            //Wrong propagation in output
            Assert.ThrowsException<InvalidGeoReferencingException>(() =>
            {
                GeoReferenceAlgorithms.GeoReferenceMesh(pointList, list);
            });


        }
    }
}
