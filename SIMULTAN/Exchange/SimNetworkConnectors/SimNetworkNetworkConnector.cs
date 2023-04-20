using SIMULTAN.Data.Components;
using SIMULTAN.Data.Geometry;
using SIMULTAN.Data.SimNetworks;
using SIMULTAN.Serializer.Geometry;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Media.Media3D;

namespace SIMULTAN.Exchange.SimNetworkConnectors
{
    /// <summary>
    /// Represents a <see cref="SimNetworkConnector"/> as a <see cref="Vertex"/>
    /// </summary>
    internal class SimNetworkNetworkConnector : SimNetworkBaseNetworkElementConnector
    {


        private static readonly HashSet<string> proxyResourceExtensions = new HashSet<string> {
            ".obj", ".fbx"
        };
        /// <summary>
        /// The block
        /// </summary>
        internal SimNetwork Network { get; }

        internal override BaseSimNetworkElement NetworkElement => Network;

        /// <inheritdoc />
        internal override BaseGeometry Geometry => Vertex;
        private SimNetworkGeometryModelConnector ModelConnector { get; }



        /// <summary>
        ///  Creates a new SimNetworkNetworkConnector
        /// </summary>
        /// <param name="vertex">The vertex which represents the SimNetwork in the 3D geometry</param>
        /// <param name="network">The network</param>
        /// <param name="connector">The main connector</param>
        /// <exception cref="ArgumentNullException"></exception>
        public SimNetworkNetworkConnector(Vertex vertex, SimNetwork network, SimNetworkGeometryModelConnector connector)
        {
            if (vertex == null)
                throw new ArgumentNullException(nameof(vertex));
            if (network == null)
                throw new ArgumentNullException(nameof(network));
            if (connector == null)
                throw new ArgumentNullException(nameof(connector));

            this.Vertex = vertex;
            this.Network = network;
            this.Network.PropertyChanged += this.Network_PropertyChanged;
            this.ModelConnector = connector;
            this.Network.RepresentationReference = new Data.GeometricReference(vertex.ModelGeometry.Model.File.Key, vertex.Id);


            UpdateProxyGeometry();
            UpdateProxyTransformation();



            UpdateColor();
        }


        #region BaseSimnetworkGeometryConnector
        /// <inheritdoc />
        internal override void OnGeometryChanged()
        {

        }
        /// <inheritdoc />
        internal override void OnTopologyChanged()
        {
            //Nothing to do
        }
        /// <inheritdoc />
        internal override void ChangeBaseGeometry(BaseGeometry geometry)
        {
            this.Vertex = geometry as Vertex;
        }

        /// <inheritdoc />
        public override void Dispose()
        {
            base.Dispose();
        }
        #endregion


        private void Network_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SimNetworkBlock.Name))
                Vertex.Name = Network.Name;
        }



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
                        Vertex.Name, Vertex, new Point3D(1, 1, 1));
                }
                else
                {
                    ProxyShapeGenerator.UpdateCube(proxy, new Point3D(1, 1, 1));
                }
            }

            if (messages.Count > 0)
                ModelConnector.Exchange.ProjectData.GeometryModels.OnImporterWarning(messages);
        }

        private void UpdateProxyTransformation()
        {
            var proxy = Vertex.ProxyGeometries.FirstOrDefault();

            //Update proxy size
            if (proxy != null)
            {
                var size = SimInstanceSize.Default;
                var rotation = Quaternion.Identity;

                proxy.Size = size.Max;
                proxy.Rotation = rotation;

            }
        }



        private void UpdateColor()
        {
            Vertex.Color.IsFromParent = true;
        }
    }
}
