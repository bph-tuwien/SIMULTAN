﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data;
using SIMULTAN.Data.Components;
using SIMULTAN.Data.SimMath;
using SIMULTAN.Data.SimNetworks;
using SIMULTAN.Tests.TestUtils;
using System;
using System.IO;
using System.Linq;

using static SIMULTAN.Data.SimNetworks.BaseSimNetworkElement;
using static SIMULTAN.Data.SimNetworks.SimNetwork;
using static SIMULTAN.Data.SimNetworks.SimNetworkPort;

namespace SIMULTAN.Tests.SimNetworks
{
    [TestClass]
    public class SimNetworkTests : BaseProjectTest
    {

        private string _name = "SimultanSimNetworkUnitTest";

        private string _bName1 = "TestBlock1";
        private string _bName2 = "TestBlock2";

        private string _BlockComp = "BlockComp";

        private string _BlockCompPorts = "BlockCompPorts";
        private string _PortInComp = "PortInComp";
        private string _PortOutComp = "PortOutComp";


        private static readonly FileInfo simNetworkProject = new FileInfo(@"./SimultanSimNetworkUnitTest.simultan");

        internal void CheckNetworkProperties(SimNetwork network, string name, SimPoint position, SimNetwork parent, SimNetworkCollection factory, double width, double height)
        {
            Assert.IsNotNull(network);
            Assert.AreEqual(network.Name, name);

            Assert.AreEqual(network.Position, position);

            Assert.AreEqual(network.ParentNetwork, parent);

            Assert.IsNotNull(network.ContainedElements);
            Assert.IsNotNull(network.Ports);

            Assert.IsNotNull(network.Factory);
            Assert.AreEqual(network.Factory, factory);

            Assert.IsNotNull(network.ContainedConnectors);
            Assert.IsNotNull(network.ContainedElements);
            Assert.IsNotNull(network.Ports);

            AssertUtil.AssertDoubleEqual(network.Width, width);
            AssertUtil.AssertDoubleEqual(network.Height, height);
        }



        internal void CheckNodeProperties(SimNetworkBlock block, string blockName, SimPoint position, SimNetwork parent, SimNetworkElementCollection containedElements, double width, double height)
        {
            Assert.IsNotNull(block);
            Assert.AreEqual(block.Name, blockName);
            Assert.AreEqual(block.Position, position);
            Assert.AreEqual(block.ParentNetwork, parent);
            Assert.IsNotNull(block.Ports);
            Assert.AreEqual(block.ParentNetwork.Factory, parent.Factory);
            Assert.IsTrue(containedElements.Contains(block));


            AssertUtil.AssertDoubleEqual(block.Width, width);
            AssertUtil.AssertDoubleEqual(block.Height, height);
        }


        internal void CheckPortProperties(SimNetwork parentNetwork, SimNetworkPort port, SimPoint position, BaseSimNetworkElement parent, PortType type, SimNetworkPortCollection portsFactory)
        {
            Assert.AreEqual(port.ParentNetworkElement, parent);
            Assert.AreEqual(port.PortType, type);
            Assert.AreEqual(parentNetwork.Factory, port.Factory);
        }



        [TestMethod]
        public void LoadAndCheckNetwork()
        {
            this.LoadProject(simNetworkProject);
            var loadedNW = this.projectData.SimNetworks.FirstOrDefault(t => t.Name == this._name);
            Assert.IsNotNull(loadedNW);
            var id = loadedNW.Id;

            this.CheckNetworkProperties(loadedNW, this._name, new SimPoint(0, 0), null, this.projectData.SimNetworks, 200, 100);
        }

        public void DeleteNetwork()
        {
            this.LoadProject(simNetworkProject);
            var loadedNW = this.projectData.SimNetworks.FirstOrDefault(t => t.Name == this._name);
            var id = loadedNW.Id;

            if (loadedNW != null)
            {
                this.projectData.SimNetworks.Remove(loadedNW);
            }
            Assert.AreEqual(0, this.projectData.SimNetworks.Count);

            //Check if ID is free
            var elment = this.projectData.IdGenerator.GetById<SimObjectNew>(id);
            Assert.AreEqual(null, elment);
        }


        private WeakReference MemoryLeakTestNetworkDelete_Action()
        {
            Assert.IsNotNull(this.projectData.SimNetworks.FirstOrDefault(t => t.Name == this._name));
            Assert.IsNotNull(this.projectData.SimNetworks.FirstOrDefault(t => t.Name == this._name).ContainedElements);
            WeakReference nwRef = new WeakReference(this.projectData.SimNetworks.FirstOrDefault(t => t.Name == this._name));
            Assert.IsTrue(nwRef.IsAlive);

            this.projectData.SimNetworks.Remove(this.projectData.SimNetworks.FirstOrDefault(t => t.Name == this._name));

            return nwRef;
        }
        [TestMethod]
        public void MemoryLeakTestNetworkDelete()
        {
            this.LoadProject(simNetworkProject);

            var nwRef = MemoryLeakTestNetworkDelete_Action();

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.WaitForFullGCComplete();
            GC.Collect();

            Assert.IsFalse(nwRef.IsAlive);
        }



