using SIMULTAN.Data.Components;
using SIMULTAN.Data.Geometry;
using SIMULTAN.Exchange.Connectors;
using System.Collections.Generic;
using System.Linq;

namespace SIMULTAN.Exchange.ConnectorInteraction
{
    /// <summary>
    /// Maintains the internal consistency of connections between components and geometry.
    /// </summary>
    internal class ConnectorManager
    {
        #region FIELDS: component and geometry managers and lookup tables

        // context
        private ComponentGeometryExchange manager_of_all;

        // connector lookup tables
        private List<ConnectorBase> connectors;
        private Dictionary<SimComponent, List<ConnectorBase>> lookup_acc_to_component;
        private Dictionary<SimComponentInstance, ConnectorBase> lookup_acc_to_instance;
        private Dictionary<ulong, List<ConnectorBase>> lookup_acc_to_geometry;

        #endregion

        #region .CTOR
        /// <summary>
        /// Initializes the lookup tables for the connectors btw components and geometry.
        /// Attaches event handlers to each connector.
        /// </summary>
        /// <param name="_manager_of_all">the parent manager that holds the component and the geometry management</param>
        public ConnectorManager(ComponentGeometryExchange _manager_of_all)
        {
            // context
            this.manager_of_all = _manager_of_all;

            // initialize the connection management
            this.connectors = new List<ConnectorBase>();
            this.lookup_acc_to_component = new Dictionary<SimComponent, List<ConnectorBase>>();
            this.lookup_acc_to_instance = new Dictionary<SimComponentInstance, ConnectorBase>();
            this.lookup_acc_to_geometry = new Dictionary<ulong, List<ConnectorBase>>();
        }
        #endregion

        #region METHODS: Dis-connect

        /// <summary>
        /// Deletes the connector, in which the component '_comp' is the source. Deletes the connectors of all
        /// automatically created sub-components and, finally, the sub-components themselves.
        /// Cleans up the lookup tables and unsubscribes all relevant events.
        /// </summary>
        /// <param name="_comp">the component to be disconnected</param>
        /// <param name="_vol">the Volume the component is to be disconnected from</param>
        internal void DisassociateComponentFromComplex(SimComponent _comp, Volume _vol)
        {
            if (_comp == null || _vol == null) return;
            if (_comp.InstanceType != SimInstanceType.Entity3D || !_comp.InstanceState.IsRealized) return;
            if (!this.lookup_acc_to_component.ContainsKey(_comp)) return;

            // 1. get all possibly affected geometry
            Dictionary<ulong, BaseGeometry> possibly_affected_geometry = ConnectorAlgorithms.GetAllGeometricPartsOf(_vol);

            // 2. find the affected connectors
            var connector_main = this.lookup_acc_to_component[_comp].First(); // there can be only one component - volume pair
            if (_vol.Id != connector_main.TargetId) return;

            IEnumerable<SimComponent> affected_components = _comp.GetAllAssociatedWithGeometry();
            Dictionary<ConnectorBase, BaseGeometry> affected_connectors = new Dictionary<ConnectorBase, BaseGeometry>();

            foreach (SimComponent c in affected_components)
            {
                if (this.lookup_acc_to_component.ContainsKey(c))
                {
                    foreach (ConnectorBase connector in this.lookup_acc_to_component[c])
                    {
                        if (possibly_affected_geometry.ContainsKey(connector.TargetId))
                            affected_connectors.Add(connector, possibly_affected_geometry[connector.TargetId]);
                    }
                }
            }

            // 2a. remove the references
            this.manager_of_all.ModifyReferencesBasedOnConnectivity(connector_main, _vol, false);

            // 3. remove the connectors (includes the removal of the event handlers from the geometry)
            foreach (var con in affected_connectors)
            {
                this.RemoveConnector(con.Key, con.Value);
            }
            this.RemoveConnector(connector_main, _vol);

            //Remove instance
            SimComponentInstance instance = null;
            SimInstancePlacementGeometry placement = null;
            var fileId = this.manager_of_all.GetModelIndex(_vol);
            foreach (var inst in _comp.Instances)
            {
                placement = (SimInstancePlacementGeometry)inst.Placements.FirstOrDefault(
                    x => x is SimInstancePlacementGeometry pg && pg.FileId == fileId && pg.GeometryId == _vol.Id);
                if (placement != null)
                {
                    instance = inst;
                    break;
                }
            }

            using (AccessCheckingDisabler.Disable(_comp.Factory))
            {
                if (instance.Placements.Count == 1)
                    _comp.Instances.Remove(instance);
                else
                    instance.Placements.Remove(placement);
            }

            // 4. remove only the sub-components associated with parts of the geometry
            ConnectorAlgorithms.RemoveParallelHierarchy(this.manager_of_all.ProjectData.Components, _comp, _vol, fileId, 3, 2);
        }

        #endregion

        #region METHODS: Connector add, remove, lookup update

        /// <summary>
        /// Attaches the event handlers to the connector and to the target geometry.
        /// Only geometry bound in a connector has even handlers attached!
        /// </summary>
        /// <param name="_connector">the (newly) created connector</param>
        /// <param name="_target">the target geometry of the connector</param>
        /// <param name="_attach_event_handlers">if true, handlers for geometry events are attached to the component; if false - nothing is attached</param>
        internal void AddConnector(ConnectorBase _connector, BaseGeometry _target, bool _attach_event_handlers = true)
        {
            _connector.SourceIsBeingDeleted += connector_SourceIsBeingDeleted;
            _connector.TargetIsBeingDeleted += connector_TargetIsBeingDeleted;

            if (_attach_event_handlers)
            {
                _target.GeometryChanged -= base_geometry_GeometryChanged;
                _target.GeometryChanged += base_geometry_GeometryChanged;
            }

            this.UpDateConnectorLookUpTables(_connector, ConnectionChange.CREATED);
            // this.AssociationChanged?.Invoke(this, new List<BaseGeometry> { _target }); // OLD
            this.manager_of_all.OnAssociationChanged(new List<BaseGeometry> { _target }); // NEW 
        }

        /// <summary>
        /// Attaches the event handlers to the connector and to the multiple target geometry.
        /// Only geometry bound in a connector has even handlers attached!
        /// </summary>
        /// <param name="_connector">the (newly) created connector</param>
        /// <param name="_targets">the multiple target geometries of the connector</param>
        /// <param name="_attach_event_handlers">if true, handlers for geometry events are attached to the component; if false - nothing is attached</param>
        internal void AddConnector(ConnectorBase _connector, IEnumerable<BaseGeometry> _targets, bool _attach_event_handlers = true)
        {
            _connector.SourceIsBeingDeleted += connector_SourceIsBeingDeleted;
            _connector.TargetIsBeingDeleted += connector_TargetIsBeingDeleted;

            if (_attach_event_handlers)
            {
                foreach (BaseGeometry target in _targets)
                {
                    target.GeometryChanged -= base_geometry_GeometryChanged;
                    target.GeometryChanged += base_geometry_GeometryChanged;
                }
            }

            this.UpDateConnectorLookUpTables(_connector, ConnectionChange.CREATED);
            this.manager_of_all.OnAssociationChanged(_targets);
        }

        /// <summary>
        /// Performs clean-up in the connector and detaches the event handlers
        /// both from the connector and the target geometry. If the geometry is Null,
        /// the clean-up just deletes the connector.
        /// </summary>
        /// <param name="_connector">the connector about to be deleted</param>
        /// <param name="_target">the affected target geometry</param>
        internal void RemoveConnector(ConnectorBase _connector, BaseGeometry _target)
        {
            _connector.BeforeDeletion();
            _connector.SourceIsBeingDeleted -= connector_SourceIsBeingDeleted;
            _connector.TargetIsBeingDeleted -= connector_TargetIsBeingDeleted;

            if (_target != null)
                _target.GeometryChanged -= base_geometry_GeometryChanged;

            this.UpDateConnectorLookUpTables(_connector, ConnectionChange.DELETED);

            if (_target != null)
                this.manager_of_all.OnAssociationChanged(new List<BaseGeometry> { _target }); // NEW
        }

