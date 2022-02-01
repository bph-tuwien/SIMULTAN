using SIMULTAN.Data.Assets;
using SIMULTAN.Data.Components;
using SIMULTAN.Data.FlowNetworks;
using SIMULTAN.Data.Geometry;
using SIMULTAN.Projects;
using SIMULTAN.Serializer.Geometry;
using SIMULTAN.Serializer.SimGeo;
using SIMULTAN.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Media.Media3D;

namespace SIMULTAN.Exchange
{
    /// <summary>
    /// An exception during the network conversion
    /// </summary>
    public class NetworkConvertException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the NetworkConvertException class
        /// </summary>
        /// <param name="message">The exception message</param>
        public NetworkConvertException(string message) : base(message) { }
    }


    /// <summary>
    /// Provides methods for converting/updating the geometry of a network
    /// </summary>
    public static class NetworkConverter
    {
        /// <summary>
        /// Converts a network into a GeometryModel
        /// </summary>
        /// <param name="network">The network</param>
        /// <param name="file">Target file for the GeometryModel file (can be a non-existing file)</param>
        /// <param name="componentExchange">The component exchange instance</param>
        /// <returns>A <see cref="GeometryModel"/> with the network geometry</returns>
        /// /// <param name="projectData">The model store in which the network geometry should be created</param>
        public static GeometryModel Convert(SimFlowNetwork network, FileInfo file, IComponentGeometryExchange componentExchange, ProjectData projectData)
        {
            if (!projectData.GeometryModels.TryGetGeometryModel(file, out var model, false))
            {
                GeometryModelData geometry = new GeometryModelData(componentExchange);
                model = new GeometryModel(Guid.NewGuid(), network.Name, file, OperationPermission.DefaultNetworkPermissions, geometry);
                SimGeoIO.Save(model, model.File, SimGeoIO.WriteMode.Plaintext);
            }


            //Create network
            componentExchange.NetworkCommunicator.AddGeometricRepresentationToNetwork(model, network, projectData);

            Update(model.Geometry, network, componentExchange.NetworkCommunicator, projectData);

            return model;
        }

        /// <summary>
        /// Links an already converted network to the existing geometry model and updates
        /// the model.
        /// </summary>
        /// <param name="network">the network</param>
        /// <param name="model">the model holding the network representation</param>
        /// <param name="componentExchange">the component exchange instance</param>
        /// <param name="projectData">The model store in which model is managed</param>
        public static void ReLinkAndUpdate(SimFlowNetwork network, GeometryModel model, IComponentGeometryExchange componentExchange, ProjectData projectData)
        {
            componentExchange.NetworkCommunicator.AddGeometricRepresentationToNetwork(model, network, projectData);
            Update(model.Geometry, network, componentExchange.NetworkCommunicator, projectData);
        }


        /// <summary>
        /// Updates GeometryModel from a network
        /// </summary>
        /// <param name="geometry">The <see cref="GeometryModelData"/> to update</param>
        /// <param name="network">The network</param>
        /// <param name="componentExchange">The component exchange instance</param>
        /// <param name="projectData">The model store in which model is managed</param>
        public static void Update(GeometryModelData geometry, SimFlowNetwork network, INetworkGeometryExchange componentExchange,
            ProjectData projectData)
        {
            int fileID = componentExchange.GetNetworkRepresentationFileIndex(geometry.Model.File);

            if (geometry.Layers.Count == 0) //No layer -> create default layer
            {
                geometry.Layers.Add(new Layer(geometry, "0"));
            }

            geometry.StartBatchOperation();

            var layer = geometry.Layers.First();

            var nodeDictionary = UpdateNodes(geometry, network, fileID, layer, projectData);
            var edgeDictionary = UpdateEdges(geometry, network, fileID, layer, nodeDictionary);

            geometry.EndBatchOperation();

            // restore parent-child relationships (when re-linking models)
            nodeDictionary.ForEach(x => RestoreParentsOf(x.Key, x.Value, componentExchange as NetworkGeometryExchange, projectData));
            edgeDictionary.ForEach(x => RestoreParentsOf(x.Key, x.Value, componentExchange as NetworkGeometryExchange, projectData));

            //Associate nodes
            foreach (var n in nodeDictionary)
                componentExchange.Associate(n.Key, n.Value);
            foreach (var e in edgeDictionary)
                componentExchange.Associate(e.Key, e.Value);
        }

        /// <summary>
        /// Reimports the assets of a given network element.
        /// </summary>
        /// <param name="geometry">The geometry data associated with the network element.</param>
        /// <param name="element">The Network element.</param>
        /// <param name="projectData">ProjectData used to cache imported geometry.</param>
        public static void ReimportNetworkElementAssets(GeometryModelData geometry, SimFlowNetworkElement element,
            ProjectData projectData)
        {
            Dictionary<string, SimMeshGeometryData> geometryCache = new Dictionary<string, SimMeshGeometryData>();

            ulong id = element.RepresentationReference.GeometryId;
            var bg = geometry.GeometryFromId(id) as Vertex;
            if (bg == null) // not a vertex
            {
                return;
            }
            List<ImportWarningMessage> messages = new List<ImportWarningMessage>();

            // Proxy geometry
            var meshAssets = element.Content == null ? null : element.Content.Component.ReferencedAssets.Where(x =>
            {
                if (x.Resource is ResourceFileEntry rfe)
                {
                    string extension = null;
                    if (rfe.ExtensionText == null)
                    {
                        if (rfe.Name.Contains("."))
                        {
                            extension = rfe.Name.Substring(rfe.Name.LastIndexOf('.') + 1);
                            if ((extension.Equals("obj") || extension.Equals("fbx")) && !rfe.Exists)
                            {
                                var message = new ImportWarningMessage(ImportWarningReason.FileNotFound, new object[] { rfe.Name });
                                if (!messages.Contains(message))
                                    messages.Add(message);
                                extension = null;
                            }
                        }
                    }
                    else
                    {
                        extension = rfe.ExtensionText;
                    }
                    return extension == null ? false : (extension.Equals("obj") || extension.Equals("fbx"));
                }
                else return false;
            });

            var proxy = geometry.ProxyGeometries.FirstOrDefault(x => x.Vertex == bg);
            if (meshAssets != null && meshAssets.Count() > 0)
            {
                var model_files = meshAssets.Select(x => new FileInfo(x.Resource.CurrentFullPath));
                proxy = ImportProxyGeometry(proxy, null, bg, model_files, projectData, messages);
            }
            else
            {
                ProxyShapeGenerator.UpdateCube(proxy, new Point3D(1, 1, 1));
            }

            if (proxy != null)
            {
                // update the size!
                proxy.Size = element.GetInstanceSize().Max;

                // update the rotation!
                var instance = element.GetUpdatedInstance(true);
                proxy.Rotation = instance != null ? instance.InstanceRotation : Quaternion.Identity;
            }


            if (messages.Count > 0)
                projectData.GeometryModels.OnImporterWarning(messages);
            bg.NotifyGeometryChanged();
        }

        private static ProxyGeometry ImportProxyGeometry(ProxyGeometry proxy, Layer layer, Vertex bg, IEnumerable<FileInfo> files,
            ProjectData projectData, IList<ImportWarningMessage> messages, GeometryNameFormatProvider nameProvider = null)
        {
            try
            {
                if (proxy == null)
                {
                    proxy = ProxyShapeGenerator.LoadModelsCombined(layer,
                        DefaultNameProvider.GeometryNameProviderOrDefault(typeof(ProxyGeometry), nameProvider),
                        bg, files, projectData);
                }
                else
                {
                    ProxyShapeGenerator.UpdateProxyGeometryCombined(proxy, files, projectData);
                }
            }
            catch (GeometryImporterException e)
            {
                var message = new ImportWarningMessage(ImportWarningReason.ImportFailed, new object[] { e.Message, e.InnerException.Message });
                if (!messages.Contains(message))
                {
                    messages.Add(message);
                }
            }
            catch (FileNotFoundException e)
            {
                var message = new ImportWarningMessage(ImportWarningReason.FileNotFound, new object[] { e.Message });
                if (!messages.Contains(message))
                {
                    messages.Add(message);
                }
            }

            return proxy;
        }

        private static Dictionary<SimFlowNetworkNode, Vertex> UpdateNodes(GeometryModelData geometry, SimFlowNetwork network, int fileID, Layer layer, ProjectData projectData)
        {
            List<ulong> all_representing_ids = GetAllRepresentationIds(network);
            Dictionary<SimFlowNetworkNode, Vertex> result = new Dictionary<SimFlowNetworkNode, Vertex>();
            Dictionary<string, SimMeshGeometryData> geometryCache = new Dictionary<string, SimMeshGeometryData>();
            List<ImportWarningMessage> messages = new List<ImportWarningMessage>();

            List<ulong> checked_ids = new List<ulong>();
            List<SimFlowNetworkNode> nested_nodes = network.GetNestedNodes();
            foreach (var node in nested_nodes) // network.ContainedNodes.Values
            {
                ulong id = node.RepresentationReference.GeometryId;

                //Check if node already exists
                var bg = geometry.GeometryFromId(id) as Vertex;
                if (bg == null)
                {
                    bg = new Vertex(layer, node.Name, new Point3D(node.Position.X / 100.0, 0, node.Position.Y / 100.0));
                }
                checked_ids.Add(bg.Id);

                // Proxy geometry
                var meshAssets = node.Content == null ? null : node.Content.Component.ReferencedAssets.Where(x =>
                {
                    if (x.Resource is ResourceFileEntry rfe)
                    {
                        string extension = null;
                        if (rfe.ExtensionText == null)
                        {
                            if (rfe.Name.Contains("."))
                            {
                                extension = rfe.Name.Substring(rfe.Name.LastIndexOf('.') + 1);
                                if ((extension.Equals("obj") || extension.Equals("fbx")) && !rfe.Exists)
                                {
                                    var message = new ImportWarningMessage(ImportWarningReason.FileNotFound, new object[] { rfe.Name });
                                    if (!messages.Contains(message))
                                        messages.Add(message);
                                    extension = null;
                                }
                            }
                        }
                        else
                        {
                            extension = rfe.ExtensionText;
                        }
                        return extension == null ? false : (extension.Equals("obj") || extension.Equals("fbx"));
                    }
                    else return false;
                });

                var proxy = geometry.ProxyGeometries.FirstOrDefault(x => x.Vertex == bg);
                if (meshAssets != null && meshAssets.Count() > 0)
                {
                    var model_files = meshAssets.Select(x => new FileInfo(x.Resource.CurrentFullPath));
                    proxy = ImportProxyGeometry(proxy, layer, bg, model_files, projectData, messages);
                }
                else
                {
                    if (proxy == null)
                    {
                        proxy = ProxyShapeGenerator.GenerateCube(layer, node.Name, bg, new Point3D(1, 1, 1));
                    }
                    else
                    {
                        ProxyShapeGenerator.UpdateCube(proxy, new Point3D(1, 1, 1));
                    }
                }

                if (proxy != null)
                {
                    // update the size!
                    proxy.Size = node.GetInstanceSize().Max;

                    // update the rotation!
                    var instance = node.GetUpdatedInstance(true);
                    proxy.Rotation = instance != null ? instance.InstanceRotation : Quaternion.Identity;
                }

                result.Add(node, bg);
            }

            // check for deleted elements -> delete representing geometry            
            var orphans = geometry.Vertices.Where(x => !checked_ids.Contains(x.Id));
            List<BaseGeometry> to_delete = GetIncidentToVertices(orphans, all_representing_ids);
            to_delete.ForEach(x => x.RemoveFromModel());

            if (messages.Count > 0)
                projectData.GeometryModels.OnImporterWarning(messages);

            return result;
        }

        private static Dictionary<SimFlowNetworkEdge, Polyline> UpdateEdges(GeometryModelData geometry, SimFlowNetwork network, int fileID, Layer layer,
            Dictionary<SimFlowNetworkNode, Vertex> nodeLookup)
        {
            Dictionary<SimFlowNetworkEdge, Polyline> result = new Dictionary<SimFlowNetworkEdge, Polyline>();

            List<ulong> checked_ids = new List<ulong>();
            List<SimFlowNetworkEdge> edges = network.GetNestedEdges();
            foreach (var netEdge in edges) // network.ContainedEdges.Values
            {
                ulong id = netEdge.RepresentationReference.GeometryId;

                SimFlowNetworkNode netEdge_Start = (netEdge.Start is SimFlowNetwork) ? (netEdge.Start as SimFlowNetwork).ConnectionToParentExitNode : netEdge.Start;
                SimFlowNetworkNode netEdge_End = (netEdge.End is SimFlowNetwork) ? (netEdge.End as SimFlowNetwork).ConnectionToParentEntryNode : netEdge.End;
                var v1 = nodeLookup[netEdge_Start];
                var v2 = nodeLookup[netEdge_End];

                //Check if edge exists. If not -> create
                var pl = geometry.GeometryFromId(id) as Polyline;
                if (pl == null)
                {
                    Edge edge = new Edge(layer, netEdge.Name, new Vertex[] { v1, v2 });
                    pl = new Polyline(layer, netEdge.Name, new Edge[] { edge });
                }
                else
                {
                    // Make sure that correct vertices are connected
                    List<Vertex> pl_vs = GetOrderedVerticesOfPolyline(pl);

                    // synchronize positions
                    if (pl_vs.First() == v1 && pl_vs.Last() != v2)
                    {
                        Edge e_to_sync = pl.Edges.Last().Edge;
                        Vertex fixed_v = pl.Edges.Last().StartVertex;
                        int index_to_sync = (e_to_sync.Vertices[0].Id == fixed_v.Id) ? 1 : 0;
                        e_to_sync.Vertices[index_to_sync].Edges.Remove(e_to_sync);
                        e_to_sync.Vertices[index_to_sync] = v2;
                    }
                    else if (pl_vs.First() == v2 && pl_vs.Last() != v1)
                    {
                        Edge e_to_sync = pl.Edges.Last().Edge;
                        Vertex fixed_v = pl.Edges.Last().StartVertex;
                        int index_to_sync = (e_to_sync.Vertices[0].Id == fixed_v.Id) ? 1 : 0;
                        e_to_sync.Vertices[index_to_sync].Edges.Remove(e_to_sync);
                        e_to_sync.Vertices[index_to_sync] = v1;
                    }

                    if (pl_vs.First() != v1 && pl_vs.Last() == v2)
                    {
                        Edge e_to_sync = pl.Edges.First().Edge;
                        Vertex v_to_synch = pl.Edges.First().StartVertex;
                        int index_to_sync = (e_to_sync.Vertices[0].Id == v_to_synch.Id) ? 0 : 1;
                        e_to_sync.Vertices[index_to_sync].Edges.Remove(e_to_sync);
                        e_to_sync.Vertices[index_to_sync] = v1;
                    }
                    else if (pl_vs.First() != v2 && pl_vs.Last() == v1)
                    {
                        Edge e_to_sync = pl.Edges.First().Edge;
                        Vertex v_to_synch = pl.Edges.First().StartVertex;
                        int index_to_sync = (e_to_sync.Vertices[0].Id == v_to_synch.Id) ? 0 : 1;
                        e_to_sync.Vertices[index_to_sync].Edges.Remove(e_to_sync);
                        e_to_sync.Vertices[index_to_sync] = v2;
                    }
                }
                checked_ids.Add(pl.Id);

                result.Add(netEdge, pl);
            }

            // check for deleted elements -> delete the representing geometry
            var orphans = geometry.Polylines.Where(x => !checked_ids.Contains(x.Id));
            List<BaseGeometry> to_delete = GetIncidentToPolylines(orphans, nodeLookup.Values.Select(x => x.Id));
            to_delete.ForEach(x => x.RemoveFromModel());

            // check for orphaned vertices
            if (edges.Count > 0)
            {
                var lone_vertices = geometry.Vertices.Where(x => x.Edges.Count == 0).ToList();
                //lone_vertices.ForEach(x => x.RemoveFromModel());
                foreach (Vertex lv in lone_vertices)
                {
                    lv.ProxyGeometries.ForEach(x => x.RemoveFromModel());
                    lv.RemoveFromModel();
                    foreach (var entry in nodeLookup.Where(x => x.Value.Id == lv.Id).ToList())
                    {
                        nodeLookup.Remove(entry.Key);
                    }
                }
            }

            return result;
        }


        #region UTILS: parent - child

        private static void RestoreParentsOf(SimFlowNetworkElement element, BaseGeometry representation, NetworkGeometryExchange exchange, ProjectData projectData)
        {
            if (element == null || representation == null || exchange == null) return;
            if (representation.Parent != null) return;

            var instance = element.GetUpdatedInstance(true);
            if (instance != null)
            {
                var component_side_ref = (SimInstancePlacementGeometry)instance.Placements.FirstOrDefault(x => x is SimInstancePlacementGeometry gp);
                if (component_side_ref != null && component_side_ref.IsValid)
                {
                    BaseGeometry parent = exchange.MainGeometryExchange.GeometryManager.GetGeometryFromId(component_side_ref.FileId, component_side_ref.GeometryId);
                    if (parent != null)
                    {
                        representation.Parent = new GeometryReference(parent, projectData.GeometryModels);
                    }
                }
            }
        }

        #endregion

        #region UTILS: query geometry

        internal static List<BaseGeometry> GetIncidentToVertices(IEnumerable<Vertex> vertices, IEnumerable<ulong> exceptions)
        {
            List<BaseGeometry> incident = new List<BaseGeometry>();
            foreach (Vertex ov in vertices)
            {
                List<BaseGeometry> ov_to_delete = GeometryModelAlgorithms.GetAllContainingGeometries(new BaseGeometry[] { ov });
                List<Polyline> polylines_to_delete = ov_to_delete.Where(x => x is Polyline && !exceptions.Contains(x.Id)).Select(x => x as Polyline).ToList();
                List<Polyline> polylines_to_keep = ov_to_delete.Where(x => x is Polyline && exceptions.Contains(x.Id)).Select(x => x as Polyline).ToList();
                if (polylines_to_delete.Count > 0)
                {
                    var polylines_children_to_delete = polylines_to_delete.SelectMany(x => GetChildrenOfPolyline(x, new List<Vertex> { ov })).ToList();
                    ov_to_delete.AddRange(polylines_children_to_delete);
                }
                if (polylines_to_keep.Count > 0)
                {
                    var polylines_children_to_keep = polylines_to_keep.SelectMany(x => GetChildrenOfPolyline(x, new List<Vertex> { ov })).ToList();
                    polylines_children_to_keep.ForEach(x => ov_to_delete.Remove(x));
                    polylines_to_keep.ForEach(x => ov_to_delete.Remove(x));
                }
                incident.AddRange(ov_to_delete);
            }
            incident = incident.Distinct().ToList();
            return incident;
        }

        internal static List<BaseGeometry> GetIncidentToPolylines(IEnumerable<Polyline> polylines, IEnumerable<ulong> exceptions)
        {
            List<BaseGeometry> incident = new List<BaseGeometry>(polylines);
            foreach (Polyline opl in polylines)
            {
                List<Vertex> opl_vs = GetOrderedVerticesOfPolyline(opl);
                List<Vertex> ends_to_remove = new List<Vertex>();
                if (!exceptions.Contains(opl_vs.First().Id))
                    ends_to_remove.Add(opl_vs.First());
                if (!exceptions.Contains(opl_vs.Last().Id))
                    ends_to_remove.Add(opl_vs.Last());
                var opl_children_to_delete = GetChildrenOfPolyline(opl, ends_to_remove);

                incident.AddRange(opl_children_to_delete);
            }
            incident = incident.Distinct().ToList();
            return incident;
        }

        private static List<BaseGeometry> GetChildrenOfPolyline(Polyline polyline, List<Vertex> includedEnds)
        {
            List<BaseGeometry> children = new List<BaseGeometry>();

            List<Edge> edges = new List<Edge>(polyline.Edges.Select(x => x.Edge));

            List<Vertex> vertices = polyline.Edges.Select(x => x.StartVertex).ToList();
            Vertex last_v = polyline.Edges.Last().Edge.Vertices.FirstOrDefault(x => x.Id != vertices.Last().Id);
            if (last_v != null && includedEnds.Contains(last_v))
                vertices.Add(last_v);
            if (!includedEnds.Contains(vertices[0]))
                vertices.RemoveAt(0);

            children.AddRange(vertices);
            children.AddRange(edges);

            return children;
        }

        internal static List<Vertex> GetOrderedVerticesOfPolyline(Polyline polyline)
        {
            List<Vertex> vertices = polyline.Edges.Select(x => x.StartVertex).ToList();
            Vertex last_v = polyline.Edges.Last().Edge.Vertices.FirstOrDefault(x => x.Id != vertices.Last().Id);
            if (last_v != null)
                vertices.Add(last_v);

            return vertices;
        }

        #endregion

        #region UTILS: query networks

        private static List<ulong> GetAllRepresentationIds(SimFlowNetwork network)
        {
            var node_ids = network.ContainedNodes.Values.Select(x => x.RepresentationReference.GeometryId);
            var edge_ids = network.ContainedEdges.Values.Select(x => x.RepresentationReference.GeometryId);
            //var nw_ids = network.ContainedFlowNetworks.Values.Select(x => x.RepresentationReference.GeometryId);

            List<ulong> all_ids = new List<ulong>();
            foreach (SimFlowNetwork sNw in network.ContainedFlowNetworks.Values)
            {
                List<ulong> sNw_all_ids = NetworkConverter.GetAllRepresentationIds(sNw);
                all_ids.AddRange(sNw_all_ids);
            }

            all_ids.AddRange(node_ids);
            all_ids.AddRange(edge_ids);
            //all_ids.AddRange(nw_ids);

            return all_ids;
        }

        #endregion
    }
}
