using SIMULTAN.Data.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using static SIMULTAN.Data.SimNetworks.SimNetworkConnector;
using static SIMULTAN.Data.SimNetworks.SimNetworkPort;

namespace SIMULTAN.Data.SimNetworks
{
    /// <summary>
    /// Represents a SimNetwork
    /// </summary>
    public partial class SimNetwork : BaseSimNetworkElement, INetwork
    {

        /// <summary>
        /// Color of the Block
        /// </summary>
        public DerivedColor Color
        {
            get { return this.color; }
            set
            {
                this.color = value;
                this.NotifyPropertyChanged(nameof(this.Color));
            }
        }
        private DerivedColor color;

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
                    var old_value = this.index_of_geometric_rep_file;
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
        /// Contained SimNetworkConnectors in the network
        /// </summary>
        public SimNetworkConnectorCollection ContainedConnectors { get; }

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
            this.ContainedConnectors = new SimNetworkConnectorCollection(this);
            this.Ports = new SimNetworkPortCollection(this);
            this.index_of_geometric_rep_file = -1;
            this.Color = new DerivedColor(Colors.DarkGray);
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
            this.ContainedConnectors = new SimNetworkConnectorCollection(this);
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
        /// <param name="connectors">The connectors inside the network. May either connect ports of sub elements or sub elements with ports of the 
        /// root network</param>
        /// <param name="color">Color of the network</param>
        internal SimNetwork(SimId id, string name, Point position, IEnumerable<SimNetworkPort> ports,
            IEnumerable<BaseSimNetworkElement> elements, IEnumerable<SimNetworkConnector> connectors, DerivedColor color)
        {
            if (id == null)
                throw new ArgumentNullException(nameof(id));
            if (name == null)
                throw new ArgumentNullException(nameof(name));
            if (position == null)
                throw new ArgumentNullException(nameof(position));
            if (elements == null)
                throw new ArgumentNullException(nameof(elements));
            if (ports == null)
                throw new ArgumentNullException(nameof(ports));
            if (connectors == null)
                throw new ArgumentNullException(nameof(connectors));
            if (color == null)
                throw new ArgumentNullException(nameof(color));

            this.Id = id;
            this.Name = name;
            this.Position = position;
            this.color = color;

            this.ContainedElements = new SimNetworkElementCollection(this);
            foreach (var element in elements)
                this.ContainedElements.Add(element);

            this.ContainedConnectors = new SimNetworkConnectorCollection(this);
            foreach (var connector in connectors)
                this.ContainedConnectors.Add(connector);

            this.Ports = new SimNetworkPortCollection(this);
            foreach (var port in ports)
                this.Ports.Add(port);

        }

        /// <summary>
        /// Constructs a new SimNetwork
        /// </summary>
        /// <param name="name">name of the SimNetwork</param>
        /// <param name="position">Position of the SimNetwork</param>
        public SimNetwork(string name, Point position)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));
            if (position == null)
                throw new ArgumentNullException(nameof(position));

            this.Name = name;
            this.Position = position;
            this.Id = SimId.Empty;
            this.ContainedElements = new SimNetworkElementCollection(this);
            this.ContainedConnectors = new SimNetworkConnectorCollection(this);
            this.Ports = new SimNetworkPortCollection(this);
            this.index_of_geometric_rep_file = -1;
            this.Color = new DerivedColor(Colors.DarkGray);
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
            var connectors = new List<(SimNetworkPort Source, SimNetworkPort Target)>();

            for (int i = block.Ports.Count - 1; i > (-1); i--)
            {
                var port = block.Ports[i];
                foreach (var con in port.Connectors)
                {
                    connectors.Add((con.Source, con.Target));
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
                foreach (var connector in connectors)
                {
                    if (connector.Source == oPort)
                    {
                        this.ContainedConnectors.Add(new SimNetworkConnector(newPort, connector.Target));
                    }
                    else if (connector.Target == oPort)
                    {
                        this.ContainedConnectors.Add(new SimNetworkConnector(connector.Source, newPort));
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
            this.ContainedConnectors.NotifyFactoryChanged(this.Factory, oldFactory);
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
        /// <param name="portPairs">Port pairs from upper levels of the network, to clone the SimNetworkConnectors</param>
        /// <returns>Returns the cloned SimNetworkBLock, and a Dictionary with the original and cloned port LocalId pairs</returns>
        public (SimNetwork Cloned, Dictionary<SimId, SimId> PortPairs) Clone(string name, SimNetworkCollection factory, SimNetwork parentNetwork, Dictionary<SimId, SimId> portPairs)
        {

            var cloned = new SimNetwork(this, name);
            //For storing the id pairs for ports (original, cloned). This is necessary to reconstruct the SimNetworkConstructors
            var portIdDictionary = new Dictionary<SimId, SimId>();

            if (factory != null)
                factory.Add(cloned);
            if (parentNetwork != null)
                parentNetwork.ContainedElements.Add(cloned);
            if (portPairs != null)
                portPairs.ToList().ForEach(x => portIdDictionary.Add(x.Key, x.Value));


            foreach (var port in this.Ports)
            {
                var newPort = new SimNetworkPort(port);
                cloned.Ports.Add(newPort);
                portIdDictionary.Add(port.Id, newPort.Id);
            }



            foreach (var item in this.ContainedElements)
            {
                if (item is SimNetworkBlock block)
                {
                    var clonedBlock = block.Clone(cloned);
                    clonedBlock.PortPairs.ToList().ForEach(x => portIdDictionary.Add(x.Key, x.Value));
                }
                if (item is SimNetwork subNetwork)
                {
                    var subNw = subNetwork.Clone(subNetwork.Name, null, cloned, portIdDictionary);
                    subNw.PortPairs.ToList().ForEach(x => portIdDictionary.Add(x.Key, x.Value));
                }
            }




            foreach (var connector in this.ContainedConnectors)
            {
                if (portIdDictionary.TryGetValue(connector.Source.Id, out var clonedSourceId) && portIdDictionary.TryGetValue(connector.Target.Id, out var clonedTargetid))
                {
                    var clonedSource = Factory.ProjectData.IdGenerator.GetById<SimNetworkPort>(clonedTargetid);
                    var clonedTarget = Factory.ProjectData.IdGenerator.GetById<SimNetworkPort>(clonedSourceId);

                    var clonedConnector = new SimNetworkConnector(clonedSource, clonedTarget);
                    clonedConnector.Color = connector.Color;
                    cloned.ContainedConnectors.Add(clonedConnector);
                }
            }

            return (cloned, portIdDictionary);
        }

        #endregion

        internal override void RestoreReferences()
        {
            foreach (var connector in ContainedConnectors)
                connector.RestoreReferences();

            foreach (var element in ContainedElements)
                element.RestoreReferences();
        }
    }
}
