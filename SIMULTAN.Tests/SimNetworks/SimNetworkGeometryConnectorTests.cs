using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data.Components;
using SIMULTAN.Data.Geometry;
using SIMULTAN.Data.SimNetworks;
using SIMULTAN.Data.Taxonomy;
using SIMULTAN.Tests.TestUtils;
using System;
using System.IO;
using System.Linq;
using System.Windows.Media.Media3D;

namespace SIMULTAN.Tests.SimNetworks
{
    [TestClass]
    public class SimNetworkGeometryConnectorTests : BaseProjectTest
    {
        private static readonly FileInfo simNetworkProject = new FileInfo(@".\SimNetworkGeometryConnectorTest.simultan");
        private string _test_network = "TestSimNetwork";
        private string _static_block = "StaticBlock";
        private string _block1 = "Block1";
        private string _block2 = "Block2";
        private string _block3 = "Block3";


        //Automatically Created network for testing purposes
        private string auto_network = "GeomConnectorTestNetwork";
        private string auto_network_block1 = "block1";
        private string auto_network_block3 = "block3";
        private string empty_block2 = "empty_block2";
        private string empty_block1 = "empty_block1";


        private string auto_network_block1_comp = "block1_comp";
        private string auto_network_block2_comp = "block2_comp";
        private string auto_network_block3_comp = "block3_comp";


