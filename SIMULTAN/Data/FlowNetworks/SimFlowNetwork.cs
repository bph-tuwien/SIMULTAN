using SIMULTAN.Data.Components;
using SIMULTAN.Data.SimNetworks;
using SIMULTAN.Data.Users;
using SIMULTAN.Utils;
using SIMULTAN.Utils.Collections;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Data;

namespace SIMULTAN.Data.FlowNetworks
{
    public class SimFlowNetwork : SimFlowNetworkNode, INetwork
    {
        #region PROPERTIES EDITING: Manager

        protected SimUserRole manager;
        public SimUserRole Manager
        {
            get { return this.manager; }
            protected set
            {
                if (this.manager != value)
                {
                    var old_value = this.manager;
                    this.manager = value;
                    this.NotifyPropertyChanged(nameof(Manager));
                }
            }
        }

        protected int index_of_geometric_rep_file;
        public int IndexOfGeometricRepFile
        {
            get { return this.index_of_geometric_rep_file; }
            set
            {
                if (this.index_of_geometric_rep_file != value)
                {
                    var old_value = this.index_of_geometric_rep_file;
                    this.index_of_geometric_rep_file = value;
                    this.NotifyPropertyChanged(nameof(IndexOfGeometricRepFile));
                }
            }
        }

        #endregion

        #region PROPERTIES: Elements (Nodes, Edges, Networks)

        public SimFlowNetwork ParentNetwork { get; private set; }

        // --------------------------------- NODES ------------------------------------ //
        protected ObservableDictionary<long, SimFlowNetworkNode> contained_nodes;
        public ObservableDictionary<long, SimFlowNetworkNode> ContainedNodes
        {
            get { return this.contained_nodes; }
            protected set
            {
                if (this.contained_nodes != null)
                {
                    this.contained_nodes.CollectionChanged -= ContainedNodes_CollectionChanged;
                }
                this.contained_nodes = value;
                if (this.contained_nodes != null)
                {
                    this.contained_nodes.CollectionChanged -= ContainedNodes_CollectionChanged;
                    this.contained_nodes.CollectionChanged += ContainedNodes_CollectionChanged;
                }
                this.NotifyPropertyChanged(nameof(ContainedNodes));
            }
        }

        private void ContainedNodes_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            this.NotifyPropertyChanged(nameof(ContainedNodes));
            this.UpdateChildrenContainer();

            object old_item = (e.OldItems == null) ? null : e.OldItems[0];
            object new_item = (e.NewItems == null) ? null : e.NewItems[0];
            if (e.Action == NotifyCollectionChangedAction.Remove && old_item is SimFlowNetworkNode)
            {
                if ((old_item as SimFlowNetworkNode).ID.LocalId == this.NodeStart_ID)
                    this.NodeStart_ID = -1;
                else if ((old_item as SimFlowNetworkNode).ID.LocalId == this.NodeEnd_ID)
                    this.NodeEnd_ID = -1;
            }
            else if (e.Action == NotifyCollectionChangedAction.Replace && new_item is SimFlowNetworkNode && old_item is SimFlowNetworkNode)
            {
                if ((old_item as SimFlowNetworkNode).ID.LocalId == this.NodeStart_ID)
                    this.NodeStart_ID = -1;
                else if ((old_item as SimFlowNetworkNode).ID.LocalId == this.NodeEnd_ID)
                    this.NodeEnd_ID = -1;
            }
        }

        // --------------------------------- EDGES ------------------------------------ //
        protected ObservableDictionary<long, SimFlowNetworkEdge> contained_edges;
        public ObservableDictionary<long, SimFlowNetworkEdge> ContainedEdges
        {
            get { return this.contained_edges; }
            protected set
            {
                if (this.contained_edges != null)
                {
                    this.contained_edges.CollectionChanged -= ContainedEdges_CollectionChanged;
                }
                this.contained_edges = value;
                if (this.contained_edges != null)
                {
                    this.contained_edges.CollectionChanged -= ContainedEdges_CollectionChanged;
                    this.contained_edges.CollectionChanged += ContainedEdges_CollectionChanged;
                }
                this.NotifyPropertyChanged(nameof(ContainedEdges));
            }
        }

