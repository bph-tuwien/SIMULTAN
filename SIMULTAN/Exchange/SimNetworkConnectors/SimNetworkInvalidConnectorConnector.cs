using SIMULTAN.Data.Geometry;
using SIMULTAN.Data.SimNetworks;
using System;
using System.Collections.Generic;

namespace SIMULTAN.Exchange.SimNetworkConnectors
{

    /// <summary>
    ///Represents a <see cref="Data.SimNetworks.SimNetworkConnector"/> as a <see cref="Polyline"/>
    /// </summary>
    internal class SimNetworkInvalidConnectorConnector : BaseSimnetworkGeometryConnector
    {

        /// <summary>
        /// The polyline representing the connector between two ports
        /// </summary>
        internal Polyline Polyline { get; private set; }


        internal SimNetworkGeometryModelConnector ModelConnector { get; }

        internal List<SimNetworkConnector> SimNetworkConnectors { get; }

        /// <inheritdoc />
        internal override IEnumerable<ISimNetworkElement> SimNetworkElement => SimNetworkConnectors;

        /// <inheritdoc />

        internal override BaseGeometry Geometry => Polyline;


        /// <summary>
        /// Initializes a new instance of the SimNetworkConnectorConnector class
        /// </summary>
        /// <param name="geometry">The polyline representing the connection between two ports</param>
        /// <param name="connectors">SimNetworkConnectors representing the connection </param>
        /// <param name="modelConnector">The main connector class for a SimNetwork which handles all the connections between network and geometry</param>
        internal SimNetworkInvalidConnectorConnector(Polyline geometry, List<SimNetworkConnector> connectors, SimNetworkGeometryModelConnector modelConnector)
        {
            if (geometry == null)
                throw new ArgumentNullException(nameof(geometry));
            if (connectors == null)
                throw new ArgumentNullException(nameof(connectors));
            if (modelConnector == null)
                throw new ArgumentNullException(nameof(modelConnector));

            this.Polyline = geometry;
            this.SimNetworkConnectors = connectors;
            //this.SimNetworkConnector.PropertyChanged += this.Connector_PropertyChanged;


            this.ModelConnector = modelConnector;

            foreach (var connector in connectors)
            {
                connector.RepresentationReference = new Data.GeometricReference(Polyline.ModelGeometry.Model.File.Key, Polyline.Id);
            }
            this.ModelConnector = modelConnector;
            UpdateColor();
        }



        #region BaseNetworkConnector

        /// <inheritdoc />
        internal override void OnGeometryChanged()
        {
            //Do nothing
        }
        /// <inheritdoc />
        internal override void ChangeBaseGeometry(BaseGeometry geometry)
        {
            this.Polyline = geometry as Polyline;

        }
        /// <inheritdoc />
        internal override void OnTopologyChanged()
        {
        }
        /// <inheritdoc />
        public override void Dispose()
        {
            // this.SimNetworkConnector.PropertyChanged -= Connector_PropertyChanged;
        }

        #endregion




        private void UpdateColor()
        {
            Polyline.Color.IsFromParent = true;
        }

    }
}