        [TestMethod]
        public void CreateSimNetworkAndCheck()
        {
            this.LoadProject(simNetworkProject);

            //Test execptions
            Assert.ThrowsException<ArgumentNullException>(() => new SimNetwork(null));
            Assert.ThrowsException<ArgumentNullException>(() => new SimNetwork(new SimId(), null, new SimPoint(0, 0),
                Enumerable.Empty<SimNetworkPort>(), Enumerable.Empty<BaseSimNetworkElement>(), Enumerable.Empty<SimNetworkConnector>(), SimColors.DarkGray));
            Assert.ThrowsException<ArgumentNullException>(() => new SimNetwork(new SimId(), "", new SimPoint(0, 0),
                null, Enumerable.Empty<BaseSimNetworkElement>(), Enumerable.Empty<SimNetworkConnector>(), SimColors.DarkGray));
            Assert.ThrowsException<ArgumentNullException>(() => new SimNetwork(new SimId(), "", new SimPoint(0, 0),
                Enumerable.Empty<SimNetworkPort>(), null, Enumerable.Empty<SimNetworkConnector>(), SimColors.DarkGray));
            Assert.ThrowsException<ArgumentNullException>(() => new SimNetwork(new SimId(), "", new SimPoint(0, 0),
                Enumerable.Empty<SimNetworkPort>(), Enumerable.Empty<BaseSimNetworkElement>(), null, (SimColors.DarkGray)));
            Assert.ThrowsException<ArgumentNullException>(() => new SimNetwork(null, new SimPoint(0, 0)));

            //Create a network
            var network = new SimNetwork(this._name);
            this.projectData.SimNetworks.Add(network);

            Assert.IsNotNull(network.Id);
            var id = network.Id;

            var elment = this.projectData.IdGenerator.GetById<SimObjectNew>(id);
            Assert.IsNotNull(elment);
            Assert.IsInstanceOfType(elment, typeof(SimNetwork));

            Assert.AreEqual(elment, network);
        }


        [TestMethod]
        public void TestBlockToSimNetworkConversion()
        {
            this.LoadProject(simNetworkProject);
            var loadedNW = this.projectData.SimNetworks.FirstOrDefault(t => t.Name == this._name);
            Assert.IsNotNull(loadedNW);


            //Add Blocks to it
            var block = new SimNetworkBlock("BlockToConvert", new SimPoint(100, 100));
            var id = block.Id;
            Assert.IsNotNull(block);
            loadedNW.ContainedElements.Add(block);
            this.CheckNodeProperties(block, "BlockToConvert", new SimPoint(100, 100), loadedNW, loadedNW.ContainedElements, block.Width, block.Height);


            var convertedNetwork = block.ParentNetwork.ConvertBlockToSubnetwork(block);

            Assert.IsNotNull(convertedNetwork);





            //Check if ID is free
            var lBlock = this.projectData.IdGenerator.GetById<SimNetworkBlock>(id);
            Assert.AreEqual(null, lBlock);


            //Check if ID is free
            var lNetwork = this.projectData.IdGenerator.GetById<SimNetwork>(convertedNetwork.Id);
            Assert.AreNotEqual(null, lNetwork);

        }




        [TestMethod]
        public void CreateBlockAndCheck()
        {
            this.LoadProject(simNetworkProject);
            var loadedNW = this.projectData.SimNetworks.FirstOrDefault(t => t.Name == this._name);
            Assert.IsNotNull(loadedNW);


            //Test execptions
            Assert.ThrowsException<ArgumentNullException>(() => new SimNetworkBlock(null, new SimPoint(0, 0), new SimId(), new SimNetworkPort[0], SimColors.DarkGray));
            Assert.ThrowsException<ArgumentNullException>(() => new SimNetworkBlock(null, new SimPoint(0, 0)));



            //Add Blocks to it
            var block = new SimNetworkBlock(this._bName1, new SimPoint(100, 100))
            {
                Height = 300,
                Width = 50
            };
            Assert.IsNotNull(block);
            loadedNW.ContainedElements.Add(block);
            this.CheckNodeProperties(block, this._bName1, new SimPoint(100, 100), loadedNW, loadedNW.ContainedElements, 50, 300);

        }


        [TestMethod]
        public void LoadBlockAndCheck()
        {
            this.LoadProject(simNetworkProject);
            var loadedNW = this.projectData.SimNetworks.FirstOrDefault(t => t.Name == this._name);

            Assert.IsNotNull(loadedNW);
            Assert.IsNotNull(loadedNW.ContainedElements);

            var loadedBlock = loadedNW.ContainedElements.FirstOrDefault(e => e.Name == this._bName1) as SimNetworkBlock;

            loadedBlock.Position = new SimPoint(100, 100);

            Assert.IsNotNull(loadedBlock);
            this.CheckNodeProperties(loadedBlock, this._bName1, new SimPoint(100, 100), loadedNW, loadedNW.ContainedElements, 200, 100);

        }

        [TestMethod]
        public void DeleteBlockAndCheck()
        {
            this.LoadProject(simNetworkProject);
            var loadedNW = this.projectData.SimNetworks.FirstOrDefault(t => t.Name == this._name);


            Assert.IsNotNull(loadedNW);
            Assert.IsNotNull(loadedNW.ContainedElements);

            var loadedBlock = loadedNW.ContainedElements.FirstOrDefault(e => e.Name == this._bName1) as SimNetworkBlock;
            var id = loadedBlock.Id;
            loadedNW.ContainedElements.Remove(loadedBlock);

            var block = loadedNW.ContainedElements.FirstOrDefault(t => t.Name == this._bName1);
            Assert.AreEqual(block, null);

            //Check if ID is free
            var elment = this.projectData.IdGenerator.GetById<SimObjectNew>(id);
            Assert.AreEqual(null, elment);
        }


