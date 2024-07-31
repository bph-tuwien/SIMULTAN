using SIMULTAN.Data.FlowNetworks;
using SIMULTAN.Data.SimMath;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace SIMULTAN.Data.Components
{
    public static class NetworkFactoryManagement
    {
        #region MERGING OF RECORDS

        /// <summary>
        /// Merges the given flow networks into the calling factory's record. The ids of the merged networks are changed to avoid duplicates!
        /// </summary>
        /// <param name="_factory">the calling component factory</param>
        /// <param name="_nws_to_add">the flow networks to merge into the record of the calling component factory</param>
        public static void AddToRecord(this SimNetworkFactory _factory, List<SimFlowNetwork> _nws_to_add)
        {
            if (_nws_to_add == null) return;
            if (_nws_to_add.Count < 1) return;

            // 1. change the ids of the new networks to avoid duplicates in the record
            long max_current_id = 0;
            if (_factory.NetworkRecord.Any())
                max_current_id = _factory.NetworkRecord.SelectMany(x => x.GetAllElementIds()).Max();

            foreach (SimFlowNetwork nw in _nws_to_add)
            {
                nw.UpdateAllElementIds(ref max_current_id);
            }
            SimFlowNetworkElement.NR_FL_NET_ELEMENTS = max_current_id + 1;

            // add to the record
            foreach (SimFlowNetwork nw in _nws_to_add)
            {
                _factory.NetworkRecord.Add(nw);
            }
        }

        #endregion

        #region RECORD CLEAN-UP

        /// <summary>
        /// Clears the component and flow network records of the calling component factory.
        /// </summary>
        /// <param name="_factory">the calling component factory</param>
        /// <returns>true</returns>
        public static bool ClearRecord(this SimNetworkFactory _factory)
        {
            _factory.NetworkRecord.Clear();
            _factory.SetCallingLocation(null);

            return true;
        }

        #endregion

        #region COMPONENT: instance clean-up

        /// <summary>
        /// Removes the instances in all components that were connected to
        /// the file with the given resource index.
        /// </summary>
        /// <param name="_fact">the calling factory</param>
        /// <param name="_index_of_resource">the resource index of the file</param>
        public static void DisconnectAllInstances(this SimNetworkFactory _fact, int _index_of_resource)
        {
            foreach (SimFlowNetwork nw in _fact.NetworkRecord)
            {
                if (nw.IndexOfGeometricRepFile == _index_of_resource)
                    nw.IndexOfGeometricRepFile = -1;
            }
        }

        #endregion

        #region NETWORK: add, convert, manage

        /// <summary>
        /// Adds a new sub-network to the given parent flow network.
        /// </summary>
        /// <param name="_factory">the calling component factory</param>
        /// <param name="_parent">the potantial parent network</param>
        /// <param name="_pos">the position of the new sub-network in 2d</param>
        /// <param name="_name">the name of the new sub-network</param>
        /// <param name="_description">the description of the new sub-network</param>
        /// <returns>the id of the newly created netowrk, or -1 if the operation was unsuccessful</returns>
        public static long AddNetworkToNetwork(this SimNetworkFactory _factory, SimFlowNetwork _parent, SimPoint _pos, string _name, string _description)
        {
            if (_parent == null) return -1;

            long id_created = _parent.AddFlowNetwork(_pos, _name, _description);
            return id_created;
        }

        /// <summary>
        /// Replaces the given node in the parent network with a sub-network and returns it.
        /// </summary>
        /// <param name="_factory">the calling component factory</param>
        /// <param name="_parent">the parent of the node to be converted</param>
        /// <param name="_node">the node to be converted</param>
        /// <returns>the newly created flow netowrk</returns>
        public static SimFlowNetwork ConvertNodeToNetwork(this SimNetworkFactory _factory, SimFlowNetwork _parent, SimFlowNetworkNode _node)
        {
            if (_parent == null) return null;

            SimFlowNetwork converted = _parent.NodeToNetwork(_node);

            return converted;
        }

        /// <summary>
        /// Replaces the given sub-network in the parent network by a node and returns it.
        /// </summary>
        /// <param name="_factory">the calling component factory</param>
        /// <param name="_parent">the parent of the sub-network to be converted</param>
        /// <param name="_nw">the sub-network to be converted</param>
        /// <returns>the newly created netowrk node</returns>
        public static SimFlowNetworkNode ConvertNetworkToNode(this SimNetworkFactory _factory, SimFlowNetwork _parent, SimFlowNetwork _nw)
        {
            if (_parent == null) return null;

            SimFlowNetworkNode converted = _parent.NetworkToNode(_nw);

            return converted;
        }

        #endregion

        #region NETWORK: remove

        /// <summary>
        /// Remove the given flow network from the record of the calling component factory. The network 
        /// cannot be removed if it is locked.
        /// </summary>
        /// <param name="_factory">the calling component factory</param>
        /// <param name="_netw">the flow network to be removed</param>
        /// <param name="_inform_content">true = de-couple the affected content, false = do not inform the affected components</param>
        /// <returns>true if the operation was successful;false otherwise</returns>
        public static bool RemoveNetwork(this SimNetworkFactory _factory, SimFlowNetwork _netw, bool _inform_content = true)
        {
            if (_netw == null) return false;

            return _factory.RemoveNetworkRegardlessOfLocking(_netw, _inform_content);
        }

        /// <summary>
        /// Remove the given flow network from the record of the calling component factory. The network is removed
        /// even if it is locked.
        /// </summary>
        /// <param name="_factory">the calling component factory</param>
        /// <param name="_netw">the flow network to be removed</param>
        /// <param name="_inform_content">true = de-couple the affected content, false = do not inform the affected components</param>
        /// <returns>true if the operation was successful;false otherwise</returns>
        internal static bool RemoveNetworkRegardlessOfLocking(this SimNetworkFactory _factory, SimFlowNetwork _netw, bool _inform_content = true)
        {
            // alert contained component(s)
            if (_inform_content)
            {
                if (_netw.Content != null)
                {
                    _netw.Content.Component.Instances.Remove(_netw.Content);
                }
                var netw_contents = _netw.GetAllElementsWithContent();
                foreach (var entry in netw_contents)
                {
                    entry.Content.Component.Instances.Remove(entry.Content);
                }
            }

            bool success_level0 = _factory.NetworkRecord.Remove(_netw);
            if (success_level0)
            {
                _factory.OnNetworkDeleted(new List<SimFlowNetwork> { _netw });
                return true;
            }
            else
            {
                foreach (SimFlowNetwork nw in _factory.NetworkRecord)
                {
                    bool success_levelN = nw.RemoveNodeOrNetwork(_netw);
                    if (success_levelN)
                    {
                        return true;
                    }
                }
                return false;
            }
        }


        #endregion

        #region NETWORK: copy

        /// <summary>
        /// Copy the given network either to the top level of the network record in the calling component factory
        /// or as a sub-network of the given parent.
        /// </summary>
        /// <param name="_factory">the calling component factory</param>
        /// <param name="_nw_to_copy">the flow network to copy</param>
        /// <returns>the id of the copy</returns>
        public static SimObjectId CopyNetwork(this SimNetworkFactory _factory, SimFlowNetwork _nw_to_copy)
        {
            if (_nw_to_copy == null) return SimObjectId.Empty;

            // add to the record
            SimFlowNetwork nw_copy = new SimFlowNetwork(_nw_to_copy);
            nw_copy.Name = _nw_to_copy.Name + " (copy)";
            _factory.OnNetworkAdded(new List<SimFlowNetwork> { nw_copy });
            _factory.NetworkRecord.Add(nw_copy);
            return nw_copy.ID;
        }

        #endregion
    }
}
