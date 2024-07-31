using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data;
using SIMULTAN.Data.Components;
using SIMULTAN.Projects;
using SIMULTAN.Serializer.CODXF;
using SIMULTAN.Serializer.DXF;
using SIMULTAN.Serializer.PADXF;
using SIMULTAN.Tests.Properties;
using SIMULTAN.Tests.TestUtils;
using System;
using System.IO;
using System.Text;

namespace SIMULTAN.Tests.IO
{
    [TestClass]
    public class ParameterDXFTests
    {
        private void CreateTestData(ExtendedProjectData data)
        {
            SimDoubleParameter parameterX = new SimDoubleParameter(99, "Parameter X", "Unit",
                SimCategory.Cooling | SimCategory.Communication | SimCategory.Light_Artificial,
                SimInfoFlow.Output,
                45.67, -12.3, double.PositiveInfinity, "text value with spaces", null,
                SimParameterOperations.EditValue | SimParameterOperations.EditName, SimParameterInstancePropagation.PropagateAlways, true);

            SimDoubleParameter parameterY = new SimDoubleParameter(100, "Parameter Y", "Unit",
                SimCategory.Cooling,
                SimInfoFlow.Output,
                55.67, -12.3, double.PositiveInfinity, "text value with spaces", null,
                SimParameterOperations.EditValue, SimParameterInstancePropagation.PropagateNever, true);

            data.ParameterLibraryManager.ParameterRecord.Add(parameterX);
            data.ParameterLibraryManager.ParameterRecord.Add(parameterY);
        }

        [TestMethod]
        public void WriteParameterFile()
        {
            Guid guid = Guid.NewGuid();
            Guid otherguid = new Guid("98478ed1-d3f4-4873-95b6-412e5e23aac5");

            ExtendedProjectData projectData = new ExtendedProjectData();
            projectData.SetCallingLocation(new DummyReferenceLocation(guid));

            CreateTestData(projectData);


            string exportedString = null;
            using (MemoryStream stream = new MemoryStream())
            {
                using (DXFStreamWriter writer = new DXFStreamWriter(stream, true))
                {
                    ParameterDxfIO.Write(writer, projectData, projectData.ParameterLibraryManager.ParameterRecord);
                }

                stream.Flush();
                stream.Position = 0;

                var array = stream.ToArray();
                exportedString = Encoding.UTF8.GetString(array);
            }

            AssertUtil.AreEqualMultiline(Resources.DXFSerializer_PADXF_Write, exportedString);
        }

        [TestMethod]
        public void ReadParameterFileV12()
        {
            Guid guid = Guid.NewGuid();
            var otherGuid = Guid.Parse("da7d8f7c-8eec-423b-b127-9d6e17f52522");

            ExtendedProjectData projectData = new ExtendedProjectData();
            var location = new DummyReferenceLocation(guid);
            projectData.SetCallingLocation(location);

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_PADXF_ReadV12)))
            {
                var info = new DXFParserInfo(guid, projectData);
                info.FileVersion = 12;
                ParameterDxfIO.Read(reader, info);
            }

            Assert.AreEqual(2, projectData.ParameterLibraryManager.ParameterRecord.Count);
            Assert.AreEqual("Parameter X", projectData.ParameterLibraryManager.ParameterRecord[0].NameTaxonomyEntry.Text);
            Assert.AreEqual("Parameter Y", projectData.ParameterLibraryManager.ParameterRecord[1].NameTaxonomyEntry.Text);
        }

        [TestMethod]
        public void ReadParameterFileV11()
        {
            Guid guid = Guid.NewGuid();
            var otherGuid = Guid.Parse("da7d8f7c-8eec-423b-b127-9d6e17f52522");

            ExtendedProjectData projectData = new ExtendedProjectData();
            var location = new DummyReferenceLocation(guid);
            projectData.SetCallingLocation(location);

            typeof(ComponentDxfIO).GetProperty("LastParsedFileVersion",
                System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic).SetValue(null, (ulong)11);

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_PADXF_ReadV11)))
            {
                var info = new DXFParserInfo(guid, projectData);
                info.FileVersion = 11;
                ParameterDxfIO.Read(reader, info);
            }

            Assert.AreEqual(2, projectData.ParameterLibraryManager.ParameterRecord.Count);
            Assert.AreEqual("Parameter X", projectData.ParameterLibraryManager.ParameterRecord[0].NameTaxonomyEntry.Text);
            Assert.AreEqual("Parameter Y", projectData.ParameterLibraryManager.ParameterRecord[1].NameTaxonomyEntry.Text);
        }


        [TestMethod]
        public void ReadParameterFileV19()
        {
            Guid guid = Guid.NewGuid();
            var otherGuid = Guid.Parse("da7d8f7c-8eec-423b-b127-9d6e17f52522");

            ExtendedProjectData projectData = new ExtendedProjectData();
            var location = new DummyReferenceLocation(guid);
            projectData.SetCallingLocation(location);

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_PADXF_ReadV19)))
            {
                var info = new DXFParserInfo(guid, projectData);
                info.FileVersion = 19;
                ParameterDxfIO.Read(reader, info);
            }

            Assert.AreEqual(2, projectData.ParameterLibraryManager.ParameterRecord.Count);
            Assert.AreEqual("Parameter X", projectData.ParameterLibraryManager.ParameterRecord[0].NameTaxonomyEntry.Text);
            Assert.AreEqual("Parameter Y", projectData.ParameterLibraryManager.ParameterRecord[1].NameTaxonomyEntry.Text);
        }
    }
}
