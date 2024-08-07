﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data.Components;
using SIMULTAN.Data.SimMath;
using SIMULTAN.Exceptions;
using SIMULTAN.Tests.TestUtils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace SIMULTAN.Tests.Components
{
    [TestClass]
    public class InstanceManagementTests : BaseProjectTest
    {
        private static readonly FileInfo testProject = new FileInfo(@"./GeometryInstanceTestsProject.simultan");
        private static readonly FileInfo accessProject = new FileInfo(@"./AccessTestsProject.simultan");
        private static readonly FileInfo emptyProject = new FileInfo(@"./EmptyProject.simultan");
        private static readonly FileInfo instanceProject = new FileInfo(@"./InstanceTestsProject.simultan");

        [TestMethod]
        public void AddInstance()
        {
            this.LoadProject(testProject);
            var component = new SimComponent();
            this.projectData.Components.Add(component);
            var lastWrite = component.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write);
            Thread.Sleep(5);

            //Add null
            Assert.ThrowsException<ArgumentNullException>(() => { component.Instances.Add(null); });

            //Add valid data
            var instance = new SimComponentInstance();
            Assert.AreEqual(null, instance.Component);

            component.Instances.Add(instance);
            var write = component.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write);

            Assert.AreEqual(1, component.Instances.Count);
            Assert.AreEqual(component, instance.Component);
            Assert.AreEqual(this.projectData.Components, instance.Factory);
            Assert.AreNotEqual(0, instance.Id.LocalId);
            Assert.IsTrue(write.lastAccess > lastWrite.lastAccess);

            //Double add
            Assert.ThrowsException<ArgumentException>(() => { component.Instances.Add(instance); });
        }
        [TestMethod]
        public void RemoveInstance()
        {
            this.LoadProject(testProject);

            var component = this.projectData.Components.First(x => x.Name == "Wall 2");
            var lastWrite = component.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write);
            Thread.Sleep(5);

            var instance = component.Instances.First();
            var instanceId = instance.Id.LocalId;

            component.Instances.Remove(instance);
            var write = component.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write);

            Assert.IsFalse(component.Instances.Contains(instance));
            Assert.AreEqual(null, instance.Component);
            Assert.AreEqual(null, instance.Factory);
            Assert.AreEqual(null, instance.Id.Location);
            Assert.AreEqual(instanceId, instance.Id.LocalId);
            Assert.IsTrue(write.lastAccess > lastWrite.lastAccess);
            Assert.AreEqual(null, this.projectData.IdGenerator.GetById<SimComponentInstance>(instance.Id));
        }
        [TestMethod]
        public void ClearInstances()
        {
            this.LoadProject(testProject);

            var component = this.projectData.Components.First(x => x.Name == "Wall 2");
            var lastWrite = component.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write);
            Thread.Sleep(5);

            var oldInstances = component.Instances.ToArray();

            component.Instances.Clear();
            var write = component.AccessLocal.LastAccess(SimComponentAccessPrivilege.Write);

            Assert.AreEqual(0, component.Instances.Count);
            Assert.IsTrue(write.lastAccess > lastWrite.lastAccess);

            foreach (var instance in oldInstances)
            {
                Assert.AreEqual(null, instance.Component);
                Assert.AreEqual(null, instance.Factory);
                Assert.AreEqual(null, instance.Id.Location);
                Assert.AreEqual(null, this.projectData.IdGenerator.GetById<SimComponentInstance>(instance.Id));
            }
        }

        private WeakReference MemoryLeakTest_Actio()
        {
            var component = this.projectData.Components.First(x => x.Name == "Wall 2");
            WeakReference instanceRef = new WeakReference(component.Instances.First());

            Assert.IsTrue(instanceRef.IsAlive);

            component.Instances.Remove((SimComponentInstance)instanceRef.Target);

            return instanceRef;
        }
        [TestMethod]
        public void MemoryLeakTest()
        {
            this.LoadProject(testProject);

            var instanceRef = MemoryLeakTest_Actio();

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            Assert.IsFalse(instanceRef.IsAlive);
        }

        [TestMethod]
        public void InstanceStateChanged()
        {
            this.LoadProject(emptyProject);

            var component = new SimComponent();
            this.projectData.Components.Add(component);
            var param1 = new SimDoubleParameter("param1", "m", 1.8)
            {
                Propagation = SimInfoFlow.Input
            };
            component.Parameters.Add(param1);

            Assert.AreEqual(new SimInstanceState(false, SimInstanceConnectionState.Ok), component.InstanceState);

            PropertyChangedEventCounter compPC = new PropertyChangedEventCounter(component);

            var instance = new SimComponentInstance();
            PropertyChangedEventCounter instPC = new PropertyChangedEventCounter(instance);

            //Set state
            instance.State = new SimInstanceState(true, SimInstanceConnectionState.Ok);
            instPC.AssertEventCount(1);
            compPC.AssertEventCount(0);
            Assert.AreEqual(nameof(SimComponentInstance.State), instPC.PropertyChangedArgs[0]);
            instPC.Reset();

            //Add instance
            component.Instances.Add(instance);
            Assert.AreEqual(new SimInstanceState(true, SimInstanceConnectionState.Ok), instance.State);
            Assert.AreEqual(new SimInstanceState(true, SimInstanceConnectionState.Ok), component.InstanceState);

            instPC.AssertEventCount(2);
            Assert.IsFalse(instPC.PropertyChangedArgs.Contains(nameof(SimComponentInstance.State)));
            compPC.AssertEventCount(1);
            Assert.AreEqual(nameof(SimComponent.InstanceState), compPC.PropertyChangedArgs[0]);
            compPC.Reset();
            instPC.Reset();

            //Change state
            instance.State = new SimInstanceState(false, SimInstanceConnectionState.GeometryNotFound);
            Assert.AreEqual(new SimInstanceState(false, SimInstanceConnectionState.GeometryNotFound), instance.State);
            Assert.AreEqual(new SimInstanceState(false, SimInstanceConnectionState.GeometryNotFound), component.InstanceState);

            instPC.AssertEventCount(1);
            compPC.AssertEventCount(1);
            Assert.AreEqual(nameof(SimComponentInstance.State), instPC.PropertyChangedArgs[0]);
            Assert.AreEqual(nameof(SimComponent.InstanceState), compPC.PropertyChangedArgs[0]);
            instPC.Reset();
            compPC.Reset();

            //Set to same state
            instance.State = new SimInstanceState(false, SimInstanceConnectionState.GeometryNotFound);
            instPC.AssertEventCount(0);
            compPC.AssertEventCount(0);

            //Remove instance
            component.Instances.Remove(instance);
            Assert.AreEqual(new SimInstanceState(false, SimInstanceConnectionState.Ok), component.InstanceState);

            compPC.AssertEventCount(1);
            Assert.AreEqual(nameof(SimComponent.InstanceState), compPC.PropertyChangedArgs[0]);
            compPC.Reset();
        }

        [TestMethod]
        public void AccessTestManagement()
        {
            this.LoadProject(accessProject, "bph", "bph");

            var archComp = this.projectData.Components.First(x => x.Name == "ArchComp");
            var bphComp = this.projectData.Components.First(x => x.Name == "BPHComp");

            var instance = new SimComponentInstance();

            Assert.ThrowsException<AccessDeniedException>(() => { archComp.Instances.Add(instance); });

            Assert.AreEqual(0, bphComp.Instances.Count);
            bphComp.Instances.Add(instance);
            Assert.AreEqual(1, bphComp.Instances.Count);

            Assert.ThrowsException<AccessDeniedException>(() => { archComp.Instances.RemoveAt(0); });
            Assert.ThrowsException<AccessDeniedException>(() => { archComp.Instances.Clear(); });

            bphComp.Instances.Remove(instance);
            Assert.AreEqual(0, bphComp.Instances.Count);
        }
        [TestMethod]
        public void AccessTestPlacements()
        {
            this.LoadProject(accessProject, "bph", "bph");

            var archComp = this.projectData.Components.First(x => x.Name == "ArchComp");
            var bphComp = this.projectData.Components.First(x => x.Name == "BPHComp");
            var node = this.projectData.NetworkManager.NetworkRecord.First().ContainedNodes.Values.First(x => x.Content == null);

            //Add placement to instance outside of tree
            var instance = new SimComponentInstance();
            instance.Placements.Add(new SimInstancePlacementNetwork(node, SimInstanceType.NetworkNode));
            instance.Placements.RemoveAt(0);

            //Placement in read only component
            instance = archComp.Instances.First();
            Assert.ThrowsException<AccessDeniedException>(() => { instance.Placements.Add(new SimInstancePlacementNetwork(node, SimInstanceType.NetworkNode)); });
            Assert.ThrowsException<AccessDeniedException>(() => { instance.Placements.RemoveAt(0); });


            //Placement in writable component
            instance = new SimComponentInstance();
            bphComp.Instances.Add(instance);
            Assert.AreEqual(bphComp, instance.Component);
            instance.Placements.Add(new SimInstancePlacementNetwork(node, SimInstanceType.NetworkNode));
            Assert.AreEqual(1, instance.Placements.Count);
            instance.Placements.RemoveAt(0);
            Assert.AreEqual(0, instance.Placements.Count);
        }
        [TestMethod]
        public void AccessTestProperties()
        {
            this.LoadProject(accessProject, "bph", "bph");

            var archComp = this.projectData.Components.First(x => x.Name == "ArchComp");
            var bphComp = this.projectData.Components.First(x => x.Name == "BPHComp");
            var instance = new SimComponentInstance();

            //Instance not in tree
            instance.Description = "new description";
            instance.InstanceRotation = new SimQuaternion(new SimVector3D(0, 0, 1), 33);
            instance.InstanceSize = new SimInstanceSize(new SimVector3D(1, 1, 1), new SimVector3D(99, 99, 99));
            instance.Name = "another name";
            instance.SizeTransfer = new SimInstanceSizeTransferDefinition();
            instance.State = new SimInstanceState(true, SimInstanceConnectionState.GeometryNotFound);

            //Instance in read only component
            var archInst = archComp.Instances.First();
            Assert.ThrowsException<AccessDeniedException>(() => { archInst.Description = "new description"; });
            Assert.ThrowsException<AccessDeniedException>(() => { archInst.InstanceRotation = new SimQuaternion(new SimVector3D(0, 0, 1), 33); });
            Assert.ThrowsException<AccessDeniedException>(() => { archInst.InstanceSize = new SimInstanceSize(new SimVector3D(1, 1, 1), new SimVector3D(99, 99, 99)); });
            Assert.ThrowsException<AccessDeniedException>(() => { archInst.Name = "another name"; });
            Assert.ThrowsException<AccessDeniedException>(() => { archInst.SizeTransfer = new SimInstanceSizeTransferDefinition(); });
            Assert.ThrowsException<AccessDeniedException>(() => { archInst.State = new SimInstanceState(true, SimInstanceConnectionState.GeometryNotFound); });

            //Instance in writable component
            instance = new SimComponentInstance();
            bphComp.Instances.Add(instance);
            Assert.AreEqual(bphComp, instance.Component);

            instance.Description = "new description";
            instance.InstanceRotation = new SimQuaternion(new SimVector3D(0, 0, 1), 33);
            instance.InstanceSize = new SimInstanceSize(new SimVector3D(1, 1, 1), new SimVector3D(99, 99, 99));
            instance.Name = "another name";
            instance.SizeTransfer = new SimInstanceSizeTransferDefinition();
            instance.State = new SimInstanceState(true, SimInstanceConnectionState.GeometryNotFound);
        }
        [TestMethod]
        public void AccessTestParametersPersistent()
        {
            this.LoadProject(accessProject, "bph", "bph");

            var archComp = this.projectData.Components.First(x => x.Name == "ArchComp");
            var bphComp = this.projectData.Components.First(x => x.Name == "BPHComp");
            var param1 = archComp.Parameters.First(x => x.NameTaxonomyEntry.Text == "Parameter1");
            var param2 = bphComp.Parameters.First(x => x.NameTaxonomyEntry.Text == "Parameter2");

            //Instance outside of tree
            //It's not possible to edit parameters before the instance has been added to the component

            //Instance in read only component
            var instance = archComp.Instances.First();
            Assert.ThrowsException<AccessDeniedException>(() => { instance.InstanceParameterValuesPersistent[param1] = 99.9; });

            //Instance in writable component
            instance = new SimComponentInstance();
            bphComp.Instances.Add(instance);
            instance.InstanceParameterValuesPersistent[param2] = 99.9;
        }
        [TestMethod]
        public void AccessTestParametersTemporary()
        {
            this.LoadProject(accessProject, "bph", "bph");

            var archComp = this.projectData.Components.First(x => x.Name == "ArchComp");
            var bphComp = this.projectData.Components.First(x => x.Name == "BPHComp");
            var param1 = archComp.Parameters.First(x => x.NameTaxonomyEntry.Text == "Parameter1");
            var param2 = bphComp.Parameters.First(x => x.NameTaxonomyEntry.Text == "Parameter2");

            //Instance outside of tree
            //It's not possible to edit parameters before the instance has been added to the component

            //Instance in read only component
            //temporary parameters don't require write access
            var instance = archComp.Instances.First();
            instance.InstanceParameterValuesTemporary[param1] = 99.9;

            //Instance in writable component
            instance = new SimComponentInstance();
            bphComp.Instances.Add(instance);
            instance.InstanceParameterValuesTemporary[param2] = 99.9;
        }


        [TestMethod]
        public void RemoveComponentCheckInstances()
        {
            this.LoadProject(instanceProject);

            var nodeComponent = this.projectData.Components.First(x => x.Name == "Edge");
            var network = this.projectData.NetworkManager.NetworkRecord.First(x => x.Name == "Network");
            var edge = network.ContainedEdges.Values.First(x => x.Name == "Edge 4");

            var instance = new SimComponentInstance(edge);
            nodeComponent.Instances.Add(instance);

            this.projectData.Components.Remove(nodeComponent);

            Assert.AreEqual(null, edge.Content);
        }
    }
}
