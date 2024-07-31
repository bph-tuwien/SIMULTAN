using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data.Components;
using SIMULTAN.Data.FlowNetworks;
using SIMULTAN.Data.SimMath;
using SIMULTAN.Exceptions;
using SIMULTAN.Tests.TestUtils;
using System;
using System.IO;
using System.Linq;



namespace SIMULTAN.Tests.Instances
{
    [TestClass]
    public class InstanceTests : BaseProjectTest
    {
        private static readonly FileInfo instanceProject = new FileInfo(@"./InstanceTestsProject.simultan");
        private static readonly FileInfo emptyProject = new FileInfo(@"./EmptyProject.simultan");

        #region General Tests

        [TestMethod]
        public void Ctor()
        {
            Assert.ThrowsException<ArgumentNullException>(() => { new SimComponentInstance((SimFlowNetworkElement)null); });
        }

        [TestMethod]
        public void PropertyChanged()
        {
            LoadProject(instanceProject);

            var nodeComponent = projectData.Components.First(x => x.Name == "Node");
            var network = projectData.NetworkManager.NetworkRecord.First(x => x.Name == "Network");
            var node = network.ContainedNodes.Values.First(x => x.Name == "Node 912f497f-2a73-4798-85d6-bdd365da555f: 2");

            var instance = new SimComponentInstance(node);
            nodeComponent.Instances.Add(instance);

            PropertyChangedEventCounter ec = new PropertyChangedEventCounter(instance);
            instance.InstanceSize = new SimInstanceSize(new SimVector3D(1.0, 2.0, 3.0), new SimVector3D(4.0, 5.0, 6.0));

            ec.AssertEventCount(1);
            Assert.AreEqual(nameof(SimComponentInstance.InstanceSize), ec.PropertyChangedArgs[0]);
        }

        #endregion

        #region Parameter Tests

        [TestMethod]
        public void InstanceAdded()
        {
            LoadProject(emptyProject);

            var component = new SimComponent();
            projectData.Components.Add(component);
            var param1 = new SimDoubleParameter("param1", "m", 1.8)
            {
                Propagation = SimInfoFlow.Input
            };
            component.Parameters.Add(param1);

            var param2 = new SimDoubleParameter("param2", "m", 2.8)
            {
                Propagation = SimInfoFlow.Automatic
            };
            component.Parameters.Add(param2);

            var instance = new SimComponentInstance();
            component.Instances.Add(instance);

            Assert.AreEqual(2, instance.InstanceParameterValuesPersistent.Count);
            Assert.IsTrue(instance.InstanceParameterValuesPersistent.Contains(param1));
            Assert.AreEqual(1.8, instance.InstanceParameterValuesPersistent[param1]);
            Assert.IsTrue(instance.InstanceParameterValuesPersistent.Contains(param2));
            Assert.AreEqual(2.8, instance.InstanceParameterValuesPersistent[param2]);

            Assert.AreEqual(2, instance.InstanceParameterValuesTemporary.Count);
            Assert.IsTrue(instance.InstanceParameterValuesTemporary.Contains(param1));
            Assert.AreEqual(1.8, instance.InstanceParameterValuesTemporary[param1]);
            Assert.IsTrue(instance.InstanceParameterValuesTemporary.Contains(param2));
            Assert.AreEqual(2.8, instance.InstanceParameterValuesTemporary[param2]);
        }

        [TestMethod]
        public void ParameterAdded()
        {
            LoadProject(emptyProject);

            var component = new SimComponent();
            projectData.Components.Add(component);
            var param1 = new SimDoubleParameter("param1", "m", 1.8)
            {
                Propagation = SimInfoFlow.Input
            };
            component.Parameters.Add(param1);

            var instance = new SimComponentInstance();
            component.Instances.Add(instance);

            Assert.AreEqual(1, instance.InstanceParameterValuesPersistent.Count);
            Assert.AreEqual(1, instance.InstanceParameterValuesTemporary.Count);

            //Add additional parameter
            var param2 = new SimDoubleParameter("param2", "m", 2.8)
            {
                Propagation = SimInfoFlow.Automatic
            };
            component.Parameters.Add(param2);

            Assert.AreEqual(2, instance.InstanceParameterValuesPersistent.Count);
            Assert.IsTrue(instance.InstanceParameterValuesPersistent.Contains(param1));
            Assert.AreEqual(1.8, instance.InstanceParameterValuesPersistent[param1]);
            Assert.IsTrue(instance.InstanceParameterValuesPersistent.Contains(param2));
            Assert.AreEqual(2.8, instance.InstanceParameterValuesPersistent[param2]);

            Assert.AreEqual(2, instance.InstanceParameterValuesTemporary.Count);
            Assert.IsTrue(instance.InstanceParameterValuesTemporary.Contains(param1));
            Assert.AreEqual(1.8, instance.InstanceParameterValuesTemporary[param1]);
            Assert.IsTrue(instance.InstanceParameterValuesTemporary.Contains(param2));
            Assert.AreEqual(2.8, instance.InstanceParameterValuesTemporary[param2]);
        }

