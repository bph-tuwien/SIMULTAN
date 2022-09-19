using SIMULTAN;
using SIMULTAN.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;

namespace SIMULTAN.Data.Geometry
{
    /// <summary>
    /// Represents the geometry of a GeometryModel
    /// </summary>
    /// 
    /// There are several events relevant for working with GeometryModels:
    /// - GeometryChanged
    ///   BaseGeometry::GeometryChanged is emitted whenever the underlying geometry has changed. This event is delayed during
    ///     batch operations and invoked during EndBatchOperation for all modified geometries
    ///   GeometryModel::GeometryChanged is invoke for each invocation of BaseGeometry::GeometryChanged. This event is delayed during
    ///     batch operations and is invoked *once* during EndBatchOperation when at least one geometry has changed.
    ///     
    /// - GeometryAdded/GeometryRemoved
    ///   Emitted when one of the ObservableCollections has changed. This events is delayed during batch operations and is
    ///   invoked during EndBatchOperation once for all added/removed geometries.
    ///   
    /// - TopologyChanged
    ///   Similar to GeometryAdded/GeometryRemoved. Invoked when one of the underlying geometry lists has changed. This event
    ///     is delayed during batch operations and issued *once* when any geometry has been added or removed.
    /// 
    /// - BatchOperationFinished is invoked once when the batch operation has ended, no matter if anything was modified.
    public class GeometryModelData : INotifyPropertyChanged
    {
        #region Properties

        /// <summary>
        /// Stores the mode this geometry is related to.
        /// Can either be the current model or the last one this geometry was attached to
        /// </summary>
        public GeometryModel Model { get; set; } = null;

        private Dictionary<ulong, BaseGeometry> geometryLookup;
        private ulong nextId = 0;

        private HashSet<BaseGeometry> batchAddedGeometry;
        private HashSet<BaseGeometry> batchRemovedGeometry;
        private HashSet<BaseGeometry> batchChangedGeometry;
        private HashSet<BaseGeometry> batchTopologyChangedGeometry;
        private HashSet<GeoReference> batchGeoReferencesChanged;

        /// <summary>
        /// Returns a list of all Vertex instances
        /// </summary>
        public ObservableCollection<Vertex> Vertices { get; private set; }
        /// <summary>
        /// Returns a list of all Edge instances
        /// </summary>
        public ObservableCollection<Edge> Edges { get; private set; }
        /// <summary>
        /// Returns a list of all EdgeLoop instances
        /// </summary>
        public ObservableCollection<EdgeLoop> EdgeLoops { get; private set; }

        /// <summary>
        /// Returns a list of lal polyline instances
        /// </summary>
        public ObservableCollection<Polyline> Polylines { get; private set; }
        /// <summary>
        /// Returns a list of all Faces instances
        /// </summary>
        public ObservableCollection<Face> Faces { get; private set; }
        /// <summary>
        /// Returns a list of all Volumes instances
        /// </summary>
        public ObservableCollection<Volume> Volumes { get; private set; }
        /// <summary>
        /// Returns a list of all top level Layer
        /// </summary>
        public ObservableCollection<Layer> Layers { get; private set; }
        /// <summary>
        /// Returns a list of proxy geometries
        /// </summary>
        public ObservableCollection<ProxyGeometry> ProxyGeometries { get; private set; }
        /// <summary>
        /// Stores a list of georeferenced vertices
        /// </summary>
        public ObservableCollection<GeoReference> GeoReferences { get; private set; }

        /// <summary>
        /// Returns an IEnumerable with all geometries in the model.
        /// </summary>
        public IEnumerable<BaseGeometry> Geometries
        {
            get
            {
                foreach (var g in Vertices)
                    yield return g;
                foreach (var g in Edges)
                    yield return g;
                foreach (var g in EdgeLoops)
                    yield return g;
                foreach (var g in Polylines)
                    yield return g;
                foreach (var g in Faces)
                    yield return g;
                foreach (var g in Volumes)
                    yield return g;
                foreach (var g in ProxyGeometries)
                    yield return g;
            }
        }

