using MathNet.Numerics;
using SIMULTAN.Data;
using SIMULTAN.Data.Components;
using SIMULTAN.Data.Geometry;
using SIMULTAN.Data.SimMath;
using SIMULTAN.Data.SimNetworks;
using SIMULTAN.Utils;
using Sprache;
using System;
using System.Collections.Generic;
using System.Linq;
using static SIMULTAN.Data.SimNetworks.BaseSimNetworkElement;
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


    /// <summary>
    /// Handles connections between a <see cref="SimNetwork"/> and a <see cref="GeometryModel"/>
    /// </summary>
    public partial class SimNetworkGeometryModelConnector
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
        /// Random number generator for creating colors
        /// </summary>
        private Random rnd = new Random();


        private Dictionary<ulong, BaseSimnetworkGeometryConnector> connectors = new Dictionary<ulong, BaseSimnetworkGeometryConnector>();
        private Dictionary<SimNetworkBlock, List<SimNetworkBlock>> staticGroups = new Dictionary<SimNetworkBlock, List<SimNetworkBlock>>();
        #endregion

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
        }

        /// <summary>
        /// Gets the network element by the geometry representation
        /// </summary>
        /// <returns></returns>
        public IEnumerable<ISimNetworkElement> GetNetworkElements(BaseGeometry geometry)
        {
            if (connectors.TryGetValue(geometry.Id, out var connector))
            {
                return connector.SimNetworkElement;
            }
            return null;
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
        /// Updates the given network´s geometry
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

            AddNetwork(network, existingConnectors);

            GeometryModel.Geometry.EndBatchOperation();
            CleanUnusedGeometry();

            foreach (var con in existingConnectors.Values)
                con.Dispose();
        }


        private void AddNetwork(SimNetwork subnetwork, Dictionary<ulong, BaseSimnetworkGeometryConnector> existingConnectors)
        {

            if (subnetwork.ContainedElements.Count == 0) //If there is not contained element, Add the SubnEtwork is a Vertex
            {
                AddDynamicBlock(subnetwork, existingConnectors);
            }
            else
            {
                AddNestedElements(subnetwork, existingConnectors);
            }
            AttachNetworkEvents(subnetwork);
        }

        private void AddNestedElements(SimNetwork network, Dictionary<ulong, BaseSimnetworkGeometryConnector> existingConnectors)
        {
            //Make sure that all nodes of the flattened network exist in the geometry model
            staticGroups.Clear();
            GetStaticGroups();
            UpdateStaticBlocks(network, staticGroups, existingConnectors);
            UpdateBlocks(network, existingConnectors);
            UpdatePorts(network, existingConnectors);
            UpdateSubnetworks(network, existingConnectors);
            UpdateNetworkConnectors(network, existingConnectors);
        }


        private void UpdatePorts(SimNetwork network, Dictionary<ulong, BaseSimnetworkGeometryConnector> existingConnectors)
        {
            foreach (var parentElement in network.ContainedElements.Where(t => t is BaseSimNetworkElement))
                foreach (var port in parentElement.Ports.Where(p => p.Connectors.Count == 0 && ((p.ParentNetworkElement is SimNetworkBlock bl && !bl.IsStatic || p.ParentNetworkElement is SimNetwork))))
                    AddPort(port, existingConnectors);
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
                var globalPosition = GetPortGlobalPosition(port);
                geometry = this.GeometryModel.Geometry.GeometryFromId(port.RepresentationReference.GeometryId) as Vertex;
            }
            if (geometry == null)
            {
                var color = port.Color;
                var position = GetPortGlobalPosition(port);
                geometry = new Vertex(this.GeometryModel.Geometry.Layers.First(), port.Name, position)
                {
                    Color = new DerivedColor(color)
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
            this.UpdateNetworkConnectors(this.Network, null);
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
                AddNetwork(node as SimNetwork, existingConnectors);
                AdjustImportedElements(node as SimNetwork);
            }
        }

        private void UpdateStaticBlocks(SimNetwork network, Dictionary<SimNetworkBlock, List<SimNetworkBlock>> staticGroups, Dictionary<ulong, BaseSimnetworkGeometryConnector> existingConnectors)
        {
            foreach (var staticGroup in staticGroups)
            {
                var handledBlocks = new List<(SimNetworkBlock block, SimMatrix3D transformation)>();
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
                    var transVector = new SimVector3D(0, 0, 0);
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


        private void UpdateNetworkConnectors(SimNetwork network, Dictionary<ulong, BaseSimnetworkGeometryConnector> existingConnectors)
        {
            foreach (var connector in network.ContainedConnectors)
            {
                UpdateConnector(connector, existingConnectors);
            }
            foreach (var subNet in network.ContainedElements.Where(r => r is SimNetwork))
                UpdateNetworkConnectors(subNet as SimNetwork, existingConnectors);
        }


        private void UpdateConnector(SimNetworkConnector connector, Dictionary<ulong, BaseSimnetworkGeometryConnector> existingConnectors)
        {
            var connectorChain = FindConnectorChain(connector);

            var startNode = connectorChain.FirstOrDefault()?.Source; //Start node is a port where the chain starts, and its parent is a block
            var endNode = connectorChain.LastOrDefault()?.Target; //End node is where the chain ends and the parent is a block


            if (startNode.ParentNetworkElement is SimNetworkBlock bl10 && bl10.IsStatic
                && endNode.ParentNetworkElement is SimNetworkBlock bl11 && bl11.IsStatic)
            {
                AddStaticConnector(connectorChain, existingConnectors);
            }
            else
            {
                AddDynamicConnector(connectorChain, existingConnectors);
            }
        }

        private List<SimNetworkConnector> FindConnectorChain(SimNetworkConnector connector)
        {
            var chain = new List<SimNetworkConnector>();
            chain.InsertRange(0, FindSourceConnectors(connector, chain));
            chain.AddRange(FindTargetConnectors(connector, chain));

            return chain;
        }


        private List<SimNetworkConnector> FindSourceConnectors(SimNetworkConnector connector, List<SimNetworkConnector> chain)
        {
            var sourceConnector = connector.Source.Connectors.FirstOrDefault(con => !chain.Contains(con));

            if (sourceConnector != null)
            {
                chain.Insert(0, sourceConnector);
                var sourceConnectors = FindSourceConnectors(sourceConnector, chain);
                chain.InsertRange(0, sourceConnectors);
            }
            return chain;
        }

        private List<SimNetworkConnector> FindTargetConnectors(SimNetworkConnector connector, List<SimNetworkConnector> chain)
        {
            var targetConnector = connector.Target.Connectors.FirstOrDefault(con => !chain.Contains(con));

            if (targetConnector != null)
            {
                chain.Add(targetConnector);
                var targetConnectors = FindTargetConnectors(targetConnector, chain);
                chain.InsertRange(0, targetConnectors);
            }
            return chain;
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



        private void AddStaticBlockFromChain(List<(SimNetworkBlock block, SimMatrix3D transformation)> existingBlocks, List<SimNetworkBlock> staticGroup, SimNetworkBlock newBlockToAdd, Dictionary<ulong, BaseSimnetworkGeometryConnector> existingConnectors)
        {
            if (connectors.TryGetValue(newBlockToAdd.RepresentationReference.GeometryId, out var existingConnectr))
                return;

            var transformation = new SimMatrix3D();
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

            var firstRot = SimQuaternion.Identity;
            var firstRotSet = false;
            if (!existingBlocks.Select(t => t.block).Contains(newBlockToAdd) && newBlockToAdd.IsStatic && newBlockToAdd.RepresentationReference == GeometricReference.Empty)
            {
                var nonCompliantConnections = new List<Tuple<SimNetworkConnector, ValidationError>>();
                if (portsToConnectWith.Count() > 0)
                {
                    var relPortPositionsToComplyWith = new List<(SimNetworkPort otherPort, SimPoint3D position, SimNetworkPort selfPort)>();
                    foreach (var port in portsToConnectWith)
                    {
                        if (existingBlocks.TryFirstOrDefault(e => e.block == port.otherPort.ParentNetworkElement as SimNetworkBlock, out var existingBlock))
                        {
                            var position = existingBlock.transformation.Transform(GetPortRelativePosition(port.otherPort));
                            relPortPositionsToComplyWith.Add((port.otherPort, position, port.selfPort));
                        }
                    }

                    var firstPort = relPortPositionsToComplyWith.ElementAt(0);
                    SimNetworkPort connectedToFirst = firstPort.selfPort;
                    var connectedToFirstRelPosition = GetPortRelativePosition(firstPort.selfPort);
                    //1. Check if a transformations exists to connect the ports with the new block

                    for (int i = 0; i < relPortPositionsToComplyWith.Count; i++)
                    {
                        if (relPortPositionsToComplyWith.ElementAt(i).otherPort != firstPort.otherPort)
                        {
                            SimNetworkPort connectedPortInNewBlock = relPortPositionsToComplyWith.ElementAt(i).selfPort;

                            if (connectedPortInNewBlock != null && newBlockToAdd.Ports.Contains(connectedPortInNewBlock))
                            {
                                var relPositionOfPort = GetPortRelativePosition(connectedPortInNewBlock);
                                var complyDistance = Distance.Euclidean(
                                    new double[] { relPortPositionsToComplyWith.ElementAt(i).position.X, relPortPositionsToComplyWith.ElementAt(i).position.Y, relPortPositionsToComplyWith.ElementAt(i).position.Z },

                                    new double[] { firstPort.position.X, firstPort.position.Y, firstPort.position.Z });
                                var initialPositionOfConnectedPort = GetPortGlobalPosition(connectedPortInNewBlock);
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
                    var firsPortGLobalPosition = GetPortGlobalPosition(firstPort.otherPort);
                    var connectedGlobalPosition = GetPortGlobalPosition(connectedToFirst);

                    transformation.Translate(((SimVector3D)firsPortGLobalPosition) - ((SimVector3D)connectedGlobalPosition));



                    //Check if rotation exists to connect the ports (the first one is connected by a simple translation transformation -->
                    //Hence we apply that transformation to the block and all of its´ ports)
                    if (portsToConnectWith.Count() > 1)
                    {
                        if (connectors.TryGetValue(firstPort.otherPort.RepresentationReference.GeometryId, out var rotCenterConnector))
                        {
                            var rotationCenter = ((Vertex)rotCenterConnector.Geometry).Position;

                            List<SimQuaternion> SimQuaternions = new List<SimQuaternion>();
                            for (int i = 0; i < relPortPositionsToComplyWith.Count; i++)
                            {
                                if (i == 0)
                                    continue;

                                var elementAt = relPortPositionsToComplyWith.ElementAt(i);
                                if (connectors.TryGetValue(elementAt.otherPort.RepresentationReference.GeometryId, out var connector))
                                {
                                    var toComplyPortVertex = connector.Geometry as Vertex;
                                    var equivalentInConneciton = transformation.Transform(GetPortGlobalPosition(elementAt.selfPort));
                                    var targetVector = new SimVector3D(toComplyPortVertex.Position.X - rotationCenter.X, toComplyPortVertex.Position.Y - rotationCenter.Y, toComplyPortVertex.Position.Z - rotationCenter.Z);
                                    var vectorToRotate = new SimVector3D(equivalentInConneciton.X - rotationCenter.X, equivalentInConneciton.Y - rotationCenter.Y, equivalentInConneciton.Z - rotationCenter.Z);

                                    targetVector.Normalize();
                                    vectorToRotate.Normalize();

                                    SimQuaternion q = SimQuaternion.Identity;
                                    var a = SimVector3D.CrossProduct((SimVector3D)vectorToRotate, targetVector);
                                    q.X = a.X;
                                    q.Y = a.Y;
                                    q.Z = a.Z;
                                    q.W = Math.Sqrt((Math.Pow(targetVector.Length, 2)) * (Math.Pow(vectorToRotate.Length, (double)2))) + SimVector3D.DotProduct(targetVector, (SimVector3D)vectorToRotate);
                                    SimQuaternions.Add(q);

                                    if (q != SimQuaternions[i - 1])
                                    {
                                        nonCompliantConnections.Add(new Tuple<SimNetworkConnector, ValidationError>(newBlockToAdd.Ports.SelectMany(t => t.Connectors).FirstOrDefault(t => t.Target == elementAt.otherPort || t.Source == elementAt.otherPort), ValidationError.RotationError));
                                    }
                                }
                            }

                            if (SimQuaternions.All(t => t == SimQuaternions[0]))
                            {
                                var quat = SimQuaternions[0];
                                quat.Normalize();
                                transformation.RotateAt(quat, rotationCenter);
                                if (!firstRotSet)
                                    firstRot = quat;
                            }
                        }
                    }
                }
            }

            //Add the block itself
            AddStaticBlock(newBlockToAdd, existingConnectors, transformation, firstRot);
            foreach (var port in newBlockToAdd.Ports)
            {
                if (port.Connectors.Count == 0)
                {
                    AddStaticPort(port, existingConnectors, true);
                }
            }
            existingBlocks.Add((newBlockToAdd, transformation));



            foreach (var item in connectedPortsWithBlockParent)
            {
                if (staticGroup.Contains(item.ParentNetworkElement) && !existingBlocks.Any(t => t.block == item.ParentNetworkElement as SimNetworkBlock))
                {
                    AddStaticBlockFromChain(existingBlocks, staticGroup, item.ParentNetworkElement as SimNetworkBlock, existingConnectors);
                }
            }
        }


        private void AddStaticBlock(SimNetworkBlock block, Dictionary<ulong, BaseSimnetworkGeometryConnector> existingConnectors, SimMatrix3D transformation, SimQuaternion rotation)
        {
            Vertex geometry = null;

            //Check if geometry for edge exists
            if (block.RepresentationReference != GeometricReference.Empty)
                geometry = GeometryModel.Geometry.GeometryFromId(block.RepresentationReference.GeometryId) as Vertex;

            if (geometry == null)
            {
                SimPoint3D position = transformation.Transform(TranslateCanvas2DPositionTo3D(block.Position));
                geometry = new Vertex(this.GeometryModel.Geometry.Layers.First(), block.Name,
                    new SimPoint3D(position.X, position.Y, position.Z))
                {
                    Color = new DerivedColor(block.Color),
                };
            }

            if (existingConnectors != null && existingConnectors.TryGetValue(geometry.Id, out var con))
            {
                geometry.Position = transformation.Transform(TranslateCanvas2DPositionTo3D(block.Position));

                con.ChangeBaseGeometry(geometry);
                connectors.Add(geometry.Id, con);
                existingConnectors.Remove(geometry.Id);
            }
            else
            {
                var conector = new SimNetworkBlockConnector(geometry, block, this, rotation);
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
                var portPosition = GetPortGlobalPosition(port);

                geometry = new Vertex(this.GeometryModel.Geometry.Layers.First(), port.Name, portPosition)
                {
                    Color = new DerivedColor(port.Color)
                };
            }
            if (geometry != null && connectors.TryGetValue(port.RepresentationReference.GeometryId, out var existingCon))
            {
                var portRelPosition = GetPortGlobalPosition(port);
                ((Vertex)existingCon.Geometry).Position = portRelPosition;
            }
            else if (geometry != null && existingConnectors != null && existingConnectors.TryGetValue(geometry.Id, out var con)
                && existingConnectors.TryGetValue(port.ParentNetworkElement.RepresentationReference.GeometryId, out var prntConn))
            {
                var position = ((Vertex)prntConn.Geometry).Position;

                geometry.Position = position;
                con.ChangeBaseGeometry(geometry);
                connectors.Add(geometry.Id, con);
                existingConnectors.Remove(geometry.Id);
            }

            else
            {
                if (connectors.TryGetValue(port.ParentNetworkElement.RepresentationReference.GeometryId, out var parentConn))
                {
                    var portConnector = new SimNetworkPortConnector(geometry, port, this);
                    connectors.Add(geometry.Id, portConnector);
                }
            }



            if (addPortProxy)
            {
                AddBlockPortConnectorProxy(port.ParentNetworkElement, port, existingConnectors);
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
                SimPoint3D position = new SimPoint3D(0, 0, 0);
                if (connectors.TryGetValue(startNode.RepresentationReference.GeometryId, out var start) && connectors.TryGetValue(endNode.RepresentationReference.GeometryId, out var end))
                {
                    var startVertex = start.Geometry as Vertex;
                    var endVertex = end.Geometry as Vertex;
                    position = startVertex.Position;
                }
                else
                {
                    position = GetPortGlobalPosition(startNode);
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
                    var startPosition = GetPortGlobalPosition(startNode);
                    var endPosition = GetPortGlobalPosition(endNode);

                    startVertex = new Vertex(this.GeometryModel.Geometry.Layers.First(), startNode.Name, startPosition);
                    endVertex = new Vertex(this.GeometryModel.Geometry.Layers.First(), endNode.Name, endPosition);
                }


                var innerEdge = new Edge(this.GeometryModel.Geometry.Layers.First(), connectorChain[0].Name + "_EDGE1",
                    new Vertex[] { startVertex, endVertex })
                {
                    Color = new DerivedColor(SimColors.Red)
                };

                geometry = new Polyline(this.GeometryModel.Geometry.Layers.First(), "CHAIN",
                new Edge[] { innerEdge })
                {
                    Color = new DerivedColor(SimColors.Red)
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
                    SimPoint3D startPortPosittion = new SimPoint3D(0, 0, 0);
                    SimPoint3D endPortPosition = new SimPoint3D(0, 0, 0);
                    startPortPosittion = GetPortGlobalPosition(startNode);
                    endPortPosition = GetPortGlobalPosition(endNode);

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
                var startPortPosittion = GetPortGlobalPosition(startNode);
                var endPortPosition = GetPortGlobalPosition(endNode);
                if (startPortPosittion == endPortPosition)
                {
                    AddStaticConnectorAsVertex(connectorChain, existingConnectors, true);

                }
                else
                {
                    AddStaticPort(startNode, existingConnectors, true);
                    AddStaticPort(endNode, existingConnectors, true);
                    AddStaticConnectorAsPoly(connectorChain, existingConnectors);
                }
            }
        }


        private void AddDynamicConnector(List<SimNetworkConnector> connectorChain, Dictionary<ulong, BaseSimnetworkGeometryConnector> existingConnectors)
        {
            Vertex geometry = null;
            var startNode = connectorChain.FirstOrDefault().Source; //Start node is a port where the chain starts, and its parent is a block
            var endNode = connectorChain.LastOrDefault().Target; //End node is where the chain ends and the parent is a block
            if (connectors.TryGetValue(startNode.ParentNetworkElement.RepresentationReference.GeometryId, out var startParent)
                && connectors.TryGetValue(endNode.ParentNetworkElement.RepresentationReference.GeometryId, out var endParent))
            {
                //Check if geometry for edge exists
                if (connectorChain[0].RepresentationReference != GeometricReference.Empty)
                    geometry = GeometryModel.Geometry.GeometryFromId(connectorChain[0].RepresentationReference.GeometryId) as Vertex;

                if (geometry == null)
                {
                    SimPoint3D position = new SimPoint3D(0, 0, 0);
                    if (startNode.ParentNetworkElement is SimNetworkBlock b && b.IsStatic)
                    {
                        position = GetPortGlobalPosition(startNode);
                    }
                    else if (endNode.ParentNetworkElement is SimNetworkBlock b1 && b1.IsStatic)
                    {
                        position = GetPortGlobalPosition(endNode);
                    }
                    else
                    {
                        position = GetPortGlobalPosition(startNode);
                        position = new SimPoint3D(position.X, position.Y, position.Z);
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
                    //Remove old Port connectors 
                    if (connectors.TryGetValue(startNode.RepresentationReference.GeometryId, out var tCon))
                    {
                        RemovePort(startNode);
                    }
                    if (connectors.TryGetValue(endNode.RepresentationReference.GeometryId, out var sCon))
                    {
                        RemovePort(endNode);
                    }
                    var conConnector = new SimNetworkConnectorConnector(geometry, connectorChain, this);
                    connectors.Add(geometry.Id, conConnector);
                }

                AddBlockToConnectorproxy(startNode.Connectors.FirstOrDefault(), startNode, existingConnectors);
                AddBlockToConnectorproxy(endNode.Connectors.FirstOrDefault(), endNode, existingConnectors);
            }

        }



        private SimPoint3D TranslateCanvas2DPositionTo3D(SimPoint point)
        {
            double canvasX = point.X;
            double canvasY = point.Y;

            double x = (canvasX) / ReduceRatio;
            double z = (canvasY) / ReduceRatio;
            double y = 0;


            return new SimPoint3D(x, y, z);
        }

        private SimPoint3D GetPortGlobalPosition(SimNetworkPort port)
        {

            var relPosition = GetPortRelativePosition(port);
            var rotation = SimQuaternion.Identity;


            if (connectors.TryGetValue(port.ParentNetworkElement.RepresentationReference.GeometryId, out var parentConnector))
            {
                rotation = ((Vertex)parentConnector.Geometry).ProxyGeometries.FirstOrDefault().Rotation;
                var matrixR = new SimMatrix3D();
                matrixR.Rotate(rotation);
                relPosition = matrixR.Transform(relPosition);

                return new SimPoint3D(
                     ((Vertex)parentConnector.Geometry).Position.X + relPosition.X,
                     ((Vertex)parentConnector.Geometry).Position.Y + relPosition.Y,
                     ((Vertex)parentConnector.Geometry).Position.Z + relPosition.Z);

            }
            else
            {
                var position = TranslateCanvas2DPositionTo3D(port.ParentNetworkElement.Position);
                return new SimPoint3D(
                   position.X + relPosition.X,
                   position.Y + relPosition.Y,
                   position.Z + relPosition.Z);

            }
        }

        private SimPoint3D GetPortRelativePosition(SimNetworkPort port)
        {
            //Static
            if (port.ParentNetworkElement is SimNetworkBlock bl && bl.IsStatic
                && port.ComponentInstance != null
                && port.ComponentInstance.InstanceParameterValuesPersistent
                        .TryGetValue(((SimDoubleParameter)port.ComponentInstance.Component.Parameters.FirstOrDefault(p => p.HasReservedTaxonomyEntry(ReservedParameterKeys.SIMNW_STATIC_PORT_POSITION_X))), out var relX)
                     && port.ComponentInstance.InstanceParameterValuesPersistent
                        .TryGetValue(((SimDoubleParameter)port.ComponentInstance.Component.Parameters.FirstOrDefault(p => p.HasReservedTaxonomyEntry(ReservedParameterKeys.SIMNW_STATIC_PORT_POSITION_Y))), out var relY)
                     && port.ComponentInstance.InstanceParameterValuesPersistent
                        .TryGetValue(((SimDoubleParameter)port.ComponentInstance.Component.Parameters.FirstOrDefault(p => p.HasReservedTaxonomyEntry(ReservedParameterKeys.SIMNW_STATIC_PORT_POSITION_Z))), out var relZ))
            {

                return new SimPoint3D(relX, relY, relZ);

            }
            //Dynamic
            else
            {
                double positionX = 0;
                double positionY = 0;
                double positionZ = 0;


                if (connectors.TryGetValue(port.ParentNetworkElement.RepresentationReference.GeometryId, out var parentConnector))
                {
                    if (port.PortType == PortType.Input)
                    {
                        positionX = -2;
                        positionZ = +(port.ParentNetworkElement.Ports.Where(t => t.PortType == PortType.Input).ToList().IndexOf(port) * 2);
                    }
                    else
                    {
                        positionX = +2;
                        positionZ = +(port.ParentNetworkElement.Ports.Where(t => t.PortType == PortType.Output).ToList().IndexOf(port) * 2);
                    }

                    positionY = 0;
                }

                else
                {
                    if (port.PortType == PortType.Input)
                    {

                        positionX = -2;
                        positionZ = (port.ParentNetworkElement.Ports.Where(t => t.PortType == PortType.Input).ToList().IndexOf(port) * 2);
                    }
                    else
                    {
                        positionX = +2;
                        positionZ = (port.ParentNetworkElement.Ports.Where(t => t.PortType == PortType.Output).ToList().IndexOf(port) * 2);
                    }
                    positionY = 0;
                }
                return new SimPoint3D(positionX, positionY, positionZ);
            }
        }


        private void AddDynamicBlock(BaseSimNetworkElement networkElement, Dictionary<ulong, BaseSimnetworkGeometryConnector> existingConnectors)
        {
            Vertex geometry = null;
            var color = new DerivedColor(SimColor.FromArgb(10, (byte)rnd.Next(256), (byte)rnd.Next(256), (byte)rnd.Next(256)));
            if (networkElement.RepresentationReference != GeometricReference.Empty)
            {
                geometry = this.GeometryModel.Geometry.GeometryFromId(networkElement.RepresentationReference.GeometryId) as Vertex;
            }
            if (geometry == null)
            {
                if (networkElement is SimNetworkBlock block)
                {
                    color = new DerivedColor(block.Color);
                }
                if (networkElement is SimNetwork network)
                {
                    color = new DerivedColor(network.Color);
                }

                var position = TranslateCanvas2DPositionTo3D(networkElement.Position);
                geometry = new Vertex(this.GeometryModel.Geometry.Layers.First(), networkElement.Name,
                    new SimPoint3D(position.X, position.Y, position.Z))
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
                connectors.Add(geometry.Id, new SimNetworkBlockConnector(geometry, block, this, SimQuaternion.Identity));
                AttachBlockEvents(block);
            }
            else if (networkElement is SimNetwork nw)
            {
                connectors.Add(geometry.Id, new SimNetworkNetworkConnector(geometry, nw, this, SimQuaternion.Identity));
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



        private (SimPoint3D Min, SimPoint3D Max) GetSubnetworkBoundingBox(SimNetwork network)
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
            return (new SimPoint3D(Convert.ToDouble(minX), Convert.ToDouble(minY), Convert.ToDouble(minZ)),
                new SimPoint3D(Convert.ToDouble(maxX), Convert.ToDouble(maxY), Convert.ToDouble(maxZ)));
        }


        internal void OnStaticPortCoordinateChanged(SimDoubleParameter param)
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
                    this.UpdateNetworkConnectors(this.Network, null);
                }
                else
                {
                    AddPort(((SimNetworkPort)((SimInstancePlacementSimNetwork)item).NetworkElement), this.connectors);
                }
            }
            this.connectors.Where(c => c.Value is SimNetworkBlockConnector).ForEach(c => ((SimNetworkBlockConnector)c.Value).transformInProgress = false);
        }


        private void AddBlockToConnectorproxy(SimNetworkConnector connector, SimNetworkPort port, Dictionary<ulong, BaseSimnetworkGeometryConnector> existingConnectors)
        {
            Vertex startVertex;
            Vertex endVertex;

            if (connectors.TryGetValue(port.ParentNetworkElement.RepresentationReference.GeometryId, out var parentConnector))
            {
                startVertex = parentConnector.Geometry as Vertex;
            }
            else
            {
                throw new Exception("Parent connector not found");
            }
            if (connectors.TryGetValue(connector.RepresentationReference.GeometryId, out var connectorConnector))
            {
                endVertex = connectorConnector.Geometry as Vertex;
            }
            else
            {
                throw new Exception("Connector not found");
            }

            var innerEdge = new Edge(this.GeometryModel.Geometry.Layers.First(), startVertex.Name + "_EDGE2",
                new Vertex[] { startVertex, endVertex })
            { Color = new DerivedColor(connector.Color) };

            var edgeGeometry = new Polyline(this.GeometryModel.Geometry.Layers.First(), startVertex.Name + "_PROXY",
                new Edge[] { innerEdge })
            { Color = new DerivedColor(connector.Color) }; ;


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
        }


        private void AddBlockPortConnectorProxy(BaseSimNetworkElement parentElement, SimNetworkPort port, Dictionary<ulong, BaseSimnetworkGeometryConnector> existingConnectors)
        {
            Vertex startVertex;
            Vertex endVertex;

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
                new Vertex[] { startVertex, endVertex })
            {
                Color = new DerivedColor(port.Color)
            };

            var edgeGeometry = new Polyline(this.GeometryModel.Geometry.Layers.First(), startVertex.Name + "_PROXY",
                new Edge[] { innerEdge })
            {
                Color = new DerivedColor(port.Color)
            };


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
            simNetwork.ContainedElements.CollectionChanged -= this.ContainedElements_CollectionChanged;
            simNetwork.ContainedConnectors.CollectionChanged -= this.ContainedConnectors_CollectionChanged;
            simNetwork.Ports.CollectionChanged -= this.Ports_CollectionChanged;


            simNetwork.ContainedElements.CollectionChanged += this.ContainedElements_CollectionChanged;
            simNetwork.ContainedConnectors.CollectionChanged += this.ContainedConnectors_CollectionChanged;
            simNetwork.Ports.CollectionChanged += this.Ports_CollectionChanged;
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
                        UpdateConnector(con, null);
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
            if (sender is SimNetworkElementCollection collection && collection.Count == 1)
            {
                UpdateNetwork(this.Network);
            }
            else
            {
                switch (e.Action)
                {
                    case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                        foreach (var item in e.NewItems)
                        {
                            if (item is SimNetworkBlock nwElement)
                                AddDynamicBlock(nwElement, null);
                            if (item is SimNetwork subNetwork)
                                AddNetwork(subNetwork, null);
                        }
                        break;
                    case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                        foreach (var item in e.OldItems)
                        {
                            if (item is SimNetworkBlock block)
                                RemoveBlock(block);

                            if (item is SimNetwork network)
                                RemoveSimNetwork(network);
                        }
                        break;
                }
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
                    foreach (var edge in geometry.Edges.ToList())
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
                    foreach (var edge in geometry.Edges.ToList())
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


                        //Clean the unused geometry
                        foreach (var edge in geometry.Edges)
                        {
                            var poliesToRemove = this.GeometryModel.Geometry.Polylines.Where(t => t.Edges.Any(p => p.Edge == edge)).ToList();
                            for (int i = poliesToRemove.Count() - 1; i >= 0; --i)
                            {
                                poliesToRemove[i].RemoveFromModel();
                            }

                        }

                        for (int i = geometry.Edges.Count - 1; i >= 0; --i)
                        {
                            geometry.Edges[i].RemoveFromModel();
                        }

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
                AddStaticPort(bConnector.Source, null, true);
            }
            else
            {
                AddPort(bConnector.Source, null);
            }
            if (bConnector.Target.ParentNetworkElement is SimNetworkBlock block1 && block1.IsStatic)
            {
                AddStaticPort(bConnector.Target, null, true);
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

                foreach (var item in network.ContainedConnectors)
                {
                    RemoveConnector(item);
                }
                foreach (var item in network.Ports)
                {
                    RemovePort(item);
                }
                foreach (var item in network.ContainedElements)
                {
                    if (item is SimNetwork nw)
                    {
                        RemoveSimNetwork(nw);
                    }

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
            foreach (var connector in connectors.Where(t => t.Value is SimNetworkConnectorConnector || t.Value is SimNetworkBlockConnector || t.Value is SimNetworkNetworkConnector))
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





        #region Dispose

        private void DetachEvents(SimNetwork simNetwork)
        {
            simNetwork.ContainedElements.CollectionChanged -= this.ContainedElements_CollectionChanged;
            simNetwork.ContainedConnectors.CollectionChanged -= this.ContainedConnectors_CollectionChanged;
            simNetwork.Ports.CollectionChanged -= this.Ports_CollectionChanged;
            GeometryModel.Geometry.TopologyChanged -= this.Geometry_TopologyChanged;
            this.GeometryModel.Replaced -= this.GeometryModel_Replaced;

            foreach (var subnet in simNetwork.ContainedElements.Where(t => t is SimNetwork))
                DetachEvents(subnet as SimNetwork);
        }
        /// <summary>
        /// Frees all resources created by this connector and detaches all event handler
        /// </summary>
        internal void Dispose()
        {
            DetachEvents(Network);

            foreach (var con in connectors.Values)
                con.Dispose();
        }

        #endregion
    }
}

