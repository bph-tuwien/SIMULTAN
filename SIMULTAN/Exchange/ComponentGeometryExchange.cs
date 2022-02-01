using SIMULTAN.Data.Assets;
using SIMULTAN.Data.Components;
using SIMULTAN.Data.FlowNetworks;
using SIMULTAN.Data.Geometry;
using SIMULTAN.Data.Users;
using SIMULTAN.Exchange.ConnectorInteraction;
using SIMULTAN.Exchange.Connectors;
using SIMULTAN.Projects;
using SIMULTAN.Serializer.SimGeo;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;

namespace SIMULTAN.Exchange
{
    /// <summary>
    /// Holds the main logic for association and dis-association between components and geometry.
    /// Manages the connection to the component factory and the geometry models.
    /// For that purpose it contains a <see cref="SimComponentCollection"/> object holding all <see cref="SimComponent"/> instances. 
    /// The currently loaded <see cref="GeometryModelData"/> (including the file path) has to be set by the GeometryViewer.GeometryViewerInstance.
    /// </summary>
    public class ComponentGeometryExchange : IComponentGeometryExchange
    {
        #region PROPERTIES

        /// <summary>
        /// Saves the resource indices for previously opened files, in order to skip some operations on reloading.
        /// </summary>
        private HashSet<int> previously_opened_files = new HashSet<int>();

        /// <summary>
        /// The project data to which this exchange object belongs
        /// </summary>
        public ProjectData ProjectData  { get; }

        /// <summary>
        /// Manages the communication between networks and geometry.
        /// </summary>
        public INetworkGeometryExchange NetworkCommunicator { get; }
        /// <summary>
        /// Manages the geometry connected to components.
        /// </summary>
        internal ConnectedGeometryManager GeometryManager { get; }
        /// <summary>
        /// Manages the connectors between components / networks and geometry.
        /// </summary>
        internal ConnectorManager ConnectorManager { get; }

        /// <inheritdoc />
        public bool EnableAsyncUpdates { get; set; } = true;

        #endregion

        #region EVENTS: for reporting changes to geometry

        /// <inheritdoc/>
        public event AssociationChangedEventHandler AssociationChanged;
        /// <summary>
        /// Emits the AssociationChanged event.
        /// </summary>
        /// <param name="affected_geometry">the geometry with changed association</param>
        public void OnAssociationChanged(IEnumerable<BaseGeometry> affected_geometry)
        {
            this.AssociationChanged?.Invoke(this, affected_geometry);
        }

        /// <inheritdoc/>
        public event GeometryInvalidatedEventHandler GeometryInvalidated;

        /// <summary>
        /// Emits the GeometryInvalidated event.
        /// </summary>
        /// <param name="affected_geometry">the geometry affected by the change</param>
        public void OnGeometryInvalidated(IEnumerable<BaseGeometry> affected_geometry)
        {
            this.GeometryInvalidated?.Invoke(this, affected_geometry);
        }

        #endregion


        #region .CTOR

        /// <summary>
        /// Initializes the lookup tables for geometry models and the geometry objects they contain.
        /// Attaches event handlers to each model and to each CONNECTED geometric object.
        /// </summary>
        public ComponentGeometryExchange(ProjectData projectData)
        {
            this.ProjectData = projectData;
            this.ProjectData.NetworkManager.NetworkAdded += ComponentManager_NetworkAdded;
            this.ProjectData.NetworkManager.NetworkDeleted += ComponentManager_NetworkDeleted;
            this.ProjectData.GeometryModels.CollectionChanged += this.GeometryModels_CollectionChanged;

            this.NetworkCommunicator = new NetworkGeometryExchange(this);
            this.ConnectorManager = new ConnectorManager(this);
            this.GeometryManager = new ConnectedGeometryManager(this);
        }

        #endregion


        #region MAIN METHODS (GV -> CB): Association / Dis-association DISPATCHER

        /// <inheritdoc/>
        public void Associate(SimComponent _comp, BaseGeometry _geometry)
        {
            // throws exceptions, if not valid
            CheckValidityOfAssociationArguments(_comp, _geometry);

            if (_geometry is Volume && _comp.InstanceType == SimInstanceType.Entity3D)
            {
                this.AssociateComponentWithComplex(_comp, _geometry as Volume);
            }
            else if (_geometry is Face && _comp.InstanceType == SimInstanceType.Attributes2D)
            {
                this.PrescribeFromComponentToFace(_comp, _geometry as Face);
            }
        }

        /// <inheritdoc/>
        public void Associate(SimComponent _comp, IEnumerable<BaseGeometry> _geometry)
        {
            // check validity of input
            CheckValidityOfAssociationArguments(_comp, _geometry);

            // associating multiple geometry with the same component is currently possible only for faces
            if (_geometry.First() is Face && _comp.InstanceType == SimInstanceType.Attributes2D)
            {
                var (connector, feedback) = this.PrescribeFromComponentToFaces(_comp, _geometry.Select(x => x as Face).ToList());
            }
        }

        /// <inheritdoc/>
        public void DisAssociate(SimComponent _comp, BaseGeometry _geometry)
        {
            // throws exceptions, if not valid
            ComponentGeometryExchange.CheckValidityOfAssociationArguments(_comp, _geometry);

            if (_geometry is Volume && _comp.InstanceType == SimInstanceType.Entity3D)
            {
                this.ConnectorManager.DisassociateComponentFromComplex(_comp, _geometry as Volume);
            }
            else if (_geometry is Face && _comp.InstanceType == SimInstanceType.Attributes2D)
            {
                this.RemovePrescriptiveAssociationToFace(_comp, _geometry as Face);
            }
        }


