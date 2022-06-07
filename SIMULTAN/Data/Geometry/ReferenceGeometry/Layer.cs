using SIMULTAN;
using SIMULTAN.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Media;

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
        public String Name
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
                color = value;
                if (color != null)
                {
                    color.PropertyChanged -= Color_PropertyChanged;
                    color.PropertyChanged += Color_PropertyChanged;
                }

                Model.StartBatchOperation();
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Color)));
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
                    if (parent != null)
                    {
                        parent.PropertyChanged += Parent_PropertyChanged;

                        bool isFromParent = color.IsFromParent;
                        this.Color = new DerivedColor(color.Color, parent, nameof(parent.Color)) { IsFromParent = isFromParent };
                    }
                    else
                    {
                        this.Color = new DerivedColor(color.Color);
                    }
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

            this.Id = id;
            this.Model = model;
            this.Name = name;
            this.Color = new DerivedColor(Colors.White);
            this.Parent = null;
            this.IsVisible = true;

            this.Elements = new ObservableCollection<BaseGeometry>();
            this.Layers = new ObservableCollection<Layer>();

            this.Layers.CollectionChanged += Layers_CollectionChanged;
            this.Model.PropertyChanged += Model_PropertyChanged;
            this.Model.GeometryRemoved += Model_GeometryRemoved;
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

        private void Model_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(GeometryModelData.IsVisible))
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsActuallyVisible)));
        }

        private void Model_GeometryRemoved(object sender, IEnumerable<BaseGeometry> geometry)
        {
            foreach (var g in geometry)
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
        }

        private void Color_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Model.StartBatchOperation();
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Color)));
            Model.EndBatchOperation();
        }

        /// <summary>
        /// Clones a layer and all sublayers and copies them into given target GeometryModel.
        /// </summary>
        /// <param name="target">Target GeometryModel</param>
        /// <returns>Cloned layer</returns>
        [Obsolete]
        public Layer Clone(GeometryModelData target)
        {
            return Clone(target, null);
        }

        /// <summary>
        /// Clones a layer and all sublayers and copies them into given target GeometryModel. Additionally, a mapping between old and new layers is maintained.
        /// </summary>
        /// <param name="target">Target GeometryModel</param>
        /// <param name="mapping">Mapping that contains old layers as keys and new (cloned) layers as values</param>
        /// <returns>Cloned layer</returns>
        [Obsolete]
        public Layer Clone(GeometryModelData target, Dictionary<Layer, Layer> mapping)
        {
            Layer newLayer = new Layer(target, this.name);
            newLayer.Color.Color = this.Color.LocalColor;

            if (mapping != null)
                mapping.Add(this, newLayer);

            newLayer.Layers.AddRange(this.Layers.Select(x => x.Clone(target, mapping)));
            return newLayer;
        }

        #endregion
    }
}