        /// <summary>
        /// Performs clean-up of the lookup tables on a major connection change:
        /// if a connector is deleted or created; if either the source or the target is deleted.
        /// </summary>
        /// <param name="_connector">the affected connector</param>
        /// <param name="_change">the change in the connector</param>
        private void UpDateConnectorLookUpTables(ConnectorBase _connector, ConnectionChange _change)
        {
            if (_connector == null) return;

            SimComponent key_comp = null;
            if (_connector is ConnectorToBaseGeometry)
                key_comp = (_connector as ConnectorToBaseGeometry).DescriptiveSource;
            else if (_connector is ConnectorPrescriptiveToFace)
                key_comp = (_connector as ConnectorPrescriptiveToFace).PrescriptiveSource;

            SimComponentInstance key_gr = null;
            if (_connector is InstanceConnectorToBaseGeometry)
                key_gr = (_connector as InstanceConnectorToBaseGeometry).Source;

            ulong key_geom = _connector.TargetId;
            switch (_change)
            {
                case ConnectionChange.CREATED:
                    this.connectors.Add(_connector);
                    if (key_comp != null)
                    {
                        if (this.lookup_acc_to_component.ContainsKey(key_comp))
                            this.lookup_acc_to_component[key_comp].Add(_connector);
                        else
                            this.lookup_acc_to_component.Add(key_comp, new List<ConnectorBase> { _connector });
                    }
                    else if (key_gr != null)
                        this.lookup_acc_to_instance.Add(key_gr, _connector);
                    if (key_geom != ulong.MaxValue)
                    {
                        if (this.lookup_acc_to_geometry.ContainsKey(key_geom))
                            this.lookup_acc_to_geometry[key_geom].Add(_connector);
                        else
                            this.lookup_acc_to_geometry.Add(key_geom, new List<ConnectorBase> { _connector });
                    }
                    break;
                case ConnectionChange.TARGET_DELETED:
                    this.connectors.Remove(_connector);
                    if (this.lookup_acc_to_geometry.ContainsKey(key_geom))
                        this.lookup_acc_to_geometry.Remove(key_geom);
                    break;
                case ConnectionChange.SOURCE_DELETED:
                case ConnectionChange.DELETED:
                    this.connectors.Remove(_connector);
                    if (key_comp != null && this.lookup_acc_to_component.ContainsKey(key_comp))
                    {
                        this.lookup_acc_to_component[key_comp].Remove(_connector);
                        if (this.lookup_acc_to_component[key_comp].Count == 0)
                            this.lookup_acc_to_component.Remove(key_comp);
                    }
                    else if (key_gr != null && this.lookup_acc_to_instance.ContainsKey(key_gr))
                        this.lookup_acc_to_instance.Remove(key_gr);
                    if (this.lookup_acc_to_geometry.ContainsKey(key_geom))
                    {
                        this.lookup_acc_to_geometry[key_geom].Remove(_connector);
                        if (this.lookup_acc_to_geometry[key_geom].Count == 0)
                            this.lookup_acc_to_geometry.Remove(key_geom);
                    }
                    break;
                case ConnectionChange.NONE:
                default:
                    break;
            }
        }

        #endregion

        #region METHODS: Connectors restore after loading

        /// <summary>
        /// Searches for associations to geometry in each component on the component factory's record
        /// and creates the appropriate connectors. To be called after a new geometry file
        /// is opened, or a new geometry model was added.
        /// </summary>
        /// <remarks>
        /// 1. Resets the look-up tables and retrieves all components associated with geometry.
        /// 2. For each of the currently opened geometry files does the following:
        ///     2.1. Looks for the geometry objects with the ids supplied by the components
        ///         2.1.2 If the geometry object could not be found, the component is informed, but it does not change.
        ///         2.2.2 IF the geometry object is found, a connector of the appropriate type is created and added to the look-up tables.
        ///             2.2.2.1 In the case of type ALIGNED_WITH, if a face could not be found, the corresponding relationship is removed from the component.
        /// IMPORTANT: This method does not restore the hierarchy between descriptive connectors! 
        /// For that, see method <see cref="ConnectorManager.RestoreConnectorHierarchy"/>.
        /// </remarks>
        public void RestoreConnectivityAfterLoading(bool updateParameters)
        {
            // reset all lookup tables
            this.connectors = new List<ConnectorBase>();
            this.lookup_acc_to_component = new Dictionary<SimComponent, List<ConnectorBase>>();
            this.lookup_acc_to_instance = new Dictionary<SimComponentInstance, ConnectorBase>();
            this.lookup_acc_to_geometry = new Dictionary<ulong, List<ConnectorBase>>();

            IEnumerable<SimComponent> all_associated_components = this.manager_of_all.ProjectData.Components.GetAllAssociatedWithGeometry();
            foreach (var entry in this.manager_of_all.ProjectData.GeometryModels)
            {
                if (!entry.File.Exists) continue;

                int resource_id_of_file = this.manager_of_all.ProjectData.AssetManager.GetResourceKey(entry.File);

                // restore...
                foreach (SimComponent c in all_associated_components)
                {
                    this.ReconnectSingleComponent(c, resource_id_of_file, updateParameters);
                }
            }
        }

        /// <summary>
        /// Those Geometric Relationships in the calling component that refer to any of the ids in 
        /// '_ids_wo_corresponding_geometry' from the given file have their connection state set to
        /// GEOMETRY_NOT_FOUND.
        /// </summary>
        /// <param name="_comp">the calling component</param>
        /// <param name="_from_file_with_id">the file id in the resource manager, where the currently loaded geometry resides</param>
        /// <param name="_ids_wo_corresponding_geometry">collection of ids whose geometry counterpart could not be found in the given file</param>
        private static void SetGeometryToNotFound(SimComponent _comp, int _from_file_with_id, HashSet<ulong> _ids_wo_corresponding_geometry)
        {
            foreach (var gr in _comp.Instances)
            {
                var placement = (SimInstancePlacementGeometry)gr.Placements.FirstOrDefault(p => p is SimInstancePlacementGeometry gp &&
                                           gp.FileId == _from_file_with_id && _ids_wo_corresponding_geometry.Contains(gp.GeometryId));
                if (placement != null)
                {
                    using (AccessCheckingDisabler.Disable(_comp.Factory))
                    {
                        gr.State = new SimInstanceState(gr.State.IsRealized, SimInstanceConnectionState.GeometryNotFound);
                        placement.State = SimInstancePlacementState.InstanceTargetMissing;
                    }
                }
            }
        }

