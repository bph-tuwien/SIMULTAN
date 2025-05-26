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
        /// Base Edge of the Face as defined by the user.
        /// Can be used to get a consistent base direction for the Face
        /// </summary>
        public Edge BaseEdge
        {
            get { return baseEdge; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException();

                if (value != baseEdge)
                {
                    if (value.PEdges.Any(x => x.Parent == this))
                    {
                        baseEdge = value;
                        NotifyPropertyChanged(nameof(BaseEdge));

                        if (this.ModelGeometry.HandleConsistency)
                        {
                            MakePEdgesConsistent();
                        }
                        NotifyTopologyChanged();
                    }
                    else
                        throw new ArgumentException($"BaseEdge is not part of the EdgeLoop {this.Name} (Id={this.Id}");
                }
            }
        }
        private Edge baseEdge;

        /// <summary>
        /// Orientation of the <see cref="BaseEdge"/>. Defines the starting orientation for ordering the PEdges
        /// </summary>
        public GeometricOrientation BaseEdgeOrientation
        {
            get { return baseEdgeOrientation; }
            set
            {
                baseEdgeOrientation = value;
                NotifyPropertyChanged(nameof(BaseEdgeOrientation));

                if (this.ModelGeometry.HandleConsistency)
                {
                    MakePEdgesConsistent();
                }
                NotifyTopologyChanged();
            }
        }
        private GeometricOrientation baseEdgeOrientation = GeometricOrientation.Forward;

        /// <summary>
        /// Initializes a new instance of the EdgeLoop class
        /// </summary>
        /// <param name="layer">The layer this object is placed on</param>
        /// <param name="nameFormat">The display name</param>
        /// <param name="edges">A list of edges in this loop</param>
        /// <param name="baseEdge">Base edge of the loop. Needs to be part of the edges of this loop</param>
        /// <param name="baseEdgeOrientation">Orientation of the baseEdge. Defines the starting orientation for ordering the PEdges</param>
        public EdgeLoop(Layer layer, string nameFormat, IEnumerable<Edge> edges, Edge baseEdge = null,
            GeometricOrientation baseEdgeOrientation = GeometricOrientation.Undefined)
            : this(layer != null ? layer.Model.GetFreeId() : ulong.MaxValue, layer, nameFormat, edges, baseEdge, baseEdgeOrientation) { }
        /// <summary>
        /// Initializes a new instance of the EdgeLoop class
        /// </summary>
        /// <param name="id">The unique identifier for this object</param>
        /// <param name="layer">The layer this object is placed on</param>
        /// <param name="nameFormat">The display name</param>
        /// <param name="edges">A list of edges in this loop</param>
        /// <param name="baseEdge">Base edge of the loop. Needs to be part of the edges of this loop</param>
        /// <param name="baseEdgeOrientation">Orientation of the baseEdge. Defines the starting orientation for ordering the PEdges</param>
        public EdgeLoop(ulong id, Layer layer, string nameFormat, IEnumerable<Edge> edges,
            Edge baseEdge = null, GeometricOrientation baseEdgeOrientation = GeometricOrientation.Undefined)
            : base(id, layer)
        {
            if (edges == null)
                throw new ArgumentNullException(nameof(edges));
            if (edges.Count() < 3)
                throw new ArgumentException("EdgeLoops need at least three edges");
            if (nameFormat == null)
                throw new ArgumentNullException(nameof(nameFormat));
            if (baseEdge != null && !edges.Contains(baseEdge))
                throw new ArgumentException("BaseEdge is not part of the edges");
            if (baseEdge != null && baseEdgeOrientation == GeometricOrientation.Undefined)
                throw new ArgumentException("Undefined orientation is not supported when a baseEdge is supplied");

            this.Name = string.Format(nameFormat, id);
            this.Faces = new List<Face>();

            if (baseEdge != null)
            {
                this.baseEdge = baseEdge; //Do not use Property since it checks if this BaseEdge is in the edge list (which it isn't during setup)
                this.baseEdgeOrientation = baseEdgeOrientation;
            }

            //Sort edges such that they form a closed loop
            (bool isloop, var orderedEdges) = baseEdge == null ? EdgeAlgorithms.OrderLoop(edges) : EdgeAlgorithms.OrderLoop(edges, baseEdge, baseEdgeOrientation);
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
            var baseEdge = this.BaseEdge;
            var baseEdgeOrientation = this.BaseEdgeOrientation;
            bool baseEdgeExists = BaseEdge != null && Edges.Any(pe => pe.Edge == BaseEdge);

            if (!baseEdgeExists)
            {
                baseEdge = this.Edges.First().Edge;
                baseEdgeOrientation = this.Edges.First().Orientation;
                if (baseEdgeOrientation == GeometricOrientation.Undefined)
                    baseEdgeOrientation = GeometricOrientation.Forward;
            }

            //Sort pedges
            (var isClosed, var sortedEdges) = EdgeAlgorithms.OrderLoop(this.Edges, baseEdge, baseEdgeOrientation);

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

            //Invalid BaseEdge -> guess new base edge
            if (!baseEdgeExists)
            {
                GuessBaseEdge();
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
            this.Edges.ForEach(x =>
            {
                x.Edge.GeometryChanged -= Edge_GeometryChanged;
                x.Edge.TopologyChanged -= Edge_TopologyChanged;
                x.Edge.PEdges.Remove(x);
            });

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


        private void GuessBaseEdge()
        {
            PEdge bestEdge = null;

            var normal = EdgeLoopAlgorithms.NormalCCW(this);

            if (!FaceAlgorithms.IsFloor(normal) && !FaceAlgorithms.IsCeiling(normal)) //Wall mode
            {
                //Try to find an edge in the XZ plane, then take the one with the lowest Y value
                double bestY = double.PositiveInfinity;
                foreach (var edge in this.Edges)
                {
                    if ((edge.StartVertex.Position.Y - edge.EndVertex.Position.Y < 0.00001) && edge.StartVertex.Position.Y < bestY)
                    {
                        bestEdge = edge;
                        bestY = edge.StartVertex.Position.Y;
                    }
                }

            }

            if (bestEdge == null) //Either wall not found or no wall
            {
                bestEdge = Edges.First();
            }

            this.baseEdge = bestEdge.Edge;
            this.baseEdgeOrientation = bestEdge.Orientation;

            if (this.Edges[0].Edge != this.baseEdge || this.Edges[0].Orientation != baseEdgeOrientation) //Resort
            {
                MakePEdgesConsistent();
            }
        }
    }
}
