using SIMULTAN.Data;
using SIMULTAN.Data.FlowNetworks;
using SIMULTAN.Data.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media.Media3D;

namespace SIMULTAN.Exchange.NetworkConnectors
{
    /// <summary>
    /// Handles the connection between a <see cref="GeometryModel"/> and a <see cref="SimFlowNetwork"/>.
    /// Manages <see cref="NetworkEdgeConnector"/> and <see cref="NetworkNodeConnector"/>.
    /// Only works with GeometryModels created by the 
    /// <see cref="ComponentGeometryExchange.ConvertNetwork(SimFlowNetwork, System.IO.FileInfo)"/> method.
    /// </summary>
    internal class NetworkGeometryModelConnector
    {
        #region Properties

        /// <summary>
        /// The network monitored by this connector
        /// </summary>
        internal SimFlowNetwork Network { get; }
        /// <summary>
        /// The geometry model managed by this connector
        /// </summary>
        internal GeometryModel GeometryModel { get; }

        /// <summary>
        /// The exchange object which created this connector
        /// </summary>
        internal ComponentGeometryExchange Exchange { get; }

        private Dictionary<ulong, BaseNetworkConnector> connectors = new Dictionary<ulong, BaseNetworkConnector>();

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="NetworkGeometryModelConnector"/> class.
        /// Modifies the GeometryModel such that it exactly fits the Network. Missing elements are created and unused elements will be deleted.
        /// </summary>
        /// <param name="model">The geometry model used to represent the network. Warning: All content in this model may be deleted if
        /// it doesn't fit to the network description</param>
        /// <param name="network">The network that should be represented</param>
        /// <param name="exchange">The exchange object</param>
        public NetworkGeometryModelConnector(GeometryModel model, SimFlowNetwork network, ComponentGeometryExchange exchange)
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

            this.GeometryModel.Geometry.GeometryChanged += this.Geometry_GeometryChanged;
            this.GeometryModel.Geometry.TopologyChanged += this.Geometry_TopologyChanged;
            this.GeometryModel.Replaced += this.GeometryModel_Replaced;

            //Add child connectors, make sure that all network elements are properly represented in the geometry model
            Update();

            AttachEvents(network);
        }


        #region Geometry EventHandler

        private void Geometry_TopologyChanged(object sender, IEnumerable<BaseGeometry> geometries)
        {
            foreach (var geom in geometries)
            {
                if (connectors.TryGetValue(geom.Id, out var con))
                    con.OnTopologyChanged();
            }
        }

        private void Geometry_GeometryChanged(object sender, IEnumerable<BaseGeometry> geometries)
        {
            foreach (var geom in geometries)
            {
                if (connectors.TryGetValue(geom.Id, out var con))
                    con.OnGeometryChanged();
            }
        }

        private void GeometryModel_Replaced(object sender, GeometryModelReplacedEventArgs e)
        {
            if (e.OldGeometry != null)
            {
                e.OldGeometry.GeometryChanged -= this.Geometry_GeometryChanged;
                e.OldGeometry.TopologyChanged -= this.Geometry_TopologyChanged;
            }

            //Reconnect everything
            Update();


            if (e.NewGeometry != null)
            {
                e.NewGeometry.GeometryChanged += this.Geometry_GeometryChanged;
                e.OldGeometry.TopologyChanged += this.Geometry_TopologyChanged;
            }
        }

        #endregion

        #region Network EventHandler

        private void Network_ElementDeleted(object sender, SimFlowNetworkElement deletedElement)
        {
            if (deletedElement is SimFlowNetwork flnet)
                DetachEvents(flnet);
            if (deletedElement is SimFlowNetworkNode flnode)
                RemoveNode(flnode);
            if (deletedElement is SimFlowNetworkEdge fledge)
                RemoveEdge(fledge);
        }

        private void Network_ElementAdded(object sender, SimFlowNetworkElement addedElement)
        {
            if (addedElement is SimFlowNetwork flnet)
                AttachEvents(flnet);
            if (addedElement is SimFlowNetworkNode flnode)
                AddNode(flnode, null);
            if (addedElement is SimFlowNetworkEdge fledge)
                AddEdge(fledge, null);
        }

