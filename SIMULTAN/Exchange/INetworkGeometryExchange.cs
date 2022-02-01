using SIMULTAN.Data.FlowNetworks;
using SIMULTAN.Data.Geometry;
using SIMULTAN.Exchange.Connectors;
using SIMULTAN.Projects;
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
    /// Event handler delegate for the ElementDeleted event.
    /// </summary>
    /// <param name="sender">the sending object</param>
    /// <param name="addedElement">the added network element</param>
    public delegate void ElementAddedEventHandler(object sender, SimFlowNetworkElement addedElement);

    /// <summary>
    /// Manages the communication between networks and geometry.
    /// </summary>
    public interface INetworkGeometryExchange
    {
        /// <summary>
        /// Retrieves the file information of the geometric representation
        /// of the given network.
        /// </summary>
        /// <param name="network">the network, whose representation we are looking for</param>
        /// <returns>the geometry representation file or Null</returns>
        FileInfo GetNetworkRepresentationFile(SimFlowNetwork network);

        /// <summary>
        /// Adds the geometry model to the geometry exchange,
        /// the geometry file - to the resources and associates it with the given 
        /// flow network as its geometric representation.
        /// </summary>
        /// <param name="model">the geometry model that represents the network</param>
        /// <param name="network">the network</param>
		/// <param name="projectData">The current project data</param>
        void AddGeometricRepresentationToNetwork(GeometryModel model, SimFlowNetwork network, ProjectData projectData);

        /// <summary>
        /// Removes the geometry model from the geometry exchange.
        /// </summary>
        /// <param name="model">the geometry model that represents the network</param>
        /// <param name="network">the network</param>
        /// <param name="projectData">The current project data</param>
        void RemoveGeometricRepresentationFromNetwork(GeometryModel model, SimFlowNetwork network, ProjectData projectData);

        /// <summary>
        /// Retrieves the index of the given file from the resource manager.
        /// </summary>
        /// <param name="representationFile"></param>
        /// <returns>the index or -1</returns>
        int GetNetworkRepresentationFileIndex(FileInfo representationFile);

        /// <summary>
        /// Associates the given network element with the geometry depending on their respective type.
        /// Throws NullReferenceException, if any of the input arguments is Null, and ArgumentException, if the geometry type is unknown.
        /// </summary>
        /// <param name="networkElement">the network element to associate</param>
        /// <param name="geometry">the geometry to associate</param>
        /// <returns>the created connector and feedback, the connector can be Null</returns>
        void Associate(SimFlowNetworkElement networkElement, BaseGeometry geometry);


        /// <summary>
        /// Resets the internal state.
        /// </summary>
        void Reset();
    }
}