        private SimNetwork CreateTestNetwork()
        {
            this.LoadProject(simNetworkProject);
            var undefinedSlot = projectData.Taxonomies.GetDefaultSlot(SimDefaultSlotKeys.Undefined);



            //Creating components for the blocks
            var block1Comp = new SimComponent() { Name = auto_network_block1_comp, InstanceType = SimInstanceType.SimNetworkBlock, CurrentSlot = new SimTaxonomyEntryReference(undefinedSlot) };
            var block2Comp = new SimComponent() { Name = auto_network_block2_comp, InstanceType = SimInstanceType.SimNetworkBlock, CurrentSlot = new SimTaxonomyEntryReference(undefinedSlot) };
            var block3Comp = new SimComponent() { Name = auto_network_block3_comp, InstanceType = SimInstanceType.SimNetworkBlock, CurrentSlot = new SimTaxonomyEntryReference(undefinedSlot) };
            this.projectData.Components.Add(block1Comp);
            this.projectData.Components.Add(block2Comp);
            this.projectData.Components.Add(block3Comp);




            //Create Network
            var nw = new SimNetwork(auto_network);
            this.projectData.SimNetworks.Add(nw);



            // BLOCK 1, Static
            var block1 = new SimNetworkBlock(auto_network_block1, new System.Windows.Point(0, 0)); //Static
            var port11 = new SimNetworkPort(PortType.Output);
            var port12 = new SimNetworkPort(PortType.Output);
            block1.Ports.Add(port11);
            block1.Ports.Add(port12);
            nw.ContainedElements.Add(block1);
            var componentInstance1 = new SimComponentInstance(block1);
            foreach (var item in block1.Ports)
            {
                if (item.PortType == PortType.Input)
                {
                    var targetSlot = block1Comp.Components.FindAvailableSlot(block1Comp.CurrentSlot.Target);
                    var subComp = new SimComponent() { Name = auto_network_block1_comp, InstanceType = SimInstanceType.InPort, CurrentSlot = new SimTaxonomyEntryReference(undefinedSlot) };
                    var child = new SimChildComponentEntry(targetSlot, subComp);
                    block1Comp.Components.Add(child);
                }
                else
                {

                    var targetSlot = block1Comp.Components.FindAvailableSlot(block1Comp.CurrentSlot.Target);
                    var subComp = new SimComponent() { Name = auto_network_block1_comp, InstanceType = SimInstanceType.OutPort, CurrentSlot = new SimTaxonomyEntryReference(undefinedSlot) };
                    var child = new SimChildComponentEntry(targetSlot, subComp);
                    block1Comp.Components.Add(child);
                }

            }
            block1Comp.Instances.Add(componentInstance1);
            block1.ConvertToStatic();


            Assert.IsTrue(block1.Ports.Any(p => p.ComponentInstance != null
                            && p.ComponentInstance.Component.Parameters.Any(n => n.NameTaxonomyEntry.TaxonomyEntryReference.Target.Key == ReservedParameterKeys.SIMNW_STATIC_PORT_POSITION_X)
                            && p.ComponentInstance.Component.Parameters.Any(t => t.NameTaxonomyEntry.TaxonomyEntryReference.Target.Key == ReservedParameterKeys.SIMNW_STATIC_PORT_POSITION_Y)
                            && p.ComponentInstance.Component.Parameters.Any(k => k.NameTaxonomyEntry.TaxonomyEntryReference.Target.Key == ReservedParameterKeys.SIMNW_STATIC_PORT_POSITION_Z)));



            // BLOCK 2, Static
            var block2 = new SimNetworkBlock(auto_network_block1, new System.Windows.Point(0, 100)); //Static
            var port21 = new SimNetworkPort(PortType.Input);
            var port22 = new SimNetworkPort(PortType.Input);
            block2.Ports.Add(port21);
            block2.Ports.Add(port22);
            nw.ContainedElements.Add(block2);
            foreach (var item in block2.Ports)
            {
                if (item.PortType == PortType.Input)
                {
                    var targetSlot = block2Comp.Components.FindAvailableSlot(block2Comp.CurrentSlot.Target);
                    var subComp = new SimComponent() { Name = auto_network_block1_comp, InstanceType = SimInstanceType.InPort, CurrentSlot = new SimTaxonomyEntryReference(undefinedSlot) };
                    var child = new SimChildComponentEntry(targetSlot, subComp);
                    block2Comp.Components.Add(child);
                }
                else
                {

                    var targetSlot = block2Comp.Components.FindAvailableSlot(block2Comp.CurrentSlot.Target);
                    var subComp = new SimComponent() { Name = auto_network_block1_comp, InstanceType = SimInstanceType.OutPort, CurrentSlot = new SimTaxonomyEntryReference(undefinedSlot) };
                    var child = new SimChildComponentEntry(targetSlot, subComp);
                    block2Comp.Components.Add(child);
                }

            }
            var componentInstance2 = new SimComponentInstance(block2);
            block2Comp.Instances.Add(componentInstance2);
            block2.ConvertToStatic();


            Assert.IsTrue(block2.Ports.Any(p => p.ComponentInstance != null
                            && p.ComponentInstance.Component.Parameters.Any(n => n.NameTaxonomyEntry.TaxonomyEntryReference.Target.Key == ReservedParameterKeys.SIMNW_STATIC_PORT_POSITION_X)
                            && p.ComponentInstance.Component.Parameters.Any(t => t.NameTaxonomyEntry.TaxonomyEntryReference.Target.Key == ReservedParameterKeys.SIMNW_STATIC_PORT_POSITION_Y)
                            && p.ComponentInstance.Component.Parameters.Any(k => k.NameTaxonomyEntry.TaxonomyEntryReference.Target.Key == ReservedParameterKeys.SIMNW_STATIC_PORT_POSITION_Z)));


            // BLOCK 3 Dynamic
            var block3 = new SimNetworkBlock(auto_network_block3, new System.Windows.Point(100, 100)); //Dynamic

            var port31 = new SimNetworkPort(PortType.Output);
            var port32 = new SimNetworkPort(PortType.Input);
            block3.Ports.Add(port31);
            block3.Ports.Add(port32);


            nw.ContainedElements.Add(block3);

            foreach (var item in block3.Ports)
            {
                if (item.PortType == PortType.Input)
                {
                    var targetSlot = block3Comp.Components.FindAvailableSlot(block3Comp.CurrentSlot.Target);
                    var subComp = new SimComponent() { Name = auto_network_block1_comp, InstanceType = SimInstanceType.InPort, CurrentSlot = new SimTaxonomyEntryReference(undefinedSlot) };
                    var child = new SimChildComponentEntry(targetSlot, subComp);
                    block3Comp.Components.Add(child);
                }
                else
                {

                    var targetSlot = block3Comp.Components.FindAvailableSlot(block3Comp.CurrentSlot.Target);
                    var subComp = new SimComponent() { Name = auto_network_block1_comp, InstanceType = SimInstanceType.OutPort, CurrentSlot = new SimTaxonomyEntryReference(undefinedSlot) };
                    var child = new SimChildComponentEntry(targetSlot, subComp);
                    block3Comp.Components.Add(child);
                }

            }

            var componentInstance3 = new SimComponentInstance(block3);
            block3Comp.Instances.Add(componentInstance3);

            //Connect the static Blocks
            port11.ConnectTo(port21);
            //port12.ConnectTo(port22);
            port31.ConnectTo(port22);

            // BLOCK 4  Dynamic
            var block4 = new SimNetworkBlock(empty_block1, new System.Windows.Point(100, 100)); //Dynamic

            var port41 = new SimNetworkPort(PortType.Output);
            var port42 = new SimNetworkPort(PortType.Input);
            block4.Ports.Add(port41);
            block4.Ports.Add(port42);
            nw.ContainedElements.Add(block4);



            // BLOCK 5  Dynamic
            var block5 = new SimNetworkBlock(empty_block2, new System.Windows.Point(100, 100)); //Dynamic

            var port51 = new SimNetworkPort(PortType.Output);
            var port52 = new SimNetworkPort(PortType.Input);
            block5.Ports.Add(port51);
            block5.Ports.Add(port52);
            nw.ContainedElements.Add(block5);




            return nw;
        }


