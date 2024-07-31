using SIMULTAN.Data.Assets;
using SIMULTAN.Data.Components;
using SIMULTAN.Data.FlowNetworks;
using SIMULTAN.Data.Geometry;
using SIMULTAN.Serializer.Geometry;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using SIMULTAN.Data.SimMath;

namespace SIMULTAN.Exchange.NetworkConnectors
{
    /// <summary>
    /// Connector between a <see cref="SimFlowNetworkNode"/> and a <see cref="Vertex"/>
    /// Also handles <see cref="ProxyGeometry"/> attached to the vertex
    /// </summary>
    internal class NetworkNodeConnector : BaseNetworkConnector
    {
        //used to prevent the size/rotation transfer between proxy and instance from endless looping
        private bool transformInProgress = false;

        private static readonly HashSet<string> proxyResourceExtensions = new HashSet<string> {
            ".obj", ".fbx"
        };

        /// <summary>
        /// The node
        /// </summary>
        internal SimFlowNetworkNode Node { get; }
        private SimComponentInstance nodeContent;

        /// <summary>
        /// The vertex
        /// </summary>
        internal Vertex Vertex { get; private set; }

        /// <inheritdoc />
        internal override BaseGeometry Geometry => Vertex;

        private NetworkGeometryModelConnector ModelConnector { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="NetworkNodeConnector"/> class
        /// </summary>
        /// <param name="vertex">The vertex</param>
        /// <param name="node">The network node</param>
        /// <param name="modelConnector">The network model connector</param>
        internal NetworkNodeConnector(Vertex vertex, SimFlowNetworkNode node, NetworkGeometryModelConnector modelConnector)
        {
            if (vertex == null)
                throw new ArgumentNullException(nameof(vertex));
            if (node == null)
                throw new ArgumentNullException(nameof(node));
            if (modelConnector == null)
                throw new ArgumentNullException(nameof(modelConnector));

            this.Vertex = vertex;
            this.Node = node;
            this.ModelConnector = modelConnector;

            this.Vertex.Name = this.Node.Name;

            this.Node.RepresentationReference = new Data.GeometricReference(vertex.ModelGeometry.Model.File.Key, vertex.Id);
            this.Node.PropertyChanged += this.Node_PropertyChanged;

            nodeContent = Node.Content;
            if (nodeContent != null)
            {
                ((INotifyCollectionChanged)nodeContent.Component.ReferencedAssets).CollectionChanged += this.Assets_CollectionChanged;
                nodeContent.PropertyChanged += NodeContent_PropertyChanged;
            }

            UpdateProxyGeometry();
            UpdateProxyTransformation();


            UpdateColor();
        }


        #region BaseNetworkConnector

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

            Node.PropertyChanged -= Node_PropertyChanged;
            if (nodeContent != null)
            {
                ((INotifyCollectionChanged)nodeContent.Component.ReferencedAssets).CollectionChanged -= this.Assets_CollectionChanged;
                nodeContent.PropertyChanged -= NodeContent_PropertyChanged;
            }
        }
        #endregion

        private void Node_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SimFlowNetworkNode.Content))
            {
                if (nodeContent != null)
                {
                    nodeContent.PropertyChanged -= NodeContent_PropertyChanged;
                    ((INotifyCollectionChanged)nodeContent.Component.ReferencedAssets).CollectionChanged -= this.Assets_CollectionChanged;
                }

                UpdateInstanceTransformation();

                nodeContent = Node.Content;
                if (nodeContent != null)
                {
                    nodeContent.PropertyChanged += NodeContent_PropertyChanged;
                    ((INotifyCollectionChanged)nodeContent.Component.ReferencedAssets).CollectionChanged += this.Assets_CollectionChanged;
                }

                UpdateColor();
            }
            else if (e.PropertyName == nameof(SimFlowNetworkNode.Name))
                Vertex.Name = Node.Name;
        }

        private void NodeContent_PropertyChanged(object sender, PropertyChangedEventArgs e)
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

        private void Assets_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            UpdateProxyGeometry();
        }


        private void UpdateProxyGeometry()
        {
            List<ImportWarningMessage> messages = new List<ImportWarningMessage>();

            //Check if proxy assets exist
            List<FileInfo> proxyAssets = new List<FileInfo>();
            if (Node.Content != null)
            {
                foreach (var asset in Node.Content.Component.ReferencedAssets)
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

        private void UpdateProxyTransformation()
        {
            var proxy = Vertex.ProxyGeometries.FirstOrDefault();

            //Update proxy size
            if (proxy != null)
            {
                this.transformInProgress = true;

                var size = SimInstanceSize.Default;
                var rotation = SimQuaternion.Identity;

                if (Node.Content != null)
                {
                    size = Node.Content.InstanceSize;
                    rotation = Node.Content.InstanceRotation;
                }

                proxy.Size = size.Max;
                proxy.Rotation = rotation;

                this.transformInProgress = false;
            }
        }

        private void UpdateInstanceTransformation()
        {
            if (!transformInProgress && Node.Content != null)
            {
                using (AccessCheckingDisabler.Disable(Node.Content.Factory))
                {
                    var proxy = Vertex.ProxyGeometries.FirstOrDefault();
                    if (proxy != null)
                    {
                        transformInProgress = true;

                        Node.Content.InstanceSize = new SimInstanceSize(Node.Content.InstanceSize.Min, proxy.Size);
                        Node.Content.InstanceRotation = proxy.Rotation;

                        transformInProgress = false;
                    }
                }
            }
        }

        private void UpdateColor()
        {
            if (Node.Content == null)
            {
                Vertex.Color.Color = NetworkColors.COL_EMPTY;
                Vertex.Color.IsFromParent = false;
            }
            else
            {
                Vertex.Color.Color = NetworkColors.COL_NEUTRAL;
                Vertex.Color.IsFromParent = true;
            }
        }
    }
}
