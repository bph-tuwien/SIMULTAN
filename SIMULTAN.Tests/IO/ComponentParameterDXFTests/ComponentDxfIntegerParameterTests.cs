using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data;
using SIMULTAN.Data.Components;
using SIMULTAN.Data.Geometry;
using SIMULTAN.Data.MultiValues;
using SIMULTAN.Data.Taxonomy;
using SIMULTAN.Projects;
using SIMULTAN.Projects.ManagedFiles;
using SIMULTAN.Serializer.CODXF;
using SIMULTAN.Serializer.DXF;
using SIMULTAN.Tests.Properties;
using SIMULTAN.Tests.TestUtils;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace SIMULTAN.Tests.IO
{
    [TestClass]
    public class ComponentDxfIntegerParameterTests
    {
        #region Parameter

        [TestMethod]
        public void WriteParameter()
        {
            SimIntegerParameter parameter = new SimIntegerParameter(99, "Parameter X", "Unit",
                SimCategory.Cooling | SimCategory.Communication | SimCategory.Light_Artificial,
                SimInfoFlow.Output,
                45, 12, int.MaxValue, "text value with spaces", null,
                SimParameterOperations.EditValue | SimParameterOperations.EditName, SimParameterInstancePropagation.PropagateAlways, true);

            string exportedString = null;
            using (MemoryStream stream = new MemoryStream())
            {
                using (DXFStreamWriter writer = new DXFStreamWriter(stream, true))
                {
                    ComponentDxfIOComponents.WriteParameter(parameter, writer);
                }

                stream.Flush();
                stream.Position = 0;

                var array = stream.ToArray();
                exportedString = Encoding.UTF8.GetString(array);
            }

            AssertUtil.AreEqualMultiline(Properties.Resources.DXFSerializer_WriteParameter_Int, exportedString);
        }

        [TestMethod]
        public void WriteParameterWithPointer()
        {
            ExtendedProjectData projectData = new ExtendedProjectData();
            ManagedFileCollection managedFiles = new ManagedFileCollection(new List<FileInfo> { }, projectData);
            HierarchicalProject project = new CompactProject(Guid.Parse("3db2ce29-f752-4bb9-b7dd-81aca75a7e90"), new FileInfo(@"C:\asdf.simultan"),
                projectData, managedFiles, new FileInfo[] { }, new DirectoryInfo[] { },
                new FileInfo[] { }, new DirectoryInfo(@"C:\unpack"));

            var table = MultiValueDxfTests.CreateBigTable();
            table.Id = new SimId(project, 2255);

            projectData.ValueManager.StartLoading();
            projectData.ValueManager.Add(table);
            projectData.ValueManager.EndLoading();

            SimIntegerParameter parameter = new SimIntegerParameter(99, "Parameter X", "Unit",
                SimCategory.Cooling | SimCategory.Communication | SimCategory.Light_Artificial,
                SimInfoFlow.Output,
                45, 12, int.MaxValue, "text value with spaces", new SimMultiValueBigTableParameterSource(table, 0, 1),
                SimParameterOperations.EditValue | SimParameterOperations.EditName, SimParameterInstancePropagation.PropagateAlways, true);
            parameter.Id = new SimId(project, 99);

            string exportedString = null;
            using (MemoryStream stream = new MemoryStream())
            {
                using (DXFStreamWriter writer = new DXFStreamWriter(stream, true))
                {
                    ComponentDxfIOComponents.WriteParameter(parameter, writer);
                }

                stream.Flush();
                stream.Position = 0;

                var array = stream.ToArray();
                exportedString = Encoding.UTF8.GetString(array);
            }

            AssertUtil.AreEqualMultiline(Properties.Resources.DXFSerializer_WriteParameterWithPointer_Int, exportedString);
        }

        [TestMethod]
        public void WriteParameterWithGeometrySource()
        {
            ExtendedProjectData projectData = new ExtendedProjectData();
            ManagedFileCollection managedFiles = new ManagedFileCollection(new List<FileInfo> { }, projectData);
            HierarchicalProject project = new CompactProject(Guid.Parse("3db2ce29-f752-4bb9-b7dd-81aca75a7e90"), new FileInfo(@"C:\asdf.simultan"),
                projectData, managedFiles, new FileInfo[] { }, new DirectoryInfo[] { },
                new FileInfo[] { }, new DirectoryInfo(@"C:\unpack"));

            TaxonomyUtils.LoadDefaultTaxonomies(projectData);

            var taxentry = projectData.Taxonomies.GetDefaultSlot(SimDefaultSlotKeys.Undefined);
            var valueSource = new SimGeometryParameterSource(SimGeometrySourceProperty.FaceArea);
            valueSource.FilterTags.Add(new SimTaxonomyEntryReference(taxentry));

            SimIntegerParameter parameter = new SimIntegerParameter(99, "Parameter X", "Unit",
                SimCategory.Cooling | SimCategory.Communication | SimCategory.Light_Artificial,
                SimInfoFlow.Output,
                45, 12, int.MaxValue, "text value with spaces", valueSource,
                SimParameterOperations.EditValue | SimParameterOperations.EditName, SimParameterInstancePropagation.PropagateNever, true);
            parameter.Id = new SimId(project, 99);

            string exportedString = null;
            using (MemoryStream stream = new MemoryStream())
            {
                using (DXFStreamWriter writer = new DXFStreamWriter(stream, true))
                {
                    ComponentDxfIOComponents.WriteParameter(parameter, writer);
                }

                stream.Flush();
                stream.Position = 0;

                var array = stream.ToArray();
                exportedString = Encoding.UTF8.GetString(array);
            }

            AssertUtil.AreEqualMultiline(Properties.Resources.DXFSerializer_WriteParameterWithGeometrySource_Int, exportedString);
        }

        [TestMethod]
        public void WriteParameterWithTaxonomyEntry()
        {
            var tax = new SimTaxonomy(new SimId(1200));
            tax.Languages.Add(CultureInfo.InvariantCulture);
            tax.Localization.SetLanguage(new SimTaxonomyLocalizationEntry(CultureInfo.InvariantCulture, "Taxonomy"));
            var taxEntry = new SimTaxonomyEntry(new SimId(1201), "key", "Parameter X");
            tax.Entries.Add(taxEntry);
            SimIntegerParameter parameter = new SimIntegerParameter(99, "Parameter X", "Unit",
                SimCategory.Cooling | SimCategory.Communication | SimCategory.Light_Artificial,
                SimInfoFlow.Output,
                45, 12, int.MaxValue, "text value with spaces", null,
                SimParameterOperations.EditValue | SimParameterOperations.EditName, SimParameterInstancePropagation.PropagateAlways, true);
            parameter.NameTaxonomyEntry = new SimTaxonomyEntryOrString(new SimTaxonomyEntryReference(taxEntry));

            string exportedString = null;
            using (MemoryStream stream = new MemoryStream())
            {
                using (DXFStreamWriter writer = new DXFStreamWriter(stream, true))
                {
                    ComponentDxfIOComponents.WriteParameter(parameter, writer);
                }

                stream.Flush();
                stream.Position = 0;

                var array = stream.ToArray();
                exportedString = Encoding.UTF8.GetString(array);
            }

            AssertUtil.AreEqualMultiline(Properties.Resources.DXFSerializer_WriteParameterWithTaxonomyEntry_Int, exportedString);
        }

        #endregion

        #region MultiValuePointer

        [TestMethod]
        public void WriteBigTablePointer()
        {
            //Setup
            ExtendedProjectData projectData = new ExtendedProjectData();
            ManagedFileCollection managedFiles = new ManagedFileCollection(new List<FileInfo> { }, projectData);
            HierarchicalProject project = new CompactProject(Guid.Parse("3db2ce29-f752-4bb9-b7dd-81aca75a7e90"), new FileInfo(@"C:\asdf.simultan"),
                projectData, managedFiles, new FileInfo[] { }, new DirectoryInfo[] { },
                new FileInfo[] { }, new DirectoryInfo(@"C:\unpack"));

            var table = MultiValueDxfTests.CreateBigTable();
            table.Id = new SimId(project, 2255);

            projectData.ValueManager.StartLoading();
            projectData.ValueManager.Add(table);
            projectData.ValueManager.EndLoading();

            SimIntegerParameter parameter = new SimIntegerParameter(99, "Parameter X", "Unit",
                SimCategory.Cooling | SimCategory.Communication | SimCategory.Light_Artificial,
                SimInfoFlow.Output,
                45, 12, int.MaxValue, "text value with spaces", new SimMultiValueBigTableParameterSource(table, 0, 1),
                SimParameterOperations.EditValue | SimParameterOperations.EditName, SimParameterInstancePropagation.PropagateAlways, true);
            parameter.Id = new SimId(project, 99);

            //Test
            string exportedString = null;
            using (MemoryStream stream = new MemoryStream())
            {
                using (DXFStreamWriter writer = new DXFStreamWriter(stream, true))
                {
                    ComponentDxfIOComponents.WriteValueSource(parameter.ValueSource, writer);
                }

                stream.Flush();
                stream.Position = 0;

                var array = stream.ToArray();
                exportedString = Encoding.UTF8.GetString(array);
            }

            AssertUtil.AreEqualMultiline(Properties.Resources.DXFSerializer_WriteMVPointer_BigTable_Int, exportedString);
        }



        [TestMethod]
        public void WriteBigTablePointerDifferentProject()
        {
            //Setup
            ExtendedProjectData projectData = new ExtendedProjectData();
            ManagedFileCollection managedFiles = new ManagedFileCollection(new List<FileInfo> { }, projectData);
            HierarchicalProject project = new CompactProject(Guid.Parse("3db2ce29-f752-4bb9-b7dd-81aca75a7e90"), new FileInfo(@"C:\asdf.simultan"),
                projectData, managedFiles, new FileInfo[] { }, new DirectoryInfo[] { },
                new FileInfo[] { }, new DirectoryInfo(@"C:\unpack"));

            var table = MultiValueDxfTests.CreateBigTable();
            table.Id = new SimId(Guid.Parse("4db2ce29-f752-4bb9-b7dd-81aca75a7e90"), 2255);

            projectData.ValueManager.StartLoading();
            projectData.ValueManager.Add(table);
            projectData.ValueManager.EndLoading();

            SimIntegerParameter parameter = new SimIntegerParameter(99, "Parameter X", "Unit",
                SimCategory.Cooling | SimCategory.Communication | SimCategory.Light_Artificial,
                SimInfoFlow.Output,
                45, 12, int.MaxValue, "text value with spaces", new SimMultiValueBigTableParameterSource(table, 0, 1),
                SimParameterOperations.EditValue | SimParameterOperations.EditName, SimParameterInstancePropagation.PropagateAlways, true);
            parameter.Id = new SimId(project, 99);

            //Test
            string exportedString = null;
            using (MemoryStream stream = new MemoryStream())
            {
                using (DXFStreamWriter writer = new DXFStreamWriter(stream, true))
                {
                    ComponentDxfIOComponents.WriteValueSource(parameter.ValueSource, writer);
                }

                stream.Flush();
                stream.Position = 0;

                var array = stream.ToArray();
                exportedString = Encoding.UTF8.GetString(array);
            }

            AssertUtil.AreEqualMultiline(Properties.Resources.DXFSerializer_WriteMVPointer_BigTable_Int, exportedString);
        }

        [TestMethod]
        public void WriteField3DPointer()
        {
            //Setup
            ExtendedProjectData projectData = new ExtendedProjectData();
            ManagedFileCollection managedFiles = new ManagedFileCollection(new List<FileInfo> { }, projectData);
            HierarchicalProject project = new CompactProject(Guid.Parse("3db2ce29-f752-4bb9-b7dd-81aca75a7e90"), new FileInfo(@"C:\asdf.simultan"),
                projectData, managedFiles, new FileInfo[] { }, new DirectoryInfo[] { },
                new FileInfo[] { }, new DirectoryInfo(@"C:\unpack"));

            var field = MultiValueDxfTests.CreateField3D();
            field.Id = new SimId(project, 2255);

            projectData.ValueManager.StartLoading();
            projectData.ValueManager.Add(field);
            projectData.ValueManager.EndLoading();

            SimIntegerParameter parameter = new SimIntegerParameter(99, "Parameter X", "Unit",
                SimCategory.Cooling | SimCategory.Communication | SimCategory.Light_Artificial,
                SimInfoFlow.Output,
                45, 12, int.MaxValue, "text value with spaces",
                new SimMultiValueField3DParameterSource(field, 3.0, -1.5, 0.23),
                SimParameterOperations.EditValue | SimParameterOperations.EditName, SimParameterInstancePropagation.PropagateAlways, true);
            parameter.Id = new SimId(project, 99);

            //Test
            string exportedString = null;
            using (MemoryStream stream = new MemoryStream())
            {
                using (DXFStreamWriter writer = new DXFStreamWriter(stream, true))
                {
                    ComponentDxfIOComponents.WriteValueSource(parameter.ValueSource, writer);
                }

                stream.Flush();
                stream.Position = 0;

                var array = stream.ToArray();
                exportedString = Encoding.UTF8.GetString(array);
            }

            AssertUtil.AreEqualMultiline(Properties.Resources.DXFSerializer_WriteMVPointer_Field3D, exportedString);
        }

        [TestMethod]
        public void ParseField3DPointer()
        {
            //Setup
            ExtendedProjectData projectData = new ExtendedProjectData();
            ManagedFileCollection managedFiles = new ManagedFileCollection(new List<FileInfo> { }, projectData);
            HierarchicalProject project = new CompactProject(Guid.Parse("3db2ce29-f752-4bb9-b7dd-81aca75a7e90"), new FileInfo(@"C:\asdf.simultan"),
                projectData, managedFiles, new FileInfo[] { }, new DirectoryInfo[] { },
                new FileInfo[] { }, new DirectoryInfo(@"C:\unpack"));

            var field = MultiValueDxfTests.CreateField3D();
            field.Id = new SimId(project, 2255);

            projectData.ValueManager.StartLoading();
            projectData.ValueManager.Add(field);
            projectData.ValueManager.EndLoading();

            //Test
            SimIntegerParameter parameter = null;

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_ReadMVPointer_Field3DV19_Int)))
            {
                var info = new DXFParserInfo(project.GlobalID, projectData);
                info.FileVersion = 19;

                reader.Read();
                parameter = ComponentDxfIOComponents.BaseParameterEntityElement.Parse(reader, info) as SimIntegerParameter;
            }

            //Check
            var mvp = parameter.ValueSource as SimMultiValueField3DParameterSource;
            Assert.IsNotNull(mvp);
            Assert.AreEqual(field, mvp.ValueField);
            Assert.AreEqual(parameter, mvp.TargetParameter);
            Assert.AreEqual(3.0, mvp.AxisValueX);
            Assert.AreEqual(-1.5, mvp.AxisValueY);
            Assert.AreEqual(0.23, mvp.AxisValueZ);
        }



        [TestMethod]
        public void ParseParameterV19()
        {
            Guid guid = Guid.NewGuid();

            SimIntegerParameter parameter = null;
            ExtendedProjectData projectData = new ExtendedProjectData();
            var location = new DummyReferenceLocation(guid);
            projectData.SetCallingLocation(location);

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_ReadCODXF_IntegerParameterV19)))
            {
                var info = new DXFParserInfo(guid, projectData);
                info.FileVersion = 19;

                reader.Read();
                parameter = ComponentDxfIOComponents.BaseParameterEntityElement.Parse(reader, info) as SimIntegerParameter;
            }

            Assert.IsNotNull(parameter);
            Assert.AreEqual("Parameter X", parameter.NameTaxonomyEntry.Text);
            Assert.AreEqual(45, parameter.Value);
        }

        #endregion
    }
}