        [TestMethod]
        public void ParameterRemoved()
        {
            LoadProject(emptyProject);

            var component = new SimComponent();
            projectData.Components.Add(component);
            var param1 = new SimDoubleParameter("param1", "m", 1.8)
            {
                Propagation = SimInfoFlow.Input
            };
            component.Parameters.Add(param1);
            var param2 = new SimDoubleParameter("param2", "m", 2.8)
            {
                Propagation = SimInfoFlow.Automatic
            };
            component.Parameters.Add(param2);

            var instance = new SimComponentInstance();
            component.Instances.Add(instance);

            Assert.AreEqual(2, instance.InstanceParameterValuesPersistent.Count);
            Assert.AreEqual(2, instance.InstanceParameterValuesTemporary.Count);

            //Remove parameter
            component.Parameters.Remove(param1);

            Assert.AreEqual(1, instance.InstanceParameterValuesPersistent.Count);
            Assert.IsTrue(instance.InstanceParameterValuesPersistent.Contains(param2));
            Assert.AreEqual(2.8, instance.InstanceParameterValuesPersistent[param2]);

            Assert.AreEqual(1, instance.InstanceParameterValuesTemporary.Count);
            Assert.IsTrue(instance.InstanceParameterValuesTemporary.Contains(param2));
            Assert.AreEqual(2.8, instance.InstanceParameterValuesTemporary[param2]);
        }

        [TestMethod]
        public void ParameterChanged()
        {
            LoadProject(emptyProject);

            var component = new SimComponent();

            projectData.Components.Add(component);
            var param1 = new SimDoubleParameter("param1", "m", 1.8)
            {
                Propagation = SimInfoFlow.Input
            };
            component.Parameters.Add(param1);
            var param2 = new SimDoubleParameter("param2", "m", 2.8)
            {
                Propagation = SimInfoFlow.Automatic
            };
            component.Parameters.Add(param2);

            var instance = new SimComponentInstance();
            Assert.AreEqual(true, instance.PropagateParameterChanges);
            instance.PropagateParameterChanges = false;
            component.Instances.Add(instance);

            Assert.AreEqual(false, instance.PropagateParameterChanges);
            Assert.AreEqual(1.8, instance.InstanceParameterValuesPersistent[param1]);
            Assert.AreEqual(2.8, instance.InstanceParameterValuesPersistent[param2]);

            //Change value, no propagation enabled
            param1.Value = 99.9;
            Assert.AreEqual(1.8, instance.InstanceParameterValuesPersistent[param1]);
            Assert.AreEqual(2.8, instance.InstanceParameterValuesPersistent[param2]);

            //Enable propagation
            component.Instances.ToList().ForEach(x => x.PropagateParameterChanges = true);
            Assert.AreEqual(true, instance.PropagateParameterChanges);
            Assert.AreEqual(99.9, instance.InstanceParameterValuesPersistent[param1]);
            Assert.AreEqual(2.8, instance.InstanceParameterValuesPersistent[param2]);

            //Change value with propagation
            param1.Value = 55.8;
            Assert.AreEqual(55.8, instance.InstanceParameterValuesPersistent[param1]);
            Assert.AreEqual(2.8, instance.InstanceParameterValuesPersistent[param2]);

            //Disable propagation
            component.Instances.ToList().ForEach(x => x.PropagateParameterChanges = false);
            Assert.AreEqual(false, instance.PropagateParameterChanges);
            Assert.AreEqual(55.8, instance.InstanceParameterValuesPersistent[param1]);
            Assert.AreEqual(2.8, instance.InstanceParameterValuesPersistent[param2]);

            //Change parameter, again without propagation
            param2.Value = 18.9;
            Assert.AreEqual(55.8, instance.InstanceParameterValuesPersistent[param1]);
            Assert.AreEqual(2.8, instance.InstanceParameterValuesPersistent[param2]);
        }

