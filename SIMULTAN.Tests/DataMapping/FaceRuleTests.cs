using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data.Components;
using SIMULTAN.DataMapping;
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
    public class FaceRuleTests : BaseProjectTest
    {
        private static readonly FileInfo mappingProject = new FileInfo(@"./ExcelMappingTests.simultan");

        #region Properties

        [TestMethod]
        public void MapName()
        {
            this.LoadProject(mappingProject);

            var comp1 = projectData.Components.First(x => x.Name == "Geometry");

            SimDataMappingRuleComponent componentRule = new SimDataMappingRuleComponent("SheetA");
            componentRule.MappingDirection = SimDataMappingDirection.Horizontal;
            componentRule.MaxMatches = int.MaxValue;
            componentRule.MaxDepth = int.MaxValue;
            componentRule.OffsetConsecutive = new RowColumnIndex(1, 0);
            componentRule.Properties.Add(SimDataMappingComponentMappingProperties.Name);
            componentRule.Filter.Add(new SimDataMappingFilterComponent(SimDataMappingComponentFilterProperties.InstanceType, SimInstanceType.AttributesFace));

            SimDataMappingRuleFace faceRule = new SimDataMappingRuleFace("SheetA");
            faceRule.MappingDirection = SimDataMappingDirection.Horizontal;
            faceRule.MaxMatches = int.MaxValue;
            faceRule.MaxDepth = int.MaxValue;
            faceRule.OffsetParent = new RowColumnIndex(0, 2);
            faceRule.OffsetConsecutive = new RowColumnIndex(1, 0);
            faceRule.Properties.Add(SimDataMappingFaceMappingProperties.Name);
            componentRule.Rules.Add(faceRule);

            var state = new SimTraversalState();
            var data = new SimMappedData();
            componentRule.Execute(comp1, state, data);

            Assert.AreEqual(1, data.Data.Count);
            Assert.IsTrue(data.Data.TryGetValue("SheetA", out var sheetData));

            Assert.AreEqual(9, sheetData.Count);
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(0, 0), "Face1");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(0, 2), "Face 13");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(1, 2), "Face 13");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(2, 2), "Face 32");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(3, 2), "Face 27");

            AssertUtil.AssertContains(sheetData, new RowColumnIndex(4, 0), "Face2");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(4, 2), "Face 33");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(5, 2), "Face 37");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(6, 2), "Face 76");
        }

        [TestMethod]
        public void MapId()
        {
            this.LoadProject(mappingProject);

            var comp1 = projectData.Components.First(x => x.Name == "Geometry");

            SimDataMappingRuleComponent componentRule = new SimDataMappingRuleComponent("SheetA");
            componentRule.MappingDirection = SimDataMappingDirection.Horizontal;
            componentRule.MaxMatches = int.MaxValue;
            componentRule.MaxDepth = int.MaxValue;
            componentRule.OffsetConsecutive = new RowColumnIndex(1, 0);
            componentRule.Properties.Add(SimDataMappingComponentMappingProperties.Name);
            componentRule.Filter.Add(new SimDataMappingFilterComponent(SimDataMappingComponentFilterProperties.InstanceType, SimInstanceType.AttributesFace));

            SimDataMappingRuleFace faceRule = new SimDataMappingRuleFace("SheetA");
            faceRule.MappingDirection = SimDataMappingDirection.Horizontal;
            faceRule.MaxMatches = int.MaxValue;
            faceRule.MaxDepth = int.MaxValue;
            faceRule.OffsetParent = new RowColumnIndex(0, 2);
            faceRule.OffsetConsecutive = new RowColumnIndex(1, 0);
            faceRule.Properties.Add(SimDataMappingFaceMappingProperties.Id);
            componentRule.Rules.Add(faceRule);

            var state = new SimTraversalState();
            var data = new SimMappedData();
            componentRule.Execute(comp1, state, data);

            Assert.AreEqual(1, data.Data.Count);
            Assert.IsTrue(data.Data.TryGetValue("SheetA", out var sheetData));

            Assert.AreEqual(9, sheetData.Count);
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(0, 0), "Face1");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(0, 2), 13);
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(1, 2), 13);
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(2, 2), 32);
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(3, 2), 27);

            AssertUtil.AssertContains(sheetData, new RowColumnIndex(4, 0), "Face2");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(4, 2), 33);
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(5, 2), 37);
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(6, 2), 76);
        }

        [TestMethod]
        public void MapArea()
        {
            this.LoadProject(mappingProject);

            var comp1 = projectData.Components.First(x => x.Name == "Geometry");

            SimDataMappingRuleComponent componentRule = new SimDataMappingRuleComponent("SheetA");
            componentRule.MappingDirection = SimDataMappingDirection.Horizontal;
            componentRule.MaxMatches = int.MaxValue;
            componentRule.MaxDepth = int.MaxValue;
            componentRule.OffsetConsecutive = new RowColumnIndex(1, 0);
            componentRule.Properties.Add(SimDataMappingComponentMappingProperties.Name);
            componentRule.Filter.Add(new SimDataMappingFilterComponent(SimDataMappingComponentFilterProperties.InstanceType, SimInstanceType.AttributesFace));

            SimDataMappingRuleFace faceRule = new SimDataMappingRuleFace("SheetA");
            faceRule.MappingDirection = SimDataMappingDirection.Horizontal;
            faceRule.MaxMatches = int.MaxValue;
            faceRule.MaxDepth = int.MaxValue;
            faceRule.OffsetParent = new RowColumnIndex(0, 2);
            faceRule.OffsetConsecutive = new RowColumnIndex(1, 0);
            faceRule.Properties.Add(SimDataMappingFaceMappingProperties.Area);
            componentRule.Rules.Add(faceRule);

            var state = new SimTraversalState();
            var data = new SimMappedData();
            componentRule.Execute(comp1, state, data);

            Assert.AreEqual(1, data.Data.Count);
            Assert.IsTrue(data.Data.TryGetValue("SheetA", out var sheetData));

            Assert.AreEqual(9, sheetData.Count);
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(0, 0), "Face1");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(0, 2), 100.0);
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(1, 2), 50.0);
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(2, 2), 25.0);
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(3, 2), 50.0);

            AssertUtil.AssertContains(sheetData, new RowColumnIndex(4, 0), "Face2");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(4, 2), 100.0);
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(5, 2), 50.0);
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(6, 2), 25.0);
        }

        [TestMethod]
        public void MapOrientation()
        {
            this.LoadProject(mappingProject);

            var comp1 = projectData.Components.First(x => x.Name == "Geometry");

            SimDataMappingRuleComponent componentRule = new SimDataMappingRuleComponent("SheetA");
            componentRule.MappingDirection = SimDataMappingDirection.Horizontal;
            componentRule.MaxMatches = int.MaxValue;
            componentRule.MaxDepth = int.MaxValue;
            componentRule.OffsetConsecutive = new RowColumnIndex(1, 0);
            componentRule.Properties.Add(SimDataMappingComponentMappingProperties.Name);
            componentRule.Filter.Add(new SimDataMappingFilterComponent(SimDataMappingComponentFilterProperties.InstanceType, SimInstanceType.AttributesFace));

            SimDataMappingRuleFace faceRule = new SimDataMappingRuleFace("SheetA");
            faceRule.MappingDirection = SimDataMappingDirection.Horizontal;
            faceRule.MaxMatches = int.MaxValue;
            faceRule.MaxDepth = int.MaxValue;
            faceRule.OffsetParent = new RowColumnIndex(0, 2);
            faceRule.OffsetConsecutive = new RowColumnIndex(1, 0);
            faceRule.Properties.Add(SimDataMappingFaceMappingProperties.Orientation);
            componentRule.Rules.Add(faceRule);

            var state = new SimTraversalState();
            var data = new SimMappedData();
            componentRule.Execute(comp1, state, data);

            Assert.AreEqual(1, data.Data.Count);
            Assert.IsTrue(data.Data.TryGetValue("SheetA", out var sheetData));

            Assert.AreEqual(9, sheetData.Count);
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(0, 0), "Face1");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(0, 2), 0.0);
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(1, 2), 180.0);
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(2, 2), 90.0);
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(3, 2), 0.0);

            AssertUtil.AssertContains(sheetData, new RowColumnIndex(4, 0), "Face2");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(4, 2), 180.0);
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(5, 2), 0.0);
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(6, 2), 0.0);
        }

        [TestMethod]
        public void MapIncline()
        {
            this.LoadProject(mappingProject);

            var comp1 = projectData.Components.First(x => x.Name == "Geometry");

            SimDataMappingRuleComponent componentRule = new SimDataMappingRuleComponent("SheetA");
            componentRule.MappingDirection = SimDataMappingDirection.Horizontal;
            componentRule.MaxMatches = int.MaxValue;
            componentRule.MaxDepth = int.MaxValue;
            componentRule.OffsetConsecutive = new RowColumnIndex(1, 0);
            componentRule.Properties.Add(SimDataMappingComponentMappingProperties.Name);
            componentRule.Filter.Add(new SimDataMappingFilterComponent(SimDataMappingComponentFilterProperties.InstanceType, SimInstanceType.AttributesFace));

            SimDataMappingRuleFace faceRule = new SimDataMappingRuleFace("SheetA");
            faceRule.MappingDirection = SimDataMappingDirection.Horizontal;
            faceRule.MaxMatches = int.MaxValue;
            faceRule.MaxDepth = int.MaxValue;
            faceRule.OffsetParent = new RowColumnIndex(0, 2);
            faceRule.OffsetConsecutive = new RowColumnIndex(1, 0);
            faceRule.Properties.Add(SimDataMappingFaceMappingProperties.Incline);
            componentRule.Rules.Add(faceRule);

            var state = new SimTraversalState();
            var data = new SimMappedData();
            componentRule.Execute(comp1, state, data);

            Assert.AreEqual(1, data.Data.Count);
            Assert.IsTrue(data.Data.TryGetValue("SheetA", out var sheetData));

            Assert.AreEqual(9, sheetData.Count);
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(0, 0), "Face1");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(0, 2), 0.0);
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(1, 2), 0.0);
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(2, 2), 0.0);
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(3, 2), -90.0);

            AssertUtil.AssertContains(sheetData, new RowColumnIndex(4, 0), "Face2");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(4, 2), 0.0);
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(5, 2), 90.0);
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(6, 2), 90.0);
        }

        #endregion

        #region Properties in Volumes

        [TestMethod]
        public void MapVolumeName()
        {
            this.LoadProject(mappingProject);

            var comp1 = projectData.Components.First(x => x.Name == "Geometry");

            SimDataMappingRuleComponent componentRule = new SimDataMappingRuleComponent("SheetA");
            componentRule.MappingDirection = SimDataMappingDirection.Horizontal;
            componentRule.MaxMatches = int.MaxValue;
            componentRule.MaxDepth = int.MaxValue;
            componentRule.OffsetConsecutive = new RowColumnIndex(1, 0);
            componentRule.Properties.Add(SimDataMappingComponentMappingProperties.Name);
            componentRule.Filter.Add(new SimDataMappingFilterComponent(SimDataMappingComponentFilterProperties.InstanceType, SimInstanceType.Entity3D));

            SimDataMappingRuleVolume volumeRule = new SimDataMappingRuleVolume("SheetA");
            volumeRule.OffsetParent = new RowColumnIndex(0, 1);
            volumeRule.OffsetConsecutive = new RowColumnIndex(1, 0);
            volumeRule.Filter.Add(new SimDataMappingFilterVolume(SimDataMappingVolumeFilterProperties.Name, "ShoeBox"));
            volumeRule.Properties.Add(SimDataMappingVolumeMappingProperties.Name);
            componentRule.Rules.Add(volumeRule);

            SimDataMappingRuleFace faceRule = new SimDataMappingRuleFace("SheetA");
            faceRule.MappingDirection = SimDataMappingDirection.Horizontal;
            faceRule.MaxMatches = int.MaxValue;
            faceRule.MaxDepth = int.MaxValue;
            faceRule.OffsetParent = new RowColumnIndex(0, 2);
            faceRule.OffsetConsecutive = new RowColumnIndex(1, 0);
            faceRule.Properties.Add(SimDataMappingFaceMappingProperties.Name);
            volumeRule.Rules.Add(faceRule);

            var state = new SimTraversalState();
            var data = new SimMappedData();
            componentRule.Execute(comp1, state, data);

            Assert.AreEqual(1, data.Data.Count);
            Assert.IsTrue(data.Data.TryGetValue("SheetA", out var sheetData));

            Assert.AreEqual(11, sheetData.Count);
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(0, 0), "Volume1");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(0, 1), "ShoeBox");

            AssertUtil.AssertContains(sheetData, new RowColumnIndex(0, 3), "Face 33");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(1, 3), "Face 40");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(2, 3), "Face 45");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(3, 3), "Face 50");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(4, 3), "Face 53");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(5, 3), "Face 55");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(6, 3), "Face 94");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(7, 3), "Face 104");

            AssertUtil.AssertContains(sheetData, new RowColumnIndex(8, 0), "Volume2");
        }

        [TestMethod]
        public void MapVolumeNameHoleGeometry()
        {
            this.LoadProject(mappingProject);

            var comp1 = projectData.Components.First(x => x.Name == "Hole Geometry");

            SimDataMappingRuleComponent componentRule = new SimDataMappingRuleComponent("SheetA");
            componentRule.MappingDirection = SimDataMappingDirection.Horizontal;
            componentRule.MaxMatches = int.MaxValue;
            componentRule.MaxDepth = int.MaxValue;
            componentRule.OffsetConsecutive = new RowColumnIndex(1, 0);
            componentRule.Properties.Add(SimDataMappingComponentMappingProperties.Name);
            componentRule.Filter.Add(new SimDataMappingFilterComponent(SimDataMappingComponentFilterProperties.InstanceType, SimInstanceType.Entity3D));

            SimDataMappingRuleVolume volumeRule = new SimDataMappingRuleVolume("SheetA");
            volumeRule.OffsetParent = new RowColumnIndex(0, 1);
            volumeRule.OffsetConsecutive = new RowColumnIndex(1, 0);
            volumeRule.Filter.Add(new SimDataMappingFilterVolume(SimDataMappingVolumeFilterProperties.Name, "HoleVolume"));
            volumeRule.Properties.Add(SimDataMappingVolumeMappingProperties.Name);
            componentRule.Rules.Add(volumeRule);

            SimDataMappingRuleFace faceRule = new SimDataMappingRuleFace("SheetA");
            faceRule.MappingDirection = SimDataMappingDirection.Horizontal;
            faceRule.MaxMatches = int.MaxValue;
            faceRule.MaxDepth = int.MaxValue;
            faceRule.OffsetParent = new RowColumnIndex(0, 2);
            faceRule.OffsetConsecutive = new RowColumnIndex(1, 0);
            faceRule.Properties.Add(SimDataMappingFaceMappingProperties.Name);
            volumeRule.Rules.Add(faceRule);

            var state = new SimTraversalState();
            var data = new SimMappedData();
            componentRule.Execute(comp1, state, data);

            Assert.AreEqual(1, data.Data.Count);
            Assert.IsTrue(data.Data.TryGetValue("SheetA", out var sheetData));

            Assert.AreEqual(13, sheetData.Count);
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(0, 0), "Volume");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(0, 1), "HoleVolume");

            AssertUtil.AssertContains(sheetData, new RowColumnIndex(0, 3), "Face 40");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(1, 3), "Face 38");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(2, 3), "Face 35");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(3, 3), "Face 30");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(4, 3), "Face 25");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(5, 3), "18");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(6, 3), "Face 76");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(7, 3), "Face 74");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(8, 3), "Face 71");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(9, 3), "Face 66");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(10, 3), "Face 61");
        }

        [TestMethod]
        public void MapVolumeOrientation()
        {
            this.LoadProject(mappingProject);

            var comp1 = projectData.Components.First(x => x.Name == "Geometry");

            SimDataMappingRuleComponent componentRule = new SimDataMappingRuleComponent("SheetA");
            componentRule.MappingDirection = SimDataMappingDirection.Horizontal;
            componentRule.MaxMatches = int.MaxValue;
            componentRule.MaxDepth = int.MaxValue;
            componentRule.OffsetConsecutive = new RowColumnIndex(1, 0);
            componentRule.Properties.Add(SimDataMappingComponentMappingProperties.Name);
            componentRule.Filter.Add(new SimDataMappingFilterComponent(SimDataMappingComponentFilterProperties.InstanceType, SimInstanceType.Entity3D));

            SimDataMappingRuleVolume volumeRule = new SimDataMappingRuleVolume("SheetA");
            volumeRule.OffsetParent = new RowColumnIndex(0, 1);
            volumeRule.OffsetConsecutive = new RowColumnIndex(1, 0);
            volumeRule.Filter.Add(new SimDataMappingFilterVolume(SimDataMappingVolumeFilterProperties.Name, "ShoeBox"));
            volumeRule.Properties.Add(SimDataMappingVolumeMappingProperties.Name);
            componentRule.Rules.Add(volumeRule);

            SimDataMappingRuleFace faceRule = new SimDataMappingRuleFace("SheetA");
            faceRule.MappingDirection = SimDataMappingDirection.Horizontal;
            faceRule.MaxMatches = int.MaxValue;
            faceRule.MaxDepth = int.MaxValue;
            faceRule.OffsetParent = new RowColumnIndex(0, 2);
            faceRule.OffsetConsecutive = new RowColumnIndex(1, 0);
            faceRule.Properties.Add(SimDataMappingFaceMappingProperties.Orientation);
            volumeRule.Rules.Add(faceRule);

            var state = new SimTraversalState();
            var data = new SimMappedData();
            componentRule.Execute(comp1, state, data);

            Assert.AreEqual(1, data.Data.Count);
            Assert.IsTrue(data.Data.TryGetValue("SheetA", out var sheetData));

            Assert.AreEqual(11, sheetData.Count);
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(0, 0), "Volume1");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(0, 1), "ShoeBox");

            AssertUtil.AssertContains(sheetData, new RowColumnIndex(0, 3), 0.0);
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(1, 3), 0.0);
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(2, 3), 90.0);
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(3, 3), 0.0);
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(4, 3), 270.0);
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(5, 3), 180.0);
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(6, 3), 90.0);
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(7, 3), 90.0);

            AssertUtil.AssertContains(sheetData, new RowColumnIndex(8, 0), "Volume2");
        }

        [TestMethod]
        public void MapVolumeIncline()
        {
            this.LoadProject(mappingProject);

            var comp1 = projectData.Components.First(x => x.Name == "Geometry");

            SimDataMappingRuleComponent componentRule = new SimDataMappingRuleComponent("SheetA");
            componentRule.MappingDirection = SimDataMappingDirection.Horizontal;
            componentRule.MaxMatches = int.MaxValue;
            componentRule.MaxDepth = int.MaxValue;
            componentRule.OffsetConsecutive = new RowColumnIndex(1, 0);
            componentRule.Properties.Add(SimDataMappingComponentMappingProperties.Name);
            componentRule.Filter.Add(new SimDataMappingFilterComponent(SimDataMappingComponentFilterProperties.InstanceType, SimInstanceType.Entity3D));

            SimDataMappingRuleVolume volumeRule = new SimDataMappingRuleVolume("SheetA");
            volumeRule.OffsetParent = new RowColumnIndex(0, 1);
            volumeRule.OffsetConsecutive = new RowColumnIndex(1, 0);
            volumeRule.Filter.Add(new SimDataMappingFilterVolume(SimDataMappingVolumeFilterProperties.Name, "ShoeBox"));
            volumeRule.Properties.Add(SimDataMappingVolumeMappingProperties.Name);
            componentRule.Rules.Add(volumeRule);

            SimDataMappingRuleFace faceRule = new SimDataMappingRuleFace("SheetA");
            faceRule.MappingDirection = SimDataMappingDirection.Horizontal;
            faceRule.MaxMatches = int.MaxValue;
            faceRule.MaxDepth = int.MaxValue;
            faceRule.OffsetParent = new RowColumnIndex(0, 2);
            faceRule.OffsetConsecutive = new RowColumnIndex(1, 0);
            faceRule.Properties.Add(SimDataMappingFaceMappingProperties.Incline);
            volumeRule.Rules.Add(faceRule);

            var state = new SimTraversalState();
            var data = new SimMappedData();
            componentRule.Execute(comp1, state, data);

            Assert.AreEqual(1, data.Data.Count);
            Assert.IsTrue(data.Data.TryGetValue("SheetA", out var sheetData));

            Assert.AreEqual(11, sheetData.Count);
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(0, 0), "Volume1");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(0, 1), "ShoeBox");

            AssertUtil.AssertContains(sheetData, new RowColumnIndex(0, 3), 0.0);
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(1, 3), -90.0);
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(2, 3), 0.0);
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(3, 3), 90.0);
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(4, 3), 0.0);
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(5, 3), 0.0);
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(6, 3), 0.0);
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(7, 3), 0.0);

            AssertUtil.AssertContains(sheetData, new RowColumnIndex(8, 0), "Volume2");
        }

        #endregion

        #region Filter

        [TestMethod]
        public void FilterName()
        {
            this.LoadProject(mappingProject);

            var comp1 = projectData.Components.First(x => x.Name == "Geometry");

            SimDataMappingRuleComponent componentRule = new SimDataMappingRuleComponent("SheetA");
            componentRule.MappingDirection = SimDataMappingDirection.Horizontal;
            componentRule.MaxMatches = int.MaxValue;
            componentRule.MaxDepth = int.MaxValue;
            componentRule.OffsetConsecutive = new RowColumnIndex(1, 0);
            componentRule.Properties.Add(SimDataMappingComponentMappingProperties.Name);
            componentRule.Filter.Add(new SimDataMappingFilterComponent(SimDataMappingComponentFilterProperties.InstanceType, SimInstanceType.AttributesFace));

            SimDataMappingRuleFace faceRule = new SimDataMappingRuleFace("SheetA");
            faceRule.MappingDirection = SimDataMappingDirection.Horizontal;
            faceRule.MaxMatches = int.MaxValue;
            faceRule.MaxDepth = int.MaxValue;
            faceRule.OffsetParent = new RowColumnIndex(0, 2);
            faceRule.OffsetConsecutive = new RowColumnIndex(1, 0);
            faceRule.Properties.Add(SimDataMappingFaceMappingProperties.Name);
            faceRule.Filter.Add(new SimDataMappingFilterFace(SimDataMappingFaceFilterProperties.Name, "Face 13"));
            componentRule.Rules.Add(faceRule);

            var state = new SimTraversalState();
            var data = new SimMappedData();
            componentRule.Execute(comp1, state, data);

            Assert.AreEqual(1, data.Data.Count);
            Assert.IsTrue(data.Data.TryGetValue("SheetA", out var sheetData));

            Assert.AreEqual(4, sheetData.Count);
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(0, 0), "Face1");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(0, 2), "Face 13");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(1, 2), "Face 13");

            AssertUtil.AssertContains(sheetData, new RowColumnIndex(2, 0), "Face2");
        }

        [TestMethod]
        public void FilterNameRegex()
        {
            this.LoadProject(mappingProject);

            var comp1 = projectData.Components.First(x => x.Name == "Geometry");

            SimDataMappingRuleComponent componentRule = new SimDataMappingRuleComponent("SheetA");
            componentRule.MappingDirection = SimDataMappingDirection.Horizontal;
            componentRule.MaxMatches = int.MaxValue;
            componentRule.MaxDepth = int.MaxValue;
            componentRule.OffsetConsecutive = new RowColumnIndex(1, 0);
            componentRule.Properties.Add(SimDataMappingComponentMappingProperties.Name);
            componentRule.Filter.Add(new SimDataMappingFilterComponent(SimDataMappingComponentFilterProperties.InstanceType, SimInstanceType.AttributesFace));

            SimDataMappingRuleFace faceRule = new SimDataMappingRuleFace("SheetA");
            faceRule.MappingDirection = SimDataMappingDirection.Horizontal;
            faceRule.MaxMatches = int.MaxValue;
            faceRule.MaxDepth = int.MaxValue;
            faceRule.OffsetParent = new RowColumnIndex(0, 2);
            faceRule.OffsetConsecutive = new RowColumnIndex(1, 0);
            faceRule.Properties.Add(SimDataMappingFaceMappingProperties.Name);
            faceRule.Filter.Add(new SimDataMappingFilterFace(SimDataMappingFaceFilterProperties.Name, new Regex("Face 3+")));
            componentRule.Rules.Add(faceRule);

            var state = new SimTraversalState();
            var data = new SimMappedData();
            componentRule.Execute(comp1, state, data);

            Assert.AreEqual(1, data.Data.Count);
            Assert.IsTrue(data.Data.TryGetValue("SheetA", out var sheetData));

            Assert.AreEqual(5, sheetData.Count);
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(0, 0), "Face1");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(0, 2), "Face 32");

            AssertUtil.AssertContains(sheetData, new RowColumnIndex(1, 0), "Face2");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(1, 2), "Face 33");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(2, 2), "Face 37");
        }

        [TestMethod]
        public void FilterFileId()
        {
            this.LoadProject(mappingProject);

            var comp1 = projectData.Components.First(x => x.Name == "Geometry");

            SimDataMappingRuleComponent componentRule = new SimDataMappingRuleComponent("SheetA");
            componentRule.MappingDirection = SimDataMappingDirection.Horizontal;
            componentRule.MaxMatches = int.MaxValue;
            componentRule.MaxDepth = int.MaxValue;
            componentRule.OffsetConsecutive = new RowColumnIndex(1, 0);
            componentRule.Properties.Add(SimDataMappingComponentMappingProperties.Name);
            componentRule.Filter.Add(new SimDataMappingFilterComponent(SimDataMappingComponentFilterProperties.InstanceType, SimInstanceType.AttributesFace));

            SimDataMappingRuleFace faceRule = new SimDataMappingRuleFace("SheetA");
            faceRule.MappingDirection = SimDataMappingDirection.Horizontal;
            faceRule.MaxMatches = int.MaxValue;
            faceRule.MaxDepth = int.MaxValue;
            faceRule.OffsetParent = new RowColumnIndex(0, 2);
            faceRule.OffsetConsecutive = new RowColumnIndex(1, 0);
            faceRule.Properties.Add(SimDataMappingFaceMappingProperties.Name);
            faceRule.Filter.Add(new SimDataMappingFilterFace(SimDataMappingFaceFilterProperties.FileKey, 1));
            componentRule.Rules.Add(faceRule);

            var state = new SimTraversalState();
            var data = new SimMappedData();
            componentRule.Execute(comp1, state, data);

            Assert.AreEqual(1, data.Data.Count);
            Assert.IsTrue(data.Data.TryGetValue("SheetA", out var sheetData));

            Assert.AreEqual(7, sheetData.Count);
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(0, 0), "Face1");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(0, 2), "Face 13");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(1, 2), "Face 32");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(2, 2), "Face 27");

            AssertUtil.AssertContains(sheetData, new RowColumnIndex(3, 0), "Face2");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(3, 2), "Face 37");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(4, 2), "Face 76");
        }

        [TestMethod]
        public void FilterWallTypeWall()
        {
            this.LoadProject(mappingProject);

            var comp1 = projectData.Components.First(x => x.Name == "Geometry");

            SimDataMappingRuleComponent componentRule = new SimDataMappingRuleComponent("SheetA");
            componentRule.MappingDirection = SimDataMappingDirection.Horizontal;
            componentRule.MaxMatches = int.MaxValue;
            componentRule.MaxDepth = int.MaxValue;
            componentRule.OffsetConsecutive = new RowColumnIndex(1, 0);
            componentRule.Properties.Add(SimDataMappingComponentMappingProperties.Name);
            componentRule.Filter.Add(new SimDataMappingFilterComponent(SimDataMappingComponentFilterProperties.InstanceType, SimInstanceType.AttributesFace));

            SimDataMappingRuleFace faceRule = new SimDataMappingRuleFace("SheetA");
            faceRule.MappingDirection = SimDataMappingDirection.Horizontal;
            faceRule.MaxMatches = int.MaxValue;
            faceRule.MaxDepth = int.MaxValue;
            faceRule.OffsetParent = new RowColumnIndex(0, 2);
            faceRule.OffsetConsecutive = new RowColumnIndex(1, 0);
            faceRule.Properties.Add(SimDataMappingFaceMappingProperties.Name);
            faceRule.Filter.Add(new SimDataMappingFilterFace(SimDataMappingFaceFilterProperties.FaceType, SimDataMappingFaceType.Wall));
            componentRule.Rules.Add(faceRule);

            var state = new SimTraversalState();
            var data = new SimMappedData();
            componentRule.Execute(comp1, state, data);

            Assert.AreEqual(1, data.Data.Count);
            Assert.IsTrue(data.Data.TryGetValue("SheetA", out var sheetData));

            Assert.AreEqual(6, sheetData.Count);
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(0, 0), "Face1");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(0, 2), "Face 13");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(1, 2), "Face 13");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(2, 2), "Face 32");

            AssertUtil.AssertContains(sheetData, new RowColumnIndex(3, 0), "Face2");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(3, 2), "Face 33");
        }

        [TestMethod]
        public void FilterWallTypeFloor()
        {
            this.LoadProject(mappingProject);

            var comp1 = projectData.Components.First(x => x.Name == "Geometry");

            SimDataMappingRuleComponent componentRule = new SimDataMappingRuleComponent("SheetA");
            componentRule.MappingDirection = SimDataMappingDirection.Horizontal;
            componentRule.MaxMatches = int.MaxValue;
            componentRule.MaxDepth = int.MaxValue;
            componentRule.OffsetConsecutive = new RowColumnIndex(1, 0);
            componentRule.Properties.Add(SimDataMappingComponentMappingProperties.Name);
            componentRule.Filter.Add(new SimDataMappingFilterComponent(SimDataMappingComponentFilterProperties.InstanceType, SimInstanceType.AttributesFace));

            SimDataMappingRuleFace faceRule = new SimDataMappingRuleFace("SheetA");
            faceRule.MappingDirection = SimDataMappingDirection.Horizontal;
            faceRule.MaxMatches = int.MaxValue;
            faceRule.MaxDepth = int.MaxValue;
            faceRule.OffsetParent = new RowColumnIndex(0, 2);
            faceRule.OffsetConsecutive = new RowColumnIndex(1, 0);
            faceRule.Properties.Add(SimDataMappingFaceMappingProperties.Name);
            faceRule.Filter.Add(new SimDataMappingFilterFace(SimDataMappingFaceFilterProperties.FaceType, SimDataMappingFaceType.Floor));
            componentRule.Rules.Add(faceRule);

            var state = new SimTraversalState();
            var data = new SimMappedData();
            componentRule.Execute(comp1, state, data);

            Assert.AreEqual(1, data.Data.Count);
            Assert.IsTrue(data.Data.TryGetValue("SheetA", out var sheetData));

            Assert.AreEqual(4, sheetData.Count);
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(0, 0), "Face1");

            AssertUtil.AssertContains(sheetData, new RowColumnIndex(1, 0), "Face2");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(1, 2), "Face 37");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(2, 2), "Face 76");
        }

        [TestMethod]
        public void FilterWallTypeCeiling()
        {
            this.LoadProject(mappingProject);

            var comp1 = projectData.Components.First(x => x.Name == "Geometry");

            SimDataMappingRuleComponent componentRule = new SimDataMappingRuleComponent("SheetA");
            componentRule.MappingDirection = SimDataMappingDirection.Horizontal;
            componentRule.MaxMatches = int.MaxValue;
            componentRule.MaxDepth = int.MaxValue;
            componentRule.OffsetConsecutive = new RowColumnIndex(1, 0);
            componentRule.Properties.Add(SimDataMappingComponentMappingProperties.Name);
            componentRule.Filter.Add(new SimDataMappingFilterComponent(SimDataMappingComponentFilterProperties.InstanceType, SimInstanceType.AttributesFace));

            SimDataMappingRuleFace faceRule = new SimDataMappingRuleFace("SheetA");
            faceRule.MappingDirection = SimDataMappingDirection.Horizontal;
            faceRule.MaxMatches = int.MaxValue;
            faceRule.MaxDepth = int.MaxValue;
            faceRule.OffsetParent = new RowColumnIndex(0, 2);
            faceRule.OffsetConsecutive = new RowColumnIndex(1, 0);
            faceRule.Properties.Add(SimDataMappingFaceMappingProperties.Name);
            faceRule.Filter.Add(new SimDataMappingFilterFace(SimDataMappingFaceFilterProperties.FaceType, SimDataMappingFaceType.Ceiling));
            componentRule.Rules.Add(faceRule);

            var state = new SimTraversalState();
            var data = new SimMappedData();
            componentRule.Execute(comp1, state, data);

            Assert.AreEqual(1, data.Data.Count);
            Assert.IsTrue(data.Data.TryGetValue("SheetA", out var sheetData));

            Assert.AreEqual(3, sheetData.Count);
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(0, 0), "Face1");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(0, 2), "Face 27");

            AssertUtil.AssertContains(sheetData, new RowColumnIndex(1, 0), "Face2");
        }

        [TestMethod]
        public void FilterWallTypeFloorOrCeiling()
        {
            this.LoadProject(mappingProject);

            var comp1 = projectData.Components.First(x => x.Name == "Geometry");

            SimDataMappingRuleComponent componentRule = new SimDataMappingRuleComponent("SheetA");
            componentRule.MappingDirection = SimDataMappingDirection.Horizontal;
            componentRule.MaxMatches = int.MaxValue;
            componentRule.MaxDepth = int.MaxValue;
            componentRule.OffsetConsecutive = new RowColumnIndex(1, 0);
            componentRule.Properties.Add(SimDataMappingComponentMappingProperties.Name);
            componentRule.Filter.Add(new SimDataMappingFilterComponent(SimDataMappingComponentFilterProperties.InstanceType, SimInstanceType.AttributesFace));

            SimDataMappingRuleFace faceRule = new SimDataMappingRuleFace("SheetA");
            faceRule.MappingDirection = SimDataMappingDirection.Horizontal;
            faceRule.MaxMatches = int.MaxValue;
            faceRule.MaxDepth = int.MaxValue;
            faceRule.OffsetParent = new RowColumnIndex(0, 2);
            faceRule.OffsetConsecutive = new RowColumnIndex(1, 0);
            faceRule.Properties.Add(SimDataMappingFaceMappingProperties.Name);
            faceRule.Filter.Add(new SimDataMappingFilterFace(SimDataMappingFaceFilterProperties.FaceType, SimDataMappingFaceType.FloorOrCeiling));
            componentRule.Rules.Add(faceRule);

            var state = new SimTraversalState();
            var data = new SimMappedData();
            componentRule.Execute(comp1, state, data);

            Assert.AreEqual(1, data.Data.Count);
            Assert.IsTrue(data.Data.TryGetValue("SheetA", out var sheetData));

            Assert.AreEqual(5, sheetData.Count);
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(0, 0), "Face1");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(0, 2), "Face 27");

            AssertUtil.AssertContains(sheetData, new RowColumnIndex(1, 0), "Face2");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(1, 2), "Face 37");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(2, 2), "Face 76");
        }

        #endregion

        #region Other stuff

        [TestMethod]
        public void MaxMatches()
        {
            this.LoadProject(mappingProject);

            var comp1 = projectData.Components.First(x => x.Name == "Geometry");

            SimDataMappingRuleComponent componentRule = new SimDataMappingRuleComponent("SheetA");
            componentRule.MappingDirection = SimDataMappingDirection.Horizontal;
            componentRule.MaxMatches = int.MaxValue;
            componentRule.MaxDepth = int.MaxValue;
            componentRule.OffsetConsecutive = new RowColumnIndex(1, 0);
            componentRule.Properties.Add(SimDataMappingComponentMappingProperties.Name);
            componentRule.Filter.Add(new SimDataMappingFilterComponent(SimDataMappingComponentFilterProperties.InstanceType, SimInstanceType.AttributesFace));

            SimDataMappingRuleFace faceRule = new SimDataMappingRuleFace("SheetA");
            faceRule.MappingDirection = SimDataMappingDirection.Horizontal;
            faceRule.MaxMatches = 2;
            faceRule.MaxDepth = int.MaxValue;
            faceRule.OffsetParent = new RowColumnIndex(0, 2);
            faceRule.OffsetConsecutive = new RowColumnIndex(1, 0);
            faceRule.Properties.Add(SimDataMappingFaceMappingProperties.Name);
            componentRule.Rules.Add(faceRule);

            var state = new SimTraversalState();
            var data = new SimMappedData();
            componentRule.Execute(comp1, state, data);

            Assert.AreEqual(1, data.Data.Count);
            Assert.IsTrue(data.Data.TryGetValue("SheetA", out var sheetData));

            Assert.AreEqual(6, sheetData.Count);
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(0, 0), "Face1");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(0, 2), "Face 13");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(1, 2), "Face 13");

            AssertUtil.AssertContains(sheetData, new RowColumnIndex(2, 0), "Face2");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(2, 2), "Face 33");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(3, 2), "Face 37");
        }

        [TestMethod]
        public void MapHoleName()
        {
            this.LoadProject(mappingProject);

            var comp1 = projectData.Components.First(x => x.Name == "Geometry (Additional)");

            SimDataMappingRuleComponent componentRule = new SimDataMappingRuleComponent("SheetA");
            componentRule.MappingDirection = SimDataMappingDirection.Horizontal;
            componentRule.MaxMatches = int.MaxValue;
            componentRule.MaxDepth = int.MaxValue;
            componentRule.OffsetConsecutive = new RowColumnIndex(1, 0);
            componentRule.Properties.Add(SimDataMappingComponentMappingProperties.Name);
            componentRule.Filter.Add(new SimDataMappingFilterComponent(SimDataMappingComponentFilterProperties.Name, "InteriorWall"));

            SimDataMappingRuleFace faceRule = new SimDataMappingRuleFace("SheetA");
            faceRule.MappingDirection = SimDataMappingDirection.Horizontal;
            faceRule.MaxMatches = int.MaxValue;
            faceRule.MaxDepth = int.MaxValue;
            faceRule.OffsetParent = new RowColumnIndex(0, 1);
            faceRule.OffsetConsecutive = new RowColumnIndex(1, 0);
            faceRule.Properties.Add(SimDataMappingFaceMappingProperties.Name);
            componentRule.Rules.Add(faceRule);

            SimDataMappingRuleFace holeRule = new SimDataMappingRuleFace("SheetA");
            holeRule.MaxMatches = int.MaxValue;
            holeRule.MaxDepth = int.MaxValue;
            holeRule.OffsetParent = new RowColumnIndex(0, 1);
            holeRule.OffsetConsecutive = new RowColumnIndex(1, 0);
            holeRule.Properties.Add(SimDataMappingFaceMappingProperties.Name);
            faceRule.Rules.Add(holeRule);

            var state = new SimTraversalState();
            var data = new SimMappedData();
            componentRule.Execute(comp1, state, data);

            Assert.AreEqual(1, data.Data.Count);
            Assert.IsTrue(data.Data.TryGetValue("SheetA", out var sheetData));

            Assert.AreEqual(4, sheetData.Count);
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(0, 0), "InteriorWall");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(0, 1), "Face 45");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(0, 2), "Face 94");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(1, 2), "Face 104");
        }

        [TestMethod]
        public void FilterHoleName()
        {
            this.LoadProject(mappingProject);

            var comp1 = projectData.Components.First(x => x.Name == "Geometry (Additional)");

            SimDataMappingRuleComponent componentRule = new SimDataMappingRuleComponent("SheetA");
            componentRule.MappingDirection = SimDataMappingDirection.Horizontal;
            componentRule.MaxMatches = int.MaxValue;
            componentRule.MaxDepth = int.MaxValue;
            componentRule.OffsetConsecutive = new RowColumnIndex(1, 0);
            componentRule.Properties.Add(SimDataMappingComponentMappingProperties.Name);
            componentRule.Filter.Add(new SimDataMappingFilterComponent(SimDataMappingComponentFilterProperties.Name, "InteriorWall"));

            SimDataMappingRuleFace faceRule = new SimDataMappingRuleFace("SheetA");
            faceRule.MappingDirection = SimDataMappingDirection.Horizontal;
            faceRule.MaxMatches = int.MaxValue;
            faceRule.MaxDepth = int.MaxValue;
            faceRule.OffsetParent = new RowColumnIndex(0, 1);
            faceRule.OffsetConsecutive = new RowColumnIndex(1, 0);
            faceRule.Properties.Add(SimDataMappingFaceMappingProperties.Name);
            componentRule.Rules.Add(faceRule);

            SimDataMappingRuleFace holeRule = new SimDataMappingRuleFace("SheetA");
            holeRule.MaxMatches = int.MaxValue;
            holeRule.MaxDepth = int.MaxValue;
            holeRule.OffsetParent = new RowColumnIndex(0, 1);
            holeRule.OffsetConsecutive = new RowColumnIndex(1, 0);
            holeRule.Filter.Add(new SimDataMappingFilterFace(SimDataMappingFaceFilterProperties.Name, new Regex(".+9.+")));
            holeRule.Properties.Add(SimDataMappingFaceMappingProperties.Name);
            faceRule.Rules.Add(holeRule);

            var state = new SimTraversalState();
            var data = new SimMappedData();
            componentRule.Execute(comp1, state, data);

            Assert.AreEqual(1, data.Data.Count);
            Assert.IsTrue(data.Data.TryGetValue("SheetA", out var sheetData));

            Assert.AreEqual(3, sheetData.Count);
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(0, 0), "InteriorWall");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(0, 1), "Face 45");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(0, 2), "Face 94");
        }

        #endregion

        [TestMethod]
        public void CloneTest()
        {
            SimDataMappingRuleFace faceRule = new SimDataMappingRuleFace("Sheet A")
            {
                Name = "SomeName",
                MappingDirection = SimDataMappingDirection.Vertical,
                MaxDepth = 3,
                MaxMatches = 5,
                OffsetConsecutive = new RowColumnIndex(8, 7),
                OffsetParent = new RowColumnIndex(10, 9),
                ReferencePointParent = SimDataMappingReferencePoint.TopRight,
            };

            faceRule.Filter.Add(new SimDataMappingFilterFace(SimDataMappingFaceFilterProperties.FileKey, 33));
            faceRule.Filter.Add(new SimDataMappingFilterFace(SimDataMappingFaceFilterProperties.Name, "asdf"));

            faceRule.Properties.Add(SimDataMappingFaceMappingProperties.Name);
            faceRule.Properties.Add(SimDataMappingFaceMappingProperties.Orientation);

            SimDataMappingRuleComponent childRule1 = new SimDataMappingRuleComponent("Sheet B");
            SimDataMappingRuleVolume childRule2 = new SimDataMappingRuleVolume("Sheet B");
            SimDataMappingRuleInstance childRule3 = new SimDataMappingRuleInstance("Sheet B");
            SimDataMappingRuleFace childRule4 = new SimDataMappingRuleFace("Sheet B");

            faceRule.Rules.Add(childRule1);
            faceRule.Rules.Add(childRule2);
            faceRule.Rules.Add(childRule3);
            faceRule.Rules.Add(childRule4);

            //Test
            var clonedRule = faceRule.Clone();

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

            Assert.IsTrue(clonedRule.Rules[0] is SimDataMappingRuleComponent);
            Assert.IsTrue(clonedRule.Rules[1] is SimDataMappingRuleVolume);
            Assert.IsTrue(clonedRule.Rules[2] is SimDataMappingRuleInstance);
            Assert.IsTrue(clonedRule.Rules[3] is SimDataMappingRuleFace);

            Assert.AreEqual(2, clonedRule.Filter.Count);
            Assert.AreEqual(SimDataMappingFaceFilterProperties.FileKey, clonedRule.Filter[0].Property);
            Assert.AreEqual(33, clonedRule.Filter[0].Value);
            Assert.AreEqual(SimDataMappingFaceFilterProperties.Name, clonedRule.Filter[1].Property);
            Assert.AreEqual("asdf", clonedRule.Filter[1].Value);

            Assert.AreEqual(2, clonedRule.Properties.Count);
            Assert.AreEqual(SimDataMappingFaceMappingProperties.Name, clonedRule.Properties[0]);
            Assert.AreEqual(SimDataMappingFaceMappingProperties.Orientation, clonedRule.Properties[1]);
        }
    }
}
