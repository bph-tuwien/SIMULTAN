using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data;
using SIMULTAN.Data.Components;
using SIMULTAN.Data.MultiValues;
using SIMULTAN.Data.Taxonomy;
using SIMULTAN.Projects;
using SIMULTAN.Projects.ManagedFiles;
using SIMULTAN.Serializer.CODXF;
using SIMULTAN.Serializer.DXF;
using SIMULTAN.Tests.Properties;
using SIMULTAN.Tests.Util;
using SIMULTAN.Tests.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SIMULTAN.Tests.IO
{
    [TestClass]
    public class ComponentDxfParameterTests
    {
        #region Parameter

        [TestMethod]
        public void WriteParameter()
        {
            SimParameter parameter = new SimParameter(99, "Parameter X", "Unit",
                SimCategory.Cooling | SimCategory.Communication | SimCategory.Light_Artificial,
                SimInfoFlow.Output,
                45.67, -12.3, double.PositiveInfinity, "text value with spaces", null,
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

            AssertUtil.AreEqualMultiline(Properties.Resources.DXFSerializer_WriteParameter, exportedString);
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

            SimParameter parameter = new SimParameter(99, "Parameter X", "Unit",
                SimCategory.Cooling | SimCategory.Communication | SimCategory.Light_Artificial,
                SimInfoFlow.Output,
                45.67, -12.3, double.PositiveInfinity, "text value with spaces", new SimMultiValueBigTable.SimMultiValueBigTablePointer(table, 2, 1),
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

            AssertUtil.AreEqualMultiline(Properties.Resources.DXFSerializer_WriteParameterWithPointer, exportedString);
        }

        [TestMethod]
        public void WriteParameterWithTaxonomyEntry()
        {
            var tax = new SimTaxonomy(new SimId(1200)) { Name = "Taxonomy"};
            var taxEntry = new SimTaxonomyEntry(new SimId(1201)) { Name = "Parameter X", Key = "key" };
            tax.Entries.Add(taxEntry);
            SimParameter parameter = new SimParameter(99, "Parameter X", "Unit",
                SimCategory.Cooling | SimCategory.Communication | SimCategory.Light_Artificial,
                SimInfoFlow.Output,
                45.67, -12.3, double.PositiveInfinity, "text value with spaces", null,
                SimParameterOperations.EditValue | SimParameterOperations.EditName, SimParameterInstancePropagation.PropagateAlways, true);
            parameter.TaxonomyEntry = new SimTaxonomyEntryOrString(new SimTaxonomyEntryReference(taxEntry));

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

            AssertUtil.AreEqualMultiline(Properties.Resources.DXFSerializer_WriteParameterWithTaxonomyEntry, exportedString);
        }

        [TestMethod]
        public void ParseParameterV0()
        {
            ExtendedProjectData projectData = new ExtendedProjectData();
            Guid guid = Guid.NewGuid();

            SimParameter parameter = null;

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_ReadCODXF_ParameterV0)))
            {
                var info = new DXFParserInfo(guid, projectData);
                info.FileVersion = 0;

                reader.Read();

                parameter = ComponentDxfIOComponents.ParameterEntityElement.Parse(reader, info);
            }

            Assert.IsNotNull(parameter);
            Assert.AreEqual(1076741824, parameter.Id.LocalId);
            Assert.AreEqual("Parameter X", parameter.TaxonomyEntry.Name);
            Assert.AreEqual("Unit", parameter.Unit);
            Assert.AreEqual(SimCategory.Cooling | SimCategory.Communication | SimCategory.Light_Artificial, parameter.Category);
            Assert.AreEqual(SimInfoFlow.Output, parameter.Propagation);
            Assert.AreEqual(SimParameterInstancePropagation.PropagateAlways, parameter.InstancePropagationMode);
            AssertUtil.AssertDoubleEqual(-12.3, parameter.ValueMin);
            AssertUtil.AssertDoubleEqual(double.PositiveInfinity, parameter.ValueMax);
            AssertUtil.AssertDoubleEqual(45.67, parameter.ValueCurrent);
            Assert.AreEqual("text value with spaces", parameter.TextValue);
            Assert.AreEqual(SimParameterOperations.EditValue | SimParameterOperations.Move, parameter.AllowedOperations);
            Assert.AreEqual(true, parameter.IsAutomaticallyGenerated);
            Assert.IsNull(parameter.MultiValuePointer);
        }

        [TestMethod]
        public void ParseParameterV5()
        {
            ExtendedProjectData projectData = new ExtendedProjectData();
            Guid guid = Guid.NewGuid();

            SimParameter parameter = null;

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_ReadCODXF_ParameterV5)))
            {
                var info = new DXFParserInfo(guid, projectData);
                info.FileVersion = 5;

                reader.Read();

                parameter = ComponentDxfIOComponents.ParameterEntityElement.Parse(reader, info);
            }

            Assert.IsNotNull(parameter);
            Assert.AreEqual(99, parameter.Id.LocalId);
            Assert.AreEqual("Parameter X", parameter.TaxonomyEntry.Name);
            Assert.AreEqual("Unit", parameter.Unit);
            Assert.AreEqual(SimCategory.Cooling | SimCategory.Communication | SimCategory.Light_Artificial, parameter.Category);
            Assert.AreEqual(SimInfoFlow.Output, parameter.Propagation);
            Assert.AreEqual(SimParameterInstancePropagation.PropagateAlways, parameter.InstancePropagationMode);
            AssertUtil.AssertDoubleEqual(-12.3, parameter.ValueMin);
            AssertUtil.AssertDoubleEqual(double.PositiveInfinity, parameter.ValueMax);
            AssertUtil.AssertDoubleEqual(45.67, parameter.ValueCurrent);
            Assert.AreEqual("text value with spaces", parameter.TextValue);
            Assert.AreEqual(SimParameterOperations.EditValue, parameter.AllowedOperations);
            Assert.AreEqual(true, parameter.IsAutomaticallyGenerated);
            Assert.IsNull(parameter.MultiValuePointer);
        }

        [TestMethod]
        public void ParseParameterV14()
        {
            ExtendedProjectData projectData = new ExtendedProjectData();
            Guid guid = Guid.NewGuid();

            SimParameter parameter = null;

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_ReadCODXF_ParameterV14)))
            {
                var info = new DXFParserInfo(guid, projectData);
                info.FileVersion = 14;

                reader.Read();

                parameter = ComponentDxfIOComponents.ParameterEntityElement.Parse(reader, info);
            }

            Assert.IsNotNull(parameter);
            Assert.AreEqual(99, parameter.Id.LocalId);
            Assert.IsNull(parameter.TaxonomyEntry.Name); // null because taxonomy entry reference not restored yet
            Assert.AreEqual(true, parameter.TaxonomyEntry.HasTaxonomyEntryReference());
            Assert.AreEqual(1200, parameter.TaxonomyEntry.TaxonomyEntryReference.TaxonomyId.LocalId);
            Assert.AreEqual(1201, parameter.TaxonomyEntry.TaxonomyEntryReference.TaxonomyEntryId.LocalId);
            Assert.AreEqual("Unit", parameter.Unit);
            Assert.AreEqual(SimCategory.Cooling | SimCategory.Communication | SimCategory.Light_Artificial, parameter.Category);
            Assert.AreEqual(SimInfoFlow.Output, parameter.Propagation);
            Assert.AreEqual(SimParameterInstancePropagation.PropagateAlways, parameter.InstancePropagationMode);
            AssertUtil.AssertDoubleEqual(-12.3, parameter.ValueMin);
            AssertUtil.AssertDoubleEqual(double.PositiveInfinity, parameter.ValueMax);
            AssertUtil.AssertDoubleEqual(45.67, parameter.ValueCurrent);
            Assert.AreEqual("text value with spaces", parameter.TextValue);
            Assert.AreEqual(SimParameterOperations.EditValue | SimParameterOperations.EditName, parameter.AllowedOperations);
            Assert.AreEqual(true, parameter.IsAutomaticallyGenerated);
            Assert.IsNull(parameter.MultiValuePointer);
        }

        [TestMethod]
        public void ParseParameterV12()
        {
            ExtendedProjectData projectData = new ExtendedProjectData();
            Guid guid = Guid.NewGuid();

            SimParameter parameter = null;

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_ReadCODXF_ParameterV12)))
            {
                var info = new DXFParserInfo(guid, projectData);
                info.FileVersion = 12;

                reader.Read();

                parameter = ComponentDxfIOComponents.ParameterEntityElement.Parse(reader, info);
            }

            Assert.IsNotNull(parameter);
            Assert.AreEqual(99, parameter.Id.LocalId);
            Assert.AreEqual("Parameter X", parameter.TaxonomyEntry.Name);
            Assert.AreEqual("Unit", parameter.Unit);
            Assert.AreEqual(SimCategory.Cooling | SimCategory.Communication | SimCategory.Light_Artificial, parameter.Category);
            Assert.AreEqual(SimInfoFlow.Output, parameter.Propagation);
            Assert.AreEqual(SimParameterInstancePropagation.PropagateAlways, parameter.InstancePropagationMode);
            AssertUtil.AssertDoubleEqual(-12.3, parameter.ValueMin);
            AssertUtil.AssertDoubleEqual(double.PositiveInfinity, parameter.ValueMax);
            AssertUtil.AssertDoubleEqual(45.67, parameter.ValueCurrent);
            Assert.AreEqual("text value with spaces", parameter.TextValue);
            Assert.AreEqual(SimParameterOperations.EditValue | SimParameterOperations.EditName, parameter.AllowedOperations);
            Assert.AreEqual(true, parameter.IsAutomaticallyGenerated);
            Assert.IsNull(parameter.MultiValuePointer);
        }

        [TestMethod]
        public void ParseParameterWithPointerV12()
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

            SimParameter parameter = null;

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_ReadCODXF_ParameterWithPointerV12)))
            {
                var info = new DXFParserInfo(project.GlobalID, projectData);
                info.FileVersion = 12;

                reader.Read();

                parameter = ComponentDxfIOComponents.ParameterEntityElement.Parse(reader, info);
            }

            Assert.IsNotNull(parameter);
            Assert.AreEqual(99, parameter.Id.LocalId);
            Assert.AreEqual("Parameter X", parameter.TaxonomyEntry.Name);
            Assert.AreEqual("Unit", parameter.Unit);
            Assert.AreEqual(SimCategory.Cooling | SimCategory.Communication | SimCategory.Light_Artificial, parameter.Category);
            Assert.AreEqual(SimInfoFlow.Output, parameter.Propagation);
            Assert.AreEqual(SimParameterInstancePropagation.PropagateAlways, parameter.InstancePropagationMode);
            AssertUtil.AssertDoubleEqual(-12.3, parameter.ValueMin);
            AssertUtil.AssertDoubleEqual(double.PositiveInfinity, parameter.ValueMax);
            AssertUtil.AssertDoubleEqual(6.0, parameter.ValueCurrent);
            Assert.AreEqual("text value with spaces", parameter.TextValue);
            Assert.AreEqual(SimParameterOperations.EditValue | SimParameterOperations.EditName, parameter.AllowedOperations);
            Assert.AreEqual(true, parameter.IsAutomaticallyGenerated);
            Assert.IsNotNull(parameter.MultiValuePointer);
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

            SimParameter parameter = new SimParameter(99, "Parameter X", "Unit",
                SimCategory.Cooling | SimCategory.Communication | SimCategory.Light_Artificial,
                SimInfoFlow.Output,
                45.67, -12.3, double.PositiveInfinity, "text value with spaces", new SimMultiValueBigTable.SimMultiValueBigTablePointer(table, 2, 1),
                SimParameterOperations.EditValue | SimParameterOperations.EditName, SimParameterInstancePropagation.PropagateAlways, true);
            parameter.Id = new SimId(project, 99);

            //Test
            string exportedString = null;
            using (MemoryStream stream = new MemoryStream())
            {
                using (DXFStreamWriter writer = new DXFStreamWriter(stream, true))
                {
                    ComponentDxfIOComponents.WriteMultiValuePointer(parameter.MultiValuePointer, writer);
                }

                stream.Flush();
                stream.Position = 0;

                var array = stream.ToArray();
                exportedString = Encoding.UTF8.GetString(array);
            }

            AssertUtil.AreEqualMultiline(Properties.Resources.DXFSerializer_WriteMVPointer_BigTable, exportedString);
        }

        [TestMethod]
        public void ParseBigTablePointerV0a()
        {
            ExtendedProjectData projectData = new ExtendedProjectData();
            ManagedFileCollection managedFiles = new ManagedFileCollection(new List<FileInfo> { }, projectData);
            HierarchicalProject project = new CompactProject(Guid.Parse("3db2ce29-f752-4bb9-b7dd-81aca75a7e90"), new FileInfo(@"C:\asdf.simultan"),
                projectData, managedFiles, new FileInfo[] { }, new DirectoryInfo[] { },
                new FileInfo[] { }, new DirectoryInfo(@"C:\unpack"));

            var info = new DXFParserInfo(project.GlobalID, projectData);
            info.FileVersion = 0;

            var table = MultiValueDxfTests.CreateBigTable();
            table.Id = new SimId(project, info.TranslateId(typeof(SimMultiValue), 2255));

            projectData.ValueManager.StartLoading();
            projectData.ValueManager.Add(table);
            projectData.ValueManager.EndLoading();

            SimParameter parameter = null;

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_ReadMVPointer_BigTableV0a)))
            {
                reader.Read();

                parameter = ComponentDxfIOComponents.ParameterEntityElement.Parse(reader, info);
            }

            var mvp = parameter.MultiValuePointer as SimMultiValueBigTable.SimMultiValueBigTablePointer;
            Assert.IsNotNull(mvp);
            Assert.AreEqual(table, mvp.ValueField);
            Assert.AreEqual(parameter, mvp.TargetParameter);
            Assert.AreEqual(1, mvp.Column);
            Assert.AreEqual(2, mvp.Row);
        }

        [TestMethod]
        public void ParseBigTablePointerV5()
        {
            ExtendedProjectData projectData = new ExtendedProjectData();
            ManagedFileCollection managedFiles = new ManagedFileCollection(new List<FileInfo> { }, projectData);
            HierarchicalProject project = new CompactProject(Guid.Parse("3db2ce29-f752-4bb9-b7dd-81aca75a7e90"), new FileInfo(@"C:\asdf.simultan"),
                projectData, managedFiles, new FileInfo[] { }, new DirectoryInfo[] { },
                new FileInfo[] { }, new DirectoryInfo(@"C:\unpack"));

            var info = new DXFParserInfo(project.GlobalID, projectData);
            info.FileVersion = 5;

            var table = MultiValueDxfTests.CreateBigTable();
            table.Id = new SimId(project, info.TranslateId(typeof(SimMultiValue), 2255));

            projectData.ValueManager.StartLoading();
            projectData.ValueManager.Add(table);
            projectData.ValueManager.EndLoading();

            SimParameter parameter = null;

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_ReadMVPointer_BigTableV5)))
            {
                reader.Read();

                parameter = ComponentDxfIOComponents.ParameterEntityElement.Parse(reader, info);
            }

            var mvp = parameter.MultiValuePointer as SimMultiValueBigTable.SimMultiValueBigTablePointer;
            Assert.IsNotNull(mvp);
            Assert.AreEqual(table, mvp.ValueField);
            Assert.AreEqual(parameter, mvp.TargetParameter);
            Assert.AreEqual(1, mvp.Column);
            Assert.AreEqual(2, mvp.Row);
        }

        [TestMethod]
        public void ParseBigTablePointerV12()
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

            SimParameter parameter = null;

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_ReadMVPointer_BigTableV12)))
            {
                var info = new DXFParserInfo(project.GlobalID, projectData);
                info.FileVersion = 12;

                reader.Read();

                parameter = ComponentDxfIOComponents.ParameterEntityElement.Parse(reader, info);
            }

            var mvp = parameter.MultiValuePointer as SimMultiValueBigTable.SimMultiValueBigTablePointer;
            Assert.IsNotNull(mvp);
            Assert.AreEqual(table, mvp.ValueField);
            Assert.AreEqual(parameter, mvp.TargetParameter);
            Assert.AreEqual(1, mvp.Column);
            Assert.AreEqual(2, mvp.Row);
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

            SimParameter parameter = new SimParameter(99, "Parameter X", "Unit",
                SimCategory.Cooling | SimCategory.Communication | SimCategory.Light_Artificial,
                SimInfoFlow.Output,
                45.67, -12.3, double.PositiveInfinity, "text value with spaces", new SimMultiValueBigTable.SimMultiValueBigTablePointer(table, 2, 1),
                SimParameterOperations.EditValue | SimParameterOperations.EditName, SimParameterInstancePropagation.PropagateAlways, true);
            parameter.Id = new SimId(project, 99);

            //Test
            string exportedString = null;
            using (MemoryStream stream = new MemoryStream())
            {
                using (DXFStreamWriter writer = new DXFStreamWriter(stream, true))
                {
                    ComponentDxfIOComponents.WriteMultiValuePointer(parameter.MultiValuePointer, writer);
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

            SimParameter parameter = new SimParameter(99, "Parameter X", "Unit",
                SimCategory.Cooling | SimCategory.Communication | SimCategory.Light_Artificial,
                SimInfoFlow.Output,
                45.67, -12.3, double.PositiveInfinity, "text value with spaces", 
                new SimMultiValueField3D.SimMultiValueField3DPointer(field, 3.0, -1.5, 0.23),
                SimParameterOperations.EditValue | SimParameterOperations.EditName, SimParameterInstancePropagation.PropagateAlways, true);
            parameter.Id = new SimId(project, 99);

            //Test
            string exportedString = null;
            using (MemoryStream stream = new MemoryStream())
            {
                using (DXFStreamWriter writer = new DXFStreamWriter(stream, true))
                {
                    ComponentDxfIOComponents.WriteMultiValuePointer(parameter.MultiValuePointer, writer);
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
            SimParameter parameter = null;

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_ReadMVPointer_Field3DV12)))
            {
                var info = new DXFParserInfo(project.GlobalID, projectData);
                info.FileVersion = 12;

                reader.Read();

                parameter = ComponentDxfIOComponents.ParameterEntityElement.Parse(reader, info);
            }

            //Check
            var mvp = parameter.MultiValuePointer as SimMultiValueField3D.SimMultiValueField3DPointer;
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

            SimParameter parameter = new SimParameter(99, "Parameter X", "Unit",
                SimCategory.Cooling | SimCategory.Communication | SimCategory.Light_Artificial,
                SimInfoFlow.Output,
                45.67, -12.3, double.PositiveInfinity, "text value with spaces",
                new SimMultiValueFunction.MultiValueFunctionPointer(func, "graph 2", 3.0, -1.5),
                SimParameterOperations.EditValue | SimParameterOperations.EditName, SimParameterInstancePropagation.PropagateAlways, true);
            parameter.Id = new SimId(project, 99);

            //Test
            string exportedString = null;
            using (MemoryStream stream = new MemoryStream())
            {
                using (DXFStreamWriter writer = new DXFStreamWriter(stream, true))
                {
                    ComponentDxfIOComponents.WriteMultiValuePointer(parameter.MultiValuePointer, writer);
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

            SimParameter parameter = null;

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_ReadMVPointer_FunctionV12)))
            {
                var info = new DXFParserInfo(project.GlobalID, projectData);
                info.FileVersion = 12;

                reader.Read();

                parameter = ComponentDxfIOComponents.ParameterEntityElement.Parse(reader, info);
            }

            var mvp = parameter.MultiValuePointer as SimMultiValueFunction.MultiValueFunctionPointer;
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
