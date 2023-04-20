using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data;
using SIMULTAN.Data.SimNetworks;
using SIMULTAN.Projects;
using SIMULTAN.Serializer.CODXF;
using SIMULTAN.Serializer.DXF;
using SIMULTAN.Tests.Properties;
using SIMULTAN.Tests.Util;
using SIMULTAN.Tests.TestUtils;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace SIMULTAN.Tests.IO
{
    [TestClass]
    public class ComponentDXFSimNetworkTests : ComponentDXFTestsBase
    {
        internal static void CheckTestData(ExtendedProjectData projectData)
        {
            var network = projectData.SimNetworks[0];
            Assert.IsNotNull(network);
            Assert.AreEqual(65001, network.Id.LocalId);
            Assert.AreEqual("My Network", network.Name);
            Assert.AreEqual(45.0, network.Position.X);
            Assert.AreEqual(46.0, network.Position.Y);

            Assert.AreEqual(1, network.Ports.Count);

            var port1 = network.Ports[0];
            Assert.AreEqual("Network Port 1", port1.Name);
            Assert.AreEqual(65008, port1.Id.LocalId);

            Assert.AreEqual(3, network.ContainedElements.Count);
            Assert.AreEqual(2, network.ContainedElements.Count(x => x is SimNetworkBlock));
            Assert.AreEqual(1, network.ContainedElements.Count(x => x is SimNetwork));

            Assert.AreEqual(1, network.ContainedConnectors.Count);
            var con1 = network.ContainedConnectors[0];
            Assert.AreEqual(65007, con1.Id.LocalId);
        }

        #region Section

        [TestMethod]
        public void WriteNetworkSection()
        {
            var guid = Guid.NewGuid();
            var otherGuid = Guid.Parse("da7d8f7c-8eec-423b-b127-9d6e17f52522");

            ExtendedProjectData projectData = new ExtendedProjectData();
            projectData.SetCallingLocation(new DummyReferenceLocation(guid));

            CreateSimNetworkTestData(projectData, guid);

            string exportedString = null;
            using (MemoryStream stream = new MemoryStream())
            {
                using (DXFStreamWriter writer = new DXFStreamWriter(stream, true))
                {
                    ComponentDxfIOSimNetworks.WriteNetworkSection(projectData.SimNetworks, writer);
                }

                stream.Flush();
                stream.Position = 0;

                var array = stream.ToArray();
                exportedString = Encoding.UTF8.GetString(array);
            }

            AssertUtil.AreEqualMultiline(Resources.DXFSerializer_CODXF_WriteNetworkSection, exportedString);
        }

        [TestMethod]
        public void ReadNetworkSectionV12()
        {
            var guid = Guid.NewGuid();
            var otherGuid = Guid.Parse("da7d8f7c-8eec-423b-b127-9d6e17f52522");

            ExtendedProjectData projectData = new ExtendedProjectData();
            projectData.SetCallingLocation(new DummyReferenceLocation(guid));

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_CODXF_ReadNetworkSectionV12)))
            {
                var info = new DXFParserInfo(guid, projectData);
                info.FileVersion = 12;

                ComponentDxfIOSimNetworks.ReadNetworkSection(reader, info);
            }

            Assert.AreEqual(1, projectData.SimNetworks.Count);

            CheckTestData(projectData);
        }

        #endregion

        #region Networks

        [TestMethod]
        public void WriteNetwork()
        {
            var guid = Guid.NewGuid();
            var otherGuid = Guid.Parse("da7d8f7c-8eec-423b-b127-9d6e17f52522");

            ExtendedProjectData projectData = new ExtendedProjectData();
            projectData.SetCallingLocation(new DummyReferenceLocation(guid));

            CreateSimNetworkTestData(projectData, guid);
            var network = projectData.SimNetworks[0];

            string exportedString = null;
            using (MemoryStream stream = new MemoryStream())
            {
                using (DXFStreamWriter writer = new DXFStreamWriter(stream, true))
                {
                    ComponentDxfIOSimNetworks.WriteNetwork(network, writer);
                }

                stream.Flush();
                stream.Position = 0;

                var array = stream.ToArray();
                exportedString = Encoding.UTF8.GetString(array);
            }

            AssertUtil.AreEqualMultiline(Properties.Resources.DXFSerializer_CODXF_WriteNetwork, exportedString);
        }

        [TestMethod]
        public void ReadNetworkV12()
        {
            var guid = Guid.NewGuid();
            var otherGuid = Guid.Parse("da7d8f7c-8eec-423b-b127-9d6e17f52522");

            ExtendedProjectData projectData = new ExtendedProjectData();
            projectData.SetCallingLocation(new DummyReferenceLocation(guid));

            SimNetwork network = null;

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_CODXF_ReadNetworkV12)))
            {
                var info = new DXFParserInfo(guid, projectData);
                info.FileVersion = 12;

                reader.Read();

                network = ComponentDxfIOSimNetworks.NetworkEntityElement.Parse(reader, info);
            }

            Assert.IsNotNull(network);
            Assert.AreEqual(65001, network.Id.LocalId);
            Assert.AreEqual("My Network", network.Name);
            Assert.AreEqual(45.0, network.Position.X);
            Assert.AreEqual(46.0, network.Position.Y);

            Assert.AreEqual(1, network.Ports.Count);

            var port1 = network.Ports[0];
            Assert.AreEqual("Network Port 1", port1.Name);
            Assert.AreEqual(65008, port1.Id.LocalId);

            Assert.AreEqual(3, network.ContainedElements.Count);
            Assert.AreEqual(2, network.ContainedElements.Count(x => x is SimNetworkBlock));
            Assert.AreEqual(1, network.ContainedElements.Count(x => x is SimNetwork));

            Assert.AreEqual(1, network.ContainedConnectors.Count);
            var con1 = network.ContainedConnectors[0];
            Assert.AreEqual(65007, con1.Id.LocalId);
        }

        #endregion

        #region Connector

        [TestMethod]
        public void WriteConnector()
        {
            var guid = Guid.NewGuid();
            var otherGuid = Guid.Parse("da7d8f7c-8eec-423b-b127-9d6e17f52522");

            ExtendedProjectData projectData = new ExtendedProjectData();
            projectData.SetCallingLocation(new DummyReferenceLocation(guid));

            CreateSimNetworkTestData(projectData, guid);
            var con = projectData.SimNetworks[0].ContainedConnectors.First();

            string exportedString = null;
            using (MemoryStream stream = new MemoryStream())
            {
                using (DXFStreamWriter writer = new DXFStreamWriter(stream, true))
                {
                    ComponentDxfIOSimNetworks.WriteConnector(con, writer);
                }

                stream.Flush();
                stream.Position = 0;

                var array = stream.ToArray();
                exportedString = Encoding.UTF8.GetString(array);
            }

            AssertUtil.AreEqualMultiline(Properties.Resources.DXFSerializer_CODXF_WriteConnector, exportedString);
        }

        [TestMethod]
        public void ReadConnectorV12()
        {
            var guid = Guid.NewGuid();
            var otherGuid = Guid.Parse("da7d8f7c-8eec-423b-b127-9d6e17f52522");

            ExtendedProjectData projectData = new ExtendedProjectData();
            projectData.SetCallingLocation(new DummyReferenceLocation(guid));

            SimNetworkConnector connector = null;

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_CODXF_ReadConnectorV12)))
            {
                var info = new DXFParserInfo(guid, projectData);
                info.FileVersion = 12;

                reader.Read();

                connector = ComponentDxfIOSimNetworks.ConnectorEntityElement.Parse(reader, info);
            }

            Assert.IsNotNull(connector);
            Assert.AreEqual(65007, connector.Id.LocalId);
            Assert.AreEqual("My Connector", connector.Name);

            var prop = typeof(SimNetworkConnector).GetField("loadingSourceId",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.AreEqual(65004, ((SimId)prop.GetValue(connector)).LocalId);

            prop = typeof(SimNetworkConnector).GetField("loadingTargetId",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.AreEqual(65006, ((SimId)prop.GetValue(connector)).LocalId);
        }

        #endregion

        #region Port

        [TestMethod]
        public void WritePort()
        {
            var guid = Guid.NewGuid();
            var otherGuid = Guid.Parse("da7d8f7c-8eec-423b-b127-9d6e17f52522");

            ExtendedProjectData projectData = new ExtendedProjectData();
            projectData.SetCallingLocation(new DummyReferenceLocation(guid));

            CreateSimNetworkTestData(projectData, guid);
            var b1 = (SimNetworkBlock)projectData.SimNetworks[0].ContainedElements.First(x => x is SimNetworkBlock);
            var p1 = b1.Ports[0];

            string exportedString = null;
            using (MemoryStream stream = new MemoryStream())
            {
                using (DXFStreamWriter writer = new DXFStreamWriter(stream, true))
                {
                    ComponentDxfIOSimNetworks.WritePort(p1, writer);
                }

                stream.Flush();
                stream.Position = 0;

                var array = stream.ToArray();
                exportedString = Encoding.UTF8.GetString(array);
            }

            AssertUtil.AreEqualMultiline(Properties.Resources.DXFSerializer_CODXF_WritePort, exportedString);
        }

        [TestMethod]
        public void ReadPortV12()
        {
            var guid = Guid.NewGuid();
            var otherGuid = Guid.Parse("da7d8f7c-8eec-423b-b127-9d6e17f52522");

            ExtendedProjectData projectData = new ExtendedProjectData();
            projectData.SetCallingLocation(new DummyReferenceLocation(guid));

            SimNetworkPort port = null;

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_CODXF_ReadPortV12)))
            {
                var info = new DXFParserInfo(guid, projectData);
                info.FileVersion = 12;

                reader.Read();

                port = ComponentDxfIOSimNetworks.PortEntityElement.Parse(reader, info);
            }

            Assert.IsNotNull(port);
            Assert.AreEqual(65004, port.Id.LocalId);
            Assert.AreEqual("My Port A", port.Name);
            Assert.AreEqual(PortType.Output, port.PortType);
        }

        #endregion

        #region Blocks

        [TestMethod]
        public void WriteBlock()
        {
            var guid = Guid.NewGuid();
            var otherGuid = Guid.Parse("da7d8f7c-8eec-423b-b127-9d6e17f52522");

            ExtendedProjectData projectData = new ExtendedProjectData();
            projectData.SetCallingLocation(new DummyReferenceLocation(guid));

            CreateSimNetworkTestData(projectData, guid);
            var b1 = (SimNetworkBlock)projectData.SimNetworks[0].ContainedElements.First(x => x is SimNetworkBlock);

            string exportedString = null;
            using (MemoryStream stream = new MemoryStream())
            {
                using (DXFStreamWriter writer = new DXFStreamWriter(stream, true))
                {
                    ComponentDxfIOSimNetworks.WriteBlock(b1, writer);
                }

                stream.Flush();
                stream.Position = 0;

                var array = stream.ToArray();
                exportedString = Encoding.UTF8.GetString(array);
            }

            AssertUtil.AreEqualMultiline(Properties.Resources.DXFSerializer_CODXF_WriteBlock, exportedString);
        }

        [TestMethod]
        public void ReadBlockV12()
        {
            var guid = Guid.NewGuid();
            var otherGuid = Guid.Parse("da7d8f7c-8eec-423b-b127-9d6e17f52522");

            ExtendedProjectData projectData = new ExtendedProjectData();
            projectData.SetCallingLocation(new DummyReferenceLocation(guid));

            SimNetworkBlock block = null;

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_CODXF_ReadBlockV12)))
            {
                var info = new DXFParserInfo(guid, projectData);
                info.FileVersion = 12;

                reader.Read();

                block = ComponentDxfIOSimNetworks.BlockEntityElement.Parse(reader, info);
            }

            Assert.IsNotNull(block);
            Assert.AreEqual(65002, block.Id.LocalId);
            Assert.AreEqual("Block A", block.Name);
            Assert.AreEqual(1.0, block.Position.X);
            Assert.AreEqual(2.0, block.Position.Y);

            Assert.AreEqual(2, block.Ports.Count);

            var port1 = block.Ports[0];
            Assert.AreEqual("My Port A", port1.Name);
            Assert.AreEqual(65004, port1.Id.LocalId);

            var port2 = block.Ports[1];
            Assert.AreEqual("My Port C", port2.Name);
            Assert.AreEqual(65005, port2.Id.LocalId);
        }

        #endregion
    }
}
