using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data;
using SIMULTAN.Data.Assets;
using SIMULTAN.Data.Components;
using SIMULTAN.Data.FlowNetworks;
using SIMULTAN.Data.Geometry;
using SIMULTAN.Exchange.NetworkConnectors;
using SIMULTAN.Tests.TestUtils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Media3D;

namespace SIMULTAN.Tests.Instances
{
    [TestClass]
    public class InstanceNetworkGeometryTests : BaseProjectTest
    {
        private static readonly FileInfo instanceProject = new FileInfo(@".\InstanceTestsProject.simultan");

        #region Convert / Replace

        [TestMethod]
        public void Convert()
        {
            LoadProject(instanceProject, "arch", "arch");

            var network = projectData.NetworkManager.NetworkRecord.First(x => x.Name == "GeometryNetwork2");
            var sn = network.ContainedFlowNetworks.Values.First();
            Assert.IsNotNull(sn);

            var node1 = network.ContainedNodes.Values.First();
            var edge1 = network.ContainedEdges.Values.First();
            var node2 = sn.ContainedNodes[sn.NodeStart_ID];
            var edge2 = node2.Edges_Out.First();
            var node3 = edge2.End;
            var edge3 = node3.Edges_Out.First();
            var node4 = edge3.End;
            Assert.AreEqual(0, node4.Edges_Out.Count);

            var gm = projectData.ComponentGeometryExchange.ConvertNetwork(network, new FileInfo(Path.Combine(project.ProjectUnpackFolder.FullName, "test.simgeo")));
            var geo = gm.Geometry;

            // correct number of geometry elements?
            Assert.AreEqual(4, geo.Vertices.Count);
            Assert.AreEqual(3, geo.Edges.Count);

            // get geometry of network elements
            var v1 = geo.GeometryFromId(node1.RepresentationReference.GeometryId) as Vertex;
            var e1 = geo.GeometryFromId(edge1.RepresentationReference.GeometryId) as Polyline;
            var v2 = geo.GeometryFromId(node2.RepresentationReference.GeometryId) as Vertex;
            var e2 = geo.GeometryFromId(edge2.RepresentationReference.GeometryId) as Polyline;
            var v3 = geo.GeometryFromId(node3.RepresentationReference.GeometryId) as Vertex;
            var e3 = geo.GeometryFromId(edge3.RepresentationReference.GeometryId) as Polyline;
            var v4 = geo.GeometryFromId(node4.RepresentationReference.GeometryId) as Vertex;

            // does geometry exist and has correct type?
            Assert.IsNotNull(v1);
            Assert.IsNotNull(e1);
            Assert.IsNotNull(v2);
            Assert.IsNotNull(e2);
            Assert.IsNotNull(v3);
            Assert.IsNotNull(e3);
            Assert.IsNotNull(v4);

            // edge count correct?
            Assert.AreEqual(1, v1.Edges.Count);
            Assert.AreEqual(2, v2.Edges.Count);
            Assert.AreEqual(2, v3.Edges.Count);
            Assert.AreEqual(1, v4.Edges.Count);

            // proxy geometry exists?
            Assert.IsNotNull(v1.ProxyGeometries);
            Assert.AreEqual(1, v1.ProxyGeometries.Count);
            Assert.IsNotNull(v2.ProxyGeometries);
            Assert.AreEqual(1, v2.ProxyGeometries.Count);
            Assert.IsNotNull(v3.ProxyGeometries);
            Assert.AreEqual(1, v3.ProxyGeometries.Count);
            Assert.IsNotNull(v4.ProxyGeometries);
            Assert.AreEqual(1, v4.ProxyGeometries.Count);

            // proxy geometry correct size?
            Assert.AreEqual(new Vector3D(1.0, 1.0, 1.0), v1.ProxyGeometries[0].Size);
            Assert.AreEqual(new Vector3D(1.0, 1.0, 1.0), v2.ProxyGeometries[0].Size);
            Assert.AreEqual(new Vector3D(1.0, 2.0, 3.0), v3.ProxyGeometries[0].Size);
            Assert.AreEqual(new Vector3D(1.0, 1.0, 1.0), v4.ProxyGeometries[0].Size);
            Assert.AreEqual(node1.GetInstanceSize().Max, v1.ProxyGeometries[0].Size);
            Assert.AreEqual(node2.GetInstanceSize().Max, v2.ProxyGeometries[0].Size);
            Assert.AreEqual(node3.GetInstanceSize().Max, v3.ProxyGeometries[0].Size);
            Assert.AreEqual(node4.GetInstanceSize().Max, v4.ProxyGeometries[0].Size);

            // check colors
            Assert.AreEqual(System.Windows.Media.Color.FromRgb(0xFF, 0xFF, 0xFF), v1.Color.Color);
            Assert.AreEqual(System.Windows.Media.Color.FromRgb(0xFF, 0xFF, 0xFF), v2.Color.Color);
            Assert.AreEqual(System.Windows.Media.Color.FromRgb(0xFF, 0xFF, 0xFF), v3.Color.Color);
            Assert.AreEqual(System.Windows.Media.Color.FromRgb(0x40, 0x40, 0x40), v4.Color.Color);
            Assert.AreEqual(System.Windows.Media.Color.FromRgb(0xFF, 0xFF, 0xFF), e1.Color.Color);
            Assert.AreEqual(System.Windows.Media.Color.FromRgb(0xFF, 0xFF, 0xFF), e2.Color.Color);
            Assert.AreEqual(System.Windows.Media.Color.FromRgb(0x40, 0x40, 0x40), e3.Color.Color);

            // polyline edge count correct?
            Assert.AreEqual(1, e1.Edges.Count);
            Assert.AreEqual(1, e2.Edges.Count);
            Assert.AreEqual(1, e3.Edges.Count);

            // Topology is correct?
            Assert.IsTrue(e1.Edges[0].Edge.Vertices.Contains(v1));
            Assert.IsTrue(e1.Edges[0].Edge.Vertices.Contains(v2));
            Assert.IsTrue(e2.Edges[0].Edge.Vertices.Contains(v2));
            Assert.IsTrue(e2.Edges[0].Edge.Vertices.Contains(v3));
            Assert.IsTrue(e3.Edges[0].Edge.Vertices.Contains(v3));
            Assert.IsTrue(e3.Edges[0].Edge.Vertices.Contains(v4));

        }

