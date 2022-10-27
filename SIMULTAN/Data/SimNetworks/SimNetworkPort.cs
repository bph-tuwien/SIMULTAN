using SIMULTAN.Data.Components;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SIMULTAN.Data.SimNetworks
{
    /// <summary>
    /// Type of the port
    /// </summary>
    public enum PortType
    {
        /// <summary>
        /// Whether it is an input
        /// </summary>
        Input = 0,
        /// <summary>
        /// Whether it is an output
        /// </summary>
        Output = 1,
    }

    /// <summary>
    /// Represents a port in a SimNetworkBlock. SimNetworkPorts can be connected with SimNetworkConnector <see cref="SimNetworkConnector"/>
    /// </summary>
    public partial class SimNetworkPort : SimNamedObject<ISimManagedCollection>, IElementWithComponent, IDisposable
    {
        #region Properties
        /// <summary>
        /// The type of the port, it can be an input or an output port
        /// </summary>
        public PortType PortType { get; set; }
        /// <summary>
        /// The containing Block 
        /// </summary>
        public BaseSimNetworkElement ParentNetworkElement { get; internal set; }

        /// <summary>
        /// List of connectors where the port is present
        /// </summary>
        public List<SimNetworkConnector> Connectors
        {
            get
            {
                return this.ParentNetworkElement.ParentNetwork.ContainedConnectors.Where(c => c.Source == this || c.Target == this).ToList();
            }
        }


        /// <summary>
        /// True whenever a connection exists with this port
        /// </summary>
        public bool IsConnected
        {
            get
            {
                if (this.isDisposed)
                {
                    return false;
                }

                return (this.ParentNetworkElement.ParentNetwork.ContainedConnectors.Where(c => c.Source == this || c.Target == this).ToList().Count > 0);

            }
        }
        /// <summary>
        /// Representing an attached component to the network block element
        /// </summary>
        public SimComponentInstance ComponentInstance
        {
            get { return this.componentInstance; }
            set
            {
                this.componentInstance = value;
                this.NotifyPropertyChanged(nameof(this.ComponentInstance));
            }
        }
        private SimComponentInstance componentInstance;
        #endregion


        #region .CTOR

        /// <summary>
        /// Constructs a new SimNetworkElementPort
        /// </summary>
        /// <param name="portType">the type of the port (input or output)</param>
        public SimNetworkPort(PortType portType)
        {
            this.PortType = portType;
            this.Id = SimId.Empty;
        }

        /// <summary>
        /// For Parsing
        /// </summary>
        /// <param name="name">The name of the port</param>
        /// <param name="id">The loaded id of the SimNetworkPort</param>
        /// <param name="portType">The type of the SimNetworkPort <see cref="PortType"/></param>
        internal SimNetworkPort(string name, SimId id, PortType portType)
        {
            this.Name = name;
            this.PortType = portType;
            this.Id = id;
        }

        #endregion


        /// <summary>
        /// Connects the port to an other one
        /// </summary>
        /// <param name="port">the port to connect</param>
        public void ConnectTo(SimNetworkPort port)
        {

            //Case 1.: Whenever the source and the target is in the same network:
            if (port.PortType != this.PortType && this.ParentNetworkElement != port.ParentNetworkElement
                && (port.ParentNetworkElement != this.ParentNetworkElement.ParentNetwork && this.ParentNetworkElement != port.ParentNetworkElement.ParentNetwork)
                && !this.ParentNetworkElement.ParentNetwork.ContainedConnectors.Any(c => c.Source == this || c.Target == this || c.Source == port || c.Target == port))
            {
                if (this.PortType == PortType.Output)
                {
                    var neConnector = new SimNetworkConnector(this, port);
                    this.ParentNetworkElement.ParentNetwork.ContainedConnectors.Add(neConnector);
                }
                else
                {
                    var neConnector = new SimNetworkConnector(port, this);
                    this.ParentNetworkElement.ParentNetwork.ContainedConnectors.Add(neConnector);
                }

            }

            //Case 2.: Whenever the connection between the ports is a cross-network connection (e.g.: a subnetwork port is connected to a network element inside the subnetwork)
            //in this case both the soruce and the target has the same PortType (input & input or output & output) --> whenever it is an input connection, the source will be the subnetwor´s port,
            //and in the case of a output connection it is the other way around
            else if (this.ParentNetworkElement is SimNetwork nw && port.ParentNetworkElement.ParentNetwork == this.ParentNetworkElement && this.PortType == port.PortType
                && !nw.ContainedConnectors.Any(c => c.Source == this || c.Target == this || c.Source == port || c.Target == port))
            {
                if (this.PortType == PortType.Input)
                {
                    var neConnector = new SimNetworkConnector(this, port);
                    nw.ContainedConnectors.Add(neConnector);
                }
                else
                {
                    var neConnector = new SimNetworkConnector(port, this);
                    nw.ContainedConnectors.Add(neConnector);
                }

            }
            else if (port.ParentNetworkElement is SimNetwork sNw && this.ParentNetworkElement.ParentNetwork == port.ParentNetworkElement && this.PortType == port.PortType
                && !sNw.ContainedConnectors.Any(c => c.Source == this || c.Target == this || c.Source == port || c.Target == port))
            {
                if (port.PortType == PortType.Input)
                {
                    var neConnector = new SimNetworkConnector(port, this);
                    sNw.ContainedConnectors.Add(neConnector);
                }
                else
                {
                    var neConnector = new SimNetworkConnector(this, port);
                    sNw.ContainedConnectors.Add(neConnector);
                }
            }



        }


        private bool isDisposed;
        /// <inheritdoc />
        public void Dispose()
        {
            if (!this.isDisposed)
            {
                this.isDisposed = true;
            }
        }


        /// <summary>
        /// Assignes the port to a new parent
        /// </summary>
        /// <param name="newParent">The BaseSimNetworkElement which will have this port assgined to</param>
        /// <returns></returns>
        [Obsolete]
        public void AssignToParent(BaseSimNetworkElement newParent)
        {
            this.ParentNetworkElement = newParent;
        }

        /// <summary>
        /// Removes all connections to the given port
        /// </summary>
        public bool RemoveConnections()
        {
            if (this.ParentNetworkElement is SimNetwork subNw)
            {
                var nestedConnectors = subNw.ContainedConnectors.Where(t => t.Source == this || t.Target == this);

                foreach (var c in nestedConnectors.ToList())
                {
                    subNw.ContainedConnectors.Remove(c);
                }
            }
            var connector = this.ParentNetworkElement.ParentNetwork.ContainedConnectors.Where(t => t.Source == this || t.Target == this).FirstOrDefault();
            this.ParentNetworkElement.ParentNetwork.ContainedConnectors.Remove(connector);
            return false;
        }


        internal void NotifyIsConnectedChanged()
        {
            this.NotifyPropertyChanged(nameof(IsConnected));
        }
    }
}