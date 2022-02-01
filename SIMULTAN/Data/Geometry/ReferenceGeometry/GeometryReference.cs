using SIMULTAN.Serializer.SimGeo;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIMULTAN.Data.Geometry
{
    /// <summary>
    /// Stores a reference to a (potentially unloaded) geometry. This class stores the GeometryModel.Id and the BaseGeometry.Id.
    /// When loaded, Target points to the actual BaseGeometry. Name is cached and available even if the corresponding GeometryModel
    /// is not loaded.
    /// </summary>
    public class GeometryReference : INotifyPropertyChanged
    {
        #region Properties

        /// <summary>
        /// The BaseGeometry.Id of the target
        /// </summary>
        public ulong GeometryID { get; private set; }
        /// <summary>
        /// The GeometryModel.Id of the target
        /// </summary>
        public Guid ModelID { get; private set; }

        /// <summary>
        /// The name. Returns Target.Name when Target != null. Otherwise a cached name
        /// </summary>
        public string Name
        {
            get
            {
                if (cachedGeometry == null)
                    return name;
                else
                    return cachedGeometry.Name;
            }
        }
        private string name;

        /// <summary>
        /// Returns the target BaseGeometry if loaded, or Null
        /// </summary>
        public BaseGeometry Target
        {
            get
            {
                return cachedGeometry;
            }
        }
        private BaseGeometry cachedGeometry;

        /// <summary>
        /// Returns true when the Geometry is loaded.
        /// </summary>
        public bool IsLoaded
        {
            get
            {
                return cachedGeometry != null;
            }
        }

        /// <summary>
        /// Stores a list of all GeometryModels in the application
        /// </summary>
        internal SimGeometryModelCollection ModelStore { get; private set; }

        #endregion

        #region Events

        /// <inheritdoc />
        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        /// <summary>
        /// Initializes a new instance of the GeometryReference class
        /// </summary>
        /// <param name="target">The target geometry</param>
        /// <param name="modelStore">The modelstore in which this geometry exists or may exist in future</param>
        public GeometryReference(BaseGeometry target, SimGeometryModelCollection modelStore)
            : this(target != null ? target.ModelGeometry.Model.Id : Guid.Empty,
                  target != null ? target.Id : 0,
                  target != null ? target.Name : "", target, modelStore)
        {
            if (target == null)
                throw new ArgumentNullException(nameof(target));
        }
        /// <summary>
        /// Initializes a new instance of the GeometryReference class
        /// </summary>
        /// <param name="modelId">Id of the target GeometryModel</param>
        /// <param name="geometryId">Id of the target BaseGeometry</param>
        /// <param name="cachedName">The cached name (only used when cachedGeometry is null and the model isn't loaded)</param>
        /// <param name="cachedGeometry">The target geometry (may be null)</param>
        /// <param name="modelStore">The modelstore in which this geometry exists or may exist in future</param>
        public GeometryReference(Guid modelId, ulong geometryId, string cachedName, BaseGeometry cachedGeometry, SimGeometryModelCollection modelStore)
        {
            if (geometryId == ulong.MaxValue)
                throw new ArgumentException("Invalid Id");
            if (modelStore == null)
                throw new ArgumentNullException(nameof(modelStore));

            this.ModelID = modelId;
            this.GeometryID = geometryId;
            this.cachedGeometry = cachedGeometry;
            this.name = cachedName;

            this.ModelStore = modelStore;
            if (this.ModelStore != null)
            {
                this.ModelStore.CollectionChanged += this.ModelStore_CollectionChanged;
            }

            if (cachedGeometry == null && ModelStore != null) //Load cached if not already loaded
            {
                if (ModelStore.TryGetGeometryModel(this.ModelID, out var model, false))
                    LoadFromModel(model);
            }
        }

        private void ModelStore_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    {
                        foreach (var model in e.NewItems.OfType<GeometryModel>())
                            if (this.ModelID == model.Id)
                                LoadFromModel(model);
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    {
                        foreach (var model in e.OldItems.OfType<GeometryModel>())
                            if (this.ModelID == model.Id)
                                Unload(model);
                    }
                    break;
                default:
                    throw new NotSupportedException("Operation not supported");
            }
        }

        private void Unload(GeometryModel model)
        {
            model.Replaced -= Model_Replaced;

            this.name = cachedGeometry.Name;
            this.cachedGeometry = null;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Target)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsLoaded)));
        }
        private void LoadFromModel(GeometryModel model)
        {
            model.Replaced += Model_Replaced;

            this.cachedGeometry = model.Geometry.GeometryFromId(GeometryID);
            if (this.cachedGeometry != null)
                this.name = cachedGeometry.Name;
            else
                this.name = string.Empty;

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Target)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsLoaded)));
        }

        private void Model_Replaced(object sender, GeometryModelReplacedEventArgs e)
        {
            LoadFromModel(sender as GeometryModel);
        }
    }
}