        private void Network_EdgeRedirected(object sender, SimFlowNetworkEdge edge)
        {
            if (edge.RepresentationReference.FileId != this.GeometryModel.File.Key)
                throw new Exception("Node is not connected to this geometry model");

            if (connectors.TryGetValue(edge.RepresentationReference.GeometryId, out var con))
            {
                if (con is NetworkEdgeConnector econ)
                    econ.OnEdgeRedirected();
            }
        }

        private void Network_ElementTopologyChanged(object sender, SimFlowNetworkElement oldElement, SimFlowNetworkElement newElement, List<SimFlowNetworkElement> changedElements)
        {
            if (oldElement is SimFlowNetwork oldNW)
                DetachEvents(oldNW);

            Update();

            if (newElement is SimFlowNetwork newNW)
                AttachEvents(newNW);
        }

        #endregion


        private void AttachEvents(SimFlowNetwork network)
        {
            network.ElementAdded += this.Network_ElementAdded;
            network.ElementDeleted += this.Network_ElementDeleted;
            network.EdgeRedirected += this.Network_EdgeRedirected;
            network.ElementTopologyChanged += this.Network_ElementTopologyChanged;

            foreach (var subnet in network.ContainedFlowNetworks.Values)
                AttachEvents(subnet);
        }

        private void DetachEvents(SimFlowNetwork network)
        {
            network.ElementAdded -= this.Network_ElementAdded;
            network.ElementDeleted -= this.Network_ElementDeleted;
            network.EdgeRedirected -= this.Network_EdgeRedirected;
            network.ElementTopologyChanged -= this.Network_ElementTopologyChanged;

            foreach (var subnet in network.ContainedFlowNetworks.Values)
                DetachEvents(subnet);
        }

        private void Update()
        {
            GeometryModel.Geometry.StartBatchOperation();

            if (GeometryModel.Geometry.Layers.Count == 0)
            {
                GeometryModel.Geometry.Layers.Add(new Layer(GeometryModel.Geometry, "0"));
            }

            Dictionary<ulong, BaseNetworkConnector> existingConnectors = new Dictionary<ulong, BaseNetworkConnector>(this.connectors);
            connectors.Clear();

            //Make sure that all nodes of the flattened network exist in the geometry model
            UpdateNodes(this.Network, existingConnectors);
            UpdateEdges(this.Network, existingConnectors);
            CleanUnusedGeometry();

            foreach (var con in existingConnectors.Values)
                con.Dispose();

            GeometryModel.Geometry.EndBatchOperation();
        }

