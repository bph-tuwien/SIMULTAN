using SIMULTAN.Data.Assets;
using SIMULTAN.Data.Components;
using SIMULTAN.Data.FlowNetworks;
using SIMULTAN.Data.Geometry;
using SIMULTAN.Data.SitePlanner;
using SIMULTAN.Exchange.GeometryConnectors;
using SIMULTAN.Exchange.NetworkConnectors;
using SIMULTAN.Exchange.SitePlannerConnectors;
using SIMULTAN.Projects;
using SIMULTAN.Serializer.SimGeo;
using SIMULTAN.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SIMULTAN.Exchange
{
    /// <summary>
    /// Manages the synchronization between Geometry and Instances.
    /// Also handles instances in Networks/Network Geometry Models and in the SitePlanner
    /// </summary>
    public class ComponentGeometryExchange : IOffsetQueryable
    {
        /// <summary>
        /// Supresses the <see cref="GeometryInvalidated"/> event when set to False.
        /// The default value is True
        /// </summary>
        internal bool EnableNotifyGeometryInvalidated
        {
            get => notifyGeometryInvalidated;
            set { notifyGeometryInvalidated = value; }
        }
        private bool notifyGeometryInvalidated = true;

        /// <summary>
        /// Enables/Disables whether Geometry related events should be handled.
        /// The default value is True, when change from False to True, a <see cref="Synchronize"/> is forced.
        /// This property helps when a large number of events in short time is expected, for example,
        /// while a UI operation for moving vertices is ongoing.
        /// </summary>
        public bool EnableGeometryEvents
        {
            get => enableGeometryEvents;
            set
            {
                if (enableGeometryEvents != value)
                {
                    enableGeometryEvents = value;
                    if (enableGeometryEvents)
                        Synchronize();
                }
            }
        }
        private bool enableGeometryEvents = true;

        /// <summary>
        /// The project data
        /// </summary>
        internal ProjectData ProjectData { get; }

        private Dictionary<GeometryModel, GeometryModelConnector> geometryModelConnectors = new Dictionary<GeometryModel, GeometryModelConnector>();
        private Dictionary<GeometryModel, NetworkGeometryModelConnector> networkModelConnectors = new Dictionary<GeometryModel, NetworkGeometryModelConnector>();
        private Dictionary<SitePlannerProject, SitePlannerProjectConnector> siteplannerConnectors = new Dictionary<SitePlannerProject, SitePlannerProjectConnector>();

        /// <summary>
        /// Initializes a new instance of the <see cref="ComponentGeometryExchange"/>
        /// </summary>
        /// <param name="projectData">The project data to which this exchange belongs to</param>
        public ComponentGeometryExchange(ProjectData projectData)
        {
            if (projectData == null)
                throw new ArgumentNullException(nameof(projectData));

            this.ProjectData = projectData;
        }

        #region Geometry Models

        /// <summary>
        /// Adds a new <see cref="GeometryModel"/> to the Exchange
        /// </summary>
        /// <param name="model">The new geometry model</param>
        internal void AddGeometryModel(GeometryModel model)
        {
            if (this.geometryModelConnectors.ContainsKey(model))
                throw new ArgumentException("Model is already registered");

            model.Exchange = this;
            var connector = new GeometryModelConnector(model, this);
            this.geometryModelConnectors.Add(model, connector);
            connector.Initialize(); //Has to be done after the connector has been added to connectors. Otherwise the offset surfaces can't be calculated

            //Check if this model is associated with a network
            var network = ProjectData.NetworkManager.NetworkRecord.FirstOrDefault(x => x.IndexOfGeometricRepFile == model.File.Key);
            if (network != null)
            {
                networkModelConnectors.Add(model, new NetworkGeometryModelConnector(model, network, this));
            }
        }
        /// <summary>
        /// Removes a <see cref="GeometryModel"/> from the Exchange, e.g., when the Model gets closed
        /// </summary>
        /// <param name="model"></param>
        internal void RemoveGeometryModel(GeometryModel model)
        {
            if (geometryModelConnectors.TryGetValue(model, out var modelConnector))
            {
                modelConnector.Dispose();
                geometryModelConnectors.Remove(model);
            }
            if (networkModelConnectors.TryGetValue(model, out var networkConnector))
            {
                networkConnector.Dispose();
                networkModelConnectors.Remove(model);
            }

            model.Exchange = null;
        }
        /// <summary>
        /// Searches for the Geometry that belongs to a specific placement
        /// </summary>
        /// <param name="placement">The placement</param>
        /// <returns>Either a matching <see cref="BaseGeometry"/>, or Null when no geometry exists.
        /// Also returns Null when the corresponding GeometryModel isn't loaded.</returns>
        public BaseGeometry GetGeometry(SimInstancePlacementGeometry placement)
        {
            var connector = this.geometryModelConnectors.FirstOrDefault(x => x.Value.GeometryModel.File.Key == placement.FileId);
            return connector.Key.Geometry.GeometryFromId(placement.GeometryId);
        }

        /// <summary>
        /// Called when the name of a BaseGeometry has changed
        /// </summary>
        /// <param name="geometry">The geometry in which the name has changed</param>
        internal void OnGeometryNameChanged(BaseGeometry geometry)
        {
            if (geometry == null)
                throw new ArgumentNullException(nameof(geometry));
            if (geometry.ModelGeometry == null || geometry.ModelGeometry.Model == null)
                throw new ArgumentException("Geometry is not part of a GeometryModel");

            if (this.geometryModelConnectors.TryGetValue(geometry.ModelGeometry.Model, out var connector))
                connector.OnGeometryNameChanged(geometry);
        }

        #endregion

        #region Instances / Placements

        /// <summary>
        /// Notifies the exchange that a new component has been added.
        /// Will add all placements of all instances
        /// </summary>
        /// <param name="component">The added component</param>
        internal void OnComponentAdded(SimComponent component)
        {
            foreach (var instance in component.Instances)
                OnInstanceAdded(instance);
        }
        /// <summary>
        /// Notifies the exchange that a component has been removed.
        /// Will remove all placements of all instances
        /// </summary>
        /// <param name="component">The removed component</param>
        internal void OnComponentRemoved(SimComponent component)
        {
            foreach (var instance in component.Instances)
                OnInstanceRemoved(instance);
        }
        /// <summary>
        /// Notifies the exchange that a new instance has been added to an already registered component
        /// Will add all placements of this instance
        /// </summary>
        /// <param name="instance">The added instance</param>
        internal void OnInstanceAdded(SimComponentInstance instance)
        {
            foreach (var geoPlacement in instance.Placements.OfType<SimInstancePlacementGeometry>())
            {
                OnPlacementAdded(geoPlacement);
            }
        }
        /// <summary>
        /// Notifies the exchange that an instance has been removed
        /// Will remove all placements of this instance
        /// </summary>
        /// <param name="instance">The removed instance</param>
        internal void OnInstanceRemoved(SimComponentInstance instance)
        {
            foreach (var geoPlacement in instance.Placements.OfType<SimInstancePlacementGeometry>())
            {
                OnPlacementRemoved(geoPlacement);
            }
        }
        /// <summary>
        /// Notifies the exchange that a new placement has been added to an already registered instance
        /// </summary>
        /// <param name="placement">The added placement</param>
        internal void OnPlacementAdded(SimInstancePlacementGeometry placement)
        {
            var resource = ProjectData.AssetManager.GetResource(placement.FileId) as ResourceFileEntry;

            if (resource != null)
            {
                //Geometry
                //Check if model is loaded
                if (ProjectData.GeometryModels.TryGetGeometryModel(resource, out var gm, false))
                {
                    if (this.geometryModelConnectors.TryGetValue(gm, out var connector))
                        connector.OnPlacementAdded(placement);
                }

                //SitePlanner
                var sitePlannerProject = ProjectData.SitePlannerManager.SitePlannerProjects.FirstOrDefault(x => x.SitePlannerFile.Key == resource.Key);
                if (sitePlannerProject != null)
                {
                    if (this.siteplannerConnectors.TryGetValue(sitePlannerProject, out var connector))
                        connector.OnPlacementAdded(placement);
                }
            }
        }
        /// <summary>
        /// Notifies the exchange that a placement has been removed
        /// </summary>
        /// <param name="placement">The removed placement</param>
        internal void OnPlacementRemoved(SimInstancePlacementGeometry placement)
        {
            var resource = ProjectData.AssetManager.GetResource(placement.FileId) as ResourceFileEntry;
            //Check if model is loaded
            if (resource != null)
            {
                if (ProjectData.GeometryModels.TryGetGeometryModel(resource, out var gm, false))
                {
                    if (this.geometryModelConnectors.TryGetValue(gm, out var connector))
                        connector.OnPlacementRemoved(placement);
                }

                var sitePlannerProject = ProjectData.SitePlannerManager.SitePlannerProjects.FirstOrDefault(x => x.SitePlannerFile.Key == resource.Key);
                if (sitePlannerProject != null)
                {
                    if (this.siteplannerConnectors.TryGetValue(sitePlannerProject, out var connector))
                        connector.OnPlacementRemoved(placement);
                }
            }
        }

        #endregion

        #region Parameters

        /// <summary>
        /// Notifies the exchange that the value of a parameter has changed
        /// </summary>
        /// <param name="parameter">The modified parameter</param>
        internal void OnParameterValueChanged(SimParameter parameter)
        {
            if (parameter.Component != null)
            {
                List<BaseGeometry> affectedGeometry = new List<BaseGeometry>();

                foreach (var inst in parameter.Component.Instances)
                {
                    affectedGeometry.AddRange(OnParameterValueChangedInternal(parameter, inst));
                }

                if (affectedGeometry.Count > 0)
                    NotifyGeometryInvalidated(affectedGeometry);
            }
        }
        /// <summary>
        /// Notifies the exchange that the value of a parameter has changed for a specific instance
        /// See <see cref="SimComponentInstance.InstanceParameterValuesPersistent"/>
        /// </summary>
        /// <param name="parameter">The modified parameter</param>
        /// <param name="instance">The instance in which the value has changed</param>
        public void OnParameterValueChanged(SimParameter parameter, SimComponentInstance instance)
        {
            var affectedGeometry = OnParameterValueChangedInternal(parameter, instance);

            if (affectedGeometry.Count > 0)
                NotifyGeometryInvalidated(affectedGeometry);
        }

        private List<BaseGeometry> OnParameterValueChangedInternal(SimParameter parameter, SimComponentInstance instance)
        {
            List<BaseGeometry> affectedGeometry = new List<BaseGeometry>();

            if (parameter.Component != null)
            {
                foreach (var placement in instance.Placements.OfType<SimInstancePlacementGeometry>())
                {
                    var resource = ProjectData.AssetManager.GetResource(placement.FileId) as ResourceFileEntry;

                    if (resource != null)
                    {
                        //Geometry
                        if (ProjectData.GeometryModels.TryGetGeometryModel(resource, out var gm, false))
                        {
                            if (this.geometryModelConnectors.TryGetValue(gm, out var connector))
                                affectedGeometry.AddRange(connector.OnParameterValueChanged(placement, parameter));
                        }

                        //Site Planner
                        var sitePlannerProject = ProjectData.SitePlannerManager.SitePlannerProjects.FirstOrDefault(x => x.SitePlannerFile.Key == resource.Key);
                        if (sitePlannerProject != null)
                        {
                            if (this.siteplannerConnectors.TryGetValue(sitePlannerProject, out var connector))
                            {
                                foreach (var affectedBuilding in connector.OnParameterValueChanged(placement, parameter))
                                {
                                    NotifyBuildingComponentParamaterChanged(affectedBuilding);
                                }
                            }
                        }
                    }
                }
            }

            return affectedGeometry;
        }
        /// <summary>
        /// Notifies the exchange that several parameters have changed their value for a specific instance
        /// See <see cref="SimComponentInstance.InstanceParameterValuesPersistent"/>
        /// </summary>
        /// <param name="parameters">The modified parameters</param>
        /// <param name="instance">The instance in which the value has changed</param>
        public void OnParameterValueChanged(IEnumerable<SimParameter> parameters, SimComponentInstance instance)
        {
            HashSet<BaseGeometry> affectedGeometry = new HashSet<BaseGeometry>();

            foreach (var parameter in parameters)
            {
                foreach (var geom in OnParameterValueChangedInternal(parameter, instance))
                    affectedGeometry.Add(geom);
            }

            if (affectedGeometry.Count > 0)
                NotifyGeometryInvalidated(affectedGeometry);
        }

        #endregion

        #region SitePlanner Projects

        /// <summary>
        /// Adds a new siteplanner project to the exchange
        /// </summary>
        /// <param name="project">The siteplanner project</param>
        internal void AddSiteplannerProject(SitePlannerProject project)
        {
            if (siteplannerConnectors.ContainsKey(project))
                throw new ArgumentException("SitePlannerProject is already registered");

            siteplannerConnectors.Add(project, new SitePlannerProjectConnector(project, this));
        }
        /// <summary>
        /// Removes a siteplanner project from the exchange
        /// </summary>
        /// <param name="project">The removed siteplanner project</param>
        internal void RemoveSiteplannerProject(SitePlannerProject project)
        {
            if (siteplannerConnectors.TryGetValue(project, out var connector))
            {
                siteplannerConnectors.Remove(project);
                connector.Dispose();
            }
        }
        /// <summary>
        /// Returns True when a SitePlanner project has been registered in the exchange
        /// </summary>
        /// <param name="project">The siteplanner project</param>
        /// <returns>True when a SitePlanner project has been registered in the exchange, otherwise False</returns>
        public bool IsConnected(SitePlannerProject project)
        {
            if (project == null)
                throw new ArgumentNullException(nameof(project));

            return siteplannerConnectors.ContainsKey(project);
        }

        #endregion


        #region Components

        /// <summary>
        /// Event handler delegate for the AssociationChanged event.
        /// </summary>
        /// <param name="sender">the sending object</param>
        /// <param name="affected_geometry">the geometry whose association (i.e., source, new connector, deleted connector) changed</param>
        public delegate void AssociationChangedEventHandler(object sender, IEnumerable<BaseGeometry> affected_geometry);
        /// <summary>
        /// Invoked when the association relationship in one or more connectors has changed:
        /// either the source component or the target geometry.
        /// </summary>
        public event AssociationChangedEventHandler AssociationChanged;
        internal void NotifyAssociationChanged(IEnumerable<BaseGeometry> geometry)
        {
            this.AssociationChanged?.Invoke(this, geometry);
        }

        /// <summary>
        /// Creates a new instance and placements that connects the component to the geometry.
        /// Throws an ArgumentException when the geometry type does not match the <see cref="SimComponent.InstanceType"/>
        /// </summary>
        /// <param name="component">The component</param>
        /// <param name="geometry">The geometry</param>
        public void Associate(SimComponent component, BaseGeometry geometry)
        {
            if (geometry == null)
                throw new ArgumentNullException(nameof(geometry));
            if (component == null)
                throw new ArgumentNullException(nameof(component));

            if (geometry.ModelGeometry == null || geometry.ModelGeometry.Model == null)
                throw new ArgumentException("Geometry does not belong to a GeometryModel");
            if (component.Factory == null)
                throw new ArgumentException("Component does not belong to a Project");

            var validity = IsValidAssociation(component, geometry);
            if (!validity.isValid)
                throw new ArgumentException("Geometry Type does not match Component.InstanceType");

            //Create instance
            if (validity.needsCreate)
            {
                SimComponentInstance instance = new SimComponentInstance(component.InstanceType,
                    geometry.ModelGeometry.Model.File.Key, geometry.Id, new ulong[] { });
                component.Instances.Add(instance);
            }
        }
        /// <summary>
        /// Creates new instances and placements that connect the component with all geometries.
        /// Throws an ArgumentException when the geometry type does not match the <see cref="SimComponent.InstanceType"/>
        /// </summary>
        /// <param name="component">The component</param>
        /// <param name="geometries">The geometries</param>
        public void Associate(SimComponent component, IEnumerable<BaseGeometry> geometries)
        {
            if (geometries == null)
                throw new ArgumentNullException(nameof(geometries));
            if (component == null)
                throw new ArgumentNullException(nameof(component));

            foreach (var geometry in geometries)
            {
                Associate(component, geometry);
            }
        }
        /// <summary>
        /// Creates an instance and placements that connect the component with a SitePlannerBuilding
        /// </summary>
        /// <param name="component">The component</param>
        /// <param name="building">The building</param>
        public void Associate(SimComponent component, SitePlannerBuilding building)
        {
            if (component == null)
                throw new ArgumentNullException(nameof(component));
            if (building == null)
                throw new ArgumentNullException(nameof(building));

            if (building.Project == null)
                throw new ArgumentException("Building is not part of a project");

            var validity = IsValidAssociation(component, building);
            if (!validity.isValid)
                throw new ArgumentException("SitePlanner building does not match Component.InstanceType");

            //Create instance
            if (validity.needsCreate)
            {
                SimComponentInstance instance = new SimComponentInstance(component.InstanceType,
                    building.Project.SitePlannerFile.Key, building.ID, null);
                component.Instances.Add(instance);
            }
        }

        /// <summary>
        /// Removes the instance which connects the component to the geometry
        /// </summary>
        /// <param name="component">The component</param>
        /// <param name="geometry">The geometry</param>
        public void Disassociate(SimComponent component, BaseGeometry geometry)
        {
            if (geometry == null)
                throw new ArgumentNullException(nameof(geometry));
            if (component == null)
                throw new ArgumentNullException(nameof(component));

            if (geometry.ModelGeometry == null || geometry.ModelGeometry.Model == null)
                throw new ArgumentException("Geometry does not belong to a GeometryModel");
            if (component.Factory == null)
                throw new ArgumentException("Component does not belong to a Project");

            //Find instance
            for (int i = 0; i < component.Instances.Count; ++i)
            {
                bool removedAnything = false;
                var inst = component.Instances[i];

                //Remove matching placements
                for (int p = 0; p < inst.Placements.Count; ++p)
                {
                    var placement = inst.Placements[p];
                    if (placement is SimInstancePlacementGeometry pg &&
                        pg.FileId == geometry.ModelGeometry.Model.File.Key && pg.GeometryId == geometry.Id)
                    {
                        inst.Placements.RemoveAt(p);
                        removedAnything = true;
                        p--;
                    }
                }

                //Remove instance when no other placements exist
                if (removedAnything && inst.Placements.Count == 0)
                {
                    component.Instances.RemoveAt(i);
                    i--;
                }
            }
        }
        /// <summary>
        /// Removes the instance which connects the component to the building
        /// </summary>
        /// <param name="component">The component</param>
        /// <param name="building">The SitePlanner building</param>
        public void Disassociate(SimComponent component, SitePlannerBuilding building)
        {
            if (building == null)
                throw new ArgumentNullException(nameof(building));
            if (component == null)
                throw new ArgumentNullException(nameof(component));

            if (building.Project == null)
                throw new ArgumentException("Building is not part of a project");

            for (int i = 0; i < component.Instances.Count; ++i)
            {
                var inst = component.Instances[i];
                bool removedAny = false;

                for (int j = 0; j < inst.Placements.Count; j++)
                {
                    if (inst.Placements[j] is SimInstancePlacementGeometry pg &&
                        pg.FileId == building.Project.SitePlannerFile.Key &&
                        pg.GeometryId == building.ID)
                    {
                        inst.Placements.RemoveAt(j);
                        removedAny = true;
                        j--;
                    }
                }

                if (removedAny && inst.Placements.Count == 0)
                {
                    component.Instances.RemoveAt(i);
                    i--;
                }
            }
        }

        /// <summary>
        /// Returns all components associated with a geometry
        /// </summary>
        /// <param name="geometry">The geometry</param>
        /// <returns>All components associated with the geometry</returns>
        public IEnumerable<SimComponent> GetComponents(BaseGeometry geometry)
        {
            if (geometry == null)
                throw new ArgumentNullException(nameof(geometry));
            if (geometry.ModelGeometry == null || geometry.ModelGeometry.Model == null)
                throw new ArgumentException("Geometry does not belong to a GeometryModel");

            if (this.geometryModelConnectors.TryGetValue(geometry.ModelGeometry.Model, out var con))
            {
                foreach (var pl in con.GetPlacements(geometry))
                    yield return pl.Instance.Component;
            }
        }
        /// <summary>
        /// Returns all placements associated with a geometry
        /// </summary>
        /// <param name="geometry">The geometry</param>
        /// <returns>All placements associated with the geometry</returns>
        public IEnumerable<SimInstancePlacementGeometry> GetPlacements(BaseGeometry geometry)
        {
            if (geometry == null)
                throw new ArgumentNullException(nameof(geometry));
            if (geometry.ModelGeometry == null || geometry.ModelGeometry.Model == null)
                throw new ArgumentException("Geometry does not belong to a GeometryModel");

            if (this.geometryModelConnectors.TryGetValue(geometry.ModelGeometry.Model, out var con))
            {
                return con.GetPlacements(geometry);
            }
            else
                return Enumerable.Empty<SimInstancePlacementGeometry>();
        }
        /// <summary>
        /// Returns all components associated with a building
        /// </summary>
        /// <param name="building">The building</param>
        /// <returns>All components associated with the building</returns>
        public IEnumerable<SimComponent> GetComponents(SitePlannerBuilding building)
        {
            if (siteplannerConnectors.TryGetValue(building.Project, out var con))
            {
                return con.GetPlacement(building).Select(x => x.Instance.Component);
            }

            return Enumerable.Empty<SimComponent>();
        }
        /// <summary>
        /// Returns all placements associated with a building
        /// </summary>
        /// <param name="building">The building</param>
        /// <returns>All placements associated with the building</returns>
        public IEnumerable<SimInstancePlacementGeometry> GetPlacements(SitePlannerBuilding building)
        {
            if (siteplannerConnectors.TryGetValue(building.Project, out var con))
            {
                return con.GetPlacement(building);
            }

            return Enumerable.Empty<SimInstancePlacementGeometry>();
        }

        private (bool isValid, bool needsCreate) IsValidAssociation(SimComponent component, BaseGeometry geometry)
        {
            if (component.InstanceType == SimInstanceType.AttributesFace && geometry is Face)
            {
                if (component.Instances.Count == 0)
                    return (true, true);
                if (component.Instances.Any(c => c.Placements.Any(x => x is SimInstancePlacementGeometry gp &&
                    gp.FileId == geometry.ModelGeometry.Model.File.Key && gp.GeometryId == geometry.Id)))
                    return (true, false);
                else
                    return (true, true);
            }
            else if (component.InstanceType == SimInstanceType.AttributesEdge && geometry is Edge)
            {
                if (component.Instances.Count == 0)
                    return (true, true);
                if (component.Instances.Any(c => c.Placements.Any(x => x is SimInstancePlacementGeometry gp &&
                    gp.FileId == geometry.ModelGeometry.Model.File.Key && gp.GeometryId == geometry.Id)))
                    return (true, false);
                else
                    return (true, true);
            }
            else if (component.InstanceType == SimInstanceType.AttributesPoint && geometry is Vertex)
            {
                if (component.Instances.Count == 0)
                    return (true, true);
                if (component.Instances.Any(c => c.Placements.Any(x => x is SimInstancePlacementGeometry gp &&
                    gp.FileId == geometry.ModelGeometry.Model.File.Key && gp.GeometryId == geometry.Id)))
                    return (true, false);
                else
                    return (true, true);
            }
            else if (component.InstanceType == SimInstanceType.Entity3D && geometry is Volume)
            {
                if (component.Instances.Count == 0)
                    return (true, true);
                if (component.Instances[0].Placements.Any(x => x is SimInstancePlacementGeometry gp &&
                    gp.FileId == geometry.ModelGeometry.Model.File.Key && gp.GeometryId == geometry.Id))
                    return (true, false);
            }

            return (false, false);
        }

        private (bool isValid, bool needsCreate) IsValidAssociation(SimComponent component, SitePlannerBuilding building)
        {
            if (component.InstanceType == SimInstanceType.BuiltStructure)
                return (component.Instances.Count == 0, true);

            return (false, false);
        }

        #endregion

        #region Networks

        /// <summary>
        /// Converts a network into a GeometryModel
        /// </summary>
        /// <param name="network">The network</param>
        /// <param name="targetFile">The location where the geometry model file should be created</param>
        /// <returns>The geometry model</returns>
        public GeometryModel ConvertNetwork(SimFlowNetwork network, FileInfo targetFile)
        {
            if (network == null)
                throw new ArgumentNullException(nameof(network));
            if (network.IndexOfGeometricRepFile != -1)
                throw new ArgumentException("Network already has a geometric representation");

            //Create empty asset
            var asset = ((ExtendedProjectData)ProjectData).Project.AddEmptyResource(targetFile);

            //Add reference to network
            network.IndexOfGeometricRepFile = asset.Key;

            //Load geometry file
            GeometryModelData geometryData = new GeometryModelData();
            GeometryModel gm = new GeometryModel(Guid.NewGuid(), targetFile.Name, asset, OperationPermission.DefaultNetworkPermissions, geometryData);

            //Get an owning resource to the model
            //Also calls AddGeometryModel in this class
            //Don't forget to free the reference after adding it to the GeometryViewer
            ProjectData.GeometryModels.AddGeometryModel(gm);

            //Save empty model
            if (!SimGeoIO.Save(gm, gm.File, SimGeoIO.WriteMode.Plaintext))
                return null;

            return gm;
        }

        #endregion


        #region IOffsetQueryable

        /// <inheritdoc />
        public event GeometryInvalidatedEventHandler GeometryInvalidated;

        /// <summary>
        /// Invokes the <see cref="GeometryInvalidated"/> event
        /// </summary>
        /// <param name="geometry">The modified geometry</param>
        public void NotifyGeometryInvalidated(IEnumerable<BaseGeometry> geometry)
        {
            if (EnableNotifyGeometryInvalidated)
                this.GeometryInvalidated?.Invoke(this, geometry);
        }

        /// <inheritdoc />
        public (double outer, double inner) GetFaceOffset(Face face)
        {
            //Happens during load
            if (face.ModelGeometry.Model == null)
                return (0, 0);

            double outer = 0.0, inner = 0.0;

            foreach (var pl in this.GetPlacements(face))
            {
                if (pl.Instance.InstanceType == SimInstanceType.AttributesFace)
                {
                    var dinParameter = pl.Instance.Component.Parameters.FirstOrDefault(x => x.Name == ReservedParameters.RP_MATERIAL_COMPOSITE_D_IN);
                    var doutParameter = pl.Instance.Component.Parameters.FirstOrDefault(x => x.Name == ReservedParameters.RP_MATERIAL_COMPOSITE_D_OUT);

                    //Check if parameters exist
                    if (dinParameter != null)
                        inner = pl.Instance.InstanceParameterValuesPersistent[dinParameter];
                    if (doutParameter != null)
                        outer = pl.Instance.InstanceParameterValuesPersistent[doutParameter];
                }
            }

            return (outer, inner);
        }

        #endregion

        #region SitePlanner Events

        /// <summary>
        /// Event handler delegate for the BuildingAssociationChanged event.
        /// </summary>
        /// <param name="sender">the sending object</param>
        /// <param name="affectedBuildings">the buildings whose association (i.e., source, new connector, deleted connector) changed</param>
        public delegate void BuildingAssociationChangedEventHandler(object sender, IEnumerable<SitePlannerBuilding> affectedBuildings);

        /// <summary>
		/// Invoked when the association relationship in one or more connectors has changed:
		/// either the source component or the target building.
		/// </summary>
		public event BuildingAssociationChangedEventHandler BuildingAssociationChanged;
        /// <summary>
        /// Invokes the <see cref="BuildingAssociationChanged"/> event
        /// </summary>
        /// <param name="affectedBuildings">The geometry for which the associations have been changed</param>
        internal void NotifyBuildingAssociationChanged(IEnumerable<SitePlannerBuilding> affectedBuildings)
        {
            this.BuildingAssociationChanged?.Invoke(this, affectedBuildings);
        }


        /// <summary>
        /// Event handler delegate for the BuildingComponentParameterChanged event.
        /// </summary>
        /// <param name="sender">the sending object</param>
        /// <param name="affectedBuilding">the buildings which is affected by the change</param>
        public delegate void BuildingComponentParameterChangedEventHandler(object sender, SitePlannerBuilding affectedBuilding);

        /// <summary>
        /// Invoked when the parameter of an associated component is changed
        /// </summary>
        public event BuildingComponentParameterChangedEventHandler BuildingComponentParamaterChanged;
        /// <summary>
        /// Invokes the <see cref="BuildingComponentParamaterChanged"/>
        /// </summary>
        /// <param name="affectedBuilding">The buildings in which the parameter has been modified</param>
        private void NotifyBuildingComponentParamaterChanged(SitePlannerBuilding affectedBuilding)
        {
            BuildingComponentParamaterChanged?.Invoke(this, affectedBuilding);
        }

        #endregion

        /// <summary>
        /// Forces a synchronization of all geometry connections.
        /// Needed when changes should be forced while <see cref="EnableGeometryEvents"/> is False
        /// </summary>
        public void Synchronize()
        {
            this.geometryModelConnectors.Values.ForEach(x => x.SynchronizeChanges());
        }
    }
}
