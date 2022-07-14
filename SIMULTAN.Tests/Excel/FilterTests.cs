using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data;
using SIMULTAN.Data.Components;
using SIMULTAN.Excel;
using SIMULTAN.Tests.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SIMULTAN.Tests.Excel
{
    [TestClass]
    public class FilterTests : BaseProjectTest
    {
        private static readonly FileInfo filterProject = new FileInfo(@".\ExcelMappingFilterTests.simultan");
        private static readonly FileInfo emptyProject = new FileInfo(@".\EmptyProject.simultan");

        #region Component Filter

        [TestMethod]
        public void ComponentCategoryFilter()
        {
            this.LoadProject(emptyProject);
            var comp = new SimComponent();
            this.projectData.Components.Add(comp);

            comp.Parameters.Add(new SimParameter("param", "unit", 1.0)
            {
                Category = SimCategory.Air
            });
            comp.Parameters.Add(new SimParameter("param", "unit", 1.0)
            {
                Category = SimCategory.Costs
            });

            Assert.AreEqual(SimCategory.Air | SimCategory.Costs, comp.Category);

            Assert.IsTrue(ExcelMappingNode.InstancePassesFilter(comp, new List<(string, object)> { ("Category", SimCategory.Air) }, null));
            Assert.IsTrue(ExcelMappingNode.InstancePassesFilter(comp, new List<(string, object)> { ("Category", SimCategory.Costs) }, null));
            Assert.IsFalse(ExcelMappingNode.InstancePassesFilter(comp, new List<(string, object)> { ("Category", SimCategory.Cooling) }, null));
            Assert.IsFalse(ExcelMappingNode.InstancePassesFilter(comp, new List<(string, object)> { ("Category", 1.0) }, null));
        }

        [TestMethod]
        public void ComponentCurrentSlotFilter()
        {
            this.LoadProject(emptyProject);
            var comp = new SimComponent();
            this.projectData.Components.Add(comp);

            comp.CurrentSlot = new SimSlotBase(SimDefaultSlots.Cost);

            Assert.IsTrue(ExcelMappingNode.InstancePassesFilter(comp, new List<(string, object)> { ("CurrentSlot", SimDefaultSlots.Cost) }, null));
            Assert.IsTrue(ExcelMappingNode.InstancePassesFilter(comp, new List<(string, object)> { ("CurrentSlot", SimDefaultSlots.Cost + "_0Ext3") }, null));
            Assert.IsFalse(ExcelMappingNode.InstancePassesFilter(comp, new List<(string, object)> { ("CurrentSlot", "test") }, null));

            var childComp = new SimComponent();
            childComp.CurrentSlot = new SimSlotBase(SimDefaultSlots.Joint);

            comp.Components.Add(new SimChildComponentEntry(new SimSlot(childComp.CurrentSlot, "Ext3"), childComp));

            Assert.IsTrue(ExcelMappingNode.InstancePassesFilter(childComp, new List<(string, object)> { ("CurrentSlot", SimDefaultSlots.Joint + "_0Ext3") }, null));
            Assert.IsTrue(ExcelMappingNode.InstancePassesFilter(childComp, new List<(string, object)> { ("CurrentSlot", SimDefaultSlots.Joint) }, null));
            Assert.IsFalse(ExcelMappingNode.InstancePassesFilter(childComp, new List<(string, object)> { ("CurrentSlot", "asdf") }, null));
        }

        [TestMethod]
        public void ComponentInstanceStateFilter()
        {
            this.LoadProject(filterProject);

            var node = this.projectData.Components.First(x => x.Name == "Node");

            // ToDo: find out how to set that
            Assert.IsTrue(ExcelMappingNode.InstancePassesFilter(node,
                new List<(string, object)> { ("InstanceState", new InstanceStateFilter(SimInstanceType.NetworkNode, false)) }, null));
            Assert.IsFalse(ExcelMappingNode.InstancePassesFilter(node,
                new List<(string, object)> { ("InstanceState", new InstanceStateFilter(SimInstanceType.Entity3D, false)) }, null));
            Assert.IsFalse(ExcelMappingNode.InstancePassesFilter(node,
                new List<(string, object)> { ("InstanceState", new InstanceStateFilter(SimInstanceType.NetworkNode, true)) }, null));
        }

        [TestMethod]
        public void ComponentIsBoundInNWFilter()
        {
            this.LoadProject(filterProject);

            var node = this.projectData.Components.First(x => x.Name == "Node");
            var node2 = this.projectData.Components.First(x => x.Name == "Node2");


            Assert.IsTrue(ExcelMappingNode.InstancePassesFilter(node, new List<(string, object)> { ("IsBoundInNetwork", true) }, null));
            Assert.IsFalse(ExcelMappingNode.InstancePassesFilter(node, new List<(string, object)> { ("IsBoundInNetwork", false) }, null));

            Assert.IsTrue(ExcelMappingNode.InstancePassesFilter(node2, new List<(string, object)> { ("IsBoundInNetwork", false) }, null));
            Assert.IsFalse(ExcelMappingNode.InstancePassesFilter(node2, new List<(string, object)> { ("IsBoundInNetwork", true) }, null));
        }

        [TestMethod]
        public void ComponentIsAutomaticallyGeneratedFilter()
        {
            this.LoadProject(emptyProject);

            var comp = new SimComponent();
            this.projectData.Components.Add(comp);
            Assert.IsTrue(ExcelMappingNode.InstancePassesFilter(comp, new List<(string, object)> { ("IsAutomaticallyGenerated", false) }, null));
            Assert.IsFalse(ExcelMappingNode.InstancePassesFilter(comp, new List<(string, object)> { ("IsAutomaticallyGenerated", true) }, null));

            var comp2 = new SimComponent()
            {
                IsAutomaticallyGenerated = true
            };
            this.projectData.Components.Add(comp2);
            Assert.IsTrue(ExcelMappingNode.InstancePassesFilter(comp2, new List<(string, object)> { ("IsAutomaticallyGenerated", true) }, null));
            Assert.IsFalse(ExcelMappingNode.InstancePassesFilter(comp2, new List<(string, object)> { ("IsAutomaticallyGenerated", false) }, null));
        }

        [TestMethod]
        public void ComponentNameFilter()
        {
            this.LoadProject(emptyProject);
            var comp = new SimComponent();
            this.projectData.Components.Add(comp);

            comp.Name = "This is a Name!";

            Assert.IsTrue(ExcelMappingNode.InstancePassesFilter(comp, new List<(string, object)> { ("Name", "This is a Name!") }, null));
            Assert.IsFalse(ExcelMappingNode.InstancePassesFilter(comp, new List<(string, object)> { ("Name", "This is not a Name!") }, null));
        }

        [TestMethod]
        public void ComponentDescriptionFilter()
        {
            this.LoadProject(emptyProject);
            var comp = new SimComponent();
            this.projectData.Components.Add(comp);

            comp.Description = "This is a Description!";

            Assert.IsTrue(ExcelMappingNode.InstancePassesFilter(comp, new List<(string, object)> { ("Description", "This is a Description!") }, null));
            Assert.IsFalse(ExcelMappingNode.InstancePassesFilter(comp, new List<(string, object)> { ("Description", "This is not a Description") }, null));
        }


        [TestMethod]
        public void ComponentLocalIDFilter()
        {
            this.LoadProject(emptyProject);
            var comp = new SimComponent();
            this.projectData.Components.Add(comp);

            comp.Id = new SimId(Guid.NewGuid(), 42L);

            Assert.IsTrue(ExcelMappingNode.InstancePassesFilter(comp, new List<(string, object)> { ("LocalID", 42L) }, null));
            Assert.IsFalse(ExcelMappingNode.InstancePassesFilter(comp, new List<(string, object)> { ("LocalID", 42) }, null));
            Assert.IsFalse(ExcelMappingNode.InstancePassesFilter(comp, new List<(string, object)> { ("LocalID", 0) }, null));
        }

        #endregion

        #region Parameter Filter

        [TestMethod]
        public void ParameterCategoryFilter()
        {
            this.LoadProject(emptyProject);
            var comp = new SimComponent();
            this.projectData.Components.Add(comp);

            var param = new SimParameter("Test", "m", 42);
            comp.Parameters.Add(param);
            param.Category = SimCategory.Acoustics;

            Assert.IsTrue(ExcelMappingNode.InstancePassesFilter(param, new List<(string, object)> { ("Category", SimCategory.Acoustics) }, null));
            Assert.IsFalse(ExcelMappingNode.InstancePassesFilter(param, new List<(string, object)> { ("Category", SimCategory.Air) }, null));
        }

        [TestMethod]
        public void ParameterDescriptionFilter()
        {
            this.LoadProject(emptyProject);
            var comp = new SimComponent();
            this.projectData.Components.Add(comp);

            var param = new SimParameter("Test", "m", 42);
            comp.Parameters.Add(param);

            param.Description = "This is a description";

            Assert.IsTrue(ExcelMappingNode.InstancePassesFilter(param, new List<(string, object)> { ("Description", "This is a description") }, null));
            Assert.IsTrue(ExcelMappingNode.InstancePassesFilter(param, new List<(string, object)> { ("Description", "") }, null));
            Assert.IsFalse(ExcelMappingNode.InstancePassesFilter(param, new List<(string, object)> { ("Description", "This is not a description") }, null));
        }

        [TestMethod]
        public void ParameterLocalIDFilter()
        {
            this.LoadProject(emptyProject);
            var comp = new SimComponent();
            this.projectData.Components.Add(comp);

            var param = new SimParameter("Test", "m", 42);
            comp.Parameters.Add(param);

            param.Id = new SimId(Guid.NewGuid(), 42L);

            Assert.IsTrue(ExcelMappingNode.InstancePassesFilter(param, new List<(string, object)> { ("LocalID", 42L) }, null));
            Assert.IsFalse(ExcelMappingNode.InstancePassesFilter(param, new List<(string, object)> { ("LocalID", 0L) }, null));
        }

        [TestMethod]
        public void ParameterValueMaxFilter()
        {
            this.LoadProject(emptyProject);
            var comp = new SimComponent();
            this.projectData.Components.Add(comp);

            var param = new SimParameter("Test", "m", 42);
            comp.Parameters.Add(param);

            param.ValueMax = 42.0;

            Assert.IsTrue(ExcelMappingNode.InstancePassesFilter(param, new List<(string, object)> { ("ValueMax", 42.0) }, null));
            Assert.IsFalse(ExcelMappingNode.InstancePassesFilter(param, new List<(string, object)> { ("ValueMax", 0.0) }, null));
        }

        [TestMethod]
        public void ParameterValueMinFilter()
        {
            this.LoadProject(emptyProject);
            var comp = new SimComponent();
            this.projectData.Components.Add(comp);

            var param = new SimParameter("Test", "m", 42);
            comp.Parameters.Add(param);

            param.ValueMin = 42.0;

            Assert.IsTrue(ExcelMappingNode.InstancePassesFilter(param, new List<(string, object)> { ("ValueMin", 42.0) }, null));
            Assert.IsFalse(ExcelMappingNode.InstancePassesFilter(param, new List<(string, object)> { ("ValueMin", 0.0) }, null));
        }

        [TestMethod]
        public void ParameterNameFilter()
        {
            this.LoadProject(emptyProject);
            var comp = new SimComponent();
            this.projectData.Components.Add(comp);

            var param = new SimParameter("Test", "m", 42);
            comp.Parameters.Add(param);

            Assert.IsTrue(ExcelMappingNode.InstancePassesFilter(param, new List<(string, object)> { ("Name", "Test") }, null));
            Assert.IsTrue(ExcelMappingNode.InstancePassesFilter(param, new List<(string, object)> { ("Name", "") }, null));
            Assert.IsFalse(ExcelMappingNode.InstancePassesFilter(param, new List<(string, object)> { ("Name", "Noname") }, null));
        }

        [TestMethod]
        public void ParameterPropagationFilter()
        {
            this.LoadProject(emptyProject);
            var comp = new SimComponent();
            this.projectData.Components.Add(comp);

            var param = new SimParameter("Test", "m", 42);
            comp.Parameters.Add(param);

            param.Propagation = SimInfoFlow.Mixed;

            Assert.IsTrue(ExcelMappingNode.InstancePassesFilter(param, new List<(string, object)> { ("Propagation", SimInfoFlow.Mixed) }, null));
            Assert.IsFalse(ExcelMappingNode.InstancePassesFilter(param, new List<(string, object)> { ("Propagation", SimInfoFlow.Automatic) }, null));
        }

        [TestMethod]
        public void ParameterTextValueFilter()
        {
            this.LoadProject(emptyProject);
            var comp = new SimComponent();
            this.projectData.Components.Add(comp);

            var param = new SimParameter("Test", "m", 42);
            comp.Parameters.Add(param);

            param.TextValue = "This is a Value";

            Assert.IsTrue(ExcelMappingNode.InstancePassesFilter(param, new List<(string, object)> { ("TextValue", "This is a Value") }, null));
            Assert.IsFalse(ExcelMappingNode.InstancePassesFilter(param, new List<(string, object)> { ("TextValue", "This is not a Value") }, null));
        }

        [TestMethod]
        public void ParameterUnitFilter()
        {
            this.LoadProject(emptyProject);
            var comp = new SimComponent();
            this.projectData.Components.Add(comp);

            var param = new SimParameter("Test", "m", 42);
            comp.Parameters.Add(param);

            Assert.IsTrue(ExcelMappingNode.InstancePassesFilter(param, new List<(string, object)> { ("Unit", "m") }, null));
            Assert.IsTrue(ExcelMappingNode.InstancePassesFilter(param, new List<(string, object)> { ("Unit", "") }, null));
            Assert.IsFalse(ExcelMappingNode.InstancePassesFilter(param, new List<(string, object)> { ("Unit", "w") }, null));
        }

        [TestMethod]
        public void ParameterValueCurrentFilter()
        {
            this.LoadProject(emptyProject);
            var comp = new SimComponent();
            this.projectData.Components.Add(comp);

            var param = new SimParameter("Test", "m", 42);
            comp.Parameters.Add(param);


            Assert.IsTrue(ExcelMappingNode.InstancePassesFilter(param, new List<(string, object)> { ("ValueCurrent", 42.0) }, null));
            Assert.IsFalse(ExcelMappingNode.InstancePassesFilter(param, new List<(string, object)> { ("ValueCurrent", 0.0) }, null));
        }
        #endregion

        #region Instance Filter

        [TestMethod]
        public void InstanceNameFilter()
        {
            this.LoadProject(filterProject);

            var node = this.projectData.Components.FirstOrDefault(x => x.Name == "Node");
            var instance = node.Instances.FirstOrDefault();

            Assert.IsTrue(ExcelMappingNode.InstancePassesFilter(instance, new List<(string, object)> { ("Name", "Geometry") }, null));
            Assert.IsFalse(ExcelMappingNode.InstancePassesFilter(instance, new List<(string, object)> { ("Name", "asdf") }, null));
        }

        #endregion
    }
}
