using SIMULTAN.Data.Components;
using SIMULTAN.Data.SitePlanner;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Exchange.SitePlannerConnectors
{
    /// <summary>
    /// Connects a <see cref="SitePlannerBuilding"/> to a component instance
    /// </summary>
    internal class SitePlannerBuildingConnector
    {
        /// <summary>
        /// The building
        /// </summary>
        internal SitePlannerBuilding Building { get; }
        /// <summary>
        /// The placement connected to the building
        /// </summary>
        internal SimInstancePlacementGeometry Placement { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SitePlannerBuildingConnector"/>
        /// </summary>
        /// <param name="building">The building</param>
        /// <param name="placement">The placement associated with the building</param>
        internal SitePlannerBuildingConnector(SitePlannerBuilding building, SimInstancePlacementGeometry placement)
        {
            if (building == null)
                throw new ArgumentNullException(nameof(building));
            if (placement == null)
                throw new ArgumentNullException(nameof(placement));

            this.Building = building;
            this.Placement = placement;

            //Make sure that parameter exists
            ExchangeHelpers.CreateParameterIfNotExists(placement.Instance.Component,
                ReservedParameterKeys.RP_PARAM_TO_GEOMETRY, ReservedParameters.RP_PARAM_TO_GEOMETRY, SimParameterInstancePropagation.PropagateIfInstance, 1.0);
        }
    }
}
