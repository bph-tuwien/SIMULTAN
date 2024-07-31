using SIMULTAN;
using SIMULTAN.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using SIMULTAN.Data.SimMath;

namespace SIMULTAN.Data.Geometry
{
    /// <summary>
    /// Represents a layer
    /// </summary>
    public class Layer : INotifyPropertyChanged
    {
        #region Properties

        /// <summary>
        /// Gets or sets the Name of the Layer
        /// </summary>
        public string Name
        {
            get { return name; }
            set { name = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name))); }
        }
        private string name;

        /// <summary>
        /// Gets or sets the color of the Layer
        /// </summary>
        public DerivedColor Color
        {
            get { return color; }
            set
            {
                if (color != null)
                    color.PropertyChanged -= Color_PropertyChanged;

                color = value;
                color.Parent = this.parent;

                if (color != null)
                    color.PropertyChanged += Color_PropertyChanged;

                Model.StartBatchOperation();
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Color)));
                Elements.ForEach(x => x.Color.NotifyParentColorChanged());
                Model.EndBatchOperation();
            }
        }
        private DerivedColor color;

        /// <summary>
        /// Returns the GeometryModel this Layer belongs to
        /// </summary>
        public GeometryModelData Model { get; private set; }

        /// <summary>
        /// Returns a list of all geometric elements in this layer
        /// </summary>
        public ObservableCollection<BaseGeometry> Elements { get; private set; }

        /// <summary>
        /// Returns a list of sublayer
        /// </summary>
        public ObservableCollection<Layer> Layers { get; private set; }
        /// <summary>
        /// Stores the parent layer. Automatically set when adding the layer as a child
        /// </summary>
        public Layer Parent
        {
            get { return parent; }
            private set
            {
                if (parent != value)
                {
                    if (parent != null)
                        parent.PropertyChanged -= Parent_PropertyChanged;

                    parent = value;
                    color.Parent = value;

                    if (parent != null)
                        parent.PropertyChanged += Parent_PropertyChanged;
                }
            }
        }
        private Layer parent;

        /// <summary>
        /// Gets or sets if this Layer is visible including visibility of parent objects (parent Layer, GeometryModel)
        /// </summary>
        public bool IsActuallyVisible
        {
            get
            {
                bool aggregated = isVisible & Model.IsVisible;

                if (Parent != null)
                    aggregated &= Parent.IsActuallyVisible;

                return aggregated;
            }
        }
        /// <summary>
        /// Gets or sets if this Layer is visible
        /// </summary>
        public bool IsVisible
        {
            get { return isVisible; }
            set
            {
                if (isVisible != value)
                {
                    isVisible = value;
                    Model.StartBatchOperation();
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsVisible)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsActuallyVisible)));

                    //Notify children
                    this.Elements.ForEach(x => x.NotifyActualVisibilityChanged());

                    Model.EndBatchOperation();
                }
            }
        }
        private bool isVisible;

        /// <summary>
        /// Unique ID of this layer
        /// </summary>
        public ulong Id { get; private set; }

        #endregion

        #region Events

        /// <inheritdoc />
        public event PropertyChangedEventHandler PropertyChanged;

        #endregion



        /// <summary>
        /// Initializes a new instance of the Layer class
        /// </summary>
        /// <param name="model">The GeometryModel this layer belongs to</param>
        /// <param name="name">The display name</param>
        public Layer(GeometryModelData model, String name) : this(model != null ? model.GetFreeId(false) : ulong.MaxValue, model, name) { }
        /// <summary>
        /// Initializes a new instance of the Layer class
        /// </summary>
        /// <param name="id">Unique Id for the layer</param>
        /// <param name="model">The GeometryModel this layer belongs to</param>
        /// <param name="name">The display name</param>
        public Layer(ulong id, GeometryModelData model, String name)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            this.Elements = new ObservableCollection<BaseGeometry>();
            this.Layers = new ObservableCollection<Layer>();
            this.Layers.CollectionChanged += Layers_CollectionChanged;

            this.Id = id;
            this.Model = model;
            this.Name = name;
            this.Parent = null;
            this.IsVisible = true;
            this.Color = new DerivedColor(SimColors.White);

            AttachEvents();
        }

        /// <summary>
        /// Searches for a layer with given name in the whole hierarchy with this layer as root. 
        /// </summary>
        /// <param name="layerName">Layer name to search for</param>
        /// <returns>Returns layer if found or null otherwise</returns>
        public Layer FindInHierarchy(string layerName)
        {
            if (this.name == layerName) return this;

            foreach (var sublayer in this.Layers)
            {
                return sublayer.FindInHierarchy(layerName);
            }

            return null;
        }


        #region EventHandler

        internal void AttachEvents()
        {
            this.Model.PropertyChanged += Model_PropertyChanged;
            this.Model.GeometryRemoved += Model_GeometryRemoved;
        }

        internal void DetachEvents()
        {
            this.Model.PropertyChanged -= Model_PropertyChanged;
            this.Model.GeometryRemoved -= Model_GeometryRemoved;
        }


        private void Model_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(GeometryModelData.IsVisible))
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsActuallyVisible)));
                Elements.ForEach(x => x.NotifyActualVisibilityChanged());
            }
        }

        private void Model_GeometryRemoved(object sender, IEnumerable<BaseGeometry> geometry)
        {
            foreach (var g in geometry)
                if (g.Layer == this)
                    Elements.Remove(g);
        }

        private void Layers_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (var oi in e.OldItems)
                {
                    this.Model.FreeId(((Layer)oi).Id);
                    ((Layer)oi).Parent = null;
                }
            }
            if (e.NewItems != null)
            {
                foreach (var oi in e.NewItems)
                {
                    this.Model.RegisterId(((Layer)oi).Id, null);
                }
            }

            this.Layers.ForEach(x => x.Parent = this);
        }

        Dictionary<string, string[]> parentTranslationTable = new Dictionary<string, string[]>()
        {
            { nameof(IsActuallyVisible), new string[] { nameof(IsActuallyVisible) } }
        };
        private void Parent_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (parentTranslationTable.ContainsKey(e.PropertyName))
                parentTranslationTable[e.PropertyName].ForEach(x => this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(x)));

            if (e.PropertyName == nameof(Layer.IsActuallyVisible))
            {
                foreach (var element in this.Elements)
                    element.NotifyActualVisibilityChanged();
            }
        }

        private void Color_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Model.StartBatchOperation();
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Color)));
            Elements.ForEach(x => x.Color.NotifyParentColorChanged());
            Model.EndBatchOperation();
        }

        #endregion
    }
}
