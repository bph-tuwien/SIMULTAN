using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data.Components;
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
    public class VolumeRuleTests : BaseProjectTest
    {
        private static readonly FileInfo mappingProject = new FileInfo(@".\ExcelMappingTests.simultan");

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
            componentRule.OffsetConsecutive = new IntIndex2D(0, 1);
            componentRule.Properties.Add(SimDataMappingComponentMappingProperties.Name);
            componentRule.Filter.Add(new SimDataMappingFilterComponent(SimDataMappingComponentFilterProperties.InstanceType, SimInstanceType.Entity3D));

            SimDataMappingRuleVolume volumeRule = new SimDataMappingRuleVolume("SheetA");
            volumeRule.MappingDirection = SimDataMappingDirection.Horizontal;
            volumeRule.MaxMatches = int.MaxValue;
            volumeRule.MaxDepth = int.MaxValue;
            volumeRule.OffsetParent = new IntIndex2D(2, 0);
            volumeRule.OffsetConsecutive = new IntIndex2D(0, 1);
            volumeRule.Properties.Add(SimDataMappingVolumeMappingProperties.Name);
            componentRule.Rules.Add(volumeRule);

            var state = new SimTraversalState();
            var data = new SimMappedData();
            componentRule.Execute(comp1, state, data);

            Assert.AreEqual(1, data.Data.Count);
            Assert.IsTrue(data.Data.TryGetValue("SheetA", out var sheetData));

            Assert.AreEqual(4, sheetData.Count);
            AssertUtil.AssertContains(sheetData, new IntIndex2D(0, 0), "Volume1");
            AssertUtil.AssertContains(sheetData, new IntIndex2D(2, 0), "ShoeBox");
            AssertUtil.AssertContains(sheetData, new IntIndex2D(0, 1), "Volume2");
            AssertUtil.AssertContains(sheetData, new IntIndex2D(2, 1), "NiceRoom");
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
            componentRule.OffsetConsecutive = new IntIndex2D(0, 1);
            componentRule.Properties.Add(SimDataMappingComponentMappingProperties.Name);
            componentRule.Filter.Add(new SimDataMappingFilterComponent(SimDataMappingComponentFilterProperties.InstanceType, SimInstanceType.Entity3D));

            SimDataMappingRuleVolume volumeRule = new SimDataMappingRuleVolume("SheetA");
            volumeRule.MappingDirection = SimDataMappingDirection.Horizontal;
            volumeRule.MaxMatches = int.MaxValue;
            volumeRule.MaxDepth = int.MaxValue;
            volumeRule.OffsetParent = new IntIndex2D(2, 0);
            volumeRule.OffsetConsecutive = new IntIndex2D(0, 1);
            volumeRule.Properties.Add(SimDataMappingVolumeMappingProperties.Id);
            componentRule.Rules.Add(volumeRule);

            var state = new SimTraversalState();
            var data = new SimMappedData();
            componentRule.Execute(comp1, state, data);

            Assert.AreEqual(1, data.Data.Count);
            Assert.IsTrue(data.Data.TryGetValue("SheetA", out var sheetData));

            Assert.AreEqual(4, sheetData.Count);
            AssertUtil.AssertContains(sheetData, new IntIndex2D(0, 0), "Volume1");
            AssertUtil.AssertContains(sheetData, new IntIndex2D(2, 0), 56);
            AssertUtil.AssertContains(sheetData, new IntIndex2D(0, 1), "Volume2");
            AssertUtil.AssertContains(sheetData, new IntIndex2D(2, 1), 79);
        }

        [TestMethod]
        public void MapVolume()
        {
            this.LoadProject(mappingProject);

            var comp1 = projectData.Components.First(x => x.Name == "Geometry");

            SimDataMappingRuleComponent componentRule = new SimDataMappingRuleComponent("SheetA");
            componentRule.MappingDirection = SimDataMappingDirection.Horizontal;
            componentRule.MaxMatches = int.MaxValue;
            componentRule.MaxDepth = int.MaxValue;
            componentRule.OffsetConsecutive = new IntIndex2D(0, 1);
            componentRule.Properties.Add(SimDataMappingComponentMappingProperties.Name);
            componentRule.Filter.Add(new SimDataMappingFilterComponent(SimDataMappingComponentFilterProperties.InstanceType, SimInstanceType.Entity3D));

            SimDataMappingRuleVolume volumeRule = new SimDataMappingRuleVolume("SheetA");
            volumeRule.MappingDirection = SimDataMappingDirection.Horizontal;
            volumeRule.MaxMatches = int.MaxValue;
            volumeRule.MaxDepth = int.MaxValue;
            volumeRule.OffsetParent = new IntIndex2D(2, 0);
            volumeRule.OffsetConsecutive = new IntIndex2D(0, 1);
            volumeRule.Properties.Add(SimDataMappingVolumeMappingProperties.Volume);
            componentRule.Rules.Add(volumeRule);

            var state = new SimTraversalState();
            var data = new SimMappedData();
            componentRule.Execute(comp1, state, data);

            Assert.AreEqual(1, data.Data.Count);
            Assert.IsTrue(data.Data.TryGetValue("SheetA", out var sheetData));

            Assert.AreEqual(4, sheetData.Count);
            AssertUtil.AssertContains(sheetData, new IntIndex2D(0, 0), "Volume1");
            AssertUtil.AssertContains(sheetData, new IntIndex2D(2, 0), 2000.0);
            AssertUtil.AssertContains(sheetData, new IntIndex2D(0, 1), "Volume2");
            AssertUtil.AssertContains(sheetData, new IntIndex2D(2, 1), 375.0);
        }

        [TestMethod]
        public void MapFloorArea()
        {
            this.LoadProject(mappingProject);

            var comp1 = projectData.Components.First(x => x.Name == "Geometry");

            SimDataMappingRuleComponent componentRule = new SimDataMappingRuleComponent("SheetA");
            componentRule.MappingDirection = SimDataMappingDirection.Horizontal;
            componentRule.MaxMatches = int.MaxValue;
            componentRule.MaxDepth = int.MaxValue;
            componentRule.OffsetConsecutive = new IntIndex2D(0, 1);
            componentRule.Properties.Add(SimDataMappingComponentMappingProperties.Name);
            componentRule.Filter.Add(new SimDataMappingFilterComponent(SimDataMappingComponentFilterProperties.InstanceType, SimInstanceType.Entity3D));

            SimDataMappingRuleVolume volumeRule = new SimDataMappingRuleVolume("SheetA");
            volumeRule.MappingDirection = SimDataMappingDirection.Horizontal;
            volumeRule.MaxMatches = int.MaxValue;
            volumeRule.MaxDepth = int.MaxValue;
            volumeRule.OffsetParent = new IntIndex2D(2, 0);
            volumeRule.OffsetConsecutive = new IntIndex2D(0, 1);
            volumeRule.Properties.Add(SimDataMappingVolumeMappingProperties.FloorArea);
            componentRule.Rules.Add(volumeRule);

            var state = new SimTraversalState();
            var data = new SimMappedData();
            componentRule.Execute(comp1, state, data);

            Assert.AreEqual(1, data.Data.Count);
            Assert.IsTrue(data.Data.TryGetValue("SheetA", out var sheetData));

            Assert.AreEqual(4, sheetData.Count);
            AssertUtil.AssertContains(sheetData, new IntIndex2D(0, 0), "Volume1");
            AssertUtil.AssertContains(sheetData, new IntIndex2D(2, 0), 200.0);
            AssertUtil.AssertContains(sheetData, new IntIndex2D(0, 1), "Volume2");
            AssertUtil.AssertContains(sheetData, new IntIndex2D(2, 1), 75.0);
        }

        [TestMethod]
        public void MapFloorHeight()
        {
            this.LoadProject(mappingProject);

            var comp1 = projectData.Components.First(x => x.Name == "Geometry");

            SimDataMappingRuleComponent componentRule = new SimDataMappingRuleComponent("SheetA");
            componentRule.MappingDirection = SimDataMappingDirection.Horizontal;
            componentRule.MaxMatches = int.MaxValue;
            componentRule.MaxDepth = int.MaxValue;
            componentRule.OffsetConsecutive = new IntIndex2D(0, 1);
            componentRule.Properties.Add(SimDataMappingComponentMappingProperties.Name);
            componentRule.Filter.Add(new SimDataMappingFilterComponent(SimDataMappingComponentFilterProperties.InstanceType, SimInstanceType.Entity3D));

            SimDataMappingRuleVolume volumeRule = new SimDataMappingRuleVolume("SheetA");
            volumeRule.MappingDirection = SimDataMappingDirection.Horizontal;
            volumeRule.MaxMatches = int.MaxValue;
            volumeRule.MaxDepth = int.MaxValue;
            volumeRule.OffsetParent = new IntIndex2D(2, 0);
            volumeRule.OffsetConsecutive = new IntIndex2D(0, 1);
            volumeRule.Properties.Add(SimDataMappingVolumeMappingProperties.Height);
            componentRule.Rules.Add(volumeRule);

            var state = new SimTraversalState();
            var data = new SimMappedData();
            componentRule.Execute(comp1, state, data);

            Assert.AreEqual(1, data.Data.Count);
            Assert.IsTrue(data.Data.TryGetValue("SheetA", out var sheetData));

            Assert.AreEqual(4, sheetData.Count);
            AssertUtil.AssertContains(sheetData, new IntIndex2D(0, 0), "Volume1");
            AssertUtil.AssertContains(sheetData, new IntIndex2D(2, 0), 10.0);
            AssertUtil.AssertContains(sheetData, new IntIndex2D(0, 1), "Volume2");
            AssertUtil.AssertContains(sheetData, new IntIndex2D(2, 1), 5.0);
        }

        [TestMethod]
        public void MapFloorElevation()
        {
            this.LoadProject(mappingProject);

            var comp1 = projectData.Components.First(x => x.Name == "Geometry");

            SimDataMappingRuleComponent componentRule = new SimDataMappingRuleComponent("SheetA");
            componentRule.MappingDirection = SimDataMappingDirection.Horizontal;
            componentRule.MaxMatches = int.MaxValue;
            componentRule.MaxDepth = int.MaxValue;
            componentRule.OffsetConsecutive = new IntIndex2D(0, 1);
            componentRule.Properties.Add(SimDataMappingComponentMappingProperties.Name);
            componentRule.Filter.Add(new SimDataMappingFilterComponent(SimDataMappingComponentFilterProperties.InstanceType, SimInstanceType.Entity3D));

            SimDataMappingRuleVolume volumeRule = new SimDataMappingRuleVolume("SheetA");
            volumeRule.MappingDirection = SimDataMappingDirection.Horizontal;
            volumeRule.MaxMatches = int.MaxValue;
            volumeRule.MaxDepth = int.MaxValue;
            volumeRule.OffsetParent = new IntIndex2D(2, 0);
            volumeRule.OffsetConsecutive = new IntIndex2D(0, 1);
            volumeRule.Properties.Add(SimDataMappingVolumeMappingProperties.FloorElevation);
            componentRule.Rules.Add(volumeRule);

            var state = new SimTraversalState();
            var data = new SimMappedData();
            componentRule.Execute(comp1, state, data);

            Assert.AreEqual(1, data.Data.Count);
            Assert.IsTrue(data.Data.TryGetValue("SheetA", out var sheetData));

            Assert.AreEqual(4, sheetData.Count);
            AssertUtil.AssertContains(sheetData, new IntIndex2D(0, 0), "Volume1");
            AssertUtil.AssertContains(sheetData, new IntIndex2D(2, 0), 0.0);
            AssertUtil.AssertContains(sheetData, new IntIndex2D(0, 1), "Volume2");
            AssertUtil.AssertContains(sheetData, new IntIndex2D(2, 1), 5.0);
        }

        [TestMethod]
        public void MapCeilingElevation()
        {
            this.LoadProject(mappingProject);

            var comp1 = projectData.Components.First(x => x.Name == "Geometry");

            SimDataMappingRuleComponent componentRule = new SimDataMappingRuleComponent("SheetA");
            componentRule.MappingDirection = SimDataMappingDirection.Horizontal;
            componentRule.MaxMatches = int.MaxValue;
            componentRule.MaxDepth = int.MaxValue;
            componentRule.OffsetConsecutive = new IntIndex2D(0, 1);
            componentRule.Properties.Add(SimDataMappingComponentMappingProperties.Name);
            componentRule.Filter.Add(new SimDataMappingFilterComponent(SimDataMappingComponentFilterProperties.InstanceType, SimInstanceType.Entity3D));

            SimDataMappingRuleVolume volumeRule = new SimDataMappingRuleVolume("SheetA");
            volumeRule.MappingDirection = SimDataMappingDirection.Horizontal;
            volumeRule.MaxMatches = int.MaxValue;
            volumeRule.MaxDepth = int.MaxValue;
            volumeRule.OffsetParent = new IntIndex2D(2, 0);
            volumeRule.OffsetConsecutive = new IntIndex2D(0, 1);
            volumeRule.Properties.Add(SimDataMappingVolumeMappingProperties.CeilingElevation);
            componentRule.Rules.Add(volumeRule);

            var state = new SimTraversalState();
            var data = new SimMappedData();
            componentRule.Execute(comp1, state, data);

            Assert.AreEqual(1, data.Data.Count);
            Assert.IsTrue(data.Data.TryGetValue("SheetA", out var sheetData));

            Assert.AreEqual(4, sheetData.Count);
            AssertUtil.AssertContains(sheetData, new IntIndex2D(0, 0), "Volume1");
            AssertUtil.AssertContains(sheetData, new IntIndex2D(2, 0), 10.0);
            AssertUtil.AssertContains(sheetData, new IntIndex2D(0, 1), "Volume2");
            AssertUtil.AssertContains(sheetData, new IntIndex2D(2, 1), 10.0);
        }

        #endregion

        #region Volume of Instance

        [TestMethod]
        public void MapInstanceName()
        {
            this.LoadProject(mappingProject);

            var comp1 = projectData.Components.First(x => x.Name == "Geometry");

            SimDataMappingRuleComponent componentRule = new SimDataMappingRuleComponent("SheetA");
            componentRule.MappingDirection = SimDataMappingDirection.Horizontal;
            componentRule.MaxMatches = int.MaxValue;
            componentRule.MaxDepth = int.MaxValue;
            componentRule.OffsetConsecutive = new IntIndex2D(0, 1);
            componentRule.Properties.Add(SimDataMappingComponentMappingProperties.Name);
            componentRule.Filter.Add(new SimDataMappingFilterComponent(SimDataMappingComponentFilterProperties.InstanceType, SimInstanceType.Entity3D));

            SimDataMappingRuleInstance instanceRule = new SimDataMappingRuleInstance("SheetA");
            instanceRule.MappingDirection = SimDataMappingDirection.Horizontal;
            instanceRule.MaxMatches = int.MaxValue;
            instanceRule.MaxDepth = int.MaxValue;
            instanceRule.OffsetParent = new IntIndex2D(1, 0);
            instanceRule.OffsetConsecutive = new IntIndex2D(0, 1);
            instanceRule.Properties.Add(SimDataMappingInstanceMappingProperties.Name);
            componentRule.Rules.Add(instanceRule);

            SimDataMappingRuleVolume volumeRule = new SimDataMappingRuleVolume("SheetA");
            volumeRule.MappingDirection = SimDataMappingDirection.Horizontal;
            volumeRule.MaxMatches = int.MaxValue;
            volumeRule.MaxDepth = int.MaxValue;
            volumeRule.OffsetParent = new IntIndex2D(1, 0);
            volumeRule.OffsetConsecutive = new IntIndex2D(0, 1);
            volumeRule.Properties.Add(SimDataMappingVolumeMappingProperties.Name);
            instanceRule.Rules.Add(volumeRule);

            var state = new SimTraversalState();
            var data = new SimMappedData();
            componentRule.Execute(comp1, state, data);

            Assert.AreEqual(1, data.Data.Count);
            Assert.IsTrue(data.Data.TryGetValue("SheetA", out var sheetData));

            Assert.AreEqual(6, sheetData.Count);
            AssertUtil.AssertContains(sheetData, new IntIndex2D(0, 0), "Volume1");
            AssertUtil.AssertContains(sheetData, new IntIndex2D(1, 0), "Geometry Placement 0:56");
            AssertUtil.AssertContains(sheetData, new IntIndex2D(2, 0), "ShoeBox");
            AssertUtil.AssertContains(sheetData, new IntIndex2D(0, 1), "Volume2");
            AssertUtil.AssertContains(sheetData, new IntIndex2D(1, 1), "Geometry Placement 1:79");
            AssertUtil.AssertContains(sheetData, new IntIndex2D(2, 1), "NiceRoom");
        }

        #endregion

        #region Volume of Faces

        [TestMethod]
        public void MapFaceName()
        {
            this.LoadProject(mappingProject);

            var comp1 = projectData.Components.First(x => x.Name == "Geometry (Additional)");

            SimDataMappingRuleComponent componentRule = new SimDataMappingRuleComponent("SheetA");
            componentRule.MappingDirection = SimDataMappingDirection.Horizontal;
            componentRule.MaxMatches = int.MaxValue;
            componentRule.MaxDepth = int.MaxValue;
            componentRule.OffsetConsecutive = new IntIndex2D(0, 1);
            componentRule.Properties.Add(SimDataMappingComponentMappingProperties.Name);
            componentRule.Filter.Add(new SimDataMappingFilterComponent(SimDataMappingComponentFilterProperties.Name, "InteriorWall"));

            SimDataMappingRuleFace faceRule = new SimDataMappingRuleFace("SheetA");
            faceRule.MappingDirection = SimDataMappingDirection.Horizontal;
            faceRule.MaxMatches = int.MaxValue;
            faceRule.MaxDepth = int.MaxValue;
            faceRule.OffsetParent = new IntIndex2D(1, 0);
            faceRule.OffsetConsecutive = new IntIndex2D(0, 1);
            faceRule.Properties.Add(SimDataMappingFaceMappingProperties.Name);
            componentRule.Rules.Add(faceRule);

            SimDataMappingRuleVolume volumeRule = new SimDataMappingRuleVolume("SheetA");
            volumeRule.MappingDirection = SimDataMappingDirection.Horizontal;
            volumeRule.MaxMatches = int.MaxValue;
            volumeRule.MaxDepth = int.MaxValue;
            volumeRule.OffsetParent = new IntIndex2D(1, 0);
            volumeRule.OffsetConsecutive = new IntIndex2D(0, 1);
            volumeRule.Properties.Add(SimDataMappingVolumeMappingProperties.Name);
            faceRule.Rules.Add(volumeRule);

            var state = new SimTraversalState();
            var data = new SimMappedData();
            componentRule.Execute(comp1, state, data);

            Assert.AreEqual(1, data.Data.Count);
            Assert.IsTrue(data.Data.TryGetValue("SheetA", out var sheetData));

            Assert.AreEqual(4, sheetData.Count);
            AssertUtil.AssertContains(sheetData, new IntIndex2D(0, 0), "InteriorWall");
            AssertUtil.AssertContains(sheetData, new IntIndex2D(1, 0), "Face 45");
            AssertUtil.AssertContains(sheetData, new IntIndex2D(2, 0), "ShoeBox");
            AssertUtil.AssertContains(sheetData, new IntIndex2D(2, 1), "Volume 79");
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
            componentRule.OffsetConsecutive = new IntIndex2D(0, 1);
            componentRule.Properties.Add(SimDataMappingComponentMappingProperties.Name);
            componentRule.Filter.Add(new SimDataMappingFilterComponent(SimDataMappingComponentFilterProperties.InstanceType, SimInstanceType.Entity3D));

            SimDataMappingRuleVolume volumeRule = new SimDataMappingRuleVolume("SheetA");
            volumeRule.MappingDirection = SimDataMappingDirection.Horizontal;
            volumeRule.MaxMatches = int.MaxValue;
            volumeRule.MaxDepth = int.MaxValue;
            volumeRule.OffsetParent = new IntIndex2D(2, 0);
            volumeRule.OffsetConsecutive = new IntIndex2D(0, 1);
            volumeRule.Properties.Add(SimDataMappingVolumeMappingProperties.Name);
            volumeRule.Filter.Add(new SimDataMappingFilterVolume(SimDataMappingVolumeFilterProperties.Name, "ShoeBox"));
            componentRule.Rules.Add(volumeRule);

            var state = new SimTraversalState();
            var data = new SimMappedData();
            componentRule.Execute(comp1, state, data);

            Assert.AreEqual(1, data.Data.Count);
            Assert.IsTrue(data.Data.TryGetValue("SheetA", out var sheetData));

            Assert.AreEqual(3, sheetData.Count);
            AssertUtil.AssertContains(sheetData, new IntIndex2D(0, 0), "Volume1");
            AssertUtil.AssertContains(sheetData, new IntIndex2D(2, 0), "ShoeBox");
            AssertUtil.AssertContains(sheetData, new IntIndex2D(0, 1), "Volume2");

            Assert.AreEqual(2, state.ModelsToRelease.Count);
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
            componentRule.OffsetConsecutive = new IntIndex2D(0, 1);
            componentRule.Properties.Add(SimDataMappingComponentMappingProperties.Name);
            componentRule.Filter.Add(new SimDataMappingFilterComponent(SimDataMappingComponentFilterProperties.InstanceType, SimInstanceType.Entity3D));

            SimDataMappingRuleVolume volumeRule = new SimDataMappingRuleVolume("SheetA");
            volumeRule.MappingDirection = SimDataMappingDirection.Horizontal;
            volumeRule.MaxMatches = int.MaxValue;
            volumeRule.MaxDepth = int.MaxValue;
            volumeRule.OffsetParent = new IntIndex2D(2, 0);
            volumeRule.OffsetConsecutive = new IntIndex2D(0, 1);
            volumeRule.Properties.Add(SimDataMappingVolumeMappingProperties.Name);
            volumeRule.Filter.Add(new SimDataMappingFilterVolume(SimDataMappingVolumeFilterProperties.Name, new Regex("Shoe.+")));
            componentRule.Rules.Add(volumeRule);

            var state = new SimTraversalState();
            var data = new SimMappedData();
            componentRule.Execute(comp1, state, data);

            Assert.AreEqual(1, data.Data.Count);
            Assert.IsTrue(data.Data.TryGetValue("SheetA", out var sheetData));

            Assert.AreEqual(3, sheetData.Count);
            AssertUtil.AssertContains(sheetData, new IntIndex2D(0, 0), "Volume1");
            AssertUtil.AssertContains(sheetData, new IntIndex2D(2, 0), "ShoeBox");
            AssertUtil.AssertContains(sheetData, new IntIndex2D(0, 1), "Volume2");

            Assert.AreEqual(2, state.ModelsToRelease.Count);
        }

        [TestMethod]
        public void FilterFileKey()
        {
            this.LoadProject(mappingProject);

            var comp1 = projectData.Components.First(x => x.Name == "Geometry");

            SimDataMappingRuleComponent componentRule = new SimDataMappingRuleComponent("SheetA");
            componentRule.MappingDirection = SimDataMappingDirection.Horizontal;
            componentRule.MaxMatches = int.MaxValue;
            componentRule.MaxDepth = int.MaxValue;
            componentRule.OffsetConsecutive = new IntIndex2D(0, 1);
            componentRule.Properties.Add(SimDataMappingComponentMappingProperties.Name);
            componentRule.Filter.Add(new SimDataMappingFilterComponent(SimDataMappingComponentFilterProperties.InstanceType, SimInstanceType.Entity3D));

            SimDataMappingRuleVolume volumeRule = new SimDataMappingRuleVolume("SheetA");
            volumeRule.MappingDirection = SimDataMappingDirection.Horizontal;
            volumeRule.MaxMatches = int.MaxValue;
            volumeRule.MaxDepth = int.MaxValue;
            volumeRule.OffsetParent = new IntIndex2D(2, 0);
            volumeRule.OffsetConsecutive = new IntIndex2D(0, 1);
            volumeRule.Properties.Add(SimDataMappingVolumeMappingProperties.Name);
            volumeRule.Filter.Add(new SimDataMappingFilterVolume(SimDataMappingVolumeFilterProperties.FileKey, 0));
            componentRule.Rules.Add(volumeRule);

            var state = new SimTraversalState();
            var data = new SimMappedData();
            componentRule.Execute(comp1, state, data);

            Assert.AreEqual(1, data.Data.Count);
            Assert.IsTrue(data.Data.TryGetValue("SheetA", out var sheetData));

            Assert.AreEqual(3, sheetData.Count);
            AssertUtil.AssertContains(sheetData, new IntIndex2D(0, 0), "Volume1");
            AssertUtil.AssertContains(sheetData, new IntIndex2D(2, 0), "ShoeBox");
            AssertUtil.AssertContains(sheetData, new IntIndex2D(0, 1), "Volume2");

            Assert.AreEqual(1, state.ModelsToRelease.Count); //Second model hasn't been loaded
        }

        #endregion

        [TestMethod]
        public void CloneTest()
        {
            SimDataMappingRuleVolume volumeRule = new SimDataMappingRuleVolume("Sheet A")
            {
                Name = "SomeName",
                MappingDirection = SimDataMappingDirection.Vertical,
                MaxDepth = 3,
                MaxMatches = 5,
                OffsetConsecutive = new IntIndex2D(7, 8),
                OffsetParent = new IntIndex2D(9, 10),
                ReferencePoint = SimDataMappingReferencePoint.TopRight,
            };

            volumeRule.Filter.Add(new SimDataMappingFilterVolume(SimDataMappingVolumeFilterProperties.FileKey, 33));
            volumeRule.Filter.Add(new SimDataMappingFilterVolume(SimDataMappingVolumeFilterProperties.Name, "asdf"));

            volumeRule.Properties.Add(SimDataMappingVolumeMappingProperties.Name);
            volumeRule.Properties.Add(SimDataMappingVolumeMappingProperties.FloorElevation);

            SimDataMappingRuleComponent childRule1 = new SimDataMappingRuleComponent("Sheet B");
            SimDataMappingRuleInstance childRule2 = new SimDataMappingRuleInstance("Sheet B");
            SimDataMappingRuleFace childRule3 = new SimDataMappingRuleFace("Sheet B");

            volumeRule.Rules.Add(childRule1);
            volumeRule.Rules.Add(childRule2);
            volumeRule.Rules.Add(childRule3);

            //Test
            var clonedRule = volumeRule.Clone();

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

            Assert.IsTrue(clonedRule.Rules[0] is SimDataMappingRuleComponent);
            Assert.IsTrue(clonedRule.Rules[1] is SimDataMappingRuleInstance);
            Assert.IsTrue(clonedRule.Rules[2] is SimDataMappingRuleFace);

            Assert.AreEqual(2, clonedRule.Filter.Count);
            Assert.AreEqual(SimDataMappingVolumeFilterProperties.FileKey, clonedRule.Filter[0].Property);
            Assert.AreEqual(33, clonedRule.Filter[0].Value);
            Assert.AreEqual(SimDataMappingVolumeFilterProperties.Name, clonedRule.Filter[1].Property);
            Assert.AreEqual("asdf", clonedRule.Filter[1].Value);

            Assert.AreEqual(2, clonedRule.Properties.Count);
            Assert.AreEqual(SimDataMappingVolumeMappingProperties.Name, clonedRule.Properties[0]);
            Assert.AreEqual(SimDataMappingVolumeMappingProperties.FloorElevation, clonedRule.Properties[1]);
        }
    }
}