        [TestMethod]
        public void ConvertAgain()
        {
            LoadProject(instanceProject, "arch", "arch");

            var network = projectData.NetworkManager.NetworkRecord.First(x => x.Name == "GeometryNetwork");

            Assert.ThrowsException<ArgumentException>(() =>
            {
                projectData.ComponentGeometryExchange.ConvertNetwork(network, new FileInfo("asdf.simgeo"));
            });
        }

        #endregion

        #region Network Changes

        [TestMethod]
        public void AddNode()
        {
            LoadProject(instanceProject);

            var network = projectData.NetworkManager.NetworkRecord.First(x => x.Name == "GeometryNetwork")
                .ContainedFlowNetworks.Values.First();

            //Open Geometry to initialize ComponentExchange
            (var gm, var resource) = ProjectUtils.LoadGeometry("Network.simgeo", projectData, sp);
            Assert.AreEqual(5, gm.Geometry.Vertices.Count);

            //Create node
            var newId = network.AddNode(new Point(1000, 1000));
            var node = network.ContainedNodes.Values.First(x => x.ID.LocalId == newId);

            Assert.AreNotEqual(GeometricReference.Empty, node.RepresentationReference);

            Assert.AreEqual(6, gm.Geometry.Vertices.Count);
            var v = gm.Geometry.Vertices.FirstOrDefault(x => x.Id == node.RepresentationReference.GeometryId);

            Assert.IsNotNull(v);
            Assert.AreEqual(1, v.ProxyGeometries.Count);
            Assert.AreEqual(node.Name, v.Name);
        }

        [TestMethod]
        public void RemoveNode()
        {
            LoadProject(instanceProject);

            var network = projectData.NetworkManager.NetworkRecord.First(x => x.Name == "GeometryNetwork")
                .ContainedFlowNetworks.Values.First();

            //Open Geometry to initialize ComponentExchange
            (var gm, var resource) = ProjectUtils.LoadGeometry("Network.simgeo", projectData, sp);
            //Create node
            var newId = network.AddNode(new System.Windows.Point(1000, 1000));
            var node = network.ContainedNodes.Values.First(x => x.ID.LocalId == newId);
            var v = gm.Geometry.Vertices.FirstOrDefault(x => x.Id == node.RepresentationReference.GeometryId);

            Assert.AreEqual(6, gm.Geometry.Vertices.Count);
            Assert.AreEqual(5, gm.Geometry.ProxyGeometries.Count);

            network.RemoveNodeOrNetwork(node);

            Assert.IsFalse(gm.Geometry.Vertices.Contains(v));
            Assert.AreEqual(5, gm.Geometry.Vertices.Count);
            Assert.AreEqual(4, gm.Geometry.ProxyGeometries.Count);
        }

        [TestMethod]
        public void AddEdge()
        {
            LoadProject(instanceProject);

            var network = projectData.NetworkManager.NetworkRecord.First(x => x.Name == "GeometryNetwork")
                .ContainedFlowNetworks.Values.First();

            //Open Geometry to initialize ComponentExchange
            (var gm, var resource) = ProjectUtils.LoadGeometry("Network.simgeo", projectData, sp);
            Assert.AreEqual(3, gm.Geometry.Polylines.Count);

            //Create edge
            var node1 = network.ContainedNodes.Values.First();

            var newId = network.AddNode(new System.Windows.Point(1000, 1000));
            var node2 = network.ContainedNodes.Values.First(x => x.ID.LocalId == newId);

            var edgeId = network.AddEdge(node1, node2);
            var edge = network.ContainedEdges.Values.First(x => x.ID.LocalId == edgeId);

            Assert.AreNotEqual(GeometricReference.Empty, edge.RepresentationReference);

            Assert.AreEqual(4, gm.Geometry.Polylines.Count);
            var poly = gm.Geometry.Polylines.FirstOrDefault(x => x.Id == edge.RepresentationReference.GeometryId);

            Assert.IsNotNull(poly);
            Assert.AreEqual(1, poly.Edges.Count);
            Assert.AreEqual(edge.Name, poly.Name);
        }