        /// <inheritdoc />
        public BaseGeometry GetGeometryFromId(int _index_of_geometry_model, ulong _geom_id)
        {
            return this.GeometryManager.GetGeometryFromId(_index_of_geometry_model, _geom_id);
        }

        /// <summary>
        /// Checks if the given component and geometry are valid.
        /// Throws a NullReferenceException in case any of the two is Null and an
        /// ArgumentException, if the geometry type cannot be recognized.
        /// </summary>
        /// <param name="_comp">the component to be associated</param>
        /// <param name="_geometry">the geometry</param>
        private static void CheckValidityOfAssociationArguments(SimComponent _comp, BaseGeometry _geometry)
        {
            if (_geometry == null || _comp == null)
                throw new NullReferenceException();

            Type geom_type = _geometry.GetType();
            if (geom_type != typeof(Volume) && geom_type != typeof(Face) &&
                geom_type != typeof(Edge) && geom_type != typeof(EdgeLoop) &&
                geom_type != typeof(Vertex) && geom_type != typeof(Polyline))
                throw new ArgumentException("Unkown geometry type!");
        }

        /// <summary>
        /// Checks if the given component and geometry are valid. 
        /// Throws a NullReferenceException in case any of the two is Null and an
        /// ArgumentException, if the geometry type cannot be recognized.
        /// </summary>
        /// <param name="_comp">the component to be associated</param>
        /// <param name="_geometry">multiple geometric instances</param>
        private static void CheckValidityOfAssociationArguments(SimComponent _comp, IEnumerable<BaseGeometry> _geometry)
        {
            if (_geometry == null || _comp == null)
                throw new NullReferenceException();

            if (_geometry.Count() == 0)
                throw new ArgumentException("The geometry collection is empty!");

            var types = _geometry.Select(x => x.GetType()).ToList();
            var distict_types = types.Distinct().ToList();
            if (distict_types.Count > 1)
                throw new ArgumentException("The geometry is of different types!");

            Type geom_type = distict_types[0];
            if (geom_type != typeof(Volume) && geom_type != typeof(Face) &&
                geom_type != typeof(Edge) && geom_type != typeof(EdgeLoop) &&
                geom_type != typeof(Vertex) && geom_type != typeof(Polyline))
                throw new ArgumentException("Unkown geometry type!");
        }

        #endregion

        #region ASSOCIATION METHODS: the component as a DESCRIPTOR

        /// <summary>
        /// Associates a component describing an architectural space with 
        /// the volume and all its sub-elements. All necessary sub-components are
        /// generated automatically. Triggers the AssociationChanged event.
        /// </summary>
        /// <param name="_comp">the space-describing component</param>
        /// <param name="_vol">the corresponding 3d volume object</param>
        /// <returns>The created connector and feedback (e.g. error message)</returns>
        internal (ConnectorToComplex connector, ConnectorConnectionState feedback) AssociateComponentWithComplex(SimComponent _comp, Volume _vol)
        {
            ConnectorConnectionState fb = ConnectorConnectionState.OK;

            // 1. check if a connector with this component as a source exists

            var resourceIdx = this.GetModelIndex(_vol);

            if (this.ConnectorManager.HasConnectorsToComponent(_comp, resourceIdx))
            {
                fb |= ConnectorConnectionState.SOURCE_ALREADY_CONNECTED;
                ConnectorBase conn = this.ConnectorManager.RetrieveConnectorToComponent(_comp, resourceIdx);
                if (conn is ConnectorToComplex)
                    return (conn as ConnectorToComplex, fb);
                else
                    return (null, fb);
            }

            // 2. check if component and volume match in structure
            fb |= ConnectionStateEvaluator.Evaluate(null, null, _comp, _vol);
            if (fb != ConnectorConnectionState.OK)
                return (null, fb);

            //Create instance


            // 3. CREATE the connector
            ConnectorToComplex connector = new ConnectorToComplex(this, _comp.Parent as SimComponent, _comp, this.GetModelIndex(_vol), _vol);
            connector.SynchronizeSourceWTarget(_vol);
            this.ConnectorManager.AddConnector(connector, _vol);

            // 4. create the component hierarchy parallel to the geometry hierarchy
            // NOTE: creates a component for the 3d volume and the 2d faces, regardless if such representations already exist
            ComponentGeometryContainer hierarchy_root = ConnectorAlgorithms.CreateParallelHierarchy(this.ProjectData.Components, _comp, _vol, 3, 2);

            // 5. create the corresponding connectors
            this.TranslateToConnectors(hierarchy_root, connector);

            // 6. update the references btw components (after all connectors have been created!!!)
            // this.AddReferencesAfterAdding(connector, _vol);
            this.ModifyReferencesBasedOnConnectivity(connector, _vol, true);

            // DONE
            this.OnAssociationChanged(ConnectorAlgorithms.ExtractFlatGeometryFromParallelHierarchy(hierarchy_root));
            return (connector, fb);
        }

