using SIMULTAN.Data.SimMath;
using SIMULTAN.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using static SIMULTAN.Data.SimNetworks.SimNetworkConnection;
using static SIMULTAN.Data.SimNetworks.SimNetworkPort;

namespace SIMULTAN.Data.SimNetworks
{
    /// <summary>
    /// Represents a SimNetwork
    /// </summary>
    public partial class SimNetwork : BaseSimNetworkElement, INetwork
    {
        /// <summary>
        /// The index of geometric representation file 
        /// </summary>
        private int index_of_geometric_rep_file;
        /// <summary>
        /// The index of geometric representation file
        /// </summary>
        public int IndexOfGeometricRepFile
        {
            get { return this.index_of_geometric_rep_file; }
            set
            {
                if (this.index_of_geometric_rep_file != value)
                {
                    this.index_of_geometric_rep_file = value;
                    this.NotifyPropertyChanged(nameof(IndexOfGeometricRepFile));
                }
            }
        }

        /// <summary>
        /// Size of the network
        /// </summary>
        public double Size { get; set; } = 1;

        /// <summary>
        /// Contained Elements in the network
        /// </summary>
        public SimNetworkElementCollection ContainedElements { get; }


        /// <summary>
        /// Contained <see cref="SimNetworkConnection"/> in the network
        /// </summary>
        public SimNetworkConnectionCollection ContainedConnections { get; }

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

        /// <summary>
        /// Event handler delegate for the <see cref="AssociationChanged"/> event.
        /// </summary>
        /// <param name="sender">The sender</param>
        /// <param name="elements">The network elements of which the association changed</param>
        public delegate void AssociationChangedEventHandler(object sender, IEnumerable<BaseSimNetworkElement> elements);
        /// <summary>
        /// Invoked when the association relationship in one or more network elements changed
        /// </summary>
        public event AssociationChangedEventHandler AssociationChanged;

        internal void OnAssociationChanged(IEnumerable<BaseSimNetworkElement> elements)
        {
            AssociationChanged?.Invoke(this, elements);
        }


        #region .CTOR
        /// <summary>
        /// Constructs a new SimNetwork
        /// </summary>
        /// <param name="name">name of the SimNetwork</param>
        public SimNetwork(string name)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            this.RepresentationReference = GeometricReference.Empty;
            this.Name = name;
            this.Id = SimId.Empty;
            this.ContainedElements = new SimNetworkElementCollection(this);
            this.ContainedConnections = new SimNetworkConnectionCollection(this);
            this.Ports = new SimNetworkPortCollection(this);
            this.index_of_geometric_rep_file = -1;
            this.Color = SimColors.DarkGray;
        }

        /// <summary>
        /// COnstructor for cloning a SimNetwork
        /// </summary>
        /// <param name="simNetwork">The simnetwork we base our clone one</param>
        /// <param name="name">The designated name of the cloned SimNetwork</param>
        private SimNetwork(SimNetwork simNetwork, string name)
        {
            this.Name = name;
            this.Id = SimId.Empty;
            this.ContainedElements = new SimNetworkElementCollection(this);
            this.ContainedConnections = new SimNetworkConnectionCollection(this);
            this.Position = simNetwork.Position;
            this.Ports = new SimNetworkPortCollection(this);
            this.index_of_geometric_rep_file = -1;
            this.Color = simNetwork.Color;

        }


