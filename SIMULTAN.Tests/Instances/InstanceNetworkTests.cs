using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data.Components;
using SIMULTAN.Tests.Utils;
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
    public class InstanceNetworkTests : BaseProjectTest
    {
        private static readonly FileInfo instanceProject = new FileInfo(@".\InstanceTestsProject.simultan");

        #region Network Instances

        [TestMethod]
        public void AddNetworkNodeInstance()
        {
            LoadProject(instanceProject);

            var nodeComponent = projectData.Components.First(x => x.Name == "Node");
            var network = projectData.NetworkManager.NetworkRecord.First(x => x.Name == "Network");
            var node = network.ContainedNodes.Values.First(x => x.Name == "Node 912f497f-2a73-4798-85d6-bdd365da555f: 2");

            Assert.AreEqual(null, node.Content);

            var instance = new SimComponentInstance(node, new Point(0, 0));
            nodeComponent.Instances.Add(instance);

            Assert.AreEqual(instance, node.Content);
            Assert.AreEqual(nodeComponent, instance.Component);

            Assert.AreEqual(1, instance.Placements.Count);
            Assert.AreEqual(new SimInstanceState(false, SimInstanceConnectionState.Ok), instance.State);

            var placement = instance.Placements[0] as SimInstancePlacementNetwork;
            Assert.AreEqual(instance, placement.Instance);
            Assert.AreEqual(node, placement.NetworkElement);
            Assert.AreEqual(SimInstancePlacementState.Valid, placement.State);

            var nodePos = new Point3D(node.Position.X * SimInstancePlacementNetwork.SCALE_PIXEL_TO_M, 0.0, node.Position.Y * SimInstancePlacementNetwork.SCALE_PIXEL_TO_M);
            Assert.AreEqual(1, instance.InstancePath.Count);
            Assert.AreEqual(nodePos, instance.InstancePath[0]);
        }

        [TestMethod]
        public void RemoveNodeInstance()
        {
            LoadProject(instanceProject);

            var nodeComponent = projectData.Components.First(x => x.Name == "Node");
            var network = projectData.NetworkManager.NetworkRecord.First(x => x.Name == "Network");
            var node = network.ContainedNodes.Values.First(x => x.Name == "Node 912f497f-2a73-4798-85d6-bdd365da555f: 2");

            var instance = new SimComponentInstance(node, new Point(0, 0));
            nodeComponent.Instances.Add(instance);

            Assert.AreEqual(instance, node.Content);

            nodeComponent.Instances.Remove(instance);

            Assert.AreEqual(null, node.Content);
            Assert.AreEqual(null, instance.Component);
        }

        [TestMethod]
        public void RemoveNodePlacement()
        {
            LoadProject(instanceProject);

            var nodeComponent = projectData.Components.First(x => x.Name == "Node");
            var network = projectData.NetworkManager.NetworkRecord.First(x => x.Name == "Network");
            var node = network.ContainedNodes.Values.First(x => x.Name == "Node 912f497f-2a73-4798-85d6-bdd365da555f: 2");

            var instance = new SimComponentInstance(node, new Point(0, 0));
            nodeComponent.Instances.Add(instance);

            Assert.AreEqual(instance, node.Content);

            instance.Placements.Remove(instance.Placements.FirstOrDefault(x => x is SimInstancePlacementNetwork));

            Assert.AreEqual(null, node.Content);
            Assert.AreEqual(nodeComponent, instance.Component);
        }

        [TestMethod]
        public void NodeInstanceMemoryLeak()
        {
            LoadProject(instanceProject);

            var nodeComponent = projectData.Components.First(x => x.Name == "Node");
            var network = projectData.NetworkManager.NetworkRecord.First(x => x.Name == "Network");
            var node = network.ContainedNodes.Values.First(x => x.Name == "Node 912f497f-2a73-4798-85d6-bdd365da555f: 2");

            var instance = new SimComponentInstance(node, new Point(0, 0));
            WeakReference instRef = new WeakReference(instance);

            nodeComponent.Instances.Add(instance);

            Assert.AreEqual(instance, node.Content);

            nodeComponent.Instances.Remove(instance);
            instance = null;

            Assert.AreEqual(null, node.Content);

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            Assert.IsFalse(instRef.IsAlive);

        }

        [TestMethod]
        public void NodePlacementMemoryLeak()
        {
            LoadProject(instanceProject);

            var nodeComponent = projectData.Components.First(x => x.Name == "Node");
            var network = projectData.NetworkManager.NetworkRecord.First(x => x.Name == "Network");
            var node = network.ContainedNodes.Values.First(x => x.Name == "Node 912f497f-2a73-4798-85d6-bdd365da555f: 2");

            var instance = new SimComponentInstance(node, new Point(0, 0));

            nodeComponent.Instances.Add(instance);

            Assert.AreEqual(instance, node.Content);

            var placement = instance.Placements.FirstOrDefault(x => x is SimInstancePlacementNetwork);
            var placementRef = new WeakReference(placement);
            instance.Placements.Remove(placement);
            placement = null;

            Assert.AreEqual(null, node.Content);

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            Assert.IsFalse(placementRef.IsAlive);

        }



        [TestMethod]
        public void AddNetworkEdgeInstance()
        {
            LoadProject(instanceProject);

            var nodeComponent = projectData.Components.First(x => x.Name == "Edge");
            var network = projectData.NetworkManager.NetworkRecord.First(x => x.Name == "Network");
            var edge = network.ContainedEdges.Values.First(x => x.Name == "Edge 4");

            Assert.AreEqual(null, edge.Content);

            var instance = new SimComponentInstance(edge, new Point(0, 0));
            nodeComponent.Instances.Add(instance);

            Assert.AreEqual(instance, edge.Content);
            Assert.AreEqual(nodeComponent, instance.Component);

            Assert.AreEqual(1, instance.Placements.Count);
            Assert.AreEqual(new SimInstanceState(false, SimInstanceConnectionState.Ok), instance.State);

            var placement = instance.Placements[0] as SimInstancePlacementNetwork;
            Assert.AreEqual(instance, placement.Instance);
            Assert.AreEqual(edge, placement.NetworkElement);
            Assert.AreEqual(SimInstancePlacementState.Valid, placement.State);

            var startPos = new Point3D(edge.Start.Position.X * SimInstancePlacementNetwork.SCALE_PIXEL_TO_M, 0.0, edge.Start.Position.Y * SimInstancePlacementNetwork.SCALE_PIXEL_TO_M);
            var endPos = new Point3D(edge.End.Position.X * SimInstancePlacementNetwork.SCALE_PIXEL_TO_M, 0.0, edge.End.Position.Y * SimInstancePlacementNetwork.SCALE_PIXEL_TO_M);
            Assert.AreEqual(2, instance.InstancePath.Count);
            Assert.AreEqual(startPos, instance.InstancePath[0]);
            Assert.AreEqual(endPos, instance.InstancePath[1]);
        }

        [TestMethod]
        public void RemoveEdgeInstance()
        {
            LoadProject(instanceProject);

            var nodeComponent = projectData.Components.First(x => x.Name == "Edge");
            var network = projectData.NetworkManager.NetworkRecord.First(x => x.Name == "Network");
            var edge = network.ContainedEdges.Values.First(x => x.Name == "Edge 4");

            var instance = new SimComponentInstance(edge, new Point(0, 0));
            nodeComponent.Instances.Add(instance);

            Assert.AreEqual(instance, edge.Content);

            nodeComponent.Instances.Remove(instance);

            Assert.AreEqual(null, edge.Content);
            Assert.AreEqual(null, instance.Component);
        }

        [TestMethod]
        public void RemoveEdgePlacement()
        {
            LoadProject(instanceProject);

            var nodeComponent = projectData.Components.First(x => x.Name == "Edge");
            var network = projectData.NetworkManager.NetworkRecord.First(x => x.Name == "Network");
            var edge = network.ContainedEdges.Values.First(x => x.Name == "Edge 4");

            var instance = new SimComponentInstance(edge, new Point(0, 0));
            nodeComponent.Instances.Add(instance);

            Assert.AreEqual(instance, edge.Content);

            instance.Placements.Remove(instance.Placements.FirstOrDefault(x => x is SimInstancePlacementNetwork));

            Assert.AreEqual(null, edge.Content);
            Assert.AreEqual(nodeComponent, instance.Component);
        }

        [TestMethod]
        public void EdgeInstanceMemoryLeak()
        {
            LoadProject(instanceProject);

            var nodeComponent = projectData.Components.First(x => x.Name == "Edge");
            var network = projectData.NetworkManager.NetworkRecord.First(x => x.Name == "Network");
            var edge = network.ContainedEdges.Values.First(x => x.Name == "Edge 4");

            var instance = new SimComponentInstance(edge, new Point(0, 0));
            WeakReference instRef = new WeakReference(instance);

            nodeComponent.Instances.Add(instance);

            Assert.AreEqual(instance, edge.Content);

            nodeComponent.Instances.Remove(instance);
            instance = null;

            Assert.AreEqual(null, edge.Content);

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            Assert.IsFalse(instRef.IsAlive);

        }

        [TestMethod]
        public void EdgePlacementMemoryLeak()
        {
            LoadProject(instanceProject);

            var nodeComponent = projectData.Components.First(x => x.Name == "Edge");
            var network = projectData.NetworkManager.NetworkRecord.First(x => x.Name == "Network");
            var edge = network.ContainedEdges.Values.First(x => x.Name == "Edge 4");

            var instance = new SimComponentInstance(edge, new Point(0, 0));

            nodeComponent.Instances.Add(instance);

            Assert.AreEqual(instance, edge.Content);

            var placement = instance.Placements.FirstOrDefault(x => x is SimInstancePlacementNetwork);
            var placementRef = new WeakReference(placement);
            instance.Placements.Remove(placement);
            placement = null;

            Assert.AreEqual(null, edge.Content);

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            Assert.IsFalse(placementRef.IsAlive);

        }

        #endregion

        #region InstanceSize

        [TestMethod]
        public void CumulativeChildAddNode()
        {
            LoadProject(instanceProject);

            var nodeComponent = projectData.Components.First(x => x.Name == "Node");
            var network = projectData.NetworkManager.NetworkRecord.First(x => x.Name == "Network");
            var node = network.ContainedNodes.Values.First(x => x.Name == "Node 912f497f-2a73-4798-85d6-bdd365da555f: 2");

            var instance = new SimComponentInstance(node, new Point(0, 0));
            nodeComponent.Instances.Add(instance);

            var cumulativeComponent = nodeComponent.Components.FirstOrDefault(x => x.Component != null && x.Component.Name == "Cumulative")?.Component;
            Assert.AreNotEqual(null, cumulativeComponent);

            List<string> cumulativeParameterKeys = new List<string>
            {
                ReservedParameterKeys.RP_LENGTH_MIN_TOTAL,
                ReservedParameterKeys.RP_AREA_MIN_TOTAL,
                ReservedParameterKeys.RP_VOLUME_MIN_TOTAL,
                ReservedParameterKeys.RP_LENGTH_MAX_TOTAL,
                ReservedParameterKeys.RP_AREA_MAX_TOTAL,
                ReservedParameterKeys.RP_VOLUME_MAX_TOTAL,
                ReservedParameterKeys.RP_COUNT,
            };

            foreach (var pKey in cumulativeParameterKeys)
            {
                Assert.AreNotEqual(null, cumulativeComponent.Parameters.FirstOrDefault(x => x.HasReservedTaxonomyEntry(pKey)));
            }
        }

        [TestMethod]
        public void CumulativeChildAddEdge()
        {
            LoadProject(instanceProject);

            var nodeComponent = projectData.Components.First(x => x.Name == "Edge");
            var network = projectData.NetworkManager.NetworkRecord.First(x => x.Name == "Network");
            var node = network.ContainedEdges.Values.First(x => x.Name == "Edge 4");

            var instance = new SimComponentInstance(node, new Point(0, 0));
            nodeComponent.Instances.Add(instance);

            var cumulativeComponent = nodeComponent.Components.FirstOrDefault(x => x.Component != null && x.Component.Name == "Cumulative")?.Component;
            Assert.AreNotEqual(null, cumulativeComponent);

            List<string> cumulativeParameterKeys = new List<string>
            {
                ReservedParameterKeys.RP_LENGTH_MIN_TOTAL,
                ReservedParameterKeys.RP_AREA_MIN_TOTAL,
                ReservedParameterKeys.RP_VOLUME_MIN_TOTAL,
                ReservedParameterKeys.RP_LENGTH_MAX_TOTAL,
                ReservedParameterKeys.RP_AREA_MAX_TOTAL,
                ReservedParameterKeys.RP_VOLUME_MAX_TOTAL,
                ReservedParameterKeys.RP_COUNT,
            };

            foreach (var pKey in cumulativeParameterKeys)
            {
                Assert.AreNotEqual(null, cumulativeComponent.Parameters.FirstOrDefault(x => x.HasReservedTaxonomyEntry(pKey)));
            }
        }

        [TestMethod]
        public void CumulativeChildUpdate()
        {
            LoadProject(instanceProject);

            var nodeComponent = projectData.Components.First(x => x.Name == "Node");
            var network = projectData.NetworkManager.NetworkRecord.First(x => x.Name == "Network");
            var node = network.ContainedNodes.Values.First(x => x.Name == "Node 912f497f-2a73-4798-85d6-bdd365da555f: 2");

            var instance = new SimComponentInstance(node, new Point(0, 0));
            nodeComponent.Instances.Add(instance);

            var cumulativeComponent = nodeComponent.Components.FirstOrDefault(x => x.Component != null && x.Component.Name == "Cumulative")?.Component;
            Assert.AreNotEqual(null, cumulativeComponent);

            Dictionary<string, SimParameter> cumulativeParameters = new Dictionary<string, SimParameter>
            {
                { ReservedParameterKeys.RP_LENGTH_MIN_TOTAL, null },
                { ReservedParameterKeys.RP_AREA_MIN_TOTAL, null },
                { ReservedParameterKeys.RP_VOLUME_MIN_TOTAL, null },
                { ReservedParameterKeys.RP_LENGTH_MAX_TOTAL, null },
                { ReservedParameterKeys.RP_AREA_MAX_TOTAL, null },
                { ReservedParameterKeys.RP_VOLUME_MAX_TOTAL, null },
                { ReservedParameterKeys.RP_COUNT, null },
            };

            foreach (var pKey in cumulativeParameters.Keys.ToList())
                cumulativeParameters[pKey] = cumulativeComponent.Parameters.FirstOrDefault(x => x.HasReservedTaxonomyEntry(pKey));

            AssertUtil.AssertDoubleEqual(0.0, cumulativeParameters[ReservedParameterKeys.RP_LENGTH_MIN_TOTAL].ValueCurrent);
            AssertUtil.AssertDoubleEqual(1.0, cumulativeParameters[ReservedParameterKeys.RP_LENGTH_MAX_TOTAL].ValueCurrent);
            AssertUtil.AssertDoubleEqual(0.0, cumulativeParameters[ReservedParameterKeys.RP_AREA_MIN_TOTAL].ValueCurrent);
            AssertUtil.AssertDoubleEqual(1.0, cumulativeParameters[ReservedParameterKeys.RP_AREA_MAX_TOTAL].ValueCurrent);
            AssertUtil.AssertDoubleEqual(0.0, cumulativeParameters[ReservedParameterKeys.RP_VOLUME_MIN_TOTAL].ValueCurrent);
            AssertUtil.AssertDoubleEqual(1.0, cumulativeParameters[ReservedParameterKeys.RP_VOLUME_MAX_TOTAL].ValueCurrent);
            AssertUtil.AssertDoubleEqual(1.0, cumulativeParameters[ReservedParameterKeys.RP_COUNT].ValueCurrent);

            instance.InstanceSize = new SimInstanceSize(new Vector3D(1.0, 2.0, 3.0), new Vector3D(4.0, 5.0, 6.0));

            AssertUtil.AssertDoubleEqual(3.0, cumulativeParameters[ReservedParameterKeys.RP_LENGTH_MIN_TOTAL].ValueCurrent);
            AssertUtil.AssertDoubleEqual(6.0, cumulativeParameters[ReservedParameterKeys.RP_LENGTH_MAX_TOTAL].ValueCurrent);
            AssertUtil.AssertDoubleEqual(2.0, cumulativeParameters[ReservedParameterKeys.RP_AREA_MIN_TOTAL].ValueCurrent);
            AssertUtil.AssertDoubleEqual(20.0, cumulativeParameters[ReservedParameterKeys.RP_AREA_MAX_TOTAL].ValueCurrent);
            AssertUtil.AssertDoubleEqual(6.0, cumulativeParameters[ReservedParameterKeys.RP_VOLUME_MIN_TOTAL].ValueCurrent);
            AssertUtil.AssertDoubleEqual(120.0, cumulativeParameters[ReservedParameterKeys.RP_VOLUME_MAX_TOTAL].ValueCurrent);
            AssertUtil.AssertDoubleEqual(1.0, cumulativeParameters[ReservedParameterKeys.RP_COUNT].ValueCurrent);
        }

        [TestMethod]
        public void CumulativeChildMultipleInstances()
        {
            LoadProject(instanceProject);

            var nodeComponent = projectData.Components.First(x => x.Name == "Node2");
            var network = projectData.NetworkManager.NetworkRecord.First(x => x.Name == "Network");
            var node = network.ContainedNodes.Values.First(x => x.Name == "Node 912f497f-2a73-4798-85d6-bdd365da555f: 2");

            var instance = new SimComponentInstance(node, new Point(0, 0));
            nodeComponent.Instances.Add(instance);

            var cumulativeComponent = nodeComponent.Components.FirstOrDefault(x => x.Component != null && x.Component.Name == "Cumulative")?.Component;
            Assert.AreNotEqual(null, cumulativeComponent);

            Dictionary<string, SimParameter> cumulativeParameters = new Dictionary<string, SimParameter>
            {
                { ReservedParameterKeys.RP_LENGTH_MIN_TOTAL, null },
                { ReservedParameterKeys.RP_AREA_MIN_TOTAL, null },
                { ReservedParameterKeys.RP_VOLUME_MIN_TOTAL, null },
                { ReservedParameterKeys.RP_LENGTH_MAX_TOTAL, null },
                { ReservedParameterKeys.RP_AREA_MAX_TOTAL, null },
                { ReservedParameterKeys.RP_VOLUME_MAX_TOTAL, null },
                { ReservedParameterKeys.RP_COUNT, null },
            };

            foreach (var pKey in cumulativeParameters.Keys.ToList())
                cumulativeParameters[pKey] = cumulativeComponent.Parameters.FirstOrDefault(x => x.HasReservedTaxonomyEntry(pKey));

            AssertUtil.AssertDoubleEqual(3.0,   cumulativeParameters[ReservedParameterKeys.RP_LENGTH_MIN_TOTAL].ValueCurrent);
            AssertUtil.AssertDoubleEqual(7.0,   cumulativeParameters[ReservedParameterKeys.RP_LENGTH_MAX_TOTAL].ValueCurrent);
            AssertUtil.AssertDoubleEqual(2.0,   cumulativeParameters[ReservedParameterKeys.RP_AREA_MIN_TOTAL].ValueCurrent);
            AssertUtil.AssertDoubleEqual(21.0,  cumulativeParameters[ReservedParameterKeys.RP_AREA_MAX_TOTAL].ValueCurrent);
            AssertUtil.AssertDoubleEqual(6.0,   cumulativeParameters[ReservedParameterKeys.RP_VOLUME_MIN_TOTAL].ValueCurrent);
            AssertUtil.AssertDoubleEqual(121.0, cumulativeParameters[ReservedParameterKeys.RP_VOLUME_MAX_TOTAL].ValueCurrent);
            AssertUtil.AssertDoubleEqual(2.0,   cumulativeParameters[ReservedParameterKeys.RP_COUNT].ValueCurrent);

            instance.InstanceSize = new SimInstanceSize(new Vector3D(1.0, 2.0, 3.0), new Vector3D(4.0, 5.0, 6.0));

            AssertUtil.AssertDoubleEqual(6.0,   cumulativeParameters[ReservedParameterKeys.RP_LENGTH_MIN_TOTAL].ValueCurrent);
            AssertUtil.AssertDoubleEqual(12.0,  cumulativeParameters[ReservedParameterKeys.RP_LENGTH_MAX_TOTAL].ValueCurrent);
            AssertUtil.AssertDoubleEqual(4.0,   cumulativeParameters[ReservedParameterKeys.RP_AREA_MIN_TOTAL].ValueCurrent);
            AssertUtil.AssertDoubleEqual(40.0,  cumulativeParameters[ReservedParameterKeys.RP_AREA_MAX_TOTAL].ValueCurrent);
            AssertUtil.AssertDoubleEqual(12.0,  cumulativeParameters[ReservedParameterKeys.RP_VOLUME_MIN_TOTAL].ValueCurrent);
            AssertUtil.AssertDoubleEqual(240.0, cumulativeParameters[ReservedParameterKeys.RP_VOLUME_MAX_TOTAL].ValueCurrent);
            AssertUtil.AssertDoubleEqual(2.0,   cumulativeParameters[ReservedParameterKeys.RP_COUNT].ValueCurrent);

            nodeComponent.Instances.Remove(instance);

            AssertUtil.AssertDoubleEqual(3.0,   cumulativeParameters[ReservedParameterKeys.RP_LENGTH_MIN_TOTAL].ValueCurrent);
            AssertUtil.AssertDoubleEqual(6.0,   cumulativeParameters[ReservedParameterKeys.RP_LENGTH_MAX_TOTAL].ValueCurrent);
            AssertUtil.AssertDoubleEqual(2.0,   cumulativeParameters[ReservedParameterKeys.RP_AREA_MIN_TOTAL].ValueCurrent);
            AssertUtil.AssertDoubleEqual(20.0,  cumulativeParameters[ReservedParameterKeys.RP_AREA_MAX_TOTAL].ValueCurrent);
            AssertUtil.AssertDoubleEqual(6.0,   cumulativeParameters[ReservedParameterKeys.RP_VOLUME_MIN_TOTAL].ValueCurrent);
            AssertUtil.AssertDoubleEqual(120.0, cumulativeParameters[ReservedParameterKeys.RP_VOLUME_MAX_TOTAL].ValueCurrent);
            AssertUtil.AssertDoubleEqual(1.0,   cumulativeParameters[ReservedParameterKeys.RP_COUNT].ValueCurrent);

        }

        #endregion

        #region Path

        [TestMethod]
        public void PathChangedNode()
        {
            LoadProject(instanceProject, "arch", "arch");

            var network = projectData.NetworkManager.NetworkRecord.First(x => x.Name == "TopNetwork");
            var node = network.ContainedNodes.Values.First(x => x.Name == "Start");

            var instance = node.Content;

            var initialPos = new Point3D(node.Position.X * SimInstancePlacementNetwork.SCALE_PIXEL_TO_M, 0.0, node.Position.Y * SimInstancePlacementNetwork.SCALE_PIXEL_TO_M);
            Assert.AreEqual(1, instance.InstancePath.Count);
            Assert.AreEqual(initialPos, instance.InstancePath[0]);

            var newPosNW = node.Position + new Vector(3, 3);
            var newPos = new Point3D(newPosNW.X * SimInstancePlacementNetwork.SCALE_PIXEL_TO_M,
                0.0, newPosNW.Y * SimInstancePlacementNetwork.SCALE_PIXEL_TO_M);

            node.Position = newPosNW;
            Assert.AreEqual(1, instance.InstancePath.Count);
            Assert.AreEqual(newPos, instance.InstancePath[0]);
        }

        [TestMethod]
        public void PathChangedNodeWithGeometry()
        {
            LoadProject(instanceProject, "arch", "arch");

            var network = projectData.NetworkManager.NetworkRecord.First(x => x.Name == "GeometryNetwork");
            var node = network.ContainedNodes.Values.First(x => x.Name == "Start");

            var instance = node.Content;

            var initialPos = instance.InstancePath[0];

            var newPosNW = node.Position + new Vector(3, 3);

            //Changing the position shouldn't do anything. Path comes from geometry
            node.Position = newPosNW;
            Assert.AreEqual(1, instance.InstancePath.Count);
            Assert.AreEqual(initialPos, instance.InstancePath[0]);
        }

        [TestMethod]
        public void PathChangedEdge()
        {
            LoadProject(instanceProject, "arch", "arch");

            var network = projectData.NetworkManager.NetworkRecord.First(x => x.Name == "TopNetwork");
            var node = network.ContainedNodes.Values.First(x => x.Name == "Start");
            var subNet = network.ContainedFlowNetworks.Values.First(x => x.Name == "SubNet1");
            var edge = network.ContainedEdges.Values.First(x => x.Name == "TopEdge1");

            var instance = edge.Content;

            var initialEnd = instance.InstancePath[1];
            var initialPos = new Point3D(node.Position.X * SimInstancePlacementNetwork.SCALE_PIXEL_TO_M, 0.0, node.Position.Y * SimInstancePlacementNetwork.SCALE_PIXEL_TO_M);
            Assert.AreEqual(2, instance.InstancePath.Count);
            Assert.AreEqual(initialPos, instance.InstancePath[0]);

            //Start Node changed
            var newPosNodeNW = node.Position + new Vector(3, 3);
            var newNodePos = new Point3D(newPosNodeNW.X * SimInstancePlacementNetwork.SCALE_PIXEL_TO_M,
                0.0, newPosNodeNW.Y * SimInstancePlacementNetwork.SCALE_PIXEL_TO_M);

            node.Position = newPosNodeNW;
            Assert.AreEqual(2, instance.InstancePath.Count);
            Assert.AreEqual(newNodePos, instance.InstancePath[0]);
            Assert.AreEqual(initialEnd, instance.InstancePath[1]);

            //End Network changed
            var newPosSubnetNW = subNet.Position + new Vector(4, -2);
            var newSubnetPos = new Point3D(newPosSubnetNW.X * SimInstancePlacementNetwork.SCALE_PIXEL_TO_M,
                0.0, newPosSubnetNW.Y * SimInstancePlacementNetwork.SCALE_PIXEL_TO_M);

            subNet.Position = newPosSubnetNW;
            Assert.AreEqual(2, instance.InstancePath.Count);
            Assert.AreEqual(newNodePos, instance.InstancePath[0]);
            Assert.AreEqual(newSubnetPos, instance.InstancePath[1]);
        }

        [TestMethod]
        public void PathChangedEdgeWithGeometry()
        {
            LoadProject(instanceProject, "arch", "arch");

            var network = projectData.NetworkManager.NetworkRecord.First(x => x.Name == "GeometryNetwork");
            var node = network.ContainedNodes.Values.First(x => x.Name == "Start");
            var subNet = network.ContainedFlowNetworks.Values.First();
            var edge = network.ContainedEdges.Values.First(x => x.Name == "TopEdge");

            var instance = edge.Content;

            var originalPath = instance.InstancePath.ToList();

            //Start Node changed
            var newPosNodeNW = node.Position + new Vector(3, 3);
            var newNodePos = new Point3D(newPosNodeNW.X * SimInstancePlacementNetwork.SCALE_PIXEL_TO_M,
                0.0, newPosNodeNW.Y * SimInstancePlacementNetwork.SCALE_PIXEL_TO_M);

            node.Position = newPosNodeNW;
            AssertUtil.ContainEqualValues(originalPath, instance.InstancePath);

            //End Network changed
            var newPosSubnetNW = subNet.Position + new Vector(4, -2);
            var newSubnetPos = new Point3D(newPosSubnetNW.X * SimInstancePlacementNetwork.SCALE_PIXEL_TO_M,
                0.0, newPosSubnetNW.Y * SimInstancePlacementNetwork.SCALE_PIXEL_TO_M);

            subNet.Position = newPosSubnetNW;
            AssertUtil.ContainEqualValues(originalPath, instance.InstancePath);
        }

        [TestMethod]
        public void SubnetPathChangedNode()
        {
            LoadProject(instanceProject, "arch", "arch");

            var network = projectData.NetworkManager.NetworkRecord.First(x => x.Name == "TopNetwork");
            var subnetwork = network.ContainedFlowNetworks.Values.First(x => x.Name == "SubNet1");
            var subnetstart = subnetwork.ContainedNodes[subnetwork.NodeStart_ID];
            var node = subnetwork.ContainedNodes.Values.First(x => x.Name == "SubNetNode1");

            var instance = node.Content;

            var initialPosFlat = subnetwork.Position - subnetstart.Position + node.Position;
            var initialPos = new Point3D(initialPosFlat.X * SimInstancePlacementNetwork.SCALE_PIXEL_TO_M,
                0.0, initialPosFlat.Y * SimInstancePlacementNetwork.SCALE_PIXEL_TO_M);
            Assert.AreEqual(1, instance.InstancePath.Count);
            Assert.AreEqual(initialPos, instance.InstancePath[0]);

            var newPosNW = node.Position + new Vector(3, 3);
            var newPosFlat = subnetwork.Position - subnetstart.Position + newPosNW;
            var newPos = new Point3D(newPosFlat.X * SimInstancePlacementNetwork.SCALE_PIXEL_TO_M,
                0.0, newPosFlat.Y * SimInstancePlacementNetwork.SCALE_PIXEL_TO_M);

            node.Position = newPosNW;
            Assert.AreEqual(1, instance.InstancePath.Count);
            Assert.AreEqual(newPos, instance.InstancePath[0]);
        }

        [TestMethod]
        public void SubnetPathChangedEdge()
        {
            LoadProject(instanceProject, "arch", "arch");

            var network = projectData.NetworkManager.NetworkRecord.First(x => x.Name == "TopNetwork");
            var subnetwork = network.ContainedFlowNetworks.Values.First(x => x.Name == "SubNet1");
            var subnetstart = subnetwork.ContainedNodes[subnetwork.NodeStart_ID];
            var node = subnetwork.ContainedNodes.Values.First(x => x.Name == "SubNetNode1");
            var edge = subnetwork.ContainedEdges.Values.First(x => x.Name == "SubNetEdge1");

            var instance = edge.Content;

            var initialEnd = instance.InstancePath[1];

            var initialStartFlat = subnetwork.Position - subnetstart.Position + subnetstart.Position;
            var initialStartPos = new Point3D(initialStartFlat.X * SimInstancePlacementNetwork.SCALE_PIXEL_TO_M,
                0.0, initialStartFlat.Y * SimInstancePlacementNetwork.SCALE_PIXEL_TO_M);
            Assert.AreEqual(2, instance.InstancePath.Count);
            Assert.AreEqual(initialStartPos, instance.InstancePath[0]);

            //End Node changed
            var newPosEndNW = node.Position + new Vector(3, 3);
            var newPosEndFlat = subnetwork.Position - subnetstart.Position + newPosEndNW;
            var newPosEnd = new Point3D(newPosEndFlat.X * SimInstancePlacementNetwork.SCALE_PIXEL_TO_M,
                0.0, newPosEndFlat.Y * SimInstancePlacementNetwork.SCALE_PIXEL_TO_M);

            node.Position = newPosEndNW;
            Assert.AreEqual(2, instance.InstancePath.Count);
            Assert.AreEqual(initialStartPos, instance.InstancePath[0]);
            Assert.AreEqual(newPosEnd, instance.InstancePath[1]);


            //Start Node changed
            var newPosStartNW = subnetstart.Position + new Vector(4, -2);
            var newPosStartFlat = subnetwork.Position;
            var newPosStart = new Point3D(newPosStartFlat.X * SimInstancePlacementNetwork.SCALE_PIXEL_TO_M,
                0.0, newPosStartFlat.Y * SimInstancePlacementNetwork.SCALE_PIXEL_TO_M);

            //Also recalcs end pos since start is first node in network
            newPosEndFlat = subnetwork.Position - newPosStartNW + node.Position;
            newPosEnd = new Point3D(newPosEndFlat.X * SimInstancePlacementNetwork.SCALE_PIXEL_TO_M,
                0.0, newPosEndFlat.Y * SimInstancePlacementNetwork.SCALE_PIXEL_TO_M);

            subnetstart.Position = newPosStartNW;
            Assert.AreEqual(2, instance.InstancePath.Count);
            Assert.AreEqual(newPosStart, instance.InstancePath[0]);
            Assert.AreEqual(newPosEnd, instance.InstancePath[1]);
        }

        [TestMethod]
        public void SubnetPathSubnetMoved()
        {
            LoadProject(instanceProject, "arch", "arch");

            var network = projectData.NetworkManager.NetworkRecord.First(x => x.Name == "TopNetwork");
            var subnetwork = network.ContainedFlowNetworks.Values.First(x => x.Name == "SubNet1");
            var subnetstart = subnetwork.ContainedNodes[subnetwork.NodeStart_ID];
            var node = subnetwork.ContainedNodes.Values.First(x => x.Name == "SubNetNode1");
            var edge = subnetwork.ContainedEdges.Values.First(x => x.Name == "SubNetEdge1");

            var nodeInstance = node.Content;
            var edgeInstance = edge.Content;

            Vector offset = new Vector(-3, 4);
            var initialNodePos = node.Position;
            var initialFirstPos = subnetstart.Position;

            subnetwork.Position += offset;

            Assert.AreEqual(1, nodeInstance.InstancePath.Count);
            var nodePosFlat = subnetwork.Position - subnetstart.Position + node.Position;
            var nodePos = new Point3D(nodePosFlat.X * SimInstancePlacementNetwork.SCALE_PIXEL_TO_M,
                0.0, nodePosFlat.Y * SimInstancePlacementNetwork.SCALE_PIXEL_TO_M);
            Assert.AreEqual(nodePos, nodeInstance.InstancePath[0]);

            var subnetStartFlat = subnetwork.Position;
            var subnetPos = new Point3D(subnetStartFlat.X * SimInstancePlacementNetwork.SCALE_PIXEL_TO_M,
                0.0, subnetStartFlat.Y * SimInstancePlacementNetwork.SCALE_PIXEL_TO_M);
            Assert.AreEqual(2, edgeInstance.InstancePath.Count);
            Assert.AreEqual(subnetPos, edgeInstance.InstancePath[0]);
            Assert.AreEqual(nodePos, edgeInstance.InstancePath[1]);
        }

        #endregion
    }
}
