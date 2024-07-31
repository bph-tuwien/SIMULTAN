using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using SIMULTAN.Data.Components;
using SIMULTAN.Data.SimMath;
using SIMULTAN.Data.SimNetworks;
using SIMULTAN.Data.Taxonomy;
using SIMULTAN.Serializer.DXF;
using SIMULTAN.Serializer.JSON;
using SIMULTAN.Tests.TestUtils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;



namespace SIMULTAN.Tests.Serializer
{
    [TestClass]
    public class JSONExporterTests : BaseProjectTest
    {
        private static readonly FileInfo testProject = new FileInfo(@"./JSONExporterTestFile.simultan");

        [TestMethod]
        public void TestWholeProjectExport()
        {
            LoadProject(testProject);
            var jsonPath = @".\" + Guid.NewGuid().ToString() + ".json";

            JSONExporter.Export(this.projectData, new FileInfo(jsonPath));
            using (StreamReader r = new StreamReader(jsonPath))
            {
                string json = r.ReadToEnd();
                var deserialized = JsonConvert.DeserializeObject<dynamic>(json);

                Assert.AreEqual(deserialized.components.Count, projectData.Components.Count);
                Assert.AreEqual(deserialized.simNetworks.Count, projectData.SimNetworks.Count);

            }
            // Delete the file
            File.Delete(jsonPath);
        }

        [TestMethod]
        public void TestNonSetEnumParamExportWithInstance()
        {
            LoadProject(testProject);
            var jsonPath = @".\" + Guid.NewGuid().ToString() + ".json";
            var comp = new SimComponent()
            {
                InstanceType = SimInstanceType.SimNetworkBlock,
                Name = "TestCompForEmptyEnum",
            };
            var testTaxonomy = new SimTaxonomy("BaseTax");
            this.projectData.Taxonomies.Add(testTaxonomy);
            var testBaseTaxEntry = new SimTaxonomyEntry("Base", "Base");
            testTaxonomy.Entries.Add(testBaseTaxEntry);
            var child1 = new SimTaxonomyEntry("Child1", "Child1");
            var child2 = new SimTaxonomyEntry("child2", "child2");
            testBaseTaxEntry.Children.Add(child1);
            testBaseTaxEntry.Children.Add(child2);

            var ent = new SimTaxonomyEntry("KEY", "ENTRYVALUE");
            testTaxonomy.Entries.Add(ent);
            comp.Slots.Add(new SimTaxonomyEntryReference(ent));
            this.projectData.Components.Add(comp);

            var enumparam = new SimEnumParameter("TestEnumParam", testBaseTaxEntry); //Do not set the Value
            comp.Parameters.Add(enumparam);

            var network = new SimNetwork("test", new SimPoint(0, 0));
            this.projectData.SimNetworks.Add(network);
            var block = new SimNetworkBlock("testBlock", new SimPoint(0, 0));
            network.ContainedElements.Add(block);
            var instance = new SimComponentInstance(block);
            comp.Instances.Add(instance);

            Assert.AreEqual(instance.InstanceParameterValuesPersistent[enumparam], null);

            JSONExporter.ExportNetworks(this.projectData, new List<SimNetwork> { network }, new FileInfo(jsonPath));


            using (StreamReader r = new StreamReader(jsonPath))
            {
                string json = r.ReadToEnd();
                var deserialized = JsonConvert.DeserializeObject<dynamic>(json);
                var rComp = projectData.Components.First(t => t.Name == "TestCompForEmptyEnum");
                var param = rComp.Parameters.First(t => t.NameTaxonomyEntry.Text == "TestEnumParam");
                var enumParam = param as SimEnumParameter;
                Assert.AreEqual(enumParam.Value, null);

                Assert.AreEqual(rComp.Instances.FirstOrDefault().InstanceParameterValuesPersistent[enumParam], null);
            }
            // Delete the file
            File.Delete(jsonPath);
        }




