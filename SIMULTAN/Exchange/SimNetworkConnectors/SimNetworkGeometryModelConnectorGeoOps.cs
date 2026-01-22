using SIMULTAN.Data.Geometry;
using SIMULTAN.Data.SimMath;
using SIMULTAN.Data.SimNetworks;
using SIMULTAN.Utils;
using System.Collections.Generic;
using System.Linq;

namespace SIMULTAN.Exchange.SimNetworkConnectors
{



    /// <summary>
    /// Handles connections between a <see cref="SimNetwork"/> and a <see cref="GeometryModel"/>
    /// </summary>
    public partial class SimNetworkGeometryModelConnector
    {

        /// <summary>
        /// Contains dummy geometries. 
        /// Dummy geometries are used e.g.: <see cref="StartPartialNetworkOperation(BaseGeometry)"/> to represent temporal geometries during a move/rotate operation
        /// </summary>
        internal List<BaseGeometry> DummyGeometries { get; set; } = new List<BaseGeometry>();


        /// <summary>
        /// Returns all the elements of the static group
        /// </summary>
        /// <param name="geom">A BaseGeometry which might be the part of a StaticBlock</param>
        /// <param name="staticGroups">The static groups, if null, groups will be searched first</param>
        /// <returns>Returns all static group geometries connected to the geometries network element</returns>
        public IEnumerable<BaseGeometry> GetStaticGroupGeometries(BaseGeometry geom, List<HashSet<SimNetworkBlock>> staticGroups = null)
        {
            List<BaseGeometry> movingGeoms = new List<BaseGeometry>();
            staticGroups ??= FindStaticGroups();

            if (connectors.TryGetValue(geom.Id, out var connector))
            {
                // Static block
                if (connector is SimNetworkBlockConnector blockConnector && blockConnector.Block.IsStatic)
                {
                    if (staticGroups.TryFirstOrDefault(t => t.Contains(blockConnector.Block), out var staticGroup))
                    {
                        foreach (var block in staticGroup)
                        {
                            // add block geometry
                            if (connectors.TryGetValue(block.RepresentationReference.GeometryId, out var sConnector))
                            {
                                movingGeoms.Add(sConnector.Geometry);
                            }
                            // add port geometries
                            foreach (var port in block.Ports)
                            {
                                // add port geometry
                                if (connectors.TryGetValue(port.RepresentationReference.GeometryId, out var pConnector))
                                {
                                    movingGeoms.Add(pConnector.Geometry);
                                }
                                // add connection geometries
                                foreach (var con in port.Connections)
                                {
                                    if (connectors.TryGetValue(con.RepresentationReference.GeometryId, out var cConnector))
                                    {
                                        movingGeoms.Add(cConnector.Geometry);
                                    }
                                }
                            }
                        }
                    }
                }
                // Find and add port geometries
                if (connector is SimNetworkPortConnector portConnector)
                {
                    // search static groups of the port's parent block
                    if (connectors.TryGetValue(portConnector.Port.ParentNetworkElement.RepresentationReference.GeometryId, out var parentConnector))
                    {
                        movingGeoms.AddRange(GetStaticGroupGeometries(parentConnector.Geometry, staticGroups));
                    }
                }
                // for connections, search the static groups of the source and target blocks
                if (connector is SimNetworkConnectionConnector conConnector)
                {
                    foreach (var connection in conConnector.SimNetworkConnections)
                    {
                        // search static source block
                        if (connection.Source.ParentNetworkElement is SimNetworkBlock sourceBlock && sourceBlock.IsStatic)
                        {
                            if (connectors.TryGetValue(sourceBlock.RepresentationReference.GeometryId, out var parentConnector))
                            {
                                movingGeoms.AddRange(GetStaticGroupGeometries(parentConnector.Geometry, staticGroups));
                            }
                        }
                        // search static target block
                        if (connection.Target.ParentNetworkElement is SimNetworkBlock targetBlock && targetBlock.IsStatic)
                        {
                            if (connectors.TryGetValue(targetBlock.RepresentationReference.GeometryId, out var parentConnector))
                            {
                                movingGeoms.AddRange(GetStaticGroupGeometries(parentConnector.Geometry, staticGroups));
                            }
                        }
                    }
                }
            }
            return movingGeoms;
        }

