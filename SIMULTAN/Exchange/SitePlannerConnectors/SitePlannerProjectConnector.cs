using SIMULTAN.Data.Components;
using SIMULTAN.Data.SitePlanner;
using SIMULTAN.Utils.Collections;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Exchange.SitePlannerConnectors
{
    /// <summary>
    /// Manages the geometry instances in a <see cref="SitePlannerProject"/>
    /// </summary>
    internal class SitePlannerProjectConnector
    {
        private ComponentGeometryExchange Exchange { get; }

        private MultiDictionary<ulong, SitePlannerBuildingConnector> connectors
            = new MultiDictionary<ulong, SitePlannerBuildingConnector>();
        private MultiDictionary<ulong, SimInstancePlacementGeometry> missingBuildings = new MultiDictionary<ulong, SimInstancePlacementGeometry>();

        /// <summary>
        /// The siteplanner project
        /// </summary>
        internal SitePlannerProject Project { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SitePlannerProjectConnector"/>
        /// </summary>
        /// <param name="project">The siteplanner project</param>
        /// <param name="exchange">The parent exchange object</param>
        internal SitePlannerProjectConnector(SitePlannerProject project, ComponentGeometryExchange exchange)
        {
            if (project == null)
                throw new ArgumentNullException(nameof(project));
            if (exchange == null)
                throw new ArgumentNullException(nameof(exchange));

            this.Project = project;
            this.Exchange = exchange;

            this.Project.Buildings.CollectionChanged += this.Buildings_CollectionChanged;

            CreateConnectors(this.Project.SitePlannerFile.Key, exchange.ProjectData.Components);
        }


        private void CreateConnectors(int modelKey, IEnumerable<SimComponent> components)
        {
            foreach (var component in components)
            {
                if (component != null)
                {
                    foreach (var inst in component.Instances)
                    {
                        foreach (var placement in inst.Placements.Where(x => x is SimInstancePlacementGeometry gp && gp.FileId == modelKey))
                        {
                            CreateConnector((SimInstancePlacementGeometry)placement);
                        }
                    }

                    CreateConnectors(modelKey, component.Components.Select(x => x.Component));
                }
            }
        }

        private void CreateConnector(SimInstancePlacementGeometry placement)
        {
            var building = Project.Buildings.FirstOrDefault(x => x.ID == placement.GeometryId);
            if (building != null)
            {
                placement.State = SimInstancePlacementState.Valid;
                this.connectors.Add(building.ID, new SitePlannerBuildingConnector(building, placement));
                this.Exchange.NotifyBuildingAssociationChanged(new SitePlannerBuilding[] { building });
            }
            else
            {
                placement.State = SimInstancePlacementState.InstanceTargetMissing;
                this.missingBuildings.Add(placement.GeometryId, placement);
            }
        }

        /// <summary>
        /// Returns a list of all placements for a specific building
        /// </summary>
        /// <param name="building">The building</param>
        /// <returns>All placements associated with the building</returns>
        internal IEnumerable<SimInstancePlacementGeometry> GetPlacement(SitePlannerBuilding building)
        {
            if (connectors.TryGetValues(building.ID, out var con))
                return con.Select(x => x.Placement);
            return Enumerable.Empty<SimInstancePlacementGeometry>();
        }

        /// <summary>
        /// Called when a new placement gets added that belongs to this siteplanner project
        /// </summary>
        /// <param name="placement">The added placement</param>
        internal void OnPlacementAdded(SimInstancePlacementGeometry placement)
        {
            if (placement.FileId != this.Project.SitePlannerFile.Key)
                throw new ArgumentException("Placement does not belong to this SitePlanner project");

            CreateConnector(placement);
        }
        /// <summary>
        /// Called when a placement gets removed that belongs to this siteplanner project
        /// </summary>
        /// <param name="placement">The removed placement</param>
        internal void OnPlacementRemoved(SimInstancePlacementGeometry placement)
        {
            if (placement.FileId != this.Project.SitePlannerFile.Key)
                throw new ArgumentException("Placement does not belong to this SitePlanner project");
            
            missingBuildings.Remove(placement.GeometryId);

            if (connectors.TryGetValues(placement.GeometryId, out var cons))
            {
                var con = cons.FirstOrDefault(x => x.Placement == placement);
                if (con != null)
                    connectors.Remove(placement.GeometryId, con);
                Exchange.NotifyBuildingAssociationChanged(new SitePlannerBuilding[] { con.Building });
            }
        }
        /// <summary>
        /// Called when the parameter value of a component that is associated with this siteplanner project gets changed
        /// </summary>
        /// <param name="placement">The placement in which the parameter has been changed</param>
        /// <param name="parameter">The modified parameter</param>
        /// <returns>Returns a list of all buildings that are affected by the change</returns>
        internal IEnumerable<SitePlannerBuilding> OnParameterValueChanged(SimInstancePlacementGeometry placement, SimParameter parameter)
        {
            if (placement.FileId != this.Project.SitePlannerFile.Key)
                throw new ArgumentException("Placement does not belong to this SitePlanner project");

            if (parameter.HasReservedTaxonomyEntry(ReservedParameterKeys.RP_PARAM_TO_GEOMETRY))
            {
                if (connectors.TryGetValues(placement.GeometryId, out var cons))
                {
                    yield return cons.First().Building;
                }
            }
        }


        private void Buildings_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    {
                        var affectedBuildings = e.NewItems.OfType<SitePlannerBuilding>().ToList();

                        foreach (var building in affectedBuildings)
                        {
                            if (missingBuildings.TryGetValues(building.ID, out var placements))
                            {
                                missingBuildings.Remove(building.ID);

                                foreach (var pl in placements)
                                {
                                    pl.State = SimInstancePlacementState.Valid;
                                    this.connectors.Add(building.ID, new SitePlannerBuildingConnector(building, pl));
                                }
                            }
                        }

                        if (affectedBuildings.Count > 0)
                            this.Exchange.NotifyBuildingAssociationChanged(affectedBuildings);
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    {
                        var affectedBuildings = e.OldItems.OfType<SitePlannerBuilding>().ToList();

                        foreach (var building in affectedBuildings)
                        {
                            if (connectors.TryGetValues(building.ID, out var cons))
                            {
                                connectors.Remove(building.ID);

                                foreach (var con in cons)
                                {
                                    con.Placement.State = SimInstancePlacementState.InstanceTargetMissing;
                                    missingBuildings.Add(building.ID, con.Placement);
                                }
                            }
                        }

                        if (affectedBuildings.Count > 0)
                            this.Exchange.NotifyBuildingAssociationChanged(affectedBuildings);
                    }
                    break;
                default:
                    throw new NotSupportedException("Operation not supported");
            }
        }
    
        /// <summary>
        /// Frees all resources of the connector and detaches all event handler
        /// </summary>
        public void Dispose()
        {
            this.Project.Buildings.CollectionChanged -= Buildings_CollectionChanged;
        }
    }
}
