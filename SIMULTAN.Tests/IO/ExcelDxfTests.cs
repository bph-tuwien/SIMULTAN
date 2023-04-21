using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data;
using SIMULTAN.Data.Components;
using SIMULTAN.Excel;
using SIMULTAN.Projects;
using SIMULTAN.Serializer;
using SIMULTAN.Serializer.DXF;
using SIMULTAN.Tests.Properties;
using SIMULTAN.Tests.Util;
using SIMULTAN.Tests.TestUtils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Media3D;
using SIMULTAN.DataMapping;
using System.Text.RegularExpressions;
using SIMULTAN.Data.Taxonomy;

namespace SIMULTAN.Tests.IO
{
    [TestClass]
    public class ExcelDxfTests
    {
        private static void CreateTestProjectData(ProjectData projectData)
        {
            projectData.Components.StartLoading();

            //Project
            SimComponent component = new SimComponent()
            {
                Name = "ParameterHost",
            };

            SimDoubleParameter parameter = new SimDoubleParameter("Param", "unit", 3.4)
            {
                Id = new SimId(projectData.Components.CalledFromLocation.GlobalID, 8899)
            };
            component.Parameters.Add(parameter);
            projectData.Components.Add(component);

            projectData.Components.EndLoading();
        }

        private static SimDataMappingTool CreateTestTool(ProjectData projectData)
        {
            CreateTestProjectData(projectData);

            projectData.DataMappingTools.StartLoading();

            var parameter = projectData.Components.First(x => x.Name == "ParameterHost")
                .Parameters.First(x => x.NameTaxonomyEntry.Name == "Param");

            //Tool
            var rule = new SimDataMappingRuleComponent("SheetA")
            {
                Name = "Demo Rule",
                OffsetParent = new Utils.IntIndex2D(1, 2),
                OffsetConsecutive = new Utils.IntIndex2D(3, 4),
                MaxMatches = 99,
                MaxDepth = 101,
                MappingDirection = SimDataMappingDirection.Vertical,
                ReferencePoint = SimDataMappingReferencePoint.TopRight,
                TraversalStrategy = SimDataMappingRuleTraversalStrategy.References,
            };
            rule.Properties.Add(SimDataMappingComponentMappingProperties.Name);
            rule.Properties.Add(SimDataMappingComponentMappingProperties.Slot);
            rule.Properties.Add(SimDataMappingComponentMappingProperties.Id);

            rule.Filter.Add(new SimDataMappingFilterComponent(SimDataMappingComponentFilterProperties.Name, "filtername"));
            rule.Filter.Add(new SimDataMappingFilterComponent(SimDataMappingComponentFilterProperties.Slot,
                new SimSlot(new SimTaxonomyEntry(new SimId(5566)), "exten")));
            rule.Filter.Add(new SimDataMappingFilterComponent(SimDataMappingComponentFilterProperties.InstanceIsRealized, true));
            rule.Filter.Add(new SimDataMappingFilterComponent(SimDataMappingComponentFilterProperties.InstanceType, SimInstanceType.AttributesFace));

            var childRule1 = new SimDataMappingRuleComponent("SheetB")
            {
                Name = "Child Rule 1",
            };
            childRule1.Properties.Add(SimDataMappingComponentMappingProperties.Name);
            childRule1.Filter.Add(new SimDataMappingFilterComponent(SimDataMappingComponentFilterProperties.Name, "child filter value"));

            var childRule2 = new SimDataMappingRuleComponent("SheetB")
            {
                Name = "Child Rule 2",
            };
            childRule2.Properties.Add(SimDataMappingComponentMappingProperties.Id);
            childRule2.Filter.Add(new SimDataMappingFilterComponent(SimDataMappingComponentFilterProperties.Name, "child filter value"));

            var parameterRule = new SimDataMappingRuleParameter("SheetB")
            {
                Name = "Parameter Rule 1",
            };
            parameterRule.Properties.Add(SimDataMappingParameterMappingProperties.Value);
            parameterRule.Filter.Add(new SimDataMappingFilterParameter(SimDataMappingParameterFilterProperties.Unit, "m/c"));

            rule.Rules.Add(childRule1);
            rule.Rules.Add(childRule2);
            rule.Rules.Add(parameterRule);

            var readRule1 = new SimDataMappingReadRule()
            {
                SheetName = "SheetA",
                Range = new Utils.RowColumnRange(4, 3, 6, 5),
                Parameter = null,
            };
            var readRule2 = new SimDataMappingReadRule()
            {
                SheetName = "SheetB",
                Range = new Utils.RowColumnRange(40, 30, 60, 50),
                Parameter = parameter,
            };

            SimDataMappingTool tool = new SimDataMappingTool("Custom Tool")
            {
                Id = new SimId(7788),
                MacroName = "Module1.MyCode"
            };
            tool.Rules.Add(rule);
            tool.ReadRules.Add(readRule1);
            tool.ReadRules.Add(readRule2);

            projectData.DataMappingTools.Add(tool);
            projectData.DataMappingTools.EndLoading();

            return tool;
        }

        private static void CheckTestTool(SimDataMappingTool tool, ProjectData projectData)
        {
            Assert.IsNotNull(tool);

            Assert.AreEqual("Custom Tool", tool.Name);
            Assert.AreEqual("Module1.MyCode", tool.MacroName);
            Assert.AreEqual(7788, tool.Id.LocalId);
            Assert.AreEqual(1, tool.Rules.Count);

            var rule = tool.Rules[0];
            Assert.AreEqual("Demo Rule", rule.Name);
            Assert.AreEqual("SheetA", rule.SheetName);
            Assert.AreEqual(1, rule.OffsetParent.X);
            Assert.AreEqual(2, rule.OffsetParent.Y);
            Assert.AreEqual(3, rule.OffsetConsecutive.X);
            Assert.AreEqual(4, rule.OffsetConsecutive.Y);
            Assert.AreEqual(99, rule.MaxMatches);
            Assert.AreEqual(101, rule.MaxDepth);
            Assert.AreEqual(SimDataMappingDirection.Vertical, rule.MappingDirection);
            Assert.AreEqual(SimDataMappingReferencePoint.TopRight, rule.ReferencePoint);
            Assert.AreEqual(SimDataMappingRuleTraversalStrategy.References, rule.TraversalStrategy);

            Assert.AreEqual(3, rule.Properties.Count);
            Assert.AreEqual(SimDataMappingComponentMappingProperties.Name, rule.Properties[0]);
            Assert.AreEqual(SimDataMappingComponentMappingProperties.Slot, rule.Properties[1]);
            Assert.AreEqual(SimDataMappingComponentMappingProperties.Id, rule.Properties[2]);

            Assert.AreEqual(4, rule.Filter.Count);
            Assert.AreEqual(SimDataMappingComponentFilterProperties.Name, rule.Filter[0].Property);
            Assert.AreEqual("filtername", rule.Filter[0].Value);
            Assert.AreEqual(SimDataMappingComponentFilterProperties.Slot, rule.Filter[1].Property);
            Assert.IsTrue(rule.Filter[1].Value is SimSlot);
            var taxEntry = projectData.IdGenerator.GetById<SimTaxonomyEntry>(
                new SimId(projectData.Taxonomies.CalledFromLocation.GlobalID, 5566));
            Assert.AreEqual(taxEntry, ((SimSlot)rule.Filter[1].Value).SlotBase.Target);
            Assert.AreEqual("exten", ((SimSlot)rule.Filter[1].Value).SlotExtension);
            Assert.AreEqual(SimDataMappingComponentFilterProperties.InstanceIsRealized, rule.Filter[2].Property);
            Assert.AreEqual(true, rule.Filter[2].Value);
            Assert.AreEqual(SimDataMappingComponentFilterProperties.InstanceType, rule.Filter[3].Property);
            Assert.AreEqual(SimInstanceType.AttributesFace, rule.Filter[3].Value);

            //Children
            Assert.AreEqual(3, rule.Rules.Count);
            Assert.IsTrue(rule.Rules[0] is SimDataMappingRuleComponent);
            Assert.AreEqual("Child Rule 1", rule.Rules[0].Name);
            Assert.IsTrue(rule.Rules[1] is SimDataMappingRuleComponent);
            Assert.AreEqual("Child Rule 2", rule.Rules[1].Name);
            Assert.IsTrue(rule.Rules[2] is SimDataMappingRuleParameter);
            Assert.AreEqual("Parameter Rule 1", rule.Rules[2].Name);

            //Read rules
            Assert.AreEqual(2, tool.ReadRules.Count);
            var rrule = tool.ReadRules[0];
            Assert.AreEqual("SheetA", rrule.SheetName);
            Assert.AreEqual(null, rrule.Parameter);
            Assert.AreEqual(3, rrule.Range.ColumnStart);
            Assert.AreEqual(4, rrule.Range.RowStart);
            Assert.AreEqual(5, rrule.Range.ColumnCount);
            Assert.AreEqual(6, rrule.Range.RowCount);

            rrule = tool.ReadRules[1];
            var parameter = projectData.Components.First(x => x.Name == "ParameterHost")
                .Parameters.First(x => x.NameTaxonomyEntry.Name == "Param");
            Assert.AreEqual("SheetB", rrule.SheetName);
            Assert.AreEqual(parameter, rrule.Parameter);
            Assert.AreEqual(30, rrule.Range.ColumnStart);
            Assert.AreEqual(40, rrule.Range.RowStart);
            Assert.AreEqual(50, rrule.Range.ColumnCount);
            Assert.AreEqual(60, rrule.Range.RowCount);
        }

        #region Data Mapping Tool

