using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data.FlowNetworks;
using SIMULTAN.Data.Users;
using SIMULTAN.Projects;
using SIMULTAN.Serializer.CODXF;
using SIMULTAN.Serializer.DXF;
using SIMULTAN.Tests.Properties;
using SIMULTAN.Tests.TestUtils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;


namespace SIMULTAN.Tests.IO
{
    [TestClass]
    public class ComponentDXFNetworkTests : ComponentDXFTestsBase
    {
        public static void CheckNetworks(ProjectData projectData, Guid guid)
        {
            Assert.AreEqual(1, projectData.NetworkManager.NetworkRecord.Count);
            var network = projectData.NetworkManager.NetworkRecord[0];

            CheckNetwork(network, guid);
        }

        private static void CheckNetwork(SimFlowNetwork network, Guid guid)
        {
            Assert.IsNotNull(network);
            Assert.AreEqual(1, network.ID.LocalId);
            Assert.AreEqual(guid, network.ID.GlobalId);
            Assert.AreEqual("Network 1", network.Name);
            Assert.AreEqual("Network Description 1", network.Description);
            Assert.AreEqual(false, network.IsValid);
            Assert.AreEqual(3, network.RepresentationReference.FileId);
            Assert.AreEqual(90U, network.RepresentationReference.GeometryId);
            Assert.AreEqual(3, network.IndexOfGeometricRepFile);
            Assert.AreEqual(0.0, network.Position.X);
            Assert.AreEqual(0.0, network.Position.Y);
            Assert.AreEqual(SimUserRole.BUILDING_PHYSICS, network.Manager);
            Assert.AreEqual(-1, network.NodeStart_ID);
            Assert.AreEqual(-1, network.NodeEnd_ID);
            Assert.AreEqual(false, network.IsDirected);

            Assert.AreEqual(2, network.ContainedNodes.Count);
            Assert.AreEqual(2, network.ContainedEdges.Count);
            Assert.AreEqual(1, network.ContainedFlowNetworks.Count);

            var subnet = network.ContainedFlowNetworks.Values.First();
            Assert.AreEqual(40.4, subnet.Position.X);
            Assert.AreEqual(-7.6, subnet.Position.Y);
            Assert.AreEqual(SimUserRole.BUILDING_PHYSICS, subnet.Manager);
            Assert.AreEqual(6, subnet.NodeStart_ID);
            Assert.AreEqual(7, subnet.NodeEnd_ID);
            Assert.AreEqual(false, subnet.IsDirected);


            var node1 = network.ContainedNodes.Values.First(x => x.Name == "Network Node");
            var node2 = network.ContainedNodes.Values.First(x => x.Name == "Network Node 2");
            var edge1 = network.ContainedEdges.Values.First(x => x.Name == "Network Edge 1");
            var edge2 = network.ContainedEdges.Values.First(x => x.Name == "Network Edge 2");

            Assert.AreEqual(node1, edge1.Start);
            Assert.AreEqual(node2, edge1.End);

            Assert.AreEqual(subnet, edge2.Start);
            Assert.AreEqual(node2, edge2.End);
        }


        #region Network Node

        [TestMethod]
        public void WriteFlowNetworkNode()
        {
            var projectData = CreateTestData();

            string exportedString = null;
            using (MemoryStream stream = new MemoryStream())
            {
                using (DXFStreamWriter writer = new DXFStreamWriter(stream, true))
                {
                    var node1 = projectData.NetworkManager.NetworkRecord[0].ContainedNodes.Values.First(x => x.Name == "Network Node");
                    ComponentDxfIONetworks.WriteNetworkNode(node1, writer);
                }

                stream.Flush();
                stream.Position = 0;

                var array = stream.ToArray();
                exportedString = Encoding.UTF8.GetString(array);
            }

            AssertUtil.AreEqualMultiline(Properties.Resources.DXFSerializer_WriteFlowNetworkNode, exportedString);
        }