        [TestMethod]
        public void ParameterChanged_PropagateAlways()
        {
            LoadProject(emptyProject);

            var component = new SimComponent();

            projectData.Components.Add(component);
            var param1 = new SimDoubleParameter("param1", "m", 1.8)
            {
                Propagation = SimInfoFlow.Input,
                InstancePropagationMode = SimParameterInstancePropagation.PropagateAlways
            };
            component.Parameters.Add(param1);
            var param2 = new SimDoubleParameter("param2", "m", 2.8)
            {
                Propagation = SimInfoFlow.Automatic,
                InstancePropagationMode = SimParameterInstancePropagation.PropagateAlways
            };
            component.Parameters.Add(param2);

            var instance = new SimComponentInstance();
            Assert.AreEqual(true, instance.PropagateParameterChanges);
            instance.PropagateParameterChanges = false;
            component.Instances.Add(instance);

            Assert.AreEqual(false, instance.PropagateParameterChanges);
            Assert.AreEqual(1.8, instance.InstanceParameterValuesPersistent[param1]);
            Assert.AreEqual(2.8, instance.InstanceParameterValuesPersistent[param2]);

            //Change value, no propagation enabled
            param1.Value = 99.9;
            Assert.AreEqual(99.9, instance.InstanceParameterValuesPersistent[param1]);
            Assert.AreEqual(2.8, instance.InstanceParameterValuesPersistent[param2]);

            //Enable propagation
            component.Instances.ToList().ForEach(x => x.PropagateParameterChanges = true);
            Assert.AreEqual(true, instance.PropagateParameterChanges);
            Assert.AreEqual(99.9, instance.InstanceParameterValuesPersistent[param1]);
            Assert.AreEqual(2.8, instance.InstanceParameterValuesPersistent[param2]);

            //Change value with propagation
            param1.Value = 55.8;
            Assert.AreEqual(55.8, instance.InstanceParameterValuesPersistent[param1]);
            Assert.AreEqual(2.8, instance.InstanceParameterValuesPersistent[param2]);

            //Disable propagation
            component.Instances.ToList().ForEach(x => x.PropagateParameterChanges = false);
            Assert.AreEqual(false, instance.PropagateParameterChanges);
            Assert.AreEqual(55.8, instance.InstanceParameterValuesPersistent[param1]);
            Assert.AreEqual(2.8, instance.InstanceParameterValuesPersistent[param2]);

            //Change parameter, again without propagation
            param2.Value = 18.9;
            Assert.AreEqual(55.8, instance.InstanceParameterValuesPersistent[param1]);
            Assert.AreEqual(18.9, instance.InstanceParameterValuesPersistent[param2]);
        }