        internal void ReconnectSingleComponent(SimComponent c, int _resource_id_of_file, bool updateParameters)
        {
            var associated_ids = c.GetAllAssociatedGeometryIds(_resource_id_of_file).ToList();
            // find the specific geometry
            List<BaseGeometry> targets = new List<BaseGeometry>();
            if (associated_ids.Count > 0)
                targets = associated_ids.Select(x => this.manager_of_all.GeometryManager.GetGeometryFromId(_resource_id_of_file, x)).Where(x => x != null).ToList();

            HashSet<ulong> orphaned_ids = null;
            List<ulong> newly_created_ids = new List<ulong>();
            var targetIds = targets.Select(x => x.Id).ToHashSet();

            if (targets.Count < associated_ids.Count)
            {
                // inform the user
                orphaned_ids = associated_ids.Where(x => !targetIds.Contains(x)).ToHashSet();
                newly_created_ids = targets.Select(bg => bg.Id).Where(x => !associated_ids.Contains(x)).ToList();

                SetGeometryToNotFound(c, _resource_id_of_file, orphaned_ids);

                if (c.Instances.Count == 1 && c.InstanceType == SimInstanceType.GeometricSurface &&
                    c.Instances[0].State.ConnectionState == SimInstanceConnectionState.GeometryNotFound)
                {
                    if (c.Parent != null)
                        c.Parent.Components.Remove(c.ParentContainer);
                }
            }
            if (targets.Count == 0)
            {
                return;
            }

            // check for other connectors with the same target
            List<ConnectorBase> existing_connectors = new List<ConnectorBase>();
            if (this.lookup_acc_to_geometry.ContainsKey(targets[0].Id))
            {
                existing_connectors = this.lookup_acc_to_geometry[targets[0].Id];
            }

            ConnectorBase connector = null;
            switch (c.InstanceType)
            {
                case SimInstanceType.Entity3D:
                    if (targets[0] is Volume && existing_connectors.Count == 0)
                        connector = new ConnectorToComplex(this.manager_of_all, c.Parent as SimComponent, c, _resource_id_of_file, targets[0] as Volume);

                    if (connector != null)
                    {
                        this.AddConnector(connector, targets[0]);
                        if (updateParameters)
                            connector.SynchronizeSourceWTarget(targets[0]);
                    }
                    break;
                case SimInstanceType.GeometricVolume:
                    if (targets[0] is Volume && existing_connectors.Count == 0)
                        connector = new ConnectorToVolume(this.manager_of_all, c.Parent as SimComponent, c, _resource_id_of_file, targets[0] as Volume);

                    if (connector != null)
                    {
                        this.AddConnector(connector, targets[0]);
                        if (updateParameters)
                            connector.SynchronizeSourceWTarget(targets[0]);
                    }
                    break;
                case SimInstanceType.GeometricSurface:
                    if (existing_connectors.Count == 0)
                    {
                        if (targets[0] is Face)
                            connector = new ConnectorToFace(this.manager_of_all, c.Parent as SimComponent, c, _resource_id_of_file, targets[0] as Face);
                        if (targets[0] is EdgeLoop)
                            connector = new ConnectorToEdgeLoop(this.manager_of_all, c.Parent as SimComponent, c, _resource_id_of_file, targets[0] as EdgeLoop);
                        else if (targets[0] is Edge)
                            connector = new ConnectorToEdge(this.manager_of_all, c.Parent as SimComponent, c, _resource_id_of_file, targets[0] as Edge);
                        else if (targets[0] is Vertex)
                            connector = new ConnectorToVertex(this.manager_of_all, c.Parent as SimComponent, c, _resource_id_of_file, targets[0] as Vertex);
                    }

                    if (connector != null)
                    {
                        this.AddConnector(connector, targets[0]);
                        if (updateParameters)
                            connector.SynchronizeSourceWTarget(targets[0]);
                    }
                    break;
                case SimInstanceType.Attributes2D:
                    for (int i = 0; i < targets.Count; i++)
                    {
                        Face f = targets[i] as Face;
                        if (f == null)
                        {
                            // remove the corresponding geometric relationship
                            var instance = c.Instances.FirstOrDefault(inst => inst.Placements.Any(
                                x => x is SimInstancePlacementGeometry pg && pg.FileId == _resource_id_of_file && pg.GeometryId == targets[i].Id));
                            c.Instances.Remove(instance);
                            continue;
                        }

                        // avoid double assignments
                        var existing_prescriptor = this.GetPrescriptorOf(f);
                        if (existing_prescriptor != null)
                        {
                            var instance = c.Instances.FirstOrDefault(inst => inst.Placements.Any(
                                x => x is SimInstancePlacementGeometry pg && pg.FileId == _resource_id_of_file && pg.GeometryId == targets[i].Id));
                            c.Instances.Remove(instance);
                            continue;
                        }

                        if (i == 0)
                        {
                            connector = new ConnectorPrescriptiveToFace(this.manager_of_all, c.Parent as SimComponent, c, _resource_id_of_file);
                            if (connector != null)
                                this.AddConnector(connector, f, false);
                        }
                        else
                        {
                            connector = this.GetAlignedWith(targets[0] as Face).ParentConnector;
                        }
                        if (connector != null)
                        {
                            // create the instance connector for the specific face
                            var result_i = (connector as ConnectorPrescriptiveToFace).ConnectToFace(_resource_id_of_file, f, updateParameters);
                            if (result_i.instance_connector != null)
                                this.AddConnector(result_i.instance_connector, f);
                        }
                    }
                    break;
                case SimInstanceType.NetworkNode:
                case SimInstanceType.NetworkEdge:
                case SimInstanceType.Group:
                case SimInstanceType.BuiltStructure:
                default:
                    break;
            }

            // finally, report problems anyway, updated 2019.10.17 (merge and update fix)
            if (orphaned_ids != null)
                SetGeometryToNotFound(c, _resource_id_of_file, orphaned_ids);
        }

        /// <summary>
        /// Performs the value propagation to the components in bulk after the creation of all connectors.
        /// </summary>
        public void ReconnectAfterLoad()
        {
            foreach (var connector in this.connectors)
            {
                if (connector is ConnectorToBaseGeometry ctc)
                {
                    ctc.SynchronizeSourceWTarget(this.manager_of_all.GeometryManager.GetGeometryFromId(ctc.TargetModelIndex, ctc.TargetId));
                }
                else if (connector is ConnectorPrescriptiveToFace cptf)
                {
                    cptf.SynchronizeSourceWTarget();
                    cptf.UpdateCumulativeParameters();
                }
            }
        }

        #endregion

        #region METHODS: connector hierarchy