        [TestMethod]
        public void RemoveEdge()
        {
            LoadProject(instanceProject);

            var network = projectData.NetworkManager.NetworkRecord.First(x => x.Name == "GeometryNetwork")
                .ContainedFlowNetworks.Values.First();

            //Open Geometry to initialize ComponentExchange
            (var gm, var resource) = ProjectUtils.LoadGeometry("Network.simgeo", projectData, sp);

            //Create edge
            var node1 = network.ContainedNodes.Values.First();

            var newId = network.AddNode(new System.Windows.Point(1000, 1000));
            var node2 = network.ContainedNodes.Values.First(x => x.ID.LocalId == newId);

            var edgeId = network.AddEdge(node1, node2);
            var edge = network.ContainedEdges.Values.First(x => x.ID.LocalId == edgeId);
            var poly = gm.Geometry.Polylines.FirstOrDefault(x => x.Id == edge.RepresentationReference.GeometryId);

            Assert.AreEqual(4, gm.Geometry.Polylines.Count);

            network.RemoveEdge(edge);

            Assert.IsFalse(gm.Geometry.Polylines.Contains(poly));
            Assert.IsFalse(gm.Geometry.Edges.Contains(poly.Edges[0].Edge));
            Assert.AreEqual(3, gm.Geometry.Polylines.Count);
        }

        [TestMethod]
        public void EdgeRedirectedStart()
        {
            LoadProject(instanceProject);

            var network = projectData.NetworkManager.NetworkRecord.First(x => x.Name == "GeometryNetwork")
                .ContainedFlowNetworks.Values.First();

            //Open Geometry to initialize ComponentExchange
            (var gm, var resource) = ProjectUtils.LoadGeometry("Network.simgeo", projectData, sp);
            Assert.AreEqual(3, gm.Geometry.Polylines.Count);

            var edge = network.ContainedEdges.Values.First(x => x.ID.LocalId == 37);
            var newStart = network.ContainedNodes.Values.First(x => x.ID.LocalId == 32);

            var originalVertex = gm.Geometry.GeometryFromId(edge.Start.RepresentationReference.GeometryId) as Vertex;
            var polyline = gm.Geometry.GeometryFromId(edge.RepresentationReference.GeometryId) as Polyline;
            Assert.AreEqual(originalVertex, polyline.Edges.First().StartVertex);

            network.RedirectEdge(edge, true, newStart);

            var expectedVertex = gm.Geometry.GeometryFromId(newStart.RepresentationReference.GeometryId) as Vertex;
            Assert.AreEqual(expectedVertex, polyline.Edges.First().StartVertex);
        }

        [TestMethod]
        public void EdgeRedirectedEnd()
        {
            LoadProject(instanceProject);

            var network = projectData.NetworkManager.NetworkRecord.First(x => x.Name == "GeometryNetwork")
                .ContainedFlowNetworks.Values.First();

            //Open Geometry to initialize ComponentExchange
            (var gm, var resource) = ProjectUtils.LoadGeometry("Network.simgeo", projectData, sp);
            Assert.AreEqual(3, gm.Geometry.Polylines.Count);

            var edge = network.ContainedEdges.Values.First(x => x.ID.LocalId == 37);
            var newEnd = network.ContainedNodes.Values.First(x => x.ID.LocalId == 32);

            var originalVertex = gm.Geometry.GeometryFromId(edge.End.RepresentationReference.GeometryId) as Vertex;
            var polyline = gm.Geometry.GeometryFromId(edge.RepresentationReference.GeometryId) as Polyline;
            Assert.AreEqual(originalVertex, polyline.Edges.Last().EndVertex);

            network.RedirectEdge(edge, false, newEnd);

            var expectedVertex = gm.Geometry.GeometryFromId(newEnd.RepresentationReference.GeometryId) as Vertex;
            Assert.AreEqual(expectedVertex, polyline.Edges.Last().EndVertex);
        }