        [TestMethod]
        public void DeleteBlockWithComponentsAndPortsWithComponents()
        {
            this.LoadProject(simNetworkProject);
            var loadedNW = this.projectData.SimNetworks.FirstOrDefault(t => t.Name == this._name);
            Assert.IsNotNull(loadedNW);
            Assert.IsNotNull(loadedNW.ContainedElements);

            var loadedBlock1 = loadedNW.ContainedElements.FirstOrDefault(e => e.Name == this._bName1) as SimNetworkBlock;
            var component = this.projectData.Components.FirstOrDefault(t => t.Name == this._BlockCompPorts);
            loadedBlock1.AssignComponent(component, true);

            var portInComp = component.Components.FirstOrDefault(t => t.Component.Name == this._PortInComp);
            Assert.IsNotNull(portInComp);
            var portOutComp = component.Components.FirstOrDefault(t => t.Component.Name == this._PortOutComp);
            Assert.IsNotNull(portOutComp);

            Assert.IsNotNull(loadedBlock1.ComponentInstance);
            Assert.IsNotNull(loadedBlock1.ComponentInstance.Component);
            Assert.AreEqual(loadedBlock1.ComponentInstance.Component.Name, this._BlockCompPorts);


            //Check if ports are contianing the subcomponents
            var portWithInPortComp = loadedBlock1.Ports.FirstOrDefault(t => t.ComponentInstance != null && t.ComponentInstance.Component == portInComp.Component);
            Assert.IsNotNull(portWithInPortComp);
            Assert.IsNotNull(portWithInPortComp.ComponentInstance);
            Assert.IsNotNull(portWithInPortComp.ComponentInstance.Component);
            Assert.AreEqual(portWithInPortComp.ComponentInstance.Component.Name, this._PortInComp);

            var portWithOutPortComp = loadedBlock1.Ports.FirstOrDefault(t => t.ComponentInstance != null && t.ComponentInstance.Component == portOutComp.Component);
            Assert.IsNotNull(portWithOutPortComp);
            Assert.IsNotNull(portWithOutPortComp.ComponentInstance);
            Assert.IsNotNull(portWithOutPortComp.ComponentInstance.Component);
            Assert.AreEqual(portWithOutPortComp.ComponentInstance.Component.Name, this._PortOutComp);


            //DELETE BLOCK
            loadedNW.ContainedElements.Remove(loadedBlock1);


            Assert.AreEqual(portOutComp.Component.Instances.SelectMany(t => t.Placements).Count(), 0);

            //TODO: Check if the Ports get their components assigned disassociated
            Assert.IsTrue(loadedBlock1.Ports.All(p => p.ComponentInstance == null));
            Assert.IsTrue(portInComp.Component.Instances.Count == 0);
            Assert.IsTrue(portOutComp.Component.Instances.Count == 0);

        }


        private WeakReference MemoryLeakTestBlockDelete_Action()
        {
            var loadedNW = this.projectData.SimNetworks.FirstOrDefault(t => t.Name == this._name);

            Assert.IsNotNull(loadedNW);
            Assert.IsNotNull(loadedNW.ContainedElements);

            WeakReference blockRef = new WeakReference(loadedNW.ContainedElements.FirstOrDefault(e => e.Name == this._bName1) as SimNetworkBlock);
            Assert.IsTrue(blockRef.IsAlive);

            loadedNW.ContainedElements.Remove(loadedNW.ContainedElements.FirstOrDefault(e => e.Name == this._bName1) as SimNetworkBlock);
            return blockRef;
        }
        [TestMethod]
        public void MemoryLeakTestBlockDelete()
        {
            this.LoadProject(simNetworkProject);

            var blockRef = MemoryLeakTestBlockDelete_Action();

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            Assert.IsFalse(blockRef.IsAlive);
        }


        [TestMethod]
        public void AddPort()
        {
            this.LoadProject(simNetworkProject);
            var loadedNW = this.projectData.SimNetworks.FirstOrDefault(t => t.Name == this._name);

            Assert.IsNotNull(loadedNW);
            Assert.IsNotNull(loadedNW.ContainedElements);

            var loadedBlock = loadedNW.ContainedElements.FirstOrDefault(e => e.Name == this._bName1) as SimNetworkBlock;
            var portsCunt = loadedBlock.Ports.Count;
            var newPort = new SimNetworkPort(PortType.Output);
            loadedBlock.Ports.Add(newPort);

            Assert.AreNotEqual(newPort.Id, SimId.Empty);
            Assert.AreEqual(portsCunt + 1, loadedBlock.Ports.Count);

            var addedPort = this.projectData.IdGenerator.GetById<SimNetworkPort>(newPort.Id);
            Assert.IsNotNull(addedPort);

            this.CheckPortProperties(loadedNW, addedPort, new SimPoint(0, 0), loadedBlock, PortType.Output, loadedBlock.Ports);
        }

        [TestMethod]
        public void RemovePort()
        {
            this.LoadProject(simNetworkProject);
            var loadedNW = this.projectData.SimNetworks.FirstOrDefault(t => t.Name == this._name);

            Assert.IsNotNull(loadedNW);
            Assert.IsNotNull(loadedNW.ContainedElements);

            var loadedBlock = loadedNW.ContainedElements.FirstOrDefault(e => e.Name == this._bName1) as SimNetworkBlock;

            var firstPort = loadedBlock.Ports.FirstOrDefault();
            var deletedId = firstPort.Id;
            loadedBlock.Ports.Remove(firstPort);

            var searchForPort = loadedBlock.Ports.FirstOrDefault(p => p.Id == deletedId);
            Assert.IsNull(searchForPort);


            //Check if ID is free
            var elment = this.projectData.IdGenerator.GetById<SimObjectNew>(deletedId);
            Assert.AreEqual(null, elment);

        }


