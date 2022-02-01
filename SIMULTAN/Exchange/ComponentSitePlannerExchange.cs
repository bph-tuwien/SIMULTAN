using SIMULTAN.Data.Components;
using SIMULTAN.Data.SitePlanner;
using SIMULTAN.Data.Users;
using SIMULTAN.Exchange.Connectors;
using SIMULTAN.Projects;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace SIMULTAN.Exchange
{
    /// <summary>
    /// Event handler delegate for the BuildingComponentParameterChanged event.
    /// </summary>
    /// <param name="sender">the sending object</param>
    /// <param name="affectedBuilding">the buildings which is affected by the change</param>
    public delegate void BuildingComponentParameterChangedEventHandler(object sender, SitePlannerBuilding affectedBuilding);

    /// <summary>
    /// Holds the main logic for association and dis-association between components and SitePlanner objects.
    /// Manages the connection to the component factory and the SitePlanner objects.
    /// For that purpose it contains a <see cref="SimComponentCollection"/> object holding all <see cref="SimComponent"/> instances. 
    /// </summary>
    public class ComponentSitePlannerExchange : IComponentSitePlannerExchange
    {
        /// <inheritdoc />
        public SitePlannerManager Manager { get; }


        /// <inheritdoc />
        public event BuildingAssociationChangedEventHandler BuildingAssociationChanged;

        /// <inheritdoc />
        public event BuildingComponentParameterChangedEventHandler BuildingComponentParamaterChanged;

        public ProjectData ProjectData { get; }


        private Dictionary<ulong, ConnectorParameterizingToBuilding> buildingToComponent;


        /// <summary>
        /// Initializes a new instance of this class
        /// </summary>
        public ComponentSitePlannerExchange(ProjectData projectData)
        {
            this.ProjectData = projectData;

            Manager = new SitePlannerManager();
            Manager.SitePlannerProjectOpened += Manager_SitePlannerProjectOpened;
            Manager.SitePlannerProjectClosed += Manager_SitePlannerProjectClosed;

            this.buildingToComponent = new Dictionary<ulong, ConnectorParameterizingToBuilding>();
        }


        /// <inheritdoc />
        public SimComponent GetComponent(SitePlannerBuilding building)
        {
            if (buildingToComponent.ContainsKey(building.ID))
                return buildingToComponent[building.ID].ParameterizingSource;

            return null;
        }

        /// <inheritdoc />
        public void Associate(SimComponent comp, SitePlannerBuilding building)
        {
            if (comp == null || building == null)
                throw new NullReferenceException();

            if (comp.InstanceType != SimInstanceType.BuiltStructure)
                throw new ArgumentException("The component is of the wrong type!");

            if (buildingToComponent.ContainsKey(building.ID))
                RemoveConnector(comp, building.ID);

            var connector = AddConnector(comp, building);

            this.BuildingAssociationChanged?.Invoke(this, new List<SitePlannerBuilding> { building });
        }

        /// <inheritdoc />
        public void DisAssociate(SimComponent comp, SitePlannerBuilding building)
        {
            if (comp == null || building == null)
                throw new NullReferenceException();

            if (comp.InstanceType != SimInstanceType.BuiltStructure)
                throw new ArgumentException("The component is of the wrong type!");

            if (!buildingToComponent.ContainsKey(building.ID))
                throw new ArgumentException("The component is not associated with the given building!");

            RemoveConnector(comp, building.ID);

            this.BuildingAssociationChanged?.Invoke(this, new List<SitePlannerBuilding> { building });
        }

        /// <summary>
        /// Invoked the BuildingComponentParameterChanged event after a parameter of an associated component changed
        /// </summary>
        /// <param name="affectedBuilding">Affected building</param>
        public void AssociatedComponentParameterChanged(SitePlannerBuilding affectedBuilding)
        {
            BuildingComponentParamaterChanged?.Invoke(this, affectedBuilding);
        }

        private Connector AddConnector(SimComponent comp, SitePlannerBuilding building)
        {
            if (buildingToComponent.ContainsKey(building.ID))
                return buildingToComponent[building.ID];

            var connector = new ConnectorParameterizingToBuilding(this, comp, building.Project.SitePlannerFile.Key, building);
            buildingToComponent[building.ID] = connector;

            return connector;
        }

        private void RemoveConnector(SimComponent comp, ulong buildingID)
        {
            if (!buildingToComponent.ContainsKey(buildingID))
                return;

            var connector = buildingToComponent[buildingID];
            buildingToComponent.Remove(buildingID);
            connector.SynchronizeSourceWTarget(null);
            connector.BeforeDeletion();
        }

        private void Manager_SitePlannerProjectOpened(object sender, SitePlannerProject project)
        {
            // Load connectors 
            IEnumerable<SimComponent> associatedComponents = this.ProjectData.Components.GetAllAssociatedWithGeometry();
            foreach (var building in project.Buildings)
            {
                foreach (var comp in associatedComponents)
                {
                    IEnumerable<ulong> associatedIDs = comp.GetAllAssociatedGeometryIds(project.SitePlannerFile.Key);

                    if (associatedIDs.Contains(building.ID))
                    {
                        AddConnector(comp, building);
                    }
                }
            }

            project.Buildings.CollectionChanged += Buildings_CollectionChanged;
        }

        private void Manager_SitePlannerProjectClosed(object sender, SitePlannerProject project)
        {
            project.Buildings.CollectionChanged -= Buildings_CollectionChanged;
        }

        private void Buildings_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove)
            {
                var building = e.OldItems[0] as SitePlannerBuilding;
                var component = GetComponent(building);
                if (component != null)
                    RemoveConnector(component, building.ID);
            }
        }
    }
}