        /// <summary>
        /// Restores the hierarchy between descriptive connectors after (re)loading of the geometry data
        /// or after a change in geometry. Missing connectors to new geometry are identified and added.
        /// The hierarchy is saved in the <see cref="ConnectorToBaseGeometry.GeometryBasedChildren"/> property.
        /// The algorithm starts at the instances of type <see cref="ConnectorToComplex"/> and assumes that the lookup tables are up-to-date.
        /// </summary>
        /// <param name="_update_refs">if true, restores geometry-based references</param>
        /// <param name="_update_parameters">if true, synchronize with the geometry target</param>
        /// <remarks>
        /// 1. Extracts all space descriptors. For each of them does the following:
        ///     1.1. Looks for the corresponding volume.
        ///         1.1.1. If it is not found, DOES NOTHING. This should not happen after <see cref="ConnectorManager.RestoreConnectivityAfterLoading"/>,
        ///                but could happen after a change in the geometry - see methods <see cref="ConnectedGeometryManager.PassGeometryAdditionToConnectors(BaseGeometry)"/>
        ///                and <see cref="ConnectedGeometryManager.PassGeometryDeletionToConnectors(BaseGeometry)"/> calling <see cref="RestoreConnectorHierarchyFor"/>.
        ///         1.1.2. If the volume is found, looks for its volume descriptor: <see cref="SynchronizeHierarchy(ConnectorToComplex, Volume, bool)"/>
        ///             1.1.2.1 If the VOLUME descriptor is found, it is added to the <see cref="ConnectorToBaseGeometry.GeometryBasedChildren"/> property of the parent space descriptor.
        ///             1.1.2.2 Otherwise, the corresponding component is found or created, the volume descriptor is created and added to the space descriptor as a child.
        ///         1.1.3. Looks for all top-level faces of the volume. For each of them does the following:
        ///             1.1.3.1. If the FACE descriptor is found, it is added to the <see cref="ConnectorToBaseGeometry.GeometryBasedChildren"/> property of the parent space descriptor.
        ///             1.1.3.2. Otherwise, the corresponding component is found or created, the face descriptor is created and added to the space descriptor as a child.
        ///             1.1.3.3. The same procedure is performed for all nested faces and holes.
        ///         1.1.4. Edges and vertices are not handled (yet).
        /// </remarks>
        public void RestoreConnectorHierarchy(bool _update_refs, bool _update_parameters)
        {
            List<ConnectorToComplex> space_descriptors = this.connectors.OfType<ConnectorToComplex>().ToList();
            foreach (ConnectorToComplex sd in space_descriptors)
            {
                Volume space = this.manager_of_all.GeometryManager.GetGeometryFromId(sd.TargetModelIndex, sd.TargetId) as Volume;
                if (space == null)
                {
                    // the connector has lost its function -> delete it
                    // this.RemoveConnector(sd, null);
                    continue;
                }

                // the 3d descriptor
                this.SynchronizeHierarchy(sd, space, _update_parameters);

                // the 2d descriptors
                Dictionary<ulong, Face> sds_2d = ConnectorAlgorithms.GetAllFacesWoSubFaces(space);
                foreach (var entry in sds_2d)
                {
                    this.SynchronizeHierarchy(sd, space, entry.Value, _update_refs, _update_parameters);
                }

                var comps_2d = sd.DescriptiveSource.Components.Where(c => c.Component.InstanceType == SimInstanceType.GeometricSurface);
                foreach (SimComponent c in comps_2d.Select(x => x.Component))
                {
                    SimInstancePlacementGeometry firstGeometryPlacement = null;
                    if (c.Instances.Any())
                        firstGeometryPlacement = (SimInstancePlacementGeometry)c.Instances[0].Placements.FirstOrDefault(x => x is SimInstancePlacementGeometry);

                    if (firstGeometryPlacement != null && !sds_2d.ContainsKey(firstGeometryPlacement.GeometryId))
                    {
                        if (this.lookup_acc_to_component.ContainsKey(c))
                        {
                            var affected_connector = this.lookup_acc_to_component[c].FirstOrDefault(
                                x => x.TargetId == firstGeometryPlacement.GeometryId && x.TargetModelIndex == firstGeometryPlacement.FileId);
                            if (affected_connector != null)
                            {
                                affected_connector.ConnState |= ConnectorConnectionState.TARGET_GEOMETRY_NULL;
                                affected_connector.OnTargetIsBeingDeleted(null);
                            }
                        }
                        else
                        {
                            firstGeometryPlacement.State = SimInstancePlacementState.InstanceTargetMissing;
                            firstGeometryPlacement.Instance.State = new SimInstanceState(firstGeometryPlacement.Instance.State.IsRealized,
                                SimInstanceConnectionState.GeometryDeleted);
                        }
                    }
                }
            }
        }

        internal void RestoreConnectorHierarchyFor(Volume _v, bool _update_refs, bool _update_parameters)
        {
            IEnumerable<ConnectorToComplex> sds = this.GetSpaceDescriptorOf(_v);
            if (sds != null)
            {
                foreach (ConnectorToComplex sd in sds)
                {
                    // the 3d descriptor
                    this.SynchronizeHierarchy(sd, _v, _update_parameters);

                    //Find removed faces

                    // the 2d descriptors
                    Dictionary<ulong, Face> sds_2d = ConnectorAlgorithms.GetAllFacesWoSubFaces(_v);
                    foreach (var entry in sds_2d)
                    {
                        this.SynchronizeHierarchy(sd, _v, entry.Value, _update_refs, _update_parameters);
                    }
                }
            }
        }

        private void SynchronizeHierarchy(ConnectorToComplex _parent_space_descriptor, Volume _child_target, bool _update_parameters)
        {
            // look for the 3d descriptor
            IEnumerable<ConnectorToVolume> sds_3d = this.GetDescriptorOf(_child_target);
            List<ConnectorToVolume> sds_3d_list = (sds_3d == null) ? new List<ConnectorToVolume>() : sds_3d.ToList();
            if (sds_3d_list.Count == 0)
            {
                // create the connector!
                // look for the corresponding component
                SimComponent comp_3d = _parent_space_descriptor.DescriptiveSource.Components.
                    FirstOrDefault(c => c.Component != null && c.Component.InstanceType == SimInstanceType.GeometricVolume)?.Component;
                bool needNewComponent = comp_3d == null;
                if (needNewComponent)
                    comp_3d = ConnectorAlgorithms.AttachAutomaticSubRepresentation(this.manager_of_all.ProjectData.Components, _parent_space_descriptor.DescriptiveSource, _child_target.Name, 3);
                var sd_3d = this.manager_of_all.AssociateComponentWithVolume(_parent_space_descriptor, comp_3d, _child_target);

                if (needNewComponent)
                    sd_3d.SynchronizeSourceWTarget(_child_target);

                sds_3d_list.Add(sd_3d);
            }
            for (int i = 0; i < sds_3d_list.Count; i++)
            {
                var sd_3d = sds_3d_list[i];
                // attach the 3d descriptor
                if (sd_3d != null && !_parent_space_descriptor.GeometryBasedChildren.Contains(sd_3d))
                    _parent_space_descriptor.GeometryBasedChildren.Add(sd_3d);
            }
        }

        private void SynchronizeHierarchy(ConnectorToComplex _parent_space_descriptor, Volume _parent_volume, Face _child_target, bool _update_refs, bool _update_parameters)
        {
            // look for the 2d descriptor
            IEnumerable<ConnectorToFace> sds_2d = this.GetDescriptorOf(_child_target, _parent_volume);
            List<ConnectorToFace> sds_2d_list = (sds_2d == null) ? new List<ConnectorToFace>() : sds_2d.ToList();

            if (sds_2d_list.Count == 0)
            {
                // create the connector!
                // look for the corresponding component


                var comps_2d = _parent_space_descriptor.DescriptiveSource.Components.Where(
                    c => c.Component != null && c.Component.InstanceType == SimInstanceType.GeometricSurface);
                SimComponent comp_2d = comps_2d.FirstOrDefault(
                    x => x.Component.Instances.Any() &&
                         x.Component.Instances[0].Placements.Any(p => p is SimInstancePlacementGeometry gp && gp.GeometryId == _child_target.Id))?.Component;

                bool isNewComponent = comp_2d == null;

                if (isNewComponent)
                    comp_2d = ConnectorAlgorithms.AttachAutomaticSubRepresentation(this.manager_of_all.ProjectData.Components, _parent_space_descriptor.DescriptiveSource, _child_target.Name, 2);

                var sd_2d = this.manager_of_all.AssociateComponentWithFace(_parent_space_descriptor, comp_2d, _child_target);

                if (isNewComponent)
                    sd_2d.SynchronizeSourceWTarget(_child_target);

                if (_update_refs)
                    this.manager_of_all.ModifyReferencesBasedOnConnectivity(sd_2d, _child_target, true);
                sds_2d_list.Add(sd_2d);
            }

            for (int i = 0; i < sds_2d_list.Count; i++)
            {
                var sd_2d = sds_2d_list[i];
                // attach the 2d descriptor
                if (sd_2d != null && !_parent_space_descriptor.GeometryBasedChildren.Contains(sd_2d))
                    _parent_space_descriptor.GeometryBasedChildren.Add(sd_2d);

                // synchronize nested faces and holes
                if (sd_2d != null)
                    this.Synchronize2DHierarchy(sd_2d, _child_target, _update_refs, _update_parameters);
            }
        }