        /// <summary>
        /// Recursively translates component - geometry pairs into connectors.
        /// </summary>
        /// <param name="_node_of_parallel_structure">node, whose children need to be translated</param>
        /// <param name="_node_realization">connector that contains the component of the node</param>
        private void TranslateToConnectors(ComponentGeometryContainer _node_of_parallel_structure, ConnectorToBaseGeometry _node_realization)
        {
            foreach (ComponentGeometryContainer n in _node_of_parallel_structure.Childen)
            {
                // translate the child node
                ConnectorToBaseGeometry n_conn = null;
                if (n.Geometry is Volume)
                    n_conn = this.AssociateComponentWithVolume(_node_realization, n.Data, n.Geometry as Volume);
                else if (n.Geometry is Face)
                    n_conn = this.AssociateComponentWithFace(_node_realization, n.Data, n.Geometry as Face);
                else if (n.Geometry is EdgeLoop)
                    n_conn = this.AssociateComponentWithEdgeLoop(_node_realization, n.Data, n.Geometry as EdgeLoop);
                else if (n.Geometry is Edge)
                    n_conn = this.AssociateComponentWithEdge(_node_realization, n.Data, n.Geometry as Edge);
                else if (n.Geometry is Vertex)
                    n_conn = this.AssociateComponentWithVertex(_node_realization, n.Data, n.Geometry as Vertex);

                // recursion
                if (n_conn != null)
                {
                    n_conn.SynchronizeSourceWTarget(n.Geometry);

                    _node_realization.GeometryBasedChildren.Add(n_conn);
                    this.TranslateToConnectors(n, n_conn);
                }
            }
        }

        /// <summary>
        /// Creates an association between a component and a volume. The component represents only the volume.
        /// The parent connector should contain a component along the parent chain of _comp and have the appropriate
        /// geometric relationship type: e.g. Relation2GeomType.DESCRIBES
        /// </summary>
        /// <param name="_parent_connector">should contain a parent component</param>
        /// <param name="_comp">the source component</param>
        /// <param name="_vol">the target volume</param>
        /// <returns>The created connector</returns>
        internal ConnectorToVolume AssociateComponentWithVolume(ConnectorToBaseGeometry _parent_connector, SimComponent _comp, Volume _vol)
        {
            BaseGeometry geom = this.GeometryManager.GetGeometryFromId(_parent_connector.TargetModelIndex, _parent_connector.TargetId);
            ConnectorConnectionState evaluation = ConnectionStateEvaluator.Evaluate(_parent_connector.DescriptiveSource, geom, _comp, _vol);
            if (evaluation != ConnectorConnectionState.OK)
                return null;

            SimComponent direct_parent = (_parent_connector.DescriptiveSource == null) ? _comp.Parent as SimComponent : _parent_connector.DescriptiveSource;
            ConnectorToVolume connector = new ConnectorToVolume(this, direct_parent, _comp, this.GetModelIndex(_vol), _vol);
            this.ConnectorManager.AddConnector(connector, _vol);
            return connector;
        }

        /// <summary>
        /// Creates an association between a component and a face. The component represents only the face. It could have
        /// sub-components representing openings, windows or contained faces.
        /// The parent connector should contain a component along the parent chain of _comp and have the appropriate
        /// geometric relationship type: e.g. Relation2GeomType.DESCRIBES or Relation2GeomType.NONE
        /// </summary>
        /// <param name="_parent_connector">should contain a parent component</param>
        /// <param name="_comp">the source component</param>
        /// <param name="_face">the target face</param>
        /// <returns>The created connector</returns>
        internal ConnectorToFace AssociateComponentWithFace(ConnectorToBaseGeometry _parent_connector, SimComponent _comp, Face _face)
        {
            BaseGeometry geom = this.GeometryManager.GetGeometryFromId(_parent_connector.TargetModelIndex, _parent_connector.TargetId);
            ConnectorConnectionState evaluation = ConnectionStateEvaluator.Evaluate(_parent_connector.DescriptiveSource, geom, _comp, _face);
            if (evaluation != ConnectorConnectionState.OK)
                return null;

            SimComponent direct_parent = (_parent_connector.DescriptiveSource == null) ? _comp.Parent as SimComponent : _parent_connector.DescriptiveSource;
            ConnectorToFace connector = new ConnectorToFace(this, direct_parent, _comp, this.GetModelIndex(_face), _face);
            this.ConnectorManager.AddConnector(connector, _face);
            return connector;
        }

        /// <summary>
        /// Creates an association between a component and an edge loop. The component represents only the edge loop.
        /// The parent connector should contain a component along the parent chain of _comp and have the appropriate
        /// geometric relationship type: e.g. Relation2GeomType.DESCRIBES_2DorLESS or Relation2GeomType.NONE
        /// </summary>
        /// <param name="_parent_connector">should contain a parent component</param>
        /// <param name="_comp">the source component</param>
        /// <param name="_loop">the target edge loop</param>
        /// <returns>The created connector</returns>
        internal ConnectorToEdgeLoop AssociateComponentWithEdgeLoop(ConnectorToBaseGeometry _parent_connector, SimComponent _comp, EdgeLoop _loop)
        {
            BaseGeometry geom = this.GeometryManager.GetGeometryFromId(_parent_connector.TargetModelIndex, _parent_connector.TargetId);
            ConnectorConnectionState evaluation = ConnectionStateEvaluator.Evaluate(_parent_connector.DescriptiveSource, geom, _comp, _loop);
            if (evaluation != ConnectorConnectionState.OK)
                return null;

            SimComponent direct_parent = (_parent_connector.DescriptiveSource == null) ? _comp.Parent as SimComponent : _parent_connector.DescriptiveSource;
            ConnectorToEdgeLoop connector = new ConnectorToEdgeLoop(this, direct_parent, _comp, this.GetModelIndex(_loop), _loop);
            this.ConnectorManager.AddConnector(connector, _loop);
            return connector;
        }

