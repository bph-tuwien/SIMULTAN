using SIMULTAN.Data.Components;
using SIMULTAN.Data.SitePlanner;
using SIMULTAN.Data.Users;
using SIMULTAN.Exchange.Connectors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Exchange
{
    /// <summary>
    /// Event handler delegate for the BuildingAssociationChanged event.
    /// </summary>
    /// <param name="sender">the sending object</param>
    /// <param name="affected_buildings">the buildings whose association (i.e., source, new connector, deleted connector) changed</param>
    public delegate void BuildingAssociationChangedEventHandler(object sender, IEnumerable<SitePlannerBuilding> affected_buildings);

    /// <summary>
    /// Interface for information exchange between components and buildings.
    /// </summary>
    public interface IComponentSitePlannerExchange
    {
        /// <summary>
        /// The manager of all buildings.
        /// </summary>
        SitePlannerManager Manager { get; }

        /// <summary>
		/// Returns the associated component for a building.
		/// </summary>
		/// <param name="building">The building</param>
		/// <returns>Component attached to the building</returns>
		SimComponent GetComponent(SitePlannerBuilding building);

        /// <summary>
        /// Associates the given component with the building depending.
        /// Throws NullReferenceException, if any of the input arguments is Null, and ArgumentException, if the component type is not appropriate.
        /// </summary>
        /// <param name="comp">the component to associate</param>
        /// <param name="building">the building to associate</param>
        /// <returns>the created connector and feedback, the connector can be Null</returns>
        void Associate(SimComponent comp, SitePlannerBuilding building);

        /// <summary>
        /// Removes the association between the given component and building.
        /// Throws NullReferenceException, if any of the input arguments is Null, and ArgumentException, if the component type is not appropriate.
        /// </summary>
        /// <param name="comp">the component to disassociate</param>
        /// <param name="building">the building to disassociate from the component</param>
        void DisAssociate(SimComponent comp, SitePlannerBuilding building);

        /// <summary>
		/// Invoked when the association relationship in one or more connectors has changed:
		/// either the source component or the target building.
		/// </summary>
		event BuildingAssociationChangedEventHandler BuildingAssociationChanged;

        /// <summary>
        /// Invoked when the parameter of an associated component is changed
        /// </summary>
        event BuildingComponentParameterChangedEventHandler BuildingComponentParamaterChanged;
    }
}