        private void SynchronizeHierarchy(ConnectorToFace _parent_face_descriptor, Face _parent_face, Face _child_target, bool _update_refs, bool _update_parameters)
        {
            // debug
            // look for the 2d descriptor
            ConnectorToFace sd_2d = this.GetDescriptorOf(_child_target, _parent_face_descriptor);
            if (sd_2d == null)
            {
                // create the connector!
                // look for the corresponding component or create it
                var comps_2d = _parent_face_descriptor.DescriptiveSource.Components.Where(
                    c => c.Component != null && c.Component.InstanceType == SimInstanceType.GeometricSurface);
                SimComponent comp_2d = comps_2d.FirstOrDefault(
                    x => x.Component.Instances.Count > 0 &&
                         x.Component.Instances[0].Placements.Any(p => p is SimInstancePlacementGeometry gp && gp.GeometryId == _child_target.Id))?.Component;

                var needNewComponent = comp_2d == null;
                if (needNewComponent)
                    comp_2d = ConnectorAlgorithms.AttachAutomaticSubRepresentation(this.manager_of_all.ProjectData.Components, _parent_face_descriptor.DescriptiveSource, _child_target.Name, 2);

                sd_2d = this.manager_of_all.AssociateComponentWithFace(_parent_face_descriptor, comp_2d, _child_target);

                if (_update_refs)
                    this.manager_of_all.ModifyReferencesBasedOnConnectivity(sd_2d, _child_target, true);

                // look for obsolete components and delete them (added 30.04.2019)
                SimComponent comp_2d_boundary = comps_2d.FirstOrDefault(
                    x => x.Component.Instances.Count > 0 &&
                         x.Component.Instances[0].Placements.Any(p => p is SimInstancePlacementGeometry pg && pg.GeometryId == _child_target.Boundary.Id))?.Component;
                if (comp_2d_boundary != null)
                {
                    ConnectorToEdgeLoop sd_1d = this.GetDescriptorOf(_child_target.Boundary, _parent_face_descriptor);
                    if (sd_1d != null)
                    {
                        if (_update_refs)
                            this.manager_of_all.ModifyReferencesBasedOnConnectivity(sd_1d, _child_target.Boundary, false);
                        if (_parent_face_descriptor.GeometryBasedChildren.Contains(sd_1d))
                            _parent_face_descriptor.GeometryBasedChildren.Remove(sd_1d);
                        this.RemoveConnector(sd_1d, _child_target.Boundary);
                    }

                    _parent_face_descriptor.DescriptiveSource.Components.Remove(comp_2d_boundary.ParentContainer);
                }
            }
            // attach the 2d descriptor
            if (sd_2d != null && !_parent_face_descriptor.GeometryBasedChildren.Contains(sd_2d))
                _parent_face_descriptor.GeometryBasedChildren.Add(sd_2d);

            // synchronize nested faces and holes
            if (sd_2d != null)
                this.Synchronize2DHierarchy(sd_2d, _child_target, _update_refs, _update_parameters);
        }

        private void SynchronizeHierarchy(ConnectorToFace _parent_face_descriptor, Face _parent_face, EdgeLoop _child_target, bool _update_parameters)
        {
            // look for the 2d descriptor
            ConnectorToEdgeLoop sd_2d = this.GetDescriptorOf(_child_target, _parent_face_descriptor);
            if (sd_2d == null)
            {
                // create the connector!
                // look for the corresponding component
                var comps_2d = _parent_face_descriptor.DescriptiveSource.Components.Where(
                    c => c.Component != null && c.Component.InstanceType == SimInstanceType.GeometricSurface);
                SimComponent comp_2d = comps_2d.FirstOrDefault(
                    x => x.Component.Instances.Count > 0 &&
                         x.Component.Instances[0].Placements.Any(p => p is SimInstancePlacementGeometry pg && pg.GeometryId == _child_target.Id))?.Component;

                var needNewComponent = comp_2d == null;
                if (needNewComponent)
                    comp_2d = ConnectorAlgorithms.AttachAutomaticSubRepresentation(this.manager_of_all.ProjectData.Components, _parent_face_descriptor.DescriptiveSource, _child_target.Name, 2);

                sd_2d = this.manager_of_all.AssociateComponentWithEdgeLoop(_parent_face_descriptor, comp_2d, _child_target);

                if (sd_2d != null && _update_parameters)
                    sd_2d.SynchronizeSourceWTarget(_child_target);
            }
            // attach the 2d descriptor
            if (sd_2d != null && !_parent_face_descriptor.GeometryBasedChildren.Contains(sd_2d))
                _parent_face_descriptor.GeometryBasedChildren.Add(sd_2d);

            // no nested faces in holes
        }


        /// <summary>
        /// Restores the hierarchy of connectors for a face, including all nested faces and holes.
        /// </summary>
        /// <param name="_conn_f">the connector to the given face</param>
        /// <param name="_f">the face</param>
        /// <param name="_update_refs">if true, updates geometry-based references</param>
        /// <param name="_update_parameters">if true, call the synchronization with the target geometry</param>
        private void Synchronize2DHierarchy(ConnectorToFace _conn_f, Face _f, bool _update_refs, bool _update_parameters)
        {
            if (_update_parameters)
                _conn_f.SynchronizeSourceWTarget(_f);

            // extract the contained elements (Faces and EdgeLoops)
            Dictionary<ulong, Face> contained_faces = ConnectorAlgorithms.GetAllContainedFacesIn(_f);
            Dictionary<ulong, EdgeLoop> contained_holes = ConnectorAlgorithms.GetAllHolesIn(_f);

            // traverse the geometry elements: sub-Faces
            foreach (var child_entry in contained_faces)
            {
                Face child_of_f = child_entry.Value;
                this.SynchronizeHierarchy(_conn_f, _f, child_of_f, _update_refs, _update_parameters);
            }

            // traverse the geometry elements: holes in the Face
            foreach (var child_entry in contained_holes)
            {
                EdgeLoop hole_in_f = child_entry.Value;
                this.SynchronizeHierarchy(_conn_f, _f, hole_in_f, _update_parameters);
            }

            // traverse the components to check for missing geometry
            var comps_2d = _conn_f.DescriptiveSource.Components.Where(c => c.Component != null && c.Component.InstanceType == SimInstanceType.GeometricSurface);
            foreach (SimComponent c in comps_2d.Select(x => x.Component))
            {
                if (c.Instances.Count == 1)
                {
                    var firstGeometryPlacement = (SimInstancePlacementGeometry)c.Instances[0].Placements.FirstOrDefault(x => x is SimInstancePlacementGeometry);

                    if (!contained_faces.ContainsKey(firstGeometryPlacement.GeometryId) &&
                        !contained_holes.ContainsKey(firstGeometryPlacement.GeometryId))
                    {
                        if (this.lookup_acc_to_component.ContainsKey(c))
                        {
                            var affected_connector = this.lookup_acc_to_component[c].FirstOrDefault(
                                x => x.TargetId == firstGeometryPlacement.GeometryId && x.TargetModelIndex == firstGeometryPlacement.FileId);
                            if (affected_connector != null)
                            {
                                // look for existing related geometry, like the edge loop of the face that is missing
                                bool deletion_admissible = false;
                                List<ulong> related_geometry = firstGeometryPlacement.RelatedIds;
                                if (related_geometry != null)
                                    deletion_admissible = related_geometry.Any(x => contained_faces.ContainsKey(x) || contained_holes.ContainsKey(x));

                                if (deletion_admissible)
                                {
                                    if (this.to_delete_deferred == null)
                                        this.to_delete_deferred = new List<(ConnectorToFace, ConnectorBase, SimComponent)>();
                                    this.to_delete_deferred.Add((_conn_f, affected_connector, c));
                                }
                                else
                                {
                                    affected_connector.ConnState |= ConnectorConnectionState.TARGET_GEOMETRY_NULL;
                                    affected_connector.OnTargetIsBeingDeleted(null);
                                }
                            }
                        }
                        else
                        {
                            firstGeometryPlacement.Instance.State = new SimInstanceState(firstGeometryPlacement.Instance.State.IsRealized,
                                SimInstanceConnectionState.GeometryDeleted);
                            firstGeometryPlacement.State = SimInstancePlacementState.InstanceTargetMissing;
                        }
                    }
                }
            }
        }

