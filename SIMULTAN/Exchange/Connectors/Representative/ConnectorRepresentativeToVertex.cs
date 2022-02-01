using SIMULTAN.Data.Assets;
using SIMULTAN.Data.Components;
using SIMULTAN.Data.FlowNetworks;
using SIMULTAN.Data.Geometry;
using SIMULTAN.Exchange.ConnectorInteraction;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace SIMULTAN.Exchange.Connectors
{
    /// <summary>
    /// Manages the connection of a network NODE container to its geometric representation.
    /// </summary>
    internal class ConnectorRepresentativeToVertex : ConnectorRepresentativeToBase
    {
        // Keep track of old component to be able to unhook event handlers
        private SimComponent oldContent = null;

        #region .CTOR

        /// <summary>
        /// Initializes an instance of the class ConnectorRepresentativeToVertex.
        /// </summary>
        /// <param name="_comm_manager">the manager, initializing this instance</param>
        /// <param name="_network_connector">the connector of the network containing the represented container</param>
        /// <param name="_source_node">the network node whose content is being represented</param>
        /// <param name="_index_of_geometry_model">the index of the <see cref="GeometryModelData"/> where the vertex resides</param>
        /// <param name="_target_vertex">the target vertex that represents the source</param>
        public ConnectorRepresentativeToVertex(ComponentGeometryExchange _comm_manager,
                                             ConnectorToGeometryModel _network_connector, SimFlowNetworkNode _source_node,
                                             int _index_of_geometry_model, Vertex _target_vertex)
            : base(_comm_manager, _network_connector, _source_node, _index_of_geometry_model, _target_vertex)
        {
            this.SynchronizeSourceWTarget(_target_vertex);
            oldContent = SourceContainer.Content?.Component;
            if (SourceContainer.Content != null)
            {
                ((INotifyCollectionChanged)SourceContainer.Content.Component.ReferencedAssets).CollectionChanged += SourceContainerReferencedAssets_CollectionChanged;
                ConnectResourcesEvents(SourceContainer.Content.Component);
            }
        }

        private void ConnectResourcesEvents(SimComponent content)
        {
            foreach (var asset in content.ReferencedAssets)
            {
                asset.Resource.ResourceChanged += Resource_ResourceChanged;
            }
        }

        private void DisconnectResourcesEvents(SimComponent content)
        {
            foreach (var asset in content.ReferencedAssets)
            {
                asset.Resource.ResourceChanged -= Resource_ResourceChanged;
            }
        }

        /// <inheritdoc/>
        protected override void ReactToChangeInContent()
        {
            if (oldContent != null)
            {
                ((INotifyCollectionChanged)oldContent.ReferencedAssets).CollectionChanged -= SourceContainerReferencedAssets_CollectionChanged;
                DisconnectResourcesEvents(oldContent);
            }
            oldContent = SourceContainer.Content?.Component;

            if (SourceContainer.Content != null)
            {
                ((INotifyCollectionChanged)SourceContainer.Content.Component.ReferencedAssets).CollectionChanged += SourceContainerReferencedAssets_CollectionChanged;
                ConnectResourcesEvents(SourceContainer.Content.Component);
            }

            base.ReactToChangeInContent();
        }

        private void Resource_ResourceChanged(object sender)
        {
            ParentConnector.ConnectorAssetsChanged(this);
        }

        private void SourceContainerReferencedAssets_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (var item in e.NewItems)
                    {
                        Asset asset = item as Asset;
                        asset.Resource.ResourceChanged += Resource_ResourceChanged;
                    }
                    ParentConnector.ConnectorAssetsChanged(this);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    foreach (var item in e.OldItems)
                    {
                        Asset asset = item as Asset;
                        asset.Resource.ResourceChanged -= Resource_ResourceChanged;
                    }
                    ParentConnector.ConnectorAssetsChanged(this);
                    break;
                case NotifyCollectionChangedAction.Reset:
                    foreach (var item in e.OldItems)
                    {
                        Asset asset = item as Asset;
                        asset.Resource.ResourceChanged -= Resource_ResourceChanged;
                    }
                    ParentConnector.ConnectorAssetsChanged(this);
                    break;
                case NotifyCollectionChangedAction.Replace:
                    foreach (var item in e.OldItems)
                    {
                        Asset asset = item as Asset;
                        asset.Resource.ResourceChanged -= Resource_ResourceChanged;
                    }
                    foreach (var item in e.NewItems)
                    {
                        Asset asset = item as Asset;
                        asset.Resource.ResourceChanged += Resource_ResourceChanged;
                    }
                    ParentConnector.ConnectorAssetsChanged(this);
                    break;
                default:
                    break;
            }
        }

        #endregion

        #region METHOD OVERRIDES

        /// <inheritdoc/>
        protected override bool SynchTargetIsAdmissible(BaseGeometry _target)
        {
            return (_target is Vertex && this.TargetId == _target.Id);
        }

        /// <inheritdoc/>
        protected override void PassRepresentationInfoToInstance(BaseGeometry _representation)
        {
            if (_representation is Vertex v_rep)
            {
                base.PassRepresentationInfoToInstance(_representation);
                if (this.ChildConnector != null)
                    this.ChildConnector.AdoptPosition(v_rep.Position);
                else if (this.instance != null)
                {
                    using (AccessCheckingDisabler.Disable(instance.Component.Factory))
                    {
                        ConnectorAlgorithms.TransferPositionToInstance(v_rep.Position, this.instance);
                        ConnectorAlgorithms.TransferTransformationToInstance(v_rep, this.instance);
                    }
                }
            }
        }

        internal override void BeforeDeletion()
        {
            base.BeforeDeletion();
            if (SourceContainer.Content != null)
            {
                ((INotifyCollectionChanged)SourceContainer.Content.Component.ReferencedAssets).CollectionChanged -= SourceContainerReferencedAssets_CollectionChanged;
                DisconnectResourcesEvents(SourceContainer.Content.Component);
            }
        }

        #endregion
    }
}
