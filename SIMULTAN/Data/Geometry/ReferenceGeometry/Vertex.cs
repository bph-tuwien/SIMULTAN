using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Media.Media3D;

namespace SIMULTAN.Data.Geometry
{
    /// <summary>
    /// Represents a point in 3D space
    /// </summary>
    [DebuggerDisplay("Vertex: {position} ID={Id}")]
    public class Vertex : BaseGeometry
    {
        /// <summary>
        /// Gets or sets the position
        /// </summary>
        public Point3D Position
        {
            get { return position; }
            set
            {
                position = value;
                OnPropertyChanged(nameof(Position));
                NotifyGeometryChanged();
            }
        }
        private Point3D position;

        /// <summary>
        /// Returns all edges the vertex is part of
        /// </summary>
        public List<Edge> Edges { get; private set; }

        /// <summary>
        /// Stores a list of proxy geometries that are attached to this vertex
        /// </summary>
        public List<ProxyGeometry> ProxyGeometries { get; private set; }

        /// <summary>
        /// Initializes a new instance of the Vertex class
        /// </summary>
        /// <param name="layer">The layer this object is placed on</param>
        /// <param name="nameFormat">The display name (this is used as a format string for string.Format. {0} is the Id</param>
        /// <param name="position">The position of the vertex</param>
        public Vertex(Layer layer, string nameFormat, Point3D position)
            : this(layer != null ? layer.Model.GetFreeId() : ulong.MaxValue, layer, nameFormat, position) { }
        /// <summary>
        /// Initializes a new instance of the Vertex class
        /// </summary>
        /// <param name="id">The unique identifier for this object</param>
        /// <param name="layer">The layer this object is placed on</param>
        /// <param name="nameFormat">The display name (this is used as a format string for string.Format. {0} is the Id</param>
        /// <param name="position">The position of the vertex</param>
        public Vertex(ulong id, Layer layer, string nameFormat, Point3D position)
            : base(id, layer)
        {
            if (nameFormat == null)
                throw new ArgumentNullException(nameof(nameFormat));

            this.Name = string.Format(nameFormat, id);
            Position = position;
            this.Edges = new List<Edge>();
            this.ProxyGeometries = new List<ProxyGeometry>();

            ModelGeometry.Vertices.Add(this);
        }

        /// <inheritdoc />
        public override void MakeConsistent(bool notifyGeometryChanged, bool hasTopologyChanged)
        {
            if (hasTopologyChanged)
            {
                this.Edges.Clear();
                this.ProxyGeometries.Clear();
            }

            OnGeometryChanged(notifyGeometryChanged);
        }
        /// <inheritdoc/>
        public override bool RemoveFromModel()
        {
            return this.ModelGeometry.Vertices.Remove(this);
        }

        /// <inheritdoc/>
        public override void AddToModel()
        {
            if (this.ModelGeometry.Vertices.Contains(this))
                throw new Exception("Geometry is already present in the model");

            this.ModelGeometry.Vertices.Add(this);
            if (this.Layer != null && !this.Layer.Elements.Contains(this))
                this.Layer.Elements.Add(this);
        }

        /// <summary>
        /// Creates a deep copy of the vertex
        /// </summary>
        /// <returns></returns>
        public Vertex Clone()
        {
            return new Vertex(this.Layer, this.Name, this.position)
            {
                IsVisible = this.IsVisible,
                Color = new DerivedColor(this.Color),
            };
        }
        /// <summary>
        /// Creates a deep copy of the vertex on a specific layer
        /// </summary>
        /// <param name="layer">The target layer</param>
        /// <returns>The new vertex</returns>
        public Vertex Clone(Layer layer)
        {
            bool colorFromParent = this.Color.IsFromParent;

            return new Vertex(layer, this.Name, this.position)
            {
                Color = new DerivedColor(this.Color.LocalColor, layer, nameof(Layer.Color))
                {
                    IsFromParent = colorFromParent
                },
                IsVisible = this.IsVisible,
            };
        }
    }
}
