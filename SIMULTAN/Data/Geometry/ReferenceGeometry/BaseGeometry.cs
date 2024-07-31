using SIMULTAN.Exceptions;
using System;
using System.ComponentModel;
using System.Diagnostics;

namespace SIMULTAN.Data.Geometry
{
    /// <summary>
    /// Base class for all geometric objects
    /// </summary>
    public abstract class BaseGeometry : INotifyPropertyChanged
    {
        #region Properties

        /// <summary>
        /// Gets or sets if this Geometry is visible including visibility of parent objects (Layer, GeometryModel)
        /// </summary>
        public bool IsActuallyVisible { get { return isVisible && this.Layer.IsActuallyVisible; } }

        /// <summary>
        /// Gets or sets if this Geometry is visible
        /// </summary>
        public bool IsVisible
        {
            get { return isVisible; }
            set
            {
                isVisible = value;
                NotifyPropertyChanged(nameof(IsVisible));
                NotifyPropertyChanged(nameof(IsActuallyVisible));
            }
        }
        private bool isVisible;

        /// <summary>
        /// Returns a unique identifier for this object
        /// </summary>
        public ulong Id { get; private set; }
        /// <summary>
        /// Returns the model this geometry object belongs to
        /// </summary>
        public GeometryModelData ModelGeometry { get { return Layer?.Model; } }
        /// <summary>
        /// Gets or sets the layer on which this geometry object is placed
        /// </summary>
        public Layer Layer
        {
            get { return layer; }
            [DebuggerHidden]
            set
            {
                MoveToLayer(value);
            }
        }
        private Layer layer;

