using SIMULTAN.Data.Components;
using SIMULTAN.Data.Geometry;
using SIMULTAN.Data.Users;
using SIMULTAN.Exchange.Connectors;
using SIMULTAN.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace SIMULTAN.Exchange.ConnectorInteraction
{
    /// <summary>
    /// Static class containing algorithms performed on multiple connectors.
    /// Creation and removal of parallel hierarchies between components and geometry.
    /// Geometry queries, e.g., search for neighborhood and part-of relationships.
    /// </summary>
    internal static class ConnectorAlgorithms
    {
        #region COMPONENT CREATION: based on geometry

        /// <summary>
        /// Attaches a hierarchy of automatically generated components to _comp.
        /// The 3d representations is its direct child, as are the 2d faces of the volume.
        /// Each 2d representation can contain 2d representations of faces within faces.
        /// The 1d and 0d representations (of edges and vertices) are attached to _comp as flat lists.
        /// </summary>
        /// <param name="_comp_factory">the component manager</param>
        /// <param name="_comp">the component at the root of the hierarchy, associated with the volume as architectural space</param>
        /// <param name="_vol">the given volume</param>
        /// <param name="_included_dimensions">if 2 is included, attaches all faces; if 1 is included, attaches all edges; if 0 is included, attaches all vertices</param>
        /// <returns></returns>
        public static ComponentGeometryContainer CreateParallelHierarchy(SimComponentCollection _comp_factory, SimComponent _comp, Volume _vol, params int[] _included_dimensions)
        {
            ComponentGeometryContainer root = new ComponentGeometryContainer { Data = _comp, Geometry = _vol, Childen = new List<ComponentGeometryContainer>() };
            if (_comp_factory == null || _comp == null || _vol == null) return root;

            // include 3D elements
            if (_included_dimensions != null && _included_dimensions.Contains(3))
            {
                SimComponent comp_3d = AttachAutomaticSubRepresentation(_comp_factory, _comp, _vol.Name, 3);
                root.Childen.Add(new ComponentGeometryContainer { Data = comp_3d, Geometry = _vol, Childen = new List<ComponentGeometryContainer>() });
            }
            List<ulong> excluded_geom_ids = new List<ulong>();
            excluded_geom_ids.Add(_vol.Id);

            // include 2D elements
            if (_included_dimensions != null && _included_dimensions.Contains(2))
            {
                foreach (PFace pf in _vol.Faces)
                {
                    Face f = pf.Face;
                    ConnectorAlgorithms.Attach2DHierarchyToNode(_comp_factory, root, f, ref excluded_geom_ids);
                }
            }

            // include 1D elements
            if (_included_dimensions != null && _included_dimensions.Contains(1))
            {
                ConnectorAlgorithms.Attach1DHierarchyToNode(_comp_factory, root, _vol, ref excluded_geom_ids);
            }

            // include 0D elements
            if (_included_dimensions != null && _included_dimensions.Contains(0))
            {
                ConnectorAlgorithms.Attach0DHierarchyToNode(_comp_factory, root, _vol, ref excluded_geom_ids);
            }

            return root;
        }

        /// <summary>
        /// Extracts a collection w/o duplicates of geometry involved in the straucture created by the method
        /// CreateParallelHierarchy(ComponentFactory, Component, Volume, params int[]).
        /// </summary>
        /// <param name="_root">the root of the CompGeomNode structure</param>
        /// <returns>a flat collection of the involved geometry</returns>
        internal static IEnumerable<BaseGeometry> ExtractFlatGeometryFromParallelHierarchy(ComponentGeometryContainer _root)
        {
            List<BaseGeometry> geometry = new List<BaseGeometry>();
            geometry.Add(_root.Geometry);
            foreach (var child in _root.Childen)
            {
                geometry.AddRange(ConnectorAlgorithms.ExtractFlatGeometryFromParallelHierarchy(child));
            }
            return geometry.Distinct();
        }

        /// <summary>
        /// Attaches a tree of 2d representations to a component - geometry (e.g. volume) parent pair.
        /// </summary>
        /// <param name="_comp_factory">component manager and creator</param>
        /// <param name="_parent_node">contains the parent component and parent geometry</param>
        /// <param name="_face">the specific face to be attached</param>
        /// <param name="_excluded_geom_ids">to avoid duplicates</param>
        private static void Attach2DHierarchyToNode(SimComponentCollection _comp_factory, ComponentGeometryContainer _parent_node, Face _face, ref List<ulong> _excluded_geom_ids)
        {
            if (_comp_factory == null || _parent_node.Data == null || _face == null) return;
            if (_parent_node.Childen == null)
                _parent_node.Childen = new List<ComponentGeometryContainer>();
            if (_excluded_geom_ids == null)
                _excluded_geom_ids = new List<ulong>();
            if (_excluded_geom_ids.Contains(_face.Id)) return;

            // 1. create the node, based on the Face, to attach
            SimComponent comp_face = AttachAutomaticSubRepresentation(_comp_factory, _parent_node.Data, _face.Name, 2);
            ComponentGeometryContainer node_face = new ComponentGeometryContainer { Data = comp_face, Geometry = _face, Childen = new List<ComponentGeometryContainer>() };
            _excluded_geom_ids.Add(_face.Id);

            // 2. recursion -> create sub-nodes for contained faces (e.g. of windows in a wall)
            foreach (EdgeLoop el in _face.Holes)
            {
                if (el.Faces != null && el.Faces.Count > 1)
                {
                    foreach (Face h in el.Faces)
                    {
                        ConnectorAlgorithms.Attach2DHierarchyToNode(_comp_factory, node_face, h, ref _excluded_geom_ids);
                    }
                }
                else
                {
                    // create sub-components for holes (added 05.12.2018)
                    SimComponent comp_el = AttachAutomaticSubRepresentation(_comp_factory, comp_face, el.Name + " Void", 2);
                    ComponentGeometryContainer node_el = new ComponentGeometryContainer { Data = comp_el, Geometry = el, Childen = new List<ComponentGeometryContainer>() };
                    _excluded_geom_ids.Add(el.Id);
                    node_face.Childen.Add(node_el);
                }
            }

            // 3. attach the newly created node to the parent node           
            _parent_node.Childen.Add(node_face);
        }

        /// <summary>
        /// Attaches a flat list of 1d representations to a component - geometry (e.g. volume) parent pair.
        /// </summary>
        /// <param name="_comp_factory">component manager and creator</param>
        /// <param name="_parent_node">contains the parent component and parent geometry</param>
        /// <param name="_vol">the volume, whose edges are to be attached</param>
        /// <param name="_excluded_geom_ids">to avoid duplicates</param>
        private static void Attach1DHierarchyToNode(SimComponentCollection _comp_factory, ComponentGeometryContainer _parent_node, Volume _vol, ref List<ulong> _excluded_geom_ids)
        {
            if (_comp_factory == null || _parent_node.Data == null || _vol == null) return;
            if (_parent_node.Childen == null)
                _parent_node.Childen = new List<ComponentGeometryContainer>();
            if (_excluded_geom_ids == null)
                _excluded_geom_ids = new List<ulong>();

            // 1. create the nodes, based on the edges in the EdgeLoop, and attach them to the parent node
            Dictionary<ulong, Edge> all_es = ConnectorAlgorithms.GetAllEdgesOf(_vol);

            // 2. create the nodes and attach them to the parent node
            foreach (var entry in all_es)
            {
                if (_excluded_geom_ids.Contains(entry.Key)) continue;

                SimComponent comp_e = AttachAutomaticSubRepresentation(_comp_factory, _parent_node.Data, entry.Value.Name, 0);
                ComponentGeometryContainer node_e = new ComponentGeometryContainer { Data = comp_e, Geometry = entry.Value, Childen = new List<ComponentGeometryContainer>() };
                _excluded_geom_ids.Add(entry.Key);
                _parent_node.Childen.Add(node_e);
            }
        }

        /// <summary>
        /// Attaches a flat list of 0d representations to a component - geometry (e.g. volume) parent pair.
        /// </summary>
        /// <param name="_comp_factory">component manager and creator</param>
        /// <param name="_parent_node">contains the parent component and parent geometry</param>
        /// <param name="_vol">the volume, whose vertices are to be attached</param>
        /// <param name="_excluded_geom_ids">to avoid duplicates</param>
        private static void Attach0DHierarchyToNode(SimComponentCollection _comp_factory, ComponentGeometryContainer _parent_node, Volume _vol, ref List<ulong> _excluded_geom_ids)
        {
            if (_comp_factory == null || _parent_node.Data == null || _vol == null) return;
            if (_parent_node.Childen == null)
                _parent_node.Childen = new List<ComponentGeometryContainer>();
            if (_excluded_geom_ids == null)
                _excluded_geom_ids = new List<ulong>();

            // 1. extract all vertices
            Dictionary<ulong, Vertex> all_vs = ConnectorAlgorithms.GetAllVerticesOf(_vol);

            // 2. create the nodes and attach them to the parent node
            foreach (var entry in all_vs)
            {
                if (_excluded_geom_ids.Contains(entry.Key)) continue;

                SimComponent comp_v = AttachAutomaticSubRepresentation(_comp_factory, _parent_node.Data, entry.Value.Name, 0);
                ComponentGeometryContainer node_v = new ComponentGeometryContainer { Data = comp_v, Geometry = entry.Value, Childen = new List<ComponentGeometryContainer>() };
                _excluded_geom_ids.Add(entry.Key);
                _parent_node.Childen.Add(node_v);
            }
        }

        #endregion

        #region COMPONENT REMOVAL: based on geometry

        /// <summary>
        /// Detaches the hierarchy of automatically generated components from _comp.
        /// </summary>
        /// <param name="_comp_factory">the component manager</param>
        /// <param name="_comp">the component at the root of the hierarchy, associated with the volume as architectural space</param>
        /// <param name="_vol">the given volume</param>
        /// <param name="_resource_id_of_geom_file">the id of the file containing the volume</param>
        /// <param name="_included_dimensions">if 2 is included, detaches all faces; if 1 is included, detaches all edges; if 0 is included, detaches all vertices</param>
        public static void RemoveParallelHierarchy(SimComponentCollection _comp_factory, SimComponent _comp, Volume _vol, int _resource_id_of_geom_file, params int[] _included_dimensions)
        {
            if (_comp_factory == null || _comp == null || _vol == null) return;

            // remove 3D elements
            if (_included_dimensions != null && _included_dimensions.Contains(3))
            {
                SimComponent comp_3d = _comp.Components.FirstOrDefault(
                    x => x.Component != null && x.Component.IsAutomaticallyGenerated &&
                         x.Component.Instances[0].Placements.Any(p => p is SimInstancePlacementGeometry pg &&
                         pg.FileId == _resource_id_of_geom_file && pg.GeometryId == _vol.Id))?.Component;

                _comp.Components.Remove(comp_3d.ParentContainer);
            }
            List<ulong> excluded_geom_ids = new List<ulong>();
            excluded_geom_ids.Add(_vol.Id);

            // remove 2D elements
            if (_included_dimensions != null && _included_dimensions.Contains(2))
            {
                foreach (PFace pf in _vol.Faces)
                {
                    Face f = pf.Face;
                    ConnectorAlgorithms.Detach2DHierarchyFromNode(_comp_factory, _comp, f, _resource_id_of_geom_file, ref excluded_geom_ids);
                }
            }

            // remove 1D elements
            if (_included_dimensions != null && _included_dimensions.Contains(1))
            {
                ConnectorAlgorithms.Detach1DHierarchyFromNode(_comp_factory, _comp, _vol, _resource_id_of_geom_file, ref excluded_geom_ids);
            }

            // remove 0D elements
            if (_included_dimensions != null && _included_dimensions.Contains(0))
            {
                ConnectorAlgorithms.Detach0DHierarchyFromNode(_comp_factory, _comp, _vol, _resource_id_of_geom_file, ref excluded_geom_ids);
            }
        }

        private static void Detach2DHierarchyFromNode(SimComponentCollection _comp_factory, SimComponent _root, Face _face, int _resource_id_of_geom_file, ref List<ulong> _excluded_geom_ids)
        {
            if (_comp_factory == null || _root == null || _face == null) return;
            if (_excluded_geom_ids == null)
                _excluded_geom_ids = new List<ulong>();
            if (_excluded_geom_ids.Contains(_face.Id)) return;

            // 1. find the corresponding sub-component

            SimComponent corresponding = _root.Components.FirstOrDefault(
                x => x.Component != null && x.Component.Instances[0].Placements.Any(p => p is SimInstancePlacementGeometry pg &&
                pg.FileId == _resource_id_of_geom_file && pg.GeometryId == _face.Id))?.Component;
            _excluded_geom_ids.Add(_face.Id);

            // 2. recursion -> remove sub-nodes for contained faces (e.g. of windows in a wall)
            foreach (EdgeLoop el in _face.Holes)
            {
                if (el.Faces.Count > 1)
                {
                    foreach (Face h in el.Faces)
                    {
                        ConnectorAlgorithms.Detach2DHierarchyFromNode(_comp_factory, corresponding, h, _resource_id_of_geom_file, ref _excluded_geom_ids);
                    }
                }
                else
                {
                    SimComponent el_corresponding = corresponding.Components.FirstOrDefault(
                        x => x.Component != null && x.Component.Instances[0].Placements.Any(p => p is SimInstancePlacementGeometry pg &&
                        pg.FileId == _resource_id_of_geom_file && pg.GeometryId == el.Id))?.Component;

                    corresponding.Components.Remove(el_corresponding.ParentContainer);
                    _excluded_geom_ids.Add(el.Id);
                }
            }

            // 3. remove the component attached to the Face (along with Faces / EdgeLoops sharing its Boundary of Holes)       
            _root.Components.Remove(corresponding.ParentContainer);
        }

        private static void Detach1DHierarchyFromNode(SimComponentCollection _comp_factory, SimComponent _root, Volume _vol, int _resource_id_of_geom_file, ref List<ulong> _excluded_geom_ids)
        {
            if (_comp_factory == null || _root == null || _vol == null) return;
            if (_excluded_geom_ids == null)
                _excluded_geom_ids = new List<ulong>();

            // 1. find the edges in the EdgeLoop
            Dictionary<ulong, Edge> all_es = ConnectorAlgorithms.GetAllEdgesOf(_vol);

            // 2. remove the corresponding sub-components from the root component
            foreach (var entry in all_es)
            {
                if (_excluded_geom_ids.Contains(entry.Key)) continue;

                SimComponent corresponding = _root.Components.FirstOrDefault(
                    x => x.Component != null && x.Component.Instances[0].Placements.Any(p => p is SimInstancePlacementGeometry pg &&
                pg.FileId == _resource_id_of_geom_file && pg.GeometryId == entry.Key))?.Component;
                _root.Components.Remove(corresponding.ParentContainer);
                _excluded_geom_ids.Add(entry.Key);
            }
        }

        private static void Detach0DHierarchyFromNode(SimComponentCollection _comp_factory, SimComponent _root, Volume _vol, int _resource_id_of_geom_file, ref List<ulong> _excluded_geom_ids)
        {
            if (_comp_factory == null || _root == null || _vol == null) return;
            if (_excluded_geom_ids == null)
                _excluded_geom_ids = new List<ulong>();

            // 1. extract all vertices
            Dictionary<ulong, Vertex> all_vs = ConnectorAlgorithms.GetAllVerticesOf(_vol);

            // 2. remove the corresponding sub-components from the root component
            foreach (var entry in all_vs)
            {
                if (_excluded_geom_ids.Contains(entry.Key)) continue;

                SimComponent corresponding = _root.Components.FirstOrDefault(
                    x => x.Component != null && x.Component.Instances[0].Placements.Any(p => p is SimInstancePlacementGeometry pg &&
                pg.FileId == _resource_id_of_geom_file && pg.GeometryId == entry.Key))?.Component;
                _root.Components.Remove(corresponding.ParentContainer);
                _excluded_geom_ids.Add(entry.Key);
            }
        }

        #endregion

        #region UTILS: geometry info

        /// <summary>
        /// Extracts a dictionary of all geoemtry comprising the volume, including the volume itself.
        /// Included are faces, edges and vertices. Also included are edge loops that are holes in a face, but 
        /// not boundary of another.
        /// </summary>
        /// <param name="_vol">the given volume</param>
        /// <returns>key = id of the geometry, value = the geometry itself</returns>
        public static Dictionary<ulong, BaseGeometry> GetAllGeometricPartsOf(Volume _vol)
        {
            if (_vol == null) return null;

            // 3d
            Dictionary<ulong, BaseGeometry> all_parts = new Dictionary<ulong, BaseGeometry> { { _vol.Id, _vol } };
            // 2d
            foreach (PFace pf in _vol.Faces)
            {
                all_parts.Add(pf.Face.Id, pf.Face);
                Dictionary<ulong, EdgeLoop> all_holes_of_the_face = ConnectorAlgorithms.GetAllHolesIn(pf.Face);
                foreach (var h in all_holes_of_the_face)
                {
                    all_parts.Add(h.Key, h.Value);
                }
                Dictionary<ulong, Face> all_contained_faces = ConnectorAlgorithms.GetAllContainedFacesIn(pf.Face);
                foreach (var cf in all_contained_faces)
                {
                    all_parts.Add(cf.Key, cf.Value);
                }
            }
            // 1d, 0d
            Dictionary<ulong, Edge> edges = ConnectorAlgorithms.GetAllEdgesOf(_vol);
            Dictionary<ulong, Vertex> vertices = ConnectorAlgorithms.GetAllVerticesOf(_vol);
            foreach (var e in edges)
            {
                all_parts.Add(e.Key, e.Value);
            }
            foreach (var v in vertices)
            {
                all_parts.Add(v.Key, v.Value);
            }

            return all_parts;
        }

        /// <summary>
        /// Finds all faces *without* their sub-faces of the given volume.
        /// </summary>
        /// <param name="_volume">the given volume</param>
        /// <returns>key = id of the face, value = the face itself</returns>
        [Obsolete("Doesn't do anything than returning the volume's face list")]
        public static Dictionary<ulong, Face> GetAllFacesWoSubFaces(Volume _volume)
        {
            Dictionary<ulong, Face> faces = new Dictionary<ulong, Face>();
            foreach (PFace pf in _volume.Faces)
            {
                faces.Add(pf.Face.Id, pf.Face);
            }
            return faces;
        }

        /// <summary>
        /// Finds all faces and sub-faces of the given volume.
        /// </summary>
        /// <param name="_volume">the given volume</param>
        /// <returns>key = id of the face, value = the face itself</returns>
        public static Dictionary<ulong, Face> GetAllFacesWSubFaces(Volume _volume)
        {
            Dictionary<ulong, Face> all_faces = new Dictionary<ulong, Face>();
            foreach (PFace pf in _volume.Faces)
            {
                all_faces.Add(pf.Face.Id, pf.Face);
                Dictionary<ulong, Face> all_contained_faces = ConnectorAlgorithms.GetAllContainedFacesIn(pf.Face);
                foreach (var cf in all_contained_faces)
                {
                    if (!(all_faces.ContainsKey(cf.Key)))
                        all_faces.Add(cf.Key, cf.Value);
                }
            }

            return all_faces;
        }

        /// <summary>
        /// Finds the neighbors of the given volume. Two Volumes are neighbors if they share a common Face.
        /// </summary>
        /// <param name="_vol">volume whose neighbors we are looking for</param>
        /// <returns>key = face common to both volumes, value = volume neighbor</returns>
        public static Dictionary<Face, Volume> GetNeighborsOf(Volume _vol)
        {
            Dictionary<Face, Volume> neighbors = new Dictionary<Face, Volume>();

            Dictionary<ulong, Face> all_faces_of_vol = ConnectorAlgorithms.GetAllFacesWSubFaces(_vol);
            foreach (var entry in all_faces_of_vol)
            {
                Volume neighbor = entry.Value.PFaces.Select(x => x.Volume).FirstOrDefault(x => x.Id != _vol.Id);
                if (neighbor != null)
                    neighbors.Add(entry.Value, neighbor);
            }

            return neighbors;
        }

        /// <summary>
        /// Fins all edge loops among the holes of the given face that are not attached to another face - 
        /// i.e., the empty openings.
        /// </summary>
        /// <param name="_face">the fiven face</param>
        /// <returns>key = id of the edge loop, value = the edge loop itself</returns>
        public static Dictionary<ulong, EdgeLoop> GetAllHolesIn(Face _face)
        {
            Dictionary<ulong, EdgeLoop> all_holes = new Dictionary<ulong, EdgeLoop>();
            if (_face == null) return all_holes;

            foreach (EdgeLoop el in _face.Holes)
            {
                if (!el.Faces.Any(x => x.Boundary == el))
                {
                    all_holes.Add(el.Id, el);
                }
            }

            return all_holes;
        }

        /// <summary>
        /// Find all faces contained by the given face.
        /// </summary>
        /// <param name="_face">the given face</param>
        /// <returns>key = id of the contained face, value = the contained face itself</returns>
        public static Dictionary<ulong, Face> GetAllContainedFacesIn(Face _face)
        {
            Dictionary<ulong, Face> all_faces = new Dictionary<ulong, Face>();
            if (_face == null) return all_faces;

            foreach (EdgeLoop el in _face.Holes)
            {
                //Check if there is a hole face
                var elFace = el.Faces.FirstOrDefault(x => x.Boundary == el);
                if (elFace != null)
                {
                    all_faces.Add(elFace.Id, elFace);
                    Dictionary<ulong, Face> all_faces_of_sub = ConnectorAlgorithms.GetAllContainedFacesIn(elFace);
                    foreach (var sub_sub_f in all_faces_of_sub)
                    {
                        all_faces.Add(sub_sub_f.Key, sub_sub_f.Value);
                    }
                }
            }
            return all_faces;
        }

        /// <summary>
        /// Finds all edges part of a volume.
        /// </summary>
        /// <param name="_vol">the volume to be queried for its edges</param>
        /// <returns>all found edges</returns>
        public static Dictionary<ulong, Edge> GetAllEdgesOf(Volume _vol)
        {
            Dictionary<ulong, Edge> all_es = new Dictionary<ulong, Edge>();
            if (_vol == null) return all_es;

            foreach (PFace pf in _vol.Faces)
            {
                foreach (var pe in pf.Face.Boundary.Edges)
                {
                    if (!all_es.ContainsKey(pe.Edge.Id))
                        all_es.Add(pe.Edge.Id, pe.Edge);

                }
                foreach (EdgeLoop loop in pf.Face.Holes)
                {
                    foreach (var pe in loop.Edges)
                    {
                        if (!all_es.ContainsKey(pe.Edge.Id))
                            all_es.Add(pe.Edge.Id, pe.Edge);
                    }
                }
            }

            return all_es;
        }

        /// <summary>
        /// Finds all vertices part of a volume.
        /// </summary>
        /// <param name="_vol">the volume to be queried for its vertices</param>
        /// <returns>all found vertices</returns>
        public static Dictionary<ulong, Vertex> GetAllVerticesOf(Volume _vol)
        {
            Dictionary<ulong, Vertex> all_vs = new Dictionary<ulong, Vertex>();
            if (_vol == null) return all_vs;

            foreach (PFace pf in _vol.Faces)
            {
                foreach (var pe in pf.Face.Boundary.Edges)
                {
                    foreach (Vertex v in pe.Edge.Vertices)
                    {
                        if (!all_vs.ContainsKey(v.Id))
                            all_vs.Add(v.Id, v);
                    }
                }
                foreach (EdgeLoop loop in pf.Face.Holes)
                {
                    foreach (var pe in loop.Edges)
                    {
                        foreach (Vertex v in pe.Edge.Vertices)
                        {
                            if (!all_vs.ContainsKey(v.Id))
                                all_vs.Add(v.Id, v);
                        }
                    }
                }
            }

            return all_vs;
        }

        /// <summary>
        /// TODO: Bernhard
        /// Retrieves all volumes containing the given face (including nested volumes).
        /// </summary>
        /// <param name="_f">the face whose containing volumes we are looking for</param>
        /// <returns>a list of volumes, can be empty but not Null</returns>
        public static List<Volume> GetVolumesOf(Face _f)
        {
            if (_f == null) return null;
            List<Volume> volumes = new List<Volume>();
            foreach (PFace pf in _f.PFaces)
            {
                if (pf.Volume != null)
                    volumes.Add(pf.Volume);
            }

            Face containing_face = _f.Boundary.Faces.FirstOrDefault(x => x.Boundary != _f.Boundary);
            if (containing_face != null)
            {
                volumes.AddRange(GetVolumesOf(containing_face));
            }
            return volumes;
        }

        #endregion

        #region UTILS: transfer of geometric information

        /// <summary>
        /// Transfers a position in 3d to the given instance. This changes its transformation matrices. The orientation
        /// is axis-aligned.
        /// </summary>
        /// <param name="_point">the source of the position information</param>
        /// <param name="_instance">the instance</param>
        internal static void TransferPositionToInstance(Point3D _point, SimComponentInstance _instance)
        {
            if (_point == null || _instance == null) return;

            // to be adapted
            Vector3D axis_X_V3D = new Vector3D(1, 0, 0);
            Vector3D axis_Z_V3D = new Vector3D(0, 0, 1);
            Vector3D axis_Y_V3D = new Vector3D(0, 1, 0);
            Matrix3D geom_ucs = GeometricTransformations.PackUCS(_point, axis_X_V3D, axis_Y_V3D, axis_Z_V3D);

            _instance.InstancePath = new List<Point3D> { _point };
        }

        internal static void TransferTransformationToInstance(Vertex _v, SimComponentInstance _instance)
        {
            if (_v.ProxyGeometries.Count > 0)
            {
                var proxy = _v.ProxyGeometries.First();

                using (AccessCheckingDisabler.Disable(_instance.Factory))
                {
                    _instance.InstanceSize = new SimInstanceSize(_instance.InstanceSize.Min, proxy.Size);
                    _instance.InstanceRotation = proxy.Rotation;
                }
            }
        }

        #endregion

        #region UTILS: Connector Info

        /// <summary>
        /// Extracts the component end of the connection managed by the given connector.
        /// </summary>
        /// <param name="_connector">the connector</param>
        /// <returns>the component source or Null</returns>
        internal static SimComponent GetConnectionSource(ConnectorBase _connector)
        {
            if (_connector == null) return null;
            if (_connector is ConnectorToBaseGeometry)
                return (_connector as ConnectorToBaseGeometry).DescriptiveSource;
            else if (_connector is ConnectorPrescriptiveToFace)
                return (_connector as ConnectorPrescriptiveToFace).PrescriptiveSource;
            else
                return null;
        }

        #endregion

        /// <summary>
        /// Attaches an automatically generated sub-component representing a geometric entity with
        /// the given name and dimensionality to the parent component.
        /// </summary>
        /// <param name="_factory">the component factory managing the parent component</param>
        /// <param name="_parent">the parent component</param>
        /// <param name="_name">name of the new sub-component</param>
        /// <param name="_nr_dimensions">dimensionality</param>
        /// <returns>the newly generated sub-component</returns>
        public static SimComponent AttachAutomaticSubRepresentation(SimComponentCollection _factory, SimComponent _parent, string _name, int _nr_dimensions)
        {
            if (_factory == null) return null;

            string name_suffix = _nr_dimensions.ToString() + "D";
            // create
            SimComponent component = new SimComponent(_factory.ProjectData.UsersManager.CurrentUser.Role);

            component.AccessLocal.ForEach(x => x.Access |= SimComponentAccessPrivilege.Read);
            component.AccessLocal[_factory.ProjectData.UsersManager.CurrentUser].Access |= SimComponentAccessPrivilege.Supervize;
            component.AccessLocal[SimUserRole.BUILDING_DEVELOPER].Access |= SimComponentAccessPrivilege.Release;

            component.IsAutomaticallyGenerated = true;
            component.Name = _name + " " + name_suffix;
            component.Description = "Representation";
            component.InstanceType = (_nr_dimensions < 3) ? SimInstanceType.GeometricSurface : SimInstanceType.GeometricVolume;

            //Make sure that the generated component has the same access profile as the parent


            // attach to parent
            if (_parent != null)
            {
                var slot = _parent.Components.FindAvailableSlot(ComponentUtils.InstanceTypeToSlotBase(component.InstanceType), "AG{0}");
                component.CurrentSlot = slot.SlotBase;
                _parent.Components.Add(new SimChildComponentEntry(slot, component));
            }

            return component;
        }
    }
}
