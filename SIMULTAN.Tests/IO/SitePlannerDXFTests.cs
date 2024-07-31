using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data;
using SIMULTAN.Data.Assets;
using SIMULTAN.Data.SimMath;
using SIMULTAN.Data.SitePlanner;
using SIMULTAN.Data.ValueMappings;
using SIMULTAN.Projects;
using SIMULTAN.Serializer.DXF;
using SIMULTAN.Serializer.SPDXF;
using SIMULTAN.Tests.Properties;
using SIMULTAN.Tests.TestUtils;
using SIMULTAN.Tests.Util;
using SIMULTAN.Utils;
using System;
using System.IO;
using System.Linq;
using System.Text;


namespace SIMULTAN.Tests.IO
{
    [TestClass]
    public class SitePlannerDXFTests : BaseProjectTest
    {
        private static readonly FileInfo testProject = new FileInfo(@"./SiteplannerTest.simultan");
        private ResourceFileEntry spRes;
        private static int startSiteplannerCount = 2;

        private ExtendedProjectData CreateProjectData()
        {
            LoadProject(testProject);

            spRes = project.AddEmptySitePlannerResource(null, "TestSiteplanner", "");

            return projectData;
        }

        [TestMethod]
        public void ParseEmptyFileV11()
        {
            CreateProjectData();

            Assert.AreEqual(startSiteplannerCount, projectData.SitePlannerManager.SitePlannerProjects.Count);
            var parseInfo = new DXFParserInfo(project.GlobalID, projectData) { CurrentFile = new FileInfo(spRes.CurrentFullPath) };

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_SPDXF_EmptyV11)))
            {
                SiteplannerDxfIO.Read(reader, parseInfo);
            }

            Assert.AreEqual(11UL, parseInfo.FileVersion);

            Assert.AreEqual(startSiteplannerCount + 1, projectData.SitePlannerManager.SitePlannerProjects.Count);
            var sp = projectData.SitePlannerManager.SitePlannerProjects[1];
            Assert.IsTrue(projectData.ComponentGeometryExchange.IsConnected(sp));
            Assert.AreEqual(0, sp.Maps.Count);
            Assert.AreEqual(0, sp.Buildings.Count);
            Assert.AreEqual(0, sp.ValueMappings.Count);
        }
        [TestMethod]
        public void ParseEmptyFileV12()
        {
            CreateProjectData();

            Assert.AreEqual(startSiteplannerCount, projectData.SitePlannerManager.SitePlannerProjects.Count);

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_SPDXF_EmptyV12)))
            {
                SiteplannerDxfIO.Read(reader, new DXFParserInfo(project.GlobalID, projectData) { CurrentFile = new FileInfo(spRes.CurrentFullPath) });
            }

            Assert.AreEqual(startSiteplannerCount + 1, projectData.SitePlannerManager.SitePlannerProjects.Count);
            var sp = projectData.SitePlannerManager.SitePlannerProjects[1];
            Assert.AreEqual(0, sp.Maps.Count);
            Assert.AreEqual(0, sp.Buildings.Count);
            Assert.AreEqual(0, sp.ValueMappings.Count);
            Assert.IsNull(sp.ActiveValueMapping);
        }
        [TestMethod]
        public void ParseEmptyFileV13()
        {
            CreateProjectData();

            Assert.AreEqual(startSiteplannerCount, projectData.SitePlannerManager.SitePlannerProjects.Count);

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_SPDXF_EmptyV13)))
            {
                SiteplannerDxfIO.Read(reader, new DXFParserInfo(project.GlobalID, projectData) { CurrentFile = new FileInfo(spRes.CurrentFullPath) });
            }

            Assert.AreEqual(startSiteplannerCount + 1, projectData.SitePlannerManager.SitePlannerProjects.Count);
            var sp = projectData.SitePlannerManager.SitePlannerProjects[1];
            Assert.AreEqual(0, sp.Maps.Count);
            Assert.AreEqual(0, sp.Buildings.Count);
            Assert.AreEqual(0, sp.ValueMappings.Count);
            Assert.IsNull(sp.ActiveValueMapping);
        }
        [TestMethod]
        public void WriteEmptyFile()
        {
            ExtendedProjectData data = new ExtendedProjectData();

            var siteplannerProject = new SitePlannerProject(null);
            data.SitePlannerManager.SitePlannerProjects.Add(siteplannerProject);

            string exportedString = null;
            using (MemoryStream stream = new MemoryStream())
            {
                using (DXFStreamWriter writer = new DXFStreamWriter(stream, true))
                {
                    SiteplannerDxfIO.Write(writer, siteplannerProject, data);
                }

                stream.Flush();
                stream.Position = 0;

                var array = stream.ToArray();
                exportedString = Encoding.UTF8.GetString(array);
            }

            AssertUtil.AreEqualMultiline(Properties.Resources.DXFSerializer_SPDXF_WriteEmpty, exportedString);
        }


        [TestMethod]
        public void ParseWithGeoMapsV11()
        {
            CreateProjectData();

            Assert.AreEqual(startSiteplannerCount, projectData.SitePlannerManager.SitePlannerProjects.Count);

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_SPDXF_GeoMapsV11)))
            {
                SiteplannerDxfIO.Read(reader, new DXFParserInfo(project.GlobalID, projectData) { CurrentFile = new FileInfo(spRes.CurrentFullPath) });
            }

            Assert.AreEqual(startSiteplannerCount + 1, projectData.SitePlannerManager.SitePlannerProjects.Count);
            var sp = projectData.SitePlannerManager.SitePlannerProjects[startSiteplannerCount];
            Assert.AreEqual(2, sp.Maps.Count);

            Assert.AreEqual(new Guid("a5ef2c5d-9519-4335-9ac2-4beb0a597326"), sp.Maps[0].GeoMapRes.ProjectId);
            Assert.AreEqual(12, sp.Maps[0].GeoMapRes.ResourceIndex);
            Assert.AreEqual("", sp.Maps[0].ElevationProviderTypeName);
            Assert.AreEqual(100, sp.Maps[0].GridCellSize);

            Assert.AreEqual(new Guid("a5ef2c5d-9519-4335-9ac2-4beb0a597327"), sp.Maps[1].GeoMapRes.ProjectId);
            Assert.AreEqual(13, sp.Maps[1].GeoMapRes.ResourceIndex);
            Assert.AreEqual("S3ElevationProviderGtiff", sp.Maps[1].ElevationProviderTypeName);
            Assert.AreEqual(123, sp.Maps[1].GridCellSize);

            Assert.AreEqual(0, sp.Buildings.Count);
            Assert.AreEqual(0, sp.ValueMappings.Count);
            Assert.IsNull(sp.ActiveValueMapping);
        }
        [TestMethod]
        public void ParseWithGeoMapsV12()
        {
            CreateProjectData();

            Assert.AreEqual(startSiteplannerCount, projectData.SitePlannerManager.SitePlannerProjects.Count);

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_SPDXF_GeoMapsV12)))
            {
                SiteplannerDxfIO.Read(reader, new DXFParserInfo(project.GlobalID, projectData) { CurrentFile = new FileInfo(spRes.CurrentFullPath) });
            }

            Assert.AreEqual(startSiteplannerCount + 1, projectData.SitePlannerManager.SitePlannerProjects.Count);
            var sp = projectData.SitePlannerManager.SitePlannerProjects[startSiteplannerCount];
            Assert.AreEqual(2, sp.Maps.Count);

            Assert.AreEqual(new Guid("a5ef2c5d-9519-4335-9ac2-4beb0a597326"), sp.Maps[0].GeoMapRes.ProjectId);
            Assert.AreEqual(12, sp.Maps[0].GeoMapRes.ResourceIndex);
            Assert.AreEqual("", sp.Maps[0].ElevationProviderTypeName);
            Assert.AreEqual(100, sp.Maps[0].GridCellSize);

            Assert.AreEqual(new Guid("a5ef2c5d-9519-4335-9ac2-4beb0a597327"), sp.Maps[1].GeoMapRes.ProjectId);
            Assert.AreEqual(13, sp.Maps[1].GeoMapRes.ResourceIndex);
            Assert.AreEqual("S3ElevationProviderGtiff", sp.Maps[1].ElevationProviderTypeName);
            Assert.AreEqual(123, sp.Maps[1].GridCellSize);

            Assert.AreEqual(0, sp.Buildings.Count);
            Assert.IsNull(sp.ActiveValueMapping);
            Assert.AreEqual(0, sp.ValueMappings.Count);
        }
        [TestMethod]
        public void ParseWithGeoMapsV13()
        {
            CreateProjectData();

            Assert.AreEqual(startSiteplannerCount, projectData.SitePlannerManager.SitePlannerProjects.Count);

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_SPDXF_GeoMapsV13)))
            {
                SiteplannerDxfIO.Read(reader, new DXFParserInfo(project.GlobalID, projectData) { CurrentFile = new FileInfo(spRes.CurrentFullPath) });
            }

            Assert.AreEqual(startSiteplannerCount + 1, projectData.SitePlannerManager.SitePlannerProjects.Count);
            var sp = projectData.SitePlannerManager.SitePlannerProjects[startSiteplannerCount];
            Assert.AreEqual(2, sp.Maps.Count);

            Assert.AreEqual(new Guid("a5ef2c5d-9519-4335-9ac2-4beb0a597326"), sp.Maps[0].GeoMapRes.ProjectId);
            Assert.AreEqual(12, sp.Maps[0].GeoMapRes.ResourceIndex);
            Assert.AreEqual("", sp.Maps[0].ElevationProviderTypeName);
            Assert.AreEqual(100, sp.Maps[0].GridCellSize);

            Assert.AreEqual(new Guid("a5ef2c5d-9519-4335-9ac2-4beb0a597327"), sp.Maps[1].GeoMapRes.ProjectId);
            Assert.AreEqual(13, sp.Maps[1].GeoMapRes.ResourceIndex);
            Assert.AreEqual("S3ElevationProviderGtiff", sp.Maps[1].ElevationProviderTypeName);
            Assert.AreEqual(123, sp.Maps[1].GridCellSize);

            Assert.AreEqual(0, sp.Buildings.Count);
            Assert.IsNull(sp.ActiveValueMapping);
            Assert.AreEqual(0, sp.ValueMappings.Count);
        }
        [TestMethod]
        public void WriteWithGeoMap()
        {
            ExtendedProjectData data = new ExtendedProjectData();
            data.SetCallingLocation(new DummyReferenceLocation(new Guid("a5ef2c5d-9519-4335-9ac2-4beb0a597326")));

            var siteplannerProject = new SitePlannerProject(null);
            data.SitePlannerManager.SitePlannerProjects.Add(siteplannerProject);

            var map = new SitePlannerMap(new ResourceReference(new Guid("a5ef2c5d-9519-4335-9ac2-4beb0a597326"), 12))
            {
                ElevationProvider = null,
                GridCellSize = 100
            };
            siteplannerProject.Maps.Add(map);
            map = new SitePlannerMap(new ResourceReference(new Guid("a5ef2c5d-9519-4335-9ac2-4beb0a597327"), 13))
            {
                ElevationProvider = null,
                ElevationProviderTypeName = "S3ElevationProviderGtiff",
                GridCellSize = 123
            };
            siteplannerProject.Maps.Add(map);

            string exportedString = null;
            using (MemoryStream stream = new MemoryStream())
            {
                using (DXFStreamWriter writer = new DXFStreamWriter(stream, true))
                {
                    SiteplannerDxfIO.Write(writer, siteplannerProject, data);
                }

                stream.Flush();
                stream.Position = 0;

                var array = stream.ToArray();
                exportedString = Encoding.UTF8.GetString(array);
            }

            AssertUtil.AreEqualMultiline(Properties.Resources.DXFSerializer_SPDXF_WriteGeoMaps, exportedString);
        }

        [TestMethod]
        public void ParseWithBuildingsV11()
        {
            CreateProjectData();

            Assert.AreEqual(startSiteplannerCount, projectData.SitePlannerManager.SitePlannerProjects.Count);

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_SPDXF_BuildingsV11)))
            {
                SiteplannerDxfIO.Read(reader, new DXFParserInfo(project.GlobalID, projectData) { CurrentFile = new FileInfo(spRes.CurrentFullPath) });
            }

            Assert.AreEqual(startSiteplannerCount + 1, projectData.SitePlannerManager.SitePlannerProjects.Count);
            var sp = projectData.SitePlannerManager.SitePlannerProjects[startSiteplannerCount];
            Assert.AreEqual(0, sp.Maps.Count);
            Assert.AreEqual(2, sp.Buildings.Count);

            Assert.AreEqual(2UL, sp.Buildings[0].ID);
            Assert.AreEqual(new Guid("a5ef2c5d-9519-4335-9ac2-4beb0a597326"), sp.Buildings[0].GeometryModelRes.ProjectId);
            Assert.AreEqual(5, sp.Buildings[0].GeometryModelRes.ResourceIndex);
            Assert.AreEqual(SimColor.FromRgb(0, 128, 255), sp.Buildings[0].CustomColor);

            Assert.AreEqual(3UL, sp.Buildings[1].ID);
            Assert.AreEqual(new Guid("a5ef2c5d-9519-4335-9ac2-4beb0a597327"), sp.Buildings[1].GeometryModelRes.ProjectId);
            Assert.AreEqual(6, sp.Buildings[1].GeometryModelRes.ResourceIndex);
            Assert.AreEqual(SimColor.FromRgb(128, 0, 32), sp.Buildings[1].CustomColor);

            Assert.IsNull(sp.ActiveValueMapping);
            Assert.AreEqual(0, sp.ValueMappings.Count);
        }
        [TestMethod]
        public void ParseWithBuildingsV12()
        {
            CreateProjectData();

            Assert.AreEqual(startSiteplannerCount, projectData.SitePlannerManager.SitePlannerProjects.Count);

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_SPDXF_BuildingsV12)))
            {
                SiteplannerDxfIO.Read(reader, new DXFParserInfo(project.GlobalID, projectData) { CurrentFile = new FileInfo(spRes.CurrentFullPath) });
            }

            Assert.AreEqual(startSiteplannerCount + 1, projectData.SitePlannerManager.SitePlannerProjects.Count);
            var sp = projectData.SitePlannerManager.SitePlannerProjects[startSiteplannerCount];
            Assert.AreEqual(0, sp.Maps.Count);
            Assert.AreEqual(2, sp.Buildings.Count);

            Assert.AreEqual(2UL, sp.Buildings[0].ID);
            Assert.AreEqual(new Guid("a5ef2c5d-9519-4335-9ac2-4beb0a597326"), sp.Buildings[0].GeometryModelRes.ProjectId);
            Assert.AreEqual(5, sp.Buildings[0].GeometryModelRes.ResourceIndex);
            Assert.AreEqual(SimColor.FromRgb(0, 128, 255), sp.Buildings[0].CustomColor);

            Assert.AreEqual(3UL, sp.Buildings[1].ID);
            Assert.AreEqual(new Guid("a5ef2c5d-9519-4335-9ac2-4beb0a597327"), sp.Buildings[1].GeometryModelRes.ProjectId);
            Assert.AreEqual(6, sp.Buildings[1].GeometryModelRes.ResourceIndex);
            Assert.AreEqual(SimColor.FromRgb(128, 0, 32), sp.Buildings[1].CustomColor);

            Assert.IsNull(sp.ActiveValueMapping);
            Assert.AreEqual(0, sp.ValueMappings.Count);
        }
        [TestMethod]
        public void ParseWithBuildingsV13()
        {
            CreateProjectData();

            Assert.AreEqual(startSiteplannerCount, projectData.SitePlannerManager.SitePlannerProjects.Count);

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_SPDXF_BuildingsV13)))
            {
                SiteplannerDxfIO.Read(reader, new DXFParserInfo(project.GlobalID, projectData) { CurrentFile = new FileInfo(spRes.CurrentFullPath) });
            }

            Assert.AreEqual(startSiteplannerCount + 1, projectData.SitePlannerManager.SitePlannerProjects.Count);
            var sp = projectData.SitePlannerManager.SitePlannerProjects[startSiteplannerCount];
            Assert.AreEqual(0, sp.Maps.Count);
            Assert.AreEqual(2, sp.Buildings.Count);

            Assert.AreEqual(2UL, sp.Buildings[0].ID);
            Assert.AreEqual(new Guid("a5ef2c5d-9519-4335-9ac2-4beb0a597326"), sp.Buildings[0].GeometryModelRes.ProjectId);
            Assert.AreEqual(5, sp.Buildings[0].GeometryModelRes.ResourceIndex);
            Assert.AreEqual(SimColor.FromRgb(0, 128, 255), sp.Buildings[0].CustomColor);

            Assert.AreEqual(3UL, sp.Buildings[1].ID);
            Assert.AreEqual(new Guid("a5ef2c5d-9519-4335-9ac2-4beb0a597327"), sp.Buildings[1].GeometryModelRes.ProjectId);
            Assert.AreEqual(6, sp.Buildings[1].GeometryModelRes.ResourceIndex);
            Assert.AreEqual(SimColor.FromRgb(128, 0, 32), sp.Buildings[1].CustomColor);

            Assert.IsNull(sp.ActiveValueMapping);
            Assert.AreEqual(0, sp.ValueMappings.Count);
        }
        [TestMethod]
        public void WriteWithBuildings()
        {
            ExtendedProjectData data = new ExtendedProjectData();
            data.SetCallingLocation(new DummyReferenceLocation(new Guid("a5ef2c5d-9519-4335-9ac2-4beb0a597326")));

            var siteplannerProject = new SitePlannerProject(null);
            data.SitePlannerManager.SitePlannerProjects.Add(siteplannerProject);

            var building = new SitePlannerBuilding(2, new ResourceReference(new Guid("a5ef2c5d-9519-4335-9ac2-4beb0a597326"), 5))
            {
                CustomColor = SimColor.FromRgb(0, 128, 255),
            };
            siteplannerProject.Buildings.Add(building);
            building = new SitePlannerBuilding(3, new ResourceReference(new Guid("a5ef2c5d-9519-4335-9ac2-4beb0a597327"), 6))
            {
                CustomColor = SimColor.FromRgb(128, 0, 32),
            };
            siteplannerProject.Buildings.Add(building);

            string exportedString = null;
            using (MemoryStream stream = new MemoryStream())
            {
                using (DXFStreamWriter writer = new DXFStreamWriter(stream, true))
                {
                    SiteplannerDxfIO.Write(writer, siteplannerProject, data);
                }

                stream.Flush();
                stream.Position = 0;

                var array = stream.ToArray();
                exportedString = Encoding.UTF8.GetString(array);
            }

            AssertUtil.AreEqualMultiline(Properties.Resources.DXFSerializer_SPDXF_WriteBuildings, exportedString);
        }


        [TestMethod]
        public void ParseWithValueMappingV5()
        {
            CreateProjectData();
            projectData.SetCallingLocation(new DummyReferenceLocation(new Guid("a5ef2c5d-9519-4335-9ac2-4beb0a597326")));

            var table = MultiValueDxfTests.CreateBigTable();
            table.Id = new SimId(projectData.SitePlannerManager.CalledFromLocation.GlobalID, 552151483658);

            projectData.ValueManager.StartLoading();
            projectData.ValueManager.Add(table);
            projectData.ValueManager.EndLoading();

            Assert.AreEqual(startSiteplannerCount, projectData.SitePlannerManager.SitePlannerProjects.Count);

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_SPDXF_AssociationsV5)))
            {
                SiteplannerDxfIO.Read(reader, new DXFParserInfo(project.GlobalID, projectData) { CurrentFile = new FileInfo(spRes.CurrentFullPath) });
            }

            Assert.AreEqual(startSiteplannerCount + 1, projectData.SitePlannerManager.SitePlannerProjects.Count);
            var sp = projectData.SitePlannerManager.SitePlannerProjects[startSiteplannerCount];
            Assert.AreEqual(0, sp.Maps.Count);
            Assert.AreEqual(0, sp.Buildings.Count);
            Assert.IsNotNull(sp.ActiveValueMapping);
            Assert.AreEqual(4, sp.ValueMappings.Count);

            var valueMapping = sp.ValueMappings[0];
            Assert.AreEqual("Test1", valueMapping.Name);
            Assert.AreEqual(new Guid("a5ef2c5d-9519-4335-9ac2-4beb0a597326"), valueMapping.Table.GlobalID);
            Assert.AreEqual(1074741824, valueMapping.Table.LocalID);
            Assert.AreEqual(SimComponentIndexUsage.Column, valueMapping.ComponentIndexUsage);

            var colorMap = valueMapping.ColorMap;
            Assert.IsInstanceOfType(colorMap, typeof(SimThresholdColorMap));
            var mtcm = (SimThresholdColorMap)colorMap;
            Assert.AreEqual(0, mtcm.ColorMarkers[0].Value);
            Assert.AreEqual(SimColorConverter.ConvertFromString("#FF3FFF00"), mtcm.ColorMarkers[0].Color);
            Assert.AreEqual(3.5, mtcm.ColorMarkers[1].Value);
            Assert.AreEqual(SimColorConverter.ConvertFromString("#FFFFFFFF"), mtcm.ColorMarkers[1].Color);
            Assert.AreEqual(100, mtcm.ColorMarkers[2].Value);
            Assert.AreEqual(SimColorConverter.ConvertFromString("#12345678"), mtcm.ColorMarkers[2].Color);

            var prefilter = valueMapping.Prefilter;
            Assert.IsInstanceOfType(prefilter, typeof(SimDefaultPrefilter));

            valueMapping = sp.ValueMappings[1];
            Assert.AreEqual("Test2", valueMapping.Name);
            Assert.AreEqual(new Guid("a5ef2c5d-9519-4335-9ac2-4beb0a597326"), valueMapping.Table.GlobalID);
            Assert.AreEqual(1074741824, valueMapping.Table.LocalID);
            Assert.AreEqual(SimComponentIndexUsage.Row, valueMapping.ComponentIndexUsage);

            colorMap = valueMapping.ColorMap;
            Assert.IsInstanceOfType(colorMap, typeof(SimLinearGradientColorMap));
            var lgcm = (SimLinearGradientColorMap)colorMap;
            Assert.AreEqual(0, lgcm.ColorMarkers[0].Value);
            Assert.AreEqual(SimColorConverter.ConvertFromString("#FF3FFF00"), lgcm.ColorMarkers[0].Color);
            Assert.AreEqual(33, lgcm.ColorMarkers[1].Value);
            Assert.AreEqual(SimColorConverter.ConvertFromString("#FFFFFFFF"), lgcm.ColorMarkers[1].Color);
            Assert.AreEqual(100, lgcm.ColorMarkers[2].Value);
            Assert.AreEqual(SimColorConverter.ConvertFromString("#FFFFFFFF"), lgcm.ColorMarkers[2].Color);

            prefilter = valueMapping.Prefilter;
            Assert.IsInstanceOfType(prefilter, typeof(SimMinimumPrefilter));

            valueMapping = sp.ValueMappings[2];
            Assert.AreEqual("Test3", valueMapping.Name);
            prefilter = valueMapping.Prefilter;
            Assert.IsInstanceOfType(prefilter, typeof(SimMaximumPrefilter));

            valueMapping = sp.ValueMappings[3];
            Assert.AreEqual("Test4", valueMapping.Name);
            prefilter = valueMapping.Prefilter;
            Assert.IsInstanceOfType(prefilter, typeof(SimAveragePrefilter));

            Assert.AreEqual(sp.ValueMappings[1], sp.ActiveValueMapping);
        }
        [TestMethod]
        public void ParseWithValueMappingV11()
        {
            CreateProjectData();
            projectData.SetCallingLocation(new DummyReferenceLocation(new Guid("a5ef2c5d-9519-4335-9ac2-4beb0a597326")));

            var table = MultiValueDxfTests.CreateBigTable();
            table.Id = new SimId(projectData.SitePlannerManager.CalledFromLocation.GlobalID, 552151483658);

            projectData.ValueManager.StartLoading();
            projectData.ValueManager.Add(table);
            projectData.ValueManager.EndLoading();

            Assert.AreEqual(startSiteplannerCount, projectData.SitePlannerManager.SitePlannerProjects.Count);

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_SPDXF_AssociationsV11)))
            {
                SiteplannerDxfIO.Read(reader, new DXFParserInfo(project.GlobalID, projectData) { CurrentFile = new FileInfo(spRes.CurrentFullPath) });
            }

            Assert.AreEqual(startSiteplannerCount + 1, projectData.SitePlannerManager.SitePlannerProjects.Count);
            var sp = projectData.SitePlannerManager.SitePlannerProjects[startSiteplannerCount];
            Assert.AreEqual(0, sp.Maps.Count);
            Assert.AreEqual(0, sp.Buildings.Count);
            Assert.AreEqual(4, sp.ValueMappings.Count);

            var mapping = sp.ValueMappings[0];
            Assert.AreEqual("Test1", mapping.Name);
            Assert.AreEqual(new Guid("a5ef2c5d-9519-4335-9ac2-4beb0a597326"), mapping.Table.GlobalID);
            Assert.AreEqual(552151483658, mapping.Table.LocalID);
            Assert.AreEqual(SimComponentIndexUsage.Column, mapping.ComponentIndexUsage);

            var colorMap = mapping.ColorMap;
            Assert.IsInstanceOfType(colorMap, typeof(SimThresholdColorMap));
            var mtcm = (SimThresholdColorMap)colorMap;
            Assert.AreEqual(0, mtcm.ColorMarkers[0].Value);
            Assert.AreEqual(SimColorConverter.ConvertFromString("#FF3FFF00"), mtcm.ColorMarkers[0].Color);
            Assert.AreEqual(3.5, mtcm.ColorMarkers[1].Value);
            Assert.AreEqual(SimColorConverter.ConvertFromString("#FFFFFFFF"), mtcm.ColorMarkers[1].Color);
            Assert.AreEqual(100, mtcm.ColorMarkers[2].Value);
            Assert.AreEqual(SimColorConverter.ConvertFromString("#12345678"), mtcm.ColorMarkers[2].Color);

            var prefilter = mapping.Prefilter;
            Assert.IsInstanceOfType(prefilter, typeof(SimDefaultPrefilter));

            mapping = sp.ValueMappings[1];
            Assert.AreEqual("Test2", mapping.Name);
            Assert.AreEqual(new Guid("a5ef2c5d-9519-4335-9ac2-4beb0a597326"), mapping.Table.GlobalID);
            Assert.AreEqual(552151483658, mapping.Table.LocalID);
            Assert.AreEqual(SimComponentIndexUsage.Row, mapping.ComponentIndexUsage);

            colorMap = mapping.ColorMap;
            Assert.IsInstanceOfType(colorMap, typeof(SimLinearGradientColorMap));
            var lgcm = (SimLinearGradientColorMap)colorMap;
            Assert.AreEqual(0, lgcm.ColorMarkers[0].Value);
            Assert.AreEqual(SimColorConverter.ConvertFromString("#FF3FFF00"), lgcm.ColorMarkers[0].Color);
            Assert.AreEqual(33, lgcm.ColorMarkers[1].Value);
            Assert.AreEqual(SimColorConverter.ConvertFromString("#FFFFFFFF"), lgcm.ColorMarkers[1].Color);
            Assert.AreEqual(100, lgcm.ColorMarkers[2].Value);
            Assert.AreEqual(SimColorConverter.ConvertFromString("#FFFFFFFF"), lgcm.ColorMarkers[2].Color);

            prefilter = mapping.Prefilter;
            Assert.IsInstanceOfType(prefilter, typeof(SimMinimumPrefilter));

            mapping = sp.ValueMappings[2];
            Assert.AreEqual("Test3", mapping.Name);
            prefilter = mapping.Prefilter;
            Assert.IsInstanceOfType(prefilter, typeof(SimMaximumPrefilter));

            mapping = sp.ValueMappings[3];
            Assert.AreEqual("Test4", mapping.Name);
            prefilter = mapping.Prefilter;
            Assert.IsInstanceOfType(prefilter, typeof(SimAveragePrefilter));

            Assert.AreEqual(sp.ValueMappings[1], sp.ActiveValueMapping);
        }
        [TestMethod]
        public void ParseWithValueMappingV12()
        {
            CreateProjectData();
            projectData.SetCallingLocation(new DummyReferenceLocation(new Guid("a5ef2c5d-9519-4335-9ac2-4beb0a597326")));

            var table = MultiValueDxfTests.CreateBigTable();
            table.Id = new SimId(projectData.SitePlannerManager.CalledFromLocation.GlobalID, 552151483658);

            projectData.ValueManager.StartLoading();
            projectData.ValueManager.Add(table);
            projectData.ValueManager.EndLoading();

            Assert.AreEqual(startSiteplannerCount, projectData.SitePlannerManager.SitePlannerProjects.Count);

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_SPDXF_AssociationsParseV12)))
            {
                SiteplannerDxfIO.Read(reader, new DXFParserInfo(project.GlobalID, projectData) { CurrentFile = new FileInfo(spRes.CurrentFullPath) });
            }

            Assert.AreEqual(startSiteplannerCount + 1, projectData.SitePlannerManager.SitePlannerProjects.Count);
            var sp = projectData.SitePlannerManager.SitePlannerProjects[startSiteplannerCount];
            Assert.AreEqual(0, sp.Maps.Count);
            Assert.AreEqual(0, sp.Buildings.Count);
            Assert.AreEqual(4, sp.ValueMappings.Count);

            var association = sp.ValueMappings[0];
            Assert.AreEqual("Test1", association.Name);
            Assert.AreEqual(new Guid("a5ef2c5d-9519-4335-9ac2-4beb0a597326"), association.Table.GlobalID);
            Assert.AreEqual(552151483658, association.Table.LocalID);
            Assert.AreEqual(SimComponentIndexUsage.Column, association.ComponentIndexUsage);

            var colorMap = association.ColorMap;
            Assert.IsInstanceOfType(colorMap, typeof(SimThresholdColorMap));
            var mtcm = (SimThresholdColorMap)colorMap;
            Assert.AreEqual(0, mtcm.ColorMarkers[0].Value);
            Assert.AreEqual(SimColorConverter.ConvertFromString("#FF3FFF00"), mtcm.ColorMarkers[0].Color);
            Assert.AreEqual(3.5, mtcm.ColorMarkers[1].Value);
            Assert.AreEqual(SimColorConverter.ConvertFromString("#FFFFFFFF"), mtcm.ColorMarkers[1].Color);
            Assert.AreEqual(100, mtcm.ColorMarkers[2].Value);
            Assert.AreEqual(SimColorConverter.ConvertFromString("#12345678"), mtcm.ColorMarkers[2].Color);

            var prefilter = association.Prefilter;
            Assert.IsInstanceOfType(prefilter, typeof(SimDefaultPrefilter));

            association = sp.ValueMappings[1];
            Assert.AreEqual("Test2", association.Name);
            Assert.AreEqual(new Guid("a5ef2c5d-9519-4335-9ac2-4beb0a597326"), association.Table.GlobalID);
            Assert.AreEqual(552151483658, association.Table.LocalID);
            Assert.AreEqual(SimComponentIndexUsage.Row, association.ComponentIndexUsage);

            colorMap = association.ColorMap;
            Assert.IsInstanceOfType(colorMap, typeof(SimLinearGradientColorMap));
            var lgcm = (SimLinearGradientColorMap)colorMap;
            Assert.AreEqual(0, lgcm.ColorMarkers[0].Value);
            Assert.AreEqual(SimColorConverter.ConvertFromString("#FF3FFF00"), lgcm.ColorMarkers[0].Color);
            Assert.AreEqual(33, lgcm.ColorMarkers[1].Value);
            Assert.AreEqual(SimColorConverter.ConvertFromString("#FFFFFFFF"), lgcm.ColorMarkers[1].Color);
            Assert.AreEqual(100, lgcm.ColorMarkers[2].Value);
            Assert.AreEqual(SimColorConverter.ConvertFromString("#FFFFFFFF"), lgcm.ColorMarkers[2].Color);

            prefilter = association.Prefilter;
            Assert.IsInstanceOfType(prefilter, typeof(SimMinimumPrefilter));

            association = sp.ValueMappings[2];
            Assert.AreEqual("Test3", association.Name);
            prefilter = association.Prefilter;
            Assert.IsInstanceOfType(prefilter, typeof(SimMaximumPrefilter));

            association = sp.ValueMappings[3];
            Assert.AreEqual("Test4", association.Name);
            prefilter = association.Prefilter;
            Assert.IsInstanceOfType(prefilter, typeof(SimAveragePrefilter));

            Assert.AreEqual(sp.ValueMappings[1], sp.ActiveValueMapping);
        }
        [TestMethod]
        public void ParseWithValueMappingV13()
        {
            //Setup
            CreateProjectData();
            projectData.SetCallingLocation(new DummyReferenceLocation(new Guid("a5ef2c5d-9519-4335-9ac2-4beb0a597326")));

            //Table
            var table = MultiValueDxfTests.CreateBigTable();
            table.Id = new SimId(projectData.SitePlannerManager.CalledFromLocation.GlobalID, 2151483658);

            //Mapping
            projectData.ValueMappings.StartLoading();
            var valueMap1 = new SimValueMapping("my mapping 1", table, new SimDefaultPrefilter(), new SimLinearGradientColorMap())
            {
                Id = new SimId(projectData.SitePlannerManager.CalledFromLocation.GlobalID, 665577)
            };
            projectData.ValueMappings.Add(valueMap1);
            var valueMap2 = new SimValueMapping("my mapping 2", table, new SimMinimumPrefilter(), new SimLinearGradientColorMap())
            {
                Id = new SimId(projectData.SitePlannerManager.CalledFromLocation.GlobalID, 665578)
            };
            projectData.ValueMappings.Add(valueMap2);
            projectData.ValueMappings.EndLoading();

            Assert.AreEqual(startSiteplannerCount, projectData.SitePlannerManager.SitePlannerProjects.Count);

            //Test
            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_SPDXF_AssociationsV13)))
            {
                SiteplannerDxfIO.Read(reader, new DXFParserInfo(project.GlobalID, projectData) { CurrentFile = new FileInfo(spRes.CurrentFullPath) });
            }

            //Checks
            Assert.AreEqual(startSiteplannerCount + 1, projectData.SitePlannerManager.SitePlannerProjects.Count);
            var sp = projectData.SitePlannerManager.SitePlannerProjects[startSiteplannerCount];

            Assert.AreEqual(2, sp.ValueMappings.Count);
            Assert.AreEqual(valueMap1, sp.ValueMappings[0]);
            Assert.AreEqual(valueMap2, sp.ValueMappings[1]);
            Assert.AreEqual(valueMap2, sp.ActiveValueMapping);
        }
        [TestMethod]
        public void WriteWithValueMapping()
        {
            ExtendedProjectData data = new ExtendedProjectData();
            data.SetCallingLocation(new DummyReferenceLocation(new Guid("a5ef2c5d-9519-4335-9ac2-4beb0a597326")));

            var siteplannerProject = new SitePlannerProject(null);
            data.SitePlannerManager.SitePlannerProjects.Add(siteplannerProject);

            //Table
            var table = MultiValueDxfTests.CreateBigTable();
            table.Id = new SimId(data.SitePlannerManager.CalledFromLocation.GlobalID, 2151483658);

            //Mapping
            data.ValueMappings.StartLoading();
            var valueMap1 = new SimValueMapping("my mapping 1", table, new SimDefaultPrefilter(), new SimLinearGradientColorMap())
            {
                Id = new SimId(data.SitePlannerManager.CalledFromLocation.GlobalID, 665577)
            };
            data.ValueMappings.Add(valueMap1);
            var valueMap2 = new SimValueMapping("my mapping 2", table, new SimMinimumPrefilter(), new SimLinearGradientColorMap())
            {
                Id = new SimId(data.SitePlannerManager.CalledFromLocation.GlobalID, 665578)
            };
            data.ValueMappings.Add(valueMap2);
            data.ValueMappings.EndLoading();

            siteplannerProject.ValueMappings.Add(valueMap1);
            siteplannerProject.ValueMappings.Add(valueMap2);
            siteplannerProject.ActiveValueMapping = valueMap2;


            string exportedString = null;
            using (MemoryStream stream = new MemoryStream())
            {
                using (DXFStreamWriter writer = new DXFStreamWriter(stream, true))
                {
                    SiteplannerDxfIO.Write(writer, siteplannerProject, data);
                }

                stream.Flush();
                stream.Position = 0;

                var array = stream.ToArray();
                exportedString = Encoding.UTF8.GetString(array);
            }

            AssertUtil.AreEqualMultiline(Properties.Resources.DXFSerializer_SPDXF_WriteAssociations, exportedString);
        }
    }
}
