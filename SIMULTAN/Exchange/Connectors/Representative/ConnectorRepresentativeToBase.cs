using SIMULTAN.Data;
using SIMULTAN.Data.Components;
using SIMULTAN.Data.FlowNetworks;
using SIMULTAN.Data.Geometry;
using SIMULTAN.Serializer.DXF;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace SIMULTAN.Exchange.Connectors
{
    /// <summary>
    /// Manages the connection of a network element container to its geometric representation.
    /// </summary>
    internal abstract class ConnectorRepresentativeToBase : ConnectorBase
    {
        #region PROPERTIES: connection

        /// <summary>
        /// The network element holding a component instance that is being represented by the target geometry.
        /// </summary>
        public SimFlowNetworkElement SourceContainer { get; }

        /// <summary>
        /// The connector of the network containing the <see cref="SourceContainer"/>.
        /// </summary>
        public ConnectorToGeometryModel ParentConnector { get; }

        /// <summary>
        /// The connector from the contained component instance of type <see cref="SimComponentInstance"/>
        /// to geometry. If the instance is unattached, it is Null.
        /// </summary>
        public ConnectorAttachedToBase ChildConnector { get; protected set; }

        /// <summary>
        /// The target geometry.
        /// </summary>
        public BaseGeometry Target { get; }

        #endregion

        #region FIELDS

        /// <summary>
        /// Holds the instance currently contained in the <see cref="SourceContainer"/> temproarily,
        /// if there is no child connector.
        /// </summary>
        protected SimComponentInstance instance;

        #endregion


        #region .CTOR

        /// <summary>
        /// Initializes an instance of the class ConnectorRepresentativeToBase. The <see cref="ChildConnector"/> is Null at this point.
        /// </summary>
        /// <param name="_comm_manager">the manager, initializing this instance</param>
        /// <param name="_network_connector">the connector of the network containing the represented container</param>
        /// <param name="_source_container">the container whose content is being represented</param>
        /// <param name="_index_of_geometry_model">the index of the <see cref="GeometryModelData"/> where the geometry resides</param>
        /// <param name="_target_geometry">the target geometry that represents the source</param>
        public ConnectorRepresentativeToBase(ComponentGeometryExchange _comm_manager,
                                             ConnectorToGeometryModel _network_connector, SimFlowNetworkElement _source_container,
                                             int _index_of_geometry_model, BaseGeometry _target_geometry)
            : base(_comm_manager, null, _index_of_geometry_model, _target_geometry)
        {
            this.Target = _target_geometry;
            this.SourceContainer = _source_container;
            this.SourceContainer.PropertyChanged += SourceContainer_PropertyChanged;
            this.SourceContainer.RepresentationReference = new GeometricReference(_index_of_geometry_model, _target_geometry.Id);

            this.instance = this.SourceContainer.GetUpdatedInstance(true);
            if (instance != null)
            {
                instance.PropertyChanged -= ChildInstance_PropertyChanged;
                instance.PropertyChanged += ChildInstance_PropertyChanged;
            }
            this.ParentConnector = _network_connector;
        }

        #endregion

        #region METHODS: Info

        /// <summary>
        /// Checks if the container holds an instance associated with any geometry at all
        /// when the geometry is *not* open.
        /// </summary>
        /// <returns>true, if there is an association, false otherwise</returns>
        public bool ContainsAttachedInstance()
        {
            if (this.SourceContainer.Content == null) return false;
            SimComponentInstance instance = this.SourceContainer.GetUpdatedInstance(true);
            if (instance != null)
            {
                var firstGeometryPlacement = (SimInstancePlacementGeometry)instance.Placements.FirstOrDefault(x => x is SimInstancePlacementGeometry);

                if (firstGeometryPlacement == null) return false;

                string geom_file_path = this.comm_manager.ProjectData.AssetManager.GetPath(firstGeometryPlacement.FileId);
                if (string.IsNullOrEmpty(geom_file_path))
                    return false;

                FileInfo file = new FileInfo(geom_file_path);
                if (string.Equals(file.Extension, ParamStructFileExtensions.FILE_EXT_GEOMETRY_INTERNAL, StringComparison.InvariantCultureIgnoreCase))
                    return true;
            }

            return false;
        }

        #endregion

        #region METHODS: attachment

        /// <summary>
        /// Checks if the container holds an instance associated with any geometry at all
        /// when the geometry is open.
        /// </summary>
        /// <returns>true, if there is an association, false otherwise</returns>
        internal bool IsAttachedToGeometry()
        {
            SimComponentInstance instance = this.SourceContainer.GetUpdatedInstance(true);
            if (instance == null) return false;
            if (this.ChildConnector == null) return false;

            return true;
        }

        /// <summary>
        /// Attaches the instance contained in the source container to the given geometry 
        /// and creates or updates the <see cref="ChildConnector"/>. If there already is an attachment, the child connector is replaced.
        /// </summary>
        /// <param name="_index_of_geometry_model">the index of the geometry model where the attachment target resides</param>
        /// <param name="_target_for_attachment">the attachment target</param>
        /// <param name="_representation">the geometry representing the container - its position or path (TO BE RETRIEVED IN THE METHOD ITSELF!)</param>
        internal void AttachToGeometry(int _index_of_geometry_model, BaseGeometry _target_for_attachment, BaseGeometry _representation)
        {
            SimComponentInstance instance = this.SourceContainer.GetUpdatedInstance(true);
            if (instance != null && _target_for_attachment != null)
            {
                if (_target_for_attachment is Volume)
                {
                    if (this.ChildConnector == null)
                    {
                        this.ChildConnector = new ConnectorAttachedToVolume(this.comm_manager, this.SourceContainer.Content?.Component, this,
                                                                          instance, _index_of_geometry_model, _target_for_attachment as Volume);
                        this.ParentConnector.CommunicateChangeInContent(this.SourceContainer, true, true);
                    }
                    else
                    {
                        // check if the target is the same or different
                        int index_of_target_for_attachment = this.comm_manager.GetModelIndex(_target_for_attachment);
                        if (this.ChildConnector.TargetId == _target_for_attachment.Id && this.ChildConnector.TargetModelIndex == index_of_target_for_attachment)
                        {
                            // same -> do nothing
                        }
                        else
                        {
                            // remove old child connector and create a new one
                            this.ChildConnector = new ConnectorAttachedToVolume(this.comm_manager, this.SourceContainer.Content?.Component, this,
                                                                        instance, _index_of_geometry_model, _target_for_attachment as Volume);
                            this.ParentConnector.CommunicateChangeInContent(this.SourceContainer, true, true);
                        }
                    }
                    this.ChildConnector.SynchronizeSourceWTarget(_target_for_attachment);
                    this.PassRepresentationInfoToInstance(_representation);

                    // handle references
                    IEnumerable<ConnectorToComplex> target_descriptors = this.comm_manager.ConnectorManager.GetSpaceDescriptorOf(_target_for_attachment as Volume);
                    if (target_descriptors != null)
                    {
                        foreach (var target_descriptor in target_descriptors)
                            this.comm_manager.ProjectData.Components.AddReferenceBasedOnGeometry(target_descriptor.DescriptiveSource, this.SourceContainer.Content?.Component);
                    }
                }

            }
        }

        /// <summary>
        /// Passes the position to the source of the <see cref="ChildConnector"/>.
        /// </summary>
        /// <param name="_representation">the geometry carrying the position or path in 3d</param>
        protected virtual void PassRepresentationInfoToInstance(BaseGeometry _representation)
        {
            GeometricReference ref_old = this.SourceContainer.RepresentationReference;
            this.SourceContainer.RepresentationReference = new GeometricReference(ref_old.FileId, _representation.Id);
        }

        /// <summary>
        /// Detaches the instance contained in the source container from any geometry 
        /// and deletes the <see cref="ChildConnector"/>. If there is no attachment or a mismatch in the attached geometry,
        /// nothing happens.
        /// </summary>
        internal void DetachFromGeometry()
        {
            if (this.ChildConnector != null)
            {
                SimComponent instance_parent = this.ChildConnector.SourceParent;
                SimComponentInstance instance = this.ChildConnector.AttachedSource;
                if (instance != null)
                {
                    var firstGeometryPlacement = (SimInstancePlacementGeometry)instance.Placements.FirstOrDefault(x => x is SimInstancePlacementGeometry);
                    if (firstGeometryPlacement != null)
                    {
                        BaseGeometry _former_target = this.comm_manager.GeometryManager.GetGeometryFromId(firstGeometryPlacement.FileId, firstGeometryPlacement.GeometryId);

                        this.ChildConnector = null;
                        this.ParentConnector.CommunicateChangeInContent(this.SourceContainer, true, false);
                        instance.State = new SimInstanceState(false, SimInstanceConnectionState.GeometryDeleted);

                        // handle references
                        if (_former_target != null)
                        {
                            IEnumerable<ConnectorToComplex> target_descriptors = this.comm_manager.ConnectorManager.GetSpaceDescriptorOf(_former_target as Volume);
                            if (target_descriptors != null)
                            {
                                foreach (var target_descriptor in target_descriptors)
                                    this.comm_manager.ProjectData.Components.RemoveReferenceBasedOnGeometry(target_descriptor.DescriptiveSource, instance_parent);
                            }
                        }
                    }
                }
            }

        }

        #endregion

        #region METHOD OVERRIDES

        /// <inheritdoc/>
        public override void SynchronizeSourceWTarget(BaseGeometry _target)
        {
            if (this.SourceContainer == null) return;

            if (this.SynchTargetIsAdmissible(_target))
            {
                // update the child connector
                this.PassRepresentationInfoToInstance(_target);
            }
        }

        /// <inheritdoc/>
        internal override void BeforeDeletion()
        {
            this.DetachFromGeometry();

            if (this.SourceContainer != null)
            {
                this.SourceContainer.PropertyChanged -= SourceContainer_PropertyChanged;
                this.instance = this.SourceContainer.GetUpdatedInstance(true);
                if (instance != null)
                    instance.PropertyChanged -= ChildInstance_PropertyChanged;
            }
        }

        #endregion

        #region EVENT HANDLERS

        /// <summary>
        /// Reacts to changes in the instance contained in the <see cref="SourceContainer"/>.
        /// </summary>
        /// <param name="sender">the instance</param>
        /// <param name="e">information about the changed properties</param>
        protected void ChildInstance_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (sender == null || e == null) return;

            if (e.PropertyName == nameof(SimComponentInstance.InstanceSize))
            {
                // tell the proxy geometry to change size
                ProxyGeometry decorator = this.ParentConnector.GetProxyDecorator(this);
                if (decorator != null)
                {
                    decorator.Size = this.SourceContainer.GetInstanceSize().Max;
                    var instance = SourceContainer.GetUpdatedInstance(true);
                    decorator.Rotation = instance != null ? instance.InstanceRotation : Quaternion.Identity;
                }
            }
        }

        /// <summary>
        /// Handles the changes in the content of the <see cref="SourceContainer"/>.
        /// </summary>
        /// <param name="sender">the source container</param>
        /// <param name="e">the information about the changed properties</param>
        protected virtual void SourceContainer_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (sender == null || e == null) return;

            if (e.PropertyName == nameof(SimFlowNetworkElement.Content))
            {
                this.ReactToChangeInContent();
            }
        }

        /// <summary>
        /// Attaches and detaches event listeners to and from the instance contained
        /// in the <see cref="SourceContainer"/> depending on the bound component.
        /// </summary>
        protected virtual void ReactToChangeInContent()
        {
            if (this.SourceContainer.Content == null)
            {
                this.DetachFromGeometry();
                if (this.instance != null)
                    instance.PropertyChanged -= ChildInstance_PropertyChanged;
                this.instance = null;
                // tell the proxy geometry to reset size
                ProxyGeometry decorator = this.ParentConnector.GetProxyDecorator(this);
                if (decorator != null)
                {
                    decorator.Size = new Vector3D(0, 0, 0);
                    decorator.Rotation = Quaternion.Identity;
                }
            }
            else
            {
                this.instance = this.SourceContainer.GetUpdatedInstance(true);
                if (instance != null)
                {
                    instance.PropertyChanged -= ChildInstance_PropertyChanged;
                    instance.PropertyChanged += ChildInstance_PropertyChanged;
                }
            }
            this.ParentConnector.CommunicateChangeInContent(this.SourceContainer, this.SourceContainer.Content != null, this.ChildConnector != null);
            this.ParentConnector.ConnectorAssetsChanged(this);
        }

        #endregion
    }
}