        private SimNetworkConnector AddConnector(SimNetwork loadedNW)
        {
            var loadedBlock1 = loadedNW.ContainedElements.FirstOrDefault(e => e.Name == this._bName1) as SimNetworkBlock;
            var loadedBlock2 = loadedNW.ContainedElements.FirstOrDefault(e => e.Name == this._bName2) as SimNetworkBlock;

            Assert.IsNotNull(loadedBlock1);
            Assert.IsNotNull(loadedBlock2);

            var sourcePort = loadedBlock1.Ports.FirstOrDefault(p => p.PortType == PortType.Output && p.Connectors.Count == 0);
            var targetPort = loadedBlock2.Ports.FirstOrDefault(p => p.PortType == PortType.Input && p.Connectors.Count == 0);

            Assert.IsNotNull(sourcePort);
            Assert.IsNotNull(targetPort);

            sourcePort.ConnectTo(targetPort);

            Assert.AreEqual(sourcePort.Connectors.Count, 1);

            return sourcePort.Connectors.FirstOrDefault();
        }

        private WeakReference MemoryLeakTestPortDelete_Action()
        {
            var loadedNW = this.projectData.SimNetworks.FirstOrDefault(t => t.Name == this._name);

            Assert.IsNotNull(loadedNW);
            Assert.IsNotNull(loadedNW.ContainedElements);

            var loadedBlock = loadedNW.ContainedElements.FirstOrDefault(e => e is SimNetworkBlock block && block.ComponentInstance == null) as SimNetworkBlock;


            WeakReference portRef = new WeakReference(loadedBlock.Ports.FirstOrDefault(t => t.ComponentInstance == null));
            Assert.IsTrue(portRef.IsAlive);

            var deletedId = loadedBlock.Ports.FirstOrDefault(t => t.ComponentInstance == null).Id;
            loadedBlock.Ports.Remove(loadedBlock.Ports.FirstOrDefault(t => t.ComponentInstance == null));

            var searchForPort = loadedBlock.Ports.FirstOrDefault(p => p.Id == deletedId);
            Assert.IsNull(searchForPort);

            return portRef;
        }
        [TestMethod]
        public void MemoryLeakTestPortDelete()
        {
            this.LoadProject(simNetworkProject);

            var portRef = MemoryLeakTestPortDelete_Action();

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            Assert.IsFalse(portRef.IsAlive);
        }


        [TestMethod]
        public void RemovePortCheckConnection()
        {
            this.LoadProject(simNetworkProject);
            var loadedNW = this.projectData.SimNetworks.FirstOrDefault(t => t.Name == this._name);

            Assert.IsNotNull(loadedNW);
            Assert.IsNotNull(loadedNW.ContainedElements);

            var loadedBlock = loadedNW.ContainedElements.FirstOrDefault(e => e.Name == this._bName1) as SimNetworkBlock;

            var connector = this.AddConnector(loadedNW);

            var firstPort = loadedBlock.Ports.FirstOrDefault(t => t.Connectors.Count > 0);

            var deletedId = firstPort.Id;
            loadedBlock.Ports.Remove(firstPort);

            var searchForPort = loadedBlock.Ports.FirstOrDefault(p => p.Id == deletedId);
            Assert.IsNull(searchForPort);


            //Check if the connected port has rmeoved its connection to the deleted port
            var elment = this.projectData.IdGenerator.GetById<SimObjectNew>(deletedId);
            Assert.AreEqual(null, elment);
            //Check if the connector is also not included in the project anymore
            var lConnector = this.projectData.IdGenerator.GetById<SimObjectNew>(connector.Id);
            Assert.IsNull(lConnector);
            Assert.AreEqual(loadedNW.ContainedConnectors.Count, 0);

        }


        [TestMethod]
        public void ConnectPort()
        {

            this.LoadProject(simNetworkProject);
            var loadedNW = this.projectData.SimNetworks.FirstOrDefault(t => t.Name == this._name);
            Assert.IsNotNull(loadedNW);
            Assert.IsNotNull(loadedNW.ContainedElements);

            var loadedBlock1 = loadedNW.ContainedElements.FirstOrDefault(e => e.Name == this._bName1) as SimNetworkBlock;
            var loadedBlock2 = loadedNW.ContainedElements.FirstOrDefault(e => e.Name == this._bName2) as SimNetworkBlock;

            Assert.IsNotNull(loadedBlock1);
            Assert.IsNotNull(loadedBlock2);

            var sourcePort = loadedBlock1.Ports.FirstOrDefault(p => p.PortType == PortType.Output && p.Connectors.Count == 0);
            var targetPort = loadedBlock2.Ports.FirstOrDefault(p => p.PortType == PortType.Input && p.Connectors.Count == 0);

            Assert.IsNotNull(sourcePort);
            Assert.IsNotNull(targetPort);

            sourcePort.ConnectTo(targetPort);

            Assert.AreEqual(sourcePort.Connectors.FirstOrDefault().Target, targetPort);
            Assert.AreEqual(sourcePort.Connectors.FirstOrDefault().Source, sourcePort);

        }

        [TestMethod]
        public void DeletePortConnection()
        {
            this.LoadProject(simNetworkProject);
            var loadedNW = this.projectData.SimNetworks.FirstOrDefault(t => t.Name == this._name);
            Assert.IsNotNull(loadedNW);
            Assert.IsNotNull(loadedNW.ContainedElements);

            var loadedBlock1 = loadedNW.ContainedElements.FirstOrDefault(e => e.Name == this._bName1) as SimNetworkBlock;
            var loadedBlock2 = loadedNW.ContainedElements.FirstOrDefault(e => e.Name == this._bName2) as SimNetworkBlock;

            Assert.IsNotNull(loadedBlock1);
            Assert.IsNotNull(loadedBlock2);

            var sourcePort = loadedBlock1.Ports.FirstOrDefault(p => p.PortType == PortType.Output && p.Connectors.Count == 0);
            var targetPort = loadedBlock2.Ports.FirstOrDefault(p => p.PortType == PortType.Input && p.Connectors.Count == 0);

            Assert.IsNotNull(sourcePort);
            Assert.IsNotNull(targetPort);


            Assert.IsNotNull(sourcePort);
            Assert.IsNotNull(targetPort);

            sourcePort.ConnectTo(targetPort);

            Assert.AreEqual(sourcePort.Connectors.FirstOrDefault().Target, targetPort);
            Assert.AreEqual(sourcePort.Connectors.FirstOrDefault().Source, sourcePort);

            sourcePort.RemoveConnections();
            Assert.IsFalse(loadedNW.ContainedConnectors.Contains(sourcePort.Connectors.FirstOrDefault()));


        }

