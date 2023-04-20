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
using SIMULTAN.Tests.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SIMULTAN.Tests.IO
{
    [TestClass]
    public class ComponentDxfStringParameterTests
    {
        #region Parameter

        [TestMethod]
        public void WriteParameter()
        {
            SimStringParameter parameter = new SimStringParameter(99, "Parameter X",
                SimCategory.Cooling | SimCategory.Communication | SimCategory.Light_Artificial,
                SimInfoFlow.Output,
                "test string", "text value with spaces", null,
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

            AssertUtil.AreEqualMultiline(Resources.DXFSerializer_WriteParameter_String, exportedString);
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

            SimStringParameter parameter = new SimStringParameter(99, "Parameter X",
                SimCategory.Cooling | SimCategory.Communication | SimCategory.Light_Artificial,
                SimInfoFlow.Output,
                "test string", "text value with spaces", new SimMultiValueBigTableParameterSource(table, 0, 4),
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

            AssertUtil.AreEqualMultiline(Properties.Resources.DXFSerializer_WriteParameterWithPointer_String, exportedString);
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

            SimStringParameter parameter = new SimStringParameter(99, "Parameter X",
                SimCategory.Cooling | SimCategory.Communication | SimCategory.Light_Artificial,
                SimInfoFlow.Output,
                "test string", "text value with spaces", valueSource,
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

            AssertUtil.AreEqualMultiline(Properties.Resources.DXFSerializer_WriteParameterWithGeometrySource_String, exportedString);
        }

        [TestMethod]
        public void WriteParameterWithTaxonomyEntry()
        {
            var tax = new SimTaxonomy(new SimId(1200)) { Name = "Taxonomy" };
            var taxEntry = new SimTaxonomyEntry(new SimId(1201)) { Name = "Parameter X", Key = "key" };
            tax.Entries.Add(taxEntry);
            SimStringParameter parameter = new SimStringParameter(99, "Parameter X",
                SimCategory.Cooling | SimCategory.Communication | SimCategory.Light_Artificial,
                SimInfoFlow.Output,
                "test string", "text value with spaces", null,
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

            AssertUtil.AreEqualMultiline(Properties.Resources.DXFSerializer_WriteParameterWithTaxonomyEntry_String, exportedString);
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

            SimStringParameter parameter = new SimStringParameter(99, "Parameter X",
                SimCategory.Cooling | SimCategory.Communication | SimCategory.Light_Artificial,
                SimInfoFlow.Output,
                "test string", "text value with spaces", new SimMultiValueBigTableParameterSource(table, 2, 1),
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

            AssertUtil.AreEqualMultiline(Properties.Resources.DXFSerializer_WriteMVPointer_BigTable, exportedString);
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

            SimStringParameter parameter = new SimStringParameter(99, "Parameter X",
                SimCategory.Cooling | SimCategory.Communication | SimCategory.Light_Artificial,
                SimInfoFlow.Output,
                "test string", "text value with spaces", new SimMultiValueBigTableParameterSource(table, 2, 1),
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

            AssertUtil.AreEqualMultiline(Properties.Resources.DXFSerializer_WriteMVPointer_BigTable, exportedString);
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

            SimStringParameter parameter = new SimStringParameter(99, "Parameter X",
                SimCategory.Cooling | SimCategory.Communication | SimCategory.Light_Artificial,
                SimInfoFlow.Output,
                "test string", "text value with spaces",
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
            SimStringParameter parameter = null;

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_ReadMVPointer_Field3DV19_String)))
            {
                var info = new DXFParserInfo(project.GlobalID, projectData);
                info.FileVersion = 19;

                reader.Read();

                parameter = ComponentDxfIOComponents.BaseParameterEntityElement.Parse(reader, info) as SimStringParameter;
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
        public void WriteFunctionPointer()
        {
            //Setup
            ExtendedProjectData projectData = new ExtendedProjectData();
            ManagedFileCollection managedFiles = new ManagedFileCollection(new List<FileInfo> { }, projectData);
            HierarchicalProject project = new CompactProject(Guid.Parse("3db2ce29-f752-4bb9-b7dd-81aca75a7e90"), new FileInfo(@"C:\asdf.simultan"),
                projectData, managedFiles, new FileInfo[] { }, new DirectoryInfo[] { },
                new FileInfo[] { }, new DirectoryInfo(@"C:\unpack"));

            var func = MultiValueDxfTests.CreateFunction();
            func.Id = new SimId(project, 2255);

            projectData.ValueManager.StartLoading();
            projectData.ValueManager.Add(func);
            projectData.ValueManager.EndLoading();

            SimStringParameter parameter = new SimStringParameter(99, "Parameter X",
                SimCategory.Cooling | SimCategory.Communication | SimCategory.Light_Artificial,
                SimInfoFlow.Output,
                "test string", "text value with spaces",
                new SimMultiValueFunctionParameterSource(func, "graph 2", 3.0, -1.5),
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

            AssertUtil.AreEqualMultiline(Properties.Resources.DXFSerializer_WriteMVPointer_Function, exportedString);
        }

        [TestMethod]
        public void ParseFunctionPointer()
        {
            ExtendedProjectData projectData = new ExtendedProjectData();
            ManagedFileCollection managedFiles = new ManagedFileCollection(new List<FileInfo> { }, projectData);
            HierarchicalProject project = new CompactProject(Guid.Parse("3db2ce29-f752-4bb9-b7dd-81aca75a7e90"), new FileInfo(@"C:\asdf.simultan"),
                projectData, managedFiles, new FileInfo[] { }, new DirectoryInfo[] { },
                new FileInfo[] { }, new DirectoryInfo(@"C:\unpack"));

            var func = MultiValueDxfTests.CreateFunction();
            func.Id = new SimId(project, 2255);

            projectData.ValueManager.StartLoading();
            projectData.ValueManager.Add(func);
            projectData.ValueManager.EndLoading();

            SimStringParameter parameter = null;

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_ReadMVPointer_FunctionV19_String)))
            {
                var info = new DXFParserInfo(project.GlobalID, projectData);
                info.FileVersion = 19;

                reader.Read();

                parameter = ComponentDxfIOComponents.BaseParameterEntityElement.Parse(reader, info) as SimStringParameter;
            }

            var mvp = parameter.ValueSource as SimMultiValueFunctionParameterSource;
            Assert.IsNotNull(mvp);
            Assert.AreEqual(func, mvp.ValueField);
            Assert.AreEqual(parameter, mvp.TargetParameter);
            Assert.AreEqual(3.0, mvp.AxisValueX);
            Assert.AreEqual(-1.5, mvp.AxisValueY);
            Assert.AreEqual("graph 2", mvp.GraphName);
        }

        #endregion
    }
}