        [TestMethod]
        public void TestExportingOnlyOneNetwork()
        {
            LoadProject(testProject);
            var jsonPath = @".\" + Guid.NewGuid().ToString() + ".json";

            var networksToExport = projectData.SimNetworks.First();
            JSONExporter.ExportNetworks(this.projectData, new List<SimNetwork> { networksToExport }, new FileInfo(jsonPath));
            using (StreamReader r = new StreamReader(jsonPath))
            {
                string json = r.ReadToEnd();
                var deserialized = JsonConvert.DeserializeObject<dynamic>(json);

                Assert.AreNotEqual(deserialized.simNetworks.Count, projectData.SimNetworks.Count);

            }
            // Delete the file
            File.Delete(jsonPath);
        }



        private List<SimComponent> GetTheComponents(SimNetwork network, List<SimComponent> comps)
        {
            foreach (var item in network.ContainedElements)
            {
                if (item is IElementWithComponent eWComp)
                {
                    if (eWComp.ComponentInstance != null && !comps.Contains(eWComp.ComponentInstance.Component))
                    {
                        comps.Add(eWComp.ComponentInstance.Component);
                    }
                }
                if (item is SimNetwork nw)
                {
                    comps.AddRange(GetTheComponents(network, comps));
                }
            }
            return comps;
        }


        [TestMethod]
        public void TestOneNetworkWithComponents()
        {
            LoadProject(testProject);
            var jsonPath = @".\" + Guid.NewGuid().ToString() + ".json";

            var networkToExport = projectData.SimNetworks.First(t => t.ContainedElements.Any(p => p is SimNetworkBlock block && block.ComponentInstance != null));
            var blockWithComponent = networkToExport.ContainedElements.FirstOrDefault(b => b is SimNetworkBlock block && block.ComponentInstance != null);

            var numberOfComponentsInNetwork = GetTheComponents(networkToExport, new List<SimComponent>());

            JSONExporter.ExportNetworks(this.projectData, new List<SimNetwork> { networkToExport }, new FileInfo(jsonPath));
            using (StreamReader r = new StreamReader(jsonPath))
            {
                string json = r.ReadToEnd();
                var deserialized = JsonConvert.DeserializeObject<dynamic>(json);

                Assert.AreNotEqual(deserialized.simNetworks.Count, projectData.SimNetworks.Count);
                Assert.AreEqual(deserialized.simNetworks[0].name.ToString(), networkToExport.Name);

                for (int i = 0; i < deserialized.simNetworks[0].blocks.Count; i++)
                {
                    var block = networkToExport.ContainedElements.OfType<SimNetworkBlock>().FirstOrDefault(t => t.Name == deserialized.simNetworks[0].blocks[i].name.ToString());
                    Assert.IsNotNull(block);
                }

                Assert.AreEqual(numberOfComponentsInNetwork.Count, deserialized.components.Count);

            }
            // Delete the file
            File.Delete(jsonPath);
        }



        #region Parameters