        [TestMethod]
        public void ConvertToSubnetwork()
        {
            LoadProject(instanceProject);

            //Open Geometry to initialize ComponentExchange
            (var gm, var resource) = ProjectUtils.LoadGeometry("Network.simgeo", projectData, sp);

            var network = projectData.NetworkManager.NetworkRecord.First(x => x.Name == "GeometryNetwork");
            var subNetwork = network.ContainedFlowNetworks.Values.First();
            var middleNode = subNetwork.ContainedNodes.Values.First(x => x.ID.LocalId == 36);

            var middleNetwork = projectData.NetworkManager.ConvertNodeToNetwork(subNetwork, middleNode);

            //Check geometry
            Dictionary<SimFlowNetworkNode, Vertex> vertices = new Dictionary<SimFlowNetworkNode, Vertex>();

            Assert.AreEqual(6, gm.Geometry.Vertices.Count);
            Assert.AreEqual(5, gm.Geometry.ProxyGeometries.Count);

            Assert.AreEqual(4, gm.Geometry.Polylines.Count);
            Assert.AreEqual(5, gm.Geometry.Edges.Count);

            var nw = network;
            while (nw != null)
            {
                foreach (var node in nw.ContainedNodes.Values)
                {
                    Assert.AreEqual(resource.Key, node.RepresentationReference.FileId);
                    Assert.AreNotEqual(ulong.MaxValue, node.RepresentationReference.GeometryId);

                    var vertex = (Vertex)gm.Geometry.GeometryFromId(node.RepresentationReference.GeometryId);
                    Assert.IsNotNull(vertex);
                    vertices.Add(node, vertex);
                }

                Assert.IsTrue(nw.ContainedFlowNetworks.Values.Count <= 1);
                nw = nw.ContainedFlowNetworks.Values.FirstOrDefault();
            }

            nw = network;
            while (nw != null)
            {
                foreach (var edge in nw.ContainedEdges.Values)
                {
                    Assert.AreEqual(resource.Key, edge.RepresentationReference.FileId);
                    Assert.AreNotEqual(ulong.MaxValue, edge.RepresentationReference.GeometryId);

                    var polyline = (Polyline)gm.Geometry.GeometryFromId(edge.RepresentationReference.GeometryId);

                    Assert.IsNotNull(polyline);
                    var start = (edge.Start is SimFlowNetwork) ? ((SimFlowNetwork)edge.Start).ConnectionToParentExitNode : edge.Start;
                    var end = (edge.End is SimFlowNetwork) ? ((SimFlowNetwork)edge.End).ConnectionToParentEntryNode : edge.End;
                    Assert.AreEqual(vertices[start], polyline.Edges.First().StartVertex);
                    Assert.AreEqual(vertices[end], polyline.Edges.Last().EndVertex);
                }

                nw = nw.ContainedFlowNetworks.Values.FirstOrDefault();
            }
        }

        [TestMethod]
        public void ConvertToNetworkNode()
        {
            LoadProject(instanceProject);

            //Open Geometry to initialize ComponentExchange
            (var gm, var resource) = ProjectUtils.LoadGeometry("Network.simgeo", projectData, sp);

            var network = projectData.NetworkManager.NetworkRecord.First(x => x.Name == "GeometryNetwork");
            var subNetwork = network.ContainedFlowNetworks.Values.First();

            var replacedNode = projectData.NetworkManager.ConvertNetworkToNode(network, subNetwork);

            //Check geometry
            Dictionary<SimFlowNetworkNode, Vertex> vertices = new Dictionary<SimFlowNetworkNode, Vertex>();

            Assert.AreEqual(3, gm.Geometry.Vertices.Count);
            Assert.AreEqual(2, gm.Geometry.ProxyGeometries.Count);

            Assert.AreEqual(1, gm.Geometry.Polylines.Count);
            Assert.AreEqual(2, gm.Geometry.Edges.Count);

            var nw = network;
            while (nw != null)
            {
                foreach (var node in nw.ContainedNodes.Values)
                {
                    Assert.AreEqual(resource.Key, node.RepresentationReference.FileId);
                    Assert.AreNotEqual(ulong.MaxValue, node.RepresentationReference.GeometryId);

                    var vertex = (Vertex)gm.Geometry.GeometryFromId(node.RepresentationReference.GeometryId);
                    Assert.IsNotNull(vertex);
                    vertices.Add(node, vertex);
                }

                Assert.IsTrue(nw.ContainedFlowNetworks.Values.Count <= 1);
                nw = nw.ContainedFlowNetworks.Values.FirstOrDefault();
            }

            nw = network;
            while (nw != null)
            {
                foreach (var edge in nw.ContainedEdges.Values)
                {
                    Assert.AreEqual(resource.Key, edge.RepresentationReference.FileId);
                    Assert.AreNotEqual(ulong.MaxValue, edge.RepresentationReference.GeometryId);

                    var polyline = (Polyline)gm.Geometry.GeometryFromId(edge.RepresentationReference.GeometryId);

                    Assert.IsNotNull(polyline);
                    var start = (edge.Start is SimFlowNetwork) ? ((SimFlowNetwork)edge.Start).ConnectionToParentExitNode : edge.Start;
                    var end = (edge.End is SimFlowNetwork) ? ((SimFlowNetwork)edge.End).ConnectionToParentEntryNode : edge.End;
                    Assert.AreEqual(vertices[start], polyline.Edges.First().StartVertex);
                    Assert.AreEqual(vertices[end], polyline.Edges.Last().EndVertex);
                }

                nw = nw.ContainedFlowNetworks.Values.FirstOrDefault();
            }
        }

        #endregion

        #region Names

