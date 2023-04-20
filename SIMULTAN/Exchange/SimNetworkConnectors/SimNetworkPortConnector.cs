using SIMULTAN.Data.Components;
using SIMULTAN.Data.Geometry;
using SIMULTAN.Data.SimNetworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media.Media3D;

namespace SIMULTAN.Exchange.SimNetworkConnectors
{


    /// <summary>
    /// Represents a <see cref="SimNetworkPort"/> as a <see cref="Vertex"/> in the geometry
    /// </summary>
    internal class SimNetworkPortConnector : BaseSimnetworkGeometryConnector
    {

        //used to prevent the size/rotation transfer between proxy and instance from endless looping
        internal bool transformInProgress = false;

        /// <summary>
        /// The Port
        /// </summary>
        internal SimNetworkPort Port { get; }
        private SimComponentInstance portContent;

        /// <summary>
        /// The vertex
        /// </summary>
        internal Vertex Vertex { get; private set; }
        /// <inheritdoc />
        internal override BaseGeometry Geometry => Vertex;
        private SimNetworkGeometryModelConnector ModelConnector { get; }



        /// <summary>
        /// Constructs a new SimNetworkPortConnector
        /// </summary>
        /// <param name="vertex">The geometric representation</param>
        /// <param name="port">The port it represents</param>
        /// <param name="connector">The geometry connector for the network which contains this port</param>
        /// <exception cref="ArgumentNullException"></exception>
        public SimNetworkPortConnector(Vertex vertex, SimNetworkPort port, SimNetworkGeometryModelConnector connector)
        {
            if (vertex == null)
                throw new ArgumentNullException(nameof(vertex));
            if (port == null)
                throw new ArgumentNullException(nameof(port));
            if (connector == null)
                throw new ArgumentNullException(nameof(connector));

            this.Vertex = vertex;
            this.Port = port;
            this.ModelConnector = connector;

            this.Port.RepresentationReference = new Data.GeometricReference(vertex.ModelGeometry.Model.File.Key, vertex.Id);
            this.Port.PropertyChanged += this.Port_PropertyChanged;


            portContent = Port.ComponentInstance;
            if (portContent != null)
            {
                portContent.PropertyChanged += this.PortContent_PropertyChanged;
            }
            UpdateColor();
        }


        private void PortContent_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {

            if (e.PropertyName == nameof(SimComponentInstance.InstanceSize))
            {
                UpdateProxyTransformation();
            }
            else if (e.PropertyName == nameof(SimComponentInstance.InstanceRotation))
            {
                UpdateProxyTransformation();
            }
        }

        private void Port_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SimNetworkBlock.ComponentInstance))
            {
                if (portContent != null)
                {
                    portContent.PropertyChanged -= PortContent_PropertyChanged;
                }

                UpdateInstanceTransformation();

                portContent = Port.ComponentInstance;
                if (portContent != null)
                {
                    portContent.PropertyChanged += PortContent_PropertyChanged;
                }

                UpdateColor();
            }
            else if (e.PropertyName == nameof(SimNetworkBlock.Name))
                Vertex.Name = Port.Name;
        }


        #region BaseSimnetworkGeometryConnector
        /// <inheritdoc />
        internal override void OnGeometryChanged()
        {
            UpdateInstanceTransformation();
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

            Port.PropertyChanged -= Port_PropertyChanged;
            if (portContent != null)
            {
                portContent.PropertyChanged -= PortContent_PropertyChanged;
            }
        }
        #endregion



        private void UpdateProxyTransformation()
        {
            var proxy = Vertex.ProxyGeometries.FirstOrDefault();

            //Update proxy size
            if (proxy != null)
            {
                this.transformInProgress = true;

                var size = SimInstanceSize.Default;
                var rotation = Quaternion.Identity;

                if (Port.ComponentInstance != null)
                {
                    size = Port.ComponentInstance.InstanceSize;
                    rotation = Port.ComponentInstance.InstanceRotation;
                }

                proxy.Size = size.Max;
                proxy.Rotation = rotation;

                this.transformInProgress = false;
            }
        }

        private void UpdateInstanceTransformation()
        {
            if (!transformInProgress && Port.ComponentInstance != null)
            {
                using (AccessCheckingDisabler.Disable(Port.ComponentInstance.Factory))
                {
                    var proxy = Vertex.ProxyGeometries.FirstOrDefault();
                    if (proxy != null)
                    {
                        transformInProgress = true;

                        Port.ComponentInstance.InstanceSize = new SimInstanceSize(Port.ComponentInstance.InstanceSize.Min, proxy.Size);
                        Port.ComponentInstance.InstanceRotation = proxy.Rotation;

                        transformInProgress = false;
                    }
                }
            }
        }

        private void UpdateColor()
        {
            if (Port.ComponentInstance == null)
            {
                Vertex.Color.IsFromParent = false;
            }
            else
            {
                Vertex.Color.IsFromParent = true;
            }
        }
    }
}