        [TestMethod]
        public void TestDoubleSerializable()
        {
            var param = new SimDoubleParameter("Parameter Distance", "km", 10.23)
            {
                ValueMin = -10.0,
                ValueMax = double.MaxValue
            };
            var serParam = new SimDoubleParameterSerializable(param);

            Assert.AreEqual(param.NameTaxonomyEntry.Text, serParam.Name.Text);
            Assert.IsNull(serParam.Name.TaxonomyEntry);
            Assert.AreEqual(param.Description, serParam.Description);
            Assert.AreEqual(param.Unit, serParam.Unit);
            Assert.AreEqual(DXFDataConverter<double>.P.ToDXFString(param.ValueMin), serParam.ValueMin);
            Assert.AreEqual(DXFDataConverter<double>.P.ToDXFString(param.ValueMax), serParam.ValueMax);
            Assert.AreEqual(DXFDataConverter<double>.P.ToDXFString(param.Value), serParam.Value);
        }
        [TestMethod]
        public void TestStringSerializable()
        {
            var param = new SimStringParameter("Parameter Distance", "10.23")
            {
                Description = "This is a description"
            };
            var serParam = new SimStringParameterSerializable(param);

            Assert.AreEqual(param.NameTaxonomyEntry.Text, serParam.Name.Text);
            Assert.IsNull(serParam.Name.TaxonomyEntry);
            Assert.AreEqual(param.Description, serParam.Description);
            Assert.AreEqual(DXFDataConverter<string>.P.ToDXFString(param.Value), serParam.Value);
        }
        [TestMethod]
        public void TestIntegerSerializable()
        {
            var param = new SimIntegerParameter("Parameter Distance", "km", 10)
            {
                ValueMin = -10,
                ValueMax = int.MaxValue
            };
            var serParam = new SimIntegerParameterSerializable(param);

            Assert.AreEqual(param.NameTaxonomyEntry.Text, serParam.Name.Text);
            Assert.IsNull(serParam.Name.TaxonomyEntry);
            Assert.AreEqual(param.Description, serParam.Description);
            Assert.AreEqual(param.Unit, serParam.Unit);
            Assert.AreEqual(DXFDataConverter<int>.P.ToDXFString(param.ValueMin), serParam.ValueMin);
            Assert.AreEqual(DXFDataConverter<int>.P.ToDXFString(param.ValueMax), serParam.ValueMax);
            Assert.AreEqual(DXFDataConverter<int>.P.ToDXFString(param.Value), serParam.Value);
        }


        [TestMethod]
        public void TestBoolSerializble()
        {
            var param = new SimBoolParameter("Parameter Distance", true)
            {
                Description = "Description"
            };
            var serParam = new SimBoolParameterSerializable(param);


            Assert.AreEqual(param.NameTaxonomyEntry.Text, serParam.Name.Text);
            Assert.IsNull(serParam.Name.TaxonomyEntry);
            Assert.AreEqual(param.Description, serParam.Description);
            Assert.AreEqual(DXFDataConverter<bool>.P.ToDXFString(param.Value), serParam.Value);
        }

        [TestMethod]
        public void TestEnumSerializable()
        {
            var parentTaxEntry = new SimTaxonomyEntry("demokey");
            var value1 = new SimTaxonomyEntry("VALUE1", "VALUE1");
            var value2 = new SimTaxonomyEntry("VALUE2", "VALUE2");

            parentTaxEntry.Children.Add(value1);
            parentTaxEntry.Children.Add(value2);

            var param = new SimEnumParameter("Parameter", parentTaxEntry)
            {
                Value = new SimTaxonomyEntryReference(value1)
            };
            var serParam = new SimEnumParameterSerializable(param);


            Assert.AreEqual(param.NameTaxonomyEntry.Text, serParam.Name.Text);
            Assert.IsNull(serParam.Name.TaxonomyEntry);
            Assert.AreEqual(param.Description, serParam.Description);
            Assert.AreEqual(value1.Key, serParam.Value);
            Assert.IsTrue(parentTaxEntry.Children.All(t => serParam.Items.Contains(t.Key)));

        }

        #endregion


        #region SimNetworks
        [TestMethod]
        public void TestSimNetworkPortSerializable()
        {
            var port = new SimNetworkPort(PortType.Input, "PORT") { Color = SimColors.Red };
            var serPort = new SimNetworkPortSerializable(port);


            Assert.AreEqual(port.LocalID, serPort.Id);
            Assert.AreEqual(port.Name, serPort.Name);
            Assert.AreEqual(port.PortType.ToString(), serPort.PortType);
            Assert.AreEqual(DXFDataConverter<SimColor>.P.ToDXFString(port.Color), serPort.Color);
        }