        /// <summary>
        /// Tells whether the geometry is associated with a static block
        /// </summary>
        /// <param name="geom">The geometry</param>
        /// <returns>A boolean telling whether the geometry is included in a static block</returns>
        public bool IstAssociatedWithStaticBlock(BaseGeometry geom)
        {
            if (connectors.TryGetValue(geom.Id, out var connector))
            {
                if (connector is SimNetworkBlockPortConnectorProxy proxyCon)
                {
                    if (proxyCon.Port.ParentNetworkElement is SimNetworkBlock block)
                    {
                        return block.IsStatic;
                    }
                }
                else if (connector is SimNetworkBlockConnector bCon)
                {
                    return bCon.Block.IsStatic;
                }
                else if (connector is SimNetworkPortConnector portCon)
                {
                    if (portCon.Port.ParentNetworkElement is SimNetworkBlock block)
                    {
                        return block.IsStatic;
                    }
                }
                else if (connector is SimNetworkConnectionConnector conCon)
                {
                    if (conCon.SimNetworkConnections
                        .Any(c => c.Source.ParentNetworkElement is SimNetworkBlock bl && bl.IsStatic ||
                                  c.Target.ParentNetworkElement is SimNetworkBlock bl1 && bl1.IsStatic))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Function which returns the moved/rotated elements during a partial move/rotate operation. 
        /// Creates dummy Geometries for static-static connections
        /// Must call <see cref="RemoveDummyGeometries()"/> to remove these dummy geometry
        /// </summary>
        /// <returns>The geometry to be moved</returns>
        public IEnumerable<BaseGeometry> StartPartialNetworkOperation(BaseGeometry geom)
        {
            List<BaseGeometry> movingGeoms = new List<BaseGeometry>();
            if (connectors.TryGetValue(geom.Id, out var connector))
            {
                if (connector is SimNetworkBlockConnector blockConnector)
                {
                    // add the block vertex
                    movingGeoms.Add(geom);
                    foreach (var port in blockConnector.Block.Ports)
                    {
                        // if port has connection connector and has connections
                        if (connectors.TryGetValue(port.RepresentationReference.GeometryId, out var pConnector)
                            && port.Connections.Count > 0
                            && pConnector is SimNetworkConnectionConnector connectionCon)
                        {
                            var connectorChain = GetConnectionChain(port.Connections[0]);
                            var startPort = connectorChain[0].Source;
                            var endPort = connectorChain[connectorChain.Count - 1].Target;

                            // if its a connection between two static blocks, create dummy geometries
                            if (startPort != null && startPort.ParentNetworkElement is SimNetworkBlock startBlock && startBlock.IsStatic &&
                                endPort != null && endPort.ParentNetworkElement is SimNetworkBlock endBlock && endBlock.IsStatic)
                            {
                                var connectionVertex = connectionCon.Geometry as Vertex;
                                var blockVertex = blockConnector.Geometry as Vertex;

                                // port of the block is moved, but the other port stays in place cause its a partial operation
                                SimNetworkPort nonMovedPort = null;
                                if (port == startPort)
                                {
                                    nonMovedPort = endPort;
                                }
                                else
                                {
                                    nonMovedPort = startPort;
                                }

                                connectors.TryGetValue(nonMovedPort.ParentNetworkElement.RepresentationReference.GeometryId, out var nonMovedBlock);
                                connectors.TryGetValue(nonMovedPort.RepresentationReference.GeometryId, out var nonMovedPortCon);

                                //Creating dummy which will be moved
                                var movedVertex = new Vertex(connectionVertex.Layer, port.Name, connectionVertex.Position)
                                {
                                    Color = new DerivedColor(SimColors.Yellow)
                                };
                                movingGeoms.Add(movedVertex);
                                DummyGeometries.Add(movedVertex);

                                //Creating a  dummy for the non moved port
                                var nonMovedVertex = new Vertex(connectionVertex.Layer, port.Name, connectionVertex.Position)
                                {
                                    Color = new DerivedColor(SimColors.Yellow)
                                };
                                DummyGeometries.Add(nonMovedVertex);

                                //Dummy block to port proxy moved
                                var movingEdge = new Edge(connectionVertex.Layer, "DUMMY",
                                    new Vertex[] { blockVertex, movedVertex })
                                {
                                    Color = new DerivedColor(SimColors.Yellow)
                                };
                                DummyGeometries.Add(movingEdge);

                                //Dummy block to port proxy non-moved
                                var nonMovingEdge = new Edge(connectionVertex.Layer, "DUMMY",
                                    new Vertex[] { ((Vertex)nonMovedBlock.Geometry), nonMovedVertex })
                                {
                                    Color = new DerivedColor(SimColors.Yellow)
                                };
                                DummyGeometries.Add(nonMovingEdge);

                                // Edge connecting the moving and non-moving vertex
                                var connectionEdge = new Edge(connectionVertex.Layer, "DUMMY",
                                    new Vertex[] { movedVertex, nonMovedVertex })
                                {
                                    Color = new DerivedColor(SimColors.Yellow)
                                };
                                DummyGeometries.Add(connectionEdge);

                                //Clean the unused geometry
                                if (connectionVertex != null)
                                {
                                    RemoveUnusedVertex(connectionVertex);
                                }
                            }
                            else
                            {
                                movingGeoms.Add(pConnector.Geometry);
                            }

                        }
                        else if (connectors.TryGetValue(port.RepresentationReference.GeometryId, out var prtCon))
                        {
                            movingGeoms.Add(prtCon.Geometry);
                        }
                    }
                }
                if (connector is SimNetworkPortConnector portConnector)
                {
                    if (connectors.TryGetValue(portConnector.Port.ParentNetworkElement.RepresentationReference.GeometryId, out var parentConnector))
                    {
                        movingGeoms.AddRange(StartPartialNetworkOperation(parentConnector.Geometry));
                    }
                }
            }
            return movingGeoms;
        }

        /// <summary>
        /// Stops the partial transformation, removes the dummy geometries, updates according network geometry
        /// </summary>
        /// <param name="effectedGeoms">The geometries involved in the partial transformation <see cref="StartPartialNetworkOperation(BaseGeometry)"/></param>
        public void EndPartialTransform(List<BaseGeometry> effectedGeoms)
        {
            this.RemoveDummyGeometries();
            this.CleanUnusedGeometry();
            UpdateNetworkConnections(this.Network, null);
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
                else if (dummyGeom is Polyline poly)
                {
                    for (int j = poly.Edges.Count - 1; j >= 0; --j)
                    {
                        poly.Edges[j].Edge.RemoveFromModel();
                    }
                    poly.RemoveFromModel();
                }
                else if (dummyGeom is Vertex vertex)
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

    }
}

