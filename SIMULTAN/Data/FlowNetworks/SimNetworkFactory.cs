using SIMULTAN.Data.SimMath;
using SIMULTAN.Data.Users;
using SIMULTAN.Projects;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;

namespace SIMULTAN.Data.FlowNetworks
{
    // Base class is not fully integrated yet
    public partial class SimNetworkFactory : SimManagedCollection<int> //int is a workaround because the factory is not refactored yet
    {
        #region CLASS MEMBERS / GENERAL PROPERTIES:

        // flow networks
        public ObservableCollection<SimFlowNetwork> NetworkRecord { get; private set; }

        #endregion

        #region .CTOR

        public SimNetworkFactory(ProjectData owner) : base(owner)
        {
            this.NetworkRecord = new ObservableCollection<SimFlowNetwork>();
        }

        #endregion

        #region EVENTS NETWORKS

        public delegate void NetworkAddedEventHandler(object sender, IEnumerable<SimFlowNetwork> addedNetworks);

        public event NetworkAddedEventHandler NetworkAdded;

        public void OnNetworkAdded(IEnumerable<SimFlowNetwork> addedNetworks)
        {
            this.NetworkAdded?.Invoke(this, addedNetworks);
        }

        public delegate void NetworkDeletedEventHandler(object sender, IEnumerable<SimFlowNetwork> deletedNetworks);

        public event NetworkDeletedEventHandler NetworkDeleted;

        public void OnNetworkDeleted(IEnumerable<SimFlowNetwork> deletedNetworks)
        {
            this.NetworkDeleted?.Invoke(this, deletedNetworks);
        }

        #endregion

        #region METHODS: Factory Methods Networks

        public SimFlowNetwork CreateEmptyNetwork(string name, SimUserRole _user)
        {
            // create component
            var network_candidate = new SimFlowNetwork(this.CalledFromLocation, new SimPoint(0, 0), name, "- - -", _user);
            this.NetworkRecord.Add(network_candidate);
            this.OnNetworkAdded(new List<SimFlowNetwork> { network_candidate });

            // return
            return network_candidate;
        }

        [Obsolete]
        internal SimFlowNetwork ReconstructNetwork(Guid _location, long _id, string _name, string _description, bool _is_valid, SimPoint _position,
                                                SimUserRole _manager, int _index_of_geom_rep_file,
                                                IList<SimFlowNetworkNode> _nodes, IList<SimFlowNetworkEdge> _edges, IList<SimFlowNetwork> _subnetworks,
                                                long _node_start_id, long _node_end_id, bool _is_directed, List<SimFlowNetworkCalcRule> _calc_rules,
                                                bool _add_to_record)
        {
            // create
            Guid actual_location = (_location == Guid.Empty && this.CalledFromLocation != null) ? this.CalledFromLocation.GlobalID : _location;
            SimFlowNetwork created = new SimFlowNetwork(actual_location, _id, _name, _description, _is_valid, _position,
                                                  _manager, _index_of_geom_rep_file, _nodes, _edges, _subnetworks,
                                                  _node_start_id, _node_end_id, _is_directed, _calc_rules);

            // add to record 
            if (_add_to_record)
            {
                this.NetworkRecord.Add(created);
            }

            return created;
        }

        #endregion

        #region METHODS: local info extraction

        [Obsolete("Remove when network elements are stored in the IdProvider")]
        public Dictionary<SimObjectId, SimFlowNetworkElement> GetAllNetworkElements()
        {
            Dictionary<SimObjectId, SimFlowNetworkElement> allElements = new Dictionary<SimObjectId, SimFlowNetworkElement>();

            foreach (var net in this.NetworkRecord)
                GetAllNetworkElements(net, allElements);

            return allElements;
        }

        private void GetAllNetworkElements(SimFlowNetwork net, Dictionary<SimObjectId, SimFlowNetworkElement> allElements)
        {
            allElements.Add(net.ID, net);

            foreach (var node in net.ContainedNodes.Values)
                allElements.Add(node.ID, node);
            foreach (var edge in net.ContainedEdges.Values)
                allElements.Add(edge.ID, edge);

            foreach (var subNet in net.ContainedFlowNetworks.Values)
                GetAllNetworkElements(subNet, allElements);
        }



        #endregion

        #region METHODS: Post-Parsing

        /// <summary>
        /// Removes the connection between components and geometric object. To be used on
        /// importing from a library into a new project.
        /// </summary>
        public void RemoveReferencesToGeometryWithinRecord()
        {
            // within the networks
            foreach (SimFlowNetwork nw in this.NetworkRecord)
            {
                RemoveReferencesToGeometryWithinRecord(nw);
            }
        }
        private void RemoveReferencesToGeometryWithinRecord(SimFlowNetwork nw)
        {
            nw.IndexOfGeometricRepFile = -1;
            nw.RepresentationReference = GeometricReference.Empty;
            foreach (var n in nw.ContainedNodes)
            {
                n.Value.RepresentationReference = GeometricReference.Empty;
            }
            foreach (var e in nw.ContainedEdges)
            {
                e.Value.RepresentationReference = GeometricReference.Empty;
            }

            foreach (var subNw in nw.ContainedFlowNetworks.Values)
                RemoveReferencesToGeometryWithinRecord(subNw);
        }

        #endregion
    }
}