        /// <summary>
        /// Creates an association between a component and an edge. The component represents only the edge.
        /// The parent connector should contain a component along the parent chain of _comp and have the appropriate
        /// geometric relationship type: e.g. Relation2GeomType.DESCRIBES or Relation2GeomType.NONE
        /// </summary>
        /// <param name="_parent_connector">should contain a parent component</param>
        /// <param name="_comp">the source component</param>
        /// <param name="_edge">the target edge</param>
        /// <returns>The created connector</returns>
        internal ConnectorToEdge AssociateComponentWithEdge(ConnectorToBaseGeometry _parent_connector, SimComponent _comp, Edge _edge)
        {
            BaseGeometry geom = this.GeometryManager.GetGeometryFromId(_parent_connector.TargetModelIndex, _parent_connector.TargetId);
            ConnectorConnectionState evaluation = ConnectionStateEvaluator.Evaluate(_parent_connector.DescriptiveSource, geom, _comp, _edge);
            if (evaluation != ConnectorConnectionState.OK)
                return null;

            SimComponent direct_parent = (_parent_connector.DescriptiveSource == null) ? _comp.Parent as SimComponent : _parent_connector.DescriptiveSource;
            ConnectorToEdge connector = new ConnectorToEdge(this, direct_parent, _comp, this.GetModelIndex(_edge), _edge);
            this.ConnectorManager.AddConnector(connector, _edge);
            connector.SynchronizeSourceWTarget(_edge);
            return connector;
        }

        /// <summary>
        /// Creates an association between a component and a vertex. The component represents only the vertex.
        /// The parent connector should contain a component along the parent chain of _comp and have the appropriate
        /// geometric relationship type: e.g. Relation2GeomType.DESCRIBES or Relation2GeomType.NONE
        /// </summary>
        /// <param name="_parent_connector">should contain a parent component</param>
        /// <param name="_comp">the source component</param>
        /// <param name="_vertex">the target vertex</param>
        /// <returns>The created connector</returns>
        internal ConnectorToVertex AssociateComponentWithVertex(ConnectorToBaseGeometry _parent_connector, SimComponent _comp, Vertex _vertex)
        {
            BaseGeometry geom = this.GeometryManager.GetGeometryFromId(_parent_connector.TargetModelIndex, _parent_connector.TargetId);
            ConnectorConnectionState evaluation = ConnectionStateEvaluator.Evaluate(_parent_connector.DescriptiveSource, geom, _comp, _vertex);
            if (evaluation != ConnectorConnectionState.OK)
                return null;

            SimComponent direct_parent = (_parent_connector.DescriptiveSource == null) ? _comp.Parent as SimComponent : _parent_connector.DescriptiveSource;
            ConnectorToVertex connector = new ConnectorToVertex(this, direct_parent, _comp, this.GetModelIndex(_vertex), _vertex);
            this.ConnectorManager.AddConnector(connector, _vertex);
            connector.SynchronizeSourceWTarget(_vertex);
            return connector;
        }

        #endregion

        #region ASSOCIATION METHODS: the component as a PRESCRIPTOR

        /// <summary>
        /// Associates a component with the given Face. The component determines the
        /// inner and outer offsets of the Face. No sub-components are generated, however
        /// the reserved parameters for the offsets, the total area and the total number of affected faces
        /// are generated, if missing. Triggers the AssociationChanged event.
        /// </summary>
        /// <param name="_comp">the prescribing component</param>
        /// <param name="_face">the target face</param>
        /// <returns>the connector and connection feedback</returns>
        internal (ConnectorPrescriptiveToFace connector, ConnectorConnectionState feedback) PrescribeFromComponentToFace(SimComponent _comp, Face _face)
        {
            ConnectorConnectionState fb = ConnectorConnectionState.OK;

            // 0. check if the face has a prescriptive component assigned to it and remove it
            InstanceConnectorToFace prescr = this.ConnectorManager.GetPrescriptorOf(_face);
            if (prescr != null)
            {
                this.RemovePrescriptiveAssociationToFace(prescr.SourceParent, _face);
            }

            var resourceIdx = this.GetModelIndex(_face);

            // 1. check if a connector with this component as a source exists           
            if (this.ConnectorManager.HasConnectorsToComponent(_comp, resourceIdx))
            {
                ConnectorBase conn = this.ConnectorManager.RetrieveConnectorToComponent(_comp, resourceIdx);
                if (conn is ConnectorPrescriptiveToFace)
                {
                    ConnectorPrescriptiveToFace pconn = conn as ConnectorPrescriptiveToFace;
                    var result = pconn.ConnectToFace(this.GetModelIndex(_face), _face);
                    if (result.newly_created)
                        this.ConnectorManager.AddConnector(result.instance_connector, _face);
                    else
                        fb |= ConnectorConnectionState.SOURCE_ALREADY_CONNECTED_TO_TARGET;
                    this.OnAssociationChanged(new List<BaseGeometry> { _face });
                    this.OnGeometryInvalidated(new List<BaseGeometry> { _face });
                    //this.AddReferencesAfterAdding(pconn, _face);
                    this.ModifyReferencesBasedOnConnectivity(pconn, _face, true);
                    return (pconn, fb);
                }
                else
                    return (null, fb);
            }

            // 2. check if component and face match
            fb |= ConnectionStateEvaluator.Evaluate(null, null, _comp, _face);
            fb &= ~ConnectorConnectionState.TARGET_GEOMETRY_IN_PARENT_MISSING; // the volume is of no consequence
            if (fb != ConnectorConnectionState.OK)
                return (null, fb);

            // 3. CREATE the connector
            ConnectorPrescriptiveToFace connector = new ConnectorPrescriptiveToFace(this, _comp.Parent as SimComponent, _comp, this.GetModelIndex(_face));
            this.ConnectorManager.AddConnector(connector, _face, false);

            // 4. create the instance connector for the specific face
            var result_1 = connector.ConnectToFace(this.GetModelIndex(_face), _face);
            this.ConnectorManager.AddConnector(result_1.instance_connector, _face);

            connector.UpdateCumulativeParameters(result_1.instance_connector.Source);

            // 5. add the references btw the relevant components
            // this.AddReferencesAfterAdding(connector, _face);
            this.ModifyReferencesBasedOnConnectivity(connector, _face, true);

            // DONE
            this.OnAssociationChanged(new List<BaseGeometry> { _face });
            this.OnGeometryInvalidated(new List<BaseGeometry> { _face });
            return (connector, fb);
        }

