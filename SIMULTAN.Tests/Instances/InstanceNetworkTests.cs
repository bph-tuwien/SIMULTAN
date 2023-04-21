using Microsoft.VisualStudio.TestTools.UnitTesting;
using SIMULTAN.Data.Components;
using SIMULTAN.Data.FlowNetworks;
using SIMULTAN.Tests.TestUtils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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


        private WeakReference NodeInstanceMemoryLeak_Action(SimComponent nodeComponent, SimFlowNetworkNode node)
        {
            var instance = new SimComponentInstance(node, new Point(0, 0));
            WeakReference instRef = new WeakReference(instance);

            nodeComponent.Instances.Add(instance);

            Assert.AreEqual(instance, node.Content);

            nodeComponent.Instances.Remove(instance);

            return instRef;
        }
        [TestMethod]
        public void NodeInstanceMemoryLeak()
        {
            LoadProject(instanceProject);

            var nodeComponent = projectData.Components.First(x => x.Name == "Node");
            var network = projectData.NetworkManager.NetworkRecord.First(x => x.Name == "Network");
            var node = network.ContainedNodes.Values.First(x => x.Name == "Node 912f497f-2a73-4798-85d6-bdd365da555f: 2");

            var instRef = NodeInstanceMemoryLeak_Action(nodeComponent, node);

            Assert.AreEqual(null, node.Content);

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            Assert.IsFalse(instRef.IsAlive);

        }

        private WeakReference NodePlacementMemoryLeak_Action(SimComponent nodeComponent, SimFlowNetworkNode node)
        {
            var instance = new SimComponentInstance(node, new Point(0, 0));

            nodeComponent.Instances.Add(instance);

            Assert.AreEqual(instance, node.Content);

            var placement = instance.Placements.FirstOrDefault(x => x is SimInstancePlacementNetwork);
            var placementRef = new WeakReference(placement);
            instance.Placements.Remove(placement);

            return placementRef;
        }
        [TestMethod]
        public void NodePlacementMemoryLeak()
        {
            LoadProject(instanceProject);

            var nodeComponent = projectData.Components.First(x => x.Name == "Node");
            var network = projectData.NetworkManager.NetworkRecord.First(x => x.Name == "Network");
            var node = network.ContainedNodes.Values.First(x => x.Name == "Node 912f497f-2a73-4798-85d6-bdd365da555f: 2");

            var placementRef = NodePlacementMemoryLeak_Action(nodeComponent, node);

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

        private WeakReference EdgeInstanceMemoryLeak_Action(SimComponent nodeComponent, SimFlowNetworkEdge edge)
        {
            var instance = new SimComponentInstance(edge, new Point(0, 0));
            WeakReference instRef = new WeakReference(instance);

            nodeComponent.Instances.Add(instance);

            Assert.AreEqual(instance, edge.Content);

            nodeComponent.Instances.Remove(instance);

            return instRef;
        }
        [TestMethod]
        public void EdgeInstanceMemoryLeak()
        {
            LoadProject(instanceProject);

            var nodeComponent = projectData.Components.First(x => x.Name == "Edge");
            var network = projectData.NetworkManager.NetworkRecord.First(x => x.Name == "Network");
            var edge = network.ContainedEdges.Values.First(x => x.Name == "Edge 4");

            var instRef = EdgeInstanceMemoryLeak_Action(nodeComponent, edge);

            Assert.AreEqual(null, edge.Content);

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            Assert.IsFalse(instRef.IsAlive);

        }

        private WeakReference EdgePlacementMemoryLeak_Action(SimComponentInstance instance)
        {
            var placement = instance.Placements.FirstOrDefault(x => x is SimInstancePlacementNetwork);
            var placementRef = new WeakReference(placement);
            instance.Placements.Remove(placement);

            return placementRef;
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

            var placementRef = EdgePlacementMemoryLeak_Action(instance);

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

            Dictionary<string, SimDoubleParameter> cumulativeParameters = new Dictionary<string, SimDoubleParameter>
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
                cumulativeParameters[pKey] = cumulativeComponent.Parameters.FirstOrDefault(x => x is SimDoubleParameter && x.HasReservedTaxonomyEntry(pKey)) as SimDoubleParameter;

            AssertUtil.AssertDoubleEqual(0.0, cumulativeParameters[ReservedParameterKeys.RP_LENGTH_MIN_TOTAL].Value);
            AssertUtil.AssertDoubleEqual(1.0, cumulativeParameters[ReservedParameterKeys.RP_LENGTH_MAX_TOTAL].Value);
            AssertUtil.AssertDoubleEqual(0.0, cumulativeParameters[ReservedParameterKeys.RP_AREA_MIN_TOTAL].Value);
            AssertUtil.AssertDoubleEqual(1.0, cumulativeParameters[ReservedParameterKeys.RP_AREA_MAX_TOTAL].Value);
            AssertUtil.AssertDoubleEqual(0.0, cumulativeParameters[ReservedParameterKeys.RP_VOLUME_MIN_TOTAL].Value);
            AssertUtil.AssertDoubleEqual(1.0, cumulativeParameters[ReservedParameterKeys.RP_VOLUME_MAX_TOTAL].Value);
            AssertUtil.AssertDoubleEqual(1.0, cumulativeParameters[ReservedParameterKeys.RP_COUNT].Value);

            instance.InstanceSize = new SimInstanceSize(new Vector3D(1.0, 2.0, 3.0), new Vector3D(4.0, 5.0, 6.0));

            AssertUtil.AssertDoubleEqual(3.0, cumulativeParameters[ReservedParameterKeys.RP_LENGTH_MIN_TOTAL].Value);
            AssertUtil.AssertDoubleEqual(6.0, cumulativeParameters[ReservedParameterKeys.RP_LENGTH_MAX_TOTAL].Value);
            AssertUtil.AssertDoubleEqual(2.0, cumulativeParameters[ReservedParameterKeys.RP_AREA_MIN_TOTAL].Value);
            AssertUtil.AssertDoubleEqual(20.0, cumulativeParameters[ReservedParameterKeys.RP_AREA_MAX_TOTAL].Value);
            AssertUtil.AssertDoubleEqual(6.0, cumulativeParameters[ReservedParameterKeys.RP_VOLUME_MIN_TOTAL].Value);
            AssertUtil.AssertDoubleEqual(120.0, cumulativeParameters[ReservedParameterKeys.RP_VOLUME_MAX_TOTAL].Value);
            AssertUtil.AssertDoubleEqual(1.0, cumulativeParameters[ReservedParameterKeys.RP_COUNT].Value);
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

            Dictionary<string, SimDoubleParameter> cumulativeParameters = new Dictionary<string, SimDoubleParameter>
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
                cumulativeParameters[pKey] = cumulativeComponent.Parameters.FirstOrDefault(x => x is SimDoubleParameter && x.HasReservedTaxonomyEntry(pKey)) as SimDoubleParameter;

            AssertUtil.AssertDoubleEqual(3.0, cumulativeParameters[ReservedParameterKeys.RP_LENGTH_MIN_TOTAL].Value);
            AssertUtil.AssertDoubleEqual(7.0, cumulativeParameters[ReservedParameterKeys.RP_LENGTH_MAX_TOTAL].Value);
            AssertUtil.AssertDoubleEqual(2.0, cumulativeParameters[ReservedParameterKeys.RP_AREA_MIN_TOTAL].Value);
            AssertUtil.AssertDoubleEqual(21.0, cumulativeParameters[ReservedParameterKeys.RP_AREA_MAX_TOTAL].Value);
            AssertUtil.AssertDoubleEqual(6.0, cumulativeParameters[ReservedParameterKeys.RP_VOLUME_MIN_TOTAL].Value);
            AssertUtil.AssertDoubleEqual(121.0, cumulativeParameters[ReservedParameterKeys.RP_VOLUME_MAX_TOTAL].Value);
            AssertUtil.AssertDoubleEqual(2.0, cumulativeParameters[ReservedParameterKeys.RP_COUNT].Value);

            instance.InstanceSize = new SimInstanceSize(new Vector3D(1.0, 2.0, 3.0), new Vector3D(4.0, 5.0, 6.0));

            AssertUtil.AssertDoubleEqual(6.0, cumulativeParameters[ReservedParameterKeys.RP_LENGTH_MIN_TOTAL].Value);
            AssertUtil.AssertDoubleEqual(12.0, cumulativeParameters[ReservedParameterKeys.RP_LENGTH_MAX_TOTAL].Value);
            AssertUtil.AssertDoubleEqual(4.0, cumulativeParameters[ReservedParameterKeys.RP_AREA_MIN_TOTAL].Value);
            AssertUtil.AssertDoubleEqual(40.0, cumulativeParameters[ReservedParameterKeys.RP_AREA_MAX_TOTAL].Value);
            AssertUtil.AssertDoubleEqual(12.0, cumulativeParameters[ReservedParameterKeys.RP_VOLUME_MIN_TOTAL].Value);
            AssertUtil.AssertDoubleEqual(240.0, cumulativeParameters[ReservedParameterKeys.RP_VOLUME_MAX_TOTAL].Value);
            AssertUtil.AssertDoubleEqual(2.0, cumulativeParameters[ReservedParameterKeys.RP_COUNT].Value);

            nodeComponent.Instances.Remove(instance);

            AssertUtil.AssertDoubleEqual(3.0, cumulativeParameters[ReservedParameterKeys.RP_LENGTH_MIN_TOTAL].Value);
            AssertUtil.AssertDoubleEqual(6.0, cumulativeParameters[ReservedParameterKeys.RP_LENGTH_MAX_TOTAL].Value);
            AssertUtil.AssertDoubleEqual(2.0, cumulativeParameters[ReservedParameterKeys.RP_AREA_MIN_TOTAL].Value);
            AssertUtil.AssertDoubleEqual(20.0, cumulativeParameters[ReservedParameterKeys.RP_AREA_MAX_TOTAL].Value);
            AssertUtil.AssertDoubleEqual(6.0, cumulativeParameters[ReservedParameterKeys.RP_VOLUME_MIN_TOTAL].Value);
            AssertUtil.AssertDoubleEqual(120.0, cumulativeParameters[ReservedParameterKeys.RP_VOLUME_MAX_TOTAL].Value);
            AssertUtil.AssertDoubleEqual(1.0, cumulativeParameters[ReservedParameterKeys.RP_COUNT].Value);

        }

        #endregion
    }
}
