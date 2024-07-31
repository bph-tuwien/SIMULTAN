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
    /// Represents a <see cref="Data.SimNetworks.SimNetworkConnector"/> as a <see cref="Vertex"/>
    /// </summary>
    internal class SimNetworkConnectorConnector : BaseSimnetworkGeometryConnector
    {

        internal bool transformInProgress = false;

        /// <summary>
        /// The vertex representing the connector between two ports
        /// </summary>
        internal Vertex Vertex { get; private set; }

        internal override BaseGeometry Geometry => Vertex;
        internal SimNetworkGeometryModelConnector ModelConnector { get; }

        internal IEnumerable<SimNetworkConnector> SimNetworkConnectors { get; }

        /// <inheritdoc />
        internal override IEnumerable<ISimNetworkElement> SimNetworkElement => SimNetworkConnectors;

        /// <summary>
        /// Constructs a new SimNetworkConnectorConnector in the case of multi-level connection (connector connected to subnetwork to subnetwork.... to block) 
        /// In that case the connection is represented by one Vertex
        /// </summary>
        /// <param name="geometry">The representing geometry</param>
        /// <param name="connectors">The two connector representing a connection through subnetwork layers</param>
        /// <param name="modelConnector">The main SimNetworkGeometryModelConnector</param>
        public SimNetworkConnectorConnector(Vertex geometry, List<SimNetworkConnector> connectors, SimNetworkGeometryModelConnector modelConnector)
        {
            if (geometry == null)
                throw new ArgumentNullException(nameof(geometry));
            if (connectors == null)
                throw new ArgumentNullException(nameof(connectors));
            if (modelConnector == null)
                throw new ArgumentNullException(nameof(modelConnector));


            this.Vertex = geometry;
            this.SimNetworkConnectors = connectors;

            foreach (var connector in connectors)
            {
                connector.RepresentationReference = new Data.GeometricReference(Vertex.ModelGeometry.Model.File.Key, Vertex.Id);
                connector.Target.RepresentationReference = new Data.GeometricReference(Vertex.ModelGeometry.Model.File.Key, Vertex.Id);
                connector.Source.RepresentationReference = new Data.GeometricReference(Vertex.ModelGeometry.Model.File.Key, Vertex.Id);
            }


            this.SimNetworkConnectors.ForEach(t => t.PropertyChanged += this.Connector_PropertyChanged);
            this.ModelConnector = modelConnector;
            UpdateProxyGeometry();
            UpdateColor();
        }

        private void Connector_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SimNetworkConnector.Color))
                UpdateColor();
        }


        /// <inheritdoc />

        private void UpdateProxyGeometry()
        {
            List<ImportWarningMessage> messages = new List<ImportWarningMessage>();

            //Check if proxy assets exist
            List<FileInfo> proxyAssets = new List<FileInfo>();

            //Check if proxy already exists
            var proxy = Vertex.ProxyGeometries.FirstOrDefault();

            //Update proxy geometry
            if (proxyAssets.Count > 0)
            {
                try
                {
                    if (proxy == null)
                    {
                        proxy = ProxyShapeGenerator.LoadModelsCombined(Vertex.ModelGeometry.Layers.First(),
                            Vertex.Name, Vertex, proxyAssets, ModelConnector.Exchange.ProjectData);
                    }
                    else
                    {
                        ProxyShapeGenerator.UpdateProxyGeometryCombined(proxy, proxyAssets, ModelConnector.Exchange.ProjectData);
                    }
                }
                catch (GeometryImporterException e)
                {
                    var message = new ImportWarningMessage(ImportWarningReason.ImportFailed, new object[] { e.Message, e.InnerException.Message });
                    if (!messages.Contains(message))
                    {
                        messages.Add(message);
                    }
                }
                catch (FileNotFoundException e)
                {
                    var message = new ImportWarningMessage(ImportWarningReason.FileNotFound, new object[] { e.Message });
                    if (!messages.Contains(message))
                    {
                        messages.Add(message);
                    }
                }
            }
            else
            {
                //Cube
                if (proxy == null)
                {
                    proxy = ProxyShapeGenerator.GenerateCube(Vertex.ModelGeometry.Layers.First(),
                        Vertex.Name, Vertex, new SimPoint3D(1, 1, 1));
                }
                else
                {
                    ProxyShapeGenerator.UpdateCube(proxy, new SimPoint3D(1, 1, 1));
                }
            }

            if (messages.Count > 0)
                ModelConnector.Exchange.ProjectData.GeometryModels.OnImporterWarning(messages);
        }


        private void UpdateColor()
        {
            Vertex.Color = new DerivedColor(this.SimNetworkConnectors.First().Color);
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
        public override void Dispose()
        {
            this.SimNetworkConnectors.ForEach(t => t.PropertyChanged -= this.Connector_PropertyChanged);
        }

        #endregion
    }
}