        /// <summary>
        /// Associates a component with the given multiple Faces. The component determines the
        /// inner and outer offsets of the Faces. No sub-components are generated, however
        /// the reserved parameters for the offsets, the total area and the total number of affected faces
        /// are generated, if missing. Triggers the AssociationChanged event for all Faces at once.
        /// </summary>
        /// <param name="_comp">the prescribing component</param>
        /// <param name="_faces">the target faces</param>
        /// <returns>the connector and connection feedback</returns>
        internal (ConnectorPrescriptiveToFace connector, ConnectorConnectionState feedback) PrescribeFromComponentToFaces(SimComponent _comp, IList<Face> _faces)
        {
            ConnectorConnectionState fb = ConnectorConnectionState.OK;
            int model_index = this.GetModelIndex(_faces[0]);
            bool faces_of_one_model = _faces.Select(x => this.GetModelIndex(x)).GroupBy(x => x).Count() == 1;
            if (!faces_of_one_model)
                throw new ArgumentException("The faces must belong to the same model!");

            // 0. FOR EACH FACE: check if the face has a prescriptive component assigned to it and remove it
            foreach (Face face in _faces)
            {
                InstanceConnectorToFace prescr = this.ConnectorManager.GetPrescriptorOf(face);
                if (prescr != null)
                    this.RemovePrescriptiveAssociationToFace(prescr.SourceParent, face);
            }

            // 1. check if a connector with this component as a source exists           
            if (this.ConnectorManager.HasConnectorsToComponent(_comp, model_index))
            {
                ConnectorBase conn = this.ConnectorManager.RetrieveConnectorToComponent(_comp, model_index);
                if (conn is ConnectorPrescriptiveToFace)
                {
                    ConnectorPrescriptiveToFace pconn = conn as ConnectorPrescriptiveToFace;
                    foreach (Face face in _faces)
                    {
                        var result = pconn.ConnectToFace(model_index, face);
                        if (result.newly_created)
                            this.ConnectorManager.AddConnector(result.instance_connector, face);
                        else
                            fb |= ConnectorConnectionState.SOURCE_ALREADY_CONNECTED_TO_TARGET;
                    }

                    this.OnAssociationChanged(_faces);
                    this.OnGeometryInvalidated(_faces);
                    this.ModifyReferencesBasedOnMultipleConnectivity(pconn, _faces, true);

                    return (pconn, fb);
                }
                else
                    return (null, fb);
            }

            // 2. check if component and faces match
            foreach (Face face in _faces)
            {
                fb |= ConnectionStateEvaluator.Evaluate(null, null, _comp, face);
            }
            fb &= ~ConnectorConnectionState.TARGET_GEOMETRY_IN_PARENT_MISSING; // the volume is of no consequence
            if (fb != ConnectorConnectionState.OK)
                return (null, fb);

            // 3. CREATE the connectors
            ConnectorPrescriptiveToFace connector = new ConnectorPrescriptiveToFace(this, _comp.Parent as SimComponent, _comp, model_index);
            this.ConnectorManager.AddConnector(connector, _faces, false);

            // 4. create the instance connectors for each specific face
            foreach (Face face in _faces)
            {
                var result_1 = connector.ConnectToFace(model_index, face, false);
                this.ConnectorManager.AddConnector(result_1.instance_connector, face);
            }
            connector.UpdateCumulativeParameters();

            // 5. add the references btw the relevant components
            this.ModifyReferencesBasedOnMultipleConnectivity(connector, _faces, true);

            // DONE
            this.OnAssociationChanged(_faces);
            this.OnGeometryInvalidated(_faces);
            return (connector, fb);
        }