        // parent - child connector tuples
        private List<(ConnectorToFace, ConnectorBase, SimComponent)> to_delete_deferred = new List<(ConnectorToFace, ConnectorBase, SimComponent)>();

        internal void PerformDeferredConnectorRemoval()
        {
            if (this.to_delete_deferred != default)
            {
                int nr_c = this.to_delete_deferred.Count;
                for (int i = nr_c - 1; i >= 0; i--)
                {
                    var parent_c = this.to_delete_deferred[i].Item1;
                    var child_c = this.to_delete_deferred[i].Item2;
                    var c = this.to_delete_deferred[i].Item3;
                    if (child_c is ConnectorToBaseGeometry && parent_c.GeometryBasedChildren.Contains(child_c))
                        parent_c.GeometryBasedChildren.Remove(child_c as ConnectorToBaseGeometry);
                    this.RemoveConnector(child_c, null);
                    if (c.Factory != null)
                    {
                        if (c.ParentContainer == null)
                            c.Factory.Remove(c);
                        else
                            c.Parent.Components.Remove(c.ParentContainer);
                    }
                }
            }
            this.to_delete_deferred = new List<(ConnectorToFace, ConnectorBase, SimComponent)>();
        }

        #endregion

        #region UTILS: Query look-up tables


        internal bool HasConnectorsToGeometry(ulong _geom_id)
        {
            return this.lookup_acc_to_geometry.ContainsKey(_geom_id);
        }

        internal List<ConnectorBase> RetrieveConnectorsToGeometry(ulong _geom_id)
        {
            if (!this.lookup_acc_to_geometry.ContainsKey(_geom_id))
                return new List<ConnectorBase>();
            return this.lookup_acc_to_geometry[_geom_id];
        }

        internal bool HasConnectorsToComponent(SimComponent _comp, int _model_index)
        {
            if (this.lookup_acc_to_component.TryGetValue(_comp, out var connector))
            {
                return connector.Any(x => x.TargetModelIndex == _model_index);
            }
            return false;
        }

        internal ConnectorBase RetrieveConnectorToComponent(SimComponent _comp, int _model_index)
        {
            if (this.lookup_acc_to_component.TryGetValue(_comp, out var connector))
                return connector.FirstOrDefault(x => x.TargetModelIndex == _model_index);
            else
                return null;
        }

        #endregion

        #region UTILS: Updating connection state of the dependent connectors

        /// <summary>
        /// Does not attach event subscriptions to the geometry objects in the lookup table.
        /// Instead, it alerts the connectors, pointing to the geometry objects. The event subscriptions are added
        /// when the connectors are updated(?). To be called when re-connecting a geometry model.
        /// </summary>
        /// <param name="_table_key">key of the table to be (re)connected</param>
        internal void ReconnectGeometryInLookupTable(string _table_key)
        {
            this.ModifyConnectionToGeometry(_table_key, true, true);
        }

        /// <summary>
        /// Alerts the connectors with targets in the corresponding lookup table
        /// that the geometry was just (dis-/re-)connected.
        /// </summary>
        /// <param name="_table_key">the key of the geometry lookup table</param>
        /// <param name="_connect">if true, connects; if false - disconnects</param>
        /// <param name="_notify_source">if true, the action is propagated to the source components</param>
        private void ModifyConnectionToGeometry(string _table_key, bool _connect, bool _notify_source)
        {
            Dictionary<ulong, BaseGeometry> table = this.manager_of_all.GeometryManager.RetrieveUpdatedTable(_table_key);
            if (table.Count == 0) return;

            foreach (var entry in table)
            {
                BaseGeometry geometry = entry.Value;
                if (this.lookup_acc_to_geometry.ContainsKey(geometry.Id))
                {
                    List<ConnectorBase> affected_connectors = this.lookup_acc_to_geometry[geometry.Id];
                    foreach (ConnectorBase c in affected_connectors)
                    {
                        if (_connect)
                            c.ConnState &= ~ConnectorConnectionState.TARGET_GEOMETRY_NULL;
                        else
                        {
                            c.ConnState |= ConnectorConnectionState.TARGET_GEOMETRY_NULL;
                            if (_notify_source)
                                c.OnTargetIsBeingDeleted(geometry);
                        }
                    }
                }
            }
        }

        #endregion

        #region UTILS: Query connectors

        /// <summary>
        /// Returns a list of associated components for a geometry. This includes the descriptive and prescriptive
        /// component connections. Not included are associations (e.g. the lamp associated with a volume) or parameterizing
        /// associations (e.g. value field carrying components associated with entire buildings).
        /// </summary>
        /// <param name="geometry">The geometry</param>
        /// <returns>A list of components attached to the geometry</returns>
        internal IEnumerable<SimComponent> RetrieveComponents(BaseGeometry geometry)
        {
            if (this.lookup_acc_to_geometry.ContainsKey(geometry.Id))
            {
                var collection_1 = this.lookup_acc_to_geometry[geometry.Id].Where(x => x is ConnectorToBaseGeometry)
                                                               .Select(x => (x as ConnectorToBaseGeometry).DescriptiveSource);
                var collection_2 = this.lookup_acc_to_geometry[geometry.Id].Where(x => x is InstanceConnectorToBaseGeometry)
                                                               .Select(x => (x as InstanceConnectorToBaseGeometry).SourceParent);
                var collection_3 = this.lookup_acc_to_geometry[geometry.Id].Where(x => x is ConnectorPrescriptiveToFace)
                                                               .Select(x => (x as ConnectorPrescriptiveToFace).PrescriptiveSource);
                return collection_1.Concat(collection_2).Concat(collection_3);
            }
            return new SimComponent[] { };
        }

        /// <summary>
        /// Looks for the connectors with target the given face and source a Geometric Relationship
        /// of a component of type ALIGNED_WITH. If none is found, it returns Null.
        /// </summary>
        /// <param name="_face">the target face</param>
        /// <returns>the connector containing the Geometric Relationship associated with the face, or Null</returns>
        internal InstanceConnectorToBaseGeometry GetAlignedWith(Face _face)
        {
            if (!this.lookup_acc_to_geometry.ContainsKey(_face.Id)) return null;

            var conns = this.lookup_acc_to_geometry[_face.Id];

            var wall_cstr = conns.OfType<InstanceConnectorToBaseGeometry>()
                                 .FirstOrDefault(x => x.Source.InstanceType == SimInstanceType.Attributes2D &&
                                                      x.Source.Placements.Any(p => p is SimInstancePlacementGeometry pg && pg.GeometryId == _face.Id));

            return wall_cstr;
        }

        /// <summary>
        /// Looks for the connectors with target the given face and source a component of type DESCRIBES_2DorLESS
        /// within the context of a component of type DESCRIBES connected to the given volume as a ConnectorToComplex.
        /// If none is found, it returns Null.
        /// </summary>
        /// <param name="_face">the face whose descriptor we are looking for</param>
        /// <param name="_context_volume">the volume within whose descriptor to look for the face connector</param>
        /// <returns>the connectors with the given face as a traget and a source component of type DESCRIBES_2DorLESS, or Null</returns>
        internal List<ConnectorToFace> GetDescriptorOf(Face _face, Volume _context_volume)
        {
            if (!this.lookup_acc_to_geometry.ContainsKey(_face.Id)) return null;

            IEnumerable<ConnectorToComplex> context_descriptors = this.GetSpaceDescriptorOf(_context_volume);
            if (context_descriptors == null) return null;

            List<ConnectorToFace> found_face_reps = new List<ConnectorToFace>();
            foreach (ConnectorToComplex context_descriptor in context_descriptors)
            {
                if (context_descriptor.DescriptiveSource == null) continue;

                var conns = this.lookup_acc_to_geometry[_face.Id];
                var face_rep = conns.Where(x => x is ConnectorToFace)
                                    .Select(x => x as ConnectorToFace)
                                    .Where(x => x.DescriptiveSource.IsDirectOrIndirectChildOf(context_descriptor.DescriptiveSource))
                                    .FirstOrDefault();
                if (face_rep != null)
                    found_face_reps.Add(face_rep);
            }
            return found_face_reps;
        }