        private WeakReference MemoryLeakCheckConnectorMemory_Action()
        {
            var loadedNW = this.projectData.SimNetworks.FirstOrDefault(t => t.Name == this._name);
            Assert.IsNotNull(loadedNW);
            Assert.IsNotNull(loadedNW.ContainedElements);

            var loadedBlock1 = loadedNW.ContainedElements.FirstOrDefault(e => e.Name == this._bName1) as SimNetworkBlock;
            var loadedBlock2 = loadedNW.ContainedElements.FirstOrDefault(e => e.Name == this._bName2) as SimNetworkBlock;

            Assert.IsNotNull(loadedBlock1);
            Assert.IsNotNull(loadedBlock2);

            var sourcePort = loadedBlock1.Ports.FirstOrDefault(p => p.PortType == PortType.Output && p.Connectors.Count == 0);
            var targetPort = loadedBlock2.Ports.FirstOrDefault(p => p.PortType == PortType.Input && p.Connectors.Count == 0);

            Assert.IsNotNull(sourcePort);
            Assert.IsNotNull(targetPort);

            sourcePort.ConnectTo(targetPort);


            WeakReference nwRef = new WeakReference(loadedNW.ContainedConnectors.FirstOrDefault());
            Assert.IsTrue(nwRef.IsAlive);

            loadedNW.ContainedConnectors.Remove(loadedNW.ContainedConnectors.FirstOrDefault());

            return nwRef;
        }
        [TestMethod]
        public void MemoryLeakCheckConnectorMemory()
        {
            this.LoadProject(simNetworkProject);

            var nwRef = MemoryLeakCheckConnectorMemory_Action();

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.WaitForFullGCComplete();
            GC.Collect();


            Assert.IsFalse(nwRef.IsAlive);
        }


        [TestMethod]
        public void DeleteBlockCheckPorts()
        {
            this.LoadProject(simNetworkProject);
            var loadedNW = this.projectData.SimNetworks.FirstOrDefault(t => t.Name == this._name);
            Assert.IsNotNull(loadedNW);
            Assert.IsNotNull(loadedNW.ContainedElements);

            var loadedBlock1 = loadedNW.ContainedElements.FirstOrDefault(e => e.Name == this._bName1) as SimNetworkBlock;
            var ports = loadedBlock1.Ports;
            loadedNW.ContainedElements.Remove(loadedBlock1);

            //Check if ID is free
            var element = this.projectData.IdGenerator.GetById<SimObjectNew>(loadedBlock1.Id);
            Assert.AreEqual(null, element);

            //Check if ports are remainig
            foreach (var item in ports)
            {
                var port = this.projectData.IdGenerator.GetById<SimObjectNew>(item.Id);
                Assert.AreEqual(null, port);
            }
        }





        [TestMethod]
        public void AssignSingleComponentToBlock()
        {
            this.LoadProject(simNetworkProject);
            var loadedNW = this.projectData.SimNetworks.FirstOrDefault(t => t.Name == this._name);
            Assert.IsNotNull(loadedNW);
            Assert.IsNotNull(loadedNW.ContainedElements);

            var loadedBlock1 = loadedNW.ContainedElements.FirstOrDefault(e => e.Name == this._bName1) as SimNetworkBlock;
            var component = this.projectData.Components.FirstOrDefault(t => t.Name == this._BlockComp);

            var compInstance = new SimComponentInstance(loadedBlock1);
            component.Instances.Add(compInstance);


            Assert.IsNotNull(loadedBlock1.ComponentInstance);
            Assert.IsNotNull(loadedBlock1.ComponentInstance.Component);
            Assert.AreEqual(loadedBlock1.ComponentInstance.Component.Name, this._BlockComp);

        }


        [TestMethod]
        public void TestSimNetworkConstructors()
        {
            this.LoadProject(simNetworkProject);


            var network1 = new SimNetwork("SimNetwork1", new SimPoint(0, 0));
            this.projectData.SimNetworks.Add(network1);
            var lNw1 = this.projectData.SimNetworks.FirstOrDefault(t => t.Name == "SimNetwork1");
            Assert.IsNotNull(lNw1);
            CheckNetworkProperties(lNw1, "SimNetwork1", new SimPoint(0, 0), null, this.projectData.SimNetworks, 200, 100);

            var network2 = new SimNetwork(SimId.Empty, "SimNetwork2", new SimPoint(0, 0),
                Enumerable.Empty<SimNetworkPort>(), Enumerable.Empty<BaseSimNetworkElement>(), Enumerable.Empty<SimNetworkConnector>(), (SimColors.DarkGray));
            this.projectData.SimNetworks.Add(network2);
            var lNw2 = this.projectData.SimNetworks.FirstOrDefault(t => t.Name == "SimNetwork2");
            Assert.IsNotNull(lNw2);
            CheckNetworkProperties(lNw2, "SimNetwork2", new SimPoint(0, 0), null, this.projectData.SimNetworks, 200, 100);

        }



