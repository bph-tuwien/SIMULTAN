using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data;
using SIMULTAN.Data.SimMath;
using SIMULTAN.Data.SitePlanner;
using SIMULTAN.Data.ValueMappings;
using SIMULTAN.Projects;
using SIMULTAN.Serializer.CODXF;
using SIMULTAN.Serializer.DXF;
using SIMULTAN.Tests.Properties;
using SIMULTAN.Tests.TestUtils;
using SIMULTAN.Tests.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace SIMULTAN.Tests.IO
{
    [TestClass]
    public class ComponentDXFValueMappingTests : ComponentDXFTestsBase
    {
        internal static void CheckTestData(ExtendedProjectData projectData)
        {
            //Check
            Assert.AreEqual(2, projectData.ValueMappings.Count);

            var valueMapping = projectData.ValueMappings[0];
            Assert.IsNotNull(valueMapping);
            Assert.AreEqual(665577, valueMapping.Id.LocalId);
            Assert.AreEqual(projectData.ValueMappings.CalledFromLocation.GlobalID, valueMapping.Id.GlobalId);
            Assert.AreEqual("my mapping 1", valueMapping.Name);
            Assert.AreEqual(SimComponentIndexUsage.Column, valueMapping.ComponentIndexUsage);

            var colorMap = valueMapping.ColorMap as SimLinearGradientColorMap;
            Assert.IsNotNull(colorMap);
            Assert.AreEqual(3, colorMap.ColorMarkers.Count);

            var marker = colorMap.ColorMarkers[0];
            Assert.AreEqual(-1.0, marker.Value);
            Assert.AreEqual(SimColor.FromArgb(255, 255, 0, 0), marker.Color);

            marker = colorMap.ColorMarkers[1];
            Assert.AreEqual(5.0, marker.Value);
            Assert.AreEqual(SimColor.FromArgb(255, 0, 255, 0), marker.Color);

            marker = colorMap.ColorMarkers[2];
            Assert.AreEqual(99.0, marker.Value);
            Assert.AreEqual(SimColor.FromArgb(255, 0, 0, 255), marker.Color);

            var prefilter = valueMapping.Prefilter as SimMinimumPrefilter;
            Assert.IsNotNull(prefilter);

            valueMapping = projectData.ValueMappings[1];
            Assert.IsNotNull(valueMapping);
            Assert.AreEqual(665578, valueMapping.Id.LocalId);
            Assert.AreEqual(projectData.ValueMappings.CalledFromLocation.GlobalID, valueMapping.Id.GlobalId);
            Assert.AreEqual("my mapping 2", valueMapping.Name);
            Assert.AreEqual(SimComponentIndexUsage.Row, valueMapping.ComponentIndexUsage);

            var colorMap2 = valueMapping.ColorMap as SimThresholdColorMap;
            Assert.IsNotNull(colorMap2);

            var prefilter2 = valueMapping.Prefilter as SimAveragePrefilter;
            Assert.IsNotNull(prefilter2);
        }

        #region Section

        [TestMethod]
        public void ReadValueMappingSectionV13()
        {
            //Setup
            var guid = Guid.NewGuid();
            ExtendedProjectData projectData = new ExtendedProjectData();
            projectData.SetCallingLocation(new DummyReferenceLocation(guid));

            var table = MultiValueDxfTests.CreateBigTable();
            table.Id = new SimId(projectData.SitePlannerManager.CalledFromLocation.GlobalID, 2151483658);
            projectData.ValueManager.StartLoading();
            projectData.ValueManager.Add(table);
            projectData.ValueManager.EndLoading();

            //Read
            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_ReadCODXF_ValueMappingSectionV13)))
            {
                var info = new DXFParserInfo(guid, projectData);
                info.FileVersion = 13;

                ComponentDxfIOValueMappings.ReadValueMappingSection(reader, info);
            }

            CheckTestData(projectData);
        }
        [TestMethod]
        public void WriteValueMappingSection()
        {
            ExtendedProjectData data = new ExtendedProjectData();
            var guid = new Guid("a5ef2c5d-9519-4335-9ac2-4beb0a597326");
            data.SetCallingLocation(new DummyReferenceLocation(guid));

            CreateValueMappingTestData(data, guid);

            string exportedString = null;
            using (MemoryStream stream = new MemoryStream())
            {
                using (DXFStreamWriter writer = new DXFStreamWriter(stream, true))
                {
                    ComponentDxfIOValueMappings.WriteValueMappingSection(data.ValueMappings, writer);
                }

                stream.Flush();
                stream.Position = 0;

                var array = stream.ToArray();
                exportedString = Encoding.UTF8.GetString(array);
            }

            AssertUtil.AreEqualMultiline(Properties.Resources.DXFSerializer_CODXF_WriteValueMappingSection, exportedString);
        }

        #endregion

        #region ValueMappings

        [TestMethod]
        public void ReadValueMappingV13()
        {
            //Setup
            var guid = Guid.NewGuid();
            ExtendedProjectData projectData = new ExtendedProjectData();
            projectData.SetCallingLocation(new DummyReferenceLocation(guid));

            var table = MultiValueDxfTests.CreateBigTable();
            table.Id = new SimId(projectData.SitePlannerManager.CalledFromLocation.GlobalID, 2151483658);
            projectData.ValueManager.StartLoading();
            projectData.ValueManager.Add(table);
            projectData.ValueManager.EndLoading();

            //Read
            SimValueMapping valueMapping = null;

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_ReadCODXF_ValueMappingV13)))
            {
                var info = new DXFParserInfo(guid, projectData);
                info.FileVersion = 13;

                reader.Read();

                valueMapping = ComponentDxfIOValueMappings.ValueMappingEntityElement.Parse(reader, info);
            }

            //Check
            Assert.IsNotNull(valueMapping);
            Assert.AreEqual(665577, valueMapping.Id.LocalId);
            Assert.AreEqual(guid, valueMapping.Id.GlobalId);
            Assert.AreEqual("my mapping 1", valueMapping.Name);
            Assert.AreEqual(SimComponentIndexUsage.Column, valueMapping.ComponentIndexUsage);

            var colorMap = valueMapping.ColorMap as SimLinearGradientColorMap;
            Assert.IsNotNull(colorMap);
            Assert.AreEqual(3, colorMap.ColorMarkers.Count);

            var marker = colorMap.ColorMarkers[0];
            Assert.AreEqual(-1.0, marker.Value);
            Assert.AreEqual(SimColor.FromArgb(255, 255, 0, 0), marker.Color);

            marker = colorMap.ColorMarkers[1];
            Assert.AreEqual(5.0, marker.Value);
            Assert.AreEqual(SimColor.FromArgb(255, 0, 255, 0), marker.Color);

            marker = colorMap.ColorMarkers[2];
            Assert.AreEqual(99.0, marker.Value);
            Assert.AreEqual(SimColor.FromArgb(255, 0, 0, 255), marker.Color);

            var prefilter = valueMapping.Prefilter as SimMinimumPrefilter;
            Assert.IsNotNull(prefilter);
        }
        [TestMethod]
        public void WriteValueMapping()
        {
            ExtendedProjectData data = new ExtendedProjectData();
            data.SetCallingLocation(new DummyReferenceLocation(new Guid("a5ef2c5d-9519-4335-9ac2-4beb0a597326")));

            //Table
            var table = MultiValueDxfTests.CreateBigTable();
            table.Id = new SimId(data.SitePlannerManager.CalledFromLocation.GlobalID, 2151483658);

            //Mapping
            data.ValueMappings.StartLoading();
            var valueMap1 = new SimValueMapping("my mapping 1", table, new SimMinimumPrefilter(), new SimLinearGradientColorMap(
                new SimColorMarker[]
                {
                    new SimColorMarker(-1.0, SimColor.FromArgb(255, 255, 0, 0)),
                    new SimColorMarker(5.0, SimColor.FromArgb(255, 0, 255, 0)),
                    new SimColorMarker(99.0, SimColor.FromArgb(255, 0, 0, 255)),
                }))
            {
                Id = new SimId(data.SitePlannerManager.CalledFromLocation.GlobalID, 665577),
                ComponentIndexUsage = SimComponentIndexUsage.Column,
            };
            data.ValueMappings.Add(valueMap1);

            string exportedString = null;
            using (MemoryStream stream = new MemoryStream())
            {
                using (DXFStreamWriter writer = new DXFStreamWriter(stream, true))
                {
                    ComponentDxfIOValueMappings.WriteValueMapping(valueMap1, writer);
                }

                stream.Flush();
                stream.Position = 0;

                var array = stream.ToArray();
                exportedString = Encoding.UTF8.GetString(array);
            }

            AssertUtil.AreEqualMultiline(Properties.Resources.DXFSerializer_CODXF_WriteValueMapping, exportedString);
        }

        #endregion

        #region ColorMaps

        [TestMethod]
        public void ReadColorMapLinearGradientV13()
        {
            var guid = Guid.NewGuid();
            ExtendedProjectData projectData = new ExtendedProjectData();
            projectData.SetCallingLocation(new DummyReferenceLocation(guid));

            SimLinearGradientColorMap colorMap = null;

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_ReadCODXF_ColorMapLinearGradientV13)))
            {
                var info = new DXFParserInfo(guid, projectData);
                info.FileVersion = 13;

                reader.Read();

                colorMap = ComponentDxfIOValueMappings.LinearGradientColorMapEntityElement.Parse(reader, info) as SimLinearGradientColorMap;
            }

            Assert.IsNotNull(colorMap);

            Assert.AreEqual(3, colorMap.ColorMarkers.Count);

            var marker = colorMap.ColorMarkers[0];
            Assert.AreEqual(-1.0, marker.Value);
            Assert.AreEqual(SimColor.FromArgb(255, 255, 0, 0), marker.Color);

            marker = colorMap.ColorMarkers[1];
            Assert.AreEqual(5.0, marker.Value);
            Assert.AreEqual(SimColor.FromArgb(255, 0, 255, 0), marker.Color);

            marker = colorMap.ColorMarkers[2];
            Assert.AreEqual(99.0, marker.Value);
            Assert.AreEqual(SimColor.FromArgb(255, 0, 0, 255), marker.Color);
        }
        [TestMethod]
        public void WriteColorMapLinearGradient()
        {
            var colorMap = new SimLinearGradientColorMap(new SimColorMarker[]
            {
                new SimColorMarker(-1.0, SimColor.FromArgb(255, 255, 0, 0)),
                new SimColorMarker(5.0, SimColor.FromArgb(255, 0, 255, 0)),
                new SimColorMarker(99.0, SimColor.FromArgb(255, 0, 0, 255)),
            });

            string exportedString = null;
            using (MemoryStream stream = new MemoryStream())
            {
                using (DXFStreamWriter writer = new DXFStreamWriter(stream, true))
                {
                    ComponentDxfIOValueMappings.WriteLinearGradientColorMap(colorMap, writer);
                }

                stream.Flush();
                stream.Position = 0;

                var array = stream.ToArray();
                exportedString = Encoding.UTF8.GetString(array);
            }

            AssertUtil.AreEqualMultiline(Properties.Resources.DXFSerializer_CODXF_WriteColorMapLinearGradient, exportedString);
        }


        [TestMethod]
        public void ReadColorMapThresholdV13()
        {
            var guid = Guid.NewGuid();
            ExtendedProjectData projectData = new ExtendedProjectData();
            projectData.SetCallingLocation(new DummyReferenceLocation(guid));

            SimThresholdColorMap colorMap = null;

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_ReadCODXF_ColorMapThresholdV13)))
            {
                var info = new DXFParserInfo(guid, projectData);
                info.FileVersion = 13;

                reader.Read();

                colorMap = ComponentDxfIOValueMappings.ThresholdColorMapEntityElement.Parse(reader, info) as SimThresholdColorMap;
            }

            Assert.IsNotNull(colorMap);

            Assert.AreEqual(3, colorMap.ColorMarkers.Count);

            var marker = colorMap.ColorMarkers[0];
            Assert.AreEqual(-1.0, marker.Value);
            Assert.AreEqual(SimColor.FromArgb(255, 255, 0, 0), marker.Color);

            marker = colorMap.ColorMarkers[1];
            Assert.AreEqual(5.0, marker.Value);
            Assert.AreEqual(SimColor.FromArgb(255, 0, 255, 0), marker.Color);

            marker = colorMap.ColorMarkers[2];
            Assert.AreEqual(99.0, marker.Value);
            Assert.AreEqual(SimColor.FromArgb(255, 0, 0, 255), marker.Color);
        }
        [TestMethod]
        public void WriteColorMapThreshold()
        {
            var colorMap = new SimThresholdColorMap(new SimColorMarker[]
            {
                new SimColorMarker(-1.0, SimColor.FromArgb(255, 255, 0, 0)),
                new SimColorMarker(5.0, SimColor.FromArgb(255, 0, 255, 0)),
                new SimColorMarker(99.0, SimColor.FromArgb(255, 0, 0, 255)),
            });

            string exportedString = null;
            using (MemoryStream stream = new MemoryStream())
            {
                using (DXFStreamWriter writer = new DXFStreamWriter(stream, true))
                {
                    ComponentDxfIOValueMappings.WriteThresholdColorMap(colorMap, writer);
                }

                stream.Flush();
                stream.Position = 0;

                var array = stream.ToArray();
                exportedString = Encoding.UTF8.GetString(array);
            }

            AssertUtil.AreEqualMultiline(Properties.Resources.DXFSerializer_CODXF_WriteColorMapThreshold, exportedString);
        }

        #endregion

        #region Prefilter

        [TestMethod]
        public void ReadPrefilterDefaultV13()
        {
            var guid = Guid.NewGuid();
            ExtendedProjectData projectData = new ExtendedProjectData();
            projectData.SetCallingLocation(new DummyReferenceLocation(guid));

            SimDefaultPrefilter prefilter = null;

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_CODXF_WritePrefilterDefault)))
            {
                var info = new DXFParserInfo(guid, projectData);
                info.FileVersion = 13;

                reader.Read();

                prefilter = ComponentDxfIOValueMappings.DefaultPrefilterEntityElement.Parse(reader, info) as SimDefaultPrefilter;
            }

            Assert.IsNotNull(prefilter);
        }
        [TestMethod]
        public void ReadPrefilterMinimumV13()
        {
            var guid = Guid.NewGuid();
            ExtendedProjectData projectData = new ExtendedProjectData();
            projectData.SetCallingLocation(new DummyReferenceLocation(guid));

            SimMinimumPrefilter prefilter = null;

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_CODXF_WritePrefilterMinimum)))
            {
                var info = new DXFParserInfo(guid, projectData);
                info.FileVersion = 13;

                reader.Read();

                prefilter = ComponentDxfIOValueMappings.MinimumPrefilterEntityElement.Parse(reader, info) as SimMinimumPrefilter;
            }

            Assert.IsNotNull(prefilter);
        }
        [TestMethod]
        public void ReadPrefilterMaximumV13()
        {
            var guid = Guid.NewGuid();
            ExtendedProjectData projectData = new ExtendedProjectData();
            projectData.SetCallingLocation(new DummyReferenceLocation(guid));

            SimMaximumPrefilter prefilter = null;

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_CODXF_WritePrefilterMaximum)))
            {
                var info = new DXFParserInfo(guid, projectData);
                info.FileVersion = 13;

                reader.Read();

                prefilter = ComponentDxfIOValueMappings.MaximumPrefilterEntityElement.Parse(reader, info) as SimMaximumPrefilter;
            }

            Assert.IsNotNull(prefilter);
        }
        [TestMethod]
        public void ReadPrefilterAverageV13()
        {
            var guid = Guid.NewGuid();
            ExtendedProjectData projectData = new ExtendedProjectData();
            projectData.SetCallingLocation(new DummyReferenceLocation(guid));

            SimAveragePrefilter prefilter = null;

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_CODXF_WritePrefilterAverage)))
            {
                var info = new DXFParserInfo(guid, projectData);
                info.FileVersion = 13;

                reader.Read();

                prefilter = ComponentDxfIOValueMappings.AveragePrefilterEntityElement.Parse(reader, info) as SimAveragePrefilter;
            }

            Assert.IsNotNull(prefilter);
        }

        [TestMethod]
        public void WritePrefilterDefault()
        {
            var prefilter = new SimDefaultPrefilter();

            string exportedString = null;
            using (MemoryStream stream = new MemoryStream())
            {
                using (DXFStreamWriter writer = new DXFStreamWriter(stream, true))
                {
                    ComponentDxfIOValueMappings.WriteSimplePrefilter(prefilter, writer);
                }

                stream.Flush();
                stream.Position = 0;

                var array = stream.ToArray();
                exportedString = Encoding.UTF8.GetString(array);
            }

            AssertUtil.AreEqualMultiline(Properties.Resources.DXFSerializer_CODXF_WritePrefilterDefault, exportedString);
        }
        [TestMethod]
        public void WritePrefilterMinimum()
        {
            var prefilter = new SimMinimumPrefilter();

            string exportedString = null;
            using (MemoryStream stream = new MemoryStream())
            {
                using (DXFStreamWriter writer = new DXFStreamWriter(stream, true))
                {
                    ComponentDxfIOValueMappings.WriteSimplePrefilter(prefilter, writer);
                }

                stream.Flush();
                stream.Position = 0;

                var array = stream.ToArray();
                exportedString = Encoding.UTF8.GetString(array);
            }

            AssertUtil.AreEqualMultiline(Properties.Resources.DXFSerializer_CODXF_WritePrefilterMinimum, exportedString);
        }
        [TestMethod]
        public void WritePrefilterMaximum()
        {
            var prefilter = new SimMaximumPrefilter();

            string exportedString = null;
            using (MemoryStream stream = new MemoryStream())
            {
                using (DXFStreamWriter writer = new DXFStreamWriter(stream, true))
                {
                    ComponentDxfIOValueMappings.WriteSimplePrefilter(prefilter, writer);
                }

                stream.Flush();
                stream.Position = 0;

                var array = stream.ToArray();
                exportedString = Encoding.UTF8.GetString(array);
            }

            AssertUtil.AreEqualMultiline(Properties.Resources.DXFSerializer_CODXF_WritePrefilterMaximum, exportedString);
        }
        [TestMethod]
        public void WritePrefilterAverage()
        {
            var prefilter = new SimAveragePrefilter();

            string exportedString = null;
            using (MemoryStream stream = new MemoryStream())
            {
                using (DXFStreamWriter writer = new DXFStreamWriter(stream, true))
                {
                    ComponentDxfIOValueMappings.WriteSimplePrefilter(prefilter, writer);
                }

                stream.Flush();
                stream.Position = 0;

                var array = stream.ToArray();
                exportedString = Encoding.UTF8.GetString(array);
            }

            AssertUtil.AreEqualMultiline(Properties.Resources.DXFSerializer_CODXF_WritePrefilterAverage, exportedString);
        }

        #endregion
    }
}