        /// <summary>
        /// Returns whether consistency handling is enabled or not.
        /// </summary>
        public bool HandleConsistency { get { return handleConsistencyCounter == 0; } }
        private int handleConsistencyCounter;

        /// <summary>
        /// Gets or sets if this Geometry is visible
        /// </summary>
        public bool IsVisible
        {
            get { return isVisible; }
            set
            {
                if (this.isVisible != value)
                {
                    StartBatchOperation();
                    this.isVisible = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsVisible)));
                    EndBatchOperation();
                }
            }
        }
        private bool isVisible;

        /// <summary>
        /// Stores the offset surface model
        /// </summary>
        public OffsetModel OffsetModel { get; private set; }


        #endregion

        #region Events

        /// <summary>
        /// Delegate for geometry add and remove events
        /// </summary>
        /// <param name="sender">Sending GeometryModel</param>
        /// <param name="geometry">The added/removed geometry</param>
        public delegate void BaseGeometryEventHandler(object sender, IEnumerable<BaseGeometry> geometry);
        /// <summary>
        /// Called whenever a BaseGeometry is added
        /// </summary>
        public event BaseGeometryEventHandler GeometryAdded;
        /// <summary>
        /// Called whenever a BaseGeometry is deleted
        /// </summary>
        public event BaseGeometryEventHandler GeometryRemoved;

        /// <summary>
        /// Invoked when a batch operation has finished
        /// </summary>
        public event EventHandler BatchOperationFinished;


        /// <summary>
        /// EventHandler for the OperationFinished event
        /// </summary>
        /// <param name="sender">The sending model</param>
        /// <param name="affectedGeometries">List of geometries that were affected by the operation</param>
        public delegate void OperationFinishedEventHandler(object sender, IEnumerable<BaseGeometry> affectedGeometries);
        /// <summary>
        /// Emitted whenever a user-control operation has finished (e.g., Move, Extrude, Draw, ...)
        /// </summary>
        public event OperationFinishedEventHandler OperationFinished;
        /// <summary>
        /// Issues a OperationFinished event
        /// </summary>
        /// <param name="affectedGeometries">List of geometries that were affected by the operation</param>
        public void OnOperationFinished(IEnumerable<BaseGeometry> affectedGeometries)
        {
            if (affectedGeometries != null && affectedGeometries.Count() > 0)
            {
                var allAffected = GeometryModelAlgorithms.GetAllAffectedGeometries(affectedGeometries);
                OperationFinished?.Invoke(this, allAffected);
            }
        }


        /// <summary>
        /// Handler for the TopologyChanged event.
        /// </summary>
        /// <param name="sender">Object which emitted the event</param>
        /// <param name="geometries">A list of all geometries that were affected. Note that this list might contain already deleted/removed geometry</param>
        public delegate void TopologyChangedEventHandler(object sender, IEnumerable<BaseGeometry> geometries);
        /// <summary>
        /// Emitted when objects were added or removed during a batch operation
        /// </summary>
        public event TopologyChangedEventHandler TopologyChanged;

        /// <summary>
        /// Handler for the GeometryChanged event.
        /// </summary>
        /// <param name="sender">Object which emitted the event</param>
        /// <param name="geometries">List of BaseGeometries where the underlying geometry has changed</param>
        public delegate void GeometryChangedEventHandler(object sender, IEnumerable<BaseGeometry> geometries);
        /// <summary>
        /// Emitted when a geometric property of this model or of one of the submodels has changed
        /// </summary>
        public event GeometryChangedEventHandler GeometryChanged;

        /// <summary>
        /// EventHandler for changed GeoReferences.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="geoReferences">The GeoReferences that got changed.</param>
        public delegate void GeoReferencesChangedEventHandler(object sender, IEnumerable<GeoReference> geoReferences);
        /// <summary>
        /// The event for when GeoReferences change.
        /// </summary>
        public event GeoReferencesChangedEventHandler GeoReferencesChanged;


        /// <inheritdoc />
        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        /// <summary>
        /// Initializes a new instance of the GeometryModelData class
        /// </summary>
        public GeometryModelData()
        {
            geometryLookup = new Dictionary<ulong, BaseGeometry>();

            Vertices = new ObservableCollection<Vertex>();
            Edges = new ObservableCollection<Edge>();
            EdgeLoops = new ObservableCollection<EdgeLoop>();
            Faces = new ObservableCollection<Face>();
            Volumes = new ObservableCollection<Volume>();
            ProxyGeometries = new ObservableCollection<ProxyGeometry>();
            Polylines = new ObservableCollection<Polyline>();
            this.GeoReferences = new ObservableCollection<GeoReference>();

            Layers = new ObservableCollection<Layer>();
            Layers.CollectionChanged += Layers_CollectionChanged;

            this.handleConsistencyCounter = 0;
            this.isVisible = true;

            ConnectEvents();

            this.OffsetModel = new OffsetModel(this);
        }

        /// <summary>
        /// Initializes a new instance of the GeometryModelData class that also sets the initial next id.
        /// This should only be used when loading from file.
        /// </summary>
        /// <param name="nextId">The next id to use for new BaseGeometries.</param>
        public GeometryModelData(ulong nextId): this()
        {
            this.nextId = nextId;
        }


        #region EventHandler

        private void Geometry_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            //Topology changes
            if (HandleConsistency)
            {
                HashSet<BaseGeometry> topologyChangedGeom = new HashSet<BaseGeometry>();
                if (e.OldItems != null)
                    foreach (var item in e.OldItems)
                        if (item is BaseGeometry)
                            topologyChangedGeom.Add((BaseGeometry)item);
                if (e.NewItems != null)
                    foreach (var item in e.NewItems)
                        if (item is BaseGeometry)
                            topologyChangedGeom.Add((BaseGeometry)item);

                OnTopologyChanged(topologyChangedGeom);
            }
            else
            {
                if (e.OldItems != null)
                    foreach (var item in e.OldItems)
                        if (item is BaseGeometry)
                            batchTopologyChangedGeometry.Add((BaseGeometry)item);
                if (e.NewItems != null)
                    foreach (var item in e.NewItems)
                        if (item is BaseGeometry)
                            batchTopologyChangedGeometry.Add((BaseGeometry)item);
            }


            //Old items
            if (e.OldItems != null && e.OldItems.Count > 0)
            {
                List<BaseGeometry> oldGeometry = new List<BaseGeometry>();

                foreach (var item in e.OldItems)
                {
                    if (item is BaseGeometry)
                    {
                        var g = ((BaseGeometry)item);
                        oldGeometry.Add(g);
                        FreeId(g.Id);
                        g.GeometryChanged -= Geometry_GeometryChanged;
                        g.TopologyChanged -= Geometry_TopologyChanged;

                        if (!HandleConsistency)
                            this.batchRemovedGeometry.Add(g);
                    }
                }

                if (oldGeometry.Count > 0)
                {
                    if (HandleConsistency)
                        this.GeometryRemoved?.Invoke(this, oldGeometry);
                }
            }

            //New items
            if (e.NewItems != null && e.NewItems.Count > 0)
            {
                List<BaseGeometry> newGeometry = new List<BaseGeometry>();

                foreach (var item in e.NewItems)
                {
                    if (item is BaseGeometry)
                    {
                        var g = (BaseGeometry)item;
                        if (!geometryLookup.ContainsKey(g.Id))
                        {
                            RegisterId(g.Id, g);
                        }

                        g.GeometryChanged += Geometry_GeometryChanged;
                        g.TopologyChanged += Geometry_TopologyChanged;

                        if (!HandleConsistency)
                            this.batchAddedGeometry.Add(g);
                        else
                            newGeometry.Add(g);
                    }
                    else if (item is Layer)
                    {
                        var l = (Layer)item;
                        if (!geometryLookup.ContainsKey(l.Id))
                        {
                            // add null if it's a layer just to keep track of the id
                            RegisterId(l.Id, null);
                    }
                }
                }

                if (HandleConsistency)
                    this.GeometryAdded?.Invoke(this, newGeometry);
            }
        }

        private void Geometry_TopologyChanged(object sender)
        {
            if (HandleConsistency)
                OnTopologyChanged(new BaseGeometry[] { (BaseGeometry)sender });
            else
                batchTopologyChangedGeometry.Add((BaseGeometry)sender);
        }

        private void Geometry_GeometryChanged(object sender)
        {
            if (HandleConsistency)
                OnGeometryChanged(new BaseGeometry[] { (BaseGeometry)sender });
            else
                batchChangedGeometry.Add((BaseGeometry)sender);
        }

        private void Layers_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (var item in e.OldItems)
                    FreeId(((Layer)item).Id);
            }
            if (e.NewItems != null)
                foreach (var item in e.NewItems)
                    RegisterId(((Layer)item).Id, null);
        }

        #endregion



        /// <summary>
        /// Returns the next free unique id.
        /// Call <see cref="RegisterId(ulong, BaseGeometry)"/> to register the id and geometry afterwards if so necessary.
        /// </summary>
        /// <param name="increment">If the id counter should be incremented.</param>
        /// <returns>A free id</returns>
        public ulong GetFreeId(bool increment = true)
        {
            var newId = nextId;
            if(increment)
                nextId++;
            return newId;
        }


        /// <summary>
        /// Registers an id as used.
        /// </summary>
        /// <param name="id">The id</param>
        /// <param name="geometry">The geometry to register, use null if it is a layer.</param>
        public void RegisterId(ulong id, BaseGeometry geometry)
        {
            if (geometryLookup.ContainsKey(id))
                throw new ArgumentException("Id was already registered");

            geometryLookup.Add(id, geometry);

            if (id >= nextId)
                nextId = id + 1;
        }

        /// <summary>
        /// Frees an id.
        /// </summary>
        /// <param name="id">The id</param>
        public void FreeId(ulong id)
        {
            geometryLookup.Remove(id);
        }

        /// <summary>
        /// Starts a batch edit operation. During this operation no consistency checks are performed. Note that list might be outdated during the batch operation
        /// </summary>
        public void StartBatchOperation()
        {
            if (handleConsistencyCounter == 0) //Only init when first batch operation starts
            {
                this.batchAddedGeometry = new HashSet<BaseGeometry>();
                this.batchRemovedGeometry = new HashSet<BaseGeometry>();
                this.batchChangedGeometry = new HashSet<BaseGeometry>();
                this.batchTopologyChangedGeometry = new HashSet<BaseGeometry>();
                this.batchGeoReferencesChanged = new HashSet<GeoReference>();
            }

            this.handleConsistencyCounter++;
        }

        /// <summary>
        /// Ends a batch edit operation and ensures the model is in a consistent state.
        /// Sends events that have been delayed during the batch operation.
        /// </summary>
        public void EndBatchOperation()
        {
            if (this.handleConsistencyCounter <= 1) //After decrement, HandleConsistency will be true
            {

                MakeConsistent();

                //Topology changed
                if (batchTopologyChangedGeometry.Count > 0)
                    OnTopologyChanged(batchTopologyChangedGeometry);

                //Add/Remove
                if (this.batchRemovedGeometry.Count > 0)
                    this.GeometryRemoved?.Invoke(this, this.batchRemovedGeometry);
                if (this.batchAddedGeometry.Count > 0)
                    this.GeometryAdded?.Invoke(this, this.batchAddedGeometry);

                //GeometryChanged
                if (batchChangedGeometry.Count > 0)
                    OnGeometryChanged(batchChangedGeometry);

                // GeoReferences changed
                if (batchGeoReferencesChanged.Count > 0)
                    OnGeoreferencesChanged(batchGeoReferencesChanged);
            }

            this.handleConsistencyCounter--;

            if (this.handleConsistencyCounter == 0)
                BatchOperationFinished?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Updates all derived data structure in a model
        /// </summary>
        public void MakeConsistent()
        {
            bool topologyHasChanged = batchTopologyChangedGeometry.Count > 0;

            //Don't send geometrychanged events for all geometries when the topology changed event has been sent
            this.Vertices.ForEach(x => x.MakeConsistent(!topologyHasChanged, topologyHasChanged));
            this.Edges.ForEach(x => x.MakeConsistent(!topologyHasChanged, topologyHasChanged));
            this.EdgeLoops.ForEach(x => x.MakeConsistent(!topologyHasChanged, topologyHasChanged));
            this.Faces.ForEach(x => x.MakeConsistent(!topologyHasChanged, topologyHasChanged));
            this.Volumes.ForEach(x => x.MakeConsistent(!topologyHasChanged, topologyHasChanged));
            this.ProxyGeometries.ForEach(x => x.MakeConsistent(!topologyHasChanged, topologyHasChanged));
            this.Polylines.ForEach(x => x.MakeConsistent(!topologyHasChanged, topologyHasChanged));
        }

        /// <summary>
        /// Creates a deep copy of the model and all geometries in the model
        /// </summary>
        /// <returns></returns>
        public GeometryModelData Clone()
        {
            GeometryModelData model = new GeometryModelData();
            GeometryModelAlgorithms.CopyContent(this, model, true);

            //Copy linked models
            //model.LinkedModels = this.LinkedModels.ToObservableCollection();

            return model;
        }

        /// <summary>
        /// Notifies the model that the topology has changed. Called by BaseGeomtries
        /// </summary>
        public void NotifyTopologyChanged(BaseGeometry sender)
        {
            if (this.HandleConsistency)
                OnTopologyChanged(new BaseGeometry[] { sender });
            else
            {
                batchTopologyChangedGeometry.Add(sender);
            }
        }


        /// <summary>
        /// Returns True when a BaseGeometry is contained in the Model
        /// </summary>
        /// <param name="geometry">The geometry</param>
        /// <returns>True when the BaseGeometry is contained in the Model, otherwise False</returns>
        public bool ContainsGeometry(BaseGeometry geometry)
        {
            if (geometry == null)
                return false;

            if (geometryLookup.TryGetValue(geometry.Id, out var geom))
                return geom == geometry;

            return false;
        }
        /// <summary>
        /// Returns the BaseGeometry with the given ID
        /// </summary>
        /// <param name="id">ID of the Geometry</param>
        /// <returns>The BaseGeometry or null when the id doesn't exist</returns>
        public BaseGeometry GeometryFromId(ulong id)
        {
            if(geometryLookup.TryGetValue(id, out var geometry))
            {
                return geometry;
            }

            return null;
        }
        /// <summary>
        /// Returns the layer with the given ID
        /// </summary>
        /// <param name="id">The id to search for</param>
        /// <returns>The layer with this id, or null when no such layer exists</returns>
        public Layer LayerFromId(ulong id)
        {
            return LayerFromId(id, this.Layers);
        }


        private Layer CloneLayer(GeometryModelData targetModel, Layer sourceLayer, Layer parentLayer, Dictionary<Layer, Layer> layerLookup)
        {
            Layer clone = new Layer(sourceLayer.Id, targetModel, sourceLayer.Name)
            {
                IsVisible = sourceLayer.IsVisible
            };
            if (parentLayer != null)
            {
                clone.Color = new DerivedColor(sourceLayer.Color.LocalColor, parentLayer, "Color");
                clone.Color.IsFromParent = sourceLayer.Color.IsFromParent;
            }
            else
                clone.Color = new DerivedColor(sourceLayer.Color.LocalColor);

            foreach (var l in sourceLayer.Layers)
                clone.Layers.Add(CloneLayer(targetModel, l, clone, layerLookup));
            layerLookup.Add(sourceLayer, clone);
            return clone;
        }
        private T CloneWithLookup<T>(T source, Func<T, T> clone, Dictionary<T, T> dict)
        {
            T t = clone(source);
            dict.Add(source, t);
            return t;
        }
        private DerivedColor CloneColor(DerivedColor source, Dictionary<Layer, Layer> layerLookup, GeometryModelData model)
        {
            if (source.Parent is Layer)
            {
                DerivedColor result = new DerivedColor(source.LocalColor, layerLookup[(Layer)source.Parent], source.PropertyName);
                result.IsFromParent = source.IsFromParent;
                return result;
            }
            else if (source.Parent is BaseGeometry && model.GeometryFromId(((BaseGeometry)source.Parent).Id) != null)
            {
                var sourceGeo = model.GeometryFromId(((BaseGeometry)source.Parent).Id);
                DerivedColor result = new DerivedColor(source.LocalColor, sourceGeo, source.PropertyName)
                {
                    IsFromParent = source.IsFromParent
                };
                return result;
            }
            else
            {
                DerivedColor result = new DerivedColor(source.Color);
                return result;
            }
        }

        private Layer LayerFromId(ulong id, IEnumerable<Layer> layers)
        {
            var result = layers.FirstOrDefault(x => x.Id == id);
            if (result != null)
                return result;

            foreach (var l in layers)
            {
                result = LayerFromId(id, l.Layers);
                if (result != null)
                    return result;
            }

            return null;
        }


        private void ConnectEvents()
        {
            Vertices.CollectionChanged += Geometry_CollectionChanged;
            Edges.CollectionChanged += Geometry_CollectionChanged;
            EdgeLoops.CollectionChanged += Geometry_CollectionChanged;
            Faces.CollectionChanged += Geometry_CollectionChanged;
            Volumes.CollectionChanged += Geometry_CollectionChanged;
            Layers.CollectionChanged += Geometry_CollectionChanged;
            ProxyGeometries.CollectionChanged += Geometry_CollectionChanged;
            Polylines.CollectionChanged += Geometry_CollectionChanged;
            this.GeoReferences.CollectionChanged += GeoReferences_CollectionChanged;
        }

        private void GeoReferences_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (HandleConsistency)
            {
                var changed = new HashSet<GeoReference>();
                if (e.OldItems != null)
                {
                    foreach (var item in e.OldItems)
                    {
                        var georef = (GeoReference)item;
                        changed.Add(georef);
                        georef.PropertyChanged -= Georef_PropertyChanged;
                    }
                }
                if (e.NewItems != null)
                {
                    foreach (var item in e.NewItems)
                    {
                        var georef = (GeoReference)item;
                        changed.Add(georef);
                        georef.PropertyChanged += Georef_PropertyChanged;
                    }
                }
                OnGeoreferencesChanged(changed);
            }
            else
            {
                if (e.OldItems != null)
                {
                    foreach (var item in e.OldItems)
                    {
                        var georef = (GeoReference)item;
                        batchGeoReferencesChanged.Add(georef);
                        georef.PropertyChanged -= Georef_PropertyChanged;
                    }
                }
                if (e.NewItems != null)
                {
                    foreach (var item in e.NewItems)
                    {
                        var georef = (GeoReference)item;
                        batchGeoReferencesChanged.Add(georef);
                        georef.PropertyChanged += Georef_PropertyChanged;
                    }
                }
            }
        }

        private void Georef_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (HandleConsistency)
            {
                var changed = new HashSet<GeoReference>();
                changed.Add((GeoReference)sender);
                OnGeoreferencesChanged(changed);
            }
            else
            {
                var changed = new HashSet<GeoReference>();
                batchGeoReferencesChanged.Add((GeoReference)sender);
            }
        }

        private void OnTopologyChanged(IEnumerable<BaseGeometry> geometries)
        {
            this.TopologyChanged?.Invoke(this, geometries);
        }

        private void OnGeometryChanged(IEnumerable<BaseGeometry> geometries)
        {
            this.GeometryChanged?.Invoke(this, geometries);
        }

        private void OnGeoreferencesChanged(IEnumerable<GeoReference> geoReferences)
        {
            this.GeoReferencesChanged?.Invoke(this, geoReferences);
        }
    }
}