        [TestMethod]
        public void ReadFlowNetworkNodeV12()
        {
            ExtendedProjectData projectData = new ExtendedProjectData();
            Guid guid = Guid.NewGuid();
            var otherGuid = Guid.Parse("da7d8f7c-8eec-423b-b127-9d6e17f52522");

            SimFlowNetworkNode node = null;

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_ReadCODXF_FlowNetworkNodeV12)))
            {
                var info = new DXFParserInfo(guid, projectData);
                info.FileVersion = 12;

                reader.Read();
                node = ComponentDxfIONetworks.NetworkNodeEntityElement.Parse(reader, info);
            }

            Assert.IsNotNull(node);
            Assert.AreEqual(9950, node.ID.LocalId);
            Assert.AreEqual(guid, node.ID.GlobalId);
            Assert.AreEqual("Network Node", node.Name);
            Assert.AreEqual("Description Text", node.Description);
            Assert.AreEqual(true, node.IsValid);
            Assert.AreEqual(3, node.RepresentationReference.FileId);
            Assert.AreEqual(66U, node.RepresentationReference.GeometryId);
            Assert.AreEqual(33.4, node.Position.X);
            Assert.AreEqual(-7.6, node.Position.Y);

            Assert.AreEqual(2, node.CalculationRules.Count);

            Assert.AreEqual("suf op", node.CalculationRules[0].Suffix_Operands);
            Assert.AreEqual("suf result", node.CalculationRules[0].Suffix_Result);
            Assert.AreEqual(SimFlowNetworkCalcDirection.Forward, node.CalculationRules[0].Direction);
            Assert.AreEqual(SimFlowNetworkOperator.Assignment, node.CalculationRules[0].Operator);

            Assert.AreEqual("suf op 2", node.CalculationRules[1].Suffix_Operands);
            Assert.AreEqual("suf result 2", node.CalculationRules[1].Suffix_Result);
            Assert.AreEqual(SimFlowNetworkCalcDirection.Backward, node.CalculationRules[1].Direction);
            Assert.AreEqual(SimFlowNetworkOperator.Subtraction, node.CalculationRules[1].Operator);
        }

        [TestMethod]
        public void ReadFlowNetworkNodeV11()
        {
            ExtendedProjectData projectData = new ExtendedProjectData();
            Guid guid = Guid.NewGuid();
            var otherGuid = Guid.Parse("da7d8f7c-8eec-423b-b127-9d6e17f52522");

            SimFlowNetworkNode node = null;

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_ReadCODXF_FlowNetworkNodeV11)))
            {
                var info = new DXFParserInfo(guid, projectData);
                info.FileVersion = 11;

                reader.Read();
                node = ComponentDxfIONetworks.NetworkNodeEntityElement.Parse(reader, info);
            }

            Assert.IsNotNull(node);
            Assert.AreEqual(9950, node.ID.LocalId);
            Assert.AreEqual(guid, node.ID.GlobalId);
            Assert.AreEqual("Network Node", node.Name);
            Assert.AreEqual("Description Text", node.Description);
            Assert.AreEqual(true, node.IsValid);
            Assert.AreEqual(3, node.RepresentationReference.FileId);
            Assert.AreEqual(66U, node.RepresentationReference.GeometryId);
            Assert.AreEqual(33.4, node.Position.X);
            Assert.AreEqual(-7.6, node.Position.Y);

            Assert.AreEqual(2, node.CalculationRules.Count);

            Assert.AreEqual("suf op", node.CalculationRules[0].Suffix_Operands);
            Assert.AreEqual("suf result", node.CalculationRules[0].Suffix_Result);
            Assert.AreEqual(SimFlowNetworkCalcDirection.Forward, node.CalculationRules[0].Direction);
            Assert.AreEqual(SimFlowNetworkOperator.Assignment, node.CalculationRules[0].Operator);

            Assert.AreEqual("suf op 2", node.CalculationRules[1].Suffix_Operands);
            Assert.AreEqual("suf result 2", node.CalculationRules[1].Suffix_Result);
            Assert.AreEqual(SimFlowNetworkCalcDirection.Backward, node.CalculationRules[1].Direction);
            Assert.AreEqual(SimFlowNetworkOperator.Subtraction, node.CalculationRules[1].Operator);
        }

        #endregion

        #region Network Edge

        [TestMethod]
        public void WriteFlowNetworkEdge()
        {
            ExtendedProjectData projectData = CreateTestData();

            string exportedString = null;
            using (MemoryStream stream = new MemoryStream())
            {
                using (DXFStreamWriter writer = new DXFStreamWriter(stream, true))
                {
                    var edge = projectData.NetworkManager.NetworkRecord[0].ContainedEdges.Values.First();
                    ComponentDxfIONetworks.WriteNetworkEdge(edge, writer);
                }

                stream.Flush();
                stream.Position = 0;

                var array = stream.ToArray();
                exportedString = Encoding.UTF8.GetString(array);
            }

            AssertUtil.AreEqualMultiline(Properties.Resources.DXFSerializer_WriteFlowNetworkEdge, exportedString);
        }

        [TestMethod]
        public void ReadFlowNetworkEdgeV12()
        {
            ExtendedProjectData projectData = new ExtendedProjectData();
            Guid guid = Guid.NewGuid();
            var otherGuid = Guid.Parse("da7d8f7c-8eec-423b-b127-9d6e17f52522");

            SimFlowNetworkEdge edge = null;

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_ReadCODXF_FlowNetworkEdgeV12)))
            {
                var info = new DXFParserInfo(guid, projectData);
                info.FileVersion = 12;

                reader.Read();
                edge = ComponentDxfIONetworks.NetworkEdgeEntityElement.Parse(reader, info);
            }

            Assert.IsNotNull(edge);
            Assert.AreEqual(4, edge.ID.LocalId);
            Assert.AreEqual(guid, edge.ID.GlobalId);
            Assert.AreEqual("Network Edge 1", edge.Name);
            Assert.AreEqual("Network Edge Description", edge.Description);
            Assert.AreEqual(true, edge.IsValid);
            Assert.AreEqual(3, edge.RepresentationReference.FileId);
            Assert.AreEqual(67U, edge.RepresentationReference.GeometryId);

            var loadingStartIdParam = typeof(SimFlowNetworkEdge).GetField("loadingStartId", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.AreEqual(2L, loadingStartIdParam.GetValue(edge));
            var loadingEndIdParam = typeof(SimFlowNetworkEdge).GetField("loadingEndId", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.AreEqual(3L, loadingEndIdParam.GetValue(edge));
        }

        [TestMethod]
        public void ReadFlowNetworkEdgeV11()
        {
            ExtendedProjectData projectData = new ExtendedProjectData();
            Guid guid = Guid.NewGuid();
            var otherGuid = Guid.Parse("da7d8f7c-8eec-423b-b127-9d6e17f52522");

            SimFlowNetworkEdge edge = null;

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_ReadCODXF_FlowNetworkEdgeV11)))
            {
                var info = new DXFParserInfo(guid, projectData);
                info.FileVersion = 11;

                reader.Read();
                edge = ComponentDxfIONetworks.NetworkEdgeEntityElement.Parse(reader, info);
            }

            Assert.IsNotNull(edge);
            Assert.AreEqual(4, edge.ID.LocalId);
            Assert.AreEqual(guid, edge.ID.GlobalId);
            Assert.AreEqual("Network Edge 1", edge.Name);
            Assert.AreEqual("Network Edge Description", edge.Description);
            Assert.AreEqual(true, edge.IsValid);
            Assert.AreEqual(3, edge.RepresentationReference.FileId);
            Assert.AreEqual(67U, edge.RepresentationReference.GeometryId);

            var loadingStartIdParam = typeof(SimFlowNetworkEdge).GetField("loadingStartId", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.AreEqual(2L, loadingStartIdParam.GetValue(edge));
            var loadingEndIdParam = typeof(SimFlowNetworkEdge).GetField("loadingEndId", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.AreEqual(3L, loadingEndIdParam.GetValue(edge));
        }

        #endregion

        #region Network

        [TestMethod]
        public void WriteFlowNetwork()
        {
            ExtendedProjectData projectData = CreateTestData();

            string exportedString = null;
            using (MemoryStream stream = new MemoryStream())
            {
                using (DXFStreamWriter writer = new DXFStreamWriter(stream, true))
                {
                    var network = projectData.NetworkManager.NetworkRecord[0];
                    ComponentDxfIONetworks.WriteNetwork(network, writer);
                }

                stream.Flush();
                stream.Position = 0;

                var array = stream.ToArray();
                exportedString = Encoding.UTF8.GetString(array);
            }

            AssertUtil.AreEqualMultiline(Resources.DXFSerializer_WriteFlowNetwork, exportedString);
        }

        [TestMethod]
        public void ReadFlowNetworkV12()
        {
            ExtendedProjectData projectData = new ExtendedProjectData();
            Guid guid = Guid.NewGuid();
            var otherGuid = Guid.Parse("da7d8f7c-8eec-423b-b127-9d6e17f52522");

            SimFlowNetwork network = null;

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_ReadCODXF_FlowNetworkV12)))
            {
                var info = new DXFParserInfo(guid, projectData);
                info.FileVersion = 12;

                reader.Read();
                network = ComponentDxfIONetworks.NetworkEntityElement.Parse(reader, info);
            }

            CheckNetwork(network, guid);
        }

        [TestMethod]
        public void ReadFlowNetworkV11()
        {
            ExtendedProjectData projectData = new ExtendedProjectData();
            Guid guid = Guid.NewGuid();
            var otherGuid = Guid.Parse("da7d8f7c-8eec-423b-b127-9d6e17f52522");

            SimFlowNetwork network = null;

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_ReadCODXF_FlowNetworkV11)))
            {
                var info = new DXFParserInfo(guid, projectData);
                info.FileVersion = 11;

                reader.Read();
                network = ComponentDxfIONetworks.NetworkEntityElement.Parse(reader, info);
            }

            CheckNetwork(network, guid);
        }

        #endregion

        #region Section

        [TestMethod]
        public void WriteFlowNetworkSection()
        {
            ExtendedProjectData projectData = CreateTestData();

            string exportedString = null;
            using (MemoryStream stream = new MemoryStream())
            {
                using (DXFStreamWriter writer = new DXFStreamWriter(stream, true))
                {
                    var network = projectData.NetworkManager.NetworkRecord[0];
                    ComponentDxfIONetworks.WriteNetworkSection(new List<SimFlowNetwork> { network }, writer);
                }

                stream.Flush();
                stream.Position = 0;

                var array = stream.ToArray();
                exportedString = Encoding.UTF8.GetString(array);
            }

            AssertUtil.AreEqualMultiline(Resources.DXFSerializer_WriteNetworkSection, exportedString);
        }

        [TestMethod]
        public void ReadFlowNetworkSectionV12()
        {
            ExtendedProjectData projectData = new ExtendedProjectData();
            Guid guid = Guid.NewGuid();
            var otherGuid = Guid.Parse("da7d8f7c-8eec-423b-b127-9d6e17f52522");

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_ReadCODXF_NetworkSectionV12)))
            {
                var info = new DXFParserInfo(guid, projectData);
                info.FileVersion = 12;

                ComponentDxfIONetworks.ReadNetworkSection(reader, info);
            }

            CheckNetworks(projectData, guid);
        }

        [TestMethod]
        public void ReadFlowNetworkSectionV11()
        {
            ExtendedProjectData projectData = new ExtendedProjectData();
            Guid guid = Guid.NewGuid();
            var otherGuid = Guid.Parse("da7d8f7c-8eec-423b-b127-9d6e17f52522");

            using (DXFStreamReader reader = new DXFStreamReader(StringStream.Create(Resources.DXFSerializer_ReadCODXF_NetworkSectionV11)))
            {
                var info = new DXFParserInfo(guid, projectData);
                info.FileVersion = 11;
                ComponentDxfIONetworks.ReadNetworkSection(reader, info);
            }

            CheckNetworks(projectData, guid);
        }

        #endregion
    }
}
