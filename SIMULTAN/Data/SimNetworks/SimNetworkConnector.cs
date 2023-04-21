using SIMULTAN.Data.Geometry;
using SIMULTAN.Exchange.SimNetworkConnectors;
using System;
using System.Windows.Media;

namespace SIMULTAN.Data.SimNetworks
{
    /// <summary>
    /// Class for representing a connector between two ports in a SimNetwork
    /// </summary>
    public partial class SimNetworkConnector : SimNamedObject<ISimManagedCollection>
    {

        /// <summary>
        /// Color of the Connector
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

        private SimId loadingSourceId, loadingTargetId;

        #region PROPERTIES: for geometric representation

        private GeometricReference geom_representation_ref;
        /// <summary>
        /// Saves the reference to the *representing* geometry. It either can be a
        ///  <see cref="SimNetworkConnectorConnector"/> or a <see cref="SimNetworkInvalidConnectorConnector"/>
        ///  Whenever the connection between the two ports in the geometry is valid, the connection is represented by a Vertex, in other cases it is represented by a Polyline
        /// </summary>
        public GeometricReference RepresentationReference
        {
            get { return this.geom_representation_ref; }
            internal set
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
        /// Representing the parent network.
        /// </summary>
        public SimNetwork ParentNetwork { get; internal set; }
        /// <summary>
        /// Source Port
        /// </summary>
        public SimNetworkPort Source { get; internal set; }
        /// <summary>
        /// The target Port
        /// </summary>
        public SimNetworkPort Target { get; internal set; }


        #region .CTOR
        /// <summary>
        /// Initializes a new SimNetworkConnector
        /// </summary>
        /// <param name="port1">The source port</param>
        /// <param name="port2">The target port</param>
        public SimNetworkConnector(SimNetworkPort port1, SimNetworkPort port2)
        {
            if (port1 == null)
                throw new ArgumentNullException(nameof(port1));
            if (port2 == null)
                throw new ArgumentNullException(nameof(port2));
            if (port1.PortType == port2.PortType)
            {
                if (port1.ParentNetworkElement is SimNetwork nw1)
                {
                    if (port1.PortType == PortType.Input)
                    {
                        this.Source = port1;
                        this.Target = port2;
                    }
                    if (port1.PortType == PortType.Output)
                    {
                        this.Source = port2;
                        this.Target = port1;
                    }
                }
                if (port2.ParentNetworkElement is SimNetwork nw2)
                {
                    if (port2.PortType == PortType.Input)
                    {
                        this.Source = port2;
                        this.Target = port1;
                    }
                    if (port2.PortType == PortType.Output)
                    {
                        this.Source = port1;
                        this.Target = port2;
                    }
                }
            }
            else
            {
                this.Source = port1.PortType == PortType.Output ? port1 : port2;
                this.Target = port2.PortType == PortType.Input ? port2 : port1;
            }

            if (this.Source == null)
                throw new ArgumentNullException("No source is provided");
            if (this.Target == null)
                throw new ArgumentNullException("No target is provided");


            this.Color = new DerivedColor(Colors.DarkGray);
            this.Name = this.Name = string.Format("{0}to{1}", this.Source.LocalID.ToString(), this.Target.LocalID.ToString());
            this.geom_representation_ref = GeometricReference.Empty;

        }


        /// <summary>
        /// For Parsing
        /// </summary>
        /// <param name="id">The loaded id of the SimNetworkConnector</param>
        internal SimNetworkConnector(SimId id)
        {
            this.Id = id;
        }

        internal SimNetworkConnector(string name, SimId id, SimId loadingSourceId, SimId loadingTargetId, DerivedColor color)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));
            this.Name = name;
            this.Id = id;
            this.loadingSourceId = loadingSourceId;
            this.loadingTargetId = loadingTargetId;
            this.color = color;
        }

        #endregion


        internal void RestoreReferences()
        {
            if (this.Factory != null)
            {
                if (this.loadingSourceId != SimId.Empty)
                    this.Source = Factory.ProjectData.IdGenerator.GetById<SimNetworkPort>(loadingSourceId);
                if (this.loadingTargetId != SimId.Empty)
                    this.Target = Factory.ProjectData.IdGenerator.GetById<SimNetworkPort>(loadingTargetId);

                this.loadingSourceId = SimId.Empty;
                this.loadingTargetId = SimId.Empty;
            }
        }
    }
}
