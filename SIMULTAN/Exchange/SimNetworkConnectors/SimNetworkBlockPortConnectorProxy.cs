using SIMULTAN.Data.Geometry;
using SIMULTAN.Data.SimMath;
using SIMULTAN.Data.SimNetworks;
using System;
using System.Collections.Generic;

namespace SIMULTAN.Exchange.SimNetworkConnectors
{

    /// <summary>
    /// Proxy geometry for the line between a Block and its´ contained Port
    /// Connector between a <see cref="SimNetworkBlock"/> and a <see cref="SimNetworkPort"/> as a <see cref="Polyline"/>
    /// </summary>
    internal class SimNetworkBlockPortConnectorProxy : BaseSimNetworkGeometryConnector
    {
        /// <summary>
        /// The polyline
        /// </summary>
        internal Polyline ConnectorGeometry { get; private set; }

        /// <summary>
        /// The SimNetworkBlock
        /// </summary>
        internal BaseSimNetworkElement ParentElement { get; }

        /// <summary>
        /// The SimNetworkPort
        /// </summary>
        internal SimNetworkPort Port { get; }

        /// <inheritdoc />
        internal override IEnumerable<ISimNetworkElement> SimNetworkElement => new List<SimNetworkPort> { Port };

        /// <inheritdoc />
        internal override BaseGeometry Geometry => ConnectorGeometry;

        public SimNetworkConnection Connection
        {
            get => connection;
            set
            {
                if (connection != value)
                {
                    connection = value;
                    ChangeBaseGeometry(Geometry);
                }
            }
        }
        private SimNetworkConnection connection;

        public bool IsValid
        {
            get => isValid;
            set
            {
                isValid = value;
                UpdateColor();
            }
        }
        private bool isValid;


        /// <summary>
        /// Initializes a new instance of the SimNetworkBlockPortConnectorProxy class
        /// </summary>
        /// <param name="geometry">The polyline which represents the proxy connection between a port and it's parent block or SimNetwork</param>
        /// <param name="parentElement">The parent element (Block or a SimNetwork)</param>
        /// <param name="port">The SimNetworkPort </param>
        /// <param name="isValid">If the static constraints are valid</param>
        /// <param name="connection">Connection of the proxy if it is a proxy for a connection between two ports.
        /// the position of the polyline representing this proxy geometry is calculated based on the connector (which represents the connection as a vertex) 
        /// <see cref="SimNetworkConnection"/> </param>
        internal SimNetworkBlockPortConnectorProxy(Polyline geometry, BaseSimNetworkElement parentElement, SimNetworkPort port, bool isValid, SimNetworkConnection connection = null)
        {
            if (geometry == null)
                throw new ArgumentNullException(nameof(geometry));
            if (parentElement == null)
                throw new ArgumentNullException(nameof(parentElement));
            if (port == null)
                throw new ArgumentNullException(nameof(port));

            this.ConnectorGeometry = geometry;
            this.ParentElement = parentElement;
            this.Port = port;
            this.ParentElement.PropertyChanged += this.Edge_PropertyChanged;
            this.Port.PropertyChanged += this.Edge_PropertyChanged;
            this.isValid = isValid;
            this.connection = connection;

            UpdateColor();
        }

        #region BaseNetworkConnector


        /// <inheritdoc />
        internal override void OnGeometryChanged()
        {
        }

        /// <inheritdoc />
        internal override void ChangeBaseGeometry(BaseGeometry geometry)
        {
            ConnectorGeometry = geometry as Polyline;
        }

        /// <inheritdoc />
        internal override void OnTopologyChanged() { }
        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.Port.PropertyChanged -= Edge_PropertyChanged;
                this.ParentElement.PropertyChanged -= Edge_PropertyChanged;
            }
            base.Dispose(disposing);
        }

        #endregion

        private void Edge_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SimNetworkConnection.Name))
                ConnectorGeometry.Name = ParentElement.Name + "to" + Port.Name;
        }

        private void UpdateColor()
        {
            var color = isValid ? Geometry.Color.Color : SimColors.Red;
            bool fromParent = isValid;
            UpdateColor(ConnectorGeometry, color, fromParent);

            for (int i = 0; i < ConnectorGeometry.Edges.Count; i++)
            {
                UpdateColor(ConnectorGeometry.Edges[i].Edge, color, fromParent);

                if (i != 0)
                    UpdateColor(ConnectorGeometry.Edges[i].StartVertex, color, fromParent);
            }
        }
        private void UpdateColor(BaseGeometry geo, SimColor color, bool fromParent)
        {
            geo.Color.Color = color;
            geo.Color.IsFromParent = fromParent;
        }
    }
}