        /// <summary>
        /// For Parsing
        /// </summary>
        /// <param name="id">The loaded id of the SimNetwork</param>
        /// <param name="name">The name of the SimNetwork</param>
        /// <param name="position">The position of the SimNetwork (it only matters whenever it is a Subnetwork)</param>
        /// <param name="ports">The ports of the network</param>
        /// <param name="elements">The elements in the network, both <see cref="SimNetwork"/> and <see cref="SimNetworkBlock"/></param>
        /// <param name="connections">The connections inside the network. May either connect ports of sub elements or sub elements with ports of the 
        /// root network</param>
        /// <param name="color">Color of the network</param>
        internal SimNetwork(SimId id, string name, SimPoint position, IEnumerable<SimNetworkPort> ports,
            IEnumerable<BaseSimNetworkElement> elements, IEnumerable<SimNetworkConnection> connections, SimColor color)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));
            if (elements == null)
                throw new ArgumentNullException(nameof(elements));
            if (ports == null)
                throw new ArgumentNullException(nameof(ports));
            if (connections == null)
                throw new ArgumentNullException(nameof(connections));

            this.Id = id;
            this.Name = name;
            this.Position = position;
            this.Color = color;

            this.ContainedElements = new SimNetworkElementCollection(this);
            foreach (var element in elements)
                this.ContainedElements.Add(element);

            this.ContainedConnections = new SimNetworkConnectionCollection(this);
            foreach (var connection in connections)
                this.ContainedConnections.Add(connection);

            this.Ports = new SimNetworkPortCollection(this);
            foreach (var port in ports)
                this.Ports.Add(port);

        }

        /// <summary>
        /// Constructs a new SimNetwork
        /// </summary>
        /// <param name="name">name of the SimNetwork</param>
        /// <param name="position">Position of the SimNetwork</param>
        public SimNetwork(string name, SimPoint position)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            this.Name = name;
            this.Position = position;
            this.Id = SimId.Empty;
            this.ContainedElements = new SimNetworkElementCollection(this);
            this.ContainedConnections = new SimNetworkConnectionCollection(this);
            this.Ports = new SimNetworkPortCollection(this);
            this.index_of_geometric_rep_file = -1;
            this.Color = SimColors.DarkGray;
        }



        /// <summary>
        /// Converts a block into a subnetwork
        /// </summary>
        public SimNetwork ConvertBlockToSubnetwork(SimNetworkBlock block)
        {
            if (block == null)
                throw new ArgumentNullException(nameof(block));

            if (block.ComponentInstance != null)
            {
                block.RemoveComponentInstance();
            }
            var subNetwork = new SimNetwork(block.Name)
            {
                Position = block.Position,
                ParentNetwork = block.ParentNetwork,
            };
            var ports = new List<SimNetworkPort>();
            var connections = new List<(SimNetworkPort Source, SimNetworkPort Target, IEnumerable<SimPoint> controlPoints)>();

            for (int i = block.Ports.Count - 1; i > (-1); i--)
            {
                var port = block.Ports[i];
                foreach (var con in port.Connections)
                {
                    connections.Add((con.Source, con.Target, con.Points));
                }

                block.Ports.Remove(port);
                ports.Add(port);
            }
            this.ContainedElements.Add(subNetwork);
            this.ContainedElements.Remove(block);
            ports.Reverse();
            foreach (var oPort in ports)
            {
                var newPort = new SimNetworkPort(oPort.PortType);
                newPort.Name = oPort.Name;
                subNetwork.Ports.Add(newPort);
                foreach (var connection in connections)
                {
                    if (connection.Source == oPort)
                    {
                        var newCon = new SimNetworkConnection(newPort, connection.Target);
                        newCon.Points.AddRange(connection.controlPoints);
                        this.ContainedConnections.Add(newCon);
                    }
                    else if (connection.Target == oPort)
                    {
                        var newCon = new SimNetworkConnection(connection.Source, newPort);
                        newCon.Points.AddRange(connection.controlPoints);
                        this.ContainedConnections.Add(newCon);
                    }
                }
            }
            return subNetwork;
        }


        /// <inheritdoc />
        protected override void OnFactoryChanged(ISimManagedCollection newFactory, ISimManagedCollection oldFactory)
        {
            //Update calculation Ids
            this.ContainedElements.NotifyFactoryChanged(this.Factory, oldFactory);
            this.ContainedConnections.NotifyFactoryChanged(this.Factory, oldFactory);
            // this.Ports.NotifyFactoryChanged(this.Factory, oldFactory); --> Handled in base class

            base.OnFactoryChanged(newFactory, oldFactory);
        }

        /// <summary>
        /// Returns all the SimNetworkPorts
        /// </summary>
        /// <returns></returns>
        public List<SimNetworkPort> GetAllPorts()
        {
            return this.GetPortsRecursively(this);
        }
        /// <summary>
        /// Returns all the SimNetworkBlocks contained in the network recursively
        /// </summary>
        /// <returns></returns>
        public List<SimNetworkBlock> GetAllBlocks()
        {
            return this.GetBlocksRecursively(this);
        }



        private List<SimNetworkBlock> GetBlocksRecursively(SimNetwork nw)
        {
            List<SimNetworkBlock> result = new List<SimNetworkBlock>();
            foreach (var item in nw.ContainedElements)
            {
                if (item is SimNetwork subNetwork)
                {
                    result.AddRange(GetBlocksRecursively(subNetwork));
                }
                if (item is SimNetworkBlock block)
                {
                    result.Add(block);
                }
            }
            return result;
        }


        private List<SimNetworkPort> GetPortsRecursively(SimNetwork nw)
        {
            List<SimNetworkPort> result = new List<SimNetworkPort>();


            foreach (var port in nw.Ports)
            {
                result.Add(port);
            }
            foreach (var item in nw.ContainedElements)
            {
                if (item is SimNetwork subNetwork)
                {
                    result.AddRange(GetPortsRecursively(subNetwork));
                }
                if (item is SimNetworkBlock block)
                {
                    foreach (var port in block.Ports)
                    {
                        result.Add(port);
                    }
                }
            }

            return result;
        }


        /// <summary>
        /// Clones this SimNetwork
        /// </summary>
        /// <param name="name">The designated name</param>
        /// <param name="factory">The root level SimNetworkCollection, if it is null, then the network must be a subnetwork</param>
        /// <param name="parentNetwork">The parent network if the item is a subnetwork</param>
        /// <param name="clonedPortsLookup">Port pairs from upper levels of the network, to clone the SimNetworkConnections</param>
        /// <returns>Returns the cloned SimNetworkBLock, and a Dictionary with the original and cloned port LocalId pairs</returns>
        public SimNetwork Clone(string name, SimNetworkCollection factory, SimNetwork parentNetwork,
            Dictionary<SimNetworkPort, SimNetworkPort> clonedPortsLookup)
        {
            var clonedNetwork = new SimNetwork(this, name);
            // For storing the id pairs for ports (original, cloned). This is necessary to reconstruct the SimNetworkConnections
            // Key is old Id and Value is the new Id
            if (clonedPortsLookup == null)
                clonedPortsLookup = new();

            if (factory != null)
                factory.Add(clonedNetwork);
            if (parentNetwork != null)
                parentNetwork.ContainedElements.Add(clonedNetwork);

            foreach (var port in this.Ports)
            {
                var newPort = new SimNetworkPort(port);
                clonedNetwork.Ports.Add(newPort);
                clonedPortsLookup.Add(port, newPort);
            }

            foreach (var item in this.ContainedElements)
            {
                if (item is SimNetworkBlock block)
                {
                    block.Clone(clonedNetwork, clonedPortsLookup);
                }
                if (item is SimNetwork subNetwork)
                {
                    subNetwork.Clone(subNetwork.Name, null, clonedNetwork, clonedPortsLookup);
                }
            }

            foreach (var connection in this.ContainedConnections)
            {
                var clonedSource = clonedPortsLookup[connection.Source];
                var clonedTarget = clonedPortsLookup[connection.Target];

                var clonedConnection = new SimNetworkConnection(clonedSource, clonedTarget);
                clonedConnection.Color = connection.Color;
                clonedNetwork.ContainedConnections.Add(clonedConnection);
            }

            return clonedNetwork;
        }

        #endregion

        internal override void RestoreReferences()
        {
            foreach (var connection in ContainedConnections)
                connection.RestoreReferences();

            foreach (var element in ContainedElements)
                element.RestoreReferences();
        }
    }
}
