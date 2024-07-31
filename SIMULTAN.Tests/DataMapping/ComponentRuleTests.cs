using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data.Components;
using SIMULTAN.Data.Taxonomy;
using SIMULTAN.DataMapping;
using SIMULTAN.Tests.TestUtils;
using SIMULTAN.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SIMULTAN.Tests.DataMapping
{
    [TestClass]
    public class ComponentRuleTests : BaseProjectTest
    {
        private static readonly FileInfo mappingProject = new FileInfo(@"./ExcelMappingTests.simultan");

        #region Basic

        [TestMethod]
        public void RootMapHorizontal()
        {
            this.LoadProject(mappingProject);

            var comp1 = projectData.Components.First(x => x.Name == "Component1");

            SimDataMappingRuleComponent rule = new SimDataMappingRuleComponent("SheetA");
            rule.MappingDirection = SimDataMappingDirection.Horizontal;
            rule.Properties.Add(SimDataMappingComponentMappingProperties.Name);
            rule.Properties.Add(SimDataMappingComponentMappingProperties.Id);
            rule.Properties.Add(SimDataMappingComponentMappingProperties.Slot);

            var state = new SimTraversalState();
            var data = new SimMappedData();
            rule.Execute(comp1, state, data);

            Assert.AreEqual(1, data.Data.Count);
            Assert.IsTrue(data.Data.TryGetValue("SheetA", out var sheetData));

            Assert.AreEqual(3, sheetData.Count);
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(0, 0), "Component1");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(0, 1), 64);
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(0, 2), "heating");
        }

        [TestMethod]
        public void RootMapVertical()
        {
            this.LoadProject(mappingProject);

            var comp1 = projectData.Components.First(x => x.Name == "Component1");

            SimDataMappingRuleComponent rule = new SimDataMappingRuleComponent("SheetA");
            rule.MappingDirection = SimDataMappingDirection.Vertical;
            rule.Properties.Add(SimDataMappingComponentMappingProperties.Name);
            rule.Properties.Add(SimDataMappingComponentMappingProperties.Id);
            rule.Properties.Add(SimDataMappingComponentMappingProperties.Slot);

            var state = new SimTraversalState();
            var data = new SimMappedData();
            rule.Execute(comp1, state, data);

            Assert.AreEqual(1, data.Data.Count);
            Assert.IsTrue(data.Data.TryGetValue("SheetA", out var sheetData));

            Assert.AreEqual(3, sheetData.Count);
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(0, 0), "Component1");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(1, 0), 64);
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(2, 0), "heating");
        }

        [TestMethod]
        public void RootOffsetParent()
        {
            this.LoadProject(mappingProject);

            var comp1 = projectData.Components.First(x => x.Name == "Component1");

            SimDataMappingRuleComponent rule = new SimDataMappingRuleComponent("SheetA");
            rule.MappingDirection = SimDataMappingDirection.Horizontal;
            rule.OffsetParent = new RowColumnIndex(3, 2);
            rule.Properties.Add(SimDataMappingComponentMappingProperties.Name);
            rule.Properties.Add(SimDataMappingComponentMappingProperties.Id);
            rule.Properties.Add(SimDataMappingComponentMappingProperties.Slot);

            var state = new SimTraversalState();
            var data = new SimMappedData();
            rule.Execute(comp1, state, data);

            Assert.AreEqual(1, data.Data.Count);
            Assert.IsTrue(data.Data.TryGetValue("SheetA", out var sheetData));

            Assert.AreEqual(3, sheetData.Count);
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(3, 2), "Component1");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(3, 3), 64);
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(3, 4), "heating");
        }

        #endregion


        #region Filter

        [TestMethod]
        public void FilterNameString()
        {
            this.LoadProject(mappingProject);

            var comp1 = projectData.Components.First(x => x.Name == "Component1");

            SimDataMappingRuleComponent rule = new SimDataMappingRuleComponent("SheetA");
            rule.MappingDirection = SimDataMappingDirection.Horizontal;
            rule.Properties.Add(SimDataMappingComponentMappingProperties.Name);

            rule.Filter.Add(new SimDataMappingFilterComponent(SimDataMappingComponentFilterProperties.Name, "X"));

            var state = new SimTraversalState();
            var data = new SimMappedData();
            rule.Execute(comp1, state, data);

            Assert.AreEqual(0, data.Data.Count);

            rule.Filter.Clear();
            rule.Filter.Add(new SimDataMappingFilterComponent(SimDataMappingComponentFilterProperties.Name, "Component1"));

            state = new SimTraversalState();
            rule.Execute(comp1, state, data);

            Assert.AreEqual(1, data.Data.Count);
            Assert.IsTrue(data.Data.TryGetValue("SheetA", out var sheetData));
            Assert.AreEqual(1, sheetData.Count);
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(0, 0), "Component1");
        }

        [TestMethod]
        public void FilterNameRegex()
        {
            this.LoadProject(mappingProject);

            var comp1 = projectData.Components.First(x => x.Name == "Component1");

            SimDataMappingRuleComponent rule = new SimDataMappingRuleComponent("SheetA");
            rule.MappingDirection = SimDataMappingDirection.Horizontal;
            rule.Properties.Add(SimDataMappingComponentMappingProperties.Name);

            rule.Filter.Add(new SimDataMappingFilterComponent(SimDataMappingComponentFilterProperties.Name, new Regex("X+")));

            var state = new SimTraversalState();
            var data = new SimMappedData();
            rule.Execute(comp1, state, data);

            Assert.AreEqual(0, data.Data.Count);

            rule.Filter.Clear();
            rule.Filter.Add(new SimDataMappingFilterComponent(SimDataMappingComponentFilterProperties.Name, new Regex(".*nent.*")));

            state = new SimTraversalState();
            rule.Execute(comp1, state, data);

            Assert.AreEqual(1, data.Data.Count);
            Assert.IsTrue(data.Data.TryGetValue("SheetA", out var sheetData));
            Assert.AreEqual(1, sheetData.Count);
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(0, 0), "Component1");


        }

        [TestMethod]
        public void RootFilterSlot()
        {
            this.LoadProject(mappingProject);

            var comp1 = projectData.Components.First(x => x.Name == "Component1");
            var compTaxEntry = projectData.Taxonomies.GetDefaultSlot(SimDefaultSlotKeys.Composite);
            var heatTaxEntry = projectData.Taxonomies.GetDefaultSlot(SimDefaultSlotKeys.Heating);

            SimDataMappingRuleComponent rule = new SimDataMappingRuleComponent("SheetA");
            rule.MappingDirection = SimDataMappingDirection.Horizontal;
            rule.Properties.Add(SimDataMappingComponentMappingProperties.Name);

            rule.Filter.Add(new SimDataMappingFilterComponent(SimDataMappingComponentFilterProperties.Slot,
                new SimTaxonomyEntryReference(compTaxEntry)));

            var state = new SimTraversalState();
            var data = new SimMappedData();
            rule.Execute(comp1, state, data);

            Assert.AreEqual(0, data.Data.Count);

            rule.Filter.Clear();
            rule.Filter.Add(new SimDataMappingFilterComponent(SimDataMappingComponentFilterProperties.Slot,
                new SimTaxonomyEntryReference(heatTaxEntry)));

            state = new SimTraversalState();
            rule.Execute(comp1, state, data);

            Assert.AreEqual(1, data.Data.Count);
            Assert.IsTrue(data.Data.TryGetValue("SheetA", out var sheetData));
            Assert.AreEqual(1, sheetData.Count);
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(0, 0), "Component1");
        }

        [TestMethod]
        public void ChildFilterSlot()
        {
            this.LoadProject(mappingProject);

            var comp1 = projectData.Components.First(x => x.Name == "Component1");
            var nonmatchingTaxEntry = projectData.Taxonomies.GetDefaultSlot(SimDefaultSlotKeys.Composite);
            var matchingTaxEntry = projectData.Taxonomies.GetDefaultSlot(SimDefaultSlotKeys.Layer);

            SimDataMappingRuleComponent rule = new SimDataMappingRuleComponent("SheetA");
            rule.MaxMatches = int.MaxValue;
            rule.MaxDepth = int.MaxValue;
            rule.MappingDirection = SimDataMappingDirection.Horizontal;
            rule.OffsetConsecutive = new RowColumnIndex(1, 0);
            rule.Properties.Add(SimDataMappingComponentMappingProperties.Name);

            rule.Filter.Add(new SimDataMappingFilterComponent(SimDataMappingComponentFilterProperties.Slot,
                new SimTaxonomyEntryReference(nonmatchingTaxEntry)));

            var state = new SimTraversalState();
            var data = new SimMappedData();
            rule.Execute(comp1, state, data);

            Assert.AreEqual(0, data.Data.Count);

            rule.Filter.Clear();
            rule.Filter.Add(new SimDataMappingFilterComponent(SimDataMappingComponentFilterProperties.Slot,
                new SimTaxonomyEntryReference(matchingTaxEntry)));

            state = new SimTraversalState();
            rule.Execute(comp1, state, data);

            Assert.AreEqual(1, data.Data.Count);
            Assert.IsTrue(data.Data.TryGetValue("SheetA", out var sheetData));
            Assert.AreEqual(4, sheetData.Count);
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(0, 0), "ChildComponent1");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(1, 0), "ChildComponent2");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(2, 0), "ChildComponent3");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(3, 0), "ChildComponent4");
        }

        [TestMethod]
        public void ChildFilterSlotWithExtension()
        {
            this.LoadProject(mappingProject);

            var comp1 = projectData.Components.First(x => x.Name == "Component1");
            var matchingTaxEntry = projectData.Taxonomies.GetDefaultSlot(SimDefaultSlotKeys.Layer);

            SimDataMappingRuleComponent rule = new SimDataMappingRuleComponent("SheetA");
            rule.MaxMatches = int.MaxValue;
            rule.MaxDepth = int.MaxValue;
            rule.MappingDirection = SimDataMappingDirection.Horizontal;
            rule.OffsetConsecutive = new RowColumnIndex(1, 0);
            rule.Properties.Add(SimDataMappingComponentMappingProperties.Name);

            rule.Filter.Add(new SimDataMappingFilterComponent(SimDataMappingComponentFilterProperties.Slot,
                new SimSlot(new SimTaxonomyEntryReference(matchingTaxEntry), "9")));

            var state = new SimTraversalState();
            var data = new SimMappedData();
            rule.Execute(comp1, state, data);

            Assert.AreEqual(0, data.Data.Count);

            rule.Filter.Clear();
            rule.Filter.Add(new SimDataMappingFilterComponent(SimDataMappingComponentFilterProperties.Slot,
                new SimSlot(new SimTaxonomyEntryReference(matchingTaxEntry), "2")));

            state = new SimTraversalState();
            rule.Execute(comp1, state, data);

            Assert.AreEqual(1, data.Data.Count);
            Assert.IsTrue(data.Data.TryGetValue("SheetA", out var sheetData));
            Assert.AreEqual(2, sheetData.Count);
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(0, 0), "ChildComponent3");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(1, 0), "ChildComponent4");
        }

        [TestMethod]
        public void FilterInstanceType()
        {
            this.LoadProject(mappingProject);

            var comp1 = projectData.Components.First(x => x.Name == "Component1");
            var compTaxEntry = projectData.Taxonomies.GetDefaultSlot(SimDefaultSlotKeys.Composite);
            var heatTaxEntry = projectData.Taxonomies.GetDefaultSlot(SimDefaultSlotKeys.Heating);

            SimDataMappingRuleComponent rule = new SimDataMappingRuleComponent("SheetA");
            rule.MaxMatches = int.MaxValue;
            rule.MaxDepth = int.MaxValue;
            rule.MappingDirection = SimDataMappingDirection.Horizontal;
            rule.OffsetConsecutive = new RowColumnIndex(1, 0);
            rule.Properties.Add(SimDataMappingComponentMappingProperties.Name);

            rule.Filter.Add(new SimDataMappingFilterComponent(SimDataMappingComponentFilterProperties.InstanceType,
                SimInstanceType.AttributesEdge));

            var state = new SimTraversalState();
            var data = new SimMappedData();
            rule.Execute(comp1, state, data);

            Assert.AreEqual(0, data.Data.Count);

            rule.Filter.Clear();
            rule.Filter.Add(new SimDataMappingFilterComponent(SimDataMappingComponentFilterProperties.InstanceType,
                SimInstanceType.AttributesFace));

            state = new SimTraversalState();
            rule.Execute(comp1, state, data);

            Assert.AreEqual(1, data.Data.Count);
            Assert.IsTrue(data.Data.TryGetValue("SheetA", out var sheetData));
            Assert.AreEqual(2, sheetData.Count);
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(0, 0), "ChildComponent1");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(1, 0), "ChildComponent3");
        }

        [TestMethod]
        public void FilterInstanceState()
        {
            this.LoadProject(mappingProject);

            var comp1 = projectData.Components.First(x => x.Name == "Component1");
            var compTaxEntry = projectData.Taxonomies.GetDefaultSlot(SimDefaultSlotKeys.Composite);
            var heatTaxEntry = projectData.Taxonomies.GetDefaultSlot(SimDefaultSlotKeys.Heating);

            SimDataMappingRuleComponent rule = new SimDataMappingRuleComponent("SheetA");
            rule.MaxMatches = int.MaxValue;
            rule.MaxDepth = int.MaxValue;
            rule.MappingDirection = SimDataMappingDirection.Horizontal;
            rule.OffsetConsecutive = new RowColumnIndex(1, 0);
            rule.Properties.Add(SimDataMappingComponentMappingProperties.Name);
            rule.Filter.Add(new SimDataMappingFilterComponent(SimDataMappingComponentFilterProperties.InstanceIsRealized, true));

            var data = new SimMappedData();
            var state = new SimTraversalState();
            rule.Execute(comp1, state, data);

            Assert.AreEqual(1, data.Data.Count);
            Assert.IsTrue(data.Data.TryGetValue("SheetA", out var sheetData));
            Assert.AreEqual(2, sheetData.Count);
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(0, 0), "ChildComponent1");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(1, 0), "ChildComponent2");
        }

        [TestMethod]
        public void FilterMultiple()
        {
            this.LoadProject(mappingProject);

            var comp1 = projectData.Components.First(x => x.Name == "Component1");
            var compTaxEntry = projectData.Taxonomies.GetDefaultSlot(SimDefaultSlotKeys.Composite);
            var heatTaxEntry = projectData.Taxonomies.GetDefaultSlot(SimDefaultSlotKeys.Heating);

            SimDataMappingRuleComponent rule = new SimDataMappingRuleComponent("SheetA");
            rule.MaxMatches = int.MaxValue;
            rule.MaxDepth = int.MaxValue;
            rule.MappingDirection = SimDataMappingDirection.Horizontal;
            rule.OffsetConsecutive = new RowColumnIndex(1, 0);
            rule.Properties.Add(SimDataMappingComponentMappingProperties.Name);
            rule.Filter.Add(new SimDataMappingFilterComponent(SimDataMappingComponentFilterProperties.InstanceIsRealized,
                true));
            rule.Filter.Add(new SimDataMappingFilterComponent(SimDataMappingComponentFilterProperties.InstanceType,
                SimInstanceType.AttributesFace));

            var data = new SimMappedData();
            var state = new SimTraversalState();
            rule.Execute(comp1, state, data);

            Assert.AreEqual(1, data.Data.Count);
            Assert.IsTrue(data.Data.TryGetValue("SheetA", out var sheetData));
            Assert.AreEqual(1, sheetData.Count);
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(0, 0), "ChildComponent1");
        }

        #endregion

        #region Child Components

        [TestMethod]
        public void SubtreeFilter()
        {
            this.LoadProject(mappingProject);

            var comp1 = projectData.Components.First(x => x.Name == "Component1");

            SimDataMappingRuleComponent rule = new SimDataMappingRuleComponent("SheetA");
            rule.MaxMatches = int.MaxValue;
            rule.MaxDepth = int.MaxValue;
            rule.MappingDirection = SimDataMappingDirection.Horizontal;
            rule.OffsetParent = new RowColumnIndex(3, 2);
            rule.ReferencePointParent = SimDataMappingReferencePoint.TopRight;
            rule.OffsetConsecutive = new RowColumnIndex(0, 1);
            rule.Properties.Add(SimDataMappingComponentMappingProperties.Name);

            rule.Filter.Add(new SimDataMappingFilterComponent(SimDataMappingComponentFilterProperties.Name, new Regex("Child.*")));

            var state = new SimTraversalState();
            var data = new SimMappedData();
            rule.Execute(comp1, state, data);

            Assert.AreEqual(1, data.Data.Count);
            Assert.IsTrue(data.Data.TryGetValue("SheetA", out var sheetData));
            Assert.AreEqual(4, sheetData.Count);
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(3, 2), "ChildComponent1");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(3, 3), "ChildComponent2");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(3, 4), "ChildComponent3");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(3, 5), "ChildComponent4");
        }

        [TestMethod]
        public void SubtreeRange()
        {
            this.LoadProject(mappingProject);

            var comp1 = projectData.Components.First(x => x.Name == "Component1");

            SimDataMappingRuleComponent rule = new SimDataMappingRuleComponent("SheetA");
            rule.MaxMatches = int.MaxValue;
            rule.MaxDepth = int.MaxValue;
            rule.MappingDirection = SimDataMappingDirection.Horizontal;
            rule.OffsetParent = new RowColumnIndex(3, 2);
            rule.OffsetConsecutive = new RowColumnIndex(1, 0);
            rule.Properties.Add(SimDataMappingComponentMappingProperties.Name);
            rule.Properties.Add(SimDataMappingComponentMappingProperties.Id);

            rule.Filter.Add(new SimDataMappingFilterComponent(SimDataMappingComponentFilterProperties.Name, new Regex("Child.*")));

            var state = new SimTraversalState();
            var data = new SimMappedData();
            rule.Execute(comp1, state, data);

            Assert.AreEqual(4, state.RangeStack.Count);

            for (int i = 0; i < 4; i++)
            {
                Assert.AreEqual(3 + i, state.RangeStack[i].RowStart);
                Assert.AreEqual(1, state.RangeStack[i].RowCount);
                Assert.AreEqual(2, state.RangeStack[i].ColumnStart);
                Assert.AreEqual(2, state.RangeStack[i].ColumnCount);
            }
        }

        [TestMethod]
        public void MaxElements()
        {
            this.LoadProject(mappingProject);

            var comp1 = projectData.Components.First(x => x.Name == "Component1");

            SimDataMappingRuleComponent rule = new SimDataMappingRuleComponent("SheetA");
            rule.MaxMatches = 2;
            rule.MaxDepth = int.MaxValue;
            rule.MappingDirection = SimDataMappingDirection.Horizontal;
            rule.OffsetParent = new RowColumnIndex(3, 2);
            rule.ReferencePointParent = SimDataMappingReferencePoint.TopRight;
            rule.OffsetConsecutive = new RowColumnIndex(0, 1);
            rule.Properties.Add(SimDataMappingComponentMappingProperties.Name);

            rule.Filter.Add(new SimDataMappingFilterComponent(SimDataMappingComponentFilterProperties.Name, new Regex("Child.*")));

            var state = new SimTraversalState();
            var data = new SimMappedData();
            rule.Execute(comp1, state, data);

            Assert.AreEqual(1, data.Data.Count);
            Assert.IsTrue(data.Data.TryGetValue("SheetA", out var sheetData));
            Assert.AreEqual(2, sheetData.Count);
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(3, 2), "ChildComponent1");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(3, 3), "ChildComponent2");
        }

        [TestMethod]
        public void MaxDepth()
        {
            this.LoadProject(mappingProject);

            var comp1 = projectData.Components.First(x => x.Name == "Component1");

            SimDataMappingRuleComponent rule = new SimDataMappingRuleComponent("SheetA");
            rule.MaxMatches = int.MaxValue;
            rule.MaxDepth = 2;
            rule.MappingDirection = SimDataMappingDirection.Horizontal;
            rule.OffsetParent = new RowColumnIndex(3, 2);
            rule.ReferencePointParent = SimDataMappingReferencePoint.TopRight;
            rule.OffsetConsecutive = new RowColumnIndex(0, 1);
            rule.Properties.Add(SimDataMappingComponentMappingProperties.Name);

            rule.Filter.Add(new SimDataMappingFilterComponent(SimDataMappingComponentFilterProperties.Name, new Regex("Child.*")));

            var state = new SimTraversalState();
            var data = new SimMappedData();
            rule.Execute(comp1, state, data);

            Assert.AreEqual(1, data.Data.Count);
            Assert.IsTrue(data.Data.TryGetValue("SheetA", out var sheetData));
            Assert.AreEqual(3, sheetData.Count);
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(3, 2), "ChildComponent1");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(3, 3), "ChildComponent2");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(3, 4), "ChildComponent3");
        }

        [TestMethod]
        public void SubtreeOnly()
        {
            this.LoadProject(mappingProject);

            var comp1 = projectData.Components.First(x => x.Name == "Component1");
            var compTaxEntry = projectData.Taxonomies.GetDefaultSlot(SimDefaultSlotKeys.Layer);

            SimDataMappingRuleComponent rule = new SimDataMappingRuleComponent("SheetA");
            rule.MaxMatches = int.MaxValue;
            rule.MaxDepth = int.MaxValue;
            rule.TraversalStrategy = SimDataMappingRuleTraversalStrategy.Subtree;
            rule.MappingDirection = SimDataMappingDirection.Horizontal;
            rule.OffsetParent = new RowColumnIndex(3, 2);
            rule.ReferencePointParent = SimDataMappingReferencePoint.TopRight;
            rule.OffsetConsecutive = new RowColumnIndex(0, 1);
            rule.Properties.Add(SimDataMappingComponentMappingProperties.Name);

            rule.Filter.Add(new SimDataMappingFilterComponent(SimDataMappingComponentFilterProperties.Slot,
                new SimTaxonomyEntryReference(compTaxEntry)));

            var state = new SimTraversalState();
            var data = new SimMappedData();
            rule.Execute(comp1, state, data);

            Assert.AreEqual(1, data.Data.Count);
            Assert.IsTrue(data.Data.TryGetValue("SheetA", out var sheetData));
            Assert.AreEqual(4, sheetData.Count);
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(3, 2), "ChildComponent1");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(3, 3), "ChildComponent2");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(3, 4), "ChildComponent3");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(3, 5), "ChildComponent4");
        }

        #endregion

        #region Properties from Geometry

        [TestMethod]
        public void MapVolumeName()
        {
            this.LoadProject(mappingProject);

            var comp1 = projectData.Components.First(x => x.Name == "Geometry")
                .Components.First(x => x.Component.Name == "Volume1").Component;

            SimDataMappingRuleComponent compRule = new SimDataMappingRuleComponent("SheetA");
            compRule.OffsetParent = new RowColumnIndex(0, 0);
            compRule.Properties.Add(SimDataMappingComponentMappingProperties.Name);

            SimDataMappingRuleVolume volumeRule = new SimDataMappingRuleVolume("SheetA");
            volumeRule.OffsetParent = new RowColumnIndex(0, 1);
            volumeRule.Properties.Add(SimDataMappingVolumeMappingProperties.Name);
            compRule.Rules.Add(volumeRule);

            SimDataMappingRuleComponent rule = new SimDataMappingRuleComponent("SheetA");
            rule.MaxMatches = int.MaxValue;
            rule.MaxDepth = int.MaxValue;
            rule.OffsetParent = new RowColumnIndex(0, 1);
            rule.OffsetConsecutive = new RowColumnIndex(1, 0);
            rule.Properties.Add(SimDataMappingComponentMappingProperties.Name);
            volumeRule.Rules.Add(rule);

            var state = new SimTraversalState();
            var data = new SimMappedData();
            compRule.Execute(comp1, state, data);

            Assert.AreEqual(1, data.Data.Count);
            Assert.IsTrue(data.Data.TryGetValue("SheetA", out var sheetData));

            Assert.AreEqual(3, sheetData.Count);

            AssertUtil.AssertContains(sheetData, new RowColumnIndex(0, 0), "Volume1");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(0, 1), "ShoeBox");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(0, 2), "Room1");

        }

        [TestMethod]
        public void MapFaceName()
        {
            this.LoadProject(mappingProject);

            var comp1 = projectData.Components.First(x => x.Name == "Geometry")
                .Components.First(x => x.Component.Name == "Face1").Component;

            SimDataMappingRuleComponent compRule = new SimDataMappingRuleComponent("SheetA");
            compRule.OffsetParent = new RowColumnIndex(0, 0);
            compRule.Properties.Add(SimDataMappingComponentMappingProperties.Name);

            SimDataMappingRuleFace faceRule = new SimDataMappingRuleFace("SheetA");
            faceRule.OffsetParent = new RowColumnIndex(0, 1);
            faceRule.Properties.Add(SimDataMappingFaceMappingProperties.Name);
            faceRule.Filter.Add(new SimDataMappingFilterFace(SimDataMappingFaceFilterProperties.Name, "Face 13"));
            compRule.Rules.Add(faceRule);

            SimDataMappingRuleComponent rule = new SimDataMappingRuleComponent("SheetA");
            rule.MaxMatches = int.MaxValue;
            rule.MaxDepth = int.MaxValue;
            rule.OffsetParent = new RowColumnIndex(0, 1);
            rule.OffsetConsecutive = new RowColumnIndex(1, 0);
            rule.Properties.Add(SimDataMappingComponentMappingProperties.Name);
            faceRule.Rules.Add(rule);

            var state = new SimTraversalState();
            var data = new SimMappedData();
            compRule.Execute(comp1, state, data);

            Assert.AreEqual(1, data.Data.Count);
            Assert.IsTrue(data.Data.TryGetValue("SheetA", out var sheetData));

            Assert.AreEqual(3, sheetData.Count);

            AssertUtil.AssertContains(sheetData, new RowColumnIndex(0, 0), "Face1");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(0, 1), "Face 13");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(0, 2), "ChildComponent1");

        }

        #endregion

        #region Properties from Instances

        [TestMethod]
        public void MapInstancedName()
        {
            //Must go over geometry, otherwise impossible to find a component which isn't already visited

            this.LoadProject(mappingProject);

            var comp1 = projectData.Components.First(x => x.Name == "Geometry")
                .Components.First(x => x.Component.Name == "Volume1").Component;

            SimDataMappingRuleComponent compRule = new SimDataMappingRuleComponent("SheetA");
            compRule.OffsetParent = new RowColumnIndex(0, 0);
            compRule.Properties.Add(SimDataMappingComponentMappingProperties.Name);

            SimDataMappingRuleVolume volumeRule = new SimDataMappingRuleVolume("SheetA");
            volumeRule.OffsetParent = new RowColumnIndex(0, 1);
            volumeRule.Properties.Add(SimDataMappingVolumeMappingProperties.Name);
            compRule.Rules.Add(volumeRule);

            SimDataMappingRuleInstance instanceRule = new SimDataMappingRuleInstance("SheetA");
            instanceRule.OffsetParent = new RowColumnIndex(0, 1);
            instanceRule.Properties.Add(SimDataMappingInstanceMappingProperties.Name);
            volumeRule.Rules.Add(instanceRule);

            SimDataMappingRuleComponent componentRule = new SimDataMappingRuleComponent("SheetA");
            componentRule.OffsetParent = new RowColumnIndex(0, 1);
            componentRule.Properties.Add(SimDataMappingComponentMappingProperties.Name);
            instanceRule.Rules.Add(componentRule);

            var state = new SimTraversalState();
            var data = new SimMappedData();
            compRule.Execute(comp1, state, data);

            Assert.AreEqual(1, data.Data.Count);
            Assert.IsTrue(data.Data.TryGetValue("SheetA", out var sheetData));

            Assert.AreEqual(4, sheetData.Count);

            AssertUtil.AssertContains(sheetData, new RowColumnIndex(0, 0), "Volume1");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(0, 1), "ShoeBox");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(0, 2), "Geometry Placement 0:56");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(0, 3), "Room1");
        }

        #endregion


        #region References

        [TestMethod]
        public void ReferencesOnly()
        {
            this.LoadProject(mappingProject);

            var comp1 = projectData.Components.First(x => x.Name == "Component1");
            var compTaxEntry = projectData.Taxonomies.GetDefaultSlot(SimDefaultSlotKeys.Layer);

            SimDataMappingRuleComponent rule = new SimDataMappingRuleComponent("SheetA");
            rule.MaxMatches = 4; //Has to be limited to prevent cycle
            rule.MaxDepth = int.MaxValue;
            rule.TraversalStrategy = SimDataMappingRuleTraversalStrategy.References;
            rule.MappingDirection = SimDataMappingDirection.Horizontal;
            rule.OffsetParent = new RowColumnIndex(3, 2);
            rule.ReferencePointParent = SimDataMappingReferencePoint.TopRight;
            rule.OffsetConsecutive = new RowColumnIndex(0, 1);
            rule.Properties.Add(SimDataMappingComponentMappingProperties.Name);

            rule.Filter.Add(new SimDataMappingFilterComponent(SimDataMappingComponentFilterProperties.Slot,
                new SimTaxonomyEntryReference(compTaxEntry)));

            var state = new SimTraversalState();
            var data = new SimMappedData();
            rule.Execute(comp1, state, data);

            Assert.AreEqual(1, data.Data.Count);
            Assert.IsTrue(data.Data.TryGetValue("SheetA", out var sheetData));
            Assert.AreEqual(1, sheetData.Count);
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(3, 2), "Target1");
        }

        [TestMethod]
        public void PreventCycle()
        {
            this.LoadProject(mappingProject);

            var comp1 = projectData.Components.First(x => x.Name == "Component1");
            var compTaxEntry = projectData.Taxonomies.GetDefaultSlot(SimDefaultSlotKeys.Layer);

            SimDataMappingRuleComponent rule = new SimDataMappingRuleComponent("SheetA");
            rule.MaxMatches = int.MaxValue; //Has to be limited to prevent cycle
            rule.MaxDepth = int.MaxValue;
            rule.TraversalStrategy = SimDataMappingRuleTraversalStrategy.References;
            rule.MappingDirection = SimDataMappingDirection.Horizontal;
            rule.OffsetParent = new RowColumnIndex(3, 2);
            rule.ReferencePointParent = SimDataMappingReferencePoint.TopRight;
            rule.ReferencePointConsecutive = SimDataMappingReferencePoint.TopRight;
            rule.OffsetConsecutive = new RowColumnIndex(0, 1);
            rule.Properties.Add(SimDataMappingComponentMappingProperties.Name);

            rule.Filter.Add(new SimDataMappingFilterComponent(SimDataMappingComponentFilterProperties.Slot,
                new SimTaxonomyEntryReference(compTaxEntry)));

            //Recursion 1
            SimDataMappingRuleComponent rule2 = new SimDataMappingRuleComponent("SheetA");
            rule2.MaxMatches = int.MaxValue;
            rule2.MaxDepth = int.MaxValue;
            rule2.TraversalStrategy = SimDataMappingRuleTraversalStrategy.References;
            rule2.MappingDirection = SimDataMappingDirection.Horizontal;
            rule2.OffsetParent = new RowColumnIndex(0, 1);
            rule2.ReferencePointParent = SimDataMappingReferencePoint.TopRight;
            rule2.ReferencePointConsecutive = SimDataMappingReferencePoint.TopRight;
            rule2.OffsetConsecutive = new RowColumnIndex(0, 1);
            rule2.Properties.Add(SimDataMappingComponentMappingProperties.Name);
            rule.Rules.Add(rule2);

            //Recursion 2
            SimDataMappingRuleComponent rule3 = new SimDataMappingRuleComponent("SheetA");
            rule3.MaxMatches = int.MaxValue;
            rule3.MaxDepth = int.MaxValue;
            rule3.TraversalStrategy = SimDataMappingRuleTraversalStrategy.References;
            rule3.MappingDirection = SimDataMappingDirection.Horizontal;
            rule3.OffsetParent = new RowColumnIndex(0, 1);
            rule3.ReferencePointParent = SimDataMappingReferencePoint.TopRight;
            rule3.ReferencePointConsecutive = SimDataMappingReferencePoint.TopRight;
            rule3.OffsetConsecutive = new RowColumnIndex(0, 1);
            rule3.Properties.Add(SimDataMappingComponentMappingProperties.Name);
            rule2.Rules.Add(rule3);

            var state = new SimTraversalState();
            var data = new SimMappedData();
            rule.Execute(comp1, state, data);

            Assert.AreEqual(1, data.Data.Count);
            Assert.IsTrue(data.Data.TryGetValue("SheetA", out var sheetData));
            Assert.AreEqual(4, sheetData.Count);
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(3, 2), "Target1");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(3, 3), "Target2");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(3, 4), "Target4");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(3, 5), "Target3");
        }

        #endregion

        #region Child Rules

        [TestMethod]
        public void ChildComponentRules()
        {
            this.LoadProject(mappingProject);

            var comp1 = projectData.Components.First(x => x.Name == "Component1");

            SimDataMappingRuleComponent rule = new SimDataMappingRuleComponent("SheetA");
            rule.MaxMatches = int.MaxValue;
            rule.MaxDepth = int.MaxValue;
            rule.MappingDirection = SimDataMappingDirection.Horizontal;
            rule.OffsetParent = new RowColumnIndex(3, 2);
            rule.ReferencePointParent = SimDataMappingReferencePoint.BottomLeft;
            rule.ReferencePointConsecutive = SimDataMappingReferencePoint.BottomLeft;
            rule.OffsetConsecutive = new RowColumnIndex(1, 0);
            rule.Properties.Add(SimDataMappingComponentMappingProperties.Name);
            rule.Properties.Add(SimDataMappingComponentMappingProperties.Slot);
            rule.Filter.Add(new SimDataMappingFilterComponent(SimDataMappingComponentFilterProperties.Name, new Regex("Child.*")));

            SimDataMappingRuleComponent childRule = new SimDataMappingRuleComponent("SheetA");
            childRule.MaxMatches = int.MaxValue;
            childRule.MaxDepth = int.MaxValue;
            childRule.MappingDirection = SimDataMappingDirection.Horizontal;
            childRule.OffsetParent = new RowColumnIndex(0, 1);
            childRule.ReferencePointParent = SimDataMappingReferencePoint.TopRight;
            childRule.ReferencePointConsecutive = SimDataMappingReferencePoint.TopRight;
            childRule.OffsetConsecutive = new RowColumnIndex(1, 1);
            childRule.Properties.Add(SimDataMappingComponentMappingProperties.Name);
            childRule.Properties.Add(SimDataMappingComponentMappingProperties.Slot);
            childRule.Filter.Add(new SimDataMappingFilterComponent(SimDataMappingComponentFilterProperties.Name, new Regex("A.*")));

            rule.Rules.Add(childRule);

            var state = new SimTraversalState();
            var data = new SimMappedData();
            rule.Execute(comp1, state, data);

            Assert.AreEqual(1, data.Data.Count);
            Assert.IsTrue(data.Data.TryGetValue("SheetA", out var sheetData));
            Assert.AreEqual(14, sheetData.Count);
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(3, 2), "ChildComponent1");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(3, 3), "layer_0");

            AssertUtil.AssertContains(sheetData, new RowColumnIndex(4, 2), "ChildComponent2");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(4, 3), "layer_1");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(4, 4), "A1");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(4, 5), "undefined_0");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(5, 6), "A2");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(5, 7), "undefined_1");

            AssertUtil.AssertContains(sheetData, new RowColumnIndex(6, 2), "ChildComponent3");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(6, 3), "layer_2");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(6, 4), "A3");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(6, 5), "undefined_0");

            AssertUtil.AssertContains(sheetData, new RowColumnIndex(7, 2), "ChildComponent4");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(7, 3), "layer_2");
        }

        [TestMethod]
        public void ChildRulesExcludeRoot()
        {
            this.LoadProject(mappingProject);

            var comp1 = projectData.Components.First(x => x.Name == "Component1");
            var compTaxEntry = projectData.Taxonomies.GetDefaultSlot(SimDefaultSlotKeys.Layer);

            SimDataMappingRuleComponent rule = new SimDataMappingRuleComponent("SheetA");
            rule.MaxMatches = int.MaxValue;
            rule.MaxDepth = int.MaxValue;
            rule.MappingDirection = SimDataMappingDirection.Horizontal;
            rule.OffsetParent = new RowColumnIndex(3, 2);
            rule.ReferencePointParent = SimDataMappingReferencePoint.TopRight;
            rule.ReferencePointConsecutive = SimDataMappingReferencePoint.TopRight;
            rule.OffsetConsecutive = new RowColumnIndex(0, 1);
            rule.Properties.Add(SimDataMappingComponentMappingProperties.Name);

            rule.Filter.Add(new SimDataMappingFilterComponent(SimDataMappingComponentFilterProperties.Name, new Regex("Child.*")));

            SimDataMappingRuleComponent rule2 = new SimDataMappingRuleComponent("SheetA");
            rule2.MaxMatches = 1;
            rule2.MaxDepth = int.MaxValue;
            rule2.MappingDirection = SimDataMappingDirection.Horizontal;
            rule2.OffsetParent = new RowColumnIndex(0, 1);
            rule2.ReferencePointParent = SimDataMappingReferencePoint.TopRight;
            rule2.ReferencePointConsecutive = SimDataMappingReferencePoint.TopRight;
            rule2.OffsetConsecutive = new RowColumnIndex(0, 1);
            rule2.Properties.Add(SimDataMappingComponentMappingProperties.Name);
            rule.Rules.Add(rule2);

            var state = new SimTraversalState();
            var data = new SimMappedData();
            rule.Execute(comp1, state, data);

            Assert.AreEqual(1, data.Data.Count);
            Assert.IsTrue(data.Data.TryGetValue("SheetA", out var sheetData));
            Assert.AreEqual(6, sheetData.Count);
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(3, 2), "ChildComponent1");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(3, 3), "ChildComponent2");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(3, 4), "A1");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(3, 5), "ChildComponent3");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(3, 6), "A3");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(3, 7), "ChildComponent4");
        }

        #endregion

        [TestMethod]
        public void CloneTest()
        {
            SimDataMappingRuleComponent componentRule = new SimDataMappingRuleComponent("Sheet A")
            {
                Name = "SomeName",
                MappingDirection = SimDataMappingDirection.Vertical,
                MaxDepth = 3,
                MaxMatches = 5,
                OffsetConsecutive = new RowColumnIndex(8, 7),
                OffsetParent = new RowColumnIndex(10, 9),
                ReferencePointParent = SimDataMappingReferencePoint.TopRight,
                TraversalStrategy = SimDataMappingRuleTraversalStrategy.References
            };

            componentRule.Filter.Add(new SimDataMappingFilterComponent(SimDataMappingComponentFilterProperties.Name, "asdf"));
            componentRule.Filter.Add(new SimDataMappingFilterComponent(SimDataMappingComponentFilterProperties.InstanceType, SimInstanceType.AttributesEdge));

            componentRule.Properties.Add(SimDataMappingComponentMappingProperties.Name);
            componentRule.Properties.Add(SimDataMappingComponentMappingProperties.Slot);

            SimDataMappingRuleComponent childRule1 = new SimDataMappingRuleComponent("Sheet B");
            SimDataMappingRuleVolume childRule2 = new SimDataMappingRuleVolume("Sheet B");
            SimDataMappingRuleParameter childRule3 = new SimDataMappingRuleParameter("Sheet B");
            SimDataMappingRuleFace childRule4 = new SimDataMappingRuleFace("Sheet B");
            SimDataMappingRuleComponent childRule5 = new SimDataMappingRuleComponent("Sheet B");

            componentRule.Rules.Add(childRule1);
            componentRule.Rules.Add(childRule2);
            componentRule.Rules.Add(childRule3);
            componentRule.Rules.Add(childRule4);
            componentRule.Rules.Add(childRule5);

            //Test
            var clonedRule = componentRule.Clone();

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
            Assert.AreEqual(SimDataMappingRuleTraversalStrategy.References, clonedRule.TraversalStrategy);

            Assert.IsTrue(clonedRule.Rules[0] is SimDataMappingRuleComponent);
            Assert.IsTrue(clonedRule.Rules[1] is SimDataMappingRuleVolume);
            Assert.IsTrue(clonedRule.Rules[2] is SimDataMappingRuleParameter);
            Assert.IsTrue(clonedRule.Rules[3] is SimDataMappingRuleFace);
            Assert.IsTrue(clonedRule.Rules[4] is SimDataMappingRuleComponent);

            Assert.AreEqual(2, clonedRule.Filter.Count);
            Assert.AreEqual(SimDataMappingComponentFilterProperties.Name, clonedRule.Filter[0].Property);
            Assert.AreEqual("asdf", clonedRule.Filter[0].Value);
            Assert.AreEqual(SimDataMappingComponentFilterProperties.InstanceType, clonedRule.Filter[1].Property);
            Assert.AreEqual(SimInstanceType.AttributesEdge, clonedRule.Filter[1].Value);

            Assert.AreEqual(2, clonedRule.Properties.Count);
            Assert.AreEqual(SimDataMappingComponentMappingProperties.Name, clonedRule.Properties[0]);
            Assert.AreEqual(SimDataMappingComponentMappingProperties.Slot, clonedRule.Properties[1]);
        }
    }
}
