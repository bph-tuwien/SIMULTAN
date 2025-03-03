using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data.Components;
using SIMULTAN.Data.Taxonomy;
using SIMULTAN.DataMapping;
using SIMULTAN.Projects;
using SIMULTAN.Tests.TestUtils;
using SIMULTAN.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SIMULTAN.Tests.DataMapping
{
    [TestClass]
    public class ParameterRuleTests : BaseProjectTest
    {
        private static readonly FileInfo mappingProject = new FileInfo(@"./ExcelMappingTests.simultan");

        #region Properties

        [TestMethod]
        public void MapName()
        {
            this.LoadProject(mappingProject);

            var comp1 = projectData.Components.First(x => x.Name == "Target1");

            SimDataMappingRuleParameter rule = new SimDataMappingRuleParameter("SheetA");
            rule.MappingDirection = SimDataMappingDirection.Vertical;
            rule.Properties.Add(SimDataMappingParameterMappingProperties.Name);

            var state = new SimTraversalState();
            var data = new SimMappedData();
            rule.Execute(comp1, state, data);

            Assert.AreEqual(1, data.Data.Count);
            Assert.IsTrue(data.Data.TryGetValue("SheetA", out var sheetData));

            Assert.AreEqual(1, sheetData.Count);
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(0, 0), "Param1");
        }

        [TestMethod]
        public void MapNameTaxonomy()
        {
            this.LoadProject(mappingProject);

            var comp1 = projectData.Components.First(x => x.Name == "Target2");

            SimDataMappingRuleParameter rule = new SimDataMappingRuleParameter("SheetA");
            rule.MappingDirection = SimDataMappingDirection.Vertical;
            rule.Properties.Add(SimDataMappingParameterMappingProperties.Name);

            var state = new SimTraversalState();
            var data = new SimMappedData();
            rule.Execute(comp1, state, data);

            Assert.AreEqual(1, data.Data.Count);
            Assert.IsTrue(data.Data.TryGetValue("SheetA", out var sheetData));

            Assert.AreEqual(1, sheetData.Count);
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(0, 0), "simnw_postn_y");
        }

        [TestMethod]
        public void MapId()
        {
            this.LoadProject(mappingProject);

            var comp1 = projectData.Components.First(x => x.Name == "Target1");

            SimDataMappingRuleParameter rule = new SimDataMappingRuleParameter("SheetA");
            rule.MappingDirection = SimDataMappingDirection.Vertical;
            rule.Properties.Add(SimDataMappingParameterMappingProperties.Id);

            var state = new SimTraversalState();
            var data = new SimMappedData();
            rule.Execute(comp1, state, data);

            Assert.AreEqual(1, data.Data.Count);
            Assert.IsTrue(data.Data.TryGetValue("SheetA", out var sheetData));

            Assert.AreEqual(1, sheetData.Count);
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(0, 0), 84L);
        }

        [TestMethod]
        public void MapValue()
        {
            this.LoadProject(mappingProject);

            var comp1 = projectData.Components.First(x => x.Name == "Target1");

            SimDataMappingRuleParameter rule = new SimDataMappingRuleParameter("SheetA");
            rule.MappingDirection = SimDataMappingDirection.Vertical;
            rule.Properties.Add(SimDataMappingParameterMappingProperties.Value);

            var state = new SimTraversalState();
            var data = new SimMappedData();
            rule.Execute(comp1, state, data);

            Assert.AreEqual(1, data.Data.Count);
            Assert.IsTrue(data.Data.TryGetValue("SheetA", out var sheetData));

            Assert.AreEqual(1, sheetData.Count);
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(0, 0), 17.6);
        }

        [TestMethod]
        public void MapTextValue()
        {
            this.LoadProject(mappingProject);

            var comp1 = projectData.Components.First(x => x.Name == "Target1");

            SimDataMappingRuleParameter rule = new SimDataMappingRuleParameter("SheetA");
            rule.MappingDirection = SimDataMappingDirection.Vertical;
            rule.Properties.Add(SimDataMappingParameterMappingProperties.Description);

            var state = new SimTraversalState();
            var data = new SimMappedData();
            rule.Execute(comp1, state, data);

            Assert.AreEqual(1, data.Data.Count);
            Assert.IsTrue(data.Data.TryGetValue("SheetA", out var sheetData));

            Assert.AreEqual(1, sheetData.Count);
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(0, 0), "sometext");
        }

        [TestMethod]
        public void MapUnit()
        {
            this.LoadProject(mappingProject);

            var comp1 = projectData.Components.First(x => x.Name == "Target1");

            SimDataMappingRuleParameter rule = new SimDataMappingRuleParameter("SheetA");
            rule.MappingDirection = SimDataMappingDirection.Vertical;
            rule.Properties.Add(SimDataMappingParameterMappingProperties.Unit);

            var state = new SimTraversalState();
            var data = new SimMappedData();
            rule.Execute(comp1, state, data);

            Assert.AreEqual(1, data.Data.Count);
            Assert.IsTrue(data.Data.TryGetValue("SheetA", out var sheetData));

            Assert.AreEqual(1, sheetData.Count);
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(0, 0), "b/c²");
        }

        [TestMethod]
        public void MapMin()
        {
            this.LoadProject(mappingProject);

            var comp1 = projectData.Components.First(x => x.Name == "Target1");

            SimDataMappingRuleParameter rule = new SimDataMappingRuleParameter("SheetA");
            rule.MappingDirection = SimDataMappingDirection.Vertical;
            rule.Properties.Add(SimDataMappingParameterMappingProperties.Min);

            var state = new SimTraversalState();
            var data = new SimMappedData();
            rule.Execute(comp1, state, data);

            Assert.AreEqual(1, data.Data.Count);
            Assert.IsTrue(data.Data.TryGetValue("SheetA", out var sheetData));

            Assert.AreEqual(1, sheetData.Count);
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(0, 0), -1.0);
        }

        [TestMethod]
        public void MapMax()
        {
            this.LoadProject(mappingProject);

            var comp1 = projectData.Components.First(x => x.Name == "Target1");

            SimDataMappingRuleParameter rule = new SimDataMappingRuleParameter("SheetA");
            rule.MappingDirection = SimDataMappingDirection.Vertical;
            rule.Properties.Add(SimDataMappingParameterMappingProperties.Max);

            var state = new SimTraversalState();
            var data = new SimMappedData();
            rule.Execute(comp1, state, data);

            Assert.AreEqual(1, data.Data.Count);
            Assert.IsTrue(data.Data.TryGetValue("SheetA", out var sheetData));

            Assert.AreEqual(1, sheetData.Count);
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(0, 0), 99.0);
        }


        [TestMethod]
        public void MapMultiplePropertiesVertical()
        {
            this.LoadProject(mappingProject);

            var comp1 = projectData.Components.First(x => x.Name == "Target1");

            SimDataMappingRuleParameter rule = new SimDataMappingRuleParameter("SheetA");
            rule.MappingDirection = SimDataMappingDirection.Vertical;
            rule.Properties.Add(SimDataMappingParameterMappingProperties.Name);
            rule.Properties.Add(SimDataMappingParameterMappingProperties.Id);
            rule.Properties.Add(SimDataMappingParameterMappingProperties.Value);

            var state = new SimTraversalState();
            var data = new SimMappedData();
            rule.Execute(comp1, state, data);

            Assert.AreEqual(1, data.Data.Count);
            Assert.IsTrue(data.Data.TryGetValue("SheetA", out var sheetData));

            Assert.AreEqual(3, sheetData.Count);
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(0, 0), "Param1");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(1, 0), 84L);
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(2, 0), 17.6);
        }

        [TestMethod]
        public void MapMultiplePropertiesHorizontal()
        {
            this.LoadProject(mappingProject);

            var comp1 = projectData.Components.First(x => x.Name == "Target1");

            SimDataMappingRuleParameter rule = new SimDataMappingRuleParameter("SheetA");
            rule.MappingDirection = SimDataMappingDirection.Horizontal;
            rule.Properties.Add(SimDataMappingParameterMappingProperties.Name);
            rule.Properties.Add(SimDataMappingParameterMappingProperties.Id);
            rule.Properties.Add(SimDataMappingParameterMappingProperties.Value);

            var state = new SimTraversalState();
            var data = new SimMappedData();
            rule.Execute(comp1, state, data);

            Assert.AreEqual(1, data.Data.Count);
            Assert.IsTrue(data.Data.TryGetValue("SheetA", out var sheetData));

            Assert.AreEqual(3, sheetData.Count);
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(0, 0), "Param1");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(0, 1), 84L);
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(0, 2), 17.6);
        }

        [TestMethod]
        public void MapMultiple()
        {
            this.LoadProject(mappingProject);

            var comp1 = projectData.Components.First(x => x.Name == "ParamMapping1");

            SimDataMappingRuleComponent compRule = new SimDataMappingRuleComponent("SheetA");
            compRule.MaxMatches = int.MaxValue;
            compRule.MaxDepth = int.MaxValue;
            compRule.TraversalStrategy = SimDataMappingRuleTraversalStrategy.SubtreeAndReferences;
            compRule.OffsetParent = new RowColumnIndex(0, 0);
            compRule.OffsetConsecutive = new RowColumnIndex(1, 0);
            compRule.Properties.Add(SimDataMappingComponentMappingProperties.Name);
            compRule.Properties.Add(SimDataMappingComponentMappingProperties.Slot);
            compRule.Filter.Add(new SimDataMappingFilterComponent(SimDataMappingComponentFilterProperties.Name, new Regex("ParamChild.*")));

            SimDataMappingRuleParameter rule = new SimDataMappingRuleParameter("SheetA");
            rule.MaxMatches = int.MaxValue;
            rule.MaxDepth = int.MaxValue;
            rule.ReferencePointParent = SimDataMappingReferencePoint.TopRight;
            rule.OffsetParent = new RowColumnIndex(0, 1);
            rule.ReferencePointConsecutive = SimDataMappingReferencePoint.BottomLeft;
            rule.OffsetConsecutive = new RowColumnIndex(1, 0);
            rule.MappingDirection = SimDataMappingDirection.Horizontal;
            rule.Properties.Add(SimDataMappingParameterMappingProperties.Name);
            rule.Properties.Add(SimDataMappingParameterMappingProperties.Value);

            compRule.Rules.Add(rule);

            var state = new SimTraversalState();
            var data = new SimMappedData();
            compRule.Execute(comp1, state, data);

            Assert.AreEqual(1, data.Data.Count);
            Assert.IsTrue(data.Data.TryGetValue("SheetA", out var sheetData));

            Assert.AreEqual(14, sheetData.Count);

            AssertUtil.AssertContains(sheetData, new RowColumnIndex(0, 0), "ParamChild1");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(0, 1), "undefined_0");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(0, 2), "Param1");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(0, 3), 1.1);

            AssertUtil.AssertContains(sheetData, new RowColumnIndex(1, 0), "ParamChild2");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(1, 1), "undefined_1");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(1, 2), "Param2");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(1, 3), 2.2);
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(2, 2), "param3_key");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(2, 3), 3.3);

            AssertUtil.AssertContains(sheetData, new RowColumnIndex(3, 0), "ParamChild3");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(3, 1), "undefined_2");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(3, 2), "Param4");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(3, 3), 4.4);
        }

        #endregion

        #region MultiValueMappings

        [TestMethod]
        public void MapValueVerticalRow()
        {
            this.LoadProject(mappingProject);

            var comp1 = projectData.Components.First(x => x.Name == "Target1");

            SimDataMappingRuleParameter rule = new SimDataMappingRuleParameter("SheetA");
            rule.MappingDirection = SimDataMappingDirection.Vertical;
            rule.ParameterRange = SimDataMappingParameterRange.CurrentRow;
            rule.Properties.Add(SimDataMappingParameterMappingProperties.Name);
            rule.Properties.Add(SimDataMappingParameterMappingProperties.Value);
            rule.Properties.Add(SimDataMappingParameterMappingProperties.Unit);

            var state = new SimTraversalState();
            var data = new SimMappedData();
            rule.Execute(comp1, state, data);

            Assert.AreEqual(1, data.Data.Count);
            Assert.IsTrue(data.Data.TryGetValue("SheetA", out var sheetData));

            Assert.AreEqual(5, sheetData.Count);
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(0, 0), "Param1");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(1, 0), 25.3);
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(1, 1), 17.6);
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(1, 2), 8);
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(2, 0), "b/c²");
        }

        [TestMethod]
        public void MapValueHorizontalRow()
        {
            this.LoadProject(mappingProject);

            var comp1 = projectData.Components.First(x => x.Name == "Target1");

            SimDataMappingRuleParameter rule = new SimDataMappingRuleParameter("SheetA");
            rule.MappingDirection = SimDataMappingDirection.Horizontal;
            rule.ParameterRange = SimDataMappingParameterRange.CurrentRow;
            rule.Properties.Add(SimDataMappingParameterMappingProperties.Name);
            rule.Properties.Add(SimDataMappingParameterMappingProperties.Value);
            rule.Properties.Add(SimDataMappingParameterMappingProperties.Unit);

            var state = new SimTraversalState();
            var data = new SimMappedData();
            rule.Execute(comp1, state, data);

            Assert.AreEqual(1, data.Data.Count);
            Assert.IsTrue(data.Data.TryGetValue("SheetA", out var sheetData));

            Assert.AreEqual(5, sheetData.Count);
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(0, 0), "Param1");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(0, 1), 25.3);
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(0, 2), 17.6);
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(0, 3), 8);
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(0, 4), "b/c²");
        }

        [TestMethod]
        public void MapValueVerticalColumn()
        {
            this.LoadProject(mappingProject);

            var comp1 = projectData.Components.First(x => x.Name == "Target1");

            SimDataMappingRuleParameter rule = new SimDataMappingRuleParameter("SheetA");
            rule.MappingDirection = SimDataMappingDirection.Vertical;
            rule.ParameterRange = SimDataMappingParameterRange.CurrentColumn;
            rule.Properties.Add(SimDataMappingParameterMappingProperties.Name);
            rule.Properties.Add(SimDataMappingParameterMappingProperties.Value);
            rule.Properties.Add(SimDataMappingParameterMappingProperties.Unit);

            var state = new SimTraversalState();
            var data = new SimMappedData();
            rule.Execute(comp1, state, data);

            Assert.AreEqual(1, data.Data.Count);
            Assert.IsTrue(data.Data.TryGetValue("SheetA", out var sheetData));

            Assert.AreEqual(4, sheetData.Count);
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(0, 0), "Param1");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(1, 0), 17.6);
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(2, 0), false);
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(3, 0), "b/c²");
        }

        [TestMethod]
        public void MapValueHorizontalColumn()
        {
            this.LoadProject(mappingProject);

            var comp1 = projectData.Components.First(x => x.Name == "Target1");

            SimDataMappingRuleParameter rule = new SimDataMappingRuleParameter("SheetA");
            rule.MappingDirection = SimDataMappingDirection.Horizontal;
            rule.ParameterRange = SimDataMappingParameterRange.CurrentColumn;
            rule.Properties.Add(SimDataMappingParameterMappingProperties.Name);
            rule.Properties.Add(SimDataMappingParameterMappingProperties.Value);
            rule.Properties.Add(SimDataMappingParameterMappingProperties.Unit);

            var state = new SimTraversalState();
            var data = new SimMappedData();
            rule.Execute(comp1, state, data);

            Assert.AreEqual(1, data.Data.Count);
            Assert.IsTrue(data.Data.TryGetValue("SheetA", out var sheetData));

            Assert.AreEqual(4, sheetData.Count);
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(0, 0), "Param1");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(0, 1), 17.6);
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(1, 1), false);
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(0, 2), "b/c²");
        }

        [TestMethod]
        public void MapValueVerticalTable()
        {
            this.LoadProject(mappingProject);

            var comp1 = projectData.Components.First(x => x.Name == "Target1");

            SimDataMappingRuleParameter rule = new SimDataMappingRuleParameter("SheetA");
            rule.MappingDirection = SimDataMappingDirection.Vertical;
            rule.ParameterRange = SimDataMappingParameterRange.Table;
            rule.Properties.Add(SimDataMappingParameterMappingProperties.Name);
            rule.Properties.Add(SimDataMappingParameterMappingProperties.Value);
            rule.Properties.Add(SimDataMappingParameterMappingProperties.Unit);

            var state = new SimTraversalState();
            var data = new SimMappedData();
            rule.Execute(comp1, state, data);

            Assert.AreEqual(1, data.Data.Count);
            Assert.IsTrue(data.Data.TryGetValue("SheetA", out var sheetData));

            Assert.AreEqual(8, sheetData.Count);
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(0, 0), "Param1");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(1, 0), 25.3);
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(1, 1), 17.6);
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(1, 2), 8);
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(2, 0), "asdf");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(2, 1), false);
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(2, 2), null);
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(3, 0), "b/c²");
        }

        [TestMethod]
        public void MapValueHorizontalTable()
        {
            this.LoadProject(mappingProject);

            var comp1 = projectData.Components.First(x => x.Name == "Target1");

            SimDataMappingRuleParameter rule = new SimDataMappingRuleParameter("SheetA");
            rule.MappingDirection = SimDataMappingDirection.Horizontal;
            rule.ParameterRange = SimDataMappingParameterRange.Table;
            rule.Properties.Add(SimDataMappingParameterMappingProperties.Name);
            rule.Properties.Add(SimDataMappingParameterMappingProperties.Value);
            rule.Properties.Add(SimDataMappingParameterMappingProperties.Unit);

            var state = new SimTraversalState();
            var data = new SimMappedData();
            rule.Execute(comp1, state, data);

            Assert.AreEqual(1, data.Data.Count);
            Assert.IsTrue(data.Data.TryGetValue("SheetA", out var sheetData));

            Assert.AreEqual(8, sheetData.Count);
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(0, 0), "Param1");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(0, 1), 25.3);
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(0, 2), 17.6);
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(0, 3), 8);
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(1, 1), "asdf");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(1, 2), false);
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(1, 3), null);
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(0, 4), "b/c²");
        }

        #endregion

        #region Instance Parameters

        [TestMethod]
        public void MapInstanceValue()
        {
            this.LoadProject(mappingProject);

            var comp1 = projectData.Components.First(x => x.Name == "Component1");

            SimDataMappingRuleComponent componentRule = new SimDataMappingRuleComponent("SheetA");
            componentRule.MappingDirection = SimDataMappingDirection.Vertical;
            componentRule.MaxMatches = int.MaxValue;
            componentRule.MaxDepth = int.MaxValue;
            componentRule.Filter.Add(new SimDataMappingFilterComponent(SimDataMappingComponentFilterProperties.Name, "ChildComponent1"));

            SimDataMappingRuleInstance instanceRule = new SimDataMappingRuleInstance("SheetA");
            instanceRule.MappingDirection = SimDataMappingDirection.Vertical;
            instanceRule.MaxMatches = int.MaxValue;
            instanceRule.MaxDepth = int.MaxValue;
            instanceRule.OffsetConsecutive = new RowColumnIndex(1, 0);
            instanceRule.Properties.Add(SimDataMappingInstanceMappingProperties.Name);
            componentRule.Rules.Add(instanceRule);

            SimDataMappingRuleParameter parameterRule = new SimDataMappingRuleParameter("SheetA");
            parameterRule.MappingDirection = SimDataMappingDirection.Vertical;
            parameterRule.MaxMatches = int.MaxValue;
            parameterRule.MaxDepth = int.MaxValue;
            parameterRule.OffsetParent = new RowColumnIndex(0, 1);
            parameterRule.OffsetConsecutive = new RowColumnIndex(1, 0);
            parameterRule.Properties.Add(SimDataMappingParameterMappingProperties.Value);
            instanceRule.Rules.Add(parameterRule);

            var state = new SimTraversalState();
            var data = new SimMappedData();
            componentRule.Execute(comp1, state, data);

            Assert.AreEqual(1, data.Data.Count);
            Assert.IsTrue(data.Data.TryGetValue("SheetA", out var sheetData));

            Assert.AreEqual(6, sheetData.Count);
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(0, 0), "Geometry Placement 0:13");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(0, 1), 5.0);
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(1, 1), 6.0);
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(2, 0), "Geometry Placement 0:20");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(2, 1), 0.0);
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(3, 1), 0.0);
        }

        [TestMethod]
        public void MapInstanceName()
        {
            this.LoadProject(mappingProject);

            var comp1 = projectData.Components.First(x => x.Name == "Component1");

            SimDataMappingRuleComponent componentRule = new SimDataMappingRuleComponent("SheetA");
            componentRule.MappingDirection = SimDataMappingDirection.Vertical;
            componentRule.MaxMatches = int.MaxValue;
            componentRule.MaxDepth = int.MaxValue;
            componentRule.Filter.Add(new SimDataMappingFilterComponent(SimDataMappingComponentFilterProperties.Name, "ChildComponent1"));

            SimDataMappingRuleInstance instanceRule = new SimDataMappingRuleInstance("SheetA");
            instanceRule.MappingDirection = SimDataMappingDirection.Vertical;
            instanceRule.MaxMatches = int.MaxValue;
            instanceRule.MaxDepth = int.MaxValue;
            instanceRule.OffsetConsecutive = new RowColumnIndex(1, 0);
            instanceRule.Properties.Add(SimDataMappingInstanceMappingProperties.Name);
            componentRule.Rules.Add(instanceRule);

            SimDataMappingRuleParameter parameterRule = new SimDataMappingRuleParameter("SheetA");
            parameterRule.MappingDirection = SimDataMappingDirection.Vertical;
            parameterRule.MaxMatches = int.MaxValue;
            parameterRule.MaxDepth = int.MaxValue;
            parameterRule.OffsetParent = new RowColumnIndex(0, 1);
            parameterRule.OffsetConsecutive = new RowColumnIndex(1, 0);
            parameterRule.Properties.Add(SimDataMappingParameterMappingProperties.Name);
            instanceRule.Rules.Add(parameterRule);

            var state = new SimTraversalState();
            var data = new SimMappedData();
            componentRule.Execute(comp1, state, data);

            Assert.AreEqual(1, data.Data.Count);
            Assert.IsTrue(data.Data.TryGetValue("SheetA", out var sheetData));

            Assert.AreEqual(6, sheetData.Count);
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(0, 0), "Geometry Placement 0:13");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(0, 1), "din");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(1, 1), "dout");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(2, 0), "Geometry Placement 0:20");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(2, 1), "din");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(3, 1), "dout");
        }

        //Since all other properties (except for value) are the same for component and instance, they aren't all tested again

        #endregion

        #region Filter

        [TestMethod]
        public void FilterNameStringOnName()
        {
            this.LoadProject(mappingProject);

            var comp1 = projectData.Components.First(x => x.Name == "ParamMapping1");

            SimDataMappingRuleComponent compRule = new SimDataMappingRuleComponent("SheetA");
            compRule.MaxMatches = int.MaxValue;
            compRule.MaxDepth = int.MaxValue;
            compRule.TraversalStrategy = SimDataMappingRuleTraversalStrategy.SubtreeAndReferences;
            compRule.OffsetParent = new RowColumnIndex(0, 0);
            compRule.OffsetConsecutive = new RowColumnIndex(1, 0);
            compRule.Properties.Add(SimDataMappingComponentMappingProperties.Name);
            compRule.Filter.Add(new SimDataMappingFilterComponent(SimDataMappingComponentFilterProperties.Name, new Regex("ParamChild.*")));

            SimDataMappingRuleParameter rule = new SimDataMappingRuleParameter("SheetA");
            rule.MaxMatches = int.MaxValue;
            rule.MaxDepth = int.MaxValue;
            rule.OffsetParent = new RowColumnIndex(0, 1);
            rule.OffsetConsecutive = new RowColumnIndex(1, 0);
            rule.MappingDirection = SimDataMappingDirection.Horizontal;
            rule.Properties.Add(SimDataMappingParameterMappingProperties.Name);
            rule.Filter.Add(new SimDataMappingFilterParameter(SimDataMappingParameterFilterProperties.Name, "Param2"));

            compRule.Rules.Add(rule);

            var state = new SimTraversalState();
            var data = new SimMappedData();
            compRule.Execute(comp1, state, data);

            Assert.AreEqual(1, data.Data.Count);
            Assert.IsTrue(data.Data.TryGetValue("SheetA", out var sheetData));

            Assert.AreEqual(4, sheetData.Count);

            AssertUtil.AssertContains(sheetData, new RowColumnIndex(0, 0), "ParamChild1");

            AssertUtil.AssertContains(sheetData, new RowColumnIndex(1, 0), "ParamChild2");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(1, 1), "Param2");

            AssertUtil.AssertContains(sheetData, new RowColumnIndex(2, 0), "ParamChild3");
        }

        [TestMethod]
        public void FilterNameStringOnTaxonomy()
        {
            this.LoadProject(mappingProject);

            var comp1 = projectData.Components.First(x => x.Name == "ParamMapping1");

            SimDataMappingRuleComponent compRule = new SimDataMappingRuleComponent("SheetA");
            compRule.MaxMatches = int.MaxValue;
            compRule.MaxDepth = int.MaxValue;
            compRule.TraversalStrategy = SimDataMappingRuleTraversalStrategy.SubtreeAndReferences;
            compRule.OffsetParent = new RowColumnIndex(0, 0);
            compRule.OffsetConsecutive = new RowColumnIndex(1, 0);
            compRule.Properties.Add(SimDataMappingComponentMappingProperties.Name);
            compRule.Filter.Add(new SimDataMappingFilterComponent(SimDataMappingComponentFilterProperties.Name, new Regex("ParamChild.*")));

            SimDataMappingRuleParameter rule = new SimDataMappingRuleParameter("SheetA");
            rule.MaxMatches = int.MaxValue;
            rule.MaxDepth = int.MaxValue;
            rule.OffsetParent = new RowColumnIndex(0, 1);
            rule.OffsetConsecutive = new RowColumnIndex(1, 0);
            rule.MappingDirection = SimDataMappingDirection.Horizontal;
            rule.Properties.Add(SimDataMappingParameterMappingProperties.Name);
            rule.Filter.Add(new SimDataMappingFilterParameter(SimDataMappingParameterFilterProperties.Name, "param3_key"));

            compRule.Rules.Add(rule);

            var state = new SimTraversalState();
            var data = new SimMappedData();
            compRule.Execute(comp1, state, data);

            Assert.AreEqual(1, data.Data.Count);
            Assert.IsTrue(data.Data.TryGetValue("SheetA", out var sheetData));

            Assert.AreEqual(4, sheetData.Count);

            AssertUtil.AssertContains(sheetData, new RowColumnIndex(0, 0), "ParamChild1");

            AssertUtil.AssertContains(sheetData, new RowColumnIndex(1, 0), "ParamChild2");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(1, 1), "param3_key");

            AssertUtil.AssertContains(sheetData, new RowColumnIndex(2, 0), "ParamChild3");
        }

        [TestMethod]
        public void FilterNameRegex()
        {
            this.LoadProject(mappingProject);

            var comp1 = projectData.Components.First(x => x.Name == "ParamMapping1");

            SimDataMappingRuleComponent compRule = new SimDataMappingRuleComponent("SheetA");
            compRule.MaxMatches = int.MaxValue;
            compRule.MaxDepth = int.MaxValue;
            compRule.TraversalStrategy = SimDataMappingRuleTraversalStrategy.SubtreeAndReferences;
            compRule.OffsetParent = new RowColumnIndex(0, 0);
            compRule.OffsetConsecutive = new RowColumnIndex(1, 0);
            compRule.Properties.Add(SimDataMappingComponentMappingProperties.Name);
            compRule.Filter.Add(new SimDataMappingFilterComponent(SimDataMappingComponentFilterProperties.Name, new Regex("ParamChild.*")));

            SimDataMappingRuleParameter rule = new SimDataMappingRuleParameter("SheetA");
            rule.MaxMatches = int.MaxValue;
            rule.MaxDepth = int.MaxValue;
            rule.OffsetParent = new RowColumnIndex(0, 1);
            rule.OffsetConsecutive = new RowColumnIndex(1, 0);
            rule.MappingDirection = SimDataMappingDirection.Horizontal;
            rule.Properties.Add(SimDataMappingParameterMappingProperties.Name);
            rule.Filter.Add(new SimDataMappingFilterParameter(SimDataMappingParameterFilterProperties.Name, new Regex("(Param2|param3_key)")));

            compRule.Rules.Add(rule);

            var state = new SimTraversalState();
            var data = new SimMappedData();
            compRule.Execute(comp1, state, data);

            Assert.AreEqual(1, data.Data.Count);
            Assert.IsTrue(data.Data.TryGetValue("SheetA", out var sheetData));

            Assert.AreEqual(5, sheetData.Count);

            AssertUtil.AssertContains(sheetData, new RowColumnIndex(0, 0), "ParamChild1");

            AssertUtil.AssertContains(sheetData, new RowColumnIndex(1, 0), "ParamChild2");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(1, 1), "Param2");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(2, 1), "param3_key");

            AssertUtil.AssertContains(sheetData, new RowColumnIndex(3, 0), "ParamChild3");
        }

        [TestMethod]
        public void FilterNameTaxonomy()
        {
            this.LoadProject(mappingProject);

            var comp1 = projectData.Components.First(x => x.Name == "ParamMapping1");

            SimDataMappingRuleComponent compRule = new SimDataMappingRuleComponent("SheetA");
            compRule.MaxMatches = int.MaxValue;
            compRule.MaxDepth = int.MaxValue;
            compRule.TraversalStrategy = SimDataMappingRuleTraversalStrategy.SubtreeAndReferences;
            compRule.OffsetParent = new RowColumnIndex(0, 0);
            compRule.OffsetConsecutive = new RowColumnIndex(1, 0);
            compRule.Properties.Add(SimDataMappingComponentMappingProperties.Name);
            compRule.Filter.Add(new SimDataMappingFilterComponent(SimDataMappingComponentFilterProperties.Name, new Regex("ParamChild.*")));

            SimDataMappingRuleParameter rule = new SimDataMappingRuleParameter("SheetA");
            rule.MaxMatches = int.MaxValue;
            rule.MaxDepth = int.MaxValue;
            rule.OffsetParent = new RowColumnIndex(0, 1);
            rule.OffsetConsecutive = new RowColumnIndex(1, 0);
            rule.MappingDirection = SimDataMappingDirection.Horizontal;
            rule.Properties.Add(SimDataMappingParameterMappingProperties.Name);
            rule.Filter.Add(new SimDataMappingFilterParameter(SimDataMappingParameterFilterProperties.Name,
                new SimTaxonomyEntryReference(
                    projectData.Taxonomies.First(x => x.Key == "tests").GetTaxonomyEntryByKey("param3_key")
                    )
                ));

            compRule.Rules.Add(rule);

            var state = new SimTraversalState();
            var data = new SimMappedData();
            compRule.Execute(comp1, state, data);

            Assert.AreEqual(1, data.Data.Count);
            Assert.IsTrue(data.Data.TryGetValue("SheetA", out var sheetData));

            Assert.AreEqual(4, sheetData.Count);

            AssertUtil.AssertContains(sheetData, new RowColumnIndex(0, 0), "ParamChild1");

            AssertUtil.AssertContains(sheetData, new RowColumnIndex(1, 0), "ParamChild2");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(1, 1), "param3_key");

            AssertUtil.AssertContains(sheetData, new RowColumnIndex(2, 0), "ParamChild3");
        }

        [TestMethod]
        public void FilterUnit()
        {
            this.LoadProject(mappingProject);

            var comp1 = projectData.Components.First(x => x.Name == "ParamMapping1");

            SimDataMappingRuleComponent compRule = new SimDataMappingRuleComponent("SheetA");
            compRule.MaxMatches = int.MaxValue;
            compRule.MaxDepth = int.MaxValue;
            compRule.TraversalStrategy = SimDataMappingRuleTraversalStrategy.SubtreeAndReferences;
            compRule.OffsetParent = new RowColumnIndex(0, 0);
            compRule.OffsetConsecutive = new RowColumnIndex(1, 0);
            compRule.Properties.Add(SimDataMappingComponentMappingProperties.Name);
            compRule.Filter.Add(new SimDataMappingFilterComponent(SimDataMappingComponentFilterProperties.Name, new Regex("ParamChild.*")));

            SimDataMappingRuleParameter rule = new SimDataMappingRuleParameter("SheetA");
            rule.MaxMatches = int.MaxValue;
            rule.MaxDepth = int.MaxValue;
            rule.OffsetParent = new RowColumnIndex(0, 1);
            rule.OffsetConsecutive = new RowColumnIndex(1, 0);
            rule.MappingDirection = SimDataMappingDirection.Horizontal;
            rule.Properties.Add(SimDataMappingParameterMappingProperties.Name);
            rule.Filter.Add(new SimDataMappingFilterParameter(SimDataMappingParameterFilterProperties.Unit, "b/c²"));

            compRule.Rules.Add(rule);

            var state = new SimTraversalState();
            var data = new SimMappedData();
            compRule.Execute(comp1, state, data);

            Assert.AreEqual(1, data.Data.Count);
            Assert.IsTrue(data.Data.TryGetValue("SheetA", out var sheetData));

            Assert.AreEqual(5, sheetData.Count);

            AssertUtil.AssertContains(sheetData, new RowColumnIndex(0, 0), "ParamChild1");

            AssertUtil.AssertContains(sheetData, new RowColumnIndex(1, 0), "ParamChild2");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(1, 1), "Param2");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(2, 1), "param3_key");

            AssertUtil.AssertContains(sheetData, new RowColumnIndex(3, 0), "ParamChild3");
        }

        [TestMethod]
        public void FilterPropagation()
        {
            this.LoadProject(mappingProject);

            var comp1 = projectData.Components.First(x => x.Name == "ParamMapping1");

            SimDataMappingRuleComponent compRule = new SimDataMappingRuleComponent("SheetA");
            compRule.MaxMatches = int.MaxValue;
            compRule.MaxDepth = int.MaxValue;
            compRule.TraversalStrategy = SimDataMappingRuleTraversalStrategy.SubtreeAndReferences;
            compRule.OffsetParent = new RowColumnIndex(0, 0);
            compRule.OffsetConsecutive = new RowColumnIndex(1, 0);
            compRule.Properties.Add(SimDataMappingComponentMappingProperties.Name);
            compRule.Filter.Add(new SimDataMappingFilterComponent(SimDataMappingComponentFilterProperties.Name, new Regex("ParamChild.*")));

            SimDataMappingRuleParameter rule = new SimDataMappingRuleParameter("SheetA");
            rule.MaxMatches = int.MaxValue;
            rule.MaxDepth = int.MaxValue;
            rule.OffsetParent = new RowColumnIndex(0, 1);
            rule.OffsetConsecutive = new RowColumnIndex(1, 0);
            rule.MappingDirection = SimDataMappingDirection.Horizontal;
            rule.Properties.Add(SimDataMappingParameterMappingProperties.Name);
            rule.Filter.Add(new SimDataMappingFilterParameter(SimDataMappingParameterFilterProperties.Propagation, SimInfoFlow.Input));

            compRule.Rules.Add(rule);

            var state = new SimTraversalState();
            var data = new SimMappedData();
            compRule.Execute(comp1, state, data);

            Assert.AreEqual(1, data.Data.Count);
            Assert.IsTrue(data.Data.TryGetValue("SheetA", out var sheetData));

            Assert.AreEqual(4, sheetData.Count);

            AssertUtil.AssertContains(sheetData, new RowColumnIndex(0, 0), "ParamChild1");

            AssertUtil.AssertContains(sheetData, new RowColumnIndex(1, 0), "ParamChild2");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(1, 1), "param3_key");

            AssertUtil.AssertContains(sheetData, new RowColumnIndex(2, 0), "ParamChild3");
        }

        [TestMethod]
        public void FilterCategory()
        {
            this.LoadProject(mappingProject);

            var comp1 = projectData.Components.First(x => x.Name == "ParamMapping1");

            SimDataMappingRuleComponent compRule = new SimDataMappingRuleComponent("SheetA");
            compRule.MaxMatches = int.MaxValue;
            compRule.MaxDepth = int.MaxValue;
            compRule.TraversalStrategy = SimDataMappingRuleTraversalStrategy.SubtreeAndReferences;
            compRule.OffsetParent = new RowColumnIndex(0, 0);
            compRule.OffsetConsecutive = new RowColumnIndex(1, 0);
            compRule.Properties.Add(SimDataMappingComponentMappingProperties.Name);
            compRule.Filter.Add(new SimDataMappingFilterComponent(SimDataMappingComponentFilterProperties.Name, new Regex("ParamChild.*")));

            SimDataMappingRuleParameter rule = new SimDataMappingRuleParameter("SheetA");
            rule.MaxMatches = int.MaxValue;
            rule.MaxDepth = int.MaxValue;
            rule.OffsetParent = new RowColumnIndex(0, 1);
            rule.OffsetConsecutive = new RowColumnIndex(1, 0);
            rule.MappingDirection = SimDataMappingDirection.Horizontal;
            rule.Properties.Add(SimDataMappingParameterMappingProperties.Name);
            rule.Filter.Add(new SimDataMappingFilterParameter(SimDataMappingParameterFilterProperties.Category, SimCategory.Costs));

            compRule.Rules.Add(rule);

            var state = new SimTraversalState();
            var data = new SimMappedData();
            compRule.Execute(comp1, state, data);

            Assert.AreEqual(1, data.Data.Count);
            Assert.IsTrue(data.Data.TryGetValue("SheetA", out var sheetData));

            Assert.AreEqual(4, sheetData.Count);

            AssertUtil.AssertContains(sheetData, new RowColumnIndex(0, 0), "ParamChild1");

            AssertUtil.AssertContains(sheetData, new RowColumnIndex(1, 0), "ParamChild2");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(1, 1), "param3_key");

            AssertUtil.AssertContains(sheetData, new RowColumnIndex(2, 0), "ParamChild3");
        }

        [TestMethod]
        public void FilterCategoryMultiple()
        {
            this.LoadProject(mappingProject);

            var comp1 = projectData.Components.First(x => x.Name == "ParamMapping1");

            SimDataMappingRuleComponent compRule = new SimDataMappingRuleComponent("SheetA");
            compRule.MaxMatches = int.MaxValue;
            compRule.MaxDepth = int.MaxValue;
            compRule.TraversalStrategy = SimDataMappingRuleTraversalStrategy.SubtreeAndReferences;
            compRule.OffsetParent = new RowColumnIndex(0, 0);
            compRule.OffsetConsecutive = new RowColumnIndex(1, 0);
            compRule.Properties.Add(SimDataMappingComponentMappingProperties.Name);
            compRule.Filter.Add(new SimDataMappingFilterComponent(SimDataMappingComponentFilterProperties.Name, new Regex("ParamChild.*")));

            SimDataMappingRuleParameter rule = new SimDataMappingRuleParameter("SheetA");
            rule.MaxMatches = int.MaxValue;
            rule.MaxDepth = int.MaxValue;
            rule.OffsetParent = new RowColumnIndex(0, 1);
            rule.OffsetConsecutive = new RowColumnIndex(1, 0);
            rule.MappingDirection = SimDataMappingDirection.Horizontal;
            rule.Properties.Add(SimDataMappingParameterMappingProperties.Name);
            rule.Filter.Add(new SimDataMappingFilterParameter(SimDataMappingParameterFilterProperties.Category, SimCategory.Light_Artificial));

            compRule.Rules.Add(rule);

            var state = new SimTraversalState();
            var data = new SimMappedData();
            compRule.Execute(comp1, state, data);

            Assert.AreEqual(1, data.Data.Count);
            Assert.IsTrue(data.Data.TryGetValue("SheetA", out var sheetData));

            Assert.AreEqual(5, sheetData.Count);

            AssertUtil.AssertContains(sheetData, new RowColumnIndex(0, 0), "ParamChild1");

            AssertUtil.AssertContains(sheetData, new RowColumnIndex(1, 0), "ParamChild2");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(1, 1), "Param2");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(2, 1), "param3_key");

            AssertUtil.AssertContains(sheetData, new RowColumnIndex(3, 0), "ParamChild3");
        }

        [TestMethod]
        public void FilterCategoryFlags()
        {
            this.LoadProject(mappingProject);

            var comp1 = projectData.Components.First(x => x.Name == "ParamMapping1");

            SimDataMappingRuleComponent compRule = new SimDataMappingRuleComponent("SheetA");
            compRule.MaxMatches = int.MaxValue;
            compRule.MaxDepth = int.MaxValue;
            compRule.TraversalStrategy = SimDataMappingRuleTraversalStrategy.SubtreeAndReferences;
            compRule.OffsetParent = new RowColumnIndex(0, 0);
            compRule.OffsetConsecutive = new RowColumnIndex(1, 0);
            compRule.Properties.Add(SimDataMappingComponentMappingProperties.Name);
            compRule.Filter.Add(new SimDataMappingFilterComponent(SimDataMappingComponentFilterProperties.Name, new Regex("ParamChild.*")));

            SimDataMappingRuleParameter rule = new SimDataMappingRuleParameter("SheetA");
            rule.MaxMatches = int.MaxValue;
            rule.MaxDepth = int.MaxValue;
            rule.OffsetParent = new RowColumnIndex(0, 1);
            rule.OffsetConsecutive = new RowColumnIndex(1, 0);
            rule.MappingDirection = SimDataMappingDirection.Horizontal;
            rule.Properties.Add(SimDataMappingParameterMappingProperties.Name);
            rule.Filter.Add(new SimDataMappingFilterParameter(SimDataMappingParameterFilterProperties.Category, SimCategory.Costs | SimCategory.Light_Artificial));

            compRule.Rules.Add(rule);

            var state = new SimTraversalState();
            var data = new SimMappedData();
            compRule.Execute(comp1, state, data);

            Assert.AreEqual(1, data.Data.Count);
            Assert.IsTrue(data.Data.TryGetValue("SheetA", out var sheetData));

            Assert.AreEqual(4, sheetData.Count);

            AssertUtil.AssertContains(sheetData, new RowColumnIndex(0, 0), "ParamChild1");

            AssertUtil.AssertContains(sheetData, new RowColumnIndex(1, 0), "ParamChild2");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(1, 1), "param3_key");

            AssertUtil.AssertContains(sheetData, new RowColumnIndex(2, 0), "ParamChild3");
        }

        #endregion

        [TestMethod]
        public void MaxMatches()
        {
            this.LoadProject(mappingProject);

            var comp1 = projectData.Components.First(x => x.Name == "ParamMapping1");

            SimDataMappingRuleComponent compRule = new SimDataMappingRuleComponent("SheetA");
            compRule.MaxMatches = int.MaxValue;
            compRule.MaxDepth = int.MaxValue;
            compRule.TraversalStrategy = SimDataMappingRuleTraversalStrategy.SubtreeAndReferences;
            compRule.OffsetParent = new RowColumnIndex(0, 0);
            compRule.OffsetConsecutive = new RowColumnIndex(1, 0);
            compRule.Properties.Add(SimDataMappingComponentMappingProperties.Name);
            compRule.Properties.Add(SimDataMappingComponentMappingProperties.Slot);
            compRule.Filter.Add(new SimDataMappingFilterComponent(SimDataMappingComponentFilterProperties.Name, new Regex("ParamChild.*")));

            SimDataMappingRuleParameter rule = new SimDataMappingRuleParameter("SheetA");
            rule.MaxMatches = 1;
            rule.MaxDepth = int.MaxValue;
            rule.ReferencePointParent = SimDataMappingReferencePoint.TopRight;
            rule.OffsetParent = new RowColumnIndex(0, 1);
            rule.ReferencePointConsecutive = SimDataMappingReferencePoint.BottomLeft;
            rule.OffsetConsecutive = new RowColumnIndex(1, 0);
            rule.MappingDirection = SimDataMappingDirection.Horizontal;
            rule.Properties.Add(SimDataMappingParameterMappingProperties.Name);
            rule.Properties.Add(SimDataMappingParameterMappingProperties.Value);

            compRule.Rules.Add(rule);

            var state = new SimTraversalState();
            var data = new SimMappedData();
            compRule.Execute(comp1, state, data);

            Assert.AreEqual(1, data.Data.Count);
            Assert.IsTrue(data.Data.TryGetValue("SheetA", out var sheetData));

            Assert.AreEqual(12, sheetData.Count);

            AssertUtil.AssertContains(sheetData, new RowColumnIndex(0, 0), "ParamChild1");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(0, 1), "undefined_0");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(0, 2), "Param1");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(0, 3), 1.1);

            AssertUtil.AssertContains(sheetData, new RowColumnIndex(1, 0), "ParamChild2");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(1, 1), "undefined_1");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(1, 2), "Param2");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(1, 3), 2.2);

            AssertUtil.AssertContains(sheetData, new RowColumnIndex(2, 0), "ParamChild3");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(2, 1), "undefined_2");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(2, 2), "Param4");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(2, 3), 4.4);
        }

        [TestMethod]
        public void MaxMatchesInstance()
        {
            this.LoadProject(mappingProject);

            var comp1 = projectData.Components.First(x => x.Name == "Component1");

            SimDataMappingRuleComponent componentRule = new SimDataMappingRuleComponent("SheetA");
            componentRule.MappingDirection = SimDataMappingDirection.Vertical;
            componentRule.MaxMatches = int.MaxValue;
            componentRule.MaxDepth = int.MaxValue;
            componentRule.Filter.Add(new SimDataMappingFilterComponent(SimDataMappingComponentFilterProperties.Name, "ChildComponent1"));

            SimDataMappingRuleInstance instanceRule = new SimDataMappingRuleInstance("SheetA");
            instanceRule.MappingDirection = SimDataMappingDirection.Vertical;
            instanceRule.MaxMatches = int.MaxValue;
            instanceRule.MaxDepth = int.MaxValue;
            instanceRule.OffsetConsecutive = new RowColumnIndex(1, 0);
            instanceRule.Properties.Add(SimDataMappingInstanceMappingProperties.Name);
            componentRule.Rules.Add(instanceRule);

            SimDataMappingRuleParameter parameterRule = new SimDataMappingRuleParameter("SheetA");
            parameterRule.MappingDirection = SimDataMappingDirection.Vertical;
            parameterRule.MaxMatches = 1;
            parameterRule.MaxDepth = int.MaxValue;
            parameterRule.OffsetParent = new RowColumnIndex(0, 1);
            parameterRule.OffsetConsecutive = new RowColumnIndex(1, 0);
            parameterRule.Properties.Add(SimDataMappingParameterMappingProperties.Name);
            instanceRule.Rules.Add(parameterRule);

            var state = new SimTraversalState();
            var data = new SimMappedData();
            componentRule.Execute(comp1, state, data);

            Assert.AreEqual(1, data.Data.Count);
            Assert.IsTrue(data.Data.TryGetValue("SheetA", out var sheetData));

            Assert.AreEqual(4, sheetData.Count);
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(0, 0), "Geometry Placement 0:13");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(0, 1), "din");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(1, 0), "Geometry Placement 0:20");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(1, 1), "din");
        }

        [TestMethod]
        public void CloneTest()
        {
            SimDataMappingRuleParameter parameterRule = new SimDataMappingRuleParameter("Sheet A")
            {
                Name = "SomeName",
                MappingDirection = SimDataMappingDirection.Vertical,
                MaxDepth = 3,
                MaxMatches = 5,
                OffsetConsecutive = new RowColumnIndex(8, 7),
                OffsetParent = new RowColumnIndex(10, 9),
                ReferencePointParent = SimDataMappingReferencePoint.TopRight,
                ParameterRange = SimDataMappingParameterRange.Table,
            };

            parameterRule.Filter.Add(new SimDataMappingFilterParameter(SimDataMappingParameterFilterProperties.Name, "asdf"));
            parameterRule.Filter.Add(new SimDataMappingFilterParameter(SimDataMappingParameterFilterProperties.Unit, "c/d²"));

            parameterRule.Properties.Add(SimDataMappingParameterMappingProperties.Name);
            parameterRule.Properties.Add(SimDataMappingParameterMappingProperties.Value);

            //Test
            var clonedRule = parameterRule.Clone();

            //Check
            Assert.AreEqual("Sheet A", clonedRule.SheetName);
            Assert.AreEqual("SomeName", clonedRule.Name);
            Assert.AreEqual(SimDataMappingDirection.Vertical, clonedRule.MappingDirection);
            Assert.AreEqual(3, clonedRule.MaxDepth);
            Assert.AreEqual(5, clonedRule.MaxMatches);
            Assert.AreEqual(7, clonedRule.OffsetConsecutive.Column);
            Assert.AreEqual(8, clonedRule.OffsetConsecutive.Row);
            Assert.AreEqual(9, clonedRule.OffsetParent.Column);
            Assert.AreEqual(10, clonedRule.OffsetParent.Row);
            Assert.AreEqual(SimDataMappingReferencePoint.TopRight, clonedRule.ReferencePointParent);
            Assert.AreEqual(SimDataMappingParameterRange.Table, clonedRule.ParameterRange);

            Assert.AreEqual(2, clonedRule.Filter.Count);
            Assert.AreEqual(SimDataMappingParameterFilterProperties.Name, clonedRule.Filter[0].Property);
            Assert.AreEqual("asdf", clonedRule.Filter[0].Value);
            Assert.AreEqual(SimDataMappingParameterFilterProperties.Unit, clonedRule.Filter[1].Property);
            Assert.AreEqual("c/d²", clonedRule.Filter[1].Value);

            Assert.AreEqual(2, clonedRule.Properties.Count);
            Assert.AreEqual(SimDataMappingParameterMappingProperties.Name, clonedRule.Properties[0]);
            Assert.AreEqual(SimDataMappingParameterMappingProperties.Value, clonedRule.Properties[1]);
        }
    }
}