        [TestMethod]
        public void ParameterChanged_PropagateIfInstance()
        {
            LoadProject(emptyProject);

            var component = new SimComponent();

            projectData.Components.Add(component);
            var param1 = new SimDoubleParameter("param1", "m", 1.8)
            {
                Propagation = SimInfoFlow.Input,
                InstancePropagationMode = SimParameterInstancePropagation.PropagateIfInstance
            };
            component.Parameters.Add(param1);
            var param2 = new SimDoubleParameter("param2", "m", 2.8)
            {
                Propagation = SimInfoFlow.Automatic,
                InstancePropagationMode = SimParameterInstancePropagation.PropagateIfInstance
            };
            component.Parameters.Add(param2);

            var instance = new SimComponentInstance();
            Assert.AreEqual(true, instance.PropagateParameterChanges);
            instance.PropagateParameterChanges = false;
            component.Instances.Add(instance);

            Assert.AreEqual(false, instance.PropagateParameterChanges);
            Assert.AreEqual(1.8, instance.InstanceParameterValuesPersistent[param1]);
            Assert.AreEqual(2.8, instance.InstanceParameterValuesPersistent[param2]);

            //Change value, no propagation enabled
            param1.Value = 99.9;
            Assert.AreEqual(1.8, instance.InstanceParameterValuesPersistent[param1]);
            Assert.AreEqual(2.8, instance.InstanceParameterValuesPersistent[param2]);

            //Enable propagation
            component.Instances.ToList().ForEach(x => x.PropagateParameterChanges = true);
            Assert.AreEqual(true, instance.PropagateParameterChanges);
            Assert.AreEqual(99.9, instance.InstanceParameterValuesPersistent[param1]);
            Assert.AreEqual(2.8, instance.InstanceParameterValuesPersistent[param2]);

            //Change value with propagation
            param1.Value = 55.8;
            Assert.AreEqual(55.8, instance.InstanceParameterValuesPersistent[param1]);
            Assert.AreEqual(2.8, instance.InstanceParameterValuesPersistent[param2]);

            //Disable propagation
            component.Instances.ToList().ForEach(x => x.PropagateParameterChanges = false);
            Assert.AreEqual(false, instance.PropagateParameterChanges);
            Assert.AreEqual(55.8, instance.InstanceParameterValuesPersistent[param1]);
            Assert.AreEqual(2.8, instance.InstanceParameterValuesPersistent[param2]);

            //Change parameter, again without propagation
            param2.Value = 18.9;
            Assert.AreEqual(55.8, instance.InstanceParameterValuesPersistent[param1]);
            Assert.AreEqual(2.8, instance.InstanceParameterValuesPersistent[param2]);
        }

        [TestMethod]
        public void ParameterChanged_PropagateNever()
        {
            LoadProject(emptyProject);

            var component = new SimComponent();

            projectData.Components.Add(component);
            var param1 = new SimDoubleParameter("param1", "m", 1.8)
            {
                Propagation = SimInfoFlow.Input,
                InstancePropagationMode = SimParameterInstancePropagation.PropagateNever
            };
            component.Parameters.Add(param1);
            var param2 = new SimDoubleParameter("param2", "m", 2.8)
            {
                Propagation = SimInfoFlow.Automatic,
                InstancePropagationMode = SimParameterInstancePropagation.PropagateNever
            };
            component.Parameters.Add(param2);

            var instance = new SimComponentInstance();
            Assert.AreEqual(true, instance.PropagateParameterChanges);
            instance.PropagateParameterChanges = false;
            component.Instances.Add(instance);

            component.Instances.ToList().ForEach(x => x.PropagateParameterChanges = false);
            Assert.AreEqual(false, instance.PropagateParameterChanges);
            Assert.AreEqual(1.8, instance.InstanceParameterValuesPersistent[param1]);
            Assert.AreEqual(2.8, instance.InstanceParameterValuesPersistent[param2]);

            //Change value, no propagation enabled
            param1.Value = 99.9;
            Assert.AreEqual(1.8, instance.InstanceParameterValuesPersistent[param1]);
            Assert.AreEqual(2.8, instance.InstanceParameterValuesPersistent[param2]);

            //Enable propagation
            component.Instances.ToList().ForEach(x => x.PropagateParameterChanges = true);
            Assert.AreEqual(true, instance.PropagateParameterChanges);
            Assert.AreEqual(1.8, instance.InstanceParameterValuesPersistent[param1]);
            Assert.AreEqual(2.8, instance.InstanceParameterValuesPersistent[param2]);

            //Change value with propagation
            param1.Value = 55.8;
            Assert.AreEqual(1.8, instance.InstanceParameterValuesPersistent[param1]);
            Assert.AreEqual(2.8, instance.InstanceParameterValuesPersistent[param2]);

            //Disable propagation
            component.Instances.ToList().ForEach(x => x.PropagateParameterChanges = false);
            Assert.AreEqual(false, instance.PropagateParameterChanges);
            Assert.AreEqual(1.8, instance.InstanceParameterValuesPersistent[param1]);
            Assert.AreEqual(2.8, instance.InstanceParameterValuesPersistent[param2]);

            //Change parameter, again without propagation
            param2.Value = 18.9;
            Assert.AreEqual(1.8, instance.InstanceParameterValuesPersistent[param1]);
            Assert.AreEqual(2.8, instance.InstanceParameterValuesPersistent[param2]);
        }

        #endregion

        #region InstanceSize

