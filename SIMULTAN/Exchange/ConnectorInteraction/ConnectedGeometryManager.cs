using SIMULTAN.Data.Geometry;
using SIMULTAN.Exchange.Connectors;
using SIMULTAN.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace SIMULTAN.Exchange.ConnectorInteraction
{
    /// <summary>
    /// Extracts information and reacts to events within the connected geometry models.
    /// </summary>
    internal class ConnectedGeometryManager
    {
        #region FIELDS

        // context
        private ComponentGeometryExchange geometryExchange;

        // geometry look-up tables
        private Dictionary<string, Dictionary<ulong, BaseGeometry>> geometry_lookup_tables;

        // geometry delete undo
        private List<ulong> recently_deleted_ids;

        private Dispatcher dispatcher;

        // deferred updates
        private DispatcherTimer offset_model_handler_delay_timer;
        private List<OffsetModel> delayed_offset_models;

        private DispatcherTimer batch_op_handler_delay_timer;
        private GeometryModelData model_to_update_after_batch_op;

        #endregion

        #region .CTOR

        /// <summary>
        /// Initializes the lookup tables for geometry models and the geometry objects they contain.
        /// Attaches event handlers to each model and to each CONNECTED geometric object.
        /// </summary>
        /// <param name="_manager_of_all">the parent manager that holds the component and the connector management</param>
        public ConnectedGeometryManager(ComponentGeometryExchange _manager_of_all)
        {
            // context
            this.geometryExchange = _manager_of_all;

            // initialize the geometry management and look-up tables
            this.geometry_lookup_tables = new Dictionary<string, Dictionary<ulong, BaseGeometry>>();

            // initialize the delete / undelete list
            this.recently_deleted_ids = new List<ulong>();

            this.dispatcher = Dispatcher.CurrentDispatcher;
            this.offset_model_handler_delay_timer = new DispatcherTimer();
            this.offset_model_handler_delay_timer.Interval = new TimeSpan(0, 0, 1);
            this.offset_model_handler_delay_timer.Tick += new EventHandler(OnOffsetChangedEventDelayTimerTick);
            this.delayed_offset_models = new List<OffsetModel>();

            this.batch_op_handler_delay_timer = new DispatcherTimer();
            this.batch_op_handler_delay_timer.Interval = new TimeSpan(0, 0, 0, 0, 500);
            this.batch_op_handler_delay_timer.Tick += new EventHandler(OnBatchOpEventDelayTimerTick);
        }



        #endregion

        #region UTILS: Query geometry independently

        /// <summary>
        /// Returns the first instance with the id _geom_id found in the geometry lookup tables.
        /// It could cause the loading of not yet used lookup tables. 
        /// </summary>
        /// <param name="_index_of_geometry_model">the index of the <see cref="GeometryModelData"/> where the geometry instance resides</param>
        /// <param name="_geom_id">the id of the instance we are looking for</param>
        /// <returns>found instance</returns>
        public BaseGeometry GetGeometryFromId(int _index_of_geometry_model, ulong _geom_id)
        {
            // get the path to the model
            string model_path = this.geometryExchange.ProjectData.AssetManager.GetPath(_index_of_geometry_model);
            if (!(this.geometry_lookup_tables.ContainsKey(model_path))) return null;

            // look in already created lookup tables
            if (this.geometry_lookup_tables[model_path].ContainsKey(_geom_id))
                return this.geometry_lookup_tables[model_path][_geom_id];

            // if not found yet, create the missing lookup tables and look in them
            if (this.geometry_lookup_tables[model_path].Count == 0)
            {
                if (geometryExchange.ProjectData.GeometryModels.TryGetGeometryModel(new FileInfo(model_path), out var model))
                {
                    this.geometry_lookup_tables[model_path] = ConnectedGeometryManager.CreateLookupTable(model.Geometry);
                    this.geometryExchange.ConnectorManager.ReconnectGeometryInLookupTable(model_path);
                    if (this.geometry_lookup_tables[model_path].ContainsKey(_geom_id))
                        return this.geometry_lookup_tables[model_path][_geom_id];
                }
            }

            // not found...
            return null;
        }

        internal IEnumerable<Volume> RetrieveVolumesInFile(string _full_path_to_file)
        {
            return this.geometry_lookup_tables[_full_path_to_file].Where(x => x.Value is Volume).Select(x => x.Value as Volume);
        }

        #endregion

        #region UTILS: Updating geometry lookup tables 

        /// <summary>
        /// Creates a lookup table for the geometric instances contained in the model.
        /// Thier keys are used as keys in the table.
        /// </summary>
        /// <param name="_model">the container of the geometric instances</param>
        /// <returns>a lookup table with keys of type ulong</returns>
        private static Dictionary<ulong, BaseGeometry> CreateLookupTable(GeometryModelData _model)
        {
            Dictionary<ulong, BaseGeometry> lookup_table = new Dictionary<ulong, BaseGeometry>();
            if (_model == null) return lookup_table;

            foreach (var v in _model.Vertices)
            {
                lookup_table.Add(v.Id, v);
            }
            foreach (var e in _model.Edges)
            {
                lookup_table.Add(e.Id, e);
            }
            foreach (var el in _model.EdgeLoops)
            {
                lookup_table.Add(el.Id, el);
            }
            foreach (var pl in _model.Polylines)
            {
                lookup_table.Add(pl.Id, pl);
            }
            foreach (var f in _model.Faces)
            {
                lookup_table.Add(f.Id, f);
            }
            foreach (var v in _model.Volumes)
            {
                lookup_table.Add(v.Id, v);
            }

            return lookup_table;
        }

        internal Dictionary<ulong, BaseGeometry> RetrieveUpdatedTable(string _table_key)
        {
            if (!(this.geometry_lookup_tables.ContainsKey(_table_key))) return new Dictionary<ulong, BaseGeometry>();

            if (this.geometry_lookup_tables[_table_key].Count == 0)
            {
                if (geometryExchange.ProjectData.GeometryModels.TryGetGeometryModel(new FileInfo(_table_key), out var modelData, false))
                    this.geometry_lookup_tables[_table_key] = ConnectedGeometryManager.CreateLookupTable(modelData.Geometry);
            }
            return this.geometry_lookup_tables[_table_key];
        }

        #endregion

        #region METHODS: handling changes in geometry models

        internal void AttachGeometryModel(GeometryModelData _model, bool _update_refs, bool updateParameters)
        {
            if (_model != null)
            {
                // (re-)attach the event handlers
                _model.GeometryAdded -= model_GeometryAdded;
                _model.GeometryAdded += model_GeometryAdded;

                _model.GeometryRemoved -= model_GeometryRemoved;
                _model.GeometryRemoved += model_GeometryRemoved;

                _model.TopologyChanged -= model_TopologyChanged;
                _model.TopologyChanged += model_TopologyChanged;

                _model.OperationFinished -= model_OperationFinished;
                _model.OperationFinished += model_OperationFinished;

                _model.BatchOperationFinished -= model_BatchOperationFinished;
                _model.BatchOperationFinished += model_BatchOperationFinished;

                _model.OffsetModel.OffsetSurfaceChanged -= model_OffsetModel_OffsetSurfaceChanged;
                _model.OffsetModel.OffsetSurfaceChanged += model_OffsetModel_OffsetSurfaceChanged;

                // the geometric object lookup tables are just initialized here; they are filled ON DEMAND
                this.geometry_lookup_tables.Add(_model.Model.File.FullName, new Dictionary<ulong, BaseGeometry>());
                this.geometryExchange.ConnectorManager.ReconnectGeometryInLookupTable(_model.Model.File.FullName);
            }
            // 08.02.2019 - transferred from ComponentGeometryExchange.AddGeometryModel
            this.geometryExchange.ConnectorManager.RestoreConnectivityAfterLoading(updateParameters);
            this.geometryExchange.ConnectorManager.RestoreConnectorHierarchy(_update_refs, updateParameters);
            if (_update_refs)
                this.geometryExchange.ModifyAllReferencesBasedOnConnectivity(true);
        }

        internal void DetachGeometryModel(GeometryModel _model, bool _notify)
        {
            if (_model == null)
                return;

            string model_key = _model.File.FullName;

            if (_notify)
                this.geometryExchange.ModifyAllReferencesBasedOnConnectivity(false);

            _model.Geometry.GeometryAdded -= model_GeometryAdded;
            _model.Geometry.GeometryRemoved -= model_GeometryRemoved;
            _model.Geometry.TopologyChanged -= model_TopologyChanged;
            _model.Geometry.OperationFinished -= model_OperationFinished;
            _model.Geometry.BatchOperationFinished -= model_BatchOperationFinished;
            _model.Geometry.OffsetModel.OffsetSurfaceChanged -= model_OffsetModel_OffsetSurfaceChanged;

            // 29.01.2019: switched the lower two commands!                    
            // this.manager_of_all.ConnectorManager.DisconnectGeometryInLookupTable(model_key, _notify); // do not disconnect when the model is removed
            if (model_to_update_after_batch_op != null)
            {
                this.OnBatchOpEventDelayTimerTick(null, EventArgs.Empty);
            }
            this.geometry_lookup_tables.Remove(model_key);
        }

        internal void TransferObservations(GeometryModelData source, GeometryModelData target)
        {
            if (source == null || target == null) return;

            this.DetachGeometryModel(source.Model, true);
            this.AttachGeometryModel(target, true, true);
        }

        internal void AttachGeometryModelOnlyToLookup(GeometryModel _model)
        {
            // the geometric object lookup tables are just initialized here; they are filled ON DEMAND
            this.geometry_lookup_tables.Add(_model.File.FullName, new Dictionary<ulong, BaseGeometry>());
        }

        #endregion

        #region EVENT HANDLERS: geometry models and geometry

        /// <summary>
        /// Adds the newly added geometric instances to the corresponding lookup table,
        /// If the table is empty, it gets populated. Otherwise the new geometry is 
        /// simply added to it.
        /// </summary>
        /// <param name="sender">the model containing the geometry</param>
        /// <param name="geometry">the added geometry instances</param>
        private void model_GeometryAdded(object sender, IEnumerable<BaseGeometry> geometry)
        {
            this.dispatcher.Invoke(() =>
            {
                GeometryModelData model = sender as GeometryModelData;
                if (model == null || geometry == null) return;

                string model_key = model.Model.File.FullName;
                if (!(this.geometry_lookup_tables.ContainsKey(model_key))) return;

                bool add_new_geometry = true;
                if (this.geometry_lookup_tables[model_key].Count == 0)
                {
                    this.geometry_lookup_tables[model_key] = ConnectedGeometryManager.CreateLookupTable(model);
                    add_new_geometry = (this.geometry_lookup_tables[model_key].Count == 0);
                }

                if (add_new_geometry)
                {
                    foreach (BaseGeometry bg in geometry)
                    {
                        // 1. add geometry to look-up tables
                        if (this.geometry_lookup_tables[model_key].Count > 0)
                        {
                            if (!this.geometry_lookup_tables[model_key].ContainsKey(bg.Id))
                                this.geometry_lookup_tables[model_key].Add(bg.Id, bg);
                        }
                        // 2. communicate change
                        this.PassGeometryAdditionToConnectors(bg);
                    }
                }
            });
        }

        private void PassGeometryAdditionToConnectors(BaseGeometry _added)
        {
            // restore connections, if possible            
            if (this.geometryExchange.ConnectorManager.HasConnectorsToGeometry(_added.Id))
            {
                List<ConnectorBase> connectors = this.geometryExchange.ConnectorManager.RetrieveConnectorsToGeometry(_added.Id);
                foreach (var c in connectors)
                {
                    c.ConnState &= ~ConnectorConnectionState.TARGET_GEOMETRY_NULL;
                    this.geometryExchange.ModifyReferencesBasedOnConnectivity(c, _added, true);
                }
            }
            else
            {
                List<Volume> affected_volumes = new List<Volume>();

                if (_added is Face added_face)
                    affected_volumes = ConnectorAlgorithms.GetVolumesOf(added_face);
                else if (_added is EdgeLoop el)
                    affected_volumes = el.Faces.Where(x => x.Boundary != el).SelectMany(x => x.PFaces.Select(pf => pf.Volume)).Distinct().ToList();

                foreach (Volume v in affected_volumes)
                {
                    this.geometryExchange.ConnectorManager.RestoreConnectorHierarchyFor(v, true, true);
                }
            }

            this.recently_deleted_ids.Remove(_added.Id);
        }

        /// <summary>
        /// Updates the lookup tables after geometry changes (e.g., after batch operations within a model),
        /// if they are empty. The add and remove operations are handled in separate event handlers.
        /// </summary>
        /// <param name="sender">the model containing the changed geometry</param>
		/// <param name="geometry">List of geometries that have been modified</param>
        private void model_TopologyChanged(object sender, IEnumerable<BaseGeometry> geometry)
        {
            GeometryModelData model = sender as GeometryModelData;

            string model_key = model.Model.File.FullName;
            if (this.geometry_lookup_tables.ContainsKey(model_key))
            {
                if (this.geometry_lookup_tables[model_key].Count == 0)
                    this.geometry_lookup_tables[model_key] = ConnectedGeometryManager.CreateLookupTable(model);
            }
        }

        /// <summary>
        /// Removes the newly removed geometric instance from the corresponding lookup table.
        /// If the table is empty, it gets populated. Otherwise the removed geometry
        /// is removed from the table.
        /// Informs all connectors with the removed instances as targets.
        /// </summary>
        /// <param name="sender">the model containing the geometry</param>
        /// <param name="geometry">the removed geometry instances</param>
        private void model_GeometryRemoved(object sender, IEnumerable<BaseGeometry> geometry)
        {
            this.dispatcher.Invoke(() =>
            {
                GeometryModelData model = sender as GeometryModelData;
                if (model == null || geometry == null) return;

                string model_key = model.Model.File.FullName;
                if (!(this.geometry_lookup_tables.ContainsKey(model_key))) return;

                bool remove_old_geometry = true;
                if (this.geometry_lookup_tables[model_key].Count == 0)
                {
                    this.geometry_lookup_tables[model_key] = ConnectedGeometryManager.CreateLookupTable(model);
                    remove_old_geometry = false;
                }

                if (remove_old_geometry)
                {
                    foreach (BaseGeometry bg in geometry)
                    {
                        // 1. communicate change
                        this.PassGeometryDeletionToConnectors(bg);
                        // 2. delete from look-up later!
                        if (this.geometry_lookup_tables[model_key].Count > 0 &&
                        this.geometry_lookup_tables[model_key].ContainsKey(bg.Id))
                        {
                            this.geometry_lookup_tables[model_key].Remove(bg.Id);
                        }
                    }
                    this.geometryExchange.ConnectorManager.PerformDeferredConnectorRemoval();
                }
            });
        }

        private void PassGeometryDeletionToConnectors(BaseGeometry _deleted)
        {
            if (this.geometryExchange.ConnectorManager.HasConnectorsToGeometry(_deleted.Id))
            {
                List<ConnectorBase> connectors = this.geometryExchange.ConnectorManager.RetrieveConnectorsToGeometry(_deleted.Id);
                foreach (var c in connectors)
                {
                    c.ConnState |= ConnectorConnectionState.TARGET_GEOMETRY_NULL;
                    c.OnTargetIsBeingDeleted(_deleted); // references are handled here
                    // handle face descriptors
                    if (_deleted is Face)
                    {
                        Face deleted_face = _deleted as Face;
                        List<Volume> affected_volumes = ConnectorAlgorithms.GetVolumesOf(deleted_face);
                        foreach (Volume v in affected_volumes)
                        {
                            this.geometryExchange.ConnectorManager.RestoreConnectorHierarchyFor(v, true, true);
                        }
                    }
                }
            }
            this.recently_deleted_ids.Add(_deleted.Id);
            this.recently_deleted_ids = this.recently_deleted_ids.Distinct().ToList();
        }

        /// <summary>
        /// Calls the event handler for the GeometryChanged event on each of the affected base geometries
        /// to perform an update after the end of a complex operation: Draw, Extrude, Move.
        /// </summary>
        /// <param name="sender">the model containing the geometry</param>
        /// <param name="affectedGeometries">the affected geometry instances</param>
        private void model_OperationFinished(object sender, IEnumerable<BaseGeometry> affectedGeometries)
        {
            GeometryModelData model = sender as GeometryModelData;
            if (model == null || affectedGeometries == null) return;

            string model_key = model.Model.File.FullName;
            if (!(this.geometry_lookup_tables.ContainsKey(model_key))) return;

            if (this.geometry_lookup_tables[model_key].Count == 0)
                this.geometry_lookup_tables[model_key] = ConnectedGeometryManager.CreateLookupTable(model);
        }

        /// <summary>
        /// after the end of a clean-up operation, which can result in deletion and creation
        /// of geometry.
        /// </summary>
        /// <param name="sender">the model containing the geometry</param>
        /// <param name="e">something, can be Null, as currently not used</param>
        private void model_BatchOperationFinished(object sender, EventArgs e)
        {
            GeometryModelData model = sender as GeometryModelData;
            if (model == null) return;

            string model_key = model.Model.File.FullName;
            if (!(this.geometry_lookup_tables.ContainsKey(model_key))) return;

            this.model_to_update_after_batch_op = model;
            this.batch_op_handler_delay_timer.Stop();

            if (this.geometryExchange.EnableAsyncUpdates)
                this.batch_op_handler_delay_timer.Start();
            else
                OnBatchOpEventDelayTimerTick(null, EventArgs.Empty);
        }

        private void HandleDifferenceOld2New(Dictionary<ulong, BaseGeometry> _old, Dictionary<ulong, BaseGeometry> _new)
        {
            // TOO SLOW...
            //var diff = ConnectedGeometryManager.CompareTables(_old, _new);
            //foreach (BaseGeometry bg in diff.deleted)
            //{
            //    this.PassGeometryDeletionToConnectors(bg);
            //}
            //foreach (BaseGeometry bg in diff.created)
            //{
            //    this.PassGeometryAdditionToConnectors(bg);
            //}

            var diffK = ConnectedGeometryManager.CompareTableKeys(_old, _new);
            foreach (ulong key in diffK.deleted_keys)
            {
                this.PassGeometryDeletionToConnectors(_old[key]);
            }
            foreach (ulong key in diffK.created_keys)
            {
                this.PassGeometryAdditionToConnectors(_new[key]);
            }
        }

        private static (IEnumerable<ulong> deleted_keys, IEnumerable<ulong> created_keys) CompareTableKeys(Dictionary<ulong, BaseGeometry> _old, Dictionary<ulong, BaseGeometry> _new)
        {
            var new_keys = _new.Keys.Except(_old.Keys);
            var old_keys = _old.Keys.Except(_new.Keys);
            return (old_keys, new_keys);
        }

        /// <summary>
        /// Reacts to a change in the offset surfaces of the model. Triggers the synchronization 
        /// of all descriptive connectors.
        /// NOTE: This is a rather expensive method that needs optimizing...
        /// </summary>
        /// <param name="sender">the model</param>
        /// <param name="modifiedFaces">A list of modified faces</param>
        private void model_OffsetModel_OffsetSurfaceChanged(object sender, IEnumerable<Face> modifiedFaces)
        {
            this.offset_model_handler_delay_timer.Stop();

            OffsetModel o_model = sender as OffsetModel;
            if (o_model != null && !this.delayed_offset_models.Contains(o_model))
                this.delayed_offset_models.Add(o_model);

            if (this.geometryExchange.EnableAsyncUpdates)
                this.offset_model_handler_delay_timer.Start();
            else
                OnOffsetChangedEventDelayTimerTick(null, EventArgs.Empty);
        }

        private void UpdateOffsetsAfterUpdate(OffsetModel _o_model)
        {
            if (_o_model == null) return;
            GeometryModelData model = _o_model.Model;
            if (model == null) return;

            string model_key = model.Model.File.FullName;
            if (!(this.geometry_lookup_tables.ContainsKey(model_key))) return;

            List<ConnectorBase> allParentInstanceConnectors = new List<ConnectorBase>();
            Face randomFace = null;

            // update the descriptors
            foreach (var entry in this.geometry_lookup_tables[model_key])
            {
                if (entry.Value is Volume vol)
                {
                    IEnumerable<ConnectorToVolume> affected_vol = this.geometryExchange.ConnectorManager.GetDescriptorOf(vol);
                    if (affected_vol != null)
                    {
                        foreach (ConnectorToVolume a_v in affected_vol)
                            a_v.SynchronizeSourceWTarget(vol);
                    }
                }
                else if (entry.Value is Face face)
                {
                    randomFace = face;
                    IEnumerable<ConnectorToFace> affected_faces = this.geometryExchange.ConnectorManager.GetAllDescriptorsOf(face);
                    foreach (ConnectorToFace cf in affected_faces)
                    {
                        cf.SynchronizeSourceWTarget(face);
                    }
                    // added 7.8.2019
                    InstanceConnectorToFace icf = this.geometryExchange.ConnectorManager.GetPrescriptorOf(face);
                    if (icf != null && icf.ParentConnector != null)
                    {
                        allParentInstanceConnectors.Add(icf.ParentConnector);
                    }
                }
                else if (entry.Value is EdgeLoop el)
                {
                    foreach (var connector in this.geometryExchange.ConnectorManager.GetDescriptorOf(el))
                    {
                        connector.SynchronizeSourceWTarget(el);
                    }
                }
            }

            allParentInstanceConnectors.Distinct().ForEach(x => x.SynchronizeSourceWTarget(randomFace));
        }

        #endregion

        #region EVENT HANDLER DELAY

        private void OnOffsetChangedEventDelayTimerTick(object sender, EventArgs e)
        {
            this.offset_model_handler_delay_timer.Stop();
            //Console.WriteLine("---------------------------------------------------------------------------nr models: {0}", this.delayed_offset_models.Count);
            foreach (OffsetModel om in this.delayed_offset_models)
            {
                this.UpdateOffsetsAfterUpdate(om);
            }
            this.delayed_offset_models = new List<OffsetModel>();
        }

        private void OnBatchOpEventDelayTimerTick(object sender, EventArgs e)
        {
            this.batch_op_handler_delay_timer.Stop();
            //Console.WriteLine("--------------------------------------------------------------------------- update after End of Batch Operation");

            if (this.model_to_update_after_batch_op != null)
            {
                string model_key = this.model_to_update_after_batch_op.Model.File.FullName;
                var old = new Dictionary<ulong, BaseGeometry>(this.geometry_lookup_tables[model_key]);
                this.geometry_lookup_tables[model_key] = ConnectedGeometryManager.CreateLookupTable(this.model_to_update_after_batch_op);
                this.HandleDifferenceOld2New(old, this.geometry_lookup_tables[model_key]);
                this.model_to_update_after_batch_op = null;
            }

        }

        #endregion

        #region RESET

        internal void Reset()
        {
            // manager_of_all remains the same
            this.geometry_lookup_tables.Clear();
            this.recently_deleted_ids.Clear();
            // dispatcher remains the same
            this.offset_model_handler_delay_timer.Stop();
            this.delayed_offset_models.Clear();
            this.batch_op_handler_delay_timer.Stop();
            this.model_to_update_after_batch_op = null;
        }

        #endregion
    }
}