        /// <summary>
        /// Removes the prescriptive association of the component to the given Face. The connector removes the instance connector
        /// responsible for managing the given face. All other instance connectors remain intact. If the given face
        /// is the last one associated with a component, the connector managing the component is also deleted.
        /// </summary>
        /// <param name="_comp">the given prescriptive component</param>
        /// <param name="_f">the face</param>
        internal void RemovePrescriptiveAssociationToFace(SimComponent _comp, Face _f)
        {
            // 1. look for the master connector
            ConnectorPrescriptiveToFace master_connector = null;
            if (this.ConnectorManager.HasConnectorsToGeometry(_f.Id))
            {
                List<ConnectorBase> connectors = this.ConnectorManager.RetrieveConnectorsToGeometry(_f.Id);
                List<InstanceConnectorToFace> fitting_connectors = connectors.Where(x => x is InstanceConnectorToFace).Select(x => x as InstanceConnectorToFace).ToList();
                if (fitting_connectors.Count > 0)
                    master_connector = fitting_connectors[0].ParentConnector as ConnectorPrescriptiveToFace;

                if (master_connector != null && master_connector.PrescriptiveSource == _comp)
                {
                    this.ModifyReferencesBasedOnConnectivity(master_connector, _f, false);
                    bool delete_master = master_connector.DisconnectFromFace(this.GetModelIndex(_f), _f);
                    if (delete_master)
                        this.ConnectorManager.RemoveConnector(master_connector, _f);
                    this.OnGeometryInvalidated(new List<BaseGeometry> { _f });
                }
            }
        }

        #endregion

        #region METHODS: Connector interaction -> references

        /// <summary>
        /// Updates the references between components resulting from the relationships
        /// between the connected geometry after major events (e.g. geometry model swap).
        /// </summary>
        /// <param name="_connecting">true = interprest the found relationships as connecting, false = ... as disconnecting</param>
        internal void ModifyAllReferencesBasedOnConnectivity(bool _connecting)
        {
            // traverse all space descriptors
            List<Volume> all_volumes = new List<Volume>();
            foreach (var entry in ProjectData.GeometryModels)
            {
                all_volumes.AddRange(this.GeometryManager.RetrieveVolumesInFile(entry.File.FullName));
            }
            foreach (Volume v in all_volumes)
            {
                IEnumerable<ConnectorToComplex> v_descr = this.ConnectorManager.GetSpaceDescriptorOf(v);
                if (v_descr != null)
                {
                    foreach (ConnectorToComplex v_d in v_descr)
                        this.ModifyReferencesBasedOnConnectivity(v_d, v, _connecting);
                }
            }

            // traverse all wall prescriptors
            List<ConnectorPrescriptiveToFace> all_prescriptors = this.ConnectorManager.GetAllFacePrescriptors().ToList();
            foreach (var pr in all_prescriptors)
            {
                this.ModifyReferencesBasedOnConnectivity(pr, null, _connecting);
            }
        }

        /// <summary>
        /// Creates or removes references between components based on neighborhood relationships
        /// or on being connected to the same geometry (e.g. wall construction and face descriptor).
        /// </summary>
        /// <param name="_connector">the newly added or removed connector</param>
        /// <param name="_target">the target geometry of the connector</param>
        /// <param name="_connecting">true = connector was just added, false = connector is being deleted</param>
        internal void ModifyReferencesBasedOnConnectivity(ConnectorBase _connector, BaseGeometry _target, bool _connecting)
        {
            ConnectorToComplex descriptor_SPACE = _connector as ConnectorToComplex;
            ConnectorToFace descriptor_FACE = _connector as ConnectorToFace;
            ConnectorPrescriptiveToFace prescriptor_FACE = _connector as ConnectorPrescriptiveToFace;

            if (descriptor_SPACE != null && _target is Volume)
            {
                // 1. look for neighbor-based reference pairs               
                List<KeyValuePair<ConnectorToBaseGeometry, ConnectorToBaseGeometry>> ref_pairs = this.ConnectorManager.GetNeighborBasedConnectorReferencePairs(_target as Volume);
                foreach (var pair in ref_pairs)
                {
                    if (_connecting)
                        this.ProjectData.Components.AddReferenceBasedOnGeometry(pair.Key.DescriptiveSource, pair.Value.DescriptiveSource);
                    else
                        this.ProjectData.Components.RemoveReferenceBasedOnGeometry(pair.Key.DescriptiveSource, pair.Value.DescriptiveSource);
                }
                // 2. look for existing wall constructions (prescriptors) associated with the faces of the volume
                Dictionary<ulong, Face> all_faces = ConnectorAlgorithms.GetAllFacesWSubFaces(_target as Volume);
                foreach (var entry in all_faces)
                {
                    List<KeyValuePair<ConnectorToFace, ConnectorPrescriptiveToFace>> f_ref_paris = this.ConnectorManager.GetFaceBasedConnectorReferencePairs(entry.Value);
                    foreach (var pair in f_ref_paris)
                    {
                        if (_connecting)
                            this.ProjectData.Components.AddReferenceBasedOnGeometry(pair.Key.DescriptiveSource, pair.Value.PrescriptiveSource);
                        else
                            this.ProjectData.Components.RemoveReferenceBasedOnGeometry(pair.Key.DescriptiveSource, pair.Value.PrescriptiveSource);
                    }
                }
            }
            else if (descriptor_FACE != null && _target is Face)
            {
                // look for affected volumes and pass the logic to them (recursion)
                List<Volume> affected_vols = ConnectorAlgorithms.GetVolumesOf(_target as Face);
                if (affected_vols.Count > 1)
                {
                    foreach (Volume v in affected_vols)
                    {
                        IEnumerable<ConnectorToComplex> v_descr = this.ConnectorManager.GetSpaceDescriptorOf(v);
                        if (v_descr != null)
                        {
                            foreach (ConnectorToComplex v_d in v_descr)
                                this.ModifyReferencesBasedOnConnectivity(v_d, v, _connecting);
                        }
                    }
                }

            }
            else if (prescriptor_FACE != null)
            {
                // 1. look for the descriptors of faces the prescriptor is applied to
                List<KeyValuePair<ConnectorToFace, ConnectorPrescriptiveToFace>> ref_pairs = this.ConnectorManager.GetAlignmentBasedConnectorReferencePairs(prescriptor_FACE);
                foreach (var pair in ref_pairs)
                {
                    if (_target != null && pair.Key.TargetId != _target.Id) continue;
                    if (_connecting)
                        this.ProjectData.Components.AddReferenceBasedOnGeometry(pair.Key.DescriptiveSource, pair.Value.PrescriptiveSource);
                    else
                        this.ProjectData.Components.RemoveReferenceBasedOnGeometry(pair.Key.DescriptiveSource, pair.Value.PrescriptiveSource);
                }
            }
            //Console.WriteLine("ModifyReferencesBasedOnConnectivity for {0}", _target?.Id);
        }

