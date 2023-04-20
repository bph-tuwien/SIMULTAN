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
        private static readonly FileInfo mappingProject = new FileInfo(@".\ExcelMappingTests.simultan");

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
            AssertUtil.AssertContains(sheetData, new IntIndex2D(0, 0), "Component1");
            AssertUtil.AssertContains(sheetData, new IntIndex2D(1, 0), 64);
            AssertUtil.AssertContains(sheetData, new IntIndex2D(2, 0), "Heating");
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
            AssertUtil.AssertContains(sheetData, new IntIndex2D(0, 0), "Component1");
            AssertUtil.AssertContains(sheetData, new IntIndex2D(0, 1), 64);
            AssertUtil.AssertContains(sheetData, new IntIndex2D(0, 2), "Heating");
        }

        [TestMethod]
        public void RootOffsetParent()
        {
            this.LoadProject(mappingProject);

            var comp1 = projectData.Components.First(x => x.Name == "Component1");

            SimDataMappingRuleComponent rule = new SimDataMappingRuleComponent("SheetA");
            rule.MappingDirection = SimDataMappingDirection.Horizontal;
            rule.OffsetParent = new IntIndex2D(2, 3);
            rule.Properties.Add(SimDataMappingComponentMappingProperties.Name);
            rule.Properties.Add(SimDataMappingComponentMappingProperties.Id);
            rule.Properties.Add(SimDataMappingComponentMappingProperties.Slot);

            var state = new SimTraversalState();
            var data = new SimMappedData();
            rule.Execute(comp1, state, data);

            Assert.AreEqual(1, data.Data.Count);
            Assert.IsTrue(data.Data.TryGetValue("SheetA", out var sheetData));

            Assert.AreEqual(3, sheetData.Count);
            AssertUtil.AssertContains(sheetData, new IntIndex2D(2, 3), "Component1");
            AssertUtil.AssertContains(sheetData, new IntIndex2D(3, 3), 64);
            AssertUtil.AssertContains(sheetData, new IntIndex2D(4, 3), "Heating");
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
            AssertUtil.AssertContains(sheetData, new IntIndex2D(0, 0), "Component1");
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
            AssertUtil.AssertContains(sheetData, new IntIndex2D(0, 0), "Component1");


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
            AssertUtil.AssertContains(sheetData, new IntIndex2D(0, 0), "Component1");
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
            rule.OffsetConsecutive = new IntIndex2D(0, 1);
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
            AssertUtil.AssertContains(sheetData, new IntIndex2D(0, 0), "ChildComponent1");
            AssertUtil.AssertContains(sheetData, new IntIndex2D(0, 1), "ChildComponent2");
            AssertUtil.AssertContains(sheetData, new IntIndex2D(0, 2), "ChildComponent3");
            AssertUtil.AssertContains(sheetData, new IntIndex2D(0, 3), "ChildComponent4");
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
            rule.OffsetConsecutive = new IntIndex2D(0, 1);
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
            AssertUtil.AssertContains(sheetData, new IntIndex2D(0, 0), "ChildComponent3");
            AssertUtil.AssertContains(sheetData, new IntIndex2D(0, 1), "ChildComponent4");
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
            rule.OffsetConsecutive = new IntIndex2D(0, 1);
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
            AssertUtil.AssertContains(sheetData, new IntIndex2D(0, 0), "ChildComponent1");
            AssertUtil.AssertContains(sheetData, new IntIndex2D(0, 1), "ChildComponent3");
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
            rule.OffsetConsecutive = new IntIndex2D(0, 1);
            rule.Properties.Add(SimDataMappingComponentMappingProperties.Name);
            rule.Filter.Add(new SimDataMappingFilterComponent(SimDataMappingComponentFilterProperties.InstanceIsRealized, true));

            var data = new SimMappedData();
            var state = new SimTraversalState();
            rule.Execute(comp1, state, data);

            Assert.AreEqual(1, data.Data.Count);
            Assert.IsTrue(data.Data.TryGetValue("SheetA", out var sheetData));
            Assert.AreEqual(2, sheetData.Count);
            AssertUtil.AssertContains(sheetData, new IntIndex2D(0, 0), "ChildComponent1");
            AssertUtil.AssertContains(sheetData, new IntIndex2D(0, 1), "ChildComponent2");
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
            rule.OffsetConsecutive = new IntIndex2D(0, 1);
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
            AssertUtil.AssertContains(sheetData, new IntIndex2D(0, 0), "ChildComponent1");
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
            rule.OffsetParent = new IntIndex2D(2, 3);
            rule.ReferencePoint = SimDataMappingReferencePoint.TopRight;
            rule.OffsetConsecutive = new IntIndex2D(1, 0);
            rule.Properties.Add(SimDataMappingComponentMappingProperties.Name);

            rule.Filter.Add(new SimDataMappingFilterComponent(SimDataMappingComponentFilterProperties.Name, new Regex("Child.*")));

            var state = new SimTraversalState();
            var data = new SimMappedData();
            rule.Execute(comp1, state, data);

            Assert.AreEqual(1, data.Data.Count);
            Assert.IsTrue(data.Data.TryGetValue("SheetA", out var sheetData));
            Assert.AreEqual(4, sheetData.Count);
            AssertUtil.AssertContains(sheetData, new IntIndex2D(2, 3), "ChildComponent1");
            AssertUtil.AssertContains(sheetData, new IntIndex2D(3, 3), "ChildComponent2");
            AssertUtil.AssertContains(sheetData, new IntIndex2D(4, 3), "ChildComponent3");
            AssertUtil.AssertContains(sheetData, new IntIndex2D(5, 3), "ChildComponent4");
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
            rule.OffsetParent = new IntIndex2D(2, 3);
            rule.OffsetConsecutive = new IntIndex2D(0, 1);
            rule.Properties.Add(SimDataMappingComponentMappingProperties.Name);
            rule.Properties.Add(SimDataMappingComponentMappingProperties.Id);

            rule.Filter.Add(new SimDataMappingFilterComponent(SimDataMappingComponentFilterProperties.Name, new Regex("Child.*")));

            var state = new SimTraversalState();
            var data = new SimMappedData();
            rule.Execute(comp1, state, data);

            Assert.AreEqual(3, state.Range.RowStart);
            Assert.AreEqual(2, state.Range.ColumnStart);
            Assert.AreEqual(4, state.Range.RowCount);
            Assert.AreEqual(2, state.Range.ColumnCount);
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
            rule.OffsetParent = new IntIndex2D(2, 3);
            rule.ReferencePoint = SimDataMappingReferencePoint.TopRight;
            rule.OffsetConsecutive = new IntIndex2D(1, 0);
            rule.Properties.Add(SimDataMappingComponentMappingProperties.Name);

            rule.Filter.Add(new SimDataMappingFilterComponent(SimDataMappingComponentFilterProperties.Name, new Regex("Child.*")));

            var state = new SimTraversalState();
            var data = new SimMappedData();
            rule.Execute(comp1, state, data);

            Assert.AreEqual(1, data.Data.Count);
            Assert.IsTrue(data.Data.TryGetValue("SheetA", out var sheetData));
            Assert.AreEqual(2, sheetData.Count);
            AssertUtil.AssertContains(sheetData, new IntIndex2D(2, 3), "ChildComponent1");
            AssertUtil.AssertContains(sheetData, new IntIndex2D(3, 3), "ChildComponent2");
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
            rule.OffsetParent = new IntIndex2D(2, 3);
            rule.ReferencePoint = SimDataMappingReferencePoint.TopRight;
            rule.OffsetConsecutive = new IntIndex2D(1, 0);
            rule.Properties.Add(SimDataMappingComponentMappingProperties.Name);

            rule.Filter.Add(new SimDataMappingFilterComponent(SimDataMappingComponentFilterProperties.Name, new Regex("Child.*")));

            var state = new SimTraversalState();
            var data = new SimMappedData();
            rule.Execute(comp1, state, data);

            Assert.AreEqual(1, data.Data.Count);
            Assert.IsTrue(data.Data.TryGetValue("SheetA", out var sheetData));
            Assert.AreEqual(3, sheetData.Count);
            AssertUtil.AssertContains(sheetData, new IntIndex2D(2, 3), "ChildComponent1");
            AssertUtil.AssertContains(sheetData, new IntIndex2D(3, 3), "ChildComponent2");
            AssertUtil.AssertContains(sheetData, new IntIndex2D(4, 3), "ChildComponent3");
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
            rule.OffsetParent = new IntIndex2D(2, 3);
            rule.ReferencePoint = SimDataMappingReferencePoint.TopRight;
            rule.OffsetConsecutive = new IntIndex2D(1, 0);
            rule.Properties.Add(SimDataMappingComponentMappingProperties.Name);

            rule.Filter.Add(new SimDataMappingFilterComponent(SimDataMappingComponentFilterProperties.Slot,
                new SimTaxonomyEntryReference(compTaxEntry)));

            var state = new SimTraversalState();
            var data = new SimMappedData();
            rule.Execute(comp1, state, data);

            Assert.AreEqual(1, data.Data.Count);
            Assert.IsTrue(data.Data.TryGetValue("SheetA", out var sheetData));
            Assert.AreEqual(4, sheetData.Count);
            AssertUtil.AssertContains(sheetData, new IntIndex2D(2, 3), "ChildComponent1");
            AssertUtil.AssertContains(sheetData, new IntIndex2D(3, 3), "ChildComponent2");
            AssertUtil.AssertContains(sheetData, new IntIndex2D(4, 3), "ChildComponent3");
            AssertUtil.AssertContains(sheetData, new IntIndex2D(5, 3), "ChildComponent4");
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
            compRule.OffsetParent = new IntIndex2D(0, 0);
            compRule.Properties.Add(SimDataMappingComponentMappingProperties.Name);

            SimDataMappingRuleVolume volumeRule = new SimDataMappingRuleVolume("SheetA");
            volumeRule.OffsetParent = new IntIndex2D(1, 0);
            volumeRule.Properties.Add(SimDataMappingVolumeMappingProperties.Name);
            compRule.Rules.Add(volumeRule);

            SimDataMappingRuleComponent rule = new SimDataMappingRuleComponent("SheetA");
            rule.MaxMatches = int.MaxValue;
            rule.MaxDepth = int.MaxValue;
            rule.OffsetParent = new IntIndex2D(1, 0);
            rule.OffsetConsecutive = new IntIndex2D(0, 1);
            rule.Properties.Add(SimDataMappingComponentMappingProperties.Name);
            volumeRule.Rules.Add(rule);

            var state = new SimTraversalState();
            var data = new SimMappedData();
            compRule.Execute(comp1, state, data);

            Assert.AreEqual(1, data.Data.Count);
            Assert.IsTrue(data.Data.TryGetValue("SheetA", out var sheetData));

            Assert.AreEqual(3, sheetData.Count);

            AssertUtil.AssertContains(sheetData, new IntIndex2D(0, 0), "Volume1");
            AssertUtil.AssertContains(sheetData, new IntIndex2D(1, 0), "ShoeBox");
            AssertUtil.AssertContains(sheetData, new IntIndex2D(2, 0), "Room1");

        }

        [TestMethod]
        public void MapFaceName()
        {
            this.LoadProject(mappingProject);

            var comp1 = projectData.Components.First(x => x.Name == "Geometry")
                .Components.First(x => x.Component.Name == "Face1").Component;

            SimDataMappingRuleComponent compRule = new SimDataMappingRuleComponent("SheetA");
            compRule.OffsetParent = new IntIndex2D(0, 0);
            compRule.Properties.Add(SimDataMappingComponentMappingProperties.Name);

            SimDataMappingRuleFace faceRule = new SimDataMappingRuleFace("SheetA");
            faceRule.OffsetParent = new IntIndex2D(1, 0);
            faceRule.Properties.Add(SimDataMappingFaceMappingProperties.Name);
            faceRule.Filter.Add(new SimDataMappingFilterFace(SimDataMappingFaceFilterProperties.Name, "Face 13"));
            compRule.Rules.Add(faceRule);

            SimDataMappingRuleComponent rule = new SimDataMappingRuleComponent("SheetA");
            rule.MaxMatches = int.MaxValue;
            rule.MaxDepth = int.MaxValue;
            rule.OffsetParent = new IntIndex2D(1, 0);
            rule.OffsetConsecutive = new IntIndex2D(0, 1);
            rule.Properties.Add(SimDataMappingComponentMappingProperties.Name);
            faceRule.Rules.Add(rule);

            var state = new SimTraversalState();
            var data = new SimMappedData();
            compRule.Execute(comp1, state, data);

            Assert.AreEqual(1, data.Data.Count);
            Assert.IsTrue(data.Data.TryGetValue("SheetA", out var sheetData));

            Assert.AreEqual(3, sheetData.Count);

            AssertUtil.AssertContains(sheetData, new IntIndex2D(0, 0), "Face1");
            AssertUtil.AssertContains(sheetData, new IntIndex2D(1, 0), "Face 13");
            AssertUtil.AssertContains(sheetData, new IntIndex2D(2, 0), "ChildComponent1");

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
            compRule.OffsetParent = new IntIndex2D(0, 0);
            compRule.Properties.Add(SimDataMappingComponentMappingProperties.Name);

            SimDataMappingRuleVolume volumeRule = new SimDataMappingRuleVolume("SheetA");
            volumeRule.OffsetParent = new IntIndex2D(1, 0);
            volumeRule.Properties.Add(SimDataMappingVolumeMappingProperties.Name);
            compRule.Rules.Add(volumeRule);

            SimDataMappingRuleInstance instanceRule = new SimDataMappingRuleInstance("SheetA");
            instanceRule.OffsetParent = new IntIndex2D(1, 0);
            instanceRule.Properties.Add(SimDataMappingInstanceMappingProperties.Name);
            volumeRule.Rules.Add(instanceRule);

            SimDataMappingRuleComponent componentRule = new SimDataMappingRuleComponent("SheetA");
            componentRule.OffsetParent = new IntIndex2D(1, 0);
            componentRule.Properties.Add(SimDataMappingComponentMappingProperties.Name);
            instanceRule.Rules.Add(componentRule);

            var state = new SimTraversalState();
            var data = new SimMappedData();
            compRule.Execute(comp1, state, data);

            Assert.AreEqual(1, data.Data.Count);
            Assert.IsTrue(data.Data.TryGetValue("SheetA", out var sheetData));

            Assert.AreEqual(4, sheetData.Count);

            AssertUtil.AssertContains(sheetData, new IntIndex2D(0, 0), "Volume1");
            AssertUtil.AssertContains(sheetData, new IntIndex2D(1, 0), "ShoeBox");
            AssertUtil.AssertContains(sheetData, new IntIndex2D(2, 0), "Geometry Placement 0:56");
            AssertUtil.AssertContains(sheetData, new IntIndex2D(3, 0), "Room1");
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
            rule.OffsetParent = new IntIndex2D(2, 3);
            rule.ReferencePoint = SimDataMappingReferencePoint.TopRight;
            rule.OffsetConsecutive = new IntIndex2D(1, 0);
            rule.Properties.Add(SimDataMappingComponentMappingProperties.Name);

            rule.Filter.Add(new SimDataMappingFilterComponent(SimDataMappingComponentFilterProperties.Slot,
                new SimTaxonomyEntryReference(compTaxEntry)));

            var state = new SimTraversalState();
            var data = new SimMappedData();
            rule.Execute(comp1, state, data);

            Assert.AreEqual(1, data.Data.Count);
            Assert.IsTrue(data.Data.TryGetValue("SheetA", out var sheetData));
            Assert.AreEqual(1, sheetData.Count);
            AssertUtil.AssertContains(sheetData, new IntIndex2D(2, 3), "Target1");
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
            rule.OffsetParent = new IntIndex2D(2, 3);
            rule.ReferencePoint = SimDataMappingReferencePoint.TopRight;
            rule.OffsetConsecutive = new IntIndex2D(1, 0);
            rule.Properties.Add(SimDataMappingComponentMappingProperties.Name);

            rule.Filter.Add(new SimDataMappingFilterComponent(SimDataMappingComponentFilterProperties.Slot,
                new SimTaxonomyEntryReference(compTaxEntry)));

            //Recursion 1
            SimDataMappingRuleComponent rule2 = new SimDataMappingRuleComponent("SheetA");
            rule2.MaxMatches = int.MaxValue;
            rule2.MaxDepth = int.MaxValue;
            rule2.TraversalStrategy = SimDataMappingRuleTraversalStrategy.References;
            rule2.MappingDirection = SimDataMappingDirection.Horizontal;
            rule2.OffsetParent = new IntIndex2D(1, 0);
            rule2.ReferencePoint = SimDataMappingReferencePoint.TopRight;
            rule2.OffsetConsecutive = new IntIndex2D(1, 0);
            rule2.Properties.Add(SimDataMappingComponentMappingProperties.Name);
            rule.Rules.Add(rule2);

            //Recursion 2
            SimDataMappingRuleComponent rule3 = new SimDataMappingRuleComponent("SheetA");
            rule3.MaxMatches = int.MaxValue;
            rule3.MaxDepth = int.MaxValue;
            rule3.TraversalStrategy = SimDataMappingRuleTraversalStrategy.References;
            rule3.MappingDirection = SimDataMappingDirection.Horizontal;
            rule3.OffsetParent = new IntIndex2D(1, 0);
            rule3.ReferencePoint = SimDataMappingReferencePoint.TopRight;
            rule3.OffsetConsecutive = new IntIndex2D(1, 0);
            rule3.Properties.Add(SimDataMappingComponentMappingProperties.Name);
            rule2.Rules.Add(rule3);

            var state = new SimTraversalState();
            var data = new SimMappedData();
            rule.Execute(comp1, state, data);

            Assert.AreEqual(1, data.Data.Count);
            Assert.IsTrue(data.Data.TryGetValue("SheetA", out var sheetData));
            Assert.AreEqual(4, sheetData.Count);
            AssertUtil.AssertContains(sheetData, new IntIndex2D(2, 3), "Target1");
            AssertUtil.AssertContains(sheetData, new IntIndex2D(3, 3), "Target2");
            AssertUtil.AssertContains(sheetData, new IntIndex2D(4, 3), "Target4");
            AssertUtil.AssertContains(sheetData, new IntIndex2D(5, 3), "Target3");
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
            rule.OffsetParent = new IntIndex2D(2, 3);
            rule.ReferencePoint = SimDataMappingReferencePoint.BottomLeft;
            rule.OffsetConsecutive = new IntIndex2D(0, 1);
            rule.Properties.Add(SimDataMappingComponentMappingProperties.Name);
            rule.Properties.Add(SimDataMappingComponentMappingProperties.Slot);
            rule.Filter.Add(new SimDataMappingFilterComponent(SimDataMappingComponentFilterProperties.Name, new Regex("Child.*")));

            SimDataMappingRuleComponent childRule = new SimDataMappingRuleComponent("SheetA");
            childRule.MaxMatches = int.MaxValue;
            childRule.MaxDepth = int.MaxValue;
            childRule.MappingDirection = SimDataMappingDirection.Horizontal;
            childRule.OffsetParent = new IntIndex2D(1, 0);
            childRule.ReferencePoint = SimDataMappingReferencePoint.TopRight;
            childRule.OffsetConsecutive = new IntIndex2D(1, 1);
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
            AssertUtil.AssertContains(sheetData, new IntIndex2D(2, 3), "ChildComponent1");
            AssertUtil.AssertContains(sheetData, new IntIndex2D(3, 3), "Layer_0");

            AssertUtil.AssertContains(sheetData, new IntIndex2D(2, 4), "ChildComponent2");
            AssertUtil.AssertContains(sheetData, new IntIndex2D(3, 4), "Layer_1");
            AssertUtil.AssertContains(sheetData, new IntIndex2D(4, 4), "A1");
            AssertUtil.AssertContains(sheetData, new IntIndex2D(5, 4), "Undefined Slot_0");
            AssertUtil.AssertContains(sheetData, new IntIndex2D(6, 5), "A2");
            AssertUtil.AssertContains(sheetData, new IntIndex2D(7, 5), "Undefined Slot_1");

            AssertUtil.AssertContains(sheetData, new IntIndex2D(2, 6), "ChildComponent3");
            AssertUtil.AssertContains(sheetData, new IntIndex2D(3, 6), "Layer_2");
            AssertUtil.AssertContains(sheetData, new IntIndex2D(4, 6), "A3");
            AssertUtil.AssertContains(sheetData, new IntIndex2D(5, 6), "Undefined Slot_0");

            AssertUtil.AssertContains(sheetData, new IntIndex2D(2, 7), "ChildComponent4");
            AssertUtil.AssertContains(sheetData, new IntIndex2D(3, 7), "Layer_2");
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
            rule.OffsetParent = new IntIndex2D(2, 3);
            rule.ReferencePoint = SimDataMappingReferencePoint.TopRight;
            rule.OffsetConsecutive = new IntIndex2D(1, 0);
            rule.Properties.Add(SimDataMappingComponentMappingProperties.Name);

            rule.Filter.Add(new SimDataMappingFilterComponent(SimDataMappingComponentFilterProperties.Name, new Regex("Child.*")));

            SimDataMappingRuleComponent rule2 = new SimDataMappingRuleComponent("SheetA");
            rule2.MaxMatches = 1;
            rule2.MaxDepth = int.MaxValue;
            rule2.MappingDirection = SimDataMappingDirection.Horizontal;
            rule2.OffsetParent = new IntIndex2D(1, 0);
            rule2.ReferencePoint = SimDataMappingReferencePoint.TopRight;
            rule2.OffsetConsecutive = new IntIndex2D(1, 0);
            rule2.Properties.Add(SimDataMappingComponentMappingProperties.Name);
            rule.Rules.Add(rule2);

            var state = new SimTraversalState();
            var data = new SimMappedData();
            rule.Execute(comp1, state, data);

            Assert.AreEqual(1, data.Data.Count);
            Assert.IsTrue(data.Data.TryGetValue("SheetA", out var sheetData));
            Assert.AreEqual(6, sheetData.Count);
            AssertUtil.AssertContains(sheetData, new IntIndex2D(2, 3), "ChildComponent1");
            AssertUtil.AssertContains(sheetData, new IntIndex2D(3, 3), "ChildComponent2");
            AssertUtil.AssertContains(sheetData, new IntIndex2D(4, 3), "A1");
            AssertUtil.AssertContains(sheetData, new IntIndex2D(5, 3), "ChildComponent3");
            AssertUtil.AssertContains(sheetData, new IntIndex2D(6, 3), "A3");
            AssertUtil.AssertContains(sheetData, new IntIndex2D(7, 3), "ChildComponent4");
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
                OffsetConsecutive = new IntIndex2D(7, 8),
                OffsetParent = new IntIndex2D(9, 10),
                ReferencePoint = SimDataMappingReferencePoint.TopRight,
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
            Assert.AreEqual(7, clonedRule.OffsetConsecutive.X);
            Assert.AreEqual(8, clonedRule.OffsetConsecutive.Y);
            Assert.AreEqual(9, clonedRule.OffsetParent.X);
            Assert.AreEqual(10, clonedRule.OffsetParent.Y);
            Assert.AreEqual(SimDataMappingReferencePoint.TopRight, clonedRule.ReferencePoint);
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