        /// <summary>
        /// Looks for the connectors with target the given face and source a component of type <see cref="SimInstanceType.GeometricSurface"/>
        /// within the context of the connector associating a parent component with a parent face. If none is found, it returns Null.
        /// </summary>
        /// <param name="_face">the face whose descriptor we are looking for</param>
        /// <param name="_context_face_descriptor">the parent face descriptor</param>
        /// <returns>a connector with the given face as a traget and a source component of type DESCRIBES_2DorLESS, or Null</returns>
        internal ConnectorToFace GetDescriptorOf(Face _face, ConnectorToFace _context_face_descriptor)
        {
            if (!this.lookup_acc_to_geometry.ContainsKey(_face.Id)) return null;
            if (_context_face_descriptor == null) return null;
            if (_context_face_descriptor.DescriptiveSource == null) return null;

            var conns = this.lookup_acc_to_geometry[_face.Id];
            var face_rep = conns.Where(x => x is ConnectorToFace)
                                .Select(x => x as ConnectorToFace)
                                .Where(x => x.DescriptiveSource.IsDirectOrIndirectChildOf(_context_face_descriptor.DescriptiveSource))
                                .FirstOrDefault();
            return face_rep;
        }

        /// <summary>
        /// Looks for the connectors with target the given edge loop and source a component of type <see cref="SimInstanceType.GeometricSurface"/>
        /// within the context of the connector associating a parent component with a parent face. If none is found, it returns Null.
        /// </summary>
        /// <param name="_loop">the edge loop whose descriptor we are looking for</param>
        /// <param name="_context_face_descriptor">the parent face descriptor</param>
        /// <returns>a connector with the given edge loop as a traget and a source component of type DESCRIBES_2DorLESS, or Null</returns>
        internal ConnectorToEdgeLoop GetDescriptorOf(EdgeLoop _loop, ConnectorToFace _context_face_descriptor)
        {
            if (!this.lookup_acc_to_geometry.ContainsKey(_loop.Id)) return null;
            if (_context_face_descriptor == null) return null;
            if (_context_face_descriptor.DescriptiveSource == null) return null;

            var conns = this.lookup_acc_to_geometry[_loop.Id];
            var loop_rep = conns.Where(x => x is ConnectorToEdgeLoop)
                                .Select(x => x as ConnectorToEdgeLoop)
                                .Where(x => x.DescriptiveSource.IsDirectOrIndirectChildOf(_context_face_descriptor.DescriptiveSource))
                                .FirstOrDefault();
            return loop_rep;
        }
        internal IEnumerable<ConnectorToEdgeLoop> GetDescriptorOf(EdgeLoop _loop)
        {
            if (!this.lookup_acc_to_geometry.ContainsKey(_loop.Id)) return Enumerable.Empty<ConnectorToEdgeLoop>();

            var conns = this.lookup_acc_to_geometry[_loop.Id];
            var loop_rep = conns.OfType<ConnectorToEdgeLoop>();
            return loop_rep;
        }

        /// <summary>
        /// Looks for the connectors with target the given face and source a component of type DESCRIBES_2DorLESS
        /// within the context of *any* volume.
        /// </summary>
        /// <param name="_face">the face whose descriptors we are looking for</param>
        /// <returns>a collection of connectors with the given face as a traget and a source component of type DESCRIBES_2DorLESS, can be emty but not Null</returns>
        internal IEnumerable<ConnectorToFace> GetAllDescriptorsOf(Face _face)
        {
            if (!this.lookup_acc_to_geometry.ContainsKey(_face.Id)) return new ConnectorToFace[0];

            var conns = this.lookup_acc_to_geometry[_face.Id];
            var face_reps = conns.Where(x => x is ConnectorToFace)
                                 .Select(x => x as ConnectorToFace);

            return face_reps;
        }

        /// <summary>
        /// Looks for the connectors with target the given volume and source a component of type DESCRIBES_3D.
        /// This is merely the representation of the volume, *not* the representation of the architectural space.
        /// If none is found, it returns Null.
        /// </summary>
        /// <param name="_volume">the volume whose descriptors we are looking for</param>
        /// <returns>a connector with the given volume as a traget and a source component of type DESCRIBES_3D, or Null</returns>
        internal IEnumerable<ConnectorToVolume> GetDescriptorOf(Volume _volume)
        {
            if (!this.lookup_acc_to_geometry.ContainsKey(_volume.Id)) return null;

            var conns = this.lookup_acc_to_geometry[_volume.Id];
            var volume_rep = conns.Where(x => x is ConnectorToVolume)
                                  .Select(x => x as ConnectorToVolume);
            //.FirstOrDefault();
            return volume_rep;
        }

        /// <summary>
        /// Looks for a connector with target the given volume and source a component of type DESCRIBES.
        /// This is the description of the *architectural space* represented by the volume.
        /// If none is found, it returns Null.
        /// </summary>
        /// <param name="_volume">the volume whose space descriptor we are looking for</param>
        /// <returns>a connector with the given volume as a traget and a source component of type DESCRIBES, or Null</returns>
        internal IEnumerable<ConnectorToComplex> GetSpaceDescriptorOf(Volume _volume)
        {
            if (!this.lookup_acc_to_geometry.ContainsKey(_volume.Id)) return null;

            var conns = this.lookup_acc_to_geometry[_volume.Id];
            var volume_desc = conns.Where(x => x is ConnectorToComplex)
                                   .Select(x => x as ConnectorToComplex);
            //.FirstOrDefault();
            return volume_desc;
        }

        /// <summary>
        /// Retrieves the instance prescriptive connectors.
        /// </summary>
        /// <returns>A collection of all prescriptive connectors, can be empty but not Null</returns>
        internal IEnumerable<ConnectorPrescriptiveToFace> GetAllFacePrescriptors()
        {
            return this.connectors.Where(x => x is ConnectorPrescriptiveToFace).Select(x => x as ConnectorPrescriptiveToFace);
        }

        /// <summary>
        /// Gets the instance connector prescribing to the given face.
        /// </summary>
        /// <param name="_face">the face</param>
        /// <returns>the instance connector managing the communication with this face, or Null</returns>
        internal InstanceConnectorToFace GetPrescriptorOf(Face _face)
        {
            if (!this.lookup_acc_to_geometry.ContainsKey(_face.Id)) return null;

            var conns = this.lookup_acc_to_geometry[_face.Id];
            var face_prescr = conns.Where(x => x is InstanceConnectorToFace)
                                   .Select(x => x as InstanceConnectorToFace)
                                   .FirstOrDefault();
            return face_prescr;
        }

        #endregion

        #region UTILS: Query connector relationships