        [TestMethod]
        public void NodeNameChanged()
        {
            LoadProject(instanceProject);

            var network = projectData.NetworkManager.NetworkRecord.First(x => x.Name == "GeometryNetwork");
            var middleNode = network.ContainedFlowNetworks.Values.First().ContainedNodes.Values.First(x => x.ID.LocalId == 36);

            //Open Geometry to initialize ComponentExchange
            (var gm, var resource) = ProjectUtils.LoadGeometry("Network.simgeo", projectData, sp);
            var vertex = gm.Geometry.GeometryFromId(middleNode.RepresentationReference.GeometryId) as Vertex;

            middleNode.Name = "asdfNode";
            Assert.AreEqual("asdfNode", vertex.Name);
        }

        [TestMethod]
        public void EdgeNameChanged()
        {
            LoadProject(instanceProject);

            var network = projectData.NetworkManager.NetworkRecord.First(x => x.Name == "GeometryNetwork");
            var edge = network.ContainedEdges.Values.First(x => x.Name == "TopEdge");

            //Open Geometry to initialize ComponentExchange
            (var gm, var resource) = ProjectUtils.LoadGeometry("Network.simgeo", projectData, sp);
            var polyline = (Polyline)gm.Geometry.GeometryFromId(edge.RepresentationReference.GeometryId);

            edge.Name = "asdfEdge";
            Assert.AreEqual("asdfEdge", polyline.Name);
        }

        #endregion

        #region Proxies

        [TestMethod]
        public void AssetAdded()
        {
            LoadProject(instanceProject);

            var network = projectData.NetworkManager.NetworkRecord.First(x => x.Name == "GeometryNetwork");
            var middleNode = network.ContainedFlowNetworks.Values.First().ContainedNodes.Values.First(x => x.ID.LocalId == 36);

            //Open Geometry to initialize ComponentExchange
            (var gm, var resource) = ProjectUtils.LoadGeometry("Network.simgeo", projectData, sp);

            var v = (Vertex)gm.Geometry.GeometryFromId(middleNode.RepresentationReference.GeometryId);
            Assert.AreEqual(1, v.ProxyGeometries.Count);
            var p = v.ProxyGeometries.First();

            Assert.AreEqual(24, p.Positions.Count);

            var obj = (ResourceFileEntry)projectData.AssetManager.Resources.First(x => x.Name == "sphere.obj");
            middleNode.Content.Component.AddAsset(obj, "");

            Assert.AreEqual(2880, p.Positions.Count);
        }

        [TestMethod]
        public void RemoveAdded()
        {
            LoadProject(instanceProject);

            var network = projectData.NetworkManager.NetworkRecord.First(x => x.Name == "GeometryNetwork");
            var middleNode = network.ContainedFlowNetworks.Values.First().ContainedNodes.Values.First(x => x.ID.LocalId == 36);

            //Open Geometry to initialize ComponentExchange
            (var gm, var resource) = ProjectUtils.LoadGeometry("Network.simgeo", projectData, sp);

            var v = (Vertex)gm.Geometry.GeometryFromId(middleNode.RepresentationReference.GeometryId);
            Assert.AreEqual(1, v.ProxyGeometries.Count);
            var p = v.ProxyGeometries.First();

            var obj = (ResourceFileEntry)projectData.AssetManager.Resources.First(x => x.Name == "sphere.obj");
            var objasset = middleNode.Content.Component.AddAsset(obj, "");

            Assert.AreEqual(2880, p.Positions.Count);

            middleNode.Content.Component.RemoveAsset(objasset);
            Assert.AreEqual(24, p.Positions.Count);
        }

        #endregion

        #region Proxy Transformation

        [TestMethod]
        public void ProxySizeChanged()
        {
            LoadProject(instanceProject, "arch", "arch");

            var network = projectData.NetworkManager.NetworkRecord.First(x => x.Name == "GeometryNetwork");
            var middleNode = network.ContainedFlowNetworks.Values.First().ContainedNodes.Values.First(x => x.ID.LocalId == 36);

            //Open Geometry to initialize ComponentExchange
            (var gm, var resource) = ProjectUtils.LoadGeometry("Network.simgeo", projectData, sp);

            var v = (Vertex)gm.Geometry.GeometryFromId(middleNode.RepresentationReference.GeometryId);
            var p = v.ProxyGeometries.First();

            Assert.AreEqual(1.0, p.Size.X);
            Assert.AreEqual(1.0, p.Size.Y);
            Assert.AreEqual(1.0, p.Size.Z);

            Assert.AreEqual(1.0, middleNode.Content.InstanceSize.Max.X);
            Assert.AreEqual(1.0, middleNode.Content.InstanceSize.Max.Y);
            Assert.AreEqual(1.0, middleNode.Content.InstanceSize.Max.Z);

            p.Size = new Vector3D(2.0, 3.0, 4.0);

            Assert.AreEqual(2.0, middleNode.Content.InstanceSize.Max.X);
            Assert.AreEqual(3.0, middleNode.Content.InstanceSize.Max.Y);
            Assert.AreEqual(4.0, middleNode.Content.InstanceSize.Max.Z);
        }

