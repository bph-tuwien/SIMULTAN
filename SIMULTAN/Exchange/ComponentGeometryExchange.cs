using MathNet.Numerics.Distributions;
using SIMULTAN.Data.Assets;
using SIMULTAN.Data.Components;
using SIMULTAN.Data.FlowNetworks;
using SIMULTAN.Data.Geometry;
using SIMULTAN.Data.SimNetworks;
using SIMULTAN.Data.SitePlanner;
using SIMULTAN.Data.Taxonomy;
using SIMULTAN.Exchange.GeometryConnectors;
using SIMULTAN.Exchange.NetworkConnectors;
using SIMULTAN.Exchange.SimNetworkConnectors;
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
        /// Suppresses the <see cref="GeometryInvalidated"/> event when set to False.
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
        /// Stores the existing connectors between SimNetwork and Geometry
        /// </summary>
        public Dictionary<GeometryModel, SimNetworkGeometryModelConnector> SimNetworkModelConnectors = new Dictionary<GeometryModel, SimNetworkGeometryModelConnector>();

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
        /// <returns>The connector that is attached to the GeometryModel</returns>
        internal GeometryModelConnector AddGeometryModel(GeometryModel model)
        {
            if (this.geometryModelConnectors.ContainsKey(model))
                throw new ArgumentException("Model is already registered");

            model.Exchange = this;
            var connector = new GeometryModelConnector(model, this);
            this.geometryModelConnectors.Add(model, connector);

            //Check if this model is associated with a network
            var network = ProjectData.NetworkManager.NetworkRecord.FirstOrDefault(x => x.IndexOfGeometricRepFile == model.File.Key);
            if (network != null)
            {
                networkModelConnectors.Add(model, new NetworkGeometryModelConnector(model, network, this));
            }

            //Check if this model is associated with a SimNetwork
            var simNetwork = ProjectData.SimNetworks.FirstOrDefault(x => x.IndexOfGeometricRepFile == model.File.Key);
            if (simNetwork != null)
            {
                SimNetworkModelConnectors.Add(model, new SimNetworkGeometryModelConnector(model, simNetwork, this));
            }

            return connector;
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
            if (SimNetworkModelConnectors.TryGetValue(model, out var simNetworkConnector))
            {
                simNetworkConnector.Dispose();
                SimNetworkModelConnectors.Remove(model);

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
        internal void OnParameterValueChanged(SimBaseParameter parameter)
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

                //Handle Static Ports in SimNetworks with GeometryModel opened
                if (parameter.Component.InstanceType == SimInstanceType.InPort ||
                    parameter.Component.InstanceType == SimInstanceType.OutPort)
                {
                    var networkPlacements = parameter.Component.Instances.SelectMany(t => t.Placements.OfType<SimInstancePlacementSimNetwork>());
                    foreach (var nwPlacement in networkPlacements)
                    {
                        if (nwPlacement.NetworkElement is SimNetworkPort port && parameter is SimDoubleParameter dParam)
                        {
                            //Check if this model is associated with a SimNetwork
                            var parent = port.ParentNetworkElement;
                            while (parent.ParentNetwork != null)
                            {
                                parent = parent.ParentNetwork;
                            }

                            SimNetwork simNetwork = parent as SimNetwork;

                            var indRepFile = simNetwork.IndexOfGeometricRepFile;
                            var resource = this.ProjectData.AssetManager.GetResource(indRepFile);
                            if (resource != null)
                            {
                                if (this.ProjectData.GeometryModels.TryGetGeometryModel(resource as ResourceFileEntry, out var geom))
                                {
                                    if (this.SimNetworkModelConnectors.TryGetValue(geom, out var simNetworkConnector))
                                    {
                                        simNetworkConnector.OnStaticPortCoordinateChanged(dParam);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        /// <summary>
        /// Notifies the exchange that the value of a parameter has changed for a specific instance
        /// See <see cref="SimComponentInstance.InstanceParameterValuesPersistent"/>
        /// </summary>
        /// <param name="parameter">The modified parameter</param>
        /// <param name="instance">The instance in which the value has changed</param>
        public void OnParameterValueChanged(SimBaseParameter parameter, SimComponentInstance instance)
        {
            var affectedGeometry = OnParameterValueChangedInternal(parameter, instance);

            if (affectedGeometry.Count > 0)
                NotifyGeometryInvalidated(affectedGeometry);

            //Handle Static Ports in SimNetworks with GeometryModel opened
            if (instance.Component.InstanceType == SimInstanceType.InPort ||
                instance.Component.InstanceType == SimInstanceType.OutPort)
            {
                var networkPlacement = instance.Placements.OfType<SimInstancePlacementSimNetwork>().FirstOrDefault();
                if (networkPlacement.NetworkElement is SimNetworkPort port && parameter is SimDoubleParameter dParam)
                {
                    //Check if this model is associated with a SimNetwork
                    var parent = port.ParentNetworkElement;
                    while (parent.ParentNetwork != null)
                    {
                        parent = parent.ParentNetwork;
                    }

                    SimNetwork simNetwork = parent as SimNetwork;

                    var indRepFile = simNetwork.IndexOfGeometricRepFile;
                    var resource = this.ProjectData.AssetManager.GetResource(indRepFile);
                    if (resource != null)
                    {
                        if (this.ProjectData.GeometryModels.TryGetGeometryModel(resource as ResourceFileEntry, out var geom))
                        {
                            if (this.SimNetworkModelConnectors.TryGetValue(geom, out var simNetworkConnector))
                            {
                                simNetworkConnector.OnStaticPortCoordinateChanged(dParam);
                            }
                        }
                    }
                }
            }
        }



        /// <summary>
        /// Call this when some filter of the parameter source changed (filter on the source or on the resource entry) to update the connectors.
        /// </summary>
        /// <param name="source">The parameter source related to the change</param>
        public void OnParameterSourceFilterChanged(SimGeometryParameterSource source)
        {
            foreach (var instance in source.TargetParameter.Component.Instances)
            {
                foreach (var placement in instance.Placements.OfType<SimInstancePlacementGeometry>())
                {
                    var resource = ProjectData.AssetManager.GetResource(placement.FileId) as ResourceFileEntry;

                    if (resource != null)
                    {
                        if (ProjectData.GeometryModels.TryGetGeometryModel(resource, out var gm, false))
                        {
                            if (this.geometryModelConnectors.TryGetValue(gm, out var connector))
                            {
                                connector.OnParameterSourceFilterChanged(source);
                            }
                            // only update while not restoring references
                            else if (!ProjectData.Components.IsRestoringReferences)
                            {
                                instance.InstanceParameterValuesPersistent[source.TargetParameter] = Double.NaN;
                                if (source.TargetParameter is SimDoubleParameter dParam)
                                {
                                    dParam.Value = double.NaN;
                                }
                                if (source.TargetParameter is SimIntegerParameter iParam)
                                {
                                    iParam.Value = default(int);
                                }
                                if (source.TargetParameter is SimBoolParameter bParam)
                                {
                                    bParam.Value = default(bool);
                                }
                                if (source.TargetParameter is SimStringParameter sParam)
                                {
                                    sParam.Value = default(string);
                                }
                                if (source.TargetParameter is SimEnumParameter eParam)
                                {
                                    eParam.Value = default(SimTaxonomyEntryReference);
                                }

                            }
                        }
                        else if (!ProjectData.Components.IsRestoringReferences)
                        {
                            instance.InstanceParameterValuesPersistent[source.TargetParameter] = Double.NaN;
                            if (source.TargetParameter is SimDoubleParameter dParam)
                            {
                                dParam.Value = double.NaN;
                            }
                            if (source.TargetParameter is SimIntegerParameter iParam)
                            {
                                iParam.Value = default(int);
                            }
                            if (source.TargetParameter is SimBoolParameter bParam)
                            {
                                bParam.Value = default(bool);
                            }
                            if (source.TargetParameter is SimStringParameter sParam)
                            {
                                sParam.Value = default(string);
                            }
                            if (source.TargetParameter is SimEnumParameter eParam)
                            {
                                eParam.Value = default(SimTaxonomyEntryReference);
                            }
                        }
                    }
                }
            }
        }

        private List<BaseGeometry> OnParameterValueChangedInternal(SimBaseParameter parameter, SimComponentInstance instance)
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
        public void OnParameterValueChanged(IEnumerable<SimBaseParameter> parameters, SimComponentInstance instance)
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

        #region Parameter Value Source

        internal void OnParameterSourceAdded(SimGeometryParameterSource source)
        {
            foreach (var instance in source.TargetParameter.Component.Instances)
            {
                foreach (var placement in instance.Placements.OfType<SimInstancePlacementGeometry>())
                {
                    var resource = ProjectData.AssetManager.GetResource(placement.FileId) as ResourceFileEntry;

                    if (resource != null)
                    {
                        if (ProjectData.GeometryModels.TryGetGeometryModel(resource, out var gm, false))
                        {
                            if (this.geometryModelConnectors.TryGetValue(gm, out var connector))
                                connector.OnParameterSourceAdded(source, placement);
                        }
                    }
                }
            }
        }
        internal void OnParameterSourceRemoved(SimGeometryParameterSource source)
        {
            foreach (var instance in source.TargetParameter.Component.Instances)
            {
                foreach (var placement in instance.Placements.OfType<SimInstancePlacementGeometry>())
                {
                    var resource = ProjectData.AssetManager.GetResource(placement.FileId) as ResourceFileEntry;

                    if (resource != null)
                    {
                        if (ProjectData.GeometryModels.TryGetGeometryModel(resource, out var gm, false))
                        {
                            if (this.geometryModelConnectors.TryGetValue(gm, out var connector))
                                connector.OnParameterSourceRemoved(source, placement);
                        }
                    }
                }
            }
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

        private static Dictionary<Type, SimInstanceType> geometryToInstanceType = new Dictionary<Type, SimInstanceType>()
        {
            { typeof(Volume), SimInstanceType.Entity3D },
            { typeof(Face), SimInstanceType.AttributesFace },
            { typeof(Edge), SimInstanceType.AttributesEdge },
            { typeof(Vertex), SimInstanceType.AttributesPoint },
        };
        /// <summary>
        /// Creates a new instance and placements that connects the component to the geometry.
        /// Throws an ArgumentException when the geometry type does not match the <see cref="SimComponent.InstanceType"/>
        /// </summary>
        /// <param name="component">The component</param>
        /// <param name="geometry">The geometry</param>
        public (SimComponentInstance instance, bool existed) Associate(SimComponent component, BaseGeometry geometry)
        {
            if (geometry == null)
                throw new ArgumentNullException(nameof(geometry));
            if (component == null)
                throw new ArgumentNullException(nameof(component));

            if (geometry.ModelGeometry == null || geometry.ModelGeometry.Model == null)
                throw new ArgumentException("Geometry does not belong to a GeometryModel");
            if (component.Factory == null)
                throw new ArgumentException("Component does not belong to a Project");

            //Check if the geometry is one of the geometries that can be connected
            if (!geometryToInstanceType.TryGetValue(geometry.GetType(), out var instType))
                throw new ArgumentException("Geometry Type does not match Component.InstanceType");

            //Check if the component already has an instance that connects to this geometry
            var instance = component.Instances.FirstOrDefault(inst => inst.Placements.Any(p => p is SimInstancePlacementGeometry pg &&
                pg.FileId == geometry.ModelGeometry.Model.File.Key && pg.GeometryId == geometry.Id));

            bool exited = instance != null;

            //Create instance
            if (!exited)
            {
                instance = new SimComponentInstance(instType,
                    geometry.ModelGeometry.Model.File.Key, geometry.Id);
                component.Instances.Add(instance);
            }

            return (instance, exited);
        }
        /// <summary>
        /// Creates new instances and placements that connect the component with all geometries.
        /// Throws an ArgumentException when the geometry type does not match the <see cref="SimComponent.InstanceType"/>
        /// </summary>
        /// <param name="component">The component</param>
        /// <param name="geometries">The geometries</param>
        public List<(SimComponentInstance instance, bool existed)> Associate(SimComponent component, IEnumerable<BaseGeometry> geometries)
        {
            if (geometries == null)
                throw new ArgumentNullException(nameof(geometries));
            if (component == null)
                throw new ArgumentNullException(nameof(component));

            List<(SimComponentInstance instance, bool existed)> result = new List<(SimComponentInstance instance, bool existed)>();

            foreach (var geometry in geometries)
            {
                result.Add(Associate(component, geometry));
            }

            return result;
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

            var exists = component.Instances.Any(
                    inst => inst.Placements.Any(p => p is SimInstancePlacementGeometry pg && pg.InstanceType == SimInstanceType.BuiltStructure &&
                            pg.FileId == building.Project.SitePlannerFile.Key &&
                            pg.GeometryId == building.ID));

            if (!exists)
            {
                SimComponentInstance instance = new SimComponentInstance(SimInstanceType.BuiltStructure,
                    building.Project.SitePlannerFile.Key, building.ID);
                component.Instances.Add(instance);
            }
        }

        /// <summary>
        /// Associates an instance of a component with a list of geometry
        /// </summary>
        /// <param name="instance">The instance to associate</param>
        /// <param name="geometries">The geometries which should be associated with the instance</param>
        public void Associate(SimComponentInstance instance, IEnumerable<BaseGeometry> geometries)
        {
            if (geometries == null)
                throw new ArgumentNullException(nameof(geometries));
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));

            foreach (var geometry in geometries)
            {
                Associate(instance, geometry);
            }
        }

        /// <summary>
        /// Associates an instance of a component with a geometry
        /// </summary>
        /// <param name="instance">The instance to associate</param>
        /// <param name="geometry">The geometry which should be associated with the instance</param>
        public void Associate(SimComponentInstance instance, BaseGeometry geometry)
        {
            if (geometry == null)
                throw new ArgumentNullException(nameof(geometry));
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));

            //Check if the geometry is one of the geometries that can be connected
            if (!geometryToInstanceType.TryGetValue(geometry.GetType(), out var instType))
                throw new ArgumentException("Geometry does not support being connected with a component");

            //Check if the instance already has a placement for this geometry
            if (!instance.Placements.Any(x => x is SimInstancePlacementGeometry gp && gp.FileId == geometry.ModelGeometry.Model.File.Key &&
                gp.GeometryId == geometry.Id))
            {
                //Create placement
                SimInstancePlacementGeometry newPlacement = new SimInstancePlacementGeometry(geometry.ModelGeometry.Model.File.Key, geometry.Id,
                    instType);
                instance.Placements.Add(newPlacement);
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
        /// Deletes the association between an instance and a geometry
        /// </summary>
        /// <param name="instance">The instance for which the association should be removed</param>
        /// <param name="geometry">The geometry which should be removed from the instance</param>
        public void Disassociate(SimComponentInstance instance, BaseGeometry geometry)
        {
            if (geometry == null)
                throw new ArgumentNullException(nameof(geometry));
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));

            //Find placement to remove
            for (int i = instance.Placements.Count - 1; i >= 0; --i)
            {
                var placement = instance.Placements[i];
                if (placement is SimInstancePlacementGeometry pgeo &&
                    pgeo.FileId == geometry.ModelGeometry.Model.File.Key && pgeo.GeometryId == geometry.Id)
                {
                    instance.Placements.RemoveAt(i);
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
                foreach (var comp in con.GetPlacements(geometry).Select(x => x.Instance.Component).Distinct())
                    yield return comp;
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

        #endregion

        #region Networks

        /// <summary>
        /// Converts a network into a GeometryModel
        /// </summary>
        /// <param name="network">The network</param>
        /// <param name="targetFile">The location where the geometry model file should be created</param>
        /// <param name="dispatcherTimer">Dispatcher timer for the offset surface generator</param>
        /// <returns>The geometry model</returns>
        public GeometryModel ConvertNetwork(SimFlowNetwork network, FileInfo targetFile, IDispatcherTimerFactory dispatcherTimer)
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
            GeometryModelData geometryData = new GeometryModelData(dispatcherTimer);
            GeometryModel gm = new GeometryModel(targetFile.Name, asset, OperationPermission.DefaultNetworkPermissions, geometryData);

            //Get an owning resource to the model
            //Also calls AddGeometryModel in this class
            //Don't forget to free the reference after adding it to the GeometryViewer
            ProjectData.GeometryModels.AddGeometryModel(gm);

            //Save empty model
            if (!SimGeoIO.Save(gm, gm.File, SimGeoIO.WriteMode.Plaintext))
                return null;

            return gm;
        }


        /// <summary>
        /// Converts a SimNetwork into a GeometryModel
        /// </summary>
        /// <param name="network">The SimNetwork</param>
        /// <param name="targetFile">The location where the geometry model file should be created</param>
        /// <param name="dispatcherTimer">Dispatcher timer for the offset surface generator</param>
        /// <returns>The geometry model</returns>
        public GeometryModel ConvertSimNetwork(SimNetwork network, FileInfo targetFile, IDispatcherTimerFactory dispatcherTimer)
        {
            if (network == null)
                throw new ArgumentNullException(nameof(network));
            if (network.IndexOfGeometricRepFile != -1)
                throw new ArgumentException("SimNetwork already has a geometric representation");

            //Create empty asset
            var asset = ((ExtendedProjectData)ProjectData).Project.AddEmptyResource(targetFile);


            //Add reference to network
            network.IndexOfGeometricRepFile = asset.Key;


            //Load geometry file
            GeometryModelData geometryData = new GeometryModelData(dispatcherTimer);
            GeometryModel gm = new GeometryModel(targetFile.Name, asset, OperationPermission.DefaultSimNetworkPermissions, geometryData);

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
                if (pl.InstanceType.HasFlag(SimInstanceType.AttributesFace))
                {
                    var dinParameter = pl.Instance.Component.Parameters.FirstOrDefault(
                        x => x.NameTaxonomyEntry.HasTaxonomyEntry && x.NameTaxonomyEntry.TaxonomyEntryReference.Target.Key == ReservedParameterKeys.RP_MATERIAL_COMPOSITE_D_IN);
                    var doutParameter = pl.Instance.Component.Parameters.FirstOrDefault(
                        x => x.NameTaxonomyEntry.HasTaxonomyEntry && x.NameTaxonomyEntry.TaxonomyEntryReference.Target.Key == ReservedParameterKeys.RP_MATERIAL_COMPOSITE_D_OUT);

                    //Check if parameters exist
                    if (dinParameter != null)
                        inner = Convert.ToDouble(pl.Instance.InstanceParameterValuesPersistent[dinParameter]);
                    if (doutParameter != null)
                        outer = Convert.ToDouble(pl.Instance.InstanceParameterValuesPersistent[doutParameter]);
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