        /// <summary>
        /// Creates or removes references between components based on neighborhood relationships
        /// or on being connected to the same geometry (e.g. wall construction and face descriptor).
        /// </summary>
        /// <param name="_connector">the newly added or removed prescriptive connector for the given faces</param>
        /// <param name="_target_faces">the target faces of the connector</param>
        /// <param name="_connecting">true = connector was just added, false = connector is being deleted</param>
        internal void ModifyReferencesBasedOnMultipleConnectivity(ConnectorPrescriptiveToFace _connector, IList<Face> _target_faces, bool _connecting)
        {
            if (_connector == null || _target_faces == null) return;
            if (_target_faces.Count() == 0) return;

            List<KeyValuePair<ConnectorToFace, ConnectorPrescriptiveToFace>> ref_pairs = this.ConnectorManager.GetAlignmentBasedConnectorReferencePairs(_connector);
            foreach (var pair in ref_pairs)
            {
                //// this check could be removed, if it takes too long... (the call should be executed with the corresponding connector and faces anyway)
                //Face f = _target_faces.FirstOrDefault(x => x.Id == pair.Key.TargetId);
                //if (f == null) continue;

                if (_connecting)
                    this.ProjectData.Components.AddReferenceBasedOnGeometry(pair.Key.DescriptiveSource, pair.Value.PrescriptiveSource);
                else
                    this.ProjectData.Components.RemoveReferenceBasedOnGeometry(pair.Key.DescriptiveSource, pair.Value.PrescriptiveSource);
            }
        }

        #endregion

        #region METHODS: Query connectors from GV

        /// <inheritdoc />
		public IEnumerable<SimComponent> GetComponents(BaseGeometry geometry)
        {
            return this.ConnectorManager.RetrieveComponents(geometry);
        }


        /// <summary>
        /// Gets the outer and inner offsets of the given face according to 
        /// the associated component representing a wall construction. If there is no
        /// such component, both offsets are 0.0.
        /// </summary>
        /// <param name="_face">the face whose offsets we are looking for</param>
        /// <returns>a tuple of doubles: the outer and inner offsets</returns>
        public (double outer, double inner) GetFaceOffset(Face _face)
        {
            if (_face == null) return (0.0, 0.0);

            var wall_cstr = this.ConnectorManager.GetAlignedWith(_face);
            if (wall_cstr != null)
            {
                var p_oo = ComponentWalker.GetFlatParameters(wall_cstr.SourceParent)
                    .FirstOrDefault(x => x.Name == ReservedParameters.RP_MATERIAL_COMPOSITE_D_OUT);
                double oo = (p_oo == null) ? 0.0 : p_oo.ValueCurrent;
                var p_oi = ComponentWalker.GetFlatParameters(wall_cstr.SourceParent)
                    .FirstOrDefault(x => x.Name == ReservedParameters.RP_MATERIAL_COMPOSITE_D_IN);
                double oi = (p_oi == null) ? 0.0 : p_oi.ValueCurrent;
                return (oo, oi);
            }

            return (0.0, 0.0);
        }

        #endregion

        #region METHODS: managing geometry as an ASSET (GV and CB)

        internal void AddAssetToComponent(ConnectorToBaseGeometry _caller, BaseGeometry _target)
        {
            if (_target == null) return;

            FileInfo fi = _target.ModelGeometry.Model.File;

            var resource = this.ProjectData.AssetManager.GetResource(fi);
            if (resource != null)
            {
                Asset duplicate = _caller.DescriptiveSource.ReferencedAssets.FirstOrDefault(
                    x => x.ResourceKey == resource.Key && x.ContainedObjectId == _target.Id.ToString());
                if (duplicate == null) // Asset doesn't exit
                {
                    GeometricAsset created = this.ProjectData.AssetManager.CreateGeometricAsset(_caller.DescriptiveSource.Id.LocalId,
                        resource.Key, _target.Id.ToString());
                    if (created != null)
                        _caller.DescriptiveSource.ReferencedAssets_Internal.Add(created);
                }
            }
        }

        internal void RemoveAssetFromComponent(ConnectorToBaseGeometry _caller, int _geometry_model_index, ulong _target_id)
        {
            //this.ProjectData.AssetManager.RemoveAssetFromComponent(_caller.DescriptiveSource, _geometry_model_index, _target_id.ToString());
            if (_geometry_model_index >= 0)
            {
                Asset asset = _caller.DescriptiveSource.ReferencedAssets.FirstOrDefault(
                    x => x.ResourceKey == _geometry_model_index && x.ContainedObjectId == _target_id.ToString());
                if (asset != null)
                {
                    _caller.DescriptiveSource.RemoveAsset(asset);
                    this.ProjectData.AssetManager.RemoveAsset(asset);
                }
            }
        }

