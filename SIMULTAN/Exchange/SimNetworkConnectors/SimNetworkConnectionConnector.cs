using SIMULTAN.Data.Geometry;
using SIMULTAN.Data.SimMath;
using SIMULTAN.Data.SimNetworks;
using SIMULTAN.Serializer.Geometry;
using SIMULTAN.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SIMULTAN.Exchange.SimNetworkConnectors
{
    /// <summary>
    /// Represents a <see cref="Data.SimNetworks.SimNetworkConnection"/> as a <see cref="Vertex"/>
    /// </summary>
    internal class SimNetworkConnectionConnector : BaseSimNetworkGeometryConnector
    {

        internal bool transformInProgress = false;

        /// <summary>
        /// The vertex representing the connector between two ports
        /// </summary>
        internal Vertex Vertex { get; private set; }

        internal override BaseGeometry Geometry => Vertex;
        internal SimNetworkGeometryModelConnector ModelConnector { get; }

        internal IEnumerable<SimNetworkConnection> SimNetworkConnections { get; }

        /// <inheritdoc />
        internal override IEnumerable<ISimNetworkElement> SimNetworkElement => SimNetworkConnections;

        /// <summary>
        /// Constructs a new SimNetworkConnectorConnector in the case of multi-level connection (connector connected to subnetwork to subnetwork.... to block) 
        /// In that case the connection is represented by one Vertex
        /// </summary>
        /// <param name="geometry">The representing geometry</param>
        /// <param name="connections">The two connections representing a connection through subnetwork layers</param>
        /// <param name="modelConnector">The main SimNetworkGeometryModelConnector</param>
        public SimNetworkConnectionConnector(Vertex geometry, List<SimNetworkConnection> connections, SimNetworkGeometryModelConnector modelConnector)
        {
            if (geometry == null)
                throw new ArgumentNullException(nameof(geometry));
            if (connections == null)
                throw new ArgumentNullException(nameof(connections));
            if (modelConnector == null)
                throw new ArgumentNullException(nameof(modelConnector));


            this.Vertex = geometry;
            this.SimNetworkConnections = connections;

            foreach (var connection in connections)
            {
                connection.RepresentationReference = new Data.GeometricReference(Vertex.ModelGeometry.Model.File.Key, Vertex.Id);
                connection.Target.RepresentationReference = new Data.GeometricReference(Vertex.ModelGeometry.Model.File.Key, Vertex.Id);
                connection.Source.RepresentationReference = new Data.GeometricReference(Vertex.ModelGeometry.Model.File.Key, Vertex.Id);
            }


            this.SimNetworkConnections.ForEach(t => t.PropertyChanged += this.Connection_PropertyChanged);
            this.ModelConnector = modelConnector;
            UpdateProxyGeometry();
            UpdateColor();
        }

        private void Connection_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SimNetworkConnection.Color))
                UpdateColor();
        }


        /// <inheritdoc />

        private void UpdateProxyGeometry()
        {
            List<ImportWarningMessage> messages = new List<ImportWarningMessage>();

            //Check if proxy already exists
            var proxy = Vertex.ProxyGeometries.FirstOrDefault();

            //Update proxy geometry
            if (proxy == null)
            {
                proxy = ProxyShapeGenerator.GenerateDoublePyramid(Vertex.ModelGeometry.Layers.First(),
                    Vertex.Name, Vertex, new SimPoint3D(1, 1, 1));
                Vertex.ProxyGeometries.Add(proxy);
            }
            else
            {
                ProxyShapeGenerator.UpdateDoublePyramid(proxy, new SimPoint3D(1, 1, 1));
            }
        }


        private void UpdateColor()
        {
            Vertex.Color = new DerivedColor(this.SimNetworkConnections.First().Color);
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
            this.Vertex = geometry as Vertex;

        }
        /// <inheritdoc />
        internal override void OnTopologyChanged()
        {
        }
        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.SimNetworkConnections.ForEach(t => t.PropertyChanged -= this.Connection_PropertyChanged);
            }
            base.Dispose(disposing);
        }

        #endregion
    }
}
