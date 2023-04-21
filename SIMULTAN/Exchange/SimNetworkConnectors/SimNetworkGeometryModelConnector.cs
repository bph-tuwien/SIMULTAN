using MathNet.Numerics;
using SIMULTAN.Data;
using SIMULTAN.Data.Components;
using SIMULTAN.Data.Geometry;
using SIMULTAN.Data.SimNetworks;
using SIMULTAN.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using static SIMULTAN.Data.SimNetworks.SimNetworkPort;

namespace SIMULTAN.Exchange.SimNetworkConnectors
{

    /// <summary>
    /// Shows the type of the Error which occurs during the lay-outing algorithm
    /// </summary>
    public enum ValidationError
    {
        /// <summary>
        /// Whenever no rotation could solve the connection of static ports
        /// </summary>
        RotationError,
        /// <summary>
        /// Whenever distances does not match between connections
        /// </summary>
        DistanceError,
    }



    /// <see cref="ComponentGeometryExchange.ConvertSimNetwork(SimNetwork, System.IO.FileInfo)"/> method.
    public class SimNetworkGeometryModelConnector
    {
        static double ReduceRatio = 20;
        #region Properties
        /// <summary>
        /// The network monitored by this connector
        /// </summary>
        internal SimNetwork Network { get; }
        /// <summary>
        /// The geometry model managed by this connector
        /// </summary>
        internal GeometryModel GeometryModel { get; }

        /// <summary>
        /// The exchange object which created this connector
        /// </summary>
        internal ComponentGeometryExchange Exchange { get; }

        /// <summary>
        /// Contains dummy geometries. 
        /// Dummy geometries are used e.g.: <see cref="StartMoveRotatePartialOperation(BaseGeometry)"/> to represent temporal geometries during a move/rotate operation
        /// </summary>
        internal List<BaseGeometry> DummyGeometries { get; set; } = new List<BaseGeometry>();

        /// <summary>
        /// Random number generator for creating colors
        /// </summary>
        private Random rnd = new Random();

        private Dictionary<ulong, BaseSimnetworkGeometryConnector> connectors = new Dictionary<ulong, BaseSimnetworkGeometryConnector>();
        private Dictionary<SimNetworkBlock, List<SimNetworkBlock>> staticGroups = new Dictionary<SimNetworkBlock, List<SimNetworkBlock>>();