        [TestMethod]
        public void TestSimNetworkBlockSerializable()
        {
            var block = new SimNetworkBlock("BLOCK", new SimPoint(1, 2));
            var port1 = new SimNetworkPort(PortType.Input, "PORT") { Color = SimColors.Red };
            var port2 = new SimNetworkPort(PortType.Input, "PORT") { Color = SimColors.Red };
            block.Ports.Add(port1);
            block.Ports.Add(port2);


            var serBlock = new SimNetworkBlockSerializable(block);

            Assert.AreEqual(block.LocalID, serBlock.Id);
            Assert.AreEqual(block.Name, serBlock.Name);
            Assert.AreEqual(DXFDataConverter<SimColor>.P.ToDXFString(block.Color), serBlock.Color);
            Assert.AreEqual(block.Position.X, serBlock.Position.X);
            Assert.AreEqual(block.Position.Y, serBlock.Position.Y);
            Assert.AreEqual(block.Ports.Count, serBlock.Ports.Count);

        }


        [TestMethod]
        public void TestSimNetwokrConnectorSerializble()
        {
            var port1 = new SimNetworkPort(PortType.Output, "PORT") { Color = SimColors.Red };
            var port2 = new SimNetworkPort(PortType.Input, "PORT") { Color = SimColors.Red };

            var connector = new SimNetworkConnector(port1, port2);
            var serConnector = new SimNetworkConnectorSerializable(connector);

            Assert.AreEqual(port1.LocalID, serConnector.Source);
            Assert.AreEqual(port2.LocalID, serConnector.Target);

        }


        [TestMethod]
        public void TestSimNetworkSerializable()
        {
            var network = new SimNetwork("NETWORK");
            var subnetwork = new SimNetwork("SUBNETWORK");
            network.ContainedElements.Add(subnetwork);

            var block1 = new SimNetworkBlock("BLOCK", new SimPoint(1, 2));
            var block2 = new SimNetworkBlock("BLOCK", new SimPoint(1, 10));

            var port1 = new SimNetworkPort(PortType.Output, "PORT") { Color = SimColors.Red };
            var port2 = new SimNetworkPort(PortType.Input, "PORT") { Color = SimColors.Red };

            block1.Ports.Add(port1);
            block2.Ports.Add(port2);
            network.ContainedElements.Add(block1);
            network.ContainedElements.Add(block2);


            var connector = new SimNetworkConnector(port1, port2);
            network.ContainedConnectors.Add(connector);

            var serNetwork = new SimNetworkSerializable(network);


            Assert.AreEqual(network.LocalID, serNetwork.Id);
            Assert.AreEqual(network.Name, serNetwork.Name);
            Assert.AreEqual(1, serNetwork.Connectors.Count);
            Assert.AreEqual(2, serNetwork.Blocks.Count);
            Assert.AreEqual(1, serNetwork.Subnetworks.Count);
        }

