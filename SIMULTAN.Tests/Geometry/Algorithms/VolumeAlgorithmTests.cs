using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data.Geometry;
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
    }
}
