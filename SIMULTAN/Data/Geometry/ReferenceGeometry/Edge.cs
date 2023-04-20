using SIMULTAN.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;

namespace SIMULTAN.Data.Geometry
{
    /// <summary>
    /// Represents an edge
    /// </summary>
    [DebuggerDisplay("Edge ID={Id}")]
    public class Edge : BaseGeometry
    {
        /// <summary>
        /// Returns the vertices defining this edge.
        /// </summary>
        /// <remarks>Has to contain exactly two vertices</remarks>
        public ObservableCollection<Vertex> Vertices { get; private set; }
        /// <summary>
        /// Returns all pedges attached to this egde
        /// </summary>
        public List<PEdge> PEdges { get; private set; }

        /// <summary>
        /// Initializes a new instance of the Edge class
        /// </summary>
        /// <param name="layer">The layer this object is placed on</param>
        /// <param name="nameFormat">The display name (this is used as a format string for string.Format. {0} is the Id</param>
        /// <param name="vertices">The vertices defining this edge</param>
        public Edge(Layer layer, string nameFormat, IEnumerable<Vertex> vertices) : this(layer != null ? layer.Model.GetFreeId() : ulong.MaxValue, layer, nameFormat, vertices) { }
        /// <summary>
        /// Initializes a new instance of the Edge class
        /// </summary>
        /// <param name="id">The unique identifier for this object</param>
        /// <param name="layer">The layer this object is placed on</param>
        /// <param name="nameFormat">The display name (this is used as a format string for string.Format. {0} is the Id</param>
        /// <param name="vertices">The vertices defining this edge</param>
        public Edge(ulong id, Layer layer, string nameFormat, IEnumerable<Vertex> vertices)
            : base(id, layer)
        {
            if (vertices == null)
                throw new ArgumentNullException(nameof(vertices));
            if (vertices.Count() != 2)
                throw new ArgumentException(string.Format("{0} has to contain exactly two elements", nameof(vertices)));
            if (vertices.Any(t => t == null))
                throw new ArgumentException(string.Format("{0} vertex can not be null", nameof(vertices)));
            if (nameFormat == null)
                throw new ArgumentNullException(nameof(nameFormat));

            this.Name = string.Format(nameFormat, id);

            this.Vertices = new ObservableCollection<Vertex>(vertices);
            this.Vertices.CollectionChanged += Vertices_CollectionChanged;


            this.PEdges = new List<PEdge>();
            MakeConsistent(false, true);

            this.ModelGeometry.Edges.Add(this);
        }

        private void Vertex_GeometryChanged(object sender)
        {
            NotifyGeometryChanged();
        }

        private void Vertices_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            //This has to happen always. Otherwise removed vertices could still send events
            foreach (var v in e.OldItems)
                ((Vertex)v).GeometryChanged -= Vertex_GeometryChanged;

            if (ModelGeometry.HandleConsistency)
            {
                foreach (var v in e.OldItems)
                    ((Vertex)v).Edges.Remove(this);

                foreach (var v in e.NewItems)
                {
                    ((Vertex)v).Edges.Add(this);
                    ((Vertex)v).GeometryChanged += Vertex_GeometryChanged;
                }
            }

            NotifyTopologyChanged();
        }

        /// <inheritdoc />
        public override void MakeConsistent(bool notifyGeometryChanged, bool hasTopologyChanged)
        {
            if (hasTopologyChanged)
            {
                this.PEdges.Clear();

                //Register edge in vertices
                foreach (var v in Vertices)
                {
                    v.Edges.Add(this);
                    v.GeometryChanged -= Vertex_GeometryChanged;
                    v.GeometryChanged += Vertex_GeometryChanged;
                }
            }

            OnGeometryChanged(notifyGeometryChanged);
            OnTopologyChanged();
        }

        /// <inheritdoc/>
        public override bool RemoveFromModel()
        {
            bool removed = this.ModelGeometry.Edges.Remove(this);

            if (ModelGeometry.HandleConsistency)
            {
                Vertices.ForEach(x => x.Edges.Remove(this));
            }

            return removed;
        }
        /// <inheritdoc/>
        public override void AddToModel()
        {
            if (ModelGeometry.Edges.Contains(this))
                throw new Exception("Geometry is already part of the model");

            if (ModelGeometry.HandleConsistency)
            {
                Vertices.ForEach(x =>
                {
                    x.Edges.Add(this);
                });
            }

            if (this.Layer != null && !this.Layer.Elements.Contains(this))
                this.Layer.Elements.Add(this);

            this.ModelGeometry.Edges.Add(this);
        }
    }
}
