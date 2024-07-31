using SIMULTAN.Data.Assets;
using SIMULTAN.Data.Components;
using SIMULTAN.Data.Geometry;
using SIMULTAN.Data.SimMath;
using SIMULTAN.Data.SimNetworks;
using SIMULTAN.Serializer.Geometry;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;

namespace SIMULTAN.Exchange.SimNetworkConnectors
{
    /// <summary>
    /// Connector between a <see cref="SimNetworkBlock"/> and a <see cref="Vertex"/>
    /// Also handles <see cref="ProxyGeometry"/> attached to the vertex
    /// </summary>
    internal class SimNetworkBlockConnector : SimNetworkBaseNetworkElementConnector
    {
        private SimQuaternion rotation;

        /// <summary>
        /// used to prevent the size/rotation transfer between proxy and instance from endless looping
        /// </summary>
        internal bool transformInProgress { get; set; } = false;

        private static readonly HashSet<string> proxyResourceExtensions = new HashSet<string> {
            ".obj", ".fbx", ".stl"
        };
        /// <summary>
        /// The block
        /// </summary>
        internal SimNetworkBlock Block { get; }

        /// <inheritdoc />
        internal override BaseSimNetworkElement NetworkElement => Block;

        /// <inheritdoc />
        internal override IEnumerable<ISimNetworkElement> SimNetworkElement => new List<BaseSimNetworkElement> { Block };

        /// <summary>
        /// Component Instance of the assigned block (if applicable)
        /// </summary>
        private SimComponentInstance blockContent;

        /// <inheritdoc />
        internal override BaseGeometry Geometry => Vertex;

        /// <summary>
        /// The parent geometry connector
        /// </summary>
        private SimNetworkGeometryModelConnector ModelConnector { get; }

        /// <summary>
        /// Initializes a new SimNetworkBlockConnector 
        /// </summary>
        /// <param name="vertex">The representative geometry</param>
        /// <param name="block">The block it represents</param>
        /// <param name="connector">The parent connector</param>
        /// <param name="rotation">The rotation</param>
        public SimNetworkBlockConnector(Vertex vertex, SimNetworkBlock block, SimNetworkGeometryModelConnector connector, SimQuaternion rotation)
        {
            if (vertex == null)
                throw new ArgumentNullException(nameof(vertex));
            if (block == null)
                throw new ArgumentNullException(nameof(block));
            if (connector == null)
                throw new ArgumentNullException(nameof(connector));


            this.Vertex = vertex;
            this.Block = block;
            this.Block.PropertyChanged += this.Block_PropertyChanged;
            this.ModelConnector = connector;
            this.rotation = rotation;
            this.NetworkElement.RepresentationReference = new Data.GeometricReference(vertex.ModelGeometry.Model.File.Key, vertex.Id);
            blockContent = Block.ComponentInstance;
            this.PortConnectors.CollectionChanged += this.PortConnectors_CollectionChanged;


            if (blockContent != null)
            {
                ((INotifyCollectionChanged)blockContent.Component.ReferencedAssets).CollectionChanged += this.Assets_CollectionChanged;
                blockContent.PropertyChanged += this.BlockContent_PropertyChanged;
            }

            UpdateProxyGeometry();
            UpdateProxyTransformation(rotation);
            UpdateColor();
        }

