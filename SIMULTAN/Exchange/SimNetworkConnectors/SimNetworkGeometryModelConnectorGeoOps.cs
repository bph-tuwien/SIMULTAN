using SIMULTAN.Data.Geometry;
using SIMULTAN.Data.SimMath;
using SIMULTAN.Data.SimNetworks;
using SIMULTAN.Utils;
using Sprache;
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
        /// Dummy geometries are used e.g.: <see cref="StartMoveRotatePartialOperation(BaseGeometry)"/> to represent temporal geometries during a move/rotate operation
        /// </summary>
        internal List<BaseGeometry> DummyGeometries { get; set; } = new List<BaseGeometry>();


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
                if (connector is SimNetworkConnectorConnector conConnector)
                {
                    foreach (var item in conConnector.SimNetworkConnectors)
                    {
                        if (item.Source.ParentNetworkElement is SimNetworkBlock block && block.IsStatic)
                        {
                            if (connectors.TryGetValue(item.Source.ParentNetworkElement.RepresentationReference.GeometryId, out var parentConnector))
                            {
                                geoms.AddRange(GetStaticGroupGeometries(parentConnector.Geometry));
                            }
                        }
                        if (item.Target.ParentNetworkElement is SimNetworkBlock block2 && block2.IsStatic)
                        {
                            if (connectors.TryGetValue(item.Source.ParentNetworkElement.RepresentationReference.GeometryId, out var parentConnector))
                            {
                                geoms.AddRange(GetStaticGroupGeometries(parentConnector.Geometry));
                            }
                        }
                    }

                }
            }
            return geoms;
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
                if (connector is SimNetworkBlockConnector bCon)
                {
                    return bCon.Block.IsStatic;
                }
                if (connector is SimNetworkPortConnector portCon)
                {
                    if (portCon.Port.ParentNetworkElement is SimNetworkBlock block)
                    {
                        return block.IsStatic;
                    }
                }
                if (connector is SimNetworkConnectorConnector conCon)
                {
                    if (conCon.SimNetworkConnectors
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
                            var connectorChain = FindConnectorChain(port.Connectors.First());
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
                                    Color = new DerivedColor(SimColors.Yellow)
                                };
                                geoms.Add(movedVertex);

                                //Creating a  dummy for the non moved port
                                var nonMovdVertex = new Vertex(pConnector.Geometry.Layer, port.Name, ((Vertex)connectorCon.Geometry).Position)
                                {
                                    Color = new DerivedColor(SimColors.Yellow)
                                };



                                //Dummy block to port proxy moved
                                var innerEdge = new Edge(this.GeometryModel.Geometry.Layers.First(), "DUMMY",

                                new Vertex[] { ((Vertex)blockConnector.Geometry), movedVertex })
                                {
                                    Color = new DerivedColor(SimColors.Yellow)
                                };

                                var blockToPortDummy1 = new Polyline(this.GeometryModel.Geometry.Layers.First(), "DUMMY",
                                new Edge[] { innerEdge })
                                {
                                    Color = new DerivedColor(SimColors.Yellow)
                                };



                                //Dummy block to port proxy non-moved
                                var innerEdge2 = new Edge(this.GeometryModel.Geometry.Layers.First(), "DUMMY",
                                new Vertex[] { ((Vertex)nonMovedBlock.Geometry), nonMovdVertex })
                                {
                                    Color = new DerivedColor(SimColors.Yellow)
                                };

                                var blockToPortDummy2 = new Polyline(this.GeometryModel.Geometry.Layers.First(), "DUMMY",
                                new Edge[] { innerEdge })
                                {
                                    Color = new DerivedColor(SimColors.Yellow)
                                };


                                var originalVertexPosition = new Vertex(pConnector.Geometry.Layer, port.Name, ((Vertex)pConnector.Geometry).Position)
                                {
                                    Color = new DerivedColor(SimColors.Yellow)
                                };
                                var nonValidConnectorEdge = new Edge(this.GeometryModel.Geometry.Layers.First(), "DUMMY",
                                new Vertex[] { movedVertex, nonMovdVertex })
                                {
                                    Color = new DerivedColor(SimColors.Yellow)
                                };

                                var invalidConnectionDumy = new Polyline(this.GeometryModel.Geometry.Layers.First(), "DUMMY",
                                new Edge[] { innerEdge2 })
                                {
                                    Color = new DerivedColor(SimColors.Yellow)
                                };



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
                        else if (connectors.TryGetValue(port.RepresentationReference.GeometryId, out var prtCon))
                        {
                            geoms.Add(prtCon.Geometry);
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
            this.CleanUnusedGeometry();
            UpdateNetworkConnectors(this.Network, null);
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

    }
}