        /// <inheritdoc />
        public bool ResourceFileExists(FileInfo file)
        {
            return this.ProjectData.AssetManager.ResourceFileEntryExists(file);
        }

        /// <inheritdoc />
        public bool IsValidResourcePath(FileInfo fi, bool isContained)
        {
            return this.ProjectData.AssetManager.IsValidResourcePath(fi, isContained);
        }

        /// <inheritdoc />
        public void AddResourceFile(FileInfo file)
        {
            this.ProjectData.AssetManager.AddResourceEntry(file);
        }

        #endregion

        #region METHODS: managing geometry models

        private void GeometryModel_DataReplaced(object sender, GeometryModelReplacedEventArgs e)
        {
            this.GeometryManager.TransferObservations(e.OldGeometry, e.NewGeometry);
        }

        private void GeometryModels_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (var model in e.NewItems.OfType<GeometryModel>())
                    {
                        AddGeometryModel(model);
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    foreach (var model in e.OldItems.OfType<GeometryModel>())
                    {
                        SimFlowNetwork nw = (this.NetworkCommunicator as NetworkGeometryExchange).GetPossibleNetworkRepresentation(model);
                        if (nw != null)
                        {
                            NetworkCommunicator.RemoveGeometricRepresentationFromNetwork(model, nw, this.ProjectData);
                        }
                        this.GeometryManager.DetachGeometryModel(model, false);

                        model.Replaced -= GeometryModel_DataReplaced;
                    }
                    break;
                default:
                    throw new NotImplementedException("Operation not supported");
            }
        }

        /// <inheritdoc />
        private void AddGeometryModel(GeometryModel _model)
        {
            var resourceKey = this.ProjectData.AssetManager.GetResourceKey(_model.File);
            this.previously_opened_files.Add(resourceKey);

            _model.Replaced += GeometryModel_DataReplaced;

            int model_index_in_resources = this.ProjectData.AssetManager.GetResourceKey(_model.File);
            // dispatcher:
            SimFlowNetwork nw = (this.NetworkCommunicator as NetworkGeometryExchange).GetPossibleNetworkRepresentation(_model);
            if (nw == null)
            {
                this.GeometryManager.AttachGeometryModel(_model.Geometry, !this.previously_opened_files.Contains(model_index_in_resources), false);
                this.GeometryInvalidated?.Invoke(this, null);

                this.ConnectorManager.ReconnectAfterLoad();
            }
            else
            {
                (this.NetworkCommunicator as NetworkGeometryExchange).ReAttachGeometryModel(_model, nw, this.ProjectData);
            }
        }

        /// <inheritdoc/>
        public bool RemoveGeometryModel(FileInfo _file, GeometryModelRemovalMode _reason)
        {
            if (this.ProjectData.GeometryModels.TryGetGeometryModel(_file, out var model, false))
            {
                this.GeometryManager.DetachGeometryModel(model, _reason == GeometryModelRemovalMode.DELETE);
                return true;
            }
            return false;
        }

        #endregion

        #region UTILS: Info

        /// <summary>
        /// Gets the index of the geometry model file containing the given geometry.
        /// </summary>
        /// <param name="_g">the given base geometry</param>
        /// <returns>the id of the file, if it exists; otherwise -1</returns>
        internal int GetModelIndex(BaseGeometry _g)
        {
            return this.ProjectData.AssetManager.GetResourceKey(_g.ModelGeometry.Model.File);
        }

        /// <inheritdoc />
        public int GetResourceFileIndex(FileInfo fi)
        {
            if (fi == null)
                return -1;

            return this.ProjectData.AssetManager.GetKey(fi.Name);
        }

        /// <inheritdoc />
        public FileInfo GetFileFromResourceIndex(int resourceIndex)
        {
            return new FileInfo(this.ProjectData.AssetManager.GetResource(resourceIndex).CurrentFullPath);
        }

        #endregion

        #region EVENT HANDLERS: component manager ...IN POGRESS...

        /// <summary>
        /// Reacts to the deletion of networks.
        /// </summary>
        /// <param name="sender">the component manager</param>
        /// <param name="deletedNetworks">the deleted networks</param>
        private void ComponentManager_NetworkDeleted(object sender, IEnumerable<SimFlowNetwork> deletedNetworks)
        {
            if (this.NetworkCommunicator != null)
            {
                (this.NetworkCommunicator as NetworkGeometryExchange).OnNetworkDeleted(deletedNetworks);
            }
        }

        /// <summary>
        /// Reacts to the addition of networks.
        /// </summary>
        /// <param name="sender">the component manager</param>
        /// <param name="addedNetworks">the added networks</param>
        private void ComponentManager_NetworkAdded(object sender, IEnumerable<SimFlowNetwork> addedNetworks)
        {
            if (this.NetworkCommunicator != null)
            {
                // TODO...
            }
        }

        #endregion

        #region RESET

        /// <summary>
        /// Resets the internal state and all managers. To be called when a project is unloaded.
        /// </summary>
        public void Reset()
        {
            this.previously_opened_files.Clear();

            this.NetworkCommunicator.Reset();
            this.GeometryManager.Reset();
            this.ConnectorManager.Reset();

            this.EnableAsyncUpdates = true;

        }

        #endregion
    }
}