        private void PortConnectors_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (var item in e.NewItems)
                    {
                        if (item is SimNetworkPortConnector portConnector)
                        {
                            portConnector.Geometry.GeometryChanged += this.Geometry_GeometryChanged;
                        }
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    foreach (var item in e.OldItems)
                    {
                        if (item is SimNetworkPortConnector portConnector)
                        {
                            portConnector.Geometry.GeometryChanged -= this.Geometry_GeometryChanged;
                        }
                    }
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        private void Geometry_GeometryChanged(object sender)
        {
            if (!transformInProgress)
            {
                this.transformInProgress = true;
                if (sender is BaseGeometry geometry)
                {
                    var portConnector = this.PortConnectors.FirstOrDefault(t => t.Geometry.Id == geometry.Id);
                    if (portConnector != null && portConnector.Port.ComponentInstance != null && portConnector.Port.ComponentInstance.Component != null)
                    {
                        //Check if the port component´s has the X,Y, Z parameters
                        var X = portConnector.Port.ComponentInstance.Component.Parameters.OfType<SimDoubleParameter>().FirstOrDefault(p => p.HasReservedTaxonomyEntry(ReservedParameterKeys.SIMNW_STATIC_PORT_POSITION_X));
                        var Y = portConnector.Port.ComponentInstance.Component.Parameters.OfType<SimDoubleParameter>().FirstOrDefault(p => p.HasReservedTaxonomyEntry(ReservedParameterKeys.SIMNW_STATIC_PORT_POSITION_Y));
                        var Z = portConnector.Port.ComponentInstance.Component.Parameters.OfType<SimDoubleParameter>().FirstOrDefault(p => p.HasReservedTaxonomyEntry(ReservedParameterKeys.SIMNW_STATIC_PORT_POSITION_Z));
                        if (X != null && Y != null && Z != null && !portConnector.transformInProgress)
                        {
                            this.Vertex.Position = new SimPoint3D(
                                  portConnector.Vertex.Position.X - X.Value,
                                  portConnector.Vertex.Position.Y - Y.Value,
                                  portConnector.Vertex.Position.Z - Z.Value);
                        }
                    }
                }
                this.transformInProgress = false;
            }
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

            Block.PropertyChanged -= Block_PropertyChanged;
            if (blockContent != null)
            {
                if (blockContent.Component != null)
                {
                    ((INotifyCollectionChanged)blockContent.Component.ReferencedAssets).CollectionChanged -= this.Assets_CollectionChanged;
                }
                blockContent.PropertyChanged -= BlockContent_PropertyChanged;
            }
        }
        #endregion


        private void Block_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SimNetworkBlock.ComponentInstance))
            {
                if (blockContent != null)
                {
                    blockContent.PropertyChanged -= BlockContent_PropertyChanged;
                    ((INotifyCollectionChanged)blockContent.Component.ReferencedAssets).CollectionChanged -= this.Assets_CollectionChanged;
                }

                UpdateInstanceTransformation();

                blockContent = Block.ComponentInstance;
                if (blockContent != null)
                {
                    blockContent.PropertyChanged += BlockContent_PropertyChanged;
                    ((INotifyCollectionChanged)blockContent.Component.ReferencedAssets).CollectionChanged += this.Assets_CollectionChanged;
                }
            }


            else if (e.PropertyName == nameof(SimNetworkBlock.Name))
                Vertex.Name = Block.Name;

            else if (e.PropertyName == nameof(SimNetworkBlock.Color))
                UpdateColor();
        }

        private void BlockContent_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SimComponentInstance.InstanceSize))
            {
                UpdateProxyTransformation(rotation);
            }
            else if (e.PropertyName == nameof(SimComponentInstance.InstanceRotation))
            {
                UpdateProxyTransformation(rotation);
            }
        }



        private void Assets_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            UpdateProxyGeometry();
        }


        private void UpdateProxyGeometry()
        {
            List<ImportWarningMessage> messages = new List<ImportWarningMessage>();

            //Check if proxy assets exist
            List<FileInfo> proxyAssets = new List<FileInfo>();
            if (Block.ComponentInstance != null)
            {
                foreach (var asset in Block.ComponentInstance.Component.ReferencedAssets)
                {
                    if (asset.Resource is ResourceFileEntry rfe && proxyResourceExtensions.Contains(Path.GetExtension(rfe.CurrentFullPath)))
                    {
                        if (rfe.Exists)
                            proxyAssets.Add(new FileInfo(rfe.CurrentFullPath));
                        else
                            messages.Add(new ImportWarningMessage(ImportWarningReason.FileNotFound, new object[] { rfe.Name }));
                    }
                }
            }

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



        private void UpdateProxyTransformation(SimQuaternion rotation)
        {
            var proxy = Vertex.ProxyGeometries.FirstOrDefault();

            //Update proxy size
            if (proxy != null)
            {
                this.transformInProgress = true;

                if (Block.ComponentInstance != null)
                {
                    if (rotation == SimQuaternion.Identity)
                    {
                        rotation = proxy.Rotation;
                    }
                }
                else
                {
                    rotation = proxy.Rotation;
                }
                proxy.Rotation = rotation;

                this.transformInProgress = false;
            }
        }

        private void UpdateInstanceTransformation()
        {
            if (!transformInProgress && Block.ComponentInstance != null)
            {
                using (AccessCheckingDisabler.Disable(Block.ComponentInstance.Factory))
                {
                    var proxy = Vertex.ProxyGeometries.FirstOrDefault();
                    if (proxy != null)
                    {
                        transformInProgress = true;

                        Block.ComponentInstance.InstanceSize = new SimInstanceSize(Block.ComponentInstance.InstanceSize.Min, proxy.Size);
                        Block.ComponentInstance.InstanceRotation = proxy.Rotation;

                        transformInProgress = false;
                    }
                }
            }
        }

        private void UpdateColor()
        {
            this.Vertex.Color = new DerivedColor(this.NetworkElement.Color);
            if (Block.ComponentInstance == null)
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