        private void UpdateNodes(SimFlowNetwork network, Dictionary<ulong, BaseNetworkConnector> existingConnectors)
        {
            GeometryModel.Geometry.StartBatchOperation();

            foreach (var node in network.ContainedNodes.Values)
                AddNode(node, existingConnectors);

            foreach (var subNetwork in network.ContainedFlowNetworks.Values)
                UpdateNodes(subNetwork, existingConnectors);

            GeometryModel.Geometry.EndBatchOperation();
        }
        private void AddNode(SimFlowNetworkNode node, Dictionary<ulong, BaseNetworkConnector> existingConnectors)
        {
            Vertex geometry = null;

            //GeometryModel.Geometry.StartBatchOperation();

            if (node.RepresentationReference != GeometricReference.Empty)
            {
                if (node.RepresentationReference.FileId != this.GeometryModel.File.Key)
                    throw new Exception("Node is not connected to this geometry model");

                geometry = this.GeometryModel.Geometry.GeometryFromId(node.RepresentationReference.GeometryId) as Vertex;
            }

            if (geometry == null)
            {
                geometry = new Vertex(this.GeometryModel.Geometry.Layers.First(), node.Name,
                    new Point3D(node.Position.X / 100.0, 0, node.Position.Y / 100.0));
            }

            if (existingConnectors != null && existingConnectors.TryGetValue(geometry.Id, out var con))
            {
                con.ChangeBaseGeometry(geometry);
                connectors.Add(geometry.Id, con);
                existingConnectors.Remove(geometry.Id);
            }
            else
            {
                connectors.Add(geometry.Id, new NetworkNodeConnector(geometry, node, this));
            }

            //GeometryModel.Geometry.EndBatchOperation();
        }
        private void RemoveNode(SimFlowNetworkNode node)
        {
            if (node.RepresentationReference != GeometricReference.Empty)
            {
                if (node.RepresentationReference.FileId != this.GeometryModel.File.Key)
                    throw new Exception("Node is not connected to this geometry model");

                var geometry = this.GeometryModel.Geometry.GeometryFromId(node.RepresentationReference.GeometryId) as Vertex;
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

        private void UpdateEdges(SimFlowNetwork network, Dictionary<ulong, BaseNetworkConnector> existingConnectors)
        {
            foreach (var edge in network.ContainedEdges.Values)
                AddEdge(edge, existingConnectors);

            foreach (var subNet in network.ContainedFlowNetworks.Values)
                UpdateEdges(subNet, existingConnectors);
        }
        private void AddEdge(SimFlowNetworkEdge edge, Dictionary<ulong, BaseNetworkConnector> existingConnectors)
        {
            var startNode = edge.Start is SimFlowNetwork ? ((SimFlowNetwork)edge.Start).ConnectionToParentExitNode : edge.Start;
            var endNode = edge.End is SimFlowNetwork ? ((SimFlowNetwork)edge.End).ConnectionToParentEntryNode : edge.End;

            //Check if geometry for edge exists
            Polyline edgeGeometry = null;
            if (edge.RepresentationReference != GeometricReference.Empty)
                edgeGeometry = GeometryModel.Geometry.GeometryFromId(edge.RepresentationReference.GeometryId) as Polyline;

            if (edgeGeometry == null)
            {
                var startVertex = connectors[startNode.RepresentationReference.GeometryId].Geometry as Vertex;
                var endVertex = connectors[endNode.RepresentationReference.GeometryId].Geometry as Vertex;

                if (startVertex == null)
                    throw new Exception("Start vertex geometry not found");
                if (endVertex == null)
                    throw new Exception("End vertex geometry not found");

                var innerEdge = new Edge(this.GeometryModel.Geometry.Layers.First(), edge.Name,
                    new Vertex[] { startVertex, endVertex });
                edgeGeometry = new Polyline(this.GeometryModel.Geometry.Layers.First(), edge.Name,
                    new Edge[] { innerEdge });
            }

            if (existingConnectors != null && existingConnectors.TryGetValue(edgeGeometry.Id, out var con))
            {
                con.ChangeBaseGeometry(edgeGeometry);
                connectors.Add(edgeGeometry.Id, con);
                existingConnectors.Remove(edgeGeometry.Id);
            }
            else
                connectors.Add(edgeGeometry.Id, new NetworkEdgeConnector(edgeGeometry, edge));
        }
        private void RemoveEdge(SimFlowNetworkEdge edge)
        {
            if (edge.RepresentationReference != GeometricReference.Empty)
            {
                if (edge.RepresentationReference.FileId != this.GeometryModel.File.Key)
                    throw new Exception("Node is not connected to this geometry model");

                var geometry = this.GeometryModel.Geometry.GeometryFromId(edge.RepresentationReference.GeometryId) as Polyline;
                if (geometry != null)
                {
                    if (connectors.TryGetValue(geometry.Id, out var con))
                    {
                        con.Dispose();
                        connectors.Remove(geometry.Id);
                    }

                    GeometryModel.Geometry.StartBatchOperation();

                    //Remove edges
                    foreach (var pe in geometry.Edges)
                        pe.Edge.RemoveFromModel();
                    //Remove middle vertices
                    for (int i = 1; i < geometry.Edges.Count; i++)
                        geometry.Edges[i].StartVertex.RemoveFromModel();
                    //Remove polyline
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

        /// <summary>
        /// Frees all ressources created by this connector and detaches all event handler
        /// </summary>
        internal void Dispose()
        {
            GeometryModel.Geometry.GeometryChanged -= this.Geometry_GeometryChanged;
            GeometryModel.Geometry.TopologyChanged -= this.Geometry_TopologyChanged;

            DetachEvents(Network);

            foreach (var con in connectors.Values)
                con.Dispose();
        }
    }
}
