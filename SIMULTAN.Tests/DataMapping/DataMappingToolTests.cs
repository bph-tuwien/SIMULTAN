using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data;
using SIMULTAN.Data.Components;
using SIMULTAN.Data.MultiValues;
using SIMULTAN.Data.Taxonomy;
using SIMULTAN.DataMapping;
using SIMULTAN.Projects;
using SIMULTAN.Tests.TestUtils;
using SIMULTAN.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SIMULTAN.Tests.DataMapping
{
    [TestClass]
    public class DataMappingToolTests : BaseProjectTest
    {
        private static readonly FileInfo emptyProject = new FileInfo(@"./EmptyProject.simultan");
        private static readonly FileInfo mappingProject = new FileInfo(@"./ExcelMappingTests.simultan");
        private static readonly FileInfo examplesProject = new FileInfo(@"./DataMappingDemo.simultan");

        [TestMethod]
        public void Ctor()
        {
            Assert.ThrowsException<ArgumentException>(() => { SimDataMappingTool invalidTool = new SimDataMappingTool(null); });
            Assert.ThrowsException<ArgumentException>(() => { SimDataMappingTool invalidTool = new SimDataMappingTool(""); });

            SimDataMappingTool tool = new SimDataMappingTool("asdf");

            Assert.AreEqual("asdf", tool.Name);
            Assert.AreEqual(SimId.Empty, tool.Id);
            Assert.AreEqual(null, tool.Factory);
        }

        [TestMethod]
        public void AddTool()
        {
            LoadProject(emptyProject);

            SimDataMappingTool tool = new SimDataMappingTool("asdf");

            this.projectData.DataMappingTools.Add(tool);

            Assert.AreNotEqual(0, tool.Id.LocalId);
            Assert.AreEqual(project, tool.Id.Location);
            Assert.AreEqual(projectData.DataMappingTools, tool.Factory);

            Assert.IsTrue(projectData.DataMappingTools.Contains(tool));
            Assert.AreEqual(tool, projectData.IdGenerator.GetById<SimDataMappingTool>(tool.Id));
        }

        [TestMethod]
        public void ClearTool()
        {
            LoadProject(emptyProject);

            SimDataMappingTool tool1 = new SimDataMappingTool("asdf");

            this.projectData.DataMappingTools.Add(tool1);
            var id = tool1.LocalID;

            this.projectData.DataMappingTools.Remove(tool1);

            Assert.AreEqual(id, tool1.Id.LocalId);
            Assert.AreEqual(null, tool1.Id.Location);
            Assert.AreEqual(null, tool1.Factory);

            Assert.IsFalse(projectData.DataMappingTools.Contains(tool1));
            Assert.AreEqual(null, projectData.IdGenerator.GetById<SimDataMappingTool>(tool1.Id));
        }

        [TestMethod]
        public void ReplaceTool()
        {
            LoadProject(emptyProject);

            SimDataMappingTool tool1 = new SimDataMappingTool("asdf");
            SimDataMappingTool tool2 = new SimDataMappingTool("asdf");

            this.projectData.DataMappingTools.Add(tool1);
            var id = tool1.LocalID;

            this.projectData.DataMappingTools[0] = tool2;

            Assert.AreEqual(id, tool1.Id.LocalId);
            Assert.AreEqual(null, tool1.Id.Location);
            Assert.AreEqual(null, tool1.Factory);

            Assert.IsFalse(projectData.DataMappingTools.Contains(tool1));
            Assert.AreEqual(null, projectData.IdGenerator.GetById<SimDataMappingTool>(tool1.Id));

            Assert.AreNotEqual(0, tool2.Id.LocalId);
            Assert.AreEqual(project, tool2.Id.Location);
            Assert.AreEqual(projectData.DataMappingTools, tool2.Factory);

            Assert.IsTrue(projectData.DataMappingTools.Contains(tool2));
            Assert.AreEqual(tool2, projectData.IdGenerator.GetById<SimDataMappingTool>(tool2.Id));
        }

        [TestMethod]
        public void RemoveTool()
        {
            LoadProject(emptyProject);

            SimDataMappingTool tool1 = new SimDataMappingTool("asdf");

            this.projectData.DataMappingTools.Add(tool1);
            var id = tool1.LocalID;

            this.projectData.DataMappingTools.Clear();

            Assert.AreEqual(id, tool1.Id.LocalId);
            Assert.AreEqual(null, tool1.Id.Location);
            Assert.AreEqual(null, tool1.Factory);

            Assert.IsFalse(projectData.DataMappingTools.Contains(tool1));
            Assert.AreEqual(null, projectData.IdGenerator.GetById<SimDataMappingTool>(tool1.Id));
        }


        #region Rules

        [TestMethod]
        public void AddMappingRule()
        {
            LoadProject(emptyProject);
            SimDataMappingTool tool = new SimDataMappingTool("asdf");

            var rule = new SimDataMappingRuleComponent("Sheet");
            Assert.IsNull(rule.Tool);

            tool.Rules.Add(rule);
            Assert.AreEqual(tool, rule.Tool);
        }

        [TestMethod]
        public void RemoveMappingRule()
        {
            LoadProject(emptyProject);
            SimDataMappingTool tool = new SimDataMappingTool("asdf");

            var rule = new SimDataMappingRuleComponent("Sheet");
            tool.Rules.Add(rule);

            tool.Rules.Remove(rule);
            Assert.AreEqual(null, rule.Tool);
        }

        [TestMethod]
        public void SetMappingRule()
        {
            LoadProject(emptyProject);
            SimDataMappingTool tool = new SimDataMappingTool("asdf");

            var rule = new SimDataMappingRuleComponent("Sheet");
            tool.Rules.Add(rule);

            var rule2 = new SimDataMappingRuleComponent("Sheet2");
            tool.Rules[0] = rule2;

            Assert.AreEqual(tool, rule2.Tool);
            Assert.AreEqual(null, rule.Tool);
        }

        [TestMethod]
        public void ClearMappingRule()
        {
            LoadProject(emptyProject);
            SimDataMappingTool tool = new SimDataMappingTool("asdf");

            var rule = new SimDataMappingRuleComponent("Sheet");
            tool.Rules.Add(rule);

            tool.Rules.Clear();
            Assert.AreEqual(null, rule.Tool);
        }

        #endregion

        #region Read Rules

        [TestMethod]
        public void AddReadMappingRule()
        {
            LoadProject(emptyProject);
            SimDataMappingTool tool = new SimDataMappingTool("asdf");

            var rule = new SimDataMappingReadRule();
            Assert.IsNull(rule.Tool);

            tool.ReadRules.Add(rule);
            Assert.AreEqual(tool, rule.Tool);
        }

        [TestMethod]
        public void RemoveReadMappingRule()
        {
            LoadProject(emptyProject);
            SimDataMappingTool tool = new SimDataMappingTool("asdf");

            var rule = new SimDataMappingReadRule();
            tool.ReadRules.Add(rule);

            tool.ReadRules.Remove(rule);
            Assert.AreEqual(null, rule.Tool);
        }

        [TestMethod]
        public void SetReadMappingRule()
        {
            LoadProject(emptyProject);
            SimDataMappingTool tool = new SimDataMappingTool("asdf");

            var rule = new SimDataMappingReadRule();
            tool.ReadRules.Add(rule);

            var rule2 = new SimDataMappingReadRule();
            tool.ReadRules[0] = rule2;

            Assert.AreEqual(tool, rule2.Tool);
            Assert.AreEqual(null, rule.Tool);
        }

        [TestMethod]
        public void ClearReadMappingRule()
        {
            LoadProject(emptyProject);
            SimDataMappingTool tool = new SimDataMappingTool("asdf");

            var rule = new SimDataMappingReadRule();
            tool.ReadRules.Add(rule);

            tool.ReadRules.Clear();
            Assert.AreEqual(null, rule.Tool);
        }

        #endregion


        #region Properties

        [TestMethod]
        public void PropertyName()
        {
            SimDataMappingTool tool = new SimDataMappingTool("asdf");

            tool.Name = "newname";

            Assert.AreEqual("newname", tool.Name);
        }

        [TestMethod]
        public void PropertyNameChanges()
        {
            SimDataMappingTool tool = new SimDataMappingTool("asdf");
            PropertyChangedEventCounter cc = new PropertyChangedEventCounter(tool);

            tool.Name = "asdf";
            cc.AssertEventCount(0);

            tool.Name = "newname";

            cc.AssertEventCount(1);
            Assert.AreEqual(nameof(SimDataMappingTool.Name), cc.PropertyChangedArgs[0]);
        }

        #endregion

        #region Execution

        [TestMethod]
        public void Simple()
        {
            LoadProject(mappingProject);

            var componentRule = new SimDataMappingRuleComponent("Sheet A");
            componentRule.OffsetParent = new RowColumnIndex(2, 1);
            componentRule.OffsetConsecutive = new RowColumnIndex(0, 1);
            componentRule.MaxDepth = int.MaxValue;
            componentRule.MaxMatches = int.MaxValue;
            componentRule.Properties.Add(SimDataMappingComponentMappingProperties.Name);

            var childComponentRule = new SimDataMappingRuleComponent("Sheet A");
            childComponentRule.OffsetParent = new RowColumnIndex(0, 2);
            childComponentRule.OffsetConsecutive = new RowColumnIndex(1, 0);
            childComponentRule.MaxDepth = int.MaxValue;
            childComponentRule.MaxMatches = int.MaxValue;
            childComponentRule.Properties.Add(SimDataMappingComponentMappingProperties.Name);
            componentRule.Rules.Add(childComponentRule);

            SimDataMappingTool tool = new SimDataMappingTool("Tool1");
            tool.Rules.Add(componentRule);
            tool.Rules.AddMapping(componentRule, projectData.Components.First(x => x.Name == "Geometry"));

            projectData.DataMappingTools.Add(tool);

            tool.Execute();

            //Checks
            var table = projectData.ValueManager.First(x => x.Name == "Tool1_Sheet A") as SimMultiValueBigTable;
            Assert.IsNotNull(table);

            Assert.AreEqual(6, table.RowHeaders.Count);
            Assert.AreEqual(4, table.ColumnHeaders.Count);

            AssertUtil.ContainEqualValues(new object[,]
                {
                    { null, null, null, null },
                    { null, null, null, null },
                    { null, "Geometry", null, "Volume1" },
                    { null, null, null, "Volume2" },
                    { null, null, null, "Face1" },
                    { null, null, null, "Face2" },
                },
                table);
        }

        [TestMethod]
        public void MultipleRulesSameSheet()
        {
            LoadProject(mappingProject);

            var componentRule1 = new SimDataMappingRuleComponent("Sheet A");
            componentRule1.OffsetParent = new RowColumnIndex(2, 1);
            componentRule1.OffsetConsecutive = new RowColumnIndex(1, 0);
            componentRule1.MaxDepth = int.MaxValue;
            componentRule1.MaxMatches = int.MaxValue;
            componentRule1.Properties.Add(SimDataMappingComponentMappingProperties.Name);

            var childComponentRule1 = new SimDataMappingRuleComponent("Sheet A");
            childComponentRule1.OffsetParent = new RowColumnIndex(0, 2);
            childComponentRule1.OffsetConsecutive = new RowColumnIndex(1, 0);
            childComponentRule1.MaxDepth = int.MaxValue;
            childComponentRule1.MaxMatches = int.MaxValue;
            childComponentRule1.Properties.Add(SimDataMappingComponentMappingProperties.Name);
            componentRule1.Rules.Add(childComponentRule1);


            var componentRule2 = new SimDataMappingRuleComponent("Sheet A");
            componentRule2.OffsetParent = new RowColumnIndex(2, 4);
            componentRule2.OffsetConsecutive = new RowColumnIndex(1, 0);
            componentRule2.MaxDepth = int.MaxValue;
            componentRule2.MaxMatches = int.MaxValue;
            componentRule2.Properties.Add(SimDataMappingComponentMappingProperties.Name);

            var childComponentRule2 = new SimDataMappingRuleComponent("Sheet A");
            childComponentRule2.OffsetParent = new RowColumnIndex(0, 1);
            childComponentRule2.OffsetConsecutive = new RowColumnIndex(1, 0);
            childComponentRule2.MaxDepth = int.MaxValue;
            childComponentRule2.MaxMatches = int.MaxValue;
            childComponentRule2.Properties.Add(SimDataMappingComponentMappingProperties.Name);
            componentRule2.Rules.Add(childComponentRule2);


            SimDataMappingTool tool = new SimDataMappingTool("Tool1");
            tool.Rules.Add(componentRule1);
            tool.Rules.Add(componentRule2);
            tool.Rules.AddMapping(componentRule1, projectData.Components.First(x => x.Name == "Geometry"));
            tool.Rules.AddMapping(componentRule2, projectData.Components.First(x => x.Name == "Geometry (Additional)"));

            projectData.DataMappingTools.Add(tool);

            tool.Execute();

            //Checks
            var table = projectData.ValueManager.First(x => x.Name == "Tool1_Sheet A") as SimMultiValueBigTable;
            Assert.IsNotNull(table);

            Assert.AreEqual(6, table.RowHeaders.Count);
            Assert.AreEqual(6, table.ColumnHeaders.Count);

            AssertUtil.ContainEqualValues(new object[,]
                {
                    { null, null, null, null, null, null },
                    { null, null, null, null, null, null },
                    { null, "Geometry", null, "Volume1", "Geometry (Additional)", "InteriorWall" },
                    { null, null, null, "Volume2", null, "Room2" },
                    { null, null, null, "Face1", null, "Window" },
                    { null, null, null, "Face2", null, "Room1" },
                },
                table);
        }

        [TestMethod]
        public void MultipleRulesDifferentSheet()
        {
            LoadProject(mappingProject);

            var componentRule1 = new SimDataMappingRuleComponent("Sheet A");
            componentRule1.OffsetParent = new RowColumnIndex(2, 1);
            componentRule1.OffsetConsecutive = new RowColumnIndex(1, 0);
            componentRule1.MaxDepth = int.MaxValue;
            componentRule1.MaxMatches = int.MaxValue;
            componentRule1.Properties.Add(SimDataMappingComponentMappingProperties.Name);

            var childComponentRule1 = new SimDataMappingRuleComponent("Sheet A");
            childComponentRule1.OffsetParent = new RowColumnIndex(0, 2);
            childComponentRule1.OffsetConsecutive = new RowColumnIndex(1, 0);
            childComponentRule1.MaxDepth = int.MaxValue;
            childComponentRule1.MaxMatches = int.MaxValue;
            childComponentRule1.Properties.Add(SimDataMappingComponentMappingProperties.Name);
            componentRule1.Rules.Add(childComponentRule1);


            var componentRule2 = new SimDataMappingRuleComponent("Sheet B");
            componentRule2.OffsetParent = new RowColumnIndex(2, 4);
            componentRule2.OffsetConsecutive = new RowColumnIndex(1, 0);
            componentRule2.MaxDepth = int.MaxValue;
            componentRule2.MaxMatches = int.MaxValue;
            componentRule2.Properties.Add(SimDataMappingComponentMappingProperties.Name);

            var childComponentRule2 = new SimDataMappingRuleComponent("Sheet B");
            childComponentRule2.OffsetParent = new RowColumnIndex(0, 1);
            childComponentRule2.OffsetConsecutive = new RowColumnIndex(1, 0);
            childComponentRule2.MaxDepth = int.MaxValue;
            childComponentRule2.MaxMatches = int.MaxValue;
            childComponentRule2.Properties.Add(SimDataMappingComponentMappingProperties.Name);
            componentRule2.Rules.Add(childComponentRule2);


            SimDataMappingTool tool = new SimDataMappingTool("Tool1");
            tool.Rules.Add(componentRule1);
            tool.Rules.Add(componentRule2);
            tool.Rules.AddMapping(componentRule1, projectData.Components.First(x => x.Name == "Geometry"));
            tool.Rules.AddMapping(componentRule2, projectData.Components.First(x => x.Name == "Geometry (Additional)"));

            projectData.DataMappingTools.Add(tool);

            tool.Execute();

            //Checks
            var tableA = projectData.ValueManager.First(x => x.Name == "Tool1_Sheet A") as SimMultiValueBigTable;
            Assert.IsNotNull(tableA);
            Assert.AreEqual(6, tableA.RowHeaders.Count);
            Assert.AreEqual(4, tableA.ColumnHeaders.Count);

            AssertUtil.ContainEqualValues(new object[,]
                {
                    { null, null, null, null },
                    { null, null, null, null },
                    { null, "Geometry", null, "Volume1" },
                    { null, null, null, "Volume2" },
                    { null, null, null, "Face1" },
                    { null, null, null, "Face2" },
                },
                tableA);

            var tableB = projectData.ValueManager.First(x => x.Name == "Tool1_Sheet B") as SimMultiValueBigTable;
            Assert.IsNotNull(tableB);
            Assert.AreEqual(6, tableB.RowHeaders.Count);
            Assert.AreEqual(6, tableB.ColumnHeaders.Count);

            AssertUtil.ContainEqualValues(new object[,]
                {
                    { null, null, null, null, null, null },
                    { null, null, null, null, null, null },
                    { null, null, null, null, "Geometry (Additional)", "InteriorWall" },
                    { null, null, null, null, null, "Room2" },
                    { null, null, null, null, null, "Window" },
                    { null, null, null, null, null, "Room1" },
                },
                tableB);
        }

        [TestMethod]
        public void MultipleComponentsPerRule()
        {
            LoadProject(mappingProject);

            var componentRule = new SimDataMappingRuleComponent("Sheet A");
            componentRule.OffsetParent = new RowColumnIndex(2, 1);
            componentRule.OffsetConsecutive = new RowColumnIndex(1, 0);
            componentRule.MaxDepth = int.MaxValue;
            componentRule.MaxMatches = int.MaxValue;
            componentRule.Properties.Add(SimDataMappingComponentMappingProperties.Name);

            var childComponentRule = new SimDataMappingRuleComponent("Sheet A");
            childComponentRule.OffsetParent = new RowColumnIndex(0, 2);
            childComponentRule.OffsetConsecutive = new RowColumnIndex(1, 0);
            childComponentRule.MaxDepth = int.MaxValue;
            childComponentRule.MaxMatches = int.MaxValue;
            childComponentRule.Properties.Add(SimDataMappingComponentMappingProperties.Name);
            componentRule.Rules.Add(childComponentRule);

            SimDataMappingTool tool = new SimDataMappingTool("Tool1");
            tool.Rules.Add(componentRule);
            tool.Rules.AddMapping(componentRule, projectData.Components.First(x => x.Name == "Geometry"));
            tool.Rules.AddMapping(componentRule, projectData.Components.First(x => x.Name == "Geometry (Additional)"));

            projectData.DataMappingTools.Add(tool);

            tool.Execute();

            //Checks
            var table = projectData.ValueManager.First(x => x.Name == "Tool1_Sheet A") as SimMultiValueBigTable;
            Assert.IsNotNull(table);

            Assert.AreEqual(10, table.RowHeaders.Count);
            Assert.AreEqual(4, table.ColumnHeaders.Count);

            AssertUtil.ContainEqualValues(new object[,]
                {
                    { null, null, null, null },
                    { null, null, null, null },
                    { null, "Geometry", null, "Volume1" },
                    { null, null, null, "Volume2" },
                    { null, null, null, "Face1" },
                    { null, null, null, "Face2" },
                    { null, "Geometry (Additional)", null, "InteriorWall" },
                    { null, null, null, "Room2" },
                    { null, null, null, "Window" },
                    { null, null, null, "Room1" },
                },
                table);
        }

        #endregion

        #region Component-Rule Mappings

        [TestMethod]
        public void AddMapping()
        {
            LoadProject(mappingProject);

            var componentRule = new SimDataMappingRuleComponent("Sheet A");
            SimDataMappingTool tool = new SimDataMappingTool("Tool1");
            tool.Rules.Add(componentRule);

            var comp1 = projectData.Components.First(x => x.Name == "Geometry");
            tool.Rules.AddMapping(componentRule, comp1);

            var mappings = tool.Rules.GetMappings(componentRule).ToList();
            Assert.AreEqual(1, mappings.Count);
            Assert.AreEqual(comp1, mappings[0]);
        }

        [TestMethod]
        public void AddMappingDuplicate()
        {
            LoadProject(mappingProject);

            var componentRule = new SimDataMappingRuleComponent("Sheet A");
            SimDataMappingTool tool = new SimDataMappingTool("Tool1");
            tool.Rules.Add(componentRule);

            var comp1 = projectData.Components.First(x => x.Name == "Geometry");
            tool.Rules.AddMapping(componentRule, comp1);

            //Duplicate add
            tool.Rules.AddMapping(componentRule, comp1);

            var mappings = tool.Rules.GetMappings(componentRule).ToList();
            Assert.AreEqual(1, mappings.Count);
            Assert.AreEqual(comp1, mappings[0]);
        }

        [TestMethod]
        public void AddMappings()
        {
            LoadProject(mappingProject);

            var componentRule = new SimDataMappingRuleComponent("Sheet A");
            SimDataMappingTool tool = new SimDataMappingTool("Tool1");
            tool.Rules.Add(componentRule);

            var comp1 = projectData.Components.First(x => x.Name == "Geometry");
            var comp2 = projectData.Components.First(x => x.Name == "Geometry (Additional)");

            tool.Rules.AddMappings(componentRule, new SimComponent[]
            {
                comp1,
                comp2,
            });

            var mappings = tool.Rules.GetMappings(componentRule).ToList();
            Assert.AreEqual(2, mappings.Count);
            Assert.IsTrue(mappings.Contains(comp1));
            Assert.IsTrue(mappings.Contains(comp2));
        }

        [TestMethod]
        public void RemoveMapping()
        {
            LoadProject(mappingProject);

            var componentRule = new SimDataMappingRuleComponent("Sheet A");
            SimDataMappingTool tool = new SimDataMappingTool("Tool1");
            tool.Rules.Add(componentRule);

            var comp1 = projectData.Components.First(x => x.Name == "Geometry");
            var comp2 = projectData.Components.First(x => x.Name == "Geometry (Additional)");

            tool.Rules.AddMappings(componentRule, new SimComponent[]
            {
                comp1,
                comp2,
            });

            tool.Rules.RemoveMapping(componentRule, comp1);

            var mappings = tool.Rules.GetMappings(componentRule).ToList();
            Assert.AreEqual(1, mappings.Count);
            Assert.IsTrue(mappings.Contains(comp2));
        }


        [TestMethod]
        public void RemoveMappingOnRemoverRootComponent()
        {
            LoadProject(mappingProject);

            var componentRule = new SimDataMappingRuleComponent("Sheet A");
            SimDataMappingTool tool = new SimDataMappingTool("Tool1");
            tool.Rules.Add(componentRule);
            projectData.DataMappingTools.Add(tool);

            var comp1 = projectData.Components.First(x => x.Name == "Geometry");
            var comp2 = projectData.Components.First(x => x.Name == "Geometry (Additional)").Components.First(x => x.Component.Name == "Room1").Component;

            tool.Rules.AddMappings(componentRule, new SimComponent[]
            {
                comp1,
                comp2,
            });

            //Delete component
            projectData.Components.Remove(comp1);

            var mappings = tool.Rules.GetMappings(componentRule).ToList();
            Assert.AreEqual(1, mappings.Count);
            Assert.IsTrue(mappings.Contains(comp2));
        }

        [TestMethod]
        public void RemoveMappingOnClearRootComponent()
        {
            LoadProject(mappingProject);

            var componentRule = new SimDataMappingRuleComponent("Sheet A");
            SimDataMappingTool tool = new SimDataMappingTool("Tool1");
            tool.Rules.Add(componentRule);
            projectData.DataMappingTools.Add(tool);

            var comp1 = projectData.Components.First(x => x.Name == "Geometry");
            var comp2 = projectData.Components.First(x => x.Name == "Geometry (Additional)").Components.First(x => x.Component.Name == "Room1").Component;

            tool.Rules.AddMappings(componentRule, new SimComponent[]
            {
                comp1,
                comp2,
            });

            //Delete component
            projectData.Components.Clear();

            var mappings = tool.Rules.GetMappings(componentRule).ToList();
            Assert.AreEqual(0, mappings.Count);
        }

        [TestMethod]
        public void RemoveMappingOnReplaceRootComponent()
        {
            LoadProject(mappingProject);

            var componentRule = new SimDataMappingRuleComponent("Sheet A");
            SimDataMappingTool tool = new SimDataMappingTool("Tool1");
            tool.Rules.Add(componentRule);
            projectData.DataMappingTools.Add(tool);

            var comp1 = projectData.Components.First(x => x.Name == "Geometry");
            var comp2 = projectData.Components.First(x => x.Name == "Geometry (Additional)").Components.First(x => x.Component.Name == "Room1").Component;

            tool.Rules.AddMappings(componentRule, new SimComponent[]
            {
                comp1,
                comp2,
            });

            SimComponent newComponent = new SimComponent();

            //Delete component
            projectData.Components[projectData.Components.IndexOf(comp1)] = newComponent;

            var mappings = tool.Rules.GetMappings(componentRule).ToList();
            Assert.AreEqual(1, mappings.Count);
            Assert.IsTrue(mappings.Contains(comp2));
        }


        [TestMethod]
        public void RemoveMappingOnRemoveChildEntry()
        {
            LoadProject(mappingProject);

            var componentRule = new SimDataMappingRuleComponent("Sheet A");
            SimDataMappingTool tool = new SimDataMappingTool("Tool1");
            tool.Rules.Add(componentRule);
            projectData.DataMappingTools.Add(tool);

            var comp1 = projectData.Components.First(x => x.Name == "Geometry");
            var comp2Owner = projectData.Components.First(x => x.Name == "Geometry (Additional)");
            var comp2 = comp2Owner.Components.First(x => x.Component.Name == "Room1");

            tool.Rules.AddMappings(componentRule, new SimComponent[]
            {
                comp1,
                comp2.Component,
            });

            //Delete component
            comp2Owner.Components.Remove(comp2);

            var mappings = tool.Rules.GetMappings(componentRule).ToList();
            Assert.AreEqual(1, mappings.Count);
            Assert.IsTrue(mappings.Contains(comp1));
        }

        [TestMethod]
        public void RemoveMappingOnRemoveChildComponent()
        {
            LoadProject(mappingProject);

            var componentRule = new SimDataMappingRuleComponent("Sheet A");
            SimDataMappingTool tool = new SimDataMappingTool("Tool1");
            tool.Rules.Add(componentRule);
            projectData.DataMappingTools.Add(tool);

            var comp1 = projectData.Components.First(x => x.Name == "Geometry");
            var comp2Owner = projectData.Components.First(x => x.Name == "Geometry (Additional)");
            var comp2 = comp2Owner.Components.First(x => x.Component.Name == "Room1");

            tool.Rules.AddMappings(componentRule, new SimComponent[]
            {
                comp1,
                comp2.Component,
            });

            //Delete component
            comp2.Component = null;

            var mappings = tool.Rules.GetMappings(componentRule).ToList();
            Assert.AreEqual(1, mappings.Count);
            Assert.IsTrue(mappings.Contains(comp1));
        }

        [TestMethod]
        public void RemoveMappingOnClearChildComponent()
        {
            LoadProject(mappingProject);

            var componentRule = new SimDataMappingRuleComponent("Sheet A");
            SimDataMappingTool tool = new SimDataMappingTool("Tool1");
            tool.Rules.Add(componentRule);
            projectData.DataMappingTools.Add(tool);

            var comp1 = projectData.Components.First(x => x.Name == "Geometry");
            var comp2Owner = projectData.Components.First(x => x.Name == "Geometry (Additional)");
            var comp2 = comp2Owner.Components.First(x => x.Component.Name == "Room1");

            tool.Rules.AddMappings(componentRule, new SimComponent[]
            {
                comp1,
                comp2.Component,
            });

            //Delete component
            comp2Owner.Components.Remove(comp2);

            var mappings = tool.Rules.GetMappings(componentRule).ToList();
            Assert.AreEqual(1, mappings.Count);
            Assert.IsTrue(mappings.Contains(comp1));
        }

        [TestMethod]
        public void RemoveMappingOnReplaceChildEntry()
        {
            LoadProject(mappingProject);

            var componentRule = new SimDataMappingRuleComponent("Sheet A");
            SimDataMappingTool tool = new SimDataMappingTool("Tool1");
            tool.Rules.Add(componentRule);
            projectData.DataMappingTools.Add(tool);

            var comp1 = projectData.Components.First(x => x.Name == "Geometry");
            var comp2Owner = projectData.Components.First(x => x.Name == "Geometry (Additional)");
            var comp2 = comp2Owner.Components.First(x => x.Component.Name == "Room1");

            tool.Rules.AddMappings(componentRule, new SimComponent[]
            {
                comp1,
                comp2.Component,
            });

            //Delete component
            var newComp = new SimComponent();
            newComp.Slots.Add(new SimTaxonomyEntryReference(comp2.Slot.SlotBase));
            comp2Owner.Components[comp2Owner.Components.IndexOf(comp2)] = new SimChildComponentEntry(
                new SimSlot(new SimTaxonomyEntryReference(newComp.Slots[0]), "2"));

            var mappings = tool.Rules.GetMappings(componentRule).ToList();
            Assert.AreEqual(1, mappings.Count);
            Assert.IsTrue(mappings.Contains(comp1));
        }

        [TestMethod]
        public void RemoveMappingOnReplaceChildComponent()
        {
            LoadProject(mappingProject);

            var componentRule = new SimDataMappingRuleComponent("Sheet A");
            SimDataMappingTool tool = new SimDataMappingTool("Tool1");
            tool.Rules.Add(componentRule);
            projectData.DataMappingTools.Add(tool);

            var comp1 = projectData.Components.First(x => x.Name == "Geometry");
            var comp2Owner = projectData.Components.First(x => x.Name == "Geometry (Additional)");
            var comp2 = comp2Owner.Components.First(x => x.Component.Name == "Room1");

            tool.Rules.AddMappings(componentRule, new SimComponent[]
            {
                comp1,
                comp2.Component,
            });

            //Delete component
            var newComp = new SimComponent();
            newComp.Slots.Add(new SimTaxonomyEntryReference(comp2.Slot.SlotBase));
            comp2.Component = newComp;

            var mappings = tool.Rules.GetMappings(componentRule).ToList();
            Assert.AreEqual(1, mappings.Count);
            Assert.IsTrue(mappings.Contains(comp1));
        }

        [TestMethod]
        public void RemoveMappingOnRemoveParentComponent()
        {
            LoadProject(mappingProject);

            var componentRule = new SimDataMappingRuleComponent("Sheet A");
            SimDataMappingTool tool = new SimDataMappingTool("Tool1");
            tool.Rules.Add(componentRule);
            projectData.DataMappingTools.Add(tool);

            var comp1 = projectData.Components.First(x => x.Name == "Geometry");
            var comp2Owner = projectData.Components.First(x => x.Name == "Geometry (Additional)");
            var comp2 = comp2Owner.Components.First(x => x.Component.Name == "Room1");

            tool.Rules.AddMappings(componentRule, new SimComponent[]
            {
                comp1,
                comp2.Component,
            });

            //Delete component
            projectData.Components.Remove(comp2Owner);

            var mappings = tool.Rules.GetMappings(componentRule).ToList();
            Assert.AreEqual(1, mappings.Count);
            Assert.IsTrue(mappings.Contains(comp1));
        }

        #endregion

        [TestMethod]
        public void CombinedRules()
        {
            LoadProject(examplesProject);

            var expected = (SimMultiValueBigTable)projectData.ValueManager.First();

            var tool = projectData.DataMappingTools.First();
            var buildingComponent = projectData.Components.First(x => x.Name == "Building");
            (var actualTables, var mappedData) = tool.Execute(new HashSet<Data.Components.SimComponent> { buildingComponent }, "result", false, false);

            Assert.IsNotNull(mappedData);
            Assert.AreEqual(1, actualTables.Count);
            AssertUtil.ContainEqualValues(expected, actualTables[0].table);
        }

        [TestMethod]
        public void CloneTest()
        {
            var tool = new SimDataMappingTool("Tool B");
            tool.Rules.Add(new SimDataMappingRuleComponent("Sheet B"));
            tool.Rules.Add(new SimDataMappingRuleComponent("Sheet C"));

            tool.ReadRules.Add(new SimDataMappingReadRule());
            tool.ReadRules.Add(new SimDataMappingReadRule());

            var clonedTool = tool.Clone();

            Assert.AreEqual("Tool B", clonedTool.Name);
            
            Assert.AreEqual(2, clonedTool.Rules.Count);
            Assert.AreEqual("Sheet B", clonedTool.Rules[0].SheetName);
            Assert.AreEqual("Sheet C", clonedTool.Rules[1].SheetName);

            Assert.AreEqual(2, clonedTool.ReadRules.Count);
        }

        #region HasChanges

        [TestMethod]
        public void AddTool_HasChanges()
        {
            LoadProject(emptyProject);

            Assert.IsFalse(projectData.DataMappingTools.HasChanges);
            var dt = projectData.DataMappingTools.LastChange;

            SimDataMappingTool tool = new SimDataMappingTool("asdf");
            this.projectData.DataMappingTools.Add(tool);
            Thread.Sleep(5);

            Assert.IsTrue(projectData.DataMappingTools.HasChanges);
            Assert.IsTrue(projectData.DataMappingTools.LastChange > dt);
        }

        #endregion
    }
}