        /// <summary>
        /// Initializes a new SimNetworkGeometryModelConnector
        /// </summary>
        /// <param name="model">The geometry model</param>
        /// <param name="network">The network it connects to the geometry model</param>
        /// <param name="exchange">The component geometry exchange</param>
        public SimNetworkGeometryModelConnector(GeometryModel model, SimNetwork network, ComponentGeometryExchange exchange)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));
            if (network == null)
                throw new ArgumentNullException(nameof(network));
            if (exchange == null)
                throw new ArgumentNullException(nameof(exchange));
            if (network.IndexOfGeometricRepFile != model.File.Key)
                throw new ArgumentException("Network is not represented by this geometry model");

            this.Network = network;
            this.GeometryModel = model;
            this.Exchange = exchange;

            this.GeometryModel.Geometry.TopologyChanged += this.Geometry_TopologyChanged;
            this.GeometryModel.Replaced += this.GeometryModel_Replaced;



            //Add child connectors, make sure that all network elements are properly represented in the geometry model
            UpdateNetwork(network);
            AttachNetworkEvents(network);

        }



        private void Geometry_TopologyChanged(object sender, IEnumerable<BaseGeometry> geometries)
        {
            foreach (var geom in geometries)
            {
                if (connectors.TryGetValue(geom.Id, out var con))
                    con.OnTopologyChanged();
            }
        }

        private void GeometryModel_Replaced(object sender, GeometryModelReplacedEventArgs e)
        {
            if (e.OldGeometry != null)
            {
                e.OldGeometry.TopologyChanged -= this.Geometry_TopologyChanged;
            }

            //Reconnect everything
            UpdateNetwork(this.Network);


            if (e.NewGeometry != null)
            {
                e.OldGeometry.TopologyChanged += this.Geometry_TopologyChanged;
            }
        }


        /// <summary>
        ///  Updates the given network´s geometry
        /// </summary>
        /// <param name="network">The network we base the geometry on</param>
        public void UpdateNetwork(SimNetwork network)
        {
            GeometryModel.Geometry.StartBatchOperation();

            if (GeometryModel.Geometry.Layers.Count == 0)
            {
                GeometryModel.Geometry.Layers.Add(new Layer(GeometryModel.Geometry, "0"));
            }
            Dictionary<ulong, BaseSimnetworkGeometryConnector> existingConnectors = new Dictionary<ulong, BaseSimnetworkGeometryConnector>(this.connectors);
            connectors.Clear();

            UpdateNetworkElements(network, existingConnectors);
            UpdateStaticConnectors(network, existingConnectors);
            UpdateConnectors(network, existingConnectors);

            GeometryModel.Geometry.EndBatchOperation();

            CleanUnusedGeometry();

            foreach (var con in existingConnectors.Values)
                con.Dispose();
        }

        private void UpdateNetworkElements(SimNetwork network, Dictionary<ulong, BaseSimnetworkGeometryConnector> existingConnectors)
        {
            //Make sure that all nodes of the flattened network exist in the geometry model
            staticGroups.Clear();
            GetStaticGroups();
            UpdateStaticBlocks(network, staticGroups, existingConnectors);
            UpdateBlocks(network, existingConnectors);
            UpdateSubnetworks(network, existingConnectors);
            UpdatePorts(network, existingConnectors);
        }




        private void UpdatePorts(SimNetwork network, Dictionary<ulong, BaseSimnetworkGeometryConnector> existingConnectors)
        {
            foreach (var parentElement in network.ContainedElements.Where(t => t is BaseSimNetworkElement))
                foreach (var port in parentElement.Ports.Where(p => p.Connectors.Count == 0 && ((p.ParentNetworkElement is SimNetworkBlock bl && !bl.IsStatic || p.ParentNetworkElement is SimNetwork))))
                    AddPort(port, existingConnectors);
        }

        /// <summary>
        /// Updates the static connectors, needed for undo redo item.
        /// </summary>
        public void UpdateStaticConnectors(List<Vertex> geoms)
        {
            for (int i = 0; i < geoms.Count; i++)
            {
                if (this.connectors.TryGetValue(geoms[i].Id, out var connector) && connector is SimNetworkBlockConnector blockCon)
                {
                    if (blockCon.Block.IsStatic)
                    {
                        foreach (var port in blockCon.Block.Ports)
                        {
                            AddStaticPort(port, null, false);
                        }
                    }
                }
            }
            this.UpdateStaticConnectors(this.Network, null);
        }

        private void UpdateStaticConnectors(SimNetwork network, Dictionary<ulong, BaseSimnetworkGeometryConnector> existingConnectors)
        {
            GeometryModel.Geometry.StartBatchOperation();
            foreach (var connector in network.ContainedConnectors
                .Where(t => (
                t.Target.ParentNetworkElement is SimNetworkBlock bl && bl.IsStatic)
                || t.Source.ParentNetworkElement is SimNetworkBlock block && block.IsStatic))
            {
                var connectorChain = FindConnectorChain(connector, new List<SimNetworkConnector>());
                var startNode = connectorChain.FirstOrDefault(c => c.Source.ParentNetworkElement is SimNetworkBlock).Source; //Start node is a port where the chain starts, and its parent is a block
                var endNode = connectorChain.FirstOrDefault(c => c.Target.ParentNetworkElement is SimNetworkBlock).Target; //End node is where the chain ends and the parent is a block

                if (startNode.ParentNetworkElement is SimNetworkBlock bl && bl.IsStatic && endNode.ParentNetworkElement is SimNetworkBlock bl2 && bl2.IsStatic)
                {
                    AddStaticConnector(connectorChain, existingConnectors);
                }
            }
            foreach (var subNet in network.ContainedElements.Where(r => r is SimNetwork))
                UpdateStaticConnectors(subNet as SimNetwork, existingConnectors);

            GeometryModel.Geometry.EndBatchOperation();
        }



        private void UpdateBlocks(SimNetwork network, Dictionary<ulong, BaseSimnetworkGeometryConnector> existingConnectors)
        {

            foreach (var node in network.ContainedElements.Where(t => t is BaseSimNetworkElement && t is SimNetworkBlock block && !block.IsStatic))
                AddDynamicBlock(node as SimNetworkBlock, existingConnectors);
        }



        private void UpdateSubnetworks(SimNetwork network, Dictionary<ulong, BaseSimnetworkGeometryConnector> existingConnectors)
        {
            foreach (var node in network.ContainedElements.Where(t => t is SimNetwork))
            {
                AddSubnetwork(node as SimNetwork, existingConnectors);
                AdjustImportedElements(node as SimNetwork);
            }
        }



        private void UpdateStaticBlocks(SimNetwork network, Dictionary<SimNetworkBlock, List<SimNetworkBlock>> staticGroups, Dictionary<ulong, BaseSimnetworkGeometryConnector> existingConnectors)
        {
            foreach (var staticGroup in staticGroups)
            {
                var handledBlocks = new List<(SimNetworkBlock block, Transform3DGroup transformation)>();
                AddStaticBlockFromChain(handledBlocks, staticGroup.Value, staticGroup.Key, existingConnectors);
            }
        }





        private void AdjustImportedElements(SimNetwork subnetwork)
        {
            var boundingBox = GetSubnetworkBoundingBox(subnetwork);
            foreach (var element in subnetwork.ParentNetwork.ContainedElements)
            {
                if (element is BaseSimNetworkElement baseSimNetworkElement && !(baseSimNetworkElement is SimNetworkBlock bl && bl.IsStatic))
                {
                    var transVector = new Vector3D(0, 0, 0);
                    if (connectors.TryGetValue(element.RepresentationReference.GeometryId, out var connector))
                    {
                        var geom = connector.Geometry as Vertex;

                        if (element.Position.X < subnetwork.Position.X)
                        {
                            if (geom.Position.X > boundingBox.Min.X)
                            {
                                // Item should be transformed by
                                var transofrmBy = (boundingBox.Min.X - geom.Position.X);
                                transVector.X = transVector.X - transofrmBy;
                            }
                        }
                        if (element.Position.X > subnetwork.Position.X)
                        {
                            if (geom.Position.X < boundingBox.Max.X)
                            {
                                // Item should be transformed by
                                var transofrmBy = (boundingBox.Max.X - geom.Position.X);
                                transVector.X = transVector.X - transofrmBy;
                            }
                        }
                        if (element.Position.Y < subnetwork.Position.Y)
                        {
                            if (geom.Position.Y > boundingBox.Min.Y)
                            {
                                // Item should be transformed by
                                var transofrmBy = (boundingBox.Min.Y - geom.Position.Y);
                                transVector.Y = transVector.Y - transofrmBy;
                            }
                        }
                        if (element.Position.Y > subnetwork.Position.Y)
                        {
                            if (geom.Position.Y < boundingBox.Max.Y)
                            {
                                // Item should be transformed by
                                var transofrmBy = (boundingBox.Max.Y - geom.Position.Y);
                                transVector.Y = transVector.Y - transofrmBy;
                            }
                        }

                        geom.Position = geom.Position - transVector;
                        connector.ChangeBaseGeometry(geom);

                    }

                    foreach (var port in baseSimNetworkElement.Ports)
                    {
                        if (connectors.TryGetValue(port.RepresentationReference.GeometryId, out var portConnector))
                        {
                            ((Vertex)portConnector.Geometry).Position = ((Vertex)portConnector.Geometry).Position - transVector;
                            portConnector.ChangeBaseGeometry(portConnector.Geometry);
                        }
                    }
                }
            }
        }


        private void UpdateConnectors(SimNetwork network, Dictionary<ulong, BaseSimnetworkGeometryConnector> existingConnectors)
        {
            //Connector between static blocks
            foreach (var blockConnector in network.ContainedConnectors
                .Where(c =>
                ((c.Source.ParentNetworkElement is SimNetworkBlock bl && !bl.IsStatic || c.Source.ParentNetworkElement is SimNetwork && c.Source.Connectors.Count == 1))
                && ((c.Target.ParentNetworkElement is SimNetworkBlock bl2 && !bl2.IsStatic || c.Target.ParentNetworkElement is SimNetwork && c.Target.Connectors.Count == 1)))) // normal connections
                AddConnector(blockConnector, existingConnectors);

            //Connectors between subnetworks and blocks
            foreach (var blockConnector in network.ContainedConnectors
                    .Where(c => (c.Source.ParentNetworkElement is SimNetworkBlock bl && !bl.IsStatic && c.Target.ParentNetworkElement is SimNetwork && c.Target.Connectors.Count == 2) ||
                                 c.Target.ParentNetworkElement is SimNetworkBlock bl2 && !bl2.IsStatic && c.Source.ParentNetworkElement is SimNetwork && c.Source.Connectors.Count == 2))
            {
                var blockToSubnetworkConnector = blockConnector;
                var chain = FindConnectorChain(blockToSubnetworkConnector, new List<SimNetworkConnector>());
                AddBlockToSubnetworkConnector(chain, existingConnectors);
            }
            //Connectors between static and dynamic blocks
            foreach (var blockConnector in network.ContainedConnectors.Where(c =>
            (c.Source.ParentNetworkElement is SimNetworkBlock bl && bl.IsStatic) && ((c.Target.ParentNetworkElement is SimNetworkBlock bl2 && !bl2.IsStatic)) ||
            (c.Source.ParentNetworkElement is SimNetworkBlock bl3 && !bl3.IsStatic) && ((c.Target.ParentNetworkElement is SimNetworkBlock bl4 && bl4.IsStatic)))) // Connection between static and dynamic
            {
                AddStaticDynamicConnector(blockConnector, existingConnectors);
            }

            foreach (var subNet in network.ContainedElements.Where(r => r is SimNetwork))
                UpdateConnectors(subNet as SimNetwork, existingConnectors);
        }


        private List<SimNetworkConnector> FindConnectorChain(SimNetworkConnector connector, List<SimNetworkConnector> currentChain)
        {
            currentChain.Add(connector);

            if (!(connector.Target.ParentNetworkElement is SimNetwork) && !(connector.Source.ParentNetworkElement is SimNetwork))
            {
                return currentChain;
            }

            if (connector.Source.ParentNetworkElement is SimNetwork && connector.Source.Connectors.Count == 2)
            {
                var notCurrentConnector = connector.Source.Connectors
                    .FirstOrDefault(c => !currentChain.Contains(c));
                if (notCurrentConnector != null)
                {
                    currentChain.Union(FindConnectorChain(notCurrentConnector, currentChain));
                }
            }
            if (connector.Target.ParentNetworkElement is SimNetwork nw && connector.Target.Connectors.Count == 2)
            {
                var notCurrentConnector = connector.Target.Connectors
                     .FirstOrDefault(c => !currentChain.Contains(c));
                if (notCurrentConnector != null)
                {
                    currentChain.Union(FindConnectorChain(notCurrentConnector, currentChain));
                }
            }
            return currentChain;
        }


        private List<List<SimNetworkBlock>> FindStaticGroups(SimNetwork network, List<List<SimNetworkBlock>> currentGroups)
        {

            foreach (SimNetworkBlock block in network.ContainedElements.Where(t => t is SimNetworkBlock bl && bl.IsStatic))
            {
                if (!currentGroups.Any(t => t.Contains(block)))
                {
                    var currentGroup = new List<SimNetworkBlock>() { block };
                    currentGroups.Add(GetStaticConnectedBlocks(currentGroup, block));
                }
            }
            foreach (var item in network.ContainedElements.Where(c => c is SimNetwork))
            {
                currentGroups.Union(FindStaticGroups(item as SimNetwork, currentGroups));
            }
            return currentGroups;
        }

        private List<SimNetworkBlock> GetStaticConnectedBlocks(List<SimNetworkBlock> currentGroup, SimNetworkBlock block)
        {
            foreach (SimNetworkConnector connector in block.Ports.SelectMany(t => t.Connectors))
            {
                if (connector.Target.ParentNetworkElement != block)
                {
                    if (connector.Target.ParentNetworkElement is SimNetworkBlock bl && bl.IsStatic && !currentGroup.Any(t => t.Id == bl.Id))
                    {
                        currentGroup.Add(bl);
                        currentGroup.Union(GetStaticConnectedBlocks(currentGroup, bl));
                    }
                    if (connector.Target.ParentNetworkElement is SimNetwork nw && connector.Target.Connectors.Count == 2)
                    {
                        currentGroup.Union(GetStaticConnectionFromConnectedSubnetwork(connector.Source, connector.Target, currentGroup));
                    }

                }
                else if (connector.Source.ParentNetworkElement != block)
                {
                    if (connector.Source.ParentNetworkElement is SimNetworkBlock bl && bl.IsStatic && !currentGroup.Any(t => t.Id == bl.Id))
                    {
                        currentGroup.Add(bl);
                        currentGroup.Union(GetStaticConnectedBlocks(currentGroup, bl));
                    }
                    if (connector.Source.ParentNetworkElement is SimNetwork nw && connector.Source.Connectors.Count == 2)
                    {
                        currentGroup.Union(GetStaticConnectionFromConnectedSubnetwork(connector.Target, connector.Source, currentGroup));
                    }
                }
            }
            return currentGroup;
        }

        private List<SimNetworkBlock> GetStaticConnectionFromConnectedSubnetwork(SimNetworkPort sourcePort, SimNetworkPort targetPort, List<SimNetworkBlock> currentGroup)
        {
            var newItems = new List<SimNetworkBlock>();

            if (targetPort.ParentNetworkElement is SimNetwork subnetwork)
            {
                foreach (var item in targetPort.Connectors)
                {
                    if (item.Target.ParentNetworkElement is SimNetworkBlock block1 && block1.IsStatic && !currentGroup.Contains(block1) && !newItems.Contains(block1))
                    {
                        currentGroup.Add(block1);
                        currentGroup.Union(GetStaticConnectedBlocks(currentGroup, block1));
                    }
                    if (item.Source.ParentNetworkElement is SimNetworkBlock block2 && block2.IsStatic && !currentGroup.Contains(block2) && !newItems.Contains(block2))
                    {
                        currentGroup.Add(block2);
                        currentGroup.Union(GetStaticConnectedBlocks(currentGroup, block2));
                    }

                    if (item.Source.ParentNetworkElement is SimNetwork sbnw1 && item.Source != targetPort && item.Source != sourcePort)
                    {
                        currentGroup.Union(GetStaticConnectionFromConnectedSubnetwork(item.Target, item.Source, currentGroup));
                    }
                    if (item.Target.ParentNetworkElement is SimNetwork sbnw12 && item.Target != targetPort && item.Source != targetPort)
                    {
                        currentGroup.Union(GetStaticConnectionFromConnectedSubnetwork(item.Source, item.Target, currentGroup));
                    }
                }
            }
            return currentGroup;
        }


        private void AddSubnetwork(SimNetwork subnetwork, Dictionary<ulong, BaseSimnetworkGeometryConnector> existingConnectors)
        {

            if (subnetwork.ContainedElements.Count == 0) //If there is not contained element, Add the SubnEtwork is a Vertex
            {
                AddDynamicBlock(subnetwork, existingConnectors);
            }
            else
            {
                AddNestedElements(subnetwork, existingConnectors);
            }

        }


        private void AddNestedElements(SimNetwork network, Dictionary<ulong, BaseSimnetworkGeometryConnector> existingConnectors)
        {
            UpdateNetworkElements(network, existingConnectors);
        }



        /// <summary>
        /// Returns a port which is connected to the input port only if the port is a port of a block. 
        /// Recursively searches for the block if there are subnetworks involved
        /// </summary>
        /// <param name="port">The port we want to check whether a port is connected which parent is  a SimNetworkBlock</param>
        /// <param name="visitedPorts">The already visited ports</param>
        /// <returns>Returns null if the port is not connected any ports with a SimNetworkBlock parent</returns>
        private SimNetworkPort GetConnectedToBlockPort(SimNetworkPort port, List<SimNetworkPort> visitedPorts)
        {
            foreach (var item in port.Connectors)
            {
                if (item.Target != port && !visitedPorts.Contains(item.Target))
                {
                    if (item.Target.ParentNetworkElement is SimNetworkBlock)
                    {
                        return item.Target;
                    }
                    if (item.Target.ParentNetworkElement is SimNetwork)
                    {
                        visitedPorts.Add(port);
                        return GetConnectedToBlockPort(item.Target, visitedPorts);
                    }

                }
                if (item.Source != port && !visitedPorts.Contains(item.Source))
                {
                    if (item.Source.ParentNetworkElement is SimNetworkBlock)
                    {
                        return item.Source;
                    }
                    if (item.Source.ParentNetworkElement is SimNetwork)
                    {
                        visitedPorts.Add(port);
                        return GetConnectedToBlockPort(item.Source, visitedPorts);
                    }
                }
            }
            return null;
        }



        private void AddStaticBlockFromChain(List<(SimNetworkBlock block, Transform3DGroup transformation)> existingBlocks, List<SimNetworkBlock> staticGroup, SimNetworkBlock newBlockToAdd, Dictionary<ulong, BaseSimnetworkGeometryConnector> existingConnectors)
        {

            if (connectors.TryGetValue(newBlockToAdd.RepresentationReference.GeometryId, out var existingConnectr))
            {
                return;
            }

            var transformationGroup = new Transform3DGroup();
            List<SimNetworkPort> connectedPortsWithBlockParent = new List<SimNetworkPort>();

            var portsToConnectWith = new List<(SimNetworkPort otherPort, SimNetworkPort selfPort)>();

            foreach (var port in newBlockToAdd.Ports)
            {
                var connectedPortWithBlockParent = GetConnectedToBlockPort(port, new List<SimNetworkPort>());
                if (connectedPortWithBlockParent != null && staticGroup.Contains(connectedPortWithBlockParent.ParentNetworkElement))
                {
                    connectedPortsWithBlockParent.Add(connectedPortWithBlockParent);
                    if (existingBlocks.Any(t => t.block == connectedPortWithBlockParent.ParentNetworkElement))
                    {
                        portsToConnectWith.Add((connectedPortWithBlockParent, port));
                    }
                }
            }


            if (!existingBlocks.Select(t => t.block).Contains(newBlockToAdd) && newBlockToAdd.IsStatic && newBlockToAdd.RepresentationReference == GeometricReference.Empty)
            {
                var nonCompliantConnections = new List<Tuple<SimNetworkConnector, ValidationError>>();
                if (portsToConnectWith.Count() > 0)
                {
                    var relPortPositionsToComplyWith = new List<(SimNetworkPort otherPort, Point3D position, SimNetworkPort selfPort)>();
                    foreach (var port in portsToConnectWith)
                    {
                        if (existingBlocks.TryFirstOrDefault(e => e.block == port.otherPort.ParentNetworkElement as SimNetworkBlock, out var existingBlock))
                        {
                            var position = existingBlock.transformation.Transform(GetStaticRelativePortPosition(port.otherPort));
                            relPortPositionsToComplyWith.Add((port.otherPort, position, port.selfPort));
                        }
                    }

                    var firstPort = relPortPositionsToComplyWith.ElementAt(0);
                    SimNetworkPort connectedToFirst = firstPort.selfPort;
                    var connectedToFirstRelPosition = GetStaticRelativePortPosition(firstPort.selfPort);
                    //1. Check if a transformations exists to connect the ports with the new block

                    for (int i = 0; i < relPortPositionsToComplyWith.Count; i++)
                    {
                        if (relPortPositionsToComplyWith.ElementAt(i).otherPort != firstPort.otherPort)
                        {
                            SimNetworkPort connectedPortInNewBlock = relPortPositionsToComplyWith.ElementAt(i).selfPort;

                            if (connectedPortInNewBlock != null && newBlockToAdd.Ports.Contains(connectedPortInNewBlock))
                            {
                                var relPositionOfPort = GetStaticRelativePortPosition(connectedPortInNewBlock);
                                var complyDistance = Distance.Euclidean(
                                    new double[] { relPortPositionsToComplyWith.ElementAt(i).position.X, relPortPositionsToComplyWith.ElementAt(i).position.Y, relPortPositionsToComplyWith.ElementAt(i).position.Z },
                                    new double[] { firstPort.position.X, firstPort.position.Y, firstPort.position.Z });
                                var initialPositionOfConnectedPort = GetStaticPortGlobalPortPosition(connectedPortInNewBlock);
                                var newBlockDistaces = Distance.Euclidean(
                                      new double[] { connectedToFirstRelPosition.X, connectedToFirstRelPosition.Y, connectedToFirstRelPosition.Z },
                                      new double[] { relPositionOfPort.X, relPositionOfPort.Y, relPositionOfPort.Z });

                                if (complyDistance != newBlockDistaces)
                                {
                                    nonCompliantConnections.Add(
                                        new Tuple<SimNetworkConnector, ValidationError>(
                                            newBlockToAdd.Ports
                                            .SelectMany(t => t.Connectors)
                                            .FirstOrDefault(t => t.Target == connectedPortInNewBlock || t.Source == connectedPortInNewBlock),
                                        ValidationError.DistanceError));
                                }
                            }
                        }
                    }

                    var parentBLockOfFirstPort = existingBlocks.FirstOrDefault(t => t.block == firstPort.otherPort.ParentNetworkElement);
                    var firsPortGLobalPosition = GetStaticPortGlobalPortPosition(firstPort.otherPort);
                    var connectedGlobalPosition = GetStaticPortGlobalPortPosition(connectedToFirst);

                    transformationGroup.Children.Add(new TranslateTransform3D(
                        ((Vector3D)firsPortGLobalPosition) - ((Vector3D)connectedGlobalPosition)));



                    //Check if rotation exists to connect the ports (the first one is connected by a simple translation transformation -->
                    //Hence we apply that transformation to the block and all of its´ ports)
                    if (portsToConnectWith.Count() > 1)
                    {
                        if (connectors.TryGetValue(firstPort.otherPort.RepresentationReference.GeometryId, out var rotCenterConnector))
                        {
                            var rotationCenter = ((Vertex)rotCenterConnector.Geometry).Position;

                            List<Quaternion> quaternions = new List<Quaternion>();
                            for (int i = 0; i < relPortPositionsToComplyWith.Count; i++)
                            {
                                if (i == 0)
                                    continue;

                                var elementAt = relPortPositionsToComplyWith.ElementAt(i);
                                if (connectors.TryGetValue(elementAt.otherPort.RepresentationReference.GeometryId, out var connector))
                                {
                                    var toComplyPortVertex = connector.Geometry as Vertex;
                                    var equivalentInConneciton = transformationGroup.Transform(GetStaticPortGlobalPortPosition(elementAt.selfPort));
                                    var targetVector = new Vector3D(toComplyPortVertex.Position.X - rotationCenter.X, toComplyPortVertex.Position.Y - rotationCenter.Y, toComplyPortVertex.Position.Z - rotationCenter.Z);
                                    var vectorToRotate = new Vector3D(equivalentInConneciton.X - rotationCenter.X, equivalentInConneciton.Y - rotationCenter.Y, equivalentInConneciton.Z - rotationCenter.Z);

                                    targetVector.Normalize();
                                    vectorToRotate.Normalize();

                                    Quaternion q = Quaternion.Identity;
                                    Vector3D a = Vector3D.CrossProduct(vectorToRotate, targetVector);
                                    q.X = a.X;
                                    q.Y = a.Y;
                                    q.Z = a.Z;
                                    q.W = Math.Sqrt((Math.Pow(targetVector.Length, 2)) * (Math.Pow(vectorToRotate.Length, 2))) + Vector3D.DotProduct(targetVector, vectorToRotate);
                                    quaternions.Add(q);

                                    if (q != quaternions[i - 1])
                                    {
                                        nonCompliantConnections.Add(new Tuple<SimNetworkConnector, ValidationError>(newBlockToAdd.Ports.SelectMany(t => t.Connectors).FirstOrDefault(t => t.Target == elementAt.otherPort || t.Source == elementAt.otherPort), ValidationError.RotationError));
                                    }
                                }
                            }

                            if (quaternions.All(t => t == quaternions[0]))
                            {
                                var quat = quaternions[0];
                                quat.Normalize();
                                var quatRotaiton = new QuaternionRotation3D(quat);
                                transformationGroup.Children.Add(new RotateTransform3D(quatRotaiton, rotationCenter));
                            }
                        }
                    }
                }
            }

            //Add the block itself
            AddStaticBlock(newBlockToAdd, existingConnectors, transformationGroup);
            foreach (var port in newBlockToAdd.Ports)
            {
                if (port.Connectors.Count == 0)
                {
                    AddStaticPort(port, existingConnectors, true);
                }

            }
            existingBlocks.Add((newBlockToAdd, transformationGroup));



            foreach (var item in connectedPortsWithBlockParent)
            {
                if (staticGroup.Contains(item.ParentNetworkElement) && !existingBlocks.Any(t => t.block == item.ParentNetworkElement as SimNetworkBlock))
                {
                    AddStaticBlockFromChain(existingBlocks, staticGroup, item.ParentNetworkElement as SimNetworkBlock, existingConnectors);
                }
            }
        }




        //Returns the position of a static port adjusted with the position of the bock, and if the block is already imported than gets
        //the position of the geometry
        private Point3D GetStaticPortGlobalPortPosition(SimNetworkPort port)
        {
            Point3D position = new Point3D();
            var relPosition = GeStaticPortRelPosition(port);

            var blockPosition = new Point3D(0, 0, 0);
            var quaternion = Quaternion.Identity;

            if (this.connectors.TryGetValue(port.ParentNetworkElement.RepresentationReference.GeometryId, out var parentConnector))
            {
                var blockConnector = ((Vertex)parentConnector.Geometry);
                quaternion = blockConnector.ProxyGeometries.FirstOrDefault().Rotation;
                blockPosition = blockConnector.Position;
            }
            else
            {
                blockPosition = new Point3D(port.ParentNetworkElement.Position.X / ReduceRatio, port.ParentNetworkElement.Position.Y / ReduceRatio, 0);
            }
            if (quaternion != Quaternion.Identity)
            {


                var rot = new RotateTransform3D(new QuaternionRotation3D(quaternion));
                relPosition = rot.Transform(relPosition);

                position = new Point3D(
                    blockPosition.X + (relPosition.X),
                    blockPosition.Y + (relPosition.Y),
                    blockPosition.Z + (relPosition.Z));
            }
            else
            {
                position = new Point3D(
                   blockPosition.X + relPosition.X,
                   blockPosition.Y + relPosition.Y,
                   blockPosition.Z + relPosition.Z);
            }
            return new Point3D(Math.Round(position.X, 5), Math.Round(position.Y, 5), Math.Round(position.Z, 5));
        }




        private void AddStaticBlock(SimNetworkBlock block, Dictionary<ulong, BaseSimnetworkGeometryConnector> existingConnectors, Transform3DGroup transformation)
        {
            Vertex geometry = null;

            //Check if geometry for edge exists
            if (block.RepresentationReference != GeometricReference.Empty)
                geometry = GeometryModel.Geometry.GeometryFromId(block.RepresentationReference.GeometryId) as Vertex;

            if (geometry == null)
            {
                Point3D position = transformation.Transform(new Point3D(block.Position.X / ReduceRatio, block.Position.Y / ReduceRatio, 0));
                geometry = new Vertex(this.GeometryModel.Geometry.Layers.First(), block.Name,
                    new Point3D(position.X, position.Y, position.Z))
                {
                    Color = block.Color,
                };
            }

            if (geometry != null && existingConnectors != null && existingConnectors.TryGetValue(geometry.Id, out var con))
            {
                if (transformation.Children.Count > 0)
                {
                    Point3D position = transformation.Transform(new Point3D(block.Position.X, block.Position.Y, 0));
                    geometry.Position = position;
                }

                con.ChangeBaseGeometry(geometry);
                connectors.Add(geometry.Id, con);
                existingConnectors.Remove(geometry.Id);
            }
            else
            {
                var rotateTransform = transformation.Children.FirstOrDefault(t => t is RotateTransform3D) as RotateTransform3D;
                Quaternion q = Quaternion.Identity;
                if (rotateTransform != null && rotateTransform.Rotation is QuaternionRotation3D quaternion)
                {
                    q = quaternion.Quaternion;
                }
                var conector = new SimNetworkBlockConnector(geometry, block, this, q);
                connectors.Add(geometry.Id, conector);


                AttachBlockEvents(block);
            }

        }


        private void AddStaticPort(SimNetworkPort port, Dictionary<ulong, BaseSimnetworkGeometryConnector> existingConnectors, bool addPortProxy)
        {
            Vertex geometry = null;

            if (port.RepresentationReference != GeometricReference.Empty)
            {
                geometry = this.GeometryModel.Geometry.GeometryFromId(port.RepresentationReference.GeometryId) as Vertex;
            }
            if (geometry == null)
            {
                Point3D position = new Point3D(0, 0, 0);

                if (connectors.TryGetValue(port.ParentNetworkElement.RepresentationReference.GeometryId, out var parentConnector))
                {
                    position = ((Vertex)parentConnector.Geometry).Position;

                }
                var portPosition = GetStaticPortGlobalPortPosition(port);

                geometry = new Vertex(this.GeometryModel.Geometry.Layers.First(), port.Name, portPosition)
                {
                    Color = port.Color
                };
            }
            if (geometry != null && connectors.TryGetValue(port.RepresentationReference.GeometryId, out var existingCon))
            {
                var portRelPosition = GetStaticPortGlobalPortPosition(port);
                ((Vertex)existingCon.Geometry).Position = portRelPosition;
            }
            else if (geometry != null && existingConnectors != null && existingConnectors.TryGetValue(geometry.Id, out var con)
                && existingConnectors.TryGetValue(port.ParentNetworkElement.RepresentationReference.GeometryId, out var prntConn))
            {
                var portRelPosition = GetStaticRelativePortPosition(port);
                var position = ((Vertex)prntConn.Geometry).Position;

                geometry.Position = position;
                con.ChangeBaseGeometry(geometry);
                connectors.Add(geometry.Id, con);
                existingConnectors.Remove(geometry.Id);
                AttachStaticPortEvents(port);
            }

            else
            {
                if (connectors.TryGetValue(port.ParentNetworkElement.RepresentationReference.GeometryId, out var parentConn))
                {
                    var portConnector = new SimNetworkPortConnector(geometry, port, this);
                    connectors.Add(geometry.Id, portConnector);
                    AttachStaticPortEvents(port);
                }
            }



            if (addPortProxy)
            {
                AddBlockPortConnectorProxy(port.ParentNetworkElement, port, existingConnectors);
            }
        }


        private void AttachStaticPortEvents(SimNetworkPort port)
        {

            if (port.ParentNetworkElement is SimNetworkBlock block && port.ComponentInstance != null && block.IsStatic)
            {
                var xParam = port.ComponentInstance.Component.Parameters.FirstOrDefault(n => n.NameTaxonomyEntry.TaxonomyEntryReference.Target.Key == ReservedParameterKeys.SIMNW_STATIC_PORT_POSITION_X);
                var yParam = port.ComponentInstance.Component.Parameters.FirstOrDefault(t => t.NameTaxonomyEntry.TaxonomyEntryReference.Target.Key == ReservedParameterKeys.SIMNW_STATIC_PORT_POSITION_Y);
                var zParam = port.ComponentInstance.Component.Parameters.FirstOrDefault(k => k.NameTaxonomyEntry.TaxonomyEntryReference.Target.Key == ReservedParameterKeys.SIMNW_STATIC_PORT_POSITION_Z);

                if (xParam != null && yParam != null && zParam != null)
                {
                    xParam.PropertyChanged -= this.Port_Param_PropertyChanged;
                    yParam.PropertyChanged -= this.Port_Param_PropertyChanged;
                    zParam.PropertyChanged -= this.Port_Param_PropertyChanged;


                    xParam.PropertyChanged += this.Port_Param_PropertyChanged;
                    yParam.PropertyChanged += this.Port_Param_PropertyChanged;
                    zParam.PropertyChanged += this.Port_Param_PropertyChanged;
                }
            }
        }

        private void AddStaticConnectorAsVertex(List<SimNetworkConnector> connectorChain, Dictionary<ulong, BaseSimnetworkGeometryConnector> existingConnectors, bool addPortConnectorProxies)
        {
            BaseGeometry geometry = null;
            var startNode = connectorChain.FirstOrDefault(c => c.Source.ParentNetworkElement is SimNetworkBlock).Source; //Start node is a port where the chain starts, and its parent is a block
            var endNode = connectorChain.FirstOrDefault(c => c.Target.ParentNetworkElement is SimNetworkBlock).Target; //End node is where the chain ends and the parent is a block

            if (connectorChain[0].RepresentationReference != GeometricReference.Empty)
            {
                geometry = this.GeometryModel.Geometry.GeometryFromId(connectorChain[0].RepresentationReference.GeometryId) as Vertex;
            }
            if (geometry == null)
            {
                Point3D position = new Point3D(0, 0, 0);
                if (connectors.TryGetValue(startNode.RepresentationReference.GeometryId, out var start) && connectors.TryGetValue(endNode.RepresentationReference.GeometryId, out var end))
                {
                    var startVertex = start.Geometry as Vertex;
                    var endVertex = end.Geometry as Vertex;
                    position = startVertex.Position;
                }
                else
                {
                    position = GetStaticPortGlobalPortPosition(startNode);
                }

                geometry = new Vertex(this.GeometryModel.Geometry.Layers.First(), connectorChain[0].Name, position);

            }
            if (geometry != null && existingConnectors != null && existingConnectors.TryGetValue(geometry.Id, out var con))
            {
                con.ChangeBaseGeometry(geometry);
                connectors.Add(geometry.Id, con);
                existingConnectors.Remove(geometry.Id);
            }
            else
            {
                var connectorConnector = new SimNetworkConnectorConnector(geometry as Vertex, connectorChain, this);
                connectors.Add(geometry.Id, connectorConnector);
            }
            if (addPortConnectorProxies)
            {
                AddBlockToConnectorproxy(connectorChain.First(), startNode, existingConnectors);
                AddBlockToConnectorproxy(connectorChain.First(), endNode, existingConnectors);
            }
        }

        private void AddStaticConnectorAsPoly(List<SimNetworkConnector> connectorChain, Dictionary<ulong, BaseSimnetworkGeometryConnector> existingConnectors)
        {
            BaseGeometry geometry = null;
            var startNode = connectorChain.FirstOrDefault(c => c.Source.ParentNetworkElement is SimNetworkBlock).Source; //Start node is a port where the chain starts, and its parent is a block
            var endNode = connectorChain.FirstOrDefault(c => c.Target.ParentNetworkElement is SimNetworkBlock).Target; //End node is where the chain ends and the parent is a block

            if (connectorChain[0].RepresentationReference != GeometricReference.Empty)
            {
                geometry = this.GeometryModel.Geometry.GeometryFromId(connectorChain[0].RepresentationReference.GeometryId) as Vertex;
            }
            if (geometry == null)
            {
                Vertex startVertex = null;
                Vertex endVertex = null;

                if (connectors.TryGetValue(startNode.RepresentationReference.GeometryId, out var startConnector)
                    && connectors.TryGetValue(endNode.RepresentationReference.GeometryId, out var endConnector))
                {
                    startVertex = startConnector.Geometry as Vertex;
                    endVertex = endConnector.Geometry as Vertex;
                }
                else
                {
                    var startPosition = GetStaticPortGlobalPortPosition(startNode);
                    var endPosition = GetStaticPortGlobalPortPosition(endNode);

                    startVertex = new Vertex(this.GeometryModel.Geometry.Layers.First(), startNode.Name, startPosition);
                    endVertex = new Vertex(this.GeometryModel.Geometry.Layers.First(), endNode.Name, endPosition);
                }


                var innerEdge = new Edge(this.GeometryModel.Geometry.Layers.First(), connectorChain[0].Name + "_EDGE1",
                    new Vertex[] { startVertex, endVertex })
                {
                    Color = new DerivedColor(Colors.Red),
                };

                geometry = new Polyline(this.GeometryModel.Geometry.Layers.First(), "CHAIN",
                new Edge[] { innerEdge });

            }
            if (geometry != null && existingConnectors != null && existingConnectors.TryGetValue(geometry.Id, out var con))
            {
                con.ChangeBaseGeometry(geometry);
                connectors.Add(geometry.Id, con);
                existingConnectors.Remove(geometry.Id);
            }
            else
            {
                var connectorConnector = new SimNetworkInvalidConnectorConnector(geometry as Polyline, connectorChain, this);
                connectors.Add(geometry.Id, connectorConnector);
            }
        }




        private void AddStaticConnector(List<SimNetworkConnector> connectorChain, Dictionary<ulong, BaseSimnetworkGeometryConnector> existingConnectors)
        {

            var startNode = connectorChain.FirstOrDefault(c => c.Source.ParentNetworkElement is SimNetworkBlock).Source; //Start node is a port where the chain starts, and its parent is a block
            var endNode = connectorChain.FirstOrDefault(c => c.Target.ParentNetworkElement is SimNetworkBlock).Target; //End node is where the chain ends and the parent is a block

            if (connectorChain[0].RepresentationReference != GeometricReference.Empty
                && connectors.TryGetValue(connectorChain[0].RepresentationReference.GeometryId, out var connectorConnector))
            {
                if (connectors.TryGetValue(startNode.RepresentationReference.GeometryId, out var sCon)
                     && connectors.TryGetValue(endNode.RepresentationReference.GeometryId, out var tCon))
                {
                    Point3D startPortPosittion = new Point3D(0, 0, 0);
                    Point3D endPortPosition = new Point3D(0, 0, 0);
                    startPortPosittion = GetStaticPortGlobalPortPosition(startNode);
                    endPortPosition = GetStaticPortGlobalPortPosition(endNode);

                    if (startPortPosittion == endPortPosition && connectorConnector.Geometry is Polyline polyGeom)
                    {
                        foreach (var edge in polyGeom.Edges)
                        {
                            edge.Edge.RemoveFromModel();
                        }
                        polyGeom.RemoveFromModel();
                        connectors.Remove(connectorChain[0].RepresentationReference.GeometryId);

                        //Clean the unused geometry
                        foreach (var edge in ((Vertex)sCon.Geometry).Edges)
                        {
                            var poliesToRemove = this.GeometryModel.Geometry.Polylines.Where(t => t.Edges.Any(p => p.Edge == edge)).ToList();
                            for (int i = poliesToRemove.Count() - 1; i >= 0; --i)
                            {
                                poliesToRemove[i].RemoveFromModel();
                            }

                        }
                        for (int i = ((Vertex)sCon.Geometry).Edges.Count - 1; i >= 0; --i)
                        {
                            ((Vertex)sCon.Geometry).Edges[i].RemoveFromModel();
                        }
                        sCon.Geometry.RemoveFromModel();


                        //Clean the unused geometry
                        foreach (var edge in ((Vertex)tCon.Geometry).Edges)
                        {
                            var poliesToRemove = this.GeometryModel.Geometry.Polylines.Where(t => t.Edges.Any(p => p.Edge == edge)).ToList();
                            for (int i = poliesToRemove.Count() - 1; i >= 0; --i)
                            {
                                poliesToRemove[i].RemoveFromModel();
                            }

                        }

                        for (int i = ((Vertex)tCon.Geometry).Edges.Count - 1; i >= 0; --i)
                        {
                            ((Vertex)tCon.Geometry).Edges[i].RemoveFromModel();
                        }

                        tCon.Geometry.RemoveFromModel();
                        AddStaticConnectorAsVertex(connectorChain, existingConnectors, true);
                        AttachStaticPortEvents(startNode);
                        AttachStaticPortEvents(endNode);
                    }
                    else if (startPortPosittion != endPortPosition && connectorConnector.Geometry is Vertex vertexGeom)
                    {
                        var geom = connectorConnector.Geometry;

                        for (int i = vertexGeom.Edges.Count - 1; i >= 0; --i)
                        {
                            vertexGeom.Edges[i].RemoveFromModel();
                        }
                        vertexGeom.RemoveFromModel();
                        connectors.Remove(connectorChain[0].RepresentationReference.GeometryId);
                        AddStaticPort(startNode, existingConnectors, true);
                        AddStaticPort(endNode, existingConnectors, true);
                        AddStaticConnectorAsPoly(connectorChain, existingConnectors);
                    }
                }
            }
            else
            {
                var startPortPosittion = GetStaticPortGlobalPortPosition(startNode);
                var endPortPosition = GetStaticPortGlobalPortPosition(endNode);
                if (startPortPosittion == endPortPosition)
                {
                    AddStaticConnectorAsVertex(connectorChain, existingConnectors, true);
                    AttachStaticPortEvents(startNode);
                    AttachStaticPortEvents(endNode);

                }
                else
                {
                    AddStaticPort(startNode, existingConnectors, true);
                    AddStaticPort(endNode, existingConnectors, true);
                    AddStaticConnectorAsPoly(connectorChain, existingConnectors);
                }
            }
        }

        /// <summary>
        /// Adds a block to a subnetwork
        /// </summary>
        /// <param name="connectorChain">The current chain of connectors</param>
        /// <param name="existingConnectors">The already imported connectors</param>
        private void AddBlockToSubnetworkConnector(List<SimNetworkConnector> connectorChain, Dictionary<ulong, BaseSimnetworkGeometryConnector> existingConnectors)
        {

            var startNode = connectorChain.FirstOrDefault(c => c.Source.ParentNetworkElement is SimNetworkBlock).Source; //Start node is a port where the chain starts, and its parent is a block
            var endNode = connectorChain.FirstOrDefault(c => c.Target.ParentNetworkElement is SimNetworkBlock).Target; //End node is where the chain ends and the parent is a block

            Vertex geometry = null;
            //Check if geometry for edge exists
            if (connectorChain[0].RepresentationReference != GeometricReference.Empty)
                geometry = GeometryModel.Geometry.GeometryFromId(connectorChain[0].RepresentationReference.GeometryId) as Vertex;

            if (geometry == null)
            {
                Point3D position = new Point3D(0, 0, 0);
                if (startNode.ParentNetworkElement is SimNetworkBlock b && b.IsStatic)
                {
                    position = GetStaticPortGlobalPortPosition(startNode);
                }
                else if (endNode.ParentNetworkElement is SimNetworkBlock b1 && b1.IsStatic)
                {
                    position = GetStaticPortGlobalPortPosition(endNode);
                }
                else
                {
                    position = GetDynamicPortGlobalPosition(startNode);
                    position = new Point3D(position.X, position.Y + startNode.ParentNetworkElement.Ports.Where(t => t.PortType == startNode.PortType).ToList().IndexOf(startNode) * 2, position.Z);
                }

                geometry = new Vertex(this.GeometryModel.Geometry.Layers.First(), startNode.Name, position);

            }
            if (geometry != null && existingConnectors != null && existingConnectors.TryGetValue(geometry.Id, out var con))
            {
                con.ChangeBaseGeometry(geometry);
                connectors.Add(geometry.Id, con);
                existingConnectors.Remove(geometry.Id);
            }
            else
            {
                if (connectors.TryGetValue(geometry.Id, out var asd))
                {
                    return;
                }
                var conConnector = new SimNetworkConnectorConnector(geometry, connectorChain, this);
                connectors.Add(geometry.Id, conConnector);
            }

            AddBlockToConnectorproxy(startNode.Connectors.FirstOrDefault(), startNode, existingConnectors);
            AddBlockToConnectorproxy(endNode.Connectors.FirstOrDefault(), endNode, existingConnectors);
        }

        private void AddConnector(SimNetworkConnector connector, Dictionary<ulong, BaseSimnetworkGeometryConnector> existingConnectors)
        {
            var startNode = connector.Source;
            var endNode = connector.Target;
            Vertex geometry = null;

            //Check if geometry for edge exists
            if (connector.RepresentationReference != GeometricReference.Empty)
                geometry = GeometryModel.Geometry.GeometryFromId(connector.RepresentationReference.GeometryId) as Vertex;

            if (geometry == null)
            {
                Point3D position = new Point3D();

                var startPortPosition = GetDynamicPortGlobalPosition(startNode);
                var endPortPosition = GetDynamicPortGlobalPosition(endNode);
                position = new Point3D(
                    (startPortPosition.X + endPortPosition.X) / 2,
                    (startPortPosition.Y + endPortPosition.Y) / 2,
                    (startPortPosition.Z + endPortPosition.Z) / 2
                    );
                geometry = new Vertex(this.GeometryModel.Geometry.Layers.First(), connector.Name, position)
                {
                    Color = connector.Color
                };
            }

            if (geometry != null && existingConnectors != null && existingConnectors.TryGetValue(geometry.Id, out var con))
            {
                con.ChangeBaseGeometry(geometry);
                connectors.Add(geometry.Id, con);
                existingConnectors.Remove(geometry.Id);
            }
            else
            {
                //Remove old Port connectors 
                if (connectors.TryGetValue(connector.Target.RepresentationReference.GeometryId, out var targetCon) &&
                    connectors.TryGetValue(connector.Source.RepresentationReference.GeometryId, out var sourceCon))
                {
                    RemovePort(connector.Target);
                    RemovePort(connector.Source);
                }
                var connectorConnector = new SimNetworkConnectorConnector(geometry, new List<SimNetworkConnector> { connector }, this);
                connectors.Add(geometry.Id, connectorConnector);
            }

            AddBlockToConnectorproxy(connector, connector.Source, existingConnectors); //Add a line to connect the source block to the vertex representing the connection
            AddBlockToConnectorproxy(connector, connector.Target, existingConnectors);  //Add a line to connect the target block to the vertex representing the connection


        }


        private Point3D GeStaticPortRelPosition(SimNetworkPort port)
        {
            if (port.ParentNetworkElement is SimNetworkBlock bl && bl.IsStatic)
            {
                if (port.ComponentInstance != null
                     && port.ComponentInstance.Component.Parameters.Any(p => p.NameTaxonomyEntry.TaxonomyEntryReference.Target.Key == ReservedParameterKeys.SIMNW_STATIC_PORT_POSITION_X)
                     && port.ComponentInstance.Component.Parameters.Any(p => p.NameTaxonomyEntry.TaxonomyEntryReference.Target.Key == ReservedParameterKeys.SIMNW_STATIC_PORT_POSITION_Y)
                     && port.ComponentInstance.Component.Parameters.Any(p => p.NameTaxonomyEntry.TaxonomyEntryReference.Target.Key == ReservedParameterKeys.SIMNW_STATIC_PORT_POSITION_Z))
                {
                    var relX = ((SimDoubleParameter)port.ComponentInstance.Component.Parameters.FirstOrDefault(p => p.NameTaxonomyEntry.TaxonomyEntryReference.Target.Key == ReservedParameterKeys.SIMNW_STATIC_PORT_POSITION_X)).Value;
                    var relY = ((SimDoubleParameter)port.ComponentInstance.Component.Parameters.FirstOrDefault(p => p.NameTaxonomyEntry.TaxonomyEntryReference.Target.Key == ReservedParameterKeys.SIMNW_STATIC_PORT_POSITION_Y)).Value;
                    var relZ = ((SimDoubleParameter)port.ComponentInstance.Component.Parameters.FirstOrDefault(p => p.NameTaxonomyEntry.TaxonomyEntryReference.Target.Key == ReservedParameterKeys.SIMNW_STATIC_PORT_POSITION_Z)).Value;
                    return new Point3D(relX, relY, relZ);
                }

                return new Point3D();
            }
            return new Point3D();
        }


        private Point3D GetDynamicPortGlobalPosition(SimNetworkPort port)
        {
            double positionX = 0;
            double positionY = 0;
            double positionZ = 0;

            if (connectors.TryGetValue(port.ParentNetworkElement.RepresentationReference.GeometryId, out var parentConnector))
            {
                if (port.PortType == PortType.Input)
                {
                    positionX = (((Vertex)parentConnector.Geometry).Position.X) - 2;
                    positionY = (((Vertex)parentConnector.Geometry).Position.Y) + (port.ParentNetworkElement.Ports.Where(t => t.PortType == PortType.Input).ToList().IndexOf(port) * 2);
                }
                else
                {
                    positionX = (((Vertex)parentConnector.Geometry).Position.X) + 2;
                    positionY = (((Vertex)parentConnector.Geometry).Position.Y) + (port.ParentNetworkElement.Ports.Where(t => t.PortType == PortType.Output).ToList().IndexOf(port) * 2);
                }

                positionZ = (((Vertex)parentConnector.Geometry).Position.Z);
            }

            else
            {
                if (port.PortType == PortType.Input)
                {

                    positionX = (port.ParentNetworkElement.Position.X) - 2;
                    positionY = (port.ParentNetworkElement.Position.Y + (port.ParentNetworkElement.Ports.Where(t => t.PortType == PortType.Input).ToList().IndexOf(port) * 2));
                }
                else
                {
                    positionX = (port.ParentNetworkElement.Position.X) + 2;
                    positionY = (port.ParentNetworkElement.Position.Y + (port.ParentNetworkElement.Ports.Where(t => t.PortType == PortType.Output).ToList().IndexOf(port) * 2));
                }
                positionZ = 0;
            }


            return new Point3D(positionX, positionY, positionZ);
        }


        private void AddDynamicBlock(BaseSimNetworkElement networkElement, Dictionary<ulong, BaseSimnetworkGeometryConnector> existingConnectors)
        {
            Vertex geometry = null;
            DerivedColor color = null;


            color = new DerivedColor(Color.FromArgb(10, (byte)rnd.Next(256), (byte)rnd.Next(256), (byte)rnd.Next(256)));
            if (networkElement.RepresentationReference != GeometricReference.Empty)
            {
                geometry = this.GeometryModel.Geometry.GeometryFromId(networkElement.RepresentationReference.GeometryId) as Vertex;
            }
            if (geometry == null)
            {
                color = networkElement is SimNetworkBlock bl ? bl.Color : networkElement is SimNetwork nw ? nw.Color : color;
                var blockColor = new DerivedColor(ChangeColorBrightness(color.Color, (float)-0.4));
                Point3D position = new Point3D(networkElement.Position.X / ReduceRatio, networkElement.Position.Y / ReduceRatio, 0);
                geometry = new Vertex(this.GeometryModel.Geometry.Layers.First(), networkElement.Name,
                    new Point3D(position.X, position.Y, 0))
                {
                    Color = color
                };
            }


            if (existingConnectors != null && existingConnectors.TryGetValue(geometry.Id, out var con))
            {
                con.ChangeBaseGeometry(geometry);
                connectors.Add(geometry.Id, con);
                existingConnectors.Remove(geometry.Id);
            }
            else if (networkElement is SimNetworkBlock block)
            {
                connectors.Add(geometry.Id, new SimNetworkBlockConnector(geometry, block, this, Quaternion.Identity));
                AttachBlockEvents(block);

            }
            else if (networkElement is SimNetwork network)
            {
                connectors.Add(geometry.Id, new SimNetworkNetworkConnector(geometry, network, this));
                AttachNetworkEvents(network);
            }
        }

        private void GetStaticGroups()
        {
            staticGroups.Clear();
            var groups = FindStaticGroups(this.Network, new List<List<SimNetworkBlock>>());
            foreach (var g in groups)
            {
                staticGroups.Add(g.FirstOrDefault(), g);
            }
        }



        /// <summary>
        /// Returns all the elements of the static group
        /// </summary>
        /// <param name="geom">A BaseGeometry which might be the part of a StaticBlock</param>
        /// <returns></returns>
        public IEnumerable<BaseGeometry> GetStaticGroupGeometries(BaseGeometry geom)
        {
            List<BaseGeometry> geoms = new List<BaseGeometry>();
            GetStaticGroups();
            if (connectors.TryGetValue(geom.Id, out var connector))
            {
                if (connector is SimNetworkBlockConnector blockConnector && blockConnector.Block.IsStatic)
                {
                    if (staticGroups.TryFirstOrDefault(t => t.Value.Contains(blockConnector.Block), out var staticGroup))
                    {
                        foreach (var block in staticGroup.Value)
                        {
                            if (connectors.TryGetValue(block.RepresentationReference.GeometryId, out var sConnector))
                            {
                                geoms.Add(sConnector.Geometry);
                            }
                            foreach (var port in block.Ports)
                            {
                                if (connectors.TryGetValue(port.RepresentationReference.GeometryId, out var pConnector))
                                {
                                    geoms.Add(pConnector.Geometry);
                                }
                                foreach (var con in port.Connectors)
                                {
                                    if (connectors.TryGetValue(con.RepresentationReference.GeometryId, out var cConnector))
                                    {
                                        geoms.Add(cConnector.Geometry);
                                    }
                                }
                            }
                        }
                    }

                }
                if (connector is SimNetworkPortConnector portConnector)
                {
                    if (connectors.TryGetValue(portConnector.Port.ParentNetworkElement.RepresentationReference.GeometryId, out var parentConnector))
                    {
                        geoms.AddRange(GetStaticGroupGeometries(parentConnector.Geometry));
                    }

                }
            }
            return geoms;
        }

        /// <summary>
        /// Function which returns the moved/rotated elements during a partial move/rotate operation. 
        /// Creates dummy Geometries for static-static connections
        /// Must call <see cref="RemoveDummyGeometries()"/> to remove these dummy geometry
        /// </summary>
        /// <returns></returns>
        public IEnumerable<BaseGeometry> StartMoveRotatePartialOperation(BaseGeometry geom)
        {
            GetStaticGroups();
            List<BaseGeometry> geoms = new List<BaseGeometry>();
            if (connectors.TryGetValue(geom.Id, out var connector))
            {
                if (connector is SimNetworkBlockConnector blockConnector)
                {
                    geoms.Add(geom);
                    foreach (var port in blockConnector.Block.Ports)
                    {
                        if (connectors.TryGetValue(port.RepresentationReference.GeometryId, out var pConnector) && port.Connectors.Count > 0 && pConnector is SimNetworkConnectorConnector connectorCon)
                        {
                            var connectorChain = FindConnectorChain(port.Connectors.First(), new List<SimNetworkConnector>());
                            var startNode = connectorChain.FirstOrDefault(c => c.Source.ParentNetworkElement is SimNetworkBlock).Source;
                            var endNode = connectorChain.FirstOrDefault(c => c.Target.ParentNetworkElement is SimNetworkBlock).Target;

                            if (
                                startNode != null && startNode.ParentNetworkElement is SimNetworkBlock block1 && block1.IsStatic &&
                                endNode != null && endNode.ParentNetworkElement is SimNetworkBlock block2 && block2.IsStatic)
                            {

                                SimNetworkPort nonMovedPort = null;
                                if (port == startNode)
                                {
                                    nonMovedPort = endNode;
                                }
                                else
                                {
                                    nonMovedPort = startNode;
                                }


                                connectors.TryGetValue(nonMovedPort.ParentNetworkElement.RepresentationReference.GeometryId, out var nonMovedBlock);
                                connectors.TryGetValue(nonMovedPort.RepresentationReference.GeometryId, out var nonMovedPortCon);

                                //Creating dummy which will be moved
                                var movedVertex = new Vertex(pConnector.Geometry.Layer, port.Name, ((Vertex)connectorCon.Geometry).Position)
                                {
                                    Color = new DerivedColor(Colors.Yellow),
                                };
                                geoms.Add(movedVertex);

                                //Creating a  dummy for the non moved port
                                var nonMovdVertex = new Vertex(pConnector.Geometry.Layer, port.Name, ((Vertex)connectorCon.Geometry).Position)
                                {
                                    Color = pConnector.Geometry.Color
                                };



                                //Dummy block to port proxy moved
                                var innerEdge = new Edge(this.GeometryModel.Geometry.Layers.First(), "DUMMY",

                                new Vertex[] { ((Vertex)blockConnector.Geometry), movedVertex })
                                {
                                    Color = new DerivedColor(Colors.Blue),
                                };

                                var blockToPortDummy1 = new Polyline(this.GeometryModel.Geometry.Layers.First(), "DUMMY",
                                new Edge[] { innerEdge });



                                //Dummy block to port proxy non-moved
                                var innerEdge2 = new Edge(this.GeometryModel.Geometry.Layers.First(), "DUMMY",
                                new Vertex[] { ((Vertex)nonMovedBlock.Geometry), nonMovdVertex })
                                {
                                    Color = new DerivedColor(Colors.Blue),
                                };

                                var blockToPortDummy2 = new Polyline(this.GeometryModel.Geometry.Layers.First(), "DUMMY",
                                new Edge[] { innerEdge });


                                var originalVertexPosition = new Vertex(pConnector.Geometry.Layer, port.Name, ((Vertex)pConnector.Geometry).Position)
                                {
                                    Color = pConnector.Geometry.Color
                                };
                                var nonValidConnectorEdge = new Edge(this.GeometryModel.Geometry.Layers.First(), "DUMMY",
                                new Vertex[] { movedVertex, nonMovdVertex })
                                {
                                    Color = new DerivedColor(Colors.Pink),
                                };

                                var invalidConnectionDumy = new Polyline(this.GeometryModel.Geometry.Layers.First(), "DUMMY",
                                new Edge[] { innerEdge2 });



                                //Clean the unused geometry
                                foreach (var edge in ((Vertex)pConnector.Geometry).Edges)
                                {
                                    var poliesToRemove = this.GeometryModel.Geometry.Polylines.Where(t => t.Edges.Any(p => p.Edge == edge)).ToList();
                                    for (int i = poliesToRemove.Count() - 1; i >= 0; --i)
                                    {
                                        poliesToRemove[i].RemoveFromModel();
                                    }

                                }
                                for (int i = ((Vertex)pConnector.Geometry).Edges.Count - 1; i >= 0; --i)
                                {
                                    ((Vertex)pConnector.Geometry).Edges[i].RemoveFromModel();

                                }
                                ((Vertex)pConnector.Geometry).RemoveFromModel();


                                DummyGeometries.Add(movedVertex);
                                DummyGeometries.Add(nonMovdVertex);
                                DummyGeometries.Add(blockToPortDummy1);
                                DummyGeometries.Add(blockToPortDummy2);
                                DummyGeometries.Add(invalidConnectionDumy);
                                DummyGeometries.Add(originalVertexPosition);
                                DummyGeometries.Add(innerEdge2);
                                DummyGeometries.Add(innerEdge);
                                DummyGeometries.Add(nonValidConnectorEdge);
                            }
                            else
                            {
                                geoms.Add(pConnector.Geometry);
                            }

                        }
                        else
                        {
                            geoms.Add(pConnector.Geometry);
                        }
                    }
                }
                if (connector is SimNetworkPortConnector portConnector)
                {
                    if (connectors.TryGetValue(portConnector.Port.ParentNetworkElement.RepresentationReference.GeometryId, out var parentConnector))
                    {
                        geoms.AddRange(StartMoveRotatePartialOperation(parentConnector.Geometry));
                    }

                }
            }
            return geoms;
        }

        /// <summary>
        /// Stops the partial transformation, removes the dummy geometries, updates according network geometry
        /// </summary>
        /// <param name="effectedGeoms">The geometries involved in the partial transformation <see cref="StartMoveRotatePartialOperation(BaseGeometry)"/></param>
        public void EndPartialTransform(List<BaseGeometry> effectedGeoms)
        {
            this.RemoveDummyGeometries();

            UpdateStaticConnectors(this.Network, null);
        }

        /// <summary>
        /// Removes the input geometry if it is a Dummy
        /// <see cref="DummyGeometries"/>
        /// </summary>
        private void RemoveDummyGeometries()
        {
            GeometryModel.Geometry.StartBatchOperation();
            for (int i = this.DummyGeometries.Count - 1; i >= 0; --i)
            {
                var dummyGeom = this.DummyGeometries[i];
                if (dummyGeom is Edge edge)
                {
                    edge.RemoveFromModel();
                }
                if (dummyGeom is Polyline poly)
                {
                    for (int j = poly.Edges.Count - 1; j >= 0; --j)
                    {
                        poly.Edges[j].Edge.RemoveFromModel();
                    }
                    poly.RemoveFromModel();
                }
                if (dummyGeom is Vertex vertex)
                {
                    for (int k = vertex.Edges.Count - 1; k >= 0; --k)
                    {
                        vertex.Edges[k].RemoveFromModel();
                    }
                    vertex.RemoveFromModel();
                }

            }

            this.DummyGeometries.Clear();
            GeometryModel.Geometry.EndBatchOperation();
        }


        private Point3D GetStaticRelativePortPosition(SimNetworkPort port)
        {
            if (connectors.TryGetValue(port.ParentNetworkElement.RepresentationReference.GeometryId, out var asd))
            {
                if (asd is SimNetworkBlockConnector blockConnector && blockConnector.transformInProgress)
                {
                    return new Point3D(double.NaN, double.NaN, double.NaN);
                }
            }
            if (port.ParentNetworkElement is SimNetworkBlock block && block.IsStatic)
            {
                double x = double.NaN;
                double y = double.NaN;
                double z = double.NaN;

                if (port.ComponentInstance == null || port.ComponentInstance.Component.Parameters.Count == 0)
                {
                    return new Point3D(double.NaN, double.NaN, double.NaN);
                }
                x = ((SimDoubleParameter)port.ComponentInstance.Component.Parameters.FirstOrDefault(t => t.NameTaxonomyEntry.TaxonomyEntryReference.Target.Key == ReservedParameterKeys.SIMNW_STATIC_PORT_POSITION_X)).Value;
                y = ((SimDoubleParameter)port.ComponentInstance.Component.Parameters.FirstOrDefault(t => t.NameTaxonomyEntry.TaxonomyEntryReference.Target.Key == ReservedParameterKeys.SIMNW_STATIC_PORT_POSITION_Y)).Value;
                z = ((SimDoubleParameter)port.ComponentInstance.Component.Parameters.FirstOrDefault(t => t.NameTaxonomyEntry.TaxonomyEntryReference.Target.Key == ReservedParameterKeys.SIMNW_STATIC_PORT_POSITION_Z)).Value;

                return new Point3D(x, y, z);
            }
            return new Point3D(double.NaN, double.NaN, double.NaN);
        }


        private void AddPort(SimNetworkPort port, Dictionary<ulong, BaseSimnetworkGeometryConnector> existingConnectors)
        {
            Vertex geometry = null;
            if (!connectors.TryGetValue(port.ParentNetworkElement.RepresentationReference.GeometryId, out var parentElement))
            {
                return;
            }
            if (port.RepresentationReference != GeometricReference.Empty)
            {
                var globalPosition = GetDynamicPortGlobalPosition(port);
                geometry = this.GeometryModel.Geometry.GeometryFromId(port.RepresentationReference.GeometryId) as Vertex;
                if (geometry != null && globalPosition != geometry.Position)
                {
                    geometry.Position = globalPosition;
                }
            }
            if (geometry == null)
            {
                var color = port.Color;
                var position = GetDynamicPortGlobalPosition(port);
                geometry = new Vertex(this.GeometryModel.Geometry.Layers.First(), port.Name, position)
                {
                    Color = color
                };
            }
            if (connectors.TryGetValue(geometry.Id, out var con))
            {
                if (this.connectors.TryGetValue(port.RepresentationReference.GeometryId, out var portConnector))
                {
                    portConnector.ChangeBaseGeometry(geometry);
                }
                else
                {
                    con.ChangeBaseGeometry(geometry);
                    connectors.Add(geometry.Id, con);
                    existingConnectors.Remove(geometry.Id);
                }

            }
            else
            {
                if (connectors.TryGetValue(port.ParentNetworkElement.RepresentationReference.GeometryId, out var parentConnector))
                {
                    var portConnector = new SimNetworkPortConnector(geometry, port, this);
                    connectors.Add(geometry.Id, portConnector);
                }
            }

            AddBlockPortConnectorProxy(port.ParentNetworkElement, port, existingConnectors);
        }





        private (Point3D Min, Point3D Max) GetSubnetworkBoundingBox(SimNetwork network)
        {
            double? minX = null;
            double? minY = null;
            double? minZ = null;
            double? maxX = null;
            double? maxY = null;
            double? maxZ = null;


            for (int i = 0; i < network.ContainedElements.Count; i++)
            {

                if (connectors.TryGetValue(network.ContainedElements[i].RepresentationReference.GeometryId, out var connector))
                {
                    if (connector is SimNetworkBaseNetworkElementConnector baseElementCon)
                    {
                        if (i == 0)
                        {
                            minX = baseElementCon.Vertex.Position.X;
                            minY = baseElementCon.Vertex.Position.Y;
                            minZ = baseElementCon.Vertex.Position.Z;
                            maxX = baseElementCon.Vertex.Position.X;
                            maxY = baseElementCon.Vertex.Position.Y;
                            maxZ = baseElementCon.Vertex.Position.Z;
                        }

                        if (baseElementCon.Vertex.Position.X < minX)
                        {
                            minX = baseElementCon.Vertex.Position.X;
                        }
                        if (baseElementCon.Vertex.Position.Y < minY)
                        {
                            minY = baseElementCon.Vertex.Position.Y;
                        }
                        if (baseElementCon.Vertex.Position.Z < minZ)
                        {
                            minZ = baseElementCon.Vertex.Position.Z;
                        }
                        if (baseElementCon.Vertex.Position.X > maxX)
                        {
                            maxX = baseElementCon.Vertex.Position.X;
                        }
                        if (baseElementCon.Vertex.Position.Y > maxY)
                        {
                            maxY = baseElementCon.Vertex.Position.Y;
                        }
                        if (baseElementCon.Vertex.Position.Z > maxZ)
                        {
                            maxZ = baseElementCon.Vertex.Position.Z;
                        }
                    }

                }
                else if (network.ContainedElements[i] is SimNetwork subNetworkWithElements)
                {
                    var subBox = GetSubnetworkBoundingBox(subNetworkWithElements);
                    if (minX == null)
                        minX = subBox.Min.X;
                    if (minY == null)
                        minY = subBox.Min.Y;
                    if (minZ == null)
                        minZ = subBox.Min.Z;
                    if (maxX == null)
                        maxX = subBox.Max.X;
                    if (maxY == null)
                        maxY = subBox.Max.Y;
                    if (maxZ == null)
                        maxZ = subBox.Max.Z;




                    if (subBox.Min.X < minX)
                    {
                        minX = subBox.Min.X;
                    }
                    if (subBox.Min.Y < minY)
                    {
                        minY = subBox.Min.Y;
                    }
                    if (subBox.Min.Z < minZ)
                    {
                        minZ = subBox.Min.Z;
                    }
                    if (subBox.Max.X > maxX)
                    {
                        maxX = subBox.Max.X;
                    }
                    if (subBox.Max.Y < maxY)
                    {
                        maxY = subBox.Max.Y;
                    }
                    if (subBox.Max.Z < maxZ)
                    {
                        maxZ = subBox.Max.Z;
                    }
                }
            }
            return (new Point3D(Convert.ToDouble(minX), Convert.ToDouble(minY), Convert.ToDouble(minZ)),
                new Point3D(Convert.ToDouble(maxX), Convert.ToDouble(maxY), Convert.ToDouble(maxZ)));
        }



        private void Port_Param_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SimBaseParameter<object>.Value) && sender is SimBaseParameter param && param.Component != null &&
               (param.NameTaxonomyEntry.TaxonomyEntryReference.Target.Key == ReservedParameterKeys.SIMNW_STATIC_PORT_POSITION_X ||
               param.NameTaxonomyEntry.TaxonomyEntryReference.Target.Key == ReservedParameterKeys.SIMNW_STATIC_PORT_POSITION_Y ||
               param.NameTaxonomyEntry.TaxonomyEntryReference.Target.Key == ReservedParameterKeys.SIMNW_STATIC_PORT_POSITION_Z))
            {

                this.connectors.Where(c => c.Value is SimNetworkBlockConnector).ForEach(c => ((SimNetworkBlockConnector)c.Value).transformInProgress = true);


                var placements = param.Component.Instances.SelectMany(t => t.Placements)
                    .Where(p => p is SimInstancePlacementSimNetwork plcmnt
                    && plcmnt.NetworkElement is SimNetworkPort port
                    && connectors.TryGetValue(port.RepresentationReference.GeometryId, out var portConnector));

                foreach (var item in placements)
                {
                    if (((SimNetworkPort)((SimInstancePlacementSimNetwork)item).NetworkElement).ParentNetworkElement is SimNetworkBlock bl && bl.IsStatic)
                    {
                        AddStaticPort(((SimNetworkPort)((SimInstancePlacementSimNetwork)item).NetworkElement), this.connectors, false);
                        this.UpdateStaticConnectors(this.Network, null);
                    }
                    else
                    {
                        AddPort(((SimNetworkPort)((SimInstancePlacementSimNetwork)item).NetworkElement), this.connectors);
                    }

                }


                this.connectors.Where(c => c.Value is SimNetworkBlockConnector).ForEach(c => ((SimNetworkBlockConnector)c.Value).transformInProgress = false);
            }
        }


        private void AddStaticDynamicConnector(SimNetworkConnector blockConnector, Dictionary<ulong, BaseSimnetworkGeometryConnector> existingConnectors)
        {
            SimNetworkPort staticPort = null;
            SimNetworkPort dynamicPort = null;

            if (blockConnector.Target.ParentNetworkElement is SimNetworkBlock bl && bl.IsStatic)
            {
                staticPort = blockConnector.Target;
                dynamicPort = blockConnector.Source;
            }
            else
            {
                staticPort = blockConnector.Source;
                dynamicPort = blockConnector.Target;
            }
            //Check if the dynamic port already has the connector 
            if (!connectors.TryGetValue(dynamicPort.RepresentationReference.GeometryId, out var dynamicConnector))
            {
                var staticPosition = GetStaticPortGlobalPortPosition(staticPort);
                var dynamicPortGeom = new Vertex(this.GeometryModel.Geometry.Layers.First(), blockConnector.Name, staticPosition);
                var dyamicPortConnector = new SimNetworkPortConnector(dynamicPortGeom, dynamicPort, this);
                connectors.Add(dynamicPortGeom.Id, dyamicPortConnector);
            }
            Vertex geometry = null;
            //Check if geometry for edge exists
            if (blockConnector.RepresentationReference != GeometricReference.Empty)
                geometry = GeometryModel.Geometry.GeometryFromId(blockConnector.RepresentationReference.GeometryId) as Vertex;

            if (geometry == null)
            {
                Point3D position = new Point3D();
                position = GetStaticPortGlobalPortPosition(staticPort);
                geometry = new Vertex(this.GeometryModel.Geometry.Layers.First(), blockConnector.Name, position);
            }

            if (geometry != null && existingConnectors != null && existingConnectors.TryGetValue(geometry.Id, out var con))
            {
                con.ChangeBaseGeometry(geometry);
                connectors.Add(geometry.Id, con);
                existingConnectors.Remove(geometry.Id);
            }
            else
            {
                var connectorConnector = new SimNetworkConnectorConnector(geometry, new List<SimNetworkConnector> { blockConnector }, this);
                connectors.Add(geometry.Id, connectorConnector);
            }

            AddBlockToConnectorproxy(blockConnector, staticPort, existingConnectors);
            AddBlockToConnectorproxy(blockConnector, dynamicPort, existingConnectors);
        }


        private void AddBlockToConnectorproxy(SimNetworkConnector connector, SimNetworkPort port, Dictionary<ulong, BaseSimnetworkGeometryConnector> existingConnectors)
        {
            Vertex startVertex = null;
            Vertex endVertex = null;

            if (connectors.TryGetValue(port.ParentNetworkElement.RepresentationReference.GeometryId, out var parentConnector))
            {
                startVertex = parentConnector.Geometry as Vertex;
            }
            else
            {
                throw new Exception("Parent connector was not found");
            }
            if (connectors.TryGetValue(connector.RepresentationReference.GeometryId, out var connectorConnector))
            {
                endVertex = connectorConnector.Geometry as Vertex;
            }
            else
            {
                throw new Exception("Connector connector was not found");
            }

            var innerEdge = new Edge(this.GeometryModel.Geometry.Layers.First(), startVertex.Name + "_EDGE2",
                new Vertex[] { startVertex, endVertex })
            { Color = connector.Color };

            var edgeGeometry = new Polyline(this.GeometryModel.Geometry.Layers.First(), startVertex.Name + "_PROXY",
                new Edge[] { innerEdge })
            { Color = connector.Color }; ;


            if (existingConnectors != null && existingConnectors.TryGetValue(edgeGeometry.Id, out var con))
            {
                con.ChangeBaseGeometry(edgeGeometry);
                connectors.Add(edgeGeometry.Id, con);
                existingConnectors.Remove(edgeGeometry.Id);
            }
            else
            {
                connectors.Add(edgeGeometry.Id, new SimNetworkBlockPortConnectorProxy(edgeGeometry, port.ParentNetworkElement, port));
            }
            // RemovePort(port); //--> this caused inconsistency in the geometry
        }


        private void AddBlockPortConnectorProxy(BaseSimNetworkElement parentElement, SimNetworkPort port, Dictionary<ulong, BaseSimnetworkGeometryConnector> existingConnectors)
        {

            Vertex startVertex = null;
            Vertex endVertex = null;

            if (connectors.TryGetValue(parentElement.RepresentationReference.GeometryId, out var parentConnector))
            {
                startVertex = parentConnector.Geometry as Vertex;
            }
            else
            {
                throw new Exception("Parent connector was not found");
            }
            if (connectors.TryGetValue(port.RepresentationReference.GeometryId, out var portConnector))
            {
                endVertex = portConnector.Geometry as Vertex;
            }
            else
            {
                endVertex = connectors[port.RepresentationReference.GeometryId].Geometry as Vertex;
            }

            var innerEdge = new Edge(this.GeometryModel.Geometry.Layers.First(), startVertex.Name + "_to_" + endVertex.Name,
                new Vertex[] { startVertex, endVertex });

            var edgeGeometry = new Polyline(this.GeometryModel.Geometry.Layers.First(), startVertex.Name + "_PROXY",
                new Edge[] { innerEdge });


            if (existingConnectors != null && existingConnectors.TryGetValue(edgeGeometry.Id, out var con))
            {
                con.ChangeBaseGeometry(edgeGeometry);
                connectors.Add(edgeGeometry.Id, con);
                existingConnectors.Remove(edgeGeometry.Id);
            }
            else
            {
                connectors.Add(edgeGeometry.Id, new SimNetworkBlockPortConnectorProxy(edgeGeometry, parentElement, port));
            }
        }


        private void AttachBlockEvents(SimNetworkBlock block)
        {
            block.Ports.CollectionChanged += this.Ports_CollectionChanged;
        }



        private void AttachNetworkEvents(SimNetwork simNetwork)
        {

            simNetwork.ContainedElements.CollectionChanged += this.ContainedElements_CollectionChanged;
            simNetwork.ContainedConnectors.CollectionChanged += this.ContainedConnectors_CollectionChanged;
            simNetwork.Ports.CollectionChanged += this.Ports_CollectionChanged;
            foreach (var subnet in simNetwork.ContainedElements.Where(n => n is SimNetwork))
                AttachNetworkEvents(subnet as SimNetwork);
        }



        private void Ports_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                    foreach (var item in e.NewItems)
                    {
                        if (item is SimNetworkPort port && sender is SimNetworkPortCollection portCollection)
                        {
                            var parentConnector = this.connectors
                                .FirstOrDefault(t => t.Value is SimNetworkBlockConnector blockConnector && blockConnector.Block == portCollection.parentElement).Value;
                            this.connectors.Where(c => c.Value is SimNetworkBlockConnector).ForEach(c => ((SimNetworkBlockConnector)c.Value).transformInProgress = true);

                            AddPort(port, null);

                            this.connectors.Where(c => c.Value is SimNetworkBlockConnector).ForEach(c => ((SimNetworkBlockConnector)c.Value).transformInProgress = false);
                        }
                        this.UpdateStaticConnectors(this.Network, null);
                    }
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                    foreach (var item in e.OldItems)
                    {
                        if (item is SimNetworkPort port)
                        {
                            RemovePort(port);
                        }
                    }
                    break;

            }
        }


        private void ContainedConnectors_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                    foreach (var item in e.NewItems)
                    {
                        var con = item as SimNetworkConnector;
                        if (((con.Target.ParentNetworkElement is SimNetworkBlock tBlock && tBlock.IsStatic) && (con.Source.ParentNetworkElement is SimNetworkBlock sBlock && sBlock.IsStatic)))
                        {
                            AddStaticConnector(new List<SimNetworkConnector>() { con }, null);
                        }
                        else if ((con.Target.ParentNetworkElement is SimNetworkBlock block && con.Source.ParentNetworkElement is SimNetworkBlock b) &&
                              ((block.IsStatic && !b.IsStatic) || (!block.IsStatic && b.IsStatic)))
                        {
                            AddStaticDynamicConnector(con, null);
                        }
                        else
                        {
                            AddConnector(con, null);
                        }
                    }
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                    foreach (var item in e.OldItems)
                    {
                        if (item is SimNetworkConnector connector)
                        {
                            RemoveConnector(connector);
                        }
                    }
                    break;

            }
        }




        private void ContainedElements_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            GeometryModel.Geometry.StartBatchOperation();

            switch (e.Action)
            {
                case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                    foreach (var item in e.NewItems)
                    {
                        if (item is SimNetworkBlock nwElement)
                            AddDynamicBlock(nwElement, null);
                    }
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                    foreach (var item in e.OldItems)
                    {
                        if (item is SimNetworkBlock block)
                        {
                            RemoveBlock(block);
                        }
                        if (item is SimNetwork network)
                        {
                            RemoveSimNetwork(network);
                        }
                    }
                    break;

            }
            GeometryModel.Geometry.EndBatchOperation();
        }


        private void RemoveBlock(SimNetworkBlock block)
        {
            if (block.RepresentationReference != GeometricReference.Empty)
            {
                if (block.RepresentationReference.FileId != this.GeometryModel.File.Key)
                    throw new Exception("Block is not connected to this geometry model");

                var geometry = this.GeometryModel.Geometry.GeometryFromId(block.RepresentationReference.GeometryId) as Vertex;
                if (geometry != null)
                {
                    if (connectors.TryGetValue(geometry.Id, out var con))
                    {
                        con.Dispose();
                        connectors.Remove(geometry.Id);
                    }

                    GeometryModel.Geometry.StartBatchOperation();

                    //Delete edges that use this vertex
                    foreach (var edge in geometry.Edges)
                        edge.RemoveFromModel();

                    //Delete proxy
                    foreach (var pro in geometry.ProxyGeometries)
                        pro.RemoveFromModel();

                    geometry.RemoveFromModel();

                    GeometryModel.Geometry.EndBatchOperation();
                }
            }
            foreach (var port in block.Ports)
            {
                RemovePort(port);
            }
        }

        private void RemovePort(SimNetworkPort port)
        {
            if (port.RepresentationReference != GeometricReference.Empty)
            {
                if (port.RepresentationReference.FileId != this.GeometryModel.File.Key)
                    throw new Exception("Port is not connected to this geometry model");



                if (port.ParentNetworkElement is SimNetworkBlock block && port.ComponentInstance != null && block.IsStatic)
                {
                    var xParam = port.ComponentInstance.Component.Parameters.FirstOrDefault(n => n.NameTaxonomyEntry.TaxonomyEntryReference.Target.Key == ReservedParameterKeys.SIMNW_STATIC_PORT_POSITION_X);
                    var yParam = port.ComponentInstance.Component.Parameters.FirstOrDefault(t => t.NameTaxonomyEntry.TaxonomyEntryReference.Target.Key == ReservedParameterKeys.SIMNW_STATIC_PORT_POSITION_Y);
                    var zParam = port.ComponentInstance.Component.Parameters.FirstOrDefault(k => k.NameTaxonomyEntry.TaxonomyEntryReference.Target.Key == ReservedParameterKeys.SIMNW_STATIC_PORT_POSITION_Z);

                    if (xParam != null && yParam != null && zParam != null)
                    {
                        xParam.PropertyChanged -= this.Port_Param_PropertyChanged;
                        yParam.PropertyChanged -= this.Port_Param_PropertyChanged;
                        zParam.PropertyChanged -= this.Port_Param_PropertyChanged;
                    }
                }

                var geometry = this.GeometryModel.Geometry.GeometryFromId(port.RepresentationReference.GeometryId) as Vertex;
                if (geometry != null)
                {
                    if (connectors.TryGetValue(geometry.Id, out var con))
                    {
                        con.Dispose();
                        connectors.Remove(geometry.Id);
                    }

                    GeometryModel.Geometry.StartBatchOperation();



                    //Delete edges that use this vertex
                    foreach (var edge in geometry.Edges)
                    {
                        var poliesToRemove = this.GeometryModel.Geometry.Polylines.Where(t => t.Edges.Any(p => p.Edge == edge)).ToList();
                        for (int i = poliesToRemove.Count() - 1; i >= 0; --i)
                        {
                            poliesToRemove[i].RemoveFromModel();
                        }
                        edge.RemoveFromModel();
                    }


                    //Delete proxy
                    foreach (var pro in geometry.ProxyGeometries)
                        pro.RemoveFromModel();

                    geometry.RemoveFromModel();

                    GeometryModel.Geometry.EndBatchOperation();
                }
            }
        }

        private void RemoveConnector(SimNetworkConnector bConnector)
        {
            if (bConnector.RepresentationReference != GeometricReference.Empty)
            {
                if (bConnector.RepresentationReference.FileId != this.GeometryModel.File.Key)
                    throw new Exception("Port is not connected to this geometry model");

                if (connectors.TryGetValue(bConnector.RepresentationReference.GeometryId, out var connector))
                {
                    if (connector is SimNetworkConnectorConnector conCon)
                    {

                        var geometry = connector.Geometry as Vertex;
                        if (connectors.TryGetValue(geometry.Id, out var con))
                        {
                            con.Dispose();
                        }

                        GeometryModel.Geometry.StartBatchOperation();

                        //Delete edges that use this vertex
                        foreach (var edge in geometry.Edges)
                            edge.RemoveFromModel();

                        //Delete proxy
                        foreach (var pro in geometry.ProxyGeometries)
                            pro.RemoveFromModel();

                        connectors.Remove(geometry.Id);
                        geometry.RemoveFromModel();

                        GeometryModel.Geometry.EndBatchOperation();
                    }
                    else if (connector is SimNetworkInvalidConnectorConnector inCon)
                    {
                        var invalidConnector = this.GeometryModel.Geometry.GeometryFromId(bConnector.RepresentationReference.GeometryId) as Polyline;
                        invalidConnector.RemoveFromModel();
                        //Delete edges that use this vertex
                        foreach (var edge in invalidConnector.Edges)
                            edge.Edge.RemoveFromModel();

                        connectors.Remove(invalidConnector.Id);
                    }
                }
            }
            if (bConnector.Source.ParentNetworkElement is SimNetworkBlock block && block.IsStatic)
            {
                AddStaticPort(bConnector.Source, null, false);
            }
            else
            {
                AddPort(bConnector.Source, null);
            }
            if (bConnector.Target.ParentNetworkElement is SimNetworkBlock block1 && block1.IsStatic)
            {
                AddStaticPort(bConnector.Target, null, false);
            }
            else
            {
                AddPort(bConnector.Target, null);
            }
        }

        private void RemoveSimNetwork(SimNetwork network)
        {
            if (network.RepresentationReference != GeometricReference.Empty)
            {
                if (network.RepresentationReference.FileId != this.GeometryModel.File.Key)
                    throw new Exception("Network is not connected to this geometry model");

                var geometry = this.GeometryModel.Geometry.GeometryFromId(network.RepresentationReference.GeometryId) as Vertex;
                if (geometry != null)
                {
                    if (connectors.TryGetValue(geometry.Id, out var con))
                    {
                        con.Dispose();
                        connectors.Remove(geometry.Id);
                    }

                    GeometryModel.Geometry.StartBatchOperation();

                    //Delete edges that use this vertex
                    foreach (var edge in geometry.Edges)
                        edge.RemoveFromModel();

                    //Delete proxy
                    foreach (var pro in geometry.ProxyGeometries)
                        pro.RemoveFromModel();

                    geometry.RemoveFromModel();

                    GeometryModel.Geometry.EndBatchOperation();
                }
            }
        }

        private void CleanUnusedGeometry()
        {

            for (int i = GeometryModel.Geometry.Volumes.Count - 1; i >= 0; --i)
                GeometryModel.Geometry.Volumes[i].RemoveFromModel();
            for (int i = GeometryModel.Geometry.Faces.Count - 1; i >= 0; --i)
                GeometryModel.Geometry.Faces[i].RemoveFromModel();
            for (int i = GeometryModel.Geometry.EdgeLoops.Count - 1; i >= 0; --i)
                GeometryModel.Geometry.EdgeLoops[i].RemoveFromModel();

            for (int i = 0; i < GeometryModel.Geometry.Polylines.Count; ++i)
            {
                if (!connectors.ContainsKey(GeometryModel.Geometry.Polylines[i].Id))
                {
                    GeometryModel.Geometry.Polylines[i].RemoveFromModel();
                    i--;
                }
            }

            HashSet<BaseGeometry> usedEdges = GeometryModel.Geometry.Polylines.SelectMany(x => x.Edges).Select(x => (BaseGeometry)x.Edge).ToHashSet();

            for (int i = 0; i < GeometryModel.Geometry.Edges.Count; ++i)
            {
                var e = GeometryModel.Geometry.Edges[i];
                if (!usedEdges.Contains(e))
                {
                    e.RemoveFromModel();
                    i--;
                }
            }

            HashSet<Vertex> usedVertices = GeometryModel.Geometry.Edges.SelectMany(x => x.Vertices).ToHashSet();
            foreach (var connector in connectors.Where(t => t.Value is SimNetworkConnectorConnector || t.Value is SimNetworkBlockConnector))
            {
                usedVertices.Add(connector.Value.Geometry as Vertex);
            }

            for (int i = 0; i < GeometryModel.Geometry.Vertices.Count; ++i)
            {
                var v = GeometryModel.Geometry.Vertices[i];
                if (!usedVertices.Contains(v))
                {
                    v.RemoveFromModel();
                    i--;
                }
            }
            usedEdges.Clear(); usedEdges = null;

            for (int i = 0; i < GeometryModel.Geometry.ProxyGeometries.Count; ++i)
            {
                var p = GeometryModel.Geometry.ProxyGeometries[i];
                if (!usedVertices.Contains(p.Vertex))
                {
                    p.RemoveFromModel();
                    i--;
                }
            }
        }





        #endregion



        #region Dispose

        private void DetachEvents(SimNetwork simNetwork)
        {
            simNetwork.ContainedElements.CollectionChanged -= this.ContainedElements_CollectionChanged;
            simNetwork.ContainedConnectors.CollectionChanged -= this.ContainedConnectors_CollectionChanged;
            simNetwork.Ports.CollectionChanged -= this.Ports_CollectionChanged;
            simNetwork.RepresentationReference = GeometricReference.Empty;
            foreach (var block in simNetwork.ContainedElements.Where(t => t is SimNetworkBlock))
            {
                block.RepresentationReference = GeometricReference.Empty;
                block.Ports.CollectionChanged -= this.Ports_CollectionChanged;
                foreach (var port in block.Ports)
                {
                    port.RepresentationReference = GeometricReference.Empty;
                    foreach (var con in port.Connectors)
                    {
                        con.RepresentationReference = GeometricReference.Empty;
                    }
                }
            }


            foreach (var subnet in simNetwork.ContainedElements.Where(t => t is SimNetwork))
                DetachEvents(subnet as SimNetwork);
        }
        /// <summary>
        /// Frees all resources created by this connector and detaches all event handler
        /// </summary>
        internal void Dispose()
        {
            GeometryModel.Geometry.TopologyChanged -= this.Geometry_TopologyChanged;
            this.GeometryModel.Replaced -= this.GeometryModel_Replaced;

            DetachEvents(Network);

            foreach (var con in connectors.Values)
                con.Dispose();
        }



        /// <summary>
        /// Creates color with corrected brightness.
        /// </summary>
        /// <param name="color">Color to correct.</param>
        /// <param name="correctionFactor">The brightness correction factor. Must be between -1 and 1. 
        /// Negative values produce darker colors.</param>
        /// <returns>
        /// Corrected <see cref="Color"/> structure.
        /// </returns>
        private static Color ChangeColorBrightness(Color color, float correctionFactor)
        {
            float red = color.R;
            float green = color.G;
            float blue = color.B;

            if (correctionFactor < 0)
            {
                correctionFactor = 1 + correctionFactor;
                red *= correctionFactor;
                green *= correctionFactor;
                blue *= correctionFactor;
            }
            else
            {
                red = (255 - red) * correctionFactor + red;
                green = (255 - green) * correctionFactor + green;
                blue = (255 - blue) * correctionFactor + blue;
            }

            return Color.FromArgb(color.A, (byte)red, (byte)green, (byte)blue);
        }

        #endregion
    }
}