        /// <summary>
        /// Finds the neighbors of the given volume. Those are other volumes that share at least one Face
        /// with the given volume. Since the geometry data model allows a maximum of 2 volumes to share a face,
        /// each face of the given volume can provide at most one neighbor. The method returns a list of 
        /// connector pairs: the key's source component has to reference the value's source component.
        /// </summary>
        /// <param name="_volume">the volume whose neighbors we are looking for</param>
        /// <returns>a list of connector pairs: the key's source component has to reference the value's source component</returns>
        internal List<KeyValuePair<ConnectorToBaseGeometry, ConnectorToBaseGeometry>> GetNeighborBasedConnectorReferencePairs(Volume _volume)
        {
            List<KeyValuePair<ConnectorToBaseGeometry, ConnectorToBaseGeometry>> reference_pairs = new List<KeyValuePair<ConnectorToBaseGeometry, ConnectorToBaseGeometry>>();
            if (!this.lookup_acc_to_geometry.ContainsKey(_volume.Id)) return reference_pairs;

            ConnectorToComplex volume_descriptor = (ConnectorToComplex)this.lookup_acc_to_geometry[_volume.Id].FirstOrDefault(x => x is ConnectorToComplex);
            if (volume_descriptor == null) return reference_pairs;

            Dictionary<Face, Volume> geometric_neigbors = ConnectorAlgorithms.GetNeighborsOf(_volume);
            if (geometric_neigbors.Count == 0) return reference_pairs;

            foreach (var entry in geometric_neigbors)
            {
                Face f = entry.Key;
                Volume f_neighbor = entry.Value;

                // the volume volume reference pairs:
                IEnumerable<ConnectorToComplex> f_neighbor_descrs = this.GetSpaceDescriptorOf(f_neighbor);
                if (f_neighbor_descrs != null)
                {
                    foreach (var f_neighbor_descr in f_neighbor_descrs)
                    {
                        reference_pairs.Add(new KeyValuePair<ConnectorToBaseGeometry, ConnectorToBaseGeometry>(volume_descriptor, f_neighbor_descr));
                        reference_pairs.Add(new KeyValuePair<ConnectorToBaseGeometry, ConnectorToBaseGeometry>(f_neighbor_descr, volume_descriptor));
                    }
                }

                // the face volume reference pairs:
                IEnumerable<ConnectorToFace> f_in_neighbors = this.GetDescriptorOf(f, f_neighbor);
                if (f_in_neighbors != null)
                {
                    foreach (var f_in_neighbor in f_in_neighbors)
                        reference_pairs.Add(new KeyValuePair<ConnectorToBaseGeometry, ConnectorToBaseGeometry>(f_in_neighbor, volume_descriptor));
                }

                IEnumerable<ConnectorToFace> fs_in_caller = this.GetDescriptorOf(f, _volume);
                if (fs_in_caller != null && f_neighbor_descrs != null)
                {
                    foreach (var f_in_caller in fs_in_caller)
                    {
                        foreach (var f_neighbor_descr in f_neighbor_descrs)
                        {
                            reference_pairs.Add(new KeyValuePair<ConnectorToBaseGeometry, ConnectorToBaseGeometry>(f_in_caller, f_neighbor_descr));
                        }
                    }
                }
            }

            return reference_pairs;
        }

        /// <summary>
        /// Looks for the the descriptors of the given face and for a wall construction (prescriptor) applied to the face.
        /// If a prescriptor is found, returns a list of connector pairs: the key's source component has to reference the value's source component (the prescriptor).
        /// </summary>
        /// <param name="_face"></param>
        /// <returns>a list of connector pairs: key = face descriptor, value = the one face prescriptor</returns>
        internal List<KeyValuePair<ConnectorToFace, ConnectorPrescriptiveToFace>> GetFaceBasedConnectorReferencePairs(Face _face)
        {
            List<KeyValuePair<ConnectorToFace, ConnectorPrescriptiveToFace>> reference_pairs = new List<KeyValuePair<ConnectorToFace, ConnectorPrescriptiveToFace>>();
            if (!this.lookup_acc_to_geometry.ContainsKey(_face.Id)) return reference_pairs;

            IEnumerable<ConnectorToFace> all_descriptors_of_f = this.GetAllDescriptorsOf(_face);
            if (all_descriptors_of_f == null || all_descriptors_of_f.Count() == 0) return reference_pairs;

            InstanceConnectorToBaseGeometry prescriptor_i = this.GetAlignedWith(_face);
            if (prescriptor_i == null || prescriptor_i.ParentConnector == null || !(prescriptor_i.ParentConnector is ConnectorPrescriptiveToFace)) return reference_pairs;
            ConnectorPrescriptiveToFace prescriptor = prescriptor_i.ParentConnector as ConnectorPrescriptiveToFace;

            foreach (var entry in all_descriptors_of_f)
            {
                reference_pairs.Add(new KeyValuePair<ConnectorToFace, ConnectorPrescriptiveToFace>(entry, prescriptor));
            }
            return reference_pairs;
        }

        /// <summary>
        /// Looks for the faces to which the prescriptive connector with source component of type ALIGNED_WITH is applied to.
        /// Creates connector reference pairs: key = each affected face's descriptor, value = the prescriptor 
        /// </summary>
        /// <param name="_pcon">the perscriptive connector</param>
        /// <returns>a list of connector pairs: key = each affected face's descriptor, value = the prescriptor</returns>
        internal List<KeyValuePair<ConnectorToFace, ConnectorPrescriptiveToFace>> GetAlignmentBasedConnectorReferencePairs(ConnectorPrescriptiveToFace _pcon)
        {
            List<KeyValuePair<ConnectorToFace, ConnectorPrescriptiveToFace>> reference_pairs = new List<KeyValuePair<ConnectorToFace, ConnectorPrescriptiveToFace>>();
            if (_pcon == null) return reference_pairs;

            var geometryPlacements = _pcon.PrescriptiveSource.Instances.SelectMany(x => x.Placements.OfType<SimInstancePlacementGeometry>());
            foreach (var placement in geometryPlacements)
            {
                var face = this.manager_of_all.GeometryManager.GetGeometryFromId(placement.FileId, placement.GeometryId) as Face;

                if (face != null)
                {
                    var face_descriptors = this.GetAllDescriptorsOf(face);
                    foreach (var fc in face_descriptors)
                        reference_pairs.Add(new KeyValuePair<ConnectorToFace, ConnectorPrescriptiveToFace>(fc, _pcon));
                }
            }

            return reference_pairs;
        }

        #endregion

        #region EVENT HANDLERS: geometry

        /// <summary>
        /// Finds all components connected to the changed geometry and trigger the synchronization.
        /// NOTE: The synchronization should not be recursive for the components!!!
        /// NOTE2: The synchronization is off as of 19.03.2019, as it happens anyway in <see cref="ConnectedGeometryManager.UpdateOffsetsAfterUpdate(OffsetModel)"/>.
        /// </summary>
        /// <param name="sender">the changed geometry</param>
        internal void base_geometry_GeometryChanged(object sender)
        {
            //BaseGeometry geom = sender as BaseGeometry;
            //if (geom == null) return;

            //ulong geom_id = geom.Id;
            //if (this.lookup_acc_to_geometry.ContainsKey(geom_id))
            //{
            //    List<ConnectorBase> connectors = this.lookup_acc_to_geometry[geom_id];
            //    connectors.ForEach(c => c.SynchronizeSourceWTarget(geom));
            //}
            //// this.EventTriggered?.Invoke(this, "c1", geom.Id.ToString()); // OLD
            //this.manager_of_all.OnEventTriggered("c1", geom.Id.ToString()); // NEW
        }

        #endregion

        #region EVENT HANDLERS: connectors

        private void connector_TargetIsBeingDeleted(object sender, BaseGeometry target)
        {
            ConnectorBase connector = sender as ConnectorBase;
            if (connector == null) return;

            // this.UpDateConnectorLookUpTables(connector, ConnectionChange.TARGET_DELETED);
            this.manager_of_all.ModifyReferencesBasedOnConnectivity(connector, target, false);
        }

        private void connector_SourceIsBeingDeleted(object sender)
        {
            ConnectorBase connector = sender as ConnectorBase;
            if (connector == null) return;

            this.UpDateConnectorLookUpTables(connector, ConnectionChange.SOURCE_DELETED);
        }

        #endregion

        #region RESET

        internal void Reset()
        {
            // manager_of_all remains the same
            this.connectors.Clear();
            this.lookup_acc_to_component.Clear();
            this.lookup_acc_to_instance.Clear();
            this.lookup_acc_to_geometry.Clear();
        }

        #endregion
    }
}