        [TestMethod]
        public void TestSimNetworkSerializableComplex()
        {
            var network = new SimNetwork("NETWORK");

            var block1 = new SimNetworkBlock("BLOCK", new SimPoint(1, 2));
            var block2 = new SimNetworkBlock("BLOCK", new SimPoint(1, 10));

            var port11 = new SimNetworkPort(PortType.Output, "PORT") { Color = SimColors.Red };
            var port12 = new SimNetworkPort(PortType.Output, "PORT") { Color = SimColors.Red };
            block1.Ports.Add(port11);
            block1.Ports.Add(port12);


            var port21 = new SimNetworkPort(PortType.Input, "PORT") { Color = SimColors.Red };
            var port22 = new SimNetworkPort(PortType.Input, "PORT") { Color = SimColors.Red };
            block2.Ports.Add(port21);
            block2.Ports.Add(port22);

            network.ContainedElements.Add(block1);
            network.ContainedElements.Add(block2);

            port11.ConnectTo(port21);

            //Subnetwork
            var subnetwork = new SimNetwork("SUBNETWORK");
            var input = new SimNetworkPort(PortType.Input, "PORT") { Color = SimColors.Red };
            var output = new SimNetworkPort(PortType.Output, "PORT") { Color = SimColors.Red };
            subnetwork.Ports.Add(input);
            subnetwork.Ports.Add(output);

            var subBlock1 = new SimNetworkBlock("BLOCK", new SimPoint(1, 2));
            var input1 = new SimNetworkPort(PortType.Input, "PORT") { Color = SimColors.Red };
            var output1 = new SimNetworkPort(PortType.Output, "PORT") { Color = SimColors.Red };
            subBlock1.Ports.Add(input1);
            subBlock1.Ports.Add(output1);
            subnetwork.ContainedElements.Add(subBlock1);

            var subBlock2 = new SimNetworkBlock("BLOCK", new SimPoint(1, 2));
            var input2 = new SimNetworkPort(PortType.Input, "PORT") { Color = SimColors.Red };
            var output2 = new SimNetworkPort(PortType.Output, "PORT") { Color = SimColors.Red };
            subBlock2.Ports.Add(input2);
            subBlock2.Ports.Add(output2);
            subnetwork.ContainedElements.Add(subBlock2);

            output1.ConnectTo(input2);
            output1.ConnectTo(input2);
            network.ContainedElements.Add(subnetwork);

            //Connect subnetowrk with other blocks
            port12.ConnectTo(input);
            input.ConnectTo(input1);

            port22.ConnectTo(output);
            output.ConnectTo(output2);

            var serNetwork = new SimNetworkSerializable(network);


            Assert.AreEqual(3, serNetwork.Connectors.Count);
            Assert.AreEqual(3, serNetwork.Subnetworks[0].Connectors.Count);
            Assert.AreEqual(2, serNetwork.Subnetworks[0].Blocks.Count);
            Assert.AreEqual(2, serNetwork.Blocks.Count);
        }

        #endregion



        #region Components and Instances

        [TestMethod]
        public void TestComponentSerializable()
        {
            var taxonomies = TaxonomyUtils.GetDefaultTaxonomies();
            var tax = new SimTaxonomy("test");
            var tax2 = new SimTaxonomy("test2");
            taxonomies.Add(tax);
            taxonomies.Add(tax2);
            var entry = new SimTaxonomyEntry("KEY", "ENTRYVALUE");
            tax.Entries.Add(entry);

            var component = new SimComponent()
            {
                Name = "COMPONENT",
                Description = "DESCRIPTION",
                InstanceType = SimInstanceType.Entity3D,
            };
            component.Slots.Add(new SimTaxonomyEntryReference(entry));

            var slotTax = new SimTaxonomyEntry("KEY", "OTHESLOT");
            tax2.Entries.Add(slotTax);
            var subComponent = new SimComponent()
            {
                Name = "SUBCOMP",
                Description = "DESCRIPTION",
                InstanceType = SimInstanceType.Entity3D,
            };
            subComponent.Slots.Add(new SimTaxonomyEntryReference(slotTax));

            var child = new SimChildComponentEntry(new SimSlot(slotTax, "DD"), subComponent);
            component.Components.Add(child);

            var param = new SimDoubleParameter("Parameter Distance", "km", 10.23)
            {
                ValueMin = -10.0,
                ValueMax = double.MaxValue
            };
            component.Parameters.Add(param);

            var serComp = new SimComponentSerializable(component);

            Assert.AreEqual(component.LocalID, serComp.Id);
            Assert.AreEqual(component.Name, serComp.Name);
            Assert.AreEqual(component.Description, serComp.Description);
            Assert.AreEqual(component.Components.Count, serComp.Components.Count);
            Assert.AreEqual(component.Parameters.Count, serComp.Parameters.Count);
            Assert.AreEqual(component.Slots.Count, serComp.Slots.Count());
        }



        #endregion
    }
}