        [TestMethod]
        public void AddBlockToSimNetwork()
        {
            this.LoadProject(simNetworkProject);
            var loadedNW = this.projectData.SimNetworks.FirstOrDefault(t => t.Name == this._name);
            Assert.IsNotNull(loadedNW);
            Assert.IsNotNull(loadedNW.ContainedElements);

            var block1 = new SimNetworkBlock("NewNetworkBlock1", new SimPoint(0, 0));
            loadedNW.ContainedElements.Add(block1);
            var lookUpBlock1 = loadedNW.ContainedElements.FirstOrDefault(t => t.Name == "NewNetworkBlock1");
            Assert.IsNotNull(lookUpBlock1);





            var block2 = new SimNetworkBlock("NewNetworkBlock2", new SimPoint(0, 0), SimId.Empty, new SimNetworkPort[0], SimColors.DarkGray);
            loadedNW.ContainedElements.Add(block2);
            var lookUpBlock2 = loadedNW.ContainedElements.FirstOrDefault(t => t.Name == "NewNetworkBlock2");
            Assert.IsNotNull(lookUpBlock2);

        }


        /// <summary>
        /// Checks whether the Placement is gone after deleting dissociating the component from network element
        /// </summary>
        [TestMethod]
        public void CheckPlacementDeletionAfterComponentDissociation()
        {
            this.LoadProject(simNetworkProject);
            var loadedNW = this.projectData.SimNetworks.FirstOrDefault(t => t.Name == this._name);
            Assert.IsNotNull(loadedNW);
            Assert.IsNotNull(loadedNW.ContainedElements);

            var loadedBlock1 = loadedNW.ContainedElements.FirstOrDefault(e => e.Name == this._bName1) as SimNetworkBlock;

            var component = this.projectData.Components.FirstOrDefault(t => t.Name == this._BlockComp);


            var compInstance = new SimComponentInstance(loadedBlock1);
            component.Instances.Add(compInstance);
            var placements1 = component.Instances.SelectMany(s => s.Placements);



            Assert.IsNotNull(loadedBlock1.ComponentInstance);
            Assert.IsNotNull(loadedBlock1.ComponentInstance.Component);
            Assert.AreEqual(loadedBlock1.ComponentInstance.Component.Name, this._BlockComp);


            loadedBlock1.RemoveComponentInstance();
            var findInstance = component.Instances.FirstOrDefault(t => t.Id == compInstance.Id);
            var placements2 = component.Instances.SelectMany(s => s.Placements);
            Assert.AreEqual(findInstance, null);
            Assert.AreEqual(placements2.Count(), 0);

            //Check if ID is free
            var elment = this.projectData.IdGenerator.GetById<SimComponentInstance>(compInstance.Id);
            Assert.AreEqual(null, elment);

        }

        /// <summary>
        /// Checks whether the Placement is gone after deleting the network element
        /// </summary>
        [TestMethod]
        public void CheckPlacementDeletionAfterDeletingNetworkElement()
        {
            this.LoadProject(simNetworkProject);
            var loadedNW = this.projectData.SimNetworks.FirstOrDefault(t => t.Name == this._name);
            Assert.IsNotNull(loadedNW);
            Assert.IsNotNull(loadedNW.ContainedElements);

            var loadedBlock1 = loadedNW.ContainedElements.FirstOrDefault(e => e.Name == this._bName1) as SimNetworkBlock;

            var component = this.projectData.Components.FirstOrDefault(t => t.Name == this._BlockComp);


            var compInstance = new SimComponentInstance(loadedBlock1);
            component.Instances.Add(compInstance);
            var placements1 = component.Instances.SelectMany(s => s.Placements);



            Assert.IsNotNull(loadedBlock1.ComponentInstance);
            Assert.IsNotNull(loadedBlock1.ComponentInstance.Component);
            Assert.AreEqual(loadedBlock1.ComponentInstance.Component.Name, this._BlockComp);


            // loadedBlock1.RemoveComponentInstance();
            loadedBlock1.ParentNetwork.ContainedElements.Remove(loadedBlock1);
            var findInstance = component.Instances.FirstOrDefault(t => t.Id == compInstance.Id);
            var placements2 = component.Instances.SelectMany(s => s.Placements);
            Assert.AreEqual(findInstance, null);
            Assert.AreEqual(placements2.Count(), 0);

            //Check if ID is free
            var elment = this.projectData.IdGenerator.GetById<SimComponentInstance>(compInstance.Id);
            Assert.AreEqual(null, elment);

        }



        [TestMethod]
        public void DisassociateSingleComponentFromBlock()
        {
            this.LoadProject(simNetworkProject);
            var loadedNW = this.projectData.SimNetworks.FirstOrDefault(t => t.Name == this._name);
            Assert.IsNotNull(loadedNW);
            Assert.IsNotNull(loadedNW.ContainedElements);

            var loadedBlock1 = loadedNW.ContainedElements.FirstOrDefault(e => e.Name == this._bName1) as SimNetworkBlock;

            var component = this.projectData.Components.FirstOrDefault(t => t.Name == this._BlockComp);


            var compInstance = new SimComponentInstance(loadedBlock1);
            component.Instances.Add(compInstance);


            Assert.IsNotNull(loadedBlock1.ComponentInstance);
            Assert.IsNotNull(loadedBlock1.ComponentInstance.Component);
            Assert.AreEqual(loadedBlock1.ComponentInstance.Component.Name, this._BlockComp);


            loadedBlock1.RemoveComponentInstance();
            var findInstance = component.Instances.FirstOrDefault(t => t.Id == compInstance.Id);
            Assert.AreEqual(findInstance, null);


            //Check if ID is free
            var elment = this.projectData.IdGenerator.GetById<SimComponentInstance>(compInstance.Id);
            Assert.AreEqual(null, elment);


        }