        [TestMethod]
        public void InstanceSizeChanged()
        {
            LoadProject(instanceProject);

            var network = projectData.NetworkManager.NetworkRecord.First(x => x.Name == "GeometryNetwork");
            var middleNode = network.ContainedFlowNetworks.Values.First().ContainedNodes.Values.First(x => x.ID.LocalId == 36);

            //Open Geometry to initialize ComponentExchange
            (var gm, var resource) = ProjectUtils.LoadGeometry("Network.simgeo", projectData, sp);

            var v = (Vertex)gm.Geometry.GeometryFromId(middleNode.RepresentationReference.GeometryId);
            var p = v.ProxyGeometries.First();

            Assert.AreEqual(1.0, p.Size.X);
            Assert.AreEqual(1.0, p.Size.Y);
            Assert.AreEqual(1.0, p.Size.Z);

            Assert.AreEqual(1.0, middleNode.Content.InstanceSize.Max.X);
            Assert.AreEqual(1.0, middleNode.Content.InstanceSize.Max.Y);
            Assert.AreEqual(1.0, middleNode.Content.InstanceSize.Max.Z);

            middleNode.Content.InstanceSize = new SimInstanceSize(new Vector3D(1, 1, 1), new Vector3D(2, 3, 4));

            Assert.AreEqual(2.0, p.Size.X);
            Assert.AreEqual(3.0, p.Size.Y);
            Assert.AreEqual(4.0, p.Size.Z);
        }

        [TestMethod]
        public void ProxyRotationChanged()
        {
            LoadProject(instanceProject, "arch", "arch");

            var network = projectData.NetworkManager.NetworkRecord.First(x => x.Name == "GeometryNetwork");
            var middleNode = network.ContainedFlowNetworks.Values.First().ContainedNodes.Values.First(x => x.ID.LocalId == 36);

            //Open Geometry to initialize ComponentExchange
            (var gm, var resource) = ProjectUtils.LoadGeometry("Network.simgeo", projectData, sp);

            var v = (Vertex)gm.Geometry.GeometryFromId(middleNode.RepresentationReference.GeometryId);
            var p = v.ProxyGeometries.First();

            Assert.AreEqual(0.0, p.Rotation.X);
            Assert.AreEqual(0.0, p.Rotation.Y);
            Assert.AreEqual(0.0, p.Rotation.Z);
            Assert.AreEqual(1.0, p.Rotation.W);

            Assert.AreEqual(0.0, middleNode.Content.InstanceRotation.X);
            Assert.AreEqual(0.0, middleNode.Content.InstanceRotation.Y);
            Assert.AreEqual(0.0, middleNode.Content.InstanceRotation.Z);
            Assert.AreEqual(1.0, middleNode.Content.InstanceRotation.W);

            p.Rotation = new Quaternion(0.1, 0.2, 0.3, 0.4);

            Assert.AreEqual(0.1, middleNode.Content.InstanceRotation.X);
            Assert.AreEqual(0.2, middleNode.Content.InstanceRotation.Y);
            Assert.AreEqual(0.3, middleNode.Content.InstanceRotation.Z);
            Assert.AreEqual(0.4, middleNode.Content.InstanceRotation.W);
        }

        [TestMethod]
        public void InstanceRotationChanged()
        {
            LoadProject(instanceProject);

            var network = projectData.NetworkManager.NetworkRecord.First(x => x.Name == "GeometryNetwork");
            var middleNode = network.ContainedFlowNetworks.Values.First().ContainedNodes.Values.First(x => x.ID.LocalId == 36);

            //Open Geometry to initialize ComponentExchange
            (var gm, var resource) = ProjectUtils.LoadGeometry("Network.simgeo", projectData, sp);

            var v = (Vertex)gm.Geometry.GeometryFromId(middleNode.RepresentationReference.GeometryId);
            var p = v.ProxyGeometries.First();

            Assert.AreEqual(0.0, p.Rotation.X);
            Assert.AreEqual(0.0, p.Rotation.Y);
            Assert.AreEqual(0.0, p.Rotation.Z);
            Assert.AreEqual(1.0, p.Rotation.W);

            Assert.AreEqual(0.0, middleNode.Content.InstanceRotation.X);
            Assert.AreEqual(0.0, middleNode.Content.InstanceRotation.Y);
            Assert.AreEqual(0.0, middleNode.Content.InstanceRotation.Z);
            Assert.AreEqual(1.0, middleNode.Content.InstanceRotation.W);

            middleNode.Content.InstanceRotation = new Quaternion(0.1, 0.2, 0.3, 0.4);

            Assert.AreEqual(0.1, p.Rotation.X);
            Assert.AreEqual(0.2, p.Rotation.Y);
            Assert.AreEqual(0.3, p.Rotation.Z);
            Assert.AreEqual(0.4, p.Rotation.W);
        }