        [TestMethod]
        [DoNotParallelize]
        public void ConvertSimNetworkToGeometryTest()
        {

            this.LoadProject(simNetworkProject);

            var loadedNW = this.projectData.SimNetworks.FirstOrDefault(t => t.Name == this._test_network);

            //Query the blocks form the SimNetwork (for later comparison)
            var block1 = loadedNW.ContainedElements.FirstOrDefault(t => t.Name == _block1);
            Assert.IsNotNull(block1);
            var block2 = loadedNW.ContainedElements.FirstOrDefault(t => t.Name == _block2);
            Assert.IsNotNull(block2);
            var block3 = loadedNW.ContainedElements.FirstOrDefault(t => t.Name == _block3);
            Assert.IsNotNull(block3);
            var staticBlock = loadedNW.ContainedElements.FirstOrDefault(t => t.Name == _static_block);
            Assert.IsNotNull(staticBlock);
            var networkFileName = Guid.NewGuid() + ".simgeo";
            var networkFile = new FileInfo(@".\" + networkFileName);


            //Create new geometry file
            if (File.Exists(@".\" + networkFileName))
            {
                File.Delete(@".\" + networkFileName);
            }
            var geometryModel = this.projectData.ComponentGeometryExchange.ConvertSimNetwork(loadedNW, networkFile);


            //Check if all the blocks got geometry assigned
            var gBlock1 = geometryModel.Geometry.Vertices.First(c => c.Name == _block1);
            Assert.IsNotNull(gBlock1);
            var gBlock2 = geometryModel.Geometry.Vertices.First(c => c.Name == _block2);
            Assert.IsNotNull(gBlock2);
            var gBlock3 = geometryModel.Geometry.Vertices.First(c => c.Name == _block3);
            Assert.IsNotNull(gBlock3);
            var gStaticBlock = geometryModel.Geometry.Vertices.First(c => c.Name == _static_block);
            Assert.IsNotNull(gStaticBlock);

            //Delete file
            networkFile.Delete();
            var resource = this.projectData.AssetManager.Resources.FirstOrDefault(t => t.Name == networkFile.Name);
            this.project.DeleteResource(resource);
            Assert.IsTrue(loadedNW.IndexOfGeometricRepFile == -1);

        }

        [TestMethod]
        [DoNotParallelize]
        public void DeleteGeometryOfSimNetwork()
        {
            this.LoadProject(simNetworkProject);

            var loadedNW = this.projectData.SimNetworks.FirstOrDefault(t => t.Name == this._test_network);

            //Query the blocks form the SimNetwork (for later comparison)
            var block1 = loadedNW.ContainedElements.FirstOrDefault(t => t.Name == _block1);
            Assert.IsNotNull(block1);
            var block2 = loadedNW.ContainedElements.FirstOrDefault(t => t.Name == _block2);
            Assert.IsNotNull(block2);
            var block3 = loadedNW.ContainedElements.FirstOrDefault(t => t.Name == _block3);
            Assert.IsNotNull(block3);
            var staticBlock = loadedNW.ContainedElements.FirstOrDefault(t => t.Name == _static_block);
            Assert.IsNotNull(staticBlock);

            var networkFileName = Guid.NewGuid() + ".simgeo";
            var networkFile = new FileInfo(@".\" + networkFileName);


            //Create new geometry file
            if (File.Exists(@".\" + networkFileName))
            {
                File.Delete(@".\" + networkFileName);
            }
            var geometryModel = this.projectData.ComponentGeometryExchange.ConvertSimNetwork(loadedNW, networkFile);


            //Delete file
            var resource = this.projectData.AssetManager.Resources.FirstOrDefault(t => t.Name == networkFile.Name);
            this.project.DeleteResource(resource);
            Assert.IsTrue(loadedNW.IndexOfGeometricRepFile == -1);

            //Delete file
            networkFile.Delete();

        }


        [TestMethod]
        [DoNotParallelize]
        public void ConvertStaticNodeToGeometry()
        {
            this.LoadProject(simNetworkProject);

            var loadedNW = this.projectData.SimNetworks.FirstOrDefault(t => t.Name == this._test_network);

            //Query the blocks form the SimNetwork (for later comparison)
            var staticBlock = loadedNW.ContainedElements.FirstOrDefault(t => t.Name == _static_block);
            Assert.IsNotNull(staticBlock);

            Assert.IsTrue(staticBlock is SimNetworkBlock);
            Assert.IsNotNull(((SimNetworkBlock)staticBlock).ComponentInstance);

            var inports = ((SimNetworkBlock)staticBlock).ComponentInstance.Component.Components.Select(ch => ch.Component).Where(c => c.InstanceType == Data.Components.SimInstanceType.InPort);
            var outports = ((SimNetworkBlock)staticBlock).ComponentInstance.Component.Components.Select(ch => ch.Component).Where(c => c.InstanceType == Data.Components.SimInstanceType.OutPort);



            Assert.IsTrue(inports.All(t => t.Parameters.Any(p => p.NameTaxonomyEntry.TaxonomyEntryReference.Target.Key == ReservedParameterKeys.SIMNW_STATIC_PORT_POSITION_X)));
            Assert.IsTrue(inports.All(t => t.Parameters.Any(p => p.NameTaxonomyEntry.TaxonomyEntryReference.Target.Key == ReservedParameterKeys.SIMNW_STATIC_PORT_POSITION_Y)));
            Assert.IsTrue(inports.All(t => t.Parameters.Any(p => p.NameTaxonomyEntry.TaxonomyEntryReference.Target.Key == ReservedParameterKeys.SIMNW_STATIC_PORT_POSITION_Z)));

            Assert.IsTrue(outports.All(t => t.Parameters.Any(p => p.NameTaxonomyEntry.TaxonomyEntryReference.Target.Key == ReservedParameterKeys.SIMNW_STATIC_PORT_POSITION_X)));
            Assert.IsTrue(outports.All(t => t.Parameters.Any(p => p.NameTaxonomyEntry.TaxonomyEntryReference.Target.Key == ReservedParameterKeys.SIMNW_STATIC_PORT_POSITION_Y)));
            Assert.IsTrue(outports.All(t => t.Parameters.Any(p => p.NameTaxonomyEntry.TaxonomyEntryReference.Target.Key == ReservedParameterKeys.SIMNW_STATIC_PORT_POSITION_Z)));



            var networkFileName = Guid.NewGuid() + ".simgeo";
            var networkFile = new FileInfo(@".\" + networkFileName);


            //Create new geometry file
            if (File.Exists(@".\" + networkFileName))
            {
                File.Delete(@".\" + networkFileName);
            }
            var geometryModel = this.projectData.ComponentGeometryExchange.ConvertSimNetwork(loadedNW, networkFile);


            var gStaticBlock = geometryModel.Geometry.Vertices.First(c => c.Name == _static_block);
            Assert.IsNotNull(gStaticBlock);

            //TODO: CHeck the position


            //Delete file
            var resource = this.projectData.AssetManager.Resources.FirstOrDefault(t => t.Name == networkFile.Name);
            this.project.DeleteResource(resource);
            Assert.IsTrue(loadedNW.IndexOfGeometricRepFile == -1);
            networkFile.Delete();

        }

        //Check connectors
        [TestMethod]
        [DoNotParallelize]
        public void CheckConnectorsConnectedToTheCorrectPorts()
        {
            this.LoadProject(simNetworkProject);

            var loadedNW = this.projectData.SimNetworks.FirstOrDefault(t => t.Name == this._test_network);

            //Query the blocks form the SimNetwork (for later comparison)
            var staticBlock = loadedNW.ContainedElements.FirstOrDefault(t => t.Name == _static_block);
            Assert.IsNotNull(staticBlock);

            Assert.IsTrue(staticBlock is SimNetworkBlock);
            Assert.IsNotNull(((SimNetworkBlock)staticBlock).ComponentInstance);

            //Query the blocks form the SimNetwork (for later comparison)
            var block1 = loadedNW.ContainedElements.FirstOrDefault(t => t.Name == _block1);
            Assert.IsNotNull(block1);
            var block2 = loadedNW.ContainedElements.FirstOrDefault(t => t.Name == _block2);
            Assert.IsNotNull(block2);
            var block3 = loadedNW.ContainedElements.FirstOrDefault(t => t.Name == _block3);
            Assert.IsNotNull(block3);

            var block1PortToBlock2 = block1.Ports.Where(t => t.PortType == PortType.Output && t.Connectors.Any(c => block2.Ports.Contains(c.Target))).FirstOrDefault();
            Assert.IsNotNull(block1PortToBlock2);
            var block1PortToBlock2Connector = block1PortToBlock2.Connectors.FirstOrDefault();

            var networkFile = new FileInfo(@".\SimNetworkGeo.simgeo");
            //Create new geometry file
            if (File.Exists(@".\SimNetworkGeo.simgeo"))
            {
                File.Delete(@".\SimNetworkGeo.simgeo");
            }

            var geometryModel = this.projectData.ComponentGeometryExchange.ConvertSimNetwork(loadedNW, networkFile);
            var geometryConnector = this.projectData.ComponentGeometryExchange.SimNetworkModelConnectors.FirstOrDefault(t => t.Value.Network == loadedNW);
            Assert.IsNotNull(geometryConnector);
            networkFile.Delete();
        }





        //Check Removing Block from the network removes the vertex and connecting edges
        [TestMethod]
        [DoNotParallelize]
        public void CheckStaticBlockToDynamicBlockConnectorPosition()
        {
            this.LoadProject(simNetworkProject);
            var network = this.CreateTestNetwork();
            var networkFileName = Guid.NewGuid() + ".simgeo";
            var networkFile = new FileInfo(@".\" + networkFileName);


            //Create new geometry file
            if (File.Exists(@".\" + networkFileName))
            {
                File.Delete(@".\" + networkFileName);
            }

            var geometryModel = this.projectData.ComponentGeometryExchange.ConvertSimNetwork(network, networkFile);
            var dynamicBlock = network.ContainedElements.FirstOrDefault(t => t.Name == this.auto_network_block3);
            var dynamicToStaticConnector = network.ContainedConnectors.FirstOrDefault(c => ((SimNetworkBlock)c.Target.ParentNetworkElement).IsStatic && !((SimNetworkBlock)c.Source.ParentNetworkElement).IsStatic);
            var staticPort = dynamicToStaticConnector.Target;

            var connectorGeom = geometryModel.Geometry.GeometryFromId(dynamicToStaticConnector.RepresentationReference.GeometryId) as Vertex;
            Assert.IsNotNull(connectorGeom);
            var staticPortGeom = geometryModel.Geometry.GeometryFromId(staticPort.RepresentationReference.GeometryId) as Vertex;
            Assert.IsNotNull(connectorGeom);


            Assert.AreEqual(connectorGeom.Position, staticPortGeom.Position);
            networkFile.Delete();
        }


        //Check Removing Block from the network removes the vertex and connecting edges
        [TestMethod]
        [DoNotParallelize]
        public void CheckConnectorsForStaticToStaticConnector()
        {

            var network = CreateTestNetwork();

            var networkFileName = Guid.NewGuid() + ".simgeo";
            var networkFile = new FileInfo(@".\" + networkFileName);


            //Create new geometry file
            if (File.Exists(@".\" + networkFileName))
            {
                File.Delete(@".\" + networkFileName);
            }
            var geometryModel = this.projectData.ComponentGeometryExchange.ConvertSimNetwork(network, networkFile);
            var geometryConnector = this.projectData.ComponentGeometryExchange.SimNetworkModelConnectors.FirstOrDefault(t => t.Value.Network == network);

            var staticConnector = network.ContainedConnectors.FirstOrDefault(t => ((SimNetworkBlock)t.Target.ParentNetworkElement).IsStatic && ((SimNetworkBlock)t.Source.ParentNetworkElement).IsStatic);



            var targetPortGeom = geometryModel.Geometry.GeometryFromId(staticConnector.Target.RepresentationReference.GeometryId) as Vertex;
            var sourePortGeom = geometryModel.Geometry.GeometryFromId(staticConnector.Source.RepresentationReference.GeometryId) as Vertex;
            var connectorGeom = geometryModel.Geometry.GeometryFromId(staticConnector.RepresentationReference.GeometryId) as Vertex;

            Assert.IsNotNull(targetPortGeom);
            Assert.IsNotNull(sourePortGeom);
            Assert.IsNotNull(connectorGeom);


            Assert.AreEqual<Point3D>(targetPortGeom.Position, sourePortGeom.Position);
            Assert.AreEqual<Point3D>(targetPortGeom.Position, connectorGeom.Position);



            Assert.IsNotNull(geometryConnector);
            networkFile.Delete();
        }


        //Check Removing Block from the network removes the vertex and connecting edges
        [TestMethod]
        [DoNotParallelize]
        public void GeomUpdateRemoveBlockFromNetwork()
        {
            var network = CreateTestNetwork();

            var networkFileName = Guid.NewGuid() + ".simgeo";
            var networkFile = new FileInfo(@".\" + networkFileName);


            //Create new geometry file
            if (File.Exists(@".\" + networkFileName))
            {
                File.Delete(@".\" + networkFileName);
            }
            var geometryModel = this.projectData.ComponentGeometryExchange.ConvertSimNetwork(network, networkFile);
            var geometryConnector = this.projectData.ComponentGeometryExchange.SimNetworkModelConnectors.FirstOrDefault(t => t.Value.Network == network);

            var blockToRemove = network.ContainedElements.FirstOrDefault(t => t.Name == this.auto_network_block1);

            network.ContainedElements.Remove(blockToRemove);
            var geometry = geometryModel.Geometry.GeometryFromId(blockToRemove.RepresentationReference.GeometryId);

            Assert.IsNull(geometry);

            foreach (var item in blockToRemove.Ports)
            {
                var portGeo = geometryModel.Geometry.GeometryFromId(item.RepresentationReference.GeometryId);
                Assert.IsNull(portGeo);
            }
            networkFile.Delete();
        }


        //Checks whether the static block´s relative position conforms to the relative position values in the port component
        [TestMethod]
        [DoNotParallelize]
        public void CheckStaticPortRelPosition()
        {
            var network = CreateTestNetwork();

            var networkFileName = Guid.NewGuid() + ".simgeo";
            var networkFile = new FileInfo(@".\" + networkFileName);


            //Create new geometry file
            if (File.Exists(@".\" + networkFileName))
            {
                File.Delete(@".\" + networkFileName);
            }
            var geometryModel = this.projectData.ComponentGeometryExchange.ConvertSimNetwork(network, networkFile);
            var geometryConnector = this.projectData.ComponentGeometryExchange.SimNetworkModelConnectors.FirstOrDefault(t => t.Value.Network == network);

            var blockToUpdate = network.ContainedElements.FirstOrDefault(t => t.Name == this.auto_network_block1);
            var portToUpdate = blockToUpdate.Ports.FirstOrDefault();
            var blockGeo = geometryModel.Geometry.GeometryFromId(blockToUpdate.RepresentationReference.GeometryId) as Vertex;
            var portGeo = geometryModel.Geometry.GeometryFromId(portToUpdate.RepresentationReference.GeometryId) as Vertex;
            var relPosition = portGeo.Position - blockGeo.Position;
            var oX = portToUpdate.ComponentInstance.Component.Parameters.OfType<SimDoubleParameter>().FirstOrDefault(t => t.NameTaxonomyEntry.TaxonomyEntryReference.Target.Key == ReservedParameterKeys.SIMNW_STATIC_PORT_POSITION_X).Value;
            var oY = portToUpdate.ComponentInstance.Component.Parameters.OfType<SimDoubleParameter>().FirstOrDefault(t => t.NameTaxonomyEntry.TaxonomyEntryReference.Target.Key == ReservedParameterKeys.SIMNW_STATIC_PORT_POSITION_Y).Value;
            var oZ = portToUpdate.ComponentInstance.Component.Parameters.OfType<SimDoubleParameter>().FirstOrDefault(t => t.NameTaxonomyEntry.TaxonomyEntryReference.Target.Key == ReservedParameterKeys.SIMNW_STATIC_PORT_POSITION_Z).Value;

            Assert.AreEqual(relPosition.X, oX);
            Assert.AreEqual(relPosition.Y, oY);
            Assert.AreEqual(relPosition.Z, oZ);


            networkFile.Delete();
        }


        //Check updating a static block´s port´s relative position if it updates the geometry
        [TestMethod]
        [DoNotParallelize]
        public void GeomUpdateEditStaticBlockPortValue()
        {
            var network = CreateTestNetwork();

            var networkFileName = Guid.NewGuid() + ".simgeo";
            var networkFile = new FileInfo(@".\" + networkFileName);


            //Create new geometry file
            if (File.Exists(@".\" + networkFileName))
            {
                File.Delete(@".\" + networkFileName);
            }
            var geometryModel = this.projectData.ComponentGeometryExchange.ConvertSimNetwork(network, networkFile);
            var geometryConnector = this.projectData.ComponentGeometryExchange.SimNetworkModelConnectors.FirstOrDefault(t => t.Value.Network == network);

            var blockToUpdate = network.ContainedElements.FirstOrDefault(t => t.Name == this.auto_network_block1);
            var portToUpdate = blockToUpdate.Ports.FirstOrDefault();
            var blockGeo = geometryModel.Geometry.GeometryFromId(blockToUpdate.RepresentationReference.GeometryId) as Vertex;
            var portGeo = geometryModel.Geometry.GeometryFromId(portToUpdate.RepresentationReference.GeometryId) as Vertex;
            var relPosition = portGeo.Position - blockGeo.Position;
            var oX = portToUpdate.ComponentInstance.Component.Parameters.OfType<SimDoubleParameter>().FirstOrDefault(t => t.NameTaxonomyEntry.TaxonomyEntryReference.Target.Key == ReservedParameterKeys.SIMNW_STATIC_PORT_POSITION_X);
            var oY = portToUpdate.ComponentInstance.Component.Parameters.OfType<SimDoubleParameter>().FirstOrDefault(t => t.NameTaxonomyEntry.TaxonomyEntryReference.Target.Key == ReservedParameterKeys.SIMNW_STATIC_PORT_POSITION_Y);
            var oZ = portToUpdate.ComponentInstance.Component.Parameters.OfType<SimDoubleParameter>().FirstOrDefault(t => t.NameTaxonomyEntry.TaxonomyEntryReference.Target.Key == ReservedParameterKeys.SIMNW_STATIC_PORT_POSITION_Z);

            Assert.AreEqual(relPosition.X, oX.Value);
            Assert.AreEqual(relPosition.Y, oY.Value);
            Assert.AreEqual(relPosition.Z, oZ.Value);

            //Change oZ
            oX.Value = 4;
            var newPortGeo = geometryModel.Geometry.GeometryFromId(portToUpdate.RepresentationReference.GeometryId) as Vertex;
            relPosition = newPortGeo.Position - blockGeo.Position;

            Assert.AreEqual(relPosition.X, oX.Value);
            Assert.AreEqual(relPosition.Y, oY.Value);
            Assert.AreEqual(relPosition.Z, oZ.Value);

            networkFile.Delete();
        }


        //Check Removing Block from the network removes the vertex and connecting edges
        [TestMethod]
        [DoNotParallelize]
        public void GeomUpdateConnectPortsInNetworkWhileHavingGeometry()
        {
            var network = CreateTestNetwork();

            var networkFileName = Guid.NewGuid() + ".simgeo";
            var networkFile = new FileInfo(@".\" + networkFileName);


            //Create new geometry file
            if (File.Exists(@".\" + networkFileName))
            {
                File.Delete(@".\" + networkFileName);
            }
            var geometryModel = this.projectData.ComponentGeometryExchange.ConvertSimNetwork(network, networkFile);
            var geometryConnector = this.projectData.ComponentGeometryExchange.SimNetworkModelConnectors.FirstOrDefault(t => t.Value.Network == network);
            var blockToUpdate = network.ContainedElements.FirstOrDefault(t => t.Name == this.auto_network_block1);

            var portToRemove = blockToUpdate.Ports.FirstOrDefault();
            portToRemove.ParentNetworkElement.Ports.Remove(portToRemove);
            var portGeom = geometryModel.Geometry.GeometryFromId(portToRemove.RepresentationReference.GeometryId);
            Assert.IsNull(portGeom);


            networkFile.Delete();

        }


        /// <summary>
        /// Whenever to static blocks are connected, if the connection can not be satisfied, then the connector is represented by a polyline. 
        /// This function checks whether it happens on updating the rel position of one of the ports in the connection
        /// </summary>
        [TestMethod]
        [DoNotParallelize]
        public void CheckChangeGeomRepresentationOfConnector()
        {
            var network = CreateTestNetwork();
            var networkFileName = Guid.NewGuid() + ".simgeo";
            var networkFile = new FileInfo(@".\" + networkFileName);


            //Create new geometry file
            if (File.Exists(@".\" + networkFileName))
            {
                File.Delete(@".\" + networkFileName);
            }
            var geometryModel = this.projectData.ComponentGeometryExchange.ConvertSimNetwork(network, networkFile);
            var geometryConnector = this.projectData.ComponentGeometryExchange.SimNetworkModelConnectors.FirstOrDefault(t => t.Value.Network == network);

            var blockToUpdate = network.ContainedElements.FirstOrDefault(t => t.Name == this.auto_network_block1);
            var portToUpdate = blockToUpdate.Ports.FirstOrDefault();

            var connector = portToUpdate.Connectors.FirstOrDefault();
            var connectorGe = geometryModel.Geometry.GeometryFromId(connector.RepresentationReference.GeometryId);
            //  Assert.IsNotNull(connectorGe);
            // Assert.IsTrue(connectorGe is Vertex);

            var oZ = portToUpdate.ComponentInstance.Component.Parameters.OfType<SimDoubleParameter>()
               .FirstOrDefault(t => t.NameTaxonomyEntry.TaxonomyEntryReference.Target.Key == ReservedParameterKeys.SIMNW_STATIC_PORT_POSITION_Z);



            //Change oZ
            oZ.Value = 4; // this should make the connection invalid


            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.WaitForFullGCComplete();
            GC.Collect();

            var updateGeo = geometryModel.Geometry.GeometryFromId(connector.RepresentationReference.GeometryId);
            Assert.IsNotNull(updateGeo);
            Assert.IsTrue(updateGeo is Polyline);

            networkFile.Delete();
        }




        /// <summary>
        /// Checks whether the geometry us updated correctly upon connecting two ports when geometry is present
        /// </summary>
        [TestMethod]
        [DoNotParallelize]
        public void ConnectPortsWhenGeometryIsPresent()
        {
            var network = CreateTestNetwork();
            var networkFileName = Guid.NewGuid() + ".simgeo";
            var networkFile = new FileInfo(@".\" + networkFileName);


            //Create new geometry file
            if (File.Exists(@".\" + networkFileName))
            {
                File.Delete(@".\" + networkFileName);
            }
            var geometryModel = this.projectData.ComponentGeometryExchange.ConvertSimNetwork(network, networkFile);
            var geometryConnector = this.projectData.ComponentGeometryExchange.SimNetworkModelConnectors.FirstOrDefault(t => t.Value.Network == network);



            var portOut = network.ContainedElements.FirstOrDefault(t => t.Name == this.empty_block1).Ports.FirstOrDefault(t => t.PortType == PortType.Output);
            var portIn = network.ContainedElements.FirstOrDefault(t => t.Name == this.empty_block2).Ports.FirstOrDefault(t => t.PortType == PortType.Input);

            Assert.AreEqual(portOut.Connectors.Count, 0);
            Assert.AreEqual(portIn.Connectors.Count, 0);

            var oldPort1Geo = geometryModel.Geometry.GeometryFromId(portOut.RepresentationReference.GeometryId);
            var oldPort2Geo = geometryModel.Geometry.GeometryFromId(portIn.RepresentationReference.GeometryId);

            Assert.IsNotNull(oldPort1Geo);
            Assert.IsNotNull(oldPort2Geo);


            //Connect the ports
            portOut.ConnectTo(portIn);


            var newPort1Geo = geometryModel.Geometry.GeometryFromId(portOut.RepresentationReference.GeometryId);
            var newPort2Geo = geometryModel.Geometry.GeometryFromId(portIn.RepresentationReference.GeometryId);

            Assert.IsNotNull(newPort1Geo);
            Assert.IsNotNull(newPort2Geo);




            Assert.IsTrue(!geometryModel.Geometry.ContainsGeometry(oldPort1Geo));
            Assert.IsTrue(!geometryModel.Geometry.ContainsGeometry(oldPort2Geo));

            networkFile.Delete();
        }

    }
}