        [TestMethod]
        public void DisassociateSingleComponentWithPortCompsFromBlock()
        {
            this.LoadProject(simNetworkProject);
            var loadedNW = this.projectData.SimNetworks.FirstOrDefault(t => t.Name == this._name);
            Assert.IsNotNull(loadedNW);
            Assert.IsNotNull(loadedNW.ContainedElements);

            var loadedBlock1 = loadedNW.ContainedElements.FirstOrDefault(e => e.Name == this._bName1) as SimNetworkBlock;
            var component = this.projectData.Components.FirstOrDefault(t => t.Name == this._BlockCompPorts);

            loadedBlock1.AssignComponent(component, true);


            var portInComp = component.Components.FirstOrDefault(t => t.Component.Name == this._PortInComp);
            Assert.IsNotNull(portInComp);
            var portOutComp = component.Components.FirstOrDefault(t => t.Component.Name == this._PortOutComp);
            Assert.IsNotNull(portOutComp);

            Assert.IsNotNull(loadedBlock1.ComponentInstance);
            Assert.IsNotNull(loadedBlock1.ComponentInstance.Component);
            Assert.AreEqual(loadedBlock1.ComponentInstance.Component.Name, this._BlockCompPorts);


            //Check if ports are contianing the subcomponents
            var portWithInPortComp = loadedBlock1.Ports.FirstOrDefault(t => t.ComponentInstance != null && t.ComponentInstance.Component == portInComp.Component);
            Assert.IsNotNull(portWithInPortComp);
            Assert.IsNotNull(portWithInPortComp.ComponentInstance);
            Assert.IsNotNull(portWithInPortComp.ComponentInstance.Component);
            Assert.AreEqual(portWithInPortComp.ComponentInstance.Component.Name, this._PortInComp);

            var portWithOutPortComp = loadedBlock1.Ports.FirstOrDefault(t => t.ComponentInstance != null && t.ComponentInstance.Component == portOutComp.Component);
            Assert.IsNotNull(portWithOutPortComp);
            Assert.IsNotNull(portWithOutPortComp.ComponentInstance);
            Assert.IsNotNull(portWithOutPortComp.ComponentInstance.Component);
            Assert.AreEqual(portWithOutPortComp.ComponentInstance.Component.Name, this._PortOutComp);


            //DELETE
            loadedBlock1.RemoveComponentInstance();

            //TODO: Check if the Ports get their components assisnged disasociated
            Assert.IsTrue(loadedBlock1.Ports.All(p => p.ComponentInstance == null));

        }


        [TestMethod]
        public void SubcomponentCreatingPortOnTheBlock()
        {
            this.LoadProject(simNetworkProject);
            var loadedNW = this.projectData.SimNetworks.FirstOrDefault(t => t.Name == this._name);
            Assert.IsNotNull(loadedNW);
            Assert.IsNotNull(loadedNW.ContainedElements);

            var loadedBlock1 = loadedNW.ContainedElements.FirstOrDefault(e => e.Name == this._bName1) as SimNetworkBlock;
            var component = this.projectData.Components.FirstOrDefault(t => t.Name == this._BlockComp);
            var initialPortCount = loadedBlock1.Ports.Count;

            //Assign the component to the Block
            loadedBlock1.AssignComponent(component, true);

            //Create the new SubComponent for the component

            var undefinedSlot = projectData.Taxonomies.GetDefaultSlot(SimDefaultSlotKeys.Undefined);
            var newPortInComponent = new SimComponent()
            {
                Name = "PortIn",
                InstanceType = SimInstanceType.InPort,
            };
            newPortInComponent.Slots.Add(new Data.Taxonomy.SimTaxonomyEntryReference(undefinedSlot));
            var newSlot = component.Components.FindAvailableSlot(undefinedSlot);
            component.Components.Add(new SimChildComponentEntry(newSlot, newPortInComponent));


            var portWithComp = loadedBlock1.Ports.FirstOrDefault(p => p.ComponentInstance != null && p.ComponentInstance.Component == newPortInComponent);
            Assert.IsNotNull(portWithComp);


            var newPortOutComponent = new SimComponent()
            {
                Name = "PortOut",
                InstanceType = SimInstanceType.OutPort,
            };
            newPortOutComponent.Slots.Add(new Data.Taxonomy.SimTaxonomyEntryReference(undefinedSlot));
            newSlot = component.Components.FindAvailableSlot(undefinedSlot);
            component.Components.Add(new SimChildComponentEntry(newSlot, newPortOutComponent));

            var portWithComp2 = loadedBlock1.Ports.FirstOrDefault(p => p.ComponentInstance.Component != null && p.ComponentInstance.Component == newPortOutComponent);
            Assert.IsNotNull(portWithComp2);
        }

        /// <summary>
        /// Checks the ctor for loading operation
        /// </summary>
        [TestMethod]
        public void TestNestedNetworkCreation()
        {
            this.LoadProject(simNetworkProject);
            var newNetwork = new SimNetwork("TestNetwork");
            this.projectData.SimNetworks.Add(newNetwork);
            var nestedNetwork = new SimNetwork("NestedNetwork", new SimPoint(0, 0));
            newNetwork.ContainedElements.Add(nestedNetwork);

            Assert.IsNotNull(newNetwork.Id);
            Assert.IsNotNull(nestedNetwork.Id);

            Assert.AreEqual(nestedNetwork.ParentNetwork, newNetwork);
            Assert.IsNull(newNetwork.ParentNetwork);
            this.CheckNetworkProperties(newNetwork, "TestNetwork", new SimPoint(0, 0), null, this.projectData.SimNetworks, 200, 100);
            this.CheckNetworkProperties(nestedNetwork, "NestedNetwork", new SimPoint(0, 0), newNetwork, this.projectData.SimNetworks, 200, 100);

        }


