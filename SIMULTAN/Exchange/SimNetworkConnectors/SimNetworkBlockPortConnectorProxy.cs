using SIMULTAN.Data.Geometry;
using SIMULTAN.Data.SimNetworks;
using System;
using System.Windows.Media;

namespace SIMULTAN.Exchange.SimNetworkConnectors
{

    /// <summary>
    /// Proxy geometry for the line between a Block and its´ contained Port
    /// Connector between a <see cref="SimNetworkBlock"/> and a <see cref="SimNetworkPort"/> as a <see cref="Polyline"/>
    /// </summary>
    internal class SimNetworkBlockPortConnectorProxy : BaseSimnetworkGeometryConnector
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

        /// <summary>
        /// The connector. If not null, the port is connected to an other. 
        /// </summary>
        internal SimNetworkConnector Connector { get; private set; }

        /// <inheritdoc />



        internal override BaseGeometry Geometry => ConnectorGeometry;


        /// <summary>
        /// Initializes a new instance of the SimNetworkBlockPortConnectorProxy class
        /// </summary>
        /// <param name="geometry">The polyline which represents the proxy connection between a port and it's parent block or SimNetwork</param>
        /// <param name="parentElement">The parent element (Block or a SimNetwork)</param>
        /// <param name="port">The SimNetworkPort </param>
        /// <param name="connector">The Connector. Whenever the port is connected to an other, 
        /// the position of the polyline representing this proxy geometry is calculated based on the connector (which represents the connection as a vertex) 
        /// <see cref="SimNetworkConnector"/> </param>
        internal SimNetworkBlockPortConnectorProxy(Polyline geometry, BaseSimNetworkElement parentElement, SimNetworkPort port, SimNetworkConnector connector = null)
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
            this.Connector = connector;

            UpdateColor();
        }


        internal void SetConnector(SimNetworkConnector connector)
        {
            this.Connector = connector;
            ChangeBaseGeometry(this.Geometry);
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
        internal override void OnTopologyChanged()
        {

        }
        /// <inheritdoc />
        public override void Dispose()
        {
            this.Port.PropertyChanged -= Edge_PropertyChanged;
            this.ParentElement.PropertyChanged -= Edge_PropertyChanged;
        }

        #endregion

        private void Edge_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SimNetworkConnector.Name))
                ConnectorGeometry.Name = ParentElement.Name + "to" + Port.Name;
        }


        private void UpdateColor()
        {
            bool fromParent = true;
            UpdateColor(ConnectorGeometry, this.Geometry.Color.Color, fromParent);

            for (int i = 0; i < ConnectorGeometry.Edges.Count; i++)
            {
                UpdateColor(ConnectorGeometry.Edges[i].Edge, this.Geometry.Color.Color, fromParent);

                if (i != 0)
                    UpdateColor(ConnectorGeometry.Edges[i].StartVertex, this.Geometry.Color.Color, fromParent);
            }
        }
        private void UpdateColor(BaseGeometry geo, Color color, bool fromParent)
        {
            geo.Color.Color = color;
            geo.Color.IsFromParent = fromParent;
        }
    }
}
