using SIMULTAN.Data.Components;
using SIMULTAN.Data.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;

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
        /// Color of the Port
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
        /// The type of the port, it can be an input or an output port
        /// </summary>
        public PortType PortType { get; private set; }
        /// <summary>
        /// The containing Block 
        /// </summary>
        public BaseSimNetworkElement ParentNetworkElement
        {
            get;
            internal set;
        }

        #region PROPERTIES: for geometric representation

        private GeometricReference geom_representation_ref;
        /// <summary>
        /// Saves the reference to the *representing* geometry.
        /// </summary>
        public GeometricReference RepresentationReference
        {
            get
            {
                return this.geom_representation_ref;
            }
            set
            {
                if (this.geom_representation_ref != value)
                {
                    this.geom_representation_ref = value;
                    this.NotifyPropertyChanged(nameof(RepresentationReference));
                }
            }
        }
        #endregion
        /// <summary>
        /// List of connectors where the port is present
        /// </summary>
        public List<SimNetworkConnector> Connectors
        {
            get
            {
                var connectors = new List<SimNetworkConnector>();
                if (this.ParentNetworkElement is SimNetwork nw)
                {
                    connectors.AddRange(nw.ContainedConnectors.Where(c => c.Source == this || c.Target == this));
                }
                connectors.AddRange(this.ParentNetworkElement.ParentNetwork.ContainedConnectors.Where(c => c.Source == this || c.Target == this));
                return connectors;
            }
        }
        /// <summary>
        /// List of ports to which the port is connected via <see cref="SimNetworkConnector"/>
        /// </summary>
        public List<SimNetworkPort> ConnectedPorts
        {
            get
            {
                var connectedPorts = new List<SimNetworkPort>();
                foreach (var con in this.Connectors)
                {
                    if (con.Source == this)
                    {
                        connectedPorts.Add(con.Target);
                    }
                    else
                    {
                        connectedPorts.Add(con.Source);
                    }
                }
                return connectedPorts;
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
        /// Constructs a new SimNetworkPort
        /// </summary>
        /// <param name="portType">The type of the port (input or output)</param>
        /// <param name="name">Name of the port</param>
        public SimNetworkPort(PortType portType, string name = "")
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));


            this.Name = name;
            this.Color = new DerivedColor(Colors.DarkGray);
            this.PortType = portType;
            this.Id = SimId.Empty;
            this.RepresentationReference = GeometricReference.Empty;
        }

        /// <summary>
        /// Constructor for cloning
        /// </summary>
        /// <param name="basePort">The port we base our clone on</param>
        public SimNetworkPort(SimNetworkPort basePort)
        {
            this.Name = basePort.Name;
            this.Color = basePort.Color;
            this.PortType = basePort.PortType;
            this.Id = SimId.Empty;
            this.RepresentationReference = GeometricReference.Empty;
        }

        /// <summary>
        /// For Parsing
        /// </summary>
        /// <param name="name">The name of the port</param>
        /// <param name="id">The loaded id of the SimNetworkPort</param>
        /// <param name="color">Color of the port</param>
        /// <param name="portType">The type of the SimNetworkPort <see cref="PortType"/></param>
        internal SimNetworkPort(string name, SimId id, PortType portType, DerivedColor color)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));
            if (id == null)
                throw new ArgumentNullException(nameof(id));
            if (color == null)
                throw new ArgumentNullException(nameof(color));

            this.Color = color;
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
                    neConnector.Color = this.Color;
                    port.Color = this.Color;
                    this.ParentNetworkElement.ParentNetwork.ContainedConnectors.Add(neConnector);
                }
                else
                {
                    var neConnector = new SimNetworkConnector(port, this);
                    neConnector.Color = this.Color;
                    port.Color = this.Color;
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
                    neConnector.Color = this.Color;
                    port.Color = this.Color;
                    nw.ContainedConnectors.Add(neConnector);
                }
                else
                {
                    var neConnector = new SimNetworkConnector(port, this);
                    neConnector.Color = this.Color;
                    port.Color = this.Color;
                    nw.ContainedConnectors.Add(neConnector);
                }

            }
            else if (port.ParentNetworkElement is SimNetwork sNw && this.ParentNetworkElement.ParentNetwork == port.ParentNetworkElement && this.PortType == port.PortType
                && !sNw.ContainedConnectors.Any(c => c.Source == this || c.Target == this || c.Source == port || c.Target == port))
            {
                if (port.PortType == PortType.Input)
                {
                    var neConnector = new SimNetworkConnector(port, this);
                    neConnector.Color = this.Color;
                    port.Color = this.Color;
                    sNw.ContainedConnectors.Add(neConnector);
                }
                else
                {
                    var neConnector = new SimNetworkConnector(this, port);
                    neConnector.Color = this.Color;
                    port.Color = this.Color;
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