        [TestMethod]
        public void WriteDataMappingTool()
        {
            var guid = Guid.NewGuid();
            ExtendedProjectData projectData = new ExtendedProjectData();
            projectData.SetCallingLocation(new DummyReferenceLocation(guid));

            var tool = CreateTestTool(projectData);

            string exportedString = null;
            using (MemoryStream stream = new MemoryStream())
            {
                using (DXFStreamWriter writer = new DXFStreamWriter(stream, true))
                {
                    ExcelDxfIO.WriteDataMappingTool(tool, writer);
                }

                stream.Flush();
                stream.Position = 0;

                var array = stream.ToArray();
                exportedString = Encoding.UTF8.GetString(array);
            }

            AssertUtil.AreEqualMultiline(Resources.DXFSerializer_ETDXF_WriteDataMappingTool, exportedString);
        }

        [TestMethod]
        public void WriteDataMappingToolMappings()
        {
            var guid = Guid.NewGuid();
            var otherGuid = Guid.Parse("da7d8f7c-8eec-423b-b127-9d6e17f52522");

            ExtendedProjectData projectData = new ExtendedProjectData();
            projectData.SetCallingLocation(new DummyReferenceLocation(guid));

            var costTax = TaxonomyUtils.GetDefaultTaxonomies().GetDefaultSlot(SimDefaultSlotKeys.Cost);

            SimComponent childComponent = new SimComponent()
            {
                CurrentSlot = new SimTaxonomyEntryReference(costTax),
                Id = new SimId(guid, 9997),
            };

            SimComponent rootComponent = new SimComponent()
            {
                CurrentSlot = new SimTaxonomyEntryReference(costTax),
                Id = new SimId(guid, 9998),
            };
            rootComponent.Components.Add(new SimChildComponentEntry(new SimSlot(costTax, "0"), childComponent));

            projectData.Components.StartLoading();
            projectData.Components.Add(rootComponent);
            projectData.Components.EndLoading();

            var tool = CreateTestTool(projectData);
            tool.Rules.AddMapping(tool.Rules.First(), rootComponent);
            tool.Rules.AddMapping(tool.Rules.First(), childComponent);

            string exportedString = null;
            using (MemoryStream stream = new MemoryStream())
            {
                using (DXFStreamWriter writer = new DXFStreamWriter(stream, true))
                {
                    ExcelDxfIO.WriteDataMappingTool(tool, writer);
                }

                stream.Flush();
                stream.Position = 0;

                var array = stream.ToArray();
                exportedString = Encoding.UTF8.GetString(array);
            }

            AssertUtil.AreEqualMultiline(Resources.DXFSerializer_ETDXF_WriteDataMappingToolMappings, exportedString);
        }