        /// <summary>
        /// Gets or sets the display name of this object
        /// </summary>
        /// <remarks>The name is NOT unique</remarks>
        public string Name
        {
            get { return name; }
            set
            {
                if (name != value)
                {
                    name = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name)));
                }
            }
        }
        private string name;

        private readonly PropertyChangedEventHandler onColorPropertyChanged;

        /// <summary>
        /// Color of the object
        /// </summary>
        public DerivedColor Color
        {
            get { return color; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException("Color may not be null");
                if (value.Parent != null)
                    throw new ArgumentException("BaseGeometries set the parent automatically");

                if (color != value)
                {
                    if (color != null)
                    {
                        color.PropertyChanged -= onColorPropertyChanged;
                        color.Parent = null;
                    }

                    color = value;
                    AssignParentColor();

                    if (color != null)
                    {
                        color.PropertyChanged += onColorPropertyChanged;
                    }

                    NotifyPropertyChanged(nameof(Color));
                }
            }
        }
        /// <summary>
        /// Called whenever the color of this object has been changed
        /// </summary>
        protected virtual void OnColorChanged() { }
        private DerivedColor color;

        /// <summary>
        /// Determines if the geometry was changed during a batch operation
        /// </summary>
        protected bool GeometryHasChanged { get { return geometryHasChanged; } }
        private bool geometryHasChanged;

        private bool topologyHasChanged;

        #endregion



        /// <summary>
        /// Initializes a new instance of the BaseGeometry class
        /// </summary>
        /// <param name="id">Unique identifier for this object</param>
        /// <param name="layer">The layer for this object</param>
        public BaseGeometry(ulong id, Layer layer)
        {
            if (layer == null)
                throw new ArgumentNullException(nameof(layer));
            if (id == ulong.MaxValue)
                throw new ArgumentException("Invalid Id");

            Id = id;

            this.onColorPropertyChanged = new PropertyChangedEventHandler(Color_PropertyChanged);

            this.layer = null;
            this.Color = new DerivedColor(layer.Color.Color, true);
            this.Name = string.Empty;
            this.geometryHasChanged = false;
            this.topologyHasChanged = false;
            this.isVisible = true;

            MoveToLayer(layer);
        }


        #region Events

        /// <summary>
        /// Handler for the GeometryChanged event.
        /// </summary>
        /// <param name="sender">Object which emitted the event</param>
        public delegate void GeometryEventHandler(object sender);
        /// <summary>
        /// Emitted when geometric properties of this object have changed
        /// </summary>
        public event GeometryEventHandler GeometryChanged;
        /// <summary>
        /// Emits the GeometryChanged event when called outside a batch operation. Otherwise notifies the geometry to send the event after the batch operation
        /// </summary>
        public void NotifyGeometryChanged()
        {
            if (ModelGeometry.HandleConsistency)
            {
                if (!topologyHasChanged)
                {
                    this.GeometryChanged?.Invoke(this);
                }
                this.geometryHasChanged = false;
            }
            else
                this.geometryHasChanged = true;
        }
        /// <summary>
        /// Calls the GeometryChanged event when NotifyGeometryChanged has marked this geometry during a batch operation
        /// This event is not sent when either notifyGeometryChanged is False or when a TopologyChanged will be emitted instead
        /// </summary>
        /// <param name="notifyGeometryChanged">Defines whether the event should be send</param>
        public void OnGeometryChanged(bool notifyGeometryChanged)
        {
            if (geometryHasChanged)
            {
                if (notifyGeometryChanged && !topologyHasChanged)
                    this.GeometryChanged?.Invoke(this);
                this.geometryHasChanged = false;
            }
        }

        /// <summary>
        /// Emitted when the topology of this element or of one of its subelements has changed (e.g. when a edge uses different vertices now)
        /// </summary>
        public event GeometryEventHandler TopologyChanged;
        /// <summary>
        /// Emits the TopologyChanged event when called outside a batch operation. Otherwise notifies the geometry to send the event after the batch operation
        /// </summary>
        public void NotifyTopologyChanged()
        {
            if (ModelGeometry.HandleConsistency)
            {
                TopologyChanged?.Invoke(this);
                this.topologyHasChanged = false;
            }
            else
            {
                ModelGeometry.NotifyTopologyChanged(this);
                this.topologyHasChanged = true;
            }
        }
        /// <summary>
        /// Calls the TopologyChanged event when NotifyTopologyChanged has marked this geometry during a batch operation
        /// </summary>
        public void OnTopologyChanged()
        {
            if (topologyHasChanged)
            {
                TopologyChanged?.Invoke(this);
                this.topologyHasChanged = false;
            }
        }

        /// <inheritdoc />
        public event PropertyChangedEventHandler PropertyChanged;
        /// <summary>
        /// Emits the PropertyChanged event
        /// </summary>
        /// <param name="prop">The name of the property</param>
        protected void NotifyPropertyChanged(string prop)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        #endregion


        /// <summary>
        /// Ensures that the geometry is consistent. Called after batch operations
        /// </summary>
        public abstract void MakeConsistent(bool notifyGeometryChanged, bool hasTopologyChanged);

        /// <summary>
        /// Removes the Geometry from the model
        /// </summary>
        public abstract bool RemoveFromModel();
        /// <summary>
        /// Adds the Geometry to the model (inverse of RemoveFromModel)
        /// </summary>
        public abstract void AddToModel();

        [DebuggerHidden]
        private void MoveToLayer(Layer newLayer)
        {
            if (newLayer != null && newLayer != layer)
            {
                if (layer != null && this.ModelGeometry != newLayer.Model)
                    throw new PropertyUnsupportedValueException("Unable to move geometry to Layer in another GeometryModel");

                if (layer != null)
                {
                    //Remove from old layer
                    layer.Elements.Remove(this);
                }

                this.layer = newLayer;
                layer.Elements.Add(this);

                AssignParentColor();

                this.NotifyPropertyChanged(nameof(Layer));
                this.NotifyPropertyChanged(nameof(IsActuallyVisible));
            }
        }

        /// <summary>
        /// Called during Layer reassignment to handle colors. The default implementation transfers the color parent to the layer.
        /// </summary>
        protected virtual void AssignParentColor()
        {
            this.Color.Parent = this.layer;
        }

        #region EventHandler

        private void Color_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            NotifyPropertyChanged(nameof(Color));
            OnColorChanged();
        }

        internal void NotifyActualVisibilityChanged()
        {
            NotifyPropertyChanged(nameof(IsActuallyVisible));
        }

        #endregion
    }
}
