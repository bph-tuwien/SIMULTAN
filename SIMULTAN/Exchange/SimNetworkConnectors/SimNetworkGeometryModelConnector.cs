using SIMULTAN.Data;
using SIMULTAN.Data.Components;
using SIMULTAN.Data.Geometry;
using SIMULTAN.Data.SimMath;
using SIMULTAN.Data.SimNetworks;
using SIMULTAN.Utils;
using Sprache;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Text.Json.Serialization.Metadata;
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
    public partial class SimNetworkGeometryModelConnector : IDisposable
    {
        private bool isDisposed = false;
        static double ReduceRatio = 20;
        static double distanceTolearance = 0.001;
        static double distanceTolearance2 = distanceTolearance * distanceTolearance;

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


        private Dictionary<ulong, BaseSimNetworkGeometryConnector> connectors = new();

        private HashSet<BaseGeometry> geometryChangedGeometries = new();
        private HashSet<BaseGeometry> topologyChangedGeometries = new();

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
            this.GeometryModel.Geometry.GeometryChanged += this.Geometry_GeometryChanged;
            this.GeometryModel.Replaced += this.GeometryModel_Replaced;

            //Add child connectors, make sure that all network elements are properly represented in the geometry model
            UpdateNetwork(network, !model.Geometry.Vertices.Any());
        }

        /// <summary>
        /// Synchronizes the changes of the network with the geometry model.
        /// </summary>
        public void SynchronizeChanges()
        {
            // Geometry Changed
            foreach (var geom in geometryChangedGeometries.ToList())
            {
                if (connectors.TryGetValue(geom.Id, out var con))
                {
                    con.OnGeometryChanged();
                    if (con is SimNetworkBlockPortConnectorProxy proxyCon
                        && proxyCon.ParentElement is SimNetworkBlock block && block.IsStatic)
                    {
                        // if connection is not null, check the connection vertex, otherwise check the port vertex
                        var geoRef = proxyCon.Connection != null ?
                            proxyCon.Connection.RepresentationReference :
                            proxyCon.Port.RepresentationReference;

                        if (geoRef != GeometricReference.Empty)
                        {
                            var refVertex = this.GeometryModel.Geometry.GeometryFromId(geoRef.GeometryId) as Vertex;
                            if (refVertex != null
                                && !CheckStaticPortConstraints(proxyCon.Port, refVertex))
                            {
                                proxyCon.IsValid = false;
                            }
                            else
                            {
                                proxyCon.IsValid = true;
                            }
                        }
                    }
                }
            }

            // Topology changed
            foreach (var geom in topologyChangedGeometries)
            {
                if (connectors.TryGetValue(geom.Id, out var con))
                    con.OnTopologyChanged();
            }

            // otherwise update whole network
            if (!geometryChangedGeometries.Any() && !topologyChangedGeometries.Any())
            {
                UpdateNetwork(Network, false);
            }

            geometryChangedGeometries.Clear();
            topologyChangedGeometries.Clear();
        }

        /// <summary>
        /// Gets the network element by the geometry representation
        /// </summary>
        /// <returns>The network elements or null if none found</returns>
        public IEnumerable<ISimNetworkElement> GetNetworkElements(BaseGeometry geometry)
        {
            if (connectors.TryGetValue(geometry.Id, out var connector))
            {
                return connector.SimNetworkElement;
            }
            return null;
        }

        private void Geometry_GeometryChanged(object sender, IEnumerable<BaseGeometry> geometries)
        {
            foreach (var geom in geometries)
                geometryChangedGeometries.Add(geom);

            if (Exchange.EnableGeometryEvents)
                SynchronizeChanges();
        }

        private void Geometry_TopologyChanged(object sender, IEnumerable<BaseGeometry> geometries)
        {
            foreach (var geom in geometries)
                topologyChangedGeometries.Add(geom);

            if (Exchange.EnableGeometryEvents)
                SynchronizeChanges();
        }

        private void GeometryModel_Replaced(object sender, GeometryModelReplacedEventArgs e)
        {
            if (e.OldGeometry != null)
            {
                e.OldGeometry.TopologyChanged -= this.Geometry_TopologyChanged;
                e.OldGeometry.GeometryChanged -= this.Geometry_GeometryChanged;
            }

            //Reconnect everything
            UpdateNetwork(this.Network, false);

            if (e.NewGeometry != null)
            {
                e.NewGeometry.TopologyChanged += this.Geometry_TopologyChanged;
                e.NewGeometry.GeometryChanged += this.Geometry_GeometryChanged;
            }
        }


        /// <summary>
        /// Updates the given network´s geometry
        /// </summary>
        /// <param name="network">The network we base the geometry on</param>
        /// <param name="isInitialConversion">If this is the initial conversion. Will reposition vertices to not overlap subnetworks.</param>
        public void UpdateNetwork(SimNetwork network, bool isInitialConversion)
        {
            GeometryModel.Geometry.StartBatchOperation();

            if (GeometryModel.Geometry.Layers.Count == 0)
            {
                GeometryModel.Geometry.Layers.Add(new Layer(GeometryModel.Geometry, "0"));
            }
            Dictionary<ulong, BaseSimNetworkGeometryConnector> existingConnectors = new(this.connectors);
            connectors.Clear(); // don't dispose connectors here, cause they are still needed for the existingConnectors

            AddNetwork(network, existingConnectors, isInitialConversion);

            GeometryModel.Geometry.EndBatchOperation();
            CleanUnusedGeometry();

            foreach (var con in existingConnectors.Values)
                con.Dispose();
        }


        /// <summary>
        /// Adds a Network
        /// </summary>
        /// <param name="subnetwork">The network or subnetwork</param>
        /// <param name="existingConnectors">The existing connectors</param>
        /// <param name="isInitialConversion">If this is the initial conversion run</param>
        private void AddNetwork(SimNetwork subnetwork, Dictionary<ulong, BaseSimNetworkGeometryConnector> existingConnectors,
            bool isInitialConversion)
        {
            PrintTrace($"AddNetwork: {subnetwork.Name}({subnetwork.Id.LocalId})");
            if (subnetwork.ContainedElements.Count == 0) //If there is no contained element, Add the subnetwork as a Vertex
            {
                AddDynamicBlock(subnetwork, existingConnectors);
            }
            else
            {
                AddNestedElements(subnetwork, existingConnectors, isInitialConversion);
            }
            AttachNetworkEvents(subnetwork);
        }

        /// <summary>
        /// Adds all the networks elements contained in a network
        /// </summary>
        /// <param name="network">The network</param>
        /// <param name="existingGeoConnectors">The existing connectors</param>
        /// <param name="isInitialConversion">If this is the initial conversion run</param>
        private void AddNestedElements(SimNetwork network, Dictionary<ulong, BaseSimNetworkGeometryConnector> existingGeoConnectors,
            bool isInitialConversion)
        {
            PrintTrace($"AddNestedElements: {network.Name}({network.Id.LocalId})");
            //Make sure that all nodes of the flattened network exist in the geometry model
            var staticGroups = FindStaticGroups();
            UpdateStaticBlocks(staticGroups, existingGeoConnectors);
            UpdateDynamicBlocks(network, existingGeoConnectors);
            UpdateUnconnectedPorts(network, existingGeoConnectors);
            UpdateSubnetworks(network, existingGeoConnectors, isInitialConversion);
            UpdateNetworkConnections(network, existingGeoConnectors);
        }


        /// <summary>
        /// Updates all ports without connections
        /// </summary>
        /// <param name="network">The network</param>
        /// <param name="existingConnectors">Existing connectors</param>
        private void UpdateUnconnectedPorts(SimNetwork network, Dictionary<ulong, BaseSimNetworkGeometryConnector> existingConnectors)
        {
            foreach (var parentElement in network.ContainedElements)
                foreach (var port in parentElement.Ports.Where(p => p.Connections.Count == 0 && ((p.ParentNetworkElement is SimNetworkBlock bl && !bl.IsStatic) || p.ParentNetworkElement is SimNetwork)))
                    AddUnconnectedPort(port, existingConnectors);
        }



        /// <summary>
        /// Adds a port vertex and polyline to the network element of a port without connections
        /// </summary>
        /// <param name="port">The unconnected port</param>
        /// <param name="existingConnectors">The existing connecitons</param>
        private void AddUnconnectedPort(SimNetworkPort port, Dictionary<ulong, BaseSimNetworkGeometryConnector> existingConnectors)
        {
            if (port.Connections.Count != 0)
                throw new ArgumentException("Port has connections, cannot add unconnected port");
            PrintTrace($"AddUnconnectedPort: {port.Name}({port.Id.LocalId}");

            Vertex vertex = null;
            // check if block has a connector
            if (!connectors.TryGetValue(port.ParentNetworkElement.RepresentationReference.GeometryId, out var parentElement))
            {
                return;
            }
            // try get vertex of port
            if (port.RepresentationReference != GeometricReference.Empty)
            {
                vertex = this.GeometryModel.Geometry.GeometryFromId(port.RepresentationReference.GeometryId) as Vertex;
            }
            // create vertex if not exists
            if (vertex == null)
            {
                var color = port.Color;
                var position = GetPortGlobalPosition(port);
                vertex = new Vertex(this.GeometryModel.Geometry.Layers.First(), port.Name, position)
                {
                    Color = new DerivedColor(color)
                };
            }

            if (connectors.TryGetValue(vertex.Id, out var con))
            {
                con.ChangeBaseGeometry(vertex);
            }
            // check if the vertex already has a connector
            else if (existingConnectors != null && existingConnectors.TryGetValue(vertex.Id, out var econ))
            {
                econ.ChangeBaseGeometry(vertex);
                connectors.Add(vertex.Id, econ);
                existingConnectors.Remove(vertex.Id);
            }
            else
            {
                var portConnector = new SimNetworkPortConnector(vertex, port, this);
                connectors.Add(vertex.Id, portConnector);
            }

            AddBlockToPortProxy(port.ParentNetworkElement, port, existingConnectors);
        }



        /// <summary>
        /// Updates the static connectors, needed for undo redo item.
        /// </summary>
        public void UpdateStaticConnectors(List<Vertex> geoms)
        {
            for (int i = 0; i < geoms.Count; i++)
            {
                if (this.connectors.TryGetValue(geoms[i].Id, out var connector)
                    && connector is SimNetworkBlockConnector blockCon
                    && blockCon.Block.IsStatic)
                {
                    foreach (var port in blockCon.Block.Ports)
                    {
                        if (!port.Connections.Any())
                        {
                            AddStaticUnconnectedPort(port, null, false);
                        }
                    }
                }
            }
            this.UpdateNetworkConnections(this.Network, null);
        }


        /// <summary>
        /// Updates all dynamic blocks
        /// </summary>
        /// <param name="network">The network</param>
        /// <param name="existingConnectors">The connectors</param>
        private void UpdateDynamicBlocks(SimNetwork network, Dictionary<ulong, BaseSimNetworkGeometryConnector> existingConnectors)
        {
            foreach (var node in network.ContainedElements.OfType<SimNetworkBlock>().Where(t => !t.IsStatic))
                AddDynamicBlock(node, existingConnectors);
        }

        /// <summary>
        /// Updates all subnetworks
        /// </summary>
        /// <param name="network">The parent network</param>
        /// <param name="existingConnectors">The existing connectors</param>
        /// <param name="isInitialConversion">If this is the initial conversion run</param>
        private void UpdateSubnetworks(SimNetwork network, Dictionary<ulong, BaseSimNetworkGeometryConnector> existingConnectors, bool isInitialConversion)
        {
            foreach (var subnet in network.ContainedElements.OfType<SimNetwork>())
            {
                AddNetwork(subnet, existingConnectors, isInitialConversion);
                if (isInitialConversion)
                {
                    FixSubnetOverlap(subnet);
                }
            }
        }

        /// <summary>
        /// Updates all groups of static blocks
        /// </summary>
        /// <param name="staticGroups">The static groups</param>
        /// <param name="existingConnectors">The existing connectors</param>
        private void UpdateStaticBlocks(List<HashSet<SimNetworkBlock>> staticGroups, Dictionary<ulong, BaseSimNetworkGeometryConnector> existingConnectors)
        {
            foreach (var group in staticGroups)
            {
                AddStaticBlockFromChain(new(), group, group.FirstOrDefault(), existingConnectors);
            }
        }

        private void FixSubnetOverlap(SimNetwork subnetwork)
        {
            var boundingBox = GetSubnetworkBoundingBox(subnetwork);
            // check all elements of the parent network if they intersect with the subnetwork bounds
            foreach (var element in subnetwork.ParentNetwork.ContainedElements)
            {
                // if dynamic block or network
                if (!(element is SimNetworkBlock block && block.IsStatic))
                {
                    var transVector = new SimVector3D(0, 0, 0);
                    if (connectors.TryGetValue(element.RepresentationReference.GeometryId, out var connector)
                        && connector.Geometry is Vertex vertex)
                    {
                        // 2D X pos is further left than the subnet but the 3D bounds are intersecting in X
                        if (element.Position.X < subnetwork.Position.X
                            && vertex.Position.X > boundingBox.Min.X)
                        {
                            var transofrmBy = (boundingBox.Min.X - vertex.Position.X);
                            transVector.X = transVector.X - transofrmBy;
                        }
                        // 2D X pos is further right than the subnet but the 3D bounds are intersecting in X
                        if (element.Position.X > subnetwork.Position.X
                            && vertex.Position.X < boundingBox.Max.X)
                        {
                            var transofrmBy = (boundingBox.Max.X - vertex.Position.X);
                            transVector.X = transVector.X - transofrmBy;
                        }

                        // Same for Y coordinate
                        if (element.Position.Y < subnetwork.Position.Y
                            && vertex.Position.Y > boundingBox.Min.Y)
                        {
                            var transofrmBy = (boundingBox.Min.Y - vertex.Position.Y);
                            transVector.Y = transVector.Y - transofrmBy;
                        }
                        if (element.Position.Y > subnetwork.Position.Y
                            && vertex.Position.Y < boundingBox.Max.Y)
                        {
                            var transofrmBy = (boundingBox.Max.Y - vertex.Position.Y);
                            transVector.Y = transVector.Y - transofrmBy;
                        }

                        vertex.Position = vertex.Position - transVector;
                        connector.ChangeBaseGeometry(vertex);
                    }

                    foreach (var port in element.Ports)
                    {
                        if (connectors.TryGetValue(port.RepresentationReference.GeometryId, out var portConnector)
                            && portConnector.Geometry is Vertex portVertex)
                        {
                            portVertex.Position = portVertex.Position - transVector;
                            portConnector.ChangeBaseGeometry(portVertex);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Updates all connections of the network recursively
        /// </summary>
        /// <param name="network">The network</param>
        /// <param name="existingGeoConnectors">The existing connectors</param>
        private void UpdateNetworkConnections(SimNetwork network, Dictionary<ulong, BaseSimNetworkGeometryConnector> existingGeoConnectors)
        {
            foreach (var connection in network.ContainedConnections)
            {
                UpdateConnection(connection, existingGeoConnectors);
            }
            // update subnet connections
            foreach (var subNet in network.ContainedElements.OfType<SimNetwork>())
            {
                UpdateNetworkConnections(subNet, existingGeoConnectors);
            }
        }

        /// <summary>
        /// Update a connection
        /// </summary>
        /// <param name="connection">the connection</param>
        /// <param name="existingGeoConnectors">The existing connectors</param>
        private void UpdateConnection(SimNetworkConnection connection, Dictionary<ulong, BaseSimNetworkGeometryConnector> existingGeoConnectors)
        {
            var connectionChain = GetConnectionChain(connection);
            var (startPort, endPort) = GetChainStartAndEndPorts(connectionChain);

            if (startPort.ParentNetworkElement is SimNetworkBlock startBlock && startBlock.IsStatic
                && endPort.ParentNetworkElement is SimNetworkBlock endBlock && endBlock.IsStatic)
            {
                AddStaticConnection(connectionChain, existingGeoConnectors);
            }
            else
            {
                AddDynamicConnection(connectionChain, existingGeoConnectors);
            }
        }

        /// <summary>
        /// Recursively find all connections that are connected trough subnetworks and terminate at blocks
        /// This method returns a list of connections when one of the ports is the input or output of a subnetwork.
        /// </summary>
        /// <param name="connection">The connection to start with</param>
        /// <returns>An ordered list of connections</returns>
        public static List<SimNetworkConnection> GetConnectionChain(SimNetworkConnection connection)
        {
            var chain = new List<SimNetworkConnection>();
            FindSourceConnections(connection, chain);
            FindTargetConnections(connection, chain);
            return chain;
        }

        /// <summary>
        /// Returns the start and end ports of a connection chain (<see cref="GetConnectionChain(SimNetworkConnection)"/>)
        /// </summary>
        /// <param name="chain">The chain</param>
        /// <returns>The start end ports</returns>
        public static (SimNetworkPort start, SimNetworkPort end) GetChainStartAndEndPorts(List<SimNetworkConnection> chain)
        {
            var startPort = chain[0].Source;
            var endPort = chain[chain.Count - 1].Target;
            return (startPort, endPort);
        }

        /// <summary>
        /// Recursively finds the source connections
        /// </summary>
        /// <param name="connection">The connection to start with</param>
        /// <param name="chain">The current chain</param>
        private static void FindSourceConnections(SimNetworkConnection connection, List<SimNetworkConnection> chain)
        {
            var sourceConnection = connection.Source.Connections.Find(con => !chain.Contains(con));
            if (sourceConnection != null)
            {
                chain.Insert(0, sourceConnection);
                FindSourceConnections(sourceConnection, chain);
            }
        }

        /// <summary>
        /// Recursively finds the target connections
        /// </summary>
        /// <param name="connection">The connection to start with</param>
        /// <param name="chain">The current chain</param>
        private static void FindTargetConnections(SimNetworkConnection connection, List<SimNetworkConnection> chain)
        {
            var targetConnection = connection.Target.Connections.Find(con => !chain.Contains(con));
            if (targetConnection != null)
            {
                chain.Add(targetConnection);
                FindTargetConnections(targetConnection, chain);
            }
        }

        /// <summary>
        /// Finds all static connected groups of <see cref="SimNetworkBlock"/>s
        /// </summary>
        /// <param name="network">The network to find the groups for</param>
        /// <param name="groups">The current groups</param>
        /// <returns>A list of connected block groups</returns>
        private List<HashSet<SimNetworkBlock>> FindStaticGroups(SimNetwork network, List<HashSet<SimNetworkBlock>> groups)
        {

            foreach (var block in network.ContainedElements.OfType<SimNetworkBlock>().Where(t => t.IsStatic))
            {
                // if block is not in any group already, find its connected blocks and add the group
                if (!groups.Exists(t => t.Contains(block)))
                {
                    groups.Add(GetStaticConnectedBlocks(new() { block }, block));
                }
            }
            // recurse into subnetworks
            foreach (var subnet in network.ContainedElements.OfType<SimNetwork>())
            {
                FindStaticGroups(subnet, groups);
            }
            return groups;
        }

        /// <summary>
        /// Recursively finds all static connected blocks and adds them to the group
        /// </summary>
        /// <param name="group">The group to add to</param>
        /// <param name="block">The block to process</param>
        /// <returns>A set of all connected static blocks</returns>
        private HashSet<SimNetworkBlock> GetStaticConnectedBlocks(HashSet<SimNetworkBlock> group, SimNetworkBlock block)
        {
            // check all connections of the block
            foreach (var connection in block.Ports.SelectMany(t => t.Connections))
            {
                // port connected to this block
                var sourcePort = connection.Source.ParentNetworkElement == block ?
                    connection.Source : connection.Target;
                // find port that this connection connects to
                var targetPort = connection.Source.ParentNetworkElement == block ?
                    connection.Target : connection.Source;
                // if the connected block is static and not already in the group, add it to the group
                if (targetPort.ParentNetworkElement is SimNetworkBlock bl && bl.IsStatic && !group.Any(t => t.Id == bl.Id))
                {
                    group.Add(bl);
                    // recursively add all connected blocks
                    GetStaticConnectedBlocks(group, bl);
                }
                // if connected to a subnetwork and subnet has connections inside, recurse into subnet
                if (targetPort.ParentNetworkElement is SimNetwork && targetPort.Connections.Count == 2)
                {
                    GetStaticConnectionFromConnectedSubnetwork(group, sourcePort, targetPort);
                }
            }
            return group;
        }

        /// <summary>
        /// Recurses through a connected subnetwork (target port) to get the connected static blocks and adds them to the group
        /// </summary>
        /// <param name="group">The group</param>
        /// <param name="sourcePort">The source port of the connection</param>
        /// <param name="targetPort">The target port of the connection, part of the subnetwork</param>
        private void GetStaticConnectionFromConnectedSubnetwork(HashSet<SimNetworkBlock> group, SimNetworkPort sourcePort, SimNetworkPort targetPort)
        {
            if (targetPort.ParentNetworkElement is SimNetwork)
            {
                // recursively add all connected blocks of the target port
                foreach (var con in targetPort.Connections)
                {
                    if (con.Target.ParentNetworkElement is SimNetworkBlock targetBlock
                        && targetBlock.IsStatic && !group.Contains(targetBlock))
                    {
                        group.Add(targetBlock);
                        GetStaticConnectedBlocks(group, targetBlock);
                    }
                    if (con.Source.ParentNetworkElement is SimNetworkBlock sourceBlock
                        && sourceBlock.IsStatic && !group.Contains(sourceBlock))
                    {
                        group.Add(sourceBlock);
                        GetStaticConnectedBlocks(group, sourceBlock);
                    }

                    if (con.Source.ParentNetworkElement is SimNetwork
                        && con.Source != targetPort && con.Source != sourcePort)
                    {
                        GetStaticConnectionFromConnectedSubnetwork(group, con.Target, con.Source);
                    }
                    if (con.Target.ParentNetworkElement is SimNetwork
                        && con.Target != targetPort && con.Source != targetPort)
                    {
                        GetStaticConnectionFromConnectedSubnetwork(group, con.Source, con.Target);
                    }
                }
            }
        }

        /// <summary>
        /// Finds the first port of a Block (not network) that is connected to the startPort.
        /// Recursively searches through subnetworks till a block is found.
        /// </summary>
        /// <param name="startPort">The port we want to check whether a port is connected which parent is  a SimNetworkBlock</param>
        /// <param name="visitedPorts">The already visited ports</param>
        /// <returns>Returns null if the start port is not connected to any ports with a SimNetworkBlock parent</returns>
        private SimNetworkPort FindConnectedPortOfBlock(SimNetworkPort startPort, HashSet<SimNetworkPort> visitedPorts)
        {
            foreach (var con in startPort.Connections)
            {
                if (con.Target != startPort && !visitedPorts.Contains(con.Target))
                {
                    if (con.Target.ParentNetworkElement is SimNetworkBlock)
                    {
                        return con.Target;
                    }
                    if (con.Target.ParentNetworkElement is SimNetwork)
                    {
                        visitedPorts.Add(startPort);
                        return FindConnectedPortOfBlock(con.Target, visitedPorts);
                    }
                }
                if (con.Source != startPort && !visitedPorts.Contains(con.Source))
                {
                    if (con.Source.ParentNetworkElement is SimNetworkBlock)
                    {
                        return con.Source;
                    }
                    if (con.Source.ParentNetworkElement is SimNetwork)
                    {
                        visitedPorts.Add(startPort);
                        return FindConnectedPortOfBlock(con.Source, visitedPorts);
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Recursively adds all connected static block connectors
        /// </summary>
        /// <param name="handledBlocks">Already handled blocks and their transforms</param>
        /// <param name="staticGroup">The group to work on</param>
        /// <param name="newBlockToAdd">The new block to add a connector for</param>
        /// <param name="existingConnectors">The existing connectors</param>
        private void AddStaticBlockFromChain(Dictionary<SimNetworkBlock, SimMatrix3D> handledBlocks, HashSet<SimNetworkBlock> staticGroup, SimNetworkBlock newBlockToAdd, Dictionary<ulong, BaseSimNetworkGeometryConnector> existingConnectors)
        {
            if (connectors.ContainsKey(newBlockToAdd.RepresentationReference.GeometryId))
                return;

            PrintTrace($"AddStaticBlockFromChain: {newBlockToAdd.Name}({newBlockToAdd.Id.LocalId}");

            var transformation = new SimMatrix3D();
            // all ports of blocks that are connected (also through subnets) to the new block and are part of the static group
            List<SimNetworkPort> connectedBlockPorts = new List<SimNetworkPort>();
            var portsToConnect = new List<(SimNetworkPort otherPort, SimNetworkPort selfPort, SimNetworkBlock block, SimMatrix3D transformation)>();

            // find all connected ports of blocks (through subnetworks)
            foreach (var port in newBlockToAdd.Ports)
            {
                var connectedPort = FindConnectedPortOfBlock(port, new());
                if (connectedPort != null && connectedPort.ParentNetworkElement is SimNetworkBlock block
                    && staticGroup.Contains(connectedPort.ParentNetworkElement))
                {
                    connectedBlockPorts.Add(connectedPort);
                    // if block was already handled, connect the ports
                    if (handledBlocks.TryGetValue(block, out var transform))
                    {
                        portsToConnect.Add((connectedPort, port, block, transform));
                    }
                }
            }

            var firstRot = SimQuaternion.Identity;
            var firstRotSet = false;
            if (!handledBlocks.ContainsKey(newBlockToAdd) && newBlockToAdd.IsStatic
                && newBlockToAdd.RepresentationReference == GeometricReference.Empty)
            {
                var nonCompliantConnections = new List<(SimNetworkConnection, ValidationError)>();
                if (portsToConnect.Count > 0)
                {
                    var relPortPositionsToComplyWith = new List<(SimNetworkPort otherPort, SimPoint3D position, SimNetworkPort selfPort)>();
                    foreach (var toConnect in portsToConnect)
                    {
                        var position = toConnect.transformation.Transform(GetPortRelativePosition(toConnect.otherPort));
                        relPortPositionsToComplyWith.Add((toConnect.otherPort, position, toConnect.selfPort));
                    }

                    var firstPort = relPortPositionsToComplyWith[0];
                    SimNetworkPort connectedToFirst = firstPort.selfPort;
                    var connectedToFirstRelPosition = GetPortRelativePosition(firstPort.selfPort);

                    //1. Check if a transformations exists to connect the ports with the new block
                    for (int i = 0; i < relPortPositionsToComplyWith.Count; i++)
                    {
                        var current = relPortPositionsToComplyWith[i];
                        if (current.otherPort != firstPort.otherPort)
                        {
                            SimNetworkPort connectedPortInNewBlock = current.selfPort;

                            if (connectedPortInNewBlock != null && newBlockToAdd.Ports.Contains(connectedPortInNewBlock))
                            {
                                var complyDistance = (current.position - firstPort.position).Length;

                                var relPositionOfPort = GetPortRelativePosition(connectedPortInNewBlock);
                                var newBlockDistaces = (connectedToFirstRelPosition - relPositionOfPort).Length;

                                if (Math.Abs(complyDistance - newBlockDistaces) > distanceTolearance)
                                {
                                    nonCompliantConnections.Add((newBlockToAdd.Ports.SelectMany(t => t.Connections)
                                        .FirstOrDefault(t => t.Target == connectedPortInNewBlock || t.Source == connectedPortInNewBlock),
                                        ValidationError.DistanceError));
                                }
                            }
                        }
                    }

                    var firsPortGLobalPosition = GetPortGlobalPosition(firstPort.otherPort);
                    var connectedGlobalPosition = GetPortGlobalPosition(connectedToFirst);

                    transformation.Translate(((SimVector3D)firsPortGLobalPosition) - ((SimVector3D)connectedGlobalPosition));

                    //Check if rotation exists to connect the ports (the first one is connected by a simple translation transformation -->
                    //Hence we apply that transformation to the block and all of its´ ports)
                    if (portsToConnect.Count > 1)
                    {
                        if (connectors.TryGetValue(firstPort.otherPort.RepresentationReference.GeometryId, out var rotCenterConnector))
                        {
                            var rotationCenter = ((Vertex)rotCenterConnector.Geometry).Position;

                            List<SimQuaternion> rotations = new List<SimQuaternion>();
                            // skip first
                            for (int i = 1; i < relPortPositionsToComplyWith.Count; i++)
                            {
                                var current = relPortPositionsToComplyWith[i];

                                if (connectors.TryGetValue(current.otherPort.RepresentationReference.GeometryId, out var connector))
                                {
                                    var toComplyPortVertex = connector.Geometry as Vertex;
                                    var equivalentInConnection = transformation.Transform(GetPortGlobalPosition(current.selfPort));
                                    var targetVector = toComplyPortVertex.Position - rotationCenter;
                                    var vectorToRotate = equivalentInConnection - rotationCenter;
                                    targetVector.Normalize();
                                    vectorToRotate.Normalize();

                                    SimQuaternion q = SimQuaternion.Identity;
                                    var a = SimVector3D.CrossProduct(vectorToRotate, targetVector);
                                    q.X = a.X;
                                    q.Y = a.Y;
                                    q.Z = a.Z;
                                    q.W = Math.Sqrt(1.0 + SimVector3D.DotProduct(targetVector, vectorToRotate));
                                    rotations.Add(q);

                                    if (q != rotations[i - 1])
                                    {
                                        nonCompliantConnections.Add((newBlockToAdd.Ports.SelectMany(t => t.Connections)
                                            .FirstOrDefault(t => t.Target == current.otherPort || t.Source == current.otherPort),
                                            ValidationError.RotationError));
                                    }
                                }
                            }

                            if (rotations.All(t => t == rotations[0]))
                            {
                                var quat = rotations[0];
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
                if (port.Connections.Count == 0)
                {
                    AddStaticUnconnectedPort(port, existingConnectors, true);
                }
            }
            handledBlocks.Add(newBlockToAdd, transformation);

            foreach (var port in connectedBlockPorts)
            {
                if (staticGroup.Contains(port.ParentNetworkElement)
                    && port.ParentNetworkElement is SimNetworkBlock block
                    && !handledBlocks.ContainsKey(block))
                {
                    AddStaticBlockFromChain(handledBlocks, staticGroup, block, existingConnectors);
                }
            }
        }

        /// <summary>
        /// Recursively calculates the 2D offset for the subnetwork to the parent network
        /// </summary>
        /// <param name="subnetwork">The subnetwork</param>
        /// <returns>The 2D offset</returns>
        private SimPoint GetSubnetOffset2D(SimNetwork subnetwork)
        {
            return new SimPoint(0, 0);
        }

        /// <summary>
        /// Adds a static block
        /// </summary>
        /// <param name="block">The block</param>
        /// <param name="existingConnectors">The existing connections</param>
        /// <param name="transformation">The transform</param>
        /// <param name="rotation">The rotation</param>
        private void AddStaticBlock(SimNetworkBlock block, Dictionary<ulong, BaseSimNetworkGeometryConnector> existingConnectors,
            SimMatrix3D transformation, SimQuaternion rotation)
        {
            PrintTrace($"AddStaticBlock: {block.Name}({block.Id.LocalId}");
            Vertex vertex = null;

            //Check if geometry for edge exists
            if (block.RepresentationReference != GeometricReference.Empty)
                vertex = GeometryModel.Geometry.GeometryFromId(block.RepresentationReference.GeometryId) as Vertex;

            // create geo if not exist
            if (vertex == null)
            {
                SimPoint3D position = transformation.Transform(TranslateCanvas2DPositionTo3D(block.Position, GetSubnetOffset2D(block.ParentNetwork)));
                vertex = new Vertex(this.GeometryModel.Geometry.Layers.First(), block.Name, position)
                {
                    Color = new DerivedColor(block.Color),
                };
            }

            // update existing if found
            if (existingConnectors != null && existingConnectors.TryGetValue(vertex.Id, out var con))
            {
                con.ChangeBaseGeometry(vertex);
                connectors.Add(vertex.Id, con);
                existingConnectors.Remove(vertex.Id);
            }
            // create new connector
            else
            {
                var conector = new SimNetworkBlockConnector(vertex, block, this, rotation);
                connectors.Add(vertex.Id, conector);
                AttachBlockEvents(block);
            }

        }


        /// <summary>
        /// Adds a static port and proxy connection between block and port
        /// </summary>
        /// <param name="port">The port</param>
        /// <param name="existingConnectors">The existing connectors</param>
        /// <param name="addPortProxy">If a proxy port should be added</param>
        private void AddStaticUnconnectedPort(SimNetworkPort port, Dictionary<ulong, BaseSimNetworkGeometryConnector> existingConnectors, bool addPortProxy)
        {
            PrintTrace($"AddStaticUnconnectedPort: {port.Name}({port.Id.LocalId}");
            Vertex vertex = null;

            if (port.RepresentationReference != GeometricReference.Empty)
            {
                vertex = this.GeometryModel.Geometry.GeometryFromId(port.RepresentationReference.GeometryId) as Vertex;
            }

            var portPosition = GetPortGlobalPosition(port);
            if (vertex == null)
            {
                vertex = new Vertex(this.GeometryModel.Geometry.Layers.First(), port.Name, portPosition)
                {
                    Color = new DerivedColor(port.Color)
                };
            }

            // if connector is already present, port was updated
            if (!connectors.ContainsKey(vertex.Id))
            {
                if (existingConnectors != null && existingConnectors.TryGetValue(vertex.Id, out var con)
                       && existingConnectors.TryGetValue(port.ParentNetworkElement.RepresentationReference.GeometryId, out var prntConn))
                {
                    con.ChangeBaseGeometry(vertex);
                    connectors.Add(vertex.Id, con);
                    existingConnectors.Remove(vertex.Id);
                }
                else
                {
                    if (connectors.TryGetValue(port.ParentNetworkElement.RepresentationReference.GeometryId, out var parentConn))
                    {
                        var portConnector = new SimNetworkPortConnector(vertex, port, this);
                        connectors.Add(vertex.Id, portConnector);
                    }
                }
            }

            if (addPortProxy)
            {
                AddBlockToPortProxy(port.ParentNetworkElement, port, existingConnectors);
            }
        }

        /// <summary>
        /// Adds a connection vertex for a connection between two static blocks
        /// </summary>
        /// <param name="connectionChain">The connection chain</param>
        /// <param name="existingConnectors">The existing connections</param>
        /// <param name="addPortConnectorProxies">If proxies for the port connectors should be created</param>
        private void AddStaticValidConnectionAsVertex(List<SimNetworkConnection> connectionChain, Dictionary<ulong, BaseSimNetworkGeometryConnector> existingConnectors, bool addPortConnectorProxies)
        {
            Vertex vertex = null;
            var (startPort, endPort) = GetChainStartAndEndPorts(connectionChain);
            PrintTrace($"AddStaticValidConnectionAsPoly: {startPort.Name}({startPort.Id.LocalId}) -> {endPort.Name}({endPort.Id.LocalId})");

            // try and find the vertex
            if (connectionChain[0].RepresentationReference != GeometricReference.Empty)
            {
                vertex = this.GeometryModel.Geometry.GeometryFromId(connectionChain[0].RepresentationReference.GeometryId) as Vertex;
            }

            // Connection is between two valid static blocks, so position of both ports is the same
            SimPoint3D position = GetPortGlobalPosition(startPort);

            if (vertex == null)
            {
                vertex = new Vertex(this.GeometryModel.Geometry.Layers[0], connectionChain[0].Name, position);
            }

            // if connector is already present, connection was updated
            if (!connectors.ContainsKey(vertex.Id))
            {
                if (existingConnectors != null && existingConnectors.TryGetValue(vertex.Id, out var con))
                {
                    con.ChangeBaseGeometry(vertex);
                    connectors.Add(vertex.Id, con);
                    existingConnectors.Remove(vertex.Id);
                }
                else
                {
                    var connectorConnector = new SimNetworkConnectionConnector(vertex, connectionChain, this);
                    connectors.Add(vertex.Id, connectorConnector);
                }
            }

            if (addPortConnectorProxies)
            {
                // ports are on a block, so they only have one connection
                AddPortToConnectionProxy(startPort, startPort.Connections[0], existingConnectors);
                AddPortToConnectionProxy(endPort, endPort.Connections[0], existingConnectors);
            }
        }

        private void AddStaticInvalidConnectionAsPoly(List<SimNetworkConnection> connectionChain, Dictionary<ulong, BaseSimNetworkGeometryConnector> existingConnectors)
        {
            Polyline polyline = null;
            var (startPort, endPort) = GetChainStartAndEndPorts(connectionChain);
            PrintTrace($"AddStaticInvalidConnectionAsPoly: {startPort.Name}({startPort.Id.LocalId}) -> {endPort.Name}({endPort.Id.LocalId})");

            // try to find if a polyline exists already
            if (connectionChain[0].RepresentationReference != GeometricReference.Empty)
            {
                polyline = this.GeometryModel.Geometry.GeometryFromId(connectionChain[0].RepresentationReference.GeometryId) as Polyline;
            }

            if (polyline == null)
            {
                Vertex startVertex = null;
                Vertex endVertex = null;

                if (connectors.TryGetValue(startPort.RepresentationReference.GeometryId, out var startConnector)
                    && connectors.TryGetValue(endPort.RepresentationReference.GeometryId, out var endConnector))
                {
                    startVertex = startConnector.Geometry as Vertex;
                    endVertex = endConnector.Geometry as Vertex;
                    // if any is null or if they both are the same, create new ones
                    if (startVertex == null || endVertex == null || endVertex == startVertex)
                    {
                        startVertex = null;
                        endVertex = null;
                    }
                }

                if (startVertex == null)
                {
                    var startPosition = GetPortGlobalPosition(startPort);
                    startVertex = new Vertex(this.GeometryModel.Geometry.Layers[0], startPort.Name, startPosition);
                }
                if (endVertex == null)
                {
                    var endPosition = GetPortGlobalPosition(endPort);
                    endVertex = new Vertex(this.GeometryModel.Geometry.Layers[0], endPort.Name, endPosition);
                }

                var innerEdge = new Edge(this.GeometryModel.Geometry.Layers[0], connectionChain[0].Name + "_EDGE1",
                    new Vertex[] { startVertex, endVertex })
                {
                    Color = new DerivedColor(SimColors.Red)
                };

                polyline = new Polyline(this.GeometryModel.Geometry.Layers[0], "CHAIN",
                new Edge[] { innerEdge })
                {
                    Color = new DerivedColor(SimColors.Red)
                };
            }

            // if connector is already present, connection was updated
            if (!connectors.ContainsKey(polyline.Id))
            {
                if (existingConnectors != null && existingConnectors.TryGetValue(polyline.Id, out var con))
                {
                    con.ChangeBaseGeometry(polyline);
                    connectors.Add(polyline.Id, con);
                    existingConnectors.Remove(polyline.Id);
                }
                else
                {
                    var connectorConnector = new SimNetworkInvalidConnectionConnector(polyline, connectionChain, this);
                    connectors.Add(polyline.Id, connectorConnector);
                }
            }
        }

        private BaseGeometry TryGetGeometry(GeometricReference geoRef)
        {
            if (geoRef == GeometricReference.Empty)
                return null;
            if (geoRef.FileId != GeometryModel.File.Key)
                return null;
            return GeometryModel.Geometry.GeometryFromId(geoRef.GeometryId);
        }

        private void AddStaticConnection(List<SimNetworkConnection> connectionChain, Dictionary<ulong, BaseSimNetworkGeometryConnector> existingGeoConnectors)
        {
            var (startPort, endPort) = GetChainStartAndEndPorts(connectionChain);

            SimPoint3D startPortPosittion = GetPortGlobalPosition(startPort);
            SimPoint3D endPortPosition = GetPortGlobalPosition(endPort);
            var portsOnSamePosition = (startPortPosittion - endPortPosition).LengthSquared < distanceTolearance2;

            // try to get the geometry 
            var connection = connectionChain[0];
            var connectionGeo = TryGetGeometry(connection.RepresentationReference);
            var startPortGeo = TryGetGeometry(startPort.RepresentationReference);
            var endPortGeo = TryGetGeometry(endPort.RepresentationReference);

            PrintTrace($"AddStaticConnection: {connection.Name}({connection.Id.LocalId}");

            if (portsOnSamePosition)
            {
                // ports moved to same position but were invalid before (polyline)
                // so remove the polyline and create a vertex
                if (connectionGeo is Polyline conPolyline)
                {
                    // remove unused polyline
                    foreach (var edge in conPolyline.Edges)
                    {
                        edge.Edge.RemoveFromModel();
                        TryRemoveConnector(edge.Edge, existingGeoConnectors);
                    }
                    conPolyline.RemoveFromModel();
                    TryRemoveConnector(conPolyline, existingGeoConnectors);

                    //Clean the unused start geometry
                    if (startPortGeo is Vertex startVertex)
                    {
                        this.RemoveUnusedVertex(startVertex, existingGeoConnectors);
                    }

                    //Clean the unused end geometry
                    if (endPortGeo is Vertex endVertex)
                    {
                        this.RemoveUnusedVertex(endVertex, existingGeoConnectors);
                    }
                }

                // valid connection
                AddStaticValidConnectionAsVertex(connectionChain, existingGeoConnectors, true);
            }
            else
            {
                // ports are not on the same position anymore, but were valid before (vertex)
                // so remove the vertex and create a polyline
                if (connectionGeo is Vertex vertexGeom)
                {
                    RemoveUnusedVertex(vertexGeom, existingGeoConnectors);
                    if (startPortGeo is Vertex startVertex)
                    {
                        TryRemoveConnector(startVertex, existingGeoConnectors);
                    }
                    if (endPortGeo is Vertex endVertex)
                    {
                        TryRemoveConnector(endVertex, existingGeoConnectors);
                    }
                }

                // add invalid connection
                AddStaticUnconnectedPort(startPort, existingGeoConnectors, true);
                AddStaticUnconnectedPort(endPort, existingGeoConnectors, true);
                AddStaticInvalidConnectionAsPoly(connectionChain, existingGeoConnectors);
            }
        }

        private void RemoveUnusedVertex(Vertex vertex, Dictionary<ulong, BaseSimNetworkGeometryConnector> existingGeoConnectors = null)
        {
            foreach (var edge in vertex.Edges.ToList())
            {
                edge.PEdges.Select(x => x.Parent).OfType<Polyline>().ToList().ForEach(p =>
                {
                    p.RemoveFromModel();
                    TryRemoveConnector(p, existingGeoConnectors);
                });
                edge.RemoveFromModel();
                TryRemoveConnector(edge, existingGeoConnectors);
            }
            foreach (var proxy in vertex.ProxyGeometries.ToList())
            {
                proxy.RemoveFromModel();
                TryRemoveConnector(proxy, existingGeoConnectors);
            }
            vertex.RemoveFromModel();
            TryRemoveConnector(vertex, existingGeoConnectors);
        }


        /// <summary>
        /// Adds the vertex for the dynamic connection between two ports.
        /// Either connects two dynamic blocks or one dynamic and one static block.
        /// </summary>
        /// <param name="connectionChain">The chain of connections</param>
        /// <param name="existingConnectors">The existing connectors</param>
        private void AddDynamicConnection(List<SimNetworkConnection> connectionChain, Dictionary<ulong, BaseSimNetworkGeometryConnector> existingConnectors)
        {
            Vertex vertex = null;
            var (startPort, endPort) = GetChainStartAndEndPorts(connectionChain);
            PrintTrace($"AddDynamicConnection: {startPort.Name}({startPort.Id.LocalId}) -> {endPort.Name}({endPort.Id.LocalId})");

            if (connectors.TryGetValue(startPort.ParentNetworkElement.RepresentationReference.GeometryId, out var startParent)
                && connectors.TryGetValue(endPort.ParentNetworkElement.RepresentationReference.GeometryId, out var endParent))
            {
                //Check if geometry for edge exists
                if (connectionChain[0].RepresentationReference != GeometricReference.Empty)
                    vertex = GeometryModel.Geometry.GeometryFromId(connectionChain[0].RepresentationReference.GeometryId) as Vertex;

                SimPoint3D position;
                if (startPort.ParentNetworkElement is SimNetworkBlock b && b.IsStatic)
                {
                    position = GetPortGlobalPosition(startPort);
                }
                else if (endPort.ParentNetworkElement is SimNetworkBlock b1 && b1.IsStatic)
                {
                    position = GetPortGlobalPosition(endPort);
                }
                else // use point in between the parents or ports
                {
                    if (startParent.Geometry is Vertex startParentVertex
                        && endParent.Geometry is Vertex endParentVertex)
                    {
                        position = (SimPoint3D)(((SimVector3D)startParentVertex.Position + (SimVector3D)endParentVertex.Position) / 2.0f);
                    }
                    else
                    {
                        var start = (SimVector3D)GetPortGlobalPosition(startPort);
                        var end = (SimVector3D)GetPortGlobalPosition(startPort);
                        position = (SimPoint3D)((start + end) / 2.0f);
                    }
                }

                // create the vertex if not found
                if (vertex == null)
                {
                    vertex = new Vertex(this.GeometryModel.Geometry.Layers.First(), startPort.Name, position);
                }

                // if in connectors, it was updated
                if (!connectors.ContainsKey(vertex.Id))
                {
                    if (existingConnectors != null && existingConnectors.TryGetValue(vertex.Id, out var con))
                    {
                        con.ChangeBaseGeometry(vertex);
                        connectors.Add(vertex.Id, con);
                        existingConnectors.Remove(vertex.Id);
                    }
                    else
                    {
                        //Remove old Port connectors 
                        if (connectors.TryGetValue(startPort.RepresentationReference.GeometryId, out var _))
                        {
                            RemovePort(startPort);
                        }
                        if (connectors.TryGetValue(endPort.RepresentationReference.GeometryId, out var _))
                        {
                            RemovePort(endPort);
                        }
                        var conConnector = new SimNetworkConnectionConnector(vertex, connectionChain, this);
                        connectors.Add(vertex.Id, conConnector);
                    }
                }

                AddPortToConnectionProxy(startPort, startPort.Connections[0], existingConnectors);
                AddPortToConnectionProxy(endPort, endPort.Connections[0], existingConnectors);
            }
        }

        private SimPoint3D TranslateCanvas2DPositionTo3D(SimPoint point, SimPoint offset)
        {
            double canvasX = point.X + offset.X;
            double canvasY = point.Y + offset.Y;

            double x = (canvasX) / ReduceRatio;
            double z = (canvasY) / ReduceRatio;
            double y = 0;

            return new SimPoint3D(x, y, z);
        }

        private SimPoint3D GetPortGlobalPosition(SimNetworkPort port)
        {
            var relPosition = GetPortRelativePosition(port);

            if (connectors.TryGetValue(port.ParentNetworkElement.RepresentationReference.GeometryId, out var parentConnector))
            {
                var rotation = SimQuaternion.Identity;
                SimPoint3D parentPos;
                if (parentConnector.Geometry is Vertex vertex)
                {
                    rotation = vertex.ProxyGeometries.FirstOrDefault()?.Rotation ?? SimQuaternion.Identity;
                    parentPos = vertex.Position;
                }
                else
                {
                    parentPos = TranslateCanvas2DPositionTo3D(port.ParentNetworkElement.Position, GetSubnetOffset2D(port.ParentNetwork));
                }
                var matrixR = new SimMatrix3D();
                matrixR.Rotate(rotation);
                relPosition = matrixR.Transform(relPosition);

                return parentPos + (SimVector3D)relPosition;

            }
            else
            {
                var position = TranslateCanvas2DPositionTo3D(port.ParentNetworkElement.Position, GetSubnetOffset2D(port.ParentNetwork));
                return position + (SimVector3D)relPosition;
            }
        }

        /// <summary>
        /// Returns the relative position of a port to its parent network element.
        /// </summary>
        /// <param name="port">The port</param>
        /// <returns>The relative position</returns>
        public static SimPoint3D GetPortRelativePosition(SimNetworkPort port)
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

                if (port.PortType == PortType.Input)
                {

                    positionX = -2;
                    positionZ = port.ParentNetworkElement.Ports.Where(t => t.PortType == PortType.Input).ToList().IndexOf(port) * 2;
                }
                else
                {
                    positionX = +2;
                    positionZ = port.ParentNetworkElement.Ports.Where(t => t.PortType == PortType.Output).ToList().IndexOf(port) * 2;
                }

                return new SimPoint3D(positionX, positionY, positionZ);
            }
        }

        /// <summary>
        /// Adds the Vertex for a dynamic block/network
        /// </summary>
        /// <param name="networkElement">The block/network</param>
        /// <param name="existingConnectors">The existing connectors</param>
        private void AddDynamicBlock(BaseSimNetworkElement networkElement, Dictionary<ulong, BaseSimNetworkGeometryConnector> existingConnectors)
        {
            PrintTrace($"AddDynamicBlock: {networkElement.Name}({networkElement.Id.LocalId})");
            Vertex vertex = null;
            var color = new DerivedColor(SimColor.FromArgb(10, (byte)rnd.Next(256), (byte)rnd.Next(256), (byte)rnd.Next(256)));
            // try to get the geometry
            if (networkElement.RepresentationReference != GeometricReference.Empty)
            {
                vertex = this.GeometryModel.Geometry.GeometryFromId(networkElement.RepresentationReference.GeometryId) as Vertex;
            }
            // create if not found
            if (vertex == null)
            {
                if (networkElement is SimNetworkBlock || networkElement is SimNetwork)
                {
                    color = new DerivedColor(networkElement.Color);
                }

                var position = TranslateCanvas2DPositionTo3D(networkElement.Position, GetSubnetOffset2D(networkElement.ParentNetwork));
                vertex = new Vertex(this.GeometryModel.Geometry.Layers.First(), networkElement.Name, position)
                {
                    Color = color
                };
            }

            // connector existed with the same geometry
            if (existingConnectors != null && existingConnectors.TryGetValue(vertex.Id, out var con))
            {
                con.ChangeBaseGeometry(vertex);
                connectors.Add(vertex.Id, con);
                existingConnectors.Remove(vertex.Id);
            }
            // create a new connector
            else if (networkElement is SimNetworkBlock block)
            {
                connectors.Add(vertex.Id, new SimNetworkBlockConnector(vertex, block, this, SimQuaternion.Identity));
                AttachBlockEvents(block);
            }
            else if (networkElement is SimNetwork nw)
            {
                connectors.Add(vertex.Id, new SimNetworkNetworkConnector(vertex, nw, this, SimQuaternion.Identity));
            }
        }

        private List<HashSet<SimNetworkBlock>> FindStaticGroups()
        {
            return FindStaticGroups(this.Network, new());
        }



        /// <summary>
        /// Calculates the 3D bounding box of a subnetwork geometry
        /// </summary>
        /// <param name="network">The network</param>
        /// <returns>The bounding box of the network geometry</returns>
        private (SimPoint3D Min, SimPoint3D Max) GetSubnetworkBoundingBox(SimNetwork network)
        {
            double minX = double.MaxValue;
            double minY = double.MaxValue;
            double minZ = double.MaxValue;
            double maxX = double.MinValue;
            double maxY = double.MinValue;
            double maxZ = double.MinValue;

            for (int i = 0; i < network.ContainedElements.Count; i++)
            {

                if (connectors.TryGetValue(network.ContainedElements[i].RepresentationReference.GeometryId, out var connector))
                {
                    if (connector is SimNetworkBaseNetworkElementConnector baseElementCon)
                    {
                        var pos = baseElementCon.Vertex.Position;
                        if (pos.X < minX)
                        {
                            minX = pos.X;
                        }
                        if (pos.Y < minY)
                        {
                            minY = pos.Y;
                        }
                        if (pos.Z < minZ)
                        {
                            minZ = pos.Z;
                        }
                        if (pos.X > maxX)
                        {
                            maxX = pos.X;
                        }
                        if (pos.Y > maxY)
                        {
                            maxY = pos.Y;
                        }
                        if (pos.Z > maxZ)
                        {
                            maxZ = pos.Z;
                        }
                    }

                }
                else if (network.ContainedElements[i] is SimNetwork subNetworkWithElements)
                {
                    var subBox = GetSubnetworkBoundingBox(subNetworkWithElements);

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
            return (new SimPoint3D(minX, minY, minZ),
                new SimPoint3D(maxX, maxY, maxZ));
        }


        /// <summary>
        /// Called when a coordinate parameter of a static port changed
        /// </summary>
        /// <param name="param"></param>
        internal void OnStaticPortCoordinateChanged(SimDoubleParameter param)
        {
            this.connectors.Values.OfType<SimNetworkBlockConnector>()
                .ForEach(c => c.transformInProgress = true);

            // find the ports of the parameter
            var ports = param.Component.Instances.SelectMany(t => t.Placements)
                .OfType<SimInstancePlacementSimNetwork>()
                .Where(p => p.NetworkElement is SimNetworkPort port
                && connectors.TryGetValue(port.RepresentationReference.GeometryId, out var portConnector))
                .Select(p => (SimNetworkPort)p.NetworkElement);

            foreach (var port in ports)
            {
                if (port.Connections.Count == 0)
                {
                    if (port.ParentNetworkElement is SimNetworkBlock bl && bl.IsStatic)
                    {
                        AddStaticUnconnectedPort(port, this.connectors, false);
                    }
                    else
                    {
                        AddUnconnectedPort(port, this.connectors);
                    }
                }
            }
            if (ports.Any())
            {
                UpdateNetworkConnections(this.Network, this.connectors);
            }
            this.connectors.Values.OfType<SimNetworkBlockConnector>()
                .ForEach(c => c.transformInProgress = false);
        }


        /// <summary>
        /// Adds the Polyline from a port's block to the connection vertex
        /// </summary>
        /// <param name="port">The port</param>
        /// <param name="connection">The connection</param>
        /// <param name="existingConnectors">The existing connections</param>
        /// <exception cref="Exception">If the geometries could not be found</exception>
        private void AddPortToConnectionProxy(SimNetworkPort port, SimNetworkConnection connection, Dictionary<ulong, BaseSimNetworkGeometryConnector> existingConnectors)
        {
            PrintTrace($"AddPortToConnectionProxy: {port.Name}({port.Id.LocalId}) -> {connection.Name}({connection.Id.LocalId})");
            Vertex blockVertex;
            Vertex connectionVertex;

            if (connectors.TryGetValue(port.ParentNetworkElement.RepresentationReference.GeometryId, out var parentConnector))
            {
                blockVertex = parentConnector.Geometry as Vertex;
            }
            else
            {
                throw new Exception("Block connector not found");
            }
            if (connectors.TryGetValue(connection.RepresentationReference.GeometryId, out var connectorConnector))
            {
                connectionVertex = connectorConnector.Geometry as Vertex;
            }
            else
            {
                throw new Exception("Connection connector not found");
            }

            // try to find edge connector and geometry
            var edgeCon = existingConnectors?.Values.OfType<SimNetworkBlockPortConnectorProxy>()
                .FirstOrDefault(t => t.ParentElement == port.ParentNetworkElement && t.Port == port);
            var polyline = edgeCon?.Geometry as Polyline;

            // try to find polyline connected to the vertices
            if (polyline == null || !polyline.ModelGeometry.ContainsGeometry(polyline))
            {
                polyline = TryFindPolylineBetweenVertices(blockVertex, connectionVertex);
            }

            if (polyline == null || !polyline.ModelGeometry.ContainsGeometry(polyline))
            {
                var innerEdge = new Edge(this.GeometryModel.Geometry.Layers.First(), blockVertex.Name + "_EDGE2",
                    new Vertex[] { blockVertex, connectionVertex })
                { Color = new DerivedColor(connection.Color) };

                polyline = new Polyline(this.GeometryModel.Geometry.Layers.First(), blockVertex.Name + "_PROXY",
                    new Edge[] { innerEdge })
                { Color = new DerivedColor(connection.Color) };
            }

            // check static constraints
            var constraintsValid = true;
            if (port.ParentNetworkElement is SimNetworkBlock block && block.IsStatic)
            {
                // connection should be positioned at the static ports position
                constraintsValid = CheckStaticPortConstraints(port, connectionVertex);
            }

            // change or create connector
            if (!connectors.ContainsKey(polyline.Id))
            {
                if (existingConnectors != null && existingConnectors.TryGetValue(polyline.Id, out var con))
                {
                    con.ChangeBaseGeometry(polyline);
                    connectors.Add(polyline.Id, con);
                    existingConnectors.Remove(polyline.Id);
                }
                else
                {
                    connectors.Add(polyline.Id, new SimNetworkBlockPortConnectorProxy(polyline, port.ParentNetworkElement, port, constraintsValid, connection));
                }
            }
        }

        private static Polyline TryFindPolylineBetweenVertices(Vertex startVertex, Vertex endVertex)
        {
            return startVertex.Edges.SelectMany(x => x.PEdges).Select(x => x.Parent)
                                .OfType<Polyline>().FirstOrDefault(pl => pl.Edges[0].Edge.Vertices.Contains(endVertex)
                                    || pl.Edges[pl.Edges.Count - 1].Edge.Vertices.Contains(endVertex));
        }


        /// <summary>
        /// Adds the Polyline from a block/network to the port
        /// </summary>
        /// <param name="parentElement">The block/network</param>
        /// <param name="port">The port</param>
        /// <param name="existingConnectors">The existing connections</param>
        /// <exception cref="Exception">If the geometries could not be found</exception>
        private void AddBlockToPortProxy(BaseSimNetworkElement parentElement, SimNetworkPort port, Dictionary<ulong, BaseSimNetworkGeometryConnector> existingConnectors)
        {
            Vertex blockVertex;
            Vertex portVertex;

            PrintTrace($"AddBlockToPortProxy: {parentElement.Name}({parentElement.Id.LocalId}) -> {port.Name}({port.Id.LocalId})");

            if (connectors.TryGetValue(parentElement.RepresentationReference.GeometryId, out var parentConnector))
            {
                blockVertex = parentConnector.Geometry as Vertex;
            }
            else
            {
                throw new Exception("Parent connector was not found");
            }

            if (connectors.TryGetValue(port.RepresentationReference.GeometryId, out var portConnector))
            {
                portVertex = portConnector.Geometry as Vertex;
            }
            else
            {
                throw new Exception($"Could not find port connector of port {port.Name}({port.Id.LocalId})");
            }


            // try to find polyline from the existing connectors
            var edgeCon = existingConnectors?.Values.OfType<SimNetworkBlockPortConnectorProxy>()
                .FirstOrDefault(t => t.ParentElement == parentElement && t.Port == port);
            var polyline = edgeCon?.Geometry as Polyline;

            // try to find polyline connected to the vertices
            if (polyline == null || !polyline.ModelGeometry.ContainsGeometry(polyline))
            {
                polyline = TryFindPolylineBetweenVertices(blockVertex, portVertex);
            }

            // create polyline if not found or if it got removed from the model
            if (polyline == null || !polyline.ModelGeometry.ContainsGeometry(polyline))
            {
                var innerEdge = new Edge(this.GeometryModel.Geometry.Layers.First(), blockVertex.Name + "_to_" + portVertex.Name,
                    new Vertex[] { blockVertex, portVertex })
                {
                    Color = new DerivedColor(port.Color)
                };

                polyline = new Polyline(this.GeometryModel.Geometry.Layers.First(), blockVertex.Name + "_PROXY",
                    new Edge[] { innerEdge })
                {
                    Color = new DerivedColor(port.Color)
                };
            }

            // check static constraints
            var constraintsValid = true;
            if (port.ParentNetworkElement is SimNetworkBlock block && block.IsStatic)
            {
                constraintsValid = CheckStaticPortConstraints(port, portVertex);
            }

            // change or create connector
            if (!connectors.ContainsKey(polyline.Id))
            {
                if (edgeCon != null) // was in existingConnectors so update
                {
                    edgeCon.ChangeBaseGeometry(polyline);
                    connectors.Add(polyline.Id, edgeCon);
                    existingConnectors.Remove(polyline.Id);
                }
                else // add new one
                {
                    connectors.Add(polyline.Id, new SimNetworkBlockPortConnectorProxy(polyline, parentElement, port, constraintsValid));
                }
            }
        }

        /// <summary>
        /// Checks if the static port constraints are valid.
        /// If the port is at its static global position, the constraints are valid.
        /// </summary>
        /// <param name="port">The port to check</param>
        /// <param name="portVertex">The vertex representation of that port or the connection</param>
        /// <returns>If the contraints are valid</returns>
        private bool CheckStaticPortConstraints(SimNetworkPort port, Vertex portVertex)
        {
            var targetPos = this.GetPortGlobalPosition(port);
            if ((targetPos - portVertex.Position).LengthSquared > distanceTolearance2)
            {
                return false;
            }

            return true;
        }

        private void AttachBlockEvents(SimNetworkBlock block)
        {
            block.Ports.CollectionChanged += this.Ports_CollectionChanged;
        }


        private void AttachNetworkEvents(SimNetwork simNetwork)
        {
            simNetwork.ContainedElements.CollectionChanged -= this.ContainedElements_CollectionChanged;
            simNetwork.ContainedConnections.CollectionChanged -= this.ContainedConnections_CollectionChanged;
            simNetwork.Ports.CollectionChanged -= this.Ports_CollectionChanged;


            simNetwork.ContainedElements.CollectionChanged += this.ContainedElements_CollectionChanged;
            simNetwork.ContainedConnections.CollectionChanged += this.ContainedConnections_CollectionChanged;
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

                            AddUnconnectedPort(port, null);

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


        private void ContainedConnections_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                    foreach (var item in e.NewItems)
                    {
                        var con = item as SimNetworkConnection;
                        UpdateConnection(con, null);
                    }
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                    foreach (var item in e.OldItems)
                    {
                        if (item is SimNetworkConnection connection)
                        {
                            RemoveConnection(connection);
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
                UpdateNetwork(this.Network, false);
            }
            else
            {
                switch (e.Action)
                {
                    case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                        foreach (var item in e.NewItems)
                        {
                            if (item is SimNetworkBlock block)
                            {
                                if (block.IsStatic)
                                {
                                    AddStaticBlockFromChain(new(), new() { block }, block, null);
                                }
                                else
                                {
                                    AddDynamicBlock(block, null);
                                }
                            }
                            if (item is SimNetwork subNetwork)
                                AddNetwork(subNetwork, null, false);
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

                var vertex = this.GeometryModel.Geometry.GeometryFromId(block.RepresentationReference.GeometryId) as Vertex;
                if (vertex != null)
                {
                    GeometryModel.Geometry.StartBatchOperation();
                    this.RemoveUnusedVertex(vertex, null);
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

                var vertex = this.GeometryModel.Geometry.GeometryFromId(port.RepresentationReference.GeometryId) as Vertex;
                if (vertex != null)
                {
                    GeometryModel.Geometry.StartBatchOperation();
                    RemoveUnusedVertex(vertex, null);
                    GeometryModel.Geometry.EndBatchOperation();
                }
            }
        }

        private void RemoveConnection(SimNetworkConnection connection)
        {
            if (connection.RepresentationReference != GeometricReference.Empty)
            {
                if (connection.RepresentationReference.FileId != this.GeometryModel.File.Key)
                    throw new Exception("Port is not connected to this geometry model");

                if (connectors.TryGetValue(connection.RepresentationReference.GeometryId, out var connector))
                {
                    if (connector is SimNetworkConnectionConnector conCon)
                    {
                        var vertex = connector.Geometry as Vertex;
                        GeometryModel.Geometry.StartBatchOperation();
                        RemoveUnusedVertex(vertex, null);
                        GeometryModel.Geometry.EndBatchOperation();
                    }
                    else if (connector is SimNetworkInvalidConnectionConnector inCon)
                    {
                        GeometryModel.Geometry.StartBatchOperation();
                        var polyline = this.GeometryModel.Geometry.GeometryFromId(connection.RepresentationReference.GeometryId) as Polyline;
                        polyline.RemoveFromModel();
                        //Delete edges that use this vertex
                        foreach (var edge in polyline.Edges.Select(x => x.Edge).Distinct())
                        {
                            edge.PEdges.Select(x => x.Parent).OfType<Polyline>().ToList().ForEach(p => p.RemoveFromModel());
                            edge.RemoveFromModel();
                        }
                        TryRemoveConnector(polyline);
                        GeometryModel.Geometry.EndBatchOperation();
                    }
                }
            }

            if (connection.Source.ParentNetworkElement is SimNetworkBlock block && block.IsStatic)
            {
                AddStaticUnconnectedPort(connection.Source, null, true);
            }
            else
            {
                AddUnconnectedPort(connection.Source, null);
            }
            if (connection.Target.ParentNetworkElement is SimNetworkBlock block1 && block1.IsStatic)
            {
                AddStaticUnconnectedPort(connection.Target, null, true);
            }
            else
            {
                AddUnconnectedPort(connection.Target, null);
            }
        }

        private void RemoveSimNetwork(SimNetwork network)
        {
            if (network.RepresentationReference != GeometricReference.Empty)
            {
                if (network.RepresentationReference.FileId != this.GeometryModel.File.Key)
                    throw new Exception("Network is not connected to this geometry model");

                var vertex = this.GeometryModel.Geometry.GeometryFromId(network.RepresentationReference.GeometryId) as Vertex;
                if (vertex != null)
                {
                    GeometryModel.Geometry.StartBatchOperation();
                    RemoveUnusedVertex(vertex);
                    GeometryModel.Geometry.EndBatchOperation();
                }

                foreach (var item in network.ContainedConnections)
                {
                    RemoveConnection(item);
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
            foreach (var connector in connectors.Where(t => t.Value is SimNetworkConnectionConnector || t.Value is SimNetworkBlockConnector || t.Value is SimNetworkNetworkConnector))
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
            usedEdges.Clear();
            usedEdges = null;

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

        private void TryRemoveConnector(BaseGeometry geo, Dictionary<ulong, BaseSimNetworkGeometryConnector> existingConnectors = null)
        {
            if (connectors.TryGetValue(geo.Id, out var connector))
            {
                connector.Dispose();
                connectors.Remove(geo.Id);
            }
            if (existingConnectors != null && existingConnectors.TryGetValue(geo.Id, out var econ))
            {
                econ.Dispose();
                existingConnectors.Remove(geo.Id);
            }
        }

        #region Dispose

        private void DetachEvents(SimNetwork simNetwork)
        {
            simNetwork.ContainedElements.CollectionChanged -= this.ContainedElements_CollectionChanged;
            simNetwork.ContainedConnections.CollectionChanged -= this.ContainedConnections_CollectionChanged;
            simNetwork.Ports.CollectionChanged -= this.Ports_CollectionChanged;
            GeometryModel.Geometry.TopologyChanged -= this.Geometry_TopologyChanged;
            GeometryModel.Geometry.GeometryChanged -= this.Geometry_GeometryChanged;
            this.GeometryModel.Replaced -= this.GeometryModel_Replaced;

            foreach (var subnet in simNetwork.ContainedElements.Where(t => t is SimNetwork))
                DetachEvents(subnet as SimNetwork);
        }



        private bool traceEnabled = false;

        /// <summary>
        /// For debugging
        /// </summary>
        /// <param name="message"></param>
        private void PrintTrace(string message)
        {
            if (traceEnabled)
            {
                Debug.WriteLine(message);
            }
        }

        /// <summary>
        /// Disposes the connector and cleans up resources.
        /// </summary>
        /// <param name="disposing">If it is actually disposing</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!isDisposed)
            {
                if (disposing)
                {
                    DetachEvents(Network);

                    foreach (var con in connectors.Values)
                        con.Dispose();

                    connectors.Clear();
                }

                isDisposed = true;
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
        }
        #endregion
    }
}

