using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data.Assets;
using SIMULTAN.Serializer.SimGeo;
using SIMULTAN.Tests.TestUtils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Tests.IO
{
    [TestClass]
    public class SimGeoIOTests : BaseProjectTest
    {
        private static readonly FileInfo projectFile = new FileInfo(@"./hole_13.simultan");

        /// <summary>
        /// Holes were added to volumes in version 14 so test if version 13 files correctly get the faces added to the volumes.
        /// </summary>
        [TestMethod]
        public void SimGeoHoleMigrationToV14()
        {
            LoadProject(projectFile);
            var geoFile = projectData.AssetManager.Resources.OfType<ContainedResourceFileEntry>().First();

            var errors = new List<SimGeoIOError>();
            var model = SimGeoIO.Load(geoFile, projectData, errors, Data.Geometry.OffsetAlgorithm.Disabled);

            Assert.AreEqual(0, errors.Count);
            Assert.AreEqual(1, model.Geometry.Volumes.Count);
            // cube is only saved with its six faces excluding the hole, so we should have 7 after migration
            Assert.AreEqual(7, model.Geometry.Volumes[0].Faces.Count);
        }
    }
}