        [TestMethod]
        public void InstanceSizeAfterAdd()
        {
            LoadProject(instanceProject);

            var network = projectData.NetworkManager.NetworkRecord.First(x => x.Name == "GeometryNetwork")
                .ContainedFlowNetworks.Values.First();

            //Open Geometry to initialize ComponentExchange
            (var gm, var resource) = ProjectUtils.LoadGeometry("Network.simgeo", projectData, sp);
            Assert.AreEqual(5, gm.Geometry.Vertices.Count);

            var node = network.ContainedNodes.Values.First(x => x.ID.LocalId == 32);
            var v = gm.Geometry.GeometryFromId(node.RepresentationReference.GeometryId) as Vertex;
            var p = v.ProxyGeometries.First();
            var comp = projectData.Components.First(x => x.Name == "gmUnusedNode");

            p.Size = new Vector3D(2.0, 3.0, 4.0);

            //Create instance
            var instance = new SimComponentInstance(node, new Point(0, 0));
            comp.Instances.Add(instance);

            Assert.AreEqual(2.0, node.Content.InstanceSize.Max.X);
            Assert.AreEqual(3.0, node.Content.InstanceSize.Max.Y);
            Assert.AreEqual(4.0, node.Content.InstanceSize.Max.Z);
        }

        [TestMethod]
        public void InstanceRotationAfterAdd()
        {
            LoadProject(instanceProject);

            var network = projectData.NetworkManager.NetworkRecord.First(x => x.Name == "GeometryNetwork")
                .ContainedFlowNetworks.Values.First();

            //Open Geometry to initialize ComponentExchange
            (var gm, var resource) = ProjectUtils.LoadGeometry("Network.simgeo", projectData, sp);
            Assert.AreEqual(5, gm.Geometry.Vertices.Count);

            var node = network.ContainedNodes.Values.First(x => x.ID.LocalId == 32);
            var v = gm.Geometry.GeometryFromId(node.RepresentationReference.GeometryId) as Vertex;
            var p = v.ProxyGeometries.First();
            var comp = projectData.Components.First(x => x.Name == "gmUnusedNode");

            p.Rotation = new Quaternion(0.1, 0.2, 0.3, 0.4);

            //Create instance
            var instance = new SimComponentInstance(node, new Point(0, 0));
            comp.Instances.Add(instance);

            Assert.AreEqual(0.1, node.Content.InstanceRotation.X);
            Assert.AreEqual(0.2, node.Content.InstanceRotation.Y);
            Assert.AreEqual(0.3, node.Content.InstanceRotation.Z);
            Assert.AreEqual(0.4, node.Content.InstanceRotation.W);
        }

        #endregion

        #region Colors


        [TestMethod]
        public void InitialNodeColorEmpty()
        {
            LoadProject(instanceProject);

            var network = projectData.NetworkManager.NetworkRecord.First(x => x.Name == "GeometryNetwork");

            var n = network.ContainedFlowNetworks.Values.First().ContainedNodes.Values.First(x => x.ID.LocalId == 32);

            //Open Geometry to initialize ComponentExchange
            (var gm, var resource) = ProjectUtils.LoadGeometry("Network.simgeo", projectData, sp);

            var vertex = gm.Geometry.GeometryFromId(n.RepresentationReference.GeometryId);

            Assert.AreEqual(NetworkColors.COL_EMPTY, vertex.Color.Color);
            Assert.AreEqual(false, vertex.Color.IsFromParent);
        }

        [TestMethod]
        public void InitialNodeColorNeutral()
        {
            LoadProject(instanceProject);

            var network = projectData.NetworkManager.NetworkRecord.First(x => x.Name == "GeometryNetwork");

            var n = network.ContainedNodes.Values.First(x => x.ID.LocalId == 27);

            //Open Geometry to initialize ComponentExchange
            (var gm, var resource) = ProjectUtils.LoadGeometry("Network.simgeo", projectData, sp);

            var vertex = gm.Geometry.GeometryFromId(n.RepresentationReference.GeometryId);

            Assert.AreEqual(NetworkColors.COL_NEUTRAL, vertex.Color.Color);
            Assert.AreEqual(true, vertex.Color.IsFromParent);
        }

        [TestMethod]
        public void InitialEdgeColorEmpty()
        {
            LoadProject(instanceProject);

            var network = projectData.NetworkManager.NetworkRecord.First(x => x.Name == "GeometryNetwork");

            var e = network.ContainedFlowNetworks.Values.First().ContainedEdges.Values.First(x => x.ID.LocalId == 38);

            //Open Geometry to initialize ComponentExchange
            (var gm, var resource) = ProjectUtils.LoadGeometry("Network.simgeo", projectData, sp);

            var polyline = (Polyline)gm.Geometry.GeometryFromId(e.RepresentationReference.GeometryId);

            Assert.AreEqual(NetworkColors.COL_EMPTY, polyline.Color.Color);
            Assert.AreEqual(false, polyline.Color.IsFromParent);

            foreach (var pe in polyline.Edges)
            {
                Assert.AreEqual(NetworkColors.COL_EMPTY, pe.Edge.Color.Color);
                Assert.AreEqual(false, pe.Edge.Color.IsFromParent);
            }
        }

