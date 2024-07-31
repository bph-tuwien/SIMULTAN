using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data.Assets;
using SIMULTAN.Data.SimMath;
using SIMULTAN.Data.SitePlanner;
using SIMULTAN.Projects;
using SIMULTAN.Serializer.DXF;
using SIMULTAN.Serializer.GMDXF;
using SIMULTAN.Tests.Properties;
using SIMULTAN.Tests.TestUtils;
using SIMULTAN.Tests.Util;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace SIMULTAN.Tests.IO
{
    [TestClass]
    public class GeoMapDxfTests : BaseProjectTest
    {
        private static readonly FileInfo testProject = new FileInfo(@"./SiteplannerTest.simultan");
        private ResourceFileEntry mapRes;

        private ExtendedProjectData CreateProjectData()
        {
            LoadProject(testProject);

            mapRes = project.AddEmptyGeoMapResource(null, "testmap", "");
            projectData.SitePlannerManager.GeoMaps[0].GeoReferences.Add(new ImageGeoReference());

            return projectData;
        }

        [TestMethod]
        public void ParseEmptyFileV11()
        {
            CreateProjectData();

            Assert.AreEqual(1, projectData.SitePlannerManager.GeoMaps.Count);

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_GMDXF_EmptyV11)))
            {
                GeoMapDxfIO.Read(reader, new DXFParserInfo(project.GlobalID, projectData) { CurrentFile = new FileInfo(mapRes.CurrentFullPath) });
            }

            Assert.AreEqual(2, projectData.SitePlannerManager.GeoMaps.Count);
            var gmap = projectData.SitePlannerManager.GeoMaps[1];
            Assert.IsNotNull(gmap);
            Assert.AreEqual(null, gmap.MapImageRes);
            Assert.AreEqual(0, gmap.GeoReferences.Count);
        }

        [TestMethod]
        public void WriteEmptyFile()
        {
            ExtendedProjectData data = new ExtendedProjectData();

            var gmap = new GeoMap(null);
            data.SitePlannerManager.GeoMaps.Add(gmap);

            string exportedString = null;
            using (MemoryStream stream = new MemoryStream())
            {
                using (DXFStreamWriter writer = new DXFStreamWriter(stream, true))
                {
                    GeoMapDxfIO.Write(writer, gmap, projectData);
                }

                stream.Flush();
                stream.Position = 0;

                var array = stream.ToArray();
                exportedString = Encoding.UTF8.GetString(array);
            }

            AssertUtil.AreEqualMultiline(Properties.Resources.DXFSerializer_GMDXF_WriteEmpty, exportedString);
        }

        [TestMethod]
        public void ParseWithImageV11()
        {
            CreateProjectData();
            Guid resGuid = new Guid("a5ef2c5d-9519-4335-9ac2-4beb0a597326");

            Assert.AreEqual(1, projectData.SitePlannerManager.GeoMaps.Count);

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_ReadGMDXF_MapResourceV11)))
            {
                GeoMapDxfIO.Read(reader, new DXFParserInfo(project.GlobalID, projectData) { CurrentFile = new FileInfo(mapRes.CurrentFullPath) });
            }

            Assert.AreEqual(2, projectData.SitePlannerManager.GeoMaps.Count);
            var gmap = projectData.SitePlannerManager.GeoMaps[1];
            Assert.IsNotNull(gmap);
            Assert.IsNotNull(gmap.MapImageRes);
            Assert.AreEqual(resGuid, gmap.MapImageRes.ProjectId);
            Assert.AreEqual(12, gmap.MapImageRes.ResourceIndex);
            Assert.IsNull(gmap.MapImageRes.ResourceFile);
            Assert.AreEqual(0, gmap.GeoReferences.Count);
        }

        [TestMethod]
        public void ParseWithImageV12()
        {
            CreateProjectData();
            Guid resGuid = new Guid("a5ef2c5d-9519-4335-9ac2-4beb0a597326");

            Assert.AreEqual(1, projectData.SitePlannerManager.GeoMaps.Count);

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_ReadGMDXF_MapResourceV12)))
            {
                GeoMapDxfIO.Read(reader, new DXFParserInfo(project.GlobalID, projectData) { CurrentFile = new FileInfo(mapRes.CurrentFullPath) });
            }

            Assert.AreEqual(2, projectData.SitePlannerManager.GeoMaps.Count);
            var gmap = projectData.SitePlannerManager.GeoMaps[1];
            Assert.IsNotNull(gmap);
            Assert.IsNotNull(gmap.MapImageRes);
            Assert.AreEqual(resGuid, gmap.MapImageRes.ProjectId);
            Assert.AreEqual(12, gmap.MapImageRes.ResourceIndex);
            Assert.IsNull(gmap.MapImageRes.ResourceFile);
            Assert.AreEqual(0, gmap.GeoReferences.Count);
        }

        [TestMethod]
        public void ParseWithImageOtherProjectV12()
        {
            CreateProjectData();
            Guid resGuid = new Guid("a5ef2c5d-9519-4335-9ac2-4beb0a597327");

            Assert.AreEqual(1, projectData.SitePlannerManager.GeoMaps.Count);

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_ReadGMDXF_MapResourceOtherProjectV12)))
            {
                GeoMapDxfIO.Read(reader, new DXFParserInfo(project.GlobalID, projectData) { CurrentFile = new FileInfo(mapRes.CurrentFullPath) });
            }

            Assert.AreEqual(2, projectData.SitePlannerManager.GeoMaps.Count);
            var gmap = projectData.SitePlannerManager.GeoMaps[1];
            Assert.IsNotNull(gmap);
            Assert.IsNotNull(gmap.MapImageRes);
            Assert.AreEqual(resGuid, gmap.MapImageRes.ProjectId);
            Assert.AreEqual(12, gmap.MapImageRes.ResourceIndex);
            Assert.IsNull(gmap.MapImageRes.ResourceFile);
            Assert.AreEqual(0, gmap.GeoReferences.Count);
        }

        [TestMethod]
        public void WriteWithImage()
        {
            CreateProjectData();
            ExtendedProjectData data = new ExtendedProjectData();

            var gmap = new GeoMap(null);
            gmap.MapImageRes = new ResourceReference(new Guid("a5ef2c5d-9519-4335-9ac2-4beb0a597326"), 12, data.AssetManager);
            data.SitePlannerManager.GeoMaps.Add(gmap);

            string exportedString = null;
            using (MemoryStream stream = new MemoryStream())
            {
                using (DXFStreamWriter writer = new DXFStreamWriter(stream, true))
                {
                    GeoMapDxfIO.Write(writer, gmap, projectData);
                }

                stream.Flush();
                stream.Position = 0;

                var array = stream.ToArray();
                exportedString = Encoding.UTF8.GetString(array);
            }

            AssertUtil.AreEqualMultiline(Properties.Resources.DXFSerializer_GMDXF_WriteMapResource, exportedString);
        }

        [TestMethod]
        public void ParseGeoReferencesV12()
        {
            CreateProjectData();
            Guid resGuid = new Guid("a5ef2c5d-9519-4335-9ac2-4beb0a597326");

            Assert.AreEqual(1, projectData.SitePlannerManager.GeoMaps.Count);

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_ReadGMDXF_GeoReferencesV12)))
            {
                GeoMapDxfIO.Read(reader, new DXFParserInfo(project.GlobalID, projectData) { CurrentFile = new FileInfo(mapRes.CurrentFullPath) });
            }

            Assert.AreEqual(2, projectData.SitePlannerManager.GeoMaps.Count);
            var gmap = projectData.SitePlannerManager.GeoMaps[1];
            Assert.IsNotNull(gmap);
            Assert.AreEqual(2, gmap.GeoReferences.Count);

            AssertUtil.AssertDoubleEqual(0.6, gmap.GeoReferences[0].ImagePosition.X);
            AssertUtil.AssertDoubleEqual(0.3, gmap.GeoReferences[0].ImagePosition.Y);
            AssertUtil.AssertDoubleEqual(10.0, gmap.GeoReferences[0].ReferencePoint.X);
            AssertUtil.AssertDoubleEqual(20.0, gmap.GeoReferences[0].ReferencePoint.Y);
            AssertUtil.AssertDoubleEqual(30.0, gmap.GeoReferences[0].ReferencePoint.Z);

            AssertUtil.AssertDoubleEqual(0.5, gmap.GeoReferences[1].ImagePosition.X);
            AssertUtil.AssertDoubleEqual(0.8, gmap.GeoReferences[1].ImagePosition.Y);
            AssertUtil.AssertDoubleEqual(40.0, gmap.GeoReferences[1].ReferencePoint.X);
            AssertUtil.AssertDoubleEqual(50.0, gmap.GeoReferences[1].ReferencePoint.Y);
            AssertUtil.AssertDoubleEqual(60.0, gmap.GeoReferences[1].ReferencePoint.Z);
        }

        [TestMethod]
        public void WriteGeoReferences()
        {
            ExtendedProjectData data = new ExtendedProjectData();

            var gmap = new GeoMap(null);
            data.SitePlannerManager.GeoMaps.Add(gmap);

            var gref1 = new ImageGeoReference(new SimPoint(0.6, 0.3), new SimPoint3D(10.0, 20.0, 30.0));
            var gref2 = new ImageGeoReference(new SimPoint(0.5, 0.8), new SimPoint3D(40.0, 50.0, 60.0));

            gmap.GeoReferences.Add(gref1);
            gmap.GeoReferences.Add(gref2);

            string exportedString = null;
            using (MemoryStream stream = new MemoryStream())
            {
                using (DXFStreamWriter writer = new DXFStreamWriter(stream, true))
                {
                    GeoMapDxfIO.Write(writer, gmap, projectData);
                }

                stream.Flush();
                stream.Position = 0;

                var array = stream.ToArray();
                exportedString = Encoding.UTF8.GetString(array);
            }

            AssertUtil.AreEqualMultiline(Properties.Resources.DXFSerializer_GMDXF_WriteGeoReferences, exportedString);
        }
    }
}
