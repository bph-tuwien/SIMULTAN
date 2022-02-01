using SIMULTAN.Data.FlowNetworks;
using SIMULTAN.Data.Geometry;
using SIMULTAN.Exchange.Connectors;
using SIMULTAN.Projects;
using SIMULTAN.Serializer.DXF;
using SIMULTAN.Serializer.SimGeo;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace SIMULTAN.Exchange
{
    /// <summary>
    /// Manages the communication between networks and geometry in the context
    /// of a <see cref="ComponentGeometryExchange"/> instance.
    /// </summary>
    public class NetworkGeometryExchange : INetworkGeometryExchange
    {
        #region CONSTANTS

        /// <summary>
        /// The color of an empty network element representation.
        /// </summary>
        public static readonly Color COL_EMPTY = (Color)ColorConverter.ConvertFromString("#FF404040");
        /// <summary>
        /// The color of an unassigned network element representation.
        /// </summary>
        public static readonly Color COL_UNASSIGNED = (Color)ColorConverter.ConvertFromString("#FFA0A0A0");
        /// <summary>
        /// The default color of a network element representation.
        /// </summary>
        public static readonly Color COL_NEUTRAL = (Color)ColorConverter.ConvertFromString("#FFffffff");

        #endregion

        #region PROPERTIES

        internal ComponentGeometryExchange MainGeometryExchange { get; }

        #endregion

        #region FIELDS

        private Dictionary<int, ConnectorToGeometryModel> network_connectors;

        #endregion

        #region .CTOR

        /// <summary>
        /// Initializes an instance of NetworkGeometryExchange.
        /// </summary>
        /// <param name="_manager_of_all">the context</param>
        public NetworkGeometryExchange(ComponentGeometryExchange _manager_of_all)
        {
            this.MainGeometryExchange = _manager_of_all;
            this.network_connectors = new Dictionary<int, ConnectorToGeometryModel>();
        }

        #endregion

        #region METHODS: Info

        /// <inheritdoc/>
        public FileInfo GetNetworkRepresentationFile(SimFlowNetwork network)
        {
            if (network == null)
                return null;
            string full_path = this.MainGeometryExchange.ProjectData.AssetManager.GetPath(network.IndexOfGeometricRepFile);
            if (string.IsNullOrEmpty(full_path))
                return null;
            if (!File.Exists(full_path))
                return null;

            return new FileInfo(full_path);
        }

        /// <inheritdoc/>
        public int GetNetworkRepresentationFileIndex(FileInfo representationFile)
        {
            return this.MainGeometryExchange.GetResourceFileIndex(representationFile);
        }

        #endregion

        #region ASSOCIATION: networks to representation

        /// <inheritdoc/>
        public void AddGeometricRepresentationToNetwork(GeometryModel model, SimFlowNetwork network, ProjectData projectData)
        {
            if (model == null || network == null) return;

            string path = model.File.FullName;
            // 1. add the file to the resource manager
            int index = this.MainGeometryExchange.ProjectData.AssetManager.AddResourceEntry(model.File);

            // 2. add the model to the main look-up tables (includes check for duplicates) - IS IT NECESSARY?
            // this.MainGeometryExchange.AddGeometryModel(model);

            // 3. perform the actual association
            ConnectorToGeometryModel nw_connector = new ConnectorToGeometryModel(this, network, model.Geometry, index, projectData);
            this.network_connectors.Add(index, nw_connector);

            // 4. perform the element-wise association LATER!
        }

        /// <inheritdoc/>
        public void RemoveGeometricRepresentationFromNetwork(GeometryModel model, SimFlowNetwork network, ProjectData projectData)
        {
            int index = this.MainGeometryExchange.ProjectData.AssetManager.GetResourceKey(model.File);

            if (network_connectors.TryGetValue(index, out var connector))
            {
                connector.OnBeingDeleted();
                network_connectors.Remove(index);
            }
        }

        /// <inheritdoc/>
        public void Associate(SimFlowNetworkElement networkElement, BaseGeometry geometry)
        {
            if (geometry == null || networkElement == null)
                throw new ArgumentNullException();

            Type geom_type = geometry.GetType();
            if (geom_type != typeof(Volume) && geom_type != typeof(Face) &&
                geom_type != typeof(Edge) && geom_type != typeof(EdgeLoop) &&
                geom_type != typeof(Vertex) && geom_type != typeof(Polyline))
                throw new ArgumentException("Unknown geometry type!");

            // find the relevant network connector - throws exception
            int index = this.MainGeometryExchange.GetModelIndex(geometry);
            ConnectorToGeometryModel representation = (this.network_connectors.ContainsKey(index)) ? this.network_connectors[index] : null;
            if (representation == null)
                throw new ArgumentException("The model containing the geometry is unknown.", nameof(geometry));

            representation.RepresentInstanceBy(networkElement, geometry);
        }

        #endregion

        #region METHODS: handling changes in the geometry models

        /// <summary>
        /// Checks if there is a flow network associated with the file of the given geometry model
        /// in the resource manager. There can only be one such association.
        /// </summary>
        /// <param name="_model">the geometry model</param>
        /// <returns>the represented network or Null</returns>
        internal SimFlowNetwork GetPossibleNetworkRepresentation(GeometryModel _model)
        {
            if (_model == null)
                return null;

            int model_index = this.MainGeometryExchange.ProjectData.AssetManager.GetKey(_model.File.Name);
            IEnumerable<SimFlowNetwork> all_associated_nws = GetAllNWAssociatedWithGeometry(this.MainGeometryExchange.ProjectData.NetworkManager);
            SimFlowNetwork represented_nw = all_associated_nws.FirstOrDefault(x => x.IndexOfGeometricRepFile == model_index);

            return represented_nw;
        }

        /// <summary>
        /// Re-attaches an already existing representation to the represented
        /// network.
        /// </summary>
        /// <param name="_model">the geometry representation</param>
        /// <param name="_network">the network</param>
		/// <param name="projectData">The model store in which the model is managed</param>
        internal void ReAttachGeometryModel(GeometryModel _model, SimFlowNetwork _network, ProjectData projectData)
        {
            if (_model != null)
            {
                // (re-)attach....
                int model_index = projectData.AssetManager.GetKey(_model.File.Name);
                this.MainGeometryExchange.GeometryManager.AttachGeometryModelOnlyToLookup(_model);

                //if (!this.network_connectors.ContainsKey(model_index))
                //    NetworkConverter.ReLinkAndUpdate(_network, _model, this.MainGeometryExchange, modelStore);
                //else
                //    NetworkConverter.Update(_model.Geometry, _network, this, modelStore);

                if (this.network_connectors.TryGetValue(model_index, out var conn))
                {
                    conn.OnBeingDeleted();
                    this.network_connectors.Remove(model_index);
                }

                NetworkConverter.ReLinkAndUpdate(_network, _model, this.MainGeometryExchange, projectData);
            }
        }

        #endregion

        #region METHODS: Handling changes in the network record

        internal void OnNetworkDeleted(IEnumerable<SimFlowNetwork> deletedNetworks)
        {
            if (deletedNetworks == null) return;
            foreach (SimFlowNetwork nw in deletedNetworks)
            {
                int model_index = nw.IndexOfGeometricRepFile;
                if (this.network_connectors.ContainsKey(model_index))
                {
                    GeometryModelData representation = this.network_connectors[model_index].Target;
                    if (representation != null)
                    {
                        //Unlink from all models that use this network
                        foreach (var model in this.MainGeometryExchange.ProjectData.GeometryModels)
                        {
                            var representationReference = model.LinkedModels.FirstOrDefault(x => x.Geometry == representation);
                            if (representationReference != null)
                                model.LinkedModels.Remove(representationReference);
                        }

                        // 2. unload/close model?
                        this.MainGeometryExchange.RemoveGeometryModel(representation.Model.File, GeometryModelRemovalMode.DELETE);
                        this.network_connectors.Remove(model_index);

                        if (this.MainGeometryExchange.ProjectData.GeometryModels.RemoveGeometryModel(representation.Model))
                        {
                            // 3. delete file from resource manager
                            this.MainGeometryExchange.ProjectData.AssetManager.DeleteResourceFileEntry(representation.Model.File.FullName, representation.Model.File.Name);
                            // 4. delete file from dir
                            if (File.Exists(representation.Model.File.FullName))
                                File.Delete(representation.Model.File.FullName);
                        }

                        // there is no turning back!
                        representation = null;
                    }
                }
            }
        }

        #endregion

        #region RESET

        /// <inheritdoc/>
        public void Reset()
        {
            // MainGeometryExchange remains the same
            this.network_connectors.Clear();
        }

        #endregion

        #region Utils

        /// <summary>
        /// Examines the flow network record non-recursively (for the moment) for any networks associated with
        /// a valid geometry file and returns a flat list of those who are.
        /// </summary>
        /// <param name="_factory">the calling component factory</param>
        /// <returns>a collection of all found flow networks on record, associated with a geometry file</returns>
        private static IEnumerable<SimFlowNetwork> GetAllNWAssociatedWithGeometry(SimNetworkFactory _factory)
        {
            List<SimFlowNetwork> all_nws = new List<SimFlowNetwork>();
            foreach (SimFlowNetwork nw in _factory.NetworkRecord)
            {
                int geom_file_index = nw.IndexOfGeometricRepFile;
                if (geom_file_index >= 0)
                {
                    string path_to_file = _factory.ProjectData.AssetManager.GetPath(geom_file_index);
                    if (!string.IsNullOrEmpty(path_to_file))
                    {
                        FileInfo file = new FileInfo(path_to_file);
                        if (string.Equals(file.Extension, ParamStructFileExtensions.FILE_EXT_GEOMETRY_INTERNAL, StringComparison.InvariantCultureIgnoreCase))
                        {
                            // valid file -->
                            all_nws.Add(nw);
                        }

                    }
                }
            }
            return all_nws;
        }

        #endregion
    }
}