        private void ContainedEdges_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            this.NotifyPropertyChanged(nameof(ContainedEdges));
            this.UpdateChildrenContainer();
        }

        // ------------------------------- NETWORKS ----------------------------------- //
        private ObservableDictionary<long, SimFlowNetwork> contained_flow_networks;
        public ObservableDictionary<long, SimFlowNetwork> ContainedFlowNetworks
        {
            get { return this.contained_flow_networks; }
            private set
            {
                if (this.contained_flow_networks != null)
                {
                    this.contained_flow_networks.CollectionChanged -= ContainedFlowNetworks_CollectionChanged;
                }
                this.contained_flow_networks = value;
                if (this.contained_flow_networks != null)
                {
                    this.contained_flow_networks.CollectionChanged -= ContainedFlowNetworks_CollectionChanged;
                    this.contained_flow_networks.CollectionChanged += ContainedFlowNetworks_CollectionChanged;
                }
                this.NotifyPropertyChanged(nameof(ContainedFlowNetworks));
            }
        }

        private void ContainedFlowNetworks_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
                foreach (var item in e.OldItems.OfType<KeyValuePair<long, SimFlowNetwork>>())
                    item.Value.Parent = null;
            if (e.NewItems != null)
                foreach (var item in e.NewItems.OfType<KeyValuePair<long, SimFlowNetwork>>())
                    item.Value.Parent = this;

            this.UpdateChildrenContainer();
        }


        // ----------------------------- QUERYING SOURCE AND SINK NODES ----------------------------- //

        /// <summary>
        /// Gets the node of the network that connects to its parent as an entry into the subnetwork.
        /// Corresponds to property <see cref="NodeStart_ID"/>.
        /// </summary>
        public SimFlowNetworkNode ConnectionToParentEntryNode
        {
            get
            {
                if (this.NodeStart_ID > -1 && this.ContainedNodes.ContainsKey(this.NodeStart_ID))
                    return this.ContainedNodes[this.NodeStart_ID];
                else
                {
                    if (this.ContainedNodes.Count > 0)
                    {
                        if (this.IsDirected)
                        {
                            List<SimFlowNetworkNode> nodes_sorted = this.SortNodesInFlowDirection();
                            if (nodes_sorted[0] is SimFlowNetwork)
                                return (nodes_sorted[0] as SimFlowNetwork).ConnectionToParentEntryNode;
                            else
                                return nodes_sorted[0];
                        }
                        else
                            return this.ContainedNodes.ElementAt(0).Value;
                    }
                    else
                        return this;
                }
            }
        }
        /// <summary>
        /// Gets the node of the network that connects to its parent as an exit from the subnetwork.
        /// Corresponds to property <see cref="NodeEnd_ID"/>.
        /// </summary>
        public SimFlowNetworkNode ConnectionToParentExitNode
        {
            get
            {
                if (this.NodeEnd_ID > -1 && this.ContainedNodes.ContainsKey(this.NodeEnd_ID))
                    return this.ContainedNodes[this.NodeEnd_ID];
                else
                {
                    if (this.ContainedNodes.Count > 0)
                    {
                        if (this.IsDirected)
                        {
                            List<SimFlowNetworkNode> nodes_sorted = this.SortNodesInFlowDirection();
                            if (nodes_sorted[nodes_sorted.Count - 1] is SimFlowNetwork)
                                return (nodes_sorted[nodes_sorted.Count - 1] as SimFlowNetwork).ConnectionToParentExitNode;
                            else
                                return nodes_sorted[nodes_sorted.Count - 1];
                        }
                        else
                            return this.ContainedNodes.ElementAt(0).Value;
                    }
                    else
                        return this;
                }
            }
        }

        #endregion

        #region PROPERTIES: First(Start, Source) Node, Last(End, Sink) Node

        private long node_start_id;
        public long NodeStart_ID
        {
            get { return this.node_start_id; }
            set
            {
                if (this.node_start_id != value && value > -1 &&
                    (this.contained_nodes.ContainsKey(value) || this.ContainedFlowNetworks.ContainsKey(value)))
                {
                    var old_value = this.node_start_id;
                    this.node_start_id = value;
                    this.NotifyPropertyChanged(nameof(NodeStart_ID));
                }
            }
        }

        private long node_end_id;
        public long NodeEnd_ID
        {
            get { return this.node_end_id; }
            set
            {
                if (this.node_end_id != value && value > -1 &&
                    (this.contained_nodes.ContainsKey(value) || this.ContainedFlowNetworks.ContainsKey(value)))
                {
                    var old_value = this.node_end_id;
                    this.node_end_id = value;
                    this.NotifyPropertyChanged(nameof(NodeEnd_ID));
                }
            }
        }

        #endregion

        #region PROPERTIES (derived): all contained elements as Children

        private CompositeCollection children;
        [Obsolete]
        public CompositeCollection Children { get { return this.children; } }

        [Obsolete]
        public void UpdateChildrenContainer()
        {
            this.SetValidity();
            this.children = new CompositeCollection
            {
                new CollectionContainer { Collection = this.contained_nodes.Values },
                new CollectionContainer { Collection = this.contained_edges.Values },
                new CollectionContainer { Collection = this.ContainedFlowNetworks.Values }
            };

            this.NotifyPropertyChanged(nameof(Children));
        }

        /// <summary>
        /// Tells whether the network has a parent
        /// </summary>
        public bool HasParent
        {
            get
            {
                if (this.ParentNetwork == null)
                {
                    return false;
                }
                return true;
            }
        }

        #endregion

        #region PROPERTIES: directed or not

        /// <summary>
        /// Indicates if the network should be regarded as a flow network capable of performing
        /// flow calculations or as an undirected cyclic graph. Once the network has been set to not directed, it cannot be converted back
        /// and cannot perfomr any flow calculations.
        /// </summary>
        /// <remarks>The handling of subnetworks is different for different types. For directed types, the subnetwork is part
        /// of the flow (e.g. it generally has a least one inflow and at least one outflow. For not directed types, each subnetwork
        /// has a anchor node, which serves as connection point to the larger network, i.e. the inflow and outflow are the same.</remarks>
        public bool IsDirected
        {
            get { return this.is_directed; }
            set
            {
                if (this.is_directed != value && this.is_directed)
                {
                    this.is_directed = value;
                    foreach (var entry in this.ContainedFlowNetworks)
                    {
                        entry.Value.IsDirected = value;
                    }
                    this.NotifyPropertyChanged(nameof(IsDirected));
                }
            }
        }
        private bool is_directed;

        #endregion

        #region PROPERTIES: synchronization

        /// <summary>
        /// Indicates if the network is currently in a consistent state
        /// </summary>
        public bool IsInConsistentState
        {
            get { return this.is_in_consistent_state; }
            private set
            {
                if (this.is_in_consistent_state != value)
                {
                    this.is_in_consistent_state = value;
                    if (this.Parent != null)
                    {
                        (this.Parent as SimFlowNetwork).IsInConsistentState &= this.is_in_consistent_state;
                    }
                    this.NotifyPropertyChanged(nameof(IsInConsistentState));
                }
            }
        }
        private bool is_in_consistent_state = true;

        #endregion

        #region EVENTS

        public delegate void ElementAddedEventHandler(object sender, SimFlowNetworkElement addedElement);
        public event ElementAddedEventHandler ElementAdded;

        public void OnElementAdded(SimFlowNetworkElement addedElement)
        {
            this.ElementAdded?.Invoke(this, addedElement);
        }

        public delegate void ElementDeletedEventHandler(object sender, SimFlowNetworkElement deletedElement);
        public event ElementDeletedEventHandler ElementDeleted;

        public void OnElementDeleted(SimFlowNetworkElement deletedElement)
        {
            this.ElementDeleted?.Invoke(this, deletedElement);
        }

        public delegate void EdgeRedirectedEventHandler(object sender, SimFlowNetworkEdge redirectedEdge);
        public event EdgeRedirectedEventHandler EdgeRedirected;

        public void OnEdgeRedirected(SimFlowNetworkEdge redirectedEdge)
        {
            this.EdgeRedirected?.Invoke(this, redirectedEdge);
        }

        public delegate void ElementTopologyChangedEventHandler(object sender, SimFlowNetworkElement oldElement, SimFlowNetworkElement newElement, List<SimFlowNetworkElement> changedElements);
        public event ElementTopologyChangedEventHandler ElementTopologyChanged;

        public void OnElementTopologyChanged(SimFlowNetworkElement oldElement, SimFlowNetworkElement newElement, List<SimFlowNetworkElement> changedElements)
        {
            this.ElementTopologyChanged?.Invoke(this, oldElement, newElement, changedElements);
        }

        #endregion

        #region .CTOR

        public SimFlowNetwork(IReferenceLocation _location, Point _position, string _name, string _description, SimUserRole _manager)
            : base(_location, _position)
        {
            if (_name == null)
                throw new ArgumentNullException(nameof(_name));

            this.name = _name;
            this.description = _description;

            this.manager = _manager;
            this.index_of_geometric_rep_file = -1;

            this.contained_nodes = new ObservableDictionary<long, SimFlowNetworkNode>();
            this.contained_nodes.CollectionChanged += ContainedNodes_CollectionChanged;
            this.contained_edges = new ObservableDictionary<long, SimFlowNetworkEdge>();
            this.contained_edges.CollectionChanged += ContainedEdges_CollectionChanged;
            this.ContainedFlowNetworks = new ObservableDictionary<long, SimFlowNetwork>();

            this.node_start_id = -1;
            this.node_end_id = -1;

            this.is_directed = true;

            // traceability state: ON
            this.Network = this;
        }
        #endregion

        #region COPY .CTOR

        internal SimFlowNetwork(SimFlowNetwork _original)
            : base(_original)
        {
            this.manager = _original.manager;
            this.index_of_geometric_rep_file = -1;

            this.contained_edges = new ObservableDictionary<long, SimFlowNetworkEdge>();
            this.contained_nodes = new ObservableDictionary<long, SimFlowNetworkNode>();
            this.ContainedFlowNetworks = new ObservableDictionary<long, SimFlowNetwork>();

            // NODES
            Dictionary<long, SimFlowNetworkNode> id_old_node_new = new Dictionary<long, SimFlowNetworkNode>();
            foreach (SimFlowNetworkNode node in _original.contained_nodes.Values)
            {
                SimFlowNetworkNode node_copy = new SimFlowNetworkNode(node);
                id_old_node_new.Add(node.ID.LocalId, node_copy);
                this.contained_nodes.Add(node_copy.ID.LocalId, node_copy);
            }
            this.contained_nodes.CollectionChanged += ContainedNodes_CollectionChanged;

            // NETWORKS
            Dictionary<long, SimFlowNetwork> id_old_network_new = new Dictionary<long, SimFlowNetwork>();
            foreach (SimFlowNetwork nw in _original.ContainedFlowNetworks.Values)
            {
                SimFlowNetwork nw_copy = new SimFlowNetwork(nw);
                id_old_network_new.Add(nw.ID.LocalId, nw_copy);
                this.ContainedFlowNetworks.Add(nw_copy.ID.LocalId, nw_copy);
            }

            // EDGES
            foreach (SimFlowNetworkEdge edge in _original.contained_edges.Values)
            {
                SimFlowNetworkNode start_copy = null;
                if (edge.Start != null)
                {
                    if (edge.Start is SimFlowNetwork && id_old_network_new.ContainsKey(edge.Start.ID.LocalId))
                        start_copy = id_old_network_new[edge.Start.ID.LocalId];
                    else if (id_old_node_new.ContainsKey(edge.Start.ID.LocalId))
                        start_copy = id_old_node_new[edge.Start.ID.LocalId];
                }

                SimFlowNetworkNode end_copy = null;
                if (edge.End != null)
                {
                    if (edge.End is SimFlowNetwork && id_old_network_new.ContainsKey(edge.End.ID.LocalId))
                        end_copy = id_old_network_new[edge.End.ID.LocalId];
                    else if (id_old_node_new.ContainsKey(edge.End.ID.LocalId))
                        end_copy = id_old_node_new[edge.End.ID.LocalId];
                }

                SimFlowNetworkEdge edge_copy = new SimFlowNetworkEdge(edge, start_copy, end_copy);

                // establish connection to nodes
                if (start_copy != null)
                {
                    start_copy.Edges_Out.Add(edge_copy);
                    start_copy.SetValidity();
                }
                if (end_copy != null)
                {
                    end_copy.Edges_In.Add(edge_copy);
                    end_copy.SetValidity();
                }

                this.contained_edges.Add(edge_copy.ID.LocalId, edge_copy);
            }
            this.contained_edges.CollectionChanged += ContainedEdges_CollectionChanged;

            // reset to -1
            this.node_start_id = -1;
            this.node_end_id = -1;

            // copy the references to the copies of the start and end nodes
            if (id_old_node_new.ContainsKey(_original.node_start_id))
                this.node_start_id = id_old_node_new[_original.node_start_id].ID.LocalId;
            if (id_old_network_new.ContainsKey(_original.node_start_id))
                this.node_start_id = id_old_network_new[_original.node_start_id].ID.LocalId;

            if (id_old_node_new.ContainsKey(_original.node_end_id))
                this.node_end_id = id_old_node_new[_original.node_end_id].ID.LocalId;
            if (id_old_network_new.ContainsKey(_original.node_end_id))
                this.node_end_id = id_old_network_new[_original.node_end_id].ID.LocalId;

            this.is_directed = _original.is_directed;

            this.Network = this;
        }

        #endregion

        #region PARSING .CTOR

        internal SimFlowNetwork(Guid _location, long _id, string _name, string _description, bool _is_valid, Point _position,
                            SimUserRole _manager, int _index_of_geometry_rep_file,
                            IEnumerable<SimFlowNetworkNode> _nodes, IEnumerable<SimFlowNetworkEdge> _edges,
                            IEnumerable<SimFlowNetwork> _subnetworks,
                            long _node_start_id, long _node_end_id, bool _is_directed, IEnumerable<SimFlowNetworkCalcRule> _calc_rules)
            : base(_location, _id, _name, _description, _is_valid, _position, _calc_rules)
        {
            // base (SimFlowNetworkElement)
            this.is_valid = _is_valid;

            this.contained_nodes = new ObservableDictionary<long, SimFlowNetworkNode>();
            this.contained_edges = new ObservableDictionary<long, SimFlowNetworkEdge>();
            this.ContainedFlowNetworks = new ObservableDictionary<long, SimFlowNetwork>();

            // base (SimFlowNetworkNode)
            this.position = _position;
            this.Edges_In = new ObservableCollection<SimFlowNetworkEdge>();
            this.Edges_Out = new ObservableCollection<SimFlowNetworkEdge>();

            // contained entities (FlowNetwork)
            // add nodes
            if (_nodes != null)
            {
                foreach (SimFlowNetworkNode n in _nodes)
                {
                    this.contained_nodes.Add(n.ID.LocalId, n);
                    n.Network = this;
                }
            }
            this.contained_nodes.CollectionChanged += ContainedNodes_CollectionChanged;

            // add subnetworks            
            if (_subnetworks != null)
            {
                foreach (SimFlowNetwork nw in _subnetworks)
                {
                    this.ContainedFlowNetworks.Add(nw.ID.LocalId, nw);
                    nw.ParentNetwork = this;
                }
            }

            // add edges
            if (_edges != null)
            {
                foreach (SimFlowNetworkEdge e in _edges)
                {
                    e.RestoreReferences(this.ContainedNodes, this.ContainedFlowNetworks);

                    // establish connection to nodes
                    if (e.Start != null)
                    {
                        e.Start.Edges_Out.Add(e);
                        // e.Start.SetValidity();
                    }
                    if (e.End != null)
                    {
                        e.End.Edges_In.Add(e);
                        // e.End.SetValidity();
                    }
                    this.contained_edges.Add(e.ID.LocalId, e);
                    e.Network = this;
                }
            }
            this.contained_edges.CollectionChanged += ContainedEdges_CollectionChanged;
            this.UpdateChildrenContainer();

            // parse start (source) and end (sink)
            this.node_start_id = _node_start_id;
            this.node_end_id = _node_end_id;

            this.is_directed = _is_directed;

            // finalize (FlowNetwork)
            this.manager = _manager;
            this.index_of_geometric_rep_file = _index_of_geometry_rep_file;

            this.Network = this;
        }


        #endregion

        #region METHODS: Overrides

        internal override void SetValidity()
        {
            base.SetValidity(); // SimFlowNetworkNode
            bool is_valid = this.IsValid;
            if (!is_valid) return;

            foreach (var entry in this.contained_nodes)
            {
                is_valid &= entry.Value.IsValid;
            }
            foreach (var entry in this.contained_edges)
            {
                is_valid &= entry.Value.IsValid;
            }
            foreach (var entry in this.ContainedFlowNetworks)
            {
                is_valid &= entry.Value.IsValid;
            }

            this.IsValid = is_valid;
        }

        #endregion

        #region METHODS: Info

        public List<string> GetUniqueParamNamesInContent()
        {
            HashSet<SimComponent> content = new HashSet<SimComponent>();
            foreach (var entry in this.contained_nodes.Values)
            {
                if (entry.Content != null && !content.Contains(entry.Content.Component))
                    content.Add(entry.Content.Component);
            }

            return GetUniqueParameterNamesFor(content);
        }
        /// <summary>
        /// Extracts the unique parameter names of the parameters contained in the given 
        /// component list. Searches in their sub-components as well.
        /// </summary>
        /// <param name="_comps">list of arbitrary components</param>
        /// <returns>a list of unique strings or an empty list</returns>
        private List<string> GetUniqueParameterNamesFor(IEnumerable<SimComponent> _comps)
        {
            if (_comps == null)
                throw new ArgumentNullException(nameof(_comps));

            HashSet<string> names = new HashSet<string>();

            foreach (SimComponent c in _comps)
            {
                var ps = ComponentWalker.GetFlatParameters(c);
                foreach (var p in ps)
                {
                    if (!names.Contains(p.NameTaxonomyEntry.Name))
                        names.Add(p.NameTaxonomyEntry.Name);
                }
            }
            return names.ToList();
        }

        // updated 16.08.2017
        public List<SimComponent> GetAllContent()
        {
            HashSet<SimComponent> contents = new HashSet<SimComponent>();
            foreach (var entry in this.contained_nodes.Values)
            {
                if (entry.Content != null && !contents.Contains(entry.Content.Component))
                    contents.Add(entry.Content.Component);
            }
            foreach (var entry in this.contained_edges.Values)
            {
                if (entry.Content != null && !contents.Contains(entry.Content.Component))
                    contents.Add(entry.Content.Component);
            }
            foreach (var entry in this.ContainedFlowNetworks.Values)
            {
                List<SimComponent> nw_contents = entry.GetAllContent();
                foreach (SimComponent nw_c in nw_contents)
                {
                    SimComponent duplicate = contents.FirstOrDefault(x => x.Id == nw_c.Id);
                    if (duplicate != null) continue;

                    contents.Add(nw_c);
                }
            }

            return contents.ToList();
        }

        internal IEnumerable<SimFlowNetworkElement> GetAllElementsWithContent()
        {
            List<SimFlowNetworkElement> contents = new List<SimFlowNetworkElement>();
            foreach (var entry in this.contained_nodes.Values)
            {
                if (entry.Content != null)
                    contents.Add(entry);
            }
            foreach (var entry in this.contained_edges.Values)
            {
                if (entry.Content != null)
                    contents.Add(entry);
            }
            foreach (var entry in this.ContainedFlowNetworks.Values)
            {
                var nw_contents = entry.GetAllElementsWithContent();
                contents.AddRange(nw_contents);
            }

            return contents;
        }

        internal override SimDoubleParameter GetFirstParamBySuffix(string _suffix, bool _in_flow_dir)
        {
            if (string.IsNullOrEmpty(_suffix)) return null;

            long id = -1;
            if (_in_flow_dir)
            {
                if (this.node_end_id < 0) return null;
                id = this.node_end_id;
            }
            else
            {
                if (this.node_start_id < 0) return null;
                id = this.node_start_id;
            }

            if (this.contained_nodes.ContainsKey(id))
            {
                return this.contained_nodes[id].GetFirstParamBySuffix(_suffix, _in_flow_dir);
            }
            else if (this.ContainedFlowNetworks.ContainsKey(id))
            {
                return this.ContainedFlowNetworks[id].GetFirstParamBySuffix(_suffix, _in_flow_dir);
            }

            return null;
        }

        public List<SimFlowNetworkElement> GetAllContainersOf(SimComponent _comp)
        {
            if (_comp == null)
                return new List<SimFlowNetworkElement>();

            List<SimFlowNetworkElement> elements = new List<SimFlowNetworkElement>();
            foreach (var instance in _comp.Instances)
            {
                foreach (var placement in instance.Placements.Where(x => x is SimInstancePlacementNetwork))
                {
                    var networkPlacement = (SimInstancePlacementNetwork)placement;
                    if (networkPlacement.NetworkElement != null)
                        elements.Add(networkPlacement.NetworkElement);
                }
            }

            return elements;
        }

        #endregion

        #region METHODS: record merging

        /// <summary>
        /// Use when merging records in the ComponentFactory.
        /// </summary>
        /// <param name="max_current_id"></param>
        /// <returns></returns>
        internal Dictionary<long, long> UpdateAllElementIds(ref long max_current_id)
        {
            Dictionary<long, long> old_new = new Dictionary<long, long>();
            List<SimFlowNetworkElement> to_change_id = new List<SimFlowNetworkElement>();

            // gather all ids (recursively)
            List<long> all_ids = this.GetAllElementIds();
            all_ids.Add(this.ID.LocalId);

            // record with which to merge: 0 1 2 3 -> max_current_id = 3
            // this nw: 6 7 12 14
            if (all_ids.Min() > max_current_id)
                return old_new;

            // record with which to merge: 0 1 2 3 -> max_current_id = 3
            // this nw: 2 3 6 7
            // shift all ids by an offset (3 - 2 + 1) = 2 -> 2 3 6 7 --> 4 5 8 9
            long offset = max_current_id - all_ids.Min() + 1;
            max_current_id = all_ids.Max() + offset; // 9

            foreach (long id in all_ids)
            {
                old_new.Add(id, id + offset);
            }

            this.ShiftContainedElementIds(old_new);
            this.ID.LocalId = old_new[this.ID.LocalId];

            return old_new;
        }

        internal List<long> GetAllElementIds()
        {
            List<long> all_ids = new List<long>();
            all_ids.AddRange(this.contained_nodes.Keys);
            all_ids.AddRange(this.contained_edges.Keys);
            all_ids.AddRange(this.ContainedFlowNetworks.Keys);

            foreach (var entry in this.ContainedFlowNetworks)
            {
                all_ids.AddRange(entry.Value.GetAllElementIds());
            }

            return all_ids;
        }

        //Shifts the IDs
        protected void ShiftContainedElementIds(Dictionary<long, long> _old_new)
        {
            // perform shift: Nodes
            List<SimFlowNetworkNode> nodes = this.contained_nodes.Values.ToList();
            this.contained_nodes.CollectionChanged -= ContainedNodes_CollectionChanged;
            this.contained_nodes = new ObservableDictionary<long, SimFlowNetworkNode>();
            if (nodes != null && nodes.Count > 0)
            {
                foreach (SimFlowNetworkNode n in nodes)
                {
                    n.ID.LocalId = _old_new[n.ID.LocalId];
                    this.contained_nodes.Add(n.ID.LocalId, n);
                }
            }
            this.contained_nodes.CollectionChanged += ContainedNodes_CollectionChanged;

            // perform shift: Edges
            List<SimFlowNetworkEdge> edges = this.contained_edges.Values.ToList();
            this.contained_edges.CollectionChanged -= ContainedEdges_CollectionChanged;
            this.contained_edges = new ObservableDictionary<long, SimFlowNetworkEdge>();
            if (edges != null && edges.Count > 0)
            {
                foreach (SimFlowNetworkEdge e in edges)
                {
                    e.ID.LocalId = _old_new[e.ID.LocalId];
                    this.contained_edges.Add(e.ID.LocalId, e);
                }
            }
            this.contained_edges.CollectionChanged += ContainedEdges_CollectionChanged;

            // perform shift: Networks
            List<SimFlowNetwork> subnetworks = this.ContainedFlowNetworks.Values.ToList();
            this.ContainedFlowNetworks = new ObservableDictionary<long, SimFlowNetwork>();
            if (subnetworks != null && subnetworks.Count > 0)
            {
                foreach (SimFlowNetwork nw in subnetworks)
                {
                    nw.ID.LocalId = _old_new[nw.ID.LocalId];
                    nw.ShiftContainedElementIds(_old_new);
                    //nw.SaveableChangeRecorded += NW_SaveableChangeRecorded;
                    this.ContainedFlowNetworks.Add(nw.ID.LocalId, nw);
                }
            }
        }


        #endregion

        #region METHODS: Add, Remove Nodes and Edges

        public long AddNode(Point _pos)
        {
            SimFlowNetworkNode node = new SimFlowNetworkNode(this.id.GlobalLocation, _pos);
            if (this.contained_nodes.ContainsKey(node.ID.LocalId)) return -1;

            this.ContainedNodes.Add(node.ID.LocalId, node);
            node.Network = this;
            this.OnElementAdded(node); // added 15.02.2019
            return node.ID.LocalId;
        }

        internal long AddFlowNetwork(Point _pos, string _name, string _description)
        {
            SimFlowNetwork nw = new SimFlowNetwork(this.id.GlobalLocation, _pos, _name, _description, this.manager);
            if (this.ContainedFlowNetworks.ContainsKey(nw.ID.LocalId)) return -1;

            // set the content of the network
            if (!this.IsDirected)
                nw.IsDirected = false;
            long n1_id = nw.AddNode(new Point(_pos.X - 200, _pos.Y));
            long n2_id = nw.AddNode(new Point(_pos.X + 200, _pos.Y));
            nw.node_start_id = n1_id;
            nw.node_end_id = n2_id;
            long e1 = nw.AddEdge(nw.contained_nodes[n1_id], nw.contained_nodes[n2_id]);

            this.ContainedFlowNetworks.Add(nw.ID.LocalId, nw);
            this.OnElementAdded(nw); // added 15.02.2019
            nw.ParentNetwork = this;
            return nw.ID.LocalId;
        }

        internal bool AddFlowNetwork(SimFlowNetwork _to_add)
        {
            if (_to_add == null) return false;
            if (this.ContainedFlowNetworks.ContainsKey(_to_add.ID.LocalId)) return false;
            if (!this.IsDirected)
                _to_add.IsDirected = false;

            this.ContainedFlowNetworks.Add(_to_add.ID.LocalId, _to_add);
            this.OnElementAdded(_to_add); // added 15.02.2019
            _to_add.ParentNetwork = this;
            return true;
        }

        public bool RemoveNodeOrNetwork(SimFlowNetworkNode _node)
        {
            bool is_flow_network = _node is SimFlowNetwork;
            if (is_flow_network && !this.ContainedFlowNetworks.ContainsKey(_node.ID.LocalId)) return false;
            if (!is_flow_network && !this.contained_nodes.ContainsKey(_node.ID.LocalId)) return false;

            // alert contained component(S)
            if (_node.Content != null)
            {
                _node.Content.Component.Instances.Remove(_node.Content);
            }
            if (is_flow_network)
            {
                SimFlowNetwork fnw = _node as SimFlowNetwork;
                var contents = fnw.GetAllElementsWithContent();
                foreach (var entry in contents)
                {
                    entry.Content.Component.Instances.Remove(entry.Content);
                }
            }

            // remove all connections
            bool success = true;
            foreach (SimFlowNetworkEdge edge in _node.Edges_In)
            {
                if (this.contained_edges.ContainsKey(edge.ID.LocalId))
                {
                    if (edge.Content != null)
                        edge.Content.Component.Instances.Remove(edge.Content);

                    success &= edge.Start.Edges_Out.Remove(edge);
                    success &= this.ContainedEdges.Remove(edge.ID.LocalId);
                }
                else
                    success = false;
            }
            foreach (SimFlowNetworkEdge edge in _node.Edges_Out)
            {
                if (this.contained_edges.ContainsKey(edge.ID.LocalId))
                {
                    if (edge.Content != null)
                        edge.Content.Component.Instances.Remove(edge.Content);

                    success &= edge.End.Edges_In.Remove(edge);
                    success &= this.ContainedEdges.Remove(edge.ID.LocalId);
                }
                else
                    success = false;
            }

            if (is_flow_network)
                success &= this.ContainedFlowNetworks.Remove(_node.ID.LocalId);
            else
                success &= this.ContainedNodes.Remove(_node.ID.LocalId);

            if (success)
                this.OnElementDeleted(_node); // added 15.02.2019
            return success;
        }

        public long AddEdge(SimFlowNetworkNode _start, SimFlowNetworkNode _end)
        {
            if (_start == null || _end == null) return -1;
            if (!this.contained_nodes.ContainsKey(_start.ID.LocalId) &&
                !this.ContainedFlowNetworks.ContainsKey(_start.ID.LocalId)) return -1;
            if (!this.contained_nodes.ContainsKey(_end.ID.LocalId) &&
                !this.ContainedFlowNetworks.ContainsKey(_end.ID.LocalId)) return -1;

            SimFlowNetworkEdge edge = new SimFlowNetworkEdge(this.ID.GlobalLocation, _start, _end);
            if (!edge.IsValid) return -1;
            if (this.contained_edges.ContainsKey(edge.ID.LocalId)) return -1;

            // establish connection to nodes
            _start.Edges_Out.Add(edge);
            _end.Edges_In.Add(edge);
            _start.SetValidity();
            _end.SetValidity();

            this.ContainedEdges.Add(edge.ID.LocalId, edge);
            this.OnElementAdded(edge); // added 15.02.2019
            edge.Network = this;
            return edge.ID.LocalId;
        }

        public bool RemoveEdge(SimFlowNetworkEdge _edge)
        {
            if (_edge == null) return false;
            if (!this.contained_edges.ContainsKey(_edge.ID.LocalId)) return false;

            // alert contained component
            if (_edge.Content != null)
            {
                _edge.Content.Component.Instances.Remove(_edge.Content);
            }

            // remove connection to nodes
            if (_edge.Start != null)
                _edge.Start.Edges_Out.Remove(_edge);
            if (_edge.End != null)
                _edge.End.Edges_In.Remove(_edge);

            _edge.Start.SetValidity();
            _edge.End.SetValidity();

            this.OnElementDeleted(_edge); // added 15.02.2019
            return this.ContainedEdges.Remove(_edge.ID.LocalId);
        }

        #endregion

        #region METHODS: Redirect Edges

        public bool RedirectEdge(SimFlowNetworkEdge _edge, bool _rerout_start, SimFlowNetworkNode _to_node)
        {
            if (_edge == null || _to_node == null) return false;

            if (!this.contained_edges.ContainsKey(_edge.ID.LocalId)) return false;
            if (!this.ContainedFlowNetworks.ContainsKey(_to_node.ID.LocalId) &&
                !this.contained_nodes.ContainsKey(_to_node.ID.LocalId)) return false;

            if (_rerout_start)
            {
                // detach from old node
                if (_edge.Start != null)
                    _edge.Start.Edges_Out.Remove(_edge);
                _edge.Start.SetValidity();

                // attach to new node
                _edge.Start = _to_node;
                _to_node.Edges_Out.Add(_edge);
            }
            else
            {
                // detach from old node
                if (_edge.End != null)
                    _edge.End.Edges_In.Remove(_edge);
                _edge.End.SetValidity();

                // attach to new node
                _edge.End = _to_node;
                _to_node.Edges_In.Add(_edge);
            }

            _to_node.SetValidity();
            this.OnEdgeRedirected(_edge); // added 15.02.2019
            return true;
        }

        #endregion

        #region METHODS: Convert between Node and Subnetwork

        internal SimFlowNetwork NodeToNetwork(SimFlowNetworkNode _node)
        {
            this.IsInConsistentState = false;

            if (_node == null) return null;
            if (!this.contained_nodes.ContainsKey(_node.ID.LocalId)) return null;
            if (_node is SimFlowNetwork && this.ContainedFlowNetworks.ContainsKey(_node.ID.LocalId)) return _node as SimFlowNetwork;

            // create a new network
            SimFlowNetwork created = new SimFlowNetwork(this.id.GlobalLocation, _node.Position, _node.Name, _node.Description, this.manager);
            if (!this.IsDirected)
                created.IsDirected = false;
            long n1_id = created.AddNode(new Point(_node.Position.X - 200, _node.Position.Y));
            long n2_id = created.AddNode(new Point(_node.Position.X + 200, _node.Position.Y));
            created.node_start_id = n1_id;
            created.node_end_id = n2_id;
            long e1 = created.AddEdge(created.contained_nodes[n1_id], created.contained_nodes[n2_id]);

            // redirect edges
            List<SimFlowNetworkElement> redirected = new List<SimFlowNetworkElement>();
            foreach (SimFlowNetworkEdge e_in in _node.Edges_In)
            {
                e_in.End = created;
                created.Edges_In.Add(e_in);
                redirected.Add(e_in);
            }
            _node.Edges_In.Clear();
            foreach (SimFlowNetworkEdge e_out in _node.Edges_Out)
            {
                e_out.Start = created;
                created.Edges_Out.Add(e_out);
                redirected.Add(e_out);
            }
            _node.Edges_Out.Clear();

            // transfer information from the node to the first node of the network
            if (_node.Content != null)
            {
                SimFlowNetworkElement.TransferContent(_node, created.contained_nodes[n1_id]);
            }

            // delete node
            this.ContainedNodes.Remove(_node.ID.LocalId);

            // add network
            this.ContainedFlowNetworks.Add(created.ID.LocalId, created);

            this.OnElementTopologyChanged(_node, created, redirected); // added 15.02.2019
            this.IsInConsistentState = true;
            return created;
        }

        internal SimFlowNetworkNode NetworkToNode(SimFlowNetwork _nw)
        {
            this.IsInConsistentState = false;

            if (_nw == null) return null;
            if (!this.ContainedFlowNetworks.ContainsKey(_nw.ID.LocalId)) return null;

            // create a new node
            SimFlowNetworkNode created = new SimFlowNetworkNode(this.ID.GlobalLocation, _nw.Position);
            created.Name = _nw.Name;
            created.Description = _nw.Description;

            // redirect edges
            List<SimFlowNetworkElement> redirected = new List<SimFlowNetworkElement>();
            foreach (SimFlowNetworkEdge e_in in _nw.Edges_In)
            {
                e_in.End = created;
                created.Edges_In.Add(e_in);
                redirected.Add(e_in);
            }
            _nw.Edges_In.Clear();
            foreach (SimFlowNetworkEdge e_out in _nw.Edges_Out)
            {
                e_out.Start = created;
                created.Edges_Out.Add(e_out);
                redirected.Add(e_out);
            }
            _nw.Edges_Out.Clear();

            // alert content(S)
            if (_nw.Content != null)
                _nw.Content.Component.Instances.Remove(_nw.Content);
            var contents = _nw.GetAllElementsWithContent();
            foreach (var entry in contents)
            {
                if (entry.Content != null)
                    entry.Content.Component.Instances.Remove(entry.Content);
            }
            // delete network
            this.ContainedFlowNetworks.Remove(_nw.ID.LocalId);

            // add node
            this.ContainedNodes.Add(created.ID.LocalId, created);

            this.OnElementTopologyChanged(_nw, created, redirected); // added 15.02.2019
            this.IsInConsistentState = true;
            return created;
        }

        #endregion

        #region METHODS: ToString

        public override string ToString()
        {
            string fl_net_str = "FlowNetwork " + this.ID.ToString() + " " + this.Name + " " + this.Description + " ";
            fl_net_str += "[ " + this.contained_nodes.Count() + " nodes, " + this.contained_edges.Count() + " edges ]";

            return fl_net_str;
        }

        #endregion

        #region METHODS: Sorting acc. to flow direction

        protected List<SimFlowNetworkNode> SortNodesInFlowDirection()
        {
            List<SimFlowNetworkNode> sorted = new List<SimFlowNetworkNode>();
            if (this.contained_nodes == null || this.ContainedFlowNetworks == null)
                return sorted;

            // inialize sorting record
            Dictionary<long, bool> was_sorted = new Dictionary<long, bool>();
            foreach (var entry in this.contained_nodes)
            {
                was_sorted.Add(entry.Key, false);
            }
            foreach (var entry in this.ContainedFlowNetworks)
            {
                was_sorted.Add(entry.Key, false);
            }

            int nr_nodes_added = 0;
            // 1. find first all nodes with NO INCOMING EDGES
            foreach (var entry in this.contained_nodes)
            {
                SimFlowNetworkNode n = entry.Value;
                if (n == null) continue;

                if (n.Edges_In.Count > 0)
                    continue;
                else
                {
                    if (!was_sorted[entry.Key])
                    {
                        sorted.Add(n);
                        nr_nodes_added++;
                        was_sorted[entry.Key] = true;
                    }
                }
            }
            foreach (var entry in this.ContainedFlowNetworks)
            {
                SimFlowNetwork nw = entry.Value;
                if (nw == null) continue;

                if (nw.Edges_In.Count > 0)
                    continue;
                else
                {
                    if (!was_sorted[entry.Key])
                    {
                        sorted.Add(nw);
                        nr_nodes_added++;
                        was_sorted[entry.Key] = true;
                    }
                }
            }
            // if there were no such nodes... just take the first node in the list, as the network is a cycle
            if (sorted.Count == 0)
            {
                SimFlowNetworkNode n1 = null;
                if (this.ContainedNodes.Count > 0)
                    n1 = this.ContainedNodes.ElementAt(0).Value;
                else if (this.ContainedFlowNetworks.Count > 0)
                    n1 = this.ContainedFlowNetworks.ElementAt(0).Value;
                if (n1 != null)
                {
                    sorted.Add(n1);
                    nr_nodes_added++;
                    was_sorted[n1.ID.LocalId] = true;
                }
            }

            // 2. and START from them or end, if nothing was added
            // but only add nodes WITH OUTGOING EDGES
            while (nr_nodes_added > 0)
            {
                int nr_nodes_added_old = nr_nodes_added;
                nr_nodes_added = 0;
                for (int i = sorted.Count - 1; i >= sorted.Count - nr_nodes_added_old; i--)
                {
                    SimFlowNetworkNode n = sorted[i];
                    if (n.Edges_Out.Count == 0) continue;

                    foreach (SimFlowNetworkEdge e_out in n.Edges_Out)
                    {
                        if (e_out.End != null && !was_sorted[e_out.End.ID.LocalId] && e_out.End.Edges_Out.Count > 0)
                        {
                            sorted.Add(e_out.End);
                            nr_nodes_added++;
                            was_sorted[e_out.End.ID.LocalId] = true;
                        }
                    }
                }

                if (nr_nodes_added == 0) break;
            }

            // 3. finally start from the sorted nodes
            // and add the ones W/O OUTGOING EDGES
            nr_nodes_added = was_sorted.Count(x => x.Value == true);
            while (nr_nodes_added > 0)
            {
                int nr_nodes_added_old = nr_nodes_added;
                nr_nodes_added = 0;
                for (int i = sorted.Count - 1; i >= 0; i--)
                {
                    SimFlowNetworkNode n = sorted[i];
                    if (n.Edges_Out.Count == 0) continue;

                    foreach (SimFlowNetworkEdge e_out in n.Edges_Out)
                    {
                        if (e_out.End != null && !was_sorted[e_out.End.ID.LocalId])
                        {
                            sorted.Add(e_out.End);
                            nr_nodes_added++;
                            was_sorted[e_out.End.ID.LocalId] = true;
                        }
                    }
                }

                if (nr_nodes_added == 0) break;
            }

            return sorted;
        }

        protected List<SimFlowNetworkNode> SortNodesInFlowDirectionUnfoldNW()
        {
            this.ResetNested();
            //Console.WriteLine("------------------- START SORTING --------------------");
            List<SimFlowNetworkNode> sorted_all = this.SortNodesInFlowDirection();
            //Console.WriteLine("---");
            //sorted_all.ForEach(x => Console.WriteLine(x.ToConnectivityInfo()));
            int nr_foldedNW = sorted_all.Count(x => x is SimFlowNetwork);

            // even if there are no nested networks, copy the edges to the appropriate lists
            if (nr_foldedNW == 0)
            {
                foreach (SimFlowNetworkNode n in sorted_all)
                {
                    n.TransferConnectionsToNested();
                }
            }

            while (nr_foldedNW > 0)
            {
                List<SimFlowNetworkNode> sorted_unfolded = new List<SimFlowNetworkNode>();
                foreach (SimFlowNetworkNode n in sorted_all)
                {
                    n.TransferConnectionsToNested();
                    if (n is SimFlowNetwork)
                    {
                        SimFlowNetwork fnw = n as SimFlowNetwork;
                        List<SimFlowNetworkNode> fnw_sorted = fnw.SortNodesInFlowDirection();
                        int nr_unfolded = fnw_sorted.Count;
                        // connect properly for value propagation
                        if (nr_unfolded > 0)
                        {
                            foreach (SimFlowNetworkNode fnw_n in fnw_sorted)
                            {
                                fnw_n.TransferConnectionsToNested();
                            }
                            fnw_sorted[0].Edges_In_Nested.AddRange(fnw.Edges_In_Nested);
                            fnw_sorted[nr_unfolded - 1].Edges_Out_Nested.AddRange(fnw.Edges_Out_Nested);
                        }
                        sorted_unfolded.AddRange(fnw_sorted);
                    }
                    else
                    {
                        sorted_unfolded.Add(n);
                    }
                }

                sorted_all = new List<SimFlowNetworkNode>(sorted_unfolded);
                //Console.WriteLine("---");
                //sorted_all.ForEach(x => Console.WriteLine(x.ToConnectivityInfo()));
                nr_foldedNW = sorted_all.Count(x => x is SimFlowNetwork);
            }

            //Console.WriteLine("-------------------- END SORTING --------------------");
            return sorted_all;
        }

        internal override void ResetNested()
        {
            base.ResetNested();
            foreach (var entry in this.ContainedNodes)
            {
                entry.Value.ResetNested();
            }
            foreach (var entry in this.ContainedFlowNetworks)
            {
                entry.Value.ResetNested();
            }
        }

        internal SimFlowNetworkNode SortAndGetFirstNode()
        {
            List<SimFlowNetworkNode> sorted = this.SortNodesInFlowDirectionUnfoldNW();
            if (sorted.Count > 0)
                return sorted[0];
            else
                return null;
        }

        public List<SimFlowNetworkNode> GetNestedNodes()
        {
            if (this.IsInConsistentState)
                return this.SortNodesInFlowDirectionUnfoldNW();
            else
                return new List<SimFlowNetworkNode>();
        }

        public List<SimFlowNetworkEdge> GetNestedEdges()
        {
            List<SimFlowNetworkNode> nodes = this.SortNodesInFlowDirectionUnfoldNW();
            List<SimFlowNetworkEdge> edges = new List<SimFlowNetworkEdge>();
            foreach (SimFlowNetworkNode n in nodes)
            {
                edges.AddRange(n.Edges_Out_Nested);
            }

            return edges.Distinct().ToList();
        }

        #endregion

        #region METHODS: Flow Calculation General

        public void ResetAllContentInstances(Point _offse_parent)
        {
            if (!this.IsDirected) return;

            Point offset = new Point();
            if (_offse_parent.X == 0 && _offse_parent.Y == 0)
            {
                offset = new Point(this.Position.X, this.Position.Y);
            }
            else
            {
                SimFlowNetworkNode first = this.SortAndGetFirstNode();
                if (first != null)
                    offset = new Point(_offse_parent.X - first.Position.X, _offse_parent.Y - first.Position.Y);
            }

            foreach (var entry in this.contained_nodes)
            {
                if (entry.Value == null) continue;
                entry.Value.ResetContentInstance(offset);
            }
            foreach (var entry in this.contained_edges)
            {
                if (entry.Value == null) continue;
                entry.Value.ResetContentInstance(offset);
            }
            foreach (var entry in this.ContainedFlowNetworks)
            {
                if (entry.Value == null) continue;
                entry.Value.ResetAllContentInstances(entry.Value.Position);
            }
        }

        public void CalculateAllFlows(bool _in_flow_dir)
        {
            if (!this.IsDirected) return;

            List<SimFlowNetworkNode> sorted = this.SortNodesInFlowDirectionUnfoldNW();
            if (_in_flow_dir)
            {
                // e.g. for exhaust air -> in the flow direction
            }
            else
            {
                // e.g. for air supply -> opposite the flow direction
                sorted.Reverse();
            }

            foreach (SimFlowNetworkNode n in sorted)
            {
                string node_conent_name = (n.Content == null) ? n.Name : n.Content.Name;
                // Debug.WriteLine("Calculating Node: " + node_conent_name);
                n.CalculateFlow(_in_flow_dir);
            }
        }

        public void PropagateCalculationRulesToAllInstances(SimFlowNetworkNode _source)
        {
            if (_source == null) return;
            if (_source.Content == null) return;

            foreach (var entry in this.contained_nodes)
            {
                if (entry.Value == null) continue;
                if (entry.Value.Content == null) continue;

                if (entry.Value.Content.Component.Id == _source.Content.Component.Id)
                    entry.Value.CalculationRules = new ObservableCollection<SimFlowNetworkCalcRule>(_source.CalculationRules);
            }
        }

        public void PropagateSizeToInstances(SimFlowNetworkElement _source, List<SimFlowNetworkElement> _targets)
        {
            if (_source == null || _source.Content == null || _targets == null) return;

            foreach (var target in _targets)
            {
                target.Content?.SetSize(_source.Content.InstanceSize.Clone(), _source.Content.SizeTransfer.Clone());
            }
        }

        #endregion

        #region METHODS: Step by Step Flow Calculation

        public List<SimFlowNetworkNode> PrepareToCalculateFlowStepByStep(bool _in_flow_dir)
        {
            if (!this.IsDirected) return new List<SimFlowNetworkNode>();

            List<SimFlowNetworkNode> sorted = this.SortNodesInFlowDirectionUnfoldNW();
            if (_in_flow_dir)
            {
                // e.g. for exhaust air -> in the flow direction
            }
            else
            {
                // e.g. for air supply -> opposite the flow direction
                sorted.Reverse();
            }
            return sorted;
        }

        public string CalculateFlowStep(List<SimFlowNetworkNode> _sorted_nodes, bool _in_flow_dir, int _step_index, bool _show_only_changes, out SimFlowNetworkNode _current_node)
        {
            _current_node = null;
            if (!this.IsDirected) return null;
            if (_sorted_nodes == null || _sorted_nodes.Count == 0) return null;
            if (_step_index < 0 || _sorted_nodes.Count - 1 < _step_index) return null;

            _current_node = _sorted_nodes[_step_index];
            if (_current_node == null) return null;
            var edges_prev = (_in_flow_dir) ? _current_node.Edges_In_Nested : _current_node.Edges_Out_Nested;

            // define feedback String
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("N " + _current_node.Name);

            // extract the relevant component instances
            SimComponentInstance instance_in_node = _current_node.Content;

            List<SimComponentInstance> instances_in_edges_prev = new List<SimComponentInstance>();
            if (edges_prev != null)
                instances_in_edges_prev = edges_prev.Select(x => x.Content).ToList();

            // document state BEFORE calculation step
            var values_in_node_BEFORE = instance_in_node.InstanceParameterValuesTemporary.GetRecords<SimDoubleParameter, double>();
            var values_in_edges_prev_BEFORE = instances_in_edges_prev.Select(x => x.InstanceParameterValuesTemporary.GetRecords<SimDoubleParameter, double>()).ToList();

            // CALCULATE FLOW
            _current_node.CalculateFlow(_in_flow_dir);

            // document state AFTER calculation step
            var values_in_edges_prev_AFTER = instances_in_edges_prev.Select(x => x.InstanceParameterValuesTemporary).ToList();

            // write the transitions
            SimFlowNetwork.ParallelDictionariesToString(values_in_node_BEFORE, instance_in_node.InstanceParameterValuesTemporary, _show_only_changes, ref sb);
            for (int i = 0; i < values_in_edges_prev_BEFORE.Count; i++)
            {
                if (edges_prev != null && edges_prev.Count > i)
                    sb.AppendLine("E " + edges_prev[i].Name);
                SimFlowNetwork.ParallelDictionariesToString(values_in_edges_prev_BEFORE[i], values_in_edges_prev_AFTER[i], _show_only_changes, ref sb);
            }

            return sb.ToString();
        }

        private const double VALUE_TOLERANCE = 0.0001;

        private static void ParallelDictionariesToString(List<KeyValuePair<SimDoubleParameter, double>> before, SimInstanceParameterCollection after, bool _show_only_differences, ref StringBuilder sb)
        {
            if (sb == null)
                sb = new StringBuilder();

            if (before != null && after != null && before.Count == after.Count)
            {
                for (int p = 0; p < before.Count; p++)
                {
                    var beforeItem = before[p];
                    var afterItem = after[beforeItem.Key];

                    if (_show_only_differences)
                    {
                        double diff = Math.Abs((double)beforeItem.Value - (double)afterItem);
                        if (diff < VALUE_TOLERANCE)
                            continue;
                    }

                    string line = "'" + beforeItem.Key.NameTaxonomyEntry.Name + "':";
                    line = line.PadRight(20, ' ') + "\t";
                    line += beforeItem.Value.ToString("F4") + " -> ";
                    line += ((double)afterItem).ToString("F4");
                    sb.AppendLine(line);
                }
            }

        }

        #endregion
    }
}
