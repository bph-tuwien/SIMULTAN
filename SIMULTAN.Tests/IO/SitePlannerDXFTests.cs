using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data;
using SIMULTAN.Data.Assets;
using SIMULTAN.Data.SitePlanner;
using SIMULTAN.Projects;
using SIMULTAN.Serializer.DXF;
using SIMULTAN.Serializer.SPDXF;
using SIMULTAN.Tests.Properties;
using SIMULTAN.Tests.Util;
using SIMULTAN.Tests.Utils;
using SIMULTAN.Utils;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Media;

namespace SIMULTAN.Tests.IO
{
    [TestClass]
    public class SitePlannerDXFTests : BaseProjectTest
    {
        private static readonly FileInfo testProject = new FileInfo(@".\SiteplannerTest.simultan");
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

            using(DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_SPDXF_EmptyV11)))
            {
                SiteplannerDxfIO.Read(reader, parseInfo);
            }

            Assert.AreEqual(11UL, parseInfo.FileVersion);

            Assert.AreEqual(startSiteplannerCount + 1, projectData.SitePlannerManager.SitePlannerProjects.Count);
            var sp = projectData.SitePlannerManager.SitePlannerProjects[1];
            Assert.IsTrue(projectData.ComponentGeometryExchange.IsConnected(sp));
            Assert.AreEqual(0, sp.Maps.Count);
            Assert.AreEqual(0, sp.Buildings.Count);
            Assert.IsNotNull(sp.ValueMap);
            Assert.AreEqual(0, sp.ValueMap.ParametersAssociations.Count);
        }

        [TestMethod]
        public void ParseEmptyFileV12()
        {
            CreateProjectData();

            Assert.AreEqual(startSiteplannerCount, projectData.SitePlannerManager.SitePlannerProjects.Count);

            using(DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_SPDXF_EmptyV12)))
            {
                SiteplannerDxfIO.Read(reader, new DXFParserInfo(project.GlobalID, projectData) { CurrentFile = new FileInfo(spRes.CurrentFullPath)});
            }

            Assert.AreEqual(startSiteplannerCount + 1, projectData.SitePlannerManager.SitePlannerProjects.Count);
            var sp = projectData.SitePlannerManager.SitePlannerProjects[1];
            Assert.AreEqual(0, sp.Maps.Count);
            Assert.AreEqual(0, sp.Buildings.Count);
            Assert.IsNotNull(sp.ValueMap);
            Assert.AreEqual(0, sp.ValueMap.ParametersAssociations.Count);
        }

        [TestMethod]
        public void WriteEmptyFileV12()
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

            AssertUtil.AreEqualMultiline(Properties.Resources.DXFSerializer_SPDXF_EmptyV12, exportedString);
        }

        [TestMethod]
        public void ParseWithGeoMapsV11()
        {
            CreateProjectData();

            Assert.AreEqual(startSiteplannerCount, projectData.SitePlannerManager.SitePlannerProjects.Count);

            using(DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_SPDXF_GeoMapsV11)))
            {
                SiteplannerDxfIO.Read(reader, new DXFParserInfo(project.GlobalID, projectData) { CurrentFile = new FileInfo(spRes.CurrentFullPath)});
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
            Assert.IsNotNull(sp.ValueMap);
            Assert.AreEqual(0, sp.ValueMap.ParametersAssociations.Count);
        }

        [TestMethod]
        public void ParseWithGeoMapsV12()
        {
            CreateProjectData();

            Assert.AreEqual(startSiteplannerCount, projectData.SitePlannerManager.SitePlannerProjects.Count);

            using(DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_SPDXF_GeoMapsV12)))
            {
                SiteplannerDxfIO.Read(reader, new DXFParserInfo(project.GlobalID, projectData) { CurrentFile = new FileInfo(spRes.CurrentFullPath)});
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
            Assert.IsNotNull(sp.ValueMap);
            Assert.AreEqual(0, sp.ValueMap.ParametersAssociations.Count);
        }

        [TestMethod]
        public void WriteWithGeoMapV12()
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

            AssertUtil.AreEqualMultiline(Properties.Resources.DXFSerializer_SPDXF_GeoMapsV12, exportedString);
        }

        [TestMethod]
        public void ParseWithBuildingsV11()
        {
            CreateProjectData();

            Assert.AreEqual(startSiteplannerCount, projectData.SitePlannerManager.SitePlannerProjects.Count);

            using(DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_SPDXF_BuildingsV11)))
            {
                SiteplannerDxfIO.Read(reader, new DXFParserInfo(project.GlobalID, projectData) { CurrentFile = new FileInfo(spRes.CurrentFullPath)});
            }

            Assert.AreEqual(startSiteplannerCount + 1, projectData.SitePlannerManager.SitePlannerProjects.Count);
            var sp = projectData.SitePlannerManager.SitePlannerProjects[startSiteplannerCount];
            Assert.AreEqual(0, sp.Maps.Count);
            Assert.AreEqual(2, sp.Buildings.Count);

            Assert.AreEqual(2UL, sp.Buildings[0].ID);
            Assert.AreEqual(new Guid("a5ef2c5d-9519-4335-9ac2-4beb0a597326"), sp.Buildings[0].GeometryModelRes.ProjectId);
            Assert.AreEqual(5, sp.Buildings[0].GeometryModelRes.ResourceIndex);
            Assert.AreEqual(Color.FromRgb(0, 128, 255), sp.Buildings[0].CustomColor);

            Assert.AreEqual(3UL, sp.Buildings[1].ID);
            Assert.AreEqual(new Guid("a5ef2c5d-9519-4335-9ac2-4beb0a597327"), sp.Buildings[1].GeometryModelRes.ProjectId);
            Assert.AreEqual(6, sp.Buildings[1].GeometryModelRes.ResourceIndex);
            Assert.AreEqual(Color.FromRgb(128, 0, 32), sp.Buildings[1].CustomColor);

            Assert.IsNotNull(sp.ValueMap);
            Assert.AreEqual(0, sp.ValueMap.ParametersAssociations.Count);
        }

        [TestMethod]
        public void ParseWithBuildingsV12()
        {
            CreateProjectData();

            Assert.AreEqual(startSiteplannerCount, projectData.SitePlannerManager.SitePlannerProjects.Count);

            using(DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_SPDXF_BuildingsV12)))
            {
                SiteplannerDxfIO.Read(reader, new DXFParserInfo(project.GlobalID, projectData) { CurrentFile = new FileInfo(spRes.CurrentFullPath)});
            }

            Assert.AreEqual(startSiteplannerCount + 1, projectData.SitePlannerManager.SitePlannerProjects.Count);
            var sp = projectData.SitePlannerManager.SitePlannerProjects[startSiteplannerCount];
            Assert.AreEqual(0, sp.Maps.Count);
            Assert.AreEqual(2, sp.Buildings.Count);

            Assert.AreEqual(2UL, sp.Buildings[0].ID);
            Assert.AreEqual(new Guid("a5ef2c5d-9519-4335-9ac2-4beb0a597326"), sp.Buildings[0].GeometryModelRes.ProjectId);
            Assert.AreEqual(5, sp.Buildings[0].GeometryModelRes.ResourceIndex);
            Assert.AreEqual(Color.FromRgb(0, 128, 255), sp.Buildings[0].CustomColor);

            Assert.AreEqual(3UL, sp.Buildings[1].ID);
            Assert.AreEqual(new Guid("a5ef2c5d-9519-4335-9ac2-4beb0a597327"), sp.Buildings[1].GeometryModelRes.ProjectId);
            Assert.AreEqual(6, sp.Buildings[1].GeometryModelRes.ResourceIndex);
            Assert.AreEqual(Color.FromRgb(128, 0, 32), sp.Buildings[1].CustomColor);

            Assert.IsNotNull(sp.ValueMap);
            Assert.AreEqual(0, sp.ValueMap.ParametersAssociations.Count);
        }
        [TestMethod]
        public void WriteWithBuildingsV12()
        {
            ExtendedProjectData data = new ExtendedProjectData();
            data.SetCallingLocation(new DummyReferenceLocation(new Guid("a5ef2c5d-9519-4335-9ac2-4beb0a597326")));

            var siteplannerProject = new SitePlannerProject(null);
            data.SitePlannerManager.SitePlannerProjects.Add(siteplannerProject);

            var building = new SitePlannerBuilding(2, new ResourceReference(new Guid("a5ef2c5d-9519-4335-9ac2-4beb0a597326"), 5))
            {
                CustomColor = Color.FromRgb(0, 128, 255),
            };
            siteplannerProject.Buildings.Add(building);
            building = new SitePlannerBuilding(3, new ResourceReference(new Guid("a5ef2c5d-9519-4335-9ac2-4beb0a597327"), 6))
            {
                CustomColor = Color.FromRgb(128, 0, 32),
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

            AssertUtil.AreEqualMultiline(Properties.Resources.DXFSerializer_SPDXF_BuildingsV12, exportedString);
        }

        [TestMethod]
        public void ParseWithAssociationV5()
        {
            CreateProjectData();
            projectData.SetCallingLocation(new DummyReferenceLocation(new Guid("a5ef2c5d-9519-4335-9ac2-4beb0a597326")));

            var table = MultiValueDxfTests.CreateBigTable();
            table.Id = new SimId(projectData.SitePlannerManager.CalledFromLocation.GlobalID, 2151483658);

            projectData.ValueManager.StartLoading();
            projectData.ValueManager.Add(table);
            projectData.ValueManager.EndLoading();

            Assert.AreEqual(startSiteplannerCount, projectData.SitePlannerManager.SitePlannerProjects.Count);

            using(DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_SPDXF_AssociationsV5)))
            {
                SiteplannerDxfIO.Read(reader, new DXFParserInfo(project.GlobalID, projectData) { CurrentFile = new FileInfo(spRes.CurrentFullPath)});
            }

            Assert.AreEqual(startSiteplannerCount + 1, projectData.SitePlannerManager.SitePlannerProjects.Count);
            var sp = projectData.SitePlannerManager.SitePlannerProjects[startSiteplannerCount];
            Assert.AreEqual(0, sp.Maps.Count);
            Assert.AreEqual(0, sp.Buildings.Count);
            Assert.IsNotNull(sp.ValueMap);
            Assert.AreEqual(4, sp.ValueMap.ParametersAssociations.Count);

            var association = sp.ValueMap.ParametersAssociations[0];
            Assert.AreEqual("Test1", association.Name);
            Assert.AreEqual(new Guid("a5ef2c5d-9519-4335-9ac2-4beb0a597326"), association.Parameters.ValueTable.GlobalID);
            Assert.AreEqual(1074741824, association.Parameters.ValueTable.LocalID);
            Assert.AreEqual(Data.SitePlanner.ComponentIndexUsage.Column, association.Parameters.ComponentIndexUsage);

            var colorMap = association.Parameters.ValueToColorMap;
            Assert.IsInstanceOfType(colorMap, typeof(MultiThresholdColorMap));
            var mtcm = (MultiThresholdColorMap)colorMap;
            var cparam = (MarkerColorMapParameters)mtcm.Parameters;
            Assert.AreEqual(0, cparam.Markers[0].Value);
            Assert.AreEqual(ColorConverter.ConvertFromString("#FF3FFF00"), cparam.Markers[0].Color);
            Assert.AreEqual(3.5, cparam.Markers[1].Value);
            Assert.AreEqual(ColorConverter.ConvertFromString("#FFFFFFFF"), cparam.Markers[1].Color);
            Assert.AreEqual(100, cparam.Markers[2].Value);
            Assert.AreEqual(ColorConverter.ConvertFromString("#12345678"), cparam.Markers[2].Color);

            var prefilter = association.Parameters.ValuePreFilter;
            Assert.IsInstanceOfType(prefilter, typeof(TimelinePrefilter));
            var tparam = (TimelinePrefilterParameters)prefilter.Parameters;
            Assert.AreEqual(1, tparam.Current);

            association = sp.ValueMap.ParametersAssociations[1];
            Assert.AreEqual("Test2", association.Name);
            Assert.AreEqual(new Guid("a5ef2c5d-9519-4335-9ac2-4beb0a597326"), association.Parameters.ValueTable.GlobalID);
            Assert.AreEqual(1074741824, association.Parameters.ValueTable.LocalID);
            Assert.AreEqual(Data.SitePlanner.ComponentIndexUsage.Row, association.Parameters.ComponentIndexUsage);

            colorMap = association.Parameters.ValueToColorMap;
            Assert.IsInstanceOfType(colorMap, typeof(MultiLinearGradientColorMap));
            cparam = (MarkerColorMapParameters)colorMap.Parameters;
            Assert.AreEqual(0, cparam.Markers[0].Value);
            Assert.AreEqual(ColorConverter.ConvertFromString("#FF3FFF00"), cparam.Markers[0].Color);
            Assert.AreEqual(33, cparam.Markers[1].Value);
            Assert.AreEqual(ColorConverter.ConvertFromString("#FFFFFFFF"), cparam.Markers[1].Color);
            Assert.AreEqual(100, cparam.Markers[2].Value);
            Assert.AreEqual(ColorConverter.ConvertFromString("#FFFFFFFF"), cparam.Markers[2].Color);

            prefilter = association.Parameters.ValuePreFilter;
            Assert.IsInstanceOfType(prefilter, typeof(MinimumPrefilter));
            Assert.IsInstanceOfType(prefilter.Parameters, typeof(EmptyPrefilterParameters));

            association = sp.ValueMap.ParametersAssociations[2];
            Assert.AreEqual("Test3", association.Name);
            prefilter = association.Parameters.ValuePreFilter;
            Assert.IsInstanceOfType(prefilter, typeof(MaximumPrefilter));
            Assert.IsInstanceOfType(prefilter.Parameters, typeof(EmptyPrefilterParameters));

            association = sp.ValueMap.ParametersAssociations[3];
            Assert.AreEqual("Test4", association.Name);
            prefilter = association.Parameters.ValuePreFilter;
            Assert.IsInstanceOfType(prefilter, typeof(AveragePrefilter));
            Assert.IsInstanceOfType(prefilter.Parameters, typeof(EmptyPrefilterParameters));

            Assert.AreEqual(1, sp.ValueMap.ActiveParametersAssociationIndex);
        }

        [TestMethod]
        public void ParseWithAssociationV11()
        {
            CreateProjectData();
            projectData.SetCallingLocation(new DummyReferenceLocation(new Guid("a5ef2c5d-9519-4335-9ac2-4beb0a597326")));

            var table = MultiValueDxfTests.CreateBigTable();
            table.Id = new SimId(projectData.SitePlannerManager.CalledFromLocation.GlobalID, 2151483658);

            projectData.ValueManager.StartLoading();
            projectData.ValueManager.Add(table);
            projectData.ValueManager.EndLoading();

            Assert.AreEqual(startSiteplannerCount, projectData.SitePlannerManager.SitePlannerProjects.Count);

            using(DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_SPDXF_AssociationsV11)))
            {
                SiteplannerDxfIO.Read(reader, new DXFParserInfo(project.GlobalID, projectData) { CurrentFile = new FileInfo(spRes.CurrentFullPath)});
            }

            Assert.AreEqual(startSiteplannerCount + 1, projectData.SitePlannerManager.SitePlannerProjects.Count);
            var sp = projectData.SitePlannerManager.SitePlannerProjects[startSiteplannerCount];
            Assert.AreEqual(0, sp.Maps.Count);
            Assert.AreEqual(0, sp.Buildings.Count);
            Assert.IsNotNull(sp.ValueMap);
            Assert.AreEqual(4, sp.ValueMap.ParametersAssociations.Count);

            var association = sp.ValueMap.ParametersAssociations[0];
            Assert.AreEqual("Test1", association.Name);
            Assert.AreEqual(new Guid("a5ef2c5d-9519-4335-9ac2-4beb0a597326"), association.Parameters.ValueTable.GlobalID);
            Assert.AreEqual(2151483658, association.Parameters.ValueTable.LocalID);
            Assert.AreEqual(Data.SitePlanner.ComponentIndexUsage.Column, association.Parameters.ComponentIndexUsage);

            var colorMap = association.Parameters.ValueToColorMap;
            Assert.IsInstanceOfType(colorMap, typeof(MultiThresholdColorMap));
            var mtcm = (MultiThresholdColorMap)colorMap;
            var cparam = (MarkerColorMapParameters)mtcm.Parameters;
            Assert.AreEqual(0, cparam.Markers[0].Value);
            Assert.AreEqual(ColorConverter.ConvertFromString("#FF3FFF00"), cparam.Markers[0].Color);
            Assert.AreEqual(3.5, cparam.Markers[1].Value);
            Assert.AreEqual(ColorConverter.ConvertFromString("#FFFFFFFF"), cparam.Markers[1].Color);
            Assert.AreEqual(100, cparam.Markers[2].Value);
            Assert.AreEqual(ColorConverter.ConvertFromString("#12345678"), cparam.Markers[2].Color);

            var prefilter = association.Parameters.ValuePreFilter;
            Assert.IsInstanceOfType(prefilter, typeof(TimelinePrefilter));
            var tparam = (TimelinePrefilterParameters)prefilter.Parameters;
            Assert.AreEqual(1, tparam.Current);

            association = sp.ValueMap.ParametersAssociations[1];
            Assert.AreEqual("Test2", association.Name);
            Assert.AreEqual(new Guid("a5ef2c5d-9519-4335-9ac2-4beb0a597326"), association.Parameters.ValueTable.GlobalID);
            Assert.AreEqual(2151483658, association.Parameters.ValueTable.LocalID);
            Assert.AreEqual(Data.SitePlanner.ComponentIndexUsage.Row, association.Parameters.ComponentIndexUsage);

            colorMap = association.Parameters.ValueToColorMap;
            Assert.IsInstanceOfType(colorMap, typeof(MultiLinearGradientColorMap));
            cparam = (MarkerColorMapParameters)colorMap.Parameters;
            Assert.AreEqual(0, cparam.Markers[0].Value);
            Assert.AreEqual(ColorConverter.ConvertFromString("#FF3FFF00"), cparam.Markers[0].Color);
            Assert.AreEqual(33, cparam.Markers[1].Value);
            Assert.AreEqual(ColorConverter.ConvertFromString("#FFFFFFFF"), cparam.Markers[1].Color);
            Assert.AreEqual(100, cparam.Markers[2].Value);
            Assert.AreEqual(ColorConverter.ConvertFromString("#FFFFFFFF"), cparam.Markers[2].Color);

            prefilter = association.Parameters.ValuePreFilter;
            Assert.IsInstanceOfType(prefilter, typeof(MinimumPrefilter));
            Assert.IsInstanceOfType(prefilter.Parameters, typeof(EmptyPrefilterParameters));

            association = sp.ValueMap.ParametersAssociations[2];
            Assert.AreEqual("Test3", association.Name);
            prefilter = association.Parameters.ValuePreFilter;
            Assert.IsInstanceOfType(prefilter, typeof(MaximumPrefilter));
            Assert.IsInstanceOfType(prefilter.Parameters, typeof(EmptyPrefilterParameters));

            association = sp.ValueMap.ParametersAssociations[3];
            Assert.AreEqual("Test4", association.Name);
            prefilter = association.Parameters.ValuePreFilter;
            Assert.IsInstanceOfType(prefilter, typeof(AveragePrefilter));
            Assert.IsInstanceOfType(prefilter.Parameters, typeof(EmptyPrefilterParameters));

            Assert.AreEqual(1, sp.ValueMap.ActiveParametersAssociationIndex);
        }

        [TestMethod]
        public void ParseWithAssociationV12()
        {
            CreateProjectData();
            projectData.SetCallingLocation(new DummyReferenceLocation(new Guid("a5ef2c5d-9519-4335-9ac2-4beb0a597326")));

            var table = MultiValueDxfTests.CreateBigTable();
            table.Id = new SimId(projectData.SitePlannerManager.CalledFromLocation.GlobalID, 2151483658);

            projectData.ValueManager.StartLoading();
            projectData.ValueManager.Add(table);
            projectData.ValueManager.EndLoading();

            Assert.AreEqual(startSiteplannerCount, projectData.SitePlannerManager.SitePlannerProjects.Count);

            using(DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_SPDXF_AssociationsParseV12)))
            {
                SiteplannerDxfIO.Read(reader, new DXFParserInfo(project.GlobalID, projectData) { CurrentFile = new FileInfo(spRes.CurrentFullPath)});
            }

            Assert.AreEqual(startSiteplannerCount + 1, projectData.SitePlannerManager.SitePlannerProjects.Count);
            var sp = projectData.SitePlannerManager.SitePlannerProjects[startSiteplannerCount];
            Assert.AreEqual(0, sp.Maps.Count);
            Assert.AreEqual(0, sp.Buildings.Count);
            Assert.IsNotNull(sp.ValueMap);
            Assert.AreEqual(4, sp.ValueMap.ParametersAssociations.Count);

            var association = sp.ValueMap.ParametersAssociations[0];
            Assert.AreEqual("Test1", association.Name);
            Assert.AreEqual(new Guid("a5ef2c5d-9519-4335-9ac2-4beb0a597326"), association.Parameters.ValueTable.GlobalID);
            Assert.AreEqual(2151483658, association.Parameters.ValueTable.LocalID);
            Assert.AreEqual(Data.SitePlanner.ComponentIndexUsage.Column, association.Parameters.ComponentIndexUsage);

            var colorMap = association.Parameters.ValueToColorMap;
            Assert.IsInstanceOfType(colorMap, typeof(MultiThresholdColorMap));
            var mtcm = (MultiThresholdColorMap)colorMap;
            var cparam = (MarkerColorMapParameters)mtcm.Parameters;
            Assert.AreEqual(0, cparam.Markers[0].Value);
            Assert.AreEqual(ColorConverter.ConvertFromString("#FF3FFF00"), cparam.Markers[0].Color);
            Assert.AreEqual(3.5, cparam.Markers[1].Value);
            Assert.AreEqual(ColorConverter.ConvertFromString("#FFFFFFFF"), cparam.Markers[1].Color);
            Assert.AreEqual(100, cparam.Markers[2].Value);
            Assert.AreEqual(ColorConverter.ConvertFromString("#12345678"), cparam.Markers[2].Color);

            var prefilter = association.Parameters.ValuePreFilter;
            Assert.IsInstanceOfType(prefilter, typeof(TimelinePrefilter));
            var tparam = (TimelinePrefilterParameters)prefilter.Parameters;
            Assert.AreEqual(1, tparam.Current);

            association = sp.ValueMap.ParametersAssociations[1];
            Assert.AreEqual("Test2", association.Name);
            Assert.AreEqual(new Guid("a5ef2c5d-9519-4335-9ac2-4beb0a597326"), association.Parameters.ValueTable.GlobalID);
            Assert.AreEqual(2151483658, association.Parameters.ValueTable.LocalID);
            Assert.AreEqual(Data.SitePlanner.ComponentIndexUsage.Row, association.Parameters.ComponentIndexUsage);

            colorMap = association.Parameters.ValueToColorMap;
            Assert.IsInstanceOfType(colorMap, typeof(MultiLinearGradientColorMap));
            cparam = (MarkerColorMapParameters)colorMap.Parameters;
            Assert.AreEqual(0, cparam.Markers[0].Value);
            Assert.AreEqual(ColorConverter.ConvertFromString("#FF3FFF00"), cparam.Markers[0].Color);
            Assert.AreEqual(33, cparam.Markers[1].Value);
            Assert.AreEqual(ColorConverter.ConvertFromString("#FFFFFFFF"), cparam.Markers[1].Color);
            Assert.AreEqual(100, cparam.Markers[2].Value);
            Assert.AreEqual(ColorConverter.ConvertFromString("#FFFFFFFF"), cparam.Markers[2].Color);

            prefilter = association.Parameters.ValuePreFilter;
            Assert.IsInstanceOfType(prefilter, typeof(MinimumPrefilter));
            Assert.IsInstanceOfType(prefilter.Parameters, typeof(EmptyPrefilterParameters));

            association = sp.ValueMap.ParametersAssociations[2];
            Assert.AreEqual("Test3", association.Name);
            prefilter = association.Parameters.ValuePreFilter;
            Assert.IsInstanceOfType(prefilter, typeof(MaximumPrefilter));
            Assert.IsInstanceOfType(prefilter.Parameters, typeof(EmptyPrefilterParameters));

            association = sp.ValueMap.ParametersAssociations[3];
            Assert.AreEqual("Test4", association.Name);
            prefilter = association.Parameters.ValuePreFilter;
            Assert.IsInstanceOfType(prefilter, typeof(AveragePrefilter));
            Assert.IsInstanceOfType(prefilter.Parameters, typeof(EmptyPrefilterParameters));

            Assert.AreEqual(1, sp.ValueMap.ActiveParametersAssociationIndex);
        }

        [TestMethod]
        public void WriteWithAssociationsV12()
        {
            ExtendedProjectData data = new ExtendedProjectData();
            data.SetCallingLocation(new DummyReferenceLocation(new Guid("a5ef2c5d-9519-4335-9ac2-4beb0a597326")));

            var siteplannerProject = new SitePlannerProject(null);
            data.SitePlannerManager.SitePlannerProjects.Add(siteplannerProject);

            var valueMap = new ValueMap();

            var table = MultiValueDxfTests.CreateBigTable();
            table.Id = new SimId(data.SitePlannerManager.CalledFromLocation.GlobalID, 2151483658);
            var vmp = new ValueMappingParameters(table);
            vmp.ComponentIndexUsage = ComponentIndexUsage.Column;

            var colorMap = vmp.RegisteredColorMaps.FirstOrDefault(x => x.GetType() == typeof(MultiThresholdColorMap));
            var cparam = (MarkerColorMapParameters)colorMap.Parameters;
            cparam.Markers.Clear();
            cparam.Markers.AddRange(new ColorMapMarker[]
            {
                new ColorMapMarker(0  , (Color)ColorConverter.ConvertFromString("#FF3FFF00")),
                new ColorMapMarker(3.5, (Color)ColorConverter.ConvertFromString("#FFFFFFFF")),
                new ColorMapMarker(100, (Color)ColorConverter.ConvertFromString("#12345678")),
            });
            vmp.ValueToColorMap = colorMap;

            var prefilter = vmp.RegisteredValuePrefilters.FirstOrDefault(x => x.GetType() == typeof(TimelinePrefilter));
            var fparam = (TimelinePrefilterParameters)prefilter.Parameters;
            fparam.Current = 1;
            vmp.ValuePreFilter = prefilter;

            var association = new ValueMappingAssociation("Test1", vmp);
            valueMap.ParametersAssociations.Add(association);

            table = MultiValueDxfTests.CreateBigTable();
            table.Id = new SimId(new Guid("a5ef2c5d-9519-4335-9ac2-4beb0a597327"), 2151483659);
            vmp = new ValueMappingParameters(table);
            vmp.ComponentIndexUsage = ComponentIndexUsage.Row;

            colorMap = vmp.RegisteredColorMaps.FirstOrDefault(x => x.GetType() == typeof(MultiLinearGradientColorMap)) as MultiLinearGradientColorMap;
            cparam = (MarkerColorMapParameters)colorMap.Parameters;
            cparam.Markers.Clear();
            cparam.Markers.AddRange(new ColorMapMarker[]
            {
                new ColorMapMarker(0  , (Color)ColorConverter.ConvertFromString("#FF3FFF00")),
                new ColorMapMarker(33, (Color)ColorConverter.ConvertFromString("#FFFFFFFF")),
                new ColorMapMarker(100, (Color)ColorConverter.ConvertFromString("#FFFFFFFF")),
            });
            vmp.ValueToColorMap = colorMap;

            prefilter = vmp.RegisteredValuePrefilters.FirstOrDefault(x => x.GetType() == typeof(MinimumPrefilter));
            vmp.ValuePreFilter = prefilter;

            association = new ValueMappingAssociation("Test2", vmp);
            valueMap.ParametersAssociations.Add(association);

            table.Id = new SimId(new Guid("a5ef2c5d-9519-4335-9ac2-4beb0a597327"), 2151483659);
            vmp = new ValueMappingParameters(table);
            vmp.ComponentIndexUsage = ComponentIndexUsage.Row;

            colorMap = vmp.RegisteredColorMaps.FirstOrDefault(x => x.GetType() == typeof(MultiLinearGradientColorMap)) as MultiLinearGradientColorMap;
            cparam = (MarkerColorMapParameters)colorMap.Parameters;
            cparam.Markers.Clear();
            cparam.Markers.AddRange(new ColorMapMarker[]
            {
                new ColorMapMarker(0  , (Color)ColorConverter.ConvertFromString("#FF3FFF00")),
                new ColorMapMarker(33, (Color)ColorConverter.ConvertFromString("#FFFFFFFF")),
                new ColorMapMarker(100, (Color)ColorConverter.ConvertFromString("#FFFFFFFF")),
            });
            vmp.ValueToColorMap = colorMap;

            prefilter = vmp.RegisteredValuePrefilters.FirstOrDefault(x => x.GetType() == typeof(MaximumPrefilter));
            vmp.ValuePreFilter = prefilter;

            association = new ValueMappingAssociation("Test3", vmp);
            valueMap.ParametersAssociations.Add(association);

            table.Id = new SimId(new Guid("a5ef2c5d-9519-4335-9ac2-4beb0a597327"), 2151483659);
            vmp = new ValueMappingParameters(table);
            vmp.ComponentIndexUsage = ComponentIndexUsage.Row;

            colorMap = vmp.RegisteredColorMaps.FirstOrDefault(x => x.GetType() == typeof(MultiLinearGradientColorMap)) as MultiLinearGradientColorMap;
            cparam = (MarkerColorMapParameters)colorMap.Parameters;
            cparam.Markers.Clear();
            cparam.Markers.AddRange(new ColorMapMarker[]
            {
                new ColorMapMarker(0  , (Color)ColorConverter.ConvertFromString("#FF3FFF00")),
                new ColorMapMarker(33, (Color)ColorConverter.ConvertFromString("#FFFFFFFF")),
                new ColorMapMarker(100, (Color)ColorConverter.ConvertFromString("#FFFFFFFF")),
            });
            vmp.ValueToColorMap = colorMap;

            prefilter = vmp.RegisteredValuePrefilters.FirstOrDefault(x => x.GetType() == typeof(AveragePrefilter));
            vmp.ValuePreFilter = prefilter;

            association = new ValueMappingAssociation("Test4", vmp);
            valueMap.ParametersAssociations.Add(association);
            siteplannerProject.ValueMap = valueMap;
            valueMap.ActiveParametersAssociationIndex = 1;

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

            AssertUtil.AreEqualMultiline(Properties.Resources.DXFSerializer_SPDXF_AssociationsV12, exportedString);
        }
    }
}