        [TestMethod]
        public void SetSize()
        {
            LoadProject(instanceProject);

            var nodeComponent = projectData.Components.First(x => x.Name == "Node2");
            var instance = nodeComponent.Instances.FirstOrDefault();

            instance.SetSize(new SimInstanceSize(new SimVector3D(11, 12, 13), new SimVector3D(14, 15, 16)), instance.SizeTransfer);

            Assert.AreEqual(11.0, instance.InstanceSize.Min.X);
            Assert.AreEqual(12.0, instance.InstanceSize.Min.Y);
            Assert.AreEqual(13.0, instance.InstanceSize.Min.Z);

            Assert.AreEqual(14.0, instance.InstanceSize.Max.X);
            Assert.AreEqual(15.0, instance.InstanceSize.Max.Y);
            Assert.AreEqual(16.0, instance.InstanceSize.Max.Z);
        }

        [TestMethod]
        public void SetSizeEvents()
        {
            LoadProject(instanceProject);

            var nodeComponent = projectData.Components.First(x => x.Name == "Node2");
            var instance = nodeComponent.Instances.FirstOrDefault();

            PropertyChangedEventCounter cc = new PropertyChangedEventCounter(instance);

            instance.SetSize(new SimInstanceSize(new SimVector3D(11, 12, 13), new SimVector3D(14, 15, 16)), instance.SizeTransfer);

            cc.AssertEventCount(1);
            Assert.AreEqual("InstanceSize", cc.PropertyChangedArgs[0]);
        }

        #endregion

        #region InstanceSize with Transfer

        [TestMethod]
        public void TransferableSizeParameter()
        {
            LoadProject(instanceProject);

            var nodeComponent = projectData.Components.First(x => x.Name == "Node2");
            var parameter = nodeComponent.Parameters.FirstOrDefault(x => x is SimDoubleParameter && x.NameTaxonomyEntry.Text == "A") as SimDoubleParameter;
            var instance = nodeComponent.Instances.FirstOrDefault();

            PropertyChangedEventCounter pc = new PropertyChangedEventCounter(instance);

            var sizeTransfer = new SimInstanceSizeTransferDefinition();
            sizeTransfer[SimInstanceSizeIndex.MinZ] = new SimInstanceSizeTransferDefinitionItem(SimInstanceSizeTransferSource.Parameter, parameter, 13.0);

            instance.SizeTransfer = sizeTransfer;

            Assert.AreEqual(25.0, instance.InstanceSize.Min.Z);
            pc.AssertEventCount(2);
            Assert.AreEqual(nameof(SimComponentInstance.InstanceSize), pc.PropertyChangedArgs[0]);
            Assert.AreEqual(nameof(SimComponentInstance.SizeTransfer), pc.PropertyChangedArgs[1]);

            sizeTransfer[SimInstanceSizeIndex.MinZ] = new SimInstanceSizeTransferDefinitionItem(SimInstanceSizeTransferSource.User, null, 5.0);
        }

        [TestMethod]
        public void TransferableSizePath()
        {
            LoadProject(instanceProject);

            var nodeComponent = projectData.Components.First(x => x.Name == "Node2");
            var parameter = nodeComponent.Parameters.FirstOrDefault(x => x is SimDoubleParameter && x.NameTaxonomyEntry.Text == "A") as SimDoubleParameter;
            var instance = nodeComponent.Instances.FirstOrDefault();

            PropertyChangedEventCounter pc = new PropertyChangedEventCounter(instance);

            var sizeTransfer = new SimInstanceSizeTransferDefinition();
            sizeTransfer[SimInstanceSizeIndex.MinZ] = new SimInstanceSizeTransferDefinitionItem(SimInstanceSizeTransferSource.Path, parameter, 13.0);

            instance.SizeTransfer = sizeTransfer;

            Assert.AreEqual(0.0, instance.InstanceSize.Min.Z);
            pc.AssertEventCount(2);
            Assert.AreEqual(nameof(SimComponentInstance.InstanceSize), pc.PropertyChangedArgs[0]);
            Assert.AreEqual(nameof(SimComponentInstance.SizeTransfer), pc.PropertyChangedArgs[1]);

            sizeTransfer[SimInstanceSizeIndex.MinZ] = new SimInstanceSizeTransferDefinitionItem(SimInstanceSizeTransferSource.User, null, 5.0);
        }

        #endregion
    }
}