        [TestMethod]
        public void InitialEdgeColorNeutral()
        {
            LoadProject(instanceProject);

            var network = projectData.NetworkManager.NetworkRecord.First(x => x.Name == "GeometryNetwork");

            var e = network.ContainedFlowNetworks.Values.First().ContainedEdges.Values.First(x => x.ID.LocalId == 37);

            //Open Geometry to initialize ComponentExchange
            (var gm, var resource) = ProjectUtils.LoadGeometry("Network.simgeo", projectData, sp);

            var polyline = (Polyline)gm.Geometry.GeometryFromId(e.RepresentationReference.GeometryId);

            Assert.AreEqual(NetworkColors.COL_NEUTRAL, polyline.Color.Color);
            Assert.AreEqual(true, polyline.Color.IsFromParent);

            foreach (var pe in polyline.Edges)
            {
                Assert.AreEqual(NetworkColors.COL_NEUTRAL, pe.Edge.Color.Color);
                Assert.AreEqual(true, pe.Edge.Color.IsFromParent);
            }
        }

        [TestMethod]
        public void NodeColorChangeInstanceAdded()
        {
            LoadProject(instanceProject);

            var network = projectData.NetworkManager.NetworkRecord.First(x => x.Name == "GeometryNetwork");

            var n = network.ContainedFlowNetworks.Values.First().ContainedNodes.Values.First(x => x.ID.LocalId == 36);
            var comp = projectData.Components.First(x => x.Name == "gmUnusedNode");

            //Open Geometry to initialize ComponentExchange
            (var gm, var resource) = ProjectUtils.LoadGeometry("Network.simgeo", projectData, sp);

            comp.Instances.Add(new SimComponentInstance(n, new Point(0, 0)));
            var vertex = gm.Geometry.GeometryFromId(n.RepresentationReference.GeometryId);

            Assert.AreEqual(NetworkColors.COL_NEUTRAL, vertex.Color.Color);
            Assert.AreEqual(true, vertex.Color.IsFromParent);
        }

        [TestMethod]
        public void NodeColorChangeInstanceRemoved()
        {
            LoadProject(instanceProject);

            var network = projectData.NetworkManager.NetworkRecord.First(x => x.Name == "GeometryNetwork");

            var n = network.ContainedFlowNetworks.Values.First().ContainedNodes.Values.First(x => x.ID.LocalId == 36);

            //Open Geometry to initialize ComponentExchange
            (var gm, var resource) = ProjectUtils.LoadGeometry("Network.simgeo", projectData, sp);

            n.Content.Component.Instances.Remove(n.Content);
            var vertex = gm.Geometry.GeometryFromId(n.RepresentationReference.GeometryId);

            Assert.AreEqual(NetworkColors.COL_EMPTY, vertex.Color.Color);
            Assert.AreEqual(false, vertex.Color.IsFromParent);
        }

        [TestMethod]
        public void EdgeColorChangeInstanceAdded()
        {
            LoadProject(instanceProject);

            var network = projectData.NetworkManager.NetworkRecord.First(x => x.Name == "GeometryNetwork");

            var e = network.ContainedFlowNetworks.Values.First().ContainedEdges.Values.First(x => x.ID.LocalId == 38);
            var comp = projectData.Components.First(x => x.Name == "gmUnusedEdge");

            //Open Geometry to initialize ComponentExchange
            (var gm, var resource) = ProjectUtils.LoadGeometry("Network.simgeo", projectData, sp);

            comp.Instances.Add(new SimComponentInstance(e, new Point(0, 0)));

            var polyline = (Polyline)gm.Geometry.GeometryFromId(e.RepresentationReference.GeometryId);

            Assert.AreEqual(NetworkColors.COL_NEUTRAL, polyline.Color.Color);
            Assert.AreEqual(true, polyline.Color.IsFromParent);

            foreach (var pe in polyline.Edges)
            {
                Assert.AreEqual(NetworkColors.COL_NEUTRAL, pe.Edge.Color.Color);
                Assert.AreEqual(true, pe.Edge.Color.IsFromParent);
            }
        }

        [TestMethod]
        public void EdgeColorChangeInstanceRemoved()
        {
            LoadProject(instanceProject);

            var network = projectData.NetworkManager.NetworkRecord.First(x => x.Name == "GeometryNetwork");

            var e = network.ContainedFlowNetworks.Values.First().ContainedEdges.Values.First(x => x.ID.LocalId == 37);

            //Open Geometry to initialize ComponentExchange
            (var gm, var resource) = ProjectUtils.LoadGeometry("Network.simgeo", projectData, sp);

            e.Content.Component.Instances.Remove(e.Content);

            var polyline = (Polyline)gm.Geometry.GeometryFromId(e.RepresentationReference.GeometryId);

            Assert.AreEqual(NetworkColors.COL_EMPTY, polyline.Color.Color);
            Assert.AreEqual(false, polyline.Color.IsFromParent);

            foreach (var pe in polyline.Edges)
            {
                Assert.AreEqual(NetworkColors.COL_EMPTY, pe.Edge.Color.Color);
                Assert.AreEqual(false, pe.Edge.Color.IsFromParent);
            }
        }

        #endregion
    }
}