        /// <summary>
        /// Checks whether the asocited component is also deleted after the deletion of a port with component association
        /// </summary>
        [TestMethod]
        public void DeleteAssociatedPort()
        {
            this.LoadProject(simNetworkProject);
            var loadedNW = this.projectData.SimNetworks.FirstOrDefault(t => t.Name == this._name);
            Assert.IsNotNull(loadedNW);
            Assert.IsNotNull(loadedNW.ContainedElements);

            var loadedBlock1 = loadedNW.ContainedElements.FirstOrDefault(e => e.Name == this._bName1) as SimNetworkBlock;
            var component = this.projectData.Components.FirstOrDefault(t => t.Name == this._BlockCompPorts);



            loadedBlock1.AssignComponent(component, true);
            var portInComp = component.Components.FirstOrDefault(t => t.Component.Name == this._PortInComp);
            Assert.IsNotNull(portInComp);
            var portOutComp = component.Components.FirstOrDefault(t => t.Component.Name == this._PortOutComp);
            Assert.IsNotNull(portOutComp);

            Assert.IsNotNull(loadedBlock1.ComponentInstance);
            Assert.IsNotNull(loadedBlock1.ComponentInstance.Component);
            Assert.AreEqual(loadedBlock1.ComponentInstance.Component.Name, this._BlockCompPorts);


            //Check if ports are contianing the subcomponents
            var portWithInPortComp = loadedBlock1.Ports.FirstOrDefault(t => t.ComponentInstance != null && t.ComponentInstance.Component == portInComp.Component);
            Assert.IsNotNull(portWithInPortComp);
            Assert.IsNotNull(portWithInPortComp.ComponentInstance);
            Assert.IsNotNull(portWithInPortComp.ComponentInstance.Component);
            Assert.AreEqual(portWithInPortComp.ComponentInstance.Component.Name, this._PortInComp);

            var portWithOutPortComp = loadedBlock1.Ports.FirstOrDefault(t => t.ComponentInstance != null && t.ComponentInstance.Component == portOutComp.Component);
            Assert.IsNotNull(portWithOutPortComp);
            Assert.IsNotNull(portWithOutPortComp.ComponentInstance);
            Assert.IsNotNull(portWithOutPortComp.ComponentInstance.Component);
            Assert.AreEqual(portWithOutPortComp.ComponentInstance.Component.Name, this._PortOutComp);

            loadedBlock1.Ports.Remove(portWithOutPortComp);
            loadedBlock1.Ports.Remove(portWithInPortComp);



            Assert.IsNull(component.Components.FirstOrDefault(t => t.Component.Name == this._PortInComp));
            Assert.IsNull(component.Components.FirstOrDefault(t => t.Component.Name == this._PortOutComp));
        }

        [TestMethod]
        /// <summary>
        /// Deltes a comontnet which is  a port type and checks whether the asosicated ports are also deleted
        /// </summary>
        public void DeleteAssociatedComp()
        {

            this.LoadProject(simNetworkProject);
            var loadedNW = this.projectData.SimNetworks.FirstOrDefault(t => t.Name == this._name);
            Assert.IsNotNull(loadedNW);
            Assert.IsNotNull(loadedNW.ContainedElements);

            var loadedBlock1 = loadedNW.ContainedElements.FirstOrDefault(e => e.Name == this._bName1) as SimNetworkBlock;
            var component = this.projectData.Components.FirstOrDefault(t => t.Name == this._BlockCompPorts);

            loadedBlock1.AssignComponent(component, true);

            var portInComp = component.Components.FirstOrDefault(t => t.Component.Name == this._PortInComp);
            Assert.IsNotNull(portInComp);
            var portOutComp = component.Components.FirstOrDefault(t => t.Component.Name == this._PortOutComp);
            Assert.IsNotNull(portOutComp);

            Assert.IsNotNull(loadedBlock1.ComponentInstance);
            Assert.IsNotNull(loadedBlock1.ComponentInstance.Component);
            Assert.AreEqual(loadedBlock1.ComponentInstance.Component.Name, this._BlockCompPorts);


            //Check if ports are contianing the subcomponents
            var portWithInPortComp = loadedBlock1.Ports.FirstOrDefault(t => t.ComponentInstance != null && t.ComponentInstance.Component == portInComp.Component);
            Assert.IsNotNull(portWithInPortComp);
            Assert.IsNotNull(portWithInPortComp.ComponentInstance);
            Assert.IsNotNull(portWithInPortComp.ComponentInstance.Component);
            Assert.AreEqual(portWithInPortComp.ComponentInstance.Component.Name, this._PortInComp);

            var portWithOutPortComp = loadedBlock1.Ports.FirstOrDefault(t => t.ComponentInstance != null && t.ComponentInstance.Component == portOutComp.Component);
            Assert.IsNotNull(portWithOutPortComp);
            Assert.IsNotNull(portWithOutPortComp.ComponentInstance);
            Assert.IsNotNull(portWithOutPortComp.ComponentInstance.Component);
            Assert.AreEqual(portWithOutPortComp.ComponentInstance.Component.Name, this._PortOutComp);


            this.projectData.Components.Remove(component);

            Assert.IsNull(loadedBlock1.ComponentInstance);
            Assert.IsTrue(loadedBlock1.Ports.All(p => p.ComponentInstance == null));

        }



    }
}
