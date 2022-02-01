using SIMULTAN.Data.Components;
using SIMULTAN.Data.FlowNetworks;
using SIMULTAN.Data.Geometry;
using SIMULTAN.Projects;
using SIMULTAN.Serializer.SimGeo;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SIMULTAN.Exchange.Connectors
{
    /// <summary>
    /// Connector that manages the connection between a network and a geometry model.
    /// </summary>
    internal class ConnectorToGeometryModel
    {
        #region PROPERTIES

        /// <summary>
        /// The flow network represented by the geometry model. Its property IndexOfGeometricRepFile
        /// carries the information for retrieving the geometry model from a file or from the
        /// resource manager.
        /// </summary>
        public SimFlowNetwork Source { get; }

        /// <summary>
        /// The geometry model representing the network. All changes in it reflect changes in the network.
        /// </summary>
        public GeometryModelData Target { get; }

        /// <summary>
        /// The connectors to all components whose instances are placed in an element of the network.
        /// </summary>
        public List<ConnectorRepresentativeToBase> ContainedConnectors { get; }

        private ProjectData projectData;

        #endregion

        #region FIELDS

        /// <summary>
        /// The instance holding and managing all network connectors
        /// </summary>
        private NetworkGeometryExchange comm_manager;

        /// <summary>
        /// The lookup table for vertices and polylines in the target geometry model.
        /// </summary>
        private Dictionary<ulong, BaseGeometry> geometry_lookup_table;
        /// <summary>
        /// Look-up of connectors according to the network element.
        /// </summary>
        private Dictionary<SimObjectId, ConnectorRepresentativeToBase> lookup_acc_to_nwElement;
        /// <summary>
        /// Look-up of connectors according to the representing geometry.
        /// </summary>
        private Dictionary<ulong, ConnectorRepresentativeToBase> lookup_acc_to_Representation;

        /// <summary>
        /// Display: A list of empty network elements for display.
        /// </summary>
        private Dictionary<SimObjectId, SimFlowNetworkElement> empty_elements;
        /// <summary>
        /// Display: A list of full network elements not yet attached to geometry (e.g. placed in a space) for display.
        /// </summary>
        private Dictionary<SimObjectId, SimFlowNetworkElement> unattached_elements;

        #endregion

        #region .CTOR

        /// <summary>
        /// Initializes a connector between a flow network and a representative geometry model.
        /// </summary>
        /// <param name="_comm_manager">the container and manager of all connectors</param>
        /// <param name="_source">the network that is being represented</param>
        /// <param name="_target">the representing geometry model</param>
        /// <param name="_index_of_target_model">the index of the geometry model in the resource manager</param>
		/// <param name="projectData">The model store in which the model is managed</param>
        public ConnectorToGeometryModel(NetworkGeometryExchange _comm_manager, SimFlowNetwork _source, GeometryModelData _target, int _index_of_target_model,
            ProjectData projectData)
        {
            this.comm_manager = _comm_manager;
            this.projectData = projectData;

            this.Source = _source;
            this.Source.IndexOfGeometricRepFile = _index_of_target_model;
            this.Source.ElementAdded += Network_ElementAdded;
            this.Source.ElementDeleted += Network_ElementDeleted;
            this.Source.EdgeRedirected += Network_EdgeRedirected;
            this.Source.ElementTopologyChanged += Network_ElementTopologyChanged;

            this.Target = _target;
            this.Target.GeometryAdded += TargetModel_GeometryAddedOrDeleted;
            this.Target.GeometryRemoved += TargetModel_GeometryAddedOrDeleted;
            this.geometry_lookup_table = ConnectorToGeometryModel.CreateLookupTable(this.Target);

            this.ContainedConnectors = new List<ConnectorRepresentativeToBase>();
            this.empty_elements = new Dictionary<SimObjectId, SimFlowNetworkElement>();
            this.unattached_elements = new Dictionary<SimObjectId, SimFlowNetworkElement>();

            this.lookup_acc_to_nwElement = new Dictionary<SimObjectId, ConnectorRepresentativeToBase>();
            this.lookup_acc_to_Representation = new Dictionary<ulong, ConnectorRepresentativeToBase>();
        }

        #endregion

        #region METHODS: Connecting the contents of the network to the geometry

        /// <summary>
        /// Assumes that the proxy geometry belongs to the model saved in <see cref="Target"/> and
        /// that the network element belongs to the network saved in <see cref="Source"/>.
        /// </summary>
        /// <param name="_nw_element">the network element that is to be represented</param>
        /// <param name="_proxy_geometry">the representing geometry anchor</param>
        /// <returns></returns>
        internal (ConnectorRepresentativeToBase connector, ConnectorConnectionState feedback) RepresentInstanceBy(SimFlowNetworkElement _nw_element, BaseGeometry _proxy_geometry)
        {
            ConnectorConnectionState fb = ConnectorConnectionState.OK;

            SimComponent comp = _nw_element.Content?.Component;
            bool comp_to_nwe_match = false;
            if (comp != null)
            {
                comp_to_nwe_match = (comp.InstanceType == SimInstanceType.NetworkNode && _nw_element is SimFlowNetworkNode && _proxy_geometry is Vertex) ||
                                    (comp.InstanceType == SimInstanceType.NetworkEdge && _nw_element is SimFlowNetworkEdge && _proxy_geometry is Polyline);
            }
            else
            {
                comp_to_nwe_match = (_nw_element is SimFlowNetworkNode && _proxy_geometry is Vertex) ||
                                    (_nw_element is SimFlowNetworkEdge && _proxy_geometry is Polyline);
            }
            if (!comp_to_nwe_match)
            {
                return (null, ConnectorConnectionState.SOURCE_COMPONENT_TARGET_GEOMETRY_MISMATCH);
            }

            // 0. check if a connector with this component as a source exists
            if (this.lookup_acc_to_nwElement.ContainsKey(_nw_element.ID))
            {
                ConnectorRepresentativeToBase existing_connector = this.lookup_acc_to_nwElement[_nw_element.ID];
                if ((existing_connector is ConnectorRepresentativeToVertex && _proxy_geometry is Vertex) ||
                    (existing_connector is ConnectorRepresentativeToPolyline && _proxy_geometry is Polyline))
                {
                    // check if the representation is the same, and update if necessary
                    if (existing_connector.TargetId != _proxy_geometry.Id)
                        this.UpdateConnector(_nw_element, _proxy_geometry);
                    return (existing_connector, fb);
                }
                else
                    fb |= ConnectorConnectionState.SOURCE_COMPONENT_TARGET_GEOMETRY_MISMATCH;
            }

            // 1. check if source and target match
            if (_proxy_geometry is Vertex)
            {
                // 1a. create a node representation
                ConnectorRepresentativeToVertex node_representation =
                    new ConnectorRepresentativeToVertex(this.comm_manager.MainGeometryExchange, this, _nw_element as SimFlowNetworkNode, this.Source.IndexOfGeometricRepFile, _proxy_geometry as Vertex);
                this.AddConnector(node_representation);
                BaseGeometry association_geometry = GetAssociationOf(_nw_element);
                if (association_geometry != null)
                    this.PlaceInstanceIn(_proxy_geometry, association_geometry);
                return (node_representation, fb);
            }
            else if (_proxy_geometry is Polyline)
            {
                // 1b. create an edge representation
                ConnectorRepresentativeToPolyline edge_representation =
                    new ConnectorRepresentativeToPolyline(this.comm_manager.MainGeometryExchange, this, _nw_element as SimFlowNetworkEdge, this.Source.IndexOfGeometricRepFile, _proxy_geometry as Polyline);
                this.AddConnector(edge_representation);
                BaseGeometry association_geometry = GetAssociationOf(_nw_element);
                if (association_geometry != null)
                    this.PlaceInstanceIn(_proxy_geometry, association_geometry);
                return (edge_representation, fb);
            }
            else
                fb |= ConnectorConnectionState.SOURCE_COMPONENT_TARGET_GEOMETRY_MISMATCH;

            return (null, fb);
        }

        /// <summary>
        /// Creates an association between the network element represented by the _proxy_geometry
        /// and the _target geometry (e.g. between a lamp representation and a space representation).
        /// </summary>
        /// <param name="_proxy_geometry">the geometry representing the network element</param>
        /// <param name="_target">the target of the association</param>
        internal void PlaceInstanceIn(BaseGeometry _proxy_geometry, BaseGeometry _target)
        {
            if (_proxy_geometry == null || _target == null) return;

            ConnectorRepresentativeToBase element_con = this.ContainedConnectors.FirstOrDefault(x => x.TargetId == _proxy_geometry.Id);
            if (element_con == null) return;

            SimFlowNetworkElement nwe = element_con.SourceContainer;
            SimComponentInstance relationship = nwe.GetUpdatedInstance(true);
            if (relationship == null || relationship.State.IsRealized) return;


            if (this.lookup_acc_to_Representation.ContainsKey(_proxy_geometry.Id))
            {
                ConnectorRepresentativeToBase representation = this.lookup_acc_to_Representation[_proxy_geometry.Id];
                if (representation != null)
                {
                    representation.AttachToGeometry(this.comm_manager.MainGeometryExchange.GetModelIndex(_target), _target, _proxy_geometry);
                    representation.SourceContainer.Content?.Component.UpdateConnectivity();
                }
            }
        }

        /// <summary>
        /// Removes the association between the network element represented by the _proxy_geometry
        /// and another geometry (e.g. between a lamp representation and a space representation).
        /// </summary>
        /// <param name="_proxy_geometry">the geometry representing the network element</param>
        internal void UnplaceInstanceFrom(BaseGeometry _proxy_geometry)
        {
            if (_proxy_geometry == null) return;

            if (this.lookup_acc_to_Representation.ContainsKey(_proxy_geometry.Id))
            {
                ConnectorRepresentativeToBase representation = this.lookup_acc_to_Representation[_proxy_geometry.Id];
                if (representation != null && representation.IsAttachedToGeometry())
                {
                    representation.DetachFromGeometry();
                    representation.SourceContainer.Content?.Component.UpdateConnectivity();
                }
            }
        }

        /// <summary>
        /// Looks for the geometry where the network container is placed.
        /// </summary>
        /// <param name="_nw_container">the given container</param>
        /// <returns>the associated geometry or Null</returns>
        private BaseGeometry GetAssociationOf(SimFlowNetworkElement _nw_container)
        {
            if (_nw_container == null) return null;

            SimComponentInstance gr = _nw_container.GetUpdatedInstance(true);
            if (gr != null)
            {
                var firstGeometryPlacement = (SimInstancePlacementGeometry)gr.Placements.FirstOrDefault(x => x is SimInstancePlacementGeometry);

                if (firstGeometryPlacement != null && firstGeometryPlacement.IsValid)
                    return this.comm_manager.MainGeometryExchange.GeometryManager.GetGeometryFromId(firstGeometryPlacement.FileId, firstGeometryPlacement.GeometryId);
            }

            return null;
        }

        #endregion

        #region METHODS: Connector add, remove, lookup update

        /// <summary>
        /// Attaches the event handlers to the connector and to the representing target geometry.
        /// Only geometry bound in a connector has even handlers attached!
        /// </summary>
        /// <param name="_connector">the (newly) created connector</param>
        internal void AddConnector(ConnectorRepresentativeToBase _connector)
        {
            _connector.Target.GeometryChanged -= base_geometry_GeometryChanged;
            _connector.Target.GeometryChanged += base_geometry_GeometryChanged;

            _connector.Target.TopologyChanged -= base_geometry_GeometryChanged;
            _connector.Target.TopologyChanged += base_geometry_GeometryChanged;

            _connector.Target.PropertyChanged -= base_geometry_PropertyChanged;
            _connector.Target.PropertyChanged += base_geometry_PropertyChanged;

            this.UpdateConnectorLookUpTables(_connector, ConnectionChange.CREATED);
            this.CommunicateChangeInContent(_connector.SourceContainer, _connector.SourceContainer.Content != null, _connector.ContainsAttachedInstance());
            this.comm_manager.MainGeometryExchange.OnAssociationChanged(new List<BaseGeometry> { _connector.Target });
        }


        /// <summary>
        /// Performs clean-up in the connector and detaches the event handlers
        /// both from the connector and the representing target geometry. If the geometry is Null,
        /// the clean-up just deletes the connector.
        /// </summary>
        /// <param name="_connector">the connector about to be deleted</param>
        /// <param name="_target">the affected representing target geometry</param>
        internal void RemoveConnector(ConnectorRepresentativeToBase _connector, BaseGeometry _target)
        {
            // get all affected (only in the case of deleted NODES)
            if (_connector is ConnectorRepresentativeToVertex)
            {
                SimFlowNetworkNode nw_node = _connector.SourceContainer as SimFlowNetworkNode;
                if (nw_node != null)
                {
                    List<SimFlowNetworkEdge> nw_edges_to_delete = new List<SimFlowNetworkEdge>();
                    nw_edges_to_delete.AddRange(nw_node.Edges_In);
                    nw_edges_to_delete.AddRange(nw_node.Edges_Out);
                    List<ConnectorRepresentativeToBase> affected = this.lookup_acc_to_nwElement.Where(x => nw_edges_to_delete.Contains(x.Value.SourceContainer as SimFlowNetworkEdge)).Select(x => x.Value).ToList();
                    foreach (ConnectorRepresentativeToBase c in affected)
                    {
                        BaseGeometry c_rep = this.geometry_lookup_table[c.TargetId];
                        this.RemoveConnector(c, c_rep);
                    }
                }
            }

            _connector.BeforeDeletion();
            if (_target != null)
            {
                _target.GeometryChanged -= base_geometry_GeometryChanged;
                _target.TopologyChanged -= base_geometry_GeometryChanged;
                _target.PropertyChanged -= base_geometry_PropertyChanged;
            }

            this.UpdateConnectorLookUpTables(_connector, ConnectionChange.DELETED);
            //if (_target != null)
            //    this.comm_manager.MainGeometryExchange.OnGeometryInvalidated(new List<BaseGeometry> { _target });
            if (_target != null)
            {

            }

        }

        /// <summary>
        /// Performs clean-up of the lookup tables on a major connection change:
        /// if a connector is deleted or cretaed; if either the source or the target is deleted.
        /// Deletion of the target throws an <see cref="InvalidOperationException"/> expection.
        /// </summary>
        /// <param name="_connector">the affected representative connector</param>
        /// <param name="_change">the change in the connector</param>
        private void UpdateConnectorLookUpTables(ConnectorRepresentativeToBase _connector, ConnectionChange _change)
        {
            if (_connector == null) return;

            SimObjectId key_nwe = _connector.SourceContainer.ID;
            ulong key_geom = _connector.TargetId;
            switch (_change)
            {
                case ConnectionChange.CREATED:
                    {
                        this.ContainedConnectors.Add(_connector);

                        var instance = _connector.SourceContainer.Content;
                        bool hasValidPlacement = false;

                        if (instance != null)
                            hasValidPlacement = instance.Placements.Any(x => x is SimInstancePlacementGeometry pg && pg.IsValid);

                        this.AddToEmptyAndUnattached(_connector.SourceContainer,
                                                      instance != null,
                                                      !hasValidPlacement);

                        this.lookup_acc_to_nwElement.Add(key_nwe, _connector);
                        if (key_geom != ulong.MaxValue)
                            this.lookup_acc_to_Representation.Add(key_geom, _connector);
                    }
                    break;
                case ConnectionChange.TARGET_DELETED:
                    // this should not happen
                    throw new InvalidOperationException("A geometric representation cannot be deleted before the element it represents!");
                case ConnectionChange.SOURCE_DELETED:
                case ConnectionChange.DELETED:
                    {
                        this.ContainedConnectors.Remove(_connector);

                        var instance = _connector.SourceContainer.Content;
                        bool hasValidPlacement = false;

                        if (instance != null)
                            hasValidPlacement = instance.Placements.Any(x => x is SimInstancePlacementGeometry pg && pg.IsValid);

                        RemoveFromEmptyAndUnattached(_connector.SourceContainer);

                        if (this.lookup_acc_to_nwElement.ContainsKey(key_nwe))
                            this.lookup_acc_to_nwElement.Remove(key_nwe);
                        //Debug.WriteLine("[ELEM] B2 " + PrintLookupAccToNWElement());
                        if (this.lookup_acc_to_Representation.ContainsKey(key_geom))
                            this.lookup_acc_to_Representation.Remove(key_geom);
                        //Debug.WriteLine("[REP] B2 " + PrintLookupAccToRepresentation());
                    }
                    break;
                case ConnectionChange.NONE:
                default:
                    break;
            }

        }

        #endregion

        #region METHODS: deletion

        internal void OnBeingDeleted()
        {
            if (this.Source != null)
            {
                this.Source.ElementAdded -= Network_ElementAdded;
                this.Source.ElementDeleted -= Network_ElementDeleted;
                this.Source.EdgeRedirected -= Network_EdgeRedirected;
                this.Source.ElementTopologyChanged -= Network_ElementTopologyChanged;
            }

            if (this.Target != null)
            {
                this.Target.GeometryAdded -= TargetModel_GeometryAddedOrDeleted;
                this.Target.GeometryRemoved -= TargetModel_GeometryAddedOrDeleted;
            }

            foreach (var connector in ContainedConnectors)
            {
                connector.Target.GeometryChanged -= base_geometry_GeometryChanged;
                connector.Target.TopologyChanged -= base_geometry_GeometryChanged;
                connector.Target.PropertyChanged -= base_geometry_PropertyChanged;
                connector.BeforeDeletion();
            }
        }

        #endregion

        #region METHODS: Feedback from network elements

        internal void CommunicateChangeInContent(SimFlowNetworkElement _element, bool _is_full, bool _is_attached)
        {
            if (_element == null) return;

            this.AddToEmptyAndUnattached(_element, _is_full, _is_attached);
            this.UpdateColor();
        }

        private void RemoveFromEmptyAndUnattached(SimFlowNetworkElement _element)
        {
            UpdateEmptyAndUnattached();

            this.empty_elements.Remove(_element.ID);
            this.unattached_elements.Remove(_element.ID);
        }
        private void AddToEmptyAndUnattached(SimFlowNetworkElement _element, bool _is_full, bool _is_attached)
        {
            UpdateEmptyAndUnattached();

            if (!_is_full && !this.empty_elements.ContainsKey(_element.ID))
                this.empty_elements.Add(_element.ID, _element);
            else if (_is_full && this.empty_elements.ContainsKey(_element.ID))
                this.empty_elements.Remove(_element.ID);

            if (_is_full && !_is_attached && !this.unattached_elements.ContainsKey(_element.ID))
                this.unattached_elements.Add(_element.ID, _element);
            else if (_is_full && _is_attached && this.unattached_elements.ContainsKey(_element.ID))
                this.unattached_elements.Remove(_element.ID);
            else if (!_is_full && this.unattached_elements.ContainsKey(_element.ID))
                this.unattached_elements.Remove(_element.ID);
        }
        private void UpdateEmptyAndUnattached()
        {
            // synchronize with the geometry lookup table first (e.g. necessary when turning a node into a subnetwork)
            List<SimObjectId> to_remove = new List<SimObjectId>();
            foreach (var entry in this.empty_elements)
            {
                ConnectorRepresentativeToBase con = this.lookup_acc_to_nwElement[entry.Key];
                if (!this.geometry_lookup_table.ContainsKey(con.TargetId))
                    to_remove.Add(entry.Key);
            }
            to_remove.ForEach(x => this.empty_elements.Remove(x));
            to_remove = new List<SimObjectId>();
            foreach (var entry in this.unattached_elements)
            {
                ConnectorRepresentativeToBase con = this.lookup_acc_to_nwElement[entry.Key];
                if (!this.geometry_lookup_table.ContainsKey(con.TargetId))
                    to_remove.Add(entry.Key);
            }
            to_remove.ForEach(x => this.unattached_elements.Remove(x));
        }


        private void UpdateColor()
        {
            // separate the representing geometry in 3 categories: empty, full, attached
            Dictionary<ulong, BaseGeometry> rep_empty = new Dictionary<ulong, BaseGeometry>();
            Dictionary<ulong, BaseGeometry> rep_full = new Dictionary<ulong, BaseGeometry>();
            Dictionary<ulong, BaseGeometry> rep_attached = new Dictionary<ulong, BaseGeometry>();

            foreach (var entry in this.empty_elements)
            {
                ConnectorRepresentativeToBase con = this.lookup_acc_to_nwElement[entry.Key];
                rep_empty.Add(con.TargetId, this.geometry_lookup_table[con.TargetId]);
            }
            foreach (var entry in this.unattached_elements)
            {
                ConnectorRepresentativeToBase con = this.lookup_acc_to_nwElement[entry.Key];
                rep_full.Add(con.TargetId, this.geometry_lookup_table[con.TargetId]);
            }
            foreach (var entry in this.geometry_lookup_table)
            {
                if (rep_empty.ContainsKey(entry.Key)) continue;
                if (rep_full.ContainsKey(entry.Key)) continue;
                rep_attached.Add(entry.Key, entry.Value);
            }

            // change color
            this.Target.StartBatchOperation();
            foreach (var entry in rep_empty)
            {
                SwitchColor(entry.Value, false, NetworkGeometryExchange.COL_EMPTY);
            }
            foreach (var entry in rep_full)
            {
                SwitchColor(entry.Value, false, NetworkGeometryExchange.COL_UNASSIGNED);
            }
            foreach (var entry in rep_attached)
            {
                SwitchColor(entry.Value, true, NetworkGeometryExchange.COL_NEUTRAL);
            }
            this.Target.EndBatchOperation();
        }

        private static void SwitchColor(BaseGeometry _geometry, bool _take_parent_color, System.Windows.Media.Color _color)
        {
            if (_geometry is Polyline)
            {
                Polyline pl = _geometry as Polyline;
                var edges = pl.Edges.Select(x => x.Edge);
                foreach (Edge e in edges)
                {
                    if (_take_parent_color)
                        e.Color.IsFromParent = true;
                    else
                    {
                        e.Color.IsFromParent = false;
                        e.Color.Color = _color;
                    }
                }
                var vertices = pl.Edges.Select(x => x.StartVertex).ToList();
                vertices = vertices.Skip(1).ToList();
                foreach (Vertex v in vertices)
                {
                    if (_take_parent_color)
                        v.Color.IsFromParent = true;
                    else
                    {
                        v.Color.IsFromParent = false;
                        v.Color.Color = _color;
                    }
                }
            }

            if (_take_parent_color)
            {
                _geometry.Color.IsFromParent = true;
            }
            else
            {
                _geometry.Color.IsFromParent = false;
                _geometry.Color.Color = _color;
            }
        }

        #endregion

        #region METHODS: Update representation

        /// <summary>
        /// Called when the assets of one of the connected component changes.
        /// Triggers an update on the representation.
        /// </summary>
        public void ConnectorAssetsChanged(ConnectorRepresentativeToBase connector)
        {
            NetworkConverter.ReimportNetworkElementAssets(connector.ParentConnector.Target, connector.SourceContainer, this.projectData);
        }

        private void UpdateRepresentation()
        {
            NetworkConverter.Update(this.Target, this.Source, this.comm_manager, this.projectData);
            this.geometry_lookup_table = ConnectorToGeometryModel.CreateLookupTable(this.Target);
            //Debug.WriteLine("[GEOM] B " + PrintGeometryLookupTable());
            this.UpdateColor();
        }

        internal void UpdateConnector(SimFlowNetworkElement _nw_element, BaseGeometry _representation)
        {
            if (!this.geometry_lookup_table.ContainsKey(_representation.Id))
                this.geometry_lookup_table.Add(_representation.Id, _representation);
            //Debug.WriteLine("[GEOM] C " + PrintGeometryLookupTable());
            if (this.lookup_acc_to_nwElement.ContainsKey(_nw_element.ID) && _representation != null)
            {
                ConnectorRepresentativeToBase connector = this.lookup_acc_to_nwElement[_nw_element.ID];
                this.RemoveConnector(connector, null);
                this.RepresentInstanceBy(_nw_element, _representation);
            }
        }

        #endregion

        #region UTILS: lookup tables

        /// <summary>
        /// Creates a lookup table for the geometric instances contained in the model.
        /// Thier keys are used as keys in the table. Only vertices and polylines are included!
        /// </summary>
        /// <param name="_model">the container of the geometric instances</param>
        /// <returns>a lookup table with keys of type ulong</returns>
        private static Dictionary<ulong, BaseGeometry> CreateLookupTable(GeometryModelData _model)
        {
            Dictionary<ulong, BaseGeometry> lookup_table = new Dictionary<ulong, BaseGeometry>();
            if (_model == null) return lookup_table;

            foreach (var v in _model.Vertices)
            {
                lookup_table.Add(v.Id, v);
            }
            foreach (var pl in _model.Polylines)
            {
                lookup_table.Add(pl.Id, pl);
                var inner_vertices = pl.Edges.Select(x => x.StartVertex).ToList();
                inner_vertices = inner_vertices.Skip(1).ToList();
                foreach (var iv in inner_vertices)
                {
                    if (lookup_table.ContainsKey(iv.Id))
                        lookup_table.Remove(iv.Id);
                }
            }

            return lookup_table;
        }

        internal ProxyGeometry GetProxyDecorator(ConnectorRepresentativeToBase _node_connector)
        {
            Vertex anchor = (this.geometry_lookup_table.ContainsKey(_node_connector.TargetId)) ? this.geometry_lookup_table[_node_connector.TargetId] as Vertex : null;
            if (anchor == null)
                return null;
            else
            {
                if (anchor.ProxyGeometries.Count > 0)
                    return anchor.ProxyGeometries[0];
                else
                    return null;
            }
        }

        #endregion

        #region EVENT HANDLERS: Network elements

        private void Network_ElementAdded(object sender, SimFlowNetworkElement addedElement)
        {
            if (addedElement != null && this.Target != null)
            {
                // alert the representation model to add a representing proxy!
                this.UpdateRepresentation();
            }
        }

        private void Network_ElementDeleted(object sender, SimFlowNetworkElement deletedElement)
        {
            if (deletedElement != null && this.Target != null)
            {
                // remove the connector and invalidate the representing geometry
                if (this.lookup_acc_to_nwElement.ContainsKey(deletedElement.ID))
                {
                    ConnectorRepresentativeToBase connector = this.lookup_acc_to_nwElement[deletedElement.ID];
                    BaseGeometry representation = null;
                    if (this.geometry_lookup_table.ContainsKey(connector.TargetId))
                        representation = this.geometry_lookup_table[connector.TargetId];
                    this.RemoveConnector(connector, representation);
                    this.UpdateRepresentation();
                }
            }
        }

        private void Network_EdgeRedirected(object sender, SimFlowNetworkEdge redirectedEdge)
        {
            if (redirectedEdge != null && this.Target != null)
            {
                // invalidate the representing geometry
                if (this.lookup_acc_to_nwElement.ContainsKey(redirectedEdge.ID))
                {
                    ConnectorRepresentativeToBase connector = this.lookup_acc_to_nwElement[redirectedEdge.ID];
                    if (this.geometry_lookup_table.ContainsKey(connector.TargetId))
                    {
                        BaseGeometry representation = this.geometry_lookup_table[connector.TargetId];
                        this.comm_manager.MainGeometryExchange.OnGeometryInvalidated(new List<BaseGeometry> { representation });
                        this.UpdateRepresentation();
                    }
                }
            }
        }

        private void Network_ElementTopologyChanged(object sender, SimFlowNetworkElement oldElement, SimFlowNetworkElement newElement, List<SimFlowNetworkElement> changedElements)
        {
            if (oldElement != null && newElement != null && this.Target != null)
            {
                if ((oldElement is SimFlowNetworkNode) && !(oldElement is SimFlowNetwork) && (newElement is SimFlowNetwork))
                {
                    this.UpdateRepresentation();
                }
            }
        }

        #endregion

        #region EVENT HANDLERS: geometry

        /// <summary>
        /// Updates the geometry lookup table.
        /// </summary>
        /// <param name="sender">the target geometry model</param>
        /// <param name="geometry">the just added or deleted geometry</param>
        private void TargetModel_GeometryAddedOrDeleted(object sender, IEnumerable<BaseGeometry> geometry)
        {
            this.geometry_lookup_table = ConnectorToGeometryModel.CreateLookupTable(this.Target);

            // update the representations in case of deletion of orphaned vertices
            Dictionary<ulong, ConnectorRepresentativeToBase> keys_to_remove = new Dictionary<ulong, ConnectorRepresentativeToBase>();
            foreach (var entry in this.lookup_acc_to_Representation)
            {
                if (!this.geometry_lookup_table.ContainsKey(entry.Key))
                {
                    keys_to_remove.Add(entry.Key, entry.Value);
                }
            }
            foreach (var entry in keys_to_remove)
            {
                this.lookup_acc_to_Representation.Remove(entry.Key);
                this.RemoveConnector(entry.Value, null);
            }
        }

        /// <summary>
        /// Finds the network element represented by the changed geometry and trigger its synchronization.
        /// </summary>
        /// <param name="sender">the changed geometry</param>
        private void base_geometry_GeometryChanged(object sender)
        {
            if (sender is BaseGeometry geom && this.lookup_acc_to_Representation.TryGetValue(geom.Id, out var connector))
            {
                connector.SynchronizeSourceWTarget(geom);
            }
        }

        /// <summary>
        /// Finds the network element represented by the sender geometry and, if the <see cref="BaseGeometry.Parent"/> property
        /// was changed, triggers the Associate or DisAssociate routine in the <see cref="NetworkGeometryExchange"/> instance.
        /// </summary>
        /// <param name="sender">the geometry whose property value just changed</param>
        /// <param name="e">info about the changed property</param>
        private void base_geometry_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            BaseGeometry geom = sender as BaseGeometry;

            if (geom != null && e.PropertyName == nameof(BaseGeometry.Parent))
            {
                this.UnplaceInstanceFrom(geom);

                if (geom.Parent != null)
                {
                    this.PlaceInstanceIn(geom, geom.Parent.Target);
                }
            }
        }

        #endregion
    }
}
