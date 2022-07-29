using System;
using System.Collections.Generic;
using System.Windows;
using static SIMULTAN.Data.SimNetworks.SimNetworkConnector;
using static SIMULTAN.Data.SimNetworks.SimNetworkPort;

namespace SIMULTAN.Data.SimNetworks
{
    /// <summary>
    /// Represents a SimNetwork
    /// </summary>
    public partial class SimNetwork : BaseSimNetworkElement
    {
        /// <summary>
        /// Contained Elements in the network
        /// </summary>
        public SimNetworkElementCollection ContainedElements { get; set; }


        /// <summary>
        /// Contained SimNetworkConnectors in the network
        /// </summary>
        public SimNetworkConnectorCollection ContainedConnectors { get; set; }



        #region .CTOR
        /// <summary>
        /// Constructs a new SimNetwork
        /// </summary>
        /// <param name="name">name of the SimNetwork</param>
        public SimNetwork(string name)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            this.Name = name;
            this.Id = SimId.Empty;
            this.ContainedElements = new SimNetworkElementCollection(this);
            this.ContainedConnectors = new SimNetworkConnectorCollection(this);
            this.Ports = new SimNetworkPortCollection(this);
        }


        /// <summary>
        /// For Parsing
        /// </summary>
        /// <param name="id">The loaded id of the SimNetwork</param>
        /// <param name="name">The name of the SimNetwork</param>
        /// <param name="position">The position of the SimNetwork (it only matters whenever it is a Subnetwork)</param>
        /// <param name="ports">The ports of the network</param>
        /// <param name="elements">The elements in the network, both <see cref="SimNetwork"/> and <see cref="SimNetworkBlock"/></param>
        /// <param name="connectors">The connectors inside the network. May either connect ports of subelements or subelements with ports of the 
        /// root network</param>
        internal SimNetwork(SimId id, string name, Point position, IEnumerable<SimNetworkPort> ports,
            IEnumerable<BaseSimNetworkElement> elements, IEnumerable<SimNetworkConnector> connectors)
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

            this.Id = id;
            this.Name = name;
            this.Position = position;

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

        /// <summary>
        /// Returns all the SimNetworkPorts
        /// </summary>
        /// <returns></returns>
        public List<SimNetworkPort> GetAllPorts()
        {
            return this.GetPortsRecursively(this);
        }
        /// <summary>
        /// Returns all the SimNetworkBlocks concinad in the network reuirsively
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


        #endregion

        /// <inheritdoc />
        protected override void OnFactoryChanged(ISimManagedCollection newFactory, ISimManagedCollection oldFactory)
        {
            this.ContainedElements.NotifyFactoryChanged(newFactory, oldFactory);
            this.ContainedConnectors.NotifyFactoryChanged(newFactory, oldFactory);

            base.OnFactoryChanged(newFactory, oldFactory);
        }

        internal override void RestoreReferences()
        {
            foreach (var connector in ContainedConnectors)
                connector.RestoreReferences();

            foreach (var element in ContainedElements)
                element.RestoreReferences();
        }
    }
}
