using SIMULTAN;
using SIMULTAN.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;

namespace SIMULTAN.Data.Geometry
{
    /// <summary>
    /// Represents a closed edge loop
    /// </summary>
    [DebuggerDisplay("EdgeLoop ID={Id}, Name={Name}")]
    public class EdgeLoop : BaseEdgeContainer
    {
        /// <summary>
        /// Returns the PEdges in the EdgeLoop
        /// </summary>
        public override ObservableCollection<PEdge> Edges
        {
            get { return edges; }
        }
        private ObservableCollection<PEdge> edges;

        /// <summary>
        /// Gets or sets the Face this Loop is associated with
        /// </summary>
        public override List<Face> Faces { get; }

        /// <summary>
        /// Initializes a new instance of the EdgeLoop class
        /// </summary>
        /// <param name="layer">The layer this object is placed on</param>
        /// <param name="nameFormat">The display name</param>
        /// <param name="edges">A list of edges in this loop</param>
        public EdgeLoop(Layer layer, string nameFormat, IEnumerable<Edge> edges)
            : this(layer != null ? layer.Model.GetFreeId() : ulong.MaxValue, layer, nameFormat, edges) { }
        /// <summary>
        /// Initializes a new instance of the EdgeLoop class
        /// </summary>
        /// <param name="id">The unique identifier for this object</param>
        /// <param name="layer">The layer this object is placed on</param>
        /// <param name="nameFormat">The display name</param>
        /// <param name="edges">A list of edges in this loop</param>
        public EdgeLoop(ulong id, Layer layer, string nameFormat, IEnumerable<Edge> edges)
            : base(id, layer)
        {
            if (edges == null)
                throw new ArgumentNullException(nameof(edges));
            if (edges.Count() < 3)
                throw new ArgumentException("EdgeLoops need at least three edges");
            if (nameFormat == null)
                throw new ArgumentNullException(nameof(nameFormat));

            this.Name = string.Format(nameFormat, id);
            this.Faces = new List<Face>();

            //Sort edges such that they form a closed loop
            (bool isloop, var orderedEdges) = EdgeAlgorithms.OrderLoop(edges);
            if (!isloop)
            {
                throw new ArgumentException("The edges do not form a closed loop");
            }

            this.edges = new ObservableCollection<PEdge>(orderedEdges.Select(x => new PEdge(x, GeometricOrientation.Undefined, this)));
            this.edges.CollectionChanged += Edges_CollectionChanged;

            this.ModelGeometry.EdgeLoops.Add(this);

            MakeConsistent(false, true);
        }

        /// <inheritdoc />
        public override void MakeConsistent(bool notifyGeometryChanged, bool hasTopologyChanged)
        {
            if (hasTopologyChanged)
            {
                this.Faces.Clear();

                MakePEdgesConsistent();
            }

            OnGeometryChanged(notifyGeometryChanged);
            OnTopologyChanged();
        }

        private void MakePEdgesConsistent()
        {
            //Sort pedges
            (var isClosed, var sortedEdges) = EdgeAlgorithms.OrderLoop(this.Edges);

            if (!isClosed)
                throw new Exception("The edges do not form a closed loop");

            this.Edges.CollectionChanged -= Edges_CollectionChanged;
            this.Edges.Clear();
            this.Edges.AddRange(sortedEdges);
            this.Edges.CollectionChanged += Edges_CollectionChanged;

            //Order PEdges correctly
            var commonVertex = Edges.Last().Edge.Vertices[0];
            if (!Edges.First().Edge.Vertices.Contains(commonVertex))
                commonVertex = Edges.Last().Edge.Vertices[1];

            foreach (var e in Edges)
            {
                if (e.Edge.Vertices[0] == commonVertex)
                    e.Orientation = GeometricOrientation.Forward;
                else
                    e.Orientation = GeometricOrientation.Backward;

                commonVertex = e.Edge.Vertices.First(x => x != commonVertex);
            }

            foreach (var e in this.Edges)
            {
                e.MakeConsistent();
                e.Parent = this;
                e.Edge.GeometryChanged -= Edge_GeometryChanged;
                e.Edge.GeometryChanged += Edge_GeometryChanged;
                e.Edge.TopologyChanged -= Edge_TopologyChanged;
                e.Edge.TopologyChanged += Edge_TopologyChanged;
            }

            //Set prev and next ptr
            for (int i = 0; i < this.Edges.Count; ++i)
            {
                this.Edges[i].Next = this.Edges[(i + 1) % this.Edges.Count];
                this.Edges[(i + 1) % this.Edges.Count].Prev = this.Edges[i];
            }
        }

        private void Edge_TopologyChanged(object sender)
        {
            MakePEdgesConsistent();
            NotifyTopologyChanged();
        }

        private void Edge_GeometryChanged(object sender)
        {
            NotifyGeometryChanged();
        }

        private void Edges_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach (var edge in e.OldItems)
                {
                    ((PEdge)edge).Edge.GeometryChanged -= Edge_GeometryChanged;
                    ((PEdge)edge).Edge.TopologyChanged -= Edge_TopologyChanged;
                }
            }

            if (ModelGeometry.HandleConsistency)
            {
                //Remove old pedges
                if (e.OldItems != null)
                {
                    foreach (var edge in e.OldItems)
                    {
                        var pedge = (PEdge)edge;
                        pedge.Edge.PEdges.Remove(pedge);
                    }
                }

                MakePEdgesConsistent();
            }

            NotifyTopologyChanged();
        }

        /// <inheritdoc/>
        public override bool RemoveFromModel()
        {
            bool result = this.ModelGeometry.EdgeLoops.Remove(this);
            this.Edges.ForEach(x => x.Edge.PEdges.Remove(x));

            return result;
        }
        /// <inheritdoc/>
        public override void AddToModel()
        {
            if (this.ModelGeometry.EdgeLoops.Contains(this))
                throw new Exception("Geometry is already present in the model");

            this.ModelGeometry.EdgeLoops.Add(this);

            if (ModelGeometry.HandleConsistency)
            {
                this.Edges.ForEach(x => x.Edge.PEdges.Add(x));
            }

            if (this.Layer != null && !this.Layer.Elements.Contains(this))
                this.Layer.Elements.Add(this);
        }
    }
}