        [TestMethod]
        public void ReadDataMappingToolV21()
        {
            Guid guid = Guid.NewGuid();
            Guid otherguid = new Guid("98478ed1-d3f4-4873-95b6-412e5e23aac5");

            ExtendedProjectData projectData = new ExtendedProjectData();
            projectData.SetCallingLocation(new DummyReferenceLocation(guid));

            CreateTestProjectData(projectData);

            var demoTax = new SimTaxonomy("demotax");
            var taxEntry = new SimTaxonomyEntry(new SimId(5566)) { Key = "demokey" };
            projectData.Taxonomies.StartLoading();
            demoTax.Entries.Add(taxEntry);
            projectData.Taxonomies.Add(demoTax);
            projectData.Taxonomies.StopLoading();

            SimDataMappingTool tool = null;

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_ETDXF_ReadDMToolV21)))
            {
                var info = new DXFParserInfo(guid, projectData);
                info.FileVersion = 21;

                reader.Read();

                tool = ExcelDxfIO.DataMappingToolEntityElement.Parse(reader, info);
            }

            HierarchicalProject.LoadDefaultTaxonomies(projectData);
            projectData.Components.RestoreDefaultTaxonomyReferences();

            CheckTestTool(tool, projectData);
        }

        [TestMethod]
        public void ReadDataMappingToolMappingsV21()
        {
            var guid = Guid.NewGuid();
            var otherGuid = Guid.Parse("da7d8f7c-8eec-423b-b127-9d6e17f52522");

            ExtendedProjectData projectData = new ExtendedProjectData();
            projectData.SetCallingLocation(new DummyReferenceLocation(guid));

            var costTax = TaxonomyUtils.GetDefaultTaxonomies().GetDefaultSlot(SimDefaultSlotKeys.Cost);

            SimComponent childComponent = new SimComponent()
            {
                CurrentSlot = new SimTaxonomyEntryReference(costTax),
                Id = new SimId(guid, 9997),
            };

            SimComponent rootComponent = new SimComponent()
            {
                CurrentSlot = new SimTaxonomyEntryReference(costTax),
                Id = new SimId(guid, 9998),
            };
            rootComponent.Components.Add(new SimChildComponentEntry(new SimSlot(costTax, "0"), childComponent));

            projectData.Components.StartLoading();
            projectData.Components.Add(rootComponent);
            projectData.Components.EndLoading();

            SimDataMappingTool tool = null;

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_ETDXF_ReadDMToolMappingsV21)))
            {
                var info = new DXFParserInfo(guid, projectData);
                info.FileVersion = 21;

                reader.Read();

                tool = ExcelDxfIO.DataMappingToolEntityElement.Parse(reader, info);
            }

            HierarchicalProject.LoadDefaultTaxonomies(projectData);
            projectData.Components.RestoreDefaultTaxonomyReferences();

            var mappings = tool.Rules.GetMappings(tool.Rules.First());
            Assert.IsNotNull(mappings);
            Assert.AreEqual(2, mappings.Count());
            Assert.IsTrue(mappings.Contains(rootComponent));
            Assert.IsTrue(mappings.Contains(childComponent));
        }

        #endregion

        #region Data Mapping Rule

        [TestMethod]
        public void WriteDataMappingRuleComponent()
        {
            var rule = new SimDataMappingRuleComponent("SheetA")
            {
                Name = "Demo Rule",
                OffsetParent = new Utils.IntIndex2D(1, 2),
                OffsetConsecutive = new Utils.IntIndex2D(3, 4),
                MaxMatches = 99,
                MaxDepth = 101,
                MappingDirection = SimDataMappingDirection.Vertical,
                ReferencePoint = SimDataMappingReferencePoint.TopRight,
                TraversalStrategy = SimDataMappingRuleTraversalStrategy.References,
            };
            rule.Properties.Add(SimDataMappingComponentMappingProperties.Name);
            rule.Properties.Add(SimDataMappingComponentMappingProperties.Slot);
            rule.Properties.Add(SimDataMappingComponentMappingProperties.Id);

            rule.Filter.Add(new SimDataMappingFilterComponent(SimDataMappingComponentFilterProperties.Name, "filtername"));
            rule.Filter.Add(new SimDataMappingFilterComponent(SimDataMappingComponentFilterProperties.Slot,
                new SimSlot(new SimTaxonomyEntry(new SimId(5566)), "exten")));
            rule.Filter.Add(new SimDataMappingFilterComponent(SimDataMappingComponentFilterProperties.InstanceIsRealized, true));
            rule.Filter.Add(new SimDataMappingFilterComponent(SimDataMappingComponentFilterProperties.InstanceType, SimInstanceType.AttributesFace));

            SimDataMappingTool tool = new SimDataMappingTool("tool");
            tool.Rules.Add(rule);

            string exportedString = null;
            using (MemoryStream stream = new MemoryStream())
            {
                using (DXFStreamWriter writer = new DXFStreamWriter(stream, true))
                {
                    ExcelDxfIO.WriteDataMappingRule(rule, writer);
                }

                stream.Flush();
                stream.Position = 0;

                var array = stream.ToArray();
                exportedString = Encoding.UTF8.GetString(array);
            }

            AssertUtil.AreEqualMultiline(Resources.DXFSerializer_ETDXF_WriteDMRuleComponent, exportedString);
        }

        [TestMethod]
        public void WriteDataMappingRuleComponentChildren()
        {
            var rule = new SimDataMappingRuleComponent("SheetA")
            {
                Name = "Demo Rule",
                OffsetParent = new Utils.IntIndex2D(1, 2),
                OffsetConsecutive = new Utils.IntIndex2D(3, 4),
                MaxMatches = 99,
                MaxDepth = 101,
                MappingDirection = SimDataMappingDirection.Vertical,
                ReferencePoint = SimDataMappingReferencePoint.TopRight,
                TraversalStrategy = SimDataMappingRuleTraversalStrategy.References,
            };
            rule.Properties.Add(SimDataMappingComponentMappingProperties.Name);
            rule.Properties.Add(SimDataMappingComponentMappingProperties.Slot);
            rule.Properties.Add(SimDataMappingComponentMappingProperties.Id);

            rule.Filter.Add(new SimDataMappingFilterComponent(SimDataMappingComponentFilterProperties.Name, "filtername"));
            rule.Filter.Add(new SimDataMappingFilterComponent(SimDataMappingComponentFilterProperties.Slot,
                new SimSlot(new SimTaxonomyEntry(new SimId(5566)), "exten")));
            rule.Filter.Add(new SimDataMappingFilterComponent(SimDataMappingComponentFilterProperties.InstanceIsRealized, true));
            rule.Filter.Add(new SimDataMappingFilterComponent(SimDataMappingComponentFilterProperties.InstanceType, SimInstanceType.AttributesFace));

            var childRule1 = new SimDataMappingRuleComponent("SheetB")
            {
                Name = "Child Rule 1",
            };
            childRule1.Properties.Add(SimDataMappingComponentMappingProperties.Name);
            childRule1.Filter.Add(new SimDataMappingFilterComponent(SimDataMappingComponentFilterProperties.Name, "child filter value"));

            var childRule2 = new SimDataMappingRuleComponent("SheetB")
            {
                Name = "Child Rule 2",
            };
            childRule2.Properties.Add(SimDataMappingComponentMappingProperties.Id);
            childRule2.Filter.Add(new SimDataMappingFilterComponent(SimDataMappingComponentFilterProperties.Name, "child filter value"));

            rule.Rules.Add(childRule1);
            rule.Rules.Add(childRule2);

            SimDataMappingTool tool = new SimDataMappingTool("tool");
            tool.Rules.Add(rule);

            string exportedString = null;
            using (MemoryStream stream = new MemoryStream())
            {
                using (DXFStreamWriter writer = new DXFStreamWriter(stream, true))
                {
                    ExcelDxfIO.WriteDataMappingRule(rule, writer);
                }

                stream.Flush();
                stream.Position = 0;

                var array = stream.ToArray();
                exportedString = Encoding.UTF8.GetString(array);
            }

            AssertUtil.AreEqualMultiline(Resources.DXFSerializer_ETDXF_WriteDMRuleComponentChildren, exportedString);
        }

        [TestMethod]
        public void ReadDataMappingRuleComponentV21()
        {
            Guid guid = Guid.NewGuid();
            Guid otherguid = new Guid("98478ed1-d3f4-4873-95b6-412e5e23aac5");

            ExtendedProjectData projectData = new ExtendedProjectData();
            projectData.SetCallingLocation(new DummyReferenceLocation(guid));

            var demoTax = new SimTaxonomy("demotax");
            var taxEntry = new SimTaxonomyEntry(new SimId(5566)) { Key = "demokey" };
            projectData.Taxonomies.StartLoading();
            demoTax.Entries.Add(taxEntry);
            projectData.Taxonomies.Add(demoTax);
            projectData.Taxonomies.StopLoading();

            SimDataMappingRuleComponent rule = null;

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_ETDXF_ReadDMRuleComponentV21)))
            {
                var info = new DXFParserInfo(guid, projectData);
                info.FileVersion = 21;

                reader.Read();

                (rule, _) = ExcelDxfIO.ComponentRuleEntityElement.Parse(reader, info);
            }

            HierarchicalProject.LoadDefaultTaxonomies(projectData);
            projectData.Components.RestoreDefaultTaxonomyReferences();

            Assert.IsNotNull(rule);

            Assert.AreEqual("Demo Rule", rule.Name);
            Assert.AreEqual("SheetA", rule.SheetName);
            Assert.AreEqual(1, rule.OffsetParent.X);
            Assert.AreEqual(2, rule.OffsetParent.Y);
            Assert.AreEqual(3, rule.OffsetConsecutive.X);
            Assert.AreEqual(4, rule.OffsetConsecutive.Y);
            Assert.AreEqual(99, rule.MaxMatches);
            Assert.AreEqual(101, rule.MaxDepth);
            Assert.AreEqual(SimDataMappingDirection.Vertical, rule.MappingDirection);
            Assert.AreEqual(SimDataMappingReferencePoint.TopRight, rule.ReferencePoint);
            Assert.AreEqual(SimDataMappingRuleTraversalStrategy.References, rule.TraversalStrategy);

            Assert.AreEqual(3, rule.Properties.Count);
            Assert.AreEqual(SimDataMappingComponentMappingProperties.Name, rule.Properties[0]);
            Assert.AreEqual(SimDataMappingComponentMappingProperties.Slot, rule.Properties[1]);
            Assert.AreEqual(SimDataMappingComponentMappingProperties.Id, rule.Properties[2]);

            Assert.AreEqual(4, rule.Filter.Count);
            Assert.AreEqual(SimDataMappingComponentFilterProperties.Name, rule.Filter[0].Property);
            Assert.AreEqual("filtername", rule.Filter[0].Value);
            Assert.AreEqual(SimDataMappingComponentFilterProperties.Slot, rule.Filter[1].Property);
            Assert.IsTrue(rule.Filter[1].Value is SimSlot);
            Assert.AreEqual(taxEntry, ((SimSlot)rule.Filter[1].Value).SlotBase.Target);
            Assert.AreEqual("exten", ((SimSlot)rule.Filter[1].Value).SlotExtension);
            Assert.AreEqual(SimDataMappingComponentFilterProperties.InstanceIsRealized, rule.Filter[2].Property);
            Assert.AreEqual(true, rule.Filter[2].Value);
            Assert.AreEqual(SimDataMappingComponentFilterProperties.InstanceType, rule.Filter[3].Property);
            Assert.AreEqual(SimInstanceType.AttributesFace, rule.Filter[3].Value);
        }

        [TestMethod]
        public void ReadDataMappingRuleComponentChildrenV21()
        {
            Guid guid = Guid.NewGuid();
            Guid otherguid = new Guid("98478ed1-d3f4-4873-95b6-412e5e23aac5");

            ExtendedProjectData projectData = new ExtendedProjectData();
            projectData.SetCallingLocation(new DummyReferenceLocation(guid));

            var demoTax = new SimTaxonomy("demotax");
            var taxEntry = new SimTaxonomyEntry(new SimId(5566)) { Key = "demokey" };
            projectData.Taxonomies.StartLoading();
            demoTax.Entries.Add(taxEntry);
            projectData.Taxonomies.Add(demoTax);
            projectData.Taxonomies.StopLoading();

            SimDataMappingRuleComponent rule = null;

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_ETDXF_ReadDMRuleChildrenV21)))
            {
                var info = new DXFParserInfo(guid, projectData);
                info.FileVersion = 21;

                reader.Read();

                (rule, _) = ExcelDxfIO.ComponentRuleEntityElement.Parse(reader, info);
            }

            HierarchicalProject.LoadDefaultTaxonomies(projectData);
            projectData.Components.RestoreDefaultTaxonomyReferences();

            Assert.IsNotNull(rule);

            Assert.AreEqual("Demo Rule", rule.Name);
            Assert.AreEqual("SheetA", rule.SheetName);
            Assert.AreEqual(1, rule.OffsetParent.X);
            Assert.AreEqual(2, rule.OffsetParent.Y);
            Assert.AreEqual(3, rule.OffsetConsecutive.X);
            Assert.AreEqual(4, rule.OffsetConsecutive.Y);
            Assert.AreEqual(99, rule.MaxMatches);
            Assert.AreEqual(101, rule.MaxDepth);
            Assert.AreEqual(SimDataMappingDirection.Vertical, rule.MappingDirection);
            Assert.AreEqual(SimDataMappingReferencePoint.TopRight, rule.ReferencePoint);
            Assert.AreEqual(SimDataMappingRuleTraversalStrategy.References, rule.TraversalStrategy);

            Assert.AreEqual(3, rule.Properties.Count);
            Assert.AreEqual(SimDataMappingComponentMappingProperties.Name, rule.Properties[0]);
            Assert.AreEqual(SimDataMappingComponentMappingProperties.Slot, rule.Properties[1]);
            Assert.AreEqual(SimDataMappingComponentMappingProperties.Id, rule.Properties[2]);

            Assert.AreEqual(4, rule.Filter.Count);
            Assert.AreEqual(SimDataMappingComponentFilterProperties.Name, rule.Filter[0].Property);
            Assert.AreEqual("filtername", rule.Filter[0].Value);
            Assert.AreEqual(SimDataMappingComponentFilterProperties.Slot, rule.Filter[1].Property);
            Assert.IsTrue(rule.Filter[1].Value is SimSlot);
            Assert.AreEqual(taxEntry, ((SimSlot)rule.Filter[1].Value).SlotBase.Target);
            Assert.AreEqual("exten", ((SimSlot)rule.Filter[1].Value).SlotExtension);
            Assert.AreEqual(SimDataMappingComponentFilterProperties.InstanceIsRealized, rule.Filter[2].Property);
            Assert.AreEqual(true, rule.Filter[2].Value);
            Assert.AreEqual(SimDataMappingComponentFilterProperties.InstanceType, rule.Filter[3].Property);
            Assert.AreEqual(SimInstanceType.AttributesFace, rule.Filter[3].Value);

            //Children
            Assert.AreEqual(2, rule.Rules.Count);
            Assert.IsTrue(rule.Rules[0] is SimDataMappingRuleComponent);
            Assert.AreEqual("Child Rule 1", rule.Rules[0].Name);
            Assert.IsTrue(rule.Rules[1] is SimDataMappingRuleComponent);
            Assert.AreEqual("Child Rule 2", rule.Rules[1].Name);
        }


        [TestMethod]
        public void WriteDataMappingRuleParameter()
        {
            var rule = new SimDataMappingRuleParameter("SheetA")
            {
                Name = "Demo Rule",
                OffsetParent = new Utils.IntIndex2D(1, 2),
                OffsetConsecutive = new Utils.IntIndex2D(3, 4),
                MaxMatches = 99,
                MaxDepth = 101,
                MappingDirection = SimDataMappingDirection.Vertical,
                ReferencePoint = SimDataMappingReferencePoint.TopRight,
                ParameterRange = SimDataMappingParameterRange.Table,
            };
            rule.Properties.Add(SimDataMappingParameterMappingProperties.Name);
            rule.Properties.Add(SimDataMappingParameterMappingProperties.Id);
            rule.Properties.Add(SimDataMappingParameterMappingProperties.Value);
            rule.Properties.Add(SimDataMappingParameterMappingProperties.Description);
            rule.Properties.Add(SimDataMappingParameterMappingProperties.Unit);
            rule.Properties.Add(SimDataMappingParameterMappingProperties.Min);
            rule.Properties.Add(SimDataMappingParameterMappingProperties.Max);

            rule.Filter.Add(new SimDataMappingFilterParameter(SimDataMappingParameterFilterProperties.Name, "filter name"));
            rule.Filter.Add(new SimDataMappingFilterParameter(SimDataMappingParameterFilterProperties.Unit, "m/c"));
            rule.Filter.Add(new SimDataMappingFilterParameter(SimDataMappingParameterFilterProperties.Propagation, SimInfoFlow.Mixed));
            rule.Filter.Add(new SimDataMappingFilterParameter(SimDataMappingParameterFilterProperties.Category, SimCategory.Humidity | SimCategory.Air));

            string exportedString = null;
            using (MemoryStream stream = new MemoryStream())
            {
                using (DXFStreamWriter writer = new DXFStreamWriter(stream, true))
                {
                    ExcelDxfIO.WriteDataMappingRule(rule, writer);
                }

                stream.Flush();
                stream.Position = 0;

                var array = stream.ToArray();
                exportedString = Encoding.UTF8.GetString(array);
            }

            AssertUtil.AreEqualMultiline(Resources.DXFSerializer_ETDXF_WriteDMRuleParameter, exportedString);
        }

        [TestMethod]
        public void ReadDataMappingRuleParameterV21()
        {
            Guid guid = Guid.NewGuid();

            ExtendedProjectData projectData = new ExtendedProjectData();
            projectData.SetCallingLocation(new DummyReferenceLocation(guid));

            SimDataMappingRuleParameter rule = null;

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_ETDXF_ReadDMRuleParameterV21)))
            {
                var info = new DXFParserInfo(guid, projectData);
                info.FileVersion = 21;

                reader.Read();

                rule = ExcelDxfIO.ParameterRuleEntityElement.Parse(reader, info);
            }

            HierarchicalProject.LoadDefaultTaxonomies(projectData);
            projectData.Components.RestoreDefaultTaxonomyReferences();

            Assert.IsNotNull(rule);

            Assert.AreEqual("Demo Rule", rule.Name);
            Assert.AreEqual("SheetA", rule.SheetName);
            Assert.AreEqual(1, rule.OffsetParent.X);
            Assert.AreEqual(2, rule.OffsetParent.Y);
            Assert.AreEqual(3, rule.OffsetConsecutive.X);
            Assert.AreEqual(4, rule.OffsetConsecutive.Y);
            Assert.AreEqual(99, rule.MaxMatches);
            Assert.AreEqual(101, rule.MaxDepth);
            Assert.AreEqual(SimDataMappingDirection.Vertical, rule.MappingDirection);
            Assert.AreEqual(SimDataMappingReferencePoint.TopRight, rule.ReferencePoint);
            Assert.AreEqual(SimDataMappingParameterRange.Table, rule.ParameterRange);

            Assert.AreEqual(7, rule.Properties.Count);
            Assert.AreEqual(SimDataMappingParameterMappingProperties.Name, rule.Properties[0]);
            Assert.AreEqual(SimDataMappingParameterMappingProperties.Id, rule.Properties[1]);
            Assert.AreEqual(SimDataMappingParameterMappingProperties.Value, rule.Properties[2]);
            Assert.AreEqual(SimDataMappingParameterMappingProperties.Description, rule.Properties[3]);
            Assert.AreEqual(SimDataMappingParameterMappingProperties.Unit, rule.Properties[4]);
            Assert.AreEqual(SimDataMappingParameterMappingProperties.Min, rule.Properties[5]);
            Assert.AreEqual(SimDataMappingParameterMappingProperties.Max, rule.Properties[6]);

            Assert.AreEqual(4, rule.Filter.Count);
            Assert.AreEqual(SimDataMappingParameterFilterProperties.Name, rule.Filter[0].Property);
            Assert.AreEqual("filter name", rule.Filter[0].Value);
            Assert.AreEqual(SimDataMappingParameterFilterProperties.Unit, rule.Filter[1].Property);
            Assert.AreEqual("m/c", rule.Filter[1].Value);
            Assert.AreEqual(SimDataMappingParameterFilterProperties.Propagation, rule.Filter[2].Property);
            Assert.AreEqual(SimInfoFlow.Mixed, rule.Filter[2].Value);
            Assert.AreEqual(SimDataMappingParameterFilterProperties.Category, rule.Filter[3].Property);
            Assert.AreEqual(SimCategory.Humidity | SimCategory.Air, rule.Filter[3].Value);
        }


        [TestMethod]
        public void WriteDataMappingRuleInstance()
        {
            var rule = new SimDataMappingRuleInstance("SheetA")
            {
                Name = "Demo Rule",
                OffsetParent = new Utils.IntIndex2D(1, 2),
                OffsetConsecutive = new Utils.IntIndex2D(3, 4),
                MaxMatches = 99,
                MaxDepth = 101,
                MappingDirection = SimDataMappingDirection.Vertical,
                ReferencePoint = SimDataMappingReferencePoint.TopRight,
            };
            rule.Properties.Add(SimDataMappingInstanceMappingProperties.Name);
            rule.Properties.Add(SimDataMappingInstanceMappingProperties.Id);

            rule.Filter.Add(new SimDataMappingFilterInstance(SimDataMappingInstanceFilterProperties.Name, "filter name"));
            rule.Filter.Add(new SimDataMappingFilterInstance(SimDataMappingInstanceFilterProperties.InstanceType, SimInstanceType.AttributesFace));

            string exportedString = null;
            using (MemoryStream stream = new MemoryStream())
            {
                using (DXFStreamWriter writer = new DXFStreamWriter(stream, true))
                {
                    ExcelDxfIO.WriteDataMappingRule(rule, writer);
                }

                stream.Flush();
                stream.Position = 0;

                var array = stream.ToArray();
                exportedString = Encoding.UTF8.GetString(array);
            }

            AssertUtil.AreEqualMultiline(Resources.DXFSerializer_ETDXF_WriteDMRuleInstance, exportedString);
        }

        [TestMethod]
        public void WriteDataMappingRuleInstanceChildren()
        {
            var rule = new SimDataMappingRuleInstance("SheetA")
            {
                Name = "Demo Rule",
                OffsetParent = new Utils.IntIndex2D(1, 2),
                OffsetConsecutive = new Utils.IntIndex2D(3, 4),
                MaxMatches = 99,
                MaxDepth = 101,
                MappingDirection = SimDataMappingDirection.Vertical,
                ReferencePoint = SimDataMappingReferencePoint.TopRight,
            };
            rule.Properties.Add(SimDataMappingInstanceMappingProperties.Name);
            rule.Properties.Add(SimDataMappingInstanceMappingProperties.Id);

            rule.Filter.Add(new SimDataMappingFilterInstance(SimDataMappingInstanceFilterProperties.Name, "filter name"));
            rule.Filter.Add(new SimDataMappingFilterInstance(SimDataMappingInstanceFilterProperties.InstanceType, SimInstanceType.AttributesFace));

            var childRule1 = new SimDataMappingRuleComponent("SheetB")
            {
                Name = "Child Component Rule"
            };
            childRule1.Properties.Add(SimDataMappingComponentMappingProperties.Name);
            childRule1.Filter.Add(new SimDataMappingFilterComponent(SimDataMappingComponentFilterProperties.Name, new Regex("a.*")));

            var childRule2 = new SimDataMappingRuleParameter("SheetB")
            {
                Name = "Child Parameter Rule"
            };
            childRule2.Properties.Add(SimDataMappingParameterMappingProperties.Value);
            childRule2.Filter.Add(new SimDataMappingFilterParameter(SimDataMappingParameterFilterProperties.Unit, "m/c"));

            rule.Rules.Add(childRule1);
            rule.Rules.Add(childRule2);

            string exportedString = null;
            using (MemoryStream stream = new MemoryStream())
            {
                using (DXFStreamWriter writer = new DXFStreamWriter(stream, true))
                {
                    ExcelDxfIO.WriteDataMappingRule(rule, writer);
                }

                stream.Flush();
                stream.Position = 0;

                var array = stream.ToArray();
                exportedString = Encoding.UTF8.GetString(array);
            }

            AssertUtil.AreEqualMultiline(Resources.DXFSerializer_ETDXF_WriteDMRuleInstanceChildren, exportedString);
        }

        [TestMethod]
        public void ReadDataMappingRuleInstanceV21()
        {
            Guid guid = Guid.NewGuid();
            Guid otherguid = new Guid("98478ed1-d3f4-4873-95b6-412e5e23aac5");

            ExtendedProjectData projectData = new ExtendedProjectData();
            projectData.SetCallingLocation(new DummyReferenceLocation(guid));

            SimDataMappingRuleInstance rule = null;

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_ETDXF_ReadDMRuleInstanceV21)))
            {
                var info = new DXFParserInfo(guid, projectData);
                info.FileVersion = 21;

                reader.Read();

                rule = ExcelDxfIO.InstanceRuleEntityElement.Parse(reader, info);
            }

            HierarchicalProject.LoadDefaultTaxonomies(projectData);
            projectData.Components.RestoreDefaultTaxonomyReferences();

            Assert.IsNotNull(rule);

            Assert.AreEqual("Demo Rule", rule.Name);
            Assert.AreEqual("SheetA", rule.SheetName);
            Assert.AreEqual(1, rule.OffsetParent.X);
            Assert.AreEqual(2, rule.OffsetParent.Y);
            Assert.AreEqual(3, rule.OffsetConsecutive.X);
            Assert.AreEqual(4, rule.OffsetConsecutive.Y);
            Assert.AreEqual(99, rule.MaxMatches);
            Assert.AreEqual(101, rule.MaxDepth);
            Assert.AreEqual(SimDataMappingDirection.Vertical, rule.MappingDirection);
            Assert.AreEqual(SimDataMappingReferencePoint.TopRight, rule.ReferencePoint);

            Assert.AreEqual(2, rule.Properties.Count);
            Assert.AreEqual(SimDataMappingInstanceMappingProperties.Name, rule.Properties[0]);
            Assert.AreEqual(SimDataMappingInstanceMappingProperties.Id, rule.Properties[1]);

            Assert.AreEqual(2, rule.Filter.Count);
            Assert.AreEqual(SimDataMappingInstanceFilterProperties.Name, rule.Filter[0].Property);
            Assert.AreEqual("filter name", rule.Filter[0].Value);
            Assert.AreEqual(SimDataMappingInstanceFilterProperties.InstanceType, rule.Filter[1].Property);
            Assert.AreEqual(SimInstanceType.AttributesFace, rule.Filter[1].Value);
        }

        [TestMethod]
        public void ReadDataMappingRuleInstanceChildrenV21()
        {
            Guid guid = Guid.NewGuid();
            Guid otherguid = new Guid("98478ed1-d3f4-4873-95b6-412e5e23aac5");

            ExtendedProjectData projectData = new ExtendedProjectData();
            projectData.SetCallingLocation(new DummyReferenceLocation(guid));

            SimDataMappingRuleInstance rule = null;

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_ETDXF_ReadDMRuleInstanceChildrenV21)))
            {
                var info = new DXFParserInfo(guid, projectData);
                info.FileVersion = 21;

                reader.Read();

                rule = ExcelDxfIO.InstanceRuleEntityElement.Parse(reader, info);
            }

            HierarchicalProject.LoadDefaultTaxonomies(projectData);
            projectData.Components.RestoreDefaultTaxonomyReferences();

            Assert.IsNotNull(rule);

            Assert.AreEqual("Demo Rule", rule.Name);
            Assert.AreEqual("SheetA", rule.SheetName);
            Assert.AreEqual(1, rule.OffsetParent.X);
            Assert.AreEqual(2, rule.OffsetParent.Y);
            Assert.AreEqual(3, rule.OffsetConsecutive.X);
            Assert.AreEqual(4, rule.OffsetConsecutive.Y);
            Assert.AreEqual(99, rule.MaxMatches);
            Assert.AreEqual(101, rule.MaxDepth);
            Assert.AreEqual(SimDataMappingDirection.Vertical, rule.MappingDirection);
            Assert.AreEqual(SimDataMappingReferencePoint.TopRight, rule.ReferencePoint);

            Assert.AreEqual(2, rule.Properties.Count);
            Assert.AreEqual(SimDataMappingInstanceMappingProperties.Name, rule.Properties[0]);
            Assert.AreEqual(SimDataMappingInstanceMappingProperties.Id, rule.Properties[1]);

            Assert.AreEqual(2, rule.Filter.Count);
            Assert.AreEqual(SimDataMappingInstanceFilterProperties.Name, rule.Filter[0].Property);
            Assert.AreEqual("filter name", rule.Filter[0].Value);
            Assert.AreEqual(SimDataMappingInstanceFilterProperties.InstanceType, rule.Filter[1].Property);
            Assert.AreEqual(SimInstanceType.AttributesFace, rule.Filter[1].Value);

            //Children
            Assert.AreEqual(2, rule.Rules.Count);
            Assert.IsTrue(rule.Rules[0] is SimDataMappingRuleComponent);
            Assert.IsTrue(rule.Rules[1] is SimDataMappingRuleParameter);
        }


        [TestMethod]
        public void WriteDataMappingRuleFace()
        {
            var rule = new SimDataMappingRuleFace("SheetA")
            {
                Name = "Demo Rule",
                OffsetParent = new Utils.IntIndex2D(1, 2),
                OffsetConsecutive = new Utils.IntIndex2D(3, 4),
                MaxMatches = 99,
                MaxDepth = 101,
                MappingDirection = SimDataMappingDirection.Vertical,
                ReferencePoint = SimDataMappingReferencePoint.TopRight,
            };
            rule.Properties.Add(SimDataMappingFaceMappingProperties.Name);
            rule.Properties.Add(SimDataMappingFaceMappingProperties.Id);
            rule.Properties.Add(SimDataMappingFaceMappingProperties.Area);
            rule.Properties.Add(SimDataMappingFaceMappingProperties.Incline);
            rule.Properties.Add(SimDataMappingFaceMappingProperties.Orientation);

            rule.Filter.Add(new SimDataMappingFilterFace(SimDataMappingFaceFilterProperties.Name, "filter name"));
            rule.Filter.Add(new SimDataMappingFilterFace(SimDataMappingFaceFilterProperties.FaceType, SimDataMappingFaceType.FloorOrCeiling));
            rule.Filter.Add(new SimDataMappingFilterFace(SimDataMappingFaceFilterProperties.FileKey, 33));

            string exportedString = null;
            using (MemoryStream stream = new MemoryStream())
            {
                using (DXFStreamWriter writer = new DXFStreamWriter(stream, true))
                {
                    ExcelDxfIO.WriteDataMappingRule(rule, writer);
                }

                stream.Flush();
                stream.Position = 0;

                var array = stream.ToArray();
                exportedString = Encoding.UTF8.GetString(array);
            }

            AssertUtil.AreEqualMultiline(Resources.DXFSerializer_ETDXF_WriteDMRuleFace, exportedString);
        }

        [TestMethod]
        public void WriteDataMappingRuleFaceChildren()
        {
            var rule = new SimDataMappingRuleFace("SheetA")
            {
                Name = "Demo Rule",
                OffsetParent = new Utils.IntIndex2D(1, 2),
                OffsetConsecutive = new Utils.IntIndex2D(3, 4),
                MaxMatches = 99,
                MaxDepth = 101,
                MappingDirection = SimDataMappingDirection.Vertical,
                ReferencePoint = SimDataMappingReferencePoint.TopRight,
            };
            rule.Properties.Add(SimDataMappingFaceMappingProperties.Name);
            rule.Properties.Add(SimDataMappingFaceMappingProperties.Id);
            rule.Properties.Add(SimDataMappingFaceMappingProperties.Area);
            rule.Properties.Add(SimDataMappingFaceMappingProperties.Incline);
            rule.Properties.Add(SimDataMappingFaceMappingProperties.Orientation);

            rule.Filter.Add(new SimDataMappingFilterFace(SimDataMappingFaceFilterProperties.Name, "filter name"));
            rule.Filter.Add(new SimDataMappingFilterFace(SimDataMappingFaceFilterProperties.FaceType, SimDataMappingFaceType.FloorOrCeiling));
            rule.Filter.Add(new SimDataMappingFilterFace(SimDataMappingFaceFilterProperties.FileKey, 33));

            var childRule1 = new SimDataMappingRuleComponent("SheetB")
            {
                Name = "Child Component Rule"
            };
            childRule1.Properties.Add(SimDataMappingComponentMappingProperties.Name);
            childRule1.Filter.Add(new SimDataMappingFilterComponent(SimDataMappingComponentFilterProperties.Name, new Regex("a.*")));

            var childRule2 = new SimDataMappingRuleInstance("SheetB")
            {
                Name = "Child Instance Rule"
            };
            childRule2.Properties.Add(SimDataMappingInstanceMappingProperties.Name);
            childRule2.Filter.Add(new SimDataMappingFilterInstance(SimDataMappingInstanceFilterProperties.Name, new Regex("b.*")));

            var childRule3 = new SimDataMappingRuleFace("SheetB")
            {
                Name = "Child Face Rule"
            };
            childRule3.Properties.Add(SimDataMappingFaceMappingProperties.Name);
            childRule3.Filter.Add(new SimDataMappingFilterFace(SimDataMappingFaceFilterProperties.Name, new Regex("c.*")));

            rule.Rules.Add(childRule1);
            rule.Rules.Add(childRule2);
            rule.Rules.Add(childRule3);

            string exportedString = null;
            using (MemoryStream stream = new MemoryStream())
            {
                using (DXFStreamWriter writer = new DXFStreamWriter(stream, true))
                {
                    ExcelDxfIO.WriteDataMappingRule(rule, writer);
                }

                stream.Flush();
                stream.Position = 0;

                var array = stream.ToArray();
                exportedString = Encoding.UTF8.GetString(array);
            }

            AssertUtil.AreEqualMultiline(Resources.DXFSerializer_ETDXF_WriteDMRuleFaceChildren, exportedString);
        }

        [TestMethod]
        public void ReadDataMappingRuleFaceV21()
        {
            Guid guid = Guid.NewGuid();
            Guid otherguid = new Guid("98478ed1-d3f4-4873-95b6-412e5e23aac5");

            ExtendedProjectData projectData = new ExtendedProjectData();
            projectData.SetCallingLocation(new DummyReferenceLocation(guid));

            SimDataMappingRuleFace rule = null;

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_ETDXF_ReadDMRuleFaceV21)))
            {
                var info = new DXFParserInfo(guid, projectData);
                info.FileVersion = 21;

                reader.Read();

                rule = ExcelDxfIO.FaceRuleEntityElement.Parse(reader, info);
            }

            HierarchicalProject.LoadDefaultTaxonomies(projectData);
            projectData.Components.RestoreDefaultTaxonomyReferences();

            Assert.IsNotNull(rule);

            Assert.AreEqual("Demo Rule", rule.Name);
            Assert.AreEqual("SheetA", rule.SheetName);
            Assert.AreEqual(1, rule.OffsetParent.X);
            Assert.AreEqual(2, rule.OffsetParent.Y);
            Assert.AreEqual(3, rule.OffsetConsecutive.X);
            Assert.AreEqual(4, rule.OffsetConsecutive.Y);
            Assert.AreEqual(99, rule.MaxMatches);
            Assert.AreEqual(101, rule.MaxDepth);
            Assert.AreEqual(SimDataMappingDirection.Vertical, rule.MappingDirection);
            Assert.AreEqual(SimDataMappingReferencePoint.TopRight, rule.ReferencePoint);

            Assert.AreEqual(5, rule.Properties.Count);
            Assert.AreEqual(SimDataMappingFaceMappingProperties.Name, rule.Properties[0]);
            Assert.AreEqual(SimDataMappingFaceMappingProperties.Id, rule.Properties[1]);
            Assert.AreEqual(SimDataMappingFaceMappingProperties.Area, rule.Properties[2]);
            Assert.AreEqual(SimDataMappingFaceMappingProperties.Incline, rule.Properties[3]);
            Assert.AreEqual(SimDataMappingFaceMappingProperties.Orientation, rule.Properties[4]);

            Assert.AreEqual(3, rule.Filter.Count);
            Assert.AreEqual(SimDataMappingFaceFilterProperties.Name, rule.Filter[0].Property);
            Assert.AreEqual("filter name", rule.Filter[0].Value);
            Assert.AreEqual(SimDataMappingFaceFilterProperties.FaceType, rule.Filter[1].Property);
            Assert.AreEqual(SimDataMappingFaceType.FloorOrCeiling, rule.Filter[1].Value);
            Assert.AreEqual(SimDataMappingFaceFilterProperties.FileKey, rule.Filter[2].Property);
            Assert.AreEqual(33, rule.Filter[2].Value);
        }

        [TestMethod]
        public void ReadDataMappingRuleFaceChildrenV21()
        {
            Guid guid = Guid.NewGuid();
            Guid otherguid = new Guid("98478ed1-d3f4-4873-95b6-412e5e23aac5");

            ExtendedProjectData projectData = new ExtendedProjectData();
            projectData.SetCallingLocation(new DummyReferenceLocation(guid));

            SimDataMappingRuleFace rule = null;

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_ETDXF_ReadDMRuleFaceChildrenV21)))
            {
                var info = new DXFParserInfo(guid, projectData);
                info.FileVersion = 21;

                reader.Read();

                rule = ExcelDxfIO.FaceRuleEntityElement.Parse(reader, info);
            }

            HierarchicalProject.LoadDefaultTaxonomies(projectData);
            projectData.Components.RestoreDefaultTaxonomyReferences();

            Assert.IsNotNull(rule);

            Assert.AreEqual("Demo Rule", rule.Name);
            Assert.AreEqual("SheetA", rule.SheetName);
            Assert.AreEqual(1, rule.OffsetParent.X);
            Assert.AreEqual(2, rule.OffsetParent.Y);
            Assert.AreEqual(3, rule.OffsetConsecutive.X);
            Assert.AreEqual(4, rule.OffsetConsecutive.Y);
            Assert.AreEqual(99, rule.MaxMatches);
            Assert.AreEqual(101, rule.MaxDepth);
            Assert.AreEqual(SimDataMappingDirection.Vertical, rule.MappingDirection);
            Assert.AreEqual(SimDataMappingReferencePoint.TopRight, rule.ReferencePoint);

            Assert.AreEqual(5, rule.Properties.Count);
            Assert.AreEqual(SimDataMappingFaceMappingProperties.Name, rule.Properties[0]);
            Assert.AreEqual(SimDataMappingFaceMappingProperties.Id, rule.Properties[1]);
            Assert.AreEqual(SimDataMappingFaceMappingProperties.Area, rule.Properties[2]);
            Assert.AreEqual(SimDataMappingFaceMappingProperties.Incline, rule.Properties[3]);
            Assert.AreEqual(SimDataMappingFaceMappingProperties.Orientation, rule.Properties[4]);

            Assert.AreEqual(3, rule.Filter.Count);
            Assert.AreEqual(SimDataMappingFaceFilterProperties.Name, rule.Filter[0].Property);
            Assert.AreEqual("filter name", rule.Filter[0].Value);
            Assert.AreEqual(SimDataMappingFaceFilterProperties.FaceType, rule.Filter[1].Property);
            Assert.AreEqual(SimDataMappingFaceType.FloorOrCeiling, rule.Filter[1].Value);
            Assert.AreEqual(SimDataMappingFaceFilterProperties.FileKey, rule.Filter[2].Property);
            Assert.AreEqual(33, rule.Filter[2].Value);

            //Children
            Assert.AreEqual(3, rule.Rules.Count);
            Assert.IsTrue(rule.Rules[0] is SimDataMappingRuleComponent);
            Assert.IsTrue(rule.Rules[1] is SimDataMappingRuleInstance);
            Assert.IsTrue(rule.Rules[2] is SimDataMappingRuleFace);
        }


        [TestMethod]
        public void WriteDataMappingRuleVolume()
        {
            var rule = new SimDataMappingRuleVolume("SheetA")
            {
                Name = "Demo Rule",
                OffsetParent = new Utils.IntIndex2D(1, 2),
                OffsetConsecutive = new Utils.IntIndex2D(3, 4),
                MaxMatches = 99,
                MaxDepth = 101,
                MappingDirection = SimDataMappingDirection.Vertical,
                ReferencePoint = SimDataMappingReferencePoint.TopRight,
            };
            rule.Properties.Add(SimDataMappingVolumeMappingProperties.Name);
            rule.Properties.Add(SimDataMappingVolumeMappingProperties.Id);
            rule.Properties.Add(SimDataMappingVolumeMappingProperties.Volume);
            rule.Properties.Add(SimDataMappingVolumeMappingProperties.FloorArea);
            rule.Properties.Add(SimDataMappingVolumeMappingProperties.Height);
            rule.Properties.Add(SimDataMappingVolumeMappingProperties.CeilingElevation);
            rule.Properties.Add(SimDataMappingVolumeMappingProperties.FloorElevation);

            rule.Filter.Add(new SimDataMappingFilterVolume(SimDataMappingVolumeFilterProperties.Name, "filter name"));
            rule.Filter.Add(new SimDataMappingFilterVolume(SimDataMappingVolumeFilterProperties.FileKey, 33));

            string exportedString = null;
            using (MemoryStream stream = new MemoryStream())
            {
                using (DXFStreamWriter writer = new DXFStreamWriter(stream, true))
                {
                    ExcelDxfIO.WriteDataMappingRule(rule, writer);
                }

                stream.Flush();
                stream.Position = 0;

                var array = stream.ToArray();
                exportedString = Encoding.UTF8.GetString(array);
            }

            AssertUtil.AreEqualMultiline(Resources.DXFSerializer_ETDXF_WriteDMRuleVolume, exportedString);
        }

        [TestMethod]
        public void WriteDataMappingRuleVolumeChildren()
        {
            var rule = new SimDataMappingRuleVolume("SheetA")
            {
                Name = "Demo Rule",
                OffsetParent = new Utils.IntIndex2D(1, 2),
                OffsetConsecutive = new Utils.IntIndex2D(3, 4),
                MaxMatches = 99,
                MaxDepth = 101,
                MappingDirection = SimDataMappingDirection.Vertical,
                ReferencePoint = SimDataMappingReferencePoint.TopRight,
            };
            rule.Properties.Add(SimDataMappingVolumeMappingProperties.Name);
            rule.Properties.Add(SimDataMappingVolumeMappingProperties.Id);
            rule.Properties.Add(SimDataMappingVolumeMappingProperties.Volume);
            rule.Properties.Add(SimDataMappingVolumeMappingProperties.FloorArea);
            rule.Properties.Add(SimDataMappingVolumeMappingProperties.Height);
            rule.Properties.Add(SimDataMappingVolumeMappingProperties.CeilingElevation);
            rule.Properties.Add(SimDataMappingVolumeMappingProperties.FloorElevation);

            rule.Filter.Add(new SimDataMappingFilterVolume(SimDataMappingVolumeFilterProperties.Name, "filter name"));
            rule.Filter.Add(new SimDataMappingFilterVolume(SimDataMappingVolumeFilterProperties.FileKey, 33));

            var childRule1 = new SimDataMappingRuleComponent("SheetB")
            {
                Name = "Child Component Rule"
            };
            childRule1.Properties.Add(SimDataMappingComponentMappingProperties.Name);
            childRule1.Filter.Add(new SimDataMappingFilterComponent(SimDataMappingComponentFilterProperties.Name, new Regex("a.*")));

            var childRule2 = new SimDataMappingRuleInstance("SheetB")
            {
                Name = "Child Instance Rule"
            };
            childRule2.Properties.Add(SimDataMappingInstanceMappingProperties.Name);
            childRule2.Filter.Add(new SimDataMappingFilterInstance(SimDataMappingInstanceFilterProperties.Name, new Regex("b.*")));

            var childRule3 = new SimDataMappingRuleFace("SheetB")
            {
                Name = "Child Face Rule"
            };
            childRule3.Properties.Add(SimDataMappingFaceMappingProperties.Name);
            childRule3.Filter.Add(new SimDataMappingFilterFace(SimDataMappingFaceFilterProperties.Name, new Regex("c.*")));

            rule.Rules.Add(childRule1);
            rule.Rules.Add(childRule2);
            rule.Rules.Add(childRule3);

            string exportedString = null;
            using (MemoryStream stream = new MemoryStream())
            {
                using (DXFStreamWriter writer = new DXFStreamWriter(stream, true))
                {
                    ExcelDxfIO.WriteDataMappingRule(rule, writer);
                }

                stream.Flush();
                stream.Position = 0;

                var array = stream.ToArray();
                exportedString = Encoding.UTF8.GetString(array);
            }

            AssertUtil.AreEqualMultiline(Resources.DXFSerializer_ETDXF_WriteDMRuleVolumeChildren, exportedString);
        }

        [TestMethod]
        public void ReadDataMappingRuleVolumeV21()
        {
            Guid guid = Guid.NewGuid();
            Guid otherguid = new Guid("98478ed1-d3f4-4873-95b6-412e5e23aac5");

            ExtendedProjectData projectData = new ExtendedProjectData();
            projectData.SetCallingLocation(new DummyReferenceLocation(guid));

            SimDataMappingRuleVolume rule = null;

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_ETDXF_ReadDMRuleVolumeV21)))
            {
                var info = new DXFParserInfo(guid, projectData);
                info.FileVersion = 21;

                reader.Read();

                rule = ExcelDxfIO.VolumeRuleEntityElement.Parse(reader, info);
            }

            HierarchicalProject.LoadDefaultTaxonomies(projectData);
            projectData.Components.RestoreDefaultTaxonomyReferences();

            Assert.IsNotNull(rule);

            Assert.AreEqual("Demo Rule", rule.Name);
            Assert.AreEqual("SheetA", rule.SheetName);
            Assert.AreEqual(1, rule.OffsetParent.X);
            Assert.AreEqual(2, rule.OffsetParent.Y);
            Assert.AreEqual(3, rule.OffsetConsecutive.X);
            Assert.AreEqual(4, rule.OffsetConsecutive.Y);
            Assert.AreEqual(99, rule.MaxMatches);
            Assert.AreEqual(101, rule.MaxDepth);
            Assert.AreEqual(SimDataMappingDirection.Vertical, rule.MappingDirection);
            Assert.AreEqual(SimDataMappingReferencePoint.TopRight, rule.ReferencePoint);

            Assert.AreEqual(7, rule.Properties.Count);
            Assert.AreEqual(SimDataMappingVolumeMappingProperties.Name, rule.Properties[0]);
            Assert.AreEqual(SimDataMappingVolumeMappingProperties.Id, rule.Properties[1]);
            Assert.AreEqual(SimDataMappingVolumeMappingProperties.Volume, rule.Properties[2]);
            Assert.AreEqual(SimDataMappingVolumeMappingProperties.FloorArea, rule.Properties[3]);
            Assert.AreEqual(SimDataMappingVolumeMappingProperties.Height, rule.Properties[4]);
            Assert.AreEqual(SimDataMappingVolumeMappingProperties.CeilingElevation, rule.Properties[5]);
            Assert.AreEqual(SimDataMappingVolumeMappingProperties.FloorElevation, rule.Properties[6]);

            Assert.AreEqual(2, rule.Filter.Count);
            Assert.AreEqual(SimDataMappingVolumeFilterProperties.Name, rule.Filter[0].Property);
            Assert.AreEqual("filter name", rule.Filter[0].Value);
            Assert.AreEqual(SimDataMappingVolumeFilterProperties.FileKey, rule.Filter[1].Property);
            Assert.AreEqual(33, rule.Filter[1].Value);
        }

        [TestMethod]
        public void ReadDataMappingRuleVolumeChildrenV21()
        {
            Guid guid = Guid.NewGuid();
            Guid otherguid = new Guid("98478ed1-d3f4-4873-95b6-412e5e23aac5");

            ExtendedProjectData projectData = new ExtendedProjectData();
            projectData.SetCallingLocation(new DummyReferenceLocation(guid));

            SimDataMappingRuleVolume rule = null;

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_ETDXF_ReadDMRuleVolumeChildrenV21)))
            {
                var info = new DXFParserInfo(guid, projectData);
                info.FileVersion = 21;

                reader.Read();

                rule = ExcelDxfIO.VolumeRuleEntityElement.Parse(reader, info);
            }

            HierarchicalProject.LoadDefaultTaxonomies(projectData);
            projectData.Components.RestoreDefaultTaxonomyReferences();

            Assert.IsNotNull(rule);

            Assert.AreEqual("Demo Rule", rule.Name);
            Assert.AreEqual("SheetA", rule.SheetName);
            Assert.AreEqual(1, rule.OffsetParent.X);
            Assert.AreEqual(2, rule.OffsetParent.Y);
            Assert.AreEqual(3, rule.OffsetConsecutive.X);
            Assert.AreEqual(4, rule.OffsetConsecutive.Y);
            Assert.AreEqual(99, rule.MaxMatches);
            Assert.AreEqual(101, rule.MaxDepth);
            Assert.AreEqual(SimDataMappingDirection.Vertical, rule.MappingDirection);
            Assert.AreEqual(SimDataMappingReferencePoint.TopRight, rule.ReferencePoint);

            Assert.AreEqual(7, rule.Properties.Count);
            Assert.AreEqual(SimDataMappingVolumeMappingProperties.Name, rule.Properties[0]);
            Assert.AreEqual(SimDataMappingVolumeMappingProperties.Id, rule.Properties[1]);
            Assert.AreEqual(SimDataMappingVolumeMappingProperties.Volume, rule.Properties[2]);
            Assert.AreEqual(SimDataMappingVolumeMappingProperties.FloorArea, rule.Properties[3]);
            Assert.AreEqual(SimDataMappingVolumeMappingProperties.Height, rule.Properties[4]);
            Assert.AreEqual(SimDataMappingVolumeMappingProperties.CeilingElevation, rule.Properties[5]);
            Assert.AreEqual(SimDataMappingVolumeMappingProperties.FloorElevation, rule.Properties[6]);

            Assert.AreEqual(2, rule.Filter.Count);
            Assert.AreEqual(SimDataMappingVolumeFilterProperties.Name, rule.Filter[0].Property);
            Assert.AreEqual("filter name", rule.Filter[0].Value);
            Assert.AreEqual(SimDataMappingVolumeFilterProperties.FileKey, rule.Filter[1].Property);
            Assert.AreEqual(33, rule.Filter[1].Value);

            //Children
            Assert.AreEqual(3, rule.Rules.Count);
            Assert.IsTrue(rule.Rules[0] is SimDataMappingRuleComponent);
            Assert.IsTrue(rule.Rules[1] is SimDataMappingRuleInstance);
            Assert.IsTrue(rule.Rules[2] is SimDataMappingRuleFace);
        }

        #endregion

        #region Data Mapping Read Rule

        [TestMethod]
        public void WriteDataMappingReadRule()
        {
            Guid guid = new Guid();
            ExtendedProjectData projectData = new ExtendedProjectData();
            projectData.SetCallingLocation(new DummyReferenceLocation(guid));

            SimComponent component = new SimComponent();

            SimDoubleParameter parameter = new SimDoubleParameter("Param", "unit", 3.4)
            {
                Id = new SimId(guid, 5566)
            };
            component.Parameters.Add(parameter);
            projectData.Components.StartLoading();
            projectData.Components.Add(component);
            projectData.Components.EndLoading();


            var rule = new SimDataMappingReadRule()
            {
                SheetName = "SheetA",
                Range = new Utils.RowColumnRange(4,3,6,5),
                Parameter = parameter,
            };

            SimDataMappingTool tool = new SimDataMappingTool("tool");
            tool.ReadRules.Add(rule);
            projectData.DataMappingTools.Add(tool);

            string exportedString = null;
            using (MemoryStream stream = new MemoryStream())
            {
                using (DXFStreamWriter writer = new DXFStreamWriter(stream, true))
                {
                    ExcelDxfIO.WriteReadRule(rule, writer);
                }

                stream.Flush();
                stream.Position = 0;

                var array = stream.ToArray();
                exportedString = Encoding.UTF8.GetString(array);
            }

            AssertUtil.AreEqualMultiline(Resources.DXFSerializer_ETDXF_WriteDMReadRule, exportedString);
        }

        [TestMethod]
        public void ReadDataMappingReadRuleV21()
        {
            Guid guid = new Guid();
            ExtendedProjectData projectData = new ExtendedProjectData();
            projectData.SetCallingLocation(new DummyReferenceLocation(guid));

            SimComponent component = new SimComponent();

            SimDoubleParameter parameter = new SimDoubleParameter("Param", "unit", 3.4)
            {
                Id = new SimId(guid, 5566)
            };
            component.Parameters.Add(parameter);

            projectData.Components.StartLoading();
            projectData.Components.Add(component);
            projectData.Components.EndLoading();

            SimDataMappingReadRule rule = null;

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_ETDXF_ReadDMReadRuleV21)))
            {
                var info = new DXFParserInfo(guid, projectData);
                info.FileVersion = 21;

                reader.Read();

                rule = ExcelDxfIO.DataMappingReadRuleEntityElement.Parse(reader, info);
            }

            Assert.IsNotNull(rule);
            Assert.AreEqual(parameter, rule.Parameter);
            Assert.AreEqual("SheetA", rule.SheetName);
            Assert.AreEqual(3, rule.Range.ColumnStart);
            Assert.AreEqual(4, rule.Range.RowStart);
            Assert.AreEqual(5, rule.Range.ColumnCount);
            Assert.AreEqual(6, rule.Range.RowCount);
        }

        #endregion

        #region Filter Value

        [TestMethod]
        public void FilterValueString()
        {
            string exportedString = null;
            using (MemoryStream stream = new MemoryStream())
            {
                using (DXFStreamWriter writer = new DXFStreamWriter(stream, true))
                {
                    ExcelDxfIO.WriteFilterValue("stringvalue", writer);
                }

                stream.Flush();
                stream.Position = 0;

                var array = stream.ToArray();
                exportedString = Encoding.UTF8.GetString(array);
            }

            AssertUtil.AreEqualMultiline(
                "6017\r\n"+
                "0\r\n" +
                "6018\r\n" +
                "stringvalue\r\n"
                , exportedString);
        }

        [TestMethod]
        public void FilterValueRegex()
        {
            string exportedString = null;
            using (MemoryStream stream = new MemoryStream())
            {
                using (DXFStreamWriter writer = new DXFStreamWriter(stream, true))
                {
                    ExcelDxfIO.WriteFilterValue(new Regex(".*asdf.?"), writer);
                }

                stream.Flush();
                stream.Position = 0;

                var array = stream.ToArray();
                exportedString = Encoding.UTF8.GetString(array);
            }

            AssertUtil.AreEqualMultiline(
                "6017\r\n" +
                "1\r\n" +
                "6018\r\n" +
                ".*asdf.?\r\n"
                , exportedString);
        }

        [TestMethod]
        public void FilterValueTaxonomyEntry()
        {
            string exportedString = null;
            using (MemoryStream stream = new MemoryStream())
            {
                using (DXFStreamWriter writer = new DXFStreamWriter(stream, true))
                {
                    ExcelDxfIO.WriteFilterValue(new SimTaxonomyEntryReference(new SimTaxonomyEntry(new SimId(5566))), writer);
                }

                stream.Flush();
                stream.Position = 0;

                var array = stream.ToArray();
                exportedString = Encoding.UTF8.GetString(array);
            }

            AssertUtil.AreEqualMultiline(
                "6017\r\n" +
                "2\r\n" +
                "6018\r\n" +
                "5566\r\n"
                , exportedString);
        }

        [TestMethod]
        public void FilterValueSlot()
        {
            string exportedString = null;
            using (MemoryStream stream = new MemoryStream())
            {
                using (DXFStreamWriter writer = new DXFStreamWriter(stream, true))
                {
                    ExcelDxfIO.WriteFilterValue(
                        new SimSlot(new SimTaxonomyEntry(new SimId(5566)), "exten")
                        , writer); ;
                }

                stream.Flush();
                stream.Position = 0;

                var array = stream.ToArray();
                exportedString = Encoding.UTF8.GetString(array);
            }

            AssertUtil.AreEqualMultiline(
                "6017\r\n" +
                "3\r\n" +
                "6019\r\n" +
                "exten\r\n" +
                "6018\r\n" +
                "5566\r\n"
                , exportedString);
        }

        [TestMethod]
        public void FilterValueSimInstanceType()
        {
            string exportedString = null;
            using (MemoryStream stream = new MemoryStream())
            {
                using (DXFStreamWriter writer = new DXFStreamWriter(stream, true))
                {
                    ExcelDxfIO.WriteFilterValue(SimInstanceType.Entity3D, writer);
                }

                stream.Flush();
                stream.Position = 0;

                var array = stream.ToArray();
                exportedString = Encoding.UTF8.GetString(array);
            }

            AssertUtil.AreEqualMultiline(
                "6017\r\n" +
                "4\r\n" +
                "6018\r\n" +
                "1\r\n"
                , exportedString);
        }

        [TestMethod]
        public void FilterValueBoolean()
        {
            string exportedString = null;
            using (MemoryStream stream = new MemoryStream())
            {
                using (DXFStreamWriter writer = new DXFStreamWriter(stream, true))
                {
                    ExcelDxfIO.WriteFilterValue(true, writer);
                }

                stream.Flush();
                stream.Position = 0;

                var array = stream.ToArray();
                exportedString = Encoding.UTF8.GetString(array);
            }

            AssertUtil.AreEqualMultiline(
                "6017\r\n" +
                "5\r\n" +
                "6018\r\n" +
                "1\r\n"
                , exportedString);
        }

        [TestMethod]
        public void FilterValueSimInfoFlow()
        {
            string exportedString = null;
            using (MemoryStream stream = new MemoryStream())
            {
                using (DXFStreamWriter writer = new DXFStreamWriter(stream, true))
                {
                    ExcelDxfIO.WriteFilterValue(SimInfoFlow.FromExternal, writer);
                }

                stream.Flush();
                stream.Position = 0;

                var array = stream.ToArray();
                exportedString = Encoding.UTF8.GetString(array);
            }

            AssertUtil.AreEqualMultiline(
                "6017\r\n" +
                "6\r\n" +
                "6018\r\n" +
                "6\r\n"
                , exportedString);
        }

        [TestMethod]
        public void FilterValueSimCategory()
        {
            string exportedString = null;
            using (MemoryStream stream = new MemoryStream())
            {
                using (DXFStreamWriter writer = new DXFStreamWriter(stream, true))
                {
                    ExcelDxfIO.WriteFilterValue(SimCategory.Humidity | SimCategory.Air, writer);
                }

                stream.Flush();
                stream.Position = 0;

                var array = stream.ToArray();
                exportedString = Encoding.UTF8.GetString(array);
            }

            AssertUtil.AreEqualMultiline(
                "6017\r\n" +
                "7\r\n" +
                "6018\r\n" +
                "192\r\n"
                , exportedString);
        }

        #endregion

        #region File

        [TestMethod]
        public void WriteExcelFile()
        {
            Guid guid = Guid.NewGuid();
            Guid otherguid = new Guid("98478ed1-d3f4-4873-95b6-412e5e23aac5");

            ExtendedProjectData projectData = new ExtendedProjectData();
            projectData.SetCallingLocation(new DummyReferenceLocation(guid));

            var tool = CreateTestTool(projectData);

            string exportedString = null;
            using (MemoryStream stream = new MemoryStream())
            {
                using (DXFStreamWriter writer = new DXFStreamWriter(stream, true))
                {
                    ExcelDxfIO.Write(writer, projectData);
                }

                stream.Flush();
                stream.Position = 0;

                var array = stream.ToArray();
                exportedString = Encoding.UTF8.GetString(array);
            }

            AssertUtil.AreEqualMultiline(Resources.DXFSerializer_ETDXF_Write, exportedString);
        }

        [TestMethod]
        public void ReadExcelFileV21()
        {
            Guid guid = Guid.NewGuid();
            var otherGuid = Guid.Parse("da7d8f7c-8eec-423b-b127-9d6e17f52522");

            ExtendedProjectData projectData = new ExtendedProjectData();
            var location = new DummyReferenceLocation(guid);
            projectData.SetCallingLocation(location);

            CreateTestProjectData(projectData);

            var demoTax = new SimTaxonomy("demotax");
            var taxEntry = new SimTaxonomyEntry(new SimId(5566)) { Key = "demokey" };
            projectData.Taxonomies.StartLoading();
            demoTax.Entries.Add(taxEntry);
            projectData.Taxonomies.Add(demoTax);
            projectData.Taxonomies.StopLoading();

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_ETDXF_ReadV21)))
            {
                var info = new DXFParserInfo(guid, projectData);
                ExcelDxfIO.Read(reader, info, false);
            }

            Assert.AreEqual(1, projectData.DataMappingTools.Count);
            CheckTestTool(projectData.DataMappingTools[0], projectData);
        }

        [TestMethod]
        public void ReadExcelFileV12()
        {
            Guid guid = Guid.NewGuid();
            var otherGuid = Guid.Parse("da7d8f7c-8eec-423b-b127-9d6e17f52522");

            ExtendedProjectData projectData = new ExtendedProjectData();
            var location = new DummyReferenceLocation(guid);
            projectData.SetCallingLocation(location);

            var comp = new SimComponent();
            var param = new SimDoubleParameter("A", "unit", 3.5) { Id = new SimId(location, 124) };
            comp.Parameters.Add(param);

            projectData.Components.StartLoading();
            projectData.Components.Add(comp);
            projectData.Components.EndLoading();

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_ETDXF_ReadV12)))
            {
                var info = new DXFParserInfo(guid, projectData);
                ExcelDxfIO.Read(reader, info, false);
            }

            Assert.AreEqual(0, projectData.DataMappingTools.Count);
        }

        [TestMethod]
        public void ReadExcelFileV11()
        {
            Guid guid = Guid.NewGuid();
            var otherGuid = Guid.Parse("da7d8f7c-8eec-423b-b127-9d6e17f52522");

            ExtendedProjectData projectData = new ExtendedProjectData();
            var location = new DummyReferenceLocation(guid);
            projectData.SetCallingLocation(location);

            var comp = new SimComponent();
            var param = new SimDoubleParameter("A", "unit", 3.5) { Id = new SimId(location, 124) };
            comp.Parameters.Add(param);

            projectData.Components.StartLoading();
            projectData.Components.Add(comp);
            projectData.Components.EndLoading();

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_ETDXF_ReadV11)))
            {
                var info = new DXFParserInfo(guid, projectData);
                info.FileVersion = 11;
                ExcelDxfIO.Read(reader, info, false);
            }

            Assert.AreEqual(0, projectData.DataMappingTools.Count);
        }

        [TestMethod]
        public void ReadExcelFileV6()
        {
            Guid guid = Guid.NewGuid();
            var otherGuid = Guid.Parse("da7d8f7c-8eec-423b-b127-9d6e17f52522");

            ExtendedProjectData projectData = new ExtendedProjectData();
            var location = new DummyReferenceLocation(guid);
            projectData.SetCallingLocation(location);

            var comp = new SimComponent();
            var param = new SimDoubleParameter("A", "unit", 3.5) { Id = new SimId(location, 124) };
            comp.Parameters.Add(param);

            projectData.Components.StartLoading();
            projectData.Components.Add(comp);
            projectData.Components.EndLoading();

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_ETDXF_ReadV6)))
            {
                var info = new DXFParserInfo(guid, projectData);
                info.FileVersion = 6;
                ExcelDxfIO.Read(reader, info, false);
            }

            Assert.AreEqual(0, projectData.DataMappingTools.Count);
        }

        [TestMethod]
        public void ReadExcelFileV4()
        {
            Guid guid = Guid.NewGuid();
            var otherGuid = Guid.Parse("da7d8f7c-8eec-423b-b127-9d6e17f52522");

            ExtendedProjectData projectData = new ExtendedProjectData();
            var location = new DummyReferenceLocation(guid);
            projectData.SetCallingLocation(location);

            var comp = new SimComponent();
            var param = new SimDoubleParameter("A", "unit", 3.5) { Id = new SimId(location, 1076741824) };
            comp.Parameters.Add(param);

            projectData.Components.StartLoading();
            projectData.Components.Add(comp);
            projectData.Components.EndLoading();

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_ETDXF_ReadV4)))
            {
                var info = new DXFParserInfo(guid, projectData);
                info.FileVersion = 4;
                ExcelDxfIO.Read(reader, info, false);
            }

            Assert.AreEqual(0, projectData.DataMappingTools.Count);
        }

        #endregion
    }
}
