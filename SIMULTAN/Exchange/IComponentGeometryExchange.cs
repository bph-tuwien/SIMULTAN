using SIMULTAN.Data.Components;
using SIMULTAN.Data.Geometry;
using SIMULTAN.Data.Users;
using SIMULTAN.Exchange.Connectors;
using SIMULTAN.Serializer.SimGeo;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Exchange
{
    /// <summary>
    /// Indicates the reason for the removal of a geometry model.
    /// </summary>
    public enum GeometryModelRemovalMode
    {
        /// <summary>
        /// The geometry model is being actually deleted with permanent consequences.
        /// </summary>
        DELETE,
        /// <summary>
        /// The geometry model is being closed. There should be no consequences for the connected components.
        /// </summary>
        CLOSE
    }

    /// <summary>
    /// Event handler delegate for the AssociationChanged event.
    /// </summary>
    /// <param name="sender">the sending object</param>
    /// <param name="affected_geometry">the geometry whose association (i.e., source, new connector, deleted connector) changed</param>
    public delegate void AssociationChangedEventHandler(object sender, IEnumerable<BaseGeometry> affected_geometry);

    /// <summary>
    /// Interface for managing the information exchange btw. components and geometry.
    /// </summary>
    public interface IComponentGeometryExchange : ILoaderGeometryExchange
    {
        #region Properties

        /// <summary>
        /// Manages the communication between networks and geometry.
        /// </summary>
        INetworkGeometryExchange NetworkCommunicator { get; }

        /// <summary>
        /// Enables async updates to the components. Helps to solve performance problems when a lot of components are assigned to geometry
        /// or when a lot of geometry changes at the same time
        /// </summary>
        bool EnableAsyncUpdates { get; set; }

        #endregion

        #region Components

        /// <summary>
        /// Returns a list of associated components for a geometry
        /// </summary>
        /// <param name="geometry">The geometry</param>
        /// <returns>A list of components attached to the geometry</returns>
        IEnumerable<SimComponent> GetComponents(BaseGeometry geometry);

        /// <summary>
        /// Associates the given component with the geometry depending on their respective type.
        /// Throws NullReferenceException, if any of the input arguments is Null, and ArgumentException, if the geometry type is unknown.
        /// </summary>
        /// <param name="_comp">the component to associate</param>
        /// <param name="_geometry">the geometry to associate</param>
        /// <returns>the created connector and feedback, the connector can be Null</returns>
        void Associate(SimComponent _comp, BaseGeometry _geometry);

        /// <summary>
        /// Associates the given component with the multiple geometry of the same type (i.e. Face).
        /// Throws NullReferenceException, if any of the input arguments is Null, and ArgumentException, if the geometry type is unknown.
        /// </summary>
        /// <param name="_comp">the component to associate</param>
        /// <param name="_geometry">the geometric objects to associate</param>
        /// <returns>the created connectors and feedback in a dictionary, the dictionary can be empty but not Null</returns>
        void Associate(SimComponent _comp, IEnumerable<BaseGeometry> _geometry);

        /// <summary>
        /// Removes the association between the given component and geometry.
        /// Throws NullReferenceException, if any of the input arguments is Null, and ArgumentException, if the geometry type is unknown.
        /// </summary>
        /// <param name="_comp">the component to disassociate</param>
        /// <param name="_geometry">the geometry to disassociate from the component</param>
        void DisAssociate(SimComponent _comp, BaseGeometry _geometry);

        /// <summary>
        /// Invoked when the association relationship in one or more connectors has changed:
        /// either the source component or the target geometry.
        /// </summary>
        event AssociationChangedEventHandler AssociationChanged;

        bool RemoveGeometryModel(FileInfo _file, GeometryModelRemovalMode _reason);

        /// <summary>
        /// Returns the geometry
        /// </summary>
        /// <param name="_index_of_geometry_model">The FileId of the geometric relationship</param>
        /// <param name="_geom_id">The Id of the actually geometry</param>
        /// <returns></returns>
        BaseGeometry GetGeometryFromId(int _index_of_geometry_model, ulong _geom_id);

        #endregion
    }
}
