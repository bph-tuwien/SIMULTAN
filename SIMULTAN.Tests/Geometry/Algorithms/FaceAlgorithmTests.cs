
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data.Geometry;
using SIMULTAN.Tests.Geometry.EventData;
using SIMULTAN.Tests.TestUtils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace SIMULTAN.Tests.Geometry.Algorithms
{
    [TestClass]
    public class FaceAlgorithmTests
    {
        [TestMethod]
        public void OrientationInclineNormal()
        {
            var io = FaceAlgorithms.OrientationIncline(new Vector3D(0, 1, 0));
            AssertUtil.AssertDoubleEqual(Math.PI / 2.0, io.incline);
            AssertUtil.AssertDoubleEqual(0.0, io.orientation);

            io = FaceAlgorithms.OrientationIncline(new Vector3D(0, -1, 0));
            AssertUtil.AssertDoubleEqual(-Math.PI / 2.0, io.incline);
            AssertUtil.AssertDoubleEqual(0.0, io.orientation);

            io = FaceAlgorithms.OrientationIncline(new Vector3D(0, 0, 1));
            AssertUtil.AssertDoubleEqual(0.0, io.incline);
            AssertUtil.AssertDoubleEqual(0.0, io.orientation);

            io = FaceAlgorithms.OrientationIncline(new Vector3D(0, 0, -1));
            AssertUtil.AssertDoubleEqual(0.0, io.incline);
            AssertUtil.AssertDoubleEqual(Math.PI, io.orientation);

            io = FaceAlgorithms.OrientationIncline(new Vector3D(1, 0, 0));
            AssertUtil.AssertDoubleEqual(0.0, io.incline);
            AssertUtil.AssertDoubleEqual(Math.PI * 0.5, io.orientation);

            io = FaceAlgorithms.OrientationIncline(new Vector3D(-1, 0, 0));
            AssertUtil.AssertDoubleEqual(0.0, io.incline);
            AssertUtil.AssertDoubleEqual(Math.PI * 1.5, io.orientation);

            var n = new Vector3D(0, 1, 1);
            n.Normalize();
            io = FaceAlgorithms.OrientationIncline(n);
            AssertUtil.AssertDoubleEqual(Math.PI / 4.0, io.incline);
            AssertUtil.AssertDoubleEqual(0.0, io.orientation);

            n = new Vector3D(1, 0, 1);
            n.Normalize();
            io = FaceAlgorithms.OrientationIncline(n);
            AssertUtil.AssertDoubleEqual(0.0, io.incline);
            AssertUtil.AssertDoubleEqual(Math.PI / 4.0, io.orientation);
        }

        [TestMethod]
        public void OrientationInclinePFace()
        {
            var model = GeometryModelHelper.CubeModel();

            Dictionary<string, double> expectedIncline = new Dictionary<string, double>
            {
                { "Floor", -Math.PI / 2.0 },
                { "Ceiling", Math.PI / 2.0 },
                { "WallN", 0.0 },
                { "WallE", 0.0 },
                { "WallS", 0.0 },
                { "WallW", 0.0 },
            };

            Dictionary<string, double> expectedOrientation = new Dictionary<string, double>
            {
                { "Floor", 0.0 },
                { "Ceiling", 0.0 },
                { "WallN", 0.0 },
                { "WallE", Math.PI * 0.5 },
                { "WallS", Math.PI * 1.0 },
                { "WallW", Math.PI * 1.5 },
            };

            foreach (var pface in model.Volumes.First().Faces)
            {
                var io = FaceAlgorithms.OrientationIncline(pface);
                Assert.AreEqual(expectedIncline[pface.Face.Name], io.incline);
                Assert.AreEqual(expectedOrientation[pface.Face.Name], io.orientation);
            }
        }
    }
}
