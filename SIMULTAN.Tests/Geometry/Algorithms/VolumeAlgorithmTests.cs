using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data.Assets;
using SIMULTAN.Data.Geometry;
using SIMULTAN.Tests.Geometry.BaseGeometries;
using SIMULTAN.Tests.Geometry.EventData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Tests.Geometry.Algorithms
{
    [TestClass]
    public class VolumeAlgorithmTests
    {
        #region Volume Calculation
        [TestMethod]
        public void VolumeTest()
        {
            var model = GeometryModelHelper.CubeModel();

            var vol = VolumeAlgorithms.Volume(model.Volumes[0]);
            Assert.AreEqual(2.0 * 2.0 * 2.0, vol);

            model.Volumes[0].Faces[0].Face.Orientation = GeometricOrientation.Forward;
            model.Volumes[0].Faces[0].Orientation = GeometricOrientation.Forward;

            vol = VolumeAlgorithms.Volume(model.Volumes[0]);
            Assert.AreEqual(2.0 * 2.0 * 2.0, vol);
        }

        [TestMethod]
        public void VolumeTestHole()
        {
            var data = GeometryModelHelper.EmptyModel();
            var layer = data.layer;
            // cube with size 3x3x3 and a hole
            var (_, _, _, f) = VolumeTests.TestData(layer);

            var v0 = new Volume(layer, "", new[] { f[0], f[1], f[2], f[3], f[4], f[5], f[6] });

            Assert.IsTrue(v0.IsConsistentOriented);

            var vol = VolumeAlgorithms.Volume(v0);
            Assert.AreEqual(3 * 3 * 3, vol);

            f[6].Orientation = (GeometricOrientation)((int)f[6].Orientation * -1); // flip should not change volume
            v0.MakeConsistent(false, true);

            vol = VolumeAlgorithms.Volume(v0);
            Assert.AreEqual(3 * 3 * 3, vol);
        }

        [TestMethod]
        public void VolumeTestHoleGeoInside()
        {
            var data = GeometryModelHelper.EmptyModel();
            var layer = data.layer;
            // cube with size 3x3x3 and a hole with geo of size 1x2x1 protruding iside volume
            var (_, _, _, f) = VolumeTests.TestDataSpecialInside(layer, false);

            var v0 = new Volume(layer, "", new[] { f[0], f[1], f[2], f[3], f[4], f[5], f[7], f[8], f[9], f[10], f[11] });

            Assert.IsTrue(v0.IsConsistentOriented);

            var vol = VolumeAlgorithms.Volume(v0);

            Assert.AreEqual(3 * 3 * 3 - 1 * 2 * 1, vol);
        }

        [TestMethod]
        public void VolumeTestHoleGeoInsideFlipped()
        {
            var data = GeometryModelHelper.EmptyModel();
            var layer = data.layer;
            // cube with size 3x3x3 and a hole with geo of size 1x2x1 protruding iside volume
            var (_, _, _, f) = VolumeTests.TestDataSpecialInside(layer, true);

            var v0 = new Volume(layer, "", new[] { f[0], f[1], f[2], f[3], f[4], f[5], f[7], f[8], f[9], f[10], f[11] });

            Assert.IsTrue(v0.IsConsistentOriented);

            var vol = VolumeAlgorithms.Volume(v0);

            Assert.AreEqual(3 * 3 * 3 - 1 * 2 * 1, vol);
        }

        [TestMethod]
        public void VolumeTestHoleGeoOutside()
        {
            var data = GeometryModelHelper.EmptyModel();
            var layer = data.layer;
            // cube with size 3x3x3 and a hole with geo of size 1x2x1 protruding outside volume
            var (_, _, _, f) = VolumeTests.TestDataSpecialOutside(layer, false);

            var v0 = new Volume(layer, "", new[] { f[0], f[1], f[2], f[3], f[4], f[5], f[7], f[8], f[9], f[10], f[11] });

            Assert.IsTrue(v0.IsConsistentOriented);

            var vol = VolumeAlgorithms.Volume(v0);

            Assert.AreEqual(3 * 3 * 3 + 1 * 2 * 1, vol);
        }

        [TestMethod]
        public void VolumeTestHoleGeoOutsideFlipped()
        {
            var data = GeometryModelHelper.EmptyModel();
            var layer = data.layer;
            // cube with size 3x3x3 and a hole with geo of size 1x2x1 protruding outside volume
            var (_, _, _, f) = VolumeTests.TestDataSpecialOutside(layer, false);

            var v0 = new Volume(layer, "", new[] { f[0], f[1], f[2], f[3], f[4], f[5], f[7], f[8], f[9], f[10], f[11] });

            Assert.IsTrue(v0.IsConsistentOriented);

            var vol = VolumeAlgorithms.Volume(v0);

            Assert.AreEqual(3 * 3 * 3 + 1 * 2 * 1, vol);
        }

        [TestMethod]
        public void VolumeTestHoleThroughGeo()
        {
            var data = GeometryModelHelper.EmptyModel();
            var layer = data.layer;
            // cube with size 3x3x3 and a hole with geo of size 1x3x1 going through geo
            var (_, _, _, f) = VolumeTests.TestDataSpecialThroughHole(layer, false, false);

            var v0 = new Volume(layer, "", new[] { f[0], f[1], f[2], f[3], f[4], f[5], f[8], f[9], f[10], f[11] });

            Assert.IsTrue(v0.IsConsistentOriented);

            var vol = VolumeAlgorithms.Volume(v0);
            Assert.AreEqual(3 * 3 * 3 - 1 * 3 * 1, vol);
        }

        [TestMethod]
        public void VolumeTestHoleThroughGeoFlipped()
        {
            var data = GeometryModelHelper.EmptyModel();
            var layer = data.layer;
            // cube with size 3x3x3 and a hole with geo of size 1x3x1 going through geo
            var (_, _, _, f) = VolumeTests.TestDataSpecialThroughHole(layer, true, false);
            var v0 = new Volume(layer, "", new[] { f[0], f[1], f[2], f[3], f[4], f[5], f[8], f[9], f[10], f[11] });
            Assert.IsTrue(v0.IsConsistentOriented);
            var vol = VolumeAlgorithms.Volume(v0);
            Assert.AreEqual(3 * 3 * 3 - 1 * 3 * 1, vol);

            (_, _, _, f) = VolumeTests.TestDataSpecialThroughHole(layer, false, true);
            v0 = new Volume(layer, "", new[] { f[0], f[1], f[2], f[3], f[4], f[5], f[8], f[9], f[10], f[11] });
            Assert.IsTrue(v0.IsConsistentOriented);
            vol = VolumeAlgorithms.Volume(v0);
            Assert.AreEqual(3 * 3 * 3 - 1 * 3 * 1, vol);

            (_, _, _, f) = VolumeTests.TestDataSpecialThroughHole(layer, true, true);
            v0 = new Volume(layer, "", new[] { f[0], f[1], f[2], f[3], f[4], f[5], f[8], f[9], f[10], f[11] });
            Assert.IsTrue(v0.IsConsistentOriented);
            vol = VolumeAlgorithms.Volume(v0);
            Assert.AreEqual(3 * 3 * 3 - 1 * 3 * 1, vol);
        }

        [TestMethod]
        public void VolumeTestHoleThroughGeoWithGeoInside()
        {
            var data = GeometryModelHelper.EmptyModel();
            var layer = data.layer;
            // cube with size 3x3x3 and a hole with geo of size 1x3x1 going through geo and additional 0.5x1x0.5 cube protruding into the volume
            var (_, _, _, f) = VolumeTests.TestDataSpecialThroughHoleWithHoleGeoInside(layer, false, false, false);

            var v0 = new Volume(layer, "", new[] { f[0], f[1], f[2], f[3], f[4], f[5], f[8], f[9], f[10], f[11], f[13], f[14], f[15], f[16], f[17] });
            Assert.IsTrue(v0.IsConsistentOriented);

            var vol = VolumeAlgorithms.Volume(v0);
            Assert.AreEqual(3 * 3 * 3 - 1 * 3 * 1 - 0.5 * 1 * 0.5, vol);
        }

        [TestMethod]
        public void VolumeTestHoleThroughGeoWithGeoInsideFlipped()
        {
            var data = GeometryModelHelper.EmptyModel();
            var layer = data.layer;
            // cube with size 3x3x3 and a hole with geo of size 1x3x1 going through geo and additional 0.5x1x0.5 cube protruding into the volume
            var (_, _, _, f) = VolumeTests.TestDataSpecialThroughHoleWithHoleGeoInside(layer, false, false, true);

            var v0 = new Volume(layer, "", new[] { f[0], f[1], f[2], f[3], f[4], f[5], f[8], f[9], f[10], f[11], f[13], f[14], f[15], f[16], f[17] });
            Assert.IsTrue(v0.IsConsistentOriented);

            var vol = VolumeAlgorithms.Volume(v0);
            Assert.AreEqual(3 * 3 * 3 - 1 * 3 * 1 - 0.5 * 1 * 0.5, vol);
        }

        [TestMethod]
        public void VolumeTestHoleThroughGeoWithGeoOutside()
        {
            var data = GeometryModelHelper.EmptyModel();
            var layer = data.layer;
            // cube with size 3x3x3 and a hole with geo of size 1x3x1 going through geo and additional 0.5x1x0.5 cube protruding outside the volume
            var (_, _, _, f) = VolumeTests.TestDataSpecialThroughHoleWithHoleGeoOutside(layer, false, false, false);

            var v0 = new Volume(layer, "", new[] { f[0], f[1], f[2], f[3], f[4], f[5], f[8], f[9], f[10], f[11], f[13], f[14], f[15], f[16], f[17] });
            Assert.IsTrue(v0.IsConsistentOriented);

            var vol = VolumeAlgorithms.Volume(v0);
            Assert.AreEqual(3 * 3 * 3 - 1 * 3 * 1 + 0.5 * 1 * 0.5, vol);
        }

        [TestMethod]
        public void VolumeTestHoleThroughGeoWithGeoOutsideFlipped()
        {
            var data = GeometryModelHelper.EmptyModel();
            var layer = data.layer;
            // cube with size 3x3x3 and a hole with geo of size 1x3x1 going through geo and additional 0.5x1x0.5 cube protruding outside the volume
            var (_, _, _, f) = VolumeTests.TestDataSpecialThroughHoleWithHoleGeoOutside(layer, false, false, true);

            var v0 = new Volume(layer, "", new[] { f[0], f[1], f[2], f[3], f[4], f[5], f[8], f[9], f[10], f[11], f[13], f[14], f[15], f[16], f[17] });
            Assert.IsTrue(v0.IsConsistentOriented);

            var vol = VolumeAlgorithms.Volume(v0);
            Assert.AreEqual(3 * 3 * 3 - 1 * 3 * 1 + 0.5 * 1 * 0.5, vol);
        }
        #endregion

        #region Area Tests

        [TestMethod]
        public void AreaTest()
        {
            var data = GeometryModelHelper.EmptyModel();
            var layer = data.layer;
            // cube with size 3x3x3 and a hole
            var (_, _, _, f) = VolumeTests.TestData(layer);

            var v0 = new Volume(layer, "", new[] { f[0], f[1], f[2], f[3], f[4], f[5], f[6] });

            Assert.IsTrue(v0.IsConsistentOriented);

            var area = VolumeAlgorithms.AreaBruttoNetto(v0);

            Assert.AreEqual(3 * 3, area.areaReference);
            Assert.AreEqual(Double.NaN, area.areaBrutto);
            Assert.AreEqual(3 * 3, area.areaNetto);
        }

        [TestMethod]
        public void AreaEmptyHoleTest()
        {
            var data = GeometryModelHelper.EmptyModel();
            var layer = data.layer;
            // cube with size 3x3x3 and a hole
            var (_, _, _, f) = VolumeTests.TestData(layer);

            var v0 = new Volume(layer, "", new[] { f[0], f[1], f[2], f[3], f[4], f[5] });

            Assert.IsFalse(v0.IsConsistentOriented);

            var area = VolumeAlgorithms.AreaBruttoNetto(v0);

            //  because the volume is inconsistent, area is zero
            Assert.AreEqual(0, area.areaReference);
            Assert.AreEqual(Double.NaN, area.areaBrutto);
            Assert.AreEqual(0, area.areaNetto);
        }

        #endregion

        [TestMethod]
        public void FindUnclosedEdgesTest()
        {
            var data = GeometryModelHelper.EmptyModel();
            var layer = data.layer;
            // cube with size 3x3x3 and a hole
            var (_, _, _, f) = VolumeTests.TestData(layer);

            var v0 = new Volume(layer, "", new[] { f[0], f[1], f[2], f[3], f[4], f[5], f[6] });
            var v1 = new Volume(layer, "", new[] { f[0], f[1], f[2], f[3], f[4], f[5] });

            Assert.IsTrue(v0.IsConsistentOriented);

            var unclosed = VolumeAlgorithms.FindUnclosedEdges(v0);
            Assert.AreEqual(0, unclosed.Count());

            unclosed = VolumeAlgorithms.FindUnclosedEdges(v1);
            Assert.AreEqual(4, unclosed.Count());
        }
    }
}
