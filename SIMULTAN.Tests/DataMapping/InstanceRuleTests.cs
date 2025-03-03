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
    public class InstanceRuleTests : BaseProjectTest
    {
        private static readonly FileInfo mappingProject = new FileInfo(@"./ExcelMappingTests.simultan");

        #region Properties

        [TestMethod]
        public void MapName()
        {
            this.LoadProject(mappingProject);

            var comp1 = projectData.Components.First(x => x.Name == "Component1");

            SimDataMappingRuleComponent compRule = new SimDataMappingRuleComponent("SheetA");
            compRule.MaxMatches = int.MaxValue;
            compRule.MaxDepth = int.MaxValue;
            compRule.TraversalStrategy = SimDataMappingRuleTraversalStrategy.Subtree;
            compRule.OffsetParent = new RowColumnIndex(0, 0);
            compRule.OffsetConsecutive = new RowColumnIndex(1, 0);
            compRule.Properties.Add(SimDataMappingComponentMappingProperties.Name);
            compRule.Filter.Add(new SimDataMappingFilterComponent(SimDataMappingComponentFilterProperties.InstanceIsRealized, true));

            SimDataMappingRuleInstance rule = new SimDataMappingRuleInstance("SheetA");
            rule.MaxMatches = int.MaxValue;
            rule.MaxDepth = int.MaxValue;
            rule.OffsetParent = new RowColumnIndex(0, 1);
            rule.OffsetConsecutive = new RowColumnIndex(0, 1);
            rule.MappingDirection = SimDataMappingDirection.Horizontal;
            rule.ReferencePointParent = SimDataMappingReferencePoint.TopRight;
            rule.Properties.Add(SimDataMappingInstanceMappingProperties.Name);

            compRule.Rules.Add(rule);

            var state = new SimTraversalState();
            var data = new SimMappedData();
            compRule.Execute(comp1, state, data);

            Assert.AreEqual(1, data.Data.Count);
            Assert.IsTrue(data.Data.TryGetValue("SheetA", out var sheetData));

            Assert.AreEqual(7, sheetData.Count);

            AssertUtil.AssertContains(sheetData, new RowColumnIndex(0, 0), "ChildComponent1");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(0, 1), "Geometry Placement 0:13");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(0, 2), "Geometry Placement 0:20");

            AssertUtil.AssertContains(sheetData, new RowColumnIndex(1, 0), "ChildComponent2");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(1, 1), "Geometry Placement 0:1");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(1, 2), "Geometry Placement 0:14");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(1, 3), "Geometry Placement 0:16");

        }

        [TestMethod]
        public void MapId()
        {
            this.LoadProject(mappingProject);

            var comp1 = projectData.Components.First(x => x.Name == "Component1");

            SimDataMappingRuleComponent compRule = new SimDataMappingRuleComponent("SheetA");
            compRule.MaxMatches = int.MaxValue;
            compRule.MaxDepth = int.MaxValue;
            compRule.TraversalStrategy = SimDataMappingRuleTraversalStrategy.Subtree;
            compRule.OffsetParent = new RowColumnIndex(0, 0);
            compRule.OffsetConsecutive = new RowColumnIndex(1, 0);
            compRule.Properties.Add(SimDataMappingComponentMappingProperties.Name);
            compRule.Filter.Add(new SimDataMappingFilterComponent(SimDataMappingComponentFilterProperties.InstanceIsRealized, true));

            SimDataMappingRuleInstance rule = new SimDataMappingRuleInstance("SheetA");
            rule.MaxMatches = int.MaxValue;
            rule.MaxDepth = int.MaxValue;
            rule.OffsetParent = new RowColumnIndex(0, 1);
            rule.OffsetConsecutive = new RowColumnIndex(0, 1);
            rule.MappingDirection = SimDataMappingDirection.Horizontal;
            rule.ReferencePointParent = SimDataMappingReferencePoint.TopRight;
            rule.Properties.Add(SimDataMappingInstanceMappingProperties.Id);

            compRule.Rules.Add(rule);

            var state = new SimTraversalState();
            var data = new SimMappedData();
            compRule.Execute(comp1, state, data);

            Assert.AreEqual(1, data.Data.Count);
            Assert.IsTrue(data.Data.TryGetValue("SheetA", out var sheetData));

            Assert.AreEqual(7, sheetData.Count);

            AssertUtil.AssertContains(sheetData, new RowColumnIndex(0, 0), "ChildComponent1");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(0, 1), 73L);
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(0, 2), 96L);

            AssertUtil.AssertContains(sheetData, new RowColumnIndex(1, 0), "ChildComponent2");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(1, 1), 76L);
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(1, 2), 97L);
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(1, 3), 98L);

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

            SimDataMappingRuleInstance rule = new SimDataMappingRuleInstance("SheetA");
            rule.MaxMatches = int.MaxValue;
            rule.MaxDepth = int.MaxValue;
            rule.OffsetParent = new RowColumnIndex(0, 1);
            rule.OffsetConsecutive = new RowColumnIndex(1, 0);
            rule.Properties.Add(SimDataMappingInstanceMappingProperties.Name);
            volumeRule.Rules.Add(rule);

            var state = new SimTraversalState();
            var data = new SimMappedData();
            compRule.Execute(comp1, state, data);

            Assert.AreEqual(1, data.Data.Count);
            Assert.IsTrue(data.Data.TryGetValue("SheetA", out var sheetData));

            Assert.AreEqual(3, sheetData.Count);

            AssertUtil.AssertContains(sheetData, new RowColumnIndex(0, 0), "Volume1");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(0, 1), "ShoeBox");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(0, 2), "Geometry Placement 0:56");

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

            SimDataMappingRuleInstance rule = new SimDataMappingRuleInstance("SheetA");
            rule.MaxMatches = int.MaxValue;
            rule.MaxDepth = int.MaxValue;
            rule.OffsetParent = new RowColumnIndex(0, 1);
            rule.OffsetConsecutive = new RowColumnIndex(1, 0);
            rule.Properties.Add(SimDataMappingInstanceMappingProperties.Name);
            faceRule.Rules.Add(rule);

            var state = new SimTraversalState();
            var data = new SimMappedData();
            compRule.Execute(comp1, state, data);

            Assert.AreEqual(1, data.Data.Count);
            Assert.IsTrue(data.Data.TryGetValue("SheetA", out var sheetData));

            Assert.AreEqual(3, sheetData.Count);

            AssertUtil.AssertContains(sheetData, new RowColumnIndex(0, 0), "Face1");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(0, 1), "Face 13");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(0, 2), "Geometry Placement 0:13");

        }

        #endregion

        #region Filter

        [TestMethod]
        public void FilterNameString()
        {
            this.LoadProject(mappingProject);

            var comp1 = projectData.Components.First(x => x.Name == "Component1");

            SimDataMappingRuleComponent compRule = new SimDataMappingRuleComponent("SheetA");
            compRule.MaxMatches = int.MaxValue;
            compRule.MaxDepth = int.MaxValue;
            compRule.TraversalStrategy = SimDataMappingRuleTraversalStrategy.Subtree;
            compRule.OffsetParent = new RowColumnIndex(0, 0);
            compRule.OffsetConsecutive = new RowColumnIndex(1, 0);
            compRule.Properties.Add(SimDataMappingComponentMappingProperties.Name);
            compRule.Filter.Add(new SimDataMappingFilterComponent(SimDataMappingComponentFilterProperties.InstanceIsRealized, true));

            SimDataMappingRuleInstance rule = new SimDataMappingRuleInstance("SheetA");
            rule.MaxMatches = int.MaxValue;
            rule.MaxDepth = int.MaxValue;
            rule.OffsetParent = new RowColumnIndex(0, 1);
            rule.OffsetConsecutive = new RowColumnIndex(0, 1);
            rule.MappingDirection = SimDataMappingDirection.Horizontal;
            rule.ReferencePointParent = SimDataMappingReferencePoint.TopRight;
            rule.Properties.Add(SimDataMappingInstanceMappingProperties.Name);
            rule.Filter.Add(new SimDataMappingFilterInstance(SimDataMappingInstanceFilterProperties.Name, "Geometry Placement 0:14"));

            compRule.Rules.Add(rule);

            var state = new SimTraversalState();
            var data = new SimMappedData();
            compRule.Execute(comp1, state, data);

            Assert.AreEqual(1, data.Data.Count);
            Assert.IsTrue(data.Data.TryGetValue("SheetA", out var sheetData));

            Assert.AreEqual(3, sheetData.Count);

            AssertUtil.AssertContains(sheetData, new RowColumnIndex(0, 0), "ChildComponent1");

            AssertUtil.AssertContains(sheetData, new RowColumnIndex(1, 0), "ChildComponent2");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(1, 1), "Geometry Placement 0:14");

        }

        [TestMethod]
        public void FilterNameRegex()
        {
            this.LoadProject(mappingProject);

            var comp1 = projectData.Components.First(x => x.Name == "Component1");

            SimDataMappingRuleComponent compRule = new SimDataMappingRuleComponent("SheetA");
            compRule.MaxMatches = int.MaxValue;
            compRule.MaxDepth = int.MaxValue;
            compRule.TraversalStrategy = SimDataMappingRuleTraversalStrategy.Subtree;
            compRule.OffsetParent = new RowColumnIndex(0, 0);
            compRule.OffsetConsecutive = new RowColumnIndex(1, 0);
            compRule.Properties.Add(SimDataMappingComponentMappingProperties.Name);
            compRule.Filter.Add(new SimDataMappingFilterComponent(SimDataMappingComponentFilterProperties.InstanceIsRealized, true));

            SimDataMappingRuleInstance rule = new SimDataMappingRuleInstance("SheetA");
            rule.MaxMatches = int.MaxValue;
            rule.MaxDepth = int.MaxValue;
            rule.OffsetParent = new RowColumnIndex(0, 1);
            rule.OffsetConsecutive = new RowColumnIndex(0, 1);
            rule.MappingDirection = SimDataMappingDirection.Horizontal;
            rule.ReferencePointParent = SimDataMappingReferencePoint.TopRight;
            rule.Properties.Add(SimDataMappingInstanceMappingProperties.Name);
            rule.Filter.Add(new SimDataMappingFilterInstance(SimDataMappingInstanceFilterProperties.Name,
                new Regex("Geometry Placement 0:(14|16)+")));

            compRule.Rules.Add(rule);

            var state = new SimTraversalState();
            var data = new SimMappedData();
            compRule.Execute(comp1, state, data);

            Assert.AreEqual(1, data.Data.Count);
            Assert.IsTrue(data.Data.TryGetValue("SheetA", out var sheetData));

            Assert.AreEqual(4, sheetData.Count);

            AssertUtil.AssertContains(sheetData, new RowColumnIndex(0, 0), "ChildComponent1");

            AssertUtil.AssertContains(sheetData, new RowColumnIndex(1, 0), "ChildComponent2");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(1, 1), "Geometry Placement 0:14");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(1, 2), "Geometry Placement 0:16");

        }

        [TestMethod]
        public void FilterInstanceType()
        {
            this.LoadProject(mappingProject);

            var comp1 = projectData.Components.First(x => x.Name == "Component1");

            SimDataMappingRuleComponent compRule = new SimDataMappingRuleComponent("SheetA");
            compRule.MaxMatches = int.MaxValue;
            compRule.MaxDepth = int.MaxValue;
            compRule.TraversalStrategy = SimDataMappingRuleTraversalStrategy.Subtree;
            compRule.OffsetParent = new RowColumnIndex(0, 0);
            compRule.OffsetConsecutive = new RowColumnIndex(1, 0);
            compRule.Properties.Add(SimDataMappingComponentMappingProperties.Name);
            compRule.Filter.Add(new SimDataMappingFilterComponent(SimDataMappingComponentFilterProperties.InstanceIsRealized, true));

            SimDataMappingRuleInstance rule = new SimDataMappingRuleInstance("SheetA");
            rule.MaxMatches = int.MaxValue;
            rule.MaxDepth = int.MaxValue;
            rule.OffsetParent = new RowColumnIndex(0, 1);
            rule.OffsetConsecutive = new RowColumnIndex(0, 1);
            rule.MappingDirection = SimDataMappingDirection.Horizontal;
            rule.ReferencePointParent = SimDataMappingReferencePoint.TopRight;
            rule.Properties.Add(SimDataMappingInstanceMappingProperties.Name);
            rule.Filter.Add(new SimDataMappingFilterInstance(SimDataMappingInstanceFilterProperties.InstanceType, SimInstanceType.AttributesPoint));

            compRule.Rules.Add(rule);

            var state = new SimTraversalState();
            var data = new SimMappedData();
            compRule.Execute(comp1, state, data);

            Assert.AreEqual(1, data.Data.Count);
            Assert.IsTrue(data.Data.TryGetValue("SheetA", out var sheetData));

            Assert.AreEqual(5, sheetData.Count);

            AssertUtil.AssertContains(sheetData, new RowColumnIndex(0, 0), "ChildComponent1");

            AssertUtil.AssertContains(sheetData, new RowColumnIndex(1, 0), "ChildComponent2");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(1, 1), "Geometry Placement 0:1");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(1, 2), "Geometry Placement 0:14");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(1, 3), "Geometry Placement 0:16");

        }

        #endregion

        [TestMethod]
        public void MaxMatches()
        {
            this.LoadProject(mappingProject);

            var comp1 = projectData.Components.First(x => x.Name == "Component1");

            SimDataMappingRuleComponent compRule = new SimDataMappingRuleComponent("SheetA");
            compRule.MaxMatches = int.MaxValue;
            compRule.MaxDepth = int.MaxValue;
            compRule.TraversalStrategy = SimDataMappingRuleTraversalStrategy.Subtree;
            compRule.OffsetParent = new RowColumnIndex(0, 0);
            compRule.OffsetConsecutive = new RowColumnIndex(1, 0);
            compRule.Properties.Add(SimDataMappingComponentMappingProperties.Name);
            compRule.Filter.Add(new SimDataMappingFilterComponent(SimDataMappingComponentFilterProperties.InstanceIsRealized, true));

            SimDataMappingRuleInstance rule = new SimDataMappingRuleInstance("SheetA");
            rule.MaxMatches = 2;
            rule.MaxDepth = int.MaxValue;
            rule.OffsetParent = new RowColumnIndex(0, 1);
            rule.OffsetConsecutive = new RowColumnIndex(0, 1);
            rule.MappingDirection = SimDataMappingDirection.Horizontal;
            rule.ReferencePointParent = SimDataMappingReferencePoint.TopRight;
            rule.Properties.Add(SimDataMappingInstanceMappingProperties.Name);

            compRule.Rules.Add(rule);

            var state = new SimTraversalState();
            var data = new SimMappedData();
            compRule.Execute(comp1, state, data);

            Assert.AreEqual(1, data.Data.Count);
            Assert.IsTrue(data.Data.TryGetValue("SheetA", out var sheetData));

            Assert.AreEqual(7, sheetData.Count);

            AssertUtil.AssertContains(sheetData, new RowColumnIndex(0, 0), "ChildComponent1");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(0, 1), "Geometry Placement 0:13");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(0, 2), "Geometry Placement 0:20");

            AssertUtil.AssertContains(sheetData, new RowColumnIndex(1, 0), "ChildComponent2");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(1, 1), "Geometry Placement 0:1");
            AssertUtil.AssertContains(sheetData, new RowColumnIndex(1, 2), "Geometry Placement 0:14");

        }

        [TestMethod]
        public void CloneTest()
        {
            SimDataMappingRuleInstance instanceRule = new SimDataMappingRuleInstance("Sheet A")
            {
                Name = "SomeName",
                MappingDirection = SimDataMappingDirection.Vertical,
                MaxDepth = 3,
                MaxMatches = 5,
                OffsetConsecutive = new RowColumnIndex(8, 7),
                OffsetParent = new RowColumnIndex(10, 9),
                ReferencePointParent = SimDataMappingReferencePoint.TopRight,
            };

            instanceRule.Filter.Add(new SimDataMappingFilterInstance(SimDataMappingInstanceFilterProperties.Name, "asdf"));
            instanceRule.Filter.Add(new SimDataMappingFilterInstance(SimDataMappingInstanceFilterProperties.InstanceType, SimInstanceType.AttributesEdge));

            instanceRule.Properties.Add(SimDataMappingInstanceMappingProperties.Name);
            instanceRule.Properties.Add(SimDataMappingInstanceMappingProperties.Id);

            SimDataMappingRuleComponent childRule1 = new SimDataMappingRuleComponent("Sheet B");
            SimDataMappingRuleVolume childRule2 = new SimDataMappingRuleVolume("Sheet B");
            SimDataMappingRuleParameter childRule3 = new SimDataMappingRuleParameter("Sheet B");
            SimDataMappingRuleFace childRule4 = new SimDataMappingRuleFace("Sheet B");

            instanceRule.Rules.Add(childRule1);
            instanceRule.Rules.Add(childRule2);
            instanceRule.Rules.Add(childRule3);
            instanceRule.Rules.Add(childRule4);

            //Test
            var clonedRule = instanceRule.Clone();

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
            Assert.IsTrue(clonedRule.Rules[2] is SimDataMappingRuleParameter);
            Assert.IsTrue(clonedRule.Rules[3] is SimDataMappingRuleFace);

            Assert.AreEqual(2, clonedRule.Filter.Count);
            Assert.AreEqual(SimDataMappingInstanceFilterProperties.Name, clonedRule.Filter[0].Property);
            Assert.AreEqual("asdf", clonedRule.Filter[0].Value);
            Assert.AreEqual(SimDataMappingInstanceFilterProperties.InstanceType, clonedRule.Filter[1].Property);
            Assert.AreEqual(SimInstanceType.AttributesEdge, clonedRule.Filter[1].Value);

            Assert.AreEqual(2, clonedRule.Properties.Count);
            Assert.AreEqual(SimDataMappingInstanceMappingProperties.Name, clonedRule.Properties[0]);
            Assert.AreEqual(SimDataMappingInstanceMappingProperties.Id, clonedRule.Properties[1]);
        }
    }
}